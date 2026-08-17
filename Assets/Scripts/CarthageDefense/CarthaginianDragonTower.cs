using System;
using UnityEngine;

/// <summary>Attach alongside CarthaginianTower on the Dragon of Carthage. Unlike CarthaginianStationaryTower
/// (instant hitscan damage), the dragon launches a DragonFireball projectile at its target — each level
/// points at its own projectile prefab, so upgrading can change what it actually shoots (bigger fireball,
/// splash damage, etc.), not just the numbers.</summary>
[RequireComponent(typeof(CarthaginianTower))]
public class CarthaginianDragonTower : MonoBehaviour
{
    [Tooltip("Where fireballs spawn from. Leave empty to launch from just above the tower's own position.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Local-space offset (relative to the StationaryTowerPlace slot's own position/rotation) applied when DragonTowerPlacementController places this prefab, so the model can be nudged onto its exact spot on the slot.")]
    [SerializeField] private Vector3 placementOffset;
    [SerializeField] private DragonTowerLevel[] levels;
    private int _activeLevel;
    private RomanShipHealth _target;
    private float _nextAttackTime;
    public DragonTowerLevel ActiveStats => levels != null && levels.Length > 0 ? levels[Mathf.Clamp(_activeLevel, 0, levels.Length - 1)] : null;
    public Vector3 PlacementOffset => placementOffset;

    public void SetLevel(int level)
    {
        if (levels == null || levels.Length == 0) { _activeLevel = 0; return; }
        _activeLevel = Mathf.Clamp(level, 0, levels.Length - 1);
    }

    private void Update()
    {
        DragonTowerLevel stats = ActiveStats;
        if (stats == null) return;
        if (_target == null || _target.IsDestroyed || Vector3.Distance(transform.position, _target.transform.position) > stats.sightRange)
            _target = FindTarget(stats.sightRange);
        if (_target == null) return;
        Face(_target.transform.position);
        if (Vector3.Distance(transform.position, _target.transform.position) > stats.attackRange || Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + stats.attackCooldown;
        Fire(stats, _target);
    }

    private void Fire(DragonTowerLevel stats, RomanShipHealth target)
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 2f;
        if (stats.projectilePrefab == null)
        {
            // No projectile assigned yet: hit instantly so the tower still functions while art is missing.
            target.TakeDamage(stats.damage);
            FloatingCombatText.Spawn(target.transform.position, "-" + Mathf.CeilToInt(stats.damage), new Color(1f, .55f, .12f));
            CombatFx.PlayExplosion(target.transform.position);
            return;
        }
        Vector3 aim = target.transform.position - origin;
        GameObject projectileObject = Instantiate(stats.projectilePrefab, origin, aim.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(aim) : Quaternion.identity);
        DragonFireball fireball = projectileObject.GetComponent<DragonFireball>();
        if (fireball == null) fireball = projectileObject.AddComponent<DragonFireball>();
        fireball.Launch(target, stats.damage, stats.splashRadius, stats.projectileSpeed);
    }

    private RomanShipHealth FindTarget(float range)
    {
        RomanShipHealth closest = null; float closestDistance = range;
        foreach (RomanShipHealth candidate in FindObjectsByType<RomanShipHealth>(FindObjectsSortMode.None))
        {
            if (candidate.IsDestroyed) continue;
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < closestDistance) { closest = candidate; closestDistance = distance; }
        }
        return closest;
    }

    private void Face(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position; direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }
}

[Serializable]
public class DragonTowerLevel
{
    public string title = "Level I";
    [Min(1f)] public float sightRange = 40f;
    [Min(0.1f)] public float attackRange = 30f;
    [Min(0.1f)] public float damage = 10f;
    [Tooltip("0 = single-target hit. Above 0 damages every Roman ship within this radius of the impact point.")]
    [Min(0f)] public float splashRadius = 0f;
    [Min(0.05f)] public float attackCooldown = 2.5f;
    [Min(0.1f)] public float projectileSpeed = 16f;
    [Tooltip("The projectile this level fires. Needs (or will auto-get) a DragonFireball component. Give higher levels a bigger/different prefab to visually sell the upgrade.")]
    public GameObject projectilePrefab;
}
