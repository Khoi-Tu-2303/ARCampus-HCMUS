// AR/ARLabelSpawner.cs
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ✅ Pool + throttled visibility + Defensive Timeout
public class ARLabelSpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject labelPrefab;
    public float spawnRadius = 80f;
    public float updateInterval = 5f;
    public int poolSize = 20;

    // Pool
    private Queue<GameObject> _pool = new Queue<GameObject>(20);
    private Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>(20);
    private Camera _arCamera;
    private float _visibilityTimer;
    private const float VISIBILITY_UPDATE_INTERVAL = 0.1f;

    void Start()
    {
        _arCamera = Camera.main;
        // Pre-warm pool
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(labelPrefab);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
        StartCoroutine(SpawnLoop());
    }

    GameObject GetFromPool(Vector3 pos, Quaternion rot)
    {
        GameObject obj = _pool.Count > 0
            ? _pool.Dequeue()
            : Instantiate(labelPrefab); // fallback
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        return obj;
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    void Update()
    {
        if (_arCamera == null) return;
        _visibilityTimer += Time.deltaTime;
        if (_visibilityTimer < VISIBILITY_UPDATE_INTERVAL) return;
        _visibilityTimer = 0f;

        Vector3 camPos = _arCamera.transform.position;
        float radiusSq = spawnRadius * spawnRadius;

        // ✅ Dùng KeyValuePair iteration — không tạo enumerator mới
        foreach (var kvp in _active)
        {
            if (kvp.Value == null) continue;
            bool shouldBeVisible = (kvp.Value.transform.position - camPos).sqrMagnitude < radiusSq;
            // ✅ Chỉ gọi SetActive khi trạng thái thực sự đổi
            if (kvp.Value.activeSelf != shouldBeVisible)
                kvp.Value.SetActive(shouldBeVisible);
        }
    }

    IEnumerator SpawnLoop()
    {
        float timeout = 30f;
        while (timeout > 0 && (
            FirebaseService.Instance == null || !FirebaseService.Instance.IsReady ||
            GPSService.Instance == null || !GPSService.Instance.IsReady))
        {
            timeout -= 1f;
            yield return new WaitForSeconds(1f);
        }

        // Không chờ arCamera — lấy trong SpawnLabels() thay vì đây
        if (timeout <= 0)
        {
            Debug.LogError("❌ Timeout waiting for services");
            yield break;
        }

        while (true)
        {
            if (_arCamera == null) _arCamera = Camera.main;
            if (_arCamera != null) SpawnLabels();

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void SpawnLabels()
    {
        var nearby = LocationService.Instance.GetNearbyLocations(spawnRadius);

        foreach (var loc in nearby)
        {
            if (_active.ContainsKey(loc.location_id) || string.IsNullOrEmpty(loc.display_name)) continue;
            if (loc.category == "junction") continue;

            Vector3 worldPos = GeoMath.GpsToARWorldPosition(
                loc.lat, loc.lng,
                GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                _arCamera.transform.position);

            GameObject label = GetFromPool(worldPos, Quaternion.identity);

            Canvas labelCanvas = label.GetComponent<Canvas>();
            if (labelCanvas != null) labelCanvas.worldCamera = _arCamera;

            var tmp = label.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = loc.display_name;

            // ==========================================
            // ✅ ĐÃ SỬA CHỖ NÀY: Bơm Data tọa độ vào cho Nhãn
            // ==========================================
            ARLabelBehavior behavior = label.GetComponent<ARLabelBehavior>();
            if (behavior != null)
            {
                behavior.Setup(loc.display_name, loc.lat, loc.lng);
            }

            _active[loc.location_id] = label;
        }
    }
}