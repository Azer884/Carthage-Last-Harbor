using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class GameManger : MonoBehaviour
{
    public static GameManger Instance;
    // Gates resource-tower income (see CarthaginianResourceTower) so the extended wave-1 reading window
    // stays a true calm-before-the-storm instead of quietly stacking up coin before the player's even
    // engaged with the tutorial tip about it. Stays true forever once wave 1 begins.
    public static bool FirstWaveHasStarted { get; private set; }

    [Header("Path Selection")]
    [SerializeField] private GameObject[] paths;

    [Header("Wave System")]
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private List<WaveDefinition> waves = new();
    [SerializeField, Min(0f)] private float timeBetweenWaves = 3f;
    [SerializeField] private bool autoStartWaves = true;
    [SerializeField, Min(0f)] private float autoStartDelay = 20f;
    [Tooltip("Wave 1 only — longer than autoStartDelay so new players have time to read the opening tips before ships start arriving.")]
    [SerializeField, Min(0f)] private float firstWaveAutoStartDelay = 45f;
    [Tooltip("Once the hand-authored wave list is exhausted, keep replaying the final wave with ship counts scaled up exponentially.")]
    [SerializeField] private bool endlessScaling = true;
    [SerializeField, Min(1f)] private float difficultyGrowthPerWave = 1.18f;

    private Coroutine _waveRoutine;
    private int _currentWaveIndex;
    private float _autoStartTimer;
    private bool _autoStartPending;
    private int _shipsQueuedThisWave;
    private bool _preWaveDelayActive;
    private float _preWaveDelayRemaining;

    public bool IsWaveRunning => (_waveRoutine != null || AnyRomanShipsAlive()) && !CartageHeart.HasBeenDestroyed;
    public bool IsAutoStartPending => _autoStartPending;
    public float AutoStartTimeRemaining => _autoStartPending ? Mathf.Max(0f, _autoStartTimer) : 0f;
    // The short "wave N about to spawn" gap after a wave is triggered (auto or manual click) but before its
    // ships actually appear — separate from the auto-start countdown, which is the (much longer) idle gap
    // before the player/timer even triggers the next wave. The big on-screen 3-2-1 uses whichever is active.
    public bool IsPreWaveDelayActive => _preWaveDelayActive;
    public float PreWaveDelayRemaining => _preWaveDelayActive ? Mathf.Max(0f, _preWaveDelayRemaining) : 0f;
    public int CurrentWaveIndex => _currentWaveIndex;
    public float AutoStartDelay => autoStartDelay;
    // Ships still to be spawned in the wave currently in progress (0 once the last one has launched).
    public int ShipsQueuedThisWave => _shipsQueuedThisWave;
    public event Action<int> WaveStarted;
    // Fires once every ship from the wave is gone (not on heart destruction — that's game over, not a win).
    public event Action<int> WaveCompleted;

    private bool AnyRomanShipsAlive()
    {
        foreach (RomanShipHealth ship in FindObjectsByType<RomanShipHealth>(FindObjectsSortMode.None))
        {
            if (!ship.IsDestroyed)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            FirstWaveHasStarted = false;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoStartWaves)
        {
            BeginAutoStartCountdown();
        }
    }

    private void Update()
    {
        if (!_autoStartPending)
        {
            return;
        }

        _autoStartTimer -= Time.deltaTime;
        if (_autoStartTimer > 0f)
        {
            return;
        }

        _autoStartPending = false;
        StartWaveSystem();
    }

    private void BeginAutoStartCountdown()
    {
        _autoStartPending = true;
        _autoStartTimer = _currentWaveIndex == 0 ? firstWaveAutoStartDelay : autoStartDelay;
    }

    public void StartWaveSystem()
    {
        _autoStartPending = false;
        if (_waveRoutine != null)
        {
            return;
        }

        if (SfxManager.Instance != null) SfxManager.Instance.PlayWaveStart();
        if (MusicManager.Instance != null) MusicManager.Instance.PlayWaveMusic();
        _waveRoutine = StartCoroutine(RunWaves());
    }

    public void ResetWaveSystem()
    {
        _autoStartPending = false;
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        _currentWaveIndex = 0;
        _shipsQueuedThisWave = 0;
        _preWaveDelayActive = false;
    }

    // Runs exactly ONE wave, then returns — it does not chain into the next wave itself. The next wave
    // only becomes available once every ship from this one is gone (see the WaitUntil below), at which
    // point it hands off to the normal auto-start countdown (or waits for the player to click Start).
    private IEnumerator RunWaves()
    {
        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager is not assigned in GameManager.");
            _waveRoutine = null;
            yield break;
        }

        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("No waves configured in GameManager.");
            _waveRoutine = null;
            yield break;
        }

        if (!endlessScaling && _currentWaveIndex >= waves.Count)
        {
            _waveRoutine = null;
            yield break;
        }

        WaveDefinition wave = BuildWave(_currentWaveIndex);
        if (wave != null)
        {
            if (wave.startDelay > 0f)
            {
                _preWaveDelayActive = true;
                _preWaveDelayRemaining = wave.startDelay;
                while (_preWaveDelayRemaining > 0f)
                {
                    _preWaveDelayRemaining -= Time.deltaTime;
                    yield return null;
                }
                _preWaveDelayActive = false;
            }

            if (_currentWaveIndex == 0) FirstWaveHasStarted = true;
            WaveStarted?.Invoke(_currentWaveIndex);
            yield return SpawnWave(wave);
            yield return new WaitUntil(() => !AnyRomanShipsAlive() || CartageHeart.HasBeenDestroyed);
            if (!CartageHeart.HasBeenDestroyed)
            {
                WaveCompleted?.Invoke(_currentWaveIndex);
                if (timeBetweenWaves > 0f)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }
            }
        }

        _currentWaveIndex++;
        _waveRoutine = null;
        if (autoStartWaves && !CartageHeart.HasBeenDestroyed)
        {
            BeginAutoStartCountdown();
        }
    }

    // Hand-authored waves play as configured. Once they run out, the final wave keeps repeating with
    // ship counts scaled up exponentially so difficulty keeps climbing indefinitely.
    private WaveDefinition BuildWave(int index)
    {
        if (waves == null || waves.Count == 0)
        {
            return null;
        }

        if (index < waves.Count)
        {
            return waves[index];
        }

        WaveDefinition template = waves[waves.Count - 1];
        int extraWaves = index - waves.Count + 1;
        float multiplier = Mathf.Pow(difficultyGrowthPerWave, extraWaves);

        WaveDefinition scaled = new WaveDefinition
        {
            waveName = "Wave " + (index + 1),
            startDelay = template.startDelay,
            ships = new List<WaveShipEntry>()
        };

        foreach (WaveShipEntry entry in template.ships)
        {
            if (entry == null || entry.ship == null)
            {
                continue;
            }

            scaled.ships.Add(new WaveShipEntry
            {
                ship = entry.ship,
                amount = Mathf.Max(1, Mathf.RoundToInt(entry.amount * multiplier)),
                spawnInterval = Mathf.Max(.15f, entry.spawnInterval / Mathf.Sqrt(multiplier)),
                attackChancePercent = Mathf.Min(100f, entry.attackChancePercent * (1f + .02f * extraWaves)),
                towerAttackChancePercent = entry.towerAttackChancePercent
            });
        }

        return scaled;
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        _shipsQueuedThisWave = 0;
        foreach (WaveShipEntry entry in wave.ships)
            if (entry != null && entry.ship != null && entry.amount > 0) _shipsQueuedThisWave += entry.amount;

        foreach (WaveShipEntry entry in wave.ships)
        {
            if (entry == null || entry.ship == null || entry.amount <= 0)
            {
                continue;
            }

            for (int i = 0; i < entry.amount; i++)
            {
                bool willAttack = UnityEngine.Random.value <= entry.attackChancePercent / 100f;
                bool willPreferTowers = UnityEngine.Random.value <= entry.towerAttackChancePercent / 100f;
                spawnManager.SpawnShip(entry.ship, willAttack, willPreferTowers);
                _shipsQueuedThisWave = Mathf.Max(0, _shipsQueuedThisWave - 1);

                if (entry.spawnInterval > 0f && i < entry.amount - 1)
                {
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }
    }

    public SplineContainer GetRandomPath()
    {
        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("No paths assigned in GameManager.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, paths.Length);
        GameObject selectedPath = paths[randomIndex];
        if (selectedPath == null)
        {
            Debug.LogError("A path entry in GameManager is null.");
            return null;
        }

        SplineContainer splineContainer = selectedPath.GetComponent<SplineContainer>();
        if (splineContainer == null)
        {
            Debug.LogError($"Path '{selectedPath.name}' is missing a SplineContainer component.");
        }

        return splineContainer;
    }

    public GameObject[] GetPathObjects()
    {
        return paths ?? Array.Empty<GameObject>();
    }

    [Serializable]
    public class WaveDefinition
    {
        public string waveName;
        [Min(0f)] public float startDelay;
        public List<WaveShipEntry> ships = new List<WaveShipEntry>();
    }

    [Serializable]
    public class WaveShipEntry
    {
        public RomeShip ship;
        [Min(1)] public int amount = 1;
        [Min(0f)] public float spawnInterval;
        [Range(0f, 100f)] public float attackChancePercent = 50f;
        [Range(0f, 100f)] public float towerAttackChancePercent = 50f;
    }
}
