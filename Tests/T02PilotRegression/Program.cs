// Milestone T02 visual/contact pilot regression (Level1To3AdventureMilestone).
// Runs the real import-time silhouette algorithm (Assets/Editor/Pilot/T02PilotSilhouette.cs,
// compiled into this suite) on geometry parsed from the actual source FBX files, the pure
// timeline/reach/frame-statistics rules, and source checks for isolation and cleanup.
// Unity import, rendering, Animator/IK, contact and device behaviour are NOT exercised here.
// Inputs: Temp/T02Pilot/source_meshes.json (exported read-only from the FBX sources).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

int fails = 0, checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
}
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Src(string rel) => File.ReadAllText(Path.Combine(root, rel)).Replace("\r\n", "\n");
string Code(string rel) => System.Text.RegularExpressions.Regex.Replace(Src(rel), @"///.*|//.*", "");

// =====================================================================
// 1. Silhouette treatment on the real sources
// =====================================================================
string meshesPath = Path.Combine(root, "Temp", "T02Pilot", "source_meshes.json");
Check(File.Exists(meshesPath), "source geometry export exists (Temp/T02Pilot/source_meshes.json)");
var sources = JsonSerializer.Deserialize<Dictionary<string, List<MeshJson>>>(File.ReadAllText(meshesPath), new JsonSerializerOptions { IncludeFields = true });

(T02PilotSilhouette.MeshData data, int openEdges) Imported(MeshJson m, float smoothAngle, bool fileFlatNormals)
{
    int cps = m.points.Length / 3;
    Vector3 P(int i) => new Vector3((float)m.points[i * 3], (float)m.points[i * 3 + 1], (float)m.points[i * 3 + 2]);
    var faceNormal = new Vector3[m.polygons.Length];
    var faceArea = new float[m.polygons.Length];
    var around = new List<int>[cps];
    for (int i = 0; i < cps; i++) around[i] = new List<int>();
    for (int f = 0; f < m.polygons.Length; f++)
    {
        int[] poly = m.polygons[f];
        Vector3 n = Vector3.zero;
        for (int k = 0; k < poly.Length; k++) { Vector3 a = P(poly[k]), b = P(poly[(k + 1) % poly.Length]); n += new Vector3((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)); }
        faceArea[f] = n.magnitude * 0.5f;
        faceNormal[f] = n.magnitude > 1e-12f ? n / n.magnitude : Vector3.up;
        foreach (int cp in poly) around[cp].Add(f);
    }
    var positions = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var weights = new List<BoneWeight>();
    var unique = new Dictionary<(int, int, int, int, int, int), int>();
    var tris = new List<int>();
    bool skinned = m.weights != null && m.weights.Count > 0;
    BoneWeight Weight(int cp)
    {
        if (!skinned || !m.weights.TryGetValue(cp.ToString(), out double[][] list)) return new BoneWeight { boneIndex0 = 0, weight0 = 1f };
        var top = list.OrderByDescending(x => x[1]).Take(4).ToList();
        double total = top.Sum(x => x[1]);
        var w = new BoneWeight();
        for (int i = 0; i < top.Count; i++)
        {
            int bone = (int)top[i][0]; float v = (float)(top[i][1] / total);
            if (i == 0) { w.boneIndex0 = bone; w.weight0 = v; } else if (i == 1) { w.boneIndex1 = bone; w.weight1 = v; }
            else if (i == 2) { w.boneIndex2 = bone; w.weight2 = v; } else { w.boneIndex3 = bone; w.weight3 = v; }
        }
        return w;
    }
    int corner = 0;
    float cos = Mathf.Cos(smoothAngle * Mathf.Deg2Rad);
    for (int f = 0; f < m.polygons.Length; f++)
    {
        int[] poly = m.polygons[f];
        var ids = new int[poly.Length];
        for (int k = 0; k < poly.Length; k++, corner++)
        {
            int cp = poly[k];
            Vector3 n;
            if (fileFlatNormals) n = faceNormal[f];
            else
            {
                n = Vector3.zero;
                foreach (int g in around[cp]) if (Vector3.Dot(faceNormal[g], faceNormal[f]) >= cos) n += faceNormal[g] * faceArea[g];
                n = n.magnitude > 1e-12f ? n.normalized : faceNormal[f];
            }
            Vector2 uv = m.uvs != null && corner < m.uvs.Length ? new Vector2((float)m.uvs[corner][0], (float)m.uvs[corner][1]) : Vector2.zero;
            var key = (cp, (int)Math.Round(uv.x * 1e5), (int)Math.Round(uv.y * 1e5), (int)Math.Round(n.x * 1e4), (int)Math.Round(n.y * 1e4), (int)Math.Round(n.z * 1e4));
            if (!unique.TryGetValue(key, out int id))
            {
                id = positions.Count; unique[key] = id;
                positions.Add(P(cp)); normals.Add(n); uvs.Add(uv); weights.Add(Weight(cp));
            }
            ids[k] = id;
        }
        for (int k = 1; k + 1 < ids.Length; k++) { tris.Add(ids[0]); tris.Add(ids[k]); tris.Add(ids[k + 1]); }
    }
    var data = new T02PilotSilhouette.MeshData { Positions = positions.ToArray(), Normals = normals.ToArray(), Weights = skinned ? weights.ToArray() : null };
    data.Uvs.Add(uvs.ToArray());
    data.Submeshes.Add(tris.ToArray());
    return (data, OpenEdges(data));
}

int NonPlanarDiagonals(MeshJson m)
{
    Vector3 P(int i) => new Vector3((float)m.points[i * 3], (float)m.points[i * 3 + 1], (float)m.points[i * 3 + 2]);
    int n = 0;
    foreach (int[] poly in m.polygons)
        for (int k = 1; k + 2 < poly.Length; k++)
        {
            Vector3 a = P(poly[0]), b = P(poly[k]), c = P(poly[k + 1]), d = P(poly[k + 2]);
            if (Vector3.Angle(Vector3.Cross(b - a, c - a), Vector3.Cross(c - a, d - a)) >= T02PilotSilhouette.Settings.Default.MinDihedral) n++;
        }
    return n;
}

int OpenEdges(T02PilotSilhouette.MeshData d)
{
    var weld = new Dictionary<(long, long, long), int>();
    int Id(Vector3 p) { var k = ((long)Math.Round(p.x / 1e-5), (long)Math.Round(p.y / 1e-5), (long)Math.Round(p.z / 1e-5)); if (!weld.TryGetValue(k, out int v)) { v = weld.Count; weld[k] = v; } return v; }
    var count = new Dictionary<(int, int), int>();
    foreach (int[] t in d.Submeshes)
        for (int i = 0; i + 2 < t.Length; i += 3)
            for (int c = 0; c < 3; c++)
            {
                int a = Id(d.Positions[t[i + c]]), b = Id(d.Positions[t[i + (c + 1) % 3]]);
                if (a == b) continue;
                var e = a < b ? (a, b) : (b, a);
                count[e] = count.TryGetValue(e, out int n) ? n + 1 : 1;
            }
    return count.Values.Count(n => n != 2);
}

var sheet = new List<string>();
foreach (var (label, angle) in new[] { ("arthur", 50f), ("washer", 40f), ("folder", 40f), ("elijah_exploration", 70f), ("elijah_conversation", 70f) })
{
    int before = 0, after = 0, vBefore = 0, vAfter = 0, split = 0, creases = 0, flat = 0, steep = 0;
    bool invariants = true, deterministic = true, uvInside = true, weightsOk = true, flatInputNoSplit = true, finite = true;
    foreach (MeshJson mesh in sources[label])
    {
        var (data, open) = Imported(mesh, angle, false);
        var refined = T02PilotSilhouette.Refine(data, T02PilotSilhouette.Settings.Default, out var report);
        var again = T02PilotSilhouette.Refine(data, T02PilotSilhouette.Settings.Default, out _);
        deterministic &= refined.Positions.SequenceEqual(again.Positions) && refined.Submeshes[0].SequenceEqual(again.Submeshes[0]);
        invariants &= OpenEdges(refined) == open && report.TrianglesAfter == report.TrianglesBefore + 2 * report.EdgesSplit &&
                      refined.Submeshes[0].All(i => i >= 0 && i < refined.Positions.Length) && refined.Normals.Length == refined.Positions.Length;
        finite &= refined.Positions.All(p => !float.IsNaN(p.x + p.y + p.z) && !float.IsInfinity(p.x + p.y + p.z));
        Vector2 min = new Vector2(data.Uvs[0].Min(u => u.x), data.Uvs[0].Min(u => u.y)), max = new Vector2(data.Uvs[0].Max(u => u.x), data.Uvs[0].Max(u => u.y));
        uvInside &= refined.Uvs[0].All(u => u.x >= min.x - 1e-4f && u.y >= min.y - 1e-4f && u.x <= max.x + 1e-4f && u.y <= max.y + 1e-4f);
        if (refined.Weights != null)
            weightsOk &= refined.Weights.All(w => Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1f) < 1e-3f);
        // Flat file normals: every edge between two source polygons is a crease. Only a non-planar quad's own
        // internal diagonal can still qualify (its two triangles share the polygon's normal).
        var (flatData, _) = Imported(mesh, angle, true);
        T02PilotSilhouette.Refine(flatData, T02PilotSilhouette.Settings.Default, out var flatReport);
        flatInputNoSplit &= flatReport.EdgesSplit <= NonPlanarDiagonals(mesh);
        before += report.TrianglesBefore; after += report.TrianglesAfter; vBefore += report.VerticesBefore; vAfter += report.VerticesAfter;
        split += report.EdgesSplit; creases += report.CreaseEdges; flat += report.FlatEdges; steep += report.SteepEdges;
    }
    sheet.Add($"{label}: offline import approximation {vBefore} v / {before} tris -> silhouette {vAfter} v / {after} tris (+{(after - before) * 100.0 / Math.Max(1, before):0}%); " +
              $"edges split {split}, kept crease {creases}, flat {flat}, steep {steep}");
    Check(invariants, $"T02 silhouette {label}: no new open edges or T-junction cracks; triangles grow exactly 2 per split edge; indices valid");
    Check(finite && deterministic, $"T02 silhouette {label}: finite positions; identical result on a second run");
    Check(uvInside, $"T02 silhouette {label}: interpolated UVs stay inside the source UV range (no new texture region)");
    Check(weightsOk, $"T02 silhouette {label}: blended skin weights stay normalised");
    Check(flatInputNoSplit, $"T02 silhouette {label}: on the file's own flat normals no edge between two source polygons is split (shading must come first)");
    if (label == "folder") Check(split <= 2 && after - before <= 4, "T02 folder: the handled folder's edges are intentional hard edges (at most a near-coplanar diagonal qualifies); no derived folder is made");
    if (label == "arthur") Check(split > 0 && creases > 0 && after < before * 4, "T02 Arthur: faceted soft edges split, hard edges kept, growth bounded (well under blanket 4x subdivision)");
    if (label == "washer") Check(split > 0 && creases > 0, "T02 washer: the coarse drum/curves are refined while the 90-degree box edges stay hard");
}
foreach (string line in sheet) Console.WriteLine("T02 sheet: " + line);

// Exact PN midpoint and weight blending properties.
Vector3 flatMid = T02PilotSilhouette.CurvedMidpoint(new Vector3(0, 0, 0), Vector3.up, new Vector3(2, 0, 0), Vector3.up);
Vector3 curvedMid = T02PilotSilhouette.CurvedMidpoint(new Vector3(-1, 0, 0), new Vector3(-1, 1, 0).normalized, new Vector3(1, 0, 0), new Vector3(1, 1, 0).normalized);
Check((flatMid - new Vector3(1, 0, 0)).magnitude < 1e-5f, "T02 silhouette: on a flat surface the split point is the straight midpoint");
Check(curvedMid.y > 0.05f && Mathf.Abs(curvedMid.x) < 1e-5f, "T02 silhouette: on a convex edge the split point bulges outward along the normals (rounder outline)");
var blended = T02PilotSilhouette.Blend(new BoneWeight { boneIndex0 = 3, weight0 = 1f }, new BoneWeight { boneIndex0 = 5, weight0 = 0.6f, boneIndex1 = 3, weight1 = 0.4f });
Check(blended.boneIndex0 == 3 && Mathf.Abs(blended.weight0 - 0.7f) < 1e-4f && blended.boneIndex1 == 5 && Mathf.Abs(blended.weight1 - 0.3f) < 1e-4f,
    "T02 silhouette: a split vertex blends both ends' influences by bone (top four, renormalised)");

// =====================================================================
// 2. Pure pilot rules: timeline, reach solve, frame statistics
// =====================================================================
List<T02PilotStep> plan = T02PilotPlan.Build();
var beats = plan.Select(s => s.Beat).ToList();
int Seq(params T02PilotBeat[] order)
{
    int at = 0, found = 0;
    foreach (T02PilotBeat b in order) { int i = beats.IndexOf(b, at); if (i < 0) return found; at = i + 1; found++; }
    return found;
}
var transfer = new[] { T02PilotBeat.Point, T02PilotBeat.Reach, T02PilotBeat.Approach, T02PilotBeat.Grip, T02PilotBeat.Lift, T02PilotBeat.Carry, T02PilotBeat.Lower, T02PilotBeat.Release, T02PilotBeat.Back, T02PilotBeat.Retract };
Check(beats[0] == T02PilotBeat.Idle && beats[1] == T02PilotBeat.WalkIn && beats[2] == T02PilotBeat.Settle && Seq(transfer) == transfer.Length &&
      beats.Count(b => b == T02PilotBeat.Grip) == 2 && beats.Count(b => b == T02PilotBeat.Release) == 2 && beats.Contains(T02PilotBeat.WalkOut),
    "T02 plan: walk in, stop, then point -> reach -> approach -> grip -> lift -> carry -> lower -> release -> back -> retract, twice (A to B, B to A), then walk out");
var transfers = plan.Where(s => s.Beat == T02PilotBeat.Carry).ToList();
Check(transfers.Count == 2 && transfers[0].From == 0 && transfers[0].To == 1 && transfers[1].From == 1 && transfers[1].To == 0,
    "T02 plan: the folder goes holder A -> B and back B -> A, so each loop ends where it began");
Check(T02PilotPlan.Next(plan.Count - 1, plan.Count) == T02PilotPlan.LoopStart && plan[T02PilotPlan.LoopStart].Beat == T02PilotBeat.WalkIn &&
      !beats.Any(b => b.ToString().Contains("Reset") || b.ToString().Contains("Teleport")),
    "T02 plan: the loop resumes by walking in again from where Arthur walked out (no reset or teleport beat)");
Check(T02PilotPlan.Holding(T02PilotBeat.Lift) && T02PilotPlan.Holding(T02PilotBeat.Carry) && T02PilotPlan.Holding(T02PilotBeat.Lower) &&
      !T02PilotPlan.Holding(T02PilotBeat.Approach) && !T02PilotPlan.Holding(T02PilotBeat.Back) && !T02PilotPlan.ArmActive(T02PilotBeat.WalkIn) &&
      !T02PilotPlan.ArmActive(T02PilotBeat.Idle) && T02PilotPlan.ArmActive(T02PilotBeat.Point) && T02PilotPlan.ArmActive(T02PilotBeat.Retract),
    "T02 plan: the folder rides the hand only between grip and release; walking and idle leave the arm to its clip");
Check(T02PilotPlan.Ease(0f) == 0f && T02PilotPlan.Ease(1f) == 1f && T02PilotPlan.Ease(2f) == 1f && T02PilotPlan.Ease(-1f) == 0f &&
      Enumerable.Range(0, 101).Select(i => T02PilotPlan.Ease(i / 100f)).Zip(Enumerable.Range(1, 100).Select(i => T02PilotPlan.Ease(i / 100f)), (a, b) => b >= a).All(x => x),
    "T02 plan: easing is monotonic 0..1 with no overshoot (hands never pass their target)");
Check(plan.Where(s => s.Beat != T02PilotBeat.WalkIn && s.Beat != T02PilotBeat.WalkOut).All(s => s.Seconds > 0f),
    "T02 plan: every timed beat has a positive duration");

Vector3 gA = new Vector3(0.22f, 1.40f, 0.95f), gB = new Vector3(-0.22f, 1.40f, 0.95f);
float depth = T02PilotReach.StationDepth(gA, gB, -0.18f, 0.18f, 1.39f, 0.02f, 0.61f, 0.88f, 0.694f, 0.16f, 0.08f, out float ratio);
Check(ratio <= 0.88f + 1e-3f && depth <= 0.694f - 0.08f - 0.16f + 1e-4f,
    $"T02 reach: the station keeps both grips within 88% of the arm and the body clear of the washer front (depth {depth:0.000}, ratio {ratio:0.00})");
T02PilotReach.StationDepth(new Vector3(0, 1.4f, 3f), new Vector3(0, 1.4f, 3f), 0, 0.18f, 1.39f, 0.02f, 0.61f, 0.88f, 0.4f, 0.16f, 0.08f, out float far);
Check(far > 1f, "T02 reach: an unreachable layout reports its true reach ratio (> 1) instead of pretending");

var stats = new T02PilotFrameStats(10);
foreach (float ms in new[] { 16.6f, 16.7f, 16.6f, 33.4f, 16.6f, 50f, float.NaN, -1f }) stats.Add(ms);
Check(stats.Count == 6 && stats.Percentile(50) == 16.6f && stats.Percentile(100) == 50f && stats.Max == 50f && stats.Over(33.3f) == 2,
    "T02 stats: nearest-rank percentiles, max and over-budget count from real samples; invalid samples ignored");
for (int i = 0; i < 20; i++) stats.Add(10f);
Check(stats.Count == 10 && stats.Max == 10f && stats.Percentile(99) == 10f, "T02 stats: a fixed window (oldest samples drop out)");
stats.Clear();
Check(stats.Count == 0 && stats.Percentile(50) == 0f, "T02 stats: cleared on rebuild");

// =====================================================================
// 3. Isolation, ownership and cleanup (source; runtime lifecycle is the user's Play Mode check)
// =====================================================================
string fixture = Code("Assets/Scripts/Pilot/T02PilotFixture.cs"), arthur = Code("Assets/Scripts/Pilot/T02PilotArthur.cs"), rules = Code("Assets/Scripts/Pilot/T02PilotRules.cs");
foreach (string forbidden in new[] { "PlayerProgressManager", "CampaignInventory", "InventoryRules", "HudHotbar", "CampaignRewards", "SaveFileStore", "PlayerPrefs",
                                      "Level3Checkpointer", "Level2Checkpointer", "Level1FieldCheckpointer", "CaseManager", "GameplayEvents", "File.Write", "Commit", "Grant(", "PlayerControlHold" })
    Check(!fixture.Replace("\"PlayerProgressManager\"", "").Contains(forbidden) && !arthur.Contains(forbidden) && !rules.Contains(forbidden), $"T02 isolation: no pilot runtime file calls {forbidden}");
Check(fixture.Contains("GameObject.Find(\"PlayerProgressManager\") == null"), "T02 isolation: the readout reports (by name only) whether a campaign owner exists");
string ppm = Src("Assets/Scripts/RewardsSystem/PlayerProgressManager.cs");
int boot = ppm.IndexOf("private static void Bootstrap()", StringComparison.Ordinal);
string bootBody = boot >= 0 ? ppm.Substring(boot, 600) : "";
Check(bootBody.IndexOf("T02PilotIsolation.IsPilotScene", StringComparison.Ordinal) > 0 &&
      bootBody.IndexOf("T02PilotIsolation.IsPilotScene", StringComparison.Ordinal) < bootBody.IndexOf("new GameObject(\"PlayerProgressManager\")", StringComparison.Ordinal),
    "T02 isolation: PlayerProgressManager's bootstrap returns before creating itself (and reading the save) in the pilot scene");
Check(T02PilotIsolation.IsPilotScene("T02_PilotReview") && !T02PilotIsolation.IsPilotScene("Office_Level1") && !T02PilotIsolation.IsPilotScene("MainMenu") &&
      !T02PilotIsolation.IsPilotScene("") && !T02PilotIsolation.IsPilotScene(null),
    "T02 isolation: only the pilot scene is excluded; every game scene keeps the campaign owner");
string Body(string src, string start) { int at = src.IndexOf(start, StringComparison.Ordinal); if (at < 0) return ""; int end = src.IndexOf("\n    }\n", at, StringComparison.Ordinal); return end < 0 ? src.Substring(at) : src.Substring(at, end - at); }
string build = Body(fixture, "public void Build()"), teardown = Body(fixture, "private void Teardown()"), enable = Body(fixture, "private void OnEnable()");
Check(build.IndexOf("Teardown();", StringComparison.Ordinal) >= 0 && build.IndexOf("Teardown();", StringComparison.Ordinal) < build.IndexOf("new GameObject(\"T02 Pilot (runtime)\")", StringComparison.Ordinal),
    "T02 cleanup: every (re)build destroys the previous runtime root first (Restart cannot duplicate actors, cameras or listeners)");
Check(enable.Contains("Active != null && Active != this") && enable.Contains("enabled = false;") && Body(fixture, "private void OnDisable()").Contains("Teardown();"),
    "T02 cleanup: a second fixture refuses to run; disabling/exiting tears everything down");
Check(teardown.Contains("Destroy(runtimeRoot)") && teardown.Contains("orbitGesture.Cancel();") && teardown.Contains("RenderSettings.ambientMode = savedAmbientMode;"),
    "T02 cleanup: teardown drops the owned drag, destroys the one root and restores the scene's ambient settings");
Check(fixture.Contains("if (!other) host.AddComponent<AudioListener>();") && fixture.Contains("host.tag = \"MainCamera\";"),
    "T02 cleanup: one camera, and an AudioListener only when the scene has none");
Check(Body(fixture, "private void Interrupt()").Contains("orbitGesture.Cancel();") && Body(fixture, "private void Interrupt()").Contains("pausedByFocus = true;") &&
      fixture.Contains("private void OnApplicationFocus(bool focused)") && fixture.Contains("private void OnApplicationPause(bool paused)"),
    "T02 cleanup: focus loss/app pause drops the orbit drag and holds the timeline");
Check(Body(arthur, "private void OnDisable()").Contains("folder.SetParent(bay, true)") && Body(arthur, "private void OnDestroy()").Contains("poseHandler?.Dispose();"),
    "T02 cleanup: a disabled Arthur never keeps the folder in his hand; the pose handler is released");
string attach = Body(arthur, "private void Attach()"), detach = Body(arthur, "private void Detach(int rest)");
Check(attach.Contains("folder.SetParent(hand, true)") && detach.Contains("folder.SetParent(bay, true)") && !attach.Contains("SetPositionAndRotation") &&
      Body(arthur, "private void EndBeat(T02PilotStep step)").Contains("Attach();") && Body(arthur, "private void BeginBeat(T02PilotStep step)").Contains("Detach(step.To);"),
    "T02 contact: the folder attaches at the end of Grip and detaches at the start of Release, keeping its world pose (no snap at either moment)");
Check(arthur.Contains("Right Index 1 Stretched") && arthur.Contains("SetFingers(-1f, -1f, -1f)") && arthur.Contains("palmLocal = side.normalized"),
    "T02 contact: the palm side comes from curling the rig's own finger muscles, not an assumed axis");
Check(arthur.Contains("clip.averageSpeed") && arthur.Contains("animator.humanScale") && arthur.Contains("applyRootMotion = false"),
    "T02 motion: walking moves at the walk clip's own ground speed (the existing NpcAnimationDriver measure), so feet do not slide");
Check(!arthur.Contains("StaticArmRig") && !arthur.Contains("NpcBodyPose") && !arthur.Contains("Mathf.Sin(Time"),
    "T02 motion: no static arm lowering, procedural sway or head bob stands in for the grasp");
string treatment = Code("Assets/Editor/Pilot/T02PilotTreatment.cs");
Check(treatment.Contains("if (!InScope(assetPath) || root == null) return;") && treatment.Contains("\"Assets/Pilot/T02/Derived/\"") &&
      treatment.Contains("mesh.blendShapeCount > 0") && treatment.Contains("mesh.bindposes = bindposes"),
    "T02 treatment: the import step only touches the derived pilot copies, skips blend-shape meshes and keeps bind poses");
string runtimeDirs = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
Check(!runtimeDirs.Contains("T02PilotSilhouette.Refine"), "T02 treatment: no runtime script refines meshes (import time only; no runtime smoothing)");

// Regressions for the user-reported review defects (native rendering remains separate).
var gesture = new T02PilotOrbitGesture();
Check(!gesture.Begin(1, Vector2.zero, true) && gesture.Move(1, Vector2.one) == Vector2.zero,
    "T02 orbit: a control-owned finger cannot turn the camera, even after leaving the panel");
Check(gesture.Begin(2, Vector2.zero, false) && !gesture.Begin(3, Vector2.zero, false) &&
    gesture.Move(3, Vector2.one) == Vector2.zero && gesture.Move(2, new Vector2(30, 20)) == new Vector2(30, 20),
    "T02 orbit: mouse/world touch captures exactly one pointer and delivers its drag");
gesture.End(3);
Check(gesture.Move(2, new Vector2(40, 25)) == new Vector2(10, 5), "T02 orbit: another finger releasing cannot steal the drag");
gesture.Cancel();
Check(gesture.Move(2, Vector2.zero) == Vector2.zero && gesture.Begin(-2, Vector2.zero, false), "T02 orbit: focus/view cancellation clears ownership; fresh mouse drag works");
gesture.End(-2);
Check(gesture.Begin(4, Vector2.one, false), "T02 orbit: release permits a fresh touch");
Check(arthur.Contains("animator.gameObject.AddComponent<T02PilotAnimatorIK>().Actor = this") &&
    arthur.Contains("private void OnAnimatorIK(int layer) { if (Actor != null) Actor.ApplyAnimatorIK(layer); }"),
    "T02 IK routing: Unity callback receiver is installed on the Animator object, forwarding calibration and contact to its actor");
Check(Body(treatment, "internal static void RefineImportedMesh(").IndexOf("!mesh.isReadable", StringComparison.Ordinal) <
    Body(treatment, "internal static void RefineImportedMesh(").IndexOf("mesh.Clear()", StringComparison.Ordinal) && treatment.Contains("((ModelImporter)assetImporter).isReadable = true"),
    "T02 import: enable CPU access before import; unreadable mesh throws before Clear");
Check(!Body(fixture, "private bool OverPanel(").Contains("StatsRect") && fixture.Contains("private View view = View.Orbit"),
    "T02 review: passive stats never block dragging; initial full-actor orbit follows Arthur at the entrance");
// Actual imported renderer transformations include large, nonuniform scales. Exercise the production
// conversion on scaled baked points captured from those imported mesh vertices (not source strings).
using (var imported = JsonDocument.Parse(Src("Tests/T02PilotRegression/imported_visibility_fixture.json")))
{
    Vector3 V(JsonElement v) => new Vector3(v.GetProperty("x").GetSingle(), v.GetProperty("y").GetSingle(), v.GetProperty("z").GetSingle());
    foreach (string label in new[] { "A", "B", "Elijah" })
    {
        float maxError = 0f;
        foreach (var sample in imported.RootElement.GetProperty(label).GetProperty("samples").EnumerateArray())
        {
            var q = sample.GetProperty("rotation");
            var rotation = new Quaternion(q.GetProperty("x").GetSingle(), q.GetProperty("y").GetSingle(), q.GetProperty("z").GetSingle(), q.GetProperty("w").GetSingle());
            var b = sample.GetProperty("baked"); var w = sample.GetProperty("world");
            var point = new Vector3(b[0].GetSingle(), b[1].GetSingle(), b[2].GetSingle());
            var expected = new Vector3(w[0].GetSingle(), w[1].GetSingle(), w[2].GetSingle());
            maxError = Mathf.Max(maxError, Vector3.Distance(CharacterBodyGeometry.BakedPointToWorld(V(sample.GetProperty("position")), rotation, point), expected));
        }
        Check(maxError < 0.0001f, $"{label}: imported renderer scales applied once (world error {maxError} m)");
    }
}

string finishFit = Body(arthur, "public void FitStandingHeight(");
Check(!fixture.Contains("model.transform.localScale *= arthurScale") &&
    finishFit.IndexOf("transform.localScale =", StringComparison.Ordinal) < finishFit.IndexOf("animator.Rebind();", StringComparison.Ordinal) &&
    finishFit.IndexOf("animator.Rebind();", StringComparison.Ordinal) < finishFit.IndexOf("RebuildPoseHandler();", StringComparison.Ordinal),
    "T02 fit ordering: scale the actor parent, rebind the Animator, only then initialize Humanoid hand-pose state");
Check(Body(arthur, "private void Calibrate()").Contains("CharacterBodyGeometry.MatchesFit") &&
    CharacterBodyGeometry.MatchesFit(new Bounds(new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 1.7f, 0.5f)), 1.7f, Vector3.zero) &&
    !CharacterBodyGeometry.MatchesFit(new Bounds(Vector3.zero, new Vector3(4f, 1.7f, 3f)), 1.7f, Vector3.zero),
    "T02 post-animation guard catches separated parts even when the overall height happens to match");

Check(!fixture.Contains("ref float arthurScale") && fixture.Contains("FitStandingHeight(arthurHeight)") &&
      finishFit.Contains("for (int pass = 0; pass < 3; pass++)"),
    "T02 each variant fits its actual post-rebind height, independently of the other bays");
string fingers = Body(arthur, "private void WriteFingerPose()");
Check(fingers.Contains("poseTransforms[i].localPosition = posePositions[i]") &&
      fingers.Contains("poseTransforms[i].localRotation = poseRotations[i]") &&
      fingers.Contains("poseTransforms[i].localScale = poseScales[i]") &&
      fingers.IndexOf("poseHandler.SetHumanPose", StringComparison.Ordinal) < fingers.IndexOf("poseTransforms[i].localScale =", StringComparison.Ordinal) &&
      fingers.Contains("fingerBones[i].localRotation = fingerRotations[i]"),
    "T02 finger-only edits retain the complete imported hierarchy, not only the pelvis");

// Actual HumanPose API boundary: getter WORLD normalized -> setter ROOT LOCAL normalized.
// Exercise every bay translation, a quarter-turn and fitted/unfitted parent scales.
foreach (float bayX in new[] { 0f, 7f, 14f })
foreach (float scale in new[] { 0.25f, 1f, 2f })
{
    var rootPosition = new Vector3(bayX, 0.4f, -2f);
    // 90-degree yaw quaternion, without invoking Unity's native Euler constructor.
    float half = (float)Math.Sqrt(0.5);
    var rootRotation = new Quaternion(0f, half, 0f, half);
    var localRotation = new Quaternion(0f, 0f, 0f, 1f);
    var localNormalized = new Vector3(0.03f, 0.98f, -0.02f);
    float humanScale = 3.5f;
    Vector3 normalizedWorld = (rootPosition + rootRotation * (localNormalized * humanScale * scale)) / humanScale;
    Vector3 actual = T02PilotPose.LocalBodyPosition(normalizedWorld, rootPosition, rootRotation, Vector3.one * scale, humanScale);
    Quaternion rotation = T02PilotPose.LocalBodyRotation(rootRotation * localRotation, rootRotation);
    Check(Vector3.Distance(actual, localNormalized) < 1e-5f && Math.Abs(rotation.w - 1f) < 1e-5f && Math.Abs(rotation.y) < 1e-5f,
        $"T02 finger pose round-trip: bay {bayX}, scale {scale}, rotated root preserves body position and orientation");
}
Check(Body(arthur, "private void WriteFingerPose()").Contains("poseTransforms[i].localPosition = posePositions[i]") &&
      Body(arthur, "private void ReadLocalPose()").Contains("T02PilotPose.LocalBodyPosition") &&
      Body(arthur, "private void ApplyCurl()").Contains("ReadLocalPose();") && Body(arthur, "private void ApplyCurl()").Contains("WriteFingerPose();"),
    "T02 finger-only writes convert body coordinates and preserve all non-finger transforms instead of moving the skeleton");

Console.WriteLine($"T02PilotRegression: {checks - fails}/{checks} checks passed");
return fails == 0 ? 0 : 1;

sealed class MeshJson
{
    public string name;
    public double[] points;
    public int[][] polygons;
    public double[][] uvs;
    public Dictionary<string, double[][]> weights;
}
