
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

        if (txtDistance != null)
        {
            float dist = GeoMath.HaversineDouble(userLat, userLng, _lat, _lng);
            txtDistance.text = $"{dist:F0}m";
        }

    }
}
