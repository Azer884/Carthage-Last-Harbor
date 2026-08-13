using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared Carthaginian manpower. Sidi Bou Said supplies regular recruits; bought men are weaker recruits.</summary>
public class CrewRoster : MonoBehaviour
{
    public static CrewRoster Instance { get; private set; }

    [SerializeField, Min(0f)] private float sidiBouSaidRecruitInterval = 12f;
    [SerializeField, Min(1)] private int sidiBouSaidRecruitsPerArrival = 2;
    [SerializeField, Min(1)] private int boughtMenPerPurchase = 3;
    [Tooltip("Share of Sidi Bou Said applicants who fail combat selection and become workers.")]
    [SerializeField, Range(0f, 1f)] private float failedFighterWorkerChance = 0.25f;
    [SerializeField] private CrewCount[] startingCrew;

    private readonly Dictionary<CrewRank, int> _available = new Dictionary<CrewRank, int>();
    private float _nextSidiArrival;

    public event Action<CrewRank, int> CrewChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (CrewRank rank in Enum.GetValues(typeof(CrewRank))) _available[rank] = 0;
        foreach (CrewCount count in startingCrew) AddCrew(count.rank, count.amount);
        _nextSidiArrival = Time.time + sidiBouSaidRecruitInterval;
    }

    private void Update()
    {
        if (sidiBouSaidRecruitInterval <= 0f || Time.time < _nextSidiArrival) return;
        int workers = 0;
        for (int i = 0; i < sidiBouSaidRecruitsPerArrival; i++)
            if (UnityEngine.Random.value < failedFighterWorkerChance) workers++;
        AddCrew(CrewRank.Recruit, sidiBouSaidRecruitsPerArrival - workers);
        if (workers > 0 && WorkerRoster.Instance != null)
            WorkerRoster.Instance.AddFailedFighterWorkers(workers);
        _nextSidiArrival = Time.time + sidiBouSaidRecruitInterval;
    }

    public int GetAvailable(CrewRank rank) { return _available.TryGetValue(rank, out int value) ? value : 0; }
    public void BuyWeakMen() { AddCrew(CrewRank.Recruit, boughtMenPerPurchase); }
    public void AddCrew(CrewRank rank, int amount)
    {
        if (amount <= 0) return;
        _available[rank] = GetAvailable(rank) + amount;
        CrewChanged?.Invoke(rank, GetAvailable(rank));
    }

    public bool TryAssignCrew(CrewRank minimumRank, int amount, out CrewCount[] assigned)
    {
        assigned = new CrewCount[0];
        if (amount <= 0) return true;
        List<CrewCount> result = new List<CrewCount>();
        int needed = amount;
        for (int rank = (int)CrewRank.SacredBand; rank >= (int)minimumRank && needed > 0; rank--)
        {
            CrewRank current = (CrewRank)rank;
            int taken = Mathf.Min(GetAvailable(current), needed);
            if (taken <= 0) continue;
            _available[current] -= taken;
            result.Add(new CrewCount { rank = current, amount = taken });
            CrewChanged?.Invoke(current, GetAvailable(current));
            needed -= taken;
        }
        if (needed == 0) { assigned = result.ToArray(); return true; }
        foreach (CrewCount crew in result) AddCrew(crew.rank, crew.amount);
        return false;
    }

    public bool TryTrain(CrewRank fromRank, int recruitsNeeded = 3)
    {
        if (fromRank >= CrewRank.SacredBand || recruitsNeeded < 1 || GetAvailable(fromRank) < recruitsNeeded) return false;
        _available[fromRank] -= recruitsNeeded;
        CrewChanged?.Invoke(fromRank, GetAvailable(fromRank));
        AddCrew(fromRank + 1, 1);
        return true;
    }
}

[Serializable]
public struct CrewCount { public CrewRank rank; [Min(0)] public int amount; }
