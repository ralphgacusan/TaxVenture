using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComputerFieldSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image borderImage;
    [SerializeField] private Button slotButton;

    public float CurrentValue { get; private set; }
    public bool IsFilled { get; private set; }

    private ComputerUI owner;
    private string slotId;

    public void Initialize(ComputerUI owningUI, string id)
    {
        owner = owningUI;
        slotId = id;
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(() => owner.OnSlotClicked(slotId));
        Clear();
    }

    /// <summary>
    /// Attempts to assign the given source value. Returns true/false so
    /// ComputerUI can decide what happens next (e.g. whether to clear the
    /// player's selection or let them try a different slot).
    /// </summary>
    public bool TryAssign(ComputerSourceValue source)
    {
        bool isCorrect = source.CorrectSlotId == slotId;

        if (isCorrect)
        {
            CurrentValue = source.Value;
            IsFilled = true;
            valueText.text = $"\u20b1{source.Value:N0}";
            SetBorder(Color.green);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FlashRedThenReset());
        }

        return isCorrect;
    }

    public void Clear()
    {
        CurrentValue = 0f;
        IsFilled = false;
        valueText.text = "Click a source, then click here";
        SetBorder(Color.white);
    }

    private IEnumerator FlashRedThenReset()
    {
        SetBorder(Color.red);
        yield return new WaitForSeconds(0.6f);
        // Only reset to white if this slot wasn't correctly filled in the meantime.
        if (!IsFilled) SetBorder(Color.white);
    }

    private void SetBorder(Color color)
    {
        if (borderImage != null) borderImage.color = color;
    }
}