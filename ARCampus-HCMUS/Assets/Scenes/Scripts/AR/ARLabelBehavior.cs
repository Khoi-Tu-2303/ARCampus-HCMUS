// AR/ARLabelBehavior.cs — PATCHED
// FIXES:
// [CRITICAL] Race condition: Setup() called AFTER SetActive(true) → OnEnable fired with lat=0, lng=0
//            Solution: SetupBeforeEnable() injects data BEFORE SetActive, OnEnable guards on _lat!=0
// [HIGH]     DistanceUpdateLoop now guards against GPS not ready to avoid bad positions

using UnityEngine;
using TMPro;
using System.Collections;

public class ARLabelBehavior : MonoBehaviour
{
    [Header("Label Config")]
    public float maxHeight = 15f;
    public float minHeight = 2f;
    public float farDistance = 100f;
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
    // ARLabelSpawner.SpawnLabels() calls this via GetFromPoolInactive()
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inject position data before the GameObject is activated.
    /// This prevents OnEnable from running with lat=0, lng=0.
    /// </summary>
    public void SetupBeforeEnable(string name, double lat, double lng)
    {
        buildingName = name;
        _lat = lat;
        _lng = lng;
        _dataReady = true;
        // Do NOT call UpdateDistance here — camera may not be ready yet.
    }

    /// <summary>
    /// Legacy path — only use if caller activates the object before calling Setup.
    /// Prefer SetupBeforeEnable() instead.
    /// </summary>
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

        // Guard: only start coroutine when data has been injected
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
        // Small initial delay — let the camera settle after activation
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

        // 1. Distance text
        if (txtDistance != null)
        {
            float dist = GeoMath.HaversineDouble(userLat, userLng, _lat, _lng);
            txtDistance.text = $"{dist:F0}m";
        }

        // 2. AR drift correction — recompute world position from latest GPS
        if (Camera.main != null)
        {
            Vector3 correctPos = GeoMath.GpsToARWorldPosition(
                _lat, _lng,
                userLat, userLng,
                Camera.main.transform);

            // Only correct X/Z — leave Y to ARLabelManager (height management)
            transform.position = new Vector3(correctPos.x, transform.position.y, correctPos.z);
        }
    }
}