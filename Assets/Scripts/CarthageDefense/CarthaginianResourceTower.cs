using UnityEngine;

/// <summary>Staffed extractor. It automatically sells its output and returns its workers when removed.</summary>
public class CarthaginianResourceTower : MonoBehaviour
{
    [SerializeField] private CarthaginianResourceDefinition definition;
    [SerializeField] private bool automaticallySell = true;
    [Tooltip("Cosmetic only — how far the selection panel's extraction-range ring is drawn. Production isn't actually limited to this radius.")]
    [SerializeField, Min(1f)] private float displayRange = 10f;
    private bool _isStaffed;
    private float _nextProductionTime;
    private int _storedUnits;

    public float DisplayRange => displayRange;
    public CarthaginianResourceDefinition Definition => definition;
    public bool IsStaffed => _isStaffed;
    public int StoredUnits => _storedUnits;

    private void Start() { TryStartProduction(); }
    public void Initialize(CarthaginianResourceDefinition resourceDefinition)
    {
        definition = resourceDefinition;
        TryStartProduction();
    }

    public bool TryStartProduction()
    {
        if (_isStaffed || definition == null || WorkerRoster.Instance == null) return false;
        if (!WorkerRoster.Instance.TryAssignWorkers(definition.workersRequired)) return false;
        _isStaffed = true;
        _nextProductionTime = Time.time + definition.productionCycleSeconds;
        return true;
    }

    private void Update()
    {
        if (!_isStaffed || definition == null || Time.time < _nextProductionTime) return;
        _nextProductionTime = Time.time + definition.productionCycleSeconds;
        _storedUnits += definition.unitsPerCycle;
        if (automaticallySell) SellStoredResources();
    }

    public int SellStoredResources()
    {
        if (_storedUnits <= 0 || definition == null || EconomyManager.Instance == null) return 0;
        int earned = _storedUnits * definition.sellValuePerUnit;
        _storedUnits = 0;
        EconomyManager.Instance.AddMoney(earned);
        FloatingCombatText.Spawn(transform.position, "+" + earned + " coin", new Color(.35f, 1f, .3f));
        SfxManager.Instance?.PlayCoinGained();
        return earned;
    }

    private void OnDestroy()
    {
        if (_isStaffed && definition != null && WorkerRoster.Instance != null)
            WorkerRoster.Instance.ReturnWorkers(definition.workersRequired);
    }
}
