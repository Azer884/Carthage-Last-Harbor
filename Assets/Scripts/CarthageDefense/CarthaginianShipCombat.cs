using UnityEngine;

/// <summary>Attach to each Carthaginian ship prefab. It patrols near its launch point and attacks Roman ships it sees.</summary>
[RequireComponent(typeof(CarthaginianShipCrew))]
public class CarthaginianShipCombat : MonoBehaviour
{
    [Header("Sailing")]
    [SerializeField, Min(0.1f)] private float sailSpeed = 5f;
    [SerializeField, Min(1f)] private float wanderRange = 25f;
    [SerializeField, Min(0.1f)] private float waypointReachedDistance = 1f;
    [Header("Combat")]
    [SerializeField, Min(1f)] private float sightRange = 30f;
    [SerializeField, Min(0.1f)] private float closeAttackRange = 8f;
    [SerializeField] private bool hasLongRangeAttack;
    [SerializeField, Min(0.1f)] private float longRangeAttackRange = 20f;
    [SerializeField, Min(0.1f)] private float attackDamage = 2f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 1f;

    private Vector3 _homePosition;
    private Vector3 _waypoint;
    private RomanShipHealth _target;
    private CarthaginianShipCrew _crew;
    private float _nextAttackTime;

    private void Awake()
    {
        _homePosition = transform.position;
        _crew = GetComponent<CarthaginianShipCrew>();
        ChooseWaypoint();
    }

    private void Update()
    {
        if (_crew.IsDestroyed) return;
        if (_target == null || _target.IsDestroyed) _target = FindTarget();
        if (_target != null) EngageTarget(); else Patrol();
    }

    private RomanShipHealth FindTarget()
    {
        RomanShipHealth closest = null;
        float closestDistance = sightRange;
        foreach (RomanShipHealth candidate in FindObjectsByType<RomanShipHealth>(FindObjectsSortMode.None))
        {
            if (candidate.IsDestroyed) continue;
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < closestDistance) { closest = candidate; closestDistance = distance; }
        }
        return closest;
    }

    private void EngageTarget()
    {
        float attackRange = hasLongRangeAttack ? longRangeAttackRange : closeAttackRange;
        Vector3 targetPosition = _target.transform.position;
        if (Vector3.Distance(transform.position, targetPosition) > attackRange)
        {
            SailTowards(targetPosition);
            return;
        }
        Face(targetPosition);
        if (Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + attackCooldown;
        _target.TakeDamage(attackDamage * _crew.DamageMultiplier);
    }

    private void Patrol()
    {
        if (Vector3.Distance(transform.position, _waypoint) <= waypointReachedDistance) ChooseWaypoint();
        SailTowards(_waypoint);
    }

    private void ChooseWaypoint()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRange;
        _waypoint = _homePosition + new Vector3(offset.x, 0f, offset.y);
    }

    private void SailTowards(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, sailSpeed * Time.deltaTime);
        Face(position);
    }

    private void Face(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, wanderRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = hasLongRangeAttack ? Color.red : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hasLongRangeAttack ? longRangeAttackRange : closeAttackRange);
    }
}
