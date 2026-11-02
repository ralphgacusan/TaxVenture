using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The optional Level 3 block of a case file (CaseDefinition.level3), the
/// smallest extension in Documentation/Level3TheNumbersTellAStory/
/// FIXTURE_CONTRACT.md: identity, period, sources, monetary rows, totals,
/// credit links, rule constants, worksheet decisions and expected results.
///
/// Immutable content, read after load. Money and rates are invariant decimal
/// strings ("500000.00", "0.20"), never floats; Level3ContentValidator checks
/// every binding and a Level_03 case without a valid block fails to load.
/// Expected decisions/fields are validator answers: they are never copied
/// into CaseData, the Bag, a client copy or the chapter checkpoint.
/// </summary>
[Serializable]
public class Level3CaseContent
{
    public int schemaVersion;
    public string contentRevision;
    public string caseId;
    public string clientId;
    public string taxpayer;
    public string periodStart;
    public string periodEnd;
    public int taxYear;
    /// <summary>"individual-professional" or "domestic-corporation".</summary>
    public string taxpayerKind;
    public string regimeId;
    public string roundingPolicyId;
    /// <summary>Company only; every fact explicit (missing is never "false").</summary>
    public Level3CorporateFacts corporateFacts = new Level3CorporateFacts();
    public List<Level3SourceDefinition> sources = new List<Level3SourceDefinition>();
    public List<Level3RowDefinition> rows = new List<Level3RowDefinition>();
    public List<Level3TotalDefinition> totals = new List<Level3TotalDefinition>();
    public List<Level3CreditLink> creditLinks = new List<Level3CreditLink>();
    public List<Level3RuleDefinition> rules = new List<Level3RuleDefinition>();
    /// <summary>Bea only: the twelve first-check decisions (validator answers).</summary>
    public List<Level3DecisionDefinition> worksheetDecisions = new List<Level3DecisionDefinition>();
    /// <summary>Validator answers for the computation chain.</summary>
    public List<Level3ExpectedField> expectedFields = new List<Level3ExpectedField>();

    /// <summary>True when the block is present (JsonUtility builds blank objects for missing ones).</summary>
    public bool IsSet => schemaVersion > 0;
}

[Serializable]
public class Level3CorporateFacts
{
    public string ordinaryRate;
    public string smallRate;
    public string smallIncomeCeiling;
    public string smallAssetCeiling;
    public string eligibilityOperator;
    public bool excludeQualifyingPremisesLand;
    public string assetRowId;
    public string incentiveRegime;
    public string operationsStart;
    public string birRegistrationDate;
    public int mcitFollowingYearOffset;
    public string mcitRate;
    public int firstMCITYear;

    public bool IsSet => !string.IsNullOrEmpty(ordinaryRate);
}

/// <summary>One source paper; documentName is the exact top-level documents[] name.</summary>
[Serializable]
public class Level3SourceDefinition
{
    public string sourceId;
    public string documentName;
    /// <summary>SupportingDocuments type (1/2/4/5/7/8), or 0 for a plain working paper.</summary>
    public int paperType;
    public string reference;
    public string periodStart;
    public string periodEnd;
    public string issuer;
    public string party;
    public string status;
}

/// <summary>One printed amount: fieldKey is the semanticKey inside its source's document.</summary>
[Serializable]
public class Level3RowDefinition
{
    public string rowId;
    public string sourceId;
    public string fieldKey;
    public string transactionId;
    public string amount;
    /// <summary>"-", "supports:&lt;rowId&gt;", "claim:&lt;rowId&gt;" or "subset:&lt;rowId&gt;".</summary>
    public string relation;
}

/// <summary>A printed total as an assertion over named rows of the same source (not a new transaction).</summary>
[Serializable]
public class Level3TotalDefinition
{
    public string sourceId;
    public string fieldKey;
    public List<string> componentRowIds = new List<string>();
    public string amount;
}

/// <summary>Credit row -> its certificate income basis -> the already counted income row.</summary>
[Serializable]
public class Level3CreditLink
{
    public string creditRowId;
    public string incomeRowId;
    public string includedInRowId;
}

/// <summary>A rule band or constant: baseTax + rate * (taxable - lower); upper "" = unbounded.</summary>
[Serializable]
public class Level3RuleDefinition
{
    public string ruleId;
    public string lower;
    public string upper;
    public string baseTax;
    public string rate;
}

[Serializable]
public class Level3DecisionDefinition
{
    public string decisionId;
    public string expected;
}

[Serializable]
public class Level3ExpectedField
{
    public string field;
    public string amount;
}
