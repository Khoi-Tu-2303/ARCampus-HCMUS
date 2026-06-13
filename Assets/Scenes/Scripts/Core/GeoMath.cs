










using UnityEngine;
using System;

public static class GeoMath
{
    
    
    

    public static float Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) *
                   Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (float)(R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a)));
    }

    
    
    
    

    public static float HaversineDouble(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) *
                   Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (float)(R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a)));
    }
    public static void InvalidateCompassCache() {  }

    public static float GetCachedNorthAngle()
    {
#if UNITY_EDITOR
        return 0f;
#else
        
        if (GPSService.Instance != null) return GPSService.Instance.ARNorthOffset;
        return 0f;
#endif
    }

    public static Vector3 GpsToARWorldPosition(
        double targetLat, double targetLng,
        double userLat, double userLng,
        Transform cameraTransform,
        float heightOffset = 1.5f)
    {
        float offsetX = (float)((targetLng - userLng) * GeoConstants.MetersPerDegreeLng * Math.Cos(userLat * Math.PI / 180.0));
        float offsetZ = (float)((targetLat - userLat) * GeoConstants.MetersPerDegreeLat);
        Vector3 rawOffset = new Vector3(offsetX, 0f, offsetZ);

        
        float northOffset = GetCachedNorthAngle();
        Vector3 rotatedOffset = Quaternion.Euler(0f, northOffset, 0f) * rawOffset;

        return cameraTransform.position + rotatedOffset + new Vector3(0f, heightOffset, 0f);
    }

    
    
    
    

    public static Vector3 LatLngToMeterOffset(
        double anchorLat, double anchorLng,
        double targetLat, double targetLng)
    {
        float zMeters = (float)((targetLat - anchorLat) * GeoConstants.MetersPerDegreeLat);
        float xMeters = (float)((targetLng - anchorLng) * GeoConstants.MetersPerDegreeLng
                                * Math.Cos(anchorLat * Math.PI / 180.0));
        return new Vector3(xMeters, 0f, zMeters);
    }

    
    
    

    public static float CalculateBearing(double lat1, double lng1, double lat2, double lng2)
    {
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double rlat1 = lat1 * Math.PI / 180.0;
        double rlat2 = lat2 * Math.PI / 180.0;
        double y = Math.Sin(dLng) * Math.Cos(rlat2);
        double x = Math.Cos(rlat1) * Math.Sin(rlat2)
                 - Math.Sin(rlat1) * Math.Cos(rlat2) * Math.Cos(dLng);

        return (float)(((Math.Atan2(y, x) * 180.0 / Math.PI) + 360.0) % 360.0);
    }
}
