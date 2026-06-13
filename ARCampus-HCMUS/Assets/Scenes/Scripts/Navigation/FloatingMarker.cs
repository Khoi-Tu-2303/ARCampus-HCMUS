
using UnityEngine;
using TMPro;

public class FloatingMarker : MonoBehaviour
{
    [Header("Hiệu ứng AR")]
    public float spinSpeed = 90f;       
    public float bobAmplitude = 0.3f;   
    public float bobFrequency = 2f;     

    [Header("Cố định kích thước trên màn hình")]
    public bool keepConstantScreenSize = true;
    public float sizeFactor = 0.05f;     

    private Vector3 startPos;
    private Vector3 initialScale;
    private Transform camTransform;

    void Start()
    {
        startPos = transform.localPosition;
        initialScale = transform.localScale;

        
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    void Update()
    {
        
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);

        
        float newY = startPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        
        if (keepConstantScreenSize && camTransform != null)
        {
            float distance = Vector3.Distance(transform.position, camTransform.position);
            
            transform.localScale = initialScale * (distance * sizeFactor);
        }
    }
}
