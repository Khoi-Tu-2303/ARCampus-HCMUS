using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SearchPanelController : MonoBehaviour
{
    [Header("Setup")]
    public TMP_InputField searchInput;
    public Transform contentParent;       // SearchScrollView → Viewport → Content
    public GameObject resultBtnPrefab;    // ResultButton prefab

    private List<LocationData> allLocations = new List<LocationData>();

    void OnEnable()
    {
        // Lấy danh sách địa điểm từ FirebaseManager
        allLocations = FirebaseManager.Instance.GetAllLocations();

        // Lắng nghe khi người dùng gõ
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        // Hiện tất cả lúc mới mở
        ShowResults(allLocations);
    }

    void OnDisable()
    {
        searchInput.onValueChanged.RemoveListener(OnSearchChanged);
    }

    void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            ShowResults(allLocations);
            return;
        }

        // Filter theo tên hoặc category
        var filtered = allLocations
            .Where(loc => loc.display_name.ToLower().Contains(keyword.ToLower())
                       || loc.category.ToLower().Contains(keyword.ToLower()))
            .ToList();

        ShowResults(filtered);
    }

    void ShowResults(List<LocationData> results)
    {
        // Xóa kết quả cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Tạo button cho mỗi kết quả
        foreach (var loc in results)
        {
            var btn = Instantiate(resultBtnPrefab, contentParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = loc.display_name;

            LocationData captured = loc;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnResultSelected(captured);
            });
        }
    }

    void OnResultSelected(LocationData loc)
    {
        Debug.Log($"📍 Selected: {loc.display_name}");
        // TODO: Mở navigation đến địa điểm này
        CampusUIManager.Instance.CloseAllPanels();
    }
}