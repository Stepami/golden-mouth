WITH players AS (SELECT player_id, min(_timestamp) AS first_seen, max(_timestamp) AS last_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT nullif(age_group, '')) > 1 THEN 'multi-age-group' ELSE coalesce(max(nullif(age_group, '')), 'unknown') END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform'
 ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform,
 max(CASE WHEN event_name = 'post_publish' THEN 1 ELSE 0 END) AS published,
 max(CASE WHEN event_name = 'post_result' THEN 1 ELSE 0 END) AS saw_result
FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id), segments AS (SELECT 'world' AS dimension, world AS segment, player_id, first_seen, last_seen, published, saw_result FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id, first_seen, last_seen, published, saw_result FROM players UNION ALL SELECT 'platform' AS dimension, platform AS segment, player_id, first_seen, last_seen, published, saw_result FROM players)
SELECT dimension, segment, count(*) AS observed_users,
 sum(CASE WHEN last_seen >= date_part('epoch', now()) * 1000000 - 30 * 86400000000 THEN 1 ELSE 0 END) AS mau_30d,
 sum(CASE WHEN last_seen >= date_part('epoch', now()) * 1000000 - 7 * 86400000000 THEN 1 ELSE 0 END) AS wau_7d,
 sum(CASE WHEN floor(last_seen / 86400000000) = floor(date_part('epoch', now()) * 1000000 / 86400000000) THEN 1 ELSE 0 END) AS dau_today,
 sum(published) AS publishers, round(100.0 * sum(published) / count(*), 1) AS publish_pct
FROM segments GROUP BY dimension, segment ORDER BY dimension, observed_users DESC
