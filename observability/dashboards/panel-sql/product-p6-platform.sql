SELECT platform, count(DISTINCT player_id) AS players, count(*) AS events
FROM "product" WHERE activity_kind = 'real'
GROUP BY platform ORDER BY players DESC
