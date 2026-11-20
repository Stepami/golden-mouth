import fs from 'fs';

const historyMs = 90 * 86400000;
const recentMs = 15 * 60000;
const uuid = /^(?:[0-9a-f]{32}|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i;
const queryTemplate = fs.readFileSync('/etc/nginx/metrika-goals/query.sql', 'utf8');

function action(kind, compactId) {
    if (kind === 'world') {
        return {
            event: 'world_created',
            field: 'world_id',
            id: [compactId.slice(0, 8), compactId.slice(8, 12), compactId.slice(12, 16),
                compactId.slice(16, 20), compactId.slice(20)].join('-'),
        };
    }
    if (kind === 'post') return { event: 'post_scored', field: 'prop_id', id: compactId };
    if (kind === 'comment') return { event: 'comment_created', field: 'prop_id', id: compactId };
    return null;
}

async function post(r) {
    r.headersOut['Content-Type'] = 'application/json';
    r.headersOut['Cache-Control'] = 'no-store';
    if (r.method !== 'POST') {
        r.headersOut.Allow = 'POST';
        return r.return(405);
    }
    if (!/^application\/json(?:\s*;|$)/i.test(r.headersIn['Content-Type'] || '') || r.variables.args) {
        return r.return(400);
    }

    let body;
    try {
        body = JSON.parse(r.requestText);
    } catch (error) {
        return r.return(400);
    }
    if (!body || typeof body !== 'object' || Object.keys(body).length !== 2 ||
        typeof body.id !== 'string' || !uuid.test(body.id)) {
        return r.return(400);
    }

    const compactId = body.id.toLowerCase().replace(/-/g, '');
    const target = action(body.kind, compactId);
    if (!target) return r.return(400);

    const authorization = process.env.METRIKA_OPENOBSERVE_BASIC_AUTH;
    if (!authorization) return r.return(503);

    const now = Date.now();
    const start = now - historyMs;
    const sql = queryTemplate.replace(/__EVENT__/g, target.event)
        .replace('__ID_FIELD__', target.field)
        .replace('__LOOKUP_ID__', target.id)
        .replace(/__COMPACT_ID__/g, compactId)
        .replace('__RECENT_MS__', String(now - recentMs))
        .replace('__NOW_MS__', String(now))
        .replace(/__HISTORY_MS__/g, String(start));

    try {
        const response = await ngx.fetch('http://openobserve:5080/api/default/_search?type=logs', {
            method: 'POST',
            headers: { Authorization: authorization, 'Content-Type': 'application/json' },
            body: JSON.stringify({
                query: { sql, start_time: start * 1000, end_time: now * 1000, size: 1 },
                timeout: 3,
            }),
        });
        if (!response.ok) return r.return(502);
        const result = await response.json();
        if (result.is_partial || (result.function_error && result.function_error.length) || !Array.isArray(result.hits) || result.hits.length > 1) {
            return r.return(502);
        }
        if (result.hits.length === 0) return r.return(202);

        const row = result.hits[0];
        if (!row || ![0, 1].includes(row.ordinary_post) || ![0, 1].includes(row.first_referred_action) ||
            [row.earlier_daily_actions, row.earlier_daily_posts].some(value => !Number.isSafeInteger(value) || value < 0)) {
            return r.return(502);
        }

        const goals = [];
        if (row.earlier_daily_actions === 0) goals.push('dau');
        if (row.first_referred_action === 1) goals.push('invited_friend_dau');
        if (body.kind === 'post' && row.ordinary_post === 1) {
            if (row.earlier_daily_posts === 0) goals.push('post_1_daily');
            if (row.earlier_daily_posts === 2) goals.push('post_3_daily');
        }
        return r.return(200, JSON.stringify(goals));
    } catch (error) {
        return r.return(502);
    }
}

export default { post };
