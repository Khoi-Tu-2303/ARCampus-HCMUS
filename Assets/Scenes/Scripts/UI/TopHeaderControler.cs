// UI/TopHeaderController.cs
using UnityEngine;
using TMPro;

public class TopHeaderController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txtCurrentLocation;

    private float checkTimer = 0f;

    void Update()
    {
        if (GPSService.Instance == null || !GPSService.Instance.IsReady || GraphService.Instance == null) return;

        // Quét 1 giây 1 lần cho nhẹ máy
        checkTimer += Time.deltaTime;
        if (checkTimer < 1f) return;
        checkTimer = 0f;

        float userLat = (float)GPSService.Instance.Latitude;
        float userLng = (float)GPSService.Instance.Longitude;

        float minDist = float.MaxValue;
        string nearestName = "Đang xác định...";

        // Tìm điểm gần nhất trong Graph
        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            // Bỏ qua mấy điểm phụ (Waypoints), chỉ lấy tên tòa nhà/cổng
            if (node.id.StartsWith("W") || node.id.StartsWith("CP_")) continue;

            float dist = GeoMath.Haversine(userLat, userLng, (float)node.lat, (float)node.lng);
            if (dist < minDist)
            {
                minDist = dist;
                nearestName = node.name;
            }
        }

        if (txtCurrentLocation != null)
        {
            // Nếu cách tòa nhà gần nhất quá 50m -> Báo đang ở ngoài sân
            if (minDist > 50f) txtCurrentLocation.text = "Khuôn viên trường";
            else txtCurrentLocation.text = nearestName;
        }
    }
}