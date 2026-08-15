using UnityEngine;

/// <summary>Attach at runtime to newly placed towers or spawned ships: pops in from zero scale with a smoke puff.</summary>
public class SpawnPopEffect : MonoBehaviour
{
    [SerializeField] private float duration = .3f;
    private Vector3 _targetScale;
    private float _elapsed;

    public static void Apply(GameObject target)
    {
        if (target == null) return;
        target.AddComponent<SpawnPopEffect>();
        CombatFx.PlaySmokePuff(target.transform.position);
    }

    private void Awake()
    {
        _targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
        t = 1f - (1f - t) * (1f - t);
        transform.localScale = _targetScale * t;
        if (t < 1f) return;
        transform.localScale = _targetScale;
        Destroy(this);
    }
}
