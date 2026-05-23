// Indoor/IndoorProximityRadar.cs
using UnityEngine;

public class IndoorProximityRadar : MonoBehaviour
{
    [Header("UI References (Đã lỗi thời - Có thể gỡ bỏ ngoài Inspector)")]
    public GameObject btnShowIndoor;

    void Start()
    {
        // Ép ẩn luôn cái nút trôi nổi cũ đi nếu lỡ còn sót ngoài Scene
        if (btnShowIndoor != null) btnShowIndoor.SetActive(false);
    }

    void Update()
    {
        // ⛔ ĐÃ XÓA SẠCH LOGIC QUÉT GPS TỰ ĐỘNG ĐỂ NHƯỜNG SÂN KHẤU CHO AR POPUP CARD
    }
}