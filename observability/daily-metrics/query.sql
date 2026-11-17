WITH product_daily AS (
  SELECT FLOOR((_timestamp + 10800000000) / 86400000000) AS day,
         COUNT(DISTINCT CASE WHEN event_name IN ('world_created', 'post_scored', 'comment_created') THEN player_id END) AS dau,
         COUNT(DISTINCT CASE WHEN event_name = 'player_created' THEN player_id END) AS new_users
  FROM logs.product
  WHERE _timestamp >= __START_TIME__ AND activity_kind = 'real' AND player_id <> ''
    AND replace(lower(player_id), '-', '') NOT IN (__EXCLUDED_PLAYERS__)
    AND event_name IN ('world_created', 'post_scored', 'comment_created', 'player_created')
  GROUP BY day
), excluded_traces AS (
  SELECT DISTINCT trace_id
  FROM logs.product
  WHERE trace_id IS NOT NULL AND replace(lower(player_id), '-', '') IN (__EXCLUDED_PLAYERS__)
), excluded_players AS (
  SELECT player_id,
         MIN(CASE WHEN activity_kind = 'real' THEN _timestamp END) AS first_seen,
         MIN(CASE WHEN event_name = 'player_deleted' THEN _timestamp END) AS deleted_at
  FROM logs.product
  WHERE replace(lower(player_id), '-', '') IN (__EXCLUDED_PLAYERS__)
  GROUP BY player_id
), relevant_spans AS (
  SELECT trace_id, span_id, _timestamp,
         CASE WHEN gen_ai_operation_name = 'chat' OR operation_name = 'chat' OR operation_name LIKE 'chat %'
              THEN 1 ELSE 0 END AS is_chat,
         CASE WHEN (reference_parent_span_id IS NULL OR reference_parent_span_id = '')
                AND replace(lower(COALESCE(user_id, 'unknown')), '-', '') NOT IN (__EXCLUDED_PLAYERS__)
              THEN 1 ELSE 0 END AS is_user_root
  FROM traces.backend
  WHERE _timestamp >= __START_TIME__ - 86400000000
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
    AND NOT EXISTS (SELECT 1 FROM excluded_traces AS ignored WHERE ignored.trace_id = span.trace_id)
  GROUP BY day
), gauge_ranked AS (
  -- shortcut: the latest Mongo gauge is a 30-minute sample; use complete account history for exact day-end counts.
  SELECT FLOOR((_timestamp + 10800000000) / 86400000000) AS day, _timestamp, value,
         ROW_NUMBER() OVER (PARTITION BY FLOOR((_timestamp + 10800000000) / 86400000000) ORDER BY _timestamp DESC) AS sample_rank
  FROM metrics.reki_players_registered
  WHERE _timestamp >= __START_TIME__ AND value IS NOT NULL
), gauge_daily AS (
  SELECT sample.day, CAST(sample.value AS BIGINT) - COUNT(player.player_id) AS total_users
  FROM gauge_ranked AS sample
  LEFT JOIN excluded_players AS player
    ON player.first_seen <= sample._timestamp AND (player.deleted_at IS NULL OR player.deleted_at > sample._timestamp)
  WHERE sample.sample_rank = 1
  GROUP BY sample.day, sample.value
), daily_rows AS (
  SELECT day, dau, new_users, 0 AS llm_requests, CAST(NULL AS BIGINT) AS total_users FROM product_daily
  UNION ALL
  SELECT day, 0, 0, llm_requests, NULL FROM llm_daily
  UNION ALL
  SELECT day, 0, 0, 0, total_users FROM gauge_daily
)
SELECT to_char(to_timestamp_micros(CAST(day AS BIGINT) * 86400000000), '%Y-%m-%d') AS date,
       SUM(dau) AS dau, SUM(new_users) AS new_users, SUM(llm_requests) AS llm_requests,
       MAX(total_users) AS total_users
FROM daily_rows
GROUP BY day
ORDER BY day
