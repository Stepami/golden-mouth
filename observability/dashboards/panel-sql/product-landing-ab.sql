-- Deployment gate: anonymous /events collector and new product fields must exist first.
-- Bind :from_ms, :to_ms, :now_ms and :campaign from the dashboard controls.
-- Query the full available history: the period filters first exposure, not later steps.
-- Anonymous visitors have no age: use All ages for the full acquisition funnel.
WITH clean_visitors AS (
  SELECT visitor_id
  FROM product
  WHERE landing_experiment = 'entry_v1'
  GROUP BY visitor_id
  HAVING count(DISTINCT landing_variant) = 1
    AND max(CASE WHEN landing_switched = true THEN 1 ELSE 0 END) = 0
), exposures AS (
  SELECT e.visitor_id, min(e.event_time_unix_ms) AS started
  FROM product e JOIN clean_visitors c ON e.visitor_id = c.visitor_id
  WHERE e.event_name = 'landing_view' AND e.landing_experiment = 'entry_v1'
  GROUP BY e.visitor_id
), entries AS (
  SELECT e.visitor_id, e.entry_id, e.landing_variant AS variant,
    e.landing_utm_campaign AS campaign, x.started
  FROM product e JOIN exposures x ON e.visitor_id = x.visitor_id
    AND e.event_time_unix_ms = x.started
  WHERE e.event_name = 'landing_view' AND e.landing_source = 'link'
    AND e.landing_initial_variant = e.landing_variant
    AND e.landing_utm_campaign = :campaign
    AND x.started >= :from_ms AND x.started < :to_ms
  GROUP BY e.visitor_id, e.entry_id, e.landing_variant, e.landing_utm_campaign, x.started
), links AS (
  SELECT DISTINCT entry_id, player_id FROM product
  WHERE event_name = 'landing_profile_link' AND entry_event_source = 'verified_profile'
), publications AS (
  SELECT e.entry_id, min(p.event_time_unix_ms) AS posted
  FROM entries e JOIN links l ON e.entry_id = l.entry_id
  JOIN product p ON l.player_id = p.player_id
  WHERE p.event_name = 'post_scored' AND coalesce(p.prop_publication_kind, 'post') = 'post'
    AND p.event_time_unix_ms >= e.started AND p.event_time_unix_ms < e.started + 86400000
  GROUP BY e.entry_id
), steps AS (
  SELECT e.entry_id, e.variant, e.campaign, e.started,
    max(CASE WHEN s.event_name = 'landing_continue' THEN 1 ELSE 0 END) AS continued,
    max(CASE WHEN s.event_name = 'landing_auth_complete' THEN 1 ELSE 0 END) AS authorized,
    max(CASE WHEN p.posted IS NOT NULL THEN 1 ELSE 0 END) AS published,
    max(CASE WHEN s.event_name = 'landing_reaction_viewed'
      AND replace(s.entry_post_id, '-', '') = replace(post.prop_id, '-', '') AND s.event_time_unix_ms >= p.posted
      THEN 1 ELSE 0 END) AS reaction
  FROM entries e
  LEFT JOIN product s ON e.entry_id = s.entry_id
    AND s.event_time_unix_ms >= e.started AND s.event_time_unix_ms < e.started + 86400000
  LEFT JOIN publications p ON e.entry_id = p.entry_id
  LEFT JOIN links l ON e.entry_id = l.entry_id
  LEFT JOIN product post ON post.player_id = l.player_id
    AND post.event_name = 'post_scored' AND coalesce(post.prop_publication_kind, 'post') = 'post'
    AND post.event_time_unix_ms = p.posted
  GROUP BY e.entry_id, e.variant, e.campaign, e.started
)
SELECT variant, campaign,
  count(*) AS visitors,
  sum(CASE WHEN started + 86400000 > :now_ms THEN 1 ELSE 0 END) AS awaiting_24h,
  sum(CASE WHEN started + 86400000 <= :now_ms THEN 1 ELSE 0 END) AS eligible_24h,
  sum(CASE WHEN started + 86400000 <= :now_ms THEN continued ELSE 0 END) AS continued_24h,
  sum(CASE WHEN started + 86400000 <= :now_ms AND continued = 1 THEN authorized ELSE 0 END) AS authorized_24h,
  sum(CASE WHEN started + 86400000 <= :now_ms AND continued = 1 AND authorized = 1 THEN published ELSE 0 END) AS published_24h,
  sum(CASE WHEN started + 86400000 <= :now_ms AND continued = 1 AND authorized = 1 AND published = 1 THEN reaction ELSE 0 END) AS reaction_24h
FROM steps GROUP BY variant, campaign ORDER BY campaign, variant
