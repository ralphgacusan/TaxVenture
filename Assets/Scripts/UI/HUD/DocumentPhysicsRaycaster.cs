using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Allows only the held document to receive 3D pointer input while world controls are held.</summary>
public sealed class DocumentPhysicsRaycaster : PhysicsRaycaster
{
    public Transform Document;

    public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
    {
        if (Document == null || !Document.gameObject.activeInHierarchy) return;
        int first = results.Count;
        base.Raycast(eventData, results);
        for (int i = results.Count - 1; i >= first; i--)
            if (!results[i].gameObject.transform.IsChildOf(Document)) results.RemoveAt(i);
    }
}
