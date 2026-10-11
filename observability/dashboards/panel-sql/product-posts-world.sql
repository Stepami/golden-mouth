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
 FROM daily d LEFT JOIN time_daily t ON d.player_id = t.player_id AND d.day = t.day), buckets AS (SELECT s.segment AS segment, CASE WHEN m.posts = 0 THEN '0 постов' WHEN m.posts = 1 THEN '1 пост' WHEN m.posts < 5 THEN '2–4 поста' WHEN m.posts < 10 THEN '5–9 постов' ELSE '10+ постов' END AS activity_bucket, count(*) AS player_days
 FROM measured m JOIN segments s ON m.player_id = s.player_id WHERE s.dimension = 'world' GROUP BY s.segment, activity_bucket)
SELECT segment, activity_bucket, round(100.0 * player_days / sum(player_days) OVER (PARTITION BY segment), 1) AS player_days_pct
FROM buckets ORDER BY segment, activity_bucket
