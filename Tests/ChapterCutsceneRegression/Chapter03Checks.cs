using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class Chapter03Checks
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
        const string dir = "Assets/Cutscenes/Chapter03/";
        string Read(string p) => File.ReadAllText(Path.Combine(root, p)).Replace("\r", "");
        string Guid(string p) => Regex.Match(Read(p + ".meta"), @"guid: ([a-f0-9]{32})").Groups[1].Value;
        string Shared(string p) => Guid("Assets/Scripts/Cutscenes/Chapters/" + p + ".cs");
        string playerGuid = Shared("ChapterCinematicPlayer"), actorGuid = Shared("ChapterCinematicActor"), shotGuid = Shared("ChapterCinematicShot"), moveGuid = Shared("ChapterCinematicPropMove"), soundGuid = Shared("ChapterCinematicSound");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var reference = Parse(Read("Assets/Scenes/Cutscenes/CS_Chapter01_End.unity"));
        var set = new HashSet<string>();
        void Retain(string id)
        {
            if (id == "0" || !reference.ContainsKey(id) || !set.Add(id)) return;
            foreach (Match m in Regex.Matches(reference[id].body, @"\{fileID: (-?\d+)\}")) Retain(m.Groups[1].Value);
        }
        foreach (var go in reference.Where(d => d.Value.cls == 1 && new[] { "Set (Office_Level1 copy)", "Exterior (Office_Level1 copy)" }.Contains(Field(d.Value.body, "m_Name")))) Retain(go.Key);
        var guids = new HashSet<string>();
        foreach (string folder in new[] { "Assets", "Library/PackageCache" })
            if (Directory.Exists(Path.Combine(root, folder)))
                foreach (string p in Directory.EnumerateFiles(Path.Combine(root, folder), "*.meta", SearchOption.AllDirectories))
                {
                    var match = Regex.Match(File.ReadAllText(p), @"^guid: ([a-f0-9]{32})", RegexOptions.Multiline);
                    if (match.Success) guids.Add(match.Groups[1].Value);
                }
        void Validate(string path)
        {
            string source = Read(path); var docs = Parse(source);
            check(Regex.Matches(source, @"^--- !u!", RegexOptions.Multiline).Count == docs.Count, path + " unique file IDs");
            check(docs.Values.All(d => Regex.Matches(d.body, @"\{fileID: (-?\d+)\}").All(m => m.Groups[1].Value == "0" || docs.ContainsKey(m.Groups[1].Value))), path + " every local reference resolves");
            check(Regex.Matches(source, @"guid: ([a-f0-9]{32})").All(m => m.Groups[1].Value.StartsWith("0000000000000000") || guids.Contains(m.Groups[1].Value)), path + " all external GUIDs exist");
            check(docs.Where(d => d.Value.cls == 4 || d.Value.cls == 224).All(t => Field(t.Value.body, "m_Father") == "" || Ref(Field(t.Value.body, "m_Father")) == "0" || List(docs[Ref(Field(t.Value.body, "m_Father"))].body, "m_Children").Contains(t.Key)), path + " every authored transform is listed by its parent");
        }
        foreach (bool ending in new[] { false, true })
        {
            string kind = ending ? "end" : "start", name = "CS_Chapter03_" + (ending ? "End" : "Start"), label = "T03 " + kind;
            var s = JsonSerializer.Deserialize<ChapterCinematicScript>(Read(dir + "chapter03_" + kind + "_script.json"), options);
            var problems = s.Validate();
            check(problems.Count == 0, label + " authoring rules: " + string.Join("; ", problems));
            check(s.id == "chapter03." + kind && s.scene == name && s.title == (ending ? "Plans Beyond the Paper" : "The File Returns"), label + " exact stable ID/title/scene");
            check(s.duration == 15 && s.fadeInSeconds == .25f && s.fadeOutSeconds == .25f && s.cueFadeSeconds == .1f && s.shotStarts.SequenceEqual(new[] { 0f, 4.8f, 10.1f }), label + " exactly 15.000 seconds including fades and three shots");
            string[] expected = ending ? new[] { "Now I can plan for that studio.", "The mayor wants supported findings, not guesses.", "Nina sent two versions. She'll explain next time." } : new[] { "Bayview's income records arrived, with the original entries.", "Check whether the summary agrees." };
            check(s.cues.Select(c => c.text).SequenceEqual(expected) && s.cues.Select(c => c.speaker).SequenceEqual(ending ? new[] { "Bea", "Receptionist", "Receptionist" } : new[] { "Receptionist", "Arthur" }), label + " approved lines and clear attribution");
            check(s.cues.Select(c => c.start).SequenceEqual(ending ? new[] { .6f, 5.2f, 10.5f } : new[] { 5.2f, 10.5f }) && s.cues.Select(c => c.end).SequenceEqual(ending ? new[] { 4.6f, 9.8f, 14.5f } : new[] { 9.8f, 14.5f }), label + " exact subtitle windows");
            check(s.cues.All(c => c.variant == "") && new[] { "", "rosa.responsible_completed", "rosa.promise_resolved", "rosa.dismissive_completed", "unknown" }.All(v => s.cues.All(c => s.CueAt(c.start + .2, v) == c)), label + " one shared presentation for corrected work and all history contexts");
            var natural = new ChapterCinematicSession(s); int count = 0;
            for (int i = 0; i < 1200; i++) { natural.Advance(1d / 60); if (natural.TakeFinished(out var r)) { count++; check(r.Id == s.id && r.PresentationTime == 15 && r.Outcome == ChapterCinematicOutcome.Completed && r.Watched, label + " natural completion at 15.000 with handled continuation"); } }
            check(count == 1 && natural.CurtainAlpha == 1 && !natural.RequestSkip(), label + " once-only result and black hold");
            foreach (double t in new[] { .3, 7, 14.9 })
            {
                var skip = new ChapterCinematicSession(s); while (skip.Time < t) skip.Advance(1d / 60);
                check(skip.RequestSkip() && !skip.RequestSkip(), label + " first-view Skip once at " + t);
                while (!skip.Done) skip.Advance(1d / 60);
                check(skip.TakeFinished(out var r) && r.Id == s.id && r.Outcome == ChapterCinematicOutcome.Skipped && r.Watched && skip.CurtainAlpha == 1 && !skip.TakeFinished(out _), label + " Skip handled once, holds black");
            }
            var abort = new ChapterCinematicSession(s); abort.Advance(.05); abort.Abort();
            check(abort.TakeFinished(out var aborted) && aborted.Id == s.id && aborted.Outcome == ChapterCinematicOutcome.Aborted && !aborted.Watched && !abort.TakeFinished(out _), label + " abort never fabricates handled/watched continuation");
            var paused = new ChapterCinematicSession(s); paused.Advance(.05); paused.Paused = true; paused.Advance(3600);
            check(paused.Time == .05, label + " focus/suspend clock remains paused");
            paused.Paused = false; paused.Advance(3600);
            check(Math.Abs(paused.Time - .05 - ChapterCinematicSession.MaxStep) < 1e-8, label + " resume cannot jump a subtitle");
            var teardown = new ChapterCinematicSession(s); teardown.Advance(.05); teardown.Teardown();
            check(teardown.TakeFinished(out var torn) && torn.Outcome == ChapterCinematicOutcome.Aborted && !torn.Watched, label + " teardown aborts safely");

            string path = "Assets/Scenes/Cutscenes/" + name + ".unity"; Validate(path); var docs = Parse(Read(path));
            check(set.Count == 1997 && set.All(id => docs.ContainsKey(id) && docs[id] == reference[id]), label + " actual T01 office/lighting/exterior copied verbatim");
            var allowed = new HashSet<string> { playerGuid, actorGuid, shotGuid, moveGuid, soundGuid, "a79441f348de89743a2939f4d699eac1", "474bcb49853aa07438625e644c072ee6" };
            check(docs.Values.Where(d => d.cls == 114).All(d => allowed.Contains(Regex.Match(Field(d.body, "m_Script"), @"guid: ([a-f0-9]{32})").Groups[1].Value)), label + " only existing cinematic/URP scripts, no gameplay owner");
            var players = docs.Values.Where(d => d.cls == 114 && d.body.Contains(playerGuid)).ToArray(); string player = players.Single().body;
            check(players.Length == 1 && Field(player, "scriptAsset").Contains(Guid(dir + "chapter03_" + kind + "_script.json")), label + " correct shared player/script binding");
            string cam = Ref(Field(player, "cinematicCamera"));
            check(docs[cam].cls == 20 && docs.Values.Count(d => d.cls == 20) == 1 && docs.Values.Count(d => d.cls == 81) == 1, label + " one bound camera and listener");
            foreach (string key in new[] { "subtitleFontSize", "subtitleMaxWidth", "subtitleBottomMargin", "subtitlePadding", "subtitlePanelAlpha", "androidBackSkips" })
                check(Field(player, key) == Field(reference.Values.Single(d => d.cls == 114 && d.body.Contains(playerGuid)).body, key), label + " unchanged T01 overlay/input setting " + key);
            var shots = List(player, "shots").Select(id => docs[id].body).ToArray();
            check(shots.Length == 3 && shots.Select(b => Number(b, "startTime")).SequenceEqual(s.shotStarts) && shots.Select(b => Number(b, "endTime")).SequenceEqual(new[] { 4.8f, 10.1f, 15f }), label + " complete three-shot sequence");
            check(shots.All(b => b.Contains(shotGuid) && Ref(Field(b, "targetCamera")) == cam && docs[Ref(Field(b, "from"))].cls == 4 && docs[Ref(Field(b, "to"))].cls == 4 && Number(b, "fieldOfView") >= 34 && Number(b, "fieldOfView") <= 50 && Number(b, "moveStart") >= Number(b, "startTime") && Number(b, "moveEnd") <= Number(b, "endTime")), label + " valid camera bindings, moderate steady lenses and bounded movements");
            check(Number(shots[2], "moveEnd") <= 12.8f, label + " final camera settles into a clear hold before fade");
            var elements = List(player, "elements").Select(id => docs[id].body).ToArray(); var actors = elements.Where(b => b.Contains(actorGuid)).ToArray();
            check(actors.Length == 2 && elements.Count(b => b.Contains(moveGuid)) == 1 && elements.Count(b => b.Contains(soundGuid)) == 2, label + " complete cast, one physical settling event, room tone and paper cue");
            check(actors.All(b => docs[Ref(Field(b, "model"))].cls == 4 && (Ref(Field(b, "animator")) == "0" ? Field(b, "sampledClip").Contains("4d8c6cb1568c845489cd64e97a4ff7e6") : docs[Ref(Field(b, "animator"))].cls == 95)), label + " real model/rig/idle bindings");
            var elijah = actors.Single(b => Field(b, "sampledClip").Contains("4d8c6cb1568c845489cd64e97a4ff7e6"));
            check(Read(path).Contains("value: 0.236135") && Ref(Field(elijah, "animator")) == "0", label + " established Elijah model scale and generic idle setup");
            check(actors.Any(b => Field(b, "stateName") == (ending ? "Idle Relaxed" : "Briefcase Idle")), label + " current Bea / established Arthur idle");
            if (ending)
            {
                string bea = Regex.Match(Field(Parse(Read("Assets/Resources/Levels/Level_03/scene_assets.asset")).Single().Value.body, "beaPrefab"), @"guid: ([a-f0-9]{32})").Groups[1].Value;
                check(Read(path).Contains(bea) && actors.Count(b => Ref(Field(b, "heldProp")) != "0") == 1, label + " live Bea identity and already-delivered visual copy");
                check(InFrame(docs, shots[0], InstancePosition(docs, "Bea logo draft and pencil") + new Vector3(0, .004f, 0), .30f, .212f, .22f), label + " sketch stays above subtitle reserve in medium shot and 4:3 crop");
                check(InFrame(docs, shots[1], InstancePosition(docs, "Already-read mayor message") + new Vector3(0, .0035f, -.025f), .42f, .13f, .22f), label + " message text stays in frame above captions");
                check(InFrame(docs, shots[2], InstancePosition(docs, "Nina future appointment") + new Vector3(0, .0125f, -.003f), .40f, .13f, .24f), label + " Nina label stays above captions including 4:3");
            }
            else
            {
                check(Read(path).Contains("e6276f65987675b40b38ace97ad579b8"), label + " established rigged Arthur identity");
                check(InFrame(docs, shots[0], InstancePosition(docs, "New income records packet") + new Vector3(0, .0085f, 0), .252f, .09f, .05f), label + " incoming headings fit opening insert including 4:3");
                check(InFrame(docs, shots[0], InstancePosition(docs, "Bayview Folder") + new Vector3(0, .0278f, .028f), .30f, .13f, .05f), label + " familiar Bayview identity visible beside new packet");
            }
            check(!Regex.IsMatch(Read(path), @"propertyPath: m_ApplyRootMotion\n\s*value: 1"), label + " no root motion or competing gameplay actor driver");
            var move = elements.Single(b => b.Contains(moveGuid));
            check(Coordinate(Field(docs[Ref(Field(move, "from"))].body, "m_LocalPosition"), "y") == Coordinate(Field(docs[Ref(Field(move, "to"))].body, "m_LocalPosition"), "y"), label + " prop moves only along its support surface");
            var sounds = elements.Where(b => b.Contains(soundGuid)).ToArray();
            check(sounds.Any(b => Field(b, "clip").Contains("8b6bf1cc7df71cc4bbb9f462f5e1df28") && Field(b, "loop") == "1" && Number(b, "end") == 15) && sounds.Any(b => Field(b, "clip").Contains("c6e31739e87f89c44bac99ca23c86a8d") && Field(b, "loop") == "0" && Number(b, "start") >= Number(move, "start") && Number(b, "start") <= Number(move, "end")), label + " established room tone and one restrained paper cue at settling");
            check(Read("ProjectSettings/EditorBuildSettings.asset").Split(new[] { "path: Assets/Scenes/Cutscenes/" + name + ".unity" }, StringSplitOptions.None).Length == 2 && !Read("Assets/Resources/Career/chapters.json").Contains(name), label + " opening/ending registered once; career definition stays gameplay");
        }
        foreach (string name in new[] { "IncomeRecordsPacket", "ReadMayorMessage", "NinaAppointmentPacket", "BeaLogoSketch" })
        {
            string path = dir + name + ".prefab"; Validate(path); var docs = Parse(Read(path));
            check(docs.Values.Where(d => d.cls == 114).All(d => d.body.Contains("9541d86e2fd84c1d9990edf0852d74ab")), name + " visual-only prefab, no gameplay component");
            foreach (var rect in docs.Values.Where(d => d.cls == 224))
            {
                var p = Position(rect.body); string anchor = Field(rect.body, "m_AnchoredPosition");
                check(Math.Abs(Coordinate(anchor, "x") - p.X) < .00001 && Math.Abs(Coordinate(anchor, "y") - p.Y) < .00001 && p.Y > 0 && p.Y < .02f, name + " correct surface-height anchor (T02 text bug guarded)");
            }
        }
        string nina = Read(dir + "NinaAppointmentPacket.prefab");
        check(nina.Contains("Nina Cruz") && nina.Contains("Marine Services") && nina.Contains("Draft A") && nina.Contains("Draft B") && nina.Contains(Guid("Assets/Cutscenes/Shared/Materials/M_BayviewFolder_BlueGray.mat")) && nina.Contains(Guid("Assets/Cutscenes/Shared/Materials/M_BayviewFolder_Label.mat")), "T03 future appointment has named envelope and two distinct draft tabs");
        check(!Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs").Any(), "T03 needs no new runtime scripts, save owner, context branch or framework");
        var newMetas = Directory.EnumerateFiles(Path.Combine(root, dir), "*.meta").Concat(new[] { Path.Combine(root, dir.TrimEnd('/') + ".meta"), Path.Combine(root, "Assets/Scenes/Cutscenes/CS_Chapter03_Start.unity.meta"), Path.Combine(root, "Assets/Scenes/Cutscenes/CS_Chapter03_End.unity.meta") }).ToArray();
        check(newMetas.All(File.Exists) && newMetas.Select(p => Regex.Match(File.ReadAllText(p), @"guid: ([a-f0-9]{32})").Groups[1].Value).Distinct().Count() == newMetas.Length, "T03 all new metadata exists with distinct GUIDs");
    }
}
