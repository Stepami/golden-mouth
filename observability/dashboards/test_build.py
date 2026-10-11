import unittest
import build


class OuterQueryFieldsTest(unittest.TestCase):
    def test_cte_fields_match_displayed_result(self):
        query = "WITH x AS (SELECT hidden AS ignored FROM product) SELECT category AS segment, count(*) AS users FROM x GROUP BY category"
        self.assertEqual(build.columns(query), ['segment', 'users'])

    def test_nested_queries_and_literals_do_not_become_axes(self):
        query = "WITH x AS (SELECT 'SELECT fake FROM fake' AS hidden FROM product) SELECT 'it''s ok' AS segment, coalesce(sum(case when hidden <> '' then 1 else 0 end), 0) AS users FROM x"
        self.assertEqual(build.columns(query), ['segment', 'users'])

    def test_final_panel_result_fields(self):
        expected = {
            'engagement': ['dimension', 'segment', 'active_users', 'active_player_days', 'completed_posts', 'posts_per_user_7d', 'posts_per_active_day', 'measured_player_days', 'avg_active_minutes', 'median_active_minutes'],
            'p9': ['dimension', 'segment', 'eligible_users', 'inactive_users', 'weekly_churn_pct'],
            'p4': ['cohort_day', 'cohort_users', 'eligible_d1', 'returned_d1', 'retention_d1_pct', 'eligible_d7', 'retention_d7_pct'],
        }
        for panel in build.product_panels:
            if panel['id'] in expected:
                self.assertEqual(build.columns(panel['queries'][0]['query']), expected[panel['id']])


if __name__ == '__main__':
    unittest.main()
