// Runs the real audio/settings code against a small fake Unity audio surface.
// Playback quality and scene/device behavior still require Unity Play Mode.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

string root = Directory.GetCurrentDirectory();
while (!Directory.Exists(Path.Combine(root, "Assets"))) root = Directory.GetParent(root)!.FullName;
string Read(string path) => File.ReadAllText(Path.Combine(root, path));
string GuidFor(string path) => Regex.Match(Read(path + ".meta"), @"guid: (\w+)").Groups[1].Value;
int checks = 0;
void Check(bool ok, string message)
{
    if (!ok) throw new Exception(message);
    checks++;
}
void Call(object target, string method, params object[] args) =>
    target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(target, args);
T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;
void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);
void Advance(AudioManager audio, float seconds = 0.1f)
{
    Time.unscaledTime += seconds;
    Time.unscaledDeltaTime = seconds;
    Time.frameCount++;
    Call(audio, "Update");
}

string libraryRoot = Path.Combine(root, "Assets/AudioManager/TaxVenture_Audio_Library");
string[] suppliedFiles = Directory.GetFiles(libraryRoot, "*", SearchOption.AllDirectories)
    .Where(path => path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
    .ToArray();
string gameplayMusicPath = Path.Combine(root, "Assets/Clip Audios/bg music 3.mp3");
string[] catalogClipFiles = suppliedFiles.Append(gameplayMusicPath).ToArray();
string catalog = Read("Assets/Resources/TaxVentureAudioLibrary.asset").Replace("\r\n", "\n");
Match section = Regex.Match(catalog, @"(?s)  clips:\r?\n(?<body>.*?)(?=\r?\n  gameplayMusic:)");
Check(section.Success, "Audio catalog has a clips list");
string[] listedGuids = Regex.Matches(section.Groups["body"].Value, @"guid: (\w+)")
    .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
string[] expectedGuids = catalogClipFiles.Select(GuidFor).ToArray();
Check(expectedGuids.Length == 84 && listedGuids.Length == 84 && expectedGuids.ToHashSet().SetEquals(listedGuids),
    "Catalog includes every one of the 83 supplied clips and the approved gameplay music track exactly once");
Check(suppliedFiles.Length == 83, "Supplied library inventory is 81 SFX clips and 2 ambience clips");

var libraryClips = catalogClipFiles.Select(path => new AudioClip { name = Path.GetFileNameWithoutExtension(path) }).ToArray();
AudioClip ByName(string name) => libraryClips.Single(clip => clip.name == name);
Resources.Library = new AudioLibrary
{
    clips = libraryClips,
    gameplayMusic = new[] { ByName("bg music 3") },
    officeAmbience = new[] { ByName("amb_Office") },
    outdoorAmbience = new[] { ByName("amb_Environment") }
};
Check(catalog.Contains("gameplayMusic:\n  - {fileID: 8300000, guid: " + GuidFor(gameplayMusicPath)), "Gameplay music is a catalog category");
Check(catalog.Contains("officeAmbience:\n  - {fileID: 8300000, guid: " + GuidFor("Assets/AudioManager/MixedAmbience/amb_Office.wav")), "Office loop is a catalog category");
Check(catalog.Contains("outdoorAmbience:\n  - {fileID: 8300000, guid: " + GuidFor("Assets/AudioManager/MixedAmbience/amb_Environment.wav")), "Town loop is a catalog category");

foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories))
    foreach (Match match in Regex.Matches(File.ReadAllText(path), "(?:PlayEffect|PlayExactEffect|library.Find)\\(\"([^\"]+)\""))
    {
        string name = match.Groups[1].Value;
        bool numbered = Regex.IsMatch(name, @"_\d{2}$");
        Check(numbered ? Resources.Library.FindFamily(name).Length > 0 : Resources.Library.Find(name) != null,
            "Missing sound: " + name);
    }

string[] sourceFiles = Directory.GetFiles(Path.Combine(root, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories);
string[] sfxFiles = Directory.GetFiles(Path.Combine(libraryRoot, "SFX"), "*", SearchOption.AllDirectories)
    .Where(path => path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
    .ToArray();
string FamilyOf(string name) => Regex.Replace(name, @"_\d{2}$", string.Empty);
string[] sfxFamilies = sfxFiles.Select(path => Path.GetFileNameWithoutExtension(path))
    .Where(name => Regex.IsMatch(name, @"^sfx_.+_\d{2}$"))
    .Select(FamilyOf).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
var sourceSfxFamilies = sourceFiles.SelectMany(path => Regex.Matches(File.ReadAllText(path), "\\\"(sfx_[A-Za-z0-9_]+_\\d{2})\\\"")
        .Cast<Match>().Select(match => FamilyOf(match.Groups[1].Value))).ToHashSet();
foreach (string family in sfxFamilies)
    Check(sourceSfxFamilies.Contains(family), "Missing contextual SFX family hook: " + family);

Check(Resources.Library.FindDialogue(false).Length == 5 && Resources.Library.FindDialogue(false).Any(clip => clip.name == "male gibberish long"),
    "Male dialogue set includes numbered voices and the long sample");
Check(Resources.Library.FindDialogue(true).Length == 4 && Resources.Library.FindDialogue(true).Any(clip => clip.name == "female gibberish long"),
    "Female dialogue set includes all voices and the long sample");

Time.unscaledTime = 0f;
Time.unscaledDeltaTime = 0.1f;
Time.frameCount = 0;
SceneManager.ActiveSceneName = "3D Menu";
var audio = new AudioManager();
var authoredMenuMusic = new AudioClip { name = "Authored menu music" };
Set(audio, "backgroundMusic", authoredMenuMusic);
Call(audio, "Awake");
Call(audio, "Start");
var sfx = Field<AudioSource>(audio, "sfxSource");
var music = Field<AudioSource>(audio, "musicSource");
var ambience = Field<AudioSource>(audio, "ambienceSource");
Check(audio.IsSFXEnabled() && audio.IsMusicEnabled(), "Both settings default to on");
Check(music.clip == authoredMenuMusic && music.isPlaying && music.loop, "Existing scene-authored music stays as a menu-only track");
Advance(audio, 1f);
Check(Math.Abs(music.volume - 0.035f) < 0.001f, "Gameplay music starts at quiet audible volume");

audio.SetOutdoor(true);
Advance(audio, 0.1f);
Check(ambience.clip.name == "amb_Environment" && ambience.loop && ambience.isPlaying, "Outdoor context starts a quiet looping town bed");
audio.SetOutdoor(false);
Advance(audio, 1f);
Check(ambience.clip.name == "amb_Office" && ambience.loop && ambience.isPlaying, "Indoor context fades to the office bed");

SceneManager.ActiveSceneName = "Office_Level1";
Call(audio, "OnSceneLoaded", new Scene { name = "Office_Level1" }, LoadSceneMode.Single);
Advance(audio, 1f);
Check(music.clip.name == "bg music 3" && music.isPlaying, "Office_Level1 selects the approved gameplay music");
int gameplayPlayCount = music.PlayCount;
SceneManager.ActiveSceneName = "Office_Level2";
Call(audio, "OnSceneLoaded", new Scene { name = "Office_Level2" }, LoadSceneMode.Single);
Check(!Field<bool>(audio, "completionMusicRequested"), "Scene changes clear completion music mode");
Check(music.clip.name == "bg music 3" && music.PlayCount == gameplayPlayCount, "Matching music continues across office scenes without restarting");
audio.SetCompletionMusic(true);
Check(music.clip.name == "bg music 3" && music.PlayCount == gameplayPlayCount, "Missing completion category keeps the current gameplay bed");
audio.SetCompletionMusic(false);

var playedBefore = sfx.Played.Count;
Advance(audio, 0.1f); audio.PlayEffect("sfx_reward_points_01");
Advance(audio, 0.1f); audio.PlayEffect("sfx_reward_points_01");
Advance(audio, 0.1f); audio.PlayEffect("sfx_reward_points_01");
Check(sfx.Played.Skip(playedBefore).TakeLast(3).SequenceEqual(new[] { "sfx_reward_points_01", "sfx_reward_points_02", "sfx_reward_points_03" }),
    "PlayEffect cycles all reward point variants without immediate repeats");

Advance(audio, 0.1f); audio.PlayEffect("sfx_card_pickup_01");
string cardFirst = sfx.Played.Last();
int cardCount = sfx.Played.Count;
audio.PlayEffect("sfx_card_pickup_02");
Check(sfx.Played.Count == cardCount, "Two hooks for numbered members of one family are coalesced");
Advance(audio, 0.1f); audio.PlayEffect("sfx_card_pickup_02");
Check(sfx.Played.Last() != cardFirst, "Requesting another family member advances only after debounce");

Advance(audio, 0.1f); audio.PlayKeyboardSFX();
string keyboardFirst = sfx.Played.Last();
Advance(audio, 0.1f); audio.PlayKeyboardSFX();
Check(sfx.Played.Last() != keyboardFirst, "Legacy keyboard hook cycles its variants");
Advance(audio, 0.1f); audio.PlayMouseClickSelectSFX();
string clickFirst = sfx.Played.Last();
Advance(audio, 0.1f); audio.PlayMouseClickSelectSFX();
Check(sfx.Played.Last() != clickFirst, "Legacy click hook cycles its variants");
Advance(audio, 0.1f); audio.PlayAchievementSFX();
string rewardFirst = sfx.Played.Last();
Advance(audio, 0.1f); audio.PlayAchievementSFX();
Check(sfx.Played.Last() != rewardFirst, "Legacy reward hook cycles its variants");

foreach (string family in sfxFamilies)
{
    AudioClip[] variants = Resources.Library.FindFamily(family + "_01");
    var reached = new HashSet<string>();
    for (int i = 0; i < variants.Length; i++)
    {
        Advance(audio, 0.1f);
        audio.PlayEffect(variants[0].name);
        reached.Add(sfx.Played.Last());
    }
    Check(reached.SetEquals(variants.Select(clip => clip.name)), "Every supplied SFX variant is reachable: " + family);
}

playedBefore = sfx.Played.Count;
audio.PlayDialogueSFX(false);
audio.PlayDialogueSFX(true);
audio.PlayNPCDialogueSFX();
audio.StopDialogueSFX();
foreach (AudioClip clip in Resources.Library.FindDialogue(false).Concat(Resources.Library.FindDialogue(true)))
    audio.PlayEffect(clip.name);
Advance(audio, 1f);
Check(sfx.Played.Count == playedBefore, "Legacy and generic playback cannot play gibberish voices");
Check(Math.Abs(music.volume - 0.035f) < 0.001f, "Silent dialogue hooks keep the lowered music level steady");
Check(Math.Abs(ambience.volume - 0.45f) < 0.001f, "Ambience is louder than the quiet background music");
Check(sourceFiles.Where(path => !path.EndsWith("AudioManager.cs")).All(path =>
    !Regex.IsMatch(File.ReadAllText(path), @"\.(?:PlayDialogueSFX|PlayNPCDialogueSFX)\s*\(")),
    "Gameplay UI never requests gibberish voices");

audio.SetMusicEnabled(false);
Check(music.mute && audio.IsSFXEnabled(), "Music switch controls music independently");
audio.SetMusicEnabled(true);
Check(!music.mute, "Music can be re-enabled");
audio.SetOutdoor(false);
Advance(audio, 0.2f); audio.PlayFootstepSFX();
Advance(audio, 0.2f); audio.PlayFootstepSFX();
Advance(audio, 0.2f); audio.PlayFootstepSFX();
Check(sfx.Played.TakeLast(3).SequenceEqual(new[] { "sfx_footstep_office_01", "sfx_footstep_office_02", "sfx_footstep_office_03" }),
    "Indoor footsteps cycle their context variants");
audio.SetOutdoor(true);
Advance(audio, 1f); audio.PlayFootstepSFX();
Advance(audio, 0.2f); audio.PlayFootstepSFX();
Advance(audio, 0.2f); audio.PlayFootstepSFX();
Check(sfx.Played.TakeLast(3).SequenceEqual(new[] { "sfx_footstep_pavement_01", "sfx_footstep_pavement_02", "sfx_footstep_pavement_03" }),
    "Outdoor footsteps switch to pavement variants");

foreach (string cutscene in new[] { "Cutscene1.0", "Cutscene2.0" })
{
    SceneManager.ActiveSceneName = cutscene;
    Call(audio, "OnSceneLoaded", new Scene { name = cutscene }, LoadSceneMode.Single);
    Check(!music.isPlaying && music.clip == null && !ambience.isPlaying, cutscene + " suppresses global music and ambience");
}
SceneManager.ActiveSceneName = "Office_Level1";
Call(audio, "OnSceneLoaded", new Scene { name = "Office_Level1" }, LoadSceneMode.Single);
Advance(audio, 1f);
Check(music.clip.name == "bg music 3" && music.isPlaying, "Gameplay music resumes after the cutscene scene");

var settings = new SettingsManager();
settings.SetSFXEnabled(false);
Check(!audio.IsSFXEnabled(), "Settings manager controls the shared SFX flag");
Check(sfx.mute && ambience.mute && !sfx.isPlaying && ambience.isPlaying,
    "Disabling SFX stops effects and mutes the looping ambience");
Check(!music.mute && audio.IsMusicEnabled(), "SFX does not mute music");
playedBefore = sfx.Played.Count;
audio.PlayAchievementSFX(); audio.PlayKeyboardSFX(); audio.PlayPaperSFX(); audio.PlayPaper2SFX();
audio.PlayFootstepSFX(); audio.PlayNPCDialogueSFX(); audio.PlayEffect("sfx_stamp_press_01");
Check(sfx.Played.Count == playedBefore, "Every playback path respects SFX off");
Call(audio, "OnDestroy");

audio = new AudioManager(); Call(audio, "Awake");
sfx = Field<AudioSource>(audio, "sfxSource");
Check(!audio.IsSFXEnabled() && sfx.mute, "Saved SFX off survives manager recreation");
settings.SetSFXEnabled(true);
audio.PlayEffect("sfx_stamp_press_01");
Check(sfx.Played.Last() == "sfx_stamp_press_01" && !sfx.mute, "Re-enabling resumes new effects");

var button = new Button(); UnityEngine.Object.Buttons = new[] { button };
Call(audio, "WireButtons"); Call(audio, "WireButtons");
Advance(audio, 1f);
button.onClick.Invoke(); Call(audio, "LateUpdate");
Check(sfx.Played.Last() == "sfx_ui_click_01" && button.onClick.ListenerCount == 1, "Buttons wire once and get click feedback");
Advance(audio, 1f);
int count = sfx.Played.Count;
button.onClick.Invoke(); audio.PlaySelectSFX(); Call(audio, "LateUpdate");
Check(sfx.Played.Count == count + 1, "Explicit action sound suppresses generic button feedback");

Advance(audio, 1f);
button.name = "BackButton";
button.onClick.Invoke(); Call(audio, "LateUpdate");
Check(sfx.Played.Last() == "sfx_ui_back_01", "Navigation button feedback follows the touched control");
Advance(audio, 1f);
button.name = "ContinueButton";
button.onClick.Invoke(); Call(audio, "LateUpdate");
Check(sfx.Played.Last().StartsWith("sfx_ui_click_"), "Continue uses a mechanical click without a voice");
Advance(audio, 1f);
audio.QueueInteractionSFX("ReceptionistInteractable"); Call(audio, "LateUpdate");
Check(sfx.Played.Last() == "sfx_ui_select_01", "NPC interaction has a distinct selection cue");
Advance(audio, 1f);
audio.QueueInteractionSFX("PaperInteractable"); Call(audio, "LateUpdate");
Check(sfx.Played.Last().StartsWith("sfx_paper_") || sfx.Played.Last() == "sfx_page_turn_01", "Paper interaction has tactile paper feedback");
Advance(audio, 1f);
audio.QueueInteractionSFX("paper_place"); Call(audio, "LateUpdate");
Check(sfx.Played.Last() == "sfx_paper_place_01", "Closing and returning paper differs from picking it up");
Advance(audio, 1f);
count = sfx.Played.Count;
audio.QueueInteractionSFX("CaseFolderInteractable");
audio.PlayEffect("sfx_folder_open_01"); Call(audio, "LateUpdate");
Check(sfx.Played.Count == count + 1, "Specialized item effects replace the queued interaction fallback");
Advance(audio, 1f);
count = sfx.Played.Count;
audio.PlayFootstepSFX();
audio.QueueInteractionSFX("Continue"); Call(audio, "LateUpdate");
Check(sfx.Played.Count == count + 2 && sfx.Played.Last().StartsWith("sfx_ui_click_"),
    "Walking cannot silence a simultaneous click");
Advance(audio, 1f);
count = sfx.Played.Count;
audio.QueueInteractionSFX("continue");
audio.PlayEffect("sfx_deadline_warning_01"); Call(audio, "LateUpdate");
Check(sfx.Played.Count == count + 2 && sfx.Played.Last().StartsWith("sfx_ui_click_"),
    "Unrelated notifications cannot consume a Continue click");
count = sfx.Played.Count - 1;
settings.SetSFXEnabled(false);
button.onClick.Invoke(); Call(audio, "LateUpdate");
Check(sfx.Played.Count == count + 1, "Button feedback respects SFX off");
Call(audio, "OnDestroy");
Check(button.onClick.ListenerCount == 0, "Manager cleans up button listeners");
// Requested paper icon variation and fixed value-transfer recordings.
audio = new AudioManager(); Call(audio, "Awake");
sfx = Field<AudioSource>(audio, "sfxSource");
settings.SetSFXEnabled(true);
var paperSounds = new HashSet<string> { "sfx_paper_pickup_01", "sfx_paper_place_01", "sfx_paper_rustle_01", "sfx_page_turn_01", "sfx_folder_open_01", "sfx_folder_close_01" };
foreach (string iconName in new[] { "NotesIconButton", "CaseFolderIconButton", "TaxpayerProfileIcon", "AnnotatedPapersIcon", "TaxCodeBookIcon" })
{
    var heard = new HashSet<string>();
    for (int i = 0; i < 3; i++)
    {
        Advance(audio, 0.2f);
        audio.QueueInteractionSFX(iconName); Call(audio, "LateUpdate");
        heard.Add(sfx.Played.Last());
    }
    Check(heard.Count == 3 && heard.All(paperSounds.Contains), "Document icon uses varied paper recordings: " + iconName);
}
foreach (string recording in new[] { "sfx_card_pickup_02", "sfx_card_place_01", "sfx_card_return_02" })
{
    for (int i = 0; i < 3; i++)
    {
        Advance(audio, 0.2f);
        audio.PlayExactEffect(recording);
        Check(sfx.Played.Last() == recording, "Exact playback preserves requested variant: " + recording);
    }
}
Advance(audio, 0.2f);
int exactCount = sfx.Played.Count;
audio.PlayExactEffect("sfx_card_pickup_02");
audio.PlayEffect("sfx_card_pickup_01");
Check(sfx.Played.Count == exactCount + 1, "Fixed variants retain family-level duplicate suppression");
string transferSoundSource = Read("Assets/Scripts/Data/DragDrop/ValueTransferManager.cs");
Check(transferSoundSource.Contains("PlayExactEffect(\"sfx_card_pickup_02\"") &&
      transferSoundSource.Contains("PlayExactEffect(\"sfx_card_place_01\""), "All shared value transfers use the requested pickup and placement recordings");
string toolkitSoundSource = Read("Assets/Scripts/Level2/IncomeToolkit.cs");
Check(toolkitSoundSource.Split("private IEnumerator SettleInto(")[1].Split("private void ReturnHome(")[0].Contains("PlayExactEffect(\"sfx_card_place_01\"") &&
      toolkitSoundSource.Split("private void UpdateInk(")[1].Split("private Quaternion HeldPen")[0].Contains("PlayExactEffect(\"sfx_card_return_02\""),
    "Side Quest 2 landing and physical highlight sweep use requested card sounds");
Call(audio, "OnDestroy");
Console.WriteLine($"Audio regression: {checks} checks passed.");

namespace UnityEngine
{
    public class Object
    {
        public string name = string.Empty;
        public static Button[] Buttons = Array.Empty<Button>();
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive) => Buttons.OfType<T>().ToArray();
        public static void Destroy(Object value) { }
        public static void DontDestroyOnLoad(Object value) { }
    }
    public class Component : Object { }
    public class MonoBehaviour : Component { public GameObject gameObject = new GameObject(); }
    public class ScriptableObject : Object { }
    public class GameObject : Object
    {
        public GameObject(string name = "") { }
        public T AddComponent<T>() where T : new() => new T();
    }
    public class AudioClip : Object { }
    public class AudioSource : Object
    {
        public bool mute, loop, playOnAwake, isPlaying;
        public float spatialBlend, volume;
        public AudioClip clip;
        public int PlayCount;
        public List<string> Played = new List<string>();
        public void Play() { isPlaying = true; PlayCount++; }
        public void Stop() { isPlaying = false; }
        public void PlayOneShot(AudioClip value, float volume = 1) { Played.Add(value.name); isPlaying = true; }
    }
    public static class Resources
    {
        public static AudioLibrary Library;
        public static T Load<T>(string path) where T : class => Library as T;
    }
    public static class Time { public static float unscaledTime, unscaledDeltaTime; public static int frameCount; }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, int> values = new Dictionary<string, int>();
        public static int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
        public static void SetInt(string key, int value) => values[key] = value;
        public static void Save() { }
    }
    public static class Debug { public static void Log(string text) { } public static void LogWarning(string text) { } }
    public enum FindObjectsInactive { Include }
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public enum LoadSceneMode { Single }
    public static class SceneManager
    {
        public static string ActiveSceneName = "3D Menu";
        public static event Action<Scene, LoadSceneMode> sceneLoaded { add { } remove { } }
        public static Scene GetActiveScene() => new Scene { name = ActiveSceneName };
    }
}
namespace UnityEngine.UI
{
    public class Button : UnityEngine.Object
    {
        public ClickEvent onClick = new ClickEvent();
        public class ClickEvent
        {
            private event UnityEngine.Events.UnityAction handlers;
            public int ListenerCount => handlers?.GetInvocationList().Length ?? 0;
            public void AddListener(UnityEngine.Events.UnityAction action) => handlers += action;
            public void RemoveListener(UnityEngine.Events.UnityAction action) => handlers -= action;
            public void Invoke() => handlers?.Invoke();
        }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
}
