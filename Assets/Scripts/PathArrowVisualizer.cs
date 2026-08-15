using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Draws directional arrow markers along the active enemy paths so players can read ship routes at a glance.</summary>
public class PathArrowVisualizer : MonoBehaviour
{
    [Header("Arrow Style")]
    [SerializeField] private Color arrowColor = new Color(1f, .93f, .45f, .95f);
    [SerializeField, Min(.02f)] private float arrowWidth = .08f;
    [SerializeField, Min(.5f)] private float arrowSpacing = 7f;
    [SerializeField, Min(12)] private int sampleCount = 80;
    [SerializeField, Min(.1f)] private float arrowSize = 1.4f;
    [SerializeField, Min(.1f)] private float arrowTravelSpeed = 2.5f;
    [SerializeField, Min(1)] private int minimumArrowsPerPath = 3;
    [SerializeField] private Vector3 defaultArrowOffset;
    [SerializeField] private PathOffset[] pathOffsets;
    [SerializeField] private float heightOffset;

    private static Material _sharedMaterial;
    private bool _built;
    private bool _isVisible = true;
    private readonly List<PathVisual> _pathVisuals = new List<PathVisual>();

    private void Start()
    {
        TryBuild();
    }

    private void Update()
    {
        if (!_built)
        {
            TryBuild();
            return;
        }

        bool shouldShow = GameManger.Instance == null || !GameManger.Instance.IsWaveRunning;
        if (_isVisible != shouldShow)
        {
            SetVisualsVisible(shouldShow);
        }

        if (!_isVisible)
        {
            return;
        }

        UpdateArrows();
    }

    private void OnDisable()
    {
        Clear();
        _built = false;
        _isVisible = true;
    }

    private void TryBuild()
    {
        if (_built || GameManger.Instance == null)
        {
            return;
        }

        Clear();
        foreach (GameObject pathObject in GameManger.Instance.GetPathObjects())
        {
            if (pathObject == null)
            {
                continue;
            }

            SplineContainer spline = pathObject.GetComponent<SplineContainer>();
            if (spline == null)
            {
                continue;
            }

            CreatePathVisual(pathObject.name, spline);
        }

        _built = true;
    }

    private void CreatePathVisual(string pathName, SplineContainer spline)
    {
        List<PathSample> samples = SampleSpline(spline);
        if (samples.Count < 2)
        {
            return;
        }

        GameObject root = new GameObject(pathName + " Path Arrows");
        root.transform.SetParent(transform, false);

        float totalLength = samples[samples.Count - 1].Distance;
        int arrowCount = Mathf.Max(minimumArrowsPerPath, Mathf.CeilToInt(totalLength / Mathf.Max(arrowSpacing, 0.01f)));
        List<PathArrowInstance> arrows = new List<PathArrowInstance>(arrowCount);
        float step = totalLength / arrowCount;
        for (int i = 0; i < arrowCount; i++)
        {
            float baseDistance = i * step;
            LineRenderer markerLine = CreateArrowMarker(root.transform, "Arrow " + (i + 1));
            markerLine.sortingOrder = 1;
            arrows.Add(new PathArrowInstance { Line = markerLine, BaseDistance = baseDistance });
        }

        _pathVisuals.Add(new PathVisual { Root = root, Samples = samples, TotalLength = totalLength, Arrows = arrows, Offset = GetOffsetForPath(pathName) });
    }

    private List<PathSample> SampleSpline(SplineContainer spline)
    {
        List<PathSample> samples = new List<PathSample>(sampleCount + 1);
        Vector3 previous = Vector3.zero;
        float totalDistance = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            spline.Evaluate(t, out float3 point, out float3 tangent, out _);
            Vector3 worldPoint = spline.transform.TransformPoint(point) + Vector3.up * heightOffset;
            Vector3 worldTangent = spline.transform.TransformDirection(tangent);
            worldTangent.y = 0f;
            if (i > 0)
            {
                totalDistance += Vector3.Distance(previous, worldPoint);
            }

            samples.Add(new PathSample
            {
                Position = worldPoint,
                Tangent = worldTangent.sqrMagnitude > 0.0001f ? worldTangent.normalized : spline.transform.forward,
                Distance = totalDistance
            });

            previous = worldPoint;
        }

        return samples;
    }

    private PathSample SampleAtDistance(List<PathSample> samples, float distance)
    {
        if (distance <= 0f)
        {
            return samples[0];
        }

        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].Distance < distance)
            {
                continue;
            }

            PathSample previous = samples[i - 1];
            PathSample next = samples[i];
            float span = Mathf.Max(next.Distance - previous.Distance, 0.0001f);
            float t = Mathf.Clamp01((distance - previous.Distance) / span);
            return new PathSample
            {
                Position = Vector3.Lerp(previous.Position, next.Position, t),
                Tangent = Vector3.Slerp(previous.Tangent, next.Tangent, t).normalized,
                Distance = distance
            };
        }

        return samples[samples.Count - 1];
    }

    private LineRenderer CreateArrowMarker(Transform parent, string objectName)
    {
        GameObject marker = new GameObject(objectName);
        marker.transform.SetParent(parent, false);
        return CreateLineRenderer(marker.transform, arrowColor, arrowWidth, "Arrow Line", false);
    }

    private void UpdateArrows()
    {
        float time = Time.time * arrowTravelSpeed;
        foreach (PathVisual visual in _pathVisuals)
        {
            if (visual == null || visual.Arrows == null || visual.Arrows.Count == 0)
            {
                continue;
            }

            for (int i = 0; i < visual.Arrows.Count; i++)
            {
                PathArrowInstance arrow = visual.Arrows[i];
                if (arrow.Line == null)
                {
                    continue;
                }

                float distance = Mathf.Repeat(arrow.BaseDistance + time, visual.TotalLength);
                PathSample sample = SampleAtDistance(visual.Samples, distance);
                SetArrowGeometry(arrow, sample.Position + visual.Offset, sample.Tangent);
            }
        }
    }

    private Vector3 GetOffsetForPath(string pathName)
    {
        if (pathOffsets != null)
        {
            for (int i = 0; i < pathOffsets.Length; i++)
            {
                if (pathOffsets[i].Matches(pathName))
                {
                    return pathOffsets[i].offset;
                }
            }
        }

        return defaultArrowOffset;
    }

    private void SetArrowGeometry(PathArrowInstance arrow, Vector3 position, Vector3 direction)
    {
        if (arrow == null || arrow.Line == null)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Transform markerTransform = arrow.Line.transform.parent;
        markerTransform.position = position;
        markerTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        Vector3 tip = new Vector3(0f, 0f, arrowSize * .5f);
        Vector3 rear = new Vector3(0f, 0f, -arrowSize * .5f);
        Vector3 side = Vector3.right * (arrowSize * .35f);

        arrow.Line.positionCount = 3;
        arrow.Line.SetPositions(new[] { rear + side, tip, rear - side });
    }

    private LineRenderer CreateLineRenderer(Transform parent, Color color, float width, string objectName, bool useWorldSpace = true)
    {
        GameObject lineObject = new GameObject(objectName, typeof(LineRenderer));
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        line.useWorldSpace = useWorldSpace;
        line.alignment = LineAlignment.View;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.positionCount = 0;
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = GetSharedMaterial();
        return line;
    }

    private void SetVisualsVisible(bool visible)
    {
        _isVisible = visible;
        for (int i = 0; i < _pathVisuals.Count; i++)
        {
            if (_pathVisuals[i] == null || _pathVisuals[i].Root == null)
            {
                continue;
            }

            _pathVisuals[i].Root.SetActive(visible);
        }
    }

    private Material GetSharedMaterial()
    {
        if (_sharedMaterial != null)
        {
            return _sharedMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        _sharedMaterial = new Material(shader) { name = "PathArrowVisualizer Material" };
        return _sharedMaterial;
    }

    private void Clear()
    {
        for (int i = _pathVisuals.Count - 1; i >= 0; i--)
        {
            PathVisual visual = _pathVisuals[i];
            if (visual != null)
            {
                if (visual.Root != null)
                {
                    Destroy(visual.Root);
                }
            }
        }

        _pathVisuals.Clear();
    }

    private class PathVisual
    {
        public GameObject Root;
        public List<PathSample> Samples;
        public float TotalLength;
        public List<PathArrowInstance> Arrows;
        public Vector3 Offset;
    }

    [System.Serializable]
    private struct PathOffset
    {
        public string pathName;
        public Vector3 offset;

        public bool Matches(string otherPathName)
        {
            return !string.IsNullOrWhiteSpace(pathName) && string.Equals(pathName, otherPathName, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private class PathArrowInstance
    {
        public LineRenderer Line;
        public float BaseDistance;
    }

    private struct PathSample
    {
        public Vector3 Position;
        public Vector3 Tangent;
        public float Distance;
    }
}


