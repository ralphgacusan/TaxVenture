"""Compile current Unity runtime AND complete Editor without starting Unity.

Uses the installed compiler and newest complete Bee response files, adding
runtime scripts not yet imported. All generated files stay in Temp/L3Repair.
Run from the project root: python3 -B Tests/run_level3_repair.py
"""
from pathlib import Path
import json
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Temp/L3Repair'
UNITY = Path('/Applications/Unity/Hub/Editor/6000.4.5f1/Unity.app/Contents/Resources/Scripting')
DOTNET = UNITY / 'NetCoreRuntime/dotnet'
CSC = UNITY / 'DotNetSdkRoslyn/csc.dll'
FRAMEWORK = next((UNITY / 'NetCoreRuntime/shared/Microsoft.NETCore.App').glob('*'))


def run(args, log):
    p = subprocess.run([str(x) for x in args], cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    log.write_text(p.stdout)
    print(f'{log.name}: exit {p.returncode}, {p.stdout.count("warning CS")} warnings')
    if p.returncode:
        print(p.stdout[-12000:])
        sys.exit(p.returncode)
    return p.stdout


def compile_unity(name):
    candidates = sorted((ROOT / 'Library/Bee/artifacts').glob(f'*/{name}.rsp'), key=lambda p: p.stat().st_mtime, reverse=True)
    original = next(p for p in candidates if all((ROOT / s).exists() for s in re.findall(r'^"([^"]+\.cs)"$', p.read_text(), re.M)))
    lines = original.read_text().splitlines()
    lines = [s for s in lines if not s.startswith(('-out:', '-refout:', '-pdb:'))]
    if name.endswith('Editor'):
        lines = [re.sub(r'Library/Bee/artifacts/[^/]+/Assembly-CSharp(?:\.ref)?\.dll', str(OUT / 'Assembly-CSharp.dll'), s) for s in lines]
    present = {s.strip('"') for s in lines if s.endswith('.cs"') or s.endswith('.cs')}
    source_root = Path('Assets/Editor') if name.endswith('Editor') else Path('Assets/Scripts')
    lines += [f'"{p.as_posix()}"' for p in source_root.rglob('*.cs') if p.as_posix() not in present]
    lines += [f'-out:"{OUT / (name + ".dll")}"', f'-refout:"{OUT / (name + ".ref.dll")}"']
    rsp = OUT / f'{name}.rsp'
    rsp.write_text('\n'.join(lines))
    print(f'{name}: {original.relative_to(ROOT)}')
    run([DOTNET, CSC, '@' + str(rsp)], OUT / f'{name}.log')


def suite(name, source=None):
    dest = OUT / name / 'Debug/net8.0'
    dest.mkdir(parents=True, exist_ok=True)
    refs = [f'-r:"{p}"' for p in FRAMEWORK.glob('*.dll')]
    runtime_rsp = (OUT / 'Assembly-CSharp.rsp').read_text()
    framework_names = {p.name for p in FRAMEWORK.glob('*.dll')}
    unity_refs = [s for s in re.findall(r'^-r:(.*)$', runtime_rsp, re.M) if Path(s.strip('"')).name not in framework_names]
    refs += ['-r:' + s for s in unity_refs]
    refs += [f'-r:"{OUT / "Assembly-CSharp.dll"}"']
    sources = [source or ROOT / f'Tests/{name}/Program.cs']
    if name == 'UnityRemoteRecoveryRegression':
        sources += [ROOT / 'Assets/Editor/UnityRemoteRecovery.cs']
    if name == 'ControlsRegression':
        sources += [ROOT / 'Assets/Scripts/Mobile/JoystickInputMath.cs']
    if name == 'T03ExplorationRegression':
        # The Editor-only clothing inset (Elijah's derived copies) is pure data and runs here.
        sources += [ROOT / 'Assets/Editor/ElijahClothingInset.cs']
    if name == 'T02PilotRegression':
        # Milestone T02: the Editor-only import-time silhouette algorithm is pure data and runs here.
        sources += [ROOT / 'Assets/Editor/Pilot/T02PilotSilhouette.cs']
    rsp = dest / 'suite.rsp'
    rsp.write_text('\n'.join(['-nostdlib+', '-langversion:latest', '-target:exe', f'-out:"{dest / (name + ".dll")}"'] + refs + [f'"{s}"' for s in sources]))
    run([DOTNET, CSC, '@' + str(rsp)], dest / 'compile.log')
    shutil.copy2(OUT / 'Assembly-CSharp.dll', dest)
    for ref in unity_refs:
        path = Path(ref.strip('"'))
        if path.name.endswith('.ref.dll'):
            path = ROOT / 'Library/ScriptAssemblies' / path.name.replace('.ref.dll', '.dll')
        if path.exists():
            shutil.copy2(path, dest / path.name)
    (dest / (name + '.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': FRAMEWORK.name}}}))
    result = run([DOTNET, dest / (name + '.dll')], dest / 'run.log')
    print(result[-1600:])


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    if '--suites' not in sys.argv:
        compile_unity('Assembly-CSharp')
        compile_unity('Assembly-CSharp-Editor')
    if '--remote' in sys.argv:
        suite('UnityRemoteRecoveryRegression')
    if '--t03' in sys.argv:
        # Milestone T03 mixed-camera exploration rules and ownership contracts only.
        suite('T03ExplorationRegression')
    if '--t02' in sys.argv:
        # Milestone T02 visual/contact pilot only: re-export the source geometry (read-only), then run.
        run([sys.executable, '-B', ROOT / 'Tests/T02PilotRegression/export_source_meshes.py'], OUT / 'T02PilotExport.log')
        suite('T02PilotRegression')
    elif '--build-only' not in sys.argv and '--t03' not in sys.argv:
        suite('Level3Regression')
        suite('ControlsRegression')
        if '--level2' in sys.argv:
            # Exactly the three historical native EventSystem comparer checks
            # cannot execute in standalone .NET. Production/test source stays intact.
            text = (ROOT / 'Tests/Level2Regression/Program.cs').read_text()
            start = text.index('// Actual EventSystem sorting:')
            end = text.index('var heldRecord =', start)
            scratch = OUT / 'Level2WithoutNativeComparer.cs'
            scratch.write_text(text[:start] + 'Console.WriteLine("SKIP: 3 native EventSystem comparer checks");\n' + text[end:])
            suite('Level2Regression', scratch)
