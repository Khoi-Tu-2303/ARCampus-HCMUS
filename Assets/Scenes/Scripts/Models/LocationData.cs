
using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class LocationData
{
    public string location_id;
    public string display_name;
    public string category;

    [Header("Indoor Future-proofing")]
    public string building;
    public string floor;
    public string anchor_id;

    [Header("Outdoor GPS")]
    public double lat;
    public double lng;
    public double altitude;

    public string description;

    [Header("Google Maps Style Data")]
    
    public string working_hours = "7:00 - 21:00";
    public List<string> image_urls = new List<string>();

    
    
}
