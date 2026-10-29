"""Internal source/content contracts; rendered Unity/device acceptance is separate."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def source(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

checks = {}
guide = json.loads(source("Assets/Resources/Tutorials/Level_01/guide.json"))
tour = next(x for x in guide["lessons"] if x["id"] == "hud-tour")["pages"]
checks["individual resource spotlights"] = all(any(p.get("point") == "hud:" + key for p in tour) for key in ("exp", "reputation", "coins"))
hotbar = source("Assets/Scripts/UI/HUD/HudHotbar.cs")
checks["shared carried tools survive fresh chapter entry"] = "ResetsOnFreshAttempt" in hotbar
checks["chest pickup claim belongs to current chapter"] = 'PickupClaimId(levelId, "notes-chest")' in hotbar
stamps = source("Assets/Scripts/Level2/Level2DeskStamps.cs")
checks["stamps always composite above captured folder"] = "toolLayer.AlwaysComposite = true" in stamps
drag = source("Assets/Scripts/Level2/Level2StampDrag.cs")
flight = drag.split("private IEnumerator FlyToStand", 1)[1].split("// =========================================================", 1)[0]
checks["stamp capture survives return until landing"] = "ReleaseLayer();" in flight
checks["review rows use bounded content layout"] = "FitContentRows" in source("Assets/Scripts/Level2/Level2OfficeReview.cs")
checks["choice prompt uses shared safe-area dock"] = "DockChoices" in source("Assets/Scripts/Level2/Level2RosaConsultation.cs")
checks["office actors reuse animation setup"] = "EnsureOfficeActors" in source("Assets/Scripts/Level2/Level2OfficeStart.cs")
for label, passed in checks.items():
    print(("PASS: " if passed else "FAIL: ") + label)
raise SystemExit(0 if all(checks.values()) else 1)
