using System;
using UnityEngine;

/// <summary>Shared treasury. Resource buildings create the reliable income of the city.</summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }
    [SerializeField, Min(0)] private int startingMoney = 150;
    public int Money { get; private set; }
    public int Debt { get; private set; }
    public event Action<int> MoneyChanged;
    public event Action<int> DebtChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startingMoney;
    }

    // Incoming money pays down any outstanding rent debt first; only the leftover, if any, becomes spendable.
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        if (Debt > 0)
        {
            int repayment = Mathf.Min(amount, Debt);
            Debt -= repayment;
            amount -= repayment;
            DebtChanged?.Invoke(Debt);
        }

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

    public void AddDebt(int amount)
    {
        if (amount <= 0) return;
        Debt += amount;
        DebtChanged?.Invoke(Debt);
    }
}
