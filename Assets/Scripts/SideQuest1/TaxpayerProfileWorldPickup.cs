using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// The physical "1-page paper" taxpayer profile that appears beside an
/// NPC once their conversation ends. Clicking it collects it into the
/// TaxpayerProfileCollectionHUD, the same way FolderToHUDIcon collects
/// the case folder — just click instead of drag, since this is a single
/// small pickup rather than a draggable object.
///
/// Place one of these inactive in the scene beside each of the 4 NPCs;
/// TaxpayerNpcInteractable activates and initializes it after dialogue.
/// </summary>
public class TaxpayerProfileWorldPickup : MonoBehaviour, IPointerClickHandler
{
    [Header("Idle Visual (optional)")]
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2f;


    [Header("Card Text (match ProfileCard_TEMP layout)")]
    [SerializeField] private TMPro.TMP_Text nameText;
    [SerializeField] private TMPro.TMP_Text descriptionText;

    private TaxpayerProfile profileData;
    private bool collected;
    private Vector3 basePosition;

    public void Initialize(TaxpayerProfile data)
    {
        profileData = data;
        collected = false;
        basePosition = transform.position;

        if (nameText != null)
        {
            nameText.text = data.TaxpayerName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
        }
    }
    private void Update()
    {
        if (collected)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = basePosition + Vector3.up * offset;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (collected || profileData == null)
        {
            return;
        }

        collected = true;

        if (TaxpayerProfileCollectionHUD.Instance != null)
        {
            TaxpayerProfileCollectionHUD.Instance.CollectProfile(profileData);
        }
        else
        {
            Debug.LogError("[TaxpayerProfileWorldPickup] TaxpayerProfileCollectionHUD.Instance is NULL.");
        }

        gameObject.SetActive(false);
    }

    public TaxpayerProfile Profile => profileData;
}
