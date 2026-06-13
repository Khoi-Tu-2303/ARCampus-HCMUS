





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
    public TextMeshProUGUI txtBtnBack;

    private WarningType _currentType;
    private BackActionTarget _currentBackTarget;
    private bool _retryInProgress = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (modalPanel != null) modalPanel.SetActive(false);
        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
        if (btnBack != null) btnBack.onClick.AddListener(OnBackClicked);
    }

    
    
    

    public void ShowWarning(WarningType type, BackActionTarget backTarget)
    {
        _currentType = type;
        _currentBackTarget = backTarget;
        _retryInProgress = false;

        if (type == WarningType.GPS)
        {
            if (txtTitle) txtTitle.text = "Mất tín hiệu GPS";
            if (txtDesc) txtDesc.text = "Không xác định được vị trí hiện tại\nVui lòng bật GPS và nhấn Thử lại";
            if (txtBtnBack) txtBtnBack.text = "Quay lại";
        }
        else if (type == WarningType.Server)
        {
            if (txtTitle) txtTitle.text = "Server mất kết nối";
            if (txtDesc) txtDesc.text = "Kết nối Server của bạn đã mất\nVui lòng kiểm tra kết nối mạng";
            if (txtBtnBack) txtBtnBack.text = "Thoát";
        }

        if (modalPanel != null) modalPanel.SetActive(true);
    }

    public void HideModal()
    {
        if (modalPanel != null) modalPanel.SetActive(false);
        _retryInProgress = false;
    }

    
    
    

    private void OnRetryClicked()
    {
        if (_currentType == WarningType.GPS)
        {
            if (GPSService.Instance == null) return;

            if (GPSService.Instance.IsReady)
            {
                
                HideModal();
                return;
            }

            if (_retryInProgress) return; 
            _retryInProgress = true;

            
            
            if (txtDesc != null) txtDesc.text = "Đang kết nối lại GPS...";
            GPSService.Instance.RequestRestart();
        }
        else if (_currentType == WarningType.Server)
        {
            if (txtDesc != null) txtDesc.text = "Đang kết nối lại...";
            FirebaseService.Instance?.FetchAllLocations();
        }
    }

    private void OnBackClicked()
    {
        HideModal();

        if (_currentBackTarget == BackActionTarget.GoToLogin)
        {
            Debug.Log("🚪 [Modal] Quay về màn hình Đăng Nhập");
            
        }
        else if (_currentBackTarget == BackActionTarget.GoToMain)
        {
            Debug.Log("🏠 [Modal] Quay về Main");

            NavigationSession.Instance?.CancelNavigation();

            if (SearchPanelController.Instance != null)
                SearchPanelController.Instance.gameObject.SetActive(false);
            if (LocationDetailController.Instance != null)
                LocationDetailController.Instance.ClosePanel();
        }
    }
}
