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
    private bool _reachedPathEnd;
    private Vector3 _returnPoint;
    private float _returnNormalizedTime;
    private Quaternion _splineRotation;
    private float _nextAttackTime;

    private const float TargetSearchInterval = 0.25f;
    private float _nextTargetSearchTime;

    // 0 at the spawn point, 1 at the spline's end (by the heart) — lets defenders prioritize whichever
    // Roman ship is furthest along its route rather than just whichever happens to be physically nearest.
    public float PathProgress => _splineAnimate != null ? _splineAnimate.NormalizedTime : 0f;

    private void Awake()
    {
        if (shipData != null)
        {
            AssignShip(shipData);
        }
        if (GetComponent<ShipBuoyancy>() == null) gameObject.AddComponent<ShipBuoyancy>();
        ShipWakeTrail.Attach(gameObject);
    }

    private void OnDestroy()
    {
        if (_splineAnimate != null) _splineAnimate.Completed -= OnPathCompleted;
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
        // Must be Once (not the default Loop) so Completed actually fires — that's how this ship knows
        // it has reached the heart and should start attacking, rather than sailing straight through it.
        _splineAnimate.Loop = SplineAnimate.LoopMode.Once;
        _splineAnimate.Completed -= OnPathCompleted;
        _splineAnimate.Completed += OnPathCompleted;
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

    // Ships never break off toward the heart mid-journey — they only commit to attacking it once they've
    // actually arrived, so a long-range ship reliably gets to stand off and fire instead of the old
    // behaviour of sometimes lunging at the heart early from an inconsistent, half-arrived state.
    private void OnPathCompleted()
    {
        _reachedPathEnd = true;
    }

    private void Update()
    {
        if (shipData == null) return;

        if (_reachedPathEnd)
        {
            EngageHeart();
            return;
        }

        if (!_canLeaveSpline)
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

    private void EngageHeart()
    {
        if (CartageHeart.Instance == null || CartageHeart.Instance.IsDestroyed) return;
        _target = CartageHeart.Instance;
        EngageTarget();
    }

    // Never considers the heart — that's handled exclusively by EngageHeart() once the ship's path is
    // actually complete, so a ship can't snipe the heart early from mid-journey.
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
        Fire(_target, targetPosition);
    }

    // Spawns an actual travelling projectile when the ship data has one assigned; otherwise falls back to
    // an instant hit with an impact spark, same graceful-degradation pattern the Dragon's fireball uses.
    private void Fire(ICombatTarget target, Vector3 targetPosition)
    {
        float damage = shipData.attackPower * GetCounterMultiplier();
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        CombatFx.PlayImpactSpark(origin);
        SfxManager.Instance?.PlayShipAttack();

        if (shipData.projectilePrefab == null)
        {
            target.TakeDamage(damage);
            FloatingCombatText.Spawn(targetPosition, "-" + Mathf.CeilToInt(damage), new Color(1f, .82f, .25f));
            CombatFx.PlayImpactSpark(targetPosition);
            CameraShake.Shake(.08f);
            return;
        }

        Vector3 aim = targetPosition - origin;
        GameObject projectileObject = Instantiate(shipData.projectilePrefab, origin, aim.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(aim) : Quaternion.identity);
        RomanProjectile projectile = projectileObject.GetComponent<RomanProjectile>();
        if (projectile == null) projectile = projectileObject.AddComponent<RomanProjectile>();
        projectile.Launch(target, damage);
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
