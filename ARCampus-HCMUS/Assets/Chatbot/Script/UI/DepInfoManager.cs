


































































































































































































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

    
    
    
    public void Open(string documentId, string departmentDisplayName)
    {
        EnsureInit(); 

        _currentDocId = documentId;
        _pendingAction = null;

        
        foreach (var field in _fields)
            if (_textMap.TryGetValue(field.key, out var tmp))
                tmp.text = field.label;

        networkErrorOverlay?.SetActive(false);
        SetLoading(false);
        SetButtonsInteractable(true);
        if (depPanel != null) depPanel.SetActive(false);
        gameObject.SetActive(true);
    }

    
    
    
    private async void OnInfoButtonClicked(string fieldKey, string label)
    {
        if (string.IsNullOrEmpty(_currentDocId)) return;

        
        _pendingAction = () => OnInfoButtonClicked(fieldKey, label);

        SetButtonsInteractable(false);
        SetLoading(true);
        networkErrorOverlay?.SetActive(false);

        string value = null;
        try
        {
            
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

        _pendingAction = null; 

        popupManager.Open(
            title: label,
            fieldKey: fieldKey,
            initialValue: value,
            onConfirm: OnPopupConfirmed
        );
    }

    
    
    
    private void OnReloadClicked()
    {
        networkErrorOverlay?.SetActive(false);
        _pendingAction?.Invoke();
    }

    
    
    
    private void OnExitClicked()
    {
        _pendingAction = null;
        UIManager.Instance.GoToLogin();
    }

    
    
    
    private async void OnPopupConfirmed(string fieldKey, string newValue)
    {
        
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
