using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CarthaginianTower", menuName = "Carthage Defense/Tower Definition")]
public class CarthaginianTowerDefinition : ScriptableObject
{
    [Header("Identity")]
    public string towerName = "Lighthouse of Baal Hammon";
    [TextArea] public string description;
    public Sprite icon;
    public TowerCategory category = TowerCategory.Attack;
    public GameObject prefab;
    [Min(0)] public int buildCost = 50;
    [Tooltip("Leave empty to allow placement anywhere in the sea.")]
    public string requiredZoneId;

    [Header("Progression")]
    public TowerLevel[] levels;
}

public enum TowerCategory { Attack, Defense }

[Serializable]
public class TowerLevel
{
    public string title = "Level I";
    [Min(1)] public int upgradeCost = 50;
    public CarthaginianShipOption[] unlockedShips;
}

[Serializable]
public class CarthaginianShipOption
{
    public string shipName = "Libyan Galley";
    public GameObject shipPrefab;
    [Min(0.1f)] public float spawnCooldown = 8f;
    [Min(1)] public int crewRequired = 8;
    public CrewRank minimumRank = CrewRank.Recruit;
}
