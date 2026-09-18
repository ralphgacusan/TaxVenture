using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Returns the open 3D tax code book to the desk.
///
/// When released over the desk target:
/// - The open book is hidden.
/// - The closed book is shown.
/// - The closed book returns to the exact original placement.
/// </summary>
public class OpenTaxCodeBookReturnToDesk :
    MonoBehaviour,
    IPointerUpHandler
{
    [Header("Book References")]

    [Tooltip("The original closed 3D tax code book.")]
    [SerializeField]
    private GameObject closedTaxCodeBook;

    [Tooltip("The separate desk target used for dropping the tax code book.")]
    [SerializeField]
    private GameObject deskDropBook;

    [Header("Drop Detection")]

    [SerializeField]
    private Camera mainCamera;

    [Header("Original Tax Code Book Placement")]

    [SerializeField]
    private Vector3 originalPosition =
        new Vector3(-0.0970001f, 0.815f, 0.2969971f);

    [SerializeField]
    private Vector3 originalRotation =
        new Vector3(0f, -88.627f, 0f);

    [SerializeField]
    private Vector3 originalScale =
        new Vector3(0.4f, 0.05f, 0.3f);

    [Tooltip("Additional height above the original position.")]
    [SerializeField]
    private float dropHeightOffset = 0f;

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;


    private Collider deskDropCollider;


    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[OpenTaxCodeBookReturnToDesk] Main Camera was not found."
            );
        }

        if (closedTaxCodeBook == null)
        {
            Debug.LogError(
                "[OpenTaxCodeBookReturnToDesk] " +
                "Closed tax code book is missing."
            );
        }

        if (deskDropBook == null)
        {
            Debug.LogError(
                "[OpenTaxCodeBookReturnToDesk] " +
                "Desk drop book is missing."
            );
        }
        else
        {
            deskDropCollider =
                deskDropBook.GetComponent<Collider>();

            if (deskDropCollider == null)
            {
                Debug.LogError(
                    "[OpenTaxCodeBookReturnToDesk] " +
                    "Desk drop book needs a Collider."
                );
            }
        }

        DebugLog(
            "Configured original position: " +
            originalPosition
        );
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (mainCamera == null ||
            closedTaxCodeBook == null ||
            deskDropCollider == null)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                eventData.position
            );

        if (!deskDropCollider.Raycast(
                ray,
                out RaycastHit hit,
                1000f
            ))
        {
            DebugLog(
                "Tax code book was released outside the desk target."
            );

            return;
        }

        ReturnTaxCodeBookToDesk();
    }


    private void ReturnTaxCodeBookToDesk()
    {
        Vector3 targetPosition =
            originalPosition +
            Vector3.up * dropHeightOffset;

        Quaternion targetRotation =
            Quaternion.Euler(originalRotation);

        /*
         * Restore the exact configured position,
         * rotation, and scale.
         */
        closedTaxCodeBook.transform.localPosition =
            originalPosition +
            Vector3.up * dropHeightOffset;

        closedTaxCodeBook.transform.localRotation =
            targetRotation;

        closedTaxCodeBook.transform.localScale =
            originalScale;
        /*
         * Show the closed book and hide the open book.
         */
        closedTaxCodeBook.SetActive(true);
        gameObject.SetActive(false);

        DebugLog(
            "Tax code book returned to the desk."
        );

        DebugLog(
            "Position: " +
            closedTaxCodeBook.transform.position
        );

        DebugLog(
            "Rotation: " +
            closedTaxCodeBook.transform.eulerAngles
        );

        DebugLog(
            "Scale: " +
            closedTaxCodeBook.transform.localScale
        );
    }


    public void ResetTaxCodeBook()
    {
        if (closedTaxCodeBook == null)
        {
            return;
        }

        closedTaxCodeBook.transform.localPosition =
            originalPosition;

        closedTaxCodeBook.transform.localRotation =
            Quaternion.Euler(originalRotation);

        closedTaxCodeBook.transform.localScale =
            originalScale;

        closedTaxCodeBook.SetActive(true);
        gameObject.SetActive(false);

        DebugLog(
            "Tax code book reset to the configured original placement."
        );
    }


    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                "[OpenTaxCodeBookReturnToDesk] " +
                message
            );
        }
    }
}