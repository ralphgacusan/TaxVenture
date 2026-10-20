using System;
using UnityEngine;

/// <summary>Follows the player's actual floor, including office-start practice modules.</summary>
public sealed class AudioEnvironment : MonoBehaviour
{
    private readonly RaycastHit[] groundHits = new RaycastHit[24];
    private float nextCheck;

    private void Update()
    {
        if (AudioManager.Instance == null || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.2f;

        int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.5f,
            Vector3.down, groundHits, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        bool? outdoor = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            // Floors are grouped under these same roots in both office levels.
            for (Transform part = hit.collider.transform; part != null; part = part.parent)
            {
                bool office = string.Equals(part.name, "OfficeBuilding", StringComparison.OrdinalIgnoreCase);
                bool outside = string.Equals(part.name, "Outside", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(part.name, "Env", StringComparison.OrdinalIgnoreCase);
                if (!office && !outside) continue;
                outdoor = outside;
                nearest = hit.distance;
                break;
            }
        }
        if (outdoor.HasValue) AudioManager.Instance.SetOutdoor(outdoor.Value);
    }
}
