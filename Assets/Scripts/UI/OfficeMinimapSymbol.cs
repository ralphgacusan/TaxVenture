using UnityEngine;
using UnityEngine.UI;

/// <summary>Font-independent player triangle / NPC diamond for the map and legend.</summary>
public sealed class OfficeMinimapSymbol : MaskableGraphic
{
    public bool IsPlayer { get; set; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        mesh.AddVert(new Vector3(rect.center.x, rect.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector3(rect.xMin, IsPlayer ? rect.yMin : rect.center.y), color, Vector2.zero);
        mesh.AddVert(new Vector3(rect.xMax, IsPlayer ? rect.yMin : rect.center.y), color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
        if (!IsPlayer)
        {
            mesh.AddVert(new Vector3(rect.center.x, rect.yMin), color, Vector2.zero);
            mesh.AddTriangle(1, 3, 2);
        }
    }
}
