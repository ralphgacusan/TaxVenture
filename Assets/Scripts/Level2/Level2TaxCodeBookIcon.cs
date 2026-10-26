using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Level 2's Tax Code Book icon on the right-side HUD column (below Notes
/// and Case Folder, above Annotated Papers). It starts EMPTY: the book is on
/// the office desk, and Arthur mentions (as a clue, not an order) that it
/// can be dragged onto the icon to carry it. The dropped book flies into the icon
/// (the desk copy is parked, hidden) and the icon fills in. From then on the
/// book travels with the player, like the Case Folder:
///
/// - The same system as the Case Folder icon (CaseFolder3DView): tap the
///   icon and the REAL open book flies out of it with the desk chair's
///   framing, centred on screen; tap again and it flies back in. Drag out
///   of the icon and drop: it opens at the drop point. Drag the open book by
///   its page (red-tinted while held) and release it over the icon to put
///   it away.
/// - Unlike a world-placed document it is held relative to the camera, so
///   walking, looking around or the controls lock settling the camera never
///   leave it tilted, upside down or behind the player.
/// - The open desk book can be dragged onto the icon too (stored the same way).
/// - Seated at the office desk, the carried book can go back onto the desk
///   stack: drag it out of the icon (or drag the floating book) and drop it
///   on the desk; it settles in its desk spot and the icon empties.
/// - It always opens on page 1, and always draws in front (composited).
/// - Works while idle, walking, at the office desk and at Rosa's records
///   desk (Mark the Money!). Its desk-only drag/return handlers are paused
///   while it floats and restored afterwards.
/// - An empty icon never lets a book be dragged out; a tap explains where
///   the book is.
///
/// Reference only: it never changes the case, rewards or progress. Stored
/// state lives in CaseReviewState.taxBookStored.
/// </summary>
[DefaultExecutionOrder(450)]
public sealed class Level2TaxCodeBookIcon : MonoBehaviour
{
    public const string ChildName = "TaxCodeBookIcon";
    public const string IconName = "TaxCodeBookIcon";
    public static readonly Vector2 IconPosition = new Vector2(10f, -300f);

    private const string ClosedBookPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/TaxCodeBook";
    private const string ViewpointPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";
    private const float OpenDuration = 0.42f;
    private const float CloseDuration = 0.3f;
    private const float IconScale = 0.12f;
    private const float StoreDuration = 0.42f;
    private static readonly Color EmptyTint = new Color(0.6f, 0.63f, 0.68f, 0.55f);

    private enum Phase { Closed, Opening, Open, Closing }

    public static Level2TaxCodeBookIcon Instance { get; private set; }

    private Canvas hudCanvas;
    private RectTransform icon;
    private HudIconDragOpen iconDrag;
    private HudDragItem dragVisual;
    private TaxCodeBook3DDrag deskBook;
    private Transform viewpoint;
    private Image iconImage;
    private float punchStarted = -10f;
    private bool storing;

    // Floating view state.
    private Phase phase = Phase.Closed;
    private Transform book;
    private Transform savedParent;
    private Vector3 savedPosition, savedScale;
    private Quaternion savedRotation;
    private bool savedActive;
    private Behaviour[] pausedHandlers;
    private CameraController.CameraMode openedInMode;
    private float elapsed;
    // Poses are camera-local (position and rotation relative to the main camera).
    private Vector3 fromPosition, toPosition, pinPosition;
    private Quaternion fromRotation, toRotation, pinRotation;
    private float fromScale, toScale, pinScale;

    public bool IsFloating => phase != Phase.Closed;

    /// <summary>The book is flying into or out of the icon (the review cutoff waits for it).</summary>
    public bool IsBusy => storing || phase == Phase.Opening || phase == Phase.Closing;

    /// <summary>
    /// Campaign Foundation T10: where the carried book lives outside Level 2.
    /// Level 1's hotbar keeps it as an inventory item that any slot can hold
    /// (HudHotbar). Null = Level 2's review, exactly as before.
    /// </summary>
    public interface IBookCarrier
    {
        /// <summary>The campaign owns the book (a slot, the bag or the drawer).</summary>
        bool Carried { get; }
        /// <summary>The finger is over the hotbar (a slot or the row): the desk book is carried from there.</summary>
        bool IsOverStoreTarget(Vector2 screen);
        /// <summary>Where the book flies for that drop (the slot it will take, or the Bag icon).</summary>
        RectTransform LandingFor(Vector2 screen);
        /// <summary>The book was carried (drop point) or put back on its desk stand (null).</summary>
        void SetCarried(bool carried, Vector2? dropScreen);
        void Say(string line);
    }

    public static IBookCarrier Carrier;

    private Vector2 storeScreen;
    private RectTransform flyTarget;

    /// <summary>The book is carried in the icon (saved with the review; Level 1: owned in the campaign inventory).</summary>
    public static bool Stored => Carrier != null ? Carrier.Carried : Level2OfficeStart.Review?.taxBookStored == true;

    private static void Say(string line)
    {
        if (Carrier != null) Carrier.Say(line);
        else Level2Say.Line(line, null, 5f);
    }

    /// <summary>Level 2: over this icon. Level 1: over the hotbar (any slot or the row).</summary>
    private bool OverStoreTarget(Vector2 screen) => Carrier != null ? Carrier.IsOverStoreTarget(screen) : OverIcon(screen);

    private void OnEnable()
    {
        Instance = this;
        TaxCodeBook3DDrag.TryStoreAt = StoreFromDesk;
        TaxCodeBook3DDrag.IsStoredElsewhere = IsBookStored;
        TaxCodeBookFloatingModel3D.TryStoreAt = StoreOpenFromDesk;
        Initialize();
    }

    private static bool IsBookStored() => Stored;

    private void Update()
    {
        if (icon == null || hudCanvas == null) Initialize();
        KeepDeskBookParked();
        UpdateIconLook();
    }

    // After the camera has moved this frame, before documents are captured (Doc3DLayer runs at 500).
    private void LateUpdate() => UpdateFloating();

    private void OnDisable()
    {
        if (phase != Phase.Closed) RestoreFloating(true);
        StopAllCoroutines();
        storing = false;
        if (TaxCodeBook3DDrag.TryStoreAt == (System.Func<TaxCodeBook3DDrag, Vector2, bool>)StoreFromDesk) TaxCodeBook3DDrag.TryStoreAt = null;
        if (TaxCodeBook3DDrag.IsStoredElsewhere == (System.Func<bool>)IsBookStored) TaxCodeBook3DDrag.IsStoredElsewhere = null;
        if (TaxCodeBookFloatingModel3D.TryStoreAt == (System.Func<TaxCodeBookFloatingModel3D, Vector2, bool>)StoreOpenFromDesk) TaxCodeBookFloatingModel3D.TryStoreAt = null;
        Cleanup();
        if (Instance == this) Instance = null;
    }

    // =========================================================
    // ICON
    // =========================================================

    private bool Initialize()
    {
        if (icon != null && hudCanvas != null) return true;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null || canvases[i].gameObject.scene != gameObject.scene || canvases[i].name != "HUD_Canvas") continue;
            hudCanvas = canvases[i];
            break;
        }
        if (hudCanvas == null) return false;
        Transform buttons = hudCanvas.transform.Find("PermanentHUD/IconButtons");
        if (buttons == null) return false;

        Transform existing = buttons.Find(IconName);
        if (existing != null) icon = existing as RectTransform;
        if (icon == null)
        {
            var iconObject = new GameObject(IconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            iconObject.layer = buttons.gameObject.layer;
            icon = (RectTransform)iconObject.transform;
            icon.SetParent(buttons, false);
            icon.anchorMin = icon.anchorMax = new Vector2(1f, 1f);
            icon.pivot = new Vector2(1f, 1f);
            icon.sizeDelta = new Vector2(120f, 120f);
            icon.anchoredPosition = IconPosition;
            icon.localScale = Vector3.one;
        }

        Image image = icon.GetComponent<Image>();
        image.sprite = HudIconArt.Get("book");
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Stored ? Color.white : EmptyTint;
        image.raycastTarget = true;
        iconImage = image;

        Button button = icon.GetComponent<Button>();
        button.targetGraphic = image;
        OfficeHudRuntime.StyleIconButton(button);
        button.onClick.RemoveListener(Toggle);
        button.onClick.AddListener(Toggle);

        iconDrag = icon.GetComponent<HudIconDragOpen>();
        if (iconDrag == null) iconDrag = icon.gameObject.AddComponent<HudIconDragOpen>();
        if (iconDrag.Visual == null)
        {
            dragVisual = HudDragItem.Create(hudCanvas.transform, "Tax Code Book", icon, new Vector2(230f, 290f), -4f, BuildClosedBook);
            iconDrag.Visual = dragVisual;
        }
        // An empty icon has nothing to drag out.
        iconDrag.CanDrag = () => Stored && !storing && CanOpen() && !IsOpen();
        iconDrag.OpenAt = screen => Open(screen);
        return true;
    }

    private void Cleanup()
    {
        if (iconDrag != null)
        {
            iconDrag.Visual = null;
            iconDrag.CanDrag = null;
            iconDrag.OpenAt = null;
            Destroy(iconDrag);
        }
        if (dragVisual != null) Destroy(dragVisual.gameObject);
        if (icon != null)
        {
            Button button = icon.GetComponent<Button>();
            if (button != null) button.onClick.RemoveListener(Toggle);
            Destroy(icon.gameObject);
        }
        icon = null;
        iconImage = null;
        iconDrag = null;
        dragVisual = null;
        hudCanvas = null;
    }

    /// <summary>Same modal rules as the other paper icons: never over a splash, lesson, reward card or cutoff.</summary>
    private static bool CanOpen()
    {
        GuideDirector guide = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        CaseReviewState review = Level2OfficeStart.Review;
        CameraController cc = CameraController.Instance;
        return !SideQuestSplash.IsShowing && Time.timeScale > 0f && !IncomeToolkit.TeachingPaused &&
               !HudRewardScreen.AnyOpen && (guide == null || !guide.IsBlocking) &&
               (Level2AnnotatedPapers.Instance == null || !Level2AnnotatedPapers.Instance.IsCollecting) &&
               (review?.clock == null || !review.clock.cutoffPending) &&
               (cc == null || IsWalking(cc.CurrentMode) || cc.CurrentMode == CameraController.CameraMode.Workstation);
    }

    /// <summary>Walking, or walking with HUD papers up (the paper viewer): the same place for the floating book.</summary>
    private static bool IsWalking(CameraController.CameraMode mode) =>
        mode == CameraController.CameraMode.FirstPerson || mode == CameraController.CameraMode.ProfileViewer;

    private bool IsOpen() => phase == Phase.Opening || phase == Phase.Open;

    /// <summary>Same toggle as the other icons: open in the centre, tap again to put it back.</summary>
    public void Toggle()
    {
        if (storing) return;
        if (phase == Phase.Opening || phase == Phase.Open)
        {
            CloseFloating();
            return;
        }
        if (phase == Phase.Closing) return;
        if (!Stored)
        {
            punchStarted = Time.unscaledTime;
            Say("Your Tax Code Book is on your desk. Drag it onto this icon to carry it with you.");
            return;
        }
        Open(null);
    }

    /// <summary>Opens the carried book in the centre of the screen (or at the drop point when dragged out).</summary>
    public bool Open(Vector2? screenPosition)
    {
        if (!Stored || storing || !CanOpen()) return false;
        // Dragged out of its slot (also onto the desk): it always opens first. The open book
        // then goes back on its desk stack, or back into the bar, by dropping it there.
        if (phase != Phase.Closed) return true;
        TaxCodeBook3DDrag desk = DeskBook();
        if (desk == null || desk.OpenBook == null) return false;
        if (desk.IsBookOpen) desk.ReturnToOriginalPlace(false);
        return OpenFloating(desk, screenPosition);
    }

    // =========================================================
    // STORING THE DESK BOOK
    // =========================================================

    /// <summary>TaxCodeBook3DDrag hook: the dragged desk book released over this icon flies into it.</summary>
    private bool StoreFromDesk(TaxCodeBook3DDrag desk, Vector2 screenPosition)
    {
        if (Stored || storing || desk == null || icon == null || (Carrier == null && Level2OfficeStart.Review == null)) return false;
        if (!OverStoreTarget(screenPosition)) return false;
        deskBook = desk;
        storeScreen = screenPosition;
        flyTarget = Carrier != null ? Carrier.LandingFor(screenPosition) : null;
        StartCoroutine(FlyIntoIcon(desk));
        return true;
    }

    private bool OverIcon(Vector2 screen)
    {
        icon.GetWorldCorners(IconCorners);
        Rect rect = Rect.MinMaxRect(IconCorners[0].x, IconCorners[0].y, IconCorners[2].x, IconCorners[2].y);
        float pad = rect.width * 0.35f;
        rect.xMin -= pad; rect.xMax += pad; rect.yMin -= pad; rect.yMax += pad;
        return rect.Contains(screen);
    }

    private System.Collections.IEnumerator FlyIntoIcon(TaxCodeBook3DDrag desk)
    {
        storing = true;
        Transform closed = desk.ClosedBook.transform;
        Camera main = MainCamera();
        Vector3 from = closed.position;
        Quaternion fromRotation = closed.rotation;
        Vector3 restScale = closed.localScale;
        float depth = main != null ? Vector3.Dot(from - main.transform.position, main.transform.forward) : 0.6f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
        for (float elapsed = 0f; elapsed < StoreDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / StoreDuration);
            float ease = t * t * (3f - 2f * t);
            Vector3 target = main != null ? IconWorldPoint(main, Mathf.Max(main.nearClipPlane + 0.05f, depth * (1f - 0.5f * ease)), from) : from;
            // A small hop toward the camera, then into the icon.
            closed.position = Vector3.Lerp(from, target, ease) + (main != null ? -main.transform.forward * 0.04f * Mathf.Sin(t * Mathf.PI) : Vector3.zero);
            closed.rotation = fromRotation * Quaternion.Euler(0f, 0f, 30f * ease);
            closed.localScale = restScale * Mathf.Lerp(1f, 0.12f, ease * ease);
            yield return null;
        }
        closed.localScale = restScale;
        desk.ParkClosedBook();
        CaseReviewState review = Level2OfficeStart.Review;
        if (Carrier != null) Carrier.SetCarried(true, storeScreen);
        else if (review != null) review.taxBookStored = true;
        GameplayEvents.RaiseGuideCue("TaxBookStored");
        storing = false;
        flyTarget = null;
        punchStarted = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
        Say("Your Tax Code Book is in its icon now. Tap it to read anywhere; tap again to put it away.");
    }

    /// <summary>TaxCodeBookFloatingModel3D hook: the open desk book dragged over this icon closes into it.</summary>
    private bool StoreOpenFromDesk(TaxCodeBookFloatingModel3D model, Vector2 screenPosition)
    {
        if (Stored || storing || icon == null || (Carrier == null && Level2OfficeStart.Review == null) || phase != Phase.Closed) return false;
        TaxCodeBook3DDrag desk = DeskBook();
        if (desk == null || !desk.IsBookOpen || !OverStoreTarget(screenPosition)) return false;
        storeScreen = screenPosition;
        flyTarget = Carrier != null ? Carrier.LandingFor(screenPosition) : null;
        StartCoroutine(FlyOpenBookIntoIcon(desk));
        return true;
    }

    private System.Collections.IEnumerator FlyOpenBookIntoIcon(TaxCodeBook3DDrag desk)
    {
        storing = true;
        Transform open = desk.OpenBook.transform;
        if (desk.BookUI != null && desk.BookUI.IsShowing) desk.BookUI.Hide();
        Camera main = MainCamera();
        Vector3 from = open.position;
        Quaternion fromRot = open.rotation;
        Vector3 restScale = open.localScale;
        float depth = main != null ? Vector3.Dot(from - main.transform.position, main.transform.forward) : 0.6f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / CloseDuration)
        {
            float ease = t * t;
            Vector3 target = main != null ? IconWorldPoint(main, depth, from) : from;
            open.SetPositionAndRotation(Vector3.Lerp(from, target, ease), fromRot * Quaternion.Euler(0f, 0f, -20f * ease));
            open.localScale = restScale * Mathf.Lerp(1f, IconScale, ease);
            yield return null;
        }
        open.localScale = restScale;
        desk.ParkOpenBook();
        CaseReviewState review = Level2OfficeStart.Review;
        if (Carrier != null) Carrier.SetCarried(true, storeScreen);
        else if (review != null) review.taxBookStored = true;
        GameplayEvents.RaiseGuideCue("TaxBookStored");
        storing = false;
        flyTarget = null;
        punchStarted = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
        Say("Your Tax Code Book is in its icon now. Tap it to read anywhere; tap again to put it away.");
    }

    /// <summary>
    /// Seated at the office desk and the point is on the book's own desk stack
    /// (its closed resting spot). Only there does an open book go back to the
    /// desk, so moving it around over the desk never puts it away by accident.
    /// </summary>
    private bool OverDeskStack(Vector2 screen)
    {
        CameraController cc = CameraController.Instance;
        Camera view = MainCamera();
        TaxCodeBook3DDrag desk = DeskBook();
        if (cc == null || view == null || desk == null || desk.ClosedBook == null || cc.CurrentMode != CameraController.CameraMode.Workstation) return false;
        Vector3 spot = view.WorldToScreenPoint(desk.RestingClosedPosition);
        return spot.z > 0f && Vector2.Distance(screen, spot) < Screen.height * DeskStackRadius;
    }

    /// <summary>How close to the closed book's desk spot a drop must be (fraction of the screen height).</summary>
    private const float DeskStackRadius = 0.14f;

    /// <summary>The carried book goes back onto its desk stack (closed, settling in place); the icon empties.</summary>
    private void ReturnToDesk()
    {
        TaxCodeBook3DDrag desk = DeskBook();
        CaseReviewState review = Level2OfficeStart.Review;
        if (desk == null || (review == null && Carrier == null)) return;
        if (Carrier != null) Carrier.SetCarried(false, null);
        else review.taxBookStored = false;
        desk.UnparkClosedBook();
        punchStarted = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
    }

    /// <summary>A carried book never reappears on the desk (desk resets and resumes included).</summary>
    private void KeepDeskBookParked()
    {
        if (!Stored || storing) return;
        TaxCodeBook3DDrag desk = DeskBook();
        if (desk != null && desk.ClosedBook != null && desk.ClosedBook.activeSelf) desk.ParkClosedBook();
    }

    // =========================================================
    // ICON LOOK
    // =========================================================

    /// <summary>Empty: grey. Filled: full colour. A pop when it changes or an empty icon is tapped.</summary>
    private static readonly Color OutTint = new Color(1f, 1f, 1f, 0.38f);

    private void UpdateIconLook()
    {
        if (icon == null || iconImage == null) return;
        // While the book is out (floating), its icon waits dimmed; it lights up again when the book is back.
        Color target = !Stored ? EmptyTint : IsOpen() ? OutTint : Color.white;
        iconImage.color = Color.Lerp(iconImage.color, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        float punch = Time.unscaledTime - punchStarted;
        float scale = punch < 0.45f ? 1f + 0.22f * Mathf.Sin(punch / 0.45f * Mathf.PI) * (1f - punch / 0.45f) : 1f;
        icon.localScale = Vector3.one * scale;
    }

    // =========================================================
    // MOVING THE OPEN BOOK
    // =========================================================

    private FloatingBookGrab grab;
    private Doc3DLayer bookLayer;
    private bool bookLayerWasAlways;

    /// <summary>The open book's page is the grip (values and page edges keep their own taps); it always draws in front.</summary>
    private void AttachGrab(TaxCodeBook3DDrag desk)
    {
        // On the book itself: drags anywhere on its page that a value or page
        // edge does not use bubble up to it, like the floating Case Folder.
        if (book != null)
        {
            grab = book.GetComponent<FloatingBookGrab>();
            if (grab == null) grab = book.gameObject.AddComponent<FloatingBookGrab>();
            grab.Owner = this;
            grab.enabled = true;
        }
        bookLayer = null;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.Source == book) bookLayer = layer;
        if (bookLayer != null)
        {
            bookLayerWasAlways = bookLayer.AlwaysComposite;
            bookLayer.AlwaysComposite = true;
        }
    }

    private void DetachGrab()
    {
        if (grab != null)
        {
            grab.EndHold();
            Destroy(grab);
        }
        grab = null;
        if (bookLayer != null) bookLayer.AlwaysComposite = bookLayerWasAlways;
        bookLayer = null;
    }

    internal bool CanMoveBook => phase == Phase.Open && book != null;

    /// <summary>
    /// The grab (2026-10-26): where the finger is, and the depth of the printed page it holds. The
    /// book then moves exactly as far as the finger does at that depth, so the page stays under the
    /// finger (moving by the model's pivot depth made the leaning page run ahead of or behind it).
    /// </summary>
    internal Vector3 GrabOffset(Vector2 screen)
    {
        Camera main = MainCamera();
        if (main == null || book == null) return Vector3.zero;
        held = HeldOnScreen.ForModel(book, main, screen);
        grabScreen = screen;
        grabPin = pinPosition;
        RectTransform page = deskBook != null && deskBook.BookUI != null ? deskBook.BookUI.PaperRoot as RectTransform : null;
        Vector3 anchor = page != null ? page.TransformPoint(page.rect.center) : book.position;
        grabDepth = Mathf.Max(main.nearClipPlane + 0.01f, Vector3.Dot(anchor - main.transform.position, main.transform.forward));
        return Vector3.zero;
    }

    private HeldOnScreen held;
    private Vector2 grabScreen;
    private Vector3 grabPin;
    private float grabDepth;

    internal void MoveBook(Vector2 screen, Vector3 offset)
    {
        Camera main = MainCamera();
        if (main == null || !CanMoveBook) return;
        // Exactly under the finger (at the held page's depth), stopping only at the screen edge.
        pinPosition = grabPin + ScreenToLocal(main, held.Clamp(screen), grabDepth) - ScreenToLocal(main, grabScreen, grabDepth);
        if (OverStoreTarget(screen) && Time.unscaledTime - punchStarted > 0.45f) punchStarted = Time.unscaledTime;
    }

    private static Vector3 ScreenToLocal(Camera main, Vector2 screen, float depth)
    {
        Vector3 world = main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        return Quaternion.Inverse(main.transform.rotation) * (world - main.transform.position);
    }

    internal void ReleaseBook(Vector2 screen)
    {
        if (!CanMoveBook) return;
        // Back into the bar (any slot or the Bag in Level 1; its icon in Level 2), or back on its desk stack.
        if (OverStoreTarget(screen)) CloseFloating();
        else if (OverDeskStack(screen))
        {
            RestoreFloating(true);
            ReturnToDesk();
        }
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
    }

    internal Transform BookTransform => book;

    private TaxCodeBook3DDrag DeskBook()
    {
        if (deskBook != null) return deskBook;
        Transform closed = Find(gameObject.scene, ClosedBookPath);
        if (closed != null) deskBook = closed.GetComponent<TaxCodeBook3DDrag>();
        if (deskBook == null)
        {
            TaxCodeBook3DDrag[] all = FindObjectsByType<TaxCodeBook3DDrag>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length && deskBook == null; i++)
                if (all[i] != null && all[i].gameObject.scene == gameObject.scene) deskBook = all[i];
        }
        if (viewpoint == null) viewpoint = Find(gameObject.scene, ViewpointPath);
        return deskBook;
    }

    /// <summary>Seated at the office desk with the closed book on screen.</summary>
    private bool DeskBookInView()
    {
        TaxCodeBook3DDrag desk = DeskBook();
        CameraController cc = CameraController.Instance;
        Camera camera = MainCamera();
        if (desk == null || cc == null || camera == null || cc.CurrentMode != CameraController.CameraMode.Workstation ||
            desk.ClosedBook == null || !desk.ClosedBook.activeInHierarchy)
            return false;
        Vector3 point = camera.WorldToViewportPoint(desk.ClosedBook.transform.position);
        return point.z > 0f && point.z < 4f && point.x > -0.05f && point.x < 1.05f && point.y > -0.05f && point.y < 1.05f;
    }

    // =========================================================
    // FLOATING VIEW
    // =========================================================

    private bool OpenFloating(TaxCodeBook3DDrag desk, Vector2? screenPosition)
    {
        Camera main = MainCamera();
        if (main == null || desk.BookUI == null) return false;
        book = desk.OpenBook.transform;
        savedParent = book.parent;
        savedPosition = book.position;
        savedRotation = book.rotation;
        savedScale = book.localScale;
        savedActive = book.gameObject.activeSelf;
        openedInMode = CameraController.Instance != null ? CameraController.Instance.CurrentMode : CameraController.CameraMode.FirstPerson;

        // Desk-only handlers (drag on the desk, drop back on the desk) wait while it floats.
        var handlers = new System.Collections.Generic.List<Behaviour>();
        foreach (Behaviour handler in book.GetComponentsInChildren<Behaviour>(true))
            if (handler != null && handler.enabled && (handler is OpenTaxCodeBookReturnToDesk || handler is TaxCodeBookFloatingModel3D))
            {
                handler.enabled = false;
                handlers.Add(handler);
            }
        pausedHandlers = handlers.ToArray();

        // Same framing as from the desk chair (CaseFolder3DView), as a camera-local pose.
        Vector3 relPosition = new Vector3(0f, -0.15f, 0.55f);
        Quaternion relRotation = Quaternion.Euler(-55f, 0f, 0f);
        if (viewpoint != null)
        {
            Quaternion inv = Quaternion.Inverse(viewpoint.rotation);
            relPosition = inv * (savedPosition - viewpoint.position);
            relRotation = inv * savedRotation;
        }
        if (relPosition.z < main.nearClipPlane + 0.1f) relPosition.z = 0.55f;
        // Read upright from the printed page itself (any level's model axes): the page faces the
        // camera, leaning back a little so the slim hardcover's depth shows.
        RectTransform page = desk.BookUI.PaperRoot as RectTransform;
        if (page != null)
        {
            Quaternion pageInBook = Quaternion.Inverse(savedRotation) * page.rotation;
            relRotation = Quaternion.Euler(ReadingLean, 0f, 0f) * Quaternion.Inverse(pageInBook);
        }
        toRotation = relRotation;
        toScale = savedScale.x;

        // Centre the printed page on screen (or the drop point), keeping its depth.
        book.SetParent(null, true);
        book.gameObject.SetActive(true);
        ApplyLocal(main, relPosition, toRotation, toScale);
        Vector3 pageCentre = page != null ? page.TransformPoint(page.rect.center) : TaxCodeBook3DDrag.VisualCentre(book.gameObject);
        Vector2 screen = screenPosition ?? new Vector2(main.pixelRect.center.x, main.pixelRect.center.y);
        // A drop near an edge still lands the whole page on screen (as the floating Case Folder does).
        if (page != null) screen = KeepPageOnScreen(main, page, pageCentre, screen);
        float depth = Vector3.Dot(pageCentre - main.transform.position, main.transform.forward);
        Vector3 wanted = main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Max(main.nearClipPlane + 0.05f, depth)));
        toPosition = relPosition + Quaternion.Inverse(main.transform.rotation) * (wanted - pageCentre);

        fromScale = savedScale.x * (screenPosition.HasValue ? 0.4f : IconScale);
        fromRotation = toRotation * Quaternion.Euler(0f, 0f, 25f);
        fromPosition = screenPosition.HasValue ? toPosition
            : Quaternion.Inverse(main.transform.rotation) * (IconWorldPoint(main, toPosition.z, main.transform.position) - main.transform.position);
        pinPosition = fromPosition;
        pinRotation = fromRotation;
        pinScale = fromScale;
        ApplyLocal(main, pinPosition, pinRotation, pinScale);
        desk.BookUI.Show();
        AttachGrab(desk);

        phase = Phase.Opening;
        elapsed = 0f;
        BringBookToFront(book);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_book_open_01");
        return true;
    }

    /// <summary>How far the floating book leans back from facing the camera (degrees).</summary>
    private const float ReadingLean = 12f;

    /// <summary>The screen point to centre the page on so all of it shows (12 px margin).</summary>
    private static Vector2 KeepPageOnScreen(Camera main, RectTransform page, Vector3 pageCentre, Vector2 screen)
    {
        page.GetWorldCorners(IconCorners);
        Vector2 centre = main.WorldToScreenPoint(pageCentre);
        float halfW = 0f, halfH = 0f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 corner = main.WorldToScreenPoint(IconCorners[i]);
            halfW = Mathf.Max(halfW, Mathf.Abs(corner.x - centre.x));
            halfH = Mathf.Max(halfH, Mathf.Abs(corner.y - centre.y));
        }
        Rect view = main.pixelRect;
        screen.x = halfW * 2f + 24f < view.width ? Mathf.Clamp(screen.x, view.xMin + halfW + 12f, view.xMax - halfW - 12f) : view.center.x;
        screen.y = halfH * 2f + 24f < view.height ? Mathf.Clamp(screen.y, view.yMin + halfH + 12f, view.yMax - halfH - 12f) : view.center.y;
        return screen;
    }

    private void CloseFloating()
    {
        if (phase != Phase.Opening && phase != Phase.Open) return;
        if (grab != null) grab.EndHold();
        Camera main = MainCamera();
        fromPosition = pinPosition;
        fromRotation = pinRotation;
        fromScale = pinScale;
        toPosition = main != null
            ? Quaternion.Inverse(main.transform.rotation) * (IconWorldPoint(main, pinPosition.z, main.transform.position) - main.transform.position)
            : pinPosition;
        toRotation = fromRotation * Quaternion.Euler(0f, 0f, -20f);
        toScale = savedScale.x * IconScale;
        phase = Phase.Closing;
        elapsed = 0f;
        // TaxCodeBookUI.Hide plays the close cue when the pages are put away.
    }

    private void UpdateFloating()
    {
        if (phase == Phase.Closed) return;
        CameraController cc = CameraController.Instance;
        Camera main = MainCamera();
        if (book == null || main == null)
        {
            RestoreFloating(true);
            return;
        }
        // Sitting down at (or leaving) the desk while it floats: it flies back into its icon
        // (2026-10-26: the usual close motion instead of a jump).
        if (cc != null && cc.CurrentMode != openedInMode && !(IsWalking(cc.CurrentMode) && IsWalking(openedInMode)) &&
            (phase == Phase.Opening || phase == Phase.Open))
        {
            CloseFloating();
        }
        // The book's own swipe-down close hid its pages: fly back too.
        if (phase == Phase.Open && deskBook != null && deskBook.BookUI != null && !BookPagesShowing())
        {
            CloseFloating();
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (phase == Phase.Opening)
        {
            float t = Mathf.Clamp01(elapsed / OpenDuration);
            float travel = 1f - (1f - t) * (1f - t) * (1f - t);
            float pop = HudTheme.EaseOutBack(t);
            pinPosition = Vector3.Lerp(fromPosition, toPosition, travel);
            pinRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, pop);
            pinScale = Mathf.LerpUnclamped(fromScale, toScale, pop);
            if (t >= 1f)
            {
                pinPosition = toPosition;
                pinRotation = toRotation;
                pinScale = toScale;
                phase = Phase.Open;
            }
        }
        else if (phase == Phase.Closing)
        {
            float t = Mathf.Clamp01(elapsed / CloseDuration);
            float ease = t * t;
            pinPosition = Vector3.Lerp(fromPosition, toPosition, ease);
            pinRotation = Quaternion.Slerp(fromRotation, toRotation, ease);
            pinScale = Mathf.Lerp(fromScale, toScale, ease);
            if (t >= 1f)
            {
                RestoreFloating(true);
                return;
            }
        }
        // Held in front of the camera every frame.
        ApplyLocal(main, pinPosition, pinRotation, pinScale);
    }

    private bool BookPagesShowing()
    {
        TaxCodeBookUI ui = deskBook.BookUI;
        return ui != null && ui.IsShowing;
    }

    private void RestoreFloating(bool hidePages)
    {
        DetachGrab();
        if (book != null)
        {
            if (hidePages && deskBook != null && deskBook.BookUI != null && deskBook.BookUI.IsShowing) deskBook.BookUI.Hide();
            book.SetParent(savedParent, true);
            book.SetPositionAndRotation(savedPosition, savedRotation);
            book.localScale = savedScale;
            book.gameObject.SetActive(savedActive);
        }
        if (pausedHandlers != null)
            foreach (Behaviour handler in pausedHandlers)
                if (handler != null) handler.enabled = true;
        pausedHandlers = null;
        book = null;
        phase = Phase.Closed;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void ApplyLocal(Camera main, Vector3 localPosition, Quaternion localRotation, float scale)
    {
        book.SetPositionAndRotation(main.transform.position + main.transform.rotation * localPosition,
            main.transform.rotation * localRotation);
        book.localScale = savedScale * (scale / Mathf.Max(1e-5f, savedScale.x));
    }

    private Vector3 IconWorldPoint(Camera main, float depth, Vector3 fallback)
    {
        RectTransform target = flyTarget != null ? flyTarget : icon;
        if (target == null || depth <= main.nearClipPlane) return fallback;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
        return main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
    }

    private static void BringBookToFront(Transform openBook)
    {
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.Source == openBook) layer.BringToFront();
    }

    private static readonly Vector3[] IconCorners = new Vector3[4];

    private static Camera MainCamera() =>
        CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;

    private static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = path.Substring(0, slash);
        string rest = path.Substring(slash + 1);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = root.transform.Find(rest);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>Drag ghost: a closed red reference book (like the desk book) with a brass title plate.</summary>
    public static void BuildClosedBook(RectTransform root, Vector2 size)
    {
        float u = size.x / 230f;
        RectTransform pages = HudTheme.CreateRect("Pages", root);
        HudTheme.Stretch(pages);
        pages.offsetMin += new Vector2(10f, -8f) * u;
        pages.offsetMax += new Vector2(8f, -8f) * u;
        HudTheme.AddImage(pages, HudTheme.Rounded, new Color32(240, 232, 210, 255), 8f * u);

        RectTransform cover = HudTheme.CreateRect("Cover", root);
        HudTheme.Stretch(cover);
        Image coverImage = HudTheme.AddImage(cover, HudTheme.Rounded, new Color32(176, 40, 42, 235), 10f * u);
        var shadow = coverImage.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadow.effectDistance = new Vector2(8f, -10f) * u;

        RectTransform spine = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Spine", cover), 0f, 0f, 30f * u, size.y);
        HudTheme.AddImage(spine, HudTheme.Rounded, new Color32(120, 22, 26, 235), 8f * u);

        RectTransform plate = HudTheme.CreateRect("Title Plate", cover);
        plate.anchorMin = plate.anchorMax = new Vector2(0.55f, 0.62f);
        plate.sizeDelta = new Vector2(150f * u, 70f * u);
        HudTheme.AddImage(plate, HudTheme.Rounded, new Color32(236, 206, 140, 255), 6f * u);
        TextMeshProUGUI title = HudTheme.CreateLabel("Title", plate, "TAX CODE\nBOOK", 20f * u, new Color32(70, 52, 24, 255),
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(title.rectTransform, 4f * u);
    }
}

/// <summary>Drag grip on the floating Tax Code Book's page: moves the book, tints it while held, drops into the icon.</summary>
internal sealed class FloatingBookGrab : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Level2TaxCodeBookIcon Owner;
    private Vector3 offset;
    private bool holding;

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Owner == null || !Owner.CanMoveBook || ValueDragSource.IsAnyDragActive) return;
        holding = true;
        offset = Owner.GrabOffset(eventData.position);
        TaxCodeBook3DDrag.SetHeldTint(Owner.BookTransform != null ? Owner.BookTransform.gameObject : null, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (holding) Owner.MoveBook(eventData.position, offset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!holding) return;
        EndHold();
        Owner.ReleaseBook(eventData.position);
    }

    public void EndHold()
    {
        if (!holding) return;
        holding = false;
        if (Owner != null && Owner.BookTransform != null) TaxCodeBook3DDrag.SetHeldTint(Owner.BookTransform.gameObject, false);
    }

    private void OnDisable() => EndHold();
}
