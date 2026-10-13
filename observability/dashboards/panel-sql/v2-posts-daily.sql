WITH clock AS (
 SELECT floor((max(date_part('epoch', now())) * 1000000 + 10800000000) / 86400000000) AS today,
 max(date_part('epoch', now())) * 1000 AS now_ms, count(*) AS stream_rows FROM product),
world_ranked AS (SELECT player_id, world_id, world, subscriber_total,
 row_number() OVER (PARTITION BY player_id, world_id ORDER BY world_version DESC, _timestamp DESC) AS state_rank
 FROM product WHERE event_name = 'world_audience_updated' AND world_id IS NOT NULL),
world_audience AS (SELECT player_id, sum(subscriber_total) AS subscribers,
 CASE WHEN count(DISTINCT world) > 1 THEN 'multi-world' ELSE max(world) END AS world
 FROM world_ranked WHERE state_rank = 1 GROUP BY player_id),
deleted_players AS (SELECT DISTINCT player_id FROM product
 WHERE event_name = 'player_deleted' AND player_id IS NOT NULL),
raw AS (SELECT p.* FROM product p WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''
 AND NOT EXISTS (SELECT 1 FROM deleted_players d WHERE d.player_id = p.player_id)),
identities AS (SELECT r.player_id, min(CASE WHEN event_name NOT IN ('active_time','notification_sent') THEN _timestamp END) AS first_seen,
 min(CASE WHEN event_name = 'player_created' THEN _timestamp END) AS registered_at,
 coalesce(max(w.world), CASE WHEN count(DISTINCT nullif(nullif(r.world, ''), 'unknown'))>1 THEN 'multi-world' ELSE max(nullif(nullif(r.world, ''), 'unknown')) END, 'unknown') AS world,
 coalesce(max(CASE WHEN age_group <> '18+' THEN nullif(age_group, '') END), max(nullif(age_group, '')), 'unknown') AS age_group,
 coalesce(max(nullif(registration_source, '')), 'unknown') AS registration_source,
 CASE WHEN count(DISTINCT CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END) > 1 THEN 'multi'
 ELSE coalesce(max(CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END), 'unknown') END AS launch_surface
 FROM raw r LEFT JOIN world_audience w ON r.player_id = w.player_id GROUP BY r.player_id),
selected AS (SELECT * FROM identities WHERE
 ('${world}' = 'all' OR world = '${world}') AND ('${age}' = 'all' OR age_group = '${age}')
 AND ('${source}' = 'all' OR registration_source = '${source}')
 AND ('${surface}' = 'all' OR launch_surface = '${surface}')),
visits AS (SELECT DISTINCT player_id, floor((_timestamp + 10800000000) / 86400000000) AS day
 FROM raw WHERE event_name NOT IN ('active_time', 'notification_sent')),
posts AS (SELECT player_id, coalesce(nullif(prop_id, ''), concat(player_id, ':legacy:', cast(event_time_unix_ms AS VARCHAR))) AS post_id,
 max(_timestamp) AS posted_at, max(coalesce(nullif(world, ''), 'unknown')) AS post_world,
 max(cast(prop_delta AS DOUBLE)) AS delta FROM raw WHERE event_name = 'post_scored' GROUP BY player_id, post_id),
selected_posts AS (SELECT p.* FROM posts p JOIN selected s ON p.player_id = s.player_id),
current_profiles AS (SELECT s.player_id, w.subscribers FROM selected s
 LEFT JOIN world_audience w ON s.player_id = w.player_id),
cohort AS (SELECT *, CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END AS cohort_at FROM selected CROSS JOIN clock WHERE CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END IS NOT NULL
 AND floor((CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END + 10800000000) / 86400000000) >= today - cast('${days}' AS INT) + 1)
SELECT to_timestamp_micros(floor((posted_at+10800000000)/86400000000)*86400000000-10800000000) AS day,post_world AS world,count(*) AS posts FROM selected_posts CROSS JOIN clock WHERE floor((posted_at+10800000000)/86400000000)>=today-cast('${days}' AS INT)+1 GROUP BY day,post_world ORDER BY day
