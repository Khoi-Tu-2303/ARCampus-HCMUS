using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using ChatApp.Managers;

/// <summary>
/// Gắn vào: UpdateInfoPanel (root panel của scene UpdateInfoScene)
///
/// Hierarchy cần thiết:
///   UpdateInfoPanel
///     Header
///       Btn_Back          → Button
///       Text_UpdateInfo   → TMP (tiêu đề)
///       Text_UserLabel    → TMP (username)
///     DepartmentScrollView
///       Viewport
///         Content
///           Dep_ThuVien   → chứa Button + Text(TMP)
///           Dep_PCTSV     → chứa Button + Text(TMP)
///           Dep_PDT       → chứa Button + Text(TMP)
/// </summary>
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
    [SerializeField] private DepInfoManager depInfoManager;   // panel con

    [Header("Panels")]
    [SerializeField] private GameObject depPanel;

    // ── Map tên department → documentId trên Firestore ──
    private readonly Dictionary<string, string> _depDocIdMap = new()
    {
        { "ThuVien", "TV"    },
        { "PCTSV",   "P_CTSV" },
        { "PDT",     "P_DT"   }
    };

    // ── Tên hiển thị tương ứng ──
    private readonly Dictionary<string, string> _depDisplayName = new()
    {
        { "ThuVien", "Thư viện"                  },
        { "PCTSV",   "Phòng Công tác Sinh viên"  },
        { "PDT",     "Phòng Đào tạo"             }
    };

    private string _currentUserId;     // ← UserID lấy từ AuthManager
    private string _defaultTitle;

    void Start()
    {
        // ── Lấy thông tin user theo cơ chế AuthManager ──
        var user = AuthManager.Instance.CurrentUser;
        if (user == null)
        {
            // Chưa đăng nhập → về Login
            SceneManager.LoadScene(loginSceneName);
            return;
        }

        _currentUserId = user.id;
        textUserLabel.text = user.IsGuest ? "Khách" : user.username;

        Debug.Log($"[UpdateInfoManager] CurrentUserId = {_currentUserId}");

        if (textTitle != null)
            _defaultTitle = textTitle.text;

        // Gán sự kiện nút
        btnBack.onClick.AddListener(OnBackClicked);
        btnThuVien.onClick.AddListener(() => OnDepartmentClicked("ThuVien"));
        btnPCTSV.onClick.AddListener(() => OnDepartmentClicked("PCTSV"));
        btnPDT.onClick.AddListener(() => OnDepartmentClicked("PDT"));

        // Ẩn panel dep info ban đầu
        if (depInfoManager != null)
            depInfoManager.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    private void OnBackClicked()
    {
        if (depInfoManager != null && depInfoManager.gameObject.activeSelf)
        {
            depInfoManager.gameObject.SetActive(false); // ẩn DepInfoPanel
            if (depPanel != null) depPanel.SetActive(true); // ← thêm dòng này
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