using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InGameConsole : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject consolePanel;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;

    private List<string> logLines = new List<string>();
    private int maxLines = 50;

    // NÂNG CẤP: Dùng bộ nhớ HashSet để nhớ mặt TẤT CẢ các log đã từng in ra
    private HashSet<string> seenLogs = new HashSet<string>();

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    public void ToggleConsole()
    {
        if (consolePanel != null) consolePanel.SetActive(!consolePanel.activeSelf);
    }

    public void ClearLog()
    {
        logLines.Clear();
        seenLogs.Clear(); // Xóa luôn bộ nhớ để nếu nó bị lại thì còn biết
        if (logText != null) logText.text = "";
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 1. Chỉ lọc duy nhất cái cảnh báo Font chữ (vì cái này chắc chắn vô hại và chiếm diện tích)
        if (logString.Contains("Unicode value") || logString.Contains("LiberationSans SDF"))
        {
            return;
        }

        // 2. CHỐNG SPAM TUYỆT ĐỐI: Tạo "CMND" cho từng dòng log
        string uniqueKey = logString + stackTrace;

        // Nếu bộ nhớ đã từng thấy dòng log này rồi -> Chặn họng luôn, không in nữa
        if (seenLogs.Contains(uniqueKey))
        {
            return;
        }

        // Nếu là lỗi mới -> Lưu vào bộ nhớ để chặn các lần sau
        seenLogs.Add(uniqueKey);

        string color = "white";
        string finalMessage = logString;

        if (type == LogType.Warning) color = "yellow";
        else if (type == LogType.Error || type == LogType.Exception)
        {
            color = "red";
            // Kèm theo vị trí dòng code bị lỗi để anh em mình biết đường sửa
            finalMessage = logString + "\n<size=70%>" + stackTrace + "</size>";
        }

        string newLog = $"<color={color}>{finalMessage}</color>";
        logLines.Add(newLog);

        if (logLines.Count > maxLines) logLines.RemoveAt(0);

        if (logText != null) logText.text = string.Join("\n\n", logLines);
    }
}