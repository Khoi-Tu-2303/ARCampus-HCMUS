// UI/PopupController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance;

    [Header("UI References (Thiết kế mới)")]
    public GameObject popupCanvas;

    [Header("Top Header")]
    public Image imgBuildingIcon;       // Icon tòa nhà (Vuông xanh)
    public TMP_Text titleText;          // Tên địa điểm chính (VD: Toà F)

    [Header("Info Box")]
    public TMP_Text descriptionText;    // Lấy từ Firebase
    public TMP_Text txtDistanceETA;     // Cách 1000m

    [Header("Action Buttons")]
    public Button btnClose;             // Nút X
    public Button btnStartNavigation;   // Nút Bắt đầu chỉ đường (Xanh đậm bự)
    public Button btnIndoorMap;         // Nút Map nhỏ bên cạnh

    // BỘ NHỚ ĐỆM (CACHE)
    private Dictionary<string, string> _descriptionCache = new Dictionary<string, string>(16);
    private string _currentBuildingIdForMap = "";
    private LocationData _currentLocation; // Nhớ data để lát bấm nút còn biết đường mà đi

    void Awake() => Instance = this;

    void Start()
    {
        if (popupCanvas != null) popupCanvas.SetActive(false);

        // Cắm dây sự kiện
        if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
        if (btnIndoorMap != null) btnIndoorMap.onClick.AddListener(OnIndoorMapClicked);
        if (btnStartNavigation != null) btnStartNavigation.onClick.AddListener(OnStartNavigationClicked);
    }

    public void ShowPopup(string buildingName)
    {
        if (FirebaseService.Instance == null || FirebaseService.Instance.AllLocations == null) return;

        LocationData loc = FirebaseService.Instance.AllLocations.Find(x => x.display_name == buildingName);
        if (loc == null) return;

        _currentLocation = loc; // Lưu lại để dùng cho nút Chỉ đường

        // Đổ Text Tiêu đề
        if (titleText != null) titleText.text = loc.display_name;

        // Tính khoảng cách
        if (txtDistanceETA != null && GPSService.Instance != null && GPSService.Instance.IsReady)
        {
            float distance = GeoMath.Haversine(GPSService.Instance.Latitude, GPSService.Instance.Longitude, loc.lat, loc.lng);
            txtDistanceETA.text = $"Cách {distance:F0}m";
        }

        // Logic ẩn/hiện nút Indoor Map
        _currentBuildingIdForMap = GetBaseBuildingName(loc.location_id);
        if (btnIndoorMap != null)
        {
            btnIndoorMap.gameObject.SetActive(!string.IsNullOrEmpty(_currentBuildingIdForMap));
        }

        // Tải Description từ Firebase
        if (_descriptionCache.TryGetValue(buildingName, out string cached))
        {
            if (descriptionText != null) descriptionText.text = cached;
            if (popupCanvas != null) popupCanvas.SetActive(true);
        }
        else
        {
            if (descriptionText != null) descriptionText.text = "Đang tải thông tin...";
            if (popupCanvas != null) popupCanvas.SetActive(true); // Cứ bật lên trước cho mượt

            FirebaseService.Instance.GetBuildingDescription(buildingName, (desc) => {
                _descriptionCache[buildingName] = desc;
                if (descriptionText != null) descriptionText.text = desc;
            });
        }
    }

    // ✅ NÚT BẮT ĐẦU CHỈ ĐƯỜNG
    void OnStartNavigationClicked()
    {
        if (_currentLocation == null) return;

        ClosePopup(); // Đóng bảng lại cho đỡ vướng

        // Tìm Node gần tòa nhà nhất và bắt đầu dẫn đường
        string nearestNode = FindNearestNodeToLocation(_currentLocation);
        if (CampusUIManager.Instance != null) CampusUIManager.Instance.StartNavigation();
        if (NavigationSession.Instance != null) NavigationSession.Instance.StartNavigation(nearestNode);
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

    // Hàm toán học tìm đường
    string FindNearestNodeToLocation(LocationData loc)
    {
        string nearest = null;
        float minDist = float.MaxValue;
        if (GraphService.Instance == null || GraphService.Instance.Nodes == null) return null;
        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            float d = GeoMath.Haversine((float)loc.lat, (float)loc.lng, (float)node.lat, (float)node.lng);
            if (d < minDist) { minDist = d; nearest = node.id; }
        }
        return nearest;
    }
}