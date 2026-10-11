WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id), segments AS (SELECT 'all' AS dimension, 'all' AS segment, player_id FROM players UNION ALL SELECT 'world' AS dimension, world AS segment, player_id FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id FROM players),
 last_month AS (SELECT player_id, max(CASE WHEN floor(_timestamp / 86400000000) = floor(date_part('epoch', now()) * 1000000 / 86400000000) - 1 THEN 1 ELSE 0 END) AS yesterday
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND _timestamp >= (floor(date_part('epoch', now()) * 1000000 / 86400000000) - 30) * 86400000000 AND _timestamp < floor(date_part('epoch', now()) * 1000000 / 86400000000) * 86400000000
 AND event_name NOT IN ('notification_sent', 'active_time') GROUP BY player_id)
SELECT s.dimension AS dimension, s.segment AS segment, count(*) AS mau_30d_yesterday,
 sum(m.yesterday) AS dau_yesterday, round(100.0 * sum(m.yesterday) / count(*), 1) AS stickiness_pct
FROM last_month m JOIN segments s ON m.player_id = s.player_id GROUP BY s.dimension, s.segment ORDER BY s.dimension, s.segment
