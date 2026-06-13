
using UnityEngine;
using System.Collections.Generic;

public class LocationService : MonoBehaviour
{
    public static LocationService Instance;

    
    private List<LocationData> _nearbyBuffer = new List<LocationData>(20);

    void Awake() => Instance = this;

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
                
                _nearbyBuffer.Add(loc);
            }
        }

        return _nearbyBuffer;
    }
}
