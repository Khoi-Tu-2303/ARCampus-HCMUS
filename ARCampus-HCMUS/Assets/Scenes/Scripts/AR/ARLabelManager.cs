

using UnityEngine;
using System.Collections.Generic;

public class ARLabelManager : MonoBehaviour
{
    private struct LabelEntry
    {
        public Transform labelTransform;
        public float minHeight;
        public float maxHeight;
        public float nearDistance;
        public float farDistance;
        public Canvas canvas;
        public Renderer[] renderers;
    }

    private static ARLabelManager _instance;
    public static ARLabelManager Instance => _instance;

    private List<LabelEntry> _labels = new List<LabelEntry>(16);
    private Transform _camTransform;
    private float _updateTimer;
    private const float UPDATE_INTERVAL = 0.05f; 

    public bool isPausedByNav = false;

    void Awake() => _instance = this;

     
    

    public static void Register(Transform t, float minH, float maxH, float near, float far)
    {
        if (_instance == null) return;

        Canvas cv = t.GetComponentInChildren<Canvas>(true);
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);

        _instance._labels.Add(new LabelEntry
        {
            labelTransform = t,
            minHeight = minH,
            maxHeight = maxH,
            nearDistance = near,
            farDistance = far,
            canvas = cv,
            renderers = renderers
        });

        if (_instance.isPausedByNav)
        {
            if (cv != null) cv.enabled = false;
            foreach (var r in renderers) r.enabled = false;
        }
    }

    public static void Unregister(Transform t)
    {
        if (_instance == null) return;
        for (int i = _instance._labels.Count - 1; i >= 0; i--)
        {
            if (_instance._labels[i].labelTransform == t)
            {
                _instance._labels.RemoveAt(i);
                break;
            }
        }
    }


    void Start() => _camTransform = Camera.main?.transform;

    void LateUpdate()
    {
        if (isPausedByNav) return;

        if (_camTransform == null) { _camTransform = Camera.main?.transform; return; }

        _updateTimer += Time.deltaTime;
        if (_updateTimer < UPDATE_INTERVAL) return;
        _updateTimer = 0f;

        Vector3 camPos = _camTransform.position;

        for (int i = _labels.Count - 1; i >= 0; i--)
        {
            var e = _labels[i];
            if (e.labelTransform == null) { _labels.RemoveAt(i); continue; }

            Vector3 pos = e.labelTransform.position;
            float dx = pos.x - camPos.x;
            float dz = pos.z - camPos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            bool shouldBeVisible = dist >= e.nearDistance;

            if (e.canvas != null && e.canvas.enabled != shouldBeVisible)
                e.canvas.enabled = shouldBeVisible;

            if (e.renderers != null)
            {
                foreach (var r in e.renderers)
                {
                    if (r != null && r.enabled != shouldBeVisible)
                        r.enabled = shouldBeVisible;
                }
            }

            if (!shouldBeVisible) continue;

            float bx = camPos.x - pos.x;
            float bz = camPos.z - pos.z;
            if (bx * bx + bz * bz > 0.01f)
                e.labelTransform.rotation = Quaternion.LookRotation(new Vector3(-bx, 0f, -bz));

            float ratio = Mathf.Clamp01((dist - e.nearDistance) / (e.farDistance - e.nearDistance));
            pos.y = Mathf.Lerp(e.minHeight, e.maxHeight, ratio);
            e.labelTransform.position = pos;
        }
    }


    public void ToggleGrayLabels(bool isNavigating)
    {
        isPausedByNav = isNavigating;

        if (_labels == null) return;

        foreach (var entry in _labels)
        {
            if (entry.labelTransform == null) continue;

            if (entry.canvas != null)
                entry.canvas.enabled = !isNavigating;

            if (entry.renderers != null)
                foreach (var r in entry.renderers)
                    if (r != null) r.enabled = !isNavigating;
        }
    }
}
