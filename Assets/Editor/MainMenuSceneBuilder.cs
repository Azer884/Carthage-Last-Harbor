using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds (or rebuilds) the Main Menu scene's UI as plain scene GameObjects: title, description,
/// Play/Settings/Credits/Quit buttons, a settings panel with music/SFX sliders, and a scrollable credits
/// panel populated from CREDITS.txt. Everything it creates is ordinary, hand-editable scene content —
/// restyle colors, text, or layout afterward in the Inspector. Safe to re-run any time: it deletes its own
/// previous output before rebuilding.</summary>
public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string StoneTexturePath = "Assets/Textures/Port/Clay001_1K-JPG_Color.jpg";
    private const string CreditsFilePath = "Assets/CREDITS.txt";

    // Sourced from CarthageTheme so the main menu and every in-game panel share one palette.
    private static readonly Color BgColor = CarthageTheme.Background;
    private static readonly Color StoneTintColor = new Color(1f, 1f, 1f, 0.16f);
    private static readonly Color PanelColor = CarthageTheme.Panel;
    private static readonly Color BorderColor = CarthageTheme.Border;
    private static readonly Color GoldColor = CarthageTheme.Gold;
    private static readonly Color CreamColor = CarthageTheme.Cream;
    private static readonly Color ButtonColor = CarthageTheme.Button;

    [MenuItem("Carthage/Build Main Menu Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        foreach (string name in new[] { "Main Menu Canvas", "EventSystem" })
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        TMP_DefaultControls.Resources tmpResources = GetTmpResources();
        DefaultControls.Resources uiResources = GetUguiResources();

        GameObject canvasGO = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        Transform root = canvasGO.transform;

        BuildBackground(root);
        BuildTitle(root);
        BuildDescriptionPanel(root);

        Button playButton = BuildMenuButton(root, tmpResources, "Play Button", "PLAY", new Vector2(380, 40));
        Button settingsButton = BuildMenuButton(root, tmpResources, "Settings Button", "SETTINGS", new Vector2(380, -60));
        Button creditsButton = BuildMenuButton(root, tmpResources, "Credits Button", "CREDITS", new Vector2(380, -160));
        Button quitButton = BuildMenuButton(root, tmpResources, "Quit Button", "QUIT", new Vector2(380, -260));

        GameObject settingsPanel = BuildSettingsPanel(root, tmpResources, uiResources, out Slider musicSlider, out Slider sfxSlider, out Button backButton);
        GameObject creditsPanel = BuildCreditsPanel(root, tmpResources, out TextMeshProUGUI creditsText, out Button creditsBackButton);

        MainMenuController controller = canvasGO.AddComponent<MainMenuController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.FindProperty("creditsFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(CreditsFilePath);
        so.FindProperty("creditsText").objectReferenceValue = creditsText;
        so.FindProperty("playButton").objectReferenceValue = playButton;
        so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.FindProperty("creditsButton").objectReferenceValue = creditsButton;
        so.FindProperty("creditsBackButton").objectReferenceValue = creditsBackButton;
        so.FindProperty("quitButton").objectReferenceValue = quitButton;
        so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
        so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main Menu scene built.");
    }

    private static void BuildBackground(Transform root)
    {
        Image bg = CreatePanel(root, "Background", BgColor);
        bg.raycastTarget = false;
        StretchFull(bg.rectTransform);

        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneTexturePath);
        if (stone == null) return;
        GameObject rawGO = new GameObject("Background Texture", typeof(RawImage));
        rawGO.transform.SetParent(root, false);
        RawImage raw = rawGO.GetComponent<RawImage>();
        raw.texture = stone;
        raw.color = StoneTintColor;
        raw.uvRect = new Rect(0, 0, 5, 5);
        raw.raycastTarget = false;
        StretchFull((RectTransform)rawGO.transform);
    }

    private static void BuildTitle(Transform root)
    {
        TextMeshProUGUI title = CreateText(root, "Title", "CARTHAGE: LAST HARBOR", 92, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center);
        title.outlineWidth = .2f;
        title.outlineColor = new Color(.05f, .03f, .02f);
        SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -70), new Vector2(1400, 130));

        TextMeshProUGUI subtitle = CreateText(root, "Subtitle", "Hold the Cothon. Break the Republic.", 32, CreamColor, FontStyles.Italic, TextAlignmentOptions.Center);
        SetRect(subtitle.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -195), new Vector2(1200, 60));
    }

    private static void BuildDescriptionPanel(Transform root)
    {
        RectTransform fill = CreateFramedPanel(root, "Description Panel", PanelColor, 4f);
        SetRect(fill.parent.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 80), new Vector2(680, 340));

        TextMeshProUGUI heading = CreateText(fill, "Description Heading", "HOW TO PLAY", 26, GoldColor, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        SetRect(heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(-48, 40));

        TextMeshProUGUI body = CreateText(fill, "Description Text",
            "Build towers and docks along the Cothon to sink Rome's ships before they reach the Heart.\n\n"
            + "• Select a building, then click the water or shore to place it (right-click cancels)\n"
            + "• Click a placed tower to view stats, upgrade, or sell it\n"
            + "• Pan with WASD / arrow keys, zoom with the scroll wheel, drag with the middle mouse button\n"
            + "• Start the wave when ready, use the speed button to fast-forward, Esc to pause",
            22, CreamColor, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        body.enableWordWrapping = true;
        SetRect(body.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(24, -70), new Vector2(-48, -90));
    }

    private static Button BuildMenuButton(Transform root, TMP_DefaultControls.Resources tmpResources, string name, string label, Vector2 anchoredPosition)
    {
        GameObject buttonGO = TMP_DefaultControls.CreateButton(tmpResources);
        buttonGO.name = name;
        buttonGO.transform.SetParent(root, false);
        SetRect((RectTransform)buttonGO.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), anchoredPosition, new Vector2(360, 80));

        Button button = buttonGO.GetComponent<Button>();
        StyleButton(button, ButtonColor);

        TextMeshProUGUI text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 30;
        text.fontStyle = FontStyles.Bold;
        text.color = CreamColor;
        return button;
    }

    private static GameObject BuildSettingsPanel(Transform root, TMP_DefaultControls.Resources tmpResources, DefaultControls.Resources uiResources,
        out Slider musicSlider, out Slider sfxSlider, out Button backButton)
    {
        Image backdrop = CreatePanel(root, "Settings Panel", new Color(0f, 0f, 0f, .72f));
        StretchFull(backdrop.rectTransform);

        RectTransform box = CreateFramedPanel(backdrop.transform, "Settings Box", PanelColor, 4f).parent.GetComponent<RectTransform>();
        SetRect(box, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 460));
        Transform boxFill = box.Find("Settings Box Fill");

        TextMeshProUGUI title = CreateText(boxFill, "Settings Title", "SETTINGS", 42, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -30), new Vector2(500, 60));

        TextMeshProUGUI musicLabel = CreateText(boxFill, "Music Label", "Music Volume", 26, CreamColor, FontStyles.Normal, TextAlignmentOptions.Left);
        SetRect(musicLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -130), new Vector2(300, 40));
        musicSlider = BuildSlider(boxFill, uiResources, "Music Slider", new Vector2(40, -175), .5f);

        TextMeshProUGUI sfxLabel = CreateText(boxFill, "Sfx Label", "SFX Volume", 26, CreamColor, FontStyles.Normal, TextAlignmentOptions.Left);
        SetRect(sfxLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -250), new Vector2(300, 40));
        sfxSlider = BuildSlider(boxFill, uiResources, "Sfx Slider", new Vector2(40, -295), .85f);

        GameObject backGO = TMP_DefaultControls.CreateButton(tmpResources);
        backGO.name = "Back Button";
        backGO.transform.SetParent(boxFill, false);
        SetRect((RectTransform)backGO.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(220, 64));
        backButton = backGO.GetComponent<Button>();
        StyleButton(backButton, ButtonColor);
        TextMeshProUGUI backText = backGO.GetComponentInChildren<TextMeshProUGUI>();
        backText.text = "BACK";
        backText.fontSize = 26;
        backText.fontStyle = FontStyles.Bold;
        backText.color = CreamColor;

        backdrop.gameObject.SetActive(false);
        return backdrop.gameObject;
    }

    private static GameObject BuildCreditsPanel(Transform root, TMP_DefaultControls.Resources tmpResources, out TextMeshProUGUI creditsText, out Button backButton)
    {
        Image backdrop = CreatePanel(root, "Credits Panel", new Color(0f, 0f, 0f, .72f));
        StretchFull(backdrop.rectTransform);

        RectTransform box = CreateFramedPanel(backdrop.transform, "Credits Box", PanelColor, 4f).parent.GetComponent<RectTransform>();
        SetRect(box, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 700));
        Transform boxFill = box.Find("Credits Box Fill");

        TextMeshProUGUI title = CreateText(boxFill, "Credits Title", "CREDITS", 42, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -30), new Vector2(500, 60));

        // Mask lives directly on the scroll view's own object rather than a separate viewport child —
        // simplest layout that still clips content to the visible area.
        GameObject scrollGO = new GameObject("Credits Scroll View", typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollGO.transform.SetParent(boxFill, false);
        Image scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, .25f);
        RectTransform scrollRt = (RectTransform)scrollGO.transform;
        SetStretch(scrollRt, 24, 24, 110, 90);

        creditsText = CreateText(scrollRt, "Credits Content", "", 24, CreamColor, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        creditsText.enableWordWrapping = true;
        creditsText.overflowMode = TextOverflowModes.Overflow;
        RectTransform contentRt = creditsText.rectTransform;
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(.5f, 1f);
        contentRt.anchoredPosition = new Vector2(0, 0);
        contentRt.sizeDelta = new Vector2(-32, 0);
        ContentSizeFitter fitter = creditsText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.viewport = scrollRt;
        scrollRect.content = contentRt;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        GameObject backGO = TMP_DefaultControls.CreateButton(tmpResources);
        backGO.name = "Credits Back Button";
        backGO.transform.SetParent(boxFill, false);
        SetRect((RectTransform)backGO.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(220, 64));
        backButton = backGO.GetComponent<Button>();
        StyleButton(backButton, ButtonColor);
        TextMeshProUGUI backText = backGO.GetComponentInChildren<TextMeshProUGUI>();
        backText.text = "BACK";
        backText.fontSize = 26;
        backText.fontStyle = FontStyles.Bold;
        backText.color = CreamColor;

        backdrop.gameObject.SetActive(false);
        return backdrop.gameObject;
    }

    private static Slider BuildSlider(Transform parent, DefaultControls.Resources uiResources, string name, Vector2 anchoredPosition, float value)
    {
        GameObject sliderGO = DefaultControls.CreateSlider(uiResources);
        sliderGO.name = name;
        sliderGO.transform.SetParent(parent, false);
        SetRect((RectTransform)sliderGO.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(540, 30));

        sliderGO.transform.Find("Background").GetComponent<Image>().color = new Color(0f, 0f, 0f, .45f);
        sliderGO.transform.Find("Fill Area/Fill").GetComponent<Image>().color = GoldColor;
        Image handle = sliderGO.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
        handle.color = CreamColor;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = value;
        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, .9f);
        colors.pressedColor = new Color(.8f, .8f, .8f);
        slider.colors = colors;
        return slider;
    }

    private static void StyleButton(Button button, Color normal) => CarthageTheme.StyleButton(button, normal);

    private static RectTransform CreateFramedPanel(Transform parent, string name, Color fillColor, float borderThickness)
    {
        RectTransform outer = CarthageTheme.CreateFramedPanel(name, parent, fillColor, borderThickness);
        return (RectTransform)outer.Find(name + " Fill");
    }

    private static Image CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetStretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static TMP_DefaultControls.Resources GetTmpResources() => new TMP_DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
        mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
    };

    private static DefaultControls.Resources GetUguiResources() => new DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
        mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
    };
}
