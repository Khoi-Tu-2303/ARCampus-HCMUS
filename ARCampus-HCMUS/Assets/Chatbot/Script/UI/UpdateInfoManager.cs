using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using ChatApp.Managers;

public class UpdateInfoManager : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button btnBack;
    [SerializeField] private TMP_Text textUserLabel;
    [SerializeField] private TMP_Text textTitle;

    [Header("Department Buttons")]
    [SerializeField] private Button btnThuVien;
    [SerializeField] private Button btnPCTSV;
    [SerializeField] private Button btnPDT;

    [Header("Scenes")]
    [SerializeField] private string loginSceneName = "LoginScene";

    [Header("Dep Info Panel (cùng Canvas, ẩn ban đầu)")]
    [SerializeField] private DepInfoManager depInfoManager;   

    [Header("Panels")]
    [SerializeField] private GameObject depPanel;

    
    private readonly Dictionary<string, string> _depDocIdMap = new()
    {
        { "ThuVien", "TV"    },
        { "PCTSV",   "P_CTSV" },
        { "PDT",     "P_DT"   }
    };

    
    private readonly Dictionary<string, string> _depDisplayName = new()
    {
        { "ThuVien", "Thư viện"                  },
        { "PCTSV",   "Phòng Công tác Sinh viên"  },
        { "PDT",     "Phòng Đào tạo"             }
    };

    private string _currentUserId;     
    private string _defaultTitle;

    void Start()
    {
        
        var user = AuthManager.Instance.CurrentUser;
        if (user == null)
        {
            
            SceneManager.LoadScene(loginSceneName);
            return;
        }

        _currentUserId = user.id;
        textUserLabel.text = user.IsGuest ? "Khách" : user.username;

        Debug.Log($"[UpdateInfoManager] CurrentUserId = {_currentUserId}");

        if (textTitle != null)
            _defaultTitle = textTitle.text;

        
        btnBack.onClick.AddListener(OnBackClicked);
        btnThuVien.onClick.AddListener(() => OnDepartmentClicked("ThuVien"));
        btnPCTSV.onClick.AddListener(() => OnDepartmentClicked("PCTSV"));
        btnPDT.onClick.AddListener(() => OnDepartmentClicked("PDT"));

        
        if (depInfoManager != null)
            depInfoManager.gameObject.SetActive(false);
    }

    
    private void OnBackClicked()
    {
        if (depInfoManager != null && depInfoManager.gameObject.activeSelf)
        {
            depInfoManager.gameObject.SetActive(false); 
            if (depPanel != null) depPanel.SetActive(true); 
            if (textTitle != null)
                textTitle.text = _defaultTitle;
            return;
        }

        AuthManager.Instance.Logout(() => SceneManager.LoadScene(loginSceneName));
    }

    private void OnDepartmentClicked(string depKey)
    {
        if (depInfoManager == null)
        {
            Debug.LogError("[UpdateInfoManager] depInfoManager chưa được gán.");
            return;
        }

        string docId = _depDocIdMap[depKey];
        string displayName = _depDisplayName[depKey];

        Debug.Log($"[UpdateInfoManager] User '{_currentUserId}' chọn department '{displayName}'.");

        if (textTitle != null)
            textTitle.text = displayName;

        depInfoManager.Open(docId, displayName);
    }
}
