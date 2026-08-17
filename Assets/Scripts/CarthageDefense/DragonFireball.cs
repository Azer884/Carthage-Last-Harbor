using UnityEngine;

/// <summary>Attach to (or is auto-added to) Dragon of Carthage projectile prefabs. Homes on its launch
/// target, then damages every Roman ship within splashRadius of the impact point — a plain single-target
/// fireball just uses a small default radius so it still connects if the ship drifted while it was inbound.</summary>
public class DragonFireball : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float defaultSpeed = 18f;
    [SerializeField, Min(0.05f)] private float impactDistance = 0.75f;
    [SerializeField, Min(0f)] private float rotationTrackSpeed = 10f;

    private RomanShipHealth _target;
    private Vector3 _lastKnownTargetPosition;
    private float _damage;
    private float _splashRadius;
    private float _speed;
    private bool _launched;

    public void Launch(RomanShipHealth target, float damage, float splashRadius, float speed)
    {
        _target = target;
        _lastKnownTargetPosition = target != null ? target.transform.position : transform.position + transform.forward * 10f;
        _damage = damage;
        _splashRadius = splashRadius;
        _speed = speed > 0f ? speed : defaultSpeed;
        _launched = true;
    }

    private void Update()
    {
        if (!_launched) return;
        Vector3 aimPoint = _target != null && !_target.IsDestroyed ? _target.transform.position : _lastKnownTargetPosition;
        _lastKnownTargetPosition = aimPoint;
        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationTrackSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, aimPoint, _speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, aimPoint) <= impactDistance) Impact();
    }

    private void Impact()
    {
        _launched = false;
        Vector3 impactPoint = transform.position;
        float radius = Mathf.Max(_splashRadius, impactDistance);
        foreach (RomanShipHealth ship in FindObjectsByType<RomanShipHealth>(FindObjectsSortMode.None))
        {
            if (ship.IsDestroyed || Vector3.Distance(ship.transform.position, impactPoint) > radius) continue;
            ship.TakeDamage(_damage);
            FloatingCombatText.Spawn(ship.transform.position, "-" + Mathf.CeilToInt(_damage), new Color(1f, .55f, .12f));
        }
        CombatFx.PlayExplosion(impactPoint, 1.2f);
        CameraShake.Shake(.15f);
        Destroy(gameObject);
    }
}
