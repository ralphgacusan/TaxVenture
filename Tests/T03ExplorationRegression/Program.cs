// Milestone T03 mixed-camera exploration regression (Level1To3AdventureMilestone).
// Behavioural checks of the pure exploration rules (ExplorationRules in Assembly-CSharp) and
// source/scene contracts for camera/body/listener ownership, cleanup and unadopted levels.
// Unity physics, Animator/Playables, touch delivery, rendering and device feel are NOT exercised.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;

int fails = 0, checks = 0;
void Check(bool ok, string what)
{
    checks++;
    if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
}
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string Src(string rel) => File.ReadAllText(Path.Combine(root, rel)).Replace("\r\n", "\n");
string Code(string rel) => Regex.Replace(Src(rel), @"///.*|//.*", "");
string Body(string src, string start)
{
    int at = src.IndexOf(start, StringComparison.Ordinal);
    if (at < 0) return "";
    int end = src.IndexOf("\n    }\n", at, StringComparison.Ordinal);
    return end < 0 ? src.Substring(at) : src.Substring(at, end - at);
}
var R = ExplorationRules.Region.Outdoor;

// =====================================================================
// 1. Active first-person baseline and explicitly stored mixed-camera pilot
// =====================================================================
foreach (string scene in new[] { "Office_Level1", "Office_Level2", "Office_Level3", "T02_PilotReview", "MainMenu", null })
    Check(!ExplorationRules.CanAdoptMixedCamera(scene, false), "Disabled pilot keeps first person: " + scene);
Check(ExplorationRules.CanAdoptMixedCamera("Office_Level1", true) &&
      !ExplorationRules.CanAdoptMixedCamera("Office_Level2", true) && !ExplorationRules.CanAdoptMixedCamera(null, true),
    "Stored pilot opt-in remains bounded to its implemented Level 1 scene");
Check(new[] { "Office_Level1", "Office_Level2", "Office_Level3" }.All(ExplorationRules.AdoptsSprint) &&
      !ExplorationRules.AdoptsSprint("T02_PilotReview") && !ExplorationRules.AdoptsSprint("MainMenu") && !ExplorationRules.AdoptsSprint(null),
    "First-person sprint available in all three playable chapters, never the isolated pilot or menus");
Check(ExplorationRules.UsesSmoothedElijah("Office_Level1") && !ExplorationRules.UsesSmoothedElijah("Office_Level2"),
    "Elijah smoothing retains its existing scope independently of the camera feature");
Check(Src("Assets/Resources/Player/ExplorationBody.asset").Contains("  mixedCameraEnabled: 0") &&
      Src("Assets/Scripts/Player/ExplorationBodyAssets.cs").Contains("public bool mixedCameraEnabled = false;") &&
      Body(Src("Assets/Scripts/Player/ExplorationRules.cs"), "public static bool AdoptsMixedCamera(string sceneName)").Contains("assets != null && assets.mixedCameraEnabled"),
    "Shipped and missing-resource defaults keep the mixed-camera/jump feature disabled");

// =====================================================================
// 2. Doorway: stable threshold with hysteresis
// =====================================================================
ExplorationRules.Region Walk(ExplorationRules.Region start, IEnumerable<float> outward, out int switches, float lateral = 0f)
{
    var r = start; switches = 0;
    foreach (float o in outward)
    {
        var next = ExplorationRules.RegionAtDoor(r, o, lateral, 0.9f);
        if (next != r) switches++;
        r = next;
    }
    return r;
}
var rng = new System.Random(3);
var dither = Enumerable.Range(0, 2000).Select(i => (float)(rng.NextDouble() * 1.1 - 0.55)).ToList();
Check(Walk(ExplorationRules.Region.Indoor, dither, out int ditherSwitches) == ExplorationRules.Region.Indoor && ditherSwitches == 0 &&
      Walk(R, dither, out int ditherSwitches2) == R && ditherSwitches2 == 0,
    "T03 doorway: standing and shuffling on the threshold (±0.55 m, 2000 samples) never switches the view");
var crossing = Enumerable.Range(0, 121).Select(i => -3f + i * 0.05f).ToList();   // inside -3 m -> outside +3 m
Check(Walk(ExplorationRules.Region.Indoor, crossing, out int outSwitches) == R && outSwitches == 1 &&
      Walk(R, Enumerable.Reverse(crossing), out int inSwitches) == ExplorationRules.Region.Indoor && inSwitches == 1,
    "T03 doorway: a deliberate crossing either way switches exactly once");
var jitterCross = crossing.Select(o => o + (float)(rng.NextDouble() * 0.5 - 0.25)).ToList();
Check(Walk(ExplorationRules.Region.Indoor, jitterCross, out int jitterSwitches) == R && jitterSwitches == 1,
    "T03 doorway: a wobbly crossing (±0.25 m) still switches exactly once");
Check(Walk(ExplorationRules.Region.Indoor, crossing, out int sideSwitches, lateral: 2.0f) == ExplorationRules.Region.Indoor && sideSwitches == 0 &&
      ExplorationRules.RegionAtDoor(ExplorationRules.Region.Indoor, 3.5f, 0f, 0.9f) == ExplorationRules.Region.Indoor,
    "T03 doorway: walking past beside the doorway, or far from it, never switches by the door line");
Check(ExplorationRules.RegionFromFloor(R, ExplorationRules.Region.Indoor, 0.2f) == R && ExplorationRules.RegionFromFloor(R, ExplorationRules.Region.Indoor, 0.9f) == ExplorationRules.Region.Indoor &&
      ExplorationRules.RegionFromFloor(R, null, 5f) == R,
    "T03 floor fallback: a spawn/reload away from doorways settles after 0.8 s; one stray sample or no floor keeps the view");
Check(ExplorationRules.CanSwitchView(true, false, false, false, false, false) &&
      !ExplorationRules.CanSwitchView(false, false, false, false, false, false) && !ExplorationRules.CanSwitchView(true, true, false, false, false, false) &&
      !ExplorationRules.CanSwitchView(true, false, true, false, false, false) && !ExplorationRules.CanSwitchView(true, false, false, true, false, false) &&
      !ExplorationRules.CanSwitchView(true, false, false, false, true, false) && !ExplorationRules.CanSwitchView(true, false, false, false, false, true),
    "T03 deferral: no view change while a task, transition, conversation, hold, viewer or the previous blend owns the camera");
Check(ExplorationRules.ThirdPersonFor(R) && !ExplorationRules.ThirdPersonFor(ExplorationRules.Region.Indoor),
    "T03 view: outdoors third person, indoors first person");

// =====================================================================
// 3. Sprint and jump
// =====================================================================
bool s = false;
var trace = new List<bool>();
foreach (float m in new[] { 0.5f, 1f, 1.46f, 1.35f, 1.26f, 1.24f, 1.65f }) { s = ExplorationRules.Sprinting(s, true, m); trace.Add(s); }
Check(trace.SequenceEqual(new[] { false, false, true, true, true, false, true }) && !ExplorationRules.Sprinting(true, false, 1f) &&
      Mathf.Approximately(ExplorationRules.SprintMultiplier, 1.4f),
    "T03 sprint: enters beyond the walk rim (1.45), holds down to 1.25, stops below it or on release; 1.4× walk");
bool[] presses = { true, false, false, false, false, false };
int jumps = 0; float lastJump = -10f; float t = 0f;
for (int frame = 0; frame < presses.Length; frame++, t += 0.05f)
    if (ExplorationRules.CanJump(presses[frame], true, 0f, t - lastJump)) { jumps++; lastJump = t; }
Check(jumps == 1, "T03 jump: one press gives one jump; holding (no new press) never repeats");
Check(!ExplorationRules.CanJump(true, false, 0.5f, 5f) && ExplorationRules.CanJump(true, false, 0.08f, 5f) && !ExplorationRules.CanJump(true, true, 0f, 0.1f) &&
      !ExplorationRules.CanJump(false, true, 0f, 5f),
    "T03 jump: no jump in the air (only a 0.10 s coyote step-off), none right after the last, none without a press");
float v0 = ExplorationRules.JumpSpeed(0.42f, -9.81f);
float y = 0f, vy = v0, x = 0f, apex = 0f, clearedAt = -1f, landX = -1f;
for (float dt = 0.002f, time = 0f; time < 2f; time += dt)
{
    vy -= 9.81f * dt; y += vy * dt; x += 3.5f * dt; apex = Mathf.Max(apex, y);
    if (clearedAt < 0f && y > 0.18f + 0.02f) clearedAt = x;
    if (y < 0f) { landX = x; break; }
}
Check(Mathf.Abs(apex - 0.42f) < 0.01f && clearedAt > 0f && landX - clearedAt > 1.0f,
    $"T03 jump: a 0.42 m apex clears the 0.18 m practice curb at walking speed with {landX - clearedAt:0.00} m of clearance window");
Check(!ExplorationRules.NeedsRecovery(-1f, 0f, 1f) && ExplorationRules.NeedsRecovery(-3.5f, 0f, 1f) && ExplorationRules.NeedsRecovery(0.5f, 0f, 3.5f) &&
      ExplorationRules.SafeGround(true, 1f, 0f, 0.5f) && !ExplorationRules.SafeGround(true, 1f, 0f, 0.1f) && !ExplorationRules.SafeGround(true, 0.5f, 0f, 1f) &&
      !ExplorationRules.SafeGround(false, 1f, 0f, 1f) && !ExplorationRules.SafeGround(true, 1f, 2f, 1f),
    "T03 recovery: a 3 m drop or 3 s airborne returns to the last safe point; only settled, level, grounded poses are saved as safe");

// =====================================================================
// 4. Camera collision and player-root targeting
// =====================================================================
Check(ExplorationRules.CollisionDistance(2.4f, null) == 2.4f && Mathf.Abs(ExplorationRules.CollisionDistance(2.4f, 1.0f) - 0.92f) < 1e-4f &&
      ExplorationRules.CollisionDistance(2.4f, 0f) == ExplorationRules.CameraMinDistance && ExplorationRules.CollisionDistance(2.4f, 9f) == 2.4f,
    "T03 camera: the orbit stops short of the nearest obstacle (skin 0.08 m), never inside the head, never past its distance");
Check(ExplorationRules.FollowDistance(2.4f, 0.9f, 0.016f) == 0.9f && ExplorationRules.FollowDistance(0.9f, 2.4f, 0.1f) < 2.4f &&
      ExplorationRules.FollowDistance(0.9f, 2.4f, 0.1f) > 0.9f,
    "T03 camera: pulls in at once when a wall comes between (no see-through frame), eases back out");
Check(ExplorationRules.PlayerCanTarget(1.5f, 3f, 1f, false) && !ExplorationRules.PlayerCanTarget(3.4f, 3f, 1f, false) &&
      !ExplorationRules.PlayerCanTarget(1.5f, 3f, -1f, false) && !ExplorationRules.PlayerCanTarget(1.5f, 3f, 1f, true),
    "T03 targeting: the player's own reach and line of sight decide; behind the player or through a wall is refused");

// =====================================================================
// 5. Ownership, cleanup and unadopted levels (source/scene contracts)
// =====================================================================
string cc = Src("Assets/Scripts/Player/CameraController.cs"), fpc = Src("Assets/Scripts/Player/FirstPersonCameraController.cs");
string pm = Src("Assets/Scripts/Player/PlayerMovement.cs"), inter = Src("Assets/Scripts/Player/Interactor.cs");
Check(Regex.IsMatch(cc, @"public enum CameraMode\s*\{\s*FirstPerson,\s*Workstation,\s*Interview,\s*CorkboardSideQuest,\s*ProfileViewer\s*\}"),
    "T03 serialized identity: CameraMode values unchanged (FirstPerson still means exploration in either view)");
string apply = Body(cc, "private void ApplyExplorationView(bool instant)"), update = Body(cc, "private void Update()");
Check(apply.Contains("writer.SetThirdPerson(tp, instant)") && apply.Contains("firstPersonCamera.SetThirdPerson(tp, true)") &&
      apply.Contains("body.SetPerspectiveShown(tp)") && apply.Contains("hands.SetThirdPersonView(tp"),
    "T03 ownership: one view decision drives the camera writer (Main Camera's controller), its helper, the body and the hands together");
Check(update.Contains("if (!mixedExploration) return;") && update.Contains("ExplorationRules.CanSwitchView(") && update.Contains("PlayerControlHold.IsHeld") &&
      update.Contains("DialogueUI.IsAnyDialogueActive || dialogueHoldsControls") && update.Contains("profileViewerControls != null") && update.Contains("writer.IsBlending"),
    "T03 deferral wiring: the owner checks transitions, conversations, holds, the viewer and the blend before switching");
Check(Body(cc, "private void SetupMixedExploration()").Contains("ExplorationRules.AdoptsMixedCamera(gameObject.scene.name)"),
    "T03 adoption wiring: the mixed loop is set up only in the adopted scene");
string production = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.EndsWith("ThirdPersonCameraFollow.cs")).Select(File.ReadAllText));
Check(!production.Contains("ThirdPersonCameraFollow"), "T03 one writer: the old ThirdPersonCameraFollow is not enabled or referenced alongside the camera owner");
Check(fpc.Contains("if (!thirdPerson && viewWeight <= 0f && viewBlendElapsed < 0f)") && fpc.Contains("Physics.SphereCastNonAlloc(") &&
      fpc.Contains("public void SetLook(float newYaw, float newPitch)") && Body(fpc, "public bool TryGetViewPose(").Contains("if (thirdPerson)"),
    "T03 camera writer: first person keeps its original path; third person adds one bounded collision cast; the return pose is the settled view");
string disable = Body(pm, "private void OnDisable()");
Check(disable.Contains("sprinting = false;") && disable.Contains("jumpRequested = false;") && disable.Contains("SetSprintLit(false)") &&
      pm.Contains("if (!focused) OnDisable();") && pm.Contains("if (paused) OnDisable();") &&
      Body(pm, "public void RequestJump()").Contains("if (!sprintAndJump || !enabled || !hasFocus || isPaused) return;"),
    "T03 cancellation: holds/transitions (disable), focus loss and pause clear sprint and any pending jump; presses only count while movement is live");
Check(pm.Contains("sprintEnabled = ExplorationRules.AdoptsSprint(gameObject.scene.name);") && pm.Contains("sprintAndJump = ExplorationRules.AdoptsMixedCamera(gameObject.scene.name);") && pm.Contains("if (sprintAndJump)\n        {\n            SenseGround();") &&
      pm.Contains("if (moveDirection.sqrMagnitude > 0.01f && (!sprintAndJump || grounded) && !BodyDrivesFootsteps)") &&
      Body(pm, "private void FixedUpdate()").Contains("float speed = sprinting ? moveSpeed * ExplorationRules.SprintMultiplier : moveSpeed;"),
    "Sprint adoption is separate from stored jump/ground/recovery support; first-person pace retains the sprint multiplier");
foreach (string forbidden in new[] { "Inventory", "Checkpoint", "PlayerProgressManager", "Commit", "Grant", "Reward", "PlayerPrefs" })
    Check(!Code("Assets/Scripts/Player/PlayerMovement.cs").Contains(forbidden) && !Code("Assets/Scripts/Player/ExplorationBody.cs").Contains(forbidden) &&
          !Code("Assets/Scripts/Player/ExplorationPracticeCurb.cs").Contains(forbidden) && !Code("Assets/Scripts/Mobile/JumpButton.cs").Contains(forbidden),
        $"T03 persistence: movement, body, curb and jump control never touch {forbidden} (falls/recovery keep carried papers and work)");
Check(inter.Contains("else if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))") &&
      Body(inter, "private IInteractable ThirdPersonTarget(Ray ray, CameraController owner)").Contains("ExplorationRules.PlayerCanTarget(flat, interactRange, ahead, blocked)") &&
      Body(inter, "private IInteractable ThirdPersonTarget(Ray ray, CameraController owner)").Contains("InteractionRange.Occluded(eye, far.point"),
    "T03 targeting wiring: first person keeps its exact ray; third person validates player reach, facing and line of sight");
Check(Src("Assets/Scripts/Player/InteractionRange.cs").Contains("if (owner != null && owner.IsThirdPersonView)") &&
      Src("Assets/Scripts/Player/InteractionRange.cs").Contains("RaycastNonAlloc(eye"),
    "T03 targeting wiring: direct world taps also need the player's line of sight in third person (bounded cast)");
string director = Src("Assets/Scripts/Player/DialogueCameraDirector.cs");
Check(director.Contains("if (framed && ExplorationBody.Current != null) ExplorationBody.Current.HideFor(this);") &&
      Body(director, "private void RestoreHandsAndVisual()").Contains("ExplorationBody.Current.ReleaseHide(this)") &&
      Body(director, "private void RestoreHandsAndVisual()").IndexOf("ReleaseHide", StringComparison.Ordinal) < Body(director, "private void RestoreHandsAndVisual()").IndexOf("ownershipLost", StringComparison.Ordinal),
    "T03 one body: the conversation's own Elijah replaces the exploration body while framed, and every end path (incl. lost ownership) gives it back");
string hands = Src("Assets/Scripts/Player/FirstPersonHands.cs");
Check(hands.Contains("public bool IsShowing => handsRoot != null && handsWanted;") && Body(hands, "public void Show()").Contains("handsRoot.SetActive(!thirdPerson)") &&
      Body(hands, "public void SetThirdPersonView(bool on, Transform carryHand)").Contains("paper.SetParent(carriedHome, false)"),
    "T03 hands/paper: snapshots keep what callers wanted; no floating camera hands in third person; the one carried paper returns home in first person");
string jump = Src("Assets/Scripts/Mobile/JumpButton.cs");
Check(jump.Contains("public const float MinimumDp = 48f;") && jump.Contains("MinimumDp * dpi / 160f") && Body(jump, "public void OnPointerDown(PointerEventData eventData)").Contains("if (eventData == null || IsPressed) return;") &&
      Body(jump, "public void OnPointerUp(PointerEventData eventData)").Contains("eventData.pointerId == pointer") && jump.Contains("private void OnApplicationFocus(bool focused) { if (!focused) Release(); }"),
    "T03 jump control: ≥48 dp target, its own pointer (move/look/jump together), one request per press, released on focus loss");
Check(Body(jump, "private void LateUpdate()").Contains("stick.gameObject.activeInHierarchy") && Body(jump, "private void LateUpdate()").Contains("movement.isActiveAndEnabled"),
    "T03 jump control: hidden whenever the stick is hidden or movement is held (tasks, dialogue, transitions)");
string body = Src("Assets/Scripts/Player/ExplorationBody.cs");
Check(body.Contains("foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) Destroy(c);") && body.Contains("DirectorUpdateMode.Manual") &&
      body.Contains("if (!IsShown || movement == null) return;") && !body.Contains("FindObjectsByType") && !body.Contains("FindAnyObjectByType"),
    "T03 body: visual only (no colliders), evaluated only while shown, no per-frame searches");
string asset = Src("Assets/Resources/Player/ExplorationBody.asset");
Check(asset.Contains("model: {fileID: 919132149155446097, guid: 87838c26fef240a3a2a99f95c91e41ea, type: 3}") &&
      asset.Contains("walk: {fileID: -203655887218126122, guid: c8feebe21aab3eb41b121dc3ef4321de, type: 3}") &&
      asset.Contains("idle: {fileID: -203655887218126122, guid: 627d8de63b4cc4c49bcf990b12b41b38, type: 3}"),
    "T03 body assets: smoothed copy of Elijah, original walk and shared idle retain their bindings");
string walkMeta = Src("Assets/3D Models/npc_elijah/Elijah_Walking.fbx.meta");
Check(walkMeta.Contains("      loopTime: 0") && walkMeta.Contains("  animationType: 3"),
    "T03 cutscene imports preserved: Elijah_Walking stays Humanoid and non-looping (the body loops it by distance instead)");

Check(Body(Src("Assets/Scripts/Player/PlayerControlHold.cs"), "private static void Apply()").Contains("foreach (FirstPersonCameraController look in Object.FindObjectsByType<FirstPersonCameraController>())"),
    "T03 holds: a control hold stops every look controller (the camera writer and its helper), so the view cannot turn under a hold");
// Scenes: no second writer, controller, body or listener; nothing in the office scenes was rewritten for T03.
foreach (string scene in new[] { "Office_Level1", "Office_Level2", "Office_Level3" })
{
    string text = Src("Assets/Scenes/" + scene + ".unity");
    string Guid(string script) => Regex.Match(Src(script + ".meta"), @"guid: ([0-9a-f]{32})").Groups[1].Value;
    int Count(string script) => Regex.Matches(text, @"m_Script: \{fileID: 11500000, guid: " + Guid(script)).Count;
    Check(Count("Assets/Scripts/Player/PlayerMovement.cs") == 1 && Count("Assets/Scripts/Player/ThirdPersonCameraFollow.cs") == 0 &&
          Count("Assets/Scripts/Player/ConversationActorVisual.cs") == 1 && Count("Assets/Scripts/Player/ExplorationBody.cs") == 0 &&
          Regex.Matches(text, @"\nAudioListener:\n").Count == 1,
        $"T03 {scene}: one player controller, one conversation Elijah, no old orbit writer, no saved exploration body, one AudioListener");
}

// Guide: silent, non-blocking control hints in their right place.
JsonDocument guide = JsonDocument.Parse(Src("Assets/Resources/Tutorials/Level_01/guide.json"));
var lessons = guide.RootElement.GetProperty("lessons").EnumerateArray().ToList();
int runJump = lessons.FindIndex(l => l.GetProperty("id").GetString() == "run-jump");
var pages = runJump >= 0 ? lessons[runJump].GetProperty("pages").EnumerateArray().ToList() : new List<JsonElement>();
Check(runJump > 0 && lessons[runJump - 1].GetProperty("id").GetString() == "look" && lessons[runJump + 1].GetProperty("id").GetString() == "bag-empty" &&
      lessons[runJump].GetProperty("when").GetString() == "lesson:look",
    "T03 guide: the run/hop hint follows 'look' and comes before the Bag/Notes lessons (their own triggers unchanged)");
Check(pages.Count == 1 && pages.All(p => p.GetProperty("style").GetString() == "hint") &&
      pages.All(p => p.GetProperty("waitFor").GetString().Contains("move:")) &&
      pages[0].GetProperty("waitFor").GetString().Contains("cue:" + PlayerMovement.CueSprinted) &&
      pages.All(p => p.GetProperty("text").GetString().Split(' ').Length <= 14),
    "First-person guide: one silent sprint hint (≤14 words), completed by sprinting or walking on; saved lesson ID retained");
Check(Src("Assets/Scripts/UI/Guide/GuideTargets.cs").Contains("case \"jump\": return HudPath(\"MobileControls/\" + JumpButton.ObjectName);"),
    "T03 guide: hud:jump resolves to the jump control beside the stick");

Check(Body(pm, "private void UpdateSprint()").Contains("if (!sprintEnabled || !hasFocus || isPaused") &&
      Body(cc, "private void SetupMixedExploration()").IndexOf("ShowSprintRing", StringComparison.Ordinal) <
      Body(cc, "private void SetupMixedExploration()").IndexOf("if (!mixedExploration) return;", StringComparison.Ordinal),
    "First-person sprint logic and its joystick feedback run before the disabled pilot returns");
Check(Body(jump, "public static JumpButton Create(").Contains("!player.SprintAndJumpEnabled") &&
      Body(cc, "private void SetupMixedExploration()").IndexOf("JumpButton.Create", StringComparison.Ordinal) >
      Body(cc, "private void SetupMixedExploration()").IndexOf("if (!mixedExploration) return;", StringComparison.Ordinal) &&
      !guide.RootElement.GetRawText().Contains("hud:jump") && !guide.RootElement.GetRawText().Contains("cue:PlayerJumped"),
    "Disabled jump cannot build a touch target and has no active guide prompt; keyboard and external requests share the guarded path");
Check(Src("Assets/Scripts/Player/ConversationActorVisual.cs").Contains("ExplorationRules.UsesSmoothedElijah(gameObject.scene.name)") &&
      !Src("Assets/Scripts/Player/ConversationActorVisual.cs").Contains("ExplorationRules.AdoptsMixedCamera"),
    "Smoothed conversation Elijah stays active without mixed-camera adoption");

// Persistence: no new save fields; Level 1 saves no player pose (so no midair pose can be stored).
Check(!Regex.IsMatch(Src("Assets/Scripts/RewardsSystem/PlayerProgressData.cs"), @"\bexploration|\bthirdperson|\bsafepoint|\bsprint|\bjump", RegexOptions.IgnoreCase) && !Regex.IsMatch(Src("Assets/Scripts/SideQuest1/Level1FieldCheckpointer.cs"), @"player\w*(Position|Pose)", RegexOptions.IgnoreCase),
    "T03 persistence: no new save data; Level 1 never stores a player/camera pose (teaching completion uses the existing guide-lessons record)");
// The T02 pilot guard and T01 safeguards stay in place.
Check(Src("Assets/Scripts/RewardsSystem/PlayerProgressManager.cs").Contains("T02PilotIsolation.IsPilotScene") &&
      Src("Assets/Scripts/Level3/Level3ReviewRules.cs").Contains("RefuseUnclipFull") && Src("Assets/Scripts/Level3/Level3CertificateRules.cs").Contains("ClipToFolder"),
    "T03 continuity: the T02 campaign-save guard and T01 certificate clip/unclip safeguards are untouched");

// =====================================================================
// 6. Third-person experience repair (11 Nov 2026): world scale
// =====================================================================
// Rest-pose native height of the imported Elijah, captured independently of BakeMesh.
float elijahNative;
using (var imported = JsonDocument.Parse(Src("Tests/T02PilotRegression/imported_visibility_fixture.json")))
{
    var e = imported.RootElement.GetProperty("Elijah");
    Check(e.GetProperty("vertices").GetInt32() == 4585 && e.GetProperty("triangles").GetInt32() == 2537,
        "T03 captured imported Elijah has populated geometry (the invisible body was not an empty FBX)");
    elijahNative = e.GetProperty("nativeHeight").GetSingle();
}
float elijahScale = ExplorationRules.CalibratedScale(elijahNative);
Check(Mathf.Abs(elijahNative * elijahScale - ExplorationRules.ElijahRestHeight) < 1e-4f && Mathf.Abs(elijahScale - 0.23604f) < 2e-4f,
    $"T03 scale: rest geometry {elijahNative:0.0000} native -> canonical {ExplorationRules.ElijahRestHeight} m at world scale {elijahScale:0.00000}");
// The rejected build fitted a posed BakeMesh reading (5.946) to 1.5726 m: rest-equivalent ~1.77 m.
float rejected = 1.5726485f / 5.946f * elijahNative;
// Adult plaza cohort, same rest-pose method (native FBX height x live scene scale chain).
var cohort = new Dictionary<string, float> {
    ["Marie"] = 1.849f * 0.8363f, ["Jhun"] = 6.683f * 0.2346f, ["Bea"] = 1.807f * 0.87f, ["Lorna"] = 1.867f * 0.86f,
    ["Milo"] = 1.802f * 0.90f, ["Arthur"] = 7.005f * 0.2346f, ["Carlo"] = 1.800f * 0.93f };
var sorted = cohort.Values.OrderBy(v => v).ToList();
float median = sorted[sorted.Count / 2];
Check(Mathf.Abs(ExplorationRules.ElijahRestHeight / median - 1f) <= 0.05f && rejected / median > 1.09f,
    $"T03 scale: Elijah {ExplorationRules.ElijahRestHeight} m is within ±5% of the adult cohort median {median:0.000} m; the rejected fit was {rejected:0.000} m ({rejected / median - 1f:+0%})");
Check(cohort.Values.All(h => Mathf.Abs(ExplorationRules.ElijahRestHeight / h - 1f) < 0.08f),
    "T03 scale: Elijah reads as an ordinary adult beside every cohort member (no giant/miniature, legitimate differences kept)");
foreach (float parentScale in new[] { 0.7f, 1f, 2f })
{
    float hostLocal = elijahScale / parentScale; // ExplorationBody.Follow cancels the parent's scale
    Check(Mathf.Abs(elijahNative * hostLocal * parentScale - ExplorationRules.ElijahRestHeight) < 1e-4f,
        $"T03 scale: under a {parentScale} parent the world height is still canonical (scale applied once)");
}
Check(ExplorationRules.PlausiblePosedHeight(1.55f, 1.58f) && ExplorationRules.PlausiblePosedHeight(1.30f, 1.58f) &&
      !ExplorationRules.PlausiblePosedHeight(1.77f, 1.58f) && !ExplorationRules.PlausiblePosedHeight(1.0f, 1.58f),
    "T03 scale: bent knees/landing may shorten the pose; it never exceeds the rest height or collapses");
string cav = Src("Assets/Scripts/Player/ConversationActorVisual.cs");
Check(body.Contains("CharacterBodyGeometry.TryMeasureRest(model, out Bounds rest)") && body.Contains("worldScale = ExplorationRules.CalibratedScale(rest.size.y);") &&
      Regex.Matches(body, @"worldScale\s*=\s*ExplorationRules\.CalibratedScale").Count == 1 && !body.Contains("worldScale *=") &&
      body.IndexOf("worldScale = ExplorationRules.CalibratedScale", StringComparison.Ordinal) < body.IndexOf("animator.Rebind();", StringComparison.Ordinal) &&
      !Body(body, "private void LateUpdate()").Contains("worldScale ="),
    "T03 scale wiring: rest geometry -> one canonical scale on the visual parent before the final rebind; never rescaled per frame");
Check(cav.Contains("CharacterBodyGeometry.TryMeasureRest(modelRoot, out Bounds rest)") && cav.Contains("ExplorationRules.CalibratedScale(rest.size.y)") &&
      !Regex.Replace(cav, @"//.*", "").Contains("CharacterBodyGeometry.TryMeasure(modelRoot") &&
      cc.Contains("body = ExplorationBody.Ensure(playerMovement);") && !cc.Contains("conversationElijah.StandingHeight"),
    "T03 scale: conversation and exploration Elijah share the one calibration (no posed BakeMesh re-fit, no height hand-off)");
Check(Mathf.Abs(ExplorationRules.CapsuleStandingHeight(1.6f, 0.5f, new Vector3(0.7f, 0.7f, 0.7f)) - 1.12f) < 1e-5f &&
      ExplorationRules.CapsuleStandingHeight(1f, 0.5f, new Vector3(2, 1, 2)) == 2f,
    "T03 physics: the 1.12 m capsule is unchanged; visual height is independent of it");
Check(CharacterBodyGeometry.MatchesFit(new Bounds(new Vector3(0f, 0.56f, 0f), new Vector3(0.4f, 1.12f, 0.3f)), 1.12f, Vector3.zero) &&
      !CharacterBodyGeometry.MatchesFit(new Bounds(Vector3.zero, new Vector3(3f, 4f, 2f)), 1.12f, Vector3.zero),
    "T03 shared fit guard (T02): grounded body accepted; oversized/separated pose rejected");

// =====================================================================
// 7. Camera composition (viewport math, unobstructed, gameplay rectangle)
// =====================================================================
float H = ExplorationRules.ElijahRestHeight;
// Managed Euler (Unity's ZXY order, z = 0): Quaternion.Euler is a native call unavailable outside the Editor.
Quaternion Euler(float pitchDeg, float yawDeg)
{
    float p = pitchDeg * Mathf.Deg2Rad * 0.5f, yv = yawDeg * Mathf.Deg2Rad * 0.5f;
    return new Quaternion(0f, (float)Math.Sin(yv), 0f, (float)Math.Cos(yv)) * new Quaternion((float)Math.Sin(p), 0f, 0f, (float)Math.Cos(p));
}
(Vector3 feet, Vector3 head, Vector3 pivot) Frame(float lookPitch, float aspect, float distanceScale, out Vector3 camPos, out Quaternion rot)
{
    Vector3 floor = new Vector3(3f, 0.2f, -5f);
    Vector3 pv = floor + Vector3.up * (H * ExplorationRules.CameraPivotFraction);
    rot = Euler(Mathf.Clamp(lookPitch + ExplorationRules.CameraPitchBias, ExplorationRules.CameraMinPitch, ExplorationRules.CameraMaxPitch), 37f);
    Vector3 full = ExplorationRules.CameraPosition(pv, rot, H * ExplorationRules.CameraDistanceHeights, ExplorationRules.CameraScreenOffset, 60f);
    camPos = pv + (full - pv) * distanceScale;
    return (ExplorationRules.ViewportFromTop(floor, camPos, rot, 60f, aspect), ExplorationRules.ViewportFromTop(floor + Vector3.up * H, camPos, rot, 60f, aspect),
            ExplorationRules.ViewportFromTop(pv, camPos, rot, 60f, aspect));
}
foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 2f, 20f / 9f })
{
    var f = Frame(0f, aspect, 1f, out Vector3 cp, out _);
    float occupancy = f.feet.y - f.head.y;
    Check(f.feet.y >= 0.88f && f.feet.y <= 0.93f && occupancy >= 0.38f && occupancy <= 0.46f && Mathf.Abs(f.feet.x - 0.5f) <= 0.03f && Mathf.Abs(f.head.x - 0.5f) <= 0.03f,
        $"T03 framing {aspect:0.00}:1: feet at {f.feet.y:0.000} from top, body {occupancy:0.000} of the view, centred ({f.feet.x:0.000})");
}
{
    var f = Frame(0f, 2f, 1f, out Vector3 cp, out Quaternion r);
    Vector3 fwd = r * Vector3.forward;
    float downward = -Mathf.Asin(fwd.y) * Mathf.Rad2Deg;
    float behind = new Vector2(cp.x - 3f, cp.z + 5f).magnitude;
    Check(downward >= 8f && downward <= 15f && Mathf.Abs(H * ExplorationRules.CameraDistanceHeights - 3.018f) < 0.01f && cp.y - 0.2f > H && behind > 2.5f,
        $"T03 framing: gentle {downward:0}° downward look from {cp.y - 0.2f:0.00} m above the floor, {behind:0.00} m behind (not overhead)");
}
foreach (float look in new[] { -40f, -20f, 36f, 60f })
{
    var f = Frame(look, 2f, 1f, out _, out _);
    Check(f.head.y > 0.02f && f.feet.y < 0.985f && f.feet.z > 0f,
        $"T03 framing: at look pitch {look}° (clamped) the whole standing body stays in view (head {f.head.y:0.00}, feet {f.feet.y:0.00})");
}
{
    var near = Frame(0f, 2f, 0.3f, out _, out _); var far = Frame(0f, 2f, 1f, out _, out _);
    Check(Mathf.Abs(near.pivot.y - far.pivot.y) < 1e-4f && Mathf.Abs(near.pivot.x - 0.5f) < 1e-4f,
        "T03 framing: collision pull-in along pivot->camera keeps the body's screen position (only its size changes)");
}
Check(!fpc.Contains("thirdPersonDistance") && fpc.Contains("ExplorationRules.CameraPosition(pivot, rotation") && fpc.Contains("VerticalFov") &&
      !Regex.Replace(fpc, @"//.*", "").Contains("fieldOfView ="),
    "T03 camera writer: composition solved from canonical height and the camera's own FOV; it never writes the shared FOV");

// Collision: immediate pull-in, delayed and frame-rate-independent recovery, no pumping.
Check(ExplorationRules.CollisionDistance(3f, null) == 3f && Mathf.Abs(ExplorationRules.CollisionDistance(3f, 1.0f) - 0.92f) < 1e-4f &&
      ExplorationRules.CollisionDistance(3f, 0f) == ExplorationRules.CameraMinDistance,
    "T03 camera: the orbit stops short of the nearest obstacle (skin 0.08 m), never inside the head");
Check(ExplorationRules.FollowDistance(3f, 0.9f, 0.016f, 5f) == 0.9f && ExplorationRules.FollowDistance(0.9f, 3f, 0.1f, 0.1f) == 0.9f &&
      ExplorationRules.FollowDistance(0.9f, 3f, 0.1f, 0.5f) > 0.9f && ExplorationRules.FollowDistance(0.9f, 3f, 0.1f, 0.5f) < 3f,
    "T03 camera: pulls in at once; holds while the way has been clear < 0.3 s; then eases out");
float Recover(int fps)
{
    float d = 0.9f, clear = 0f, dt = 1f / fps;
    for (int i = 0; i < fps; i++) { clear += dt; d = ExplorationRules.FollowDistance(d, 3f, dt, clear); }
    return d;
}
Check(Mathf.Abs(Recover(30) - Recover(120)) < 0.03f && Recover(60) > 2.3f,
    $"T03 camera: recovery is frame-rate independent (30 fps {Recover(30):0.00} m vs 120 fps {Recover(120):0.00} m after 1 s)");
{
    // A collider edge flickering every other frame must not pump the distance back and forth.
    float d = 3f, clear = 0f, minAfter = 3f, maxAfter = 0f; int changes = 0; float last = d;
    for (int i = 0; i < 120; i++)
    {
        float allowed = i % 2 == 0 ? 1.2f : 3f;
        clear = allowed < 3f ? 0f : clear + 1f / 60f;
        d = ExplorationRules.FollowDistance(d, allowed, 1f / 60f, clear);
        if (i > 10) { minAfter = Mathf.Min(minAfter, d); maxAfter = Mathf.Max(maxAfter, d); if (Mathf.Abs(d - last) > 1e-5f) changes++; }
        last = d;
    }
    Check(maxAfter - minAfter < 1e-4f && changes == 0, "T03 camera: a flickering obstacle holds one stable distance (no pumping)");
}
Check(ExplorationRules.PassesOver(0.5f, 0.95f, 0.18f) && ExplorationRules.PassesOver(0.9f, 0.9f, 0.18f) && !ExplorationRules.PassesOver(3f, 1.2f, 0.18f) &&
      fpc.Contains("if (IsPerson(h.collider)) continue;") && fpc.Contains("ExplorationRules.PassesOver(h.collider.bounds.max.y"),
    "T03 camera: benches/curbs below the rising camera path and nearby NPCs never zoom the view; walls and ceilings still block");
Check(fpc.Contains("bool tooClose = hidingBodyClose ? distance < 0.75f : distance < 0.55f;") && Body(fpc, "private void OnDisable()").Contains("ReleaseCloseHide();"),
    "T03 camera: squeezed against a wall the body hides (with hysteresis) instead of filling the screen from inside; released on exit");
{
    float tracked = 0f, hop = 0f, hv = ExplorationRules.JumpSpeed(0.42f, -9.81f), maxTracked = 0f;
    for (int i = 0; i < 120 && !(i > 5 && hop <= 0f); i++)
    {
        hv -= 9.81f / 60f; hop = Mathf.Max(0f, hop + hv / 60f);
        tracked = ExplorationRules.TrackFloor(tracked, hop, false, 0f, 1f / 60f); maxTracked = Mathf.Max(maxTracked, tracked);
    }
    float dropTracked = 0f;
    for (int i = 0; i < 60; i++) dropTracked = ExplorationRules.TrackFloor(dropTracked, -1f, false, 0f, 1f / 60f);
    float step = 0f;
    for (int i = 0; i < 30; i++) step = ExplorationRules.TrackFloor(step, 0.2f, true, 0.2f, 1f / 60f);
    Check(maxTracked < 1e-4f && dropTracked < -0.99f && Mathf.Abs(step - 0.2f) < 0.01f && ExplorationRules.TrackFloor(0f, 9f, true, 9f, 1f / 60f) == 9f,
        "T03 camera: steady through a hop arc, follows a real drop and a curb step smoothly, snaps after a teleport");
}

// =====================================================================
// 8. Locomotion: deliberate pace, coherent stride, controlled response
// =====================================================================
Check(Mathf.Abs(ExplorationRules.TargetSpeed(true, 0.5f, false, 3.5f) - 0.725f) < 1e-4f && ExplorationRules.TargetSpeed(true, 1f, false, 3.5f) == ExplorationRules.WalkSpeed &&
      ExplorationRules.TargetSpeed(true, 1f, true, 3.5f) == ExplorationRules.RunSpeed &&
      ExplorationRules.TargetSpeed(false, 1f, false, 3.5f) == 3.5f && Mathf.Abs(ExplorationRules.TargetSpeed(false, 1f, true, 3.5f) - 4.9f) < 1e-4f,
    "T03 pace: outdoors small stick walks (analog to 1.45 m/s), the outer ring runs (3.8 m/s); indoor first person keeps 3.5/4.9");
float walkStride = 0.857f * H, runStride = ExplorationRules.RunStrideHeights * H;
float walkRate = ExplorationRules.WalkSpeed / walkStride * 1.1333334f, runRate = ExplorationRules.RunSpeed / runStride * 0.6f;
Check(walkRate >= 0.6f && walkRate <= 1.3f && runRate >= 0.85f && runRate <= 1.15f,
    $"T03 stride: rim walk plays Elijah's walk at {walkRate:0.00}x, the run at {runRate:0.00}x its authored cadence (feet match travel)");
Check(ExplorationRules.RunWeight(ExplorationRules.WalkSpeed) == 0f && ExplorationRules.RunWeight(ExplorationRules.RunSpeed) == 1f &&
      ExplorationRules.WalkWeight(0f) == 0f && ExplorationRules.WalkWeight(ExplorationRules.WalkSpeed) == 1f,
    "T03 blend: rim walking never shows the run; full run is pure run; standing is pure idle");
{
    Vector3 vel = Vector3.zero; float t60 = 0f;
    for (int i = 0; i < 6; i++) { vel = ExplorationRules.ApproachVelocity(vel, Vector3.forward * ExplorationRules.WalkSpeed, 1f / 60f, true); t60 += 1f / 60f; }
    float startFraction = vel.magnitude / ExplorationRules.WalkSpeed;
    Vector3 run = Vector3.forward * ExplorationRules.RunSpeed; int stopFrames = 0;
    while (run.magnitude > 0.05f && stopFrames < 120) { run = ExplorationRules.ApproachVelocity(run, Vector3.zero, 1f / 60f, true); stopFrames++; }
    Vector3 rev = Vector3.forward * ExplorationRules.RunSpeed; float sideways = 0f;
    for (int i = 0; i < 60; i++) { rev = ExplorationRules.ApproachVelocity(rev, Vector3.back * ExplorationRules.RunSpeed, 1f / 60f, true); sideways = Mathf.Max(sideways, Mathf.Abs(rev.x)); }
    Vector3 settle = new Vector3(0.03f, 0f, 0.02f);
    settle = ExplorationRules.ApproachVelocity(settle, Vector3.zero, 1f / 60f, true);
    Check(startFraction > 0.7f && stopFrames / 60f <= 0.2f && sideways < 1e-5f && rev.z < -3.5f && settle == Vector3.zero,
        $"T03 response: {startFraction:P0} of walk speed after 100 ms; a run stops in {stopFrames / 60f:0.00} s; reversal passes straight through (no orbit); release settles to zero");
    Vector3 a30 = Vector3.zero, a120 = Vector3.zero;
    for (int i = 0; i < 30; i++) a30 = ExplorationRules.ApproachVelocity(a30, Vector3.forward * 3.8f, 1f / 30f, true);
    for (int i = 0; i < 120; i++) a120 = ExplorationRules.ApproachVelocity(a120, Vector3.forward * 3.8f, 1f / 120f, true);
    Vector3 air = Vector3.forward * 3.8f;
    for (int i = 0; i < 6; i++) air = ExplorationRules.ApproachVelocity(air, Vector3.right * 3.8f, 1f / 60f, false);
    Check(Mathf.Abs(a30.z - a120.z) < 0.02f && air.magnitude <= 3.8001f && air.z > 2.5f && air.x > 0.3f,
        "T03 response: acceleration is frame-rate independent; in the air momentum is kept with limited, predictable steering");
}
{
    // Distance matching: one shared phase, exact cycles per stride, steps on real contacts, no phase jump across blends.
    float ph = 0f, maxStep = 0f; int steps = 0, lefts = 0; float travelled = 0f;
    for (int i = 0; i < 600; i++)
    {
        float spd = Mathf.Lerp(ExplorationRules.WalkSpeed, ExplorationRules.RunSpeed, Mathf.Clamp01((i - 120) / 120f));
        float d = spd / 60f; travelled += d;
        float runW = ExplorationRules.RunWeight(spd);
        float before = ph;
        ph = ExplorationRules.AdvancePhase(ph, d, Mathf.Lerp(walkStride, runStride, runW));
        maxStep = Mathf.Max(maxStep, Mathf.Repeat(ph - before, 1f));
        if (ExplorationRules.CrossedContact(before, ph, out bool left)) { steps++; if (left) lefts++; }
    }
    Check(maxStep < 0.05f && steps > 0 && Mathf.Abs(lefts * 2 - steps) <= 1,
        $"T03 sync: walk->run ramp advances the shared phase smoothly (max {maxStep:0.000}/frame); {steps} heel contacts alternate left/right");
    Check(ExplorationRules.AdvancePhase(0.3f, 0f, 1f) == 0.3f && Mathf.Abs(ExplorationRules.AdvancePhase(0.9f, 0.2f * walkStride, walkStride) - 0.1f) < 1e-4f &&
          ExplorationRules.CrossedContact(0.2f, 0.23f, out bool l1) && l1 && ExplorationRules.CrossedContact(0.7f, 0.72f, out bool l2) && !l2 &&
          !ExplorationRules.CrossedContact(0.3f, 0.3f, out _) && !ExplorationRules.CrossedContact(0.25f, 0.7f, out _),
        "T03 sync: standing holds the phase; wrap is continuous; contacts at 0.22 (left) and 0.71 (right) fire once");
}
float facing = 90f;
for (int i = 0; i < 1000; i++) facing = ExplorationRules.FacingYaw(facing, Vector3.zero, 1f / 60f);
Check(facing == 90f, "T03 heading: stationary body never spins through idle frames");
float turn = ExplorationRules.FacingYaw(0f, Vector3.forward * 0.01f, 0.1f, Vector3.back);
Check(Mathf.Abs(turn) <= 60.001f && Mathf.Abs(turn) > 50f,
    "T03 heading: a reversal turns toward the wanted direction at once (bounded), even while still sliding forward");
for (int i = 0; i < 100; i++) turn = ExplorationRules.FacingYaw(turn, Vector3.back, 0.1f);
Check(Mathf.Abs(Mathf.DeltaAngle(turn, 180f)) < 1e-5f, "T03 heading: a completed turn settles in the direction of travel");
Check(body.Contains("idlePlayable.SetSpeed(0d)") && body.Contains("walkPlayable.SetSpeed(0d)") && body.Contains("runPlayable.SetSpeed(0d)") &&
      body.Contains("walkPlayable.SetTime(phase * walkClip.length)") && body.Contains("runPlayable.SetTime(phase * runClip.length)") &&
      body.Contains("ExplorationRules.CrossedContact(before, phase, out _)") && body.Contains("graph.Evaluate(0f);") && !body.Contains("graph.Evaluate(dt)") &&
      body.Contains("model.localPosition = fittedPosition;"),
    "T03 graph wiring: explicit clocks; walk and run share one phase; footsteps on contacts; one root authority (no root motion)");

// =====================================================================
// 9. Jump: explicit monotonic phases driven by physics
// =====================================================================
(List<ExplorationRules.JumpPhase> phases, bool clipMonotonic, float flight, float apexRise, bool landedWhileRising, float maxWeightStep) SimulateJump(float floorDrop, bool moving)
{
    var track = new ExplorationRules.JumpTrack();
    var seen = new List<ExplorationRules.JumpPhase>();
    float dt = 1f / 60f, jy = 0f, v = 0f, launch = ExplorationRules.JumpSpeed(ExplorationRules.JumpHeight, -9.81f), windup = 0f, air = 0f, apexY = 0f;
    bool airborne = false, launched = false, monotonic = true, badLand = false; float lastClip = -1f, lastW = 0f, maxW = 0f;
    for (int i = 0; i < 240; i++)
    {
        if (!launched) { windup += dt; if (windup >= ExplorationRules.JumpWindup) { launched = true; airborne = true; v = launch; } }
        else if (airborne) { v -= 9.81f * dt; jy += v * dt; air += dt; apexY = Mathf.Max(apexY, jy); if (jy <= -floorDrop) { jy = -floorDrop; airborne = false; } }
        float below = airborne && v < 0f ? jy + floorDrop : -1f;
        float vyForTrack = airborne || !launched ? v : v; // contact frame reports the impact speed
        track.Step(launched ? -1f : windup, airborne, vyForTrack, launched ? launch : 0f, below, moving, dt);
        if (track.Phase == ExplorationRules.JumpPhase.Land && v > 0f) badLand = true;
        if (seen.Count == 0 || seen[^1] != track.Phase) seen.Add(track.Phase);
        if (track.Phase != ExplorationRules.JumpPhase.None) { if (track.ClipTime < lastClip - 1e-5f) monotonic = false; lastClip = track.ClipTime; }
        maxW = Mathf.Max(maxW, Mathf.Abs(track.Weight - lastW)); lastW = track.Weight;
        if (!airborne && launched) v = 0f;
        if (launched && !airborne && track.Phase == ExplorationRules.JumpPhase.None) break;
    }
    return (seen, monotonic, air, apexY, badLand, maxW);
}
foreach (bool moving in new[] { false, true })
{
    var j = SimulateJump(0f, moving);
    Check(j.phases.SequenceEqual(new[] { ExplorationRules.JumpPhase.Windup, ExplorationRules.JumpPhase.Rise, ExplorationRules.JumpPhase.Fall, ExplorationRules.JumpPhase.Land, ExplorationRules.JumpPhase.None }) &&
          j.clipMonotonic && !j.landedWhileRising && j.maxWeightStep <= 16f / 60f + 1e-4f,
        $"T03 jump ({(moving ? "moving" : "standing")}): windup -> rise -> fall -> land -> done, clip time never runs backward, no landing while rising, weight eases");
    Check(j.apexRise / H >= 0.25f && j.apexRise / H <= 0.40f && j.flight >= 0.55f && j.flight <= 0.80f,
        $"T03 jump feel: apex {j.apexRise:0.00} m ({j.apexRise / H:0.00} standing heights), {j.flight:0.00} s of flight");
}
{
    var off = SimulateJump(0.6f, true);
    Check(off.phases.Contains(ExplorationRules.JumpPhase.Land) && off.clipMonotonic, "T03 jump: a hop down a ledge still lands once, monotonic");
    var ledge = new ExplorationRules.JumpTrack();
    ledge.Step(-1f, true, -0.5f, 0f, -1f, true, 1f / 60f);
    var flicker = new ExplorationRules.JumpTrack();
    for (int i = 0; i < 30; i++) flicker.Step(-1f, false, 0f, 0f, -1f, false, 1f / 60f);
    var cancelled = new ExplorationRules.JumpTrack();
    cancelled.Step(0.02f, false, 0f, 0f, -1f, false, 1f / 60f); cancelled.Step(-1f, false, 0f, 0f, -1f, false, 1f / 60f);
    Check(ledge.Phase == ExplorationRules.JumpPhase.Fall && ledge.ClipTime >= ExplorationRules.JumpClipApex &&
          flicker.Phase == ExplorationRules.JumpPhase.None && flicker.Weight == 0f && cancelled.Phase == ExplorationRules.JumpPhase.None,
        "T03 jump: walking off an edge starts at the apex pose (no mid-air anticipation); ground flicker never plays a landing; a held windup cancels cleanly");
    var noProbe = new ExplorationRules.JumpTrack { Phase = ExplorationRules.JumpPhase.Fall };
    for (int i = 0; i < 300; i++) noProbe.Step(-1f, true, -3f, 1f, -1f, false, 1f / 60f);
    Check(noProbe.Phase == ExplorationRules.JumpPhase.Fall && noProbe.ClipTime < ExplorationRules.JumpClipContact,
        "T03 jump: a long fall without a reliable probe prepares the legs but never plays contact before real contact");
}
Check(ExplorationRules.CoyoteTime >= 0.08f && ExplorationRules.CoyoteTime <= 0.12f && ExplorationRules.JumpBuffer >= 0.10f && ExplorationRules.JumpBuffer <= 0.15f &&
      ExplorationRules.CanJump(true, false, 0.08f, 5f, false) && !ExplorationRules.CanJump(true, false, 0.08f, 5f, true) &&
      !ExplorationRules.CanJump(true, false, 0.11f, 5f, false),
    "T03 jump timing: 0.10 s coyote after walking off an edge (not after a jump: no mid-air second hop); 0.13 s press buffer");
string tryJump = Body(pm, "private void TryJump()");
Check(tryJump.Contains("Time.time - jumpRequestedAt > ExplorationRules.JumpBuffer") && tryJump.IndexOf("jumpRequested = false;\n        lastJumpAt", StringComparison.Ordinal) > 0 &&
      tryJump.Contains("ExplorationRules.CanJump(true, grounded, Time.time - lastGroundedAt, Time.time - lastJumpAt, jumpedSinceGrounded)") &&
      Body(pm, "private void OnDisable()").Contains("windupStartedAt = -1f;") && Body(pm, "private void OnDisable()").Contains("stick = 0f;"),
    "T03 jump wiring: a buffered press is consumed exactly once; holds/focus loss cancel a pending windup and the stick");
Check(Body(pm, "private void SenseGround()").Contains("if (h.normal.y < 0.55f) continue;") &&
      Body(pm, "private void SenseGround()").Contains("grounded = hit && rb.linearVelocity.y <= 0.1f && !(jumping && Time.time - lastJumpAt < 0.1f);"),
    "T03 ground: walls/steep faces are not floor; the ascending body cannot be re-grounded by a cast that still reaches the floor");

// =====================================================================
// 10. Jacket: shirt stays under the coat (import-time, derived copies only)
// =====================================================================
{
    // Synthetic layers: a coat cylinder (radius 1) and a shirt 1 mm inside it, plus one bulged vertex poking out
    // and an uncovered shirt vertex well below the coat's hem.
    var outer = new List<Vector3>(); var tris = new List<int>();
    int seg = 24;
    for (int i = 0; i < seg; i++)
    {
        float a = i * Mathf.PI * 2f / seg;
        outer.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))); outer.Add(new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a)));
    }
    for (int i = 0; i < seg; i++)
    {
        int a0 = 2 * i, a1 = 2 * i + 1, b0 = 2 * ((i + 1) % seg), b1 = 2 * ((i + 1) % seg) + 1;
        // Unity front face: Cross(b - a, c - a) points away from the axis.
        tris.AddRange(new[] { a0, a1, b0, b0, a1, b1 });
    }
    Vector3 n0 = Vector3.Cross(outer[tris[1]] - outer[tris[0]], outer[tris[2]] - outer[tris[0]]);
    var inner = new[] { new Vector3(0.999f, 0.5f, 0f), new Vector3(1.004f, 0.5f, 0.05f), new Vector3(0f, 0.5f, 0.999f), new Vector3(0.95f, -0.5f, 0f), new Vector3(0.2f, 0.5f, 0f) };
    var copy = (Vector3[])inner.Clone();
    int moved = ElijahClothingInset.Apply(copy, outer.ToArray(), tris.ToArray());
    float Radial(Vector3 p) => new Vector2(p.x, p.z).magnitude;
    Check(Vector3.Dot(n0, new Vector3(outer[0].x, 0f, outer[0].z)) > 0f && moved == 3 &&
          Radial(copy[0]) <= 0.985f && Radial(copy[1]) <= 0.985f && Radial(copy[2]) <= 0.985f && copy[3] == inner[3] && copy[4] == inner[4],
        "T03 jacket: touching or bulging shirt vertices sit 2 cm (model units) under the coat; uncovered and deep vertices are untouched");
    string treat = Src("Assets/Editor/ElijahModelTreatment.cs");
    Check(treat.Contains("new HashSet<string> { \"coat\", \"head\", \"hair\", \"ears\" }") && treat.Contains("if (Refined.Contains(skin.name)) T02PilotTreatment.RefineImportedMesh(") &&
          treat.Contains("ElijahClothingInset.Apply(inner, outer, coat.sharedMesh.triangles)") && treat.Contains("public override uint GetVersion() => 4;") &&
          treat.Contains("assetPath == \"Assets/Resources/Player/Elijah_Exploration_Smooth.fbx\""),
        "T03 jacket: only outer layers are refined; the shirt is inset; scoped to Elijah's derived copies; version bump re-imports them");
}

Console.WriteLine($"T03ExplorationRegression: {checks - fails}/{checks} checks passed");
return fails == 0 ? 0 : 1;
