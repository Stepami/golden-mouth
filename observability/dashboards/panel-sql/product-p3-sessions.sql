SELECT histogram(_timestamp, '1 day') AS day, count(DISTINCT session_id) AS sessions, count(*) AS events
FROM "product" WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''
GROUP BY day ORDER BY day
