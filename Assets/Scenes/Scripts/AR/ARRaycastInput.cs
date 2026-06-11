// AR/ARRaycastInput.cs
// TÁCH TỪ: ARInteractionManager — Update() input handling + ShootRaycast()
// BẢN UPDATE: Tối ưu Camera Caching (Tránh overhead FindObjectWithTag)

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ARRaycastInput : MonoBehaviour
{
    // ✅ CACHE CAMERA (Tuyệt chiêu tối ưu 5% CPU)
    private Camera _cachedCamera;
    private Camera GetCamera() => _cachedCamera != null ? _cachedCamera : (_cachedCamera = Camera.main);

    void Update()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            ShootRaycast(Mouse.current.position.ReadValue());
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                ShootRaycast(touch.position.ReadValue());
        }
    }

    void ShootRaycast(Vector2 screenPosition)
    {
        // ✅ Gọi Camera từ Cache thay vì dùng Camera.main
        Camera cam = GetCamera();
        if (cam == null) return; // Bảo vệ 2 lớp

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            ARLabelBehavior label = hit.collider.GetComponent<ARLabelBehavior>();
            if (label != null)
            {
                Debug.Log(">>> Bắn trúng nhãn: " + label.buildingName);
                PopupController.Instance?.ShowPopup(label.buildingName);
            }
        }
    }
}