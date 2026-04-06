using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ARLabelSpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject labelPrefab;
    public float spawnRadius = 200f;
    public float updateInterval = 5f;

    private Dictionary<string, GameObject> spawnedLabels = new();
    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (!FirebaseManager.Instance.IsReady || !GPSManager.Instance.IsReady)
        {
            Debug.Log("⏳ Waiting for Firebase + GPS...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("✅ Starting AR Label spawn loop");

        while (true)
        {
            SpawnLabels();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void SpawnLabels()
    {
        var nearby = GPSManager.Instance.GetNearbyLocations(spawnRadius);
        Debug.Log($"🔍 SpawnLabels called, nearby count: {nearby.Count}, prefab: {labelPrefab}");

        foreach (var loc in nearby)
        {
            if (spawnedLabels.ContainsKey(loc.location_id)) continue;

            Debug.Log($"🏷️ Attempting spawn: {loc.display_name}, prefab null? {labelPrefab == null}");

            Vector3 worldPos = GPSToARPosition(loc.lat, loc.lng);
            GameObject label = Instantiate(labelPrefab, worldPos, Quaternion.identity);

            var tmp = label.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = loc.display_name;

            label.AddComponent<BillboardLabel>();
            spawnedLabels[loc.location_id] = label;
            Debug.Log($"🏷️ Spawned label: {loc.display_name}");
        }
    }

    Vector3 GPSToARPosition(double lat, double lng)
    {
        double userLat = GPSManager.Instance.Latitude;
        double userLng = GPSManager.Instance.Longitude;

        float offsetX = (float)((lng - userLng) * 111320 * Math.Cos(userLat * Math.PI / 180));
        float offsetZ = (float)((lat - userLat) * 110540);

        return new Vector3(offsetX, 2f, offsetZ);
    }

    void Update()
    {
        foreach (var (id, label) in spawnedLabels)
        {
            if (label == null) continue;
            float dist = Vector3.Distance(arCamera.transform.position, label.transform.position);
            label.SetActive(dist < spawnRadius);
        }
    }
}

public class BillboardLabel : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 dirToCamera = Camera.main.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(-dirToCamera);
        }
    }
}