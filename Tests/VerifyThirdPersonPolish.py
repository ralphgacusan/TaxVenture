"""Focused assembly/asset contracts. Does not substitute for Unity rendering/touch review."""
from pathlib import Path
import re
R = Path(__file__).resolve().parents[1]
def src(p): return (R/p).read_text()
body = src('Assets/Scripts/Player/ExplorationBody.cs')
fpc = src('Assets/Scripts/Player/FirstPersonCameraController.cs')
stick = src('Assets/Scripts/Mobile/VirtualJoystick.cs')
jump = src('Assets/Scripts/Mobile/JumpButton.cs')
actor = src('Assets/Scripts/Pilot/T02PilotArthur.cs')
fixture = src('Assets/Scripts/Pilot/T02PilotFixture.cs')
checks = {
 'outdoor height is the one canonical rest-pose calibration (not the capsule, not a posed re-fit)': 'CharacterBodyGeometry.TryMeasureRest(model, out Bounds rest)' in body and 'ExplorationRules.CalibratedScale(rest.size.y)' in body and 'CapsuleStandingHeight' not in body,
 'camera composition is centred (no shoulder offset) in the lower middle': 'ExplorationRules.CameraPosition(pivot, rotation' in fpc and 'Vector3.right' not in fpc.split('private Pose ThirdPersonPose')[1].split('private void KeepBodyReadable')[0],
 'distinct run and jump animation inputs are evaluated': 'runPlayable.SetTime' in body and 'jumpPlayable.SetTime(jump.ClipTime' in body,
 'sprint travel extends beyond the walking rim': 'SprintTravelLimit' in stick and 'SprintMarker' in stick,
 'jump uses a graphic icon with more space from the bag': '"JUMP"' not in jump and 'JumpIcon' in jump and '(-310f, 230f)' in jump,
 'pilot fits each posed variant independently': 'ref float arthurScale' not in fixture and 'FitStandingHeight(arthurHeight)' in fixture,
 'finger edits restore all non-finger transforms': 'fingerRotations' in actor and 'poseTransforms' in actor,
 'smoothed model copies preserve source FBX data': all((R/'Assets/Resources/Player'/n).exists() for n in ['Elijah_Exploration_Smooth.fbx','Elijah_Conversation_Smooth.fbx']),
 'authored sprint and jump clips exist': all((R/'Assets/Resources/Player'/n).exists() for n in ['Elijah_Sprint.anim','Elijah_Jump.anim']),
}
# Verify the concrete assets, not only the code that consumes them.
for source, name in [('Elijah_Walking','Elijah_Exploration_Smooth'), ('npc_elijah_Idle','Elijah_Conversation_Smooth')]:
 original = R/'Assets/3D Models/npc_elijah'/(source+'.fbx')
 derived = R/'Assets/Resources/Player'/(name+'.fbx')
 checks[name+' preserves source identity/rig/UV bytes'] = original.read_bytes() == derived.read_bytes()
 meta = Path(str(derived)+'.meta').read_text()
 checks[name+' requests calculated smooth normals'] = 'normalImportMode: 1' in meta and 'normalSmoothAngle: 70' in meta
for name, loop in [('Elijah_Sprint',1),('Elijah_Jump',0)]:
 text = src('Assets/Resources/Player/'+name+'.anim')
 curves = {}
 for block in text.split('  - serializedVersion: 2\n')[1:]:
  attribute = re.search(r'    attribute: (.+)',block)
  if attribute: curves[attribute[1]] = [(float(t),float(v)) for t,v in re.findall(r'        time: ([^\n]+)\n        value: ([^\n]+)',block)]
 checks[name+' has Humanoid muscle curves without generic scale tracks'] = len(curves)>90 and 'm_ScaleCurves: []' in text and 'classID: 4' not in text
 checks[name+' stays in place without forward root travel'] = all(v==0 for k in ['RootT.x','RootT.z'] for t,v in curves[k])
 checks[name+' uses intentional loop policy'] = 'm_LoopTime: '+str(loop) in text
 checks[name+' has authored knee motion'] = max(v for t,v in curves['Left Lower Leg Stretch'])-min(v for t,v in curves['Left Lower Leg Stretch']) > .5
 if loop:
  checks[name+' closes every looping curve continuously'] = all(abs(values[0][1]-values[-1][1])<1e-5 for values in curves.values())
treatment = src('Assets/Editor/ElijahModelTreatment.cs')
checks['derived models retain original material slots'] = 'skin.sharedMaterials = slots' in treatment and 'original.sharedMaterials' in treatment
asset = src('Assets/Resources/Player/ExplorationBody.asset')
for field,name in [('model','Elijah_Exploration_Smooth.fbx'),('conversationModel','Elijah_Conversation_Smooth.fbx'),('sprint','Elijah_Sprint.anim'),('jump','Elijah_Jump.anim')]:
 guid = re.search(r'^guid: (\w+)',src('Assets/Resources/Player/'+name+'.meta'),re.M)[1]
 checks[field+' resource reference resolves to the adopted asset'] = bool(re.search(r'^  '+field+r': .*guid: '+guid,asset,re.M))
for label, ok in checks.items(): print(('PASS: ' if ok else 'FAIL: ')+label)
print(f'{sum(checks.values())}/{len(checks)} polish contracts; native rendering not executed')
raise SystemExit(not all(checks.values()))
