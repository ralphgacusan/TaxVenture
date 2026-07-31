using UnityEngine;

/// <summary>
/// PURPOSE:
/// Central registry for the two fixed workspace zones (Left/Right), per R7:
/// "LEFT SIDE: Tax Code, Computer. RIGHT SIDE: Case Folder." This is the
/// SINGLE source of truth for which screen region is which — any future
/// interactable panel just asks WorkspaceLayoutManager.Instance for the
/// zone it belongs in, rather than hardcoding its own screen position.
///
/// RESPONSIBILITIES:
/// - Hold references to LeftZone / RightZone WorkspaceZone components
/// - Provide simple accessors so panels don't need direct references to
///   each other's zones
///
/// CONNECTS WITH:
/// - CaseFolderUI: uses RightZone
/// - TaxCodeBookUI, ComputerHomeUI: use LeftZone
/// - Future interactables: register into LeftZone or RightZone as appropriate
/// </summary>
public class WorkspaceLayoutManager : MonoBehaviour
{
    public static WorkspaceLayoutManager Instance { get; private set; }

    [SerializeField] private WorkspaceZone leftZone;
    [SerializeField] private WorkspaceZone rightZone;

    public WorkspaceZone LeftZone => leftZone;
    public WorkspaceZone RightZone => rightZone;

    private void Awake()
    {
        Instance = this;
    }
}