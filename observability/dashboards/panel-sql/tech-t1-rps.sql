SELECT histogram(_timestamp, '1 hour') AS ts, count(*) AS requests
FROM "backend" WHERE sourcecontext = 'Serilog.AspNetCore.RequestLoggingMiddleware' AND statuscode IS NOT NULL
GROUP BY ts ORDER BY ts
