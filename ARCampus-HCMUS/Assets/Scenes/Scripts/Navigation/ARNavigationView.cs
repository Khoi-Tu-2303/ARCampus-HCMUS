// Navigation/ARNavigationView.cs
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

    // ✅ MẢNG LƯU TRỮ TỌA ĐỘ ĐÃ ĐƯỢC "ĐỔ BÊ TÔNG" XUỐNG ĐƯỜNG
    private List<GraphNode> _currentPath;
    private Vector3[] _linePointsBuffer = new Vector3[64];
    private Vector3[] _nodeWorldPositions;
    private int _currentWaypointIndex = 0;

    private Camera _cachedCamera;
    private Camera GetCamera() => _cachedCamera != null ? _cachedCamera : (_cachedCamera = Camera.main);

    // Không cần dùng anchor gốc nữa vì đã có NodeWorldPositions
    public void InitAnchor(double startLat, double startLng) { }

    public void SpawnArrow()
    {
        if (arrow3DPrefab == null) return;
        if (currentArrow3D == null) currentArrow3D = Instantiate(arrow3DPrefab);
        else currentArrow3D.SetActive(true);
    }

    public void SpawnNodeLabel()
    {
        if (nextNodeLabelPrefab == null) return;
        if (currentNextNodeLabel == null)
        {
            currentNextNodeLabel = Instantiate(nextNodeLabelPrefab);
            nextNodeText = currentNextNodeLabel.GetComponent<TMP_Text>() ?? currentNextNodeLabel.GetComponentInChildren<TMP_Text>();
        }
        else currentNextNodeLabel.SetActive(true);
    }

    // ──────────────────────────────────────────────────────────
    // 1. TÍNH TOÁN VÀ ĐÓNG BĂNG TỌA ĐỘ TOÀN BỘ LỘ TRÌNH (CHỈ CHẠY 1 LẦN)
    // ──────────────────────────────────────────────────────────
    public void DrawARPath(List<GraphNode> path)
    {
        _currentPath = path;
        Camera cam = GetCamera();
        if (pathLine == null || path == null || cam == null) return;

        int count = path.Count;
        if (count < 2) { pathLine.positionCount = 0; return; }

        pathLine.useWorldSpace = true;

        if (_linePointsBuffer.Length < count)
        {
            int newSize = _linePointsBuffer.Length;
            while (newSize < count) newSize *= 2;
            _linePointsBuffer = new Vector3[newSize];
        }

        _nodeWorldPositions = new Vector3[count];

        double uLat = GPSService.Instance.Latitude;
        double uLng = GPSService.Instance.Longitude;

        // Tính tọa độ 3D cho toàn bộ các trạm dựa trên GPS và La bàn HIỆN TẠI, sau đó khóa chặt!
        for (int i = 0; i < count; i++)
        {
            _nodeWorldPositions[i] = GeoMath.GpsToARWorldPosition(
                path[i].lat, path[i].lng,
                uLat, uLng,
                cam.transform,
                lineYOffset
            );
        }

        pathLine.positionCount = count;
        UpdateARPathLine(0); // Gọi ngay để cập nhật dây
    }

    // Giữ lại hàm này để tương thích với NavigationSession nhưng bỏ qua bearing địa lý
    public void UpdateARArrow(float bearing) { }

    // ──────────────────────────────────────────────────────────
    // 2. MŨI TÊN CHỈ CẦN XOAY VỀ HƯỚNG CÁC TRẠM ĐÃ ĐƯỢC ĐÓNG BĂNG
    // ──────────────────────────────────────────────────────────
    void Update()
    {
        if (currentArrow3D == null || !currentArrow3D.activeSelf) return;
        Camera cam = GetCamera();

        // Nếu chưa có mảng tọa độ đóng băng thì không xoay
        if (cam == null || _nodeWorldPositions == null || _currentWaypointIndex >= _nodeWorldPositions.Length) return;

        // Mũi tên luôn bay lơ lửng trước mặt Camera
        Vector3 targetPos = cam.transform.position + cam.transform.forward * arrowDistance;
        targetPos.y = cam.transform.position.y + arrowHeightOffset;

        currentArrow3D.transform.position = Vector3.Lerp(
            currentArrow3D.transform.position, targetPos, Time.deltaTime * 5f);

        // MŨI TÊN CHỈ THẲNG VÀO TRẠM TIẾP THEO TRONG KHÔNG GIAN 3D
        Vector3 dirToNode = _nodeWorldPositions[_currentWaypointIndex] - cam.transform.position;
        dirToNode.y = 0; // Khóa trục Y để mũi tên không bị chúi xuống đất hay chĩa lên trời

        if (dirToNode.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToNode);
            currentArrow3D.transform.rotation = Quaternion.Slerp(
                currentArrow3D.transform.rotation,
                targetRot,
                Time.deltaTime * 5f);
        }
    }

    // ──────────────────────────────────────────────────────────
    // 3. VẼ DÂY DỰA TRÊN CÁC TRẠM ĐÃ ĐÓNG BĂNG (KHÔNG DÙNG LẠI GPS NỮA)
    // ──────────────────────────────────────────────────────────
    public void UpdateARPathLine(int waypointIndex)
    {
        _currentWaypointIndex = waypointIndex;
        Camera cam = GetCamera();
        if (pathLine == null || cam == null || _currentPath == null || _nodeWorldPositions == null) return;
        if (waypointIndex >= pathLine.positionCount) return;

        // Điểm đầu tiên luôn dính vào gót chân Camera
        Vector3 feetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
        _linePointsBuffer[0] = feetPos;

        // Các trạm ĐÃ ĐI QUA -> Gom hết về gót chân để giấu đi
        for (int i = 1; i < waypointIndex; i++)
        {
            _linePointsBuffer[i] = feetPos;
        }

        // Các trạm CHƯA ĐI QUA -> Lấy thẳng tọa độ từ mảng đã đóng băng!
        for (int i = waypointIndex; i < _currentPath.Count; i++)
        {
            _linePointsBuffer[i] = _nodeWorldPositions[i];
        }

        pathLine.SetPositions(_linePointsBuffer);
    }

    // ──────────────────────────────────────────────────────────
    // 4. ĐẶT NHÃN VÀO ĐÚNG TỌA ĐỘ ĐÓNG BĂNG CỦA TRẠM (Đứng im như tượng)
    // ──────────────────────────────────────────────────────────
    public void UpdateNextNodeLabel(GraphNode target, float distanceToTarget)
    {
        if (currentNextNodeLabel == null || _nodeWorldPositions == null || _currentWaypointIndex >= _nodeWorldPositions.Length) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        Vector3 fixedNodePos = _nodeWorldPositions[_currentWaypointIndex];

        // Kéo Nhãn bay bổng lên so với gót chân Camera
        fixedNodePos.y = cam.transform.position.y + NavigationConstants.LabelFloatHeight;

        currentNextNodeLabel.transform.position = fixedNodePos;

        if (nextNodeText != null)
        {
            string name = target.name;
            if (string.IsNullOrEmpty(name) || target.id.StartsWith("W_") || target.id.StartsWith("CP_"))
                name = "Điểm tiếp theo";
            nextNodeText.text = $"{name}\n{distanceToTarget:F0} m";
        }
    }

    public void ClearAll()
    {
        if (currentArrow3D != null) currentArrow3D.SetActive(false);
        if (currentNextNodeLabel != null) currentNextNodeLabel.SetActive(false);
        if (pathLine != null) pathLine.positionCount = 0;
        _currentPath = null;
        _nodeWorldPositions = null;
    }
}