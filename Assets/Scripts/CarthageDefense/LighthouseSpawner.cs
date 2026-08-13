using UnityEngine;

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
        CarthaginianShipOption option = level.unlockedShips[Random.Range(0, level.unlockedShips.Length)];
        _nextSpawnTime = Time.time + option.spawnCooldown;
        if (option.shipPrefab == null || !CrewRoster.Instance.TryAssignCrew(option.minimumRank, option.crewRequired, out CrewCount[] crew)) return false;
        Transform point = launchPoint != null ? launchPoint : transform;
        GameObject ship = Instantiate(option.shipPrefab, point.position, point.rotation);
        CarthaginianShipCrew shipCrew = ship.GetComponent<CarthaginianShipCrew>();
        if (shipCrew != null) shipCrew.AssignCrew(crew);
        return true;
    }
}
