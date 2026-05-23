using UnityEngine;

public class BillboardLabel : MonoBehaviour
{
    private Camera mainCam;
    private Vector3 initialScale;

    [Header("Cài đặt Kích thước")]
    public bool keepConstantScreenSize = true;
    public float sizeFactor = 0.05f; // Nếu thấy to quá/nhỏ quá thì ông chỉnh số này ngoài Unity nhé

    void Start()
    {
        // Caching: Lưu sẵn Camera từ đầu để đéo phải tìm kiếm mỗi frame
        mainCam = Camera.main;

        // Lưu lại kích thước gốc lúc mới sinh ra
        initialScale = transform.localScale;
    }

    // 👉 Ăn tiền ở chữ LateUpdate: Đảm bảo Camera AR di chuyển xong xuôi thì bảng mới xoay và scale theo
    void LateUpdate()
    {
        if (mainCam == null) return;

        // 1. LOGIC GIỮ NGUYÊN KÍCH THƯỚC TRÊN MÀN HÌNH
        if (keepConstantScreenSize)
        {
            float distance = Vector3.Distance(transform.position, mainCam.transform.position);
            // Càng xa thì tự scale to lên, càng gần thì tự scale nhỏ đi -> Đánh lừa thị giác là nó đứng im
            transform.localScale = initialScale * (distance * sizeFactor);
        }

        // 2. LOGIC KHÓA TRỤC Y CỰC XỊN (Xoay mặt về Camera)
        Vector3 dirToCamera = mainCam.transform.position - transform.position;
        dirToCamera.y = 0;

        if (dirToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-dirToCamera);
        }
    }
}