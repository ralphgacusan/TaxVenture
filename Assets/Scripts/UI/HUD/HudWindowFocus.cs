using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// PURPOSE:
/// "Whatever I touch comes to the front" across every document: the paper
/// windows (Notes, taxpayer profile cards, any FloatingWindow) and the 3D
/// documents (Case Folder and Tax Code Book on the desk, the floating Case
/// Folder), all in ONE front-to-back order (see Doc3DLayer).
///
/// On a press: whichever document is in front at that point — an open
/// window under the finger, or a 3D document actually hit there — is brought
/// to the front (with a small pop). Works even when the press lands on a
/// button, value chip or page inside it. The press itself is not consumed.
///
/// Every frame: a paper window lying under a 3D document that is now in
/// front of it lets touches through, so the document can be used where it
/// covers the paper.
/// </summary>
public sealed class HudWindowFocus : MonoBehaviour
{
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private PointerEventData pointer;
    private EventSystem pointerOwner;

    private void Update()
    {
        if (!TryGetPressStart(out Vector2 screenPosition)) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        if (pointer == null || pointerOwner != eventSystem)
        {
            pointer = new PointerEventData(eventSystem);
            pointerOwner = eventSystem;
        }
        pointer.Reset();
        pointer.position = screenPosition;

        hits.Clear();
        eventSystem.RaycastAll(pointer, hits);

        // Front-most open window under the finger.
        FloatingWindow bestWindow = null;
        int bestOrder = int.MinValue;
        for (int i = 0; i < FloatingWindow.Instances.Count; i++)
        {
            FloatingWindow w = FloatingWindow.Instances[i];
            if (w == null || !w.IsOpen() || w.SortingOrder <= bestOrder) continue;
            if (!w.ContainsScreenPoint(screenPosition)) continue;
            bestWindow = w;
            bestOrder = w.SortingOrder;
        }

        // A 3D document drawn at that point, if it is further in front. "Drawn there" is tested on
        // the document's own shape (2026-10-26): a raycast hit can belong to a document that is not
        // the one seen there, which kept the Case Folder in front of a touched Tax Code Book.
        Doc3DLayer bestDocument = null;
        for (int d = 0; d < Doc3DLayer.All.Count; d++)
        {
            Doc3DLayer layer = Doc3DLayer.All[d];
            if (layer == null || !layer.IsVisible || layer.SortingOrder <= bestOrder) continue;
            bool shown = layer.ShowsAt(screenPosition, out bool known);
            if (known ? !shown && !HitsDocument(layer.transform) : !HitsDocument(layer.Source) && !HitsDocument(layer.transform)) continue;
            bestDocument = layer;
            bestOrder = layer.SortingOrder;
        }

        if (bestDocument != null) bestDocument.BringToFront();
        else if (bestWindow != null) bestWindow.BringToFront();
    }

    private void LateUpdate()
    {
        // Papers under a 3D document that is in front of them pass touches
        // through to that document.
        for (int i = 0; i < FloatingWindow.Instances.Count; i++)
        {
            FloatingWindow w = FloatingWindow.Instances[i];
            if (w == null || !w.UsesPaperMotion) continue;

            if (w.UsesPointRaycastFiltering)
            {
                // Its own ICanvasRaycastFilter handles coverage at the pointer,
                // so an overlapping book cannot disable every exposed field.
                w.SetBlocksRaycasts(true);
                continue;
            }

            bool covered = false;
            if (w.IsOpen())
            {
                Rect paper = Doc3DLayer.ScreenRect(w.WindowRect);
                for (int d = 0; d < Doc3DLayer.All.Count && !covered; d++)
                {
                    Doc3DLayer layer = Doc3DLayer.All[d];
                    covered = layer != null && layer.IsComposited && layer.SortingOrder > w.SortingOrder &&
                              layer.TryGetFootprint(out Rect footprint) && footprint.Overlaps(paper);
                }
            }
            w.SetBlocksRaycasts(!covered);
        }
    }

    /// <summary>A hit inside the document, or on its layer's own UI (e.g. a held tool's pointer surface).</summary>
    private bool HitsDocument(Transform document)
    {
        if (document == null) return false;
        for (int i = 0; i < hits.Count; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go != null && go.transform.IsChildOf(document)) return true;
        }
        return false;
    }

    private static bool TryGetPressStart(out Vector2 position)
    {
        position = default;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.wasPressedThisFrame)
                {
                    position = touches[i].position.ReadValue();
                    return true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        return false;
    }
}
