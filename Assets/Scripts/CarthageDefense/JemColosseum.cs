using UnityEngine;

/// <summary>Attach to the Jem arena building. Training converts crew into the next rank for a money cost
/// that scales with the rank being trained.</summary>
public class JemColosseum : MonoBehaviour
{
    [SerializeField, Min(1)] private int traineesPerPromotion = 3;
    [Tooltip("Money cost to train out of each rank, indexed by CrewRank (Recruit, Sailor, Veteran).")]
    [SerializeField] private int[] trainingCostByRank = { 20, 40, 80 };

    public int TraineesPerPromotion => traineesPerPromotion;

    public int GetTrainingCost(CrewRank rank)
    {
        int index = (int)rank;
        return trainingCostByRank != null && index >= 0 && index < trainingCostByRank.Length ? trainingCostByRank[index] : 0;
    }

    public bool CanTrain(CrewRank rank)
    {
        return rank < CrewRank.SacredBand
            && CrewRoster.Instance != null && CrewRoster.Instance.GetAvailable(rank) >= traineesPerPromotion
            && EconomyManager.Instance != null && EconomyManager.Instance.Money >= GetTrainingCost(rank);
    }

    // Trains up to the requested count in one click (fewer if crew/money runs out first), so the
    // player can type "10" instead of clicking one batch at a time.
    public int TrainCount(CrewRank rank, int count)
    {
        int trained = 0;
        int totalCost = 0;
        for (int i = 0; i < count && CanTrain(rank); i++)
        {
            int cost = GetTrainingCost(rank);
            if (cost > 0 && !EconomyManager.Instance.TrySpend(cost)) break;
            if (!CrewRoster.Instance.TryTrain(rank, traineesPerPromotion))
            {
                if (cost > 0) EconomyManager.Instance.AddMoney(cost);
                break;
            }
            trained++;
            totalCost += cost;
        }

        if (trained <= 0) return 0;
        if (totalCost > 0)
        {
            FloatingCombatText.Spawn(transform.position, "-" + totalCost, new Color(1f, .35f, .3f));
            SfxManager.Instance?.PlayCoinSpent();
        }
        FloatingCombatText.Spawn(transform.position + Vector3.up * .6f, "+" + trained + " " + (rank + 1), new Color(.35f, 1f, .3f));
        SfxManager.Instance?.PlayCrewTrained();
        return trained;
    }
}
