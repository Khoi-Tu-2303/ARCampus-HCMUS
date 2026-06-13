
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class FloorViewer : MonoBehaviour
{
    public static FloorViewer Instance;

    [Header("UI References (Hệ Canvas Xịn)")]
    public Canvas indoorViewerCanvas;
    public Image indoorMapDisplay;
    public Button btnCloseViewer;
    public ScrollRect scrollRect;
    public TextMeshProUGUI txtBuildingTitle;

    [Header("Floor Navigation")]
    public TextMeshProUGUI txtCurrentFloorLabel;
    public Transform floorButtonParent;
    public GameObject floorButtonPrefab;

    [Header("Màu sắc Nút Tầng")]
    public Color selectedBgColor = new Color(0.1f, 0.3f, 0.8f, 1f);
    public Color unselectedBgColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color selectedTextColor = Color.white;
    public Color unselectedTextColor = Color.black;

    [Header("Zoom Settings")]
    public float minZoom = 1f;
    public float maxZoom = 6f;
    public float zoomSpeed = 0.1f;

    private List<Sprite> loadedFloorSprites = new List<Sprite>();
    private List<string> loadedFloorNames = new List<string>();
    private List<Image> floorBtnBackgrounds = new List<Image>();
    private List<TextMeshProUGUI> floorBtnTexts = new List<TextMeshProUGUI>();

    private int currentFloorIndex = 0;
#pragma warning disable 0414 
    private float previousDistance = 0f;
#pragma warning restore 0414
    private RectTransform contentRect;
    private RectTransform viewportRect;
    private Coroutine _loadCoroutine;

    
    
    private Dictionary<string, List<(string fileSuffix, string displayName)>> buildingStructure = new Dictionary<string, List<(string, string)>>()
    {
        
        { "A", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2"), ("3", "Tầng 3") } },
        
        
        { "C", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2") } },
        
        
        { "D", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2") } },
        
        
        { "E", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2"), ("3", "Tầng 3") } },
        
        
        { "F", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2"), ("3", "Tầng 3") } },
        
        
        { "G", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2"), ("3", "Tầng 3"), ("4", "Tầng 4"), ("5", "Tầng 5") } },
        
        
        { "NĐH", new List<(string, string)> { ("ham", "Tầng Hầm"), ("1", "Tầng 1"), ("2", "Tầng 2"), ("3", "Tầng 3"), ("4", "Tầng 4"), ("5", "Tầng 5"), ("6", "Tầng 6"), ("7", "Tầng 7"), ("8", "Tầng 8"), ("9", "Tầng 9") } }
    };

    void Awake()
    {
        Instance = this;
        if (indoorMapDisplay != null) contentRect = indoorMapDisplay.GetComponent<RectTransform>();
        if (scrollRect != null && scrollRect.viewport != null) viewportRect = scrollRect.viewport;
    }

    void OnEnable() { if (btnCloseViewer != null) btnCloseViewer.onClick.AddListener(HideIndoorViewer); }
    void OnDisable() { if (btnCloseViewer != null) btnCloseViewer.onClick.RemoveListener(HideIndoorViewer); }
    void Start() { if (indoorViewerCanvas != null) indoorViewerCanvas.enabled = false; if (scrollRect != null) scrollRect.enabled = false; }
    void Update() { if (indoorViewerCanvas == null || !indoorViewerCanvas.enabled || contentRect == null) return; HandleZoom(); }

    public void ZoomIn() => ApplyZoom(0.5f);
    public void ZoomOut() => ApplyZoom(-0.5f);

    private void ApplyZoom(float delta)
    {
        if (contentRect == null) return;
        Vector3 s = contentRect.localScale + Vector3.one * delta;
        s.x = Mathf.Clamp(s.x, minZoom, maxZoom);
        s.y = Mathf.Clamp(s.y, minZoom, maxZoom);
        s.z = 1f;
        contentRect.localScale = s;
    }

    void HandleZoom()
    {
        bool isZooming = false;
        float finalZoomDelta = 0f;
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftShift))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f) { isZooming = true; finalZoomDelta = scroll * 5f; }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) SwitchToAdjacentFloor(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  SwitchToAdjacentFloor(-1);
#else
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0), t1 = Input.GetTouch(1);
            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began) previousDistance = Vector2.Distance(t0.position, t1.position);
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                isZooming = true;
                float curr = Vector2.Distance(t0.position, t1.position);
                finalZoomDelta = (curr - previousDistance) * zoomSpeed * 0.05f;
                previousDistance = curr;
            }
        }
#endif
        if (isZooming) ApplyZoom(finalZoomDelta);
    }

    public void OpenViewer(string buildingId)
    {
        if (indoorViewerCanvas == null || indoorMapDisplay == null) return;
        if (_loadCoroutine != null) StopCoroutine(_loadCoroutine);
        StartCoroutine(LoadFloorSpritesAsync(buildingId));
    }

    private IEnumerator LoadFloorSpritesAsync(string buildingId)
    {
        if (txtBuildingTitle != null)
        {
            if (buildingId == "NĐH")
                txtBuildingTitle.text = "Nhà điều hành";
            else if (buildingId.Length == 1)
                txtBuildingTitle.text = "Tòa " + buildingId; 
            else
                txtBuildingTitle.text = buildingId;
        }
        loadedFloorSprites.Clear();
        loadedFloorNames.Clear();
        floorBtnBackgrounds.Clear();
        floorBtnTexts.Clear();

        if (floorButtonParent != null)
        {
            foreach (Transform child in floorButtonParent) Destroy(child.gameObject);
        }

        
        if (buildingStructure.ContainsKey(buildingId))
        {
            var floorsInfo = buildingStructure[buildingId];
            foreach (var floor in floorsInfo)
            {
                
                var request = Resources.LoadAsync<Sprite>($"IndoorMaps/{buildingId}_{floor.fileSuffix}");
                yield return request;
                if (request.asset != null)
                {
                    loadedFloorSprites.Add((Sprite)request.asset);
                    loadedFloorNames.Add(floor.displayName);
                }
                else
                {
                    Debug.LogWarning($"[FloorViewer] Mất file ảnh: IndoorMaps/{buildingId}_{floor.fileSuffix}");
                }
            }
        }
        else
        {
            
            var fallbackRequest = Resources.LoadAsync<Sprite>($"IndoorMaps/{buildingId}");
            yield return fallbackRequest;
            if (fallbackRequest.asset != null)
            {
                loadedFloorSprites.Add((Sprite)fallbackRequest.asset);
                loadedFloorNames.Add("Sơ đồ chung");
            }
        }

        if (loadedFloorSprites.Count == 0) yield break;

        indoorViewerCanvas.enabled = true;
        if (scrollRect != null) scrollRect.enabled = true;
        if (viewportRect == null && scrollRect != null) viewportRect = scrollRect.viewport;

        
        if (loadedFloorSprites.Count > 1 && floorButtonPrefab != null && floorButtonParent != null)
        {
            floorButtonParent.parent.gameObject.SetActive(true);

            for (int j = 0; j < loadedFloorSprites.Count; j++)
            {
                var btnObj = Instantiate(floorButtonPrefab, floorButtonParent);
                Image bg = btnObj.GetComponent<Image>();
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = loadedFloorNames[j];
                floorBtnBackgrounds.Add(bg);
                floorBtnTexts.Add(txt);

                int idx = j;
                btnObj.GetComponent<Button>()?.onClick.AddListener(() => SwitchToTargetFloorIndex(idx));
            }
        }
        else if (floorButtonParent != null)
        {
            floorButtonParent.parent.gameObject.SetActive(false);
        }

        SwitchToTargetFloorIndex(0);
    }

    public void SwitchToAdjacentFloor(int step) => SwitchToTargetFloorIndex(Mathf.Clamp(currentFloorIndex + step, 0, loadedFloorSprites.Count - 1));

    void SwitchToTargetFloorIndex(int index)
    {
        if (index < 0 || index >= loadedFloorSprites.Count) return;
        currentFloorIndex = index;
        Sprite sp = loadedFloorSprites[index];
        indoorMapDisplay.sprite = sp;

        if (txtCurrentFloorLabel != null) txtCurrentFloorLabel.text = loadedFloorNames[index];
        if (contentRect != null) contentRect.localScale = Vector3.one;

        float containerWidth = 1080f; 
        if (viewportRect != null) containerWidth = viewportRect.rect.width;
        else if (indoorViewerCanvas != null) containerWidth = indoorViewerCanvas.GetComponent<RectTransform>().rect.width;

        float aspect = sp.rect.width / sp.rect.height;
        contentRect.sizeDelta = new Vector2(containerWidth, containerWidth / aspect);
        contentRect.anchoredPosition = Vector2.zero;

        for (int i = 0; i < floorBtnBackgrounds.Count; i++)
        {
            if (floorBtnBackgrounds[i] == null || floorBtnTexts[i] == null) continue;

            bool isSelected = (i == index);
            floorBtnBackgrounds[i].color = isSelected ? selectedBgColor : unselectedBgColor;
            floorBtnTexts[i].color = isSelected ? selectedTextColor : unselectedTextColor;
        }
    }

    public void HideIndoorViewer() { if (indoorViewerCanvas != null) indoorViewerCanvas.enabled = false; }
}
