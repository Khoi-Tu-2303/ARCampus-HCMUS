// Navigation/NavigationSession.cs — PATCHED
// FIXES:
// [HIGH]     Checkpoint detection: added 3-sample hysteresis to eliminate GPS jitter false triggers
// [HIGH]     Switched to GeoMath.HaversineDouble() — eliminates ~1-2m float precision error
// [HIGH]     FindNearestNode() now has 200m distance guard — prevents wrong snap outside campus
// [HIGH]     CancelNavigation() null-guarded CampusUIManager.Instance?.StopNavigation()
// [MEDIUM]   Intermediate waypoint threshold raised to 15m for GPS inaccuracy tolerance
// [MEDIUM]   AdvanceWaypoint() skips already-passed nearby waypoints (fast walk edge case)

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NavigationSession : MonoBehaviour
{
    public static NavigationSession Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Sub-components (assign in Inspector)")]
    public ARNavigationView arView;
    public NavigationHUD hud;
    public Button btnCancel;
    public GameObject navigationOverlay;

    [Header("Settings")]
    public float arrivalThreshold = NavigationConstants.ArrivalThresholdMeters;

    private List<GraphNode> currentPath;
    private int waypointIndex = 0;
    private float bearing;
    private float distanceToTarget;

    // Pre-computed remaining path distances (avoid recalculating every tick)
    private float[] _remainingPathDistances;
    private string _destinationName;

    private float _navUpdateTimer;
    private const float NAV_UPDATE_RATE = 0.5f; // 2 Hz

    private float _lastDisplayedTotalDist = -1f;

    // ── HYSTERESIS: checkpoint triggers only after N consecutive in-range samples ──
    private int _arrivalConfirmCount = 0;
    private const int ARRIVAL_CONFIRM_REQUIRED = 3; // require 3 consecutive readings

    // ── MAX DISTANCE FOR NODE SNAP ──
    private const float MAX_SNAP_DISTANCE = 200f; // metres

    void OnEnable()
    {
        if (btnCancel != null) btnCancel.onClick.AddListener(CancelNavigation);
        hud?.ClearUI();
    }

    void OnDisable()
    {
        if (btnCancel != null) btnCancel.onClick.RemoveListener(CancelNavigation);
    }

    // ──────────────────────────────────────────────────────────
    // START NAVIGATION
    // ──────────────────────────────────────────────────────────

    public void StartNavigation(string destinationNodeId)
    {
        ARLabelManager.Instance?.ToggleGrayLabels(true);

        if (!GraphService.Instance.IsLoaded) return;

        string startNode = FindNearestNode();
        if (startNode == null)
        {
            Debug.LogWarning("⚠️ [Nav] Start node not found (outside campus?)");
            hud?.ClearUI();
            return;
        }

        currentPath = PathFinder.Instance.FindPath(startNode, destinationNodeId);
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning($"⚠️ [Nav] No path found: {startNode} → {destinationNodeId}");
            hud?.ClearUI();
            return;
        }

        waypointIndex = 0;
        _navUpdateTimer = NAV_UPDATE_RATE; // fire immediately on first Update
        _lastDisplayedTotalDist = -1f;
        _arrivalConfirmCount = 0;

        _destinationName = GraphService.Instance.Nodes.TryGetValue(destinationNodeId, out var dn)
            ? (!string.IsNullOrEmpty(dn.name) ? dn.name : dn.id)
            : destinationNodeId;

        // Pre-compute remaining distances per waypoint
        _remainingPathDistances = new float[currentPath.Count];
        float total = 0f;
        for (int i = currentPath.Count - 1; i > 0; i--)
        {
            total += GeoMath.HaversineDouble(
                currentPath[i].lat, currentPath[i].lng,
                currentPath[i - 1].lat, currentPath[i - 1].lng);
            _remainingPathDistances[i - 1] = total;
        }
        _remainingPathDistances[currentPath.Count - 1] = 0f;

        arView?.InitAnchor(GPSService.Instance.Latitude, GPSService.Instance.Longitude);
        arView?.SpawnArrow();
        arView?.SpawnNodeLabel();
        arView?.DrawARPath(currentPath);

        MapController.Instance?.DrawRouteOnMap(currentPath);

        if (navigationOverlay != null) navigationOverlay.SetActive(true);
        hud?.UpdateTopCard(_destinationName, total);
    }

    // ──────────────────────────────────────────────────────────
    // UPDATE LOOP — 2 Hz
    // ──────────────────────────────────────────────────────────

    void Update()
    {
        if (currentPath == null || waypointIndex >= currentPath.Count) return;

        _navUpdateTimer += Time.deltaTime;
        if (_navUpdateTimer < NAV_UPDATE_RATE) return;
        _navUpdateTimer = 0f;

        if (!GPSService.Instance.IsReady || Camera.main == null) return;

        var target = currentPath[waypointIndex];
        double lat = GPSService.Instance.Latitude;
        double lng = GPSService.Instance.Longitude;

        // Use double-precision Haversine to avoid ~1-2m float error
        distanceToTarget = GeoMath.HaversineDouble(lat, lng, target.lat, target.lng);

        bool isFinalNode = (waypointIndex == currentPath.Count - 1);
        float activeThreshold = isFinalNode ? arrivalThreshold : 15f;

        if (distanceToTarget < activeThreshold)
        {
            _arrivalConfirmCount++;

            // HYSTERESIS: only advance after N consecutive in-range readings
            if (_arrivalConfirmCount >= ARRIVAL_CONFIRM_REQUIRED)
            {
                _arrivalConfirmCount = 0;
                AdvanceWaypoint();
                return; // recalculate next frame
            }
        }
        else
        {
            // Out of range — reset hysteresis counter
            _arrivalConfirmCount = 0;
        }

        // Update arrow bearing
        bearing = GeoMath.CalculateBearing(
            (float)lat, (float)lng,
            (float)target.lat, (float)target.lng);

        hud?.SetArrowAngle(bearing - Input.compass.trueHeading);
        arView?.UpdateARArrow(bearing);
        arView?.UpdateARPathLine(waypointIndex);
        arView?.UpdateNextNodeLabel(target, distanceToTarget);

        // Update total distance display only when it rounds to a new integer
        float totalDist = distanceToTarget + _remainingPathDistances[waypointIndex];
        float roundedTotalDist = Mathf.Round(totalDist);

        if (roundedTotalDist != _lastDisplayedTotalDist)
        {
            _lastDisplayedTotalDist = roundedTotalDist;
            hud?.UpdateTopCard(_destinationName, roundedTotalDist);
        }
    }

    // ──────────────────────────────────────────────────────────
    // WAYPOINT ADVANCE
    // ──────────────────────────────────────────────────────────

    void AdvanceWaypoint()
    {
        waypointIndex++;

        if (waypointIndex >= currentPath.Count)
        {
            HandleArrival();
            return;
        }

        // Skip any immediately adjacent waypoints the user has already passed
        // (handles fast walking through densely placed nodes)
        const int MAX_SKIP = 2;
        for (int skip = 0; skip < MAX_SKIP && waypointIndex < currentPath.Count - 1; skip++)
        {
            float nextDist = GeoMath.HaversineDouble(
                GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                currentPath[waypointIndex].lat, currentPath[waypointIndex].lng);

            if (nextDist < 10f) // already within 10m of next node
                waypointIndex++;
            else
                break;
        }

    target_changed:; // update bearing immediately
        if (waypointIndex < currentPath.Count)
        {
            bearing = GeoMath.CalculateBearing(
                (float)GPSService.Instance.Latitude, (float)GPSService.Instance.Longitude,
                (float)currentPath[waypointIndex].lat, (float)currentPath[waypointIndex].lng);
            hud?.SetArrowAngle(bearing - Input.compass.trueHeading);
        }
    }

    void HandleArrival()
    {
        hud?.UpdateTopCard("Đã đến nơi!", 0);
        arView?.ClearAll();
        MapController.Instance?.ClearRoute();

        if (ArrivalModalController.Instance != null)
        {
            string finalNodeId = currentPath[currentPath.Count - 1].id;
            ArrivalModalController.Instance.ShowModal(_destinationName, finalNodeId);
        }

        ARLabelManager.Instance?.ToggleGrayLabels(false);

        currentPath = null; // stops Update loop
    }

    // ──────────────────────────────────────────────────────────
    // CANCEL NAVIGATION
    // ──────────────────────────────────────────────────────────

    public void CancelNavigation()
    {
        currentPath = null;
        waypointIndex = 0;
        _arrivalConfirmCount = 0;

        hud?.ClearUI();
        arView?.ClearAll();
        MapController.Instance?.ClearRoute();

        // FIX: null-guard — crash if CampusUIManager destroyed on scene reload
        CampusUIManager.Instance?.StopNavigation();

        ARLabelManager.Instance?.ToggleGrayLabels(false);
    }

    // ──────────────────────────────────────────────────────────
    // FIND NEAREST NODE — with distance guard
    // ──────────────────────────────────────────────────────────

    string FindNearestNode()
    {
        if (!GPSService.Instance.IsReady) return null;

        double lat = GPSService.Instance.Latitude;
        double lng = GPSService.Instance.Longitude;
        string nearest = null;
        float minDist = float.MaxValue;

        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            float d = GeoMath.HaversineDouble(lat, lng, node.lat, node.lng);
            if (d < minDist)
            {
                minDist = d;
                nearest = node.id;
            }
        }

        // Guard: don't snap to a node that is far outside campus
        if (minDist > MAX_SNAP_DISTANCE)
        {
            Debug.LogWarning($"⚠️ [Nav] Nearest node is {minDist:F0}m away. User may be outside campus.");
            return null;
        }

        return nearest;
    }
}
