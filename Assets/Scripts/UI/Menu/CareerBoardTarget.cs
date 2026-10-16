using System;
using UnityEngine;

/// <summary>
/// Something on the 3D career board that reacts to touch (used bare, with Interactable false, as a tap blocker): a BoxCollider the
/// presenter raycasts against, hover (pointer devices only), press and tap.
/// </summary>
public class CareerBoardTarget : MonoBehaviour
{
    public event Action<CareerBoardTarget> Tapped;

    /// <summary>False = taps are swallowed (e.g. a disabled button, or the open sheet's paper).</summary>
    public virtual bool Interactable { get; set; } = true;

    protected bool Hovered { get; private set; }
    protected bool Pressed { get; private set; }

    public void SetHover(bool value)
    {
        if (Hovered == value) return;
        Hovered = value;
        OnHoverChanged();
    }

    public void SetPressed(bool value)
    {
        if (Pressed == value) return;
        Pressed = value;
        OnPressChanged();
    }

    public void Tap()
    {
        if (!Interactable) return;
        OnTapped();
        Tapped?.Invoke(this);
    }

    protected virtual void OnHoverChanged() { }
    protected virtual void OnPressChanged() { }
    protected virtual void OnTapped() { }

    public BoxCollider AddHitBox(Transform owner, Vector3 center, Vector3 size)
    {
        var box = owner.gameObject.GetComponent<BoxCollider>();
        if (box == null) box = owner.gameObject.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        return box;
    }
}
