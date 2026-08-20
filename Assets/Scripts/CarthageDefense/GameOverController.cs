using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shows a GAME OVER panel and pauses play when the Carthage heart is destroyed. TRY AGAIN reloads the scene.</summary>
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }
    private const string BestWaveKey = "CarthageDefense.BestWaveReached";
    private GameObject _panel;
    private CanvasGroup _panelGroup;
    private RectTransform _titleRect;
    private RectTransform _buttonRect;
    private TextMeshProUGUI _statsText;
    private RectTransform _statsRect;
    private Coroutine _introRoutine;

    public static GameOverController Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Game Over Controller").AddComponent<GameOverController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CartageHeart.Destroyed += ShowGameOver;
        CreatePanel();
    }

    private void OnDestroy()
    {
        CartageHeart.Destroyed -= ShowGameOver;
        if (Instance == this) Instance = null;
    }

    private void CreatePanel()
    {
        GameObject root = new GameObject("Game Over UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("Panel", typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(root.transform, false);
        Image background = panel.GetComponent<Image>();
        background.color = CarthageTheme.Overlay;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        _panel = panel;
        _panelGroup = panel.GetComponent<CanvasGroup>();

        RectTransform dialog = CarthageTheme.CreateFramedPanel("Game Over Dialog", panel.transform, CarthageTheme.Panel, 4f);
        dialog.anchorMin = new Vector2(.32f, .3f);
        dialog.anchorMax = new Vector2(.68f, .68f);
        dialog.offsetMin = dialog.offsetMax = Vector2.zero;

        GameObject titleObject = new GameObject("Title", typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(dialog, false);
        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 64;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = CarthageTheme.Gold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(.05f, .62f);
        titleRect.anchorMax = new Vector2(.95f, .88f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
        _titleRect = titleRect;

        GameObject statsObject = new GameObject("Stats", typeof(TextMeshProUGUI));
        statsObject.transform.SetParent(dialog, false);
        _statsText = statsObject.GetComponent<TextMeshProUGUI>();
        _statsText.fontSize = 28;
        _statsText.alignment = TextAlignmentOptions.Center;
        _statsText.color = CarthageTheme.Cream;
        RectTransform statsRect = _statsText.rectTransform;
        statsRect.anchorMin = new Vector2(.05f, .44f);
        statsRect.anchorMax = new Vector2(.95f, .58f);
        statsRect.offsetMin = statsRect.offsetMax = Vector2.zero;
        _statsRect = statsRect;

        GameObject buttonObject = new GameObject("Try Again Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(dialog, false);
        buttonObject.GetComponent<Image>().color = CarthageTheme.ButtonNegative;
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(.2f, .14f);
        buttonRect.anchorMax = new Vector2(.8f, .3f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        _buttonRect = buttonRect;

        GameObject labelObject = new GameObject("Text", typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "TRY AGAIN";
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = CarthageTheme.Cream;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        buttonObject.GetComponent<Button>().onClick.AddListener(RestartGame);

        _panel.SetActive(false);
    }

    private void ShowGameOver()
    {
        // The wave index only increments once a wave is fully cleared, so at the moment of death it still
        // holds whichever wave was in progress — +1 to show it as the 1-based number the player sees in the HUD.
        int waveReached = (GameManger.Instance != null ? GameManger.Instance.CurrentWaveIndex : 0) + 1;
        int bestWave = PlayerPrefs.GetInt(BestWaveKey, 0);
        bool isNewBest = waveReached > bestWave;
        if (isNewBest) { bestWave = waveReached; PlayerPrefs.SetInt(BestWaveKey, bestWave); PlayerPrefs.Save(); }
        if (_statsText != null)
            _statsText.text = "Wave reached: " + waveReached + (isNewBest ? "  (NEW BEST!)" : "") + "\nBest: " + bestWave;

        if (GameManger.Instance != null) GameManger.Instance.ResetWaveSystem();
        SfxManager.Instance?.PlayHeartDestroyed();
        MusicManager.Instance?.PlayGameOverMusic();
        if (_panel != null)
        {
            _panel.SetActive(true);
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            _introRoutine = StartCoroutine(AnimateIntro());
        }
        Time.timeScale = 0f;
    }

    // Uses unscaled time throughout since Time.timeScale is set to 0 right after this fires — the panel
    // still needs to animate in while the game is frozen behind it.
    private IEnumerator AnimateIntro()
    {
        _panelGroup.alpha = 0f;
        Vector3 titleBase = Vector3.one;
        Vector3 statsBase = Vector3.one;
        Vector3 buttonBase = Vector3.one;
        _titleRect.localScale = Vector3.zero;
        _statsRect.localScale = Vector3.zero;
        _buttonRect.localScale = Vector3.zero;

        const float fadeDuration = .3f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _panelGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _panelGroup.alpha = 1f;

        const float bounceDuration = .5f;
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            float titleScale = OvershootEase(t);
            _titleRect.localScale = titleBase * titleScale;
            float statsT = Mathf.Clamp01((elapsed - .1f) / bounceDuration);
            _statsRect.localScale = statsBase * OvershootEase(statsT);
            float buttonT = Mathf.Clamp01((elapsed - .2f) / bounceDuration);
            _buttonRect.localScale = buttonBase * OvershootEase(buttonT);
            yield return null;
        }
        _titleRect.localScale = titleBase;
        _statsRect.localScale = statsBase;
        _buttonRect.localScale = buttonBase;
    }

    // Classic "back out" overshoot: scales past 1 then settles, giving the text a punchy pop-in feel.
    private static float OvershootEase(float t)
    {
        t = Mathf.Clamp01(t);
        const float overshoot = 1.7f;
        float shifted = t - 1f;
        return 1f + shifted * shifted * ((overshoot + 1f) * shifted + overshoot);
    }

    private void RestartGame()
    {
        SfxManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
