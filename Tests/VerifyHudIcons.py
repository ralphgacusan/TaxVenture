"""Guard the shared HUD icon wiring in both office levels.
Run: python Tests/VerifyHudIcons.py
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def source(path):
    return (ROOT / "Assets/Scripts" / path).read_text(encoding="utf-8-sig")


failures = []


def check(condition, message):
    print(f"{'PASS' if condition else 'FAIL'}: {message}")
    if not condition:
        failures.append(message)


submittable = source("UI/HUD/HudSubmittableIcon.cs")
check("!enabled || CanUseIcon()" in submittable,
      "Disabled legacy folder handlers leave the Notes image clickable")
check(re.search(r"private void OnDisable\(\)\s*\{[^}]*UpdateRaycastState\(\);", submittable),
      "Disabling a legacy handler immediately releases its raycast gate")
check(re.search(r"private void OnEnable\(\)\s*\{[^}]*UpdateRaycastState\(\);", submittable),
      "Re-enabling a folder handler restores its availability gate")

drag = source("UI/HUD/HudIconDragOpen.cs")
check("button.IsInteractable()" in drag,
      "Dragging respects button and parent CanvasGroup availability")
check("eventData.eligibleForClick = false" in drag,
      "A drag cannot also toggle the document as a click on release")
check("PointerEventData.InputButton.Left" in drag,
      "Secondary mouse buttons cannot drag documents out of icons")

profiles = source("SideQuest1/TaxpayerProfileCollectionHUD.cs")
check("iconGroup.alpha = available ? 1f : 0.4f" in profiles,
      "Empty taxpayer profiles start dim and collected profiles become opaque")
check("iconButton.interactable = available" in profiles,
      "Empty taxpayer profiles are unavailable to clicks")
check(re.search(r"private void Start\(\)[\s\S]*?UpdateCountText\(\);", profiles),
      "Profile availability refreshes after other scene objects initialize")

for script in ("Level2/Level2TaxCodeBookIcon.cs", "Level2/Level2AnnotatedPapers.cs"):
    check("OfficeHudRuntime.StyleIconButton(button)" in source(script),
          f"{Path(script).stem} uses the shared hover and press styling")

for level in (1, 2):
    scene = (ROOT / f"Assets/Scenes/Office_Level{level}.unity").read_text(encoding="utf-8-sig")
    check("m_TargetAssemblyTypeName: NotesPanelUI, Assembly-CSharp" in scene and
          "m_MethodName: Toggle" in scene,
          f"Level {level} retains the Notes button's controller binding")

raise SystemExit(1 if failures else 0)
