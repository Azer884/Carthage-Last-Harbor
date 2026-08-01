using UnityEngine;
using UnityEngine.Splines;

public class RomanShip : MonoBehaviour
{
    public RomeShip shipData;

    private Mesh _mesh;
    private float _speed;

    private SplineAnimate _splineAnimate;
    private SplineContainer _pathContainer;
    private ICombatTarget _target;
    private bool _canLeaveSpline;
    private bool _preferTowers;
    private bool _useLongRangeAttack;
    private bool _isEngaging;
    private float _nextAttackTime;

    private const float TargetSearchInterval = 0.25f;
    private float _nextTargetSearchTime;

    private void Awake()
    {
        if (shipData != null)
        {
            AssignShip(shipData);
        }
    }

    public void AssignShip(RomeShip ship)
    {
        if (ship == null)
        {
            Debug.LogError("Ship data is not assigned!");
            return;
        }

        shipData = ship;
        _mesh = ship.mesh;
        _speed = ship.speed;
        _useLongRangeAttack = ship.hasLongRangeAttack && Random.value <= ship.longRangeAttackChance;
        
        _splineAnimate = GetComponent<SplineAnimate>();
        if (_splineAnimate == null)
        {
            Debug.LogError("SplineAnimate component is missing!");
            return;
        }

        _splineAnimate.MaxSpeed = _speed;
        _pathContainer = null;
        if (GameManger.Instance != null)
        {
            _pathContainer = GameManger.Instance.GetRandomPath();
        }
        else
        {
            Debug.LogError("GameManager instance is missing!");
        }

        if (_pathContainer != null)
        {
            _splineAnimate.Container = _pathContainer;
            _splineAnimate.Play();
        }

        if (_mesh == null)
            return;
        // Apply the mesh to the MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = _mesh;
        }
    }

    public void SetCombatDecision(bool canLeaveSpline, bool preferTowers)
    {
        _canLeaveSpline = canLeaveSpline;
        _preferTowers = preferTowers;
    }

    private void Update()
    {
        if (shipData == null || !_canLeaveSpline)
            return;

        if (_target == null || _target.IsDestroyed)
        {
            if (_isEngaging)
                ReturnToSpline();

            if (Time.time >= _nextTargetSearchTime)
            {
                _nextTargetSearchTime = Time.time + TargetSearchInterval;
                _target = FindBestTarget();
                if (_target != null)
                    BeginEngagement();
            }

            return;
        }

        EngageTarget();
    }

    private ICombatTarget FindBestTarget()
    {
        float detectionRange = _useLongRangeAttack ? shipData.longRangeAttackRange : shipData.closeAttackRange;
        ICombatTarget preferred = null;
        ICombatTarget fallback = null;
        float preferredDistance = float.MaxValue;
        float fallbackDistance = float.MaxValue;

        foreach (CarthaginianTarget candidate in FindObjectsByType<CarthaginianTarget>(FindObjectsSortMode.None))
            ConsiderTarget(candidate, detectionRange, ref preferred, ref preferredDistance, ref fallback, ref fallbackDistance);

        foreach (CartageHeart candidate in FindObjectsByType<CartageHeart>(FindObjectsSortMode.None))
            ConsiderTarget(candidate, detectionRange, ref preferred, ref preferredDistance, ref fallback, ref fallbackDistance);

        return preferred ?? fallback;
    }

    private void ConsiderTarget(ICombatTarget candidate, float detectionRange, ref ICombatTarget preferred, ref float preferredDistance, ref ICombatTarget fallback, ref float fallbackDistance)
    {
        if (candidate == null || candidate.IsDestroyed)
            return;

        float distance = Vector3.Distance(transform.position, candidate.TargetTransform.position);
        if (distance > detectionRange)
            return;

        bool matchesPreference = candidate.TargetType == (_preferTowers ? CarthaginianTargetType.Tower : CarthaginianTargetType.Ship);
        if (matchesPreference && distance < preferredDistance)
        {
            preferred = candidate;
            preferredDistance = distance;
        }
        else if (distance < fallbackDistance)
        {
            fallback = candidate;
            fallbackDistance = distance;
        }
    }

    private void BeginEngagement()
    {
        _isEngaging = true;
        _splineAnimate.Pause();
    }

    private void EngageTarget()
    {
        Vector3 targetPosition = _target.TargetTransform.position;
        float attackRange = _useLongRangeAttack ? shipData.longRangeAttackRange : shipData.closeAttackRange;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
            Face(targetPosition);
            return;
        }

        Face(targetPosition);
        if (Time.time < _nextAttackTime)
            return;

        _nextAttackTime = Time.time + shipData.attackCooldown;
        _target.TakeDamage(shipData.attackPower);
    }

    private void ReturnToSpline()
    {
        _isEngaging = false;
        _target = null;
        if (_pathContainer == null)
            return;

        Vector3 localPosition = _pathContainer.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(_pathContainer.Spline, localPosition, out _, out float normalizedTime);
        _splineAnimate.NormalizedTime = normalizedTime;
        _splineAnimate.Play();
    }

    private void Face(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (shipData == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shipData.closeAttackRange);
        if (shipData.hasLongRangeAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, shipData.longRangeAttackRange);
        }
    }
}
