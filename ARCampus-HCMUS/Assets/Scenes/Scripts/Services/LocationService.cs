// Services/LocationService.cs
using UnityEngine;
using System.Collections.Generic;

public class LocationService : MonoBehaviour
{
    public static LocationService Instance;

    // ✅ TÚI ĐỰNG DÙNG CHUNG (Zero Allocation Buffer)
    private List<LocationData> _nearbyBuffer = new List<LocationData>(20);

    void Awake() => Instance = this;

    /// <summary>
    /// Trả về danh sách địa điểm trong bán kính (mét) tính từ vị trí GPS hiện tại.
    /// Zero Allocation: Tái sử dụng buffer để tránh tạo rác Garbage Collection.
    /// </summary>
    public IReadOnlyList<LocationData> GetNearbyLocations(float radiusMetres = 200f)
    {
        _nearbyBuffer.Clear();

        if (GPSService.Instance == null || !GPSService.Instance.IsReady) return _nearbyBuffer;
        if (FirebaseService.Instance == null) return _nearbyBuffer;

        double userLat = GPSService.Instance.Latitude;
        double userLng = GPSService.Instance.Longitude;

        foreach (var loc in FirebaseService.Instance.AllLocations)
        {
            float dist = GeoMath.Haversine(userLat, userLng, loc.lat, loc.lng);
            if (dist <= radiusMetres)
            {
                // Tui tắt cái Debug.Log ở đây đi để Console của ông đỡ bị spam dơ màn hình lúc test
                _nearbyBuffer.Add(loc);
            }
        }

        return _nearbyBuffer;
    }
}