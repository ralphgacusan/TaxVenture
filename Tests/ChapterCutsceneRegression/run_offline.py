#!/usr/bin/env python3
"""Run chapter regression with the project's installed Unity compiler, never Unity.

python3 Tests/ChapterCutsceneRegression/run_offline.py [--game]
Outputs and full logs go to Temp/ChapterCutsceneRegression. No SDK/package install.
--game additionally compiles the game using the current Library/Bee response file;
this requires the user's existing imported Library but does not refresh/import it.
"""
import argparse
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Temp/ChapterCutsceneRegression"


def compile_sources(dotnet, csc, args, name):
    response = OUT / (name + ".rsp")
    response.write_text("\n".join('"' + str(a) + '"' for a in args), encoding="utf-8")
    result = subprocess.run([str(dotnet), str(csc), "-noconfig", "@" + str(response)], cwd=ROOT, capture_output=True, text=True)
    log = result.stdout + result.stderr
    (OUT / (name + "_compile.log")).write_text(log, encoding="utf-8")
    errors = [line for line in log.splitlines() if "error " in line]
    warnings = [line for line in log.splitlines() if "warning " in line]
    print(f"{name}: exit {result.returncode}, {len(errors)} errors, {len(warnings)} warnings")
    for line in errors:
        print(line)
    return result.returncode


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", action="store_true")
    options = parser.parse_args()
    version = re.search(r"m_EditorVersion: (\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text())[1]
    scripting = Path("/Applications/Unity/Hub/Editor") / version / "Unity.app/Contents/Resources/Scripting"
    dotnet = scripting / "NetCoreRuntime/dotnet"
    csc = scripting / "DotNetSdkRoslyn/csc.dll"
    if not dotnet.exists() or not csc.exists():
        parser.error(f"Unity {version}'s bundled compiler/runtime was not found at {scripting}; use dotnet run --project Tests/ChapterCutsceneRegression if an SDK is available")
    runtimes = list((scripting / "NetCoreRuntime/shared/Microsoft.NETCore.App").glob("*"))
    runtime = max(runtimes, key=lambda p: tuple(int(v) for v in p.name.split(".")))
    OUT.mkdir(parents=True, exist_ok=True)
    project = Path(__file__).with_name("ChapterCutsceneRegression.csproj")
    sources = [str((project.parent / item.attrib["Include"]).resolve()) for item in ET.parse(project).iter("Compile")]
    dll = OUT / "ChapterCutsceneRegression.dll"
    args = ["-nologo", "-nostdlib-", "-langversion:latest", "-nullable:disable", "-target:exe", "-out:" + str(dll)]
    args += ["-r:" + str(p) for p in sorted(runtime.glob("*.dll")) if "Native" not in p.name and p.name != "mscorlib.dll"]
    if compile_sources(dotnet, csc, args + sources, "regression"):
        return 1
    (OUT / "ChapterCutsceneRegression.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": runtime.name}}}), encoding="utf-8")
    result = subprocess.run([str(dotnet), str(dll)], cwd=ROOT, capture_output=True, text=True)
    (OUT / "regression.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    print("\n".join(line for line in result.stdout.splitlines() if line.startswith(("FAIL:", "WARN:")) or "checks passed" in line))
    if result.returncode:
        print(result.stderr)
        return result.returncode
    if options.game:
        responses = list((ROOT / "Library/Bee/artifacts").glob("*/Assembly-CSharp.rsp"))
        if not responses:
            parser.error("No current Assembly-CSharp.rsp: game compilation needs an existing imported Library")
        response = max(responses, key=lambda p: p.stat().st_mtime)
        source = response.read_text()
        source = re.sub(r"^-out:.*$", '-out:"' + str(OUT / "Assembly-CSharp.dll") + '"', source, flags=re.M)
        source = re.sub(r"^-refout:.*$", '-refout:"' + str(OUT / "Assembly-CSharp.ref.dll") + '"', source, flags=re.M)
        for script in list((ROOT / "Assets/Scripts/Cutscenes/Chapters").glob("*.cs")) + list((ROOT / "Assets/Cutscenes/Chapter02").glob("*.cs")):
            relative = script.relative_to(ROOT).as_posix()
            if relative not in source:
                source += '\n"' + str(script) + '"\n'
        game_response = OUT / "game.rsp"
        game_response.write_text(source, encoding="utf-8")
        result = subprocess.run([str(dotnet), str(csc), "@" + str(game_response)], cwd=ROOT, capture_output=True, text=True)
        log = result.stdout + result.stderr
        (OUT / "game_compile.log").write_text(log, encoding="utf-8")
        errors = [line for line in log.splitlines() if "error " in line]
        warnings = [line for line in log.splitlines() if "warning " in line]
        print(f"game: exit {result.returncode}, {len(errors)} errors, {len(warnings)} warnings")
        for line in errors:
            print(line)
        if result.returncode:
            return result.returncode
    print(f"Logs: {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
