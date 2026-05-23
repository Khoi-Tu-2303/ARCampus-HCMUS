// Map/MapController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    public static MapController Instance;

    [Header("Thành phần Bản đồ")]
    public ScrollRect mapScrollRect;
    public RectTransform mapContent;

    [Header("UI Elements (Tự động co giãn)")]
    public GameObject mapBottomNavCard;   // Kéo MapBottomNavCard vào đây
    public RectTransform zoomButtonGroup; // Kéo cụm ButtonZoom vào đây

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
    public float minZoom = 0.5f; // Giữ nguyên biến của ông
    public float maxZoom = 10f;

    private float currentZoom = 1f;

    // --- BIẾN TỐI ƯU BLUE DOT ---
    private double _lastLat;
    private double _lastLng;

    void Awake() => Instance = this;

    void Start()
    {
        if (mapContent != null) currentZoom = mapContent.localScale.x;

        // ✅ GỌI HÀM TÍNH TOÁN MIN ZOOM CỦA ÔNG LÚC MỚI VÀO
        CalculateAndApplyMinZoom();

        if (mapScrollRect != null)
        {
            // ✅ KHÓA CỨNG MAP, KHÔNG CHO KÉO TRÔI RA KHỎI VIỀN
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
        if (Input.mouseScrollDelta.y != 0)
            ZoomMap(Input.mouseScrollDelta.y * zoomSpeedPC);
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0), t1 = Input.GetTouch(1);
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

    // =========================================================
    // HÀM NÀY CHỨA ĐÚNG LOGIC CỦA ÔNG, CHỈ TÁCH RA ĐỂ GỌI LẠI ĐƯỢC
    // =========================================================
    public void CalculateAndApplyMinZoom()
    {
        if (mapScrollRect != null && mapScrollRect.viewport != null && mapContent != null)
        {
            // Ép cái Map phải bự hơn hoặc bằng cái Viewport ở cả 2 chiều X và Y
            float minZoomX = mapScrollRect.viewport.rect.width / mapContent.rect.width;
            float minZoomY = mapScrollRect.viewport.rect.height / mapContent.rect.height;

            // Lấy con số lớn hơn để đảm bảo không bị hụt ở bất kỳ chiều nào
            minZoom = Mathf.Max(minZoomX, minZoomY);

            // Ép zoom hiện tại không được nhỏ hơn mức tối thiểu vừa tính
            if (currentZoom < minZoom)
            {
                currentZoom = minZoom;
                mapContent.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
        }
    }

    // =========================================================
    // HÀM MỚI: CHUYỂN ĐỔI CHẾ ĐỘ VÀ TÍNH LẠI ZOOM THEO VIEWPORT MỚI
    // =========================================================
    public void ToggleNavigationMode(bool isNavigating)
    {
        // 1. Bật/Tắt thẻ Nav
        if (mapBottomNavCard != null) mapBottomNavCard.SetActive(isNavigating);

        // 2. Nâng hạ cụm nút Zoom (+)(-)
        if (isNavigating)
        {
            if (zoomButtonGroup != null) zoomButtonGroup.anchoredPosition = new Vector2(-20, 270);
        }
        else
        {
            if (zoomButtonGroup != null) zoomButtonGroup.anchoredPosition = new Vector2(-20, 20);
        }

        // 3. Gọi Coroutine để dời việc tính toán sang Frame tiếp theo
        StartCoroutine(WaitAndRecalculateZoom());
    }

    // Cái này là Phép thuật thời gian của Unity nè
    private System.Collections.IEnumerator WaitAndRecalculateZoom()
    {
        // Lệnh này bắt code đứng đợi 1 Frame để UI Layout giãn cái Viewport ra xong xuôi
        yield return null;

        // Lúc này Viewport đã mang kích thước thật (dài tới đáy), gọi hàm tính Toán là chuẩn 100%!
        CalculateAndApplyMinZoom();
    }
}