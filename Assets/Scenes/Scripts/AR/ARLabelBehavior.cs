// AR/ARLabelBehavior.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class ARLabelBehavior : MonoBehaviour
{
    [Header("Cấu hình Nhãn")]
    public float maxHeight = 15f;
    public float minHeight = 2f;
    public float farDistance = 40f;
    public float nearDistance = 5f;
    public string buildingName;

    [Header("UI Mới (Thiết kế Google Maps)")]
    public TextMeshProUGUI txtDistance; // Kéo thả cái Text hiển thị "45m" vào đây

    private double _lat;
    private double _lng;
    private Coroutine _distanceRoutine;

    // Sếp Spawner sẽ gọi hàm này lúc đẻ nhãn ra để bơm Data vào
    public void Setup(string name, double lat, double lng)
    {
        buildingName = name;
        _lat = lat;
        _lng = lng;
        UpdateDistance(); // Tính ngay lập tức phát đầu tiên
    }

    void OnEnable()
    {
        ARLabelManager.Register(transform, minHeight, maxHeight, nearDistance, farDistance);
        _distanceRoutine = StartCoroutine(DistanceUpdateLoop());
    }

    void OnDisable()
    {
        ARLabelManager.Unregister(transform);
        if (_distanceRoutine != null) StopCoroutine(_distanceRoutine);
    }

    IEnumerator DistanceUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Cứ 1 giây tính khoảng cách 1 lần cho nhẹ máy
            UpdateDistance();
        }
    }

    void UpdateDistance()
    {
        if (GPSService.Instance == null || !GPSService.Instance.IsReady || txtDistance == null) return;

        float dist = GeoMath.Haversine(
            GPSService.Instance.Latitude, GPSService.Instance.Longitude,
            _lat, _lng
        );

        txtDistance.text = $"{dist:F0}m away";
    }
}