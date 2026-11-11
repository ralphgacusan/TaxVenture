"""Character repair conformance checks. No Unity process. Source scheduling is not rendered evidence."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[1]
body = (root/'Assets/Scripts/Player/ExplorationBody.cs').read_text()
fixture = (root/'Assets/Scripts/Pilot/T02PilotFixture.cs').read_text()
actor = (root/'Assets/Scripts/Pilot/T02PilotArthur.cs').read_text()
failures=[]
def check(ok,label):
 print(('PASS: ' if ok else 'FAIL: ')+label)
 if not ok:failures.append(label)
check('idlePlayable.SetSpeed(0d)' in body and 'walkPlayable.SetSpeed(0d)' in body, 'explicit clip clock cannot also advance automatically')
check('EvaluatePose(dt)' not in body, 'explicit timestamps are sampled without another delta-time increment')
check('model.localScale *= fit' not in body, 'Humanoid model root is not rescaled after graph binding')
check('model.transform.localScale *= arthurScale' not in fixture, 'pilot does not rescale the Animator after IK/pose-handler initialization')
check('CapsuleStandingHeight' not in body and 'ExplorationRules.CalibratedScale(rest.size.y)' in body, 'outdoor visual uses the canonical calibrated height (not the capsule)')
check('ExplorationRules.AdvancePhase(phase, distance, stride)' in body, 'walk/run cadence follows distance travelled over the blended stride')
check('RebuildPoseHandler' in actor, 'pilot hand-pose handler binds after fitted animator initialization')
print(f'Character playback contracts: {7-len(failures)}/7; native render/animation NOT executed')
raise SystemExit(bool(failures))
