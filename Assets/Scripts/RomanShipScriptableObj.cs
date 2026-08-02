using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RomeShip", menuName = "Rome/RomeShip")]
public class RomeShip : ScriptableObject
{
    public string shipName;
    public string shipDescription;
    public Mesh mesh;
    
    public int crewSize;
    public float attackPower;
    [Min(0.1f)] public float viewRange = 50f;
    [FormerlySerializedAs("range")]
    [Min(0.1f)] public float closeAttackRange = 10f;
    public bool hasLongRangeAttack;
    [Min(0.1f)] public float longRangeAttackRange = 30f;
    [Range(0f, 1f)] public float longRangeAttackChance = 0.5f;
    [Min(0.01f)] public float attackCooldown = 1f;
    public float speed;
}
