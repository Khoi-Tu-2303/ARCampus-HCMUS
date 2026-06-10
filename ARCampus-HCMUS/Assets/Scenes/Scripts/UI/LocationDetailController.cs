// UI/LocationDetailController.cs — PATCHED v2
// FIXES (Phase 6):
// [MEDIUM] FindNearestNodeToLocation() used float-cast Haversine — same 1-2m precision issue
//          as checkpoint detection. For node snapping from a LocationData point, the error is
//          usually harmless, BUT if two nodes are very close (<5m apart) the wrong one could be
//          selected. Fix: switch to HaversineDouble to stay consistent with NavigationSession.
// [LOW]    _imageCache never evicted — over a long session, loading many buildings fills memory.
//          Fix: cap cache at MAX_IMAGE_CACHE entries, evict by insertion order via a Queue.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class LocationDetailController : MonoBehaviour
{
    public static LocationDetailController Instance;

    [Header("UI References")]
    public GameObject detailPanel;
    public GameObject dimmedOverlay;

    [Header("Image Display")]
    public Image imgCover;
    public Sprite defaultPlaceholder;

    [Header("Texts")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtCategory;
    public TextMeshProUGUI txtDescription;

    [Header("Buttons")]
    public Button btnDimmedOverlay;
    public Button btnStartNavigation;
    public Button btnIndoorMap;

    private LocationData _currentData;

    // FIX: Bounded image cache — evict oldest when over limit
    private const int MAX_IMAGE_CACHE = 10;
    private Dictionary<string, Sprite> _imageCache = new Dictionary<string, Sprite>(MAX_IMAGE_CACHE);
    private Queue<string> _imageCacheOrder = new Queue<string>(MAX_IMAGE_CACHE);

    private Coroutine _imageLoadCoroutine;
    private string _currentBuildingIdForMap = "";
    private int _openVersion = 0;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (dimmedOverlay != null) dimmedOverlay.SetActive(false);

        if (btnDimmedOverlay != null) btnDimmedOverlay.onClick.AddListener(ClosePanel);
        if (btnStartNavigation != null) btnStartNavigation.onClick.AddListener(OnNavigationClicked);
        if (btnIndoorMap != null) btnIndoorMap.onClick.AddListener(OnIndoorMapClicked);
    }

    // ── OPEN ─────────────────────────────────────────────────────
    public void OpenDetailPanel(LocationData locData, string indoorDocId = "", string customTitle = "")
    {
        if (locData == null) return;

        // Tăng mã vé lên 1 mỗi lần mở bảng mới
        int myVersion = ++_openVersion;
        _currentData = locData;

        if (txtName != null) txtName.text = string.IsNullOrEmpty(customTitle) ? locData.display_name : customTitle;
        if (txtCategory != null) txtCategory.text = string.IsNullOrEmpty(indoorDocId) ? locData.category : "Phòng / Khu vực";

        if (!string.IsNullOrEmpty(indoorDocId))
        {
            if (txtDescription != null) txtDescription.text = "Đang tải thông tin chi tiết...";
            FirebaseService.Instance.GetIndoorDescription(indoorDocId, (desc) => {
                // CHỈ IN CHỮ RA NẾU MÃ VÉ CÒN KHỚP (Bảng chưa bị đóng hoặc chuyển qua bảng khác)
                if (myVersion != _openVersion) return;
                if (txtDescription != null) txtDescription.text = desc;
            });
        }
        else
        {
            if (txtDescription != null) txtDescription.text = string.IsNullOrEmpty(locData.description) ? "Chưa có thông tin mô tả." : locData.description;
        }

        // ... Đoạn load hình ảnh bên dưới giữ nguyên ...
        if (imgCover != null)
        {
            if (defaultPlaceholder != null) imgCover.sprite = defaultPlaceholder;
            string imageName = GetBuildingImageName(locData.location_id);
            if (_imageLoadCoroutine != null) StopCoroutine(_imageLoadCoroutine);
            _imageLoadCoroutine = StartCoroutine(LoadCoverImageAsync(imageName));
        }

        _currentBuildingIdForMap = GetBuildingImageName(locData.location_id);
        if (btnIndoorMap != null) btnIndoorMap.gameObject.SetActive(!string.IsNullOrEmpty(_currentBuildingIdForMap));
        if (dimmedOverlay != null) dimmedOverlay.SetActive(true);
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    // ── CLOSE ────────────────────────────────────────────────────
    public void ClosePanel()
    {
        _openVersion++; // Hủy vé cũ, vô hiệu hóa các dữ liệu Firebase đang tải dở dang
        if (detailPanel != null) detailPanel.SetActive(false);
        if (dimmedOverlay != null) dimmedOverlay.SetActive(false);
    }

    // ── INDOOR MAP ───────────────────────────────────────────────
    void OnIndoorMapClicked()
    {
        if (string.IsNullOrEmpty(_currentBuildingIdForMap)) return;
        FloorViewer.Instance?.OpenViewer(_currentBuildingIdForMap);
        ClosePanel();
    }

    // ── NAVIGATION ──────────────────────────────────────────────
    void OnNavigationClicked()
    {
        if (_currentData == null) return;
        ClosePanel();

        // FIX: Use HaversineDouble for node snap (consistent with NavigationSession precision)
        string nearestNode = FindNearestNodeToLocation(_currentData);
        if (nearestNode == null) return;

        CampusUIManager.Instance?.StartNavigation();
        NavigationSession.Instance?.StartNavigation(nearestNode);
    }

    // FIX: switched from float Haversine to HaversineDouble for sub-metre accuracy
    string FindNearestNodeToLocation(LocationData loc)
    {
        if (GraphService.Instance == null || GraphService.Instance.Nodes == null) return null;

        string nearest = null;
        float minDist = float.MaxValue;

        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            // HaversineDouble → consistent precision with NavigationSession
            float d = GeoMath.HaversineDouble(loc.lat, loc.lng, node.lat, node.lng);
            if (d < minDist) { minDist = d; nearest = node.id; }
        }
        return nearest;
    }

    // ── IMAGE CACHE (bounded) ────────────────────────────────────
    IEnumerator LoadCoverImageAsync(string imageName)
    {
        if (string.IsNullOrEmpty(imageName)) yield break;

        if (_imageCache.TryGetValue(imageName, out Sprite cached))
        {
            imgCover.sprite = cached;
            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<Sprite>($"LocationImage/{imageName}");
        yield return request;

        if (request.asset == null) yield break;

        Sprite loaded = request.asset as Sprite;

        // FIX: evict oldest entry if cache is full
        if (_imageCache.Count >= MAX_IMAGE_CACHE)
        {
            string oldest = _imageCacheOrder.Dequeue();
            _imageCache.Remove(oldest);
        }

        _imageCache[imageName] = loaded;
        _imageCacheOrder.Enqueue(imageName);

        imgCover.sprite = loaded;
    }

    // ── HELPERS ──────────────────────────────────────────────────
    string GetBuildingImageName(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";
        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXT")) return "NXT";
        if (nodeId.StartsWith("NXS")) return "NXS";

        // ✅ THÊM DÒNG NÀY: Bảo vệ Căn tin, Quán ăn, Nước uống không bị bắt nhầm
        if (nodeId.StartsWith("CT") || nodeId.StartsWith("Căn") || nodeId.StartsWith("FOOD") || nodeId.StartsWith("DRINK"))
        {
            return "Canteen";
        }
        char c = nodeId[0];
        // Tới đây thì chữ C của Căn tin không còn lọt xuống đây được nữa
        if (c >= 'A' && c <= 'G') return c.ToString();

        return nodeId;
    }
}