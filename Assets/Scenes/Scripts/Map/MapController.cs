
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class MapController : MonoBehaviour
{
    public static MapController Instance;

    [Header("Thành phần Bản đồ")]
    public ScrollRect mapScrollRect;
    public RectTransform mapContent;

    [Header("UI Elements (Tự động co giãn)")]
    public GameObject mapBottomNavCard;
    public RectTransform zoomButtonGroup;

    [Header("Chấm xanh (Vị trí user)")]
    public RectTransform blueDot;

    [Header("Route Renderer")]
    public RouteRenderer2D routeRenderer;

    [Header("Tọa độ Đời thực")]
    public double topLeftLat = CampusMapConstants.TopLeftLat;
    public double topLeftLng = CampusMapConstants.TopLeftLng;
    public double botRightLat = CampusMapConstants.BotRightLat;
    public double botRightLng = CampusMapConstants.BotRightLng;

    [Header("Cài đặt Zoom")]
    public float zoomSpeedMobile = 0.005f;
    public float zoomSpeedPC = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 10f;

    private float currentZoom = 1f;

    private double _lastLat;
    private double _lastLng;

    void Awake() => Instance = this;

    void Start()
    {
        if (mapContent != null) currentZoom = mapContent.localScale.x;

        
        
        StartCoroutine(WaitAndRecalculateZoom());

        if (mapScrollRect != null)
        {
            mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    void Update()
    {
        HandleZoom();

        if (GPSService.Instance != null && GPSService.Instance.IsReady)
        {
            double lat = GPSService.Instance.Latitude;
            double lng = GPSService.Instance.Longitude;

            if (lat != _lastLat || lng != _lastLng)
            {
                _lastLat = lat;
                _lastLng = lng;
                UpdateBlueDotPosition();
            }
        }
    }

    
    void HandleZoom()
    {
        
        if (Input.touchCount == 0)
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                
                float normalizedScroll = Input.mouseScrollDelta.y > 0 ? 1f : -1f;
                ZoomMap(normalizedScroll * zoomSpeedPC);
            }
        }
        
        else if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float diff = (t0.position - t1.position).magnitude - (t0Prev - t1Prev).magnitude;
            ZoomMap(diff * zoomSpeedMobile);
        }
    }

    void ZoomMap(float increment)
    {
        currentZoom = Mathf.Clamp(currentZoom + increment, minZoom, maxZoom);
        mapContent.localScale = new Vector3(currentZoom, currentZoom, 1f);
    }

    void UpdateBlueDotPosition()
    {
        if (blueDot == null || mapContent == null || !GPSService.Instance.IsReady) return;
        blueDot.anchoredPosition = GetLocalPositionFromGPS(GPSService.Instance.Latitude, GPSService.Instance.Longitude);
    }

    public Vector2 GetLocalPositionFromGPS(double targetLat, double targetLng)
    {
        float percentX = (float)((targetLng - topLeftLng) / (botRightLng - topLeftLng));
        float percentY = (float)((topLeftLat - targetLat) / (topLeftLat - botRightLat));
        return new Vector2(percentX * mapContent.rect.width, -(percentY * mapContent.rect.height));
    }

    public void DrawRouteOnMap(List<GraphNode> path) => routeRenderer?.DrawRoute(path);
    public void ClearRoute() => routeRenderer?.ClearRoute();
    public void ZoomIn() => ZoomMap(1f);
    public void ZoomOut() => ZoomMap(-1f);

    public void CalculateAndApplyMinZoom()
    {
        if (mapScrollRect != null && mapScrollRect.viewport != null && mapContent != null)
        {
            float minZoomX = mapScrollRect.viewport.rect.width / mapContent.rect.width;
            float minZoomY = mapScrollRect.viewport.rect.height / mapContent.rect.height;

            minZoom = Mathf.Max(minZoomX, minZoomY);

            if (currentZoom < minZoom)
            {
                currentZoom = minZoom;
                mapContent.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
        }
    }

    public void ToggleNavigationMode(bool isNavigating)
    {
        if (mapBottomNavCard != null) mapBottomNavCard.SetActive(isNavigating);

        if (isNavigating)
        {
            if (zoomButtonGroup != null) zoomButtonGroup.anchoredPosition = new Vector2(-20, 270);
        }
        else
        {
            if (zoomButtonGroup != null) zoomButtonGroup.anchoredPosition = new Vector2(-20, 20);
        }

        StartCoroutine(WaitAndRecalculateZoom());
    }

    private System.Collections.IEnumerator WaitAndRecalculateZoom()
    {
        yield return new WaitForSeconds(0.35f);

        CalculateAndApplyMinZoom();
    }
}
