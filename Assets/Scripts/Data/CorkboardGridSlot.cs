using UnityEngine;

/// <summary>
/// PURPOSE:
/// Defines one fixed position in the Corkboard's 2x10 grid. Plain data —
/// no logic — used by CorkboardDocumentSpawner to know exactly where each
/// document should be placed, replacing the old random-scatter approach.
/// </summary>
[System.Serializable]
public class CorkboardGridSlot
{
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
}