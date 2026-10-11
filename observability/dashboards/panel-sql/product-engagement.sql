WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id), segments AS (
 SELECT 'all' AS dimension, 'all' AS segment, player_id FROM players UNION ALL SELECT 'world' AS dimension, world AS segment, player_id FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id FROM players),
 daily AS (SELECT player_id, floor(_timestamp / 86400000000) AS day,
 sum(CASE WHEN event_name = 'post_scored' THEN 1 ELSE 0 END) AS posts,
 sum(CASE WHEN event_name = 'active_time' THEN try_cast(prop_ms AS DOUBLE) END) AS active_ms,
 max(CASE WHEN event_name = 'level_up' THEN 1 ELSE 0 END) AS level_up,
 max(CASE WHEN event_name = 'card_earned' THEN 1 ELSE 0 END) AS card_earned
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND floor(_timestamp / 86400000000) >= floor(date_part('epoch', now()) * 1000000 / 86400000000) - 7 AND floor(_timestamp / 86400000000) < floor(date_part('epoch', now()) * 1000000 / 86400000000)
 AND event_name NOT IN ('notification_sent', 'active_time') GROUP BY player_id, day
 HAVING max(CASE WHEN event_name NOT IN ('notification_sent', 'active_time') THEN 1 ELSE 0 END) = 1),
 time_daily AS (SELECT player_id, floor(_timestamp / 86400000000) AS day, sum(try_cast(prop_ms AS DOUBLE)) AS active_ms
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name = 'active_time' AND floor(_timestamp / 86400000000) >= floor(date_part('epoch', now()) * 1000000 / 86400000000) - 7 AND floor(_timestamp / 86400000000) < floor(date_part('epoch', now()) * 1000000 / 86400000000) GROUP BY player_id, day),
 measured AS (SELECT d.player_id AS player_id, d.day AS day, d.posts AS posts, t.active_ms AS active_ms, d.level_up AS level_up, d.card_earned AS card_earned
 FROM daily d LEFT JOIN time_daily t ON d.player_id = t.player_id AND d.day = t.day)
SELECT s.dimension AS dimension, s.segment AS segment, count(DISTINCT m.player_id) AS active_users,
 count(*) AS active_player_days, sum(m.posts) AS completed_posts,
 round(1.0 * sum(m.posts) / count(DISTINCT m.player_id), 2) AS posts_per_user_7d,
 round(1.0 * sum(m.posts) / count(*), 2) AS posts_per_active_day,
 count(m.active_ms) AS measured_player_days,
 round(avg(m.active_ms) / 60000.0, 2) AS avg_active_minutes,
 round(approx_percentile_cont(m.active_ms, 0.5) / 60000.0, 2) AS median_active_minutes
FROM measured m JOIN segments s ON m.player_id = s.player_id GROUP BY s.dimension, s.segment ORDER BY s.dimension, active_users DESC
