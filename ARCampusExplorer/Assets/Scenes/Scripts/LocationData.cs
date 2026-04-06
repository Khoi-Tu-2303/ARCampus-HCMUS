using System;
using System.Collections.Generic;

[Serializable]
public class LocationData
{
    public string location_id;
    public string display_name;
    public string category;
    public string building;
    public int floor;
    public double lat;
    public double lng;
    public string description;

    // Tính khoảng cách đến user (metres)
    public float DistanceTo(double userLat, double userLng)
    {
        const double R = 6371000;
        double dLat = (lat - userLat) * Math.PI / 180;
        double dLng = (lng - userLng) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(userLat * Math.PI / 180) * Math.Cos(lat * Math.PI / 180) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (float)(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }
}