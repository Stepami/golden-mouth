SELECT histogram(_timestamp, '1 hour') AS ts, count(*) AS errors
FROM "backend" WHERE statuscode >= 500
GROUP BY ts ORDER BY ts
