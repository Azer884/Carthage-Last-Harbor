using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows a rotating strip of gameplay tips during the extended wave-1 reading window (see
/// GameManger.firstWaveAutoStartDelay) and disappears for good the instant wave 1 actually starts. Purely
/// informational — doesn't touch pacing itself, just narrates it.</summary>
public class TutorialTipsUI : MonoBehaviour
{
    public static TutorialTipsUI Instance { get; private set; }

    [SerializeField, Min(1f)] private float secondsPerTip = 6f;
    private static readonly string[] Tips =
    {
        "Build a Resource Tower now — it starts earning coin the moment the first wave begins.",
        "Building your first attack tower? Start with the Lighthouse of Baal Hammon.",
        "Every ship needs crew. Train Recruits into Sailors, Veterans, and the Sacred Band at El Jem Colosseum.",
        "Short on crew? The Mercenary Market sells reinforcements for coin.",
        "Upgrading a tower can change what it fires, not just how hard it hits.",
        "Press Esc any time to pause, adjust volume, or restart.",
    };

    private GameObject _panel;
    private TextMeshProUGUI _tipText;
    private int _tipIndex;
    private float _nextTipTime;

    public static TutorialTipsUI Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Tutorial Tips UI").AddComponent<TutorialTipsUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreatePanel();
    }

    private void Update()
    {
        bool shouldShow = GameManger.Instance != null && !GameManger.FirstWaveHasStarted;
        if (_panel.activeSelf != shouldShow) _panel.SetActive(shouldShow);
        if (!shouldShow) return;

        if (Time.time >= _nextTipTime)
        {
            _tipIndex = (_tipIndex + 1) % Tips.Length;
            _tipText.text = "TIP — " + Tips[_tipIndex];
            _nextTipTime = Time.time + secondsPerTip;
        }
    }

    private void CreatePanel()
    {
        GameObject root = new GameObject("Tutorial Tips Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform panel = CarthageTheme.CreateFramedPanel("Tip Banner", root.transform, CarthageTheme.PanelDim, 3f);
        panel.anchorMin = new Vector2(.30f, .80f); panel.anchorMax = new Vector2(.82f, .875f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        _panel = panel.gameObject;

        GameObject textObject = new GameObject("Tip Text", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel, false);
        _tipText = textObject.GetComponent<TextMeshProUGUI>();
        _tipText.fontSize = 20;
        _tipText.color = CarthageTheme.Gold;
        _tipText.alignment = TextAlignmentOptions.Center;
        _tipText.enableWordWrapping = true;
        _tipText.overflowMode = TextOverflowModes.Overflow;
        _tipText.text = "TIP — " + Tips[0];
        RectTransform textRect = _tipText.rectTransform;
        textRect.anchorMin = new Vector2(.05f, .1f); textRect.anchorMax = new Vector2(.95f, .9f);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        _nextTipTime = Time.time + secondsPerTip;
        _panel.SetActive(false);
    }
}
