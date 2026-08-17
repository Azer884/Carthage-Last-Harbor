using UnityEngine;

/// <summary>Attach to (or is auto-added to) Roman ship projectile prefabs (catapult stones, fire arrows,
/// etc). Homes on its launch target — any ICombatTarget, so it works whether a Roman ship is firing at a
/// Carthaginian ship/tower or at the heart — then applies damage and an impact spark on arrival.</summary>
public class RomanProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 14f;
    [SerializeField, Min(0.05f)] private float impactDistance = .6f;
    [SerializeField, Min(0f)] private float rotationTrackSpeed = 8f;

    private ICombatTarget _target;
    private Vector3 _lastKnownTargetPosition;
    private float _damage;
    private bool _launched;

    public void Launch(ICombatTarget target, float damage)
    {
        _target = target;
        _lastKnownTargetPosition = target != null ? target.TargetTransform.position : transform.position + transform.forward * 10f;
        _damage = damage;
        _launched = true;
    }

    private void Update()
    {
        if (!_launched) return;
        Vector3 aimPoint = _target != null && !_target.IsDestroyed ? _target.TargetTransform.position : _lastKnownTargetPosition;
        _lastKnownTargetPosition = aimPoint;
        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationTrackSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, aimPoint, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, aimPoint) <= impactDistance) Impact();
    }

    private void Impact()
    {
        _launched = false;
        if (_target != null && !_target.IsDestroyed)
        {
            _target.TakeDamage(_damage);
            FloatingCombatText.Spawn(transform.position, "-" + Mathf.CeilToInt(_damage), new Color(1f, .82f, .25f));
        }
        CombatFx.PlayImpactSpark(transform.position);
        CameraShake.Shake(.1f);
        Destroy(gameObject);
    }
}
