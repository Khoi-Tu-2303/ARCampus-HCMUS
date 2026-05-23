// Map/RouteRenderer2D.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RouteRenderer2D : MonoBehaviour
{
    [Header("Route Drawing")]
    public GameObject routeLinePrefab; // Prefab dấu chấm của ông
    public RectTransform routeContainer;
    public RectTransform destinationPin;

    [Header("Cài đặt Chấm (Dots)")]
    public float dotSpacing = 30f; // 🌟 CHỈNH SỐ NÀY TO LÊN THÌ CHẤM SẼ THƯA RA

    private List<RectTransform> activeRouteLines = new List<RectTransform>();
    private Queue<RectTransform> _linePool = new Queue<RectTransform>(32);

    RectTransform GetLine()
    {
        if (_linePool.Count > 0)
        {
            var l = _linePool.Dequeue();
            l.gameObject.SetActive(true);
            return l;
        }
        return Instantiate(routeLinePrefab, routeContainer).GetComponent<RectTransform>();
    }

    void ReturnLine(RectTransform l)
    {
        l.gameObject.SetActive(false);
        _linePool.Enqueue(l);
    }

    public void DrawRoute(List<GraphNode> path)
    {
        ClearRoute(); // Dọn kho trước
        if (path == null || path.Count == 0 || routeLinePrefab == null || routeContainer == null) return;

        var map = MapController.Instance;
        if (map == null) return;

        var points = new List<Vector2>();
        points.Add(map.GetLocalPositionFromGPS(GPSService.Instance.Latitude, GPSService.Instance.Longitude));
        foreach (var node in path) points.Add(map.GetLocalPositionFromGPS(node.lat, node.lng));

        float accumulatedDistance = 0f;
        Vector2 prefabSize = routeLinePrefab.GetComponent<RectTransform>().sizeDelta;

        // 🌟 THUẬT TOÁN "RẢI ĐINH": Rải từng chấm cách đều nhau
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 posA = points[i], posB = points[i + 1];
            Vector2 dir = posB - posA;
            float segmentLength = dir.magnitude;
            Vector2 dirNormalized = dir.normalized;

            while (accumulatedDistance < segmentLength)
            {
                // Tính tọa độ cho cái chấm hiện tại
                Vector2 dotPos = posA + dirNormalized * accumulatedDistance;

                // Móc 1 cái chấm từ Pool ra
                var rect = GetLine();
                rect.anchoredPosition = dotPos;

                // Khóa cứng Size và Rotation (Không kéo giãn nữa nên đảm bảo tròn vo 100%)
                rect.sizeDelta = prefabSize;
                rect.localRotation = Quaternion.identity;

                activeRouteLines.Add(rect);

                // Tiến lên 1 khoảng cách dotSpacing để rải chấm tiếp theo
                accumulatedDistance += dotSpacing;
            }

            // Mang phần khoảng cách bị dư vắt sang đoạn đường cua tiếp theo
            accumulatedDistance -= segmentLength;
        }

        // Đẩy Pin đích đến và cục BlueDot lên trên cùng
        if (destinationPin != null)
        {
            destinationPin.gameObject.SetActive(true);
            destinationPin.anchoredPosition = map.GetLocalPositionFromGPS(path[path.Count - 1].lat, path[path.Count - 1].lng);
            destinationPin.SetAsLastSibling();
        }

        if (map.blueDot != null) map.blueDot.SetAsLastSibling();
    }

    public void ClearRoute()
    {
        foreach (var line in activeRouteLines)
        {
            if (line != null) ReturnLine(line);
        }
        activeRouteLines.Clear();

        if (destinationPin != null) destinationPin.gameObject.SetActive(false);
    }
}