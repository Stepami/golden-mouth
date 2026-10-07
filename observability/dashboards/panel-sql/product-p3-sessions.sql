SELECT histogram(_timestamp, '1 day') AS day, count(DISTINCT session_id) AS sessions, count(*) AS events
FROM "product" WHERE activity_kind = 'real'
GROUP BY day ORDER BY day
