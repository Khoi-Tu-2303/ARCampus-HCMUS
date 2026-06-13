








using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;

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

    
    private float[] _remainingPathDistances;
    private string _destinationName;

    private float _navUpdateTimer;
    private const float NAV_UPDATE_RATE = 0.5f; 

    private float _lastDisplayedTotalDist = -1f;

    
    private int _arrivalConfirmCount = 0;
    private const int ARRIVAL_CONFIRM_REQUIRED = 3; 
    
    private int _rerouteConfirmCount = 0;
    private const int REROUTE_CONFIRM_REQUIRED = 3;
    
    private const float MAX_SNAP_DISTANCE = 200f; 

    void OnEnable()
    {
        if (btnCancel != null) btnCancel.onClick.AddListener(CancelNavigation);
        hud?.ClearUI();
        ARSession.stateChanged += OnARTrackingChanged;
    }

    void OnDisable()
    {
        if (btnCancel != null) btnCancel.onClick.RemoveListener(CancelNavigation);
        ARSession.stateChanged -= OnARTrackingChanged;
    }

    
    
    

    public void StartNavigation(string destinationNodeId)
    {
        if (currentPath != null) CancelNavigation();
        ARLabelManager.Instance?.ToggleGrayLabels(true);

        if (!GraphService.Instance.IsLoaded) return;

        string startNode = FindBestStartNode(destinationNodeId);
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
        _navUpdateTimer = NAV_UPDATE_RATE; 
        _lastDisplayedTotalDist = -1f;
        _arrivalConfirmCount = 0;

        _destinationName = GraphService.Instance.Nodes.TryGetValue(destinationNodeId, out var dn)
            ? (!string.IsNullOrEmpty(dn.name) ? dn.name : dn.id)
            : destinationNodeId;

        
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

        
        distanceToTarget = GeoMath.HaversineDouble(lat, lng, target.lat, target.lng);
        
        if (waypointIndex > 0)
        {
            double prevLat = currentPath[waypointIndex - 1].lat;
            double prevLng = currentPath[waypointIndex - 1].lng;
            float distToPrev = GeoMath.HaversineDouble(lat, lng, prevLat, prevLng);

            
            if (distanceToTarget > 80f && distToPrev > 80f)
            {
                _rerouteConfirmCount++;
                if (_rerouteConfirmCount >= REROUTE_CONFIRM_REQUIRED)
                {
                    _rerouteConfirmCount = 0;
                    Debug.LogWarning("⚠️ [Nav] Xác nhận đi lạc sau 3 lần đo! Đang tính toán lộ trình mới...");
                    string finalDestId = currentPath[currentPath.Count - 1].id;

                    CampusUIManager.Instance?.StartNavigation();
                    StartNavigation(finalDestId);
                }
                return; 
            }
            else
            {
                _rerouteConfirmCount = 0; 
            }
        }
        bool isFinalNode = (waypointIndex == currentPath.Count - 1);
        float activeThreshold = isFinalNode ? arrivalThreshold : 20f;

        if (distanceToTarget < activeThreshold)
        {
            _arrivalConfirmCount++;

            
            if (_arrivalConfirmCount >= ARRIVAL_CONFIRM_REQUIRED)
            {
                _arrivalConfirmCount = 0;
                AdvanceWaypoint();
                return; 
            }
        }
        else
        {
            
            _arrivalConfirmCount = 0;
        }

        
        bearing = GeoMath.CalculateBearing(lat, lng, target.lat, target.lng);

        float smoothedHeading = Camera.main.transform.eulerAngles.y - GPSService.Instance.ARNorthOffset;
        hud?.SetArrowAngle(bearing - smoothedHeading);
        arView?.UpdateARArrow(bearing);
        arView?.UpdateARPathLine(waypointIndex);
        arView?.UpdateNextNodeLabel(target, distanceToTarget);

        
        float totalDist = distanceToTarget + _remainingPathDistances[waypointIndex];
        float roundedTotalDist = Mathf.Round(totalDist);

        if (roundedTotalDist != _lastDisplayedTotalDist)
        {
            _lastDisplayedTotalDist = roundedTotalDist;
            hud?.UpdateTopCard(_destinationName, roundedTotalDist);
        }
    }

    
    
    

    void AdvanceWaypoint()
    {
        waypointIndex++;

        if (waypointIndex >= currentPath.Count)
        {
            HandleArrival();
            return;
        }

        
        
        const int MAX_SKIP = 2;
        for (int skip = 0; skip < MAX_SKIP && waypointIndex < currentPath.Count - 1; skip++)
        {
            float nextDist = GeoMath.HaversineDouble(
                GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                currentPath[waypointIndex].lat, currentPath[waypointIndex].lng);

            if (nextDist < 10f) 
                waypointIndex++;
            else
                break;
        }

        if (waypointIndex < currentPath.Count)
        {
            bearing = GeoMath.CalculateBearing(
                    GPSService.Instance.Latitude, GPSService.Instance.Longitude,
                    currentPath[waypointIndex].lat, currentPath[waypointIndex].lng);
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

        currentPath = null; 
    }

    
    
    

    public void CancelNavigation()
    {
        currentPath = null;
        waypointIndex = 0;
        _arrivalConfirmCount = 0;

        hud?.ClearUI();
        arView?.ClearAll();
        MapController.Instance?.ClearRoute();

        
        CampusUIManager.Instance?.StopNavigation();

        ARLabelManager.Instance?.ToggleGrayLabels(false);
    }

    
    
    

    string FindBestStartNode(string destId)
    {
        if (!GPSService.Instance.IsReady) return null;

        double uLat = GPSService.Instance.Latitude;
        double uLng = GPSService.Instance.Longitude;

        
        if (!GraphService.Instance.Nodes.TryGetValue(destId, out var destNode))
            return null;

        string bestNode = null;
        float bestScore = float.MaxValue;

        
        Vector2 vecToDest = new Vector2((float)(destNode.lng - uLng), (float)(destNode.lat - uLat)).normalized;
        float distUserToDest = GeoMath.HaversineDouble(uLat, uLng, destNode.lat, destNode.lng);

        foreach (var node in GraphService.Instance.Nodes.Values)
        {
            float distUserToNode = GeoMath.HaversineDouble(uLat, uLng, node.lat, node.lng);

            
            if (distUserToNode > MAX_SNAP_DISTANCE) continue;

            
            Vector2 vecToNode = new Vector2((float)(node.lng - uLng), (float)(node.lat - uLat));
            float dotProduct = 0f;
            if (vecToNode.sqrMagnitude > 0)
            {
                dotProduct = Vector2.Dot(vecToDest, vecToNode.normalized);
            }

            
            
            float anglePenalty = 1.5f - 0.5f * dotProduct;
            float score = distUserToNode * anglePenalty;

            
            
            float distNodeToDest = GeoMath.HaversineDouble(node.lat, node.lng, destNode.lat, destNode.lng);
            if (distNodeToDest > distUserToDest)
            {
                score += (distNodeToDest - distUserToDest) * 2f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestNode = node.id;
            }
        }

        if (bestScore == float.MaxValue)
        {
            Debug.LogWarning("⚠️ [Nav] User ở quá xa (ngoài MAX_SNAP_DISTANCE), không tìm thấy Node khởi đầu.");
            return null;
        }

        return bestNode;
    }
    
    private void OnARTrackingChanged(ARSessionStateChangedEventArgs args)
    {
        
        if (args.state == ARSessionState.SessionTracking && currentPath != null)
        {
            if (GPSService.Instance != null && GPSService.Instance.IsReady)
            {
                Debug.Log("🔄 [AR] Tái định vị lại Không gian AR...");
                
                arView?.InitAnchor(GPSService.Instance.Latitude, GPSService.Instance.Longitude);
                
                arView?.DrawARPath(currentPath);
            }
        }
    }
}
