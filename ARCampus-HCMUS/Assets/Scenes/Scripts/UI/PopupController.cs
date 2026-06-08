// UI/PopupController.cs — PATCHED
// FIXES:
// [MEDIUM] FindNearestNodeToLocation() used float-cast GeoMath.Haversine() — inconsistent
//          precision vs NavigationSession and LocationDetailController. Switched to HaversineDouble.
// [LOW]    OnStartNavigationClicked() passed nearestNode to StartNavigation() without null-guard.
//          Added null check to prevent NullReferenceException when graph is not loaded.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance;

    [Header("UI References")]
    public GameObject popupCanvas;

    [Header("Top Header")]
    public UnityEngine.UI.Image imgBuildingIcon;
    public TMP_Text titleText;

    [Header("Info Box")]
    public TMP_Text descriptionText;
    public TMP_Text txtDistanceETA;

    [Header("Action Buttons")]
    public Button btnClose;
    public Button btnStartNavigation;
    public Button btnIndoorMap;

    private Dictionary<string, string> _descriptionCache = new Dictionary<string, string>(16);
    private string _currentBuildingIdForMap = "";
    private LocationData _currentLocation;

    void Awake() => Instance = this;

    void Start()
    {
        if (popupCanvas != null) popupCanvas.SetActive(false);

        if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
        if (btnIndoorMap != null) btnIndoorMap.onClick.AddListener(OnIndoorMapClicked);
        if (btnStartNavigation != null) btnStartNavigation.onClick.AddListener(OnStartNavigationClicked);
    }

    public void ShowPopup(string buildingName)
    {
        if (FirebaseService.Instance == null || FirebaseService.Instance.AllLocations == null) return;

        LocationData loc = FirebaseService.Instance.AllLocations.Find(x => x.display_name == buildingName);
        if (loc == null) return;

        _currentLocation = loc;

        if (titleText != null) titleText.text = loc.display_name;

        if (txtDistanceETA != null && GPSService.Instance != null && GPSService.Instance.IsReady)
        {
            float distance = GeoMath.HaversineDouble(
                GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                loc.lat, loc.lng);
            txtDistanceETA.text = $"Cách {distance:F0}m";
        }

        _currentBuildingIdForMap = GetBaseBuildingName(loc.location_id);
        if (btnIndoorMap != null)
            btnIndoorMap.gameObject.SetActive(!string.IsNullOrEmpty(_currentBuildingIdForMap));

        if (_descriptionCache.TryGetValue(buildingName, out string cached))
        {
            if (descriptionText != null) descriptionText.text = cached;
            if (popupCanvas != null) popupCanvas.SetActive(true);
        }
        else
        {
            if (descriptionText != null) descriptionText.text = "Đang tải thông tin...";
            if (popupCanvas != null) popupCanvas.SetActive(true);

            FirebaseService.Instance.GetBuildingDescription(buildingName, (desc) => {
                _descriptionCache[buildingName] = desc;
                if (descriptionText != null) descriptionText.text = desc;
            });
        }
    }

    void OnStartNavigationClicked()
    {
        if (_currentLocation == null) return;

        ClosePopup();

        // FIX: null-guard — graph may not be loaded yet
        string nearestNode = FindNearestNodeToLocation(_currentLocation);
        if (nearestNode == null) return;

        CampusUIManager.Instance?.StartNavigation();
        NavigationSession.Instance?.StartNavigation(nearestNode);
    }

    void OnIndoorMapClicked()
    {
        if (string.IsNullOrEmpty(_currentBuildingIdForMap)) return;
        FloorViewer.Instance?.OpenViewer(_currentBuildingIdForMap);
        ClosePopup();
    }

    public void ClosePopup()
    {
        if (popupCanvas != null) popupCanvas.SetActive(false);
    }

    string GetBaseBuildingName(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";
        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXS") || nodeId.StartsWith("NXT")) return "NX";
        char c = nodeId[0];
        if (c >= 'A' && c <= 'G') return c.ToString();
        return nodeId;
    }

    // FIX: Use HaversineDouble — consistent precision with NavigationSession
    string FindNearestNodeToLocation(LocationData loc)
    {
        if (GraphService.Instance == null || GraphService.Instance.Nodes == null) return null;

        string nearest = null;
        float minDist = float.MaxValue;

        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            float d = GeoMath.HaversineDouble(loc.lat, loc.lng, node.lat, node.lng);
            if (d < minDist) { minDist = d; nearest = node.id; }
        }
        return nearest;
    }
}