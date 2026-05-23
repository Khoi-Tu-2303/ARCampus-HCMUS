using UnityEngine;
using System.Collections;

public class ARCompassAligner : MonoBehaviour
{
    IEnumerator Start()
    {
        float timeout = 10f;

        // ✅ ĐÃ FIX: Sắp xếp lại dấu ngoặc cho chuẩn logic
        while ((GPSService.Instance == null || !GPSService.Instance.IsReady) && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (GPSService.Instance != null && GPSService.Instance.IsReady)
            AlignWithTrueNorth();
    }

    public void AlignWithTrueNorth()
    {
        float heading = Input.compass.trueHeading;
        float cameraY = Camera.main.transform.localEulerAngles.y;
        transform.rotation = Quaternion.Euler(0, heading - cameraY, 0);
    }
}