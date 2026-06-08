using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ARSystemTests
{
    // ==========================================
    // MODULE 1: TOÁN HỌC KHÔNG GIAN (GEOMATH)
    // ==========================================

    [TestCase(10.875606504905619, 106.79703189178923, 10.875600207227308, 106.7963727868007, 72.08f, 5f, "Khoảng cách lớn hơn 50m")]
    [TestCase(10.875515299284046, 106.79793151913242, 10.875516660562553, 106.7982794489701, 37.38f, 5f, "Khoảng cách lớn hơn 30m")]
    [TestCase(10.875516660562553, 106.7982794489701, 10.875521446818041, 106.79843903078228, 10.77f, 8f, "Khoảng cách khoảng 10m")]
    [TestCase(10.876284367815927, 106.79755249684132, 10.876221058457418, 106.79755223759872, 9.88f, 8f, "Khoảng cách khoảng 10m")]
    public void Haversine_CalculatesDistance_AtVariousScales(
        double lat1, double lng1,
        double lat2, double lng2,
        float expectedDistance, float tolerance, string testDescription)
    {
        // 1. Act (Thực thi tính toán)
        float actualDistance = GeoMath.HaversineDouble(lat1, lng1, lat2, lng2);

        // 2. TÍNH TOÁN ĐỘ LỆCH THỰC TẾ ĐỂ GHI LOG BÁO CÁO
        float absoluteError = Mathf.Abs(expectedDistance - actualDistance);

        // 3. XUẤT LOG (Sẽ hiển thị ngay trong Test Runner)
        Debug.Log($"--- BÁO CÁO KIỂM THỬ: {testDescription} ---");
        Debug.Log($"[Đầu vào] Tọa độ 1 : {lat1:F6}, {lng1:F6}");
        Debug.Log($"[Đầu vào] Tọa độ 2 : {lat2:F6}, {lng2:F6}");
        Debug.Log($"[Tính toán] Quãng đường lý thuyết : {expectedDistance} m");
        Debug.Log($"[Tính toán] Quãng đường thực tế   : {actualDistance} m");
        Debug.Log($"[Phân tích] Độ lệch (Error)     : {absoluteError} m");
        Debug.Log($"[Ngưỡng] Sai số cho phép        : <= {tolerance} m");

        // 4. Assert (Đo lường Pass/Fail)
        Assert.AreEqual(expectedDistance, actualDistance, tolerance,
            $"FAIL [{testDescription}]\n=> Tính ra: {actualDistance}m | Kỳ vọng: {expectedDistance}m");

        // 5. Nếu đi qua được lệnh Assert mà không văng lỗi, nghĩa là test đã PASS.
        Debug.Log("=> KẾT LUẬN: ĐẠT (PASS) - Thuật toán ổn định trong ngưỡng cho phép.\n");
    }

    // ==========================================
    // MODULE 1.2: TOÁN HỌC KHÔNG GIAN (GEOMATH)
    // KIỂM THỬ GÓC PHƯƠNG VỊ (BEARING / AZIMUTH ANGLE)
    // ==========================================
    [TestCase(10.875808955069743f, 106.79843986789899f, 10.876354541234988f, 106.79843265183723f, 360f - 359.25100989663025f, 0.5f, "Test góc")]
    [TestCase(10.875947501222882f, 106.79806663657297f, 10.875942208979708f, 106.79827728965455f, 91.45597289807722f, 0.5f, "Test góc")]
    [TestCase(10.876217015276083f, 106.7979763026944f, 10.876160814212653f, 106.79805878544056f, 124.58043678310361f, 0.5f, "Test góc")]
    [TestCase(10.875282024798778f, 106.79894111395271f, 10.875368474237348f, 106.79897884129343f, 23.333285221611902f, 0.5f, "Test góc")]

    public void CalculateBearing_ReturnsCorrectAngle_AllDirections(
            float lat1, float lng1,
            float lat2, float lng2,
            float expectedAngle, float tolerance, string testDescription)
    {
        float actualAngle = GeoMath.CalculateBearing(lat1, lng1, lat2, lng2);

        // LOGIC LẬT GÓC THEO YÊU CẦU:
        // Biến góc > 180 (VD: 359.2 độ) thành độ lệch tuyệt đối so với trục 0 (VD: 0.8 độ)
        if (actualAngle > 180f)
        {
            actualAngle = 360f - actualAngle;
        }

        Debug.Log($"--- BÁO CÁO KIỂM THỬ: {testDescription} ---");
        Debug.Log($"[Đầu vào] User tại   : {lat1:F6}, {lng1:F6}");
        Debug.Log($"[Đầu vào] Đích đến tại: {lat2:F6}, {lng2:F6}");
        Debug.Log($"[Tính toán] Góc lý thuyết : {expectedAngle}°");
        Debug.Log($"[Tính toán] Góc thực tế sau khi lật: {actualAngle}°");

        float difference = Mathf.Abs(expectedAngle - actualAngle);
        Debug.Log($"[Phân tích] Độ lệch góc : {difference}° (Dung sai: <= {tolerance}°)");

        Assert.IsTrue(difference <= tolerance,
            $"FAIL [{testDescription}]\n=> Tính ra: {actualAngle}° | Kỳ vọng: {expectedAngle}°");

        Debug.Log("=> KẾT LUẬN: ĐẠT (PASS) - Trục xoay hoạt động chính xác.\n");
    }

    // ==========================================
    // MODULE 1.3: TOÁN HỌC KHÔNG GIAN (GEOMATH)
    // KIỂM THỬ HỆ TỌA ĐỘ AR (AR WORLD POSITION) VỚI CAMERA XOAY
    // ==========================================

    // ==========================================
    // MODULE 1.3: KIỂM THỬ HỆ TỌA ĐỘ AR BẰNG ĐỒNG BỘ CAMERA
    // (Self-Validation: AR Object must always center when Camera looks at it)
    // ==========================================

    // Test 1: Ngã rẽ trong trường (Khoảng cách ngắn)
    [TestCase(10.876354541234988, 106.79843265183723, 10.875808955069743, 106.79843986789899, "")]
    [TestCase(10.875516660562553, 106.79703050561062, 10.875086024296607, 106.79693876475551, "")]
    [TestCase(10.876217015276083, 106.7979763026944, 10.876160814212653, 106.79805878544056, "")]
    [TestCase(10.875282024798778, 106.79894111395271, 10.875368474237348, 106.79897884129343, "")]
    public void GpsToARWorldPosition_ProjectsToCorrectWorldCoordinates(
        double userLat, double userLng, double targetLat, double targetLng, string testDescription)
    {
        // 1. Tự động đo khoảng cách và góc
        float distance = GeoMath.HaversineDouble(userLat, userLng, targetLat, targetLng);
        float bearing = GeoMath.CalculateBearing((float)userLat, (float)userLng, (float)targetLat, (float)targetLng);

        // 2. Chạy hàm của hệ thống
        GameObject dummyCam = new GameObject("DummyCam");
        Vector3 arPosition = GeoMath.GpsToARWorldPosition(targetLat, targetLng, userLat, userLng, dummyCam.transform);

        // 3. TÍNH TOÁN EXPECTED KẾT QUẢ ĐÚNG CHUẨN WORLD SPACE
        // Phải đổi Bearing sang Radian để chạy hàm Sin/Cos
        float expectedX = distance * Mathf.Sin(bearing * Mathf.Deg2Rad);
        float expectedZ = distance * Mathf.Cos(bearing * Mathf.Deg2Rad);

        // 4. Báo cáo
        Debug.Log($"--- BÁO CÁO: {testDescription} ---");
        Debug.Log($"[Đo đạc] Khoảng cách: {distance:F2}m | Góc: {bearing:F2}°");
        Debug.Log($"[Kỳ vọng] Hệ thống phải render tại : X={expectedX:F2}, Z={expectedZ:F2}");
        Debug.Log($"[Thực tế] Hệ thống đang render tại : X={arPosition.x:F2}, Z={arPosition.z:F2}");

        // 5. Assert (Dung sai 1.0 mét cho hao hụt lượng giác)
        Assert.AreEqual(expectedX, arPosition.x, 1.0f, "Toán học chiếu World Space trục X bị sai!");
        Assert.AreEqual(expectedZ, arPosition.z, 1.0f, "Toán học chiếu World Space trục Z bị sai!");

        Debug.Log("=> KẾT LUẬN: ĐẠT (PASS) - Phép chiếu bề mặt cong sang mặt phẳng Euclid 3D hoạt động hoàn hảo.\n");
        Object.DestroyImmediate(dummyCam);
    }

    // ==========================================
    // MODULE 2: THUẬT TOÁN TÌM ĐƯỜNG (PATHFINDER)
    // ==========================================

    [Test]
    public void FindPath_AStarAlgorithm_ReturnsOptimalRoute()
    {
        // BÀI TOÁN GỐC: PathFinder phụ thuộc vào GraphService (Singleton). 
        // Trong Unit Test, ta phải tạo một GraphService "Giả" (Mock) trên RAM.

        // 1. Arrange
        GameObject servicesObj = new GameObject("Services");
        var mockGraph = servicesObj.AddComponent<GraphService>();
        var mockPathFinder = servicesObj.AddComponent<PathFinder>();

        GraphService.Instance = mockGraph;
        PathFinder.Instance = mockPathFinder;
        // Tạo 3 node: A -> B -> C. Đi thẳng từ A đến C là tối ưu nhất.
        mockGraph.Nodes = new Dictionary<string, GraphNode>
        {
            { "A", new GraphNode { id = "A", lat = 10.0, lng = 106.0 } },
            { "B", new GraphNode { id = "B", lat = 10.1, lng = 106.1 } }, // Điểm rẽ xa
            { "C", new GraphNode { id = "C", lat = 10.2, lng = 106.2 } }
        };
        mockGraph.Edges = new List<GraphEdge>
        {
            new GraphEdge { from = "A", to = "B", distance_m = 100f },
            new GraphEdge { from = "B", to = "C", distance_m = 100f },
            new GraphEdge { from = "A", to = "C", distance_m = 150f } // Đường chim bay ngắn hơn
        };
        mockGraph.IsLoaded = true;

        // Gọi hàm khởi tạo Cache (giả lập Awake)
        mockPathFinder.BuildNeighborCache();

        // 2. Act
        List<GraphNode> path = mockPathFinder.FindPath("A", "C");

        // 3. Assert
        Assert.IsNotNull(path, "Thuật toán trả về Null, không tìm thấy đường!");
        Assert.AreEqual(2, path.Count, "Lộ trình sai! Phải đi thẳng A -> C (2 nodes)");
        Assert.AreEqual("A", path[0].id);
        Assert.AreEqual("C", path[1].id);

        GraphService.Instance = null;
        PathFinder.Instance = null;
        Object.DestroyImmediate(servicesObj); // Dọn dẹp
    }
}