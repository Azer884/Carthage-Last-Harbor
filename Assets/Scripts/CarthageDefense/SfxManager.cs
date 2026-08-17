using UnityEngine;

/// <summary>Central one-shot SFX player. Drag clips onto the named slots in the Inspector; anything left
/// empty is silently skipped, so partial setups never error.</summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Building")]
    [SerializeField] private AudioClip towerPlaced;
    [SerializeField] private AudioClip placementInvalid;
    [SerializeField] private AudioClip towerUpgraded;
    [Header("Ships")]
    [SerializeField] private AudioClip shipSpawned;
    [SerializeField] private AudioClip shipDestroyed;
    [SerializeField] private AudioClip shipAttack;
    [Tooltip("Looping sail/oar sound. Not a one-shot — CarthaginianShipCombat reads this clip and loops it on its own AudioSource while sailing.")]
    [SerializeField] private AudioClip shipSailingLoop;
    [Header("Dragon")]
    [SerializeField] private AudioClip dragonAttack;
    [Header("Economy")]
    [SerializeField] private AudioClip coinGained;
    [SerializeField] private AudioClip coinSpent;
    [SerializeField] private AudioClip crewTrained;
    [SerializeField] private AudioClip crewBought;
    [SerializeField] private AudioClip crewGenerated;
    [Header("Waves / Game state")]
    [SerializeField] private AudioClip waveStart;
    [SerializeField] private AudioClip heartDestroyed;
    [SerializeField] private AudioClip buttonClick;
    [Header("Ambient")]
    [Tooltip("Background sea/wind loop, played continuously and separately from the one-shot SFX source.")]
    [SerializeField] private AudioClip ambientLoop;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = .35f;
    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float volume = .85f;
    [SerializeField, Range(0f, 1f)] private float loopVolume = .5f;

    private AudioSource _source;
    private AudioSource _ambientSource;

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

        if (ambientLoop != null)
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.clip = ambientLoop;
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            _ambientSource.volume = ambientVolume;
            _ambientSource.Play();
        }
    }

    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip, volume * volumeScale);
    }

    public void PlayTowerPlaced() => Play(towerPlaced);
    public void PlayPlacementInvalid() => Play(placementInvalid);
    public void PlayTowerUpgraded() => Play(towerUpgraded);
    public void PlayShipSpawned() => Play(shipSpawned);
    public void PlayShipDestroyed() => Play(shipDestroyed);
    public void PlayShipAttack() => Play(shipAttack);
    public AudioClip ShipSailingLoopClip => shipSailingLoop;
    public float LoopVolume => loopVolume;
    public void PlayDragonAttack() => Play(dragonAttack);
    public void PlayCoinGained() => Play(coinGained);
    public void PlayCoinSpent() => Play(coinSpent);
    public void PlayCrewTrained() => Play(crewTrained);
    public void PlayCrewBought() => Play(crewBought);
    public void PlayCrewGenerated() => Play(crewGenerated);
    public void PlayWaveStart() => Play(waveStart);
    public void PlayHeartDestroyed() => Play(heartDestroyed);
    public void PlayButtonClick() => Play(buttonClick);
}
