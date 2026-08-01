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

    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => targetType;
    public bool IsDestroyed => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        if (IsDestroyed && destroyGameObjectOnDeath)
            Destroy(gameObject);
    }
}
