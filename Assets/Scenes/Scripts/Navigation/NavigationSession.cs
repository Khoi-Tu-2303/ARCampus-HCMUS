// Navigation/NavigationSession.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NavigationSession : MonoBehaviour
{
    public static NavigationSession Instance;
    void Awake() => Instance = this;

    [Header("Sub-components (gán trong Inspector)")]
    public ARNavigationView arView;
    public NavigationHUD hud;
    public Button btnCancel;
    public GameObject navigationOverlay;

    [Header("Settings")]
    public float arrivalThreshold = NavigationConstants.ArrivalThresholdMeters;

    private List<GraphNode> currentPath;
    private int waypointIndex = 0;
    private float bearing;
    private float distanceToTarget; // Khoảng cách tới điểm quẹo tiếp theo (cho AR)

    // TỐI ƯU TOÁN HỌC: Cache trước tổng độ dài đường đi
    private float[] _remainingPathDistances;
    private string _destinationName;

    private float _navUpdateTimer;
    private const float NAV_UPDATE_RATE = 0.5f; // Quét 2 lần/giây
    private float _lastDisplayedTotalDist = -1f;

    void OnEnable()
    {
        if (btnCancel != null) btnCancel.onClick.AddListener(CancelNavigation);
        hud?.ClearUI();
    }

    void OnDisable()
    {
        if (btnCancel != null) btnCancel.onClick.RemoveListener(CancelNavigation);
    }

    public void StartNavigation(string destinationNodeId)
    {
        // ✅ GỌI HÀM ToggleGrayLabels (True = Đang dẫn đường, tắt hết nhãn xám đi)
        if (ARLabelManager.Instance != null)
            ARLabelManager.Instance.ToggleGrayLabels(true);

        if (!GraphService.Instance.IsLoaded) return;

        string startNode = FindNearestNode();
        if (startNode == null) { hud?.ClearUI(); return; }

        currentPath = PathFinder.Instance.FindPath(startNode, destinationNodeId);
        if (currentPath == null || currentPath.Count == 0) { hud?.ClearUI(); return; }

        waypointIndex = 0;
        _navUpdateTimer = NAV_UPDATE_RATE;
        _lastDisplayedTotalDist = -1f;

        // Lấy tên đẹp của đích đến để in lên UI
        _destinationName = GraphService.Instance.Nodes.TryGetValue(destinationNodeId, out var dn)
            ? (!string.IsNullOrEmpty(dn.name) ? dn.name : dn.id)
            : destinationNodeId;

        // 🚀 TỐI ƯU: Tính toán trước khoảng cách của toàn bộ các đoạn đường
        _remainingPathDistances = new float[currentPath.Count];
        float total = 0;
        for (int i = currentPath.Count - 1; i > 0; i--)
        {
            total += GeoMath.Haversine((float)currentPath[i].lat, (float)currentPath[i].lng, (float)currentPath[i - 1].lat, (float)currentPath[i - 1].lng);
            _remainingPathDistances[i - 1] = total;
        }
        _remainingPathDistances[currentPath.Count - 1] = 0;

        arView?.InitAnchor(GPSService.Instance.Latitude, GPSService.Instance.Longitude);
        arView?.SpawnArrow();
        arView?.SpawnNodeLabel();
        arView?.DrawARPath(currentPath);

        MapController.Instance?.DrawRouteOnMap(currentPath);

        if (navigationOverlay != null) navigationOverlay.SetActive(true);
        hud?.UpdateTopCard(_destinationName, total); // In ngay lúc vừa bấm
    }

    void Update()
    {
        if (currentPath == null || waypointIndex >= currentPath.Count) return;

        _navUpdateTimer += Time.deltaTime;
        if (_navUpdateTimer < NAV_UPDATE_RATE) return;
        _navUpdateTimer = 0f;

        if (!GPSService.Instance.IsReady || Camera.main == null) return;

        var target = currentPath[waypointIndex];
        float lat = (float)GPSService.Instance.Latitude;
        float lng = (float)GPSService.Instance.Longitude;

        distanceToTarget = GeoMath.Haversine(lat, lng, target.lat, target.lng);

        if (distanceToTarget < arrivalThreshold)
        {
            waypointIndex++;
            if (waypointIndex >= currentPath.Count)
            {
                hud?.UpdateTopCard("Đã đến nơi!", 0);

                // 1. Dọn dẹp mũi tên AR và đường kẻ dưới đất cho sạch màn hình
                arView?.ClearAll();
                MapController.Instance?.ClearRoute();

                // 2. Bung bảng chúc mừng rực rỡ lên
                if (ArrivalModalController.Instance != null)
                {
                    // Lấy ra ID của trạm cuối cùng (chính là điểm đến)
                    string finalNodeId = currentPath[currentPath.Count - 1].id;

                    // Truyền thêm finalNodeId vào để nó tự gọt lấy chữ cái tòa nhà
                    ArrivalModalController.Instance.ShowModal(_destinationName, finalNodeId);
                }

                // ✅ GỌI HÀM ToggleGrayLabels (False = Hết dẫn đường, bật lại nhãn xám)
                if (ARLabelManager.Instance != null)
                    ARLabelManager.Instance.ToggleGrayLabels(false);

                // 3. Xóa đường đi để ngắt vòng lặp Update
                currentPath = null;
                return;
            }
            target = currentPath[waypointIndex];
            distanceToTarget = GeoMath.Haversine(lat, lng, target.lat, target.lng);
        }

        bearing = GeoMath.CalculateBearing(lat, lng, (float)target.lat, (float)target.lng);
        hud?.SetArrowAngle(bearing - Input.compass.trueHeading);

        arView?.UpdateARArrow(bearing);
        arView?.UpdateARPathLine(waypointIndex);
        arView?.UpdateNextNodeLabel(target, distanceToTarget); // Truyền điểm trung gian (ví dụ 10m)

        // TÍNH TỔNG KHOẢNG CÁCH (Khoảng cách tới điểm quẹo + Phần đường còn lại)
        float totalDist = distanceToTarget + _remainingPathDistances[waypointIndex];
        float roundedTotalDist = Mathf.Round(totalDist);

        if (roundedTotalDist != _lastDisplayedTotalDist)
        {
            _lastDisplayedTotalDist = roundedTotalDist;
            hud?.UpdateTopCard(_destinationName, roundedTotalDist); // Bắn lên UI (ví dụ 45m)
        }
    }

    public void CancelNavigation()
    {
        currentPath = null;
        waypointIndex = 0;
        hud?.ClearUI();
        arView?.ClearAll();
        MapController.Instance?.ClearRoute();
        CampusUIManager.Instance.StopNavigation();

        // ✅ GỌI HÀM ToggleGrayLabels (False = Hủy dẫn đường, bật lại nhãn xám)
        if (ARLabelManager.Instance != null)
            ARLabelManager.Instance.ToggleGrayLabels(false);
    }

    string FindNearestNode()
    {
        if (!GPSService.Instance.IsReady) return null;
        float lat = (float)GPSService.Instance.Latitude;
        float lng = (float)GPSService.Instance.Longitude;
        string nearest = null;
        float minDist = float.MaxValue;
        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            float d = GeoMath.Haversine(lat, lng, node.lat, node.lng);
            if (d < minDist) { minDist = d; nearest = node.id; }
        }
        return nearest;
    }
}