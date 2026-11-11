using System;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
#endif

/// <summary>ADB output decisions, independent of Unity so reconnect/refusal paths can be tested.</summary>
public static class UnityRemoteForwarding
{
    public static string UsbDevice(string output, out string reason)
    {
        string selected = null;
        int count = 0;
        reason = "Connect one Android phone with USB debugging authorized.";
        foreach (string line in (output ?? "").Split('\n'))
        {
            string[] fields = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2 || fields[0] == "List" || fields[0].StartsWith("*", StringComparison.Ordinal)) continue;
            // Unity Remote supports a single Android device. Do not guess when another
            // Android device/emulator is connected, or authorize a phone ourselves.
            if (fields[1] != "device") { reason = "Unlock the phone and accept its USB debugging prompt, or reconnect its USB cable."; return null; }
            count++;
            if (Array.Exists(fields, part => part.StartsWith("usb:", StringComparison.Ordinal)) && Regex.IsMatch(fields[0], @"^[A-Za-z0-9._:-]+$")) selected = fields[0];
        }
        if (count != 1 || selected == null)
        {
            if (count > 1) reason = "Unity Remote needs only one connected Android device; disconnect the others.";
            return null;
        }
        reason = null;
        return selected;
    }

    /// <summary>1 = correct mapping; 0 = missing, safe to add; -1 = occupied, never overwrite.</summary>
    public static int Mapping(string output, string device)
    {
        foreach (string line in (output ?? "").Split('\n'))
        {
            string[] fields = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 3 || fields[1] != "tcp:7201") continue;
            return fields[0] == device && fields[2] == "tcp:7201" ? 1 : -1;
        }
        return 0;
    }
}

#if UNITY_EDITOR
/// <summary>Editor-only recovery of the session-scoped Unity Remote USB forward.
/// Runs after imports/restarts and checks reconnects without blocking Editor input.
/// Never restarts ADB, changes device authorization, starts Play, or edits scenes/saves.</summary>
[InitializeOnLoad]
public static class UnityRemoteRecovery
{
    private const string EnabledKey = "TaxVenture.UnityRemote.AutoRecovery";
    private const string ToggleMenu = "Tools/TaxVenture/Unity Remote/Automatic Connection Recovery";
    private const double CheckSeconds = 8;
    private const double TimeoutSeconds = 5;
    private static Process process;
    private static Task<string> stdout, stderr;
    private static Action<string> completed;
    private static double nextCheck, deadline;
    private static string device, adb, lastStatus;

    static UnityRemoteRecovery()
    {
        if (UnityEngine.Application.isBatchMode) return;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= PlayChanged;
        EditorApplication.playModeStateChanged += PlayChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= Stop;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting -= Stop;
        EditorApplication.quitting += Stop;
    }

    [MenuItem(ToggleMenu)]
    private static void Toggle()
    {
        bool on = !EditorPrefs.GetBool(EnabledKey, true);
        EditorPrefs.SetBool(EnabledKey, on);
        if (!on) Stop();
        nextCheck = 0;
    }

    [MenuItem(ToggleMenu, true)]
    private static bool ToggleState() { Menu.SetChecked(ToggleMenu, EditorPrefs.GetBool(EnabledKey, true)); return true; }

    [MenuItem("Tools/TaxVenture/Unity Remote/Repair Connection Now")]
    private static void RepairNow()
    {
        EditorPrefs.SetBool(EnabledKey, true);
        lastStatus = null;
        nextCheck = 0;
    }

    private static void PlayChanged(PlayModeStateChange state) { nextCheck = 0; }

    private static void Tick()
    {
        if (process != null)
        {
            if (EditorApplication.timeSinceStartup > deadline)
            { Stop(); Status("ADB did not respond. Recovery will retry without restarting the ADB server."); return; }
            if (!process.HasExited || !stdout.IsCompleted || !stderr.IsCompleted) return;
            bool ok = process.ExitCode == 0 && stdout.Status == TaskStatus.RanToCompletion;
            string output = ok ? stdout.Result : "";
            Action<string> next = completed;
            Stop();
            if (ok) next?.Invoke(output);
            else Status("ADB connection check failed. Reconnect USB and authorize the phone; recovery will retry.");
            return;
        }
        if (!EditorPrefs.GetBool(EnabledKey, true) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + CheckSeconds;
        // Respect a deliberately selected non-Android Remote device.
        string remote = EditorSettings.unityRemoteDevice;
        if (!string.IsNullOrEmpty(remote) && remote != "None" && remote != "Any Android Device") return;
        adb = FindAdb();
        if (adb == null) { Status("Android SDK platform-tools/adb not found. Configure the SDK in Unity Preferences > External Tools."); return; }
        Run("devices -l", output =>
        {
            device = UnityRemoteForwarding.UsbDevice(output, out string reason);
            if (device == null) { Status(reason); return; }
            if (EditorPrefs.GetBool("AndroidADBKillServerOnExit", true)) EditorPrefs.SetBool("AndroidADBKillServerOnExit", false);
            if (EditorSettings.unityRemoteDevice != "Any Android Device") EditorSettings.unityRemoteDevice = "Any Android Device";
            Run("forward --list", forwards =>
            {
                int mapping = UnityRemoteForwarding.Mapping(forwards, device);
                if (mapping < 0) { Status("Port 7201 belongs to another ADB mapping; recovery will not overwrite it."); return; }
                if (mapping == 1) { lastStatus = null; return; }
                Run("-s " + device + " forward --no-rebind tcp:7201 tcp:7201", ignored =>
                {
                    // Verify rather than assuming a successful command remains in effect.
                    Run("forward --list", verified =>
                    {
                        if (UnityRemoteForwarding.Mapping(verified, device) == 1)
                            Status("Restored Unity Remote USB forwarding. Keep Unity Remote open; if it is waiting, stop and start Play once.");
                        else Status("Unity Remote forwarding is still missing; recovery will retry.");
                    });
                });
            });
        });
    }

    private static void Run(string arguments, Action<string> next)
    {
        try
        {
            process = new Process { StartInfo = new ProcessStartInfo(adb, arguments)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
            process.Start();
            stdout = process.StandardOutput.ReadToEndAsync();
            stderr = process.StandardError.ReadToEndAsync();
            completed = next;
            deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
        }
        catch (Exception)
        { Stop(); Status("Could not run the configured SDK's ADB. Recovery will retry."); }
    }

    private static string FindAdb()
    {
        string contents = EditorApplication.applicationContentsPath;
        string[] roots = { EditorPrefs.GetString("AndroidSdkRoot", ""),
            Path.Combine(contents, "PlaybackEngines/AndroidPlayer/SDK"),
            Path.GetFullPath(Path.Combine(contents, "../../PlaybackEngines/AndroidPlayer/SDK")),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"), Environment.GetEnvironmentVariable("ANDROID_HOME") };
        foreach (string root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            string file = Path.Combine(root, "platform-tools", UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor ? "adb.exe" : "adb");
            if (File.Exists(file)) return file;
        }
        return null;
    }

    private static void Status(string message)
    {
        if (lastStatus == message) return;
        lastStatus = message;
        UnityEngine.Debug.Log("[UnityRemoteRecovery] " + message);
    }

    private static void Stop()
    {
        if (process != null)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            finally { process.Dispose(); process = null; }
        }
        completed = null;
    }
}
#endif
