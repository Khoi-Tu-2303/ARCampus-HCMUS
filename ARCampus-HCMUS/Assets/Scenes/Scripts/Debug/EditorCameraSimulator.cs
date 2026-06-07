using UnityEngine;
using UnityEngine.InputSystem; // Gọi hệ thống Input Mới

public class EditorCameraSimulator : MonoBehaviour
{
    // Giảm độ nhạy xuống một chút vì New Input đọc delta chuột rất mượt và nhanh
    public float sensitivity = 0.2f;
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
#if UNITY_EDITOR
        // Giữ chuột phải để xoay camera
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            rotationY += mouseDelta.x * sensitivity;
            rotationX -= mouseDelta.y * sensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            
            transform.localEulerAngles = new Vector3(rotationX, rotationY, 0);
        }
#endif
    }
}