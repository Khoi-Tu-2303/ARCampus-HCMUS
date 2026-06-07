// Map/RouteRenderer2D.cs — PATCHED v2
// FIXES (Phase 6 — Memory & GC):
// [MEDIUM] DrawRoute() allocated a new List<Vector2> points on every call.
//          Fix: Pre-allocate _pointsBuffer (reused across calls, grown only when needed).
// [LOW]    activeRouteLines was cleared via foreach + ReturnLine which is correct,
//          but List.Clear() after the loop is now explicit (was relying on prior clear).
//          No behaviour change — just clarity.

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RouteRenderer2D : MonoBehaviour
{
    [Header("Route Drawing")]
    public GameObject routeLinePrefab;
    public RectTransform routeContainer;
    public RectTransform destinationPin;

    [Header("Dot Spacing")]
    public float dotSpacing = 30f;

    private List<RectTransform> activeRouteLines = new List<RectTransform>(64);
    private Queue<RectTransform> _linePool = new Queue<RectTransform>(64);

    // FIX: Reusable point buffer — no allocation per DrawRoute call.
    // Grows only when path count exceeds current capacity (rare after first large route).
    private Vector2[] _pointsBuffer = new Vector2[64];
    private int _pointsCount = 0;

    // ── POOL ────────────────────────────────────────────────────
    RectTransform GetLine()
    {
        if (_linePool.Count > 0)
        {
            var l = _linePool.Dequeue();
            l.gameObject.SetActive(true);
            return l;
        }
        return Instantiate(routeLinePrefab, routeContainer).GetComponent<RectTransform>();
    }

    void ReturnLine(RectTransform l)
    {
        l.gameObject.SetActive(false);
        _linePool.Enqueue(l);
    }

    // ── DRAW ─────────────────────────────────────────────────────
    public void DrawRoute(List<GraphNode> path)
    {
        ClearRoute();
        if (path == null || path.Count == 0 || routeLinePrefab == null || routeContainer == null) return;

        var map = MapController.Instance;
        if (map == null || !GPSService.Instance.IsReady) return;

        // FIX: Fill reusable buffer instead of allocating new List<Vector2>
        int needed = path.Count + 1; // user pos + all nodes
        if (_pointsBuffer.Length < needed)
            _pointsBuffer = new Vector2[needed + 16]; // grow with headroom

        _pointsBuffer[0] = map.GetLocalPositionFromGPS(
            GPSService.Instance.Latitude,
            GPSService.Instance.Longitude);

        for (int i = 0; i < path.Count; i++)
            _pointsBuffer[i + 1] = map.GetLocalPositionFromGPS(path[i].lat, path[i].lng);

        _pointsCount = needed;

        // ── Rải Đinh algorithm (dot spacing) ──
        Vector2 prefabSize = routeLinePrefab.GetComponent<RectTransform>().sizeDelta;
        float accumulated = 0f;

        for (int i = 0; i < _pointsCount - 1; i++)
        {
            Vector2 posA = _pointsBuffer[i];
            Vector2 posB = _pointsBuffer[i + 1];
            Vector2 dir = posB - posA;
            float segLen = dir.magnitude;
            Vector2 dirN = dir / segLen; // normalized without extra alloc

            while (accumulated < segLen)
            {
                Vector2 dotPos = posA + dirN * accumulated;

                var rect = GetLine();
                rect.anchoredPosition = dotPos;
                rect.sizeDelta = prefabSize;
                rect.localRotation = Quaternion.identity;

                activeRouteLines.Add(rect);
                accumulated += dotSpacing;
            }
            accumulated -= segLen; // carry remainder to next segment
        }

        // ── Destination pin ──
        if (destinationPin != null)
        {
            destinationPin.gameObject.SetActive(true);
            destinationPin.anchoredPosition =
                map.GetLocalPositionFromGPS(path[path.Count - 1].lat, path[path.Count - 1].lng);
            destinationPin.SetAsLastSibling();
        }

        if (map.blueDot != null) map.blueDot.SetAsLastSibling();
    }

    // ── CLEAR ────────────────────────────────────────────────────
    public void ClearRoute()
    {
        for (int i = 0; i < activeRouteLines.Count; i++)
        {
            if (activeRouteLines[i] != null) ReturnLine(activeRouteLines[i]);
        }
        activeRouteLines.Clear();

        if (destinationPin != null) destinationPin.gameObject.SetActive(false);
    }
}