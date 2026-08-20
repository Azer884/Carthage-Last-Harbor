using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared color palette and small UI helpers so every menu — main menu, in-game build bar, tower
/// panel, mercenary market, pause/game-over — reads as one consistent dark-stone-and-gold Carthage style.
/// Runtime UI is rebuilt from scratch each time these scripts run, so changes here apply immediately; the
/// one exception is CarthaginianBuildMenu's category accent colors, which are serialized Inspector fields
/// on the scene's own component instance and need updating there separately.</summary>
public static class CarthageTheme
{
    public static readonly Color Background = new Color32(0x18, 0x11, 0x0C, 0xFF);
    public static readonly Color Panel = new Color32(0x33, 0x16, 0x11, 0xE8);
    public static readonly Color PanelDim = new Color32(0x22, 0x0F, 0x0B, 0xF0);
    public static readonly Color Border = new Color32(0xC9, 0x9A, 0x4A, 0xFF);
    public static readonly Color Gold = new Color32(0xEC, 0xC8, 0x74, 0xFF);
    public static readonly Color Cream = new Color32(0xEE, 0xE3, 0xCF, 0xFF);
    public static readonly Color Button = new Color32(0x6E, 0x24, 0x19, 0xFF);
    public static readonly Color ButtonPositive = new Color32(0x4C, 0x5A, 0x28, 0xFF);
    public static readonly Color ButtonNegative = new Color32(0x7A, 0x1E, 0x18, 0xFF);
    public static readonly Color Overlay = new Color(0f, 0f, 0f, .72f);

    // Build-menu category accents — distinct enough to scan at a glance, still drawn from the warm palette.
    public static readonly Color CategoryAttack = new Color32(0x8C, 0x2A, 0x1D, 0xF2);
    public static readonly Color CategoryDefense = new Color32(0x5C, 0x47, 0x1E, 0xF2);
    public static readonly Color CategoryResource = new Color32(0x33, 0x52, 0x28, 0xF2);

    // Plain single-color panel — same shape as the old ad-hoc "new GameObject(name, typeof(Image))"
    // helpers scattered across the UI scripts, centralized here.
    public static RectTransform CreateFlatPanel(string objectName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    // A thin gold border behind an inset fill — the "framed panel" look used throughout the main menu.
    // Returns the OUTER rect so existing call sites that size/position it and then parent more children
    // onto it keep working unchanged: the border is only a couple pixels, so ratio-anchored children land
    // inside the fill exactly as before.
    public static RectTransform CreateFramedPanel(string objectName, Transform parent, Color fillColor, float borderThickness = 2f)
    {
        RectTransform outer = CreateFlatPanel(objectName, parent, Border);
        RectTransform fill = CreateFlatPanel(objectName + " Fill", outer, fillColor);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(borderThickness, borderThickness);
        fill.offsetMax = new Vector2(-borderThickness, -borderThickness);
        return outer;
    }

    // Selectable's ColorTint multiplies the target graphic's own color by these, so the Image keeps the
    // real theme color and normalColor stays white (identity); only the highlight/press tints vary around it.
    public static void StyleButton(Button button, Color normal)
    {
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = normal;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.75f, .75f, .75f);
        colors.selectedColor = Color.white;
        button.colors = colors;
    }
}
