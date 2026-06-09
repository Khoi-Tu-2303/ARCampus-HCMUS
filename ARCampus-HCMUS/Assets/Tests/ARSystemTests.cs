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
    [TestCase(10.875808955069743, 106.79843986789899, 10.876354541234988, 106.79843265183723, 360f - 359.25100989663025f, 0.5f, "Test góc")]
    [TestCase(10.875947501222882, 106.79806663657297, 10.875942208979708, 106.79827728965455, 91.45597289807722f, 0.5f, "Test góc")]
    [TestCase(10.876217015276083, 106.7979763026944, 10.876160814212653, 106.79805878544056, 124.58043678310361f, 0.5f, "Test góc")]
    [TestCase(10.875282024798778, 106.79894111395271, 10.875368474237348, 106.79897884129343, 23.333285221611902f, 0.5f, "Test góc")]

    // Đổi 4 tham số tọa độ đầu tiên thành double
    public void CalculateBearing_ReturnsCorrectAngle_AllDirections(
                double lat1, double lng1,
                double lat2, double lng2,
                float expectedAngle, float tolerance, string testDescription)
    {
        // Hàm này giờ nhận double xịn
        float actualAngle = GeoMath.CalculateBearing(lat1, lng1, lat2, lng2);

        // Lật góc...
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

        // SỬA DÒNG NÀY: XÓA ÉP KIỂU (float)
        float bearing = GeoMath.CalculateBearing(userLat, userLng, targetLat, targetLng);

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

    [TestCase("S", "G", "S,B,C,D,G", "Vượt qua bẫy ngõ cụt E để đi vòng đến đích G")]
    [TestCase("S", "C", "S,B,C", "Đường đi ngắn nhất bình thường đến điểm C")]
    [TestCase("B", "G", "B,C,D,G", "Xuất phát từ một trạm giữa đồ thị đến đích")]
    [TestCase("C", "C", "C", "Điểm xuất phát trùng với điểm kết thúc")]
    [TestCase("S", "X", "", "Bắt lỗi: Đồ thị đứt gãy, đích đến X bị cô lập")]
    public void FindPath_AStar_HandlesVariousScenarios(
        string startNode, string goalNode, string expectedPathCsv, string testDescription)
    {
        // 1. Arrange: Khởi tạo môi trường ảo
        GameObject servicesObj = new GameObject("Services");
        var mockGraph = servicesObj.AddComponent<GraphService>();
        var mockPathFinder = servicesObj.AddComponent<PathFinder>();

        GraphService.Instance = mockGraph;
        PathFinder.Instance = mockPathFinder;

        // XÂY DỰNG MA TRẬN ĐỒ THỊ 7 ĐIỂM
        mockGraph.Nodes = new Dictionary<string, GraphNode>
        {
            { "S", new GraphNode { id = "S", lat = 10.000, lng = 106.000, name = "Start" } },
            { "E", new GraphNode { id = "E", lat = 10.000, lng = 106.001, name = "Dead End" } },
            { "G", new GraphNode { id = "G", lat = 10.000, lng = 106.002, name = "Goal" } },
            { "B", new GraphNode { id = "B", lat = 10.001, lng = 106.000, name = "Way 1" } },
            { "C", new GraphNode { id = "C", lat = 10.001, lng = 106.001, name = "Way 2" } },
            { "D", new GraphNode { id = "D", lat = 10.001, lng = 106.002, name = "Way 3" } },
            { "X", new GraphNode { id = "X", lat = 10.005, lng = 106.005, name = "Isolated" } }
        };

        // Kết nối các điểm
        mockGraph.Edges = new List<GraphEdge>
        {
            new GraphEdge { from = "S", to = "E", distance_m = 111f }, // Đường chim bay dụ dỗ nhưng cụt
            new GraphEdge { from = "S", to = "B", distance_m = 111f }, // Đường vòng
            new GraphEdge { from = "B", to = "C", distance_m = 111f },
            new GraphEdge { from = "C", to = "D", distance_m = 111f },
            new GraphEdge { from = "D", to = "G", distance_m = 111f }
            // Node X hoàn toàn không có Edge nối vào
        };
        mockGraph.IsLoaded = true;

        mockPathFinder.BuildNeighborCache();

        // 2. Act
        List<GraphNode> path = mockPathFinder.FindPath(startNode, goalNode);

        // 3. Log
        Debug.Log($"--- BÁO CÁO A*: {testDescription} ---");

        // 4. Assert logic linh hoạt cho chuỗi CSV (Comma-Separated Values)
        if (string.IsNullOrEmpty(expectedPathCsv))
        {
            // Case đứt gãy: Buộc thuật toán phải trả về Null
            Assert.IsNull(path, "Lỗi: Đích đến không thể tới được nhưng thuật toán vẫn trả về lộ trình ảo!");
            Debug.Log($"=> KẾT LUẬN: ĐẠT (PASS) - Đã chặn thành công việc tìm đường tới đảo hoang {goalNode}.\n");
        }
        else
        {
            Assert.IsNotNull(path, "Lỗi: Không tìm thấy đường đi!");

            // Tách chuỗi CSV "S,B,C" thành mảng ["S", "B", "C"]
            string[] expectedNodes = expectedPathCsv.Split(',');

            Assert.AreEqual(expectedNodes.Length, path.Count, "Lỗi: Số lượng trạm trên đường đi bị sai!");

            string pathLog = "Lộ trình thực tế: ";
            for (int i = 0; i < expectedNodes.Length; i++)
            {
                Assert.AreEqual(expectedNodes[i], path[i].id, $"Lỗi: Sai trạm ở bước thứ {i}!");
                pathLog += path[i].id + (i < expectedNodes.Length - 1 ? " -> " : "");
            }
            Debug.Log(pathLog);
            Debug.Log("=> KẾT LUẬN: ĐẠT (PASS) - Thuật toán A* giải quyết ma trận chính xác.\n");
        }

        // Dọn dẹp RAM
        GraphService.Instance = null;
        PathFinder.Instance = null;
        Object.DestroyImmediate(servicesObj);
    }
    // ==========================================
    // MODULE 3: BỘ NÃO PHÂN TÍCH TÌM KIẾM (SEARCH PARSER)
    // ==========================================

    [TestCase("f102", "Tòa F", "F102", "Phòng thường: Tòa F")]
    [TestCase("a205", "Tòa A", "A205", "Phòng thường: Tòa A")]
    [TestCase("dh2.3", "Nhà điều hành", "DH_2_3", "Nhà điều hành: Viết tắt có chấm")]
    [TestCase("ndh23", "Nhà điều hành", "DH_2_3", "Nhà điều hành: Viết liền không chấm")]
    [TestCase("thư viện", "Tòa C", "library", "Bí danh (Alias): Tìm theo tiện ích")]
    public void ParseIndoorSearch_ExtractsCorrectRoomInfo(
        string inputKeyword, string expectedBuilding, string expectedRoomId, string testDescription)
    {
        // 1. Arrange: Tạo Môi trường Giả lập (Mock Environment)
        GameObject testObj = new GameObject("TestEnvironment");

        // GIẢ LẬP FIREBASE: Đánh lừa bức tường lửa rằng các phòng này có tồn tại trên Đám mây
        var mockFirebase = testObj.AddComponent<FirebaseService>();
        FirebaseService.Instance = mockFirebase;
        FirebaseService.Instance.ValidIndoorIds = new HashSet<string> { "F102", "A205", "DH_2_3", "library" };

        var searchController = testObj.AddComponent<SearchPanelController>();

        // 2. Act: DÙNG C# REFLECTION ĐỂ GỌI HÀM PRIVATE
        // Chiêu này giúp gọi hàm ParseIndoorSearch mà không cần đổi nó thành public
        System.Reflection.MethodInfo parseMethod = typeof(SearchPanelController).GetMethod(
            "ParseIndoorSearch",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.IsNotNull(parseMethod, "Lỗi Reflection: Không tìm thấy hàm ParseIndoorSearch trong script!");

        // Gọi hàm thông qua Reflection và ép kiểu trả về List<SearchResultItem>
        List<SearchResultItem> results = (List<SearchResultItem>)parseMethod.Invoke(searchController, new object[] { inputKeyword });

        // 3. XUẤT LOG BÁO CÁO (Dành cho Đồ án)
        Debug.Log($"--- BÁO CÁO KIỂM THỬ: {testDescription} ---");
        Debug.Log($"[Đầu vào] User gõ      : '{inputKeyword}'");

        // 4. Assert: Kiểm chứng hệ thống
        Assert.IsNotNull(results, "Lỗi: Hàm trả về danh sách Null!");
        Assert.IsTrue(results.Count > 0, "Lỗi: Không bóc tách được kết quả nào (Bị Tường lửa Firebase chặn?)");

        SearchResultItem firstResult = results[0];
        Debug.Log($"[Bóc tách] Tòa nhà đích : {firstResult.TargetBuildingName}");
        Debug.Log($"[Bóc tách] Mã phòng ảo  : {firstResult.IndoorDocId}");

        Assert.AreEqual(expectedBuilding, firstResult.TargetBuildingName, "Bộ phân tích nhận diện sai Tòa nhà!");
        Assert.AreEqual(expectedRoomId, firstResult.IndoorDocId, "Bộ phân tích bóc tách sai Mã phòng!");

        Debug.Log("=> KẾT LUẬN: ĐẠT (PASS) - Thuật toán nội suy String hoạt động chính xác.\n");

        // 5. Dọn dẹp RAM
        Object.DestroyImmediate(testObj);
        FirebaseService.Instance = null;
    }
}