WITH clock AS (
 SELECT floor((max(date_part('epoch', now())) * 1000000 + 10800000000) / 86400000000) AS today,
 max(date_part('epoch', now())) * 1000 AS now_ms, count(*) AS stream_rows FROM product),
world_ranked AS (SELECT player_id, world_id, world, subscriber_total,
 row_number() OVER (PARTITION BY player_id, world_id ORDER BY world_version DESC, _timestamp DESC) AS state_rank
 FROM product WHERE event_name = 'world_audience_updated' AND world_id IS NOT NULL),
world_audience AS (SELECT player_id, sum(subscriber_total) AS subscribers,
 CASE WHEN count(DISTINCT world) > 1 THEN 'multi-world' ELSE max(world) END AS world
 FROM world_ranked WHERE state_rank = 1 GROUP BY player_id),
profile_attributes AS (SELECT player_id,
 coalesce(max(CASE WHEN age_group <> '18+' THEN nullif(nullif(age_group, ''), 'unknown') END), max(nullif(nullif(age_group, ''), 'unknown')), 'unknown') AS age_group,
 coalesce(max(nullif(nullif(registration_source, ''), 'unknown')), 'unknown') AS registration_source
 FROM product WHERE event_name IN ('player_created', 'profile_snapshot') GROUP BY player_id),
raw AS (SELECT * FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''),
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
markers AS (SELECT snapshot_time_unix_ms, max(registered_count) AS expected FROM product
 WHERE event_name = 'profiles_snapshot_complete' GROUP BY snapshot_time_unix_ms),
complete AS (SELECT m.snapshot_time_unix_ms FROM markers m LEFT JOIN product p ON
 p.event_name = 'profile_snapshot' AND p.snapshot_time_unix_ms = m.snapshot_time_unix_ms CROSS JOIN clock c
 WHERE m.snapshot_time_unix_ms >= c.now_ms - 180000 AND m.snapshot_time_unix_ms <= c.now_ms - 10000
 GROUP BY m.snapshot_time_unix_ms, m.expected HAVING count(DISTINCT p.player_id) = m.expected),
latest AS (SELECT max(snapshot_time_unix_ms) AS snapshot_ms FROM complete),
current_profiles AS (SELECT p.player_id, max(w.subscribers) AS subscribers FROM product p JOIN latest l ON
 p.event_name = 'profile_snapshot' AND p.snapshot_time_unix_ms = l.snapshot_ms
 LEFT JOIN identities i ON p.player_id = i.player_id
 LEFT JOIN world_audience w ON p.player_id = w.player_id
 LEFT JOIN profile_attributes a ON p.player_id = a.player_id WHERE
 ('${world}' = 'all' OR coalesce(i.world, 'unknown') = '${world}') AND ('${age}' = 'all' OR coalesce(nullif(i.age_group, 'unknown'), a.age_group, 'unknown') = '${age}')
 AND ('${source}' = 'all' OR coalesce(nullif(i.registration_source, 'unknown'), a.registration_source, 'unknown') = '${source}')
 AND ('${surface}' = 'all' OR coalesce(i.launch_surface, 'unknown') = '${surface}') GROUP BY p.player_id),
cohort AS (SELECT *, CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END AS cohort_at FROM selected CROSS JOIN clock WHERE CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END IS NOT NULL
 AND floor((CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END + 10800000000) / 86400000000) >= today - cast('${days}' AS INT) + 1)
, coverage AS (SELECT greatest(0,least(30,c.today-floor((min(r._timestamp)+10800000000)/86400000000)-1)) AS observed_days,c.today FROM raw r CROSS JOIN clock c GROUP BY c.today),
 stick AS (SELECT w.observed_days,count(DISTINCT v.player_id) AS mau,count(v.player_id) AS active_profile_days, sum(CASE WHEN v.player_id IS NOT NULL AND v.day>=w.today-7 THEN 1 ELSE 0 END) AS active_days_7d FROM coverage w LEFT JOIN (SELECT v.* FROM visits v JOIN selected s ON v.player_id=s.player_id) v ON v.day>=w.today-w.observed_days AND v.day<w.today GROUP BY w.observed_days)
 SELECT observed_days,mau,active_profile_days,round(1.0*active_profile_days/nullif(observed_days,0),2) AS mean_dau,round(1.0*active_days_7d/nullif(least(7,observed_days),0),2) AS mean_dau_7d,
 round(100.0*active_profile_days/nullif(observed_days*mau,0),1) AS stickiness_pct FROM stick
