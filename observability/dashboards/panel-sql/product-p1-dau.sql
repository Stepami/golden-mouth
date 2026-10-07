SELECT histogram(_timestamp, '1 day') AS day, count(DISTINCT player_id) AS dau
FROM "product" WHERE activity_kind = 'real'
GROUP BY day ORDER BY day
