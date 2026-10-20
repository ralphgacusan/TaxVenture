using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// PURPOSE:
/// Crash-safe, tamper-evident storage for one save file (no Unity calls, so
/// it is unit-tested outside the editor).
///
/// WRITE (atomic): the new content goes to "&lt;file&gt;.tmp" and is flushed to
/// disk; the current file becomes "&lt;file&gt;.bak"; then the temp file takes
/// its place. A crash at any point leaves either the old or the new save
/// complete, never half a file.
///
/// READ: the main file first; if it is missing, unreadable or fails its
/// check, the backup. A file that fails the check is kept aside as
/// "&lt;file&gt;.rejected" (never deleted) so nothing is lost silently.
///
/// INTEGRITY: Sign() returns an HMAC-SHA256 of the content with a key built
/// into the game. It stops casual editing of the save (raising EXP in a
/// text editor); it is not protection against someone who decompiles the
/// game, which no offline save can offer.
/// </summary>
public sealed class SaveFileStore
{
    // Changing this key invalidates every signed save (they then load from
    // their backup or as unsigned legacy data), so leave it alone.
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("TaxVenture.Save.v3|9f2c41d7-ledger");

    public string FilePath { get; }
    public string BackupPath => FilePath + ".bak";
    public string TempPath => FilePath + ".tmp";
    public string RejectedPath => FilePath + ".rejected";

    public SaveFileStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A save path is required.", nameof(filePath));
        FilePath = filePath;
    }

    /// <summary>HMAC-SHA256 of the text, as lowercase hex.</summary>
    public static string Sign(string text)
    {
        using (var hmac = new HMACSHA256(Key))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>Constant-time comparison of two signatures.</summary>
    public static bool SignatureMatches(string text, string signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        string expected = Sign(text);
        if (expected.Length != signature.Length) return false;
        int diff = 0;
        for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ signature[i];
        return diff == 0;
    }

    /// <summary>Writes the text atomically, keeping the previous file as the backup.</summary>
    public void Write(string text)
    {
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        byte[] bytes = new UTF8Encoding(false).GetBytes(text ?? string.Empty);
        using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        if (File.Exists(FilePath))
        {
            try
            {
                File.Replace(TempPath, FilePath, BackupPath, true);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (IOException) { }

            // File.Replace is not available everywhere (some mobile file
            // systems): keep the backup by copy, then swap in the new file.
            File.Copy(FilePath, BackupPath, true);
            File.Delete(FilePath);
        }
        File.Move(TempPath, FilePath);
    }

    /// <summary>Replace all recoverable generations for a confirmed fresh start.</summary>
    public void Reset(string text)
    {
        Write(text);
        Write(text);
        if (File.Exists(RejectedPath)) File.Delete(RejectedPath);
    }

    /// <summary>
    /// Reads the first candidate (main file, then backup) that the validator
    /// accepts. Returns null when none is usable. fromBackup tells which one
    /// was used; a rejected main file is kept as .rejected.
    /// </summary>
    public string Read(Func<string, bool> isValid, out bool fromBackup, out string problem)
    {
        fromBackup = false;
        problem = null;

        string main = TryRead(FilePath, ref problem);
        if (main != null)
        {
            if (isValid == null || isValid(main)) return main;
            problem = "the save failed its check";
            KeepRejected();
        }

        string backup = TryRead(BackupPath, ref problem);
        if (backup != null && (isValid == null || isValid(backup)))
        {
            fromBackup = true;
            return backup;
        }
        return null;
    }

    public bool Exists => File.Exists(FilePath) || File.Exists(BackupPath);

    private static string TryRead(string path, ref string problem)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(text))
            {
                problem = $"{Path.GetFileName(path)} is empty";
                return null;
            }
            return text;
        }
        catch (Exception e)
        {
            problem = $"{Path.GetFileName(path)} could not be read ({e.Message})";
            return null;
        }
    }

    private void KeepRejected()
    {
        try
        {
            File.Copy(FilePath, RejectedPath, true);
        }
        catch
        {
            // Keeping the copy is best effort; the main file is left untouched.
        }
    }
}
