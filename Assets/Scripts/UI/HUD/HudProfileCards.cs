using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Office_Level1 taxpayer profiles as paper windows (one FloatingWindow per
/// profile, the ProfileCardFace design), sharing window behaviour with the
/// Notes paper and the Case Folder: open together, touched one on top,
/// draggable, paper motion.
///
/// TWO WAYS IN AND OUT:
/// - Tap the profile icon: cards fly out to their places / fly back in.
/// - Drag a stack out of the icon (HudIconDragOpen + HudDragItem); dropping
///   it scatters the cards from the drop point to their places.
/// - Return by drag: press and HOLD any card still for HoldSeconds (long on
///   purpose, so normal moving is not affected); the cards gather into a
///   stack under the finger. Drop the stack on the profile icon to put
///   them away, or anywhere else to scatter them back.
///
/// While any card is open the player is held still (HudWindowLock); the HUD
/// stays visible. Read-only: profiles come from CollectedProfilesWorldPanel.
/// </summary>
public sealed class HudProfileCards : MonoBehaviour
{
    private const float CardWidth = 540f;
    private const float CardHeight = 330f;
    private const float HoldSeconds = 1.2f;
    private const float HoldRingDelay = 0.3f;
    private const float HoldSlop = 22f;
    private const float GatherDuration = 0.25f;
    private const float ScatterDuration = 0.35f;
    private static readonly float[] Tilts = { -3f, 2.4f, 1.6f, -2.2f };

    private enum Mode { Idle, Gathering, Stacked, Scattering }

    private RectTransform layer;
    private RectTransform profileIcon;
    private readonly List<FloatingWindow> cards = new List<FloatingWindow>();
    private readonly List<Vector2> slots = new List<Vector2>();
    private readonly List<Vector2> fromPositions = new List<Vector2>();
    private bool holding;

    // Long-press gather state.
    private Mode mode = Mode.Idle;
    private FloatingWindow pressedCard;
    private bool pressing;
    private float pressTime;
    private Vector2 pressStart;
    private Vector2 pointer;
    // The press's own pointer record: the input module keeps its position
    // up to date while the finger/mouse is down, so no device polling.
    private PointerEventData pressData;
    private float modeTime;
    private Image holdRing;

    public static HudProfileCards Create(Transform hudCanvas, RectTransform profileIcon)
    {
        RectTransform layer = HudTheme.CreateRect("Profile Cards", hudCanvas);
        HudTheme.Stretch(layer);
        var view = layer.gameObject.AddComponent<HudProfileCards>();
        view.layer = layer;
        view.profileIcon = profileIcon;
        view.BuildHoldRing(hudCanvas);
        TaxpayerProfileCollectionHUD.IconClickOverride = view.HandleIconTapped;
        return view;
    }

    private void OnDestroy()
    {
        if (TaxpayerProfileCollectionHUD.IconClickOverride == (System.Func<bool>)HandleIconTapped)
            TaxpayerProfileCollectionHUD.IconClickOverride = null;
        if (holdRing != null) Destroy(holdRing.transform.parent.gameObject);
        ReleaseHold();
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    private bool HandleIconTapped()
    {
        if (AnyCardOpen())
        {
            CloseAll();
            return true;
        }

        List<TaxpayerProfile> profiles = CurrentProfiles();
        if (profiles.Count == 0) return true; // nothing collected yet: nothing to show

        OpenCards(profiles, null);
        return true;
    }

    /// <summary>True when a profile stack can be dragged out of the icon.</summary>
    public bool CanDragOut() => !AnyCardOpen() && CurrentProfiles().Count > 0;

    /// <summary>Read-only: true while any profile card is open (used by the level guide).</summary>
    public bool IsShowingCards => AnyCardOpen();

    /// <summary>Dropped stack: scatter the cards from the drop point.</summary>
    public bool OpenFromDrop(Vector2 screenPosition)
    {
        if (!CanDragOut()) return false;
        OpenCards(CurrentProfiles(), screenPosition);
        return true;
    }

    public static List<TaxpayerProfile> CurrentProfiles()
    {
        if (CollectedProfilesWorldPanel.Instance != null)
            return CollectedProfilesWorldPanel.Instance.GetProfilesToShow();

        var list = new List<TaxpayerProfile>();
        if (TaxpayerProfileCollectionHUD.Instance != null)
            list.AddRange(TaxpayerProfileCollectionHUD.Instance.CollectedProfiles);
        return list;
    }

    private void OpenCards(List<TaxpayerProfile> profiles, Vector2? origin)
    {
        bool wasOpen = holding;
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] != null) Destroy(cards[i].gameObject);
        cards.Clear();
        slots.Clear();
        mode = Mode.Idle;

        int rows = Mathf.CeilToInt(profiles.Count / 2f);
        for (int i = 0; i < profiles.Count; i++)
        {
            FloatingWindow card = BuildCard(profiles[i], i);
            cards.Add(card);

            // Fan the cards out like papers laid on a desk.
            Vector2 offset = Vector2.zero;
            if (profiles.Count > 1)
            {
                int col = i % 2;
                int row = i / 2;
                offset = new Vector2((col - 0.5f) * (CardWidth * 1.04f) + (row % 2 == 0 ? -10f : 12f),
                    ((rows - 1) * 0.5f - row) * (CardHeight * 1.05f));
            }
            slots.Add(offset);

            if (origin.HasValue) card.SetPaperOpenOrigin(origin.Value);
            card.SetPaperOpenDelay(i * 0.07f);
            card.OpenWindowWithOffset(offset);
        }

        if (!wasOpen && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX();
        HudWindowLock.Acquire(this);
        holding = true;
    }

    /// <summary>Closes every open profile card (e.g. Office Hours' next morning).</summary>
    public void CloseCards() => CloseAll();

    private void CloseAll()
    {
        bool wasOpen = holding;
        mode = Mode.Idle;
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] != null && cards[i].IsOpen()) cards[i].CloseWindow();
        ReleaseHold();
        if (wasOpen && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX(true);
    }

    private bool AnyCardOpen()
    {
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] != null && cards[i].IsOpen()) return true;
        return false;
    }

    private void ReleaseHold()
    {
        if (!holding) return;
        holding = false;
        HudWindowLock.Release(this);
    }

    // =========================================================
    // LONG PRESS: GATHER -> STACK -> PUT AWAY / SCATTER
    // =========================================================

    internal void CardPressed(FloatingWindow card, PointerEventData data)
    {
        if (mode != Mode.Idle || data == null) return;
        pressedCard = card;
        pressData = data;
        pressing = true;
        pressTime = 0f;
        pressStart = data.position;
        pointer = data.position;
    }

    internal void CardDragged(FloatingWindow card, PointerEventData data)
    {
        if (card != pressedCard || data == null) return;
        Vector2 screen = data.position;
        pointer = screen;
        // Moving the card is a normal move, not a gather.
        if (mode == Mode.Idle && pressing && (screen - pressStart).sqrMagnitude > HoldSlop * HoldSlop)
        {
            pressing = false;
            ShowRing(false);
        }
    }

    internal void CardReleased(FloatingWindow card, PointerEventData data)
    {
        if (card != pressedCard || data == null) return;
        HandleRelease(data.position);
        pressData = null;
    }

    private void HandleRelease(Vector2 screen)
    {
        pointer = screen;
        pressing = false;
        ShowRing(false);

        if (mode != Mode.Gathering && mode != Mode.Stacked) return;

        bool onIcon = profileIcon != null &&
                      RectTransformUtility.RectangleContainsScreenPoint(profileIcon, screen, null);
        if (onIcon)
        {
            Debug.Log("[ProfileCards] Stack dropped on the profile icon: putting the cards away.");
            CloseAll();
            return;
        }

        Debug.Log("[ProfileCards] Stack released away from the icon: scattering back.");

        // Scatter back to their places.
        CaptureFrom();
        mode = Mode.Scattering;
        modeTime = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Track the press through its own pointer record (kept current by the
        // input module while the pointer is down).
        if (pressData != null && (pressing || mode == Mode.Gathering || mode == Mode.Stacked))
        {
            pointer = pressData.position;
            if (pressing && mode == Mode.Idle && (pointer - pressStart).sqrMagnitude > HoldSlop * HoldSlop)
            {
                pressing = false;
                ShowRing(false);
            }
        }

        if (pressing && mode == Mode.Idle)
        {
            pressTime += dt;
            if (pressTime >= HoldRingDelay)
            {
                ShowRing(true);
                PlaceRing(pointer, (pressTime - HoldRingDelay) / (HoldSeconds - HoldRingDelay));
            }
            if (pressTime >= HoldSeconds) BeginGather();
        }

        if (mode == Mode.Scattering)
        {
            modeTime += dt;
            float t = HudTheme.EaseOutCubic(modeTime / ScatterDuration);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var rect = (RectTransform)cards[i].transform;
                rect.anchoredPosition = Vector2.Lerp(fromPositions[i], slots[i], t);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(Tilts[i % Tilts.Length] * 0.3f, Tilts[i % Tilts.Length], t));
            }
            if (modeTime >= ScatterDuration) mode = Mode.Idle;
        }

        // Cards are closed one by one (dragged onto the icon, or X): give
        // control back once the last one is put away.
        if (holding && !AnyCardOpen()) ReleaseHold();
    }

    private void LateUpdate()
    {
        // Runs after FloatingWindow's own drag so the stack wins.
        if (mode != Mode.Gathering && mode != Mode.Stacked) return;

        Vector2 local = ToLocal(pointer);
        float t = 1f;
        if (mode == Mode.Gathering)
        {
            modeTime += Time.unscaledDeltaTime;
            t = HudTheme.EaseOutCubic(modeTime / GatherDuration);
            if (modeTime >= GatherDuration) mode = Mode.Stacked;
        }

        int stackIndex = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null || !cards[i].IsOpen()) continue;
            var rect = (RectTransform)cards[i].transform;
            int depth = cards[i] == pressedCard ? 0 : ++stackIndex;
            Vector2 stackPos = local + new Vector2(7f, -7f) * depth;
            rect.anchoredPosition = Vector2.Lerp(fromPositions[i], stackPos, t);
            float tilt = Tilts[i % Tilts.Length] * 0.3f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(Tilts[i % Tilts.Length], tilt, t));
        }
    }

    private void BeginGather()
    {
        Debug.Log("[ProfileCards] Hold complete: gathering the cards into a stack.");
        pressing = false;
        ShowRing(false);
        CaptureFrom();
        mode = Mode.Gathering;
        modeTime = 0f;

        // Held card ends up on top of the stack.
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] != null && cards[i] != pressedCard && cards[i].IsOpen()) cards[i].BringToFront();
        if (pressedCard != null) pressedCard.BringToFront();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void CaptureFrom()
    {
        fromPositions.Clear();
        for (int i = 0; i < cards.Count; i++)
            fromPositions.Add(cards[i] != null ? ((RectTransform)cards[i].transform).anchoredPosition : Vector2.zero);
    }

    private Vector2 ToLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
        return local;
    }

    // =========================================================
    // HOLD RING
    // =========================================================

    private void BuildHoldRing(Transform hudCanvas)
    {
        RectTransform ringLayer = HudTheme.CreateRect("Profile Hold Ring", hudCanvas);
        HudTheme.Stretch(ringLayer);
        var canvas = ringLayer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 2650;

        RectTransform ring = HudTheme.CreateRect("Ring", ringLayer);
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        ring.sizeDelta = new Vector2(96f, 96f);
        holdRing = HudTheme.AddImage(ring, HudTheme.Circle, new Color(1f, 1f, 1f, 0.85f));
        holdRing.type = Image.Type.Filled;
        holdRing.fillMethod = Image.FillMethod.Radial360;
        holdRing.fillOrigin = (int)Image.Origin360.Top;
        RectTransform hole = HudTheme.CreateRect("Hole", ring);
        HudTheme.Stretch(hole, 9f);
        HudTheme.AddImage(hole, HudTheme.Circle, new Color(0.1f, 0.14f, 0.2f, 0.35f));
        ring.gameObject.SetActive(false);
    }

    private void ShowRing(bool show)
    {
        if (holdRing != null && holdRing.gameObject.activeSelf != show) holdRing.gameObject.SetActive(show);
    }

    private void PlaceRing(Vector2 screen, float progress)
    {
        if (holdRing == null) return;
        var ringLayer = (RectTransform)holdRing.transform.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ringLayer, screen, null, out Vector2 local);
        holdRing.rectTransform.anchoredPosition = local;
        holdRing.fillAmount = Mathf.Clamp01(progress);
    }

    // =========================================================
    // CARD
    // =========================================================

    private FloatingWindow BuildCard(TaxpayerProfile profile, int index)
    {
        RectTransform card = HudTheme.CreateRect("Profile Card " + (index + 1), layer);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(CardWidth, CardHeight);

        // Invisible hit area for tap-to-front, dragging and the long press.
        Image hit = card.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        ProfileCardFace.Build(card, profile.TaxpayerName, profile.Description,
            ProfileCardFace.NumberOf(profile.TaxpayerName), new Vector2(CardWidth, CardHeight), true, out Button close, 1.1f);

        // Window behaviour shared with Notes and the Case Folder.
        FloatingWindow window = card.gameObject.AddComponent<FloatingWindow>();
        card.gameObject.AddComponent<GraphicRaycaster>();
        if (profileIcon != null) window.SetReturnIcon(profileIcon);
        window.UsePaperMotion(Tilts[index % Tilts.Length], profileIcon);
        if (close != null) close.onClick.AddListener(window.CloseWindow);

        var hold = card.gameObject.AddComponent<ProfileCardHold>();
        hold.Owner = this;
        hold.Window = window;

        card.gameObject.SetActive(false);
        return window;
    }
}
