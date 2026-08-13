using System;
using UnityEngine;

/// <summary>Shared treasury. Resource buildings create the reliable income of the city.</summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }
    [SerializeField, Min(0)] private int startingMoney = 150;
    public int Money { get; private set; }
    public event Action<int> MoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startingMoney;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        MoneyChanged?.Invoke(Money);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Money < amount) return false;
        Money -= amount;
        MoneyChanged?.Invoke(Money);
        return true;
    }
}
