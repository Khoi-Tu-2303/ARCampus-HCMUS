using UnityEngine;
using UnityEngine.UI;

public class CampusUIManager : MonoBehaviour
{
    public static CampusUIManager Instance;

    [Header("Panels")]
    public GameObject bottomBar;
    public GameObject searchPanel;
    public GameObject buildingPanel;
    public GameObject mapPanel;
    public GameObject navigationOverlay;

    void Awake() => Instance = this;

    // Gọi từ nút "Tìm kiếm"
    public void OnSearchPressed()
    {
        searchPanel.SetActive(true);
        bottomBar.SetActive(false);
    }

    // Gọi từ nút "Tòa nhà"
    public void OnBuildingPressed()
    {
        buildingPanel.SetActive(true);
        bottomBar.SetActive(false);
    }

    // Gọi từ nút "Bản đồ"
    public void OnMapPressed()
    {
        mapPanel.SetActive(true);
        bottomBar.SetActive(false);
    }

    // Đóng tất cả panel, về camera AR
    public void CloseAllPanels()
    {
        searchPanel.SetActive(false);
        buildingPanel.SetActive(false);
        mapPanel.SetActive(false);
        bottomBar.SetActive(true);
    }

    // Bắt đầu navigate → ẩn hết UI
    public void StartNavigation()
    {
        CloseAllPanels();
        bottomBar.SetActive(false);
        navigationOverlay.SetActive(true);
    }

    // Dừng navigate → hiện lại UI
    public void StopNavigation()
    {
        navigationOverlay.SetActive(false);
        bottomBar.SetActive(true);
    }
}
