using UnityEngine;
using UnityEngine.UI;

/// <summary>A shallow desk-paper taper for overlay sheets. Field transforms use
/// the same width function, so the visible controls keep their real hit areas.</summary>
public sealed class FindingsPaperPerspective : BaseMeshEffect
{
    public static float WidthAt(float fromTop)
    {
        float t = fromTop < 0f ? 0f : fromTop > 1f ? 1f : fromTop;
        return 0.92f + 0.11f * t;
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive()) return;
        Rect rect = graphic.rectTransform.rect;
        if (rect.height <= 0f) return;
        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            float fromTop = (rect.yMax - vertex.position.y) / rect.height;
            vertex.position.x = rect.center.x + (vertex.position.x - rect.center.x) * WidthAt(fromTop);
            vertices.SetUIVertex(vertex, i);
        }
    }
}
