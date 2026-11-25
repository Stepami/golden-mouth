WITH product_events AS (
  SELECT FLOOR((_timestamp + 10800000000) / 86400000000) AS day,
         player_id,
         event_name = 'player_created' AS is_registration
  FROM logs.product
  WHERE _timestamp >= __START_TIME__ AND activity_kind = 'real' AND player_id <> ''
    AND event_name IN ('world_created', 'post_scored', 'comment_created', 'player_created')
  GROUP BY day, player_id, is_registration
), product_daily AS (
  SELECT day,
         COUNT(CASE WHEN NOT is_registration THEN 1 END) AS dau,
         COUNT(CASE WHEN is_registration THEN 1 END) AS new_users
  FROM product_events
  WHERE day < __END_DAY__
  GROUP BY day
), retention AS (
  SELECT cohort.day,
         COUNT(CASE WHEN activity.day = cohort.day + 1 THEN 1 END) AS retained_d1,
         COUNT(CASE WHEN activity.day = cohort.day + 3 THEN 1 END) AS retained_d3,
         COUNT(CASE WHEN activity.day = cohort.day + 7 THEN 1 END) AS retained_d7,
         COUNT(CASE WHEN activity.day = cohort.day + 14 THEN 1 END) AS retained_d14
  FROM product_events AS cohort
  JOIN product_events AS activity
    ON activity.player_id = cohort.player_id
    AND activity.day - cohort.day IN (1, 3, 7, 14)
  WHERE cohort.is_registration AND NOT activity.is_registration AND cohort.day < __END_DAY__
  GROUP BY cohort.day
), relevant_spans AS (
  SELECT trace_id, span_id, _timestamp,
         CASE WHEN gen_ai_operation_name = 'chat' OR operation_name = 'chat' OR operation_name LIKE 'chat %'
              THEN 1 ELSE 0 END AS is_chat,
         CASE WHEN reference_parent_span_id IS NULL OR reference_parent_span_id = ''
              THEN 1 ELSE 0 END AS is_user_root
  FROM traces.backend
  WHERE _timestamp >= __START_TIME__ - 86400000000 AND _timestamp < __END_TIME__
    AND (
      gen_ai_operation_name = 'chat' OR operation_name = 'chat' OR operation_name LIKE 'chat %'
      OR operation_name IN ('POST posts', 'POST posts/{postId}/replies', 'POST posts/{postId}/replies/generation',
                            'POST posts/publications', 'POST posts/publications/{publicationId}/moderation')
      OR (
        tickerq_job_function IN ('GenerateStarFeedOnPostPublishedJob', 'GenerateStarFeedOnCommentPublishedJob',
                                 'GenerateStarFeedPostJob')
        AND COALESCE(tickerq_job_current_attempt, 1) = 1
      )
    )
), user_spans AS (
  SELECT trace_id, span_id, _timestamp, is_chat,
         MAX(CASE WHEN is_chat = 0 THEN is_user_root ELSE 0 END) OVER (PARTITION BY trace_id) AS has_user_root
  FROM relevant_spans
), llm_daily AS (
  SELECT FLOOR((span._timestamp + 10800000000) / 86400000000) AS day,
         COUNT(DISTINCT span.span_id) AS llm_requests
  FROM user_spans AS span
  WHERE span._timestamp >= __START_TIME__ AND span.is_chat = 1 AND span.has_user_root = 1
  GROUP BY day
), gauge_ranked AS (
  -- shortcut: the latest Mongo gauge is a 30-minute sample; use complete account history for exact day-end counts.
  SELECT FLOOR((_timestamp + 10800000000) / 86400000000) AS day, value,
         ROW_NUMBER() OVER (PARTITION BY FLOOR((_timestamp + 10800000000) / 86400000000) ORDER BY _timestamp DESC) AS sample_rank
  FROM metrics.reki_players_registered
  WHERE _timestamp >= __START_TIME__ AND _timestamp < __END_TIME__ AND value IS NOT NULL
), daily_rows AS (
  SELECT day, dau, new_users, 0 AS llm_requests, CAST(NULL AS BIGINT) AS total_users FROM product_daily
  UNION ALL
  SELECT day, 0, 0, llm_requests, NULL FROM llm_daily
  UNION ALL
  SELECT day, 0, 0, 0, CAST(value AS BIGINT) FROM gauge_ranked WHERE sample_rank = 1
), daily AS (
  SELECT day, SUM(dau) AS dau, SUM(new_users) AS new_users, SUM(llm_requests) AS llm_requests,
         MAX(total_users) AS total_users
  FROM daily_rows
  GROUP BY day
)
SELECT to_char(to_timestamp_micros(CAST(daily.day AS BIGINT) * 86400000000), '%Y-%m-%d') AS date,
       daily.dau, daily.new_users, daily.llm_requests, daily.total_users,
       COALESCE(retention.retained_d1, 0) AS retained_d1,
       COALESCE(retention.retained_d3, 0) AS retained_d3,
       COALESCE(retention.retained_d7, 0) AS retained_d7,
       COALESCE(retention.retained_d14, 0) AS retained_d14
FROM daily
LEFT JOIN retention ON daily.day = retention.day
ORDER BY daily.day
