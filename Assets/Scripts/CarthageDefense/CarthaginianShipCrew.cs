using System;
using UnityEngine;

/// <summary>A ship's health is its living crew count. Higher ranks make its attacks stronger.</summary>
public class CarthaginianShipCrew : MonoBehaviour, ICombatTarget
{
    // Mercenaries fight a little worse than native Carthaginian crew of the same rank.
    private const float MercenaryQualityMultiplier = .85f;

    [SerializeField] private bool returnSurvivingCrewToRoster = true;
    private CrewCount[] _crew = new CrewCount[0];
    private float _health;
    private float _maxHealth;
    private FloatingHealthBar _healthBar;
    private CarthaginianTower _originTower;
    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => CarthaginianTargetType.Ship;
    public bool IsDestroyed => _health <= 0f;
    public int CrewNumber { get { int total = 0; foreach (CrewCount count in _crew) total += count.amount; return total; } }
    public void SetOriginTower(CarthaginianTower tower) => _originTower = tower;
    public float DamageMultiplier
    {
        get
        {
            if (_crew == null || _crew.Length == 0 || CrewNumber == 0) return 1f;
            float quality = 0f;
            foreach (CrewCount count in _crew)
            {
                float rankQuality = 1f + (int)count.rank * 0.15f;
                if (count.isMercenary) rankQuality *= MercenaryQualityMultiplier;
                quality += count.amount * rankQuality;
            }
            return quality / CrewNumber;
        }
    }

    public void AssignCrew(CrewCount[] crew)
    {
        _crew = crew ?? new CrewCount[0];
        _health = 0f;
        foreach (CrewCount count in _crew) _health += count.amount;
        _maxHealth = _health;
        EnsureHealthBar();
    }

    // This — not CarthaginianTarget — is a Carthaginian ship's real health authority, so the floating bar
    // belongs here, tracking actual crew count rather than a separate, unrelated fixed health value.
    private void EnsureHealthBar()
    {
        if (_healthBar != null) { _healthBar.SetFraction(_maxHealth > 0f ? _health / _maxHealth : 0f); return; }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float top = 1.2f;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            top = bounds.max.y - transform.position.y + .7f;
        }
        _healthBar = FloatingHealthBar.Attach(transform, top, .2f, new Color(.25f, .78f, .3f, .95f));
        _healthBar.SetFraction(1f);
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f) return;
        _health = Mathf.Max(0f, _health - damage);
        if (_healthBar != null) _healthBar.SetFraction(_maxHealth > 0f ? _health / _maxHealth : 0f);
        if (IsDestroyed)
        {
            CombatFx.PlayExplosion(transform.position, 1.3f);
            CameraShake.Shake(.4f);
            SfxManager.Instance?.PlayShipDestroyed();
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Frees the launching tower's ship-capacity slot regardless of how the ship met its end — sunk in
        // combat or scuttled by the player via TowerSelectionManager.
        if (_originTower != null) _originTower.UnregisterShip(this);
        if (!returnSurvivingCrewToRoster || IsDestroyed || CrewRoster.Instance == null) return;
        foreach (CrewCount count in _crew) CrewRoster.Instance.AddCrew(count.rank, count.amount, count.isMercenary);
    }
}
