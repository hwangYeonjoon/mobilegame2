import unittest
from release_preflight import inspect

class PreflightTests(unittest.TestCase):
    def test_unconfigured_is_missing_and_never_echoes_secrets(self):
        import json
        result=inspect({'SCRAP_KEYSTORE_PASSWORD':'never-echo-this-secret'}, {}, True)
        self.assertFalse(result['configuration_ready'])
        self.assertNotIn('never-echo-this-secret',json.dumps(result))
        self.assertTrue(result['not_verified'])
    def test_example_and_wrong_ad_ids_rejected(self):
        result=inspect({'ADMOB_REWARDED_UNIT_ID':'ca-app-pub-1111111111111111/1111111111'}, {'releaseEndpoint':'https://game.example.com','adMobAppId':'ca-app-pub-3940256099942544~3347511713','rewardedUnitId':'ca-app-pub-1111111111111111/2222222222'})
        missing={x['check'] for x in result['checks'] if x['status']=='missing'}
        self.assertTrue({'server_https','admob_game_app','admob_game_unit'}<=missing)
if __name__=='__main__': unittest.main()
