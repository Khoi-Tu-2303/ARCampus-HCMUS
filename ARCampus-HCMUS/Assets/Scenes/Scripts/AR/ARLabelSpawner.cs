using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ARLabelSpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject labelPrefab;
    public float spawnRadius = 100f;
    public float updateInterval = 5f;
    public int poolSize = 20;

    private Queue<GameObject> _pool = new Queue<GameObject>(20);
    private Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>(20);
    private Camera _arCamera;
    private float _visibilityTimer;
    private const float VISIBILITY_UPDATE_INTERVAL = 0.1f;

    private readonly List<string> _outOfRangeBuffer = new List<string>(8);
    void Start()
    {
        _arCamera = Camera.main;

        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(labelPrefab);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }

        StartCoroutine(SpawnLoop());
    }

    // ──────────────────────────────────────────────────────────
    // POOL HELPERS
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a pooled (or new) object WITHOUT activating it.
    /// Caller must call SetupBeforeEnable() then SetActive(true) manually.
    /// </summary>
    GameObject GetFromPoolInactive()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            // Do NOT call SetActive here — caller does it after setup
            return obj;
        }
        // Pool exhausted — instantiate a new one (inactive by default from prefab)
        var newObj = Instantiate(labelPrefab);
        newObj.SetActive(false);
        return newObj;
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    // ──────────────────────────────────────────────────────────
    // VISIBILITY UPDATE — runs at 10Hz, culls out-of-range labels
    // ──────────────────────────────────────────────────────────

    void Update()
    {
        if (_arCamera == null) return;

        _visibilityTimer += Time.deltaTime;
        if (_visibilityTimer < VISIBILITY_UPDATE_INTERVAL) return;
        _visibilityTimer = 0f;

        Vector3 camPos = _arCamera.transform.position;
        float radiusSq = spawnRadius * spawnRadius;

        _outOfRangeBuffer.Clear(); // Đổ rác cũ ra để xài lại túi

        foreach (var kvp in _active)
        {
            if (kvp.Value == null) { _outOfRangeBuffer.Add(kvp.Key); continue; }

            Vector3 diff = kvp.Value.transform.position - camPos;
            diff.y = 0f;

            if (diff.sqrMagnitude >= radiusSq)
            {
                ReturnToPool(kvp.Value);
                _outOfRangeBuffer.Add(kvp.Key); // Xài buffer
            }
        }

        foreach (var key in _outOfRangeBuffer) // Xài buffer
            _active.Remove(key);
    }

    // ──────────────────────────────────────────────────────────
    // SPAWN LOOP
    // ──────────────────────────────────────────────────────────

    IEnumerator SpawnLoop()
    {
        // Wait indefinitely until both GPS and Firebase are ready
        while (FirebaseService.Instance == null || !FirebaseService.Instance.IsReady ||
               GPSService.Instance == null || !GPSService.Instance.IsReady)
        {
            yield return new WaitForSeconds(1f);
        }

        while (true)
        {
            if (_arCamera == null) _arCamera = Camera.main;
            if (_arCamera != null && GPSService.Instance.IsReady)
                SpawnLabels();

            yield return new WaitForSeconds(updateInterval);
        }
    }

    // ──────────────────────────────────────────────────────────
    // SPAWN LABELS
    // FIX: SetupBeforeEnable() called BEFORE SetActive(true)
    // ──────────────────────────────────────────────────────────

    void SpawnLabels()
    {
        var nearby = LocationService.Instance.GetNearbyLocations(spawnRadius);

        foreach (var loc in nearby)
        {
            if (string.IsNullOrEmpty(loc.display_name)) continue;
            if (loc.category == "junction") continue;
            if (_active.ContainsKey(loc.location_id)) continue;

            Vector3 worldPos = GeoMath.GpsToARWorldPosition(
                loc.lat, loc.lng,
                GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                _arCamera.transform);

            // STEP 1: Get inactive object from pool (NOT activated yet)
            GameObject label = GetFromPoolInactive();

            // STEP 2: Configure canvas camera
            Canvas labelCanvas = label.GetComponent<Canvas>();
            if (labelCanvas != null) labelCanvas.worldCamera = _arCamera;

            // STEP 3: Set display name text
            var tmp = label.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = loc.display_name;

            // STEP 4: Inject GPS data BEFORE activation — prevents OnEnable race condition
            ARLabelBehavior behavior = label.GetComponent<ARLabelBehavior>();
            if (behavior != null)
                behavior.SetupBeforeEnable(loc.display_name, loc.lat, loc.lng);

            // STEP 5: Place in world
            label.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            // STEP 6: NOW activate — OnEnable will find _lat/_lng already set
            label.SetActive(true);

            _active[loc.location_id] = label;
        }
    }
}