using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel shown when the Carthage Heart is selected. Buys or rents mercenary crew per row.</summary>
public class MercenaryMarketUI : MonoBehaviour
{
    public static MercenaryMarketUI Instance { get; private set; }

    private GameObject _panel;
    private TextMeshProUGUI _debtText;
    private Row[] _rows;

    private class Row
    {
        public TextMeshProUGUI info;
        public TextMeshProUGUI stock;
        public TMP_InputField quantity;
        public Button action;
        public TextMeshProUGUI actionLabel;
    }

    public static MercenaryMarketUI Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("Mercenary Market UI").AddComponent<MercenaryMarketUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreatePanel();
    }

    public bool IsShown => _panel != null && _panel.activeSelf;

    public void Show()
    {
        MercenaryMarket.Ensure();
        if (_panel == null) return;
        _panel.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void Update()
    {
        if (IsShown) Refresh();
    }

    private void CreatePanel()
    {
        GameObject root = new GameObject("Mercenary Market Root", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 115;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("Mercenary Market Panel", typeof(Image));
        panel.transform.SetParent(root.transform, false);
        panel.GetComponent<Image>().color = new Color(.025f, .035f, .06f, .96f);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.30f, .18f);
        rect.anchorMax = new Vector2(.70f, .80f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        _panel = panel;

        CreateText("MERCENARY MARKET", 23, TextAnchor.UpperCenter, new Vector2(.05f, .89f), new Vector2(.86f, .98f));
        _debtText = CreateText(string.Empty, 14, TextAnchor.UpperCenter, new Vector2(.05f, .83f), new Vector2(.95f, .89f));
        _debtText.color = new Color(1f, .55f, .3f);

        Button close = CreateButton(new Vector2(.88f, .90f), new Vector2(.97f, .98f));
        close.GetComponent<Image>().color = new Color(.38f, .12f, .1f, 1f);
        close.GetComponentInChildren<TextMeshProUGUI>().text = "X";
        close.onClick.AddListener(() => TowerSelectionManager.Instance?.Deselect());

        _rows = new Row[3];
        for (int i = 0; i < _rows.Length; i++)
        {
            float top = .79f - i * .26f;
            float bottom = top - .22f;
            Row row = new Row
            {
                info = CreateText(string.Empty, 16, TextAnchor.UpperLeft, new Vector2(.05f, bottom + .11f), new Vector2(.95f, top)),
                stock = CreateText(string.Empty, 13, TextAnchor.UpperLeft, new Vector2(.05f, bottom), new Vector2(.45f, bottom + .11f)),
                quantity = CreateInputField(new Vector2(.47f, bottom), new Vector2(.62f, bottom + .11f)),
                action = CreateButton(new Vector2(.64f, bottom), new Vector2(.95f, bottom + .11f))
            };
            row.actionLabel = row.action.GetComponentInChildren<TextMeshProUGUI>();
            int index = i;
            row.action.onClick.AddListener(() => OnAction(index));
            _rows[i] = row;
        }

        _panel.SetActive(false);
    }

    private void OnAction(int index)
    {
        MercenaryMarket market = MercenaryMarket.Ensure();
        if (index < 0 || index >= market.Offers.Length) return;
        int quantity = ParseQuantity(_rows[index].quantity.text, market.Offers[index].currentStock);
        if (market.CanBuy(index, quantity)) market.Buy(index, quantity);
        else if (market.CanRent(index, quantity)) market.Rent(index, quantity);
        Refresh();
    }

    private int ParseQuantity(string text, int maxStock)
    {
        if (!int.TryParse(text, out int quantity)) quantity = 1;
        return Mathf.Clamp(quantity, 1, Mathf.Max(1, maxStock));
    }

    // Avoids re-disabling/re-enabling an already-correct GameObject every frame — doing so would clear
    // EventSystem focus if the player has a quantity InputField selected.
    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target.activeSelf != active) target.SetActive(active);
    }

    private void Refresh()
    {
        MercenaryMarket market = MercenaryMarket.Ensure();
        _debtText.text = EconomyManager.Instance != null && EconomyManager.Instance.Debt > 0
            ? "Outstanding debt: " + EconomyManager.Instance.Debt + " coin — auto-repaid from income"
            : string.Empty;

        bool locked = market.IsLocked;
        for (int i = 0; i < _rows.Length && i < market.Offers.Length; i++)
        {
            MercenaryOffer offer = market.Offers[i];
            Row row = _rows[i];
            row.info.text = offer.troopName + " — " + offer.rank + " (hired from " + offer.regionName + ")";

            if (offer.currentStock <= 0)
            {
                row.stock.text = "None available this wave.";
                SetActiveSafe(row.quantity.gameObject, false);
                SetActiveSafe(row.action.gameObject, false);
                continue;
            }

            SetActiveSafe(row.quantity.gameObject, true);
            SetActiveSafe(row.action.gameObject, true);

            int quantity = ParseQuantity(row.quantity.text, offer.currentStock);
            bool canAffordBuy = EconomyManager.Instance != null && EconomyManager.Instance.Money >= market.GetBuyCost(i, quantity);

            if (canAffordBuy)
            {
                row.stock.text = "Stock: " + offer.currentStock + " | " + market.GetBuyCost(i, quantity) + " coin";
                row.actionLabel.text = "BUY";
            }
            else
            {
                row.stock.text = "Stock: " + offer.currentStock + " | rent for " + market.GetRentCost(i, quantity) + " debt";
                row.actionLabel.text = "RENT";
            }

            row.action.interactable = !locked && (canAffordBuy ? market.CanBuy(i, quantity) : market.CanRent(i, quantity));
            if (locked) row.stock.text = "Market closed this wave — recent rent still settling.";
        }
    }

    private TextMeshProUGUI CreateText(string text, int size, TextAnchor anchor, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Text", typeof(TextMeshProUGUI));
        obj.transform.SetParent(_panel.transform, false);
        TextMeshProUGUI result = obj.GetComponent<TextMeshProUGUI>();
        result.text = text; result.fontSize = size; result.alignment = TmpTextUtility.ToTmpAlignment(anchor); result.color = Color.white;
        result.enableWordWrapping = true; result.overflowMode = TextOverflowModes.Overflow;
        RectTransform rect = result.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return result;
    }

    private Button CreateButton(Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Button", typeof(Image), typeof(Button));
        obj.transform.SetParent(_panel.transform, false);
        obj.GetComponent<Image>().color = new Color(.14f, .35f, .16f, 1f);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(obj.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 14; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.text = "BUY";
        RectTransform textRect = text.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        return obj.GetComponent<Button>();
    }

    private TMP_InputField CreateInputField(Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject("Quantity Input", typeof(Image), typeof(TMP_InputField));
        obj.transform.SetParent(_panel.transform, false);
        obj.GetComponent<Image>().color = new Color(1f, 1f, 1f, .15f);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Text", typeof(TextMeshProUGUI));
        textObject.transform.SetParent(obj.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 14; text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        RectTransform textRect = text.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(4f, 2f); textRect.offsetMax = new Vector2(-4f, -2f);

        TMP_InputField field = obj.GetComponent<TMP_InputField>();
        field.textViewport = rect;
        field.textComponent = text;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.text = "1";
        return field;
    }
}
