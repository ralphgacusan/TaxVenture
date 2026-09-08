using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Loads and deserializes Level/Case JSON files from the Resources folder,
/// with explicit validation and error handling.
///
/// FILE LAYOUT:
///
/// Resources/Levels/{levelId}/level.json
/// Resources/Levels/{levelId}/cases/{caseId}.json
///
/// EXAMPLE:
///
/// Assets/Resources/Levels/level_01/level.json
/// Assets/Resources/Levels/level_01/cases/case_01.json
///
/// Android:
/// Resources.Load<TextAsset>() works with files packaged inside the
/// Android APK, so this avoids the StreamingAssets/File.ReadAllText
/// problem on mobile.
///
/// PUBLIC API:
///
///     JsonCaseLoader.LoadLevel("level_01");
///     JsonCaseLoader.LoadCase("level_01", "case_01");
///
/// The rest of the project can continue using the same public methods.
/// </summary>
public static class JsonCaseLoader
{
    /// <summary>
    /// Loads a level definition from:
    /// Resources/Levels/{levelId}/level.json
    /// </summary>
    public static CaseValidationResult<LevelDefinition> LoadLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                "Cannot load level because levelId is null or empty."
            );
        }

        // Resources.Load does NOT use the .json extension.
        string resourcePath = $"Levels/{levelId}/level";

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Level JSON not found in Resources: " +
                $"Resources/{resourcePath}.json"
            );
        }

        if (string.IsNullOrWhiteSpace(jsonFile.text))
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Level JSON is empty: Resources/{resourcePath}.json"
            );
        }

        LevelDefinition parsed;

        try
        {
            parsed = JsonUtility.FromJson<LevelDefinition>(jsonFile.text);
        }
        catch (Exception e)
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Failed to parse level JSON at " +
                $"Resources/{resourcePath}.json: {e.Message}"
            );
        }

        if (parsed == null)
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Level JSON at Resources/{resourcePath}.json parsed to null."
            );
        }

        if (string.IsNullOrEmpty(parsed.levelId))
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Level JSON at Resources/{resourcePath}.json " +
                $"is missing 'levelId'."
            );
        }

        if (parsed.caseIds == null || parsed.caseIds.Count == 0)
        {
            return CaseValidationResult<LevelDefinition>.Fail(
                $"Level '{parsed.levelId}' has no caseIds — " +
                $"at least one case is required."
            );
        }

        Debug.Log(
            $"[JsonCaseLoader] Successfully loaded level '{parsed.levelId}' " +
            $"with {parsed.caseIds.Count} case(s)."
        );

        return CaseValidationResult<LevelDefinition>.Success(parsed);
    }


    /// <summary>
    /// Loads a case definition from:
    /// Resources/Levels/{levelId}/cases/{caseId}.json
    /// </summary>
    public static CaseValidationResult<CaseDefinition> LoadCase(
        string levelId,
        string caseId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                "Cannot load case because levelId is null or empty."
            );
        }

        if (string.IsNullOrEmpty(caseId))
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                "Cannot load case because caseId is null or empty."
            );
        }

        // Resources.Load does NOT use the .json extension.
        string resourcePath =
            $"Levels/{levelId}/cases/{caseId}";

        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                $"Case JSON not found in Resources: " +
                $"Resources/{resourcePath}.json"
            );
        }

        if (string.IsNullOrWhiteSpace(jsonFile.text))
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                $"Case JSON is empty: Resources/{resourcePath}.json"
            );
        }

        CaseDefinition parsed;

        try
        {
            parsed = JsonUtility.FromJson<CaseDefinition>(jsonFile.text);
        }
        catch (Exception e)
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                $"Failed to parse case JSON at " +
                $"Resources/{resourcePath}.json: {e.Message}"
            );
        }

        if (parsed == null)
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                $"Case JSON at Resources/{resourcePath}.json " +
                $"parsed to null."
            );
        }

        string validationError =
            ValidateCaseDefinition(parsed, resourcePath);

        if (validationError != null)
        {
            return CaseValidationResult<CaseDefinition>.Fail(
                validationError
            );
        }

        Debug.Log(
            $"[JsonCaseLoader] Successfully loaded case '{parsed.caseId}' " +
            $"from Resources/{resourcePath}.json"
        );

        return CaseValidationResult<CaseDefinition>.Success(parsed);
    }


    /// <summary>
    /// Checks every field the rest of the system will assume exists.
    /// A case that fails validation should never silently proceed with
    /// incomplete or invalid data.
    /// </summary>
    private static string ValidateCaseDefinition(
        CaseDefinition def,
        string sourcePath)
    {
        if (string.IsNullOrEmpty(def.caseId))
        {
            return $"Case at Resources/{sourcePath}.json " +
                   $"is missing 'caseId'.";
        }

        if (string.IsNullOrEmpty(def.fullName))
        {
            return $"Case '{def.caseId}' is missing client 'fullName'.";
        }

        if (def.answerKey == null)
        {
            return $"Case '{def.caseId}' is missing 'answerKey'.";
        }

        if (def.documents == null)
        {
            return $"Case '{def.caseId}' has a null 'documents' list " +
                   $"(use an empty array [] if the case has no documents).";
        }

        foreach (var doc in def.documents)
        {
            if (doc == null)
            {
                return $"Case '{def.caseId}' contains a null document entry.";
            }

            if (string.IsNullOrEmpty(doc.documentName))
            {
                return $"Case '{def.caseId}' has a document with a " +
                       $"missing 'documentName'.";
            }

            if (doc.fields == null)
            {
                return $"Case '{def.caseId}', document '{doc.documentName}' " +
                       $"has a null 'fields' list.";
            }

            foreach (var field in doc.fields)
            {
                if (field == null)
                {
                    return $"Case '{def.caseId}', document " +
                           $"'{doc.documentName}' contains a null field entry.";
                }

                if (string.IsNullOrEmpty(field.semanticKey))
                {
                    return $"Case '{def.caseId}', document " +
                           $"'{doc.documentName}' has a field " +
                           $"('{field.label}') with a missing 'semanticKey'.";
                }

                if (field.type != "Number" &&
                    field.type != "Text" &&
                    field.type != "Enum")
                {
                    return $"Case '{def.caseId}', document " +
                           $"'{doc.documentName}', field '{field.label}' " +
                           $"has invalid type '{field.type}' " +
                           $"(expected Number, Text, or Enum).";
                }
            }
        }

        return null;
    }
}