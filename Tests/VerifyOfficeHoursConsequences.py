"""Guard consequence-only office hours and player-facing wording."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "Assets/Scripts/UI/HUD/OfficeHours.cs").read_text(encoding="utf-8-sig")
settings = json.loads((root / "Assets/Resources/Levels/Level_01/office_hours.json").read_text(encoding="utf-8-sig"))
guide = json.loads((root / "Assets/Resources/Tutorials/Level_01/guide.json").read_text(encoding="utf-8-sig"))
summary = (root / "Assets/Scripts/UI/AuditSummaryPopupUI.cs").read_text(encoding="utf-8-sig")
completion = source.split("private void HandleCaseCompleted()", 1)[1].split("private bool CanCloseTheDay()", 1)[0]
assert "AddBonus(" not in source, "Office hours must never award an on-time bonus"
assert "public bool BonusAwarded => false;" in source, "Legacy bonus state must remain false"
assert "if (CaseDone) return;" in completion, "Completion consequences must apply only once"
assert "if (Announced && Day > 1)" in completion and "Score.AddPenalty(" in completion
assert "Mathf.Clamp(Day - 1, 0, Mathf.Max(0, Config.lateMaxDays))" in completion
assert "Config.lateExpPerDay * daysLate, Config.lateHeartsPerDay * daysLate" in completion
assert settings.get("onTimeBonusExp", 0) == 0 and settings.get("onTimeBonusHearts", 0) == 0
assert settings["lateExpPerDay"] == 15 and settings["lateHeartsPerDay"] == 1 and settings["lateMaxDays"] == 3
assert "on-time bonus" not in json.dumps(guide).lower() and "bonus" not in settings["arthurOnTime"].lower()
assert "onTimeBonus" not in summary
print("PASS: on-time submission grants nothing; once-only capped late consequences and corrected wording remain wired")
