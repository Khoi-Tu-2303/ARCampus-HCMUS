// AR/ARCompassAligner.cs — PATCHED
// FIXES:
// [MEDIUM] AlignWithTrueNorth() was called only once in Start().
//          After GPS recovery, compass could drift without re-alignment.
//          Solution: Subscribe to GPSService.OnGPSReady and OnGPSRecovered events.
//          Also properly unsubscribe in OnDestroy to avoid memory leaks.

using UnityEngine;
using System.Collections;

public class ARCompassAligner : MonoBehaviour
{
    void Start()
    {
        // Subscribe to GPS events so we re-align every time GPS becomes available
        if (GPSService.Instance != null)
        {
            GPSService.Instance.OnGPSReady += AlignWithTrueNorth;
            GPSService.Instance.OnGPSRecovered += AlignWithTrueNorth;
        }
        else
        {
            // Fallback: GPSService not yet alive, wait for it
            StartCoroutine(WaitAndSubscribe());
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe to prevent NullReferenceException after object destruction
        if (GPSService.Instance != null)
        {
            GPSService.Instance.OnGPSReady -= AlignWithTrueNorth;
            GPSService.Instance.OnGPSRecovered -= AlignWithTrueNorth;
        }
    }

    IEnumerator WaitAndSubscribe()
    {
        float timeout = 15f;
        while (GPSService.Instance == null && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (GPSService.Instance != null)
        {
            GPSService.Instance.OnGPSReady += AlignWithTrueNorth;
            GPSService.Instance.OnGPSRecovered += AlignWithTrueNorth;

            // If GPS is already running when we subscribe, align immediately
            if (GPSService.Instance.IsReady)
                AlignWithTrueNorth();
        }
    }

    public void AlignWithTrueNorth()
    {
        float heading = Input.compass.trueHeading;
        float cameraY = Camera.main != null ? Camera.main.transform.localEulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(0f, heading - cameraY, 0f);
    }
}