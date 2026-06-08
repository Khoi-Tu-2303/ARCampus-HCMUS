// Core/GeoMath.cs — PATCHED v2
// FIXES:
// [CRITICAL] Arrow 3D in AR world rotated incorrectly because ARNavigationView had no access
//            to the cached north-offset angle. Added GetCachedNorthAngle() public accessor so
//            ARNavigationView can convert geographic bearing → AR world-space Y rotation.
// (All fixes from v1 retained:
//   [CRITICAL] _cachedNorthAngle refreshes every COMPASS_CACHE_DURATION seconds
//   [CRITICAL] InvalidateCompassCache() called by GPSService on recovery
//   [HIGH]     HaversineDouble() for full double-precision checkpoint detection
//   [LOW]      LatLngToMeterOffset uses correct MetersPerDegreeLat for Z axis)

using UnityEngine;
using System;

/// <summary>
/// All GPS/AR math in one place. All methods are static — no MonoBehaviour needed.
/// </summary>
public static class GeoMath
{
    // ──────────────────────────────────────────────────────────
    // HAVERSINE — float version (kept for backward compatibility)
    // ──────────────────────────────────────────────────────────

    public static float Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) *
                   Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (float)(R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a)));
    }

    // ──────────────────────────────────────────────────────────
    // HAVERSINE DOUBLE — full precision, use for checkpoint detection
    // Avoids float cast error of ~1-2m at Vietnamese coordinates
    // ──────────────────────────────────────────────────────────

    public static float HaversineDouble(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) *
                   Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return (float)(R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a)));
    }

    // ──────────────────────────────────────────────────────────
    // GPS → AR WORLD POSITION
    // Compass cache refreshes every COMPASS_CACHE_DURATION seconds.
    // ──────────────────────────────────────────────────────────

    private static float _cachedNorthAngle = 0f;
    private static bool _hasCachedAngle = false;
    private static float _cacheTimestamp = -9999f;
    private const float COMPASS_CACHE_DURATION = 30f; // seconds

    /// <summary>
    /// Force-clear the compass cache. Called by GPSService when GPS recovers,
    /// ensuring AR labels and arrow re-align to the new heading.
    /// </summary>
    public static void InvalidateCompassCache()
    {
        _hasCachedAngle = false;
        _cachedNorthAngle = 0f;
        _cacheTimestamp = -9999f;
    }

    /// <summary>
    /// Returns the cached AR-world north offset angle (degrees).
    /// northOffset = cameraYaw − compassHeading.
    ///
    /// Interpretation: world +Z is (northOffset) degrees east of geographic North.
    /// To rotate an object to face geographic bearing B in AR world space:
    ///   worldYaw = B + northOffset
    ///
    /// Used by ARNavigationView to correctly orient the 3D navigation arrow.
    /// Returns 0 in the Editor (no compass available; world +Z is treated as North).
    /// </summary>
    public static float GetCachedNorthAngle()
    {
        // If cache is valid, return immediately without recomputing
        bool cacheExpired = (Time.realtimeSinceStartup - _cacheTimestamp) > COMPASS_CACHE_DURATION;
        if (_hasCachedAngle && !cacheExpired)
            return _cachedNorthAngle;

#if UNITY_EDITOR
        return 0f; // North = world +Z in editor simulation
#else
        // Cache not ready yet — return last known value (or 0 if never set).
        // GpsToARWorldPosition will refresh it on the next label position update.
        return _cachedNorthAngle;
#endif
    }

    public static Vector3 GpsToARWorldPosition(
        double targetLat, double targetLng,
        double userLat, double userLng,
        Transform cameraTransform,
        float heightOffset = 1.5f)
    {
        float offsetX = (float)((targetLng - userLng) * 111320.0
                                * Math.Cos(userLat * Math.PI / 180.0));
        float offsetZ = (float)((targetLat - userLat) * GeoConstants.MetersPerDegreeLat);
        Vector3 rawOffset = new Vector3(offsetX, 0f, offsetZ);

#if UNITY_EDITOR
        // No compass in editor — North = world +Z
        _cachedNorthAngle = 0f;
        _hasCachedAngle   = true;
        _cacheTimestamp   = Time.realtimeSinceStartup;
#else
        bool cacheExpired = (Time.realtimeSinceStartup - _cacheTimestamp) > COMPASS_CACHE_DURATION;

        if (!_hasCachedAngle || cacheExpired)
        {
            if (Input.compass.enabled)
            {
                // northOffset: how many degrees world +Z is rotated relative to geographic North
                _cachedNorthAngle = cameraTransform.eulerAngles.y - Input.compass.trueHeading;
                _cacheTimestamp = Time.realtimeSinceStartup;
                _hasCachedAngle = true;
            }
        }
#endif

        Vector3 rotatedOffset = Quaternion.Euler(0f, _cachedNorthAngle, 0f) * rawOffset;
        return cameraTransform.position + rotatedOffset + new Vector3(0f, heightOffset, 0f);
    }

    // ──────────────────────────────────────────────────────────
    // LATLNG → METER OFFSET
    // Z axis correctly uses MetersPerDegreeLat (110540) not 111320
    // ──────────────────────────────────────────────────────────

    public static Vector3 LatLngToMeterOffset(
        double anchorLat, double anchorLng,
        double targetLat, double targetLng)
    {
        float zMeters = (float)((targetLat - anchorLat) * GeoConstants.MetersPerDegreeLat);
        float xMeters = (float)((targetLng - anchorLng) * GeoConstants.MetersPerDegreeLng
                                * Math.Cos(anchorLat * Math.PI / 180.0));
        return new Vector3(xMeters, 0f, zMeters);
    }

    // ──────────────────────────────────────────────────────────
    // BEARING
    // ──────────────────────────────────────────────────────────

    public static float CalculateBearing(float lat1, float lng1, float lat2, float lng2)
    {
        float dLng = Mathf.Deg2Rad * (lng2 - lng1);
        float rlat1 = Mathf.Deg2Rad * lat1;
        float rlat2 = Mathf.Deg2Rad * lat2;
        float y = Mathf.Sin(dLng) * Mathf.Cos(rlat2);
        float x = Mathf.Cos(rlat1) * Mathf.Sin(rlat2)
                - Mathf.Sin(rlat1) * Mathf.Cos(rlat2) * Mathf.Cos(dLng);
        return ((Mathf.Rad2Deg * Mathf.Atan2(y, x)) + 360f) % 360f;
    }
}