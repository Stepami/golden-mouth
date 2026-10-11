SELECT 'Наблюдаемые игроки' AS population, count(*) AS observed_users,
 sum(published) AS publishers, sum(saw_result) AS result_users,
 round(100.0 * sum(published) / nullif(count(*), 0), 1) AS publish_pct,
 round(100.0 * sum(saw_result) / nullif(count(*), 0), 1) AS result_pct
FROM (SELECT player_id, min(_timestamp) AS first_seen, max(_timestamp) AS last_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT nullif(age_group, '')) > 1 THEN 'multi-age-group' ELSE coalesce(max(nullif(age_group, '')), 'unknown') END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform'
 ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform,
 max(CASE WHEN event_name = 'post_publish' THEN 1 ELSE 0 END) AS published,
 max(CASE WHEN event_name = 'post_result' THEN 1 ELSE 0 END) AS saw_result
FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' GROUP BY player_id)
