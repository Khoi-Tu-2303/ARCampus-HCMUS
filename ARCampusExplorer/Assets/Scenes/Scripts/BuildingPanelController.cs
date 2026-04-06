using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildingPanelController : MonoBehaviour
{
    [Header("Setup")]
    public Transform contentParent;      // ScrollView → Viewport → Content
    public GameObject buildingBtnPrefab; // Prefab vừa tạo

    // Danh sách tòa — sau này load từ Firebase
    private List<string> buildings = new List<string>
    {
        "Tòa A", "Tòa B", "Tòa C", "Tòa D",
        "Tòa E", "Tòa F", "Tòa G", "Thư viện"
    };

    void OnEnable()
    {
        // Mỗi lần mở panel → load lại danh sách
        LoadBuildings();
    }

    void LoadBuildings()
    {
        // Xóa buttons cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Tạo button cho mỗi tòa
        foreach (var building in buildings)
        {
            var btn = Instantiate(buildingBtnPrefab, contentParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = building;

            // Capture biến để dùng trong lambda
            string b = building;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnBuildingSelected(b);
            });
        }
    }

    void OnBuildingSelected(string buildingName)
    {
        Debug.Log($"🏢 Selected: {buildingName}");
        // TODO: Mở FloorMap panel với tòa được chọn
        CampusUIManager.Instance.CloseAllPanels();
    }
}
