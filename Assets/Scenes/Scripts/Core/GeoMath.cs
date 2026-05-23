// Core/GeoMath.cs
using UnityEngine;

/// <summary>
/// Tập trung toàn bộ công thức toán học liên quan đến GPS và AR positioning.
/// Tất cả hàm đều là static — không cần MonoBehaviour, không cần Instance.
/// </summary>
public static class GeoMath
{
    // =============================================
    // HAVERSINE DISTANCE
    // Tính khoảng cách thực tế giữa 2 tọa độ GPS (đơn vị: mét)
    // Thay thế cho 3 bản copy ở PathFinder, GraphLoader, LocationData
    // =============================================
    public static float Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const float R = 6371000f;
        float dLat = Mathf.Deg2Rad * (float)(lat2 - lat1);
        float dLng = Mathf.Deg2Rad * (float)(lng2 - lng1);
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(Mathf.Deg2Rad * (float)lat1) *
                  Mathf.Cos(Mathf.Deg2Rad * (float)lat2) *
                  Mathf.Sin(dLng / 2) * Mathf.Sin(dLng / 2);
        return R * 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
    }

    // =============================================
    // GPS → AR WORLD POSITION
    // Chuyển tọa độ GPS của một địa điểm thành Vector3 trong không gian AR.
    // Dùng vị trí Camera làm gốc tọa độ.
    // Thay thế cho ARLabelSpawner.GPSToARPosition()
    // =============================================
    public static Vector3 GpsToARWorldPosition(
        double targetLat, double targetLng,
        double userLat, double userLng,
        Vector3 cameraPosition,
        float heightOffset = 1.5f)
    {
        float offsetX = (float)((targetLng - userLng) * 111320 * System.Math.Cos(userLat * System.Math.PI / 180));
        float offsetZ = (float)((targetLat - userLat) * 110540);
        return cameraPosition + new Vector3(offsetX, heightOffset, offsetZ);
    }

    // =============================================
    // LATLNG → METER ANCHOR VECTOR
    // Tính vector mét chênh lệch từ điểm gốc (anchor) đến điểm đích.
    // Dùng cho AR path line và next node label.
    // Thay thế cho NavigationController.LatLngToMeterAnchorVector()
    // =============================================
    public static Vector3 LatLngToMeterOffset(
        double anchorLat, double anchorLng,
        double targetLat, double targetLng)
    {
        float zMeters = (float)(targetLat - anchorLat) * 111320f;
        float xMeters = (float)(targetLng - anchorLng) * 111320f * Mathf.Cos((float)anchorLat * Mathf.Deg2Rad);
        return new Vector3(xMeters, 0, zMeters);
    }

    // =============================================
    // CALCULATE BEARING
    // Tính góc hướng (0-360°) từ điểm A đến điểm B.
    // Thay thế cho NavigationController.CalculateBearing()
    // =============================================
    public static float CalculateBearing(float lat1, float lng1, float lat2, float lng2)
    {
        float dLng = Mathf.Deg2Rad * (lng2 - lng1);
        float rlat1 = Mathf.Deg2Rad * lat1;
        float rlat2 = Mathf.Deg2Rad * lat2;
        float y = Mathf.Sin(dLng) * Mathf.Cos(rlat2);
        float x = Mathf.Cos(rlat1) * Mathf.Sin(rlat2) -
                  Mathf.Sin(rlat1) * Mathf.Cos(rlat2) * Mathf.Cos(dLng);
        float b = Mathf.Rad2Deg * Mathf.Atan2(y, x);
        return (b + 360f) % 360f;
    }
}