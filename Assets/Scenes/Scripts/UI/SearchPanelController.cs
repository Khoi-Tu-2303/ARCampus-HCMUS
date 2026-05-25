// UI/SearchPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// ✅ TẠO 1 STRUCT ĐỂ LƯU KẾT QUẢ TÌM KIẾM (Cho cả Tòa nhà thật lẫn Phòng ảo)
public struct SearchResultItem
{
    public string DisplayText;
    public string TargetBuildingName; // Tên tòa nhà để dẫn đường tới
    public string IndoorDocId;        // ID của phòng (VD: F102, library). Rỗng thì là tòa nhà bthg.
}

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
    
    // ✅ ĐỔI TỪ LIST STRING SANG LIST CỦA STRUCT
    private List<SearchResultItem> _filteredResults = new List<SearchResultItem>(32);
    private List<SearchResultItem> _recentResults = new List<SearchResultItem>();
    private List<SearchResultItem> _recommendedResults = new List<SearchResultItem>(4);

    private List<GameObject> _buttonPool = new List<GameObject>(32);
    private List<Button> _buttonComponents = new List<Button>(32);
    private List<TextMeshProUGUI> _buttonTexts = new List<TextMeshProUGUI>(32);

    // ✅ BỘ TỪ ĐIỂN MAPPING (Khách gõ chữ bên trái -> Map ra dữ liệu bên phải)
    private Dictionary<string, (string building, string indoorId, string displayName)> specialAliases = new Dictionary<string, (string, string, string)>()
    {
        { "thư viện", ("Tòa C", "library", "Thư viện (Tòa C)") },
        { "library", ("Tòa C", "library", "Thư viện (Tòa C)") },
    };

    void Awake() { Instance = this; }

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
        _recommendedResults.Clear();
        if (_buildingNames.Count == 0) return;
        var randomList = _buildingNames.OrderBy(x => Random.value).Take(4).ToList();
        foreach (var name in randomList)
        {
            _recommendedResults.Add(new SearchResultItem { DisplayText = name, TargetBuildingName = name, IndoorDocId = "" });
        }
    }

    string GetBuildingNameFromLocation(LocationData loc)
    {
        string id = loc.location_id;
        if (string.IsNullOrEmpty(id)) return "Khác";
        if (id.StartsWith("FOOD_")) return string.IsNullOrEmpty(loc.display_name) ? "Quán ăn" : loc.display_name;
        if (id.StartsWith("NĐH")) return "Nhà điều hành";
        if (id.StartsWith("NTD")) return "Nhà thể dục";
        if (id.StartsWith("NXT") || id.StartsWith("NXS")) return "Nhà xe";
        if (id.StartsWith("CAFE") || id.StartsWith("CT")) || id.StartsWith("Căn")) return "Khu ăn uống / Cafe";

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
        _filteredResults.Clear();

        if (string.IsNullOrEmpty(keyword))
        {
            bool hasRecent = _recentResults.Count > 0;
            if (recentHeader != null) recentHeader.SetActive(hasRecent);
            if (recommendedHeader != null) recommendedHeader.SetActive(true);

            _filteredResults.AddRange(_recentResults);
            _filteredResults.AddRange(_recommendedResults);

            ShowResults(_filteredResults, true, _recentResults.Count);
        }
        else
        {
            if (recentHeader != null) recentHeader.SetActive(false);
            if (recommendedHeader != null) recommendedHeader.SetActive(false);

            string lkw = keyword.ToLowerInvariant().Trim();

            if (lkw.Contains("căng tin") || lkw.Contains("canteen") || lkw.Contains("nhà ăn"))
            {
                lkw = "căn tin"; // Tự động lái về chữ "căn tin" để khớp với Data
            }

            bool isSearchingFood = lkw.Contains("quán ăn") || lkw.Contains("đồ ăn") || lkw.Contains("ăn uống");
            // 1. TÌM THEO TÊN TÒA NHÀ GỐC NHƯ BÌNH THƯỜNG
            for (int i = 0; i < _normalizedNames.Count; i++)
            {
                string bName = _buildingNames[i];
                string bNameLower = _normalizedNames[i];

                // Lấy ID của điểm đang xét để check Prefix
                string firstId = groupedBuildings[bName][0].location_id;

                bool isMatch = bNameLower.Contains(lkw);

                // Nếu user gõ "quán ăn" và ID của điểm này bắt đầu bằng "FOOD_" -> Duyệt ngay & luôn!
                if (isSearchingFood && firstId.StartsWith("FOOD_"))
                {
                    isMatch = true;
                }

                if (isMatch)
                {
                    _filteredResults.Add(new SearchResultItem { DisplayText = bName, TargetBuildingName = bName, IndoorDocId = "" });
                }
            }

            // 2. KÍCH HOẠT BỘ NÃO TÌM KIẾM PHÒNG/TIỆN ÍCH
            var indoorMatches = ParseIndoorSearch(lkw);
            _filteredResults.AddRange(indoorMatches);

            ShowResults(_filteredResults, false, 0);
        }
    }

    // ✅ BỘ NÃO PHÂN TÍCH TỪ KHÓA ĐỂ SINH RA KẾT QUẢ ẢO (Magic lies here)
    List<SearchResultItem> ParseIndoorSearch(string lkw)
    {
        List<SearchResultItem> list = new List<SearchResultItem>();
        
        // 1. Check Tiện ích (Canteen, Thư viện)
        foreach (var alias in specialAliases)
        {
            if (alias.Key.Contains(lkw) || lkw.Contains(alias.Key))
            {
                list.Add(new SearchResultItem { 
                    TargetBuildingName = alias.Value.building, 
                    IndoorDocId = alias.Value.indoorId, 
                    DisplayText = alias.Value.displayName 
                });
            }
        }

        string cleanKw = lkw.Replace(" ", "");

        // 2. Check Phòng Học (VD: f102, a205)
        if (cleanKw.Length >= 2 && cleanKw.Length <= 4)
        {
            char buildingChar = char.ToUpper(cleanKw[0]);
            if (buildingChar >= 'A' && buildingChar <= 'G') // Tòa A đến G
            {
                string roomNumber = cleanKw.Substring(1);
                if (int.TryParse(roomNumber, out _)) // Đảm bảo phần đuôi là số
                {
                    string indoorId = $"{buildingChar}{roomNumber}"; // Nặn ra F102
                    string bName = $"Tòa {buildingChar}";
                    list.Add(new SearchResultItem {
                        TargetBuildingName = bName,
                        IndoorDocId = indoorId,
                        DisplayText = $"Phòng {indoorId} ({bName})"
                    });
                }
            }
        }

        // 3. Check Nhà điều hành (VD: dh2.3, dh23)
        // 3. Check Nhà điều hành (VD: dh2.3, dh23, ndh2.3, nd2.3)
        if (cleanKw.StartsWith("dh") || cleanKw.StartsWith("đh") || cleanKw.StartsWith("ndh") || cleanKw.StartsWith("nđh") || cleanKw.StartsWith("nd"))
        {
            // Bóc hết đống chữ cái rườm rà ra, chỉ chừa lại phần số
            string nums = cleanKw.Replace("dh", "").Replace("đh", "").Replace("ndh", "").Replace("nđh", "").Replace("nd", "")
                                 .Replace(".", "_").Replace("-", "_");

            if (!string.IsNullOrEmpty(nums))
            {
                if (nums.Contains("_")) // User gõ dh2_3 hoặc nd6.3
                {
                    list.Add(new SearchResultItem
                    {
                        TargetBuildingName = "Nhà điều hành",
                        IndoorDocId = $"DH_{nums}", // Ép thành format DH_2_3 để map với Firebase
                        DisplayText = $"Phòng {nums.Replace("_", ".")} (Nhà điều hành)"
                    });
                }
                else if (nums.Length >= 2) // User gõ lười dh23 hoặc nd63 -> tự bóc tách thành 6 và 3
                {
                    string floor = nums.Substring(0, 1);
                    string room = nums.Substring(1);
                    list.Add(new SearchResultItem
                    {
                        TargetBuildingName = "Nhà điều hành",
                        IndoorDocId = $"DH_{floor}_{room}",
                        DisplayText = $"Phòng {floor}.{room} (Nhà điều hành)"
                    });
                }
            }
        }

        return list;
    }

    void ShowResults(List<SearchResultItem> results, bool isDefaultMode, int recentCount)
    {
        int needed = results.Count;

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
                _buttonTexts[i].text = results[i].DisplayText;

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

                var capturedItem = results[i];
                _buttonComponents[i].onClick.RemoveAllListeners();
                _buttonComponents[i].onClick.AddListener(() => {
                    OnItemSelected(capturedItem);
                });
            }
        }
    }

    // ✅ HÀM CLICK ĐÃ ĐƯỢC NÂNG CẤP ĐỂ HIỂU ĐƯỢC KẾT QUẢ ẢO
    void OnItemSelected(SearchResultItem item)
    {
        if (groupedBuildings.ContainsKey(item.TargetBuildingName))
        {
            List<LocationData> gates = groupedBuildings[item.TargetBuildingName];
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

                // ⚠️ BƯỚC 3 SẮP TỚI MÌNH SẼ SỬA THÀNH:
                LocationDetailController.Instance.OpenDetailPanel(nearestGate, item.IndoorDocId, item.DisplayText);
            }
        }
    }
}