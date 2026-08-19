using UnityEngine;

/// <summary>Health receiver for Roman ships. SpawnManager configures it from RomeShip.crewSize.</summary>
public class RomanShipHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private float maxHealth = 10f;
    [SerializeField, Min(0)] private int bounty = 10;
    private float _currentHealth;
    private FloatingHealthBar _healthBar;

    public bool IsDestroyed => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = maxHealth;
        // Not creating the bar here on purpose: RomanShip.shipData (which carries the per-type
        // showHealthBar flag) isn't assigned yet at this point — SpawnManager calls AssignShip() before
        // Configure(), so Configure() is the first place that flag is reliably known.
    }

    public void Configure(float crewSize, int shipBounty)
    {
        maxHealth = Mathf.Max(1f, crewSize);
        bounty = Mathf.Max(0, shipBounty);
        _currentHealth = maxHealth;
        EnsureHealthBar();
    }

    // Only the ship type(s) with RomeShip.showHealthBar set get one — most waves have plenty of ships on
    // screen at once, and a bar over every single one would be pure clutter for anything but the notable
    // "big" ship.
    private void EnsureHealthBar()
    {
        RomanShip ship = GetComponent<RomanShip>();
        if (ship == null || ship.shipData == null || !ship.shipData.showHealthBar)
        {
            if (_healthBar != null) { Destroy(_healthBar.gameObject); _healthBar = null; }
            return;
        }
        if (_healthBar != null) { _healthBar.SetFraction(_currentHealth / maxHealth); return; }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float top = 1.2f;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            top = bounds.max.y - transform.position.y + .7f;
        }
        _healthBar = FloatingHealthBar.Attach(transform, top, .2f);
        _healthBar.SetFraction(_currentHealth / maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        if (_healthBar != null) _healthBar.SetFraction(_currentHealth / maxHealth);
        if (IsDestroyed)
        {
            CombatFx.PlayExplosion(transform.position, 1.3f);
            CameraShake.Shake(.4f);
            SfxManager.Instance?.PlayShipDestroyed();
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(bounty);
            if (bounty > 0)
            {
                FloatingCombatText.Spawn(transform.position, "+" + bounty, new Color(.35f, 1f, .3f));
                SfxManager.Instance?.PlayCoinGained();
            }
            Destroy(gameObject);
        }
    }
}
