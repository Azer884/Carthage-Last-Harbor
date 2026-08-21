using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Places the Dragon of Carthage. Unlike TowerPlacementController's free sea/land placement, the
/// dragon can only occupy scene objects tagged StationaryTowerPlace — each already carries an Outline
/// component (the same one TowerSelectionManager uses for hover/select) that this glows while the player
/// is choosing a spot. Each slot holds at most one dragon.</summary>
public class DragonTowerPlacementController : MonoBehaviour
{
    public const string SlotTag = "StationaryTowerPlace";

    [SerializeField] private Camera placementCamera;
    [SerializeField] private TowerPlacementController freePlacementController;

    private CarthaginianTowerDefinition _selectedDragon;
    private GameObject _preview;
    private MaterialPropertyBlock _previewProperties;
    public bool IsPlacing => _selectedDragon != null;
    // Read by TopDownCameraController (in LateUpdate, after this Update has run) so a right-click that
    // cancels placement doesn't also spin the camera to look at that same click.
    public bool CancelledPlacementThisFrame { get; private set; }

    private void Awake()
    {
        if (freePlacementController == null) freePlacementController = FindAnyObjectByType<TowerPlacementController>();
    }

    public void SelectDragon(CarthaginianTowerDefinition dragon)
    {
        if (freePlacementController != null) freePlacementController.CancelPlacement();
        _selectedDragon = dragon;
        SetSlotGlow(true);
        CreatePreview(dragon != null ? dragon.prefab : null);
    }

    public void CancelPlacement()
    {
        SetSlotGlow(false);
        _selectedDragon = null;
        if (_preview != null) Destroy(_preview);
        _preview = null;
    }

    private void Update()
    {
        CancelledPlacementThisFrame = false;
        if (!IsPlacing || Mouse.current == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame) { SfxManager.Instance?.PlayButtonClick(); CancelPlacement(); CancelledPlacementThisFrame = true; return; }
        Camera cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null) { SetPreviewVisible(false); return; }
        GameObject slot = FindSlotUnderPointer(cam, Mouse.current.position.ReadValue());
        bool valid = slot != null && !IsOccupied(slot);
        UpdatePreview(slot, valid);
        if (!Mouse.current.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        if (!valid)
        {
            // A slot-less click has no good world position to anchor text on — just the sting, same as
            // before. An occupied slot does, so it gets the full explain-why treatment.
            if (slot != null) ErrorFeedback.Show(slot.transform.position, "Slot occupied");
            else SfxManager.Instance?.PlayPlacementInvalid();
            return;
        }
        if (!TryPlace(slot)) ErrorFeedback.Show(slot.transform.position, "Not enough coin");
    }

    private bool TryPlace(GameObject slot)
    {
        if (_selectedDragon == null || _selectedDragon.prefab == null) return false;
        if (_selectedDragon.buildCost > 0 && (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(_selectedDragon.buildCost))) return false;
        CarthaginianDragonTower dragonPrefabComponent = _selectedDragon.prefab.GetComponent<CarthaginianDragonTower>();
        Vector3 offset = dragonPrefabComponent != null ? dragonPrefabComponent.PlacementOffset : Vector3.zero;
        Vector3 position = slot.transform.TransformPoint(offset);
        GameObject dragon = Instantiate(_selectedDragon.prefab, position, _selectedDragon.prefab.transform.rotation, slot.transform);
        MatchPrefabWorldScale(dragon.transform, _selectedDragon.prefab.transform.localScale, slot.transform);
        TowerSelectionManager.EnsureSelectableCollider(dragon);
        CarthaginianTower runtimeTower = dragon.GetComponent<CarthaginianTower>();
        if (runtimeTower != null) runtimeTower.Initialize(_selectedDragon);
        TowerSelectionManager.Ensure().Select(dragon);
        SpawnPopEffect.Apply(dragon);
        SfxManager.Instance?.PlayTowerPlaced();
        if (_selectedDragon.buildCost > 0)
        {
            FloatingCombatText.Spawn(position, "-" + _selectedDragon.buildCost, new Color(1f, .35f, .3f));
            SfxManager.Instance?.PlayCoinSpent();
        }
        CancelPlacement();
        return true;
    }

    // Instantiate(prefab, position, rotation, parent) carries the prefab's own localScale over as-is; once
    // parented under a slot with a non-1 scale (e.g. a scaled-down piece of an imported port model), the
    // dragon's world size would shrink/stretch by that factor. Divide it back out so the dragon always ends
    // up at the size it was authored at, regardless of what it's standing on.
    private static void MatchPrefabWorldScale(Transform instance, Vector3 prefabLocalScale, Transform parent)
    {
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
        instance.localScale = new Vector3(
            SafeDivide(prefabLocalScale.x, parentScale.x),
            SafeDivide(prefabLocalScale.y, parentScale.y),
            SafeDivide(prefabLocalScale.z, parentScale.z));
    }

    private static float SafeDivide(float a, float b) => Mathf.Abs(b) > 0.0001f ? a / b : a;

    private GameObject FindSlotUnderPointer(Camera camera, Vector2 screenPoint)
    {
        RaycastHit[] hits = Physics.RaycastAll(camera.ScreenPointToRay(screenPoint), 500f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Transform current = hit.collider.transform;
            while (current != null)
            {
                if (current.CompareTag(SlotTag)) return current.gameObject;
                current = current.parent;
            }
        }
        // The tagged pieces are chunks of an imported model and can be small/oddly shaped, so an exact
        // raycast miss doesn't necessarily mean the player wasn't aiming at one — fall back to whichever
        // slot's screen position is closest to the cursor, same forgiving-click idea TowerSelectionManager
        // already uses for buildings.
        return FindSlotNearScreenPoint(camera, screenPoint);
    }

    private GameObject FindSlotNearScreenPoint(Camera camera, Vector2 screenPoint)
    {
        GameObject closest = null;
        float closestPixels = 60f;
        foreach (GameObject slot in GameObject.FindGameObjectsWithTag(SlotTag))
        {
            Vector3 projected = camera.WorldToScreenPoint(slot.transform.position);
            if (projected.z <= 0f) continue;
            float pixels = Vector2.Distance(screenPoint, new Vector2(projected.x, projected.y));
            if (pixels >= closestPixels) continue;
            closest = slot;
            closestPixels = pixels;
        }
        return closest;
    }

    public static bool IsOccupied(GameObject slot) => slot != null && slot.GetComponentInChildren<CarthaginianDragonTower>() != null;

    private void SetSlotGlow(bool glow)
    {
        foreach (GameObject slot in GameObject.FindGameObjectsWithTag(SlotTag))
        {
            Outline outline = slot.GetComponent<Outline>();
            if (outline == null) continue;
            outline.enabled = glow && !IsOccupied(slot);
        }
    }

    private void CreatePreview(GameObject prefab)
    {
        if (_preview != null) Destroy(_preview);
        if (prefab == null) { _preview = null; return; }
        _preview = Instantiate(prefab);
        _preview.name = "Dragon Placement Preview (not built)";
        foreach (MonoBehaviour behaviour in _preview.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        foreach (CarthaginianTarget target in _preview.GetComponentsInChildren<CarthaginianTarget>()) target.SetTargetable(false);
        foreach (Collider collider in _preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Renderer renderer in _preview.GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.materials)
            {
                // URP Lit uses these properties; harmless for other shaders and keeps the ghost see-through.
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        SetPreviewVisible(false);
    }

    private void UpdatePreview(GameObject slot, bool valid)
    {
        if (_preview == null) return;
        if (!valid || slot == null) { SetPreviewVisible(false); return; }
        CarthaginianDragonTower dragonPrefabComponent = _selectedDragon != null && _selectedDragon.prefab != null ? _selectedDragon.prefab.GetComponent<CarthaginianDragonTower>() : null;
        Vector3 offset = dragonPrefabComponent != null ? dragonPrefabComponent.PlacementOffset : Vector3.zero;
        _preview.transform.position = slot.transform.TransformPoint(offset);
        _preview.transform.rotation = _selectedDragon.prefab.transform.rotation;
        MatchPrefabWorldScale(_preview.transform, _selectedDragon.prefab.transform.localScale, slot.transform);
        SetPreviewVisible(true);
        if (_previewProperties == null) _previewProperties = new MaterialPropertyBlock();
        Color color = new Color(.1f, 1f, .25f, .48f);
        foreach (Renderer renderer in _preview.GetComponentsInChildren<Renderer>())
        {
            _previewProperties.Clear();
            _previewProperties.SetColor("_BaseColor", color);
            _previewProperties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_previewProperties);
        }
    }

    private void SetPreviewVisible(bool visible)
    {
        if (_preview != null && _preview.activeSelf != visible) _preview.SetActive(visible);
    }

    private void OnDisable() { CancelPlacement(); }

#if UNITY_EDITOR
    // Tagging pieces of an imported model (e.g. an FBX port) as StationaryTowerPlace doesn't give them
    // a Collider (imports only add Transform/MeshFilter/MeshRenderer) or an Outline component, so clicks
    // never hit them and they never glow. Run this once after tagging slot objects to patch both in.
    [ContextMenu("Auto-Fix Slot Colliders And Outlines")]
    private void AutoFixSlots()
    {
        int fixedCount = 0;
        foreach (GameObject slot in GameObject.FindGameObjectsWithTag(SlotTag))
        {
            TowerSelectionManager.EnsureSelectableCollider(slot);
            if (slot.GetComponent<Outline>() == null) { slot.AddComponent<Outline>().enabled = false; fixedCount++; }
            UnityEditor.EditorUtility.SetDirty(slot);
        }
        Debug.Log("DragonTowerPlacementController: patched colliders/outlines on " + fixedCount + " StationaryTowerPlace slot(s) missing an Outline.");
    }
#endif
}
