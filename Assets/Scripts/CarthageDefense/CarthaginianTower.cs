using UnityEngine;

/// <summary>Runtime tower progression. Each upgrade unlocks the ships configured for that level.</summary>
public class CarthaginianTower : MonoBehaviour
{
    [SerializeField] private CarthaginianTowerDefinition definition;
    [SerializeField] private int currentLevel;
    public CarthaginianTowerDefinition Definition => definition;
    public int CurrentLevel => currentLevel;
    public TowerLevel ActiveLevel => definition != null && definition.levels != null && definition.levels.Length > 0
        ? definition.levels[Mathf.Clamp(currentLevel, 0, definition.levels.Length - 1)] : null;
    public bool CanUpgrade => definition != null && definition.levels != null && currentLevel < definition.levels.Length - 1;
    public int NextUpgradeCost => CanUpgrade ? definition.levels[currentLevel + 1].upgradeCost : 0;

    private void Awake()
    {
        LighthouseSpawner lighthouse = GetComponent<LighthouseSpawner>();
        if (lighthouse != null) lighthouse.SetTower(this);
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
    }

    public void Initialize(CarthaginianTowerDefinition towerDefinition)
    {
        definition = towerDefinition;
        currentLevel = 0;
        LighthouseSpawner lighthouse = GetComponent<LighthouseSpawner>();
        if (lighthouse != null) lighthouse.SetTower(this);
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
    }

    public bool TryUpgrade()
    {
        if (definition == null || definition.levels == null || currentLevel >= definition.levels.Length - 1) return false;
        if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(NextUpgradeCost)) return false;
        currentLevel++;
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
        return true;
    }
}
