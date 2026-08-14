using UnityEngine;
using System.Collections.Generic;

/// <summary>Lighthouse towers dispatch ships only when Sidi Bou Said/bought crew can fill them.</summary>
public class LighthouseSpawner : MonoBehaviour
{
    [SerializeField] private Transform launchPoint;
    [SerializeField] private bool spawnAutomatically = true;
    private CarthaginianTower _tower;
    private float _nextSpawnTime;

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
        CarthaginianShipOption option = affordableShips[Random.Range(0, affordableShips.Count)];
        return TrySpawnShip(option);
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
        return true;
    }
}
