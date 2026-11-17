import fs from 'fs';

const DAY_MS = 86400000;
const MSK_OFFSET_MS = 10800000;
const SOURCE_DIR = '/etc/nginx/daily-metrics';

function midnight(date) {
    if (typeof date !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
        return NaN;
    }

    const utc = Date.parse(date + 'T00:00:00Z');
    return Number.isFinite(utc) && new Date(utc).toISOString().slice(0, 10) === date
        ? utc - MSK_OFFSET_MS
        : NaN;
}

function moscowTime(timestamp) {
    return new Date(timestamp + MSK_OFFSET_MS).toISOString().replace('Z', '+03:00');
}

function tokenEquals(provided, expected) {
    if (provided.length !== expected.length) {
        return false;
    }

    let difference = 0;
    for (let i = 0; i < expected.length; i++) {
        difference |= provided.charCodeAt(i) ^ expected.charCodeAt(i);
    }
    return difference === 0;
}

function fail(r, status, error) {
    r.return(status, JSON.stringify({ error }));
}

async function get(r) {
    r.headersOut['Content-Type'] = 'application/json';
    r.headersOut['Cache-Control'] = 'no-store';
    if (r.method !== 'GET') {
        r.headersOut.Allow = 'GET';
        return fail(r, 405, 'Method not allowed');
    }

    const apiToken = process.env.METRICS_API_TOKEN || '';
    const account = process.env.METRICS_OPENOBSERVE_ACCOUNT;
    const openObserveToken = process.env.METRICS_OPENOBSERVE_TOKEN;
    if (apiToken.length < 32 || !account || !openObserveToken || apiToken === openObserveToken) {
        return fail(r, 503, 'Metrics export is not configured');
    }

    const auth = /^Bearer ([A-Za-z0-9._~+/-]+=*)$/i.exec(r.headersIn.Authorization || '');
    if (!auth || !tokenEquals(auth[1], apiToken)) {
        r.headersOut['WWW-Authenticate'] = 'Bearer';
        return fail(r, 401, 'Invalid or missing token');
    }

    const start = midnight(r.args.date_from);
    const end = midnight(r.args.date_to) + DAY_MS;
    const now = Date.now();
    const count = (end - start) / DAY_MS;
    if (!Number.isFinite(count) || count < 1 || count > 90 || end - DAY_MS > now) {
        return fail(r, 400, 'Use a valid date_from and date_to in YYYY-MM-DD, up to 90 days through today');
    }

    const excludedIds = (process.env.METRICS_EXCLUDED_PLAYER_IDS || '').trim().toLowerCase();
    const excluded = excludedIds ? excludedIds.split(/\s*,\s*/) : [];
    if (excluded.some(id => !/^[a-f0-9]{32}$/.test(id))) {
        return fail(r, 503, 'Metrics export is not configured');
    }

    try {
        const ids = excluded.map(id => "'" + id + "'").join(', ') || "''";
        const sql = fs.readFileSync(SOURCE_DIR + '/query.sql', 'utf8')
            .replace(/__EXCLUDED_PLAYERS__/g, ids)
            .replace(/__START_TIME__/g, String(start * 1000));
        const dates = [];
        for (let time = start; time < end; time += DAY_MS) {
            dates.push(moscowTime(time).slice(0, 10));
        }
        const query_fn = fs.readFileSync(SOURCE_DIR + '/result.vrl', 'utf8')
            .replace('__DATES__', JSON.stringify(dates));

        const response = await ngx.fetch('http://openobserve:5080/api/default/_search?type=logs', {
            method: 'POST',
            headers: {
                Authorization: 'Basic ' + Buffer.from(account + ':' + openObserveToken).toString('base64'),
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                query: {
                    sql,
                    query_fn,
                    start_time: excluded.length ? 1 : (start - DAY_MS) * 1000,
                    end_time: Math.min(end, now) * 1000 - 1,
                    size: count,
                },
                timeout: 8,
            }),
        });

        if (!response.ok) {
            r.warn('OpenObserve metrics search returned ' + response.status);
            return fail(r, 502, 'Metrics search failed');
        }

        const result = await response.json();
        if (result.is_partial || (result.function_error && result.function_error.length) || !Array.isArray(result.hits) ||
            result.hits.length !== count || result.hits.some((row, i) =>
                row.date !== dates[i] || typeof row.is_final !== 'boolean' ||
                [row.dau, row.llm_requests, row.tool_requests, row.total_users, row.new_users]
                    .some(value => !Number.isSafeInteger(value) || value < 0))) {
            return fail(r, 503, 'Metrics are unavailable for the requested dates');
        }

        const data = result.hits.map(row => ({
            date: row.date,
            dau: row.dau,
            llm_requests: row.llm_requests,
            tool_requests: row.tool_requests,
            total_users: row.total_users,
            new_users: row.new_users,
            is_final: row.is_final,
        }));
        r.return(200, JSON.stringify({
            startup_id: '29378',
            timezone: 'Europe/Moscow',
            generated_at: moscowTime(Date.now()),
            data,
        }));
    } catch (error) {
        return fail(r, 502, 'Metrics search failed');
    }
}

export default { get };
