using System;
using System.IO;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Reads and deserializes Level/Case JSON files from StreamingAssets, with
/// explicit validation and error handling at every step — missing file,
/// unreadable file, malformed JSON, and missing required fields are all
/// caught and reported with a clear message rather than throwing an
/// unhandled exception into gameplay code.
///
/// FILE LAYOUT EXPECTED:
/// StreamingAssets/Levels/{levelId}/level.json
/// StreamingAssets/Levels/{levelId}/cases/{caseId}.json
///
/// PLATFORM NOTE (flagged for the eventual Mobile Porting Phase):
/// On Android, StreamingAssets is compressed inside the APK, and
/// File.ReadAllText does NOT work there directly — a platform-conditional
/// UnityWebRequest-based read is required on-device. This loader's PUBLIC
/// API (LoadLevel/LoadCase) is written so that swap only touches the
/// private ReadFileText method below, nothing else in the project.
/// </summary>
public static class JsonCaseLoader
{
    public static CaseValidationResult<LevelDefinition> LoadLevel(string levelId)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", levelId, "level.json");

        var readResult = ReadFileText(path);
        if (!readResult.IsSuccess)
        {
            return CaseValidationResult<LevelDefinition>.Fail(readResult.ErrorMessage);
        }

        LevelDefinition parsed;
        try
        {
            parsed = JsonUtility.FromJson<LevelDefinition>(readResult.Data);
        }
        catch (Exception e)
        {
            return CaseValidationResult<LevelDefinition>.Fail($"Failed to parse level JSON at {path}: {e.Message}");
        }

        if (parsed == null)
        {
            return CaseValidationResult<LevelDefinition>.Fail($"Level JSON at {path} parsed to null.");
        }

        if (string.IsNullOrEmpty(parsed.levelId))
        {
            return CaseValidationResult<LevelDefinition>.Fail($"Level JSON at {path} is missing 'levelId'.");
        }

        if (parsed.caseIds == null || parsed.caseIds.Count == 0)
        {
            return CaseValidationResult<LevelDefinition>.Fail($"Level '{parsed.levelId}' has no caseIds — at least one case is required.");
        }

        return CaseValidationResult<LevelDefinition>.Success(parsed);
    }

    public static CaseValidationResult<CaseDefinition> LoadCase(string levelId, string caseId)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", levelId, "cases", $"{caseId}.json");

        var readResult = ReadFileText(path);
        if (!readResult.IsSuccess)
        {
            return CaseValidationResult<CaseDefinition>.Fail(readResult.ErrorMessage);
        }

        CaseDefinition parsed;
        try
        {
            parsed = JsonUtility.FromJson<CaseDefinition>(readResult.Data);
        }
        catch (Exception e)
        {
            return CaseValidationResult<CaseDefinition>.Fail($"Failed to parse case JSON at {path}: {e.Message}");
        }

        if (parsed == null)
        {
            return CaseValidationResult<CaseDefinition>.Fail($"Case JSON at {path} parsed to null.");
        }

        string validationError = ValidateCaseDefinition(parsed, path);
        if (validationError != null)
        {
            return CaseValidationResult<CaseDefinition>.Fail(validationError);
        }

        return CaseValidationResult<CaseDefinition>.Success(parsed);
    }

    /// <summary>
    /// Checks every field the rest of the system will assume exists.
    /// Deliberately strict — a case that fails validation should never
    /// silently proceed with partial/garbage data.
    /// </summary>
    private static string ValidateCaseDefinition(CaseDefinition def, string sourcePath)
    {
        if (string.IsNullOrEmpty(def.caseId))
            return $"Case at {sourcePath} is missing 'caseId'.";

        if (string.IsNullOrEmpty(def.fullName))
            return $"Case '{def.caseId}' is missing client 'fullName'.";

        if (def.answerKey == null)
            return $"Case '{def.caseId}' is missing 'answerKey'.";

        if (def.documents == null)
            return $"Case '{def.caseId}' has a null 'documents' list (use an empty array [] if the case has no documents).";

        foreach (var doc in def.documents)
        {
            if (string.IsNullOrEmpty(doc.documentName))
                return $"Case '{def.caseId}' has a document with a missing 'documentName'.";

            if (doc.fields == null)
                return $"Case '{def.caseId}', document '{doc.documentName}' has a null 'fields' list.";

            foreach (var field in doc.fields)
            {
                if (string.IsNullOrEmpty(field.semanticKey))
                    return $"Case '{def.caseId}', document '{doc.documentName}' has a field ('{field.label}') with a missing 'semanticKey'.";

                if (field.type != "Number" && field.type != "Text" && field.type != "Enum")
                    return $"Case '{def.caseId}', document '{doc.documentName}', field '{field.label}' has invalid type '{field.type}' (expected Number, Text, or Enum).";
            }
        }

        return null; // valid
    }

    private static CaseValidationResult<string> ReadFileText(string path)
    {
        if (!File.Exists(path))
        {
            return CaseValidationResult<string>.Fail($"File not found: {path}");
        }

        try
        {
            string text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                return CaseValidationResult<string>.Fail($"File is empty: {path}");
            }
            return CaseValidationResult<string>.Success(text);
        }
        catch (Exception e)
        {
            return CaseValidationResult<string>.Fail($"Failed to read file {path}: {e.Message}");
        }
    }
}