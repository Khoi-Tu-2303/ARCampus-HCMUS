
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArrivalModalController : MonoBehaviour
{
    public static ArrivalModalController Instance;

    [Header("UI References")]
    public GameObject modalPanel;       
    public TextMeshProUGUI txtWelcome;  

    [Header("Buttons")]
    public Button btnReturnToMain;
    public Button btnOpenIndoorMap;    

    
    private string _currentBuildingId = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (modalPanel != null) modalPanel.SetActive(false);

        if (btnReturnToMain != null) btnReturnToMain.onClick.AddListener(OnReturnClicked);
        if (btnOpenIndoorMap != null) btnOpenIndoorMap.onClick.AddListener(OnIndoorMapClicked);
    }

    public void ShowModal(string destinationName, string nodeId = "")
    {
        if (txtWelcome != null)
        {
            
            
            txtWelcome.text = $"{destinationName}";
        }

        
        _currentBuildingId = GetBuildingId(nodeId);

        
        if (btnOpenIndoorMap != null)
        {
            btnOpenIndoorMap.gameObject.SetActive(!string.IsNullOrEmpty(_currentBuildingId));
        }

        if (modalPanel != null) modalPanel.SetActive(true);
    }

    void OnReturnClicked()
    {
        if (modalPanel != null) modalPanel.SetActive(false);

        
        if (NavigationSession.Instance != null)
        {
            NavigationSession.Instance.CancelNavigation();
        }
    }

    void OnIndoorMapClicked()
    {
        
        if (modalPanel != null) modalPanel.SetActive(false);
        if (NavigationSession.Instance != null)
        {
            NavigationSession.Instance.CancelNavigation();
        }

        
        if (!string.IsNullOrEmpty(_currentBuildingId))
        {
            Debug.Log($"🗺️ [ArrivalModal] Mở Indoor Map cho tòa: {_currentBuildingId}");
            if (FloorViewer.Instance != null)
            {
                FloorViewer.Instance.OpenViewer(_currentBuildingId);
            }
        }
    }

    
    string GetBuildingId(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "";

        if (nodeId.StartsWith("NĐH")) return "NĐH";
        if (nodeId.StartsWith("NTD")) return "NTD";
        if (nodeId.StartsWith("NXS") || nodeId.StartsWith("NXT")) return "NX";

        
        char c = nodeId[0];
        if (c >= 'A' && c <= 'G') return c.ToString();

        return ""; 
    }
}
