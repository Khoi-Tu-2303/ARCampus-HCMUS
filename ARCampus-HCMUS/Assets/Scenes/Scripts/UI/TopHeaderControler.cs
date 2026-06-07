// UI/TopHeaderController.cs — PATCHED
// FIXES:
// [LOW] O(n) Haversine scan over ALL graph nodes every second.
//       Solution: Pre-filter named nodes (exclude W_ and CP_ prefixes) in Start()
//       and cache the filtered list. Scan only runs once against ~10-30 named nodes
//       instead of 200+ nodes.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TopHeaderController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txtCurrentLocation;

    private float _checkTimer = 0f;

    // Pre-filtered list of named nodes (no waypoints, no checkpoints)
    private List<GraphNode> _namedNodes;
    private bool _nodesReady = false;

    void Start()
    {
        StartCoroutine(WaitAndCacheNodes());
    }

    IEnumerator WaitAndCacheNodes()
    {
        // Wait until graph is loaded
        while (GraphService.Instance == null || !GraphService.Instance.IsLoaded)
            yield return new WaitForSeconds(0.5f);

        _namedNodes = new List<GraphNode>(32);
        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            if (!node.id.StartsWith("W") && !node.id.StartsWith("CP_"))
                _namedNodes.Add(node);
        }

        _nodesReady = true;
        Debug.Log($"[TopHeader] Cached {_namedNodes.Count} named nodes for proximity check.");
    }

    void Update()
    {
        if (!_nodesReady) return;
        if (GPSService.Instance == null || !GPSService.Instance.IsReady) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer < 1f) return;
        _checkTimer = 0f;

        float userLat = (float)GPSService.Instance.Latitude;
        float userLng = (float)GPSService.Instance.Longitude;
        float minDist = float.MaxValue;
        string nearestName = "Đang xác định...";

        // Scan only pre-filtered named nodes
        foreach (var node in _namedNodes)
        {
            float dist = GeoMath.Haversine(userLat, userLng, (float)node.lat, (float)node.lng);
            if (dist < minDist)
            {
                minDist = dist;
                nearestName = node.name;
            }
        }

        if (txtCurrentLocation != null)
        {
            txtCurrentLocation.text = minDist > 30f ? "Khuôn viên trường" : nearestName;
        }
    }
}