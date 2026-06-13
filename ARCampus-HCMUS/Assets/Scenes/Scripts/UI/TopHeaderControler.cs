






using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TopHeaderController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txtCurrentLocation;

    private float _checkTimer = 0f;

    
    private List<GraphNode> _namedNodes;
    private bool _nodesReady = false;

    void Start()
    {
        StartCoroutine(WaitAndCacheNodes());
    }

    IEnumerator WaitAndCacheNodes()
    {
        
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
