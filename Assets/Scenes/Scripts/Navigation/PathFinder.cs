// Navigation/PathFinder.cs
// THAY GraphLoader.Instance → GraphService.Instance
// THAY HaversineDistance() → GeoMath.Haversine()
// XÓA hàm HaversineDistance() static (đã chuyển vào GeoMath)

using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    public static PathFinder Instance;

    // ✅ Cache neighbors — chỉ build một lần sau khi graph load
    private Dictionary<string, List<(string id, float dist)>> _neighbors;
    // ✅ Reuse buffers — tránh allocate mỗi lần FindPath
    private Dictionary<string, float> _gScore = new();
    private Dictionary<string, float> _fScore = new();
    private Dictionary<string, string> _cameFrom = new();

    void Awake() => Instance = this;

    // ✅ Gọi từ GraphService sau khi ParseGeoJSON xong
    public void BuildNeighborCache()
    {
        var nodes = GraphService.Instance.Nodes;
        var edges = GraphService.Instance.Edges;
        _neighbors = new Dictionary<string, List<(string, float)>>(nodes.Count);
        foreach (var id in nodes.Keys)
            _neighbors[id] = new List<(string, float)>(8); // capacity hint
        foreach (var edge in edges)
        {
            _neighbors[edge.from].Add((edge.to, edge.distance_m));
            _neighbors[edge.to].Add((edge.from, edge.distance_m));
        }
    }

    public List<GraphNode> FindPath(string startId, string goalId)
    {
        if (_neighbors == null) BuildNeighborCache();
        var nodes = GraphService.Instance.Nodes;
        if (!nodes.ContainsKey(startId) || !nodes.ContainsKey(goalId)) return null;

        // ✅ Reuse dictionaries — chỉ Clear() không alloc mới
        _gScore.Clear(); _fScore.Clear(); _cameFrom.Clear();

        // ✅ Dùng SortedList hoặc MinHeap thay List.Remove O(n)
        // Đây là priority queue đơn giản, đủ cho 200 nodes campus
        var openSet = new SortedList<float, string>();
        float h0 = GeoMath.Haversine(nodes[startId].lat, nodes[startId].lng,
                                      nodes[goalId].lat, nodes[goalId].lng);
        _gScore[startId] = 0;
        _fScore[startId] = h0;
        openSet[h0] = startId;

        while (openSet.Count > 0)
        {
            string current = openSet.Values[0];
            openSet.RemoveAt(0);
            if (current == goalId) return ReconstructPath(_cameFrom, current, nodes);

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
                    float fN = tentative + GeoMath.Haversine(
                        nodes[neighbor].lat, nodes[neighbor].lng,
                        nodes[goalId].lat, nodes[goalId].lng);
                    _fScore[neighbor] = fN;
                    if (!openSet.ContainsValue(neighbor))
                        openSet[fN + UnityEngine.Random.value * 0.001f] = neighbor;
                }
            }
        }
        return null;
    }

    List<GraphNode> ReconstructPath(Dictionary<string, string> cameFrom,
        string current, Dictionary<string, GraphNode> nodes)
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