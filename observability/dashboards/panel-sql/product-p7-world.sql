SELECT world, count(DISTINCT player_id) AS players, count(*) AS events
FROM "product" WHERE activity_kind = 'real'
GROUP BY world ORDER BY players DESC
