using UnityEngine;

/// <summary>Screen shake using the "trauma" model (bump a single decaying value, offset scales with its
/// square). Runs in LateUpdate — after TopDownCameraController's own Update — and always adds/removes its
/// own exact offset each frame, so it layers on top of normal camera movement instead of fighting it.</summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField, Min(0f)] private float maxOffset = 1.2f;
    [SerializeField, Min(0f)] private float maxTilt = 2.5f;
    [SerializeField, Min(0.1f)] private float noiseFrequency = 22f;
    [SerializeField, Min(0.1f)] private float decayPerSecond = 1.8f;

    private float _trauma;
    private Vector3 _lastPositionOffset;
    private float _lastTilt;
    private float _seedX;
    private float _seedY;

    public static void Shake(float amount)
    {
        if (Instance == null) return;
        Instance._trauma = Mathf.Clamp01(Mathf.Max(Instance._trauma, amount));
    }

    private void Awake()
    {
        Instance = this;
        _seedX = Random.Range(0f, 1000f);
        _seedY = Random.Range(1000f, 2000f);
    }

    private void LateUpdate()
    {
        if (_trauma <= 0f)
        {
            if (_lastPositionOffset != Vector3.zero || _lastTilt != 0f) RemoveLastOffset();
            return;
        }

        float shake = _trauma * _trauma;
        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(_seedX, Time.unscaledTime * noiseFrequency) - .5f) * 2f,
            (Mathf.PerlinNoise(_seedY, Time.unscaledTime * noiseFrequency) - .5f) * 2f,
            0f) * shake * maxOffset;
        float tilt = (Mathf.PerlinNoise(_seedX + _seedY, Time.unscaledTime * noiseFrequency) - .5f) * 2f * shake * maxTilt;

        transform.position += offset - _lastPositionOffset;
        transform.Rotate(Vector3.forward, tilt - _lastTilt, Space.Self);
        _lastPositionOffset = offset;
        _lastTilt = tilt;

        _trauma = Mathf.Max(0f, _trauma - decayPerSecond * Time.unscaledDeltaTime);
    }

    private void RemoveLastOffset()
    {
        transform.position -= _lastPositionOffset;
        transform.Rotate(Vector3.forward, -_lastTilt, Space.Self);
        _lastPositionOffset = Vector3.zero;
        _lastTilt = 0f;
    }
}
