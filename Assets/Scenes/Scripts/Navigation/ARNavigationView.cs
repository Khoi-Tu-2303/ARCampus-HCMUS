// Navigation/ARNavigationView.cs
// BẢN UPDATE CUỐI: Zero GC + Trả tự do Mũi tên (60FPS) + Cache Camera

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ARNavigationView : MonoBehaviour
{
    [Header("AR 3D Arrow")]
    public GameObject arrow3DPrefab;
    public float arrowDistance = NavigationConstants.ArrowDistance;
    public float arrowHeightOffset = NavigationConstants.ArrowHeightOffset;

    [Header("AR Path Line")]
    public LineRenderer pathLine;
    public float lineYOffset = NavigationConstants.LineYOffset;

    [Header("AR Next Node Label")]
    public GameObject nextNodeLabelPrefab;

    private GameObject currentArrow3D;
    private GameObject currentNextNodeLabel;
    private TMP_Text nextNodeText;

    // Mỏ neo tọa độ — được set khi bắt đầu navigation
    private double navStartLat;
    private double navStartLng;
    private Vector3 navStartFeetPos;

    // ✅ TỐI ƯU ZERO GC: Pre-alloc mảng cố định để không đẻ rác khi vẽ LineRenderer
    private Vector3[] _linePointsBuffer = new Vector3[64];

    // ✅ BIẾN GIAO TIẾP TỐI ƯU MƯỢT 60FPS
    private float _targetBearing;

    // ✅ CACHE CAMERA: Tránh gọi Camera.main gây lag
    private Camera _cachedCamera;
    private Camera GetCamera() => _cachedCamera != null ? _cachedCamera : (_cachedCamera = Camera.main);


    public void InitAnchor(double startLat, double startLng)
    {
        navStartLat = startLat;
        navStartLng = startLng;
        Camera cam = GetCamera();
        if (cam != null)
        {
            navStartFeetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
        }
    }

    public void SpawnArrow()
    {
        if (currentArrow3D == null && arrow3DPrefab != null)
            currentArrow3D = Instantiate(arrow3DPrefab);
    }

    public void SpawnNodeLabel()
    {
        if (nextNodeLabelPrefab != null && currentNextNodeLabel == null)
        {
            currentNextNodeLabel = Instantiate(nextNodeLabelPrefab);
            nextNodeText = currentNextNodeLabel.GetComponent<TMP_Text>()
                            ?? currentNextNodeLabel.GetComponentInChildren<TMP_Text>();
        }
    }

    public void DrawARPath(List<GraphNode> path)
    {
        Camera cam = GetCamera();
        if (pathLine == null || path == null || cam == null) return;
        pathLine.useWorldSpace = true;

        int count = path.Count; // start point + nodes

        // Nới rộng cái túi (buffer) nếu đường đi đợt này quá dài
        if (_linePointsBuffer.Length < count)
            _linePointsBuffer = new Vector3[count + 16]; // grow thêm dự phòng

        _linePointsBuffer[0] = navStartFeetPos;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 offset = GeoMath.LatLngToMeterOffset(navStartLat, navStartLng, path[i].lat, path[i].lng);
            _linePointsBuffer[i] = navStartFeetPos + offset;
        }

        pathLine.positionCount = count;
        // ✅ Cực kỳ an toàn: SetPositions sẽ tự động chỉ lấy đúng `positionCount` điểm đầu tiên trong Buffer
        pathLine.SetPositions(_linePointsBuffer);
    }

    // ✅ NHẬN LỆNH TỪ SESSION (2Hz): Chỉ cập nhật góc quay mục tiêu
    public void UpdateARArrow(float bearing)
    {
        _targetBearing = bearing;
    }

    // ✅ THỰC THI ĐỒ HỌA (60Hz): Xử lý mượt mà mỗi frame
    void Update()
    {
        if (currentArrow3D == null) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        // 1. Tính tọa độ ngay trước mặt Camera (ĐÃ FIX: Thêm .transform)
        Vector3 targetPos = cam.transform.position + cam.transform.forward * arrowDistance;
        targetPos.y = cam.transform.position.y + arrowHeightOffset;

        // 2. Ép Mũi tên bay theo Camera liên tục (Mượt như Sunsilk)
        currentArrow3D.transform.position = Vector3.Lerp(currentArrow3D.transform.position, targetPos, Time.deltaTime * 5f);

        // 3. Xoay Mũi tên theo góc GPS đã được nạp từ UpdateARArrow
        currentArrow3D.transform.rotation = Quaternion.Slerp(currentArrow3D.transform.rotation,
                                                               Quaternion.Euler(0, _targetBearing, 0), Time.deltaTime * 5f);
    }
    public void UpdateARPathLine(int waypointIndex)
    {
        Camera cam = GetCamera();
        if (pathLine == null || waypointIndex >= pathLine.positionCount || cam == null) return;

        Vector3 feetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
        pathLine.SetPosition(0, feetPos);

        if (waypointIndex > 1)
        {
            for (int i = 1; i < waypointIndex; i++)
                pathLine.SetPosition(i, feetPos);
        }
    }

    public void UpdateNextNodeLabel(GraphNode target, float distanceToTarget)
    {
        if (currentNextNodeLabel == null) return;
        Vector3 offset = GeoMath.LatLngToMeterOffset(navStartLat, navStartLng, target.lat, target.lng);
        currentNextNodeLabel.transform.position = navStartFeetPos + offset;
        if (nextNodeText != null)
        {
            // ✅ ĐÃ FIX: Nếu không có tên, hoặc tên là cái Node rác W_ thì in chữ "Điểm tiếp theo"
            string name = target.name;
            if (string.IsNullOrEmpty(name) || target.id.StartsWith("W_") || target.id.StartsWith("CP_"))
            {
                name = "Điểm tiếp theo";
            }

            nextNodeText.text = $"{name}\n{distanceToTarget:F0} m";
        }
    }

    public void ClearAll()
    {
        if (currentArrow3D != null) { Destroy(currentArrow3D); currentArrow3D = null; }
        if (pathLine != null) pathLine.positionCount = 0;
        if (currentNextNodeLabel != null) { Destroy(currentNextNodeLabel); currentNextNodeLabel = null; }
    }
}