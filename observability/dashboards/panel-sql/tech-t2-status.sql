SELECT histogram(_timestamp, '1 hour') AS ts, statuscode, count(*) AS n
FROM "backend" WHERE statuscode IS NOT NULL
GROUP BY ts, statuscode ORDER BY ts
