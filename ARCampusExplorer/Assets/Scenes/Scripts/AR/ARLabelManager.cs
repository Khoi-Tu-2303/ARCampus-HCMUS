// AR/ARLabelManager.cs — PATCHED
// FIXES:
// [LOW-MEDIUM] ToggleGrayLabels called GetComponentInChildren<Canvas>() and
//              GetComponentsInChildren<Renderer>() inside a loop over all labels.
//              These are expensive hierarchy traversals (O(depth) per call).
//              Solution: Cache Canvas and Renderer[] inside LabelEntry at Register time.

using UnityEngine;
using System.Collections.Generic;

public class ARLabelManager : MonoBehaviour
{
    // ── LabelEntry stores cached components to avoid repeated GetComponent calls ──
    private struct LabelEntry
    {
        public Transform labelTransform;
        public float minHeight;
        public float maxHeight;
        public float nearDistance;
        public float farDistance;
        // Cached at Register time
        public Canvas canvas;
        public Renderer[] renderers;
    }

    private static ARLabelManager _instance;
    public static ARLabelManager Instance => _instance;

    private List<LabelEntry> _labels = new List<LabelEntry>(16);
    private Transform _camTransform;
    private float _updateTimer;
    private const float UPDATE_INTERVAL = 0.05f; // 20 Hz

    /// <summary>When true, LateUpdate skips position management and labels are hidden.</summary>
    public bool isPausedByNav = false;

    void Awake() => _instance = this;

    // ──────────────────────────────────────────────────────────
    // REGISTER / UNREGISTER
    // ──────────────────────────────────────────────────────────

    public static void Register(Transform t, float minH, float maxH, float near, float far)
    {
        if (_instance == null) return;

        // Cache components at registration time (once per label lifetime)
        Canvas cv = t.GetComponentInChildren<Canvas>(true);
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);

        _instance._labels.Add(new LabelEntry
        {
            labelTransform = t,
            minHeight = minH,
            maxHeight = maxH,
            nearDistance = near,
            farDistance = far,
            canvas = cv,
            renderers = renderers
        });

        // If navigation is active, immediately hide this newly registered label
        if (_instance.isPausedByNav)
        {
            if (cv != null) cv.enabled = false;
            foreach (var r in renderers) r.enabled = false;
        }
    }

    public static void Unregister(Transform t)
    {
        if (_instance == null) return;
        for (int i = _instance._labels.Count - 1; i >= 0; i--)
        {
            if (_instance._labels[i].labelTransform == t)
            {
                _instance._labels.RemoveAt(i);
                break;
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────

    void Start() => _camTransform = Camera.main?.transform;

    void LateUpdate()
    {
        if (isPausedByNav) return;

        if (_camTransform == null) { _camTransform = Camera.main?.transform; return; }

        _updateTimer += Time.deltaTime;
        if (_updateTimer < UPDATE_INTERVAL) return;
        _updateTimer = 0f;

        Vector3 camPos = _camTransform.position;

        for (int i = _labels.Count - 1; i >= 0; i--)
        {
            var e = _labels[i];
            if (e.labelTransform == null) { _labels.RemoveAt(i); continue; }

            Vector3 pos = e.labelTransform.position;
            float dx = pos.x - camPos.x;
            float dz = pos.z - camPos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            // 🛑 BẢN FIX: ẨN NHÃN KHI USER ĐỨNG QUÁ GẦN (< nearDistance)
            bool shouldBeVisible = dist >= e.nearDistance;

            // Bật/Tắt Canvas
            if (e.canvas != null && e.canvas.enabled != shouldBeVisible)
                e.canvas.enabled = shouldBeVisible;

            // Bật/Tắt các Renderer (Model 3D nếu có)
            if (e.renderers != null)
            {
                foreach (var r in e.renderers)
                {
                    if (r != null && r.enabled != shouldBeVisible)
                        r.enabled = shouldBeVisible;
                }
            }

            // Đã tàng hình rồi thì không cần phải xoay mặt hay tính toán độ cao nữa cho đỡ tốn CPU
            if (!shouldBeVisible) continue;

            // Billboard — face camera on Y axis only
            float bx = camPos.x - pos.x;
            float bz = camPos.z - pos.z;
            if (bx * bx + bz * bz > 0.01f)
                e.labelTransform.rotation = Quaternion.LookRotation(new Vector3(-bx, 0f, -bz));

            // Height lerp based on distance
            float ratio = Mathf.Clamp01((dist - e.nearDistance) / (e.farDistance - e.nearDistance));
            pos.y = Mathf.Lerp(e.minHeight, e.maxHeight, ratio);
            e.labelTransform.position = pos;
        }
    }

    // ──────────────────────────────────────────────────────────
    // TOGGLE VISIBILITY — uses cached Canvas/Renderer, O(n) not O(n*depth)
    // ──────────────────────────────────────────────────────────

    public void ToggleGrayLabels(bool isNavigating)
    {
        isPausedByNav = isNavigating;

        if (_labels == null) return;

        foreach (var entry in _labels)
        {
            if (entry.labelTransform == null) continue;

            // Use cached Canvas reference
            if (entry.canvas != null)
                entry.canvas.enabled = !isNavigating;

            // Use cached Renderer[] reference
            if (entry.renderers != null)
                foreach (var r in entry.renderers)
                    if (r != null) r.enabled = !isNavigating;
        }
    }
}