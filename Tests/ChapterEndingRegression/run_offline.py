"""Run real ending rules against the current offline game build; never starts Unity."""
from pathlib import Path
import runpy
import sys

root = Path(__file__).resolve().parents[2]
harness = runpy.run_path(str(root / "Tests/run_level3_repair.py"))
harness["OUT"].mkdir(parents=True, exist_ok=True)
if "--reuse-game" not in sys.argv:
    harness["compile_unity"]("Assembly-CSharp")
harness["suite"]("ChapterEndingRegression")
