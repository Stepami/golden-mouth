SELECT 'Все игроки' AS segment, count(DISTINCT player_id) AS observed_profiles_90d,
 to_timestamp_micros(min(_timestamp)) AS telemetry_since_utc,
 count(DISTINCT CASE WHEN _timestamp >= date_part('epoch', now()) * 1000000 - 30 * 86400000000 THEN player_id END) AS mau_30d,
 count(DISTINCT CASE WHEN _timestamp >= date_part('epoch', now()) * 1000000 - 7 * 86400000000 THEN player_id END) AS wau_7d,
 count(DISTINCT CASE WHEN floor(_timestamp / 86400000000) = floor(date_part('epoch', now()) * 1000000 / 86400000000) THEN player_id END) AS dau_today,
 count(DISTINCT CASE WHEN floor(_timestamp / 86400000000) = floor(date_part('epoch', now()) * 1000000 / 86400000000) - 1 THEN player_id END) AS dau_yesterday,
 round(100.0 * count(DISTINCT CASE WHEN floor(_timestamp / 86400000000) = floor(date_part('epoch', now()) * 1000000 / 86400000000) - 1 THEN player_id END) /
 nullif(count(DISTINCT CASE WHEN _timestamp >= (floor(date_part('epoch', now()) * 1000000 / 86400000000) - 30) * 86400000000 AND _timestamp < floor(date_part('epoch', now()) * 1000000 / 86400000000) * 86400000000 THEN player_id END), 0), 1) AS stickiness_yesterday_pct
 FROM product WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> '' AND event_name NOT IN ('notification_sent', 'active_time')
