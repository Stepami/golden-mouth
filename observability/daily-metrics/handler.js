import fs from 'fs';

const DAY_MS = 86400000;
const MSK_OFFSET_MS = 10800000;
const RETENTION_DAYS = [1, 3, 7, 14];
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

function fail(r, status, error) {
    if (status === 401) {
        r.headersOut['WWW-Authenticate'] = 'Bearer';
    }
    r.return(status, JSON.stringify({ error }));
}

async function get(r) {
    r.headersOut['Content-Type'] = 'application/json';
    r.headersOut['Cache-Control'] = 'no-store';
    if (r.method !== 'GET') {
        r.headersOut.Allow = 'GET';
        return fail(r, 405, 'Method not allowed');
    }

    const auth = /^Bearer ([A-Za-z0-9._~+/-]+=*)$/i.exec(r.headersIn.Authorization || '');
    if (!auth) {
        return fail(r, 401, 'Invalid or missing token');
    }

    const start = midnight(r.args.date_from);
    const end = midnight(r.args.date_to) + DAY_MS;
    const now = Date.now();
    const count = (end - start) / DAY_MS;
    if (!Number.isFinite(count) || count < 1 || count > 90 || end - DAY_MS > now) {
        return fail(r, 400, 'Use a valid date_from and date_to in YYYY-MM-DD, up to 90 days through today');
    }

    try {
        const sql = fs.readFileSync(SOURCE_DIR + '/query.sql', 'utf8')
            .replace(/__START_TIME__/g, String(start * 1000))
            .replace(/__END_TIME__/g, String(end * 1000))
            .replace(/__END_DAY__/g, String((end + MSK_OFFSET_MS) / DAY_MS));
        const dates = [];
        for (let time = start; time < end; time += DAY_MS) {
            dates.push(moscowTime(time).slice(0, 10));
        }
        const query_fn = fs.readFileSync(SOURCE_DIR + '/result.vrl', 'utf8')
            .replace('__DATES__', JSON.stringify(dates));

        const response = await ngx.fetch('http://openobserve:5080/api/default/_search?type=logs', {
            method: 'POST',
            headers: {
                Authorization: 'Basic ' + auth[1],
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                query: {
                    sql,
                    query_fn,
                    start_time: (start - DAY_MS) * 1000,
                    end_time: Math.min(end + 14 * DAY_MS, now) * 1000 - 1,
                    size: count,
                },
                timeout: 8,
            }),
        });

        if (response.status === 401 || response.status === 403) {
            return fail(r, 401, 'Invalid or unauthorized OpenObserve token');
        }
        if (!response.ok) {
            r.warn('OpenObserve metrics search returned ' + response.status);
            return fail(r, 502, 'Metrics search failed');
        }

        const result = await response.json();
        if (result.is_partial || (result.function_error && result.function_error.length) || !Array.isArray(result.hits) ||
            result.hits.length !== count || result.hits.some((row, i) =>
                row.date !== dates[i] || typeof row.is_final !== 'boolean' ||
                [row.dau, row.llm_requests, row.tool_requests, row.total_users, row.new_users,
                    ...RETENTION_DAYS.map(days => row['retained_d' + days])]
                    .some(value => !Number.isSafeInteger(value) || value < 0))) {
            return fail(r, 503, 'Metrics are unavailable for the requested dates');
        }

        const data = result.hits.map((row, i) => {
            const day = {
                date: row.date,
                dau: row.dau,
                llm_requests: row.llm_requests,
                tool_requests: row.tool_requests,
                total_users: row.total_users,
                new_users: row.new_users,
                is_final: row.is_final,
            };
            for (const days of RETENTION_DAYS) {
                const field = 'retained_d' + days;
                day[field] = start + (i + days + 1) * DAY_MS <= now ? row[field] : null;
            }
            return day;
        });
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
