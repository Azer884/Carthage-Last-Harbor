using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class GameManger : MonoBehaviour
{
    public static GameManger Instance;

    [Header("Path Selection")]
    [SerializeField] private GameObject[] paths;

    [Header("Wave System")]
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private List<WaveDefinition> waves = new();
    [SerializeField, Min(0f)] private float timeBetweenWaves = 3f;
    [SerializeField] private bool autoStartWaves = true;

    private Coroutine _waveRoutine;
    private int _currentWaveIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
            StartWaveSystem();
        }
    }

    public void StartWaveSystem()
    {
        if (_waveRoutine != null)
        {
            return;
        }

        _waveRoutine = StartCoroutine(RunWaves());
    }

    public void ResetWaveSystem()
    {
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        _currentWaveIndex = 0;
    }

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

        for (_currentWaveIndex = 0; _currentWaveIndex < waves.Count; _currentWaveIndex++)
        {
            WaveDefinition wave = waves[_currentWaveIndex];
            if (wave == null)
            {
                continue;
            }

            if (wave.startDelay > 0f)
            {
                yield return new WaitForSeconds(wave.startDelay);
            }

            yield return SpawnWave(wave);

            if (_currentWaveIndex < waves.Count - 1 && timeBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        _waveRoutine = null;
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
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
