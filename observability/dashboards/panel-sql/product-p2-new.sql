SELECT histogram(first_seen, '1 day') AS day, count(*) AS new_players
FROM (SELECT player_id, min(_timestamp) AS first_seen FROM "product" WHERE activity_kind = 'real' GROUP BY player_id)
GROUP BY day ORDER BY day
