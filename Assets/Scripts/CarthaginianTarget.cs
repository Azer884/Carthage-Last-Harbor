using UnityEngine;

/// <summary>
/// Add this to Carthaginian ships or defensive towers so Roman ships can find and attack them.
/// </summary>
public class CarthaginianTarget : MonoBehaviour, ICombatTarget
{
    [SerializeField] private CarthaginianTargetType targetType = CarthaginianTargetType.Ship;
    [SerializeField, Min(1f)] private float maxHealth = 10f;
    [SerializeField] private bool destroyGameObjectOnDeath = true;

    private float _currentHealth;
    private bool _isTargetable = true;
    private FloatingHealthBar _healthBar;

    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => targetType;
    public bool IsDestroyed => !_isTargetable || _currentHealth <= 0f;

    /// <summary>Used by placement ghosts so enemy ships never attack an unbuilt preview. Ghosts call this
    /// right after Instantiate (i.e. right after Awake already ran and made a health bar), so also tear
    /// down the bar here rather than only gating its creation.</summary>
    public void SetTargetable(bool targetable)
    {
        _isTargetable = targetable;
        if (!targetable && _healthBar != null) { Destroy(_healthBar.gameObject); _healthBar = null; }
    }

    private void Awake()
    {
        _currentHealth = maxHealth;
        // Placement ghosts are untargetable previews (see SetTargetable) — no health bar needed there.
        if (_isTargetable)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            float top = 1.5f;
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                top = bounds.max.y - transform.position.y + .4f;
            }
            _healthBar = FloatingHealthBar.Attach(transform, top, .08f);
            _healthBar.SetFraction(1f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        if (_healthBar != null) _healthBar.SetFraction(_currentHealth / maxHealth);
        if (IsDestroyed && destroyGameObjectOnDeath)
            Destroy(gameObject);
    }
}
