using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Level1CutsceneController : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Sprite[] frames;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Scene")]
    [SerializeField] private string levelSceneName = "Level1";

    private int currentFrame = 0;
    private bool isTransitioning = false;

    private void Start()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("[Level1Cutscene] No frames assigned.");
            return;
        }

        if (fadeOverlay == null)
        {
            Debug.LogError("[Level1Cutscene] Fade Overlay is not assigned.");
            return;
        }

        // Start completely black.
        fadeOverlay.alpha = 1f;

        frameImage.sprite = frames[0];

        // Fade into the first frame.
        StartCoroutine(FadeIn());
    }

    public void OnContinuePressed()
    {
        if (isTransitioning)
            return;

        currentFrame++;

        if (currentFrame >= frames.Length)
        {
            StartCoroutine(FinishCutscene());
            return;
        }

        StartCoroutine(ChangeFrame());
    }

    private IEnumerator ChangeFrame()
    {
        isTransitioning = true;

        // Fade to black.
        yield return StartCoroutine(Fade(0f, 1f));

        // Change frame while screen is black.
        frameImage.sprite = frames[currentFrame];

        // Fade back in.
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
    }

    private IEnumerator FinishCutscene()
    {
        isTransitioning = true;

        Debug.Log("[Level1Cutscene] Cutscene finished.");

        // Fade completely to black.
        yield return StartCoroutine(Fade(0f, 1f));

        // Load Level 1.
        SceneManager.LoadScene(levelSceneName);
    }

    private IEnumerator FadeIn()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        fadeOverlay.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;

            fadeOverlay.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        fadeOverlay.alpha = to;
    }
}