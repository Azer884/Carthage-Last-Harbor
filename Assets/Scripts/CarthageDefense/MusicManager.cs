using System.Collections;
using UnityEngine;

/// <summary>Loops and crossfades background music. Assign tracks in the Inspector; anything left empty
/// is skipped, so a partial setup never errors.</summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    private const string MusicVolumeKey = "CarthageDefense.MusicVolume";

    // Live-adjustable from any settings panel (main menu or in-game pause) — updates the currently
    // playing source immediately instead of only taking effect on the next scene load.
    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp01(value);
            if (_active != null) _active.volume = volume;
            PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        }
    }

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip waveMusic;
    [SerializeField] private AudioClip gameOverMusic;
    [SerializeField, Range(0f, 1f)] private float volume = .5f;
    [SerializeField, Min(.1f)] private float crossfadeDuration = 1.5f;
    [Header("Pause muffle")]
    [SerializeField] private float pausedCutoffFrequency = 700f;
    [SerializeField] private float cutoffLerpSpeed = 4f;

    private const float UnfilteredCutoffFrequency = 22000f; // AudioLowPassFilter's own default — effectively no filtering.

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _active;
    private Coroutine _fadeRoutine;
    private AudioLowPassFilter _lowPassFilter;
    private float _targetCutoffFrequency = UnfilteredCutoffFrequency;

    public static MusicManager Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Music Manager").AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Overrides the Inspector default with whatever the player set in the main menu settings panel.
        volume = PlayerPrefs.GetFloat(MusicVolumeKey, volume);
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();
        _sourceA.loop = _sourceB.loop = true;
        _sourceA.playOnAwake = _sourceB.playOnAwake = false;
        _active = _sourceA;
        // One filter on this GameObject affects both sources — Unity applies AudioLowPassFilter to every
        // AudioSource attached to the same object.
        _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        _lowPassFilter.cutoffFrequency = UnfilteredCutoffFrequency;
    }

    // Called by CarthaginianBuildMenu on pause/resume — muffles the music while paused rather than snapping
    // the cutoff instantly, since Time.timeScale is 0 while paused so this has to run on unscaled time.
    public void SetMuffled(bool muffled) => _targetCutoffFrequency = muffled ? pausedCutoffFrequency : UnfilteredCutoffFrequency;

    private void Update()
    {
        if (_lowPassFilter == null) return;
        _lowPassFilter.cutoffFrequency = Mathf.Lerp(_lowPassFilter.cutoffFrequency, _targetCutoffFrequency, Time.unscaledDeltaTime * cutoffLerpSpeed);
    }

    // No auto-played default track here: the singleton can now first spin up from either MainMenu or
    // GameScene (whichever loads first), so each scene's own controller explicitly requests its track
    // instead of this manager guessing which one it should be.
    public void PlayMenuMusic() => Crossfade(menuMusic);
    // Build phase shares the menu track by design (same calm pre-wave mood) — Crossfade no-ops if it's
    // already playing, so arriving from the main menu doesn't even trigger a fade.
    public void PlayBuildMusic() => Crossfade(menuMusic);
    public void PlayWaveMusic() => Crossfade(waveMusic);
    public void PlayGameOverMusic() => Crossfade(gameOverMusic);

    private void Crossfade(AudioClip clip)
    {
        if (clip == null || clip == _active.clip) return;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip clip)
    {
        AudioSource incoming = _active == _sourceA ? _sourceB : _sourceA;
        AudioSource outgoing = _active;
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            // Unscaled so the game-over sting still fades in while Time.timeScale is 0.
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);
            incoming.volume = volume * t;
            outgoing.volume = volume * (1f - t);
            yield return null;
        }

        incoming.volume = volume;
        outgoing.Stop();
        _active = incoming;
    }
}
