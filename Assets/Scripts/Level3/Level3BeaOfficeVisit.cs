using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bea's walk from her plaza table to the office (Level 3 T08), on her actor
/// beside Level3BeaInteractable. One owner for her body: NpcLife's quest
/// travel (the same move Level 2's Rosa makes), never a second mover.
///
/// - Live: a short beat after her worksheet is collected (the chapter reaches
///   BeaComputation) she walks town_life.json's toOffice doorway points to the
///   conference room's client spot and waits there. A conversation pauses the
///   walk (NpcLife) and the walk waits for a talk that is under way.
/// - Resume: a chapter that was already at her computation (or later) when
///   the scene loaded finds her in the room at once (no walk, nothing shown).
/// - The room's stand-in client steps aside for her (ClientConsultation).
/// - A route that cannot be walked ends with NpcLife's out-of-view placement;
///   without NpcLife (no model) or without route points she is placed only
///   while neither she nor the spot is on screen. Never a rendered teleport.
/// Arriving awards nothing and saves nothing: her place follows the phase.
/// </summary>
public sealed class Level3BeaOfficeVisit : MonoBehaviour
{
    public static Level3BeaOfficeVisit Instance { get; private set; }

    /// <summary>She stands at the conference room's client spot.</summary>
    public bool InRoom { get; private set; }
    /// <summary>She is walking there (a conversation pauses her).</summary>
    public bool OnTheWay { get; private set; }

    private Level3BeaTable table;
    private NpcLife life;
    private Transform spot;
    private bool decided;
    private bool placeWhenUnseen;
    private float leaveAt = -1f;
    private Renderer[] renderers;

    public void Init(Level3BeaTable owner)
    {
        table = owner;
        life = GetComponent<NpcLife>();
    }

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (InRoom || table == null || table.Content == null) return;
        Level3Checkpointer chapter = Level3Checkpointer.Active;
        if (chapter == null || !chapter.IsReady || chapter.State == null) return;
        Level3Phase phase = chapter.Phase;
        if (!decided)
        {
            decided = true;
            // Resumed at (or past) her computation: she is already waiting in the room.
            if (phase >= Level3Phase.BeaComputation)
            {
                PlaceInRoomNow();
                return;
            }
        }
        if (placeWhenUnseen)
        {
            if (!Visible() && !SpotVisible()) PlaceInRoomNow();
            return;
        }
        if (OnTheWay || phase < Level3Phase.BeaComputation) return;
        if (leaveAt < 0f) leaveAt = Time.unscaledTime + Mathf.Max(0f, table.Content.leaveDelaySeconds);
        if (Time.unscaledTime < leaveAt || (table.Bea != null && table.Bea.Talking) || DialogueUI.IsAnyDialogueActive) return;
        StartWalk();
    }

    private bool FindSpot()
    {
        if (spot != null) return true;
        spot = PlayerPlacement.Find(gameObject.scene, table.Content.officeSpotPath);
        if (spot == null) Debug.LogWarning($"[Level3] Bea's office spot '{table.Content.officeSpotPath}' was not found; she stays at her table.");
        return spot != null;
    }

    private void StartWalk()
    {
        if (!FindSpot())
        {
            // Nowhere to go: she keeps her table (the credit step still happens with her there).
            InRoom = false;
            enabled = false;
            return;
        }
        var stops = new List<Vector3>();
        if (table.Content.toOffice != null)
            foreach (TownPoint point in table.Content.toOffice)
                if (point != null && point.IsSet) stops.Add(NpcLife.Ground(point.Position));
        // The room's stand-in steps aside now, while the player is still out in the plaza.
        ClientConsultation.StandInStepsAside(spot);
        if (life == null || stops.Count == 0)
        {
            placeWhenUnseen = true;
            Debug.Log("[Level3] Bea goes to the conference room (placed when unseen: no walk owner or no route).");
            return;
        }
        stops.Add(RoomFeet());
        OnTheWay = true;
        life.TravelForQuest(stops, RoomFacing(), Arrive);
        Debug.Log("[Level3] Bea walks to the conference room.");
    }

    private void Arrive()
    {
        OnTheWay = false;
        InRoom = true;
        Debug.Log("[Level3] Bea waits in the conference room.");
    }

    /// <summary>At the room's client spot at once (a resume under the load fade, or unseen).</summary>
    private void PlaceInRoomNow()
    {
        if (!FindSpot()) { enabled = false; return; }
        ClientConsultation.StandInStepsAside(spot);
        if (life != null) life.PlaceForQuest(RoomFeet(), RoomFacing());
        else transform.SetPositionAndRotation(RoomFeet(), RoomFacing());
        Physics.SyncTransforms();
        placeWhenUnseen = OnTheWay = false;
        InRoom = true;
        Debug.Log("[Level3] Bea is in the conference room.");
    }

    private Vector3 RoomFeet()
    {
        Vector3 feet = spot.position;
        if (Physics.Raycast(spot.position + Vector3.up * 1f, Vector3.down, out RaycastHit floor, 2.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            feet.y = floor.point.y;
        return feet;
    }

    /// <summary>She faces the way the room's client spot faces (into the room, toward the door).</summary>
    private Quaternion RoomFacing() => Quaternion.Euler(0f, spot.eulerAngles.y, 0f);

    private bool Visible()
    {
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
            if (r != null && r.enabled && r.isVisible) return true;
        return false;
    }

    private bool SpotVisible()
    {
        Camera view = HeldOnScreen.ActiveCamera(Camera.main);
        if (view == null || spot == null) return false;
        Vector3 v = view.WorldToViewportPoint(spot.position + Vector3.up * 0.9f);
        return v.z > 0f && v.x > -0.1f && v.x < 1.1f && v.y > -0.1f && v.y < 1.1f &&
               !Physics.Linecast(view.transform.position, spot.position + Vector3.up * 0.9f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
}
