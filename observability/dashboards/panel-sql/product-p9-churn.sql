WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time') GROUP BY player_id), segments AS (
 SELECT 'all' AS dimension, 'all' AS segment, player_id FROM players
 UNION ALL SELECT 'world' AS dimension, world AS segment, player_id FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id FROM players),
 activity AS (SELECT player_id,
 max(CASE WHEN floor(_timestamp / 86400000000) >= floor(date_part('epoch', now()) * 1000000 / 86400000000) - 14 AND floor(_timestamp / 86400000000) < floor(date_part('epoch', now()) * 1000000 / 86400000000) - 7 THEN 1 ELSE 0 END) AS previous_week,
 max(CASE WHEN floor(_timestamp / 86400000000) >= floor(date_part('epoch', now()) * 1000000 / 86400000000) - 7 AND floor(_timestamp / 86400000000) < floor(date_part('epoch', now()) * 1000000 / 86400000000) THEN 1 ELSE 0 END) AS last_week
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time') GROUP BY player_id),
 coverage AS (SELECT min(_timestamp) AS first_event FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time'))
SELECT s.dimension AS dimension, s.segment AS segment,
 CASE WHEN min(c.first_event) <= (floor(date_part('epoch', now()) * 1000000 / 86400000000) - 14) * 86400000000 THEN sum(a.previous_week) END AS eligible_users,
 CASE WHEN min(c.first_event) <= (floor(date_part('epoch', now()) * 1000000 / 86400000000) - 14) * 86400000000 THEN sum(CASE WHEN a.previous_week = 1 AND a.last_week = 0 THEN 1 ELSE 0 END) END AS inactive_users,
 CASE WHEN min(c.first_event) <= (floor(date_part('epoch', now()) * 1000000 / 86400000000) - 14) * 86400000000 THEN
 round(100.0 * sum(CASE WHEN a.previous_week = 1 AND a.last_week = 0 THEN 1 ELSE 0 END) / nullif(sum(a.previous_week), 0), 1) END AS weekly_churn_pct
FROM segments s JOIN activity a ON s.player_id = a.player_id CROSS JOIN coverage c
GROUP BY s.dimension, s.segment ORDER BY s.dimension, s.segment
