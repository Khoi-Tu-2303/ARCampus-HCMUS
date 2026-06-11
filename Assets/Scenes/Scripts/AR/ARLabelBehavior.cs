// AR/ARLabelBehavior.cs — PATCHED (BẢN KHÓA TỌA ĐỘ)
// FIXES:
// [CRITICAL] Race condition: Setup() called AFTER SetActive(true) → OnEnable fired with lat=0, lng=0
//            Solution: SetupBeforeEnable() injects data BEFORE SetActive, OnEnable guards on _lat!=0
// [HIGH]     DistanceUpdateLoop now guards against GPS not ready to avoid bad positions
// [🔥 NEW FIX] Removed GPS drift correction in UpdateDistance. 
//            Label X/Z positions are now baked upon spawn to let ARFoundation handle spatial tracking, preventing jitter.

using UnityEngine;
using TMPro;
using System.Collections;

public class ARLabelBehavior : MonoBehaviour
{
    [Header("Label Config")]
    public float maxHeight = 15f;
    public float minHeight = 2f;
    public float farDistance = 200f;
    public float nearDistance = 10f;
    public string buildingName;

    [Header("UI")]
    public TextMeshProUGUI txtDistance;

    private double _lat;
    private double _lng;
    private bool _dataReady = false;
    private Coroutine _distanceRoutine;

    // ──────────────────────────────────────────────────────────
    // SETUP — MUST be called BEFORE SetActive(true)
    // ──────────────────────────────────────────────────────────

    public void SetupBeforeEnable(string name, double lat, double lng)
    {
        buildingName = name;
        _lat = lat;
        _lng = lng;
        _dataReady = true;
    }

    public void Setup(string name, double lat, double lng)
    {
        buildingName = name;
        _lat = lat;
        _lng = lng;
        _dataReady = true;
        UpdateDistance();
    }

    // ──────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────

    void OnEnable()
    {
        ARLabelManager.Register(transform, minHeight, maxHeight, nearDistance, farDistance);

        if (_dataReady && (_lat != 0.0 || _lng != 0.0))
        {
            if (_distanceRoutine != null) StopCoroutine(_distanceRoutine);
            _distanceRoutine = StartCoroutine(DistanceUpdateLoop());
        }
    }

    void OnDisable()
    {
        ARLabelManager.Unregister(transform);
        if (_distanceRoutine != null)
        {
            StopCoroutine(_distanceRoutine);
            _distanceRoutine = null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // DISTANCE UPDATE LOOP
    // ──────────────────────────────────────────────────────────

    IEnumerator DistanceUpdateLoop()
    {
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            UpdateDistance();
            yield return new WaitForSeconds(1f);
        }
    }

    void UpdateDistance()
    {
        if (!_dataReady) return;
        if (GPSService.Instance == null || !GPSService.Instance.IsReady) return;
        if (ARLabelManager.Instance != null && ARLabelManager.Instance.isPausedByNav) return;

        double userLat = GPSService.Instance.Latitude;
        double userLng = GPSService.Instance.Longitude;

        // 1. CHỈ CẬP NHẬT CHỮ TEXT KHOẢNG CÁCH (10m, 15m...)
        if (txtDistance != null)
        {
            float dist = GeoMath.HaversineDouble(userLat, userLng, _lat, _lng);
            txtDistance.text = $"{dist:F0}m";
        }

        // 2. ❌ ĐÃ XÓA HOÀN TOÀN ĐOẠN CẬP NHẬT TỌA ĐỘ THEO GPS (DRIFT CORRECTION)
        // Hành động này giúp nhãn bị "đổ bê tông" xuống đường. 
        // Khi sếp di chuyển, ARFoundation (SLAM) sẽ tự động quản lý vị trí của nó, không bị sóng GPS làm giật/nhảy nữa!
    }
}