
using System;
using UnityEngine;

[Serializable]
public class ClientPrefabEntry
{
    [Tooltip("Must match the clientId in the case JSON.")]
    public string clientId;

    [Tooltip("3D client prefab that will be spawned.")]
    public GameObject prefab;
}

/// <summary>
/// Manages the 3D client currently displayed in the Conference Room.
///
/// JSON example:
///
/// "clientId": "client_001",
/// "clientModel": "Client_001"
///
/// Inspector mapping:
///
/// client_001 → Client_01
/// client_002 → Client_02
/// client_003 → Client_03
///
/// The clientId is used for runtime lookup.
/// clientModel remains case metadata and does not need to match the
/// prefab's actual Unity filename.
/// </summary>
public class ConferenceRoomClient : MonoBehaviour
{
    public static ConferenceRoomClient Instance { get; private set; }


    [Header("Where the current client model is placed")]
    [SerializeField] private Transform modelRoot;


    [Header("Client Prefabs")]
    [SerializeField] private ClientPrefabEntry[] clients;


    private GameObject currentClient;


    private void Awake()
    {
        // =========================================================
        // SINGLETON
        // =========================================================

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[ConferenceRoomClient] Duplicate instance detected. " +
                "Destroying duplicate."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[ConferenceRoomClient] Awake.");
    }


    /// <summary>
    /// Shows the client associated with the supplied clientId.
    ///
    /// Example:
    /// ShowClient("client_001")
    /// → spawns the prefab assigned to client_001.
    /// </summary>
    public void ShowClient(string clientId)
    {
        Debug.Log(
            $"[ConferenceRoomClient] ShowClient() called with ID: {clientId}"
        );


        // =========================================================
        // VALIDATE CLIENT ID
        // =========================================================

        if (string.IsNullOrEmpty(clientId))
        {
            Debug.LogError(
                "[ConferenceRoomClient] Client ID is empty."
            );

            return;
        }


        // =========================================================
        // VALIDATE CLIENT ARRAY
        // =========================================================

        if (clients == null || clients.Length == 0)
        {
            Debug.LogError(
                "[ConferenceRoomClient] No client prefabs assigned."
            );

            return;
        }


        // =========================================================
        // FIND CLIENT
        // =========================================================

        ClientPrefabEntry selectedClient = null;

        foreach (ClientPrefabEntry client in clients)
        {
            if (client == null)
                continue;

            if (client.clientId == clientId)
            {
                selectedClient = client;
                break;
            }
        }


        if (selectedClient == null)
        {
            Debug.LogError(
                $"[ConferenceRoomClient] No prefab found for client ID: " +
                $"{clientId}"
            );

            return;
        }


        // =========================================================
        // VALIDATE PREFAB
        // =========================================================

        if (selectedClient.prefab == null)
        {
            Debug.LogError(
                $"[ConferenceRoomClient] Prefab is missing for client ID: " +
                $"{clientId}"
            );

            return;
        }


        // =========================================================
        // VALIDATE MODEL ROOT
        // =========================================================

        if (modelRoot == null)
        {
            Debug.LogError(
                "[ConferenceRoomClient] Model Root is not assigned."
            );

            return;
        }


        // =========================================================
        // REMOVE PREVIOUS CLIENT
        // =========================================================

        if (currentClient != null)
        {
            Debug.Log(
                "[ConferenceRoomClient] Removing previous client."
            );

            Destroy(currentClient);
            currentClient = null;
        }


        // =========================================================
        // SPAWN NEW CLIENT
        // =========================================================

        currentClient = Instantiate(
            selectedClient.prefab,
            modelRoot.position,
            modelRoot.rotation,
            modelRoot
        );


        Debug.Log(
            $"[ConferenceRoomClient] Successfully showing client: " +
            $"{clientId}"
        );

        Debug.Log(
            $"[ConferenceRoomClient] Spawned prefab: " +
            $"{selectedClient.prefab.name}"
        );
    }


    /// <summary>
    /// Removes the currently displayed client from the Conference Room.
    /// </summary>
    public void HideClient()
    {
        if (currentClient == null)
        {
            Debug.Log(
                "[ConferenceRoomClient] No client currently displayed."
            );

            return;
        }


        Debug.Log(
            $"[ConferenceRoomClient] Hiding client: " +
            $"{currentClient.name}"
        );


        Destroy(currentClient);
        currentClient = null;
    }


    /// <summary>
    /// Returns the currently spawned client GameObject.
    /// Useful later if you want to control its Animator,
    /// dialogue, movement, etc.
    /// </summary>
    public GameObject GetCurrentClient()
    {
        return currentClient;
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

