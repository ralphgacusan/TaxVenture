"""Build current game and run intro checks using the existing offline harness; never starts Unity."""
from pathlib import Path
import runpy

root = Path(__file__).resolve().parents[2]
harness = runpy.run_path(str(root / 'Tests/run_level3_repair.py'))
harness['OUT'].mkdir(parents=True, exist_ok=True)
harness['compile_unity']('Assembly-CSharp')
harness['suite']('IntroCutsceneRegression')
