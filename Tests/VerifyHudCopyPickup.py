"""Drive the production icon lifecycle without starting Unity or reading saves."""
from pathlib import Path
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Temp/HudCopyPickupRegression"
SCRIPTING = Path("/Applications/Unity/Hub/Editor/6000.4.5f1/Unity.app/Contents/Resources/Scripting")
DOTNET = SCRIPTING / "NetCoreRuntime/dotnet"
FRAMEWORK = next((SCRIPTING / "NetCoreRuntime/shared/Microsoft.NETCore.App").glob("*"))
OUT.mkdir(parents=True, exist_ok=True)

# Only engine-facing dependencies are substituted. The icon's subscription,
# refresh, lock and unlock methods are compiled from the production file.
HARNESS = r'''
using System;
using System.Reflection;
namespace UnityEngine {
    public class MonoBehaviour { public string name = "CaseFolderIconButton"; }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class CanvasGroup { public float alpha; }
    public static class Debug { public static void Log(object message) { } }
}
namespace UnityEngine.UI { public class Button { public bool interactable; } }
public class HudSubmittableIcon {
    public bool Available;
    public int Updates;
    public void SetAvailable(bool value) { Available = value; Updates++; }
}
public static class FolderToHUDIcon {
    public static int Count;
    public static int GetStoredFolderCount() => Count;
}
public class HudHotbar { public static HudHotbar Instance; public object Inventory; }
public static class InventoryRules { public static bool Owns(object r, string id) => false; }
public static class InventoryCatalogue { public const string CaseFolder = "case-folder"; }
public static class GameplayEvents {
    public static event Action OnCaseFolderOpened, OnCaseFolderStored, OnCaseFolderReturned,
        OnCaseCompleted, OnTaxCodeBookFirstOpened, OnNotesUnlockRequested, OnTaxReturnCollected,
        OnClientCopyPickedUp;
    public static void PickUpCopy() => OnClientCopyPickedUp?.Invoke();
}
public static class Program {
    static int checks;
    static void Check(bool ok, string message) {
        checks++;
        if (!ok) throw new Exception(message);
    }
    static void Call(HudIconButton icon, string name) => typeof(HudIconButton)
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(icon, null);
    static void Set(HudIconButton icon, string name, object value) => typeof(HudIconButton)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(icon, value);
    public static void Main() {
        var icon = new HudIconButton();
        var button = new UnityEngine.UI.Button();
        var pointer = new HudSubmittableIcon();
        Set(icon, "button", button);
        Set(icon, "submittableIcon", pointer);
        Call(icon, "Awake");
        Call(icon, "OnEnable");
        Check(!button.interactable && !pointer.Available, "Auditor's empty icon starts locked");
        // Quiet storage changes count only: no OnCaseFolderStored is raised.
        FolderToHUDIcon.Count = 1;
        GameplayEvents.PickUpCopy();
        Check(button.interactable && pointer.Available,
            "Copy pickup must unlock the actual Button and its drag/pointer controller");
        int updates = pointer.Updates;
        Call(icon, "OnDisable");
        GameplayEvents.PickUpCopy();
        Check(pointer.Updates == updates, "Disabled icon must unsubscribe from pickup");
        Call(icon, "OnEnable");
        updates = pointer.Updates;
        GameplayEvents.PickUpCopy();
        Check(pointer.Updates == updates + 1, "Re-enabled icon has exactly one subscription");
        FolderToHUDIcon.Count = 0;
        GameplayEvents.PickUpCopy();
        Check(!button.interactable && !pointer.Available, "A notification cannot invent a carried copy");
        Call(icon, "OnDisable");
        var notes = new HudIconButton();
        var notesPointer = new HudSubmittableIcon();
        Set(notes, "unlockTrigger", HudIconButton.UnlockTrigger.NotesUnlockRequested);
        Set(notes, "submittableIcon", notesPointer);
        Call(notes, "Awake");
        Call(notes, "OnEnable");
        FolderToHUDIcon.Count = 1;
        GameplayEvents.PickUpCopy();
        Check(!notesPointer.Available, "Copy pickup cannot unlock an unrelated icon");
        Call(notes, "OnDisable");
        Console.WriteLine($"PASS: {checks} production icon lifecycle checks (native pointer/rendering not run)");
    }
}
'''
(OUT / "Harness.cs").write_text(HARNESS)
assembly = OUT / "HudCopyPickup.dll"
args = ["-nostdlib+", "-langversion:latest", "-target:exe", "-nowarn:0067", f'-out:"{assembly}"']
args += [f'-r:"{p}"' for p in FRAMEWORK.glob("*.dll")]
args += [f'"{OUT / "Harness.cs"}"', f'"{ROOT / "Assets/Scripts/UI/HUD/HudIconButton.cs"}"']
rsp = OUT / "compile.rsp"
rsp.write_text("\n".join(args))
subprocess.run([DOTNET, SCRIPTING / "DotNetSdkRoslyn/csc.dll", "@" + str(rsp)], check=True)
assembly.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
    "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": FRAMEWORK.name}}}))
subprocess.run([DOTNET, assembly], check=True)
