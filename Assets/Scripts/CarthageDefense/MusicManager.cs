using System.Collections;
using UnityEngine;

/// <summary>Loops and crossfades background music. Assign tracks in the Inspector; anything left empty
/// is skipped, so a partial setup never errors.</summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip buildMusic;
    [SerializeField] private AudioClip waveMusic;
    [SerializeField] private AudioClip gameOverMusic;
    [SerializeField, Range(0f, 1f)] private float volume = .5f;
    [SerializeField, Min(.1f)] private float crossfadeDuration = 1.5f;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _active;
    private Coroutine _fadeRoutine;

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
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();
        _sourceA.loop = _sourceB.loop = true;
        _sourceA.playOnAwake = _sourceB.playOnAwake = false;
        _active = _sourceA;
    }

    private void Start()
    {
        PlayBuildMusic();
    }

    public void PlayBuildMusic() => Crossfade(buildMusic);
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
