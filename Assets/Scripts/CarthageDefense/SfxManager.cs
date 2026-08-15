using UnityEngine;

/// <summary>Central one-shot SFX player. Drag clips onto the named slots in the Inspector; anything left
/// empty is silently skipped, so partial setups never error.</summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Building")]
    [SerializeField] private AudioClip towerPlaced;
    [SerializeField] private AudioClip placementInvalid;
    [Header("Ships")]
    [SerializeField] private AudioClip shipSpawned;
    [SerializeField] private AudioClip shipDestroyed;
    [Header("Economy")]
    [SerializeField] private AudioClip coinGained;
    [SerializeField] private AudioClip coinSpent;
    [SerializeField] private AudioClip crewTrained;
    [Header("Waves / Game state")]
    [SerializeField] private AudioClip waveStart;
    [SerializeField] private AudioClip heartDestroyed;
    [SerializeField] private AudioClip buttonClick;
    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float volume = .85f;

    private AudioSource _source;

    public static SfxManager Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Sfx Manager").AddComponent<SfxManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip, volume * volumeScale);
    }

    public void PlayTowerPlaced() => Play(towerPlaced);
    public void PlayPlacementInvalid() => Play(placementInvalid);
    public void PlayShipSpawned() => Play(shipSpawned);
    public void PlayShipDestroyed() => Play(shipDestroyed);
    public void PlayCoinGained() => Play(coinGained);
    public void PlayCoinSpent() => Play(coinSpent);
    public void PlayCrewTrained() => Play(crewTrained);
    public void PlayWaveStart() => Play(waveStart);
    public void PlayHeartDestroyed() => Play(heartDestroyed);
    public void PlayButtonClick() => Play(buttonClick);
}
