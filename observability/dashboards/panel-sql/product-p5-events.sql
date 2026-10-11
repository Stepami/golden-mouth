SELECT event_name, count(DISTINCT player_id) AS players, count(*) AS events
FROM "product" WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''
GROUP BY event_name ORDER BY events DESC
