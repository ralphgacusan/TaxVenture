using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

// Level 1 Living Plaza regression (Documentation/Level1LivingPlaza).
// Pure rules compiled from production: MarieTripRules.cs, PageTurnMath.cs.
// Everything else is checked from the shipped files: plaza_life.json,
// Office_Level1.unity, NpcHumanoid.controller and source contracts.
// Unity Play Mode, rendering, animation and feel are NOT covered here.

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
string Src(params string[] parts) => Read(new[] { "Assets", "Scripts" }.Concat(parts).ToArray());
int Index(string body, string text) => body.IndexOf(text, StringComparison.Ordinal);
string Body(string source, string start) { int i = Index(source, start); return i < 0 ? "" : source.Substring(i); }
string Between(string source, string start, string end) { string b = Body(source, start); int e = Index(b, end); return e > 0 ? b.Substring(0, e) : b; }
string MetaGuid(params string[] parts) =>
    Regex.Match(Read(parts.Take(parts.Length - 1).Concat(new[] { parts[^1] + ".meta" }).ToArray()), @"guid: (\w+)").Groups[1].Value;

// =====================================================================
// A. Marie's trip rules (pure)
// =====================================================================
var L = typeof(MarieTripRules.Leg);
MarieTripRules.Leg[] legs = (MarieTripRules.Leg[])Enum.GetValues(L);
bool[] tf = { false, true };
bool nothingBefore = true, noRestart = true, roomTerminal = true, neverBack = true;
foreach (var leg in legs)
    foreach (bool doors in tf)
        foreach (bool reception in tf)
            foreach (bool checkIn in tf)
            {
                if (MarieTripRules.Next(leg, false, doors, reception, checkIn) != leg) nothingBefore = false;
                var next = MarieTripRules.Next(leg, true, doors, reception, checkIn);
                if ((leg == MarieTripRules.Leg.ToReception || leg == MarieTripRules.Leg.ToRoom) && next != leg) noRestart = false;
                if (leg == MarieTripRules.Leg.InRoom && next != leg) roomTerminal = false;
                if ((int)next < (int)leg) neverBack = false;
            }
Check(nothingBefore, "before the side quest Marie never leaves her plaza activity (any doors / hand-over state)");
Check(noRestart && roomTerminal && neverBack, "a trip in progress is never restarted, the conference room is final, and she never goes back");
Check(MarieTripRules.Next(MarieTripRules.Leg.Plaza, true, true, false, true) == MarieTripRules.Leg.Plaza,
    "she waits in the plaza while the front doors are still shut");
Check(MarieTripRules.Next(MarieTripRules.Leg.Plaza, true, false, false, false) == MarieTripRules.Leg.ToReception,
    "side quest done and doors open: she walks to the front desk");
Check(MarieTripRules.Next(MarieTripRules.Leg.Plaza, true, false, true, false) == MarieTripRules.Leg.ToRoom,
    "hand-over finished before she left the plaza: she walks straight to the conference room");
Check(MarieTripRules.Next(MarieTripRules.Leg.AtReception, true, false, false, true) == MarieTripRules.Leg.AtReception,
    "at the front desk she waits for the receptionist's hand-over");
Check(MarieTripRules.Next(MarieTripRules.Leg.AtReception, true, false, true, false) == MarieTripRules.Leg.AtReception &&
      MarieTripRules.Next(MarieTripRules.Leg.AtReception, true, false, true, true) == MarieTripRules.Leg.ToRoom,
    "hand-over done (even before she arrived): after her short check-in she walks on, never stuck waiting for an event that already happened");
Check(MarieTripRules.Resume(true, false, false) == MarieTripRules.Leg.InRoom && MarieTripRules.Resume(false, true, true) == MarieTripRules.Leg.InRoom &&
      MarieTripRules.Resume(false, true, false) == MarieTripRules.Leg.AtReception && MarieTripRules.Resume(false, false, false) == MarieTripRules.Leg.Plaza &&
      MarieTripRules.Resume(false, false, true) == MarieTripRules.Leg.Plaza,
    "resume: saved in the room or hand-over done -> conference room; side quest only -> front desk; otherwise the plaza");
Check(MarieTripRules.StartsOutside(MarieTripRules.Leg.Plaza) && MarieTripRules.StartsOutside(MarieTripRules.Leg.ToReception) &&
      !MarieTripRules.StartsOutside(MarieTripRules.Leg.AtReception),
    "a walk to the room goes through the front doors only when she is still outside");

// =====================================================================
// B. Page turn timing (pure, production PageTurnMath)
// =====================================================================
bool monotonicF = true, monotonicB = true, printFadeMonotonic = true;
float prevF = -1f, prevB = 181f, prevPrintAlpha = 1f;
for (int i = 0; i <= 200; i++)
{
    float t = i / 200f;
    float f = PageTurnMath.Angle(1, t), b = PageTurnMath.Angle(-1, t);
    float printAlpha = PageTurnMath.PrintAlpha(f);
    if (f < prevF - 1e-4f) monotonicF = false;
    if (b > prevB + 1e-4f) monotonicB = false;
    if (printAlpha > prevPrintAlpha + 1e-4f || PageTurnMath.FrontVisible(f) != (printAlpha > 0f)) printFadeMonotonic = false;
    prevF = f; prevB = b;
    prevPrintAlpha = printAlpha;
}
Check(PageTurnMath.Angle(1, 0f) == 0f && PageTurnMath.Angle(1, 1f) == 180f && monotonicF,
    "forward: the old page lifts off the spine from flat (0) to laid over (180), never reversing");
Check(PageTurnMath.Angle(-1, 0f) == 180f && PageTurnMath.Angle(-1, 1f) == 0f && monotonicB,
    "backward: the previous page comes back over the spine (180) and lands flat (0), never reversing");
Check(printFadeMonotonic && PageTurnMath.PrintAlpha(0f) == 1f &&
      Math.Abs(PageTurnMath.PrintAlpha((PageTurnMath.PrintFadeStartAngle + PageTurnMath.PrintFadeEndAngle) * 0.5f) - 0.5f) < 1e-5f &&
      PageTurnMath.PrintAlpha(PageTurnMath.PrintFadeEndAngle) == 0f && !PageTurnMath.FrontVisible(90f),
    "front print fades smoothly and monotonically to zero before the leaf becomes edge-on");
bool printGoneBeforeHalf = Enumerable.Range(1, 100).Any(i =>
    PageTurnMath.PrintAlpha(PageTurnMath.Angle(1, i / 100f)) == 0f && i < 50);
Check(printGoneBeforeHalf && PageTurnMath.PrintAlpha(PageTurnMath.Angle(1, 0f)) == 1f &&
      PageTurnMath.PrintAlpha(PageTurnMath.Angle(1, 1f)) == 0f,
    "the print is readable at lift-off and disappears before halfway while the paper keeps turning");
Check(PageTurnMath.Alpha(1, 0f) == 1f && PageTurnMath.Alpha(1, 0.69f) == 1f && PageTurnMath.Alpha(1, 1f) == 0f &&
      PageTurnMath.Alpha(-1, 0f) == 0f && PageTurnMath.Alpha(-1, 0.31f) == 1f,
    "the turned-away leaf stays solid until it lands, then fades; a returning leaf appears as it lifts");
// Destination content is committed only after the leaf has settled.
string stage = Src("UI", "Books", "PageTurnStage.cs");
string begin = Between(stage, "public bool Begin(", "/// <summary>Ends the turn now");
string animatedBegin = Between(begin, "pendingCommit = commit;", "host.StartCoroutine(Play(");
Check(begin.Contains("pendingCommit = commit;") && !animatedBegin.Contains("commit?.Invoke();") &&
      begin.Contains("LeafPage = direction >= 0 ? fromPage : -1;") &&
      begin.Contains("UnderPage = direction < 0 ? fromPage : -1;") && begin.Contains("HidePaper();") &&
      begin.Contains("if (!canAnimate)\n        {\n            commit?.Invoke();\n            return false;"),
    "destination contents stay unrendered during both turn directions; only source print rides the old leaf or backing, with an immediate fallback when animation is unavailable");
float Simulate(float fps, float duration)
{
    float elapsed = 0f, real = 0f;
    while (PageTurnMath.Progress(elapsed, duration) < 1f) { elapsed += PageTurnMath.Step(1f / fps); real += 1f / fps; }
    return real;
}
bool timingOk = new[] { 30f, 60f, 90f, 120f, 144f }.All(fps => Math.Abs(Simulate(fps, 0.5f) - 0.5f) <= 1f / fps + 1e-4f) &&
                Math.Abs(Simulate(12f, 0.5f) - 0.5f) <= 1f / 12f + 1e-4f;
Check(timingOk, "a 0.5 s turn lasts 0.5 s (within one frame) at 12, 30, 60, 90, 120 and 144 fps");
Check(PageTurnMath.Step(2f) == PageTurnMath.MaxStep && PageTurnMath.Step(-1f) == 0f && Simulate(4f, 0.5f) > 0.5f,
    "a single very slow frame (content rebuild) advances at most MaxStep: the turn slows a moment instead of skipping");
Check(stage.Contains("elapsed += PageTurnMath.Step(Time.unscaledDeltaTime);") && stage.Contains("PageTurnMath.Progress(elapsed, duration)"),
    "the turn runs on unscaled time (the desk UI keeps working while the game is paused), through PageTurnMath");
string complete = Between(stage, "public void Complete()", "private IEnumerator Play(");
Check(complete.Contains("host.StopCoroutine(routine)") && complete.Contains("Destroy(leaf.gameObject)") && complete.Contains("Destroy(under.gameObject)") &&
      complete.Contains("ShowPaper();") && complete.Contains("paper.localRotation = restRotation;") &&
      complete.Contains("pendingCommit = null;") && complete.Contains("commit?.Invoke();") &&
      complete.Split(new[] { "commit?.Invoke();" }, StringSplitOptions.None).Length == 2 &&
      complete.Contains("leaf.gameObject.SetActive(false);") && complete.Contains("under.gameObject.SetActive(false);") &&
      Index(complete, "paper.localRotation = restRotation;") < Index(complete, "commit?.Invoke();") &&
      Index(complete, "commit?.Invoke();") < Index(complete, "ShowPaper();"),
    "completing (new input, closing, disabling) stops the turn, removes both copies, shows the live paper and puts it at rest");
string strip = Between(stage, "private static void StripToGraphics(", "private static bool Keeps(");
Check(strip.Contains("DestroyImmediate(component)") && strip.Contains("graphic.raycastTarget = false;") &&
      Between(stage, "private RectTransform VisualCopy(", "private static void StripToGraphics(").Contains("holder.SetActive(false);"),
    "copies keep only graphics: built under an inactive holder (no script wakes), every script/collider removed, no raycast targets");
string visualCopy = Between(stage, "private RectTransform VisualCopy(", "private static void StripToGraphics(");
Check(Index(stage, "Canvas.ForceUpdateCanvases();") < Index(stage, "under = VisualCopy(") &&
      visualCopy.Contains("CaptureRectLayout(source);") && visualCopy.Contains("RestoreRectLayout(copy);") &&
      visualCopy.Contains("layout.ignoreLayout = true;") && visualCopy.Contains("AddPaperShadow(copy);") &&
      stage.Contains("color.a *= printAlpha;"),
    "the copy freezes resolved text geometry, ignores its parent's layout, fades ink independently and adds a light paper shadow");
foreach (string ui in new[] { "CaseFolderUI.cs", "TaxCodeBookUI.cs" })
{
    string src = Src("UI", ui);
    string turn = Between(src, "private void TurnToCurrentPage(", "private void StopPageTurn(");
    Check(Index(turn, "StopPageTurn();") >= 0 && Index(turn, "StopPageTurn();") < Index(turn, "pageTurn.Begin(") &&
          turn.Contains("int shown = ") && !src.Contains("DeskMotion.TurnPage") && Between(src, "private void StopPageTurn(", "private RectTransform GetPageTurnRoot()").Contains("pageTurn?.Complete();"),
        ui + ": a new turn first completes the running one (rapid taps cannot corrupt the index or leave a half page), and closing completes it");
}
string folder = Src("UI", "CaseFolderUI.cs");
string book = Src("UI", "TaxCodeBookUI.cs");
Check(Between(folder, "public void NextPage()", "public void PreviousPage()").Contains("if (currentPageIndex < pages.Count - 1)") &&
      Between(folder, "public void PreviousPage()", "// PAGE LAYOUT").Contains("if (currentPageIndex > 0)") &&
      Between(book, "public void NextPage()", "public void PreviousPage()").Contains("if (currentSectionIndex < Sections.Count - 1)") &&
      Between(book, "public void PreviousPage()", "// PAGE TURN").Contains("if (currentSectionIndex > 0)"),
    "first and last pages: no turn past either end in the folder or the book");
Check(Between(folder, "private System.Collections.IEnumerator LeafThrough(", "public RectTransform SurfaceFor(").Contains("() => { currentPageIndex = shown; RenderPage(shown); }") &&
      Between(folder, "private System.Collections.IEnumerator LeafThrough(", "public RectTransform SurfaceFor(").Contains("while (pageTurn.IsTurning) yield return null;"),
    "leafing through several pages commits each page after its turn settles and waits for completion");
Check(Src("SideQuest1", "AttachedRecordsPage.cs").Contains("RectTransform surface = pages.IsShowing ? pages.SurfaceFor(PageIndex) : null;"),
    "attached records ride the surface printing their page during a turn (never left on a covered page)");

// =====================================================================
// C. Ambient content (plaza_life.json)
// =====================================================================
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
JsonDocument plaza = JsonDocument.Parse(Read("Assets", "Resources", "Levels", "Level_01", "plaza_life.json"));
var npcs = plaza.RootElement.GetProperty("npcs").EnumerateArray().ToList();
var ids = npcs.Select(n => n.GetProperty("id").GetString()).ToList();
var names = npcs.Select(n => n.GetProperty("name").GetString()).ToList();
Check(npcs.Count == 4 && ids.Distinct().Count() == 4 && names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 4,
    "exactly four ambient characters, with unique ids and names");
string[] emoteTags = { "heart", "sparkles", "alert", "question", "thinking", "idea", "sweat", "worried", "annoyed", "music" };
string[] banned = { "resident", "citizen", "alien", "classification", "taxpayer type", "click", "mouse", "wasd", "keyboard" };
bool shapeOk = true, emotesOk = true, wordsOk = true, briefOk = true, writersOk = true;
foreach (JsonElement npc in npcs)
{
    foreach (string key in new[] { "fullName", "role", "background", "attitude" })
        if (string.IsNullOrWhiteSpace(npc.GetProperty(key).GetString())) writersOk = false;
    var talks = npc.GetProperty("talks").EnumerateArray().ToList();
    if (talks.Count < 2) shapeOk = false;
    foreach (JsonElement talk in talks)
    {
        var lines = talk.GetProperty("lines").EnumerateArray().ToList();
        if (lines.Count < 1 || lines.Count > 4) shapeOk = false;
        int words = 0;
        foreach (JsonElement line in lines)
        {
            string speaker = line.GetProperty("speaker").GetString();
            string text = line.GetProperty("text").GetString() ?? "";
            if (speaker != "Npc" && speaker != "Player") shapeOk = false;
            if (text.Length == 0 || text.Length > 110) briefOk = false;
            words += text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (banned.Any(b => text.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)) wordsOk = false;
            if (line.TryGetProperty("emote", out JsonElement emote) && !string.IsNullOrEmpty(emote.GetString()) && !emoteTags.Contains(emote.GetString())) emotesOk = false;
        }
        if (words > 45) briefOk = false;
    }
}
Check(shapeOk, "every character has at least two short talks (1-4 lines each, Npc/Player speakers): replayable small talk");
Check(briefOk, "small talk stays brief (a line under 110 characters, a talk under 45 words): flavour, never a lecture");
Check(wordsOk, "no talk names a taxpayer classification or uses non-touch wording (golden rule, mobile wording)");
Check(emotesOk, "every emote tag is one the existing emotion system knows");
Check(writersOk, "each character has a full name, a role, a background and an attitude for writers");
var attitudes = npcs.Select(n => n.GetProperty("attitude").GetString().ToLowerInvariant()).ToList();
Check(attitudes.Distinct().Count() == 4 && attitudes.Contains("friendly") && attitudes.Contains("busy") && attitudes.Contains("reserved") && attitudes.Any(a => a.Contains("dismissive")),
    "the four personalities differ: friendly, busy, reserved and mildly dismissive");
JsonElement travel = plaza.RootElement.GetProperty("marieTravel");
Check(new[] { "toOffice", "atReception", "toRoom" }.All(k => travel.GetProperty(k).GetArrayLength() >= 1),
    "Marie has a one-line answer for each part of her walk (to the office, at the desk, to the room)");

// =====================================================================
// D. Scene wiring (Office_Level1.unity)
// =====================================================================
string scene = Read("Assets", "Scenes", "Office_Level1.unity").Replace("\r\n", "\n");
var docs = new Dictionary<string, (int cls, string body, bool stripped)>();
foreach (string chunk in ("\n" + scene).Split("\n--- !u!").Skip(1))
{
    Match h = Regex.Match(chunk, @"^(\d+) &(-?\d+)( stripped)?\n");
    docs[h.Groups[2].Value] = (int.Parse(h.Groups[1].Value), chunk.Substring(h.Length), h.Groups[3].Success);
}
string F(string body, string key) { Match m = Regex.Match(body, @"^\s*" + Regex.Escape(key) + @": (.*)$", RegexOptions.Multiline); return m.Success ? m.Groups[1].Value.Trim() : null; }
string Ref(string body, string key) { Match m = Regex.Match(body, @"^\s*" + Regex.Escape(key) + @": \{fileID: (-?\d+)", RegexOptions.Multiline); return m.Success ? m.Groups[1].Value : null; }
List<string> ArrayRefs(string body, string key)
{
    Match m = Regex.Match(body, @"^\s*" + Regex.Escape(key) + @":\n(?<items>(?:^\s+- \{fileID: -?\d+\}\n?)+)", RegexOptions.Multiline);
    return Regex.Matches(m.Groups["items"].Value, @"\{fileID: (-?\d+)\}").Select(x => x.Groups[1].Value).ToList();
}
var goName = docs.Where(d => d.Value.cls == 1 && !d.Value.stripped).ToDictionary(d => d.Key, d => F(d.Value.body, "m_Name"));
var trGo = new Dictionary<string, string>();
var trFather = new Dictionary<string, string>();
foreach (var d in docs.Where(d => d.Value.cls == 4 && !d.Value.stripped))
{
    trGo[d.Key] = Ref(d.Value.body, "m_GameObject");
    trFather[d.Key] = Ref(d.Value.body, "m_Father");
}
var goTr = trGo.GroupBy(p => p.Value).ToDictionary(g => g.Key, g => g.First().Key);
string PathOf(string tr) { var parts = new List<string>(); while (tr != null && tr != "0" && trGo.ContainsKey(tr)) { parts.Add(goName.GetValueOrDefault(trGo[tr], "?")); tr = trFather.GetValueOrDefault(tr); } parts.Reverse(); return string.Join("/", parts); }
string GoByPath(string path) => goTr.Where(p => goName.ContainsKey(p.Key) && PathOf(p.Value) == path).Select(p => p.Key).FirstOrDefault();
List<string> Components(string go) => Regex.Matches(docs[go].body, @"component: \{fileID: (-?\d+)\}").Select(m => m.Groups[1].Value).ToList();
string ScriptGuid(string comp) { Match m = Regex.Match(docs[comp].body, @"m_Script: \{fileID: \d+, guid: (\w+)"); return m.Success ? m.Groups[1].Value : null; }
List<string> WithScript(string go, string guid) => Components(go).Where(c => docs.ContainsKey(c) && ScriptGuid(c) == guid).ToList();
var prefabs = docs.Where(d => d.Value.cls == 1001).Select(d => (id: d.Key, parent: Ref(d.Value.body, "m_TransformParent"),
    source: Regex.Match(d.Value.body, @"m_SourcePrefab: \{fileID: \d+, guid: (\w+)").Groups[1].Value, body: d.Value.body)).ToList();
List<(string id, string parent, string source, string body)> ModelsUnder(string go)
{
    var result = new List<(string, string, string, string)>();
    string tr = goTr[go];
    foreach (var child in trFather.Where(p => p.Value == tr).Select(p => p.Key))
        result.AddRange(prefabs.Where(p => p.parent == child));
    result.AddRange(prefabs.Where(p => p.parent == tr));
    return result;
}

string gAmbient = MetaGuid("Assets", "Scripts", "SideQuest1", "AmbientNpcInteractable.cs");
string gTaxpayer = MetaGuid("Assets", "Scripts", "SideQuest1", "TaxpayerNpcInteractable.cs");
string gMarker = MetaGuid("Assets", "Scripts", "FSM", "NPC", "NpcStatusMarker.cs");
string gLife = MetaGuid("Assets", "Scripts", "FSM", "NPC", "NpcLife.cs");
string gLoco = MetaGuid("Assets", "Scripts", "FSM", "NPC", "NpcLocomotion.cs");
string gArm = MetaGuid("Assets", "Scripts", "FSM", "NPC", "StaticArmRig.cs");
string gRoute = MetaGuid("Assets", "Scripts", "SideQuest1", "MarieOfficeRoute.cs");
string gGuide = MetaGuid("Assets", "Scripts", "SideQuest1", "TutorialNpcController.cs");
string gController = MetaGuid("Assets", "AssetIntegration", "NpcHumanoid.controller");
string gPickup = MetaGuid("Assets", "Scripts", "SideQuest1", "TaxpayerProfileWorldPickup.cs");

var ambientComps = docs.Where(d => d.Value.cls == 114 && ScriptGuid(d.Key) == gAmbient).ToList();
var sceneIds = ambientComps.Select(c => F(c.Value.body, "ambientId")).ToList();
Check(ambientComps.Count == 4 && sceneIds.OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x)),
    "exactly four ambient NPCs in Level 1, matching the four characters in plaza_life.json");
var ambientGos = ambientComps.Select(c => Ref(c.Value.body, "m_GameObject")).ToList();
string pickupPrefab = MetaGuid("Assets", "Prefabs", "ProfileWorldPickup_TEMP.prefab");
// Pickups that live inside prefab instances have no scene GameObject of their own: their instance parent is checked through ModelsUnder.
var pickupOwners = docs.Where(d => d.Value.cls == 114 && ScriptGuid(d.Key) == gPickup)
    .Select(d => Ref(d.Value.body, "m_GameObject")).Where(go => go != null && goTr.ContainsKey(go)).Select(go => PathOf(goTr[go])).ToList();
Check(ambientGos.All(g => WithScript(g, gTaxpayer).Count == 0 && WithScript(g, gMarker).Count <= 1 &&
                          ModelsUnder(g).All(p => p.source != pickupPrefab) &&
                          pickupOwners.All(owner => !owner.StartsWith(PathOf(goTr[g])))),
    "ambient NPCs have no side-quest talk or profile/ID pickup and never duplicate conversation markers");
Check(ambientGos.All(g => PathOf(goTr[g]).StartsWith("Environment/Outside/Plaza/AmbientNPCs/")),
    "ambient NPCs live in the plaza (Environment/Outside/Plaza/AmbientNPCs)");

string[] protectedPaths =
{
    "Environment/Outside/Plaza/NPCS/NPC_Marie", "Environment/Outside/Plaza/NPCS/NPC_Jhun", "Environment/Outside/Plaza/NPCS/NPC_Milo",
    "Environment/Outside/Plaza/NPCS/NPC_Zorbix", "Environment/Outside/Plaza/NPCS/NPC_TutorialGuide",
    "Environment/OfficeBuilding/Rooms/Reception/Receptionist", "Environment/OfficeBuilding/Rooms/AuditOffice/Auditor"
};
var protectedGos = protectedPaths.Select(GoByPath).ToList();
Check(protectedGos.All(g => g != null), "all seven protected characters are still in the scene at their paths");
var protectedModels = protectedGos.SelectMany(g => ModelsUnder(g)).Select(p => p.source).Distinct().ToList();
var ambientModels = ambientGos.SelectMany(g => ModelsUnder(g)).Select(p => p.source).ToList();
Check(ambientModels.Count == 4 && ambientModels.Distinct().Count() == 4 && !ambientModels.Intersect(protectedModels).Any() &&
      protectedModels.Count >= 7,
    "each ambient NPC uses its own unused model; none reuses a protected character's model (four originals, Arthur, receptionist, Auditor)");
var synty = ambientModels.Select(g => Directory.GetFiles(Path.Combine(root, "Assets", "Synty", "PolygonOffice", "Prefabs", "Characters"), "*.prefab.meta")
    .FirstOrDefault(m => File.ReadAllText(m).Contains("guid: " + g))).ToList();
Check(synty.All(m => m != null), "the ambient models are existing PolygonOffice characters (the TaxVentureGameEnvironment cast); nothing imported");

var npcIdsInScene = docs.Where(d => d.Value.cls == 114 && ScriptGuid(d.Key) == gTaxpayer).Select(d => (id: F(d.Value.body, "npcId"), pickup: Ref(d.Value.body, "profilePickup"))).ToList();
Check(npcIdsInScene.Count == 4 && npcIdsInScene.Select(x => x.id).OrderBy(x => x).SequenceEqual(new[] { "npc_jhunjhun", "npc_marie", "npc_milo", "npc_zorbix" }) &&
      npcIdsInScene.All(x => x.pickup != null && x.pickup != "0"),
    "only the four original clients carry a side-quest talk, each still with its own profile pickup");

bool lifeOk = protectedGos.Take(5).All(g => WithScript(g, gLife).Count == 1 && WithScript(g, gLoco).Count == 1) &&
              ambientGos.All(g => WithScript(g, gLife).Count == 1 && WithScript(g, gLoco).Count == 1);
Check(lifeOk, "the four clients, Arthur and the four ambient NPCs each have one NpcLife and one NpcLocomotion");
string jhunLife = docs[WithScript(protectedGos[1], gLife)[0]].body;
string marieLife = docs[WithScript(protectedGos[0], gLife)[0]].body;
Check(Ref(jhunLife, "seat") != "0" && float.Parse(F(jhunLife, "standForPlayerDistance"), System.Globalization.CultureInfo.InvariantCulture) > 0f &&
      F(jhunLife, "standToTalk") == "1",
    "Jhun sits on the bench beside his spot and stands up for the player before he talks");
Check(F(marieLife, "phoneUntilRounds") == "1" && Regex.Matches(Between(marieLife, "route:", "pauseSeconds"), @"fileID: \d+").Count == 2 && F(marieLife, "activity") == "1",
    "Marie strolls a short route on her phone call while she waits for the rounds");
Check(WithScript(protectedGos[5], gArm).Count == 1 && WithScript(protectedGos[6], gArm).Count == 1 && WithScript(protectedGos[5], gLife).Count == 0 &&
      WithScript(protectedGos[6], gLife).Count == 0,
    "the receptionist and the Auditor get the arm rig (no T-pose) and stay at their posts (no walking life)");

var routes = docs.Where(d => d.Value.cls == 114 && ScriptGuid(d.Key) == gRoute).ToList();
Check(routes.Count == 1 && new[] { "entranceOutside", "entranceInside", "receptionWait", "innerDoorNorth", "innerDoorSouth", "conferenceDoorOutside", "conferenceDoorInside" }
          .All(k => Ref(routes[0].Value.body, k) is string r && r != "0" && docs.ContainsKey(r)),
    "one authored Marie route: both sides of the front doors, the front-desk wait, both sides of the inner and conference-room doors");

string miloGo = GoByPath("Environment/Outside/Plaza/NPCS/NPC_Milo");
string zorbixGo = GoByPath("Environment/Outside/Plaza/NPCS/NPC_Zorbix");
string miloLifeId = WithScript(miloGo, gLife).Single();
string zorbixLifeId = WithScript(zorbixGo, gLife).Single();
string miloLifeBody = docs[miloLifeId].body, zorbixLifeBody = docs[zorbixLifeId].body;
List<string> miloRoute = ArrayRefs(miloLifeBody, "route"), zorbixRoute = ArrayRefs(zorbixLifeBody, "route");
bool RouteIsAuthoredLoop(string body, List<string> stops)
{
    bool indexOk = int.TryParse(F(body, "startRouteIndex"), out int start) && start >= 0 && start < stops.Count;
    return F(body, "activity") == "1" && F(body, "loopRoute") == "1" && stops.Count >= 5 &&
           stops.Distinct().Count() == stops.Count && stops.All(t => docs.TryGetValue(t, out var d) && d.cls == 4 &&
               trGo.ContainsKey(t) && PathOf(t).StartsWith("Level 1 NPC Life/", StringComparison.Ordinal)) &&
           indexOk && Ref(body, "home") == stops[StartIndex(body)];
}
int StartIndex(string body) => int.TryParse(F(body, "startRouteIndex"), out int value) ? value : -1;
Check(RouteIsAuthoredLoop(miloLifeBody, miloRoute) && RouteIsAuthoredLoop(zorbixLifeBody, zorbixRoute) &&
      !miloRoute.SequenceEqual(zorbixRoute),
    "Milo and Zorbix each have a distinct looping patrol with at least five authored plaza stops, starting at their current home marker");
string miloMeet = Ref(miloLifeBody, "meetPoint"), zorbixMeet = Ref(zorbixLifeBody, "meetPoint");
var longDistanceChatLeaders = new[] { miloLifeBody, zorbixLifeBody }.Count(b => F(b, "leadsChat") == "1");
var meetLeaders = new[] { miloLifeBody, zorbixLifeBody }.Count(b => F(b, "meetAndWave") == "1");
Check(Ref(miloLifeBody, "partner") == zorbixLifeId && Ref(zorbixLifeBody, "partner") == miloLifeId &&
      miloMeet != null && miloMeet != "0" && miloMeet == zorbixMeet && docs.ContainsKey(miloMeet) &&
      longDistanceChatLeaders == 0 && meetLeaders == 1,
    "Milo and Zorbix reference each other, share one meet point, and have one meet leader without a separate long-distance chat");

var danteComp = ambientComps.Single(c => F(c.Value.body, "ambientId") == "dante");
string danteGo = Ref(danteComp.Value.body, "m_GameObject");
string danteLifeId = WithScript(danteGo, gLife).Single();
string danteLifeBody = docs[danteLifeId].body;
var otherAmbientGos = ambientComps.Where(c => F(c.Value.body, "ambientId") != "dante")
    .Select(c => Ref(c.Value.body, "m_GameObject"));
var expectedVisitTargets = protectedGos.Take(5).Concat(otherAmbientGos)
    .SelectMany(g => WithScript(g, gLife).Take(1)).ToHashSet();
var visitTargets = ArrayRefs(danteLifeBody, "visitTargets");
Match visitRadius = Regex.Match(F(danteLifeBody, "visitAreaRadius") ?? "", @"^([-+0-9.eE]+)$");
bool radiusOk = visitRadius.Success && float.TryParse(visitRadius.Groups[1].Value,
    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius) &&
    float.IsFinite(radius) && radius > 0f;
string visitCenter = Ref(danteLifeBody, "visitAreaCenter");
Check(F(danteLifeBody, "activity") == "3" && visitTargets.Count == 8 && visitTargets.Distinct().Count() == 8 &&
      visitTargets.ToHashSet().SetEquals(expectedVisitTargets) && visitTargets.All(t => docs.ContainsKey(t) && ScriptGuid(t) == gLife) &&
      visitCenter != null && visitCenter != "0" && docs.ContainsKey(visitCenter) && docs[visitCenter].cls == 4 && radiusOk,
    "Dante has an ordered visit activity for the other eight plaza NPCs, bounded by an authored center and finite positive radius");

string guideComp = docs.First(d => d.Value.cls == 114 && ScriptGuid(d.Key) == gGuide).Key;
string visualRoot = Ref(docs[guideComp].body, "visualRoot");
string twinPi = docs.ContainsKey(visualRoot) ? Ref(docs[visualRoot].body, "m_PrefabInstance") : null;
var twin = prefabs.FirstOrDefault(p => p.id == twinPi);
var staticArthur = prefabs.FirstOrDefault(p => p.source == "2065c3f07211be8498ccb8697abf26c8" && p.parent == twin.parent);
Check(twin.source == MetaGuid("Assets", "3D Models", "npc_tutorial_static", "npc_tutorial_static1@Standing W_Briefcase Idle.fbx") &&
      Read("Assets", "3D Models", "npc_tutorial_static", "npc_tutorial_static1@Standing W_Briefcase Idle.fbx.meta").Contains("animationType: 3") &&
      staticArthur.body != null && Regex.IsMatch(staticArthur.body, @"propertyPath: m_IsActive\n\s*value: 0"),
    "Arthur shows his own rigged twin (same mesh, Humanoid) instead of the static T-pose copy, which stays hidden");
var overrides = prefabs.Where(p => p.body.Contains("propertyPath: m_Controller") &&
        (protectedGos.Take(5).Concat(ambientGos).Any(g => ModelsUnder(g).Any(m => m.id == p.id))))
    .Select(p => Regex.Match(p.body, @"propertyPath: m_Controller\n\s*value: [^\n]*\n\s*objectReference: \{fileID: 9100000, guid: (\w+)").Groups[1].Value).ToList();
Check(overrides.Count == 9 && overrides.All(g => g == gController),
    "the four clients, Arthur and the four ambient NPCs play the shared NpcHumanoid controller");

var docIds = Regex.Matches(scene, @"^--- !u!\d+ &(-?\d+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToList();
var idSet = new HashSet<string>(docIds);
var dangling = Regex.Matches(scene, @"\{fileID: (-?\d+)\}").Select(m => m.Groups[1].Value).Where(r => r != "0" && !idSet.Contains(r)).Distinct().ToList();
Check(docIds.Count == idSet.Count && dangling.Count == 0 && !scene.Contains("\r\n"), "scene: unique object ids, no missing local references, LF line endings");
Check(scene.TrimEnd().EndsWith("- {fileID: 2147002502}"), "scene: Arthur Start Placement stays the last scene root (Campaign Foundation contract)");

// =====================================================================
// E. Shared controller asset
// =====================================================================
string controller = Read("Assets", "AssetIntegration", "NpcHumanoid.controller");
var stateDocs = controller.Split("--- !u!").Where(c => c.StartsWith("1102 ")).ToList();
var stateNames = stateDocs.Select(c => Regex.Match(c, @"m_Name: (.*)").Groups[1].Value.Trim()).ToList();
Check(new[] { "Standing Idle", "Idle Relaxed", "Briefcase Idle", "Walking", "Strolling" }.All(stateNames.Contains),
    "controller: three idles from the characters' own clips, plus Walking and Strolling");
bool clipsHumanoid = stateDocs.All(c =>
{
    string guid = Regex.Match(c, @"m_Motion: \{fileID: -?\d+, guid: (\w+)").Groups[1].Value;
    string meta = Directory.GetFiles(Path.Combine(root, "Assets", "3D Models"), "*.fbx.meta", SearchOption.AllDirectories).FirstOrDefault(m => File.ReadAllText(m).Contains("guid: " + guid));
    return meta != null && File.ReadAllText(meta).Contains("animationType: 3");
});
Check(clipsHumanoid, "every state plays an existing Humanoid clip (retargets onto the Synty and Mixamo avatars)");
bool loops = stateDocs.Where(c => c.Contains("m_Name: Walking") || c.Contains("m_Name: Strolling")).All(c =>
{
    string self = Regex.Match(c, @"^1102 &(-?\d+)").Groups[1].Value;
    string t = Regex.Match(c, @"m_Transitions:\n  - \{fileID: (-?\d+)\}").Groups[1].Value;
    string trans = controller.Split("--- !u!").FirstOrDefault(x => x.StartsWith("1101 &" + t + "\n"));
    return trans != null && trans.Contains("m_DstState: {fileID: " + self + "}") && trans.Contains("m_HasExitTime: 1");
});
Check(loops && Read("Assets", "3D Models", "npc_elijah", "Elijah_Walking.fbx.meta").Contains("loopTime: 0"),
    "the walk loops through a self-transition, so the clip shared with the cutscenes keeps its import settings");
Check(Regex.IsMatch(controller, @"m_Name: Strolling\n  m_Speed: 0\.8"), "strolling plays the same walk slower (patrols, pacing)");

// =====================================================================
// F. Source contracts
// =====================================================================
string ambientSrc = Src("SideQuest1", "AmbientNpcInteractable.cs");
string plazaSrc = Src("SideQuest1", "PlazaLifeContent.cs");
string lifeSrc = Src("FSM", "NPC", "NpcLife.cs");
string locoSrc = Src("FSM", "NPC", "NpcLocomotion.cs");
string poseSrc = Src("FSM", "NPC", "NpcBodyPose.cs");
string taxpayerSrc = Src("SideQuest1", "TaxpayerNpcInteractable.cs");
string pickupSrc = Src("SideQuest1", "TaxpayerProfileWorldPickup.cs");
string placementSrc = Src("SideQuest1", "ArthurStartPlacement.cs");
Check(placementSrc.Contains("skin.BakeMesh(snapshot, true)") &&
      placementSrc.Contains("skin.transform.TransformPoint(vertex).y") && !placementSrc.Contains("skin.localBounds") &&
      placementSrc.Contains("Destroy(snapshot)") && placementSrc.Contains("DestroyImmediate(snapshot)"),
    "Arthur grounds baked skin geometry in renderer space, never animation culling bounds, and releases the snapshot");
Check(placementSrc.Contains("mover.SetFeetOffset(guideRoot.position.y - groundY)") &&
      Src("SideQuest1", "TutorialNpcController.cs").Contains("ArthurStartPlacement.GroundModel(transform, corkboardPose.position)"),
    "Arthur preserves his measured sole offset while walking and re-grounds a restored corkboard pose");
string armSrc = Src("FSM", "NPC", "StaticArmRig.cs");
string[] rewards = { "AwardConversation", "CaseProgressionManager", "TaxpayerProfile", "profilePickup", "TownEvents", "GameplayEvents", "PlayerProgressManager",
    "LevelScoreLedger", "Score", "SaveNow", "RequestSave", "LevelGate", "OfficeObjectives", "HudFeedback", "Trust", "Coins", "HudHotbar", "Inventory" };
string Code(string s) => Regex.Replace(s, @"///.*|//.*", "");
Check(rewards.All(w => Code(ambientSrc).IndexOf(w, StringComparison.Ordinal) < 0) && rewards.All(w => Code(plazaSrc).IndexOf(w, StringComparison.Ordinal) < 0) &&
      ambientSrc.Contains("NpcAvailabilityRules.Colleague(hasSpoken)") &&
      ambientSrc.Contains("!ArthurOpening.Active.PlazaConversationsOpen") &&
      ambientSrc.Contains("if (GetComponent<NpcStatusMarker>() == null) gameObject.AddComponent<NpcStatusMarker>();") &&
      Between(ambientSrc, "private void EndTalk(bool completed)", "__END__").Contains("if (completed)\n        {\n            hasSpoken = true;"),
    "ambient talk is talk only: no profile/ID, score, coins, trust, inventory, save, gate, objective or town event; triangle/check markers track conversations without giving away ID owners");
Check(new[] { lifeSrc, locoSrc, poseSrc, armSrc }.All(s => rewards.Where(w => w != "Score" && w != "Inventory").All(w => Code(s).IndexOf(w, StringComparison.Ordinal) < 0)) &&
      new[] { lifeSrc, locoSrc, poseSrc, armSrc }.All(s => !Code(s).Contains("TryStartDialogue") && !Code(s).Contains("ChangeState(")),
    "NPC life, movement, poses and the arm rig never start dialogue, change NPC states or touch progress");
Check(Regex.Matches(Code(locoSrc), @"planner\.FindPath\(").Count == 1 && Between(locoSrc, "private void BeginLeg(", "private void Update()").Contains("planner.FindPath(") &&
      !Between(locoSrc, "private void Update()", "private void Finish(").Contains("FindPath("),
    "paths are planned once per leg (and on a stuck replan), never per frame");
Check(Between(locoSrc, "public bool Go(", "public void Stop(").Contains("if (IsTraveling && priority < CurrentPriority) return false;") &&
      Between(locoSrc, "public void Stop(", "public void SetFeetOffset(").Contains("if (IsTraveling && priority < CurrentPriority) return;") &&
      Between(lifeSrc, "public void TravelForQuest(", "public void PlaceForQuest(").Contains("Mover.Stop(NpcLocomotion.Priority.Ambient);"),
    "quest trips outrank ambient ones: an ambient walk can neither replace nor stop a required move");
string frame = Between(lifeSrc, "private void Update()", "// THE PLAYER'S CONVERSATION");
Check(Index(frame, "if (inDialogue)") < Index(frame, "if (questLocked)") && Index(frame, "if (questLocked)") < Index(frame, "switch (activity)") &&
      Between(lifeSrc, "private void BeginConversation()", "private void UpdateConversation()").Contains("Mover.Suspended = true;"),
    "priority order: the player's conversation pauses everything (a quest walk resumes after it), then quest travel, then the activity");
Check(Between(lifeSrc, "private void UpdateQuest()", "private void FinishQuest(").Contains("if (pose != null && !pose.IsStanding)") &&
      Between(lifeSrc, "private void BeginConversation()", "private void UpdateConversation()").Contains("if (seat != null && standToTalk) pose.Sit(false);") &&
      Between(lifeSrc, "private void UpdateConversation()", "private void EndConversation()").Contains("if (pose != null && !pose.IsStanding) return;"),
    "a seated NPC stands before walking off or turning to talk (an interrupted sit simply eases back up from where it is)");
string lifeFrame = Between(lifeSrc, "private void Update()", "private bool OwnsDialogue()");
string pendingHandover = Between(taxpayerSrc, "public bool HasPendingHandover =>", ";");
Check(lifeSrc.Contains("private bool MayWalk => taxpayer == null || !taxpayer.HasPendingHandover;") &&
      taxpayerSrc.Contains("public bool HasPendingHandover =>") && pendingHandover.Contains("profilePickup") &&
      pickupSrc.Contains("public bool IsCollected => collected;") &&
      lifeFrame.Contains("if (!MayWalk && Mover.IsTraveling && Mover.CurrentPriority == NpcLocomotion.Priority.Ambient)") &&
      lifeFrame.Contains("Mover.Stop(NpcLocomotion.Priority.Ambient);") && lifeFrame.Contains("homeFeet = Mover.Feet;"),
    "a client stops an active ambient leg while its talked-to profile awaits collection, then can resume after collection");
string visitEligibility = Between(lifeSrc, "private bool CanVisitTarget(", "// =========================================================");
string activeVisitCheck = Between(lifeSrc, "private void UpdateSocialPause()", "private void BeginSocialPause(");
string visitSetup = Between(lifeSrc, "private void StartVisit(", "private bool CanVisitTarget(");
string meetSetup = Between(lifeSrc, "private bool TryStartMeet()", "private void UpdateSocialPause()");
string reservationEnd = Between(lifeSrc, "private void EndSocialPause(", "private void EnsurePose()");
string socialCancel = Between(lifeSrc, "private static void CancelSocialPause(", "private static bool IsSocialReserved(");
Check(visitEligibility.Contains("bool reservedByThisVisit = socialOwner == this && socialPartner == target && socialKind == SocialKind.Visit;") &&
      visitEligibility.Contains("(IsSocialReserved(target) && !reservedByThisVisit)") &&
      activeVisitCheck.Contains("if (!CanVisitTarget(other, checkArea: socialOwnerArrived))"),
    "a visit owner may continue validating its own reserved target instead of rejecting its active visit");
string socialDeadline = "socialApproachDeadline = Time.time + SocialApproachSeconds(";
string deadlineGuard = "if (Time.time >= socialApproachDeadline)";
Check(visitSetup.Contains(socialDeadline) && meetSetup.Contains(socialDeadline) &&
      activeVisitCheck.Contains(deadlineGuard) &&
      Index(activeVisitCheck, deadlineGuard) < Index(activeVisitCheck, "if (!socialOwnerArrived)") &&
      Between(activeVisitCheck, deadlineGuard, "if (socialKind == SocialKind.Meet)").Contains("EndSocialPause(false);"),
    "visit and meet approaches have a distance-based deadline that cancels before waiting indefinitely for arrival");
Check(activeVisitCheck.Contains("if (other == null || !other.isActiveAndEnabled || inDialogue || other.inDialogue ||") &&
      activeVisitCheck.Contains("OwnsDialogue() || other.OwnsDialogue() || questLocked || other.questLocked)") &&
      socialCancel.Contains("participant == socialOwner || participant == socialPartner") &&
      socialCancel.Contains("socialOwner.EndSocialPause(false);") &&
      reservationEnd.Contains("socialOwner = null;") && reservationEnd.Contains("socialPartner = null;") &&
      reservationEnd.Contains("Mover.Suspended = OwnsDialogue();") &&
      reservationEnd.Contains("other.Mover.Suspended = other.OwnsDialogue();"),
    "dialogue, quest travel, participant removal and failed approaches release both social reservations and suspensions");
string visitHeading = Between(lifeSrc, "private void StartVisit(", "private bool CanVisitTarget(");
string meetHeading = Between(lifeSrc, "private bool TryStartMeet()", "private void UpdateSocialPause()");
string turnToward = Between(locoSrc, "public float TurnToward(", "public Quaternion FaceRotation(");
Check(visitHeading.Contains("socialOwnerFacing = Quaternion.LookRotation(towardTarget)") &&
      meetHeading.Contains("socialOwnerFacing = Quaternion.LookRotation(ownerToPartner)") &&
      !visitHeading.Contains(".FaceRotation(") && !meetHeading.Contains(".FaceRotation(") &&
      turnToward.Contains("Quaternion target = FaceRotation(direction);"),
    "social approach headings apply the avatar FacingOffset once in the mover, without double-applying FaceRotation");
string beginConversation = Between(lifeSrc, "private void BeginConversation()", "private void UpdateConversation()");
string endConversation = Between(lifeSrc, "private void EndConversation()", "// ACTIVITIES");
Check(beginConversation.Contains("CancelSocialPause(this);") &&
      endConversation.Contains("resumeAt = Time.time + UnityEngine.Random.Range(0.8f, 1.4f);") &&
      Index(frame, "if (Time.time < resumeAt) return;") < Index(frame, "if (Mover.Suspended) Mover.Suspended = false;") &&
      frame.Contains("if (Mover.Suspended) Mover.Suspended = false;"),
    "ending player dialogue releases a social pause and clears the mover suspension after the short farewell beat");
Check(meetSetup.Contains("Time.time < resumeAt") && meetSetup.Contains("Time.time < partner.resumeAt") &&
      meetSetup.Contains("Mover.Suspended || partner.Mover.Suspended"),
    "neither walker joins a meet during the post-dialogue resume beat or while either mover is suspended");
Check(locoSrc.Contains("driver.AnimatedWalkSpeed") && Src("FSM", "NPC", "NpcAnimationDriver.cs").Contains("animator.GetCurrentAnimatorClipInfo(animatorLayer, clipInfos);") &&
      locoSrc.Contains("animator.applyRootMotion = false;") && Between(locoSrc, "private void Update()", "private void Finish(").Contains("if (angle > pivotAngle)"),
    "walking matches the clip's own ground speed (no sliding feet), ignores root motion and pivots before sharp turns");
string consult = Src("SideQuest1", "ClientConsultation.cs");
Check(Between(consult, "private void ArriveInRoom(", "private void PlaceInRoom(").Contains("inRoom = true;") &&
      !Between(consult, "private void ArriveInRoom(", "private void PlaceInRoom(").Contains("LevelGate") &&
      Src("SideQuest1", "TutorialNpcController.cs").Contains("life.TravelForQuest(new[] { feet }, Quaternion.LookRotation(Flat(facing)), null);"),
    "arrival is presence only: Marie in the room enables visits, Arthur at the board changes nothing (the board still opens from his talk)");
string opening = Src("SideQuest1", "ArthurOpening.cs");
Check(Between(opening, "private void OnPickupLanded()", "// READING").Contains("if (!practice && guide != null) guide.WalkToCorkboard();") &&
      Between(opening, "private IEnumerator Start()", "private void LateUpdate()").Contains("guide.PlaceAtCorkboard();"),
    "Arthur walks to the corkboard after his hand-over; a resumed run finds him there");
Check(Src("FSM", "NPC", "NpcIdlePacing.cs").Contains("if (GetComponent<NpcLife>() != null)"),
    "the old pacing component steps aside where NpcLife owns the NPC (one owner for movement and facing)");
Check(armSrc.Contains("Instantiate(source)") && !Code(armSrc).Contains("sharedMesh.boneWeights =") &&
      Read("Assets", "3D Models", "npc_receptionist_static", "npc_receptionist_accessories.fbx.meta").Contains("isReadable: 1") &&
      Read("Assets", "3D Models", "npc_auditor_static", "npc_auditor_static.fbx.meta").Contains("isReadable: 1"),
    "the arm rig skins a runtime copy (the model asset is never modified); both T-pose models allow reading their mesh");

Console.WriteLine();
Console.WriteLine(failures == 0 ? $"{checks}/{checks} checks passed" : $"{failures} of {checks} checks FAILED");
return failures == 0 ? 0 : 1;
