SELECT histogram(_timestamp, '1 day') AS day, count(DISTINCT player_id) AS dau
FROM "product" WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time')
GROUP BY day ORDER BY day
