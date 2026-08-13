using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Blocks tower placement near a SplineContainer. This replaces manually placing many path colliders.</summary>
[RequireComponent(typeof(SplineContainer))]
public class EnemyPathPlacementBlocker : MonoBehaviour
{
    [SerializeField, Min(0f)] private float extraClearance = 0f;
    private SplineContainer _path;

    private void Awake() { _path = GetComponent<SplineContainer>(); }
    public bool IsBlocked(Vector3 worldPosition, float requestedClearance)
    {
        if (_path == null) _path = GetComponent<SplineContainer>();
        if (_path == null) return false;
        float3 localPosition = _path.transform.InverseTransformPoint(worldPosition);
        SplineUtility.GetNearestPoint(_path.Spline, localPosition, out float3 nearestLocal, out _);
        Vector3 nearestWorld = _path.transform.TransformPoint(nearestLocal);
        return Vector3.Distance(worldPosition, nearestWorld) <= requestedClearance + extraClearance;
    }

    private void OnDrawGizmosSelected()
    {
        if (_path == null) _path = GetComponent<SplineContainer>();
        if (_path == null) return;
        Gizmos.color = new Color(1f, .15f, .1f, .35f);
        const int samples = 80;
        for (int i = 0; i < samples; i++)
        {
            _path.Evaluate(i / (float)(samples - 1), out float3 point, out _, out _);
            Gizmos.DrawSphere(_path.transform.TransformPoint(point), .25f + extraClearance);
        }
    }
}
