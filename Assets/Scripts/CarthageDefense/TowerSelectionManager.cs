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
    private TowerSelectionOutline _outline;
    private GameObject _panel;
    private Text _title;
    private Text _details;
    private Button _upgrade;
    private Text _upgradeText;
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

    private void Update()
    {
        if (_selected == null) { HidePanel(); return; }
        RefreshPanel();
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || (_placement != null && _placement.IsPlacing)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera camera = Camera.main;
        if (camera == null || !Physics.Raycast(camera.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit)) { Deselect(); return; }
        GameObject selected = GetSelectableBuilding(hit.collider);
        if (selected != null) Select(selected); else Deselect();
    }

    public void Select(GameObject building)
    {
        if (building == null || (building.GetComponent<CarthaginianTower>() == null && building.GetComponent<CarthaginianResourceTower>() == null)) return;
        if (_selected == building) return;
        Deselect();
        _selected = building;
        _outline = building.GetComponent<TowerSelectionOutline>();
        if (_outline == null) _outline = building.AddComponent<TowerSelectionOutline>();
        _outline.enabled = true;
        _panel.SetActive(true);
        RefreshPanel();
    }

    public void Deselect()
    {
        if (_outline != null) _outline.enabled = false;
        _selected = null; _outline = null;
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
        _upgrade = CreateButton(panel.transform, new Vector2(.08f, .15f), new Vector2(.92f, .25f));
        _upgradeText = _upgrade.GetComponentInChildren<Text>();
        _upgrade.onClick.AddListener(UpgradeSelected);
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
                + "\nSell value: " + GetSellValue() + " coin";
            _upgrade.gameObject.SetActive(tower.CanUpgrade);
            if (tower.CanUpgrade) _upgradeText.text = "UPGRADE — " + tower.NextUpgradeCost + " coin";
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
    }

    private int GetSellValue()
    {
        CarthaginianTower tower = _selected != null ? _selected.GetComponent<CarthaginianTower>() : null;
        if (tower != null && tower.Definition != null) return Mathf.FloorToInt(tower.Definition.buildCost * sellRefundFraction);
        CarthaginianResourceTower resource = _selected != null ? _selected.GetComponent<CarthaginianResourceTower>() : null;
        return resource != null && resource.Definition != null ? Mathf.FloorToInt(resource.Definition.buildCost * sellRefundFraction) : 0;
    }

    private void UpgradeSelected() { if (_selected != null) _selected.GetComponent<CarthaginianTower>()?.TryUpgrade(); }
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

/// <summary>Yellow world-space outline for the currently selected building.</summary>
public class TowerSelectionOutline : MonoBehaviour
{
    private LineRenderer _line;
    private void OnEnable() { CreateOrUpdateLine(); }
    private void Start() { CreateOrUpdateLine(); }
    private void OnDisable() { if (_line != null) _line.enabled = false; }
    private void CreateOrUpdateLine()
    {
        if (_line == null)
        {
            GameObject line = new GameObject("Yellow Selection Outline"); line.transform.SetParent(transform, false);
            _line = line.AddComponent<LineRenderer>(); _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = _line.endColor = Color.yellow; _line.widthMultiplier = .12f; _line.loop = true; _line.positionCount = 4; _line.useWorldSpace = true; _line.sortingOrder = 100;
        }
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0) { bounds = renderers[0].bounds; foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds); }
        float y = bounds.min.y + .05f; float pad = .25f;
        _line.SetPositions(new[] { new Vector3(bounds.min.x - pad, y, bounds.min.z - pad), new Vector3(bounds.min.x - pad, y, bounds.max.z + pad), new Vector3(bounds.max.x + pad, y, bounds.max.z + pad), new Vector3(bounds.max.x + pad, y, bounds.min.z - pad) });
        _line.enabled = true;
    }
}
