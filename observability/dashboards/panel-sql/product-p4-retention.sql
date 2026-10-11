WITH players AS (SELECT player_id, min(_timestamp) AS first_seen,
 CASE WHEN count(DISTINCT nullif(world, '')) > 1 THEN 'multi-world' ELSE coalesce(max(nullif(world, '')), 'unknown') END AS world,
 CASE WHEN count(DISTINCT CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END) > 1 THEN 'multi-age-group' ELSE coalesce(max(CASE WHEN age_group = '18+' THEN NULL ELSE nullif(age_group, '') END), CASE WHEN max(age_group) = '18+' THEN '18+ (legacy)' ELSE 'unknown' END) END AS age_group,
 CASE WHEN count(DISTINCT nullif(platform, '')) > 1 THEN 'multi-platform' ELSE coalesce(max(nullif(platform, '')), 'unknown') END AS platform
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time') GROUP BY player_id),
 activity AS (SELECT DISTINCT player_id, floor(_timestamp / 86400000000) AS day FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time')),
 watermark AS (SELECT floor(max(_timestamp) / 86400000000) AS day FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time'))
SELECT to_timestamp_micros(floor(p.first_seen / 86400000000) * 86400000000) AS cohort_day,
 count(*) AS cohort_users,
 sum(CASE WHEN floor(p.first_seen / 86400000000) + 1 < w.day THEN 1 ELSE 0 END) AS eligible_d1,
 sum(CASE WHEN floor(p.first_seen / 86400000000) + 1 < w.day AND a1.player_id IS NOT NULL THEN 1 ELSE 0 END) AS returned_d1,
 round(100.0 * sum(CASE WHEN floor(p.first_seen / 86400000000) + 1 < w.day AND a1.player_id IS NOT NULL THEN 1 ELSE 0 END) /
 nullif(sum(CASE WHEN floor(p.first_seen / 86400000000) + 1 < w.day THEN 1 ELSE 0 END), 0), 1) AS retention_d1_pct,
 sum(CASE WHEN floor(p.first_seen / 86400000000) + 7 < w.day THEN 1 ELSE 0 END) AS eligible_d7,
 round(100.0 * sum(CASE WHEN floor(p.first_seen / 86400000000) + 7 < w.day AND a7.player_id IS NOT NULL THEN 1 ELSE 0 END) /
 nullif(sum(CASE WHEN floor(p.first_seen / 86400000000) + 7 < w.day THEN 1 ELSE 0 END), 0), 1) AS retention_d7_pct
FROM players p CROSS JOIN watermark w
LEFT JOIN activity a1 ON p.player_id = a1.player_id AND a1.day = floor(p.first_seen / 86400000000) + 1
LEFT JOIN activity a7 ON p.player_id = a7.player_id AND a7.day = floor(p.first_seen / 86400000000) + 7
GROUP BY cohort_day ORDER BY cohort_day
