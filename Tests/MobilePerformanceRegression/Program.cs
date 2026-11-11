using System;
using System.IO;

// Android frame budget and shipped performance settings. Source, asset and logic checks only;
// real frame rates on a phone remain a manual check.

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Read(params string[] parts) => File.ReadAllText(Path.Combine(root, Path.Combine(parts)));

// Feeds a steady frame time for the given number of seconds; returns how many level changes fired.
int Run(FrameBudget budget, float frameSeconds, float seconds)
{
    int changes = 0;
    for (float t = 0f; t < seconds; t += frameSeconds)
        if (budget.Sample(frameSeconds)) changes++;
    return changes;
}

var smooth = new FrameBudget(0);
Check(smooth.TargetFrameRate == 60 && !smooth.ReducesResolution, "a scene starts at 60 fps and full resolution");
Check(Run(smooth, 1f / 60f, 60f) == 0 && smooth.Level == 0, "a phone holding 60 fps is never stepped down");

var slight = new FrameBudget(0);
Run(slight, 1f / 45f, 8f);
Check(slight.Level == 1 && slight.TargetFrameRate == 60 && slight.ReducesResolution, "a small shortfall lowers resolution first and keeps 60 fps");

var slow = new FrameBudget(0);
Run(slow, 1f / 30f, 30f);
Check(slow.Level == FrameBudget.LowestLevel && slow.TargetFrameRate == 30 && !slow.ReducesResolution, "a slow phone ends at steady 30 fps with normal resolution");
Check(Run(slow, 1f / 20f, 30f) == 0, "the lowest step is final");

var hitches = new FrameBudget(0);
bool changed = false;
for (int i = 0; i < 40; i++) changed |= hitches.Sample(1f);
Check(!changed && hitches.Level == 0, "scene-load and resume hitches do not count as slow frames");

Check(new FrameBudget(99).Level == FrameBudget.LowestLevel && new FrameBudget(-1).Level == 0, "a remembered level stays in range");

string pipeline = Read("Assets", "Settings", "Mobile_RPAsset.asset");
Check(pipeline.Contains("m_SupportsHDR: 0"), "the mobile pipeline draws without an HDR buffer");
Check(pipeline.Contains("m_RenderScale: 0.8"), "the mobile render scale is unchanged");
Check(pipeline.Contains("m_ShadowDistance: 35"), "the shadow distance is trimmed");

string runtime = Read("Assets", "Scripts", "Core", "MobilePerformance.cs");
Check(runtime.Contains("if (!Application.isMobilePlatform) return;"), "the Editor and Play Mode are untouched");

string cutscene = Read("Assets", "Scripts", "Cutscenes", "Cutscene01Runtime.cs");
Check(cutscene.Contains("backdropCamera.cullingMask = mask") && !cutscene.Contains("backdropCamera.enabled"), "the covered cutscene camera stays enabled, so Camera.main still works");

string minimap = Read("Assets", "Scripts", "UI", "OfficeMinimap.cs");
Check(minimap.Contains("mapCamera.enabled = ShouldRenderMap();") && minimap.Contains("if (!visible) mapDrawn = false;"), "the minimap redraws less often and draws at once when shown again");

string audio = Read("Assets", "Scripts", "AudioManager", "AudioManager.cs");
Check(audio.Contains("!PointerPressedThisFrame() && Time.unscaledTime < nextButtonScan"), "buttons are wired on press, before their click fires");

string batching = Read("Assets", "Editor", "AndroidStaticBatchingBuildStep.cs");
Check(batching.Contains("report == null || report.summary.platform != BuildTarget.Android"), "static batching runs only in Android player builds");
foreach (string moving in new[] { "door", "gate", "ocean" })
    Check(batching.Contains($"\"{moving}\""), $"scenery named {moving} stays dynamic");

string setup = Read("Assets", "Editor", "AndroidPerformanceSetup.cs");
Check(setup.Contains("ui ? shownSize : Mathf.Min(shownSize, MaxWorldTexture)"), "UI art keeps its resolution when compressed");
Check(setup.Contains("AudioClipLoadType.CompressedInMemory") && !setup.Contains("AudioClipLoadType.Streaming"), "long clips are ready at once, so playlist checks still behave");

Console.WriteLine($"Mobile performance: {checks - failures}/{checks} passed");
return failures == 0 ? 0 : 1;
