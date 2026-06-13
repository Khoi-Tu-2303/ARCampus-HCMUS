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

    void OnEnable() { ARSession.stateChanged += OnARStateChanged; }
    void OnDisable() { ARSession.stateChanged -= OnARStateChanged; }

    private void OnARStateChanged(ARSessionStateChangedEventArgs args)
    {
        if (args.state == ARSessionState.SessionTracking)
        {
            Debug.Log("🔄 [ARLabelSpawner] Relocalized! Dọn dẹp nhãn cũ...");
            foreach (var kvp in _active)
            {
                if (kvp.Value != null) ReturnToPool(kvp.Value);
            }
            _active.Clear();
        }
    }
    
    
    

    GameObject GetFromPoolInactive()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            
            return obj;
        }
        
        var newObj = Instantiate(labelPrefab);
        newObj.SetActive(false);
        return newObj;
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

        _outOfRangeBuffer.Clear(); 

        foreach (var kvp in _active)
        {
            if (kvp.Value == null) { _outOfRangeBuffer.Add(kvp.Key); continue; }

            Vector3 diff = kvp.Value.transform.position - camPos;
            diff.y = 0f;

            if (diff.sqrMagnitude >= radiusSq)
            {
                ReturnToPool(kvp.Value);
                _outOfRangeBuffer.Add(kvp.Key); 
            }
        }

        foreach (var key in _outOfRangeBuffer) 
            _active.Remove(key);
    }

    
    
    

    IEnumerator SpawnLoop()
    {
        
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

            
            GameObject label = GetFromPoolInactive();

            
            Canvas labelCanvas = label.GetComponent<Canvas>();
            if (labelCanvas != null) labelCanvas.worldCamera = _arCamera;

            
            var tmp = label.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = loc.display_name;

            
            ARLabelBehavior behavior = label.GetComponent<ARLabelBehavior>();
            if (behavior != null)
                behavior.SetupBeforeEnable(loc.display_name, loc.lat, loc.lng);

            
            label.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            
            label.SetActive(true);

            _active[loc.location_id] = label;
        }
    }
}
