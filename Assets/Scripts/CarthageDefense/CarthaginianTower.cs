using System.Collections;
using UnityEngine;

/// <summary>Runtime tower progression. Each upgrade unlocks the ships configured for that level.</summary>
public class CarthaginianTower : MonoBehaviour
{
    [SerializeField] private CarthaginianTowerDefinition definition;
    [SerializeField] private int currentLevel;
    private Coroutine _bounceRoutine;
    private Vector3 _baseScale;
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
        ApplyVisual();
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
        ApplyVisual();
    }

    // Per-level model prefabs are dragged in as the tower's own children, in order — child 0 is the
    // Level I model, child 1 is Level II, and so on. Only the child matching currentLevel is shown; the
    // rest of that same index range is hidden. Any extra children beyond the level count (e.g. a
    // LighthouseSpawner's LaunchPoint) are left alone. Each child carries its own Outline component, so
    // no cross-refresh is needed here: a child's Outline caches its own renderer the moment it first
    // becomes active, which is exactly when SetActive(true) below fires it for the first time.
    private void ApplyVisual()
    {
        if (definition == null || definition.levels == null) return;
        int childCount = Mathf.Min(transform.childCount, definition.levels.Length);
        for (int i = 0; i < childCount; i++)
            transform.GetChild(i).gameObject.SetActive(i == currentLevel);
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
        ApplyVisual();
        SfxManager.Instance?.PlayTowerUpgraded();
        PlayUpgradeFx(currentLevel);
        PlayUpgradeBounce(currentLevel);
        return true;
    }

    // Spawns at the tower's own origin — Sidi Bou Said gets an especially big celebratory burst (15x)
    // since it's the one-off civic building; every other tower gets a strong but smaller one (5x).
    private void PlayUpgradeFx(int tier)
    {
        float scale = GetComponent<SidiBouSaidTower>() != null ? 15f : 5f;
        CombatFx.PlayUpgradeBurst(transform.position, tier, scale);
    }

    // Bigger and slightly longer at higher tiers — makes the top upgrade feel like the payoff it is,
    // instead of the exact same wobble every time.
    private void PlayUpgradeBounce(int tier)
    {
        if (_bounceRoutine == null) _baseScale = transform.localScale;
        else StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(BounceScale(tier));
    }

    private IEnumerator BounceScale(int tier)
    {
        float duration = .3f + tier * .04f;
        float bounceAmount = .14f + tier * .05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float bounce = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * bounceAmount;
            transform.localScale = _baseScale * (1f + bounce);
            yield return null;
        }
        transform.localScale = _baseScale;
        _bounceRoutine = null;
    }

    private bool CanRosterFillUpgradeCrew()
    {
        int available = 0;
        for (int rank = (int)NextUpgradeMinimumRank; rank <= (int)CrewRank.SacredBand; rank++) available += CrewRoster.Instance.GetAvailable((CrewRank)rank);
        return available >= NextUpgradeCrewRequired;
    }
}
