import sqlite3
import unittest
import product_v2
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
            'accounts': ['metric', 'accounts'],
            'active-today': ['metric', 'dau'],
            'funnel': ['stage', 'users', 'base', 'conversion_pct'],
            'retention': ['cohort_day', 'registered', 'eligible_d1', 'returned_d1', 'd1_pct', 'eligible_d3', 'returned_d3', 'd3_pct', 'eligible_d7', 'returned_d7', 'd7_pct'],
        }
        for panel in build.product_panels:
            if panel['id'] in expected:
                self.assertEqual(build.columns(panel['queries'][0]['query']), expected[panel['id']])


class WorldAudienceTest(unittest.TestCase):
    def test_latest_version_deduplicates_delivery_and_sums_worlds(self):
        db = sqlite3.connect(':memory:')
        db.execute('CREATE TABLE product (player_id TEXT, world_id TEXT, world TEXT, subscriber_total INTEGER, world_version INTEGER, _timestamp INTEGER, event_name TEXT)')
        db.executemany('INSERT INTO product VALUES (?, ?, ?, ?, ?, ?, ?)', [
            ('player', 'a', 'stage', 120, 0, 1, 'world_audience_updated'),
            ('player', 'a', 'stage', 100, 2, 2, 'world_audience_updated'),
            ('player', 'a', 'stage', 150, 1, 3, 'world_audience_updated'),
            ('player', 'a', 'stage', 100, 2, 4, 'world_audience_updated'),
            ('player', 'b', 'it', 40, 0, 1, 'world_audience_updated'),
        ])
        ctes = product_v2.COMMON.split('world_ranked AS', 1)[1].split('raw AS', 1)[0].rstrip().rstrip(',')
        self.assertEqual(db.execute('WITH world_ranked AS' + ctes + ' SELECT * FROM world_audience').fetchall(), [('player', 140, 'multi-world')])
        db.close()


class ProfileAttributesTest(unittest.TestCase):
    def test_id_only_snapshot_retains_registration_dimensions(self):
        db = sqlite3.connect(':memory:')
        db.execute('CREATE TABLE product (player_id TEXT, age_group TEXT, registration_source TEXT, event_name TEXT)')
        db.executemany('INSERT INTO product VALUES (?, ?, ?, ?)', [
            ('player', '14-17', 'referral', 'player_created'),
            ('player', None, None, 'profile_snapshot'),
            ('legacy', '18+', 'direct', 'profile_snapshot'),
            ('legacy', None, None, 'profile_snapshot'),
        ])
        cte = product_v2.COMMON.split('profile_attributes AS', 1)[1].split('raw AS', 1)[0].rstrip().rstrip(',')
        self.assertEqual(db.execute('WITH profile_attributes AS' + cte + ' SELECT * FROM profile_attributes ORDER BY player_id').fetchall(), [
            ('legacy', '18+', 'direct'), ('player', '14-17', 'referral'),
        ])
        db.close()


if __name__ == '__main__':
    unittest.main()
