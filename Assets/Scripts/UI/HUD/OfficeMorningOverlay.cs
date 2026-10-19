using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shared next-morning presentation for both office-hours policies.</summary>
public static class OfficeMorningOverlay
{
    public static CanvasGroup Create(Scene scene, int sortingOrder, out TextMeshProUGUI title, out TextMeshProUGUI time)
    {
        var host = new GameObject("Next Morning Overlay", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();
        host.AddComponent<HudKeepVisibleDuringRewards>();

        var root = (RectTransform)host.transform;
        CanvasGroup group = host.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        RectTransform bg = HudTheme.CreateRect("Night", root);
        HudTheme.Stretch(bg);
        Image image = HudTheme.AddImage(bg, null, new Color(0.03f, 0.05f, 0.1f, 1f));
        image.raycastTarget = true;

        title = HudTheme.CreateLabel("Title", root, "", 64f, HudTheme.BrassSoft, TextAlignmentOptions.Center,
            FontStyles.Bold | FontStyles.UpperCase);
        title.characterSpacing = 16f;
        title.rectTransform.sizeDelta = new Vector2(1200f, 90f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        time = HudTheme.CreateLabel("Time", root, "", 34f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        time.rectTransform.sizeDelta = new Vector2(900f, 60f);
        time.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        return group;
    }

    public static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null) yield break;
        if (seconds <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        group.alpha = to;
    }
}
