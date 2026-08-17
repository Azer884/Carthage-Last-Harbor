using UnityEngine;
using System.Collections.Generic;

/// <summary>Lighthouse towers dispatch ships only when Sidi Bou Said/bought crew can fill them.</summary>
public class LighthouseSpawner : MonoBehaviour
{
    [SerializeField] private Transform launchPoint;
    [SerializeField] private bool spawnAutomatically = true;
    [Tooltip("Cosmetic only — how far the selection panel's spawn-range ring is drawn. Ships aren't actually limited to launching within this radius.")]
    [SerializeField, Min(1f)] private float displayRange = 18f;
    private CarthaginianTower _tower;
    private float _nextSpawnTime;

    public float DisplayRange => displayRange;
    public void SetTower(CarthaginianTower tower) { _tower = tower; }
    private void Awake() { if (_tower == null) _tower = GetComponent<CarthaginianTower>(); }
    private void Update()
    {
        if (!spawnAutomatically || _tower == null || Time.time < _nextSpawnTime) return;
        TrySpawnShip();
    }

    public bool TrySpawnShip()
    {
        TowerLevel level = _tower.ActiveLevel;
        if (level == null || level.unlockedShips == null || level.unlockedShips.Length == 0 || CrewRoster.Instance == null) return false;
        List<CarthaginianShipOption> affordableShips = new List<CarthaginianShipOption>();
        foreach (CarthaginianShipOption candidate in level.unlockedShips)
            if (candidate != null && CanAfford(candidate)) affordableShips.Add(candidate);
        if (affordableShips.Count == 0) return false;

        // Try a random affordable option first (keeps the existing variety); if it can't actually be
        // crewed — not enough of the required rank on hand — fall back through the rest, weakest crew
        // requirement first, instead of spawning nothing just because the roll picked something too
        // demanding for the crew currently available. An upgraded dock should still launch *something*.
        CarthaginianShipOption preferred = affordableShips[Random.Range(0, affordableShips.Count)];
        if (TrySpawnShip(preferred)) return true;

        affordableShips.Remove(preferred);
        affordableShips.Sort((a, b) => a.crewRequired.CompareTo(b.crewRequired));
        foreach (CarthaginianShipOption fallback in affordableShips)
            if (TrySpawnShip(fallback)) return true;

        return false;
    }

    public bool CanAfford(CarthaginianShipOption option)
    {
        return option != null && EconomyManager.Instance != null && EconomyManager.Instance.Money >= option.shipCost;
    }

    public bool TrySpawnShip(CarthaginianShipOption option)
    {
        if (option == null || !CanAfford(option) || CrewRoster.Instance == null) return false;
        _nextSpawnTime = Time.time + option.spawnCooldown;
        if (option.shipPrefab == null || !CrewRoster.Instance.TryAssignCrew(option.minimumRank, option.crewRequired, out CrewCount[] crew)) return false;
        if (!EconomyManager.Instance.TrySpend(option.shipCost))
        {
            foreach (CrewCount crewCount in crew) CrewRoster.Instance.AddCrew(crewCount.rank, crewCount.amount);
            return false;
        }
        Transform point = launchPoint != null ? launchPoint : transform;
        GameObject ship = Instantiate(option.shipPrefab, point.position, point.rotation);
        CarthaginianShipCrew shipCrew = ship.GetComponent<CarthaginianShipCrew>();
        if (shipCrew != null) shipCrew.AssignCrew(crew);
        SpawnPopEffect.Apply(ship);
        SfxManager.Instance?.PlayShipSpawned();
        if (option.shipCost > 0)
        {
            FloatingCombatText.Spawn(point.position, "-" + option.shipCost, new Color(1f, .35f, .3f));
            SfxManager.Instance?.PlayCoinSpent();
        }
        return true;
    }
}
