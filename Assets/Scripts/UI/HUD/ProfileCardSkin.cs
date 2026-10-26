using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Gives the physical papers a plaza client hands over — the copy an NPC
/// drops in the plaza and the corkboard side-quest cards (tray and pinned) —
/// the look of that client's ID card (user revision 2026-10-27, replacing the
/// flat taxpayer-profile card): the same 3D ID card as Marie's and Arthur's
/// (IdCardModel: bevelled, laminated, Type 1 front), with the client's
/// taxpayer profile printed on its back. A tap on a corkboard card turns it
/// over (CardFlip); the world drop is still collected with one tap.
///
/// HOW: the card is real geometry in front of the old paper's face, centred
/// on it and wider (WidthFactor) so the print reads on the corkboard. The old
/// paper mesh and its 3D texts are hidden (not deleted) and the paper's box
/// collider is resized to the card, so grabbing matches what is seen.
/// Pickup and drag still go through the physics collider.
///
/// FEEDBACK: on the corkboard a frame behind the card shows the result — a
/// muted green when the card is in the right area, a muted red when it is not.
/// Presentation only; Office_Level1 and Play Mode only.
/// </summary>
public sealed class ProfileCardSkin : MonoBehaviour
{
    private const string TargetScene = "Office_Level1";
    private const float FrontOffset = 0.002f;
    private const float WidthFactor = 1.6f;
    private const float CardAspect = 1.6f;

    private static readonly float[] CorkboardTilts = { -1.6f, 1.2f, -0.9f, 1.8f };
    private static readonly Color CorrectFrame = new Color32(88, 135, 92, 255);
    private static readonly Color WrongFrame = new Color32(170, 78, 72, 255);

    private Renderer resultFrame;
    private Material frameMaterial;
    private CardFlip flip;

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
            var go = new GameObject("Client ID Card");
            go.layer = face.gameObject.layer;
            go.transform.SetParent(face, false);
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
        if (correct.HasValue) skin.frameMaterial.color = correct.Value ? CorrectFrame : WrongFrame;
    }

    /// <summary>Turns the card's ID over (front: the ID; back: the profile). False when it cannot turn now.</summary>
    public static bool Flip(Component card)
    {
        if (card == null) return false;
        ProfileCardSkin skin = card.GetComponentInChildren<ProfileCardSkin>(true);
        return skin != null && skin.flip != null && skin.flip.Toggle();
    }

    /// <summary>The card's flip (null before it is skinned).</summary>
    public static CardFlip FlipOf(Component card)
    {
        ProfileCardSkin skin = card != null ? card.GetComponentInChildren<ProfileCardSkin>(true) : null;
        return skin != null ? skin.flip : null;
    }

    private void Build(Transform face, TMP_Text nameText, TMP_Text descriptionText)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

        Rect area = MeasureFace(face, out float frontZ);

        // A landscape ID card, wider than the old paper, same centre, at a uniform world scale.
        Vector3 lossy = face.lossyScale;
        float sx = Mathf.Max(1e-5f, Mathf.Abs(lossy.x));
        float sy = Mathf.Max(1e-5f, Mathf.Abs(lossy.y));
        float sz = Mathf.Max(1e-5f, Mathf.Abs(lossy.z));
        float worldWidth = area.width * sx * WidthFactor;
        float worldHeight = worldWidth / CardAspect;
        var size = new Vector2(worldWidth, worldHeight);

        // Card space: +Y out of its front (toward the reader, the face's -Z), +Z its top (the face's +Y).
        bool corkboard = face.GetComponentInParent<ProfileDragCard>(true) != null;
        TaxpayerProfile profile = Find(nameText.text);
        float tilt = corkboard && profile != null ? CorkboardTilts[Mathf.Abs(ProfileCardFace.NumberOf(profile.TaxpayerName)) % CorkboardTilts.Length] : 0f;
        transform.localPosition = new Vector3(area.center.x, area.center.y, frontZ - FrontOffset);
        transform.localRotation = Quaternion.Euler(0f, 0f, tilt) * Quaternion.Euler(-90f, 0f, 0f);
        transform.localScale = new Vector3(1f / sx, 1f / sz, 1f / sy);

        ClientRecord record = profile != null ? CaseRecordRules.FromProfile(profile)
            : new ClientRecord { id = "id-unknown", type = ClientRecord.Identity, title = "IDENTITY CARD", name = nameText.text,
                profileText = descriptionText != null ? descriptionText.text : null };
        flip = IdCardModel.Build(transform, record, size, gameObject.layer, out _, out _);
        IdCardModel.AddDropShadow(transform, record, size, gameObject.layer);

        // Result frame behind the card (hidden until the board checks it).
        var frame = new GameObject("Result Frame");
        frame.layer = gameObject.layer;
        frame.transform.SetParent(transform, false);
        frame.transform.localPosition = new Vector3(0f, -0.0012f, 0f);
        frame.AddComponent<MeshFilter>().sharedMesh = SupportingDocumentMesh.RoundedSlab(size.x * 1.07f, size.y * 1.11f, 0.001f, size.x * 0.07f);
        resultFrame = frame.AddComponent<MeshRenderer>();
        frameMaterial = new Material(SupportingDocumentMesh.Matte(CorrectFrame)) { name = "ID Result Frame" };
        resultFrame.sharedMaterial = frameMaterial;
        resultFrame.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        resultFrame.enabled = false;

        // Hide the old square paper and its texts (not deleted).
        nameText.enabled = false;
        if (descriptionText != null) descriptionText.enabled = false;
        foreach (MeshRenderer paper in face.GetComponents<MeshRenderer>()) paper.forceRenderingOff = true;

        // Grabbing matches the card: resize the paper's box collider to it.
        BoxCollider box = face.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size = new Vector3(worldWidth / sx, worldHeight / sy, box.size.z);
            box.center = new Vector3(area.center.x, area.center.y, box.center.z);
        }
    }

    private void OnDestroy()
    {
        if (frameMaterial != null) Destroy(frameMaterial);
    }

    private static TaxpayerProfile Find(string name)
    {
        foreach (TaxpayerProfile profile in TaxpayerProfileDatabase.All)
            if (profile != null && profile.TaxpayerName == name) return profile;
        return null;
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
