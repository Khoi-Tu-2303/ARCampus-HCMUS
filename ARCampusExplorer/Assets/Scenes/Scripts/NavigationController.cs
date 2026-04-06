using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform arrowImage;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI instructionText;
    public Button btnCancel;

    private LocationData targetLocation;

    void OnEnable()
    {
        btnCancel.onClick.AddListener(CancelNavigation);
        UpdateUI("Chọn điểm đến để bắt đầu", "--");
    }

    void OnDisable()
    {
        btnCancel.onClick.RemoveListener(CancelNavigation);
    }

    // Gọi từ BuildingPanel hoặc SearchPanel khi user chọn điểm đến
    public void StartNavigation(LocationData destination)
    {
        targetLocation = destination;
        instructionText.text = $"Đang dẫn đến: {destination.display_name}";
        Debug.Log($"🧭 Navigation started → {destination.display_name}");
        // TODO Phase B: tính A* path + hiện AR arrow
    }

    void Update()
    {
        if (targetLocation == null) return;
        // TODO Phase B: tính góc + khoảng cách realtime
        UpdateArrowDirection(0f);
        UpdateDistance(0f);
    }

    void UpdateArrowDirection(float angleDeg)
    {
        // Xoay mũi tên theo hướng cần đi
        arrowImage.rotation = Quaternion.Euler(0, 0, -angleDeg);
    }

    void UpdateDistance(float distanceMeters)
    {
        distanceText.text = distanceMeters > 0
            ? $"{distanceMeters:F0} m"
            : "-- m";
    }

    void UpdateUI(string instruction, string distance)
    {
        instructionText.text = instruction;
        distanceText.text = distance;
    }

    void CancelNavigation()
    {
        targetLocation = null;
        CampusUIManager.Instance.CloseAllPanels();
        Debug.Log("🚫 Navigation cancelled");
    }
}
