using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// One section of the Tax Code Book (e.g. "Section 1 - Residency Status").
/// Plain serializable data class so it can be edited as a list inside a
/// ScriptableObject in the Inspector, without needing its own asset file
/// per section.
///
/// CONNECTS WITH:
/// - TaxCodeBookData (ScriptableObject): holds a list of these
/// - TaxCodeBookUI: reads Heading/Body to render on the shared paper
/// </summary>
[Serializable]
public class TaxCodeSection
{
    public string heading;
    [TextArea(4, 20)]
    public string body;
}