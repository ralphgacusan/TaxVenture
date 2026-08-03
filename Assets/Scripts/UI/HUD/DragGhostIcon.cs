using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The small paper/folder image that follows the cursor while a
/// HudSubmittableIcon is being dragged. Purely visual — no interaction
/// logic, no raycast target (so it never blocks detection of what's
/// underneath it, e.g. the SubmissionTray).
/// </summary>
public class DragGhostIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    public void SetSprite(Sprite sprite)
    {
        if (iconImage != null) iconImage.sprite = sprite;
    }

    public void FollowPointer(Vector2 screenPosition, Canvas canvas)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, screenPosition, canvas.worldCamera, out Vector2 localPos);

        ((RectTransform)transform).anchoredPosition = localPos;
    }
}