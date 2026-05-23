// UI/SystemModalController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum WarningType { GPS, Server }
public enum BackActionTarget { GoToLogin, GoToMain }

public class SystemModalController : MonoBehaviour
{
    public static SystemModalController Instance;

    [Header("UI References")]
    public GameObject modalPanel;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDesc;

    [Header("Buttons")]
    public Button btnRetry;
    public Button btnBack;
    public TextMeshProUGUI txtBtnBack; // Để đổi chữ "Quay lại" hoặc "Thoát"

    private WarningType _currentType;
    private BackActionTarget _currentBackTarget;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (modalPanel != null) modalPanel.SetActive(false);

        // Cắm dây sự kiện
        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
        if (btnBack != null) btnBack.onClick.AddListener(OnBackClicked);
    }

    /// <summary>
    /// Gọi hàm này ở bất cứ đâu để bật Modal lên
    /// VD: SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToMain);
    /// </summary>
    public void ShowWarning(WarningType type, BackActionTarget backTarget)
    {
        _currentType = type;
        _currentBackTarget = backTarget;

        // Tự động "thay ruột" Modal dựa vào loại lỗi
        if (type == WarningType.GPS)
        {
            if (txtTitle) txtTitle.text = "Mất tín hiệu GPS";
            if (txtDesc) txtDesc.text = "Không xác định được vị trí hiện tại\nVui lòng tìm vị trí tín hiệu tốt hơn";
            if (txtBtnBack) txtBtnBack.text = "Quay lại";
        }
        else if (type == WarningType.Server)
        {
            if (txtTitle) txtTitle.text = "Server mất kết nối";
            if (txtDesc) txtDesc.text = "Kết nối Server của bạn đã mất\nVui lòng kiểm tra kết nối mạng";
            if (txtBtnBack) txtBtnBack.text = "Thoát"; // Figma của ông để chữ Thoát
        }

        if (modalPanel != null) modalPanel.SetActive(true);
    }

    public void HideModal()
    {
        if (modalPanel != null) modalPanel.SetActive(false);
    }

    // ==========================================
    // LOGIC NÚT BẤM
    // ==========================================
    private void OnRetryClicked()
    {
        if (_currentType == WarningType.GPS)
        {
            Debug.Log("🔄 [Modal] Đang tải lại GPS...");
            // CHECK LOGIC GPS: Nếu đã có tín hiệu trở lại thì tắt bảng
            if (GPSService.Instance != null && GPSService.Instance.IsReady)
            {
                // Thêm logic check sai số (Accuracy) ở đây nếu cần.
                // VD: if (GPSService.Instance.Accuracy < 20f) { ... }
                HideModal();
            }
            else
            {
                Debug.LogWarning("⚠️ [Modal] GPS vẫn chưa có, không tắt bảng!");
                // (Có thể thêm hiệu ứng rung lắc cái bảng ở đây cho sinh động)
            }
        }
        else if (_currentType == WarningType.Server)
        {
            Debug.Log("🔄 [Modal] Đang kết nối lại Server...");
            // Tạm để đó, sau này team Server nối API vô đây
            // Gọi hàm check server, nếu OK thì HideModal();
        }
    }

    private void OnBackClicked()
    {
        HideModal();

        if (_currentBackTarget == BackActionTarget.GoToLogin)
        {
            Debug.Log("🚪 [Modal] Quay về màn hình Đăng Nhập (Login Scene)");

            // =========================================================
            // 🛑 TODO CHO TEAM: KHI NÀO CÓ MÀN HÌNH ĐĂNG NHẬP THÌ MỞ COMMENT DÒNG DƯỚI
            // (Nhớ đổi "LoginScene" thành tên Scene thật của team làm)
            // =========================================================
            // UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
        else if (_currentBackTarget == BackActionTarget.GoToMain)
        {
            Debug.Log("🏠 [Modal] Quay về màn hình Main (Hủy các tác vụ đang dở)");

            // 1. Nếu đang dẫn đường thì tắt cmn đi để dọn sạch bản đồ
            if (NavigationSession.Instance != null)
            {
                NavigationSession.Instance.CancelNavigation();
            }

            // =========================================================
            // 🛑 TODO CHO TEAM: NẾU MUỐN KHI VỀ MAIN NÓ TẮT LUÔN CẢ BẢNG TÌM KIẾM/CHI TIẾT:
            // =========================================================
            if (SearchPanelController.Instance != null) SearchPanelController.Instance.gameObject.SetActive(false);
            if (LocationDetailController.Instance != null) LocationDetailController.Instance.ClosePanel();
        }
    }
}