using UnityEngine;
using UnityEngine.UI;

/// <summary>World-space health bar that floats above its target and always faces the camera, smoothly
/// animating toward whatever fraction SetFraction() was last given. Attach via FloatingHealthBar.Attach(...)
/// then call SetFraction() whenever the owner's health changes.</summary>
public class FloatingHealthBar : MonoBehaviour
{
    private Transform _target;
    private Vector3 _worldOffset;
    private Image _fill;
    private float _targetFraction = 1f;
    private float _displayFraction = 1f;
    private static Sprite _solidSprite;

    // Image.Type.Filled silently fails to render any fill geometry at all when the Image has no Sprite
    // assigned — it just draws as a plain flat rect regardless of fillAmount. A screen-space Image can get
    // away with that because the default UI sprite fallback kicks in, but a fresh Image created purely in
    // code (like this one, and the heart's HUD bar) has no such fallback. A solid white square sprite is
    // all Filled actually needs; tint comes from Image.color as normal.
    public static Sprite GetSolidSprite()
    {
        if (_solidSprite != null) return _solidSprite;
        Texture2D texture = new Texture2D(4, 4);
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels32(pixels);
        texture.Apply();
        _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(.5f, .5f), 4f);
        return _solidSprite;
    }

    // `scale` is the canvas's direct localScale (what you'd tune by hand in the Inspector on the "Floating
    // Health Bar" object) rather than an indirect world-width — easier to dial in visually per object type.
    public static FloatingHealthBar Attach(Transform target, float worldHeight, float scale = .2f)
    {
        GameObject canvasObject = new GameObject("Floating Health Bar", typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120f, 16f);
        canvasObject.transform.localScale = Vector3.one * scale;

        GameObject background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        Image bgImage = background.GetComponent<Image>();
        bgImage.sprite = GetSolidSprite();
        bgImage.color = new Color(.08f, .08f, .08f, .75f);
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill", typeof(Image));
        fillObject.transform.SetParent(background.transform, false);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = GetSolidSprite();
        fillImage.color = new Color(.85f, .18f, .16f, .95f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = new Vector2(.04f, .12f); fillRect.anchorMax = new Vector2(.96f, .88f); fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        FloatingHealthBar bar = canvasObject.AddComponent<FloatingHealthBar>();
        bar._target = target;
        bar._worldOffset = Vector3.up * worldHeight;
        bar._fill = fillImage;
        return bar;
    }

    public void SetFraction(float fraction) => _targetFraction = Mathf.Clamp01(fraction);

    private void LateUpdate()
    {
        if (_target == null) { Destroy(gameObject); return; }
        transform.position = _target.position + _worldOffset;
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = camera.transform.rotation;

        _displayFraction = Mathf.MoveTowards(_displayFraction, _targetFraction, Time.unscaledDeltaTime * 1.5f);
        if (_fill != null) _fill.fillAmount = _displayFraction;
    }
}
