using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CarthageDefense
{
/// <summary>
/// Generates the complete build UI at runtime. Add it to any scene object: no Canvas, buttons,
/// panels, or EventSystem need to be created manually.
/// </summary>
public class CarthaginianBuildMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerPlacementController placementController;
    [SerializeField] private DragonTowerPlacementController dragonPlacementController;
    [Header("Tower catalogue")]
    [SerializeField] private CarthaginianTowerDefinition[] attackTowers;
    [SerializeField] private CarthaginianTowerDefinition[] defenseTowers;
    [SerializeField] private CarthaginianResourceDefinition[] resourceTowers;
    [Header("Layout")]
    [SerializeField, Range(0.15f, 0.5f)] private float menuHeightFraction = 0.25f;
    [SerializeField] private Color attackColor = CarthageTheme.CategoryAttack;
    [SerializeField] private Color defenseColor = CarthageTheme.CategoryDefense;
    [SerializeField] private Color resourceColor = CarthageTheme.CategoryResource;

    private TextMeshProUGUI _moneyText;
    private TextMeshProUGUI _crewText;
    private TextMeshProUGUI _workerText;
    private Button _startWaveButton;
    private TextMeshProUGUI _startWaveLabel;
    private TextMeshProUGUI _tooltip;
    private GameObject _tooltipPanel;
    private RectTransform _menu;
    private bool _isVisible = true;
    private bool _wasWaveRunning;
    private Image _heartFill;
    private float _heartFillTarget = 1f;
    private Coroutine _heartFillRoutine;
    private TextMeshProUGUI _heartText;
    private TextMeshProUGUI _timerText;
    private float _waveStartTime;
    private TextMeshProUGUI _incomingText;
    private TextMeshProUGUI _countdownText;
    private RectTransform _countdownRect;
    private int _lastCountdownShown = -1;
    private Coroutine _countdownPunchRoutine;
    private TextMeshProUGUI _waveBannerText;
    private Coroutine _waveBannerRoutine;
    private Button _speedButton;
    private TextMeshProUGUI _speedLabel;
    private static readonly float[] SpeedCycle = { 1.5f, 2f, 5f, 1f };
    private int _speedIndex = -1;
    private float _activeTimeScale = 1f;
    private bool _isPaused;
    private GameObject _pausePanel;
    private readonly List<(Button button, int cost)> _buildButtons = new List<(Button, int)>();

    private void Awake()
    {
        if (placementController == null) placementController = FindAnyObjectByType<TowerPlacementController>();
        if (dragonPlacementController == null) dragonPlacementController = FindAnyObjectByType<DragonTowerPlacementController>();
        EnsureEventSystem();
        TowerSelectionManager.Ensure();
        GameOverController.Ensure();
        SfxManager.Ensure();
        // Already playing build music if we arrived from the main menu; otherwise (e.g. Play started
        // directly on GameScene in the Editor) this is what actually kicks music off.
        MusicManager.Ensure().PlayBuildMusic();
        MercenaryMarket.Ensure();
        MercenaryMarketUI.Ensure();
        if (Camera.main != null && Camera.main.GetComponent<TopDownCameraController>() == null) Camera.main.gameObject.AddComponent<TopDownCameraController>();
        if (Camera.main != null && Camera.main.GetComponent<CameraShake>() == null) Camera.main.gameObject.AddComponent<CameraShake>();
        AmbientParticles.Ensure();
        EnsurePathArrowVisualizer();
        CreateMenu();
    }

    private void Start()
    {
        // GameManger.Instance is guaranteed set by the time any Start() runs, regardless of script order.
        if (GameManger.Instance != null)
        {
            GameManger.Instance.WaveStarted += OnWaveStarted;
            GameManger.Instance.WaveCompleted += OnWaveCompleted;
        }
    }

    private void OnDestroy()
    {
        if (GameManger.Instance != null)
        {
            GameManger.Instance.WaveStarted -= OnWaveStarted;
            GameManger.Instance.WaveCompleted -= OnWaveCompleted;
        }
    }

    private void OnWaveStarted(int waveIndex)
    {
        _waveStartTime = Time.time;
        if (_waveBannerText == null) return;
        if (_waveBannerRoutine != null) StopCoroutine(_waveBannerRoutine);
        _waveBannerRoutine = StartCoroutine(PlayWaveBanner("WAVE " + (waveIndex + 1), new Color(1f, .35f, .3f, 1f)));
    }

    private void OnWaveCompleted(int waveIndex)
    {
        if (_waveBannerText == null) return;
        if (_waveBannerRoutine != null) StopCoroutine(_waveBannerRoutine);
        _waveBannerRoutine = StartCoroutine(PlayWaveBanner("WAVE " + (waveIndex + 1) + " WON", new Color(.4f, 1f, .5f, 1f)));
    }

    private IEnumerator PlayWaveBanner(string text, Color color)
    {
        _waveBannerText.gameObject.SetActive(true);
        _waveBannerText.text = text;
        _waveBannerText.color = color;
        const float fadeIn = .35f, hold = 1f, fadeOut = .5f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTextAlpha(_waveBannerText, Mathf.Clamp01(elapsed / fadeIn));
            yield return null;
        }
        SetTextAlpha(_waveBannerText, 1f);
        yield return new WaitForSecondsRealtime(hold);
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTextAlpha(_waveBannerText, 1f - Mathf.Clamp01(elapsed / fadeOut));
            yield return null;
        }
        _waveBannerText.gameObject.SetActive(false);
    }

    private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            StandaloneInputModule legacyModule = existing.GetComponent<StandaloneInputModule>();
            if (legacyModule != null) Destroy(legacyModule);
            if (existing.GetComponent<InputSystemUIInputModule>() == null) existing.gameObject.AddComponent<InputSystemUIInputModule>();
            return;
        }
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private void CreateMenu()
    {
        GameObject root = new GameObject("Carthaginian Build Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform menu = CreatePanel("Three Build Sections", root.transform, CarthageTheme.Panel);
        _menu = menu;
        menu.anchorMin = new Vector2(0f, .15f); menu.anchorMax = new Vector2(.27f, .85f);
        menu.offsetMin = new Vector2(18f, 0f); menu.offsetMax = Vector2.zero;

        CreateSection(menu, "ATTACK FLEET", attackTowers, attackColor, 2);
        CreateSection(menu, "CORE DEFENSE", defenseTowers, defenseColor, 1);
        CreateResourceSection(menu, "RESOURCES", resourceTowers, resourceColor, 0);
        CreateStatusBar(root.transform);
        CreateTimerPanel(root.transform);
        CreateHealthBarPanel(root.transform);
        CreateIncomingPanel(root.transform);
        CreateCountdownBanner(root.transform);
        CreateWaveBanner(root.transform);
        CreateTooltip(root.transform);
        CreateToggleButton(root.transform);
        CreateSpeedButton(root.transform);
        CreatePausePanel(root.transform);
    }

    private void CreateWaveBanner(Transform parent)
    {
        GameObject textObject = new GameObject("Wave Start Banner", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        _waveBannerText = textObject.GetComponent<TextMeshProUGUI>();
        _waveBannerText.fontSize = 90;
        _waveBannerText.fontStyle = FontStyles.Bold;
        _waveBannerText.alignment = TextAlignmentOptions.Center;
        _waveBannerText.color = new Color(1f, .35f, .3f, 1f);
        _waveBannerText.enableWordWrapping = false;
        _waveBannerText.overflowMode = TextOverflowModes.Overflow;
        RectTransform rect = _waveBannerText.rectTransform;
        rect.anchorMin = new Vector2(.2f, .74f);
        rect.anchorMax = new Vector2(.8f, .86f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        _waveBannerText.gameObject.SetActive(false);
    }

    private void CreateSpeedButton(Transform parent)
    {
        _speedButton = CreateButton(parent as RectTransform, "1x", null, 0, 1);
        RectTransform rect = _speedButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.14f, .88f); rect.anchorMax = new Vector2(.27f, .95f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        CenterButtonLabel(_speedButton);
        _speedLabel = _speedButton.GetComponentInChildren<TextMeshProUGUI>();
        _speedButton.onClick.AddListener(CycleSpeed);
    }

    private void CycleSpeed()
    {
        SfxManager.Instance?.PlayButtonClick();
        _speedIndex = (_speedIndex + 1) % SpeedCycle.Length;
        _activeTimeScale = SpeedCycle[_speedIndex];
        if (!_isPaused) Time.timeScale = _activeTimeScale;
        if (_speedLabel != null) _speedLabel.text = _activeTimeScale + "x";
    }

    private void CreatePausePanel(Transform parent)
    {
        RectTransform overlay = CarthageTheme.CreateFlatPanel("Pause Panel", parent, CarthageTheme.Overlay);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        _pausePanel = overlay.gameObject;

        RectTransform dialog = CarthageTheme.CreateFramedPanel("Pause Dialog", overlay, CarthageTheme.Panel, 4f);
        dialog.anchorMin = new Vector2(.36f, .3f); dialog.anchorMax = new Vector2(.64f, .62f);
        dialog.offsetMin = dialog.offsetMax = Vector2.zero;

        TextMeshProUGUI paused = CreateText("PAUSED", dialog, 48, TextAnchor.MiddleCenter, new Vector2(.1f, .68f), new Vector2(.9f, .92f));
        paused.color = CarthageTheme.Gold;
        paused.fontStyle = FontStyles.Bold;

        Button resume = CreateButton(dialog, "Resume Button", null, 0, 1);
        RectTransform resumeRect = resume.GetComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(.15f, .38f); resumeRect.anchorMax = new Vector2(.85f, .58f);
        resumeRect.offsetMin = resumeRect.offsetMax = Vector2.zero;
        CenterButtonLabel(resume);
        resume.GetComponentInChildren<TextMeshProUGUI>().text = "RESUME";
        resume.onClick.AddListener(() => { SfxManager.Instance?.PlayButtonClick(); TogglePause(); });

        Button restart = CreateButton(dialog, "Restart Button", null, 0, 1);
        RectTransform restartRect = restart.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(.15f, .12f); restartRect.anchorMax = new Vector2(.85f, .32f);
        restartRect.offsetMin = restartRect.offsetMax = Vector2.zero;
        CenterButtonLabel(restart);
        restart.GetComponent<Image>().color = CarthageTheme.ButtonNegative;
        restart.GetComponentInChildren<TextMeshProUGUI>().text = "RESTART";
        restart.onClick.AddListener(() =>
        {
            SfxManager.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        _pausePanel.SetActive(false);
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : _activeTimeScale;
        if (_pausePanel != null) _pausePanel.SetActive(_isPaused);
    }

    private void HandlePauseInput()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (CartageHeart.HasBeenDestroyed) return; // GameOverController already owns timeScale at that point.
        TogglePause();
    }

    // Bottom-right of the HUD row was empty (health bar only spans .38-.62) — a natural home for wave info.
    private void CreateIncomingPanel(Transform parent)
    {
        RectTransform bar = CreatePanel("Ships Incoming", parent, CarthageTheme.Panel);
        bar.anchorMin = new Vector2(.64f, .015f);
        bar.anchorMax = new Vector2(.98f, .075f);
        bar.offsetMin = bar.offsetMax = Vector2.zero;

        _incomingText = CreateText(string.Empty, bar, 14, TextAnchor.MiddleCenter, new Vector2(.04f, .1f), new Vector2(.96f, .9f));
        _incomingText.color = new Color(1f, .55f, .5f, 1f);
    }

    private void CreateCountdownBanner(Transform parent)
    {
        GameObject textObject = new GameObject("Wave Countdown", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        _countdownText = textObject.GetComponent<TextMeshProUGUI>();
        _countdownText.fontSize = 160;
        _countdownText.fontStyle = FontStyles.Bold;
        _countdownText.alignment = TextAlignmentOptions.Center;
        _countdownText.color = CarthageTheme.Gold;
        _countdownText.enableWordWrapping = false;
        _countdownText.overflowMode = TextOverflowModes.Overflow;
        _countdownRect = _countdownText.rectTransform;
        _countdownRect.anchorMin = new Vector2(.3f, .35f);
        _countdownRect.anchorMax = new Vector2(.7f, .65f);
        _countdownRect.offsetMin = _countdownRect.offsetMax = Vector2.zero;
        _countdownText.gameObject.SetActive(false);
    }

    private void CreateTimerPanel(Transform parent)
    {
        RectTransform bar = CreatePanel("Wave Timer", parent, CarthageTheme.Panel);
        bar.anchorMin = new Vector2(0f, .015f);
        bar.anchorMax = new Vector2(.20f, .075f);
        bar.offsetMin = new Vector2(18f, 0f);
        bar.offsetMax = Vector2.zero;

        _timerText = CreateText(string.Empty, bar, 14, TextAnchor.MiddleCenter, new Vector2(.04f, .1f), new Vector2(.96f, .9f));
        _timerText.color = CarthageTheme.Gold;
    }

    private void CreateHealthBarPanel(Transform parent)
    {
        RectTransform bar = CreatePanel("Heart Health Bar", parent, CarthageTheme.Panel);
        bar.anchorMin = new Vector2(.38f, .015f);
        bar.anchorMax = new Vector2(.62f, .075f);
        bar.offsetMin = bar.offsetMax = Vector2.zero;

        _heartText = CreateText("Heart: -- / --", bar, 14, TextAnchor.MiddleCenter, new Vector2(.04f, .55f), new Vector2(.96f, .95f));

        RectTransform healthBackground = CreatePanel("Heart Health Background", bar, new Color(.12f, .04f, .04f, 1f));
        healthBackground.anchorMin = new Vector2(.04f, .12f);
        healthBackground.anchorMax = new Vector2(.96f, .48f);
        healthBackground.offsetMin = healthBackground.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill", typeof(Image));
        fillObject.transform.SetParent(healthBackground, false);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = FloatingHealthBar.GetSolidSprite();
        fillImage.color = new Color(.8f, .18f, .16f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        _heartFill = fillImage;
    }

    private void Update()
    {
        HandlePauseInput();
        RefreshStatusBar();
        RefreshStartWaveButton();
        RefreshBuildButtonAffordability();
        RefreshHud();
        RefreshIncoming();
        RefreshCountdown();
        HandleClickFeedback();
    }

    // Can't afford it → the button is disabled outright, so the player can't even enter placement/preview
    // mode for something they have no way to actually pay for.
    private void RefreshBuildButtonAffordability()
    {
        int money = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
        foreach ((Button button, int cost) in _buildButtons)
            if (button != null) button.interactable = money >= cost;
    }

    private void RefreshIncoming()
    {
        if (_incomingText == null) return;
        int queued = GameManger.Instance != null ? GameManger.Instance.ShipsQueuedThisWave : 0;
        int alive = 0;
        foreach (RomanShipHealth ship in FindObjectsByType<RomanShipHealth>(FindObjectsSortMode.None))
            if (!ship.IsDestroyed) alive++;
        int total = queued + alive;
        _incomingText.text = total > 0 ? "Roman ships incoming: " + total : "No ships incoming";
    }

    // Big animated number for the last few seconds before a wave auto-starts, punching in fresh each time
    // the displayed integer changes rather than replaying continuously.
    private void RefreshCountdown()
    {
        if (_countdownText == null) return;
        // Only the pre-wave delay drives the big number — it's the countdown that actually precedes ships
        // appearing, and every wave has one (>=4s), so it fires reliably whether the wave was auto-started
        // or clicked manually. The (much longer) auto-start idle wait already has its own small HUD timer;
        // also feeding it into this same countdown made an auto-started wave flash the big number twice in
        // a row — once as the idle wait ended, again a moment later as the pre-wave delay began — and cut
        // the second one's "4" down to a sliver since it interrupted the first one's fade-out.
        bool pending = GameManger.Instance != null && GameManger.Instance.IsPreWaveDelayActive;
        float remaining = pending ? GameManger.Instance.PreWaveDelayRemaining : 0f;
        int shown = pending && remaining <= 3.5f ? Mathf.CeilToInt(remaining) : -1;
        if (shown == _lastCountdownShown) return;
        _lastCountdownShown = shown;
        if (shown <= 0) { _countdownText.gameObject.SetActive(false); return; }
        _countdownText.gameObject.SetActive(true);
        _countdownText.text = shown.ToString();
        if (_countdownPunchRoutine != null) StopCoroutine(_countdownPunchRoutine);
        _countdownPunchRoutine = StartCoroutine(PunchCountdown());
    }

    // Genuine pop IN (overshoot scale-up from nothing) then pop OUT (shrink + fade) rather than just
    // settling at scale 1 and cutting — sized to fit roughly the ~1s window before the next number.
    private IEnumerator PunchCountdown()
    {
        const float popInDuration = .22f;
        const float holdDuration = .5f;
        const float popOutDuration = .25f;

        float elapsed = 0f;
        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _countdownRect.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(elapsed / popInDuration));
            SetCountdownAlpha(1f);
            yield return null;
        }
        _countdownRect.localScale = Vector3.one;
        SetCountdownAlpha(1f);
        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < popOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popOutDuration);
            _countdownRect.localScale = Vector3.one * Mathf.Lerp(1f, .3f, t);
            SetCountdownAlpha(1f - t);
            yield return null;
        }
        _countdownRect.localScale = Vector3.one;
    }

    private void SetCountdownAlpha(float alpha) => SetTextAlpha(_countdownText, alpha);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float shifted = t - 1f;
        return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
    }

    // Pure visual feedback — a tiny spark wherever the player clicks, regardless of what (if anything) it hits.
    private void HandleClickFeedback()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 500f))
            CombatFx.PlayClickSpark(hit.point);
    }

    private void RefreshHud()
    {
        if (_heartText != null)
        {
            CartageHeart heart = CartageHeart.Instance;
            int current = heart != null ? Mathf.Max(0, heart.CurrentHealth) : 0;
            int max = heart != null && heart.MaxHealth > 0 ? heart.MaxHealth : 1;
            _heartText.text = "Heart: " + current + " / " + max;
            float targetFraction = (float)current / max;
            if (_heartFill != null && !Mathf.Approximately(targetFraction, _heartFillTarget))
            {
                _heartFillTarget = targetFraction;
                if (_heartFillRoutine != null) StopCoroutine(_heartFillRoutine);
                _heartFillRoutine = StartCoroutine(AnimateHeartFill(targetFraction));
            }
        }

        if (_timerText == null) return;
        if (GameManger.Instance != null && GameManger.Instance.IsWaveRunning)
        {
            // No timer while a wave is actually running — "Ships Incoming" already covers wave progress.
            _timerText.text = string.Empty;
        }
        else if (GameManger.Instance != null && (GameManger.Instance.IsAutoStartPending || GameManger.Instance.IsPreWaveDelayActive))
        {
            float remaining = GameManger.Instance.IsAutoStartPending ? GameManger.Instance.AutoStartTimeRemaining : GameManger.Instance.PreWaveDelayRemaining;
            _timerText.text = "Next wave: " + Mathf.CeilToInt(remaining) + "s";
        }
        else
        {
            // Counts down once from AutoStartDelay (even with auto-start off) instead of an endlessly
            // rising session clock, then settles on a static ready prompt rather than looping pointlessly.
            float cycle = GameManger.Instance != null ? Mathf.Max(1f, GameManger.Instance.AutoStartDelay) : 20f;
            float sinceLoad = Time.timeSinceLevelLoad;
            _timerText.text = sinceLoad < cycle ? "Ready in: " + Mathf.CeilToInt(cycle - sinceLoad) + "s" : "Ready — click START WAVE";
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    private IEnumerator AnimateHeartFill(float target)
    {
        float start = _heartFill.fillAmount;
        const float duration = .35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _heartFill.fillAmount = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _heartFill.fillAmount = target;
        _heartFillRoutine = null;
    }

    private void RefreshStartWaveButton()
    {
        bool waveRunning = GameManger.Instance != null && GameManger.Instance.IsWaveRunning;
        if (waveRunning != _wasWaveRunning)
        {
            _wasWaveRunning = waveRunning;
            if (!waveRunning) MusicManager.Instance?.PlayBuildMusic();
        }

        if (_startWaveButton != null) _startWaveButton.interactable = !waveRunning;
        if (_startWaveLabel == null) return;

        if (waveRunning) _startWaveLabel.text = "WAVE ACTIVE";
        else if (GameManger.Instance != null && GameManger.Instance.IsAutoStartPending)
            _startWaveLabel.text = "START WAVE\n(" + Mathf.CeilToInt(GameManger.Instance.AutoStartTimeRemaining) + "s)";
        else _startWaveLabel.text = "START WAVE";
    }

    private void CreateSection(RectTransform parent, string heading, CarthaginianTowerDefinition[] definitions, Color color, int index)
    {
        RectTransform section = CreatePanel(heading, parent, color);
        section.anchorMin = new Vector2(0f, index / 3f); section.anchorMax = new Vector2(1f, (index + 1) / 3f);
        section.offsetMin = new Vector2(6f, 6f); section.offsetMax = new Vector2(-6f, -6f);
        StyleSectionHeading(CreateText(heading, section, 17, TextAnchor.UpperCenter, new Vector2(0f, .71f), new Vector2(1f, 1f)));
        if (definitions == null || definitions.Length == 0) { CreateText("No towers assigned", section, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1f, .71f)); return; }
        for (int i = 0; i < definitions.Length; i++) if (definitions[i] != null) CreateTowerButton(section, definitions[i], i, definitions.Length);
    }

    private void CreateResourceSection(RectTransform parent, string heading, CarthaginianResourceDefinition[] definitions, Color color, int index)
    {
        RectTransform section = CreatePanel(heading, parent, color);
        section.anchorMin = new Vector2(0f, index / 3f); section.anchorMax = new Vector2(1f, (index + 1) / 3f);
        section.offsetMin = new Vector2(6f, 6f); section.offsetMax = new Vector2(-6f, -6f);
        StyleSectionHeading(CreateText(heading, section, 17, TextAnchor.UpperCenter, new Vector2(0f, .71f), new Vector2(1f, 1f)));
        if (definitions == null || definitions.Length == 0) { CreateText("No towers assigned", section, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1f, .71f)); return; }
        for (int i = 0; i < definitions.Length; i++) if (definitions[i] != null) CreateResourceButton(section, definitions[i], i, definitions.Length);
    }

    private static void StyleSectionHeading(TextMeshProUGUI heading)
    {
        heading.color = CarthageTheme.Gold;
        heading.fontStyle = FontStyles.Bold;
    }

    private void CreateTowerButton(RectTransform parent, CarthaginianTowerDefinition definition, int index, int count)
    {
        Button button = CreateButton(parent, definition.towerName, definition.icon, index, count);
        bool placesOnStationarySlot = definition.prefab != null && definition.prefab.GetComponent<CarthaginianDragonTower>() != null;
        button.onClick.AddListener(() =>
        {
            SfxManager.Instance?.PlayButtonClick();
            if (placesOnStationarySlot) { if (dragonPlacementController != null) dragonPlacementController.SelectDragon(definition); }
            else { dragonPlacementController?.CancelPlacement(); if (placementController != null) placementController.SelectTower(definition); }
        });
        AddTooltip(button.gameObject, BuildTowerTooltip(definition));
        _buildButtons.Add((button, definition.buildCost));
    }

    private void CreateResourceButton(RectTransform parent, CarthaginianResourceDefinition definition, int index, int count)
    {
        Button button = CreateButton(parent, definition.buildingName, definition.icon, index, count);
        button.onClick.AddListener(() => { SfxManager.Instance?.PlayButtonClick(); dragonPlacementController?.CancelPlacement(); if (placementController != null) placementController.SelectResourceTower(definition); });
        AddTooltip(button.gameObject, BuildResourceTooltip(definition));
        _buildButtons.Add((button, definition.buildCost));
    }

    private Button CreateButton(RectTransform parent, string label, Sprite icon, int index, int count)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>(); image.color = CarthageTheme.PanelDim;
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = CarthageTheme.Gold; colors.pressedColor = CarthageTheme.Border; button.colors = colors;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        float width = 1f / count; rect.anchorMin = new Vector2(index * width, .06f); rect.anchorMax = new Vector2((index + 1) * width, .67f);
        rect.offsetMin = new Vector2(7f, 0f); rect.offsetMax = new Vector2(-7f, 0f);
        if (icon != null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(Image)); iconObject.transform.SetParent(buttonObject.transform, false);
            Image iconImage = iconObject.GetComponent<Image>(); iconImage.sprite = icon; iconImage.preserveAspect = true;
            RectTransform iconRect = iconObject.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(.12f, .29f); iconRect.anchorMax = new Vector2(.88f, .95f); iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        }
        CreateText(label, buttonObject.GetComponent<RectTransform>(), 15, TextAnchor.LowerCenter, new Vector2(.04f, .03f), new Vector2(.96f, .31f));
        return button;
    }

    private void CreateTooltip(Transform parent)
    {
        RectTransform panel = CreatePanel("Tower Tooltip", parent, CarthageTheme.PanelDim);
        panel.anchorMin = new Vector2(.35f, .28f); panel.anchorMax = new Vector2(.65f, .52f); panel.offsetMin = panel.offsetMax = Vector2.zero;
        _tooltipPanel = panel.gameObject;
        _tooltip = CreateText(string.Empty, panel, 17, TextAnchor.UpperLeft, new Vector2(.06f, .08f), new Vector2(.94f, .92f));
        _tooltipPanel.SetActive(false);
    }

    private void CreateToggleButton(Transform parent)
    {
        Button button = CreateButton(parent as RectTransform, "BUILD", null, 0, 1);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, .88f); rect.anchorMax = new Vector2(.13f, .95f);
        rect.offsetMin = new Vector2(18f, 0f); rect.offsetMax = Vector2.zero;
        CenterButtonLabel(button);
        button.onClick.AddListener(() => { SfxManager.Instance?.PlayButtonClick(); ToggleMenu(); });
    }

    private void CreateStatusBar(Transform parent)
    {
        RectTransform bar = CreatePanel("Status Bar", parent, CarthageTheme.Panel);
        bar.anchorMin = new Vector2(.30f, .91f);
        bar.anchorMax = new Vector2(.98f, .985f);
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
        _startWaveButton = CreateButton(bar, "START WAVE", null, 0, 1);
        RectTransform startRect = _startWaveButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(.02f, .15f);
        startRect.anchorMax = new Vector2(.20f, .85f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;
        CenterButtonLabel(_startWaveButton);
        _startWaveLabel = _startWaveButton.GetComponentInChildren<TextMeshProUGUI>();
        _startWaveButton.onClick.AddListener(() => { SfxManager.Instance?.PlayButtonClick(); GameManger.Instance?.StartWaveSystem(); });

        _moneyText = CreateText("Coins: -- TND", bar, 16, TextAnchor.MiddleLeft, new Vector2(.24f, .18f), new Vector2(.52f, .82f));
        _crewText = CreateText("Crew: --", bar, 16, TextAnchor.MiddleLeft, new Vector2(.54f, .18f), new Vector2(.78f, .82f));
        AddTooltip(_crewText.gameObject, BuildCrewBreakdownTooltip);
        _workerText = CreateText("Workers: --", bar, 16, TextAnchor.MiddleLeft, new Vector2(.80f, .18f), new Vector2(.98f, .82f));
    }

    // Recomputed fresh every time the player hovers, rather than baked once, since available crew per
    // rank changes constantly as ships are crewed, mercs are bought, and El Jem promotes trainees.
    private string BuildCrewBreakdownTooltip()
    {
        StringBuilder text = new StringBuilder("CREW AVAILABLE");
        if (CrewRoster.Instance == null) { text.Append("\n\nNo crew roster yet."); return text.ToString(); }
        foreach (CrewRank rank in Enum.GetValues(typeof(CrewRank)))
            text.Append("\n").Append(rank).Append(": ").Append(CrewRoster.Instance.GetAvailable(rank));
        return text.ToString();
    }

    private void EnsurePathArrowVisualizer()
    {
        if (FindPathArrowVisualizer() != null) return;

        Type visualizerType = FindTypeByName("PathArrowVisualizer");
        if (visualizerType == null) return;

        new GameObject("Path Arrow Visualizer").AddComponent(visualizerType);
    }

    private Component FindPathArrowVisualizer()
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (behaviour != null && behaviour.GetType().Name == "PathArrowVisualizer")
                return behaviour;
        return null;
    }

    private Type FindTypeByName(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null) return type;
        }

        return null;
    }

    private void RefreshStatusBar()
    {
        if (_moneyText != null)
        {
            _moneyText.text = EconomyManager.Instance != null ? "Coins: " + EconomyManager.Instance.Money + " TND" : "Coins: -- TND";
            if (EconomyManager.Instance != null && EconomyManager.Instance.Debt > 0)
                _moneyText.text += " (debt " + EconomyManager.Instance.Debt + ")";
        }

        if (_crewText != null)
            _crewText.text = "Crew available: " + GetTotalCrewAvailable();

        if (_workerText != null)
            _workerText.text = WorkerRoster.Instance != null ? "Workers available: " + WorkerRoster.Instance.AvailableWorkers : "Workers available: --";
    }

    private int GetTotalCrewAvailable()
    {
        if (CrewRoster.Instance == null) return 0;

        int total = 0;
        foreach (CrewRank rank in Enum.GetValues(typeof(CrewRank)))
            total += CrewRoster.Instance.GetAvailable(rank);
        return total;
    }

    private void CenterButtonLabel(Button button)
    {
        if (button == null) return;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;
        label.alignment = TextAlignmentOptions.Center;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(.04f, .05f);
        rect.anchorMax = new Vector2(.96f, .95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void ToggleMenu()
    {
        _isVisible = !_isVisible;
        if (_menu != null) _menu.gameObject.SetActive(_isVisible);
        HideTooltip();
    }

    // Gold-bordered by default (see CarthageTheme.CreateFramedPanel) — every HUD panel, section, and
    // dialog built through this helper picks up the frame automatically.
    private RectTransform CreatePanel(string objectName, Transform parent, Color color) => CarthageTheme.CreateFramedPanel(objectName, parent, color);

    private TextMeshProUGUI CreateText(string text, RectTransform parent, int fontSize, TextAnchor alignment, Vector2 min, Vector2 max)
    {
        GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI)); textObject.transform.SetParent(parent, false);
        TextMeshProUGUI result = textObject.GetComponent<TextMeshProUGUI>(); result.text = text; result.fontSize = fontSize; result.alignment = TmpTextUtility.ToTmpAlignment(alignment); result.color = CarthageTheme.Cream; result.enableWordWrapping = true; result.overflowMode = TextOverflowModes.Overflow;
        RectTransform rect = result.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return result;
    }

    private string BuildTowerTooltip(CarthaginianTowerDefinition definition)
    {
        bool placesOnStationarySlot = definition.prefab != null && definition.prefab.GetComponent<CarthaginianDragonTower>() != null;
        StringBuilder text = new StringBuilder();
        text.AppendLine(definition.towerName).AppendLine();
        text.AppendLine(string.IsNullOrWhiteSpace(definition.description) ? "No description has been entered." : definition.description);
        text.AppendLine().Append("Build cost: ").Append(definition.buildCost).Append(" coin");
        text.Append("\nPlacement: ").Append(placesOnStationarySlot ? "Carthage Port only (designated slot)"
            : string.IsNullOrEmpty(definition.requiredZoneId) ? "Any valid sea" : definition.requiredZoneId);
        if (definition.levels != null && definition.levels.Length > 0) text.Append("\nLevels: ").Append(definition.levels.Length);
        // Shown so the player knows, before ever placing the tower, what crew they'll need on hand to
        // actually crew its first ship — otherwise a freshly built tower can sit useless with no obvious reason why.
        if (definition.levels != null && definition.levels.Length > 0 && definition.levels[0].unlockedShips != null)
            foreach (CarthaginianShipOption ship in definition.levels[0].unlockedShips)
            {
                if (ship == null) continue;
                text.Append("\nShip: ").Append(ship.shipName).Append(" — ").Append(ship.shipCost).Append(" coin");
                text.Append("\n  Crew needed: ").Append(ship.crewRequired).Append(" ").Append(ship.minimumRank).Append("+");
                CarthaginianShipCombat combat = ship.shipPrefab != null ? ship.shipPrefab.GetComponent<CarthaginianShipCombat>() : null;
                if (combat != null) text.Append("\n  ").Append(ShipCounterTable.Describe(combat.CombatClass));
            }
        return text.ToString();
    }

    private string BuildResourceTooltip(CarthaginianResourceDefinition definition)
    {
        return definition.buildingName + "\n\n" + (string.IsNullOrWhiteSpace(definition.description) ? "No description has been entered." : definition.description)
            + "\n\nBuild cost: " + definition.buildCost + " coin"
            + "\nPlacement: " + definition.environment + (string.IsNullOrEmpty(definition.requiredZoneId) ? string.Empty : " — " + definition.requiredZoneId)
            + "\nWorkers: " + definition.workersRequired
            + "\nIncome: " + definition.unitsPerCycle + " " + definition.resource + " / " + definition.productionCycleSeconds + " sec";
    }

    private void AddTooltip(GameObject target, string text) => AddTooltip(target, () => text);

    private void AddTooltip(GameObject target, Func<string> textProvider)
    {
        BuildMenuTooltipHover hover = target.AddComponent<BuildMenuTooltipHover>();
        hover.Initialize(this, textProvider);
    }

    public void ShowTooltip(string text) { if (_tooltipPanel != null) { _tooltip.text = text; _tooltipPanel.SetActive(true); } }
    public void HideTooltip() { if (_tooltipPanel != null) _tooltipPanel.SetActive(false); }

#if UNITY_EDITOR
    [ContextMenu("Auto-fill Catalogue from Project")]
    private void AutoFillCatalogue()
    {
        List<CarthaginianTowerDefinition> attack = new List<CarthaginianTowerDefinition>();
        List<CarthaginianTowerDefinition> defense = new List<CarthaginianTowerDefinition>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:CarthaginianTowerDefinition"))
        {
            CarthaginianTowerDefinition tower = UnityEditor.AssetDatabase.LoadAssetAtPath<CarthaginianTowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (tower == null) continue;
            (tower.category == TowerCategory.Attack ? attack : defense).Add(tower);
        }
        List<CarthaginianResourceDefinition> resources = new List<CarthaginianResourceDefinition>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:CarthaginianResourceDefinition"))
            resources.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<CarthaginianResourceDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
        attackTowers = attack.ToArray(); defenseTowers = defense.ToArray(); resourceTowers = resources.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

public class BuildMenuTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CarthaginianBuildMenu _menu;
    private Func<string> _textProvider;
    public void Initialize(CarthaginianBuildMenu menu, Func<string> textProvider) { _menu = menu; _textProvider = textProvider; }
    // Called fresh on every hover rather than baking the string in once, so tooltips whose underlying
    // data changes at runtime (e.g. crew counts) never show stale numbers.
    public void OnPointerEnter(PointerEventData eventData) { if (_menu != null) _menu.ShowTooltip(_textProvider != null ? _textProvider() : string.Empty); }
    public void OnPointerExit(PointerEventData eventData) { if (_menu != null) _menu.HideTooltip(); }
}

}

