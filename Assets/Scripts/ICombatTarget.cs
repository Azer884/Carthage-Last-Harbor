using UnityEngine;

public enum CarthaginianTargetType
{
    Ship,
    Tower
}

public interface ICombatTarget
{
    Transform TargetTransform { get; }
    CarthaginianTargetType TargetType { get; }
    bool IsDestroyed { get; }
    void TakeDamage(float damage);
}
