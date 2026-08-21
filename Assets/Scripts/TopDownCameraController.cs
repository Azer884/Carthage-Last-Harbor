using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Top-down tower defense camera for the New Input System: WASD/arrow pan, wheel zoom, middle-mouse
/// drag. Rotation is whatever angle the camera is set to in the scene — nothing here ever changes it, so
/// panning always moves the same screen-relative direction no matter where you are on the map.</summary>
[RequireComponent(typeof(Camera))]
public class TopDownCameraController : MonoBehaviour
{
    [Header("Pan")]
    [SerializeField, Min(.1f)] private float panSpeed = 9f;
    [SerializeField, Min(0f)] private float dragSpeed = .015f;
    [Header("Zoom")]
    [SerializeField, Min(.1f)] private float zoomSpeed = 7f;
    [SerializeField, Min(1f)] private float minimumHeight = 12f;
    [SerializeField, Min(2f)] private float maximumHeight = 70f;
    [Header("Map Bounds")]
    [SerializeField] private bool useMapBounds = true;
    [SerializeField] private Vector2 minimumMapPosition = new Vector2(-25f, -25f);
    [SerializeField] private Vector2 maximumMapPosition = new Vector2(25f, 25f);
    public bool UsesMapBounds => useMapBounds;
    public Vector2 MinimumMapPosition => minimumMapPosition;
    public Vector2 MaximumMapPosition => maximumMapPosition;

    private Vector2 _dragOrigin;

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;
        Vector2 motion = GetKeyboardMotion();
        if (Mouse.current.middleButton.wasPressedThisFrame) _dragOrigin = Mouse.current.position.ReadValue();
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 delta = Mouse.current.position.ReadValue() - _dragOrigin;
            motion += new Vector2(-delta.x, -delta.y) * dragSpeed;
            _dragOrigin = Mouse.current.position.ReadValue();
        }
        if (motion.sqrMagnitude > 0f)
        {
            Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = transform.right; right.y = 0f; right.Normalize();
            transform.position += (right * (motion.x * panSpeed * Time.deltaTime) + forward * (motion.y * panSpeed * Time.deltaTime));
        }
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > .01f) transform.position += transform.forward * (scroll * zoomSpeed * Time.deltaTime);
        ClampPosition();
    }

    private Vector2 GetKeyboardMotion()
    {
        float x = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
        float y = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y).normalized;
    }

    private void ClampPosition()
    {
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minimumHeight, maximumHeight);
        if (useMapBounds)
        {
            position.x = Mathf.Clamp(position.x, minimumMapPosition.x, maximumMapPosition.x);
            position.z = Mathf.Clamp(position.z, minimumMapPosition.y, maximumMapPosition.y);
        }
        transform.position = position;
    }
}
