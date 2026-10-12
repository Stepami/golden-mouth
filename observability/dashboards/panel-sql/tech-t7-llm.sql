SELECT concat('chat ', coalesce(nullif(gen_ai_request_model, ''), nullif(gen_ai_response_model, ''),
       CASE WHEN operation_name LIKE 'chat %' THEN nullif(substr(operation_name, 6), '') END,
       'unknown')) AS operation,
       count(*) AS calls, avg(duration) AS avg_us, max(duration) AS max_us
FROM "backend"
WHERE gen_ai_operation_name = 'chat' OR operation_name = 'chat' OR operation_name LIKE 'chat %'
GROUP BY operation ORDER BY calls DESC
