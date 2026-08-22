using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Selects completed Carthaginian buildings and provides their management panel.</summary>
public class TowerSelectionManager : MonoBehaviour
{
    public static TowerSelectionManager Instance { get; private set; }
    [SerializeField, Range(.1f, .95f)] private float sellRefundFraction = .6f;
    private TowerPlacementController _placement;
    private DragonTowerPlacementController _dragonPlacement;
    private GameObject _selected;
    // Multi-part models (e.g. Sidi Bou Said) carry an Outline on every mesh piece, not just one, so the
    // whole silhouette lights up together instead of a single arbitrary part.
    private Outline[] _selectedOutlines = System.Array.Empty<Outline>();
    private GameObject _hoverTarget;
    private Outline[] _hoverOutlines = System.Array.Empty<Outline>();
    private SightRangeRing _selectedRing;
    private GameObject _panel;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _details;
    private Button _upgrade;
    private TextMeshProUGUI _upgradeText;
    private TextMeshProUGUI _upgradeHint;
    private Button _sell;
    private TextMeshProUGUI _sellText;
    private Button[] _trainButtons;
    private TextMeshProUGUI[] _trainButtonTexts;
    private TMP_InputField[] _trainInputs;

    public static TowerSelectionManager Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Tower Selection Manager").AddComponent<TowerSelectionManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _placement = FindFirstObjectByType<TowerPlacementController>();
        _dragonPlacement = FindFirstObjectByType<DragonTowerPlacementController>();
        CreatePanel();
    }

    private void Start()
    {
        // Also supports towers that were already placed in the scene before this manager existed.
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None)) EnsureSelectableCollider(tower.gameObject);
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None)) EnsureSelectableCollider(resource.gameObject);
        foreach (JemColosseum jem in FindObjectsByType<JemColosseum>(FindObjectsSortMode.None)) EnsureSelectableCollider(jem.gameObject);
        foreach (CartageHeart heart in FindObjectsByType<CartageHeart>(FindObjectsSortMode.None)) EnsureSelectableCollider(heart.gameObject);
        foreach (CarthaginianShipCombat ship in FindObjectsByType<CarthaginianShipCombat>(FindObjectsSortMode.None)) EnsureSelectableCollider(ship.gameObject);
        SetAllSelectableOutlines(false);
    }

    public static void EnsureSelectableCollider(GameObject building) => EnsureSelectableCollider(building, Vector3.one);

    // sizeScale lets a caller shrink the auto-fitted box per-axis before it's applied — e.g. a model whose
    // raw render bounds (wingspan, raised head) are much wider than its actual footprint.
    public static void EnsureSelectableCollider(GameObject building, Vector3 sizeScale)
    {
        if (building == null || building.GetComponentInChildren<Collider>() != null) return;
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        BoxCollider collider = building.AddComponent<BoxCollider>();
        if (renderers.Length == 0) { collider.size = Vector3.one * 2f; return; }
        Bounds worldBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) worldBounds.Encapsulate(renderer.bounds);
        Vector3 localMin = building.transform.InverseTransformPoint(worldBounds.min);
        Vector3 localMax = building.transform.InverseTransformPoint(worldBounds.max);
        collider.center = (localMin + localMax) * .5f;
        Vector3 fullSize = new Vector3(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y), Mathf.Abs(localMax.z - localMin.z));
        collider.size = Vector3.Scale(fullSize, sizeScale);
    }

    // Ships are spawned at runtime, never hand-authored with an Outline like scene-placed towers, so give
    // them one on demand (disabled — hover/select turns it on) the first time selection logic touches them.
    public static void EnsureSelectableOutline(GameObject building)
    {
        if (building == null || building.GetComponentInChildren<Outline>(true) != null) return;
        building.AddComponent<Outline>().enabled = false;
    }

    private void Update()
    {
        if (_selected != null && _selected.GetComponent<CartageHeart>() == null) RefreshPanel();
        else if (_selected == null) HidePanel();
        // Keeps the ring's radius live — e.g. growing smoothly the instant an upgrade increases sight
        // range, instead of only refreshing next time the tower is reselected.
        if (_selected != null && _selectedRing != null && TryGetRangeVisual(_selected, out float liveRange, out Color liveColor))
            _selectedRing.Show(liveRange, liveColor);
        if (Mouse.current == null || (_placement != null && _placement.IsPlacing) || (_dragonPlacement != null && _dragonPlacement.IsPlacing)) { SetHover(null); return; }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { SetHover(null); return; }
        Camera camera = Camera.main;
        if (camera == null) return;
        GameObject hovered = GetBuildingUnderPointer(camera, Mouse.current.position.ReadValue());
        SetHover(hovered);
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (hovered != null) { SfxManager.Instance?.PlayButtonClick(); Select(hovered); }
        else if (_selected != null) { SfxManager.Instance?.PlayButtonClick(); Deselect(); }
    }

    private GameObject GetBuildingUnderPointer(Camera camera, Vector2 screenPoint)
    {
        RaycastHit[] hits = Physics.RaycastAll(camera.ScreenPointToRay(screenPoint), Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        GameObject selected = null;
        foreach (RaycastHit hit in hits)
        {
            selected = GetSelectableBuilding(hit.collider);
            if (selected != null) break;
        }
        // Some water/terrain meshes can intercept every physics hit. Fall back to the visible tower model.
        return selected ?? FindBuildingNearScreenPoint(camera, screenPoint);
    }

    public void Select(GameObject building)
    {
        if (building == null) return;
        bool isHeart = building.GetComponent<CartageHeart>() != null;
        if (!isHeart && building.GetComponent<CarthaginianTower>() == null && building.GetComponent<CarthaginianResourceTower>() == null
            && building.GetComponent<JemColosseum>() == null && building.GetComponent<CarthaginianShipCombat>() == null) return;
        if (_selected == building) return;
        Deselect();
        _selected = building;
        // Active-only: towers built from per-level model children (Outline on each) only ever have one
        // level active at a time, so this only ever picks up whichever level is currently showing.
        _selectedOutlines = building.GetComponentsInChildren<Outline>();
        foreach (Outline outline in _selectedOutlines) if (outline != null) outline.enabled = true;
        if (TryGetRangeVisual(building, out float range, out Color rangeColor))
        {
            _selectedRing = building.GetComponent<SightRangeRing>();
            if (_selectedRing == null) _selectedRing = building.AddComponent<SightRangeRing>();
            _selectedRing.Show(range, rangeColor);
        }
        if (isHeart) { MercenaryMarketUI.Ensure().Show(); return; }
        _panel.SetActive(true);
        RefreshPanel();
    }

    public void Deselect()
    {
        // A selected object can be destroyed by something other than this manager — a ship sinking in
        // combat, a tower sold via other code paths — without ever going through Deselect() first, leaving
        // a stale reference here. Guard every touch of these cached arrays against that.
        foreach (Outline outline in _selectedOutlines) if (outline != null) outline.enabled = false;
        if (_selectedRing != null) _selectedRing.Hide();
        if (_selected != null && _selected.GetComponent<CartageHeart>() != null) MercenaryMarketUI.Instance?.Hide();
        _selected = null; _selectedOutlines = System.Array.Empty<Outline>(); _selectedRing = null;
        HidePanel();
    }

    private static readonly Color SightRangeColor = new Color(.3f, .85f, 1f, .16f);
    private static readonly Color SpawnRangeColor = new Color(1f, .75f, .2f, .16f);
    private static readonly Color ExtractionRangeColor = new Color(.45f, .85f, .35f, .16f);

    // Combat units show their real sight range in blue. Towers with no combat range of their own (ship
    // docks, resource extractors) instead show a cosmetic reach indicator in its own color, so every
    // selectable building displays *something* without conflating "how far it can see" with "how far it
    // spawns/mines from".
    private bool TryGetRangeVisual(GameObject building, out float range, out Color color)
    {
        CarthaginianStationaryTower stationary = building.GetComponent<CarthaginianStationaryTower>();
        if (stationary != null && stationary.ActiveStats != null) { range = stationary.ActiveStats.sightRange; color = SightRangeColor; return true; }
        CarthaginianDragonTower dragon = building.GetComponent<CarthaginianDragonTower>();
        if (dragon != null && dragon.ActiveStats != null) { range = dragon.ActiveStats.sightRange; color = SightRangeColor; return true; }
        CarthaginianShipCombat ship = building.GetComponent<CarthaginianShipCombat>();
        if (ship != null) { range = ship.SightRange; color = SightRangeColor; return true; }
        LighthouseSpawner spawner = building.GetComponent<LighthouseSpawner>();
        if (spawner != null) { range = spawner.DisplayRange; color = SpawnRangeColor; return true; }
        CarthaginianResourceTower resource = building.GetComponent<CarthaginianResourceTower>();
        if (resource != null) { range = resource.DisplayRange; color = ExtractionRangeColor; return true; }
        range = 0f; color = default; return false;
    }

    private GameObject GetSelectableBuilding(Collider collider)
    {
        if (collider == null) return null;
        CarthaginianTower tower = collider.GetComponentInParent<CarthaginianTower>();
        if (tower != null) return tower.gameObject;
        CarthaginianResourceTower resource = collider.GetComponentInParent<CarthaginianResourceTower>();
        if (resource != null) return resource.gameObject;
        JemColosseum jem = collider.GetComponentInParent<JemColosseum>();
        if (jem != null) return jem.gameObject;
        CartageHeart heart = collider.GetComponentInParent<CartageHeart>();
        if (heart != null) return heart.gameObject;
        CarthaginianShipCombat ship = collider.GetComponentInParent<CarthaginianShipCombat>();
        return ship != null ? ship.gameObject : null;
    }

    private void SetHover(GameObject building)
    {
        if (_hoverTarget == building) return;
        // Leave outlines the selection is still using alone — otherwise moving the mouse off a selected
        // building would turn its glow off even though it's still selected.
        foreach (Outline outline in _hoverOutlines)
            if (outline != null && System.Array.IndexOf(_selectedOutlines, outline) < 0) outline.enabled = false;
        _hoverTarget = building;
        _hoverOutlines = building != null ? building.GetComponentsInChildren<Outline>() : System.Array.Empty<Outline>();
        foreach (Outline outline in _hoverOutlines) if (outline != null) outline.enabled = true;
    }

    private void SetAllSelectableOutlines(bool enabled)
    {
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None))
            foreach (Outline outline in tower.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None))
            foreach (Outline outline in resource.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
        foreach (JemColosseum jem in FindObjectsByType<JemColosseum>(FindObjectsSortMode.None))
            foreach (Outline outline in jem.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
        foreach (CartageHeart heart in FindObjectsByType<CartageHeart>(FindObjectsSortMode.None))
            foreach (Outline outline in heart.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
        foreach (CarthaginianShipCombat ship in FindObjectsByType<CarthaginianShipCombat>(FindObjectsSortMode.None))
            foreach (Outline outline in ship.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
    }

    private GameObject FindBuildingNearScreenPoint(Camera camera, Vector2 screenPoint)
    {
        GameObject closest = null;
        float closestPixels = float.MaxValue;
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(tower.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(resource.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        foreach (JemColosseum jem in FindObjectsByType<JemColosseum>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(jem.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        foreach (CartageHeart heart in FindObjectsByType<CartageHeart>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(heart.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        foreach (CarthaginianShipCombat ship in FindObjectsByType<CarthaginianShipCombat>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(ship.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        return closest;
    }

    private void ConsiderVisibleBuilding(GameObject building, Camera camera, Vector2 screenPoint, ref GameObject closest, ref float closestPixels)
    {
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) if (!(renderer is LineRenderer)) bounds.Encapsulate(renderer.bounds);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x <= 1; x++) for (int y = 0; y <= 1; y++) for (int z = 0; z <= 1; z++)
        {
            Vector3 point = camera.WorldToScreenPoint(new Vector3(x == 0 ? bounds.min.x : bounds.max.x, y == 0 ? bounds.min.y : bounds.max.y, z == 0 ? bounds.min.z : bounds.max.z));
            if (point.z <= 0f) continue;
            min = Vector3.Min(min, point); max = Vector3.Max(max, point);
        }
        const float padding = 25f;
        if (screenPoint.x >= min.x - padding && screenPoint.x <= max.x + padding && screenPoint.y >= min.y - padding && screenPoint.y <= max.y + padding)
        {
            float depth = camera.WorldToScreenPoint(bounds.center).z;
            if (depth > 0f && depth < closestPixels) { closest = building; closestPixels = depth; }
            return;
        }
        Vector3 projected = camera.WorldToScreenPoint(bounds.center);
        if (projected.z <= 0f) return;
        float pixels = Vector2.Distance(screenPoint, new Vector2(projected.x, projected.y));
        if (pixels > 75f || pixels >= closestPixels) return;
        closest = building; closestPixels = pixels;
    }

    private void CreatePanel()
    {
        GameObject root = new GameObject("Selected Tower UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 110;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        RectTransform panel = CarthageTheme.CreateFramedPanel("Tower Details", root.transform, CarthageTheme.Panel, 3f);
        panel.anchorMin = new Vector2(.72f, .25f); panel.anchorMax = new Vector2(.98f, .75f); panel.offsetMin = panel.offsetMax = Vector2.zero;
        _panel = panel.gameObject;
        _title = CreateText(panel.transform, 23, TextAnchor.UpperCenter, new Vector2(.06f, .79f), new Vector2(.94f, .95f));
        _title.color = CarthageTheme.Gold; _title.fontStyle = FontStyles.Bold;
        _details = CreateScrollableText(panel.transform, new Vector2(.06f, .28f), new Vector2(.94f, .78f));
        _upgradeHint = CreateText(panel.transform, 13, TextAnchor.MiddleCenter, new Vector2(.08f, .255f), new Vector2(.92f, .29f));
        _upgradeHint.color = CarthageTheme.Gold;
        Button close = CreateButton(panel.transform, new Vector2(.82f, .87f), new Vector2(.94f, .95f));
        close.GetComponent<Image>().color = CarthageTheme.ButtonNegative;
        close.GetComponentInChildren<TextMeshProUGUI>().text = "X";
        close.onClick.AddListener(() => { SfxManager.Instance?.PlayButtonClick(); Deselect(); });
        _upgrade = CreateButton(panel.transform, new Vector2(.08f, .15f), new Vector2(.92f, .25f));
        _upgradeText = _upgrade.GetComponentInChildren<TextMeshProUGUI>();
        _upgrade.onClick.AddListener(UpgradeSelected);
        UpgradeRequirementHover upgradeHover = _upgrade.gameObject.AddComponent<UpgradeRequirementHover>();
        upgradeHover.Initialize(this);
        _sell = CreateButton(panel.transform, new Vector2(.08f, .04f), new Vector2(.92f, .13f));
        _sell.GetComponent<Image>().color = CarthageTheme.ButtonNegative;
        _sellText = _sell.GetComponentInChildren<TextMeshProUGUI>();
        _sellText.text = "SELL";
        _sell.onClick.AddListener(SellSelected);

        _trainButtons = new Button[3];
        _trainButtonTexts = new TextMeshProUGUI[3];
        _trainInputs = new TMP_InputField[3];
        for (int i = 0; i < _trainButtons.Length; i++)
        {
            float xMin = .08f + i * .29f;
            float xMax = xMin + .26f;
            TMP_InputField trainInput = CreateInputField(panel.transform, new Vector2(xMin, .255f), new Vector2(xMax, .305f));
            Button trainButton = CreateButton(panel.transform, new Vector2(xMin, .15f), new Vector2(xMax, .245f));
            TextMeshProUGUI trainText = trainButton.GetComponentInChildren<TextMeshProUGUI>();
            trainText.fontSize = 12;
            CrewRank rank = (CrewRank)i;
            int index = i;
            trainButton.onClick.AddListener(() => TrainSelected(rank, index));
            trainButton.gameObject.SetActive(false);
            trainInput.gameObject.SetActive(false);
            _trainButtons[i] = trainButton;
            _trainButtonTexts[i] = trainText;
            _trainInputs[i] = trainInput;
        }

        _panel.SetActive(false);
    }

    private TMP_InputField CreateInputField(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Quantity Input", typeof(Image), typeof(TMP_InputField));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(1f, 1f, 1f, .15f);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(obj.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 13; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        RectTransform textRect = text.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(4f, 1f); textRect.offsetMax = new Vector2(-4f, -1f);

        TMP_InputField field = obj.GetComponent<TMP_InputField>();
        field.textViewport = rect;
        field.textComponent = text;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.text = "1";
        return field;
    }

    private TextMeshProUGUI CreateText(Transform parent, int size, TextAnchor anchor, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Text", typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.fontSize = size; text.color = CarthageTheme.Cream; text.alignment = TmpTextUtility.ToTmpAlignment(anchor);
        text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
        RectTransform rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return text;
    }

    // Long ship/level descriptions no longer overflow the fixed-size panel — this wraps the details
    // text in a masked, draggable ScrollRect with a visible handle.
    private TextMeshProUGUI CreateScrollableText(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject scrollObject = new GameObject("Details Scroll", typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, .12f);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = min; scrollRectTransform.anchorMax = max; scrollRectTransform.offsetMin = scrollRectTransform.offsetMax = Vector2.zero;

        GameObject viewportObject = new GameObject("Viewport", typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        viewportObject.GetComponent<Image>().color = Color.white;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = new Vector2(.93f, 1f); viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

        GameObject contentObject = new GameObject("Content", typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        TextMeshProUGUI text = contentObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 17; text.color = CarthageTheme.Cream; text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Overflow;
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = new Vector2(1f, 1f); contentRect.pivot = new Vector2(.5f, 1f);
        contentRect.offsetMin = new Vector2(4f, 0f); contentRect.offsetMax = new Vector2(-4f, 0f);
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = new GameObject("Scrollbar", typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        scrollbarObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, .1f);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(.95f, 0f); scrollbarRect.anchorMax = new Vector2(1f, 1f); scrollbarRect.offsetMin = scrollbarRect.offsetMax = Vector2.zero;
        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject handleAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
        handleAreaObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero; handleAreaRect.anchorMax = Vector2.one; handleAreaRect.offsetMin = new Vector2(2f, 2f); handleAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject handleObject = new GameObject("Handle", typeof(Image));
        handleObject.transform.SetParent(handleAreaObject.transform, false);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(1f, .85f, .35f, .85f);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = new Vector2(1f, .3f); handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
        scrollbar.handleRect = handleRect; scrollbar.targetGraphic = handleImage;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        return text;
    }

    private Button CreateButton(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Button", typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = CarthageTheme.ButtonPositive;
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = CreateText(obj.transform, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one); text.text = "UPGRADE";
        text.color = CarthageTheme.Cream;
        return obj.GetComponent<Button>();
    }

    // gameObject.SetActive() is a no-op when the value isn't changing, but this panel refreshes every
    // frame — an unconditional SetActive(false) followed by SetActive(true) on the same object within
    // one call genuinely disables and re-enables it, which clears EventSystem focus. That was kicking
    // the player out of the train quantity InputField on every frame, mid-keystroke.
    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target.activeSelf != active) target.SetActive(active);
    }

    private void RefreshPanel()
    {
        if (_selected == null || _panel == null) return;
        SetActiveSafe(_sell.gameObject, true);
        if (_sellText != null) _sellText.text = "SELL";
        foreach (Button trainButton in _trainButtons) SetActiveSafe(trainButton.gameObject, false);
        foreach (TMP_InputField trainInput in _trainInputs) SetActiveSafe(trainInput.gameObject, false);
        CarthaginianTower tower = _selected.GetComponent<CarthaginianTower>();
        if (tower != null)
        {
            CarthaginianTowerDefinition definition = tower.Definition;
            _title.text = definition != null ? definition.towerName : _selected.name;
            _details.text = (definition != null ? definition.description : string.Empty) + "\n\nLevel: " + (tower.CurrentLevel + 1) + (definition != null && definition.levels != null ? " / " + definition.levels.Length : string.Empty)
                + "\n" + GetTowerStats(tower) + (tower.Sellable ? "\nSell value: " + GetSellValue() + " coin" : string.Empty);
            SetActiveSafe(_upgrade.gameObject, tower.CanUpgrade);
            if (tower.CanUpgrade)
            {
                _upgradeText.text = "UPGRADE — " + tower.NextUpgradeCost + " coin";
                _upgrade.interactable = tower.CanAffordUpgrade;
            }
            _upgradeHint.text = string.Empty;
            SetActiveSafe(_sell.gameObject, tower.Sellable);
            return;
        }
        JemColosseum jem = _selected.GetComponent<JemColosseum>();
        if (jem != null)
        {
            _title.text = "El Jem Colosseum";
            System.Text.StringBuilder details = new System.Text.StringBuilder("Trains crew into the next rank.\n");
            for (int i = 0; i < _trainButtons.Length; i++)
            {
                CrewRank rank = (CrewRank)i;
                int available = CrewRoster.Instance != null ? CrewRoster.Instance.GetAvailable(rank) : 0;
                details.Append("\n").Append(rank).Append(" → ").Append(rank + 1).Append(": ").Append(jem.TraineesPerPromotion).Append(" crew + ")
                    .Append(jem.GetTrainingCost(rank)).Append(" coin (").Append(available).Append(" available)");
                SetActiveSafe(_trainButtons[i].gameObject, true);
                SetActiveSafe(_trainInputs[i].gameObject, true);
                _trainButtons[i].interactable = jem.CanTrain(rank);
                _trainButtonTexts[i].text = "TRAIN " + rank + "\n" + jem.GetTrainingCost(rank) + " coin ea.";
            }
            _details.text = details.ToString();
            SetActiveSafe(_upgrade.gameObject, false);
            _upgradeHint.text = string.Empty;
            SetActiveSafe(_sell.gameObject, false);
            return;
        }
        CarthaginianShipCombat ship = _selected.GetComponent<CarthaginianShipCombat>();
        if (ship != null)
        {
            CarthaginianShipCrew crew = _selected.GetComponent<CarthaginianShipCrew>();
            _title.text = _selected.name.Replace("(Clone)", string.Empty).Trim();
            _details.text = "Crew aboard: " + (crew != null ? crew.CrewNumber : 0)
                + "\nDamage: " + ship.AttackDamage + "\nSight range: " + ship.SightRange + "\nAttack range: " + ship.AttackRange + "\nAttack cooldown: " + ship.AttackCooldown + " sec"
                + "\n" + ShipCounterTable.Describe(ship.CombatClass)
                + "\n\nScuttling returns its surviving crew to the roster and frees a build slot at its dock.";
            SetActiveSafe(_upgrade.gameObject, false);
            _upgradeHint.text = string.Empty;
            SetActiveSafe(_sell.gameObject, true);
            if (_sellText != null) _sellText.text = "SCUTTLE";
            return;
        }
        CarthaginianResourceTower resource = _selected.GetComponent<CarthaginianResourceTower>();
        CarthaginianResourceDefinition resourceDefinition = resource != null ? resource.Definition : null;
        _title.text = resourceDefinition != null ? resourceDefinition.buildingName : _selected.name;
        _details.text = (resourceDefinition != null ? resourceDefinition.description : string.Empty)
            + "\n\nWorkers: " + (resourceDefinition != null ? resourceDefinition.workersRequired : 0)
            + "\nIncome: " + (resourceDefinition != null ? resourceDefinition.IncomePerSecond.ToString("0.0") + " TND/sec" : "")
            + "\nSell value: " + GetSellValue() + " coin";
        SetActiveSafe(_upgrade.gameObject, false);
        _upgradeHint.text = string.Empty;
    }

    private string GetTowerStats(CarthaginianTower tower)
    {
        CarthaginianStationaryTower stationary = tower.GetComponent<CarthaginianStationaryTower>();
        if (stationary != null && stationary.ActiveStats != null)
        {
            StationaryTowerLevel stats = stationary.ActiveStats;
            return "Damage: " + stats.damage + "\nSight range: " + stats.sightRange + "\nAttack range: " + stats.attackRange + "\nAttack cooldown: " + stats.attackCooldown + " sec";
        }
        CarthaginianDragonTower dragon = tower.GetComponent<CarthaginianDragonTower>();
        if (dragon != null && dragon.ActiveStats != null)
        {
            DragonTowerLevel stats = dragon.ActiveStats;
            return "Damage: " + stats.damage + (stats.splashRadius > 0f ? " (splash " + stats.splashRadius + "m)" : "")
                + "\nSight range: " + stats.sightRange + "\nAttack range: " + stats.attackRange + "\nAttack cooldown: " + stats.attackCooldown + " sec";
        }
        SidiBouSaidTower sidiBouSaid = tower.GetComponent<SidiBouSaidTower>();
        if (sidiBouSaid != null) return "Generates " + sidiBouSaid.CrewPerSecond.ToString("0.00") + " crew/sec";
        TowerLevel level = tower.ActiveLevel;
        if (level == null || level.unlockedShips == null || level.unlockedShips.Length == 0) return "No ships unlocked.";
        System.Text.StringBuilder output = new System.Text.StringBuilder("Ships at sea: ").Append(tower.ActiveShipCount).Append(" / ").Append(tower.MaxActiveShips);
        output.Append("\n\nAvailable ships:");
        foreach (CarthaginianShipOption ship in level.unlockedShips)
        {
            if (ship == null) continue;
            bool affordable = EconomyManager.Instance != null && EconomyManager.Instance.Money >= ship.shipCost;
            int availableCrew = 0;
            if (CrewRoster.Instance != null)
                for (int rank = (int)ship.minimumRank; rank <= (int)CrewRank.SacredBand; rank++) availableCrew += CrewRoster.Instance.GetAvailable((CrewRank)rank);
            bool hasEnoughCrew = availableCrew >= ship.crewRequired;
            output.Append("\n• ").Append(ship.shipName).Append(" — ").Append(ship.shipCost).Append(" coin ").Append(affordable ? "[READY]" : "[NEED COIN]")
                .Append("\n  Crew: ").Append(hasEnoughCrew ? string.Empty : "<color=#FF6B5E>").Append(ship.crewRequired).Append(" ").Append(ship.minimumRank).Append("+ needed (")
                .Append(availableCrew).Append(" available)").Append(hasEnoughCrew ? string.Empty : "</color>").Append(", ").Append(ship.spawnCooldown).Append(" sec spawn");
            CarthaginianShipCombat combat = ship.shipPrefab != null ? ship.shipPrefab.GetComponent<CarthaginianShipCombat>() : null;
            if (combat != null)
            {
                output.Append("\n  DMG ").Append(combat.AttackDamage).Append(" | sight ").Append(combat.SightRange).Append(" | range ").Append(combat.AttackRange).Append(" | ").Append(combat.AttackCooldown).Append(" sec");
                output.Append("\n  ").Append(ShipCounterTable.Describe(combat.CombatClass));
            }
        }
        return output.ToString();
    }

    private int GetSellValue()
    {
        CarthaginianTower tower = _selected != null ? _selected.GetComponent<CarthaginianTower>() : null;
        if (tower != null && tower.Definition != null) return Mathf.FloorToInt(tower.Definition.buildCost * sellRefundFraction);
        CarthaginianResourceTower resource = _selected != null ? _selected.GetComponent<CarthaginianResourceTower>() : null;
        return resource != null && resource.Definition != null ? Mathf.FloorToInt(resource.Definition.buildCost * sellRefundFraction) : 0;
    }

    private void UpgradeSelected()
    {
        SfxManager.Instance?.PlayButtonClick();
        if (_selected == null) return;
        CarthaginianTower tower = _selected.GetComponent<CarthaginianTower>();
        if (tower == null) return;
        if (tower.TryUpgrade()) { RefreshSelectedOutlines(); return; }
        // Normally caught by the button's own interactable state, but crew/money can shift between
        // frames (e.g. a ship finishing a spawn cycle) — this is the fallback for that edge case.
        int money = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
        ErrorFeedback.Show(_selected.transform.position, money < tower.NextUpgradeCost ? "Not enough coin" : "Not enough crew");
    }

    // Upgrading swaps which level's model is active (a different per-level child, or — for Sidi Bou Said —
    // a different explicit model reference), so the outline set captured back at selection time is stale
    // the instant that happens. Without this the tower would look deselected right after upgrading, since
    // the old level's (now inactive) outlines are the only ones still tracked as "on".
    private void RefreshSelectedOutlines()
    {
        foreach (Outline outline in _selectedOutlines) if (outline != null) outline.enabled = false;
        _selectedOutlines = _selected != null ? _selected.GetComponentsInChildren<Outline>() : System.Array.Empty<Outline>();
        foreach (Outline outline in _selectedOutlines) if (outline != null) outline.enabled = true;
    }
    private void TrainSelected(CrewRank rank, int inputIndex)
    {
        SfxManager.Instance?.PlayButtonClick();
        if (_selected == null) return;
        JemColosseum jem = _selected.GetComponent<JemColosseum>();
        if (jem == null) return;
        if (!int.TryParse(_trainInputs[inputIndex].text, out int count)) count = 1;
        jem.TrainCount(rank, Mathf.Max(1, count));
    }
    public void ShowUpgradeRequirement()
    {
        CarthaginianTower tower = _selected != null ? _selected.GetComponent<CarthaginianTower>() : null;
        if (tower == null || !tower.CanUpgrade) return;
        int money = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
        int crew = tower.NextUpgradeCrewRequired;
        int availableCrew = 0;
        if (CrewRoster.Instance != null)
            for (int rank = (int)tower.NextUpgradeMinimumRank; rank <= (int)CrewRank.SacredBand; rank++) availableCrew += CrewRoster.Instance.GetAvailable((CrewRank)rank);
        _upgradeHint.text = "Money: " + tower.NextUpgradeCost + " required | " + money + " owned\nCrew: " + crew + " " + tower.NextUpgradeMinimumRank + "+ required | " + availableCrew + " available";
    }
    public void HideUpgradeRequirement() { if (_upgradeHint != null) _upgradeHint.text = string.Empty; }
    private void SellSelected()
    {
        SfxManager.Instance?.PlayButtonClick();
        if (_selected == null) return;
        if (_selected.GetComponent<CarthaginianShipCombat>() != null)
        {
            // No coin refund for scuttling — the payoff is CarthaginianShipCrew.OnDestroy() returning its
            // surviving crew to the roster and freeing a ship-capacity slot at its launching tower.
            GameObject ship = _selected; Deselect(); Destroy(ship);
            return;
        }
        CarthaginianTower tower = _selected.GetComponent<CarthaginianTower>();
        if (tower != null && !tower.Sellable) return;
        if (_selected.GetComponent<JemColosseum>() != null) return;
        CarthaginianResourceTower resource = _selected.GetComponent<CarthaginianResourceTower>();
        if (resource != null) resource.SellStoredResources();
        if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(GetSellValue());
        GameObject target = _selected; Deselect(); Destroy(target);
    }
    private void HidePanel() { if (_panel != null) _panel.SetActive(false); }
}

public class UpgradeRequirementHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TowerSelectionManager _manager;
    public void Initialize(TowerSelectionManager manager) { _manager = manager; }
    public void OnPointerEnter(PointerEventData eventData) { if (_manager != null) _manager.ShowUpgradeRequirement(); }
    public void OnPointerExit(PointerEventData eventData) { if (_manager != null) _manager.HideUpgradeRequirement(); }
}
