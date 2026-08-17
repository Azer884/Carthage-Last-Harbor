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
    public bool IsPlacing => _selectedDragon != null;

    private void Awake()
    {
        if (freePlacementController == null) freePlacementController = FindAnyObjectByType<TowerPlacementController>();
    }

    public void SelectDragon(CarthaginianTowerDefinition dragon)
    {
        if (freePlacementController != null) freePlacementController.CancelPlacement();
        _selectedDragon = dragon;
        SetSlotGlow(true);
    }

    public void CancelPlacement()
    {
        SetSlotGlow(false);
        _selectedDragon = null;
    }

    private void Update()
    {
        if (!IsPlacing || Mouse.current == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame) { CancelPlacement(); return; }
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null) return;
        GameObject slot = FindSlotUnderPointer(cam, Mouse.current.position.ReadValue());
        if (slot == null || IsOccupied(slot)) { SfxManager.Instance?.PlayPlacementInvalid(); return; }
        TryPlace(slot);
    }

    private bool TryPlace(GameObject slot)
    {
        if (_selectedDragon == null || _selectedDragon.prefab == null) return false;
        if (_selectedDragon.buildCost > 0 && (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(_selectedDragon.buildCost))) return false;
        CarthaginianDragonTower dragonPrefabComponent = _selectedDragon.prefab.GetComponent<CarthaginianDragonTower>();
        Vector3 offset = dragonPrefabComponent != null ? dragonPrefabComponent.PlacementOffset : Vector3.zero;
        Vector3 position = slot.transform.TransformPoint(offset);
        GameObject dragon = Instantiate(_selectedDragon.prefab, position, slot.transform.rotation, slot.transform);
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
        return null;
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
