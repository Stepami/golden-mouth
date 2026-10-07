SELECT db_query_summary, count(*) AS calls, avg(duration) AS avg_us, max(duration) AS max_us
FROM "backend" WHERE db_system_name = 'mongodb'
GROUP BY db_query_summary ORDER BY calls DESC
