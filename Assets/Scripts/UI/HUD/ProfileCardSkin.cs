using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Gives the physical taxpayer profile papers in the world — the paper an
/// NPC drops in the plaza and the corkboard side-quest cards (tray and
/// pinned) — the same card as the HUD profile windows (ProfileCardFace):
/// the same landscape shape, with large readable text.
///
/// HOW: a world-space canvas sits just in front of the paper's face. The
/// card keeps the HUD card's 540:330 shape, centred on the paper and wider
/// than the old square paper (WidthFactor) so the text is big enough to read
/// on the corkboard. The old paper mesh and its 3D texts are hidden (not
/// deleted) and the paper's box collider is resized to the card, so grabbing
/// matches what is seen. The canvas takes no UI raycasts: pickup and drag
/// still go through the physics collider.
///
/// FEEDBACK: on the corkboard a thick frame shows the result — a muted green
/// when the card is in the right area, a muted red when it is not.
/// Presentation only; Office_Level1 and Play Mode only.
/// </summary>
public sealed class ProfileCardSkin : MonoBehaviour
{
    private const string TargetScene = "Office_Level1";
    private const float FrontOffset = 0.003f;
    private const float WidthFactor = 1.6f;
    private const float TextBoost = 1.6f;
    private const float PixelScale = 1.2f;

    private const float CorkboardTextBoost = 1.3f;
    private static readonly float[] CorkboardTilts = { -1.6f, 1.2f, -0.9f, 1.8f };
    private static readonly Color CorrectFrame = new Color32(88, 135, 92, 255);
    private static readonly Color WrongFrame = new Color32(170, 78, 72, 255);

    private Image resultFrame;

    /// <summary>Called from ProfileCardTextLayout.Apply after the texts are set.</summary>
    public static void Apply(TMP_Text nameText, TMP_Text descriptionText)
    {
        // Play Mode only: cards can also be initialised by editor validation,
        // and the scene must never be modified while editing.
        if (!Application.isPlaying) return;
        if (nameText == null || nameText.gameObject.scene.name != TargetScene) return;
        Transform face = nameText.transform.parent;
        if (face == null) return;

        ProfileCardSkin skin = face.GetComponentInChildren<ProfileCardSkin>(true);
        if (skin == null)
        {
            var go = new GameObject("Profile Card Skin", typeof(RectTransform));
            go.layer = face.gameObject.layer;
            go.transform.SetParent(face, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            skin = go.AddComponent<ProfileCardSkin>();
        }

        skin.Build(face, nameText, descriptionText);
    }

    /// <summary>
    /// Corkboard result: true = right area (green frame), false = wrong area
    /// (red frame), null = not placed (no frame).
    /// </summary>
    public static void SetResult(Component card, bool? correct)
    {
        if (card == null || !Application.isPlaying) return;
        ProfileCardSkin skin = card.GetComponentInChildren<ProfileCardSkin>(true);
        if (skin == null || skin.resultFrame == null) return;

        skin.resultFrame.enabled = correct.HasValue;
        if (correct.HasValue) skin.resultFrame.color = correct.Value ? CorrectFrame : WrongFrame;
    }

    private void Build(Transform face, TMP_Text nameText, TMP_Text descriptionText)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

        Rect area = MeasureFace(face, out float frontZ);

        // Landscape card like the HUD cards: wider than the paper, same centre.
        Vector3 lossy = face.lossyScale;
        float sx = Mathf.Max(1e-5f, Mathf.Abs(lossy.x));
        float sy = Mathf.Max(1e-5f, Mathf.Abs(lossy.y));
        float aspect = ProfileCardFace.DesignSize.x / ProfileCardFace.DesignSize.y;
        float worldWidth = area.width * sx * WidthFactor;
        float worldHeight = worldWidth / aspect;
        Vector2 local = new Vector2(worldWidth / sx, worldHeight / sy);
        Vector2 pixels = ProfileCardFace.DesignSize * PixelScale;

        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = pixels;
        rect.localScale = new Vector3(local.x / pixels.x, local.y / pixels.y, local.x / pixels.x);
        rect.localRotation = Quaternion.identity;
        rect.localPosition = new Vector3(area.center.x, area.center.y, frontZ - FrontOffset);

        // Corkboard cards (user request 2026-10-24) look like the HUD profile
        // cards: paperclip, full header and the Classification line, a soft
        // drop shadow and a slight paper tilt. Same size as before.
        bool corkboard = face.GetComponentInParent<ProfileDragCard>(true) != null;
        int number = ProfileCardFace.NumberOf(nameText.text);
        if (corkboard)
        {
            RectTransform shadow = HudTheme.CreateRect("Drop Shadow", rect);
            HudTheme.Stretch(shadow);
            shadow.anchoredPosition = new Vector2(pixels.x * 0.018f, -pixels.y * 0.035f);
            HudTheme.AddImage(shadow, HudTheme.Rounded, new Color(0f, 0f, 0f, 0.32f), 14f);
            rect.localRotation = Quaternion.Euler(0f, 0f, CorkboardTilts[Mathf.Abs(number) % CorkboardTilts.Length]);
        }

        // Result frame behind the card (hidden until placed on the corkboard).
        float border = pixels.x * 0.045f;
        RectTransform frame = HudTheme.CreateRect("Result Frame", rect);
        HudTheme.Stretch(frame, -border);
        resultFrame = HudTheme.AddImage(frame, HudTheme.Rounded, CorrectFrame, 14f);
        resultFrame.enabled = false;

        string name = nameText.text;
        string description = descriptionText != null ? descriptionText.text : string.Empty;
        ProfileCardFace.Build(rect, name, description, number, pixels, false, out _,
            corkboard ? CorkboardTextBoost : TextBoost, corkboard, !corkboard);

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

        // Hide the old square paper and its texts (not deleted).
        nameText.enabled = false;
        if (descriptionText != null) descriptionText.enabled = false;
        foreach (MeshRenderer paper in face.GetComponents<MeshRenderer>()) paper.forceRenderingOff = true;

        // Grabbing matches the card: resize the paper's box collider to it.
        BoxCollider box = face.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size = new Vector3(local.x, local.y, box.size.z);
            box.center = new Vector3(area.center.x, area.center.y, box.center.z);
        }
    }

    /// <summary>
    /// The paper's face in the face transform's own plane (x/y), from the
    /// paper mesh (text meshes ignored). frontZ is the side the text faces.
    /// </summary>
    private static Rect MeasureFace(Transform face, out float frontZ)
    {
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        frontZ = 0f;

        Transform searchRoot = face;
        for (int pass = 0; pass < 2 && !any; pass++)
        {
            foreach (MeshFilter filter in searchRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<TMP_Text>() != null) continue;
                if (filter.GetComponentInParent<ProfileCardSkin>() != null) continue;

                Bounds b = filter.sharedMesh.bounds;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3(
                        (c & 1) == 0 ? b.min.x : b.max.x,
                        (c & 2) == 0 ? b.min.y : b.max.y,
                        (c & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = face.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!any)
                    {
                        min = max = p;
                        frontZ = p.z;
                        any = true;
                    }
                    else
                    {
                        min = Vector2.Min(min, p);
                        max = Vector2.Max(max, p);
                        frontZ = Mathf.Min(frontZ, p.z);
                    }
                }
            }

            // The text's parent may be a child of the paper: look one level up.
            if (!any && face.parent != null) searchRoot = face.parent;
        }

        if (!any || max.x - min.x < 0.01f || max.y - min.y < 0.01f)
        {
            frontZ = -0.01f;
            return new Rect(-0.5f, -0.5f, 1f, 1f);
        }

        frontZ = Mathf.Min(frontZ, -0.005f);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
