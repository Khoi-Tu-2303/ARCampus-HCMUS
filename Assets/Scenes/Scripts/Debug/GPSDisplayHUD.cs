using UnityEngine;
using TMPro;

public class GPSDisplayHUD : MonoBehaviour
{
    [Header("Kéo cục Txt_Coords vào đây")]
    public TextMeshProUGUI txtCoords;

    [Header("--- CHẾ ĐỘ MOCK GPS (TEST TRÊN MÁY) ---")]
    [Tooltip("Tích vào đây nếu muốn tự gõ số test trên Editor")]
    public bool useEditorMock = true;

    
    [Range(-90f, 90f)] public float mockLat = 10.8756f;
    [Range(-180f, 180f)] public float mockLng = 106.8006f;

    private bool isServiceRunning = false;

    void OnEnable()
    {
        
        InvokeRepeating(nameof(UpdateGPSData), 0.1f, 0.5f);

        
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
            
            currentLat = mockLat;
            currentLng = mockLng;
        }
        else
        {
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                currentLat = Input.location.lastData.latitude;
                currentLng = Input.location.lastData.longitude;
            }
            else
            {
                
                txtCoords.text = "<align=left><color=#00A896><size=50>LAT:</size> <color=#FFFF00>WAITING...</color>\n<size=50>LNG:</size> <color=#FFFF00>WAITING...</color></align>";
                return;
            }
        }

        
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
