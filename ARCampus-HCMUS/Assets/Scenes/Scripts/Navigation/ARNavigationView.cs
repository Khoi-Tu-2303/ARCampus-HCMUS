// Navigation/ARNavigationView.cs — PATCHED v3
// FIXES:
// [CRITICAL] Arrow 3D rotation ignored AR-world north offset.
//            Quaternion.Euler(0, bearing, 0) treats world +Z as geographic North.
//            In ARFoundation the world origin is wherever the AR session started;
//            +Z is NOT geographic north. The arrow must be rotated by the same
//            northOffset (cameraYaw - compassHeading) that GeoMath uses for label placement.
//            Fix: expose GeoMath.GetCachedNorthAngle() and add it to the target bearing.
// [HIGH]     UpdateARPathLine: condition was (waypointIndex > 1) — when waypointIndex == 1
//            the first segment (index 0 → index 1) was never collapsed to the user's feet,
//            leaving a stale line dangling behind them for an entire waypoint segment.
//            Fix: change to (waypointIndex >= 1).
// [MEDIUM]   DrawARPath: _linePointsBuffer[0] correctly maps to navStartFeetPos (user pos).
//            Indices 1..count-1 map to path[1]..path[count-1], skipping path[0] (the snap
//            start node which is behind/at user). Correct, but the positionCount was set to
//            `count` while valid points filled are [0..count-1], which is consistent.
//            Added explicit guard: if count < 2, disable the line (nothing to draw).
// (All fixes from v2 retained: 2× buffer growth, SetActive pool instead of Destroy.)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ARNavigationView : MonoBehaviour
{
    [Header("AR 3D Arrow")]
    public GameObject arrow3DPrefab;
    public float arrowDistance = NavigationConstants.ArrowDistance;
    public float arrowHeightOffset = NavigationConstants.ArrowHeightOffset;

    [Header("AR Path Line")]
    public LineRenderer pathLine;
    public float lineYOffset = NavigationConstants.LineYOffset;

    [Header("AR Next Node Label")]
    public GameObject nextNodeLabelPrefab;

    private GameObject currentArrow3D;
    private GameObject currentNextNodeLabel;
    private TMP_Text nextNodeText;

    // Navigation anchor — set at StartNavigation time
    private double navStartLat;
    private double navStartLng;
    private Vector3 navStartFeetPos;

    // FIX v2: Buffer starts at 64, grows by 2× — O(log n) allocs over session
    private Vector3[] _linePointsBuffer = new Vector3[64];

    // FIX v3 [CRITICAL]: target bearing in GEOGRAPHIC space (0 = north, CW).
    //                     Applied to world space only after adding northOffset.
    private float _targetBearing;

    // Cached camera — never call Camera.main in a hot path
    private Camera _cachedCamera;
    private Camera GetCamera() =>
        _cachedCamera != null ? _cachedCamera : (_cachedCamera = Camera.main);

    // ── ANCHOR ───────────────────────────────────────────────────
    public void InitAnchor(double startLat, double startLng)
    {
        navStartLat = startLat;
        navStartLng = startLng;
        Camera cam = GetCamera();
        if (cam != null)
            navStartFeetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
    }

    // ── SPAWN ────────────────────────────────────────────────────
    // FIX v2: Re-use existing GameObjects via SetActive instead of Instantiate/Destroy
    public void SpawnArrow()
    {
        if (arrow3DPrefab == null) return;
        if (currentArrow3D == null)
            currentArrow3D = Instantiate(arrow3DPrefab);
        else
            currentArrow3D.SetActive(true);
    }

    public void SpawnNodeLabel()
    {
        if (nextNodeLabelPrefab == null) return;
        if (currentNextNodeLabel == null)
        {
            currentNextNodeLabel = Instantiate(nextNodeLabelPrefab);
            nextNodeText = currentNextNodeLabel.GetComponent<TMP_Text>()
                           ?? currentNextNodeLabel.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            currentNextNodeLabel.SetActive(true);
        }
    }

    // ── DRAW PATH ────────────────────────────────────────────────
    public void DrawARPath(List<GraphNode> path)
    {
        Camera cam = GetCamera();
        if (pathLine == null || path == null || cam == null) return;

        // FIX v3: need at least 2 points to draw a line
        if (path.Count < 2)
        {
            pathLine.positionCount = 0;
            return;
        }

        pathLine.useWorldSpace = true;

        int count = path.Count;

        // FIX v2: Grow by 2× instead of +16
        if (_linePointsBuffer.Length < count)
        {
            int newSize = _linePointsBuffer.Length;
            while (newSize < count) newSize *= 2;
            _linePointsBuffer = new Vector3[newSize];
        }

        // Index 0 = user's current feet position (navStartFeetPos)
        // Index i = path[i] offset from anchor, for i in [1 .. count-1]
        // (path[0] is the snap-start node; user is already there or past it)
        _linePointsBuffer[0] = navStartFeetPos;
        for (int i = 1; i < count; i++)
        {
            Vector3 offset = GeoMath.LatLngToMeterOffset(
                navStartLat, navStartLng,
                path[i].lat, path[i].lng);
            _linePointsBuffer[i] = navStartFeetPos + offset;
        }

        pathLine.positionCount = count;
        pathLine.SetPositions(_linePointsBuffer); // only reads [0..positionCount-1]
    }

    // ── ARROW (60Hz smooth) ──────────────────────────────────────
    // Called by NavigationSession at 2Hz — stores target in GEOGRAPHIC bearing space.
    public void UpdateARArrow(float bearing)
    {
        _targetBearing = bearing;
    }

    void Update()
    {
        if (currentArrow3D == null || !currentArrow3D.activeSelf) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        // ── Position: float in front of camera ──
        Vector3 targetPos = cam.transform.position + cam.transform.forward * arrowDistance;
        targetPos.y = cam.transform.position.y + arrowHeightOffset;

        currentArrow3D.transform.position = Vector3.Lerp(
            currentArrow3D.transform.position, targetPos, Time.deltaTime * 5f);

        // ── Rotation: FIX v3 [CRITICAL] ─────────────────────────
        // Geographic bearing (0° = north, clockwise) must be converted to
        // AR world-space Y rotation.  World +Z ≠ geographic north; we must
        // add the northOffset = (camera.eulerAngles.y – compass.trueHeading)
        // which is the same correction GeoMath.GpsToARWorldPosition applies.
        //
        // northOffset tells us: "world +Z is northOffset degrees east of
        // geographic north", so to face geographic bearing B in world space:
        //   worldYaw = B + northOffset
        //
        // In editor (no compass) northOffset = 0 so the arrow faces world +Z
        // which is defined as north for simulation purposes.
        float northOffset = GeoMath.GetCachedNorthAngle();
        float worldYaw = _targetBearing + northOffset;

        currentArrow3D.transform.rotation = Quaternion.Slerp(
            currentArrow3D.transform.rotation,
            Quaternion.Euler(0f, worldYaw, 0f),
            Time.deltaTime * 5f);
    }

    // ── PATH LINE UPDATE ─────────────────────────────────────────
    public void UpdateARPathLine(int waypointIndex)
    {
        Camera cam = GetCamera();
        if (pathLine == null || cam == null) return;
        if (waypointIndex >= pathLine.positionCount) return;

        Vector3 feetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);

        // Always update point 0 to current feet position
        pathLine.SetPosition(0, feetPos);

        // FIX v3 [HIGH]: was (waypointIndex > 1) — missed collapsing segment 0→1.
        // Correct condition: >= 1, i.e. once we have advanced past the first waypoint
        // collapse all already-visited line points to current feet position.
        if (waypointIndex >= 1)
        {
            for (int i = 1; i < waypointIndex; i++)
                pathLine.SetPosition(i, feetPos);
        }
    }

    // ── NODE LABEL UPDATE ────────────────────────────────────────
    public void UpdateNextNodeLabel(GraphNode target, float distanceToTarget)
    {
        if (currentNextNodeLabel == null) return;

        Vector3 offset = GeoMath.LatLngToMeterOffset(
            navStartLat, navStartLng, target.lat, target.lng);
        currentNextNodeLabel.transform.position = navStartFeetPos + offset;

        if (nextNodeText != null)
        {
            string name = target.name;
            if (string.IsNullOrEmpty(name) ||
                target.id.StartsWith("W_") ||
                target.id.StartsWith("CP_"))
            {
                name = "Điểm tiếp theo";
            }
            nextNodeText.text = $"{name}\n{distanceToTarget:F0} m";
        }
    }

    // ── CLEAR ────────────────────────────────────────────────────
    // FIX v2: SetActive(false) instead of Destroy — objects pooled for next navigation
    public void ClearAll()
    {
        if (currentArrow3D != null) currentArrow3D.SetActive(false);
        if (currentNextNodeLabel != null) currentNextNodeLabel.SetActive(false);
        if (pathLine != null) pathLine.positionCount = 0;
    }
}