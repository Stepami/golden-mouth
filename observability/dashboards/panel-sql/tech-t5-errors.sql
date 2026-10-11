SELECT sourcecontext, count(*) AS n FROM "backend"
WHERE severity IN ('Error','Critical','Fatal')
GROUP BY sourcecontext ORDER BY n DESC
