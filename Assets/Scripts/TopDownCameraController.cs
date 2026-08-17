using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>New Input System camera for a top-down tower defense: WASD/arrow pan, wheel zoom, middle-mouse drag, and mouse-facing rotation.</summary>
[RequireComponent(typeof(Camera))]
public class TopDownCameraController : MonoBehaviour
{
    [Header("Pan")]
    [SerializeField, Min(.1f)] private float panSpeed = 9f;
    [SerializeField, Min(0f)] private float dragSpeed = .015f;
    [SerializeField, Min(0f)] private float turnToCenterSpeed = 2.5f;
    [SerializeField] private Vector3 focalCenter = Vector3.zero;
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
    [Header("Mouse Look")]
    [SerializeField] private bool lookAtMouse = true;
    [SerializeField, Min(-1000f)] private float mouseLookPlaneY;
    [SerializeField, Min(.1f)] private Vector2 mouseLookAreaSize = new Vector2(10f, 10f);

    private Camera _camera;
    private Vector2 _dragOrigin;
    private bool _hasLookTarget;
    private Vector3 _lookTarget;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

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
        if (lookAtMouse && _camera != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            _hasLookTarget = TryGetMouseLookTarget(out _lookTarget);
        }

        Vector3 lookTarget = _hasLookTarget ? _lookTarget : focalCenter;
        Vector3 toTarget = lookTarget - transform.position;
        if (toTarget.sqrMagnitude > .01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnToCenterSpeed * Time.deltaTime);
        }
    }

    private bool TryGetMouseLookTarget(out Vector3 lookTarget)
    {
        lookTarget = focalCenter;

        if (Mouse.current == null)
        {
            return false;
        }

        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, mouseLookPlaneY, 0f));
        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 center = new Vector3(focalCenter.x, mouseLookPlaneY, focalCenter.z);
        float halfWidth = mouseLookAreaSize.x * .5f;
        float halfHeight = mouseLookAreaSize.y * .5f;
        lookTarget = new Vector3(
            Mathf.Clamp(hitPoint.x, center.x - halfWidth, center.x + halfWidth),
            mouseLookPlaneY,
            Mathf.Clamp(hitPoint.z, center.z - halfHeight, center.z + halfHeight));
        return true;
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
