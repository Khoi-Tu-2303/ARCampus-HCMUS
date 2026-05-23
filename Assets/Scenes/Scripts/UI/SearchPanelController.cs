// UI/SearchPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SearchPanelController : MonoBehaviour
{
    public static SearchPanelController Instance;
    [Header("Setup UI")]
    public TMP_InputField searchInput;
    public Transform contentParent;
    public GameObject resultBtnPrefab;

    [Header("Headers UI")]
    public GameObject recentHeader;
    public GameObject recommendedHeader;

    private List<LocationData> allRawLocations = new List<LocationData>();
    private Dictionary<string, List<LocationData>> groupedBuildings = new Dictionary<string, List<LocationData>>();

    private List<string> _buildingNames = new List<string>(32);
    private List<string> _normalizedNames = new List<string>(32);
    private List<string> _filteredNames = new List<string>(32);

    private List<string> _recentNames = new List<string>();
    private List<string> _recommendedNames = new List<string>(4);

    private List<GameObject> _buttonPool = new List<GameObject>(32);
    private List<Button> _buttonComponents = new List<Button>(32);
    private List<TextMeshProUGUI> _buttonTexts = new List<TextMeshProUGUI>(32);
    void Awake()
    {
        // ✅ THÊM CẢ DÒNG NÀY VÀO AWAKE ĐỂ GÁN INSTANCE
        Instance = this;
    }
    void OnEnable()
    {
        searchInput.onValueChanged.AddListener(OnSearchChanged);
        if (FirebaseService.Instance != null && FirebaseService.Instance.IsReady && FirebaseService.Instance.AllLocations.Count > 0)
            ProcessAndShowData();
        else if (FirebaseService.Instance != null)
            FirebaseService.Instance.OnLocationsLoaded += ProcessAndShowData;
    }

    void OnDisable()
    {
        searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        if (FirebaseService.Instance != null)
            FirebaseService.Instance.OnLocationsLoaded -= ProcessAndShowData;
    }

    void ProcessAndShowData()
    {
        allRawLocations = FirebaseService.Instance.GetAllLocations();
        groupedBuildings.Clear();

        foreach (var loc in allRawLocations)
        {
            string buildingName = GetBuildingNameFromLocation(loc);
            if (!groupedBuildings.ContainsKey(buildingName))
                groupedBuildings[buildingName] = new List<LocationData>();
            groupedBuildings[buildingName].Add(loc);
        }

        _buildingNames.Clear();
        _normalizedNames.Clear();
        foreach (var key in groupedBuildings.Keys)
        {
            _buildingNames.Add(key);
            _normalizedNames.Add(key.ToLowerInvariant());
        }

        GenerateRecommendations();
        OnSearchChanged("");
    }

    void GenerateRecommendations()
    {
        _recommendedNames.Clear();
        if (_buildingNames.Count == 0) return;
        var randomList = _buildingNames.OrderBy(x => Random.value).Take(4).ToList();
        _recommendedNames.AddRange(randomList);
    }

    string GetBuildingNameFromLocation(LocationData loc)
    {
        string id = loc.location_id;
        if (string.IsNullOrEmpty(id)) return "Khác";

        if (id.StartsWith("NĐH")) return "Nhà điều hành";
        if (id.StartsWith("NTD")) return "Nhà thể dục";
        if (id.StartsWith("NXT") || id.StartsWith("NXS")) return "Nhà xe";
        if (id.StartsWith("CAFE") || id.StartsWith("CT")) return "Khu ăn uống / Cafe";

        if (id.StartsWith("A")) return "Tòa A";
        if (id.StartsWith("B")) return "Tòa B";
        if (id.StartsWith("C") && id.Length <= 2) return "Tòa C";
        if (id.StartsWith("D")) return "Tòa D";
        if (id.StartsWith("E")) return "Tòa E";
        if (id.StartsWith("F")) return "Tòa F";
        if (id.StartsWith("G")) return "Tòa G";

        return "Khu vực khác";
    }

    void OnSearchChanged(string keyword)
    {
        _filteredNames.Clear();

        if (string.IsNullOrEmpty(keyword))
        {
            bool hasRecent = _recentNames.Count > 0;
            if (recentHeader != null) recentHeader.SetActive(hasRecent);
            if (recommendedHeader != null) recommendedHeader.SetActive(true);

            _filteredNames.AddRange(_recentNames);
            _filteredNames.AddRange(_recommendedNames);

            ShowResults(_filteredNames, true, _recentNames.Count);
        }
        else
        {
            if (recentHeader != null) recentHeader.SetActive(false);
            if (recommendedHeader != null) recommendedHeader.SetActive(false);

            string lkw = keyword.ToLowerInvariant();
            for (int i = 0; i < _normalizedNames.Count; i++)
            {
                if (_normalizedNames[i].Contains(lkw))
                    _filteredNames.Add(_buildingNames[i]);
            }
            ShowResults(_filteredNames, false, 0);
        }
    }

    void ShowResults(List<string> names, bool isDefaultMode, int recentCount)
    {
        int needed = names.Count;

        while (_buttonPool.Count < needed)
        {
            var btn = Instantiate(resultBtnPrefab, contentParent);
            _buttonPool.Add(btn);
            _buttonComponents.Add(btn.GetComponent<Button>());
            _buttonTexts.Add(btn.GetComponentInChildren<TextMeshProUGUI>());
        }

        for (int i = 0; i < _buttonPool.Count; i++)
        {
            bool active = i < needed;
            if (_buttonPool[i].activeSelf != active)
                _buttonPool[i].SetActive(active);

            if (active)
            {
                _buttonTexts[i].text = names[i];

                if (isDefaultMode)
                {
                    if (i < recentCount && recentHeader != null && recentHeader.activeSelf)
                        _buttonPool[i].transform.SetSiblingIndex(recentHeader.transform.GetSiblingIndex() + 1 + i);
                    else if (recommendedHeader != null)
                        _buttonPool[i].transform.SetSiblingIndex(recommendedHeader.transform.GetSiblingIndex() + 1 + (i - recentCount));
                }
                else
                {
                    _buttonPool[i].transform.SetAsLastSibling();
                }

                string captured = names[i];
                _buttonComponents[i].onClick.RemoveAllListeners();
                _buttonComponents[i].onClick.AddListener(() => {
                    OnBuildingSelected(captured);
                });
            }
        }
    }

    void OnBuildingSelected(string buildingName)
    {
        if (groupedBuildings.ContainsKey(buildingName))
        {
            List<LocationData> gates = groupedBuildings[buildingName];
            float userLat = (float)GPSService.Instance.Latitude;
            float userLng = (float)GPSService.Instance.Longitude;
            LocationData nearestGate = null;
            float minDistanceToUser = float.MaxValue;

            foreach (var gate in gates)
            {
                float dist = GeoMath.Haversine(userLat, userLng, (float)gate.lat, (float)gate.lng);
                if (dist < minDistanceToUser)
                {
                    minDistanceToUser = dist;
                    nearestGate = gate;
                }
            }

            if (LocationDetailController.Instance != null)
            {
                LocationDetailController.Instance.OpenDetailPanel(nearestGate);
            }
        }
    }
}