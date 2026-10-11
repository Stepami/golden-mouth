SELECT count(DISTINCT player_id) AS dau_today
FROM product
WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''
 AND event_name NOT IN ('notification_sent', 'active_time')
 AND _timestamp >= floor(date_part('epoch', now()) * 1000000 / 86400000000) * 86400000000
 AND _timestamp < (floor(date_part('epoch', now()) * 1000000 / 86400000000) + 1) * 86400000000
