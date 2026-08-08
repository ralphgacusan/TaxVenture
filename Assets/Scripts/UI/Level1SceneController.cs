using System.Collections;
using UnityEngine;

public class Level1SceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        fadeOverlay.alpha = 1f;

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;

            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        fadeOverlay.alpha = 0f;
    }
}