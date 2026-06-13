using UnityEngine;

public class BillboardLabel : MonoBehaviour
{
    private Camera mainCam;
    private Vector3 initialScale;

    [Header("Cài đặt Kích thước")]
    public bool keepConstantScreenSize = true;
    public float sizeFactor = 0.05f; 

    void Start()
    {
        
        mainCam = Camera.main;

        
        initialScale = transform.localScale;
    }

    
    void LateUpdate()
    {
        if (mainCam == null) return;

        
        if (keepConstantScreenSize)
        {
            float distance = Vector3.Distance(transform.position, mainCam.transform.position);
            
            transform.localScale = initialScale * (distance * sizeFactor);
        }

        
        Vector3 dirToCamera = mainCam.transform.position - transform.position;
        dirToCamera.y = 0;

        if (dirToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-dirToCamera);
        }
    }
}
