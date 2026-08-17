using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RomeShip", menuName = "Rome/RomeShip")]
public class RomeShip : ScriptableObject
{
    public string shipName;
    public string shipDescription;
    public Mesh mesh;
    [Tooltip("Deals 1.5x damage to the class it counters (Warship > Skirmisher > Heavy > Warship).")]
    public ShipCombatClass combatClass = ShipCombatClass.Warship;

    public int crewSize;
    [Min(0)] public int bounty = 10;
    public float attackPower;
    [Min(0.1f)] public float viewRange = 50f;
    [FormerlySerializedAs("range")]
    [Min(0.1f)] public float closeAttackRange = 10f;
    public bool hasLongRangeAttack;
    [Min(0.1f)] public float longRangeAttackRange = 30f;
    [Range(0f, 1f)] public float longRangeAttackChance = 0.5f;
    [Min(0.01f)] public float attackCooldown = 1f;
    public float speed;
    [Tooltip("Optional. If assigned, attacks spawn this and it travels to the target (auto-gets a RomanProjectile component if it doesn't have one). Leave empty for an instant hit with just an impact spark.")]
    public GameObject projectilePrefab;
}
