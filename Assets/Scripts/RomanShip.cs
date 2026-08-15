using UnityEngine;
using UnityEngine.Splines;
using float3 = Unity.Mathematics.float3;

public class RomanShip : MonoBehaviour
{
    public RomeShip shipData;

    [Header("Separation")]
    [SerializeField, Min(0f)] private float separationRadius = 3.5f;
    [SerializeField, Min(0f)] private float separationStrength = 1.5f;

    private Mesh _mesh;
    private float _speed;

    private SplineAnimate _splineAnimate;
    private SplineContainer _pathContainer;
    private ICombatTarget _target;
    private bool _canLeaveSpline;
    private bool _preferTowers;
    private bool _useLongRangeAttack;
    private bool _isEngaging;
    private bool _isReturningToSpline;
    private bool _isAligningWithSpline;
    private Vector3 _returnPoint;
    private float _returnNormalizedTime;
    private Quaternion _splineRotation;
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

        if (_isReturningToSpline)
        {
            SailBackToSpline();
            return;
        }

        if (_isAligningWithSpline)
        {
            AlignWithSpline();
            return;
        }

        if (_target == null || _target.IsDestroyed)
        {
            if (_isEngaging)
            {
                ReturnToSpline();
                return;
            }

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
        float detectionRange = shipData.viewRange;
        ICombatTarget preferred = null;
        ICombatTarget fallback = null;
        float preferredDistance = float.MaxValue;
        float fallbackDistance = float.MaxValue;

        foreach (CarthaginianTarget candidate in FindObjectsByType<CarthaginianTarget>())
            ConsiderTarget(candidate, detectionRange, ref preferred, ref preferredDistance, ref fallback, ref fallbackDistance);

        foreach (CarthaginianShipCrew candidate in FindObjectsByType<CarthaginianShipCrew>())
            ConsiderTarget(candidate, detectionRange, ref preferred, ref preferredDistance, ref fallback, ref fallbackDistance);

        foreach (CartageHeart candidate in FindObjectsByType<CartageHeart>())
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
            Vector3 movePoint = targetPosition + ComputeSeparation() * separationStrength;
            transform.position = Vector3.MoveTowards(transform.position, movePoint, _speed * Time.deltaTime);
            Face(targetPosition);
            return;
        }

        Face(targetPosition);
        if (Time.time < _nextAttackTime)
            return;

        _nextAttackTime = Time.time + shipData.attackCooldown;
        float damage = shipData.attackPower * GetCounterMultiplier();
        _target.TakeDamage(damage);
        FloatingCombatText.Spawn(targetPosition, "-" + Mathf.CeilToInt(damage), new Color(1f, .82f, .25f));
    }

    private float GetCounterMultiplier()
    {
        CarthaginianShipCombat targetCombat = _target != null && _target.TargetTransform != null
            ? _target.TargetTransform.GetComponent<CarthaginianShipCombat>() : null;
        return targetCombat != null ? ShipCounterTable.GetDamageMultiplier(shipData.combatClass, targetCombat.CombatClass) : 1f;
    }

    // Keeps ships from converging onto the exact same point when several of them close on one target.
    private Vector3 ComputeSeparation()
    {
        Vector3 push = Vector3.zero;
        foreach (RomanShip other in FindObjectsByType<RomanShip>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            push += SeparationFrom(other.transform.position);
        }

        foreach (CarthaginianShipCrew other in FindObjectsByType<CarthaginianShipCrew>(FindObjectsSortMode.None))
        {
            if (other.IsDestroyed) continue;
            push += SeparationFrom(other.transform.position);
        }

        return push;
    }

    private Vector3 SeparationFrom(Vector3 otherPosition)
    {
        Vector3 offset = transform.position - otherPosition;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance <= 0.001f || distance >= separationRadius) return Vector3.zero;
        return offset.normalized * (separationRadius - distance);
    }

    private void ReturnToSpline()
    {
        _isEngaging = false;
        _target = null;
        if (_pathContainer == null)
            return;

        float3 localPosition = _pathContainer.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(_pathContainer.Spline, localPosition, out float3 localReturnPoint, out _returnNormalizedTime);
        _returnPoint = _pathContainer.transform.TransformPoint(localReturnPoint);
        _isReturningToSpline = true;
    }

    private void SailBackToSpline()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            _returnPoint,
            _speed * Time.deltaTime);
        Face(_returnPoint);

        if (Vector3.SqrMagnitude(transform.position - _returnPoint) > 0.0001f)
            return;

        _isReturningToSpline = false;
        SetSplineRotation();
        _isAligningWithSpline = true;
    }

    private void SetSplineRotation()
    {
        _pathContainer.Evaluate(_returnNormalizedTime, out _, out float3 localTangent, out _);
        Vector3 worldTangent = _pathContainer.transform.TransformDirection(localTangent);
        worldTangent.y = 0f;
        _splineRotation = worldTangent.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(worldTangent)
            : transform.rotation;
    }

    private void AlignWithSpline()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, _splineRotation, 6f * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, _splineRotation) > 1f)
            return;

        transform.rotation = _splineRotation;
        _isAligningWithSpline = false;
        _splineAnimate.NormalizedTime = _returnNormalizedTime;
        _splineAnimate.Play();
    }

    private void Face(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (shipData == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shipData.viewRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shipData.closeAttackRange);
        if (shipData.hasLongRangeAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, shipData.longRangeAttackRange);
        }
    }
}
