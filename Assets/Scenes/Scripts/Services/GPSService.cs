// Services/GPSService.cs
using UnityEngine;
using System.Collections;

public class GPSService : MonoBehaviour
{
    public static GPSService Instance;
    public double Latitude = 0;
    public double Longitude = 0;
    public bool IsReady = false;

    [Header("Debug - Test ở nhà")]
    public bool useMockGPS = true;
    public double mockLat = 10.87527;
    public double mockLng = 106.79797;

    [Header("Cài đặt Cảnh báo")]
    public float maxAllowedAccuracy = 20f; // Sai số tối đa (trên 50m là báo động)

    // ✅ THÊM BIẾN GIẢ LẬP LỖI Ở ĐÂY
    [Header("Giả lập Lỗi")]
    public bool simulateGPSLost = false;

    void Awake() => Instance = this;

    IEnumerator Start()
    {
        if (useMockGPS)
        {
            Latitude = mockLat;
            Longitude = mockLng;
            IsReady = true;
            Debug.Log($"🧪 Mock GPS: {Latitude}, {Longitude}");
            // Bỏ comment dòng dưới nếu muốn UpdateGPS vẫn chạy khi xài Mock GPS
            InvokeRepeating(nameof(UpdateGPS), 0f, 3f);
            yield break;
        }

        // 1. Check quyền truy cập
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            yield return new WaitForSeconds(1f);
        }

        // Nếu xin quyền xong mà user vẫn Say No -> Bật Modal đá về Login
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
        {
            Debug.LogError("❌ User từ chối quyền GPS!");
            if (SystemModalController.Instance != null)
                SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToLogin);
            yield break;
        }

        Input.compass.enabled = true;
        Input.location.Start(3f, 1f);

        int timeout = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1f);
            timeout--;
        }

        // 2. Check kết quả khởi động GPS
        if (Input.location.status == LocationServiceStatus.Running)
        {
            IsReady = true;
            Debug.Log("✅ GPS Ready!");
            InvokeRepeating(nameof(UpdateGPS), 0f, 3f);
        }
        else
        {
            Debug.LogError("❌ GPS failed: " + Input.location.status);
            if (SystemModalController.Instance != null)
                SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToLogin); // Mới vào mà lỗi thì về Login
        }
    }

    void UpdateGPS()
    {
        // ✅ THÊM ĐOẠN CHECK GIẢ LẬP LỖI VÀO NGAY ĐẦU HÀM
        if (simulateGPSLost)
        {
            IsReady = false;
            if (SystemModalController.Instance != null)
                SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToMain);
            return; // Cắt luồng chạy, không update tọa độ nữa
        }

        // Nếu đang xài Mock GPS thì bỏ qua mấy cái check của điện thoại thật
        if (useMockGPS) return;

        // Đang chạy mà tự nhiên user tắt định vị trên điện thoại
        if (Input.location.status != LocationServiceStatus.Running)
        {
            IsReady = false;
            if (SystemModalController.Instance != null)
                // ĐÁ VỀ LOGIN CHỨ KHÔNG VỀ MAIN NỮA
                SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToLogin);
            return;
        }

        Latitude = Input.location.lastData.latitude;
        Longitude = Input.location.lastData.longitude;
        float accuracy = Input.location.lastData.horizontalAccuracy;

        // 3. LOGIC CHECK SAI SỐ (WARNING CỰC GẮT)
        if (accuracy > maxAllowedAccuracy)
        {
            IsReady = false;
            if (SystemModalController.Instance != null)
                // CHỈ HỦY DẪN ĐƯỜNG VÀ VỀ MÀN HÌNH MAIN
                SystemModalController.Instance.ShowWarning(WarningType.GPS, BackActionTarget.GoToMain);
        }
        else
        {
            IsReady = true; // Sóng khỏe lại thì bật cờ Ready
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log($"📍 GPS: {Latitude:F6}, {Longitude:F6} | Sai số: {accuracy}m");
#endif
    }
}