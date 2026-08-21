using UnityEngine;

public enum ResourceEnvironment { Sea, Land }
public enum CarthaginianResource { Salt, Iron, Oil }

[CreateAssetMenu(fileName = "CarthaginianResourceTower", menuName = "Carthage Defense/Resource Tower Definition")]
public class CarthaginianResourceDefinition : ScriptableObject
{
    public string buildingName = "Salt Extractor";
    [TextArea] public string description;
    public Sprite icon;
    public GameObject prefab;
    public ResourceEnvironment environment = ResourceEnvironment.Sea;
    [Tooltip("Leave empty to allow this building in any valid environment.")]
    public string requiredZoneId;
    public CarthaginianResource resource = CarthaginianResource.Salt;
    [Min(0)] public int buildCost = 75;
    [Min(1)] public int workersRequired = 2;
    [Min(1)] public int unitsPerCycle = 2;
    [Min(0.1f)] public float productionCycleSeconds = 10f;
    [Min(1)] public int sellValuePerUnit = 3;

    // What players actually spend/earn in — raw resource units/sec (salt, iron, oil...) means nothing to
    // them at a glance, but TND/sec is directly comparable to build cost and every other coin figure in the UI.
    public float IncomePerSecond => productionCycleSeconds > 0f ? unitsPerCycle * sellValuePerUnit / productionCycleSeconds : 0f;
}
