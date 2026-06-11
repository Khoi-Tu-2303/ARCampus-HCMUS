using UnityEngine;
using TMPro;

public class RegisterKeyboardAdjuster : MonoBehaviour
{
    [Header("References")]
    public RectTransform loginPanel;
    public TMP_InputField inputRegFullName;
    public TMP_InputField inputRegStudentID;

    private float _defaultPanelY;
    private Canvas _canvas;
    private bool _isAdjusted = false;  

    void Start()
    {
        _canvas = loginPanel.GetComponentInParent<Canvas>();
        _defaultPanelY = loginPanel.anchoredPosition.y;

        inputRegFullName.onSelect.AddListener(OnLowerInputSelected);
        inputRegStudentID.onSelect.AddListener(OnLowerInputSelected);

        inputRegFullName.onDeselect.AddListener(OnInputDeselected);
        inputRegStudentID.onDeselect.AddListener(OnInputDeselected);
    }

    void OnLowerInputSelected(string text)
    {
        StopCoroutine("DelayedReset");

        if (!_isAdjusted)
        {
            StartCoroutine(AdjustForKeyboard());
        }
    }

    void OnInputDeselected(string text)
    {
        StartCoroutine(DelayedReset());
    }

    System.Collections.IEnumerator AdjustForKeyboard()
    {
        yield return new WaitForSeconds(0.3f);

        float keyboardHeight = GetKeyboardHeight();
        if (keyboardHeight > 0)
        {
            ShiftPanelUp(keyboardHeight);
            _isAdjusted = true;
        }
    }

    System.Collections.IEnumerator DelayedReset()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (!inputRegFullName.isFocused && !inputRegStudentID.isFocused)
        {
            ResetPanel();
            _isAdjusted = false;
        }
    }

    float GetKeyboardHeight()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");
                AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect");
                decorView.Call("getWindowVisibleDisplayFrame", rect);

                int screenHeight = Screen.height;
                int visibleHeight = rect.Get<int>("bottom");
                int keyboardHeight = screenHeight - visibleHeight;

                return keyboardHeight > 100 ? keyboardHeight : 0f;
            }
        }
        catch
        {
            return TouchScreenKeyboard.area.height;
        }
#else
        return TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
#endif
    }

    void ShiftPanelUp(float keyboardHeight)
    {
        float scaleFactor = _canvas.scaleFactor;
        float offset = keyboardHeight / scaleFactor;

        Vector2 pos = loginPanel.anchoredPosition;
        pos.y = _defaultPanelY + offset;
        loginPanel.anchoredPosition = pos;
    }

    void ResetPanel()
    {
        Vector2 pos = loginPanel.anchoredPosition;
        pos.y = _defaultPanelY;
        loginPanel.anchoredPosition = pos;
    }

    void OnDestroy()
    {
        if (inputRegFullName != null)
        {
            inputRegFullName.onSelect.RemoveListener(OnLowerInputSelected);
            inputRegFullName.onDeselect.RemoveListener(OnInputDeselected);
        }
        if (inputRegStudentID != null)
        {
            inputRegStudentID.onSelect.RemoveListener(OnLowerInputSelected);
            inputRegStudentID.onDeselect.RemoveListener(OnInputDeselected);
        }
    }
}