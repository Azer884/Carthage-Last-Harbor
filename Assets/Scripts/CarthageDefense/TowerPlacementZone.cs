using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerPlacementZone : MonoBehaviour
{
    [SerializeField] private string zoneId;
    public string ZoneId => zoneId;
    public bool Contains(Vector3 position)
    {
        Collider area = GetComponent<Collider>();
        return area != null && area.ClosestPoint(position) == position;
    }
}
