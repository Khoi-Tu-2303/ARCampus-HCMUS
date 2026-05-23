using UnityEngine;

public class CampusUIManager : MonoBehaviour
{
    // Giữ lại cái này của ông để mốt mấy script khác (như MapController) dễ dàng gọi tới
    public static CampusUIManager Instance;

    [Header("UI Chính")]
    public GameObject bottomBar;

    [Header("Màn hình Popup (Overlays)")]
    public GameObject searchOverlay;
    public GameObject mapOverlay;
    public GameObject navigationOverlay;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Vừa vào app là dọn dẹp sạch sẽ, chỉ chừa lại BottomBar
        CloseAllPanels();
    }

    // --- CÁC HÀM GẮN VÀO NÚT BẤM DƯỚI BOTTOM BAR ---

    public void ToggleSearch()
    {
        bool isCurrentlyOn = searchOverlay.activeSelf;
        searchOverlay.SetActive(!isCurrentlyOn); // Đang tắt thì bật, đang bật thì tắt

        // Mở Search thì tự động tắt Map
        if (!isCurrentlyOn && mapOverlay != null) mapOverlay.SetActive(false);
    }

    public void ToggleMap()
    {
        bool isCurrentlyOn = mapOverlay.activeSelf;
        mapOverlay.SetActive(!isCurrentlyOn);

        // Mở Map thì tự động tắt Search
        if (!isCurrentlyOn && searchOverlay != null) searchOverlay.SetActive(false);
    }

    // --- CÁC HÀM ĐIỀU KHIỂN CHUNG ---

    public void CloseAllPanels()
    {
        if (searchOverlay != null) searchOverlay.SetActive(false);
        if (mapOverlay != null) mapOverlay.SetActive(false);
        if (navigationOverlay != null) navigationOverlay.SetActive(false);

        // Đảm bảo thanh menu luôn hiện
        if (bottomBar != null) bottomBar.SetActive(true);
    }

    public void StartNavigation()
    {
        CloseAllPanels();
        // Khi bắt đầu đi theo mũi tên AR, ẩn luôn cả thanh BottomBar cho rộng màn hình
        if (bottomBar != null) bottomBar.SetActive(true);
        if (navigationOverlay != null) navigationOverlay.SetActive(true);
    }

    public void StopNavigation()
    {
        if (navigationOverlay != null) navigationOverlay.SetActive(false);
        if (bottomBar != null) bottomBar.SetActive(true);
    }
}