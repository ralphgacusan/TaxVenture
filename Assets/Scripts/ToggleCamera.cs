using System.Collections;
using UnityEngine;

public class ToggleCamera : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCam;
    public Camera levelSelectorCam;

    [Header("Main Camera Endpoint")]
    public Transform mainCameraEndPoint;

    [Header("Level Selector Points")]
    public Transform levelSelectorStartPoint;
    public Transform levelSelectorEndPoint;

    [Header("Transition Settings")]
    public float transitionDuration = 2f;

    private Vector3 mainCameraStartPosition;
    private Quaternion mainCameraStartRotation;

    private bool isTransitioning = false;

    private void Start()
    {
        // Remember the original Main Camera position
        mainCameraStartPosition = mainCam.transform.position;
        mainCameraStartRotation = mainCam.transform.rotation;

        // Start with Main Camera
        mainCam.gameObject.SetActive(true);
        levelSelectorCam.gameObject.SetActive(false);
    }

    // ==========================================
    // FORWARD TRANSITION
    // MAIN MENU → LEVEL SELECTOR
    // ==========================================

    public void SwitchToLevelSelector()
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionToLevelSelector());
    }

    private IEnumerator TransitionToLevelSelector()
    {
        isTransitioning = true;

        float elapsed = 0f;

        Vector3 mainStart = mainCam.transform.position;
        Quaternion mainStartRot = mainCam.transform.rotation;

        Vector3 mainEnd = mainCameraEndPoint.position;
        Quaternion mainEndRot = mainCameraEndPoint.rotation;

        Vector3 levelStart = levelSelectorStartPoint.position;
        Quaternion levelStartRot = levelSelectorStartPoint.rotation;

        Vector3 levelEnd = levelSelectorEndPoint.position;
        Quaternion levelEndRot = levelSelectorEndPoint.rotation;

        // Make sure Level Selector starts at its START point
        levelSelectorCam.transform.position = levelStart;
        levelSelectorCam.transform.rotation = levelStartRot;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // Smooth the entire animation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Move Main Camera
            mainCam.transform.position = Vector3.Lerp(
                mainStart,
                mainEnd,
                smoothT
            );

            mainCam.transform.rotation = Quaternion.Slerp(
                mainStartRot,
                mainEndRot,
                smoothT
            );

            // Move Level Selector Camera at the same time
            levelSelectorCam.transform.position = Vector3.Lerp(
                levelStart,
                levelEnd,
                smoothT
            );

            levelSelectorCam.transform.rotation = Quaternion.Slerp(
                levelStartRot,
                levelEndRot,
                smoothT
            );

            // Switch cameras at 50%
            if (t >= 0.5f && mainCam.gameObject.activeSelf)
            {
                mainCam.gameObject.SetActive(false);
                levelSelectorCam.gameObject.SetActive(true);
            }

            yield return null;
        }

        // Make sure final positions are exact
        mainCam.transform.position = mainEnd;
        mainCam.transform.rotation = mainEndRot;

        levelSelectorCam.transform.position = levelEnd;
        levelSelectorCam.transform.rotation = levelEndRot;

        // Make sure Level Selector is active
        mainCam.gameObject.SetActive(false);
        levelSelectorCam.gameObject.SetActive(true);

        isTransitioning = false;
    }

    // ==========================================
    // REVERSE TRANSITION
    // LEVEL SELECTOR → MAIN MENU
    // ==========================================

    public void SwitchToMainMenu()
    {
        Debug.Log("BACK BUTTON PRESSED!");

        if (isTransitioning)
            return;

        StartCoroutine(TransitionToMainMenu());
    }

    private IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        float elapsed = 0f;

        // ------------------------------------------
        // LEVEL SELECTOR CAMERA
        // END → START
        // ------------------------------------------

        Vector3 levelStart =
            levelSelectorCam.transform.position;

        Quaternion levelStartRot =
            levelSelectorCam.transform.rotation;

        Vector3 levelEnd =
            levelSelectorStartPoint.position;

        Quaternion levelEndRot =
            levelSelectorStartPoint.rotation;


        // ------------------------------------------
        // MAIN CAMERA
        // END → START
        // ------------------------------------------

        Vector3 mainStart =
            mainCameraEndPoint.position;

        Quaternion mainStartRot =
            mainCameraEndPoint.rotation;

        Vector3 mainEnd =
            mainCameraStartPosition;

        Quaternion mainEndRot =
            mainCameraStartRotation;


        // Put Main Camera at its END position
        mainCam.transform.position = mainStart;
        mainCam.transform.rotation = mainStartRot;

        // Main camera is hidden
        mainCam.gameObject.SetActive(false);

        // Level selector is visible
        levelSelectorCam.gameObject.SetActive(true);


        // ------------------------------------------
        // START REVERSE ANIMATION
        // ------------------------------------------

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / transitionDuration
            );

            // Smooth the entire animation
            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);


            // LEVEL SELECTOR: END → START
            levelSelectorCam.transform.position =
                Vector3.Lerp(
                    levelStart,
                    levelEnd,
                    smoothT
                );

            levelSelectorCam.transform.rotation =
                Quaternion.Slerp(
                    levelStartRot,
                    levelEndRot,
                    smoothT
                );


            // MAIN CAMERA: END → START
            mainCam.transform.position =
                Vector3.Lerp(
                    mainStart,
                    mainEnd,
                    smoothT
                );

            mainCam.transform.rotation =
                Quaternion.Slerp(
                    mainStartRot,
                    mainEndRot,
                    smoothT
                );


            // --------------------------------------
            // SWITCH AT 50%
            // --------------------------------------

            if (t >= 0.5f &&
                levelSelectorCam.gameObject.activeSelf)
            {
                levelSelectorCam.gameObject.SetActive(false);
                mainCam.gameObject.SetActive(true);
            }

            yield return null;
        }


        // ------------------------------------------
        // FINAL POSITIONS
        // ------------------------------------------

        levelSelectorCam.transform.position = levelEnd;
        levelSelectorCam.transform.rotation = levelEndRot;

        mainCam.transform.position = mainEnd;
        mainCam.transform.rotation = mainEndRot;


        // Make sure Main Camera is active
        levelSelectorCam.gameObject.SetActive(false);
        mainCam.gameObject.SetActive(true);

        isTransitioning = false;
    }
}