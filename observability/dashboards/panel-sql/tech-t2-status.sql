SELECT histogram(_timestamp, '1 hour') AS ts, cast(statuscode AS varchar) AS statuscode, count(*) AS n
FROM "backend" WHERE sourcecontext = 'Serilog.AspNetCore.RequestLoggingMiddleware' AND statuscode IS NOT NULL
GROUP BY ts, statuscode ORDER BY ts
