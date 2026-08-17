using System;
using UnityEngine;

[Serializable]
public class MercenaryOffer
{
    public string regionName = "Libya";
    public string troopName = "Libyan Levies";
    public CrewRank rank = CrewRank.Recruit;
    [Min(1)] public int basePrice = 8;
    [Min(0)] public int maxStock = 12;
    [NonSerialized] public int currentStock;
}

/// <summary>Sells foreign crew for money, or — when the player can't afford it — rents them against debt.
/// Stock re-rolls every wave and can come up empty, since Carthage didn't always find willing mercenaries
/// at the docks. Only Recruit/Sailor/Veteran are on offer; the Sacred Band isn't for hire.</summary>
public class MercenaryMarket : MonoBehaviour
{
    public static MercenaryMarket Instance { get; private set; }

    [SerializeField] private MercenaryOffer[] offers =
    {
        new MercenaryOffer { regionName = "Libya", troopName = "Libyan Levies", rank = CrewRank.Recruit, basePrice = 8, maxStock = 12 },
        new MercenaryOffer { regionName = "Iberia", troopName = "Iberian Mercenaries", rank = CrewRank.Sailor, basePrice = 22, maxStock = 6 },
        new MercenaryOffer { regionName = "Gaul", troopName = "Gallic Mercenaries", rank = CrewRank.Veteran, basePrice = 55, maxStock = 3 },
    };
    [SerializeField, Range(1f, 3f)] private float rentPriceMultiplier = 1.6f;

    private int _lockedUntilWaveIndex = -1;

    public MercenaryOffer[] Offers => offers;
    public event Action MarketChanged;

    public static MercenaryMarket Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Mercenary Market").AddComponent<MercenaryMarket>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RestockAll();
    }

    private void Start()
    {
        // Subscribing here (not Awake) guarantees GameManger.Instance is already set, regardless of
        // script execution order or whether this object was created at runtime mid-Awake elsewhere.
        if (GameManger.Instance != null) GameManger.Instance.WaveStarted += OnWaveStarted;
    }

    private void OnDestroy()
    {
        if (GameManger.Instance != null) GameManger.Instance.WaveStarted -= OnWaveStarted;
        if (Instance == this) Instance = null;
    }

    private void OnWaveStarted(int waveIndex)
    {
        RestockAll();
    }

    public void RestockAll()
    {
        foreach (MercenaryOffer offer in offers) offer.currentStock = UnityEngine.Random.Range(0, offer.maxStock + 1);
        MarketChanged?.Invoke();
    }

    public bool IsLocked => GameManger.Instance != null && GameManger.Instance.CurrentWaveIndex <= _lockedUntilWaveIndex;

    public int GetBuyCost(int index, int quantity)
    {
        if (index < 0 || index >= offers.Length || quantity <= 0) return 0;
        return offers[index].basePrice * quantity;
    }

    public int GetRentCost(int index, int quantity)
    {
        if (index < 0 || index >= offers.Length || quantity <= 0) return 0;
        return Mathf.CeilToInt(offers[index].basePrice * quantity * rentPriceMultiplier);
    }

    public bool CanBuy(int index, int quantity)
    {
        if (IsLocked || index < 0 || index >= offers.Length || quantity <= 0) return false;
        return offers[index].currentStock >= quantity && EconomyManager.Instance != null && EconomyManager.Instance.Money >= GetBuyCost(index, quantity);
    }

    public bool CanRent(int index, int quantity)
    {
        if (IsLocked || index < 0 || index >= offers.Length || quantity <= 0) return false;
        return offers[index].currentStock >= quantity && EconomyManager.Instance != null && EconomyManager.Instance.Debt <= 0;
    }

    public bool Buy(int index, int quantity)
    {
        if (!CanBuy(index, quantity) || CrewRoster.Instance == null) return false;
        MercenaryOffer offer = offers[index];
        int cost = GetBuyCost(index, quantity);
        if (!EconomyManager.Instance.TrySpend(cost)) return false;

        offer.currentStock -= quantity;
        CrewRoster.Instance.AddCrew(offer.rank, quantity, true);
        Vector3 position = FxPosition();
        FloatingCombatText.Spawn(position, "-" + cost, new Color(1f, .35f, .3f));
        FloatingCombatText.Spawn(position + Vector3.up * .6f, "+" + quantity + " " + offer.rank, new Color(.35f, 1f, .3f));
        SfxManager.Instance?.PlayCoinSpent();
        SfxManager.Instance?.PlayCrewBought();
        MarketChanged?.Invoke();
        return true;
    }

    public bool Rent(int index, int quantity)
    {
        if (!CanRent(index, quantity) || CrewRoster.Instance == null || EconomyManager.Instance == null) return false;
        MercenaryOffer offer = offers[index];
        int cost = GetRentCost(index, quantity);

        offer.currentStock -= quantity;
        CrewRoster.Instance.AddCrew(offer.rank, quantity, true);
        EconomyManager.Instance.AddDebt(cost);
        _lockedUntilWaveIndex = (GameManger.Instance != null ? GameManger.Instance.CurrentWaveIndex : 0) + 1;

        Vector3 position = FxPosition();
        FloatingCombatText.Spawn(position, "+" + quantity + " " + offer.rank, new Color(.35f, 1f, .3f));
        FloatingCombatText.Spawn(position + Vector3.up * .6f, "debt +" + cost, new Color(1f, .6f, .2f));
        SfxManager.Instance?.PlayCoinSpent();
        SfxManager.Instance?.PlayCrewBought();
        MarketChanged?.Invoke();
        return true;
    }

    private Vector3 FxPosition()
    {
        return CartageHeart.Instance != null ? CartageHeart.Instance.transform.position : transform.position;
    }
}
