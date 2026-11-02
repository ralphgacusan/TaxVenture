using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Bea's withholding certificate (B-WHT, Level 3 T08) as ONE physical paper
/// the player holds: the standard record paper (ClientRecordPaper, Type 8
/// face from Level3RecordRules: real thickness, edge, clip and print), owned
/// by this holder, under Level3Gameplay/DeskCalculator.
///
/// - Built only once B-WHT is acquired (saved by the handover first). Its
///   identity is the case's source id, like every paper in the Case Folder.
/// - Handover: Raise(from Bea's hands) lifts it out of her hands into a
///   readable pose; the conversation lowers it when its line moves on (a
///   paper takes no touches while a dialogue is open).
/// - At the desk: the certificate tab on Bea's computation sheet raises it in
///   front of the tools; tap it (or tap anywhere off it) to lower it again.
///   While it is up the tools take no touches (one input owner).
/// - Lowered, it rests just below the view ("in hand") and is hidden; nothing
///   is ever left floating. Leaving the desk, settings, a conversation that
///   ends or losing focus lowers it; disabling puts it straight away.
/// Nothing here saves, grades or links the credit (the sheet does).
/// </summary>
public sealed class Level3CertificatePaper : MonoBehaviour, IClientRecordHolder
{
    public const string CueRead = "CertificateRead";
    /// <summary>The paper's printed width in metres (a Type 8 sheet, 700 x 980 face units).</summary>
    public const float PaperWidth = 0.18f;
    private const float ReadDepth = 0.5f;
    private const float HandDrop = 0.75f;

    /// <summary>Where it is being read: in a conversation (clear of the dialogue box) or at the desk.</summary>
    public enum Context { None, Conversation, Desk }

    public static Level3CertificatePaper Instance { get; private set; }

    /// <summary>It came back down (after a lift): the conversation or the desk goes on.</summary>
    public event Action Lowered;

    public Context Showing { get; private set; }

    /// <summary>Up, lifting, held or on its way down (not resting in hand).</summary>
    public bool IsUp => paper != null && !paper.IsAttached;

    private Transform hand;
    private Transform workspace;
    private ClientRecordPaper paper;
    private int lowerRequested = -1;

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (paper != null && !paper.IsAttached) paper.Gather();
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
    }

    private void OnDestroy()
    {
        if (workspace != null) Destroy(workspace.gameObject);
        if (hand != null) Destroy(hand.gameObject);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && paper != null) paper.CancelDrag();
    }

    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;

    /// <summary>The certificate is Bea's acquired source and its paper can be shown.</summary>
    public bool Available => Level3CertificateRules.Acquired(Chapter) && EnsureBuilt();

    private bool EnsureBuilt()
    {
        if (paper != null) return true;
        Level3ChapterCheckpoint chapter = Chapter;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (!Level3CertificateRules.Acquired(chapter) || definition == null || definition.caseId != Level3ChapterRules.BeaCaseId) return false;
        List<ClientRecord> records = Level3RecordRules.Build(definition, new[] { Level3ChapterRules.BeaCreditSource });
        if (records.Count == 0)
        {
            Debug.LogWarning("[Level3] Bea's withholding certificate (B-WHT) could not be printed from her case content.");
            return false;
        }
        ClientRecord record = records[0];
        hand = new GameObject("Certificate In Hand").transform;
        hand.gameObject.layer = gameObject.layer;
        SceneManager.MoveGameObjectToScene(hand.gameObject, gameObject.scene);
        workspace = new GameObject("Certificate Out").transform;
        SceneManager.MoveGameObjectToScene(workspace.gameObject, gameObject.scene);
        Vector2 units = SupportingDocumentFace.SizeOf(record.type);
        var size = new Vector2(PaperWidth, PaperWidth * units.y / units.x);
        paper = ClientRecordPaper.Create(this, hand, workspace, record, size, gameObject.layer);
        hand.gameObject.SetActive(false);
        Debug.Log($"[Level3] Bea's certificate paper is ready ({record.id}, Type 8).");
        return true;
    }

    // =========================================================
    // RAISE / LOWER
    // =========================================================

    /// <summary>Lifts it into its readable pose; from = where it starts (Bea's hands at the handover), else from the hand below the view.</summary>
    public bool Raise(Context context, Vector3? from = null)
    {
        if (!Available) return false;
        Showing = context;
        lowerRequested = -1;
        if (!paper.IsAttached) return true;
        Camera camera = ViewCamera;
        if (camera == null) return false;
        hand.gameObject.SetActive(true);
        PlaceHand(camera);
        if (from.HasValue)
        {
            // Held out toward the player at its real size, then lifted to read.
            hand.position = from.Value;
            hand.localScale = Vector3.one;
        }
        paper.TakeOut();
        // The holder's own clip and shadow go back below the view at once; the paper flies on its own.
        PlaceHand(camera);
        return true;
    }

    /// <summary>Back down into the hand (animated); Lowered follows when it has landed.</summary>
    public void Lower()
    {
        lowerRequested = -1;
        if (paper == null || paper.IsAttached) return;
        paper.CancelDrag();
        paper.PutBack();
    }

    /// <summary>Straight down without motion (an interruption).</summary>
    public void Gather()
    {
        lowerRequested = -1;
        if (paper != null && !paper.IsAttached) paper.Gather();
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
    }

    private void LateUpdate()
    {
        if (paper == null || hand == null || !hand.gameObject.activeSelf) return;
        Camera camera = ViewCamera;
        if (camera != null) PlaceHand(camera);
        // A tap on the paper itself lowers it (a drag that started this frame does not).
        if (lowerRequested == Time.frameCount && paper.IsSettledOut) Lower();
        if (Showing == Context.Desk && IsUp && (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || !AtDesk)) Lower();
        // A conversation that ended without lowering it (cancelled, interrupted) never leaves it up.
        if (Showing == Context.Conversation && IsUp && !DialogueUI.IsAnyDialogueActive) Lower();
    }

    private static bool AtDesk => CameraController.Instance != null &&
        CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;

    /// <summary>Just below the bottom of the view, facing the camera like the reading pose, at the reading pose's scale (it slides down out of view).</summary>
    private void PlaceHand(Camera camera)
    {
        Quaternion facing = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        float visibleH = 2f * ReadDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        hand.SetPositionAndRotation(camera.transform.position + camera.transform.forward * ReadDepth - camera.transform.up * (visibleH * HandDrop + paper.Size.y),
            facing);
        hand.localScale = Vector3.one * ReadingScale(camera);
    }

    private float ReadingScale(Camera camera)
    {
        bool talk = Showing == Context.Conversation;
        float visibleH = 2f * ReadDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleW = visibleH * Mathf.Max(0.1f, camera.aspect);
        return Mathf.Min(visibleH * (talk ? 0.56f : 0.8f) / paper.Size.y, visibleW * (talk ? 0.62f : 0.72f) / paper.Size.x);
    }

    // =========================================================
    // IClientRecordHolder
    // =========================================================

    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);

    public void ReadingPose(ClientRecordPaper record, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera camera = ViewCamera;
        Rect pixels = camera.pixelRect;
        // In a conversation it reads above the dialogue box; at the desk, centred in front of the tools.
        float y = Showing == Context.Conversation ? 0.62f : 0.52f;
        float depth = Mathf.Max(camera.nearClipPlane + 0.12f, ReadDepth);
        position = camera.ScreenToWorldPoint(new Vector3(pixels.x + pixels.width * 0.5f, pixels.y + pixels.height * y, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        worldScale = ReadingScale(camera);
    }

    public void Extracted(ClientRecordPaper record)
    {
        record.SetEventCamera(ViewCamera);
        record.BringWindowToFront();
    }

    public void PaperSettled(ClientRecordPaper record) => GameplayEvents.RaiseGuideCue(CueRead);

    public void PaperLanded(ClientRecordPaper record)
    {
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
        Lowered?.Invoke();
    }

    /// <summary>Released low on the screen: it goes back down; anywhere else it stays up where it was dropped.</summary>
    public bool WantsReturn(ClientRecordPaper record, Vector2 screen, Action ready)
    {
        Camera camera = ViewCamera;
        if (camera == null || screen.y > camera.pixelRect.y + camera.pixelRect.height * 0.18f) return false;
        ready?.Invoke();
        return true;
    }

    /// <summary>A tap while it is up asks to lower it (checked after the frame's input, so a drag is never mistaken for it).</summary>
    public bool BroughtToFront(ClientRecordPaper record)
    {
        record.BringWindowToFront();
        lowerRequested = Time.frameCount;
        return false;
    }

    public void PaperFlipped(ClientRecordPaper record, bool backUp) { }
    public void PointerDown(ClientRecordPaper record, PointerEventData eventData) { }
    public void PointerUp(ClientRecordPaper record, PointerEventData eventData) { }
    public void PointerMoved(ClientRecordPaper record) { }
    public bool IsGrouped(ClientRecordPaper record) => false;
    public void BeginGroupDrag(ClientRecordPaper record, PointerEventData eventData) { }
    public void DragGroup(PointerEventData eventData) { }
    public void EndGroupDrag(PointerEventData eventData) { }
}
