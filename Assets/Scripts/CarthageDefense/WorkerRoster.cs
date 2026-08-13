using System;
using UnityEngine;

/// <summary>Non-combat manpower for extractors. Workers come from failed fighter applicants or direct purchase.</summary>
public class WorkerRoster : MonoBehaviour
{
    public static WorkerRoster Instance { get; private set; }
    [SerializeField, Min(1)] private int boughtWorkersPerPurchase = 2;
    [SerializeField, Min(0)] private int workerPurchaseCost = 25;
    [SerializeField, Min(0)] private int startingWorkers = 4;

    public int AvailableWorkers { get; private set; }
    public event Action<int> WorkersChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        AvailableWorkers = startingWorkers;
    }

    public void AddFailedFighterWorkers(int amount) { AddWorkers(amount); }
    public void AddWorkers(int amount)
    {
        if (amount <= 0) return;
        AvailableWorkers += amount;
        WorkersChanged?.Invoke(AvailableWorkers);
    }

    public bool TryAssignWorkers(int amount)
    {
        if (amount <= 0 || AvailableWorkers < amount) return false;
        AvailableWorkers -= amount;
        WorkersChanged?.Invoke(AvailableWorkers);
        return true;
    }

    public void ReturnWorkers(int amount) { AddWorkers(amount); }

    public bool TryBuyWorkers()
    {
        if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(workerPurchaseCost)) return false;
        AddWorkers(boughtWorkersPerPurchase);
        return true;
    }
}
