SELECT operation_name, count(*) AS calls, avg(duration) AS avg_us, max(duration) AS max_us
FROM "backend" WHERE operation_name LIKE 'chat %'
GROUP BY operation_name ORDER BY calls DESC
