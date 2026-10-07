SELECT age_group, count(DISTINCT player_id) AS players, count(*) AS events
FROM "product" WHERE activity_kind = 'real'
GROUP BY age_group ORDER BY players DESC
