using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    // Ships are built from scratch here rather than instantiating a pre-made prefab: RomeShip.modelPrefab
    // supplies only the visual (mesh/rig + its own Animator), and every gameplay component — RomanShip,
    // SplineAnimate, RomanShipHealth — gets attached by script, so each ship type's asset is just data plus
    // whichever model it points at instead of a whole separate prefab per type.
    public void SpawnShip(RomeShip shipData, bool willAttack = true, bool willPreferTowers = false)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is not assigned.");
            return;
        }

        if (shipData == null)
        {
            Debug.LogError("Ship data is not assigned.");
            return;
        }

        GameObject ship = new GameObject(string.IsNullOrEmpty(shipData.shipName) ? "Roman Ship" : shipData.shipName);
        ship.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        RomanShip romanShip = ship.AddComponent<RomanShip>();
        romanShip.AssignShip(shipData);
        romanShip.SetCombatDecision(willAttack, willPreferTowers);

        RomanShipHealth health = ship.AddComponent<RomanShipHealth>();
        health.Configure(shipData.crewSize, shipData.bounty);

        SpawnPopEffect.Apply(ship);
        SfxManager.Instance?.PlayShipSpawned();
    }
}
