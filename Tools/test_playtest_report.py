import json
import tempfile
import unittest
from pathlib import Path
from playtest_report import summarize
class CombatQualityTests(unittest.TestCase):
    def test_excludes_qa_and_deduplicates_exports_and_separates_builds(self):
        with tempfile.TemporaryDirectory() as directory:
            def event(id,kind,subject,number,value=0,version='0.21.0',test=False):
                return dict(id=id,at='2026-11-06T00:00:00Z',kind=kind,subject=subject,number=number,value=value,version=version,platform='Android',test=test)
            events=[event('1','combat_outcome','machines',2),event('2','feedback','replay_interest',3),event('3','performance','frame_p95_ms',100,33),event('4','combat_outcome','machines',99,test=True),event('5','combat_outcome','machines',0,version='0.20.0')]
            paths=[]
            for name in ('one','duplicate'):
                p=Path(directory)/name;p.write_text(json.dumps(dict(participant='local-test',events=events)));paths.append(p)
            result=summarize(paths);new=result['combat_quality_by_build_platform']['0.21.0 / Android']
            self.assertEqual(new['reported_finished_runs'],1)
            self.assertEqual(new['mean_machines_per_reported_run'],2)
            self.assertEqual(new['replay_interest_responses'],{'3':1})
            self.assertEqual(new['reported_p95_ms_range'],[33,33])
            self.assertEqual(len(result['combat_quality_by_build_platform']),2)
    def test_robot_milestones_are_samples_not_inferred_rates(self):
        with tempfile.TemporaryDirectory() as directory:
            events=[]
            for i, (kind, subject, stage, value, test) in enumerate([
                ('robot_milestone','parts_ready',2,80,False),
                ('robot_milestone','first_craft',2,90,False),
                ('robot_milestone','first_specialized_board',2,120,False),
                ('robot_milestone','first_specialized_board',2,30,True),
                ('robot_discovery','prompt_shown',1,30,False),
                ('robot_discovery','prompt_opened',1,35,False)]):
                events.append(dict(id=str(i),at='2026-11-09T00:00:00Z',kind=kind,subject=subject,number=stage,value=value,test=test,version='0.29.0',platform='Android'))
            p=Path(directory)/'events.json';p.write_text(json.dumps(dict(participant='test',events=events)))
            result=summarize([p,p]);row=result['robot_milestones_by_build_platform_stage']['0.29.0 / Android / stage_2']
            self.assertEqual(row['first_specialized_board']['median_seconds'],120)
            self.assertEqual(row['first_specialized_board']['observed_events'],1)
            self.assertNotIn('first_board',row)
            self.assertEqual(result['robot_discovery_events']['0.29.0 / Android / stage_1']['prompt_opened'],1)
    def test_no_exports_does_not_invent_human_evidence(self):
        result=summarize([])
        self.assertEqual(result['participants'],0)
        self.assertEqual(result['combat_quality_by_build_platform'],{})
        self.assertIsNone(result['observed_retention']['day_1']['rate'])
if __name__=='__main__': unittest.main()
