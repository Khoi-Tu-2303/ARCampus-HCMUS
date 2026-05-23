using UnityEngine;
using System.Collections.Generic;

public class ARLabelManager : MonoBehaviour
{
    private struct LabelEntry
    {
        public Transform labelTransform;
        public float minHeight;
        public float maxHeight;
        public float nearDistance;
        public float farDistance;
    }

    private static ARLabelManager _instance;
    public static ARLabelManager Instance => _instance;
    private List<LabelEntry> _labels = new List<LabelEntry>(16);
    private Transform _camTransform;
    private float _updateTimer;
    private const float UPDATE_INTERVAL = 0.05f; // 20Hz đủ rồi, không cần 60Hz

    // ✅ CÔNG TẮC KHÓA UPDATE KHI ĐANG DẪN ĐƯỜNG
    public bool isPausedByNav = false;

    void Awake() { _instance = this; }

    public static void Register(Transform t, float minH, float maxH, float near, float far)
    {
        if (_instance == null) return;
        _instance._labels.Add(new LabelEntry
        {
            labelTransform = t,
            minHeight = minH,
            maxHeight = maxH,
            nearDistance = near,
            farDistance = far
        });
    }

    public static void Unregister(Transform t)
    {
        if (_instance == null) return;
        for (int i = _instance._labels.Count - 1; i >= 0; i--)
            if (_instance._labels[i].labelTransform == t)
            { _instance._labels.RemoveAt(i); break; }
    }

    void Start() => _camTransform = Camera.main?.transform;

    void LateUpdate()
    {
        // ✅ NẾU ĐANG DẪN ĐƯỜNG, ĐÓNG BĂNG LUÔN, KHÔNG CẬP NHẬT KHOẢNG CÁCH NỮA
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

            // Billboard
            float bx = camPos.x - pos.x;
            float bz = camPos.z - pos.z;
            if (bx * bx + bz * bz > 0.01f)
                e.labelTransform.rotation = Quaternion.LookRotation(new Vector3(-bx, 0, -bz));

            // Height
            float ratio = Mathf.Clamp01((dist - e.nearDistance) / (e.farDistance - e.nearDistance));
            pos.y = Mathf.Lerp(e.minHeight, e.maxHeight, ratio);
            e.labelTransform.position = pos;
        }
    }

    // ✅ HÀM MỚI TỐI ƯU HƠN ĐỂ DỌN DẸP NHÃN XÁM
    // ✅ HÀM MỚI TỐI ƯU HƠN VÀ ĐÃ FIX LỖI CRASH "COLLECTION MODIFIED"
    // ✅ HÀM TÀNG HÌNH: Chỉ tắt phần nhìn (Canvas/Renderer), giữ nguyên logic chạy ngầm!
    public void ToggleGrayLabels(bool isNavigating)
    {
        isPausedByNav = isNavigating; // Khóa Update tính toán khoảng cách cho đỡ tốn pin

        if (_labels == null) return;

        foreach (var entry in _labels)
        {
            if (entry.labelTransform != null)
            {
                // 1. Nếu nó là UI (Canvas) -> Tắt màn hình vẽ của nó
                Canvas canvas = entry.labelTransform.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = !isNavigating; // Tắt component Canvas, KHÔNG tắt GameObject
                }

                // 2. Nếu nó là 3D (MeshRenderer) -> Tắt phần vẽ 3D
                Renderer[] renderers = entry.labelTransform.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.enabled = !isNavigating;
                }
            }
        }
    }
}