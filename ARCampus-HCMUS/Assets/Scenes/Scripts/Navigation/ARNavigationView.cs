// Navigation/ARNavigationView.cs — PATCHED v2
// FIXES (Phase 6 — GC & Memory):
// [LOW] _linePointsBuffer re-allocated with `new Vector3[count + 16]` on path length growth.
//       In practice this triggers on first navigation (default 64 slots may be enough),
//       but re-routing with a longer path mid-session creates a GC spike.
//       Fix: Grow buffer by 2× (not +16) — ensures O(log n) total allocations over the
//       session lifetime rather than O(n/16) linear growth.
// [LOW] ClearAll() called Destroy() on arrow/label — creating and destroying GameObjects
//       every navigation is wasteful. Fix: SetActive(false) instead of Destroy, and
//       re-use via SetActive(true) on next SpawnArrow()/SpawnNodeLabel().
//       (Requires arrow and label prefabs to be tolerant of re-activation — they are.)

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

    // FIX: Buffer starts at 64, grows by 2× — O(log n) allocs over session
    private Vector3[] _linePointsBuffer = new Vector3[64];

    // Decoupled bearing for smooth 60Hz arrow without 2Hz GPS stutter
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
    // FIX: Re-use existing GameObjects via SetActive instead of Instantiate/Destroy
    public void SpawnArrow()
    {
        if (arrow3DPrefab == null) return;
        if (currentArrow3D == null)
            currentArrow3D = Instantiate(arrow3DPrefab);
        else
            currentArrow3D.SetActive(true); // re-use from previous navigation
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
            currentNextNodeLabel.SetActive(true); // re-use
        }
    }

    // ── DRAW PATH ────────────────────────────────────────────────
    public void DrawARPath(List<GraphNode> path)
    {
        Camera cam = GetCamera();
        if (pathLine == null || path == null || cam == null) return;
        pathLine.useWorldSpace = true;

        int count = path.Count;

        // FIX: Grow by 2× instead of +16 to reduce alloc frequency on large campuses
        if (_linePointsBuffer.Length < count)
        {
            int newSize = _linePointsBuffer.Length;
            while (newSize < count) newSize *= 2;
            _linePointsBuffer = new Vector3[newSize];
        }

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
    // Called by NavigationSession at 2Hz — sets target only
    public void UpdateARArrow(float bearing)
    {
        _targetBearing = bearing;
    }

    void Update()
    {
        if (currentArrow3D == null || !currentArrow3D.activeSelf) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        Vector3 targetPos = cam.transform.position + cam.transform.forward * arrowDistance;
        targetPos.y = cam.transform.position.y + arrowHeightOffset;

        currentArrow3D.transform.position = Vector3.Lerp(
            currentArrow3D.transform.position, targetPos, Time.deltaTime * 5f);

        currentArrow3D.transform.rotation = Quaternion.Slerp(
            currentArrow3D.transform.rotation,
            Quaternion.Euler(0, _targetBearing, 0),
            Time.deltaTime * 5f);
    }

    // ── PATH LINE UPDATE ─────────────────────────────────────────
    public void UpdateARPathLine(int waypointIndex)
    {
        Camera cam = GetCamera();
        if (pathLine == null || waypointIndex >= pathLine.positionCount || cam == null) return;

        Vector3 feetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
        pathLine.SetPosition(0, feetPos);

        if (waypointIndex > 1)
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
    // FIX: SetActive(false) instead of Destroy — objects pooled for next navigation
    public void ClearAll()
    {
        if (currentArrow3D != null) currentArrow3D.SetActive(false);
        if (currentNextNodeLabel != null) currentNextNodeLabel.SetActive(false);
        if (pathLine != null) pathLine.positionCount = 0;
    }
}