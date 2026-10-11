SELECT histogram(_timestamp, '1 hour') AS ts, severity, count(*) AS n
FROM "backend" WHERE severity IN ('Error','Critical','Fatal')
GROUP BY ts, severity ORDER BY ts
