// Services/GPSService.cs — PRODUCTION-READY GPS STATE MACHINE
// FIXES:
// [CRITICAL] GPS toggle runtime failure — replaced InvokeRepeating with coroutine state machine
// [CRITICAL] Stale location not invalidated after GPS disabled
// [CRITICAL] OnRetryClicked did not restart Input.location
// [CRITICAL] Modal spam from repeated InvokeRepeating calls
// [HIGH]     OnApplicationPause not handled
// [HIGH]     GeoMath compass cache not reset on GPS recovery

using UnityEngine;
using System.Collections;
using System;

public enum GPSState
{
    Uninitialized,
    Requesting,
    Starting,
    Running,
    Lost,
    Recovering
}

public class GPSService : MonoBehaviour
{
    public static GPSService Instance;

    [Header("Read-only State")]
    public GPSState State = GPSState.Uninitialized;
    public double Latitude;
    public double Longitude;
    public float Accuracy;

    // IsReady is TRUE only when State == Running AND last update is fresh
    public bool IsReady => State == GPSState.Running && !_locationStale;

    [Header("Debug - Mock GPS")]
    public bool useMockGPS = false;
    public double mockLat = 10.87527;
    public double mockLng = 106.79797;

    [Header("Settings")]
    public float maxAllowedAccuracy = 25f;
    public float staleLocationTimeout = 12f;   // seconds without timestamp change → stale
    public float gpsCheckInterval = 2f;         // monitor loop interval

    [Header("Simulate Error (Editor)")]
    public bool simulateGPSLost = false;

    // C# events — other systems subscribe instead of polling
    public event Action OnGPSReady;
    public event Action OnGPSLost;
    public event Action OnGPSRecovered;

    private Coroutine _monitorCoroutine;
    private double _lastLocationTimestamp = -1.0;
    private float _lastTimestampChangeRealtime = -1f;
    private bool _locationStale = false;
    private bool _modalShown = false;    // anti-spam guard

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        if (useMockGPS)
        {
            Latitude = mockLat;
            Longitude = mockLng;
            State = GPSState.Running;
            _locationStale = false;
            OnGPSReady?.Invoke();
            Debug.Log($"🧪 Mock GPS Active: {Latitude}, {Longitude}");
            _monitorCoroutine = StartCoroutine(MonitorGPSLoop());
            yield break;
        }

        yield return StartCoroutine(InitializeGPS());
    }

    // ──────────────────────────────────────────────────────────
    // INITIALIZATION
    // ──────────────────────────────────────────────────────────

    IEnumerator InitializeGPS()
    {
        State = GPSState.Requesting;

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.FineLocation))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.FineLocation);
            yield return new WaitForSeconds(1.5f);
        }

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.FineLocation))
        {
            State = GPSState.Lost;
            ShowGPSModal(BackActionTarget.GoToLogin);
            yield break;
        }
#endif

        yield return StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        State = GPSState.Starting;

        // Stop first if already running (safe for re-start)
        if (Input.location.status == LocationServiceStatus.Running ||
            Input.location.status == LocationServiceStatus.Failed)
        {
            Input.location.Stop();
            yield return new WaitForSeconds(0.5f);
        }

        Input.compass.enabled = true;
        Input.location.Start(3f, 1f);

        float timeout = 15f;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            UpdateLocationData();
            State = GPSState.Running;
            _locationStale = false;
            _modalShown = false;

            GeoMath.InvalidateCompassCache();
            OnGPSReady?.Invoke();
            Debug.Log("✅ GPS Ready!");

            if (_monitorCoroutine != null) StopCoroutine(_monitorCoroutine);
            _monitorCoroutine = StartCoroutine(MonitorGPSLoop());
        }
        else
        {
            State = GPSState.Lost;
            Debug.LogError("❌ GPS failed to start: " + Input.location.status);
            ShowGPSModal(BackActionTarget.GoToLogin);
        }
    }

    // ──────────────────────────────────────────────────────────
    // MONITOR LOOP  — replaces InvokeRepeating entirely
    // ──────────────────────────────────────────────────────────

    IEnumerator MonitorGPSLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(gpsCheckInterval);

            // ── MOCK PATH ──
            if (useMockGPS)
            {
                if (simulateGPSLost)
                {
                    if (State == GPSState.Running) HandleGPSLost();
                }
                else
                {
                    if (State == GPSState.Lost || State == GPSState.Recovering)
                        HandleGPSRestored();
                }
                continue;
            }

            // ── REAL DEVICE PATH ──
            var status = Input.location.status;
            if (!Input.location.isEnabledByUser)
            {
                if (State == GPSState.Running) HandleGPSLost();
                continue;
            }
            if (status != LocationServiceStatus.Running)
            {
                if (State == GPSState.Running) HandleGPSLost();
                continue;
            }

            // Detect stale location: timestamp unchanged for too long
            double currentTs = Input.location.lastData.timestamp;
            if (currentTs != _lastLocationTimestamp)
            {
                // Timestamp changed — location is fresh
                _lastLocationTimestamp = currentTs;
                _lastTimestampChangeRealtime = Time.realtimeSinceStartup;
                _locationStale = false;
            }
            else
            {
                float elapsed = Time.realtimeSinceStartup - _lastTimestampChangeRealtime;
                if (elapsed > staleLocationTimeout && !_locationStale)
                {
                    _locationStale = true;
                    Debug.LogWarning($"⚠️ GPS location stale for {elapsed:F0}s");
                    if (State == GPSState.Running) HandleGPSLost();
                    continue;
                }
            }

            // Accuracy check
            float accuracy = Input.location.lastData.horizontalAccuracy;
            if (accuracy > maxAllowedAccuracy)
            {
                // Bad accuracy — do NOT immediately lose; wait for next tick
                Debug.LogWarning($"⚠️ GPS accuracy poor: {accuracy:F0}m");
            }
            else
            {
                UpdateLocationData();
                if (State != GPSState.Running)
                    HandleGPSRestored();
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // STATE TRANSITIONS
    // ──────────────────────────────────────────────────────────

    void HandleGPSLost()
    {
        State = GPSState.Lost;
        OnGPSLost?.Invoke();
        Debug.LogWarning("❌ GPS Lost!");
        ShowGPSModal(BackActionTarget.GoToMain);
    }

    void HandleGPSRestored()
    {
        State = GPSState.Running;
        _locationStale = false;
        _modalShown = false;
        UpdateLocationData();
        GeoMath.InvalidateCompassCache();
        OnGPSRecovered?.Invoke();
        Debug.Log("✅ GPS Recovered!");

        SystemModalController.Instance?.HideModal();
    }

    void UpdateLocationData()
    {
        var data = Input.location.lastData;
        Latitude = data.latitude;
        Longitude = data.longitude;
        Accuracy = data.horizontalAccuracy;
        _lastLocationTimestamp = data.timestamp;
        _lastTimestampChangeRealtime = Time.realtimeSinceStartup;
    }

    // ──────────────────────────────────────────────────────────
    // PUBLIC API — called by SystemModalController.OnRetryClicked
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Restart GPS services. Safe to call multiple times.
    /// Modal hides automatically when GPS recovers (HandleGPSRestored).
    /// </summary>
    public void RequestRestart()
    {
        if (State == GPSState.Running && !_locationStale) return;
        if (State == GPSState.Recovering || State == GPSState.Starting) return;

        State = GPSState.Recovering;
        StopAllCoroutines();

        if (useMockGPS)
        {
            HandleGPSRestored();
            _monitorCoroutine = StartCoroutine(MonitorGPSLoop());
            return;
        }

        StartCoroutine(StartLocationService());
    }

    // ──────────────────────────────────────────────────────────
    // MODAL — anti-spam: only show once until state changes
    // ──────────────────────────────────────────────────────────

    void ShowGPSModal(BackActionTarget target)
    {
        if (_modalShown) return;
        _modalShown = true;
        SystemModalController.Instance?.ShowWarning(WarningType.GPS, target);
    }

    // ──────────────────────────────────────────────────────────
    // APP LIFECYCLE
    // ──────────────────────────────────────────────────────────

    void OnApplicationPause(bool paused)
    {
        if (useMockGPS) return;

        if (paused)
        {
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }
        }
        else
        {
            StartCoroutine(ResumeGPSCheck());
        }
    }

    IEnumerator ResumeGPSCheck()
    {
        yield return new WaitForSeconds(1f); // let OS settle

        if (Input.location.status != LocationServiceStatus.Running)
        {
            State = GPSState.Lost;
            RequestRestart();
        }
        else
        {
            UpdateLocationData();
            if (_monitorCoroutine == null)
                _monitorCoroutine = StartCoroutine(MonitorGPSLoop());
        }
    }
}
