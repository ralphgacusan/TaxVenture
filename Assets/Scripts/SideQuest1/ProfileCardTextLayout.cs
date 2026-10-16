using TMPro;
using UnityEngine;

public static class ProfileCardTextLayout
{
    public static void Apply(TMP_Text nameText, TMP_Text descriptionText)
    {
        Configure(nameText, new Vector2(0f, 0.35f), new Vector2(0.92f, 0.16f), -0.01f, 1.05f, 0.7f, 1.15f);
        Configure(descriptionText, new Vector2(0f, -0.08f), new Vector2(0.9f, 0.64f), -0.011f, 0.95f, 0.6f, 1f);

        if (nameText != null)
        {
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
        }

        if (descriptionText != null)
        {
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Office HUD: same card look as the HUD profile cards (Office_Level1 only).
        ProfileCardSkin.Apply(nameText, descriptionText);
    }

    private static void Configure(TMP_Text text, Vector2 position, Vector2 size, float depth, float fontSize, float minFontSize, float maxFontSize)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localPosition = new Vector3(position.x, position.y, depth);

        text.color = Color.black;
        text.fontSize = fontSize;
        text.fontSizeMin = minFontSize;
        text.fontSizeMax = maxFontSize;
        text.enableAutoSizing = true;
        text.enableWordWrapping = true;
        text.lineSpacing = 0f;
        text.characterSpacing = 0f;
        text.wordSpacing = 0f;
        text.characterHorizontalScale = 1f;
        text.overflowMode = TextOverflowModes.Overflow;
    }
}
