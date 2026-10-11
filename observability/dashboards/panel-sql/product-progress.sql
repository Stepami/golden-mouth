WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id), scored AS (SELECT player_id, _timestamp, row_number() OVER (PARTITION BY player_id ORDER BY _timestamp) AS post_number FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name = 'post_scored'),
 milestones AS (SELECT player_id, min(CASE WHEN post_number = 1 THEN _timestamp END) AS first_post, min(CASE WHEN post_number = 3 THEN _timestamp END) AS third_post FROM scored GROUP BY player_id),
 awards AS (SELECT player_id, min(CASE WHEN event_name = 'level_up' THEN _timestamp END) AS first_level,
 min(CASE WHEN event_name = 'card_earned' THEN _timestamp END) AS first_card FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id),
 segments AS (SELECT 'all' AS dimension, 'all' AS segment, player_id FROM players UNION ALL SELECT 'world' AS dimension, world AS segment, player_id FROM players UNION ALL SELECT 'age_group' AS dimension, age_group AS segment, player_id FROM players)
SELECT s.dimension AS dimension, s.segment AS segment, count(*) AS observed_users,
 count(m.first_post) AS first_post_users, round(100.0 * count(m.first_post) / count(*), 1) AS first_post_pct,
 count(m.third_post) AS third_post_users, round(100.0 * count(m.third_post) / count(*), 1) AS third_post_pct,
 round(approx_percentile_cont((m.third_post - p.first_seen) / 3600000000.0, 0.5), 2) AS median_hours_to_3_posts,
 count(a.first_level) AS level_up_users, round(100.0 * count(a.first_level) / count(*), 1) AS level_up_pct,
 count(a.first_card) AS card_users, round(100.0 * count(a.first_card) / count(*), 1) AS card_pct
FROM players p JOIN segments s ON p.player_id = s.player_id LEFT JOIN milestones m ON p.player_id = m.player_id LEFT JOIN awards a ON p.player_id = a.player_id
GROUP BY s.dimension, s.segment ORDER BY s.dimension, s.segment
