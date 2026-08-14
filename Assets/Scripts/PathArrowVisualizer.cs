using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Draws directional arrow markers along the active enemy paths so players can read ship routes at a glance.</summary>
public class PathArrowVisualizer : MonoBehaviour
{
    [Header("Arrow Style")]
    [SerializeField] private Color pathColor = new Color(.95f, .72f, .18f, .8f);
    [SerializeField] private Color arrowColor = new Color(1f, .93f, .45f, .95f);
    [SerializeField, Min(.02f)] private float pathWidth = .12f;
    [SerializeField, Min(.02f)] private float arrowWidth = .08f;
    [SerializeField, Min(.5f)] private float arrowSpacing = 7f;
    [SerializeField, Min(12)] private int sampleCount = 80;
    [SerializeField, Min(.1f)] private float arrowSize = 1.4f;
    [SerializeField] private float heightOffset = .08f;

    private static Material _sharedMaterial;
    private bool _built;
    private readonly List<GameObject> _generated = new List<GameObject>();

    private void Start()
    {
        TryBuild();
    }

    private void Update()
    {
        if (!_built)
        {
            TryBuild();
        }
    }

    private void OnDisable()
    {
        Clear();
        _built = false;
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
        _generated.Add(root);

        LineRenderer pathLine = CreateLineRenderer(root.transform, pathColor, pathWidth, "Path Line");
        Vector3[] pathPoints = new Vector3[samples.Count];
        for (int i = 0; i < samples.Count; i++)
        {
            pathPoints[i] = samples[i].Position;
        }
        pathLine.positionCount = pathPoints.Length;
        pathLine.SetPositions(pathPoints);

        float totalLength = samples[samples.Count - 1].Distance;
        for (float distance = arrowSpacing; distance < totalLength; distance += arrowSpacing)
        {
            PathSample sample = SampleAtDistance(samples, distance);
            CreateArrowMarker(root.transform, sample.Position, sample.Tangent);
        }
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

    private void CreateArrowMarker(Transform parent, Vector3 position, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 forward = direction.normalized;
        Vector3 backward = -(forward * (arrowSize * .55f));
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized * (arrowSize * .38f);
        Vector3 tip = position + forward * arrowSize;
        Vector3 left = position + backward + side;
        Vector3 right = position + backward - side;

        GameObject marker = new GameObject("Arrow");
        marker.transform.SetParent(parent, false);
        _generated.Add(marker);

        LineRenderer markerLine = CreateLineRenderer(marker.transform, arrowColor, arrowWidth, "Arrow Line");
        markerLine.positionCount = 3;
        markerLine.SetPositions(new[] { left, tip, right });
    }

    private LineRenderer CreateLineRenderer(Transform parent, Color color, float width, string objectName)
    {
        GameObject lineObject = new GameObject(objectName, typeof(LineRenderer));
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
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
        for (int i = _generated.Count - 1; i >= 0; i--)
        {
            if (_generated[i] != null)
            {
                Destroy(_generated[i]);
            }
        }

        _generated.Clear();
    }

    private struct PathSample
    {
        public Vector3 Position;
        public Vector3 Tangent;
        public float Distance;
    }
}


