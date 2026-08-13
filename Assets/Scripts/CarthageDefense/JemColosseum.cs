using UnityEngine;

/// <summary>Attach to the Jem arena building. Training converts three crew into one member of the next rank.</summary>
public class JemColosseum : MonoBehaviour
{
    [SerializeField, Min(1)] private int traineesPerPromotion = 3;
    public bool Train(CrewRank rank)
    {
        return CrewRoster.Instance != null && CrewRoster.Instance.TryTrain(rank, traineesPerPromotion);
    }
}
