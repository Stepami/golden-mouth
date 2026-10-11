WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id), scored AS (SELECT player_id, count(*) AS posts FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name = 'post_scored' GROUP BY player_id), segments AS (SELECT 'all' AS dimension, 'all' AS segment, player_id FROM players UNION ALL SELECT 'world' AS dimension, world AS segment, player_id FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id FROM players), totals AS (
SELECT s.dimension AS dimension, s.segment AS segment, count(*) AS users,
 sum(CASE WHEN c.posts >= 1 THEN 1 ELSE 0 END) AS reached_1,
 sum(CASE WHEN c.posts >= 3 THEN 1 ELSE 0 END) AS reached_3,
 sum(CASE WHEN c.posts >= 10 THEN 1 ELSE 0 END) AS reached_10
FROM segments s LEFT JOIN scored c ON s.player_id = c.player_id GROUP BY s.dimension, s.segment)
SELECT concat(dimension, ': ', segment) AS segment, '1 пост' AS milestone, round(100.0 * reached_1 / users, 1) AS users_pct FROM totals
UNION ALL SELECT concat(dimension, ': ', segment) AS segment, '3 поста' AS milestone, round(100.0 * reached_3 / users, 1) AS users_pct FROM totals
UNION ALL SELECT concat(dimension, ': ', segment) AS segment, '10 постов' AS milestone, round(100.0 * reached_10 / users, 1) AS users_pct FROM totals
