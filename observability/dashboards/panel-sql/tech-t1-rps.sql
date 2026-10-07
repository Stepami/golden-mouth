SELECT histogram(_timestamp, '1 hour') AS ts, count(*) AS requests
FROM "backend" WHERE sourcecontext = 'Microsoft.AspNetCore.Hosting.Diagnostics' AND path IS NOT NULL
GROUP BY ts ORDER BY ts
