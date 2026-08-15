using System;
using UnityEngine;

/// <summary>Attach alongside CarthaginianTower on Sidi Bou Said. Each level supplies recruits at its own rate
/// and shows a different model; CarthaginianTower drives which level is active.</summary>
[RequireComponent(typeof(CarthaginianTower))]
public class SidiBouSaidTower : MonoBehaviour
{
    [SerializeField] private SidiBouSaidLevel[] levels;
    private int _activeLevel;
    private float _nextArrival;

    public void SetLevel(int level)
    {
        _activeLevel = levels != null && levels.Length > 0 ? Mathf.Clamp(level, 0, levels.Length - 1) : 0;
        ApplyModel();
    }

    public float CrewPerSecond
    {
        get
        {
            SidiBouSaidLevel level = ActiveLevel();
            return level != null && level.recruitInterval > 0f ? level.recruitsPerArrival / level.recruitInterval : 0f;
        }
    }

    // Runs when the component is first added (or reset) in the Editor, so it starts pre-filled with 4 tiers.
    private void Reset()
    {
        levels = new[]
        {
            new SidiBouSaidLevel { title = "Level I", recruitInterval = 10f, recruitsPerArrival = 3, failedFighterWorkerChance = 0.3f },
            new SidiBouSaidLevel { title = "Level II", recruitInterval = 8f, recruitsPerArrival = 3, failedFighterWorkerChance = 0.25f },
            new SidiBouSaidLevel { title = "Level III", recruitInterval = 6f, recruitsPerArrival = 4, failedFighterWorkerChance = 0.2f },
            new SidiBouSaidLevel { title = "Level IV", recruitInterval = 4f, recruitsPerArrival = 5, failedFighterWorkerChance = 0.15f },
        };
    }

    private void Awake()
    {
        // Sidi Bou Said is a fixed civic building — the player should never be able to sell it off.
        GetComponent<CarthaginianTower>()?.SetSellable(false);
        SidiBouSaidLevel level = ActiveLevel();
        _nextArrival = Time.time + (level != null ? level.recruitInterval : 0f);
        ApplyModel();
    }

    private void Update()
    {
        SidiBouSaidLevel level = ActiveLevel();
        if (level == null || level.recruitInterval <= 0f || Time.time < _nextArrival || CrewRoster.Instance == null) return;
        _nextArrival = Time.time + level.recruitInterval;

        int workers = 0;
        for (int i = 0; i < level.recruitsPerArrival; i++)
            if (UnityEngine.Random.value < level.failedFighterWorkerChance) workers++;

        CrewRoster.Instance.AddCrew(CrewRank.Recruit, level.recruitsPerArrival - workers);
        if (workers > 0 && WorkerRoster.Instance != null)
            WorkerRoster.Instance.AddFailedFighterWorkers(workers);

        FloatingCombatText.Spawn(transform.position, "+" + level.recruitsPerArrival + " crew", new Color(.35f, 1f, .3f));
    }

    private SidiBouSaidLevel ActiveLevel() =>
        levels != null && levels.Length > 0 ? levels[Mathf.Clamp(_activeLevel, 0, levels.Length - 1)] : null;

    private void ApplyModel()
    {
        SidiBouSaidLevel active = ActiveLevel();
        if (levels == null) return;
        foreach (SidiBouSaidLevel level in levels)
            if (level.model != null) level.model.SetActive(level == active);
    }
}

[Serializable]
public class SidiBouSaidLevel
{
    public string title = "Level I";
    [Min(0f)] public float recruitInterval = 12f;
    [Min(1)] public int recruitsPerArrival = 2;
    [Tooltip("Share of applicants who fail combat selection and become workers instead.")]
    [Range(0f, 1f)] public float failedFighterWorkerChance = 0.25f;
    [Tooltip("Model shown while this level is active. Leave empty to keep the current model.")]
    public GameObject model;
}
