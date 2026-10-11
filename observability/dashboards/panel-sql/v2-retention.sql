WITH clock AS (
 SELECT floor((max(date_part('epoch', now())) * 1000000 + 10800000000) / 86400000000) AS today,
 max(date_part('epoch', now())) * 1000 AS now_ms, count(*) AS stream_rows FROM product),
raw AS (SELECT * FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''),
identities AS (SELECT player_id, min(CASE WHEN event_name NOT IN ('active_time','notification_sent') THEN _timestamp END) AS first_seen,
 min(CASE WHEN event_name = 'player_created' THEN _timestamp END) AS registered_at,
 coalesce(max(CASE WHEN event_name = 'player_created' THEN nullif(world, '') END), CASE WHEN count(DISTINCT nullif(world, ''))>1 THEN 'multi-world' ELSE max(nullif(world, '')) END, 'unknown') AS world,
 coalesce(max(CASE WHEN age_group <> '18+' THEN nullif(age_group, '') END), max(nullif(age_group, '')), 'unknown') AS age_group,
 coalesce(max(nullif(registration_source, '')), 'unknown') AS registration_source,
 CASE WHEN count(DISTINCT CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END) > 1 THEN 'multi'
 ELSE coalesce(max(CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END), 'unknown') END AS launch_surface
 FROM raw GROUP BY player_id),
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
current_profiles AS (SELECT p.player_id, max(p.subscriber_total) AS subscribers FROM product p JOIN latest l ON
 p.event_name = 'profile_snapshot' AND p.snapshot_time_unix_ms = l.snapshot_ms
 LEFT JOIN identities i ON p.player_id = i.player_id WHERE
 ('${world}' = 'all' OR p.world = '${world}') AND ('${age}' = 'all' OR p.age_group = '${age}')
 AND ('${source}' = 'all' OR p.registration_source = '${source}')
 AND ('${surface}' = 'all' OR coalesce(i.launch_surface, 'unknown') = '${surface}') GROUP BY p.player_id),
cohort AS (SELECT *, CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END AS cohort_at FROM selected CROSS JOIN clock WHERE CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END IS NOT NULL
 AND floor((CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END + 10800000000) / 86400000000) >= today - cast('${days}' AS INT) + 1)
, cohort_days AS (SELECT player_id,floor((cohort_at+10800000000)/86400000000) AS registered_day FROM cohort), returns AS (
 SELECT p.player_id,p.registered_day,c.today,max(CASE WHEN v.day=p.registered_day+1 THEN 1 ELSE 0 END) AS d1,
 max(CASE WHEN v.day=p.registered_day+3 THEN 1 ELSE 0 END) AS d3,max(CASE WHEN v.day=p.registered_day+7 THEN 1 ELSE 0 END) AS d7
 FROM cohort_days p CROSS JOIN clock c LEFT JOIN visits v ON p.player_id=v.player_id GROUP BY p.player_id,p.registered_day,c.today)
 SELECT to_timestamp_micros(registered_day*86400000000-10800000000) AS cohort_day,count(*) AS registered,
 sum(CASE WHEN registered_day+1<today THEN 1 ELSE 0 END) AS eligible_d1,
 sum(CASE WHEN registered_day+1<today THEN d1 ELSE 0 END) AS returned_d1,
 round(100.0*sum(CASE WHEN registered_day+1<today THEN d1 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+1<today THEN 1 ELSE 0 END),0),1) AS d1_pct,
 sum(CASE WHEN registered_day+3<today THEN 1 ELSE 0 END) AS eligible_d3,
 sum(CASE WHEN registered_day+3<today THEN d3 ELSE 0 END) AS returned_d3,
 round(100.0*sum(CASE WHEN registered_day+3<today THEN d3 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+3<today THEN 1 ELSE 0 END),0),1) AS d3_pct,
 sum(CASE WHEN registered_day+7<today THEN 1 ELSE 0 END) AS eligible_d7,
 sum(CASE WHEN registered_day+7<today THEN d7 ELSE 0 END) AS returned_d7,
 round(100.0*sum(CASE WHEN registered_day+7<today THEN d7 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+7<today THEN 1 ELSE 0 END),0),1) AS d7_pct
 FROM returns GROUP BY registered_day ORDER BY registered_day
