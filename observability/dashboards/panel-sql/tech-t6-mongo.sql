SELECT coalesce(db_query_summary, db_operation_summary, db_operation_name, db_command_name, operation_name) AS operation, count(*) AS calls, avg(duration) AS avg_us, max(duration) AS max_us
FROM "backend" WHERE db_system_name = 'mongodb'
GROUP BY operation ORDER BY calls DESC
