using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared Carthaginian manpower. Sidi Bou Said (see SidiBouSaidTower) supplies regular native recruits;
/// the Mercenary Market (see MercenaryMarket) sells or rents weaker foreign crew of the same ranks.</summary>
public class CrewRoster : MonoBehaviour
{
    public static CrewRoster Instance { get; private set; }

    [SerializeField] private CrewCount[] startingCrew;

    private readonly Dictionary<CrewRank, int> _availableNative = new Dictionary<CrewRank, int>();
    private readonly Dictionary<CrewRank, int> _availableMercenary = new Dictionary<CrewRank, int>();

    public event Action<CrewRank, int> CrewChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (CrewRank rank in Enum.GetValues(typeof(CrewRank))) { _availableNative[rank] = 0; _availableMercenary[rank] = 0; }
        foreach (CrewCount count in startingCrew) AddCrew(count.rank, count.amount, count.isMercenary);
    }

    public int GetAvailable(CrewRank rank) { return GetAvailableNative(rank) + GetAvailableMercenary(rank); }
    public int GetAvailableNative(CrewRank rank) { return _availableNative.TryGetValue(rank, out int value) ? value : 0; }
    public int GetAvailableMercenary(CrewRank rank) { return _availableMercenary.TryGetValue(rank, out int value) ? value : 0; }

    public void AddCrew(CrewRank rank, int amount, bool isMercenary = false)
    {
        if (amount <= 0) return;
        Dictionary<CrewRank, int> pool = isMercenary ? _availableMercenary : _availableNative;
        pool[rank] = (pool.TryGetValue(rank, out int value) ? value : 0) + amount;
        CrewChanged?.Invoke(rank, GetAvailable(rank));
    }

    // Prefers native crew first; only dips into mercenaries for the remainder, so callers see which
    // portion of what they were handed is the weaker foreign stock.
    public bool TryAssignCrew(CrewRank minimumRank, int amount, out CrewCount[] assigned)
    {
        assigned = new CrewCount[0];
        if (amount <= 0) return true;
        List<CrewCount> result = new List<CrewCount>();
        int needed = amount;
        for (int rank = (int)CrewRank.SacredBand; rank >= (int)minimumRank && needed > 0; rank--)
        {
            CrewRank current = (CrewRank)rank;
            int takenNative = Mathf.Min(GetAvailableNative(current), needed);
            if (takenNative > 0)
            {
                _availableNative[current] -= takenNative;
                result.Add(new CrewCount { rank = current, amount = takenNative, isMercenary = false });
                needed -= takenNative;
            }

            int takenMercenary = needed > 0 ? Mathf.Min(GetAvailableMercenary(current), needed) : 0;
            if (takenMercenary > 0)
            {
                _availableMercenary[current] -= takenMercenary;
                result.Add(new CrewCount { rank = current, amount = takenMercenary, isMercenary = true });
                needed -= takenMercenary;
            }

            if (takenNative > 0 || takenMercenary > 0) CrewChanged?.Invoke(current, GetAvailable(current));
        }

        if (needed == 0) { assigned = result.ToArray(); return true; }
        foreach (CrewCount crew in result) AddCrew(crew.rank, crew.amount, crew.isMercenary);
        return false;
    }

    // Native crew trains first; the promotion only carries the mercenary label if mercenaries had to
    // be used to fill the batch, so mixed batches still promote into a weaker unit.
    public bool TryTrain(CrewRank fromRank, int recruitsNeeded = 3)
    {
        if (fromRank >= CrewRank.SacredBand || recruitsNeeded < 1 || GetAvailable(fromRank) < recruitsNeeded) return false;

        int takenNative = Mathf.Min(GetAvailableNative(fromRank), recruitsNeeded);
        int takenMercenary = recruitsNeeded - takenNative;
        _availableNative[fromRank] -= takenNative;
        _availableMercenary[fromRank] -= takenMercenary;
        CrewChanged?.Invoke(fromRank, GetAvailable(fromRank));
        AddCrew(fromRank + 1, 1, takenMercenary > 0);
        return true;
    }
}

[Serializable]
public struct CrewCount { public CrewRank rank; [Min(0)] public int amount; public bool isMercenary; }
