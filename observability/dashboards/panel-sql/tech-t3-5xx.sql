SELECT histogram(_timestamp, '1 hour') AS ts, count(*) AS errors
FROM "backend" WHERE sourcecontext = 'Serilog.AspNetCore.RequestLoggingMiddleware' AND statuscode BETWEEN 500 AND 599
GROUP BY ts ORDER BY ts
