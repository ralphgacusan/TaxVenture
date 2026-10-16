using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Finds a walkable route from the player to the objective for the ground
/// arrows, using physics queries only.
///
/// WHY NOT A NAVMESH:
/// The scene has no baked NavMesh, and its floors/walls are MeshColliders
/// whose meshes are not readable, which a runtime NavMesh build cannot use
/// in a player build. Physics queries do work on them, so this probes a
/// lazy 2D grid (cells are checked only when the search reaches them, then
/// cached) and runs A* over it.
///
/// A cell is walkable when a ray from above finds a floor (normal facing
/// up) with a free capsule of body space on it. Triggers, the player, NPCs
/// and anything named "door" are ignored. A search is capped; if the goal is
/// not reached, the route to the closest point is returned (partial).
/// </summary>
public sealed class GuideGridPath
{
    public const float Cell = 0.5f;
    private const float MaxStep = 0.5f;
    private const float BodyRadius = 0.26f;
    private const float KneeHeight = 0.42f;
    private const float HeadHeight = 1.55f;
    private const int MaxNewProbesPerSearch = 2600;
    private const int MaxExpansions = 9000;

    private struct CellInfo
    {
        public bool Walkable;
        public float Y;
    }

    private sealed class Node
    {
        public long Key;
        public int X, Z;
        public float G, F;
        public Node Parent;
        public bool Closed;
        public int HeapIndex;
    }

    private readonly Dictionary<long, CellInfo> cells = new Dictionary<long, CellInfo>();
    private readonly Dictionary<long, Node> nodes = new Dictionary<long, Node>();
    private readonly List<Node> heap = new List<Node>();
    private readonly RaycastHit[] hits = new RaycastHit[24];
    private readonly Collider[] overlaps = new Collider[24];
    private readonly int mask;
    private float referenceY;
    private int probesThisSearch;
    private Transform player;

    public int CachedCells => cells.Count;

    public GuideGridPath()
    {
        int ui = LayerMask.NameToLayer("UI");
        mask = Physics.DefaultRaycastLayers;
        if (ui >= 0) mask &= ~(1 << ui);
    }

    /// <summary>Forget probed cells (e.g. after doors or furniture moved).</summary>
    public void Clear() => cells.Clear();

    /// <summary>
    /// Fills `result` with world points from `from` toward `to` (smoothed).
    /// Returns true when the goal area was reached, false for a partial route.
    /// </summary>
    public bool FindPath(Vector3 from, Vector3 to, Transform playerRoot, List<Vector3> result)
    {
        result.Clear();
        player = playerRoot;
        referenceY = from.y;
        probesThisSearch = 0;
        nodes.Clear();
        heap.Clear();

        if (!NearestWalkable(from, 2, out int sx, out int sz) ||
            !NearestWalkable(to, 5, out int gx, out int gz))
            return false;

        Node start = GetNode(sx, sz);
        start.G = 0f;
        start.F = Heuristic(sx, sz, gx, gz);
        Push(start);
        Node best = start;
        float bestH = start.F;
        Node goal = null;
        int expansions = 0;

        while (heap.Count > 0 && expansions < MaxExpansions)
        {
            Node current = Pop();
            current.Closed = true;
            expansions++;
            if (current.X == gx && current.Z == gz)
            {
                goal = current;
                break;
            }
            float h = Heuristic(current.X, current.Z, gx, gz);
            if (h < bestH)
            {
                bestH = h;
                best = current;
            }
            if (probesThisSearch > MaxNewProbesPerSearch) continue;

            CellInfo here = cells[Key(current.X, current.Z)];
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = current.X + dx, nz = current.Z + dz;
                if (!Probe(nx, nz, out CellInfo next) || Mathf.Abs(next.Y - here.Y) > MaxStep) continue;
                // No corner cutting past walls.
                if (dx != 0 && dz != 0 && (!Probe(current.X + dx, current.Z, out _) || !Probe(current.X, current.Z + dz, out _)))
                    continue;

                Node n = GetNode(nx, nz);
                if (n.Closed) continue;
                float g = current.G + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                bool inHeap = n.HeapIndex >= 0;
                if (inHeap && g >= n.G) continue;
                n.G = g;
                n.F = g + Heuristic(nx, nz, gx, gz) * 1.05f;
                n.Parent = current;
                if (inHeap) SiftUp(n.HeapIndex);
                else Push(n);
            }
        }

        Node end = goal ?? best;
        var raw = new List<Vector3>();
        for (Node n = end; n != null; n = n.Parent)
            raw.Add(CellCenter(n.X, n.Z, cells[n.Key].Y));
        raw.Reverse();
        Smooth(raw, result);
        if (goal != null && result.Count > 0) result[result.Count - 1] = new Vector3(to.x, result[result.Count - 1].y, to.z);
        return goal != null;
    }

    // =========================================================
    // GRID PROBING
    // =========================================================

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

    private static int ToCell(float v) => Mathf.FloorToInt(v / Cell);

    private static Vector3 CellCenter(int x, int z, float y) => new Vector3((x + 0.5f) * Cell, y, (z + 0.5f) * Cell);

    private bool NearestWalkable(Vector3 p, int radius, out int cx, out int cz)
    {
        int bx = ToCell(p.x), bz = ToCell(p.z);
        float bestD = float.MaxValue;
        cx = bx;
        cz = bz;
        bool found = false;
        for (int r = 0; r <= radius && !found; r++)
        for (int dz = -r; dz <= r; dz++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
            if (!Probe(bx + dx, bz + dz, out _)) continue;
            float d = dx * dx + dz * dz;
            if (d < bestD)
            {
                bestD = d;
                cx = bx + dx;
                cz = bz + dz;
                found = true;
            }
        }
        return found;
    }

    private bool Probe(int x, int z, out CellInfo info)
    {
        long key = Key(x, z);
        if (cells.TryGetValue(key, out info)) return info.Walkable;
        probesThisSearch++;
        info = Evaluate(x, z);
        cells[key] = info;
        return info.Walkable;
    }

    private CellInfo Evaluate(int x, int z)
    {
        var info = new CellInfo();
        Vector3 top = CellCenter(x, z, referenceY + 7f);
        int n = Physics.RaycastNonAlloc(top, Vector3.down, hits, 13f, mask, QueryTriggerInteraction.Ignore);

        // The top surface at about the player's level: not the roof above, and
        // not hidden geometry under the visible ground (arrows there would be
        // invisible). A desk top found here has no step to the floor, so the
        // route never uses it.
        float bestY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = hits[i];
            float y = h.point.y;
            if (h.normal.y < 0.6f || Ignored(h.collider)) continue;
            if (y > referenceY + 1.2f || y < referenceY - 3f) continue;
            if (y > bestY) bestY = y;
        }
        if (bestY == float.MinValue || !HasRoom(new Vector3(top.x, bestY, top.z))) return info;

        info.Walkable = true;
        info.Y = bestY;
        return info;
    }

    private bool HasRoom(Vector3 feet)
    {
        int blockers = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (KneeHeight + BodyRadius),
            feet + Vector3.up * HeadHeight, BodyRadius, overlaps, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < blockers; i++)
            if (!Ignored(overlaps[i])) return false;
        return true;
    }

    private bool Ignored(Collider c)
    {
        if (c == null) return true;
        Transform t = c.transform;
        if (player != null && t.IsChildOf(player)) return true;
        Rigidbody body = c.attachedRigidbody;
        if (body != null && !body.isKinematic) return true;
        if (c.GetComponentInParent<INpcAvailabilitySource>() != null) return true;
        for (int i = 0; i < 3 && t != null; i++, t = t.parent)
            if (t.name.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    // =========================================================
    // SMOOTHING
    // =========================================================

    private void Smooth(List<Vector3> raw, List<Vector3> result)
    {
        if (raw.Count == 0) return;
        result.Add(raw[0]);
        int anchor = 0;
        while (anchor < raw.Count - 1)
        {
            int furthest = anchor + 1;
            for (int j = raw.Count - 1; j > anchor + 1; j--)
            {
                if (LineWalkable(raw[anchor], raw[j]))
                {
                    furthest = j;
                    break;
                }
            }
            result.Add(raw[furthest]);
            anchor = furthest;
        }
    }

    private bool LineWalkable(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        d.y = 0f;
        float length = d.magnitude;
        int steps = Mathf.CeilToInt(length / (Cell * 0.5f));
        float lastY = a.y;
        for (int i = 1; i <= steps; i++)
        {
            Vector3 p = a + d * (i / (float)steps);
            if (!cells.TryGetValue(Key(ToCell(p.x), ToCell(p.z)), out CellInfo c) || !c.Walkable) return false;
            if (Mathf.Abs(c.Y - lastY) > MaxStep) return false;
            lastY = c.Y;
        }
        return true;
    }

    // =========================================================
    // A* SUPPORT
    // =========================================================

    private Node GetNode(int x, int z)
    {
        long key = Key(x, z);
        if (!nodes.TryGetValue(key, out Node n))
        {
            n = new Node { Key = key, X = x, Z = z, G = float.MaxValue, HeapIndex = -1 };
            nodes[key] = n;
        }
        return n;
    }

    private static float Heuristic(int x, int z, int gx, int gz)
    {
        int dx = Mathf.Abs(x - gx), dz = Mathf.Abs(z - gz);
        return (dx + dz) + (1.4142f - 2f) * Mathf.Min(dx, dz);
    }

    private void Push(Node n)
    {
        n.HeapIndex = heap.Count;
        heap.Add(n);
        SiftUp(n.HeapIndex);
    }

    private Node Pop()
    {
        Node top = heap[0];
        Node last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        top.HeapIndex = -1;
        if (heap.Count > 0)
        {
            heap[0] = last;
            last.HeapIndex = 0;
            SiftDown(0);
        }
        return top;
    }

    private void SiftUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].F <= heap[i].F) break;
            Swap(i, parent);
            i = parent;
        }
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            int l = i * 2 + 1, r = l + 1, smallest = i;
            if (l < heap.Count && heap[l].F < heap[smallest].F) smallest = l;
            if (r < heap.Count && heap[r].F < heap[smallest].F) smallest = r;
            if (smallest == i) break;
            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        Node t = heap[a];
        heap[a] = heap[b];
        heap[b] = t;
        heap[a].HeapIndex = a;
        heap[b].HeapIndex = b;
    }
}
