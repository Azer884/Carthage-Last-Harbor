using UnityEngine;

/// <summary>Runtime tower progression. Each upgrade unlocks the ships configured for that level.</summary>
public class CarthaginianTower : MonoBehaviour
{
    [SerializeField] private CarthaginianTowerDefinition definition;
    [SerializeField] private int currentLevel;
    [Tooltip("Uncheck for fixed civic buildings (e.g. Sidi Bou Said) that the player should never be able to sell off.")]
    [SerializeField] private bool sellable = true;
    public CarthaginianTowerDefinition Definition => definition;
    public bool Sellable => sellable;
    public void SetSellable(bool value) { sellable = value; }
    public int CurrentLevel => currentLevel;
    public TowerLevel ActiveLevel => definition != null && definition.levels != null && definition.levels.Length > 0
        ? definition.levels[Mathf.Clamp(currentLevel, 0, definition.levels.Length - 1)] : null;
    public bool CanUpgrade => definition != null && definition.levels != null && currentLevel < definition.levels.Length - 1;
    public int NextUpgradeCost => CanUpgrade ? definition.levels[currentLevel + 1].upgradeCost : 0;
    public int NextUpgradeCrewRequired => CanUpgrade ? definition.levels[currentLevel + 1].upgradeCrewRequired : 0;
    public CrewRank NextUpgradeMinimumRank => CanUpgrade ? definition.levels[currentLevel + 1].minimumUpgradeCrewRank : CrewRank.Recruit;
    public bool CanAffordUpgrade => CanUpgrade && EconomyManager.Instance != null && EconomyManager.Instance.Money >= NextUpgradeCost
        && (NextUpgradeCrewRequired == 0 || CrewRoster.Instance != null && CanRosterFillUpgradeCrew());

    private void Awake()
    {
        LighthouseSpawner lighthouse = GetComponent<LighthouseSpawner>();
        if (lighthouse != null) lighthouse.SetTower(this);
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
        SidiBouSaidTower sidiBouSaid = GetComponent<SidiBouSaidTower>();
        if (sidiBouSaid != null) sidiBouSaid.SetLevel(currentLevel);
        CarthaginianDragonTower dragonTower = GetComponent<CarthaginianDragonTower>();
        if (dragonTower != null) dragonTower.SetLevel(currentLevel);
    }

    public void Initialize(CarthaginianTowerDefinition towerDefinition)
    {
        definition = towerDefinition;
        currentLevel = 0;
        LighthouseSpawner lighthouse = GetComponent<LighthouseSpawner>();
        if (lighthouse != null) lighthouse.SetTower(this);
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
        SidiBouSaidTower sidiBouSaid = GetComponent<SidiBouSaidTower>();
        if (sidiBouSaid != null) sidiBouSaid.SetLevel(currentLevel);
        CarthaginianDragonTower dragonTower = GetComponent<CarthaginianDragonTower>();
        if (dragonTower != null) dragonTower.SetLevel(currentLevel);
    }

    public bool TryUpgrade()
    {
        if (definition == null || definition.levels == null || currentLevel >= definition.levels.Length - 1) return false;
        if (!CanAffordUpgrade || !EconomyManager.Instance.TrySpend(NextUpgradeCost)) return false;
        if (NextUpgradeCrewRequired > 0 && !CrewRoster.Instance.TryAssignCrew(NextUpgradeMinimumRank, NextUpgradeCrewRequired, out _))
        {
            EconomyManager.Instance.AddMoney(NextUpgradeCost);
            return false;
        }
        currentLevel++;
        CarthaginianStationaryTower stationaryTower = GetComponent<CarthaginianStationaryTower>();
        if (stationaryTower != null) stationaryTower.SetLevel(currentLevel);
        SidiBouSaidTower sidiBouSaid = GetComponent<SidiBouSaidTower>();
        if (sidiBouSaid != null) sidiBouSaid.SetLevel(currentLevel);
        CarthaginianDragonTower dragonTower = GetComponent<CarthaginianDragonTower>();
        if (dragonTower != null) dragonTower.SetLevel(currentLevel);
        return true;
    }

    private bool CanRosterFillUpgradeCrew()
    {
        int available = 0;
        for (int rank = (int)NextUpgradeMinimumRank; rank <= (int)CrewRank.SacredBand; rank++) available += CrewRoster.Instance.GetAvailable((CrewRank)rank);
        return available >= NextUpgradeCrewRequired;
    }
}
