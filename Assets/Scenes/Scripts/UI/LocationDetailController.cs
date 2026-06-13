








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

    
    public void OpenDetailPanel(LocationData locData, string indoorDocId = "", string customTitle = "")
    {
        if (locData == null) return;

        
        int myVersion = ++_openVersion;
        _currentData = locData;

        if (txtName != null) txtName.text = string.IsNullOrEmpty(customTitle) ? locData.display_name : customTitle;
        if (txtCategory != null) txtCategory.text = string.IsNullOrEmpty(indoorDocId) ? locData.category : "Phòng / Khu vực";

        if (!string.IsNullOrEmpty(indoorDocId))
        {
            if (txtDescription != null) txtDescription.text = "Đang tải thông tin chi tiết...";
            FirebaseService.Instance.GetIndoorDescription(indoorDocId, (desc) => {
                
                if (myVersion != _openVersion) return;
                if (txtDescription != null) txtDescription.text = desc;
            });
        }
        else
        {
            if (txtDescription != null) txtDescription.text = string.IsNullOrEmpty(locData.description) ? "Chưa có thông tin mô tả." : locData.description;
        }

        
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

    
    public void ClosePanel()
    {
        _openVersion++; 
        if (detailPanel != null) detailPanel.SetActive(false);
        if (dimmedOverlay != null) dimmedOverlay.SetActive(false);
    }

    
    void OnIndoorMapClicked()
    {
        if (string.IsNullOrEmpty(_currentBuildingIdForMap)) return;
        FloorViewer.Instance?.OpenViewer(_currentBuildingIdForMap);
        ClosePanel();
    }

    
    void OnNavigationClicked()
    {
        if (_currentData == null) return;
        ClosePanel();

        
        string nearestNode = FindNearestNodeToLocation(_currentData);
        if (nearestNode == null) return;

        CampusUIManager.Instance?.StartNavigation();
        NavigationSession.Instance?.StartNavigation(nearestNode);
    }

    
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

        
        if (_imageCache.Count >= MAX_IMAGE_CACHE)
        {
            string oldest = _imageCacheOrder.Dequeue();
            _imageCache.Remove(oldest);
        }

        _imageCache[imageName] = loaded;
        _imageCacheOrder.Enqueue(imageName);

        imgCover.sprite = loaded;
    }

    
    string GetBuildingImageName(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";
        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXT")) return "NXT";
        if (nodeId.StartsWith("NXS")) return "NXS";

        
        if (nodeId.StartsWith("CT") || nodeId.StartsWith("Căn") || nodeId.StartsWith("FOOD") || nodeId.StartsWith("DRINK"))
        {
            return "Canteen";
        }
        char c = nodeId[0];
        
        if (c >= 'A' && c <= 'G') return c.ToString();

        return nodeId;
    }
}
