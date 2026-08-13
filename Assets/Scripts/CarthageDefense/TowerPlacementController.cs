using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Places defensive towers only on colliders in Sea Mask and outside the Enemy Path Mask.</summary>
public class TowerPlacementController : MonoBehaviour
{
    [SerializeField] private Camera placementCamera;
    [SerializeField] private LayerMask seaMask;
    [SerializeField] private LayerMask landMask;
    [SerializeField] private LayerMask enemyPathMask;
    [SerializeField, Min(0.1f)] private float pathClearance = 4f;
    [SerializeField, Min(0.1f)] private float towerSpacing = 3f;

    private CarthaginianTowerDefinition _selectedTower;
    private CarthaginianResourceDefinition _selectedResourceTower;
    private GameObject _placementPreview;
    private MaterialPropertyBlock _previewProperties;
    public bool IsPlacing => _selectedTower != null || _selectedResourceTower != null;
    public void SelectTower(CarthaginianTowerDefinition tower) { _selectedTower = tower; _selectedResourceTower = null; CreatePreview(tower != null ? tower.prefab : null); }
    public void SelectResourceTower(CarthaginianResourceDefinition tower) { _selectedResourceTower = tower; _selectedTower = null; CreatePreview(tower != null ? tower.prefab : null); }
    public void CancelPlacement() { _selectedTower = null; _selectedResourceTower = null; if (_placementPreview != null) Destroy(_placementPreview); _placementPreview = null; }

    private void Awake()
    {
        _previewProperties = new MaterialPropertyBlock();
    }

    public bool TryPlaceResource(CarthaginianResourceDefinition definition, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.prefab == null || !IsValidResourcePlacement(definition, position)) return false;
        if (definition.buildCost > 0 && (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(definition.buildCost))) return false;
        GameObject building = Instantiate(definition.prefab, position, rotation);
        CarthaginianResourceTower resourceTower = building.GetComponent<CarthaginianResourceTower>();
        if (resourceTower != null) resourceTower.Initialize(definition);
        TowerSelectionManager.Ensure().Select(building);
        return true;
    }

    private void Update()
    {
        if (!IsPlacing || Mouse.current == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame) { CancelPlacement(); return; }
        Camera cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null) return;
        LayerMask placementMask = _selectedResourceTower != null && _selectedResourceTower.environment == ResourceEnvironment.Land ? landMask : seaMask;
        if (!Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 500f, placementMask)) { SetPreviewVisible(false); return; }
        bool valid = _selectedResourceTower != null ? IsValidResourcePlacement(_selectedResourceTower, hit.point) : IsValidPlacement(_selectedTower, hit.point);
        UpdatePreview(hit.point, valid);
        if (!Mouse.current.leftButton.wasPressedThisFrame || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || !valid) return;
        if (_selectedResourceTower != null) TryPlaceResource(_selectedResourceTower, hit.point, Quaternion.identity);
        else TryPlace(_selectedTower, hit.point, Quaternion.identity);
    }

    public bool TryPlace(CarthaginianTowerDefinition definition, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.prefab == null || !IsValidPlacement(definition, position)) return false;
        if (definition.buildCost > 0 && (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(definition.buildCost))) return false;
        GameObject tower = Instantiate(definition.prefab, position, rotation);
        CarthaginianTower runtimeTower = tower.GetComponent<CarthaginianTower>();
        if (runtimeTower != null) runtimeTower.Initialize(definition);
        TowerSelectionManager.Ensure().Select(tower);
        return true;
    }

    public bool IsValidPlacement(CarthaginianTowerDefinition definition, Vector3 position)
    {
        // This also protects calls to TryPlace made from UI/buttons rather than the mouse path above.
        if (!Physics.CheckSphere(position, 0.15f, seaMask, QueryTriggerInteraction.Ignore)) return false;
        if (IsNearEnemyPath(position)) return false;
        if (Physics.CheckSphere(position, towerSpacing, ~enemyPathMask, QueryTriggerInteraction.Ignore))
        {
            foreach (Collider hit in Physics.OverlapSphere(position, towerSpacing, ~enemyPathMask, QueryTriggerInteraction.Ignore))
                if (hit.GetComponentInParent<CarthaginianTower>() != null) return false;
        }
        if (string.IsNullOrEmpty(definition.requiredZoneId)) return true;
        foreach (TowerPlacementZone zone in FindObjectsByType<TowerPlacementZone>(FindObjectsSortMode.None))
            if (zone.ZoneId == definition.requiredZoneId && zone.Contains(position)) return true;
        return false;
    }

    public bool IsValidResourcePlacement(CarthaginianResourceDefinition definition, Vector3 position)
    {
        LayerMask environmentMask = definition.environment == ResourceEnvironment.Sea ? seaMask : landMask;
        if (!Physics.CheckSphere(position, 0.15f, environmentMask, QueryTriggerInteraction.Ignore)) return false;
        if (IsNearEnemyPath(position)) return false;
        foreach (Collider hit in Physics.OverlapSphere(position, towerSpacing, ~enemyPathMask, QueryTriggerInteraction.Ignore))
            if (hit.GetComponentInParent<CarthaginianTower>() != null || hit.GetComponentInParent<CarthaginianResourceTower>() != null) return false;
        if (string.IsNullOrEmpty(definition.requiredZoneId)) return true;
        foreach (TowerPlacementZone zone in FindObjectsByType<TowerPlacementZone>(FindObjectsSortMode.None))
            if (zone.ZoneId == definition.requiredZoneId && zone.Contains(position)) return true;
        return false;
    }

    private bool IsNearEnemyPath(Vector3 position)
    {
        if (Physics.CheckSphere(position, pathClearance, enemyPathMask, QueryTriggerInteraction.Collide)) return true;
        foreach (EnemyPathPlacementBlocker blocker in FindObjectsByType<EnemyPathPlacementBlocker>(FindObjectsSortMode.None))
            if (blocker.IsBlocked(position, pathClearance)) return true;
        return false;
    }

    private void CreatePreview(GameObject prefab)
    {
        if (_placementPreview != null) Destroy(_placementPreview);
        if (prefab == null) return;
        _placementPreview = Instantiate(prefab);
        _placementPreview.name = "Placement Preview (not built)";
        foreach (MonoBehaviour behaviour in _placementPreview.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        foreach (Collider collider in _placementPreview.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Renderer renderer in _placementPreview.GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.materials)
            {
                // URP Lit uses these properties; harmless for other shaders and keeps the ghost see-through.
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        UpdatePreview(Vector3.zero, false);
    }

    private void UpdatePreview(Vector3 position, bool isValid)
    {
        if (_placementPreview == null) return;
        if (_previewProperties == null) _previewProperties = new MaterialPropertyBlock();
        _placementPreview.transform.position = position;
        SetPreviewVisible(true);
        Color color = isValid ? new Color(.1f, 1f, .25f, .48f) : new Color(1f, .08f, .08f, .48f);
        foreach (Renderer renderer in _placementPreview.GetComponentsInChildren<Renderer>())
        {
            _previewProperties.Clear();
            _previewProperties.SetColor("_BaseColor", color);
            _previewProperties.SetColor("_Color", color);
            renderer.SetPropertyBlock(_previewProperties);
        }
    }

    private void SetPreviewVisible(bool visible)
    {
        if (_placementPreview != null && _placementPreview.activeSelf != visible) _placementPreview.SetActive(visible);
    }

    private void OnDisable() { CancelPlacement(); }
}
