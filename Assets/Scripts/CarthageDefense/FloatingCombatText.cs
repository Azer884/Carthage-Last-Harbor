using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Screen-space text that rises from a world position and fades out. Used for money/crew/damage deltas.</summary>
public class FloatingCombatText : MonoBehaviour
{
    private static Canvas _canvas;

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
        floating._worldPosition = worldPosition + RandomNearbyOffset();
        // Placed immediately instead of waiting for the first Update() — otherwise the RectTransform sits
        // at its default (canvas-center) position for one frame and visibly teleports to the right spot.
        floating.UpdateScreenPosition();
    }

    // Random offset close to the spawn point, with a guaranteed minimum radius so repeated hits at the
    // same spot (several ships damaging one target, several popups off one point) never land exactly on
    // top of each other regardless of how many texts are already active nearby.
    private static Vector3 RandomNearbyOffset()
    {
        Camera camera = Camera.main;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right; else right.Normalize();

        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(.45f, 1f);
        Vector3 horizontal = right * (Mathf.Cos(angle) * radius);
        float vertical = Random.Range(.15f, .7f) + Mathf.Abs(Mathf.Sin(angle)) * radius * .3f;
        return horizontal + Vector3.up * vertical;
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

    private void UpdateScreenPosition()
    {
        Camera camera = Camera.main;
        if (camera == null || _text == null) return;
        Vector3 screenPoint = camera.WorldToScreenPoint(_worldPosition);
        _text.enabled = screenPoint.z > 0f;
        if (_text.enabled) _text.rectTransform.position = screenPoint;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        _worldPosition += Vector3.up * (riseSpeed * Time.deltaTime);
        UpdateScreenPosition();

        float t = lifetime > 0f ? _elapsed / lifetime : 1f;
        if (_text != null)
        {
            Color color = _text.color;
            color.a = Mathf.Clamp01(1f - t);
            _text.color = color;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
