// Navigation/FloatingMarker.cs
using UnityEngine;
using TMPro;

public class FloatingMarker : MonoBehaviour
{
    [Header("Hiệu ứng AR")]
    public float spinSpeed = 90f;       // Tốc độ xoay (độ/giây)
    public float bobAmplitude = 0.3f;   // Độ cao nảy lên xuống
    public float bobFrequency = 2f;     // Tốc độ nảy

    [Header("Cố định kích thước trên màn hình")]
    public bool keepConstantScreenSize = true;
    public float sizeFactor = 0.05f;     // Chỉnh số này để tăng/giảm độ to nhỏ tổng thể

    private Vector3 startPos;
    private Vector3 initialScale;
    private Transform camTransform;

    void Start()
    {
        startPos = transform.localPosition;
        initialScale = transform.localScale;

        // Nhận diện Camera chính để tính khoảng cách
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1. Tự động xoay tròn quanh trục Z (Sửa trục Z theo ý ông)
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);

        // 2. Nảy lên xuống mượt mà bằng sóng Sin
        float newY = startPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // 3. ✅ THUẬT TOÁN ĐỘC QUYỀN: Giữ nguyên kích thước hiển thị bất kể xa gần
        if (keepConstantScreenSize && camTransform != null)
        {
            float distance = Vector3.Distance(transform.position, camTransform.position);
            // Càng ra xa, vật thể tự nhân tỷ lệ bự lên để đánh lừa thị giác người nhìn
            transform.localScale = initialScale * (distance * sizeFactor);
        }
    }
}