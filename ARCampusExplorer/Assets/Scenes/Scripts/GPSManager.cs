using UnityEngine;
using System.Collections;

public class GPSManager : MonoBehaviour
{
    public static GPSManager Instance;
    public double Latitude = 0;
    public double Longitude = 0;
    public bool IsReady = false;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        // Xin permission
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
            UnityEngine.Android.Permission.FineLocation))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
            UnityEngine.Android.Permission.FineLocation);
            yield return new WaitForSeconds(1f);
        }

        // Bật GPS
        Input.location.Start(3f, 1f);

        int timeout = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1f);
            timeout--;
            Debug.Log($"⏳ GPS initializing... {timeout}s left");
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            IsReady = true;
            Debug.Log("✅ GPS Ready!");
            InvokeRepeating(nameof(UpdateGPS), 0f, 3f);
        }
        else
        {
            Debug.LogError("❌ GPS failed: " + Input.location.status);
        }
    }

    void UpdateGPS()
    {
        Latitude = Input.location.lastData.latitude;
        Longitude = Input.location.lastData.longitude;
        Debug.Log($"📍 GPS: {Latitude:F6}, {Longitude:F6}");
    }

    // Lấy danh sách địa điểm gần nhất
    public System.Collections.Generic.List<LocationData> GetNearbyLocations(float radiusMetres = 200f)
    {
        var nearby = new System.Collections.Generic.List<LocationData>();
        if (!IsReady || FirebaseManager.Instance == null) return nearby;

        foreach (var loc in FirebaseManager.Instance.AllLocations)
        {
            float dist = loc.DistanceTo(Latitude, Longitude);
            if (dist <= radiusMetres)
            {
                Debug.Log($"🏫 Nearby: {loc.display_name} — {dist:F0}m");
                nearby.Add(loc);
            }
        }
        return nearby;
    }
}
