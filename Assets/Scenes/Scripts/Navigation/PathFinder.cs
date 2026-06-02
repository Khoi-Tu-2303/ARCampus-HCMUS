// Navigation/PathFinder.cs — PATCHED
// FIXES:
// [MEDIUM] SortedList key collision: Random.value * 0.001f hack could still collide on large graphs.
//          Solution: Use (float f, int tie) tuple key — tiebreaker guarantees uniqueness.
// No other logic changes — A* algorithm and neighbor cache are correct.

using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    public static PathFinder Instance;

    // Neighbor cache — built once after graph loads
    private Dictionary<string, List<(string id, float dist)>> _neighbors;

    // Reuse score buffers to avoid per-call allocation
    private Dictionary<string, float> _gScore = new Dictionary<string, float>();
    private Dictionary<string, float> _fScore = new Dictionary<string, float>();
    private Dictionary<string, string> _cameFrom = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ──────────────────────────────────────────────────────────
    // NEIGHBOR CACHE — call from GraphService after ParseGeoJSON
    // ──────────────────────────────────────────────────────────

    public void BuildNeighborCache()
    {
        var nodes = GraphService.Instance.Nodes;
        var edges = GraphService.Instance.Edges;

        _neighbors = new Dictionary<string, List<(string, float)>>(nodes.Count);
        foreach (var id in nodes.Keys)
            _neighbors[id] = new List<(string, float)>(8);

        foreach (var edge in edges)
        {
            if (_neighbors.ContainsKey(edge.from))
                _neighbors[edge.from].Add((edge.to, edge.distance_m));
            if (_neighbors.ContainsKey(edge.to))
                _neighbors[edge.to].Add((edge.from, edge.distance_m));
        }
    }

    // ──────────────────────────────────────────────────────────
    // A* PATHFINDING
    // FIX: Replaced SortedList<float, string> (collision-prone) with
    //      SortedList<(float f, int tie), string> — guaranteed unique keys.
    // ──────────────────────────────────────────────────────────

    public List<GraphNode> FindPath(string startId, string goalId)
    {
        if (_neighbors == null) BuildNeighborCache();

        var nodes = GraphService.Instance.Nodes;
        if (!nodes.ContainsKey(startId) || !nodes.ContainsKey(goalId)) return null;

        _gScore.Clear();
        _fScore.Clear();
        _cameFrom.Clear();

        // Tiebreaker counter — guarantees unique keys even when f-scores collide
        int insertionCounter = 0;
        var openSet = new SortedList<(float f, int tie), string>(
            Comparer<(float f, int tie)>.Create((a, b) =>
            {
                int fc = a.f.CompareTo(b.f);
                return fc != 0 ? fc : a.tie.CompareTo(b.tie);
            }));

        float h0 = GeoMath.HaversineDouble(
            nodes[startId].lat, nodes[startId].lng,
            nodes[goalId].lat, nodes[goalId].lng);

        _gScore[startId] = 0f;
        _fScore[startId] = h0;
        openSet[(h0, insertionCounter++)] = startId;

        // Track which node IDs are currently in the open set (for fast contains check)
        var inOpen = new HashSet<string> { startId };

        while (openSet.Count > 0)
        {
            string current = openSet.Values[0];
            openSet.RemoveAt(0);
            inOpen.Remove(current);

            if (current == goalId)
                return ReconstructPath(_cameFrom, current, nodes);

            if (!_neighbors.TryGetValue(current, out var nbList)) continue;
            float gCurrent = _gScore.TryGetValue(current, out float g) ? g : float.MaxValue;

            foreach (var (neighbor, dist) in nbList)
            {
                float tentative = gCurrent + dist;
                float gN = _gScore.TryGetValue(neighbor, out float gv) ? gv : float.MaxValue;

                if (tentative < gN)
                {
                    _cameFrom[neighbor] = current;
                    _gScore[neighbor] = tentative;
                    float fN = tentative + GeoMath.HaversineDouble(
                        nodes[neighbor].lat, nodes[neighbor].lng,
                        nodes[goalId].lat, nodes[goalId].lng);
                    _fScore[neighbor] = fN;

                    if (!inOpen.Contains(neighbor))
                    {
                        openSet[(fN, insertionCounter++)] = neighbor;
                        inOpen.Add(neighbor);
                    }
                }
            }
        }

        return null; // no path found
    }

    // ──────────────────────────────────────────────────────────
    // RECONSTRUCT PATH
    // ──────────────────────────────────────────────────────────

    List<GraphNode> ReconstructPath(
        Dictionary<string, string> cameFrom,
        string current,
        Dictionary<string, GraphNode> nodes)
    {
        var path = new List<GraphNode>(16);
        path.Add(nodes[current]);

        while (cameFrom.TryGetValue(current, out string prev))
        {
            current = prev;
            path.Insert(0, nodes[current]);
        }

        return path;
    }
}