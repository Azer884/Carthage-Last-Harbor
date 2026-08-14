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
    private GameObject _selected;
    private Outline _selectedOutline;
    private Outline _hoverOutline;
    private GameObject _panel;
    private Text _title;
    private Text _details;
    private Button _upgrade;
    private Text _upgradeText;
    private Text _upgradeHint;
    private Font _font;

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
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreatePanel();
    }

    private void Start()
    {
        // Also supports towers that were already placed in the scene before this manager existed.
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None)) EnsureSelectableCollider(tower.gameObject);
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None)) EnsureSelectableCollider(resource.gameObject);
        SetAllSelectableOutlines(false);
    }

    public static void EnsureSelectableCollider(GameObject building)
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
        collider.size = new Vector3(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y), Mathf.Abs(localMax.z - localMin.z));
    }

    private void Update()
    {
        if (_selected != null) RefreshPanel();
        else HidePanel();
        if (Mouse.current == null || (_placement != null && _placement.IsPlacing)) { SetHover(null); return; }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { SetHover(null); return; }
        Camera camera = Camera.main;
        if (camera == null) return;
        GameObject hovered = GetBuildingUnderPointer(camera, Mouse.current.position.ReadValue());
        SetHover(hovered);
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (hovered != null) Select(hovered); else Deselect();
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
        if (building == null || (building.GetComponent<CarthaginianTower>() == null && building.GetComponent<CarthaginianResourceTower>() == null)) return;
        if (_selected == building) return;
        Deselect();
        _selected = building;
        _selectedOutline = building.GetComponentInChildren<Outline>(true);
        if (_selectedOutline != null) _selectedOutline.enabled = true;
        _panel.SetActive(true);
        RefreshPanel();
    }

    public void Deselect()
    {
        if (_selectedOutline != null) _selectedOutline.enabled = false;
        _selected = null; _selectedOutline = null;
        HidePanel();
    }

    private GameObject GetSelectableBuilding(Collider collider)
    {
        if (collider == null) return null;
        CarthaginianTower tower = collider.GetComponentInParent<CarthaginianTower>();
        if (tower != null) return tower.gameObject;
        CarthaginianResourceTower resource = collider.GetComponentInParent<CarthaginianResourceTower>();
        return resource != null ? resource.gameObject : null;
    }

    private void SetHover(GameObject building)
    {
        Outline next = building != null ? building.GetComponentInChildren<Outline>(true) : null;
        if (_hoverOutline == next) return;
        if (_hoverOutline != null && _hoverOutline != _selectedOutline) _hoverOutline.enabled = false;
        _hoverOutline = next;
        if (_hoverOutline != null) _hoverOutline.enabled = true;
    }

    private void SetAllSelectableOutlines(bool enabled)
    {
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None))
            foreach (Outline outline in tower.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None))
            foreach (Outline outline in resource.GetComponentsInChildren<Outline>(true)) outline.enabled = enabled;
    }

    private GameObject FindBuildingNearScreenPoint(Camera camera, Vector2 screenPoint)
    {
        GameObject closest = null;
        float closestPixels = float.MaxValue;
        foreach (CarthaginianTower tower in FindObjectsByType<CarthaginianTower>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(tower.gameObject, camera, screenPoint, ref closest, ref closestPixels);
        foreach (CarthaginianResourceTower resource in FindObjectsByType<CarthaginianResourceTower>(FindObjectsSortMode.None))
            ConsiderVisibleBuilding(resource.gameObject, camera, screenPoint, ref closest, ref closestPixels);
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
        GameObject panel = new GameObject("Tower Details", typeof(Image)); panel.transform.SetParent(root.transform, false);
        Image image = panel.GetComponent<Image>(); image.color = new Color(.025f, .035f, .06f, .95f);
        RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.72f, .25f); rect.anchorMax = new Vector2(.98f, .75f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        _panel = panel;
        _title = CreateText(panel.transform, 23, TextAnchor.UpperCenter, new Vector2(.06f, .79f), new Vector2(.94f, .95f));
        _details = CreateText(panel.transform, 17, TextAnchor.UpperLeft, new Vector2(.08f, .28f), new Vector2(.92f, .78f));
        _upgradeHint = CreateText(panel.transform, 13, TextAnchor.MiddleCenter, new Vector2(.08f, .255f), new Vector2(.92f, .29f));
        _upgradeHint.color = new Color(1f, .85f, .35f, 1f);
        Button close = CreateButton(panel.transform, new Vector2(.82f, .87f), new Vector2(.94f, .95f));
        close.GetComponent<Image>().color = new Color(.38f, .12f, .1f, 1f);
        close.GetComponentInChildren<Text>().text = "X";
        close.onClick.AddListener(Deselect);
        _upgrade = CreateButton(panel.transform, new Vector2(.08f, .15f), new Vector2(.92f, .25f));
        _upgradeText = _upgrade.GetComponentInChildren<Text>();
        _upgrade.onClick.AddListener(UpgradeSelected);
        UpgradeRequirementHover upgradeHover = _upgrade.gameObject.AddComponent<UpgradeRequirementHover>();
        upgradeHover.Initialize(this);
        Button sell = CreateButton(panel.transform, new Vector2(.08f, .04f), new Vector2(.92f, .13f));
        sell.GetComponent<Image>().color = new Color(.5f, .14f, .10f, 1f);
        sell.GetComponentInChildren<Text>().text = "SELL";
        sell.onClick.AddListener(SellSelected);
        _panel.SetActive(false);
    }

    private Text CreateText(Transform parent, int size, TextAnchor anchor, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Text", typeof(Text)); obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>(); text.font = _font; text.fontSize = size; text.color = Color.white; text.alignment = anchor; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return text;
    }

    private Button CreateButton(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Button", typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(.14f, .35f, .16f, 1f);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = CreateText(obj.transform, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one); text.text = "UPGRADE";
        return obj.GetComponent<Button>();
    }

    private void RefreshPanel()
    {
        if (_selected == null || _panel == null) return;
        CarthaginianTower tower = _selected.GetComponent<CarthaginianTower>();
        if (tower != null)
        {
            CarthaginianTowerDefinition definition = tower.Definition;
            _title.text = definition != null ? definition.towerName : _selected.name;
            _details.text = (definition != null ? definition.description : string.Empty) + "\n\nLevel: " + (tower.CurrentLevel + 1) + (definition != null && definition.levels != null ? " / " + definition.levels.Length : string.Empty)
                + "\n" + GetTowerStats(tower) + "\nSell value: " + GetSellValue() + " coin";
            _upgrade.gameObject.SetActive(tower.CanUpgrade);
            if (tower.CanUpgrade)
            {
                _upgradeText.text = "UPGRADE — " + tower.NextUpgradeCost + " coin";
                _upgrade.interactable = tower.CanAffordUpgrade;
            }
            _upgradeHint.text = string.Empty;
            return;
        }
        CarthaginianResourceTower resource = _selected.GetComponent<CarthaginianResourceTower>();
        CarthaginianResourceDefinition resourceDefinition = resource != null ? resource.Definition : null;
        _title.text = resourceDefinition != null ? resourceDefinition.buildingName : _selected.name;
        _details.text = (resourceDefinition != null ? resourceDefinition.description : string.Empty)
            + "\n\nWorkers: " + (resourceDefinition != null ? resourceDefinition.workersRequired : 0)
            + "\nIncome: " + (resourceDefinition != null ? resourceDefinition.unitsPerCycle + " " + resourceDefinition.resource + " / " + resourceDefinition.productionCycleSeconds + " sec" : "")
            + "\nSell value: " + GetSellValue() + " coin";
        _upgrade.gameObject.SetActive(false);
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
        TowerLevel level = tower.ActiveLevel;
        if (level == null || level.unlockedShips == null || level.unlockedShips.Length == 0) return "No ships unlocked.";
        System.Text.StringBuilder output = new System.Text.StringBuilder("Available ships:");
        foreach (CarthaginianShipOption ship in level.unlockedShips)
        {
            if (ship == null) continue;
            bool affordable = EconomyManager.Instance != null && EconomyManager.Instance.Money >= ship.shipCost;
            output.Append("\n• ").Append(ship.shipName).Append(" — ").Append(ship.shipCost).Append(" coin ").Append(affordable ? "[READY]" : "[NEED COIN]")
                .Append("\n  ").Append(ship.crewRequired).Append(" ").Append(ship.minimumRank).Append("+, ").Append(ship.spawnCooldown).Append(" sec spawn");
            CarthaginianShipCombat combat = ship.shipPrefab != null ? ship.shipPrefab.GetComponent<CarthaginianShipCombat>() : null;
            if (combat != null) output.Append("\n  DMG ").Append(combat.AttackDamage).Append(" | sight ").Append(combat.SightRange).Append(" | range ").Append(combat.AttackRange).Append(" | ").Append(combat.AttackCooldown).Append(" sec");
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

    private void UpgradeSelected() { if (_selected != null) _selected.GetComponent<CarthaginianTower>()?.TryUpgrade(); }
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
        if (_selected == null) return;
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
