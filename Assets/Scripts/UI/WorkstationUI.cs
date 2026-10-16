using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// Controls the workstation exit control — now a single bottom-center
/// placeholder button reading "Exit Desk" / "Exit Corkboard" (dynamic per
/// workstation), replacing the old top-right "Close" button entirely, per
/// R2 redesign. Still purely a "show/hide + forward click to
/// CameraController.ExitFirstPerson()" responsibility — no gameplay logic.
///
/// CONNECTS WITH:
/// - CameraController: calls Show(label) on EnterFirstPerson, Hide() on exit;
///   ExitFirstPerson() is called when the exit button is clicked
/// </summary>
public class WorkstationUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI exitButtonLabel;

    [Header("Button Style (matches the NPC dialogue Continue button)")]
    [Tooltip("Optional: copy the look of this button (e.g. the dialogue Continue button) at startup.")]
    [SerializeField] private Button styleSource;
    [SerializeField] private Color buttonColor = new Color(0.95f, 0.8f, 0.43f, 1f);
    [SerializeField] private Color labelColor = new Color(0.08f, 0.08f, 0.1f, 1f);

    private void Awake()
    {
        ApplyButtonStyle();
        Hide();
    }

    /// <summary>
    /// Gives the exit button the dialogue button's fill, sprite, colour
    /// transitions and label colour. The button's size and font size are
    /// left untouched.
    /// </summary>
    private void ApplyButtonStyle()
    {
        if (exitButtonLabel == null)
            return;

        Button exitButton = exitButtonLabel.GetComponentInParent<Button>(true);
        Image exitImage = exitButton != null ? exitButton.targetGraphic as Image : null;
        Color fill = buttonColor;
        Color text = labelColor;

        if (styleSource != null)
        {
            if (styleSource.targetGraphic is Image sourceImage)
            {
                fill = sourceImage.color;
                if (exitImage != null)
                {
                    exitImage.sprite = sourceImage.sprite;
                    exitImage.type = sourceImage.type;
                    exitImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
                }
            }

            TextMeshProUGUI sourceLabel = styleSource.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sourceLabel != null)
            {
                text = sourceLabel.color;
                exitButtonLabel.font = sourceLabel.font;
                exitButtonLabel.fontStyle = sourceLabel.fontStyle;
            }

            if (exitButton != null)
            {
                exitButton.transition = styleSource.transition;
                exitButton.colors = styleSource.colors;
            }
        }

        if (exitImage != null)
            exitImage.color = fill;
        exitButtonLabel.color = text;
    }

    /// <summary>
    /// Shows the bottom-center exit control with workstation-specific text,
    /// e.g. "Exit Desk" or "Exit Corkboard".
    /// </summary>
    public void Show(string exitLabel)
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (exitButtonLabel != null) exitButtonLabel.text = exitLabel;
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>Wired to the bottom-center Exit button's OnClick().</summary>
    public void OnExitButtonPressed()
    {
        CameraController.Instance.ExitFirstPerson();
    }
}