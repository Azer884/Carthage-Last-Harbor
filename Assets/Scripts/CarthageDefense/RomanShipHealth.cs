using UnityEngine;

/// <summary>Health receiver for Roman ships. SpawnManager configures it from RomeShip.crewSize.</summary>
public class RomanShipHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private float maxHealth = 10f;
    [SerializeField, Min(0)] private int bounty = 10;
    private float _currentHealth;

    public bool IsDestroyed => _currentHealth <= 0f;

    private void Awake() { _currentHealth = maxHealth; }

    public void Configure(float crewSize, int shipBounty)
    {
        maxHealth = Mathf.Max(1f, crewSize);
        bounty = Mathf.Max(0, shipBounty);
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        if (IsDestroyed)
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(bounty);
            Destroy(gameObject);
        }
    }
}
