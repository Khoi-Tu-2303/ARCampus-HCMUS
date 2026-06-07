using UnityEngine;
using TMPro;

public class GPSDisplayHUD : MonoBehaviour
{
    [Header("Kéo cục Txt_Coords vào đây")]
    public TextMeshProUGUI txtCoords;

    [Header("--- CHẾ ĐỘ MOCK GPS (TEST TRÊN MÁY) ---")]
    [Tooltip("Tích vào đây nếu muốn tự gõ số test trên Editor")]
    public bool useEditorMock = true;

    // Gắn Range để ông kéo slider trong Inspector cực mượt
    [Range(-90f, 90f)] public float mockLat = 10.8756f;
    [Range(-180f, 180f)] public float mockLng = 106.8006f;

    private bool isServiceRunning = false;

    void OnEnable()
    {
        // Gọi hàm cập nhật 0.5 giây/lần cực kỳ êm ái, KHÔNG BAO GIỜ TRÀN RAM
        InvokeRepeating(nameof(UpdateGPSData), 0.1f, 0.5f);

        // Nếu KHÔNG dùng Mock Editor mà muốn lấy từ app MockGPS của OS/Điện thoại
        if (!useEditorMock && Input.location.isEnabledByUser)
        {
            Input.location.Start(5f, 5f);
            isServiceRunning = true;
        }
    }

    void UpdateGPSData()
    {
        if (txtCoords == null) return;

        float currentLat = 0f;
        float currentLng = 0f;

        if (useEditorMock)
        {
            // 1. Lấy thẳng số từ thanh kéo Inspector để test real-time
            currentLat = mockLat;
            currentLng = mockLng;
        }
        else
        {
            // 2. Lấy từ OS (Hỗ trợ cả GPS thật lẫn app MockGPS chạy ngầm trên Android)
            if (Input.location.status == LocationServiceStatus.Running)
            {
                currentLat = Input.location.lastData.latitude;
                currentLng = Input.location.lastData.longitude;
            }
            else
            {
                // Bơm text lúc đang chờ tín hiệu (Giữ đúng form màu của ông)
                txtCoords.text = "<align=left><color=#00A896><size=50>LAT:</size> <color=#FFFF00>WAITING...</color>\n<size=50>LNG:</size> <color=#FFFF00>WAITING...</color></align>";
                return;
            }
        }

        // BƠM CHUỖI RICH TEXT CHẨN 100% THEO ĐÚNG MẪU ÔNG CHỈNH
        txtCoords.text = $"<align=left><color=#00A896><size=50>LAT:</size> <color=#FF0000>{currentLat:F4}° N</color>\n<size=50>LNG:</size> <color=#FF0000>{currentLng:F4}° E</color></align>";
    }

    void OnDisable()
    {
        CancelInvoke(nameof(UpdateGPSData));
        if (isServiceRunning)
        {
            Input.location.Stop();
            isServiceRunning = false;
        }
    }
}