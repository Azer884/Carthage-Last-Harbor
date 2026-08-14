using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Generates the complete build UI at runtime. Add it to any scene object: no Canvas, buttons,
/// panels, or EventSystem need to be created manually.
/// </summary>
public class CarthaginianBuildMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TowerPlacementController placementController;
    [Header("Tower catalogue")]
    [SerializeField] private CarthaginianTowerDefinition[] attackTowers;
    [SerializeField] private CarthaginianTowerDefinition[] defenseTowers;
    [SerializeField] private CarthaginianResourceDefinition[] resourceTowers;
    [Header("Layout")]
    [SerializeField, Range(0.15f, 0.5f)] private float menuHeightFraction = 0.25f;
    [SerializeField] private Color attackColor = new Color(0.55f, 0.16f, 0.11f, 0.95f);
    [SerializeField] private Color defenseColor = new Color(0.12f, 0.27f, 0.48f, 0.95f);
    [SerializeField] private Color resourceColor = new Color(0.18f, 0.40f, 0.24f, 0.95f);

    private Font _font;
    private Text _moneyText;
    private Text _crewText;
    private Text _workerText;
    private Button _startWaveButton;
    private Text _tooltip;
    private GameObject _tooltipPanel;
    private RectTransform _menu;
    private bool _isVisible = true;

    private void Awake()
    {
        if (placementController == null) placementController = FindAnyObjectByType<TowerPlacementController>();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        TowerSelectionManager.Ensure();
        if (Camera.main != null && Camera.main.GetComponent<TopDownCameraController>() == null) Camera.main.gameObject.AddComponent<TopDownCameraController>();
        EnsurePathArrowVisualizer();
        CreateMenu();
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

        RectTransform menu = CreatePanel("Three Build Sections", root.transform, new Color(0.025f, 0.035f, 0.06f, 0.93f));
        _menu = menu;
        menu.anchorMin = new Vector2(0f, .15f); menu.anchorMax = new Vector2(.27f, .85f);
        menu.offsetMin = new Vector2(18f, 0f); menu.offsetMax = Vector2.zero;

        CreateSection(menu, "ATTACK FLEET", attackTowers, attackColor, 2);
        CreateSection(menu, "CORE DEFENSE", defenseTowers, defenseColor, 1);
        CreateResourceSection(menu, "RESOURCES", resourceTowers, resourceColor, 0);
        CreateStatusBar(root.transform);
        CreateTooltip(root.transform);
        CreateToggleButton(root.transform);
    }

    private void Update()
    {
        RefreshStatusBar();
    }

    private void CreateSection(RectTransform parent, string heading, CarthaginianTowerDefinition[] definitions, Color color, int index)
    {
        RectTransform section = CreatePanel(heading, parent, color);
        section.anchorMin = new Vector2(0f, index / 3f); section.anchorMax = new Vector2(1f, (index + 1) / 3f);
        section.offsetMin = new Vector2(6f, 6f); section.offsetMax = new Vector2(-6f, -6f);
        CreateText(heading, section, 17, TextAnchor.UpperCenter, new Vector2(0f, .71f), new Vector2(1f, 1f));
        if (definitions == null || definitions.Length == 0) { CreateText("No towers assigned", section, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1f, .71f)); return; }
        for (int i = 0; i < definitions.Length; i++) if (definitions[i] != null) CreateTowerButton(section, definitions[i], i, definitions.Length);
    }

    private void CreateResourceSection(RectTransform parent, string heading, CarthaginianResourceDefinition[] definitions, Color color, int index)
    {
        RectTransform section = CreatePanel(heading, parent, color);
        section.anchorMin = new Vector2(0f, index / 3f); section.anchorMax = new Vector2(1f, (index + 1) / 3f);
        section.offsetMin = new Vector2(6f, 6f); section.offsetMax = new Vector2(-6f, -6f);
        CreateText(heading, section, 17, TextAnchor.UpperCenter, new Vector2(0f, .71f), new Vector2(1f, 1f));
        if (definitions == null || definitions.Length == 0) { CreateText("No towers assigned", section, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1f, .71f)); return; }
        for (int i = 0; i < definitions.Length; i++) if (definitions[i] != null) CreateResourceButton(section, definitions[i], i, definitions.Length);
    }

    private void CreateTowerButton(RectTransform parent, CarthaginianTowerDefinition definition, int index, int count)
    {
        Button button = CreateButton(parent, definition.towerName, definition.icon, index, count);
        button.onClick.AddListener(() => { if (placementController != null) placementController.SelectTower(definition); });
        AddTooltip(button.gameObject, BuildTowerTooltip(definition));
    }

    private void CreateResourceButton(RectTransform parent, CarthaginianResourceDefinition definition, int index, int count)
    {
        Button button = CreateButton(parent, definition.buildingName, definition.icon, index, count);
        button.onClick.AddListener(() => { if (placementController != null) placementController.SelectResourceTower(definition); });
        AddTooltip(button.gameObject, BuildResourceTooltip(definition));
    }

    private Button CreateButton(RectTransform parent, string label, Sprite icon, int index, int count)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>(); image.color = new Color(0.08f, 0.07f, 0.055f, .97f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(1f, .77f, .25f, 1f); colors.pressedColor = new Color(.78f, .46f, .12f, 1f); button.colors = colors;
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
        RectTransform panel = CreatePanel("Tower Tooltip", parent, new Color(.02f, .02f, .03f, .96f));
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
        button.onClick.AddListener(ToggleMenu);
    }

    private void CreateStatusBar(Transform parent)
    {
        RectTransform bar = CreatePanel("Status Bar", parent, new Color(0.025f, 0.035f, 0.06f, 0.93f));
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
        _startWaveButton.onClick.AddListener(() => GameManger.Instance?.StartWaveSystem());

        _moneyText = CreateText("Coins: -- TND", bar, 16, TextAnchor.MiddleLeft, new Vector2(.24f, .18f), new Vector2(.52f, .82f));
        _crewText = CreateText("Crew: --", bar, 16, TextAnchor.MiddleLeft, new Vector2(.54f, .18f), new Vector2(.78f, .82f));
        _workerText = CreateText("Workers: --", bar, 16, TextAnchor.MiddleLeft, new Vector2(.80f, .18f), new Vector2(.98f, .82f));
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
            _moneyText.text = EconomyManager.Instance != null ? "Coins: " + EconomyManager.Instance.Money + " TND" : "Coins: -- TND";

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

    public void ToggleMenu()
    {
        _isVisible = !_isVisible;
        if (_menu != null) _menu.gameObject.SetActive(_isVisible);
        HideTooltip();
    }

    private RectTransform CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName, typeof(Image)); panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>(); image.color = color;
        return panel.GetComponent<RectTransform>();
    }

    private Text CreateText(string text, RectTransform parent, int fontSize, TextAnchor alignment, Vector2 min, Vector2 max)
    {
        GameObject textObject = new GameObject("Text", typeof(Text)); textObject.transform.SetParent(parent, false);
        Text result = textObject.GetComponent<Text>(); result.font = _font; result.text = text; result.fontSize = fontSize; result.alignment = alignment; result.color = Color.white; result.horizontalOverflow = HorizontalWrapMode.Wrap; result.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = result.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return result;
    }

    private string BuildTowerTooltip(CarthaginianTowerDefinition definition)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine(definition.towerName).AppendLine();
        text.AppendLine(string.IsNullOrWhiteSpace(definition.description) ? "No description has been entered." : definition.description);
        text.AppendLine().Append("Build cost: ").Append(definition.buildCost).Append(" coin");
        text.Append("\nPlacement: ").Append(string.IsNullOrEmpty(definition.requiredZoneId) ? "Any valid sea" : definition.requiredZoneId);
        if (definition.levels != null && definition.levels.Length > 0) text.Append("\nLevels: ").Append(definition.levels.Length);
        if (definition.levels != null && definition.levels.Length > 0 && definition.levels[0].unlockedShips != null)
            foreach (CarthaginianShipOption ship in definition.levels[0].unlockedShips)
                if (ship != null) text.Append("\nShip: ").Append(ship.shipName).Append(" — ").Append(ship.shipCost).Append(" coin");
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

    private void AddTooltip(GameObject button, string text)
    {
        BuildMenuTooltipHover hover = button.AddComponent<BuildMenuTooltipHover>();
        hover.Initialize(this, text);
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
    private string _text;
    public void Initialize(CarthaginianBuildMenu menu, string text) { _menu = menu; _text = text; }
    public void OnPointerEnter(PointerEventData eventData) { if (_menu != null) _menu.ShowTooltip(_text); }
    public void OnPointerExit(PointerEventData eventData) { if (_menu != null) _menu.HideTooltip(); }
}
