using UnityEngine;

/// <summary>Filled translucent disc showing a selected tower's or ship's sight range. Editor Gizmos can't
/// draw in the Game view at runtime, so TowerSelectionManager adds/enables this on whatever is selected.
/// Towers here carry large, often non-uniform scale (e.g. the Dragon's transform is scaled ~4x4x10) — a
/// disc parented under that would get stretched into an ellipse and sized completely wrong if it just used
/// a plain local-space radius. Instead the disc lives on a child object whose own scale is continuously set
/// to the inverse of the parent's lossy scale, cancelling it out, so the rendered circle is always exactly
/// `radius` world units regardless of what it's attached to or how that parent is currently scaled
/// (including mid-bounce during an upgrade animation).</summary>
public class SightRangeRing : MonoBehaviour
{
    private const int Segments = 64;
    private static Mesh _discMesh;
    private static Material _material;

    private Transform _disc;
    private MeshRenderer _discRenderer;
    private MaterialPropertyBlock _properties;
    private float _targetRadius;
    private float _displayRadius;
    private bool _hasDisplayRadius;
    private bool _visible;

    // Called every frame by TowerSelectionManager while something's selected — cheap, and it's how an
    // upgrade's new (larger) sight range gets picked up and animated to immediately instead of only
    // refreshing the next time the tower is reselected.
    public void Show(float radius, Color color)
    {
        _targetRadius = Mathf.Max(0.1f, radius);
        if (!_hasDisplayRadius) { _displayRadius = _targetRadius; _hasDisplayRadius = true; }
        EnsureDisc();
        if (_properties == null) _properties = new MaterialPropertyBlock();
        _properties.SetColor("_Color", color);
        _discRenderer.SetPropertyBlock(_properties);
        _visible = true;
        _disc.gameObject.SetActive(true);
        ApplyScaleCompensation();
    }

    public void Hide()
    {
        _visible = false;
        _hasDisplayRadius = false;
        if (_disc != null) _disc.gameObject.SetActive(false);
    }

    // Recomputed every frame (not just on Show) so the circle stays a true circle even while the parent's
    // scale is animating (the upgrade bounce) or while a ship rotates/moves, and so the radius itself can
    // smoothly grow/shrink toward whatever Show() was last called with.
    private void LateUpdate()
    {
        if (!_visible) return;
        float rate = Mathf.Max(_targetRadius, _displayRadius, 1f) * 2.5f;
        _displayRadius = Mathf.MoveTowards(_displayRadius, _targetRadius, Time.unscaledDeltaTime * rate);
        ApplyScaleCompensation();
    }

    private void ApplyScaleCompensation()
    {
        Vector3 parentScale = transform.lossyScale;
        _disc.localScale = new Vector3(SafeRadius(parentScale.x), 1f, SafeRadius(parentScale.z));
        // Setting world position directly (rather than a local offset) sidesteps the tower's own pivot
        // height entirely — towers like the Lighthouse have their pivot many units above the waterline —
        // so the ring always sits right at the water surface instead of hovering up near the model.
        _disc.position = new Vector3(transform.position.x, GetWaterSurfaceY() + .05f, transform.position.z);
    }

    private float SafeRadius(float axisScale) => Mathf.Abs(axisScale) > 0.0001f ? _displayRadius / axisScale : _displayRadius;

    private float GetWaterSurfaceY()
    {
        int seaMask = LayerMask.GetMask("Sea");
        if (seaMask != 0 && Physics.Raycast(transform.position + Vector3.up * 300f, Vector3.down, out RaycastHit hit, 600f, seaMask, QueryTriggerInteraction.Collide))
            return hit.point.y;
        return transform.position.y;
    }

    private void EnsureDisc()
    {
        if (_disc != null) return;
        GameObject discObject = new GameObject("Sight Range Disc");
        discObject.transform.SetParent(transform, false);
        discObject.AddComponent<MeshFilter>().sharedMesh = GetDiscMesh();
        _discRenderer = discObject.AddComponent<MeshRenderer>();
        _discRenderer.sharedMaterial = GetMaterial();
        _discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _discRenderer.receiveShadows = false;
        _discRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _disc = discObject.transform;
    }

    // Unit circle (radius 1) in the XZ plane — the child's own localScale bakes in both the actual world
    // radius and the parent-scale cancellation, so the mesh itself never needs regenerating per tower.
    private static Mesh GetDiscMesh()
    {
        if (_discMesh != null) return _discMesh;
        Vector3[] vertices = new Vector3[Segments + 1];
        int[] triangles = new int[Segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
        for (int i = 0; i < Segments; i++)
        {
            int next = i + 2 > Segments ? 1 : i + 2;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = next;
        }
        Mesh mesh = new Mesh { name = "SightRangeDisc" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _discMesh = mesh;
        return mesh;
    }

    // Sprites/Default renders both faces (no backface culling) and alpha-blends by default, so the disc
    // reads correctly from the top-down camera without needing a custom shader or a URP-specific one.
    private static Material GetMaterial()
    {
        if (_material != null) return _material;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        _material = new Material(shader) { name = "SightRangeDisc Material" };
        return _material;
    }
}
