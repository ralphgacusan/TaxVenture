// Level guide (tutorial) regression: trigger/target vocabulary and the
// shipped Level_01 guide.json content. Run from this folder:
//   dotnet run --no-restore
// (build the scratch Assembly-CSharp first; see GuideRegression.csproj).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

int fails = 0, checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Res(string rel) => Path.Combine(root, "Assets", "Resources", rel);

// ================= Trigger parser =================
Check(GuideTriggers.SplitAlternatives("value:placed|value:rejected").Count == 2, "split two events");
Check(GuideTriggers.SplitAlternatives("step:area:A/B|C/D").Count == 1, "area paths stay together");
Check(GuideTriggers.SplitAlternatives("step:area:A/B|C/D|mode:Workstation").Count == 2, "path then new trigger");
Check(GuideTriggers.SplitAlternatives("").Count == 0, "empty");

Check(GuideTriggers.TryParse("value:placed#3", out var t1, out _) && t1.Kind == GuideTriggers.Kind.Event && t1.Count == 3 && t1.Key == "value:placed", "count suffix");
Check(!GuideTriggers.TryParse("value:placed#0", out _, out _), "count must be >= 1");
Check(GuideTriggers.TryParse("event:ProfileCollected", out var t2, out _) && t2.Key == "event:ProfileCollected", "objective event");
Check(!GuideTriggers.TryParse("event:Nope", out _, out _), "unknown objective event rejected");
Check(GuideTriggers.TryParse("state:CaseCompleteState", out var t3, out _) && t3.Kind == GuideTriggers.Kind.Event, "state event");
Check(!GuideTriggers.TryParse("state:NotAState", out _, out _), "unknown state rejected");
Check(GuideTriggers.TryParse("mode:Workstation", out var t4, out _) && t4.Kind == GuideTriggers.Kind.Condition && t4.Arg == "Workstation", "mode");
Check(!GuideTriggers.TryParse("mode:Flying", out _, out _), "unknown mode rejected");
Check(GuideTriggers.TryParse("near:4.5:type:TaxpayerNpcInteractable", out var t5, out _) && Math.Abs(t5.Number - 4.5f) < 1e-4 && t5.Arg == "type:TaxpayerNpcInteractable", "near");
Check(!GuideTriggers.TryParse("near:type:Foo", out _, out _), "near needs metres");
Check(!GuideTriggers.TryParse("near:3:bogus", out _, out _), "near needs a valid target");
Check(GuideTriggers.TryParse("step:event:ReceptionistTalkedTo", out var t6, out _) && t6.Arg == "event:ReceptionistTalkedTo", "step");
Check(GuideTriggers.TryParse("move:4", out var t7, out _) && t7.Kind == GuideTriggers.Kind.Move && t7.Number == 4f, "move");
Check(GuideTriggers.TryParse("look:90", out var t8, out _) && t8.Kind == GuideTriggers.Kind.Look, "look");
Check(GuideTriggers.TryParse("seconds:5", out var t9, out _) && t9.Kind == GuideTriggers.Kind.Seconds, "seconds");
Check(!GuideTriggers.TryParse("move:abc", out _, out _), "move needs a number");
Check(GuideTriggers.TryParse("dialogue:active", out var t10, out _) && t10.Kind == GuideTriggers.Kind.Condition, "dialogue:active");
Check(GuideTriggers.TryParse("open:notes", out var t13, out _) && t13.Kind == GuideTriggers.Kind.Condition && t13.Arg == "notes", "open:notes");
Check(!GuideTriggers.TryParse("open:attic", out _, out _), "unknown window rejected");
Check(GuideTriggers.SplitAlternatives("mode:FirstPerson|open:profiles").Count == 2, "open: starts an alternative");
Check(GuideTriggers.TryParse("not:open:notes", out var t14, out _) && t14.Negate && t14.Key == GuideTriggers.OpenPrefix, "not:open:notes");
Check(!GuideTriggers.TryParse("not:value:placed", out _, out _), "not: only on conditions");
Check(GuideTriggers.TryParse("active:world:Environment/A/B", out var t15, out _) && t15.Kind == GuideTriggers.Kind.Condition && t15.Arg == "world:Environment/A/B", "active:");
Check(!GuideTriggers.TryParse("active:nowhere", out _, out _), "active: needs a valid target");
Check(GuideTriggers.SplitAlternatives("event:CaseFolderOpened|active:world:Environment/A/B").Count == 2, "event|active split");
Check(GuideTriggers.TryParse("case:filled", out var t11, out _) && t11.Kind == GuideTriggers.Kind.Condition, "case:filled");
Check(GuideTriggers.TryParse("lesson:welcome", out var t12, out _) && t12.Key == "lesson:welcome", "lesson event");
Check(GuideTriggers.Validate("move:3", allowWaitOnly: false) != null, "move not allowed in when");
Check(GuideTriggers.Validate("move:3", allowWaitOnly: true) == null, "move allowed in waitFor");
Check(GuideTriggers.Validate("value:placed|value:rejected", allowWaitOnly: true) == null, "alternatives valid");
Check(GuideTriggers.Validate("value:placed|bogus", allowWaitOnly: true) != null, "one bad alternative fails");

// ================= Targets =================
foreach (string ok in new[] { "chip", "field", "hud:minimap", "hud:joystick", "world:Environment/X", "type:CorkboardSlot", "ui:Foo", "screen:0.5,0.5", "screen:0.5,0.5,0.2,0.3", "waypoint" })
    Check(GuideTargets.Validate(ok) == null, "target valid: " + ok);
foreach (string bad in new[] { "", "hud:nothing", "world:", "screen:0.5", "screen:a,b", "Environment/X" })
    Check(GuideTargets.Validate(bad) != null, "target invalid: " + bad);

// ================= guide.json content =================
var guide = JsonDocument.Parse(File.ReadAllText(Res("Tutorials/Level_01/guide.json"))).RootElement;
var objectives = JsonDocument.Parse(File.ReadAllText(Res("Levels/Level_01/objectives.json"))).RootElement;
var stepTriggers = new HashSet<string>();
foreach (var o in objectives.GetProperty("objectives").EnumerateArray())
    foreach (var s in o.GetProperty("steps").EnumerateArray())
        stepTriggers.Add(ObjectiveTriggers.Normalize(s.GetProperty("trigger").GetString()));

string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

// Portraits exist.
string folder = Str(guide.GetProperty("guide"), "portraitFolder");
foreach (var p in guide.GetProperty("guide").GetProperty("portraits").EnumerateArray())
{
    string file = Str(p, "file");
    Check(File.Exists(Res(folder + "/" + file + ".png")) && File.Exists(Res(folder + "/" + file + ".png.meta")), "portrait exists: " + file);
}
Check(guide.GetProperty("guide").GetProperty("portraits").EnumerateArray().Any(p => Str(p, "mood") == "default"), "has a default portrait");

// Waypoints point at real objective steps; every step has a waypoint.
var waypointSteps = new HashSet<string>();
foreach (var w in guide.GetProperty("waypoints").EnumerateArray())
{
    string step = ObjectiveTriggers.Normalize(Str(w, "step"));
    waypointSteps.Add(step);
    Check(stepTriggers.Contains(step), "waypoint step exists in objectives.json: " + step);
    Check(!string.IsNullOrWhiteSpace(Str(w, "target")), "waypoint has a target: " + step);
}
foreach (string step in stepTriggers)
    Check(waypointSteps.Contains(step), "objective step has arrows: " + step);

// Lessons.
var ids = new HashSet<string>();
var allIds = guide.GetProperty("lessons").EnumerateArray().Select(l => Str(l, "id")).ToList();
var typeNames = new HashSet<string>();
foreach (var lesson in guide.GetProperty("lessons").EnumerateArray())
{
    string id = Str(lesson, "id");
    Check(!string.IsNullOrEmpty(id) && ids.Add(id), "lesson id unique: " + id);
    Check(GuideTriggers.Validate(Str(lesson, "when"), false) == null, $"{id}: when valid ({GuideTriggers.Validate(Str(lesson, "when"), false)})");
    string onlyIn = Str(lesson, "onlyIn");
    if (onlyIn != null) Check(GuideTriggers.Validate(onlyIn, false) == null, $"{id}: onlyIn valid");
    string after = Str(lesson, "after");
    if (after != null) Check(allIds.Contains(after), $"{id}: after exists");
    foreach (string trig in GuideTriggers.SplitAlternatives(Str(lesson, "when") + (onlyIn != null ? "|" + onlyIn : "")))
        if (trig.StartsWith("lesson:")) Check(allIds.Contains(trig.Substring(7)), $"{id}: {trig} exists");

    int pageNo = 0;
    foreach (var page in lesson.GetProperty("pages").EnumerateArray())
    {
        pageNo++;
        string where = $"{id} p{pageNo}";
        bool card = Str(page, "show") == "welcome";
        bool hasText = !string.IsNullOrWhiteSpace(Str(page, "text")) || (!string.IsNullOrWhiteSpace(Str(page, "textTouch")) && !string.IsNullOrWhiteSpace(Str(page, "textDesktop")));
        Check(card || hasText, where + ": has text for both platforms");
        string wait = Str(page, "waitFor");
        if (wait != null) Check(GuideTriggers.Validate(wait, true) == null, $"{where}: waitFor valid ({GuideTriggers.Validate(wait, true)})");
        foreach (string key in new[] { "point", "to" })
        {
            string target = Str(page, key);
            if (target == null) continue;
            Check(GuideTargets.Validate(target) == null, $"{where}: {key} valid");
            if (target.StartsWith("type:")) typeNames.Add(target.Substring(5));
        }
        string gesture = Str(page, "gesture");
        if (gesture != null) Check(GuideTriggers.Gestures.Contains(gesture), $"{where}: gesture valid");
        if (gesture == "drag") Check(Str(page, "to") != null, $"{where}: drag has a 'to'");
        string mood = Str(page, "mood");
        if (mood != null) Check(guide.GetProperty("guide").GetProperty("portraits").EnumerateArray().Any(p => Str(p, "mood") == mood), $"{where}: mood {mood} has a portrait");
        string style = Str(page, "style");
        if (style != null) Check(style == "hint" || style == "box", $"{where}: style valid");
        string box = Str(page, "box");
        if (box != null) Check(new[] { "auto", "top", "bottom", "center" }.Contains(box), $"{where}: box valid");
    }
}
foreach (var w in guide.GetProperty("waypoints").EnumerateArray())
    foreach (string token in Str(w, "target").Split('|'))
        if (token.StartsWith("type:")) typeNames.Add(token.Substring(5));

// Every type: names a real component in the game assembly.
var assembly = typeof(GuideDirector).Assembly;
foreach (string name in typeNames)
{
    Type type = assembly.GetType(name, false);
    Check(type != null && typeof(UnityEngine.Component).IsAssignableFrom(type), "component type exists: " + name);
}

// Mobile game: touch wording, START button.
Check(Str(guide, "platform") == "touch", "platform is touch");
Check(Str(guide.GetProperty("welcome"), "button") == "START", "welcome button says START");
foreach (var lesson in guide.GetProperty("lessons").EnumerateArray())
    foreach (var page in lesson.GetProperty("pages").EnumerateArray())
    {
        string tx = (Str(page, "text") ?? "").ToLowerInvariant();
        Check(!tx.Contains("mouse") && !tx.Contains("w a s d") && !tx.Contains("click"), "no desktop wording: " + Str(lesson, "id"));
    }
Check(allIds.Contains("notes"), "Notes lesson exists");
Check(allIds.Contains("submit"), "Auditor hand-in lesson exists");
foreach (string id in new[] { "book-pages", "investigate", "sidequest-card", "audit-card", "level-complete" })
    Check(allIds.Contains(id), "lesson exists: " + id);
foreach (var lesson in guide.GetProperty("lessons").EnumerateArray())
{
    string when = Str(lesson, "when") ?? "";
    if (when.StartsWith("card:"))
        Check(lesson.TryGetProperty("duringResults", out var dr) && dr.GetBoolean() && lesson.TryGetProperty("endIfGone", out var eg) && eg.GetBoolean(),
            Str(lesson, "id") + ": card lessons need duringResults and endIfGone");
    if (when.StartsWith("card:"))
        Check(lesson.GetProperty("pages").GetArrayLength() == 1 && !lesson.GetProperty("pages")[0].TryGetProperty("waitFor", out _),
            Str(lesson, "id") + ": a result-card lesson is one tap-to-continue page");
}
Check(GuideTriggers.TryParse("card:level", out var tc, out _) && tc.Kind == GuideTriggers.Kind.Condition, "card:level");
Check(!GuideTriggers.TryParse("card:shop", out _, out _), "unknown card rejected");
Check(GuideTargets.Validate("hud:card") == null && GuideTargets.Validate("hud:continue") == null, "hud:card / hud:continue");
Check(guide.GetProperty("trail").GetProperty("waitForGuide").GetBoolean(), "arrows wait for the guide");
var revealLesson = guide.GetProperty("lessons").EnumerateArray().FirstOrDefault(l => l.GetProperty("pages").EnumerateArray().Any(p => p.TryGetProperty("revealArrows", out var r) && r.GetBoolean()));
Check(revealLesson.ValueKind == JsonValueKind.Object && Str(revealLesson, "id") == "follow", "arrows are revealed by the follow lesson");
foreach (var lesson in guide.GetProperty("lessons").EnumerateArray())
    foreach (var page in lesson.GetProperty("pages").EnumerateArray())
    {
        // Value steps must use the smart targets (never Name/TIN rows).
        if (Str(page, "point") == "type:CaseFolderFieldRow" || Str(page, "to") == "type:CaseFolderFieldRow")
            Check(false, Str(lesson, "id") + ": use field, not type:CaseFolderFieldRow");
    }
Check(!File.ReadAllText(Res("Tutorials/Level_01/guide.json")).Contains("value:placed#3"), "store-folder waits for all fields really filled");

// ================= town.json (Phase 1 teaching content) =================
var town = JsonDocument.Parse(File.ReadAllText(Res("Levels/Level_01/town.json"))).RootElement;
string[] labels = { "resident", "citizen", "alien", "classification" };
bool Labelled(string t) { string l = (t ?? "").ToLowerInvariant(); return labels.Any(w => l.Contains(w)); }
var townIds = new HashSet<string>();
foreach (var tp in town.GetProperty("taxpayers").EnumerateArray())
{
    string npcId = Str(tp, "npcId");
    townIds.Add(npcId);
    var builtInTalk = TownNpcDialogueLibrary.GetDialogueFor(npcId, "Npc", out bool known);
    Check(known, "town.json npcId is a real plaza client: " + npcId);
    var talk = tp.GetProperty("talk").EnumerateArray().ToList();
    Check(talk.Count == builtInTalk.Count, npcId + ": town.json talk matches the built-in copy (line count)");
    for (int i = 0; i < talk.Count && i < builtInTalk.Count; i++)
    {
        string text = Str(talk[i], "text");
        bool you = Str(talk[i], "who") == "you";
        Check(text == builtInTalk[i].Text && you == (builtInTalk[i].Speaker == DialogueSpeaker.Player), $"{npcId}: talk line {i + 1} matches the built-in copy");
        Check(!Labelled(text), $"{npcId}: talk never names a classification");
    }
    var profile = TaxpayerProfileDatabase.GetByNpcId(npcId);
    string desc = Str(tp, "description");
    Check(desc == profile.Description, npcId + ": description matches the built-in copy");
    Check(!Labelled(desc) && desc.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 14, npcId + ": description is short and label-free");
    var reaction = CorkboardTeaching.ReactionById(profile.ProfileId);
    foreach (string key in new[] { "placedRight", "hintCitizenship", "hintResidence", "hintBoth" })
    {
        string line = Str(tp, key);
        Check(!string.IsNullOrWhiteSpace(line) && !Labelled(line), $"{npcId}: {key} present and label-free");
    }
    Check(Str(tp, "placedRight") == reaction.PlacedRight && Str(tp, "hintCitizenship") == reaction.HintCitizenship &&
          Str(tp, "hintResidence") == reaction.HintResidence && Str(tp, "hintBoth") == reaction.HintBoth, npcId + ": reactions match the built-in copy");
}
Check(townIds.SetEquals(new[] { "npc_marie", "npc_jhunjhun", "npc_milo", "npc_zorbix" }), "town.json covers all four plaza clients");
var intro = town.GetProperty("arthurBoardIntro").EnumerateArray().ToList();
Check(intro.Count == CorkboardTeaching.IntroLines.Count, "Arthur's intro matches the built-in copy");
for (int i = 0; i < intro.Count; i++)
    Check(Str(intro[i], "text") == CorkboardTeaching.IntroLines[i].Value && !Labelled(Str(intro[i], "text")), $"Arthur intro line {i + 1} matches and stays indirect");
Check(!File.ReadAllText(Res("Tutorials/Level_01/guide.json")).Contains("corkboard-wrong"), "the generic wrong-corner hint lesson is gone (Arthur reacts instead)");

// ================= Office Hours (Phase 2) =================
Check(OfficeHoursMath.ParseTime("7:30 AM", -1) == 7 * 3600 + 30 * 60, "parse 7:30 AM");
Check(OfficeHoursMath.ParseTime("12:05 AM", -1) == 5 * 60, "parse 12:05 AM");
Check(OfficeHoursMath.ParseTime("1:15 PM", -1) == 13 * 3600 + 15 * 60, "parse 1:15 PM");
Check(OfficeHoursMath.ParseTime("13:15", -1) == 13 * 3600 + 15 * 60, "parse 24h");
Check(OfficeHoursMath.ParseTime("7:75", -1) == -1 && OfficeHoursMath.ParseTime("soon", -1) == -1, "bad times fall back");
Check(OfficeHoursMath.Format(7 * 3600 + 30 * 60) == "7:30 AM" && OfficeHoursMath.Format(12 * 3600) == "12:00 PM" && OfficeHoursMath.Format(0) == "12:00 AM", "format 12-hour clock");
Check(OfficeHoursMath.Format(9 * 3600 + 5 * 60) == "9:05 AM", "format pads minutes");
// Allowance adapts to the side quest, within limits.
float typical = OfficeHoursMath.AllowanceSeconds(360, 300, 360, 0.4f, 270, 480);
float quick = OfficeHoursMath.AllowanceSeconds(150, 300, 360, 0.4f, 270, 480);
float slow = OfficeHoursMath.AllowanceSeconds(700, 300, 360, 0.4f, 270, 480);
float verySlow = OfficeHoursMath.AllowanceSeconds(5000, 300, 360, 0.4f, 270, 480);
Check(Math.Abs(typical - 300) < 0.01f, "typical player gets the base time");
Check(quick < typical && quick >= 270, "a quick player gets a tighter but fair deadline");
Check(slow > typical && verySlow == 480, "a slow player gets more room, never endless");
Check(OfficeHoursMath.AllowanceSeconds(0, 300, 360, 0.4f, 270, 480) == 270, "never below the minimum");
// Deadline: now + allowance x speed, rounded up to 5 minutes.
int now = OfficeHoursMath.ParseTime("8:31 AM", 0);
int dl = OfficeHoursMath.Deadline(now, 300, 10);
Check(dl % 300 == 0 && dl >= now + 3000 && dl < now + 3000 + 300, "deadline rounds up to the next 5 minutes");
Check(OfficeHoursMath.MinutesLeft(dl - 61, dl) == 2 && OfficeHoursMath.MinutesLeft(dl + 5, dl) == 0, "minutes left");
Check(Math.Abs(OfficeHoursMath.RealSecondsLeft(dl - 600, dl, 10) - 60f) < 0.01f, "real seconds left");
Check(OfficeHoursMath.CrossedWarning(16, 15, 15) && !OfficeHoursMath.CrossedWarning(15, 14, 15) && !OfficeHoursMath.CrossedWarning(1, 0, 0), "warning fires once when crossing");

var oh = JsonDocument.Parse(File.ReadAllText(Res("Levels/Level_01/office_hours.json"))).RootElement;
Check(oh.GetProperty("enabled").GetBoolean(), "office hours enabled");
Check(OfficeHoursMath.ParseTime(Str(oh, "startTime"), -1) >= 0 && OfficeHoursMath.ParseTime(Str(oh, "nextMorningTime"), -1) >= 0, "office_hours.json times are readable");
Check(oh.GetProperty("minSeconds").GetSingle() <= oh.GetProperty("maxSeconds").GetSingle(), "allowance limits in order");
Check(oh.GetProperty("receptionistLines").EnumerateArray().Any(l => (l.GetString() ?? "").Contains("{deadline}")), "the receptionist says the leaving time");
Check((Str(oh, "arthurWarning") ?? "").Contains("{minutes}") && (Str(oh, "arthurNextMorning") ?? "").Contains("{deadline}"), "Arthur's lines use their tokens");
Check(allIds.Contains("office-hours"), "Office Hours lesson exists");

// ================= Consequences (Phase 3) =================
var caseFile = JsonDocument.Parse(File.ReadAllText(Res("Levels/Level_01/cases/case_001.json"))).RootElement;
var answerKey = caseFile.GetProperty("answerKey");
string[] answers = { Str(answerKey, "residencyStatus"), Str(answerKey, "taxpayerType"), Str(answerKey, "incomeSource") };
bool GivesAway(string text)
{
    string flat = (text ?? "").Replace(" ", "").ToLowerInvariant();
    return answers.Any(a => !string.IsNullOrEmpty(a) && flat.Contains(a.ToLowerInvariant()));
}
var worry = caseFile.GetProperty("clientAfterFailedAudit").EnumerateArray().ToList();
Check(worry.Count >= 3 && worry.Any(l => Str(l, "speaker") == "Npc") && worry.Any(l => Str(l, "speaker") == "Player"), "Marie's after-audit talk is a real two-sided talk");
foreach (var l in worry) Check(!GivesAway(Str(l, "text")) && !Labelled(Str(l, "text")), "Marie's after-audit talk gives clues, not answers");
foreach (string key in new[] { "residencyStatus", "taxpayerType", "incomeSource" })
{
    string costLine = Str(caseFile.GetProperty("mistakeCosts"), key);
    Check(!string.IsNullOrWhiteSpace(costLine) && !GivesAway(costLine), "real-world cost for " + key + " exists and never gives the answer");
}
Check(!string.IsNullOrWhiteSpace(Str(caseFile, "clientFailedBubble")) && !string.IsNullOrWhiteSpace(Str(caseFile, "clientApprovedBubble")), "Marie reacts to both outcomes");
Check(caseFile.GetProperty("auditorRetryLines").GetArrayLength() >= 2, "the Auditor gets stricter (at least two retry lines)");
Check(!string.IsNullOrWhiteSpace(Str(caseFile, "auditorSameAnswersLine")), "the Auditor refuses identical answers");
Check(new MistakeCostDefinition { residencyStatus = "a", taxpayerType = "b", incomeSource = "c" }.For("taxpayerType") == "b", "cost lookup by field key");
Check(new ComplianceIssue("x", "incomeSource").FieldKey == "incomeSource", "issues carry their field");
Check(CorkboardSideQuestManager.MissesBeforeLock == 2 && CorkboardSideQuestManager.LockSeconds > 0f, "corkboard locks a card after two misses");

// ================= Late penalty + case finale =================
Check(CaseFinaleRules.TierFor(0, true, 1) == CaseFinaleRules.Tier.Great, "first try, on time = great");
Check(CaseFinaleRules.TierFor(1, true, 1) == CaseFinaleRules.Tier.Good, "one failed audit = good");
Check(CaseFinaleRules.TierFor(0, false, 2) == CaseFinaleRules.Tier.Good, "one day late = good");
Check(CaseFinaleRules.TierFor(1, false, 2) == CaseFinaleRules.Tier.Okay, "a failed audit and late = okay");
Check(CaseFinaleRules.TierFor(3, true, 1) == CaseFinaleRules.Tier.Okay, "many failed audits = okay");
Check(CaseFinaleRules.TierFor(0, false, 3) == CaseFinaleRules.Tier.Okay, "two days late = okay");
var ledger = new LevelScoreLedger();
ledger.Reset(new LevelRewardRules(), 1);
ledger.AddConversation("npc_a", "Talk", null);
ledger.AddCase("Case", 0, 0);
int expBefore = ledger.TotalExp, heartsBefore = ledger.TotalHearts;
var late = ledger.AddPenalty("Late case", 15, 1);
Check(late != null && ledger.TotalExp == expBefore - 15 && ledger.TotalHearts == heartsBefore - 1 && late.Exp == -15, "late penalty is taken off");
ledger.AddPenalty("Huge", 100000, 100000);
Check(ledger.TotalExp == 0 && ledger.TotalHearts == 0, "a penalty never takes totals below zero");
Check(ledger.AddPenalty("Nothing left", 5, 1) == null, "nothing to take = no entry");
Check(oh.GetProperty("lateExpPerDay").GetInt32() > 0 && oh.GetProperty("lateMaxDays").GetInt32() >= 1, "office_hours.json late penalty set");
var finale = caseFile.GetProperty("clientFinale");
foreach (string tier in new[] { "great", "good", "okay" })
{
    var lines = finale.GetProperty(tier).EnumerateArray().Select(l => l.GetString()).ToList();
    Check(lines.Count >= 1 && lines.All(l => !string.IsNullOrWhiteSpace(l) && !GivesAway(l)), "Marie has " + tier + " lines");
}
float left = finale.GetProperty("screenLeft").GetSingle();
Check(left > 0.05f && left < 0.5f, "Marie stands on the left half");

// ================= Splash + release settings =================
var splash = town.GetProperty("sideQuestSplash");
Check(!string.IsNullOrWhiteSpace(Str(splash, "title")) && !string.IsNullOrWhiteSpace(Str(splash, "kicker")) &&
      !string.IsNullOrWhiteSpace(Str(splash, "description")) && !string.IsNullOrWhiteSpace(Str(splash, "instruction")), "Side Quest 1 title card has its text");
Check((Str(splash, "description") ?? "").Split(' ').Length <= 30, "the title card description stays short");
Check(!Labelled(Str(splash, "description")) && !Labelled(Str(splash, "instruction")), "the title card never names a classification");
Check(oh.GetProperty("baseSeconds").GetSingle() == 300 && oh.GetProperty("minSeconds").GetSingle() == 270 &&
      oh.GetProperty("maxSeconds").GetSingle() == 480 && Math.Abs(oh.GetProperty("factor").GetSingle() - 0.4f) < 0.001f,
      "office_hours.json is back on the release timings (300 / 0.4 / 270 / 480)");

// The progressive order: the first lesson starts the level, the tour follows it.
var lessonList = guide.GetProperty("lessons").EnumerateArray().ToList();
Check(Str(lessonList[0], "when") == "start" && Str(lessonList[0].GetProperty("pages")[0], "show") == "welcome", "level opens with the welcome card");

Console.WriteLine(fails == 0 ? $"GuideRegression: {checks} checks PASS" : $"GuideRegression: {fails} of {checks} FAILED");
return fails == 0 ? 0 : 1;
