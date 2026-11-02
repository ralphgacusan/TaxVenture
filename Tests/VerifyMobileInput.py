"""Static startup/input regression contracts; not a Unity Remote or device test."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
guard = (ROOT / 'Assets/Scripts/Mobile/MobileInputGuard.cs').read_text(encoding='utf-8-sig')
joystick = (ROOT / 'Assets/Scripts/Mobile/VirtualJoystick.cs').read_text(encoding='utf-8-sig')
checks = {
    'rendering-debug runtime UI disabled before scene input starts': bool(re.search(
        r'RuntimeInitializeOnLoadMethod\(RuntimeInitializeLoadType.BeforeSceneLoad\)[\s\S]*?'
        r'DebugManager\.instance\.enableRuntimeUI\s*=\s*false', guard)),
    'visible and persistent debug overlays explicitly closed':
        'DebugManager.instance.displayRuntimeUI = false;' in guard and
        'DebugManager.instance.displayPersistentRuntimeUI = false;' in guard,
    'UI keeps mouse and each touch independent':
        'UIPointerBehavior.AllPointersAsIs' in guard and
        'InputSystemUIInputModule' in guard and 'pointerBehavior =' in guard,
    'scene callback is registered idempotently with domain reload disabled':
        'SceneManager.sceneLoaded -= ConfigureSceneInput;' in guard and
        'SceneManager.sceneLoaded += ConfigureSceneInput;' in guard,
    'only modules belonging to the loaded scene are configured':
        'module.gameObject.scene == scene' in guard,
    'no touch simulation or focus-reset bypass introduced':
        'TouchSimulation.Enable' not in guard and
        'if (!hasFocus)\n            ResetJoystick();' in joystick.replace('\r\n', '\n'),
    'joystick releases only its own pointer and still resets on genuine pause/disable':
        'eventData.pointerId == activePointerId' in joystick and
        'private void OnApplicationPause(bool paused)' in joystick and
        'private void OnDisable()' in joystick,
}
for title, passed in checks.items():
    print(('PASS: ' if passed else 'FAIL: ') + title)
print('Static contracts only; touch delivery, frame time and device behavior require user testing.')
sys.exit(0 if all(checks.values()) else 1)
