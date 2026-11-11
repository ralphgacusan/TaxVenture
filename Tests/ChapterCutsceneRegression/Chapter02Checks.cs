using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class Chapter02Checks
{
    private static Dictionary<string, (int cls, string body)> Parse(string text) =>
        Regex.Matches(text.Replace("\r", ""), @"^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- |\z)", RegexOptions.Multiline | RegexOptions.Singleline)
            .ToDictionary(m => m.Groups[2].Value, m => (int.Parse(m.Groups[1].Value), m.Groups[3].Value));
    private static string Field(string body, string field) => Regex.Match(body, @"^  " + field + @": (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
    private static string Ref(string value) => Regex.Match(value, @"fileID: (-?\d+)").Groups[1].Value;
    private static float Number(string body, string field) => float.Parse(Field(body, field), CultureInfo.InvariantCulture);
    private static IEnumerable<string> List(string body, string field) => Regex.Matches(
        Regex.Match(body, @"^  " + field + @":\n((?:  - \{fileID: -?\d+\}\n)*)", RegexOptions.Multiline).Groups[1].Value,
        @"fileID: (-?\d+)").Select(m => m.Groups[1].Value);

    private static float Coordinate(string value, string axis) => float.Parse(Regex.Match(value, axis + @": ([-+\d.eE]+)").Groups[1].Value, CultureInfo.InvariantCulture);
    private static Vector3 Position(string body) => new Vector3(Coordinate(Field(body, "m_LocalPosition"), "x"), Coordinate(Field(body, "m_LocalPosition"), "y"), Coordinate(Field(body, "m_LocalPosition"), "z"));
    private static Vector3 InstancePosition(Dictionary<string, (int cls, string body)> docs, string name)
    {
        string body = docs.Values.Single(d => d.cls == 1001 && d.body.Contains("propertyPath: m_Name\n      value: " + name + "\n")).body;
        float Axis(string axis) => float.Parse(Regex.Match(body, @"propertyPath: m_LocalPosition\." + axis + @"\n\s*value: ([-+\d.eE]+)").Groups[1].Value, CultureInfo.InvariantCulture);
        return new Vector3(Axis("x"), Axis("y"), Axis("z"));
    }
    // Source-level layout guard, not rendered acceptance. All shot pose markers
    // and prop parents are identity-rooted in these two authored scenes.
    private static bool InFrame(Dictionary<string, (int cls, string body)> docs, string shot, Vector3 center, float width, float depth, float minY)
    {
        foreach (string endpoint in new[] { "from", "to" })
        {
            string pose = docs[Ref(Field(shot, endpoint))].body, rotation = Field(pose, "m_LocalRotation");
            var q = Quaternion.Normalize(new Quaternion(Coordinate(rotation, "x"), Coordinate(rotation, "y"), Coordinate(rotation, "z"), Coordinate(rotation, "w")));
            foreach (float x in new[] { -.5f, .5f }) foreach (float z in new[] { -.5f, .5f })
            {
                Vector3 local = Vector3.Transform(center + new Vector3(x * width, 0, z * depth) - Position(pose), Quaternion.Conjugate(q));
                float tan = (float)Math.Tan(Number(shot, "fieldOfView") * Math.PI / 360);
                float vy = .5f + local.Y / (2 * local.Z * tan);
                float vx = .5f + local.X / (2 * local.Z * tan * (4f / 3f));
                if (local.Z <= .05f || vy < minY || vy > .95f || vx < .03f || vx > .97f) return false;
            }
        }
        return true;
    }

    public static void Run(string root, Action<bool, string> check)
    {
        const string assets = "Assets/Cutscenes/Chapter02/";
        string Read(string p) => File.ReadAllText(Path.Combine(root, p)).Replace("\r", "");
        string Guid(string p) => Regex.Match(Read(p + ".meta"), @"guid: ([a-f0-9]{32})").Groups[1].Value;
        string Shared(string name) => Guid("Assets/Scripts/Cutscenes/Chapters/" + name + ".cs");
        string playerGuid = Shared("ChapterCinematicPlayer"), shotGuid = Shared("ChapterCinematicShot"), actorGuid = Shared("ChapterCinematicActor"), moveGuid = Shared("ChapterCinematicPropMove"), soundGuid = Shared("ChapterCinematicSound");
        string contextGuid = Guid(assets + "Chapter02CinematicContext.cs");
        var json = new JsonSerializerOptions { IncludeFields = true };
        var variants = new[] {
            (Chapter02CinematicContext.RosaPreview.Neutral, Chapter02CinematicContext.Neutral, "Thank you for explaining the corrected copy."),
            (Chapter02CinematicContext.RosaPreview.ResponsibleCompleted, Chapter02CinematicContext.ResponsibleCompleted, "You checked it with me. Thank you."),
            (Chapter02CinematicContext.RosaPreview.PromiseResolved, Chapter02CinematicContext.PromiseResolved, "Next time, please check before you promise."),
            (Chapter02CinematicContext.RosaPreview.DismissiveCompleted, Chapter02CinematicContext.DismissiveCompleted, "Thank you for finishing the review.")
        };
        var endScript = JsonSerializer.Deserialize<ChapterCinematicScript>(Read(assets + "chapter02_end_script.json"), json);
        foreach (var v in variants)
        {
            var isolated = Chapter02CinematicContext.Resolve(null, v.Item1);
            var supplied = Chapter02CinematicContext.Resolve(new ChapterCinematicContext(v.Item2), Chapter02CinematicContext.RosaPreview.Neutral);
            check(isolated.Variant == v.Item2 && supplied.Variant == v.Item2, "T02 preview and supplied context resolve " + v.Item2);
            check(endScript.CueAt(1, supplied.Variant)?.text == v.Item3 && endScript.cues.Count(c => c.AppliesTo(v.Item2) && c.start < 4.8f) == 1, "T02 exactly one correct Rosa line for " + v.Item2);
            check(endScript.CueAt(6, supplied.Variant) == null && endScript.CueAt(11, supplied.Variant)?.text == "Bea's next. She's bringing her receipts.", "T02 variants preserve silent footer and common appointment cue");
        }
        foreach (string missing in new[] { "", "unknown", "responsible", null })
            check(Chapter02CinematicContext.Resolve(new ChapterCinematicContext(missing), Chapter02CinematicContext.RosaPreview.ResponsibleCompleted).Variant == Chapter02CinematicContext.Neutral,
                "T02 missing/legacy supplied context stays neutral, overriding Inspector preview");
        check(Chapter02CinematicContext.Resolve(null, (Chapter02CinematicContext.RosaPreview)999).Variant == Chapter02CinematicContext.Neutral, "T02 unknown Inspector enum safely selects neutral");
        // Exercise the real Awake adapter too, including explicit-context precedence.
        var adapter = new Chapter02CinematicContext();
        var awake = typeof(Chapter02CinematicContext).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ChapterCinematicPlayer.PendingContext = new ChapterCinematicContext(Chapter02CinematicContext.PromiseResolved);
        awake.Invoke(adapter, null);
        check(ChapterCinematicPlayer.PendingContext.Variant == Chapter02CinematicContext.PromiseResolved, "T02 Awake preserves supplied contract context");
        ChapterCinematicPlayer.PendingContext = null;
        awake.Invoke(adapter, null);
        check(ChapterCinematicPlayer.PendingContext.Variant == Chapter02CinematicContext.Neutral, "T02 fresh standalone Awake is neutral");
        ChapterCinematicPlayer.PendingContext = null;

        var reference = Parse(Read("Assets/Scenes/Cutscenes/CS_Chapter01_End.unity"));
        // Set ownership starts at T01's actual roots; include prefab-added components.
        var retained = new HashSet<string>();
        void Retain(string id)
        {
            if (id == "0" || !reference.ContainsKey(id) || !retained.Add(id)) return;
            foreach (Match m in Regex.Matches(reference[id].body, @"\{fileID: (-?\d+)\}")) Retain(m.Groups[1].Value);
        }
        foreach (var go in reference.Where(d => d.Value.cls == 1 && (Field(d.Value.body, "m_Name") == "Set (Office_Level1 copy)" || Field(d.Value.body, "m_Name") == "Exterior (Office_Level1 copy)"))) Retain(go.Key);
        var guids = new HashSet<string>();
        foreach (string directory in new[] { "Assets", "Library/PackageCache" })
            if (Directory.Exists(Path.Combine(root, directory)))
                foreach (string meta in Directory.EnumerateFiles(Path.Combine(root, directory), "*.meta", SearchOption.AllDirectories))
                {
                    var g = Regex.Match(File.ReadAllText(meta), @"^guid: ([a-f0-9]{32})", RegexOptions.Multiline);
                    if (g.Success) guids.Add(g.Groups[1].Value);
                }
        void ValidateReferences(Dictionary<string, (int cls, string body)> docs, string label)
        {
            var missing = docs.Values.SelectMany(d => Regex.Matches(d.body, @"\{fileID: (-?\d+)\}").Select(m => m.Groups[1].Value)).Where(id => id != "0" && !docs.ContainsKey(id)).Distinct().ToArray();
            check(missing.Length == 0, label + " every local reference resolves: " + string.Join(",", missing));
            var absent = docs.Values.SelectMany(d => Regex.Matches(d.body, @"guid: ([a-f0-9]{32})").Select(m => m.Groups[1].Value)).Where(g => !g.StartsWith("0000000000000000") && !guids.Contains(g)).Distinct().ToArray();
            check(absent.Length == 0, label + " all asset/package GUIDs exist: " + string.Join(",", absent));
            bool hierarchy = true;
            foreach (var t in docs.Where(d => d.Value.cls == 4 || d.Value.cls == 224))
            {
                if (Field(t.Value.body, "m_Father") == "") continue; // imported stripped transform
                string father = Ref(Field(t.Value.body, "m_Father"));
                if (father != "0" && (!docs.ContainsKey(father) || !List(docs[father].body, "m_Children").Contains(t.Key))) hierarchy = false;
                foreach (string child in List(t.Value.body, "m_Children"))
                    if (docs.ContainsKey(child) && Field(docs[child].body, "m_Father") != "" && Ref(Field(docs[child].body, "m_Father")) != t.Key) hierarchy = false;
            }
            check(hierarchy, label + " parent and child links agree");
        }
        foreach (bool ending in new[] { false, true })
        {
            string kind = ending ? "end" : "start", name = ending ? "CS_Chapter02_End" : "CS_Chapter02_Start", label = "T02 " + kind;
            var script = JsonSerializer.Deserialize<ChapterCinematicScript>(Read(assets + "chapter02_" + kind + "_script.json"), json);
            var errors = script.Validate();
            check(errors.Count == 0, label + " all script authoring rules: " + string.Join("; ", errors));
            check(script.id == "chapter02." + kind && script.scene == name && script.title == (ending ? "A Familiar Name" : "An Ordinary File"), label + " exact ID, scene, title");
            check(script.duration == 15 && script.fadeInSeconds == .25f && script.fadeOutSeconds == .25f && script.shotStarts.SequenceEqual(new[] { 0f, 4.8f, 10.1f }), label + " exact 15.000 authored seconds, fades and three intervals");
            if (!ending) check(script.CueAt(1, "") == null && script.CueAt(6, "")?.text == "Bayview's first records. More will follow." && script.CueAt(11, "")?.text == "Start with the company details.", label + " opening dialogue and silent establishing beat");
            foreach (string variant in ending ? variants.Select(v => v.Item2) : new[] { "" })
            {
                var session = new ChapterCinematicSession(script);
                int results = 0;
                for (int frame = 0; frame < 1200; frame++) { session.Advance(1d / 60d); if (session.TakeFinished(out var result)) { results++; check(result.Id == script.id && result.PresentationTime == 15 && result.Outcome == ChapterCinematicOutcome.Completed, label + " natural completion " + variant); } }
                check(results == 1, label + " one natural result " + variant);
                foreach (double time in new[] { .3, 7, 14.9 })
                {
                    var skip = new ChapterCinematicSession(script);
                    while (skip.Time < time) skip.Advance(1d / 60d);
                    check(skip.RequestSkip() && !skip.RequestSkip(), label + " first/middle/late Skip once at " + time);
                    while (!skip.Done) skip.Advance(1d / 60d);
                    check(skip.TakeFinished(out var r) && r.Id == script.id && r.Outcome == ChapterCinematicOutcome.Skipped && r.Watched && !skip.TakeFinished(out _), label + " Skip is handled once, black hold contract");
                }
            }
            string path = "Assets/Scenes/Cutscenes/" + name + ".unity", source = Read(path);
            var docs = Parse(source);
            check(Regex.Matches(source, @"^--- !u!", RegexOptions.Multiline).Count == docs.Count, label + " unique scene file IDs");
            ValidateReferences(docs, label);
            check(retained.Count > 1900 && retained.All(id => docs.ContainsKey(id) && docs[id] == reference[id]), label + " complete T01 office, lighting and exterior copied verbatim (" + retained.Count + " objects)");
            var allowed = new HashSet<string> { playerGuid, shotGuid, actorGuid, moveGuid, soundGuid, contextGuid, "a79441f348de89743a2939f4d699eac1", "474bcb49853aa07438625e644c072ee6" };
            check(docs.Values.Where(d => d.cls == 114).All(d => allowed.Contains(Regex.Match(Field(d.body, "m_Script"), @"guid: ([a-f0-9]{32})").Groups[1].Value)), label + " scene carries only cinematic/URP scripts");
            var players = docs.Values.Where(d => d.cls == 114 && d.body.Contains(playerGuid)).ToArray();
            check(players.Length == 1 && Field(players[0].body, "scriptAsset").Contains(Guid(assets + "chapter02_" + kind + "_script.json")), label + " one shared player bound to correct script");
            string player = players.Single().body, cam = Ref(Field(player, "cinematicCamera"));
            check(docs[cam].cls == 20 && docs.Values.Count(d => d.cls == 20) == 1 && docs.Values.Count(d => d.cls == 81) == 1, label + " one bound camera/listener");
            foreach (string key in new[] { "subtitleFontSize", "subtitleMaxWidth", "subtitleBottomMargin", "subtitlePadding", "subtitlePanelAlpha", "androidBackSkips" })
                check(Field(player, key) == Field(reference.Values.Single(d => d.cls == 114 && d.body.Contains(playerGuid)).body, key), label + " same T01 presentation setting " + key);
            var shots = List(player, "shots").Select(id => docs[id].body).ToArray();
            check(shots.Length == 3 && shots.Select(b => Number(b, "startTime")).SequenceEqual(script.shotStarts), label + " three bound shots match script");
            check(shots.All(b => b.Contains(shotGuid) && Ref(Field(b, "targetCamera")) == cam && docs[Ref(Field(b, "from"))].cls == 4 && docs[Ref(Field(b, "to"))].cls == 4 && Number(b, "fieldOfView") >= 34 && Number(b, "fieldOfView") <= 50 && Number(b, "moveStart") >= Number(b, "startTime") && Number(b, "moveEnd") <= Number(b, "endTime")), label + " all camera poses, lenses and movement windows valid");
            check(Number(shots[2], "moveEnd") <= 12, label + " insert settles before three-second reading hold");
            if (!ending)
            {
                Vector3 folder = InstancePosition(docs, "Bayview Folder") + new Vector3(0, .0278f, .028f);
                Vector3 envelope = InstancePosition(docs, "First records - closed envelope") + new Vector3(0, .006f, 0);
                check(InFrame(docs, shots[0], folder, .30f, .13f, .22f) && InFrame(docs, shots[1], folder, .30f, .13f, .22f), label + " Bayview label stays above caption reserve in both reception shots, including 4:3");
                check(InFrame(docs, shots[0], envelope, .28f, .12f, .05f) && InFrame(docs, shots[1], envelope, .28f, .12f, .22f), label + " settled envelope visible in establishing and closer shot");
            }
            else
            {
                check(InFrame(docs, shots[1], InstancePosition(docs, "Reviewed paper - CoastCare footer") + new Vector3(0, .003f, -.035f), .44f, .14f, .22f), label + " CoastCare footer fits insert and 4:3 crop");
                check(InFrame(docs, shots[2], InstancePosition(docs, "Bea appointment") + new Vector3(0, .003f, -.025f), .44f, .14f, .24f), label + " appointment text clears captions, including 4:3");
                check(InFrame(docs, shots[2], InstancePosition(docs, "Closed receipt envelope") + new Vector3(0, .006f, 0), .28f, .12f, .24f), label + " entire receipt envelope clears conservative caption reserve, including 4:3");
            }
            var elements = List(player, "elements").Select(id => docs[id].body).ToArray();
            var actors = elements.Where(b => b.Contains(actorGuid)).ToArray();
            check(actors.Length == 2 && elements.Count(b => b.Contains(moveGuid)) == 1 && elements.Count(b => b.Contains(soundGuid)) == 2, label + " complete two-actor cast, settling prop and two sound bindings");
            check(actors.All(b => docs[Ref(Field(b, "model"))].cls == 4 && (Ref(Field(b, "animator")) == "0" ? Field(b, "sampledClip").Contains("4d8c6cb1568c845489cd64e97a4ff7e6") : docs[Ref(Field(b, "animator"))].cls == 95)), label + " actors bind correct models, Animator/Elijah clip");
            check(source.Contains("guid: " + (ending ? "d39f95956aeb5ea40bc6d4060080e829" : "3c5b1db0b16674f40a46b5819998a15f")), label + " correct current Rosa / distinct Marco asset");
            check(!Regex.IsMatch(source, @"propertyPath: m_ApplyRootMotion\n\s*value: 1"), label + " root motion disabled");
            var move = elements.Single(b => b.Contains(moveGuid));
            var from = docs[Ref(Field(move, "from"))].body; var to = docs[Ref(Field(move, "to"))].body;
            check(Regex.Match(Field(from, "m_LocalPosition"), @"y: ([\d.-]+)").Value == Regex.Match(Field(to, "m_LocalPosition"), @"y: ([\d.-]+)").Value, label + " prop settles at constant surface height");
            var sounds = elements.Where(b => b.Contains(soundGuid)).ToArray();
            check(sounds.Any(b => Field(b, "clip").Contains("8b6bf1cc7df71cc4bbb9f462f5e1df28") && Field(b, "loop") == "1") && sounds.Any(b => Field(b, "clip").Contains("c6e31739e87f89c44bac99ca23c86a8d") && Field(b, "loop") == "0" && Number(b, "start") >= Number(move, "start") && Number(b, "start") <= Number(move, "end")), label + " original room tone and one restrained placement cue");
            check(docs.Values.Count(d => d.cls == 114 && d.body.Contains(contextGuid)) == (ending ? 1 : 0), label + " context adapter only on Rosa ending");
            check(Read("ProjectSettings/EditorBuildSettings.asset").Split(new[] { "path: Assets/Scenes/Cutscenes/" + name + ".unity" }, StringSplitOptions.None).Length == 2 && !Read("Assets/Resources/Career/chapters.json").Contains(name), label + " opening/ending registered once; career definition stays gameplay");
            if (ending) check(actors.Count(b => Ref(Field(b, "heldProp")) != "0") == 1, label + " Rosa already holds her delivered copy");
        }
        foreach (string name in new[] { "DocumentEnvelope", "RosaReviewedPaper", "BeaAppointmentNote" }) ValidateReferences(Parse(Read(assets + name + ".prefab")), "T02 " + name);
        check(Read(assets + "RosaReviewedPaper.prefab").Contains("Prepared with\\n<b>CoastCare Tax Assistance</b>") && Read(assets + "BeaAppointmentNote.prefab").Contains("<b>Beatriz Mendoza</b>\\n— Freelance Artist"), "T02 exact printed service identity and appointment, readable line breaks");
        // A tilted world-space label still anchors in its parent's XY plane.
        // Using page-depth Z for anchored Y moves headings into the air and
        // buries negative-Z footers below the sheet when Unity rebuilds layout.
        foreach (string name in new[] { "RosaReviewedPaper", "BeaAppointmentNote" })
        {
            var print = Parse(Read(assets + name + ".prefab"));
            foreach (var rect in print.Values.Where(d => d.cls == 224))
            {
                Vector3 position = Position(rect.body);
                string anchor = Field(rect.body, "m_AnchoredPosition");
                string textName = Field(print[Ref(Field(rect.body, "m_GameObject"))].body, "m_Name");
                check(Math.Abs(Coordinate(anchor, "x") - position.X) < .00001f && Math.Abs(Coordinate(anchor, "y") - position.Y) < .00001f
                    && position.Y > .002f && position.Y < .006f,
                    "T02 " + name + " / " + textName + " anchors text just above paper in parent XY, not page depth");
            }
        }
        string sourceContext = Read(assets + "Chapter02CinematicContext.cs");
        check(sourceContext.Contains("[DefaultExecutionOrder(-1000)]"), "T02 context prepared before shared player Awake");
        foreach (string forbidden in new[] { "PlayerProgress", "PlayerPrefs", "Resources.Load", "SceneManager", "ApplyLevelResult", "CommitFollowThrough", "ClientCopyHandover", "trust", "Trust" })
            check(!sourceContext.Contains(forbidden), "T02 context never accesses " + forbidden);
    }
}
