// Debug/InGameConsole.cs — PATCHED
// FIXES:
// [LOW] seenLogs HashSet grew unboundedly — unique stackTraces could consume MBs over a long
//       debug session. Solution: evict oldest entry when limit is exceeded (FIFO via Queue).

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
    private const int MAX_LINES = 50;

    // Bounded deduplication: HashSet for O(1) lookup, Queue for eviction order
    private HashSet<string> seenLogs = new HashSet<string>();
    private Queue<string> seenLogsQ = new Queue<string>();
    private const int MAX_SEEN = 200; // cap memory usage

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    public void ToggleConsole()
    {
        if (consolePanel != null) consolePanel.SetActive(!consolePanel.activeSelf);
    }

    public void ClearLog()
    {
        logLines.Clear();
        seenLogs.Clear();
        seenLogsQ.Clear();
        if (logText != null) logText.text = "";
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Filter known-harmless Unity font warnings
        if (logString.Contains("Unicode value") || logString.Contains("LiberationSans SDF"))
            return;

        // Deduplication — bounded by MAX_SEEN
        string uniqueKey = logString + stackTrace;
        if (seenLogs.Contains(uniqueKey)) return;

        // Evict oldest entry if at capacity
        if (seenLogs.Count >= MAX_SEEN)
        {
            string oldest = seenLogsQ.Dequeue();
            seenLogs.Remove(oldest);
        }

        seenLogs.Add(uniqueKey);
        seenLogsQ.Enqueue(uniqueKey);

        string color = "white";
        string finalMessage = logString;

        if (type == LogType.Warning)
            color = "yellow";
        else if (type == LogType.Error || type == LogType.Exception)
        {
            color = "red";
            finalMessage = logString + "\n<size=70%>" + stackTrace + "</size>";
        }

        logLines.Add($"<color={color}>{finalMessage}</color>");
        if (logLines.Count > MAX_LINES) logLines.RemoveAt(0);

        if (logText != null) logText.text = string.Join("\n\n", logLines);
    }
}
