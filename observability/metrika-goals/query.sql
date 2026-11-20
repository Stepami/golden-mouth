WITH target AS (
    SELECT player_id, event_time_unix_ms AS target_at,
           FLOOR((event_time_unix_ms + 10800000) / 86400000) AS target_day,
           CASE WHEN prop_publication_kind = 'post' THEN 1 ELSE 0 END AS ordinary_post
    FROM "product"
    WHERE event_name = '__EVENT__'
      AND __ID_FIELD__ = '__LOOKUP_ID__'
      AND activity_kind = 'real'
      AND player_id IS NOT NULL AND player_id <> ''
      AND event_time_unix_ms >= __RECENT_MS__
      AND event_time_unix_ms <= __NOW_MS__
    ORDER BY event_time_unix_ms, player_id
    LIMIT 1
), actions AS (
    SELECT p.event_name,
           CASE WHEN p.event_name = 'world_created' THEN REPLACE(p.world_id, '-', '') ELSE p.prop_id END AS action_id,
           MIN(p.event_time_unix_ms) AS action_at,
           MAX(CASE WHEN p.event_name = 'post_scored' AND p.prop_publication_kind = 'post' THEN 1 ELSE 0 END) AS ordinary_post
    FROM "product" p
    JOIN target t ON p.player_id = t.player_id
    WHERE p.activity_kind = 'real'
      AND p.event_name IN ('world_created', 'post_scored', 'comment_created')
      AND p.event_time_unix_ms >= __HISTORY_MS__
      AND p.event_time_unix_ms <= t.target_at
    GROUP BY p.event_name, action_id
), earlier AS (
    SELECT a.action_at, a.ordinary_post
    FROM actions a CROSS JOIN target t
    WHERE a.action_id IS NOT NULL AND a.action_id <> ''
      AND (a.action_at < t.target_at OR
          (a.action_at = t.target_at AND (a.action_id < '__COMPACT_ID__' OR
          (a.action_id = '__COMPACT_ID__' AND a.event_name < '__EVENT__'))))
), referral AS (
    SELECT MIN(p.event_time_unix_ms) AS registered_at
    FROM "product" p
    JOIN target t ON p.player_id = t.player_id
    WHERE p.event_name = 'player_created'
      AND p.registration_source = 'referral'
      AND p.event_time_unix_ms >= __HISTORY_MS__
      AND p.event_time_unix_ms <= t.target_at
)
SELECT CAST(t.ordinary_post AS BIGINT) AS ordinary_post,
       COUNT(CASE WHEN FLOOR((a.action_at + 10800000) / 86400000) = t.target_day THEN 1 END) AS earlier_daily_actions,
       COUNT(CASE WHEN FLOOR((a.action_at + 10800000) / 86400000) = t.target_day AND a.ordinary_post = 1 THEN 1 END) AS earlier_daily_posts,
       CASE WHEN r.registered_at IS NOT NULL
              AND COUNT(CASE WHEN a.action_at >= r.registered_at THEN 1 END) = 0
            THEN 1 ELSE 0 END AS first_referred_action
FROM target t CROSS JOIN referral r LEFT JOIN earlier a ON 1 = 1
GROUP BY t.ordinary_post, t.target_day, r.registered_at
