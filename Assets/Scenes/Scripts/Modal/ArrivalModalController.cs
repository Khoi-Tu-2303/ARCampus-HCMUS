// UI/ArrivalModalController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArrivalModalController : MonoBehaviour
{
    public static ArrivalModalController Instance;

    [Header("UI References")]
    public GameObject modalPanel;       // Cái bảng chúc mừng (chứa cả lớp nền đen)
    public TextMeshProUGUI txtWelcome;  // Text "Bạn đã ở đây"

    [Header("Buttons")]
    public Button btnReturnToMain;
    public Button btnOpenIndoorMap;     // ✅ Đã đổi tên chuẩn thành Indoor Map

    // Biến lưu ID tòa nhà để truyền cho Map
    private string _currentBuildingId = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Vừa vào game là phải giấu nó đi ngay
        if (modalPanel != null) modalPanel.SetActive(false);

        // Cắm dây sự kiện cho 2 nút
        if (btnReturnToMain != null) btnReturnToMain.onClick.AddListener(OnReturnClicked);
        if (btnOpenIndoorMap != null) btnOpenIndoorMap.onClick.AddListener(OnIndoorMapClicked);
    }

    // ✅ Thêm tham số nodeId (VD: "C_CongChinh") để code tự bóc tách ra chữ "C"
    public void ShowModal(string destinationName, string nodeId = "")
    {
        if (txtWelcome != null)
        {
            // Set text hiển thị. 
            // Có thể chỉnh font/size trực tiếp trong Unity, code chỉ đẩy Data
            txtWelcome.text = $"{destinationName}";
        }

        // Lấy ID tòa nhà dựa vào NodeID
        _currentBuildingId = GetBuildingId(nodeId);

        // Logic thông minh: Có ID tòa nhà thì mới hiện nút "Mở Indoor Map"
        if (btnOpenIndoorMap != null)
        {
            btnOpenIndoorMap.gameObject.SetActive(!string.IsNullOrEmpty(_currentBuildingId));
        }

        if (modalPanel != null) modalPanel.SetActive(true);
    }

    void OnReturnClicked()
    {
        if (modalPanel != null) modalPanel.SetActive(false);

        // Bấm nút xong thì gọi Sếp Navigation dẹp đường, quay về màn hình chính
        if (NavigationSession.Instance != null)
        {
            NavigationSession.Instance.CancelNavigation();
        }
    }

    void OnIndoorMapClicked()
    {
        // 1. Ẩn modal và tắt chế độ dẫn đường ngoài trời
        if (modalPanel != null) modalPanel.SetActive(false);
        if (NavigationSession.Instance != null)
        {
            NavigationSession.Instance.CancelNavigation();
        }

        // 2. Mở bản đồ trong nhà của tòa nhà đó lên
        if (!string.IsNullOrEmpty(_currentBuildingId))
        {
            Debug.Log($"🗺️ [ArrivalModal] Mở Indoor Map cho tòa: {_currentBuildingId}");
            if (FloorViewer.Instance != null)
            {
                FloorViewer.Instance.OpenViewer(_currentBuildingId);
            }
        }
    }

    // ✅ Hàm gọt chuỗi: Bóc tách ID tòa nhà từ chuỗi NodeID
    string GetBuildingId(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";

        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXS") || nodeId.StartsWith("NXT")) return "NX";

        // Bắt các tòa nhà từ A đến G
        char c = nodeId[0];
        if (c >= 'A' && c <= 'G') return c.ToString();

        return ""; // Trả về rỗng nếu không thuộc tòa nào
    }
}