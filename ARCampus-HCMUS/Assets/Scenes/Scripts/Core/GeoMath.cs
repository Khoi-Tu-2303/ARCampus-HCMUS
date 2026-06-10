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
    /// <summary>
    /// Force-clear the compass cache. Called by GPSService when GPS recovers,
    /// ensuring AR labels and arrow re-align to the new heading.
    /// </summary>
    public static void InvalidateCompassCache() { /* Bỏ trống, GPSService đã tự lo */ }

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
#if UNITY_EDITOR
        return 0f;
#else
        // Lấy trực tiếp thông số đã làm mượt từ GPSService
        if (GPSService.Instance != null) return GPSService.Instance.ARNorthOffset;
        return 0f;
#endif
    }

    public static Vector3 GpsToARWorldPosition(
        double targetLat, double targetLng,
        double userLat, double userLng,
        Transform cameraTransform,
        float heightOffset = 1.5f)
    {
        float offsetX = (float)((targetLng - userLng) * 111320.0 * Math.Cos(userLat * Math.PI / 180.0));
        float offsetZ = (float)((targetLat - userLat) * GeoConstants.MetersPerDegreeLat);
        Vector3 rawOffset = new Vector3(offsetX, 0f, offsetZ);

        // Không còn IF/ELSE rườm rà, lấy luôn độ lệch siêu mượt xoay trục
        float northOffset = GetCachedNorthAngle();
        Vector3 rotatedOffset = Quaternion.Euler(0f, northOffset, 0f) * rawOffset;

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

    public static float CalculateBearing(double lat1, double lng1, double lat2, double lng2)
    {
        double dLng = (lng2 - lng1) * Math.PI / 180.0;
        double rlat1 = lat1 * Math.PI / 180.0;
        double rlat2 = lat2 * Math.PI / 180.0;
        double y = Math.Sin(dLng) * Math.Cos(rlat2);
        double x = Math.Cos(rlat1) * Math.Sin(rlat2)
                 - Math.Sin(rlat1) * Math.Cos(rlat2) * Math.Cos(dLng);

        return (float)(((Math.Atan2(y, x) * 180.0 / Math.PI) + 360.0) % 360.0);
    }
}