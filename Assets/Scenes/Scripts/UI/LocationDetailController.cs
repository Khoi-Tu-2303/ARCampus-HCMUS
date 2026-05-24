// UI/LocationDetailController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class LocationDetailController : MonoBehaviour
{
    public static LocationDetailController Instance;

    [Header("UI References (Popup Card Style)")]
    public GameObject detailPanel;
    public GameObject dimmedOverlay;

    [Header("Image Display")]
    public Image imgCover;
    public Sprite defaultPlaceholder;

    [Header("Texts (Theo UI Mới)")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtCategory;    // Hiện chữ "Phòng chức năng" màu xanh
    public TextMeshProUGUI txtDescription; // Hiện đoạn văn mô tả

    [Header("Buttons")]
    public Button btnDimmedOverlay; // Bấm ra ngoài vùng xám để tắt
    public Button btnStartNavigation;
    public Button btnIndoorMap;

    private LocationData _currentData;
    private Dictionary<string, Sprite> _imageCache = new Dictionary<string, Sprite>();
    private Coroutine _imageLoadCoroutine;
    private string _currentBuildingIdForMap = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (dimmedOverlay != null) dimmedOverlay.SetActive(false);

        // Vùng xám đã được cài đặt để tắt Popup
        if (btnDimmedOverlay != null) btnDimmedOverlay.onClick.AddListener(ClosePanel);

        if (btnStartNavigation != null) btnStartNavigation.onClick.AddListener(OnNavigationClicked);
        if (btnIndoorMap != null) btnIndoorMap.onClick.AddListener(OnIndoorMapClicked);
    }

    // ✅ CẬP NHẬT: Thêm 2 tham số để chứa thông tin ảo từ thanh Search
    public void OpenDetailPanel(LocationData locData, string indoorDocId = "", string customTitle = "")
    {
        if (locData == null) return;
        _currentData = locData;

        // 1. TÊN HIỂN THỊ: Nếu Search truyền vào Tên Phòng thì xài, không thì xài Tên Tòa
        if (txtName != null)
            txtName.text = string.IsNullOrEmpty(customTitle) ? locData.display_name : customTitle;

        if (txtCategory != null)
            txtCategory.text = string.IsNullOrEmpty(indoorDocId) ? locData.category : "Phòng / Khu vực";

        // 2. MÔ TẢ: Phân luồng thần thánh ở đây!
        if (!string.IsNullOrEmpty(indoorDocId))
        {
            // BẬT CHẾ ĐỘ INDOOR: Chạy lệnh kéo Firebase mới
            if (txtDescription != null) txtDescription.text = "Đang tải thông tin chi tiết...";

            // Gọi hàm mới ông vừa viết trong FirebaseService
            FirebaseService.Instance.GetIndoorDescription(indoorDocId, (desc) => {
                if (txtDescription != null) txtDescription.text = desc;
            });
        }
        else
        {
            // CHẾ ĐỘ BÌNH THƯỜNG: Hiện mô tả của Tòa nhà
            if (txtDescription != null)
            {
                txtDescription.text = string.IsNullOrEmpty(locData.description)
                    ? "Chưa có thông tin mô tả cho địa điểm này."
                    : locData.description;
            }
        }

        // 3. ẢNH: Vẫn lấy ID gốc của Tòa nhà (locData) nên ảnh Tòa không bao giờ bị đổi!
        if (imgCover != null)
        {
            if (defaultPlaceholder != null) imgCover.sprite = defaultPlaceholder;
            string imageName = GetBuildingImageName(locData.location_id);
            if (_imageLoadCoroutine != null) StopCoroutine(_imageLoadCoroutine);
            _imageLoadCoroutine = StartCoroutine(LoadCoverImageAsync(imageName));
        }

        // Logic Indoor Map giữ nguyên
        _currentBuildingIdForMap = GetBuildingImageName(locData.location_id);
        if (btnIndoorMap != null)
        {
            bool hasMap = !string.IsNullOrEmpty(_currentBuildingIdForMap);
            btnIndoorMap.gameObject.SetActive(hasMap);
        }

        if (dimmedOverlay != null) dimmedOverlay.SetActive(true);
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    void OnIndoorMapClicked()
    {
        if (string.IsNullOrEmpty(_currentBuildingIdForMap)) return;
        FloorViewer.Instance?.OpenViewer(_currentBuildingIdForMap);
        ClosePanel();
    }

    string GetBuildingImageName(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";
        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXS") || nodeId.StartsWith("NXT")) return "NX";
        char c = nodeId[0];
        if (c >= 'A' && c <= 'G') return c.ToString();
        return nodeId;
    }

    IEnumerator LoadCoverImageAsync(string imageName)
    {
        if (string.IsNullOrEmpty(imageName)) yield break;
        if (_imageCache.ContainsKey(imageName)) { imgCover.sprite = _imageCache[imageName]; yield break; }
        ResourceRequest request = Resources.LoadAsync<Sprite>($"LocationImages/{imageName}");
        yield return request;
        if (request.asset != null)
        {
            Sprite loadedSprite = request.asset as Sprite;
            _imageCache[imageName] = loadedSprite;
            imgCover.sprite = loadedSprite;
        }
    }

    public void ClosePanel()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (dimmedOverlay != null) dimmedOverlay.SetActive(false);
    }

    void OnNavigationClicked()
    {
        if (_currentData == null) return;
        ClosePanel();
        string nearestNode = FindNearestNodeToLocation(_currentData);
        if (CampusUIManager.Instance != null) CampusUIManager.Instance.StartNavigation();
        if (NavigationSession.Instance != null) NavigationSession.Instance.StartNavigation(nearestNode);
    }

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