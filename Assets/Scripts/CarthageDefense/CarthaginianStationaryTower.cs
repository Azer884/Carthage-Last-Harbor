using System;
using UnityEngine;

/// <summary>For non-spawning towers such as the Dragon of Carthage. It attacks Roman ships from its fixed position.</summary>
public class CarthaginianStationaryTower : MonoBehaviour
{
    [SerializeField] private StationaryTowerLevel[] levels;
    private int _activeLevel;
    private RomanShipHealth _target;
    private float _nextAttackTime;
    public StationaryTowerLevel ActiveStats => levels != null && levels.Length > 0 ? levels[Mathf.Clamp(_activeLevel, 0, levels.Length - 1)] : null;

    public void SetLevel(int level)
    {
        if (levels == null || levels.Length == 0) { _activeLevel = 0; return; }
        _activeLevel = Mathf.Clamp(level, 0, levels.Length - 1);
    }

    private void Update()
    {
        if (levels == null || levels.Length == 0) return;
        StationaryTowerLevel stats = levels[_activeLevel];
        if (_target == null || _target.IsDestroyed || Vector3.Distance(transform.position, _target.transform.position) > stats.sightRange)
            _target = FindTarget(stats.sightRange);
        if (_target == null) return;
        Face(_target.transform.position);
        if (Vector3.Distance(transform.position, _target.transform.position) > stats.attackRange || Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + stats.attackCooldown;
        _target.TakeDamage(stats.damage);
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
public class StationaryTowerLevel
{
    [Min(1f)] public float sightRange = 35f;
    [Min(0.1f)] public float attackRange = 25f;
    [Min(0.1f)] public float damage = 5f;
    [Min(0.05f)] public float attackCooldown = 1.5f;
}
