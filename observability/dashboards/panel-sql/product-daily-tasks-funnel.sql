-- Use complete product history since :history_from_ms. :from_ms and :to_ms bound the first post,
-- while :now_ms excludes players whose 24-hour follow-up window is still open.
WITH registrations AS (
  SELECT player_id, min(event_time_unix_ms) AS registered_at
  FROM product
  WHERE event_name = 'player_created' AND activity_kind = 'real' AND player_id <> ''
  GROUP BY player_id
  HAVING min(event_time_unix_ms) >= :history_from_ms
), ordinary_posts AS (
  SELECT p.player_id, replace(p.prop_id, '-', '') AS post_id, min(p.event_time_unix_ms) AS saved_at
  FROM product p JOIN registrations r ON r.player_id = p.player_id
  WHERE p.activity_kind = 'real' AND p.event_name = 'post_scored'
    AND p.prop_publication_kind = 'post' AND p.prop_id <> ''
    AND p.event_time_unix_ms >= r.registered_at
  GROUP BY p.player_id, replace(p.prop_id, '-', '')
), ranked_posts AS (
  SELECT player_id, saved_at, row_number() OVER (PARTITION BY player_id ORDER BY saved_at, post_id) AS ordinal
  FROM ordinary_posts
), post_times AS (
  SELECT player_id, min(CASE WHEN ordinal = 1 THEN saved_at END) AS first_at,
    min(CASE WHEN ordinal = 2 THEN saved_at END) AS second_at
  FROM ranked_posts
  WHERE ordinal <= 2
  GROUP BY player_id
), cohort AS (
  SELECT player_id, first_at, CASE WHEN second_at <= first_at + 86400000 THEN second_at END AS second_at,
    CASE WHEN second_at <= first_at + 86400000 THEN second_at ELSE first_at + 86400000 END AS stop_at
  FROM post_times p
  WHERE first_at >= :from_ms AND first_at < :to_ms AND first_at + 86400000 <= :now_ms
    AND NOT EXISTS (SELECT 1 FROM product d WHERE d.event_name = 'player_deleted' AND d.player_id = p.player_id)
), daily_events AS (
  SELECT p.player_id, p.event_time_unix_ms AS at, p.prop_id, p.prop_step
  FROM product p
  WHERE p.activity_kind = 'real' AND (
    (p.event_name = 'tap' AND p.prop_id IN ('daily_tasks_viewed', 'daily_tasks_inline_viewed', 'daily_task_action'))
    OR (p.event_name = 'onboarding_step' AND p.prop_step IN ('daily_tasks_intro_shown', 'daily_tasks_intro_done'))
  )
), saved_actions AS (
  SELECT player_id, min(event_time_unix_ms) AS at
  FROM product
  WHERE activity_kind = 'real' AND event_name IN ('post_scored', 'comment_created') AND prop_id <> ''
  GROUP BY player_id, event_name, replace(prop_id, '-', '')
  UNION ALL
  SELECT player_id, min(event_time_unix_ms) AS at
  FROM product
  WHERE activity_kind = 'real' AND event_name = 'onboarding_step'
    AND prop_step = 'daily_tasks_progress' AND prop_kind = 'likes' AND prop_id <> ''
  GROUP BY player_id, world_id, prop_id
), exposure AS (
  SELECT c.player_id, c.first_at, c.second_at, c.stop_at,
    min(CASE WHEN e.prop_step = 'daily_tasks_intro_shown' THEN e.at END) AS intro_at,
    min(CASE WHEN e.prop_step = 'daily_tasks_intro_done' THEN e.at END) AS intro_done_at,
    min(CASE WHEN e.prop_id = 'daily_tasks_viewed' THEN e.at END) AS list_at,
    min(CASE WHEN e.prop_id IN ('daily_tasks_viewed', 'daily_tasks_inline_viewed') THEN e.at END) AS viewed_at,
    min(CASE WHEN e.prop_id = 'daily_task_action' THEN e.at END) AS click_at
  FROM cohort c LEFT JOIN daily_events e ON e.player_id = c.player_id AND e.at >= c.first_at AND e.at <= c.stop_at
  GROUP BY c.player_id, c.first_at, c.second_at, c.stop_at
), sequence AS (
  SELECT x.player_id, min(e.at) AS viewed_after_intro_at
  FROM exposure x JOIN daily_events e ON e.player_id = x.player_id AND e.at >= x.intro_at AND e.at <= x.stop_at
  WHERE e.prop_id IN ('daily_tasks_viewed', 'daily_tasks_inline_viewed')
  GROUP BY x.player_id
), actions AS (
  SELECT x.player_id,
    min(CASE WHEN e.at >= x.viewed_at THEN e.at END) AS saved_after_view_at,
    min(CASE WHEN e.at >= s.viewed_after_intro_at THEN e.at END) AS saved_after_intro_and_view_at
  FROM exposure x LEFT JOIN sequence s ON s.player_id = x.player_id
  JOIN saved_actions e ON e.player_id = x.player_id AND e.at > x.first_at AND e.at <= x.stop_at
  GROUP BY x.player_id
)
SELECT CASE WHEN x.intro_at IS NOT NULL THEN 'shown' ELSE 'not_shown' END AS intro,
  CASE WHEN x.list_at IS NOT NULL THEN 'list' WHEN x.viewed_at IS NOT NULL THEN 'inline' ELSE 'not_viewed' END AS tasks,
  count(*) AS first_posts,
  count(x.intro_done_at) AS guide_completed,
  count(x.click_at) AS task_actions_opened,
  count(s.viewed_after_intro_at) AS viewed_after_intro,
  count(a.saved_after_view_at) AS saved_after_view,
  count(a.saved_after_intro_and_view_at) AS saved_after_intro_and_view,
  count(x.second_at) AS second_posts_24h,
  sum(CASE WHEN x.second_at IS NOT NULL AND a.saved_after_intro_and_view_at IS NOT NULL THEN 1 ELSE 0 END) AS completed_sequence_24h,
  round(100.0 * count(x.second_at) / count(*), 2) AS second_post_rate_pct,
  round(avg((x.second_at - x.first_at) / 60000.0), 2) AS avg_minutes_to_second_post
FROM exposure x LEFT JOIN sequence s ON s.player_id = x.player_id LEFT JOIN actions a ON a.player_id = x.player_id
GROUP BY 1, 2 ORDER BY 1, 2
