using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shows a GAME OVER panel and pauses play when the Carthage heart is destroyed. TRY AGAIN reloads the scene.</summary>
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }
    private GameObject _panel;

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

        GameObject panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(root.transform, false);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, .85f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        _panel = panel;

        GameObject titleObject = new GameObject("Title", typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 64;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(.9f, .2f, .15f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(.2f, .55f);
        titleRect.anchorMax = new Vector2(.8f, .72f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        GameObject buttonObject = new GameObject("Try Again Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        buttonObject.GetComponent<Image>().color = new Color(.5f, .14f, .1f, 1f);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(.4f, .4f);
        buttonRect.anchorMax = new Vector2(.6f, .48f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;

        GameObject labelObject = new GameObject("Text", typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "TRY AGAIN";
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        buttonObject.GetComponent<Button>().onClick.AddListener(RestartGame);

        _panel.SetActive(false);
    }

    private void ShowGameOver()
    {
        if (GameManger.Instance != null) GameManger.Instance.ResetWaveSystem();
        if (_panel != null) _panel.SetActive(true);
        SfxManager.Instance?.PlayHeartDestroyed();
        MusicManager.Instance?.PlayGameOverMusic();
        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        SfxManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
