using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Screen-space text that rises from a world position and fades out. Used for money/crew/damage deltas.</summary>
public class FloatingCombatText : MonoBehaviour
{
    private static Canvas _canvas;
    private static readonly List<FloatingCombatText> _active = new List<FloatingCombatText>();

    [SerializeField] private float lifetime = 1.1f;
    [SerializeField] private float riseSpeed = 1.1f;

    private Vector3 _worldPosition;
    private TextMeshProUGUI _text;
    private float _elapsed;

    public static void Spawn(Vector3 worldPosition, string message, Color color)
    {
        EnsureCanvas();
        GameObject obj = new GameObject("Floating Text", typeof(TextMeshProUGUI));
        obj.transform.SetParent(_canvas.transform, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = message;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.sizeDelta = new Vector2(240f, 40f);

        FloatingCombatText floating = obj.AddComponent<FloatingCombatText>();
        floating._text = text;
        floating._worldPosition = worldPosition + StackOffset(worldPosition);
        _active.Add(floating);
    }

    // Staggers texts that spawn at nearly the same spot at nearly the same time, so repeated hits
    // (several ships damaging one target, several popups off one point) don't render on top of each other.
    private static Vector3 StackOffset(Vector3 worldPosition)
    {
        float vertical = 0f;
        foreach (FloatingCombatText other in _active)
        {
            if (other == null) continue;
            if (Vector3.Distance(other._worldPosition, worldPosition) < 1.5f) vertical += .5f;
        }
        Vector2 jitter = Random.insideUnitCircle * .35f;
        return new Vector3(jitter.x, vertical, jitter.y);
    }

    private static void EnsureCanvas()
    {
        if (_canvas != null) return;
        GameObject root = new GameObject("Floating Text Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Object.DontDestroyOnLoad(root);
        _canvas = canvas;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        _worldPosition += Vector3.up * (riseSpeed * Time.deltaTime);

        Camera camera = Camera.main;
        if (camera != null && _text != null)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(_worldPosition);
            _text.enabled = screenPoint.z > 0f;
            if (_text.enabled) _text.rectTransform.position = screenPoint;
        }

        float t = lifetime > 0f ? _elapsed / lifetime : 1f;
        if (_text != null)
        {
            Color color = _text.color;
            color.a = Mathf.Clamp01(1f - t);
            _text.color = color;
        }

        if (t >= 1f) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _active.Remove(this);
    }
}
