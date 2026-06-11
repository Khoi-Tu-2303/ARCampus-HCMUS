//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Gắn vào: Panel chứa DepInfoScrollView (cùng cấp với DepartmentScrollView).
///// Panel này ẩn ban đầu; được bật khi user chọn một department.
/////
///// Hierarchy:
/////   DepInfoScrollView
/////     Viewport
/////       Content
/////         GioiThieuChung  → Button + Text(TMP)
/////         ViTri           → Button + Text(TMP)
/////         ThoiGian        → Button + Text(TMP)
/////         QuyDinh         → Button + Text(TMP)
/////         ThongTinLienLac → Button + Text(TMP)
/////         ThongBao        → Button + Text(TMP)
///// </summary>
//public class DepInfoManager : MonoBehaviour
//{
//    [Header("Info Buttons (theo thứ tự trong scroll)")]
//    [SerializeField] private Button btnGioiThieuChung;
//    [SerializeField] private Button btnViTri;
//    [SerializeField] private Button btnThoiGian;
//    [SerializeField] private Button btnQuyDinh;
//    [SerializeField] private Button btnThongTinLienLac;
//    [SerializeField] private Button btnThongBao;

//    [Header("Text hiển thị trên từng nút")]
//    [SerializeField] private TMP_Text txtGioiThieuChung;
//    [SerializeField] private TMP_Text txtViTri;
//    [SerializeField] private TMP_Text txtThoiGian;
//    [SerializeField] private TMP_Text txtQuyDinh;
//    [SerializeField] private TMP_Text txtThongTinLienLac;
//    [SerializeField] private TMP_Text txtThongBao;

//    [Header("Popup")]
//    [SerializeField] private PopupUpdateManager popupManager;

//    [Header("Loading indicator (tuỳ chọn)")]
//    [SerializeField] private GameObject loadingOverlay;

//    // ── Keys Firestore tương ứng với từng button ──
//    private static readonly (string key, string label)[] _fields =
//    {
//        ("GioiThieuChung",  "1. Giới thiệu chung"),
//        ("ViTri",           "2. Vị trí"),
//        ("ThoiGian",        "3. Thời gian"),
//        ("QuyDinh",         "4. Quy định"),
//        ("ThongTinLienLac", "5. Thông tin liên lạc"),
//        ("ThongBao",        "6. Thông báo"),
//    };

//    private string _currentDocId;
//    private Dictionary<string, string> _currentData = new();

//    // Cache TMP text theo key để dễ cập nhật
//    private Dictionary<string, TMP_Text> _textMap;
//    private Button[] _buttons;

//    void Awake()
//    {
//        _textMap = new Dictionary<string, TMP_Text>
//        {
//            { "GioiThieuChung",  txtGioiThieuChung  },
//            { "ViTri",           txtViTri           },
//            { "ThoiGian",        txtThoiGian        },
//            { "QuyDinh",         txtQuyDinh         },
//            { "ThongTinLienLac", txtThongTinLienLac },
//            { "ThongBao",        txtThongBao        },
//        };

//        _buttons = new[] { btnGioiThieuChung, btnViTri, btnThoiGian, btnQuyDinh, btnThongTinLienLac, btnThongBao };

//        // Gán sự kiện click cho từng nút
//        for (int i = 0; i < _fields.Length; i++)
//        {
//            int idx = i; // capture
//            _buttons[idx].onClick.AddListener(() => OnInfoButtonClicked(_fields[idx].key, _fields[idx].label));
//        }
//    }

//    // ─────────────────────────────────────────────
//    //  PUBLIC: Được gọi từ UpdateInfoManager
//    // ─────────────────────────────────────────────
//    public async void Open(string documentId, string departmentDisplayName)
//    {
//        _currentDocId = documentId;
//        _currentData.Clear();

//        gameObject.SetActive(true);
//        SetButtonsInteractable(false); // disable NGAY, trước mọi await

//        if (loadingOverlay != null) loadingOverlay.SetActive(true);

//        // Reset label
//        foreach (var field in _fields)
//            if (_textMap.TryGetValue(field.key, out TMP_Text tmp))
//                tmp.text = field.label;

//        // ── Chờ Firebase init ──
//        float waited = 0f;
//        while (!FirebaseManager.Instance.IsInitialized && waited < 10f)
//        {
//            await System.Threading.Tasks.Task.Delay(100);
//            waited += 0.1f;
//        }

//        if (!FirebaseManager.Instance.IsInitialized)
//        {
//            Debug.LogError("[DepInfoManager] Firebase chưa khởi tạo sau 10 giây.");
//            if (loadingOverlay != null) loadingOverlay.SetActive(false);
//            SetButtonsInteractable(true);
//            return;
//        }

//        var data = await FirebaseManager.Instance.GetDepartmentInfo(documentId);

//        if (loadingOverlay != null) loadingOverlay.SetActive(false);

//        if (data != null)
//        {
//            _currentData = data;
//            RefreshUI();
//        }
//        else
//        {
//            Debug.LogError($"[DepInfoManager] Không thể tải dữ liệu cho '{documentId}'.");
//            // data null vẫn enable buttons để user có thể retry
//        }

//        SetButtonsInteractable(true); // ✅ luôn enable dù data null hay không
//    }

//    // ─────────────────────────────────────────────
//    //  Cập nhật UI sau khi nhận dữ liệu
//    // ─────────────────────────────────────────────
//    private void RefreshUI()
//    {
//        foreach (var field in _fields)
//        {
//            if (_textMap.TryGetValue(field.key, out TMP_Text tmp))
//            {
//                // ✅ Chỉ hiện label cố định, không hiện data
//                tmp.text = field.label;
//            }
//        }
//    }

//    // ─────────────────────────────────────────────
//    //  Khi click nút thông tin → mở popup
//    // ─────────────────────────────────────────────
//    private void OnInfoButtonClicked(string fieldKey, string label)
//    {
//        if (popupManager == null)
//        {
//            Debug.LogError("[DepInfoManager] popupManager chưa được gán.");
//            return;
//        }

//        // ✅ Guard: chưa load xong thì không mở popup
//        if (string.IsNullOrEmpty(_currentDocId))
//        {
//            Debug.LogWarning("[DepInfoManager] Chưa có department được chọn.");
//            return;
//        }

//        string currentValue = _currentData.TryGetValue(fieldKey, out string v) ? v : "";
//        popupManager.Open(
//            title: label,
//            fieldKey: fieldKey,
//            initialValue: currentValue,
//            onConfirm: OnPopupConfirmed
//        );
//    }

//    // ─────────────────────────────────────────────
//    //  Callback khi popup xác nhận
//    // ─────────────────────────────────────────────
//    private async void OnPopupConfirmed(string fieldKey, string newValue)
//    {
//        SetButtonsInteractable(false);
//        if (loadingOverlay != null) loadingOverlay.SetActive(true);

//        bool success = await FirebaseManager.Instance.UpdateDepartmentField(_currentDocId, fieldKey, newValue);

//        if (loadingOverlay != null) loadingOverlay.SetActive(false);
//        SetButtonsInteractable(true);

//        if (success)
//        {
//            _currentData[fieldKey] = newValue;
//            RefreshUI();
//            Debug.Log($"[DepInfoManager] Cập nhật '{fieldKey}' thành công.");
//        }
//        else
//        {
//            Debug.LogError($"[DepInfoManager] Cập nhật '{fieldKey}' thất bại.");
//        }
//    }

//    // ─────────────────────────────────────────────
//    private void SetButtonsInteractable(bool interactable)
//    {
//        foreach (var btn in _buttons)
//            if (btn != null) btn.interactable = interactable;
//    }
//}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;

public class DepInfoManager : MonoBehaviour
{
    [Header("Info Buttons")]
    [SerializeField] private Button btnGioiThieuChung;
    [SerializeField] private Button btnViTri;
    [SerializeField] private Button btnThoiGian;
    [SerializeField] private Button btnQuyDinh;
    [SerializeField] private Button btnThongTinLienLac;
    [SerializeField] private Button btnThongBao;

    [Header("Text trên từng nút")]
    [SerializeField] private TMP_Text txtGioiThieuChung;
    [SerializeField] private TMP_Text txtViTri;
    [SerializeField] private TMP_Text txtThoiGian;
    [SerializeField] private TMP_Text txtQuyDinh;
    [SerializeField] private TMP_Text txtThongTinLienLac;
    [SerializeField] private TMP_Text txtThongBao;

    [Header("Popup chỉnh sửa")]
    [SerializeField] private PopupUpdateManager popupManager;

    [Header("Loading Indicator")]
    [SerializeField] private GameObject loadingOverlay;

    [Header("Network Error Popup")]
    [SerializeField] private GameObject networkErrorOverlay;
    [SerializeField] private Button btnReload;
    [SerializeField] private Button btnExit;

    [Header("Panels")]
    [SerializeField] private GameObject depPanel;


    private static readonly (string key, string label)[] _fields =
    {
        ("GioiThieuChung",  "1. Giới thiệu chung"),
        ("ViTri",           "2. Vị trí"),
        ("ThoiGian",        "3. Thời gian"),
        ("QuyDinh",         "4. Quy định"),
        ("ThongTinLienLac", "5. Thông tin liên lạc"),
        ("ThongBao",        "6. Thông báo"),
    };

    private string _currentDocId;
    private Action _pendingAction; 

    private Dictionary<string, TMP_Text> _textMap;
    private Button[] _buttons;
    private bool _inited = false;

    // ─────────────────────────────────────────────
    //  Init (gọi thủ công vì object có thể inactive khi Awake)
    // ─────────────────────────────────────────────
    private void EnsureInit()
    {
        if (_inited) return;
        _inited = true;

        _textMap = new Dictionary<string, TMP_Text>
        {
            { "GioiThieuChung",  txtGioiThieuChung  },
            { "ViTri",           txtViTri           },
            { "ThoiGian",        txtThoiGian        },
            { "QuyDinh",         txtQuyDinh         },
            { "ThongTinLienLac", txtThongTinLienLac },
            { "ThongBao",        txtThongBao        },
        };

        _buttons = new[]
        {
            btnGioiThieuChung, btnViTri, btnThoiGian,
            btnQuyDinh, btnThongTinLienLac, btnThongBao
        };

        for (int i = 0; i < _fields.Length; i++)
        {
            int idx = i;
            _buttons[idx].onClick.AddListener(() =>
                OnInfoButtonClicked(_fields[idx].key, _fields[idx].label));
        }

        if (btnReload != null) btnReload.onClick.AddListener(OnReloadClicked);
        if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

        if (networkErrorOverlay != null) networkErrorOverlay.SetActive(false);
    }

    void Awake() => EnsureInit();

    // ─────────────────────────────────────────────
    //  PUBLIC: Gọi từ UpdateInfoManager
    // ─────────────────────────────────────────────
    public void Open(string documentId, string departmentDisplayName)
    {
        EnsureInit(); // đảm bảo kể cả khi Awake chưa chạy

        _currentDocId = documentId;
        _pendingAction = null;

        // Reset label
        foreach (var field in _fields)
            if (_textMap.TryGetValue(field.key, out var tmp))
                tmp.text = field.label;

        networkErrorOverlay?.SetActive(false);
        SetLoading(false);
        SetButtonsInteractable(true);
        if (depPanel != null) depPanel.SetActive(false);
        gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    //  Click button info → fetch field từ Firebase
    // ─────────────────────────────────────────────
    private async void OnInfoButtonClicked(string fieldKey, string label)
    {
        if (string.IsNullOrEmpty(_currentDocId)) return;

        // Lưu pending để Reload dùng lại — giống ConversationListController
        _pendingAction = () => OnInfoButtonClicked(fieldKey, label);

        SetButtonsInteractable(false);
        SetLoading(true);
        networkErrorOverlay?.SetActive(false);

        string value = null;
        try
        {
            // Không loop chờ Firebase — nếu chưa init thì catch và hiện lỗi
            value = await FirebaseManager.Instance.GetDepartmentField(_currentDocId, fieldKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DepInfoManager] Exception: {e.Message}");
        }

        SetLoading(false);
        SetButtonsInteractable(true);

        if (value == null)
        {
            ShowNetworkError();
            return;
        }

        _pendingAction = null; // thành công → xoá pending

        popupManager.Open(
            title: label,
            fieldKey: fieldKey,
            initialValue: value,
            onConfirm: OnPopupConfirmed
        );
    }

    // ─────────────────────────────────────────────
    //  Reload — giống ConversationListController
    // ─────────────────────────────────────────────
    private void OnReloadClicked()
    {
        networkErrorOverlay?.SetActive(false);
        _pendingAction?.Invoke();
    }

    // ─────────────────────────────────────────────
    //  Exit error popup → đóng, không làm gì thêm
    // ─────────────────────────────────────────────
    private void OnExitClicked()
    {
        _pendingAction = null;
        UIManager.Instance.GoToLogin();
    }

    // ─────────────────────────────────────────────
    //  Callback popup xác nhận → ghi Firebase
    // ─────────────────────────────────────────────
    private async void OnPopupConfirmed(string fieldKey, string newValue)
    {
        // Lưu pending để Reload có thể retry ghi
        _pendingAction = () => OnPopupConfirmed(fieldKey, newValue);

        SetButtonsInteractable(false);
        SetLoading(true);

        bool success = false;
        try
        {
            success = await FirebaseManager.Instance.UpdateDepartmentField(
                _currentDocId, fieldKey, newValue);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DepInfoManager] Ghi lỗi: {e.Message}");
        }

        SetLoading(false);
        SetButtonsInteractable(true);

        if (!success)
        {
            ShowNetworkError();
            return;
        }

        _pendingAction = null;
        Debug.Log($"[DepInfoManager] Cập nhật '{fieldKey}' thành công.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    private void ShowNetworkError()
    {
        Debug.Log($"[DepInfoManager] ShowNetworkError called. overlay={networkErrorOverlay}");

        if (networkErrorOverlay != null)
            networkErrorOverlay.SetActive(true);
        else
            Debug.LogWarning("[DepInfoManager] networkErrorOverlay chưa gán!");
    }

    private void SetLoading(bool on)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(on);
    }

    private void SetButtonsInteractable(bool v)
    {
        foreach (var btn in _buttons)
            if (btn != null) btn.interactable = v;
    }
}