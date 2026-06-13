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
    
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    
    private string _currentFieldKey;
    private Action<string, string> _onConfirmCallback; 

    void Start()
    {
        
        btnCancel.onClick.AddListener(Close);
        btnConfirm.onClick.AddListener(OnConfirmClicked);

        
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
        
    }


    
    
    
    public void Close()
    {
        
        inputField.DeactivateInputField();
        inputField.ReleaseSelection();
        SetVisible(false);
    }

    
    
    
    private void OnConfirmClicked()
    {
        string newValue = inputField.text.Trim();
        _onConfirmCallback?.Invoke(_currentFieldKey, newValue);
        Close();
    }

    
    private void SetVisible(bool visible)
    {
        if (overlay != null) overlay.SetActive(visible);
        gameObject.SetActive(visible);
    }
}

