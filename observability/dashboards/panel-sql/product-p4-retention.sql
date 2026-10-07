SELECT to_timestamp_micros(a.day_index * 86400000000) AS day,
       count(DISTINCT a.player_id) AS dau,
       count(DISTINCT b.player_id) AS returned_d1
FROM (SELECT DISTINCT player_id, floor(_timestamp / 86400000000) AS day_index FROM "product" WHERE activity_kind = 'real') a
LEFT JOIN (SELECT DISTINCT player_id, floor(_timestamp / 86400000000) AS day_index FROM "product" WHERE activity_kind = 'real') b
  ON b.player_id = a.player_id AND b.day_index = a.day_index + 1
GROUP BY a.day_index ORDER BY a.day_index
