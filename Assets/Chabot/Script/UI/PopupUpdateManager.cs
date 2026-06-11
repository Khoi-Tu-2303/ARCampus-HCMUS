using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupUpdateManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text textTitle;
    [SerializeField] private TMP_InputField inputField;

    [Header("Buttons")]
    //[SerializeField] private Button btnExit;
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    // Trạng thái nội tại
    private string _currentFieldKey;
    private Action<string, string> _onConfirmCallback; // (fieldKey, newValue)

    void Start()
    {
        //btnExit.onClick.AddListener(Close);
        btnCancel.onClick.AddListener(Close);
        btnConfirm.onClick.AddListener(OnConfirmClicked);

        // Ẩn overlay + popup khi start
        SetVisible(false);
    }

    public void Open(string title, string fieldKey, string initialValue, Action<string, string> onConfirm)
    {
        _currentFieldKey = fieldKey;
        _onConfirmCallback = onConfirm;

        SetVisible(true);

        textTitle.text = title;

        inputField.DeactivateInputField();
        inputField.text = initialValue;
        inputField.ReleaseSelection();
        // Không cần ActivateInputField — user tự tap vào để nhập
    }


    // ─────────────────────────────────────────────
    //  Đóng popup
    // ─────────────────────────────────────────────
    public void Close()
    {
        // ✅ Deactivate trước khi ẩn, tránh caret bị treo
        inputField.DeactivateInputField();
        inputField.ReleaseSelection();
        SetVisible(false);
    }

    // ─────────────────────────────────────────────
    //  Xác nhận
    // ─────────────────────────────────────────────
    private void OnConfirmClicked()
    {
        string newValue = inputField.text.Trim();
        _onConfirmCallback?.Invoke(_currentFieldKey, newValue);
        Close();
    }

    // ─────────────────────────────────────────────
    private void SetVisible(bool visible)
    {
        if (overlay != null) overlay.SetActive(visible);
        gameObject.SetActive(visible);
    }
}

