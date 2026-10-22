using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Settings board regression: settings live in the 3D Menu (no Settings
// scene). The 3D board keeps the old SettingsPanel audio behaviour, drops in
// front of the main menu camera, hides the menu buttons while open and brings
// them back on close, and every way into settings ends up here. In gameplay
// scenes InGameSettings shows the same board from the HUD settings button and
// pauses the game while it is open. Source and scene checks only; Play Mode
// checks stay manual.

int checks = 0, failures = 0;
void Check(bool ok, string message)
{
    checks++;
    if (ok) Console.WriteLine("PASS: " + message);
    else { failures++; Console.WriteLine("FAIL: " + message); }
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string PathOf(params string[] parts) => Path.Combine(root, Path.Combine(parts));
string Read(params string[] parts) => File.ReadAllText(PathOf(parts));
string board = Read("Assets", "Scripts", "Settings", "SettingsBoard3D.cs");
string toggle = Read("Assets", "Scripts", "Settings", "SettingsToggle3D.cs");
string menuScene = Read("Assets", "Scenes", "3D Menu.unity");
string mainMenu = Read("Assets", "Scripts", "MainMenu.cs");
string mainMenuUi = Read("Assets", "Scripts", "UI", "MainMenuUI.cs");
string menuEntry = Read("Assets", "Scripts", "UI", "MenuEntry.cs");

int Index(string text, string needle, int from = 0) => text.IndexOf(needle, from, StringComparison.Ordinal);

// ---------- same audio behaviour as SettingsUI ----------
int music = Index(board, "private void OnMusicChanged");
Check(music > 0 && Index(board, "PlaySelectSFX", music) < Index(board, "SetMusicEnabled(enabled)", music),
    "music: the select sound plays before the music changes (as SettingsUI)");
int sfx = Index(board, "private void OnSfxChanged");
int on = Index(board, "if (enabled)", sfx);
Check(sfx > 0 && Index(board, "SetSFXEnabled(true)", on) < Index(board, "PlaySelectSFX", on), "SFX on: enabled before the click so it is heard");
int off = Index(board, "else", on);
Check(Index(board, "PlaySelectSFX", off) < Index(board, "SetSFXEnabled(false)", off), "SFX off: click first, then disabled");
Check(board.Contains("IsMusicEnabled()") && board.Contains("IsSFXEnabled()"), "switches show the saved AudioManager values when opened");

// ---------- no Settings scene ----------
Check(!File.Exists(PathOf("Assets", "Scenes", "Settings.unity")), "there is no separate Settings scene");
Check(!Read("ProjectSettings", "EditorBuildSettings.asset").Contains("Settings.unity"), "Settings.unity is not in Build Settings");
string scripts = string.Join("\n", Directory.GetFiles(PathOf("Assets", "Scripts"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
Check(!Regex.IsMatch(scripts, "LoadScene\\(\\s*\"Settings\"") && !Regex.IsMatch(scripts, "settingsSceneName"), "no code loads a Settings scene any more");
Check(!board.Contains("SceneManager.LoadScene"), "BACK closes the board in place (no scene load)");

// ---------- opening from the main menu ----------
Check(mainMenu.Contains("Press(note, OpenSettings)") && mainMenu.Contains("settingsBoard.Open()"),
    "the 3D settings button opens the board after its shared press animation");
Check(Index(mainMenu, "settingsBoard.IsOpen || settingsBoard.MenuButtonsAnimating)) return;") > 0 && Index(mainMenu, "settingsBoard.IsOpen || settingsBoard.MenuButtonsAnimating)) return;") < Index(mainMenu, "Input.touchCount"),
    "menu taps are ignored while settings are open");
Check(mainMenuUi.Contains("FindAnyObjectByType<SettingsBoard3D>()") && mainMenuUi.Contains("board.Open()"),
    "the UI-wired settings button (MainMenuUI) opens the same board");
Check(mainMenuUi.Contains("MenuEntry.OpenSettings = true") && mainMenuUi.Contains("LoadScene(MenuEntry.MenuSceneName)"),
    "from other scenes, settings opens the 3D Menu with the board open");
Check(menuEntry.Contains("ConsumeOpenSettings") && board.Contains("pendingOpen = !inGame && MenuEntry.ConsumeOpenSettings()"),
    "the 3D Menu opens settings on arrival when asked (the in-game board leaves that request alone)");
Check(board.Contains("cameraManager.IsTransitioning || cameraManager.IsLevelSelectorVisible"),
    "settings only open on the main menu view (not mid-transition or on the level select)");

// ---------- hide / restore the menu buttons ----------
Check(Index(board, "HideMenuButtons();", Index(board, "public bool Open()")) > 0, "opening hides the menu buttons");
Check(Index(board, "RestoreMenuButtons();", Index(board, "private void FinishClose()")) > 0, "closing brings the menu buttons back");
Check(board.Contains("if (entry.Key == null || !entry.Value) continue;") && board.Contains("if (!wasActive) continue;"),
    "buttons return to the state they had before (an inactive one stays inactive)");

// ---------- pop animation for the menu buttons ----------
Check(board.Contains("StartPop(button, showing: false") && board.Contains("StartPop(entry.Key, showing: true"),
    "buttons pop out when settings open and pop back in when they close");
Check(board.Contains("PopStagger") && board.Contains("order++ * PopStagger"), "the pops are staggered one after another");
int tick = Index(board, "TickButtonPops(Time.unscaledDeltaTime);");
Check(tick > 0 && tick < Index(board, "if (!open || root == null) return;"), "pops keep animating after the board has closed");
Check(board.Contains("HudTheme.EaseOutBack(t)") && board.Contains("1.12f"), "pop-in overshoots (ease-out-back); pop-out swells to 112% then shrinks");
Check(Index(board, "pop.Button.SetActive(false);") > Index(board, "private void TickButtonPops") && board.Contains("pop.Button.transform.localScale = size;"),
    "a popped-out button is switched off only after its animation, and its original size is restored");
Check(board.Contains("buttonScales[button] = button.transform.localScale"), "each button keeps its own original size");
foreach (string button in new[] { "play_button", "tutorial_button", "settings_button" })
    Check(board.Contains("\"" + button + "\"") && menuScene.Contains("value: " + button), $"'{button}' is hidden while open and exists in 3D Menu");

// ---------- same angle as the main menu ----------
string guid = Regex.Match(Read("Assets", "Scripts", "Settings", "SettingsBoard3D.cs.meta"), @"guid: (\w+)").Groups[1].Value;
Check(guid.Length == 32 && menuScene.Contains("guid: " + guid), "3D Menu has the SettingsBoard3D component");
Check(Regex.IsMatch(menuScene, @"Assembly-CSharp::SettingsBoard3D\s+viewCamera: \{fileID: 336904156\}\s+cameraManager: \{fileID: 1431406759\}"),
    "the board drops in front of the main menu camera (ToggleCamera.mainCam)");
Check(Regex.IsMatch(menuScene, @"mainCam: \{fileID: 336904156\}"), "that camera is the menu's main camera");
Check(Regex.IsMatch(menuScene, @"Assembly-CSharp::MainMenu\s+cameraManager: \{fileID: 1431406759\}\s+settingsBoard: \{fileID: 2300000012\}"),
    "MainMenu points at the board");
Check(Regex.IsMatch(menuScene, @"m_Roots:[\s\S]*\{fileID: 2300000011\}"), "the board object is a scene root");
Check(board.Contains("root.SetPositionAndRotation(cam.position + cam.forward * Distance, cam.rotation)"), "the board faces the camera, at its angle");

// ---------- 3D, touch, motion ----------
Check(!board.Contains("TextMeshProUGUI") && !toggle.Contains("TextMeshProUGUI"), "board lettering is world TextMeshPro, not UI text");
Check(board.Contains("CameraRenderType.Overlay") && board.Contains("cullingMask &= ~(1 << BoardLayer)"), "the board draws on its own overlay camera");
Check(board.Contains("1 << BoardLayer, QueryTriggerInteraction"), "touches only hit the board");
Check(toggle.Contains("AddHitBox(transform") && toggle.Contains("rowSize.x, rowSize.y"), "the whole row is the switch's touch area");
string wording = board + toggle;
Check(!Regex.IsMatch(wording, "\"[^\"\\r\\n]*\\b([Cc]lick|[Mm]ouse|WASD)\\b[^\"\\r\\n]*\""), "player-facing text has no click / mouse wording");
Check(toggle.Contains("EaseOutBack") && board.Contains("DropDuration") && board.Contains("LeaveDuration"), "motion: switch slide, drop-in and lift-away");
Check(board.Contains("escapeKey.wasPressedThisFrame"), "Escape / Android back closes settings");

// ================= IN-GAME SETTINGS =================
string inGame = Read("Assets", "Scripts", "Settings", "InGameSettings.cs");
string settingsUi = Read("Assets", "Scripts", "Settings", "SettingsUI.cs");
string placeholder = Read("Assets", "Scripts", "UI", "HUD", "SettingsButtonPlaceholder.cs");
string guard = Read("Assets", "Scripts", "Mobile", "MobileInputGuard.cs");
string hudRuntime = Read("Assets", "Scripts", "UI", "HUD", "OfficeHudRuntime.cs");
string minimap = Read("Assets", "Scripts", "UI", "OfficeMinimap.cs");
string level1 = Read("Assets", "Scenes", "Office_Level1.unity");
string level2 = Read("Assets", "Scenes", "Office_Level2.unity");
string Body(string text, string signature)
{
    int start = Index(text, signature);
    if (start < 0) return "";
    int next = text.IndexOf("\n    private ", start + signature.Length, StringComparison.Ordinal);
    int nextPublic = text.IndexOf("\n    public ", start + signature.Length, StringComparison.Ordinal);
    if (next < 0 || (nextPublic >= 0 && nextPublic < next)) next = nextPublic;
    return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
}

// ---------- same board, no visual or behaviour changes ----------
Check(inGame.Contains("AddComponent<SettingsBoard3D>()") && inGame.Contains("board.ConfigureInGame()"),
    "in-game settings use the very same SettingsBoard3D (one design, one set of rows and switches)");
string configure = Body(board, "public void ConfigureInGame()");
Check(configure.Contains("inGame = true") && configure.Contains("cameraManager = null") && configure.Contains("hideWhileOpen = new string[0]") &&
      !configure.Contains("Audio") && !configure.Contains("Color") && !configure.Contains("Size"),
    "in-game mode only changes the camera, menu manager and hidden objects (no look or audio changes)");
Check(!Regex.IsMatch(inGame, @"AudioManager\.Instance\.Set"), "audio parity: in-game settings change audio only through the board's SettingsUI-matching handlers");
Check(!Regex.IsMatch(menuScene, @"Assembly-CSharp::SettingsBoard3D[\s\S]{0,400}inGame: 1"), "the 3D Menu board is not in in-game mode");
Check(Body(board, "public bool Open()").Contains("if (inGame) FollowGameplayCamera();"),
    "in-game, the board drops in front of the gameplay camera live at open time");
Check(board.Contains("CameraController.Instance") && board.Contains("controller.MainCamera") && board.Contains(": Camera.main"),
    "the gameplay camera is CameraController's camera, else Camera.main");
Check(board.Contains("oldData.cameraStack.Remove(overlayCamera)") && board.Contains("baseData.cameraStack.Add(overlayCamera)"),
    "the overlay camera moves to the gameplay camera's stack (last, so it draws on top)");
Check(board.Contains("hideWhileOpen.Length == 0) return;"), "an empty hide list skips the scene scan");
Check(Body(board, "public bool Open()").Contains("Opened?.Invoke()") && Body(board, "private void FinishClose()").Contains("Closed?.Invoke()"),
    "the board reports when it opens and when it has fully closed");

// ---------- pause configuration ----------
string configPath = PathOf("Assets", "Resources", "Settings", "in_game_settings.json");
Check(File.Exists(configPath) && File.Exists(configPath + ".meta") && File.Exists(PathOf("Assets", "Resources", "Settings.meta")),
    "the pause config is a Resources JSON with .meta files");
Check(inGame.Contains("ConfigResource = \"Settings/in_game_settings\""), "InGameSettings reads Resources/Settings/in_game_settings.json");
using (var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath)))
{
    var cfg = json.RootElement;
    var scenes = cfg.GetProperty("scenes").EnumerateArray().Select(e => e.GetString()).ToArray();
    Check(scenes.Contains("Office_Level1") && scenes.Contains("Office_Level2"), "config: Office_Level1 and Office_Level2 get the in-game board");
    Check(scenes.All(s => File.Exists(PathOf("Assets", "Scenes", s + ".unity"))), "config: every listed scene exists");
    Check(cfg.GetProperty("pauseGameWhenOpen").GetBoolean(), "config: pausing is on by default");
    Check(cfg.GetProperty("pauseMode").GetString() == "FreezeTime", "config: the default pause mode is FreezeTime");
    Check(cfg.GetProperty("hideHudWhileOpen").GetBoolean(), "config: the HUD hides while settings are open");
    foreach (string field in new[] { "scenes", "pauseGameWhenOpen", "pauseMode", "hideHudWhileOpen" })
        Check(Regex.IsMatch(inGame, @"public [\w\[\]]+ " + field + @" = "), $"config field '{field}' is read by InGameSettings.Config");
}
Check(inGame.Contains("[SerializeField] private bool pauseGameWhenOpen") && inGame.Contains("[SerializeField] private PauseMode pauseMode") &&
      inGame.Contains("[SerializeField] private bool useResourcesConfig"),
    "inspector fields: pauseGameWhenOpen, pauseMode, and a switch between JSON and inspector values");
Check(Regex.IsMatch(inGame, @"if \(useResourcesConfig\)\s*\{[\s\S]*pauseGameWhenOpen = config\.pauseGameWhenOpen;[\s\S]*pauseMode = ParsePauseMode\(config\.pauseMode\);"),
    "the JSON values apply when useResourcesConfig is on");
Check(Body(inGame, "public static PauseMode ParsePauseMode").Contains("\"StoryOnly\", StringComparison.OrdinalIgnoreCase") &&
      Body(inGame, "public static PauseMode ParsePauseMode").Contains(": PauseMode.FreezeTime"),
    "pauseMode: StoryOnly by name, anything else falls back to FreezeTime");

// ---------- pause on open, resume on close ----------
string pause = Body(inGame, "private void Pause()");
string resume = Body(inGame, "private void Resume()");
Check(pause.Contains("if (!pauseGameWhenOpen || paused) return;"), "pause off: opening settings leaves the game running");
Check(Regex.IsMatch(pause, @"FreezeTime\)\s*\{\s*savedTimeScale = Time\.timeScale;\s*Time\.timeScale = 0f;"),
    "FreezeTime: the old time scale is saved, then time stops (NPCs, physics, timers, office hours)");
Check(resume.Contains("Time.timeScale = savedTimeScale;"), "resume restores the saved time scale (not a hard-coded 1)");
Check(pause.Contains("machine.Pause();") && pause.Contains("!machine.IsPaused") && resume.Contains("if (pausedStateMachine)"),
    "StoryOnly: pauses the game state machine, and only resumes it if settings paused it");
Check(!pause.Contains("machine.Pause();") || Index(pause, "machine.Pause();") > Index(pause, "else"),
    "FreezeTime never pauses the state machine (no ChangeState is dropped)");
Check(Body(inGame, "private void HandleOpened()").Contains("Pause();") && Body(inGame, "private void HandleClosed()").Contains("Resume();"),
    "the game pauses when the board opens and resumes when BACK / Escape has closed it");
Check(inGame.Contains("board.Opened += HandleOpened;") && inGame.Contains("board.Closed += HandleClosed;"), "pause follows the board's open / closed events");
Check(Body(inGame, "private void OnDestroy()").Contains("Resume();"), "leaving the scene while open never leaves the game frozen");
Check(!board.Contains("Time.deltaTime") && board.Contains("Time.unscaledDeltaTime") && toggle.Contains("Time.unscaledDeltaTime") &&
      !toggle.Contains("Time.deltaTime"), "the board and switches animate on unscaled time, so they work while time is frozen");
string officeHours = Read("Assets", "Scripts", "UI", "HUD", "OfficeHours.cs");
Check(officeHours.Contains("Time.timeScale <= 0f") && officeHours.Contains("GameStateMachine.Instance.IsPaused"),
    "office hours stop under both pause modes (time scale 0 or state machine paused)");

// ---------- world input and HUD while open ----------
Check(Body(inGame, "private void HandleOpened()").Contains("PlayerControlHold.Acquire(this)") && inGame.Contains("PlayerControlHold.Release(this)"),
    "no walking, looking or interacting while open (PlayerControlHold), released on close");
Check(inGame.Contains("if (holdingControls) PlayerControlHold.Refresh();"), "the hold is refreshed every frame (applies after a conversation ends)");
Check(guard.Contains("if (InGameSettings.IsOpen) return true;") && guard.Contains("|| InGameSettings.IsOpen) return false;"),
    "world taps are blocked while settings are open (MobileInputGuard)");
Check(Body(inGame, "private void HandleClosed()").Contains("MobileInputGuard.SuppressWorldTapsThisFrame()"), "the closing tap never reaches the world");
Check(Body(inGame, "private void HideHud()").Contains("canvas.renderMode == RenderMode.WorldSpace") &&
      Body(inGame, "private void HideHud()").Contains("blocksRaycasts = false") && Body(inGame, "private void ShowHud()").Contains("Destroy(entry.Group)"),
    "the screen-space HUD hides (alpha 0, no touches) and is restored exactly on close");
Check(settingsUi.Contains("|| InGameSettings.IsOpen;"), "SettingsUI.IsVisible is true while the in-game board is open (HUD readers keep working)");
Check(hudRuntime.Contains("!InGameSettings.IsOpen && (settings == null || !settings.IsVisible)") && minimap.Contains("!InGameSettings.IsOpen && (settings == null || !settings.IsVisible)"),
    "the Office HUD and minimap hide while the in-game board is open");
Check(hudRuntime.Contains("!HudRewardScreen.AnyOpen &&\r\n               !InGameSettings.IsOpen;") || hudRuntime.Contains("!HudRewardScreen.AnyOpen &&\n               !InGameSettings.IsOpen;"),
    "the HUD settings / icon buttons fade out while settings are open");

// ---------- opening from the gameplay HUD ----------
string settingsUiGuid = Regex.Match(Read("Assets", "Scripts", "Settings", "SettingsUI.cs.meta"), @"guid: (\w+)").Groups[1].Value;
Check(Regex.IsMatch(level1, @"m_TargetAssemblyTypeName: SettingsUI, Assembly-CSharp\s+m_MethodName: OpenSettings"),
    "Office_Level1: the HUD settings button calls SettingsUI.OpenSettings");
Check(Regex.IsMatch(Body(settingsUi, "public void OpenSettings()"), @"\{\s*//[^\n]*\n\s*if \(InGameSettings\.TryOpen\(\)\) return;"),
    "SettingsUI.OpenSettings opens the 3D board first (the 2D panel never shows in-game)");
// Office_Level2 is a one-time copy of Office_Level1 (Level 2 initiative, SCENE_REUSE.md),
// so its HUD settings button is Level 1's; the old scene (Office_Level2_OLD) used the placeholder.
Check(Regex.IsMatch(level2, @"m_TargetAssemblyTypeName: SettingsUI, Assembly-CSharp\s+m_MethodName: OpenSettings"),
    "Office_Level2: the HUD settings button calls SettingsUI.OpenSettings (copied from Office_Level1)");
Check(placeholder.Contains("if (InGameSettings.TryOpen()) return;"), "SettingsButtonPlaceholder (Office_Level2_OLD) opens the in-game board");
Check(Index(mainMenuUi, "InGameSettings.TryOpen()") > 0 && Index(mainMenuUi, "InGameSettings.TryOpen()") < Index(mainMenuUi, "FindAnyObjectByType<SettingsBoard3D>()"),
    "MainMenuUI.OnSettingsPressed opens the in-game board in gameplay scenes (no trip to the 3D Menu)");
Check(Body(inGame, "private void Start()").Contains("legacy.CloseSettings();"), "the old 2D SettingsPanel is kept closed at runtime");
Check(File.Exists(PathOf("Assets", "Prefabs", "Settings", "SettingsPanel.prefab")) && level1.Contains("guid: " + settingsUiGuid),
    "the legacy SettingsPanel prefab and Office_Level1's SettingsUI are kept (never deleted)");
Check(inGame.Contains("[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]") && inGame.Contains("SceneManager.sceneLoaded += HandleSceneLoaded"),
    "the in-game board is built at runtime for each configured scene (no scene edits)");
Check(inGame.Contains("GetComponentInChildren<InGameSettings>(true) != null) return;"), "a scene that already has InGameSettings is not given a second one");

// ---------- files ----------
string inGameGuid = Regex.Match(Read("Assets", "Scripts", "Settings", "InGameSettings.cs.meta"), @"guid: (\w+)").Groups[1].Value;
int guidUses = Directory.GetFiles(PathOf("Assets"), "*.meta", SearchOption.AllDirectories).Count(f => File.ReadAllText(f).Contains("guid: " + inGameGuid));
Check(inGameGuid.Length == 32 && guidUses == 1, "InGameSettings.cs.meta has its own fresh GUID");
foreach (string file in new[] { "InGameSettings.cs", "SettingsBoard3D.cs" })
{
    string raw = Read("Assets", "Scripts", "Settings", file);
    Check(raw.Contains("\r\n") && !Regex.IsMatch(raw, "(?<!\r)\n"), $"{file} uses CRLF line endings");
}
Check(!Regex.IsMatch(inGame, "\"[^\"\\r\\n]*\\b([Cc]lick|[Mm]ouse|WASD)\\b[^\"\\r\\n]*\""), "in-game settings text has no click / mouse wording");

// ---------- confirmed settings actions ----------
string progressManager = Read("Assets", "Scripts", "RewardsSystem", "PlayerProgressManager.cs");
string checkpointer = Read("Assets", "Scripts", "Level2", "Level2Checkpointer.cs");
Check(board.Contains("if (inGame) primaryActionButton = AddAction(ReplayLabel") && !board.Contains("RESET PROGRESS") && board.Contains("inGame ? \"BACK TO MENU\" : \"QUIT GAME\""), "menu and gameplay expose the requested actions");
Check(board.Contains("cancelButton.Tapped += _ => CancelConfirmation()") && board.Contains("confirmButton.Tapped += _ => ConfirmAction()"), "confirmation has separate cancel and confirm actions");
Check(Body(board, "private void GoBack()").Contains("CancelConfirmation(); return;"), "Android back cancels a confirmation before resuming gameplay");
Check(Body(board, "private CareerBoardTarget RaycastTarget").Contains("!target.transform.IsChildOf(confirmation)"), "confirmation blocks touches to settings behind it");
Check(board.Contains("Physics.SyncTransforms()"), "animated touch targets stay aligned while physics is paused");
Check(board.Contains("optionsReady = ready && !modal && !actionBusy"), "audio and navigation are disabled behind confirmation");
Check(board.Contains("confirmationElapsed > 0.22f") && board.Contains("if (actionBusy || pendingAction == SettingsAction.None) return;"), "opening tap and repeated confirmation cannot run actions twice");
Check(board.Contains("InGameSettings.Active.IsPaused") && board.Contains("TutorialSettings ? \"II  TUTORIAL PAUSED\" : \"II  LEVEL PAUSED\""), "pause indicator reflects the actual pause state");
Check(board.Contains("inGame ? \"RESUME\" : \"<  BACK\""), "gameplay clearly labels the resume button");
Check(board.Contains("Application.Quit()") && board.Contains("UnityEditor.EditorApplication.isPlaying = false"), "quit closes a build or stops editor play mode");
Check(Body(inGame, "public bool LeaveLevel").Contains("Application.CanStreamedLevelBeLoaded(destination)"), "scene exits validate the destination before discarding an attempt");
Check(Body(inGame, "public bool LeaveLevel").Contains("!TrainingSession.IsPracticing"), "practice replay leaves the career checkpoint alone");
Check(checkpointer.Contains("!PlayerProgressManager.Instance.ClearCheckpoint(LevelId)"), "replay clears the resumable attempt before reloading");
Check(checkpointer.Contains("if (settingsExit || !UsesCheckpoint) return true;"), "outgoing level cannot autosave its discarded attempt");
Check(progressManager.Contains("store.Reset(encoded)") && progressManager.Contains("pendingResult = null;") && progressManager.Contains("lastAppliedRunId = null;"), "fresh start replaces stored and pending progress");
Check(progressManager.Contains("PlayerPrefs.DeleteKey(\"TaxVenture.Guide.Level_01.finished\")") && progressManager.Contains("PlayerPrefs.DeleteKey(\"TaxVenture.Guide.Level_02.finished\")") && !progressManager.Contains("PlayerPrefs.DeleteAll"), "fresh start resets tutorials while keeping audio preferences");

Check(checkpointer.Contains("if (!SaveCurrentCheckpoint()) return false;"), "back to menu stays paused in settings if checkpoint saving fails");

// Presentation and scene transitions (engine rendering remains a manual check).
string sceneFade = Read("Assets", "Scripts", "UI", "HUD", "SceneFade.cs");
Check(Body(inGame, "public bool LeaveLevel").Contains("SceneFade.FadeOutThenLoad(destination, fadeInDestination: destination != Cutscene01Runtime.SceneName)"), "replay and leave use fade out plus destination fade in");
Check(Body(inGame, "private void HideHud()").Contains("canvas.GetComponent<SceneFade>() != null"), "hiding settings HUD never hides or restores the transition curtain");
Check(Body(sceneFade, "private IEnumerator FadeAndLoad").IndexOf("yield return Fade(", StringComparison.Ordinal) < Body(sceneFade, "private IEnumerator FadeAndLoad").IndexOf("SceneManager.LoadScene", StringComparison.Ordinal), "scene loading waits until fade to black finishes");
Check(Body(sceneFade, "private IEnumerator Fade(").Contains("Time.unscaledDeltaTime"), "fades progress while settings hold timeScale at zero");
Check(Body(sceneFade, "private static void HandleSceneLoaded").Contains("EnsureForScene(scene, requested)") && sceneFade.Contains("(!force && !OfficeLevelProfile.Allows"), "requested fade in also covers the menu destination");
Check(board.Contains("if (success && (pendingAction == SettingsAction.Replay || pendingAction == SettingsAction.Menu ||"), "successful level exits (and a confirmed New Game) keep settings locked and paused until scene teardown");
Check(mainMenu.Contains("MenuEntry.IsStoryLaunching || SceneFade.BlocksInput ||"), "black curtain also blocks the menu's direct touch raycasts");
Check(board.Contains("new Vector3(x, -178f, -26f)") && board.Contains("new Vector2(340f, 88f)") && !board.Contains("new Vector2(600f, 76f)"), "secondary settings actions use compact side-by-side buttons");

Check(inGame.Contains("replay && !TrainingSession.IsPracticing && gameObject.scene.name == \"Office_Level1\"") && inGame.Contains("replayFirstStory ? Cutscene01Runtime.SceneName"), "Level 1 replay includes its skippable intro; practice drills still reload directly");

// Fresh-menu visibility and tutorial wording follow the same first-completion gate.
Check(board.Contains("bool showPrimary = inGame;") && board.Contains("primaryActionButton.gameObject.SetActive(showPrimary);"), "settings omit Reset Progress in every menu state while retaining in-game replay");
Check(board.Contains("new Vector3(showPrimary ? 184f : 0f, -178f, -26f)"), "Quit is centred when fresh-menu Reset is hidden");
Check(board.Contains("bool newGame = action == SettingsAction.NewGame;") && board.Contains("if (newGame && !MenuEntry.HasCareerAccess) return;"), "New Game remains gated by saved completion after removal of settings Reset");
Check(board.Contains("TutorialSettings ? \"REPLAY TUTORIAL\" : \"REPLAY LEVEL\"") && board.Contains("TutorialSettings ? \"RESUME TUTORIAL\" : \"RESUME\""), "practice settings label replay and resume as tutorial actions");
Check(board.Contains("REPLAY TUTORIAL?") && board.Contains("LEAVE TUTORIAL?") && board.Contains("Start this tutorial again?"), "tutorial confirmation copy matches the actual practice session");
Check(mainMenu.Contains("ProgressChanged += RefreshMenu") && mainMenu.Contains("entry.Key.SetActive(MenuEntry.HasCareerAccess && entry.Value)"), "fresh menu gates tutorial/side-quest objects and refreshes after resetting progress");
Check(Body(board, "private void RestoreMenuButtons()").Contains("!MenuEntry.HasCareerAccess"), "closing settings after reset cannot bring hidden fresh-menu buttons back");
Check(Body(inGame, "public bool LeaveLevel").Contains("MenuEntry.OpenLevelSelect = false;") && Body(inGame, "public bool LeaveLevel").Contains("MenuEntry.OpenSettings = false;"), "leaving an unfinished level explicitly requests the main menu");
Check(Body(menuEntry, "public static bool ConsumeOpenLevelSelect()").Contains("return open && HasCareerAccess;"), "stale progress-view requests are rejected until Level 1 is finished");

string newGameOpen = Body(board, "public bool OpenNewGameConfirmation()");
Check(newGameOpen.Contains("confirmation.SetParent(root, false)") && newGameOpen.Contains("board.gameObject.SetActive(false)") &&
      !newGameOpen.Contains("!Open()") && board.Contains("confirmation.localScale = Vector3.one * HudTheme.EaseOutBack"),
    "New Game presents only the existing animated confirmation, without opening the settings slab");
Check(!board.Contains("SettingsAction.Reset") && !board.Contains("RESET PROGRESS"),
    "settings cannot construct or dispatch a reset action");
Check(board.Contains("menuHiddenAction != null && pops.Count == 0") && board.Contains("menuHiddenAction = null;") &&
      mainMenu.Contains("settingsBoard.HideMenuForTransition(() => RunAction(action))"),
    "Play/Continue use the existing disappearance and execute once after all pops finish");
Check(board.Contains("!cameraManager.IsTransitioning && !cameraManager.IsLevelSelectorVisible") &&
      board.Contains("transitionMenuHidden = false;") && mainMenu.Contains("settingsBoard.Closed += OnSettingsClosed"),
    "Back or a failed start restores notes and refreshes the authoritative menu model");
Check(Body(mainMenu, "private void OnEnable()").Contains("settingsBoard.Closed += OnSettingsClosed") &&
      Body(mainMenu, "private void OnDisable()").Contains("settingsBoard.Closed -= OnSettingsClosed"),
    "Continue/Back camera disable-enable restores the note-refresh subscription without duplicate listeners");
// Real file-store reset: corrupting or removing the primary must never revive the old career.
string resetDirectory = PathOf("Temp", "SettingsResetRegression", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(resetDirectory);
try
{
    var store = new SaveFileStore(Path.Combine(resetDirectory, "progress.json"));
    store.Write("old career");
    store.Write("newer old career");
    File.WriteAllText(store.RejectedPath, "rejected old career");
    File.WriteAllText(store.TempPath, "interrupted old career");
    store.Reset("fresh career");
    Check(File.ReadAllText(store.FilePath) == "fresh career" && File.ReadAllText(store.BackupPath) == "fresh career", "reset replaces both main save and recovery backup");
    Check(!File.Exists(store.RejectedPath) && !File.Exists(store.TempPath), "reset removes rejected and interrupted old data");
    File.WriteAllText(store.FilePath, "damaged");
    string recovered = store.Read(t => t == "fresh career", out bool backup, out _);
    Check(backup && recovered == "fresh career", "damaged primary recovers fresh progress after reset");
    File.Delete(store.FilePath);
    recovered = store.Read(t => t == "fresh career", out backup, out _);
    Check(backup && recovered == "fresh career", "missing primary recovers fresh progress after reset");
    var empty = new SaveFileStore(Path.Combine(resetDirectory, "first-run.json"));
    empty.Reset("fresh first run");
    Check(File.ReadAllText(empty.BackupPath) == "fresh first run", "reset also succeeds without an existing save");
}
finally { Directory.Delete(resetDirectory, true); }

Console.WriteLine($"\n{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
