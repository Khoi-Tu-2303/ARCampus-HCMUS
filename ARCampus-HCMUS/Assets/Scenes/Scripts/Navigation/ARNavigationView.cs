
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

    private List<GraphNode> _currentPath;
    private Vector3[] _linePointsBuffer = new Vector3[64];
    private Vector3[] _nodeWorldPositions;
    private int _currentWaypointIndex = 0;

    private Camera _cachedCamera;
    private Camera GetCamera() => _cachedCamera != null ? _cachedCamera : (_cachedCamera = Camera.main);

    public void InitAnchor(double startLat, double startLng) { }

    public void SpawnArrow()
    {
        if (arrow3DPrefab == null) return;
        if (currentArrow3D == null) currentArrow3D = Instantiate(arrow3DPrefab);
        else currentArrow3D.SetActive(true);
    }

    public void SpawnNodeLabel()
    {
        if (nextNodeLabelPrefab == null) return;
        if (currentNextNodeLabel == null)
        {
            currentNextNodeLabel = Instantiate(nextNodeLabelPrefab);
            nextNodeText = currentNextNodeLabel.GetComponent<TMP_Text>() ?? currentNextNodeLabel.GetComponentInChildren<TMP_Text>();
        }
        else currentNextNodeLabel.SetActive(true);
    }

    public void DrawARPath(List<GraphNode> path)
    {
        _currentPath = path;
        Camera cam = GetCamera();
        if (pathLine == null || path == null || cam == null) return;

        int count = path.Count;
        if (count < 2) { pathLine.positionCount = 0; return; }

        pathLine.useWorldSpace = true;

        if (_linePointsBuffer.Length < count)
        {
            int newSize = _linePointsBuffer.Length;
            while (newSize < count) newSize *= 2;
            _linePointsBuffer = new Vector3[newSize];
        }

        _nodeWorldPositions = new Vector3[count];

        double uLat = GPSService.Instance.Latitude;
        double uLng = GPSService.Instance.Longitude;

        for (int i = 0; i < count; i++)
        {
            _nodeWorldPositions[i] = GeoMath.GpsToARWorldPosition(
                path[i].lat, path[i].lng,
                uLat, uLng,
                cam.transform,
                lineYOffset
            );
        }

        pathLine.positionCount = count;
        UpdateARPathLine(0); 
    }

    public void UpdateARArrow(float bearing) { }

    void Update()
    {
        if (currentArrow3D == null || !currentArrow3D.activeSelf) return;
        Camera cam = GetCamera();

        if (cam == null || _nodeWorldPositions == null || _currentWaypointIndex >= _nodeWorldPositions.Length) return;

        Vector3 targetPos = cam.transform.position + cam.transform.forward * arrowDistance;
        targetPos.y = cam.transform.position.y + arrowHeightOffset;

        currentArrow3D.transform.position = Vector3.Lerp(
            currentArrow3D.transform.position, targetPos, Time.deltaTime * 5f);

        Vector3 dirToNode = _nodeWorldPositions[_currentWaypointIndex] - cam.transform.position;
        dirToNode.y = 0; 

        if (dirToNode.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToNode);
            currentArrow3D.transform.rotation = Quaternion.Slerp(
                currentArrow3D.transform.rotation,
                targetRot,
                Time.deltaTime * 5f);
        }
    }

    public void UpdateARPathLine(int waypointIndex)
    {
        _currentWaypointIndex = waypointIndex;
        Camera cam = GetCamera();
        if (pathLine == null || cam == null || _currentPath == null || _nodeWorldPositions == null) return;
        if (waypointIndex >= pathLine.positionCount) return;

        Vector3 feetPos = cam.transform.position + new Vector3(0, lineYOffset, 0);
        _linePointsBuffer[0] = feetPos;

        for (int i = 1; i < waypointIndex; i++)
        {
            _linePointsBuffer[i] = feetPos;
        }

        for (int i = waypointIndex; i < _currentPath.Count; i++)
        {
            _linePointsBuffer[i] = _nodeWorldPositions[i];
        }

        pathLine.SetPositions(_linePointsBuffer);
    }

    
    public void UpdateNextNodeLabel(GraphNode target, float distanceToTarget)
    {
        if (currentNextNodeLabel == null || _nodeWorldPositions == null || _currentWaypointIndex >= _nodeWorldPositions.Length) return;

        Camera cam = GetCamera();
        if (cam == null) return;

        Vector3 fixedNodePos = _nodeWorldPositions[_currentWaypointIndex];

        fixedNodePos.y = cam.transform.position.y + NavigationConstants.LabelFloatHeight;

        currentNextNodeLabel.transform.position = fixedNodePos;

        if (nextNodeText != null)
        {
            string name = target.name;
            if (string.IsNullOrEmpty(name) || target.id.StartsWith("W_") || target.id.StartsWith("CP_"))
                name = "Điểm tiếp theo";
            nextNodeText.text = $"{name}\n{distanceToTarget:F0} m";
        }
    }

    public void ClearAll()
    {
        if (currentArrow3D != null) currentArrow3D.SetActive(false);
        if (currentNextNodeLabel != null) currentNextNodeLabel.SetActive(false);
        if (pathLine != null) pathLine.positionCount = 0;
        _currentPath = null;
        _nodeWorldPositions = null;
    }
}
