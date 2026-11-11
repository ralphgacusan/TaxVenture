using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent touch-control symbols; no font glyph or texture dependency.</summary>
public sealed class TraversalIcon : MaskableGraphic
{
    public enum Kind { Jump, Sprint }
    public Kind Symbol;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (Symbol == Kind.Sprint)
        {
            Line(mesh, new Vector2(-0.3f, 0.08f), new Vector2(0f, 0.32f));
            Line(mesh, new Vector2(0f, 0.32f), new Vector2(0.3f, 0.08f));
            Line(mesh, new Vector2(-0.3f, -0.25f), new Vector2(0f, -0.01f));
            Line(mesh, new Vector2(0f, -0.01f), new Vector2(0.3f, -0.25f));
            return;
        }
        // Airborne person with raised arms, bent legs, and an upward arrow.
        Vector2 head = new Vector2(-0.12f, 0.28f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8f, b = (i + 1) * Mathf.PI / 8f;
            Triangle(mesh, head, head + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.085f,
                head + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 0.085f);
        }
        Line(mesh, new Vector2(-0.12f, 0.12f), new Vector2(-0.12f, -0.12f));
        Line(mesh, new Vector2(-0.12f, 0.09f), new Vector2(-0.36f, 0.27f));
        Line(mesh, new Vector2(-0.12f, 0.09f), new Vector2(0.08f, 0.28f));
        Line(mesh, new Vector2(-0.12f, -0.12f), new Vector2(-0.33f, -0.22f));
        Line(mesh, new Vector2(-0.33f, -0.22f), new Vector2(-0.23f, -0.37f));
        Line(mesh, new Vector2(-0.12f, -0.12f), new Vector2(0.07f, -0.27f));
        Line(mesh, new Vector2(0.07f, -0.27f), new Vector2(0.2f, -0.2f));
        Line(mesh, new Vector2(0.34f, -0.05f), new Vector2(0.34f, 0.34f));
        Line(mesh, new Vector2(0.34f, 0.34f), new Vector2(0.22f, 0.2f));
        Line(mesh, new Vector2(0.34f, 0.34f), new Vector2(0.46f, 0.2f));
    }

    private Vector2 Point(Vector2 p) => rectTransform.rect.center + p * Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);

    private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(Point(a), color, Vector2.zero);
        mesh.AddVert(Point(b), color, Vector2.zero);
        mesh.AddVert(Point(c), color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }

    private void Line(VertexHelper mesh, Vector2 a, Vector2 b)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * 0.035f;
        Triangle(mesh, a - n, a + n, b + n);
        Triangle(mesh, a - n, b + n, b - n);
    }
}
