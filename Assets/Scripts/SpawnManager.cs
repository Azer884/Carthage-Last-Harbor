using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private Transform spawnPoint;

    public void SpawnShip(RomeShip shipData, bool willAttack = true, bool willPreferTowers = false)
    {
        if (shipPrefab == null)
        {
            Debug.LogError("Selected ship prefab is null.");
            return;
        }

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

        GameObject ship = Instantiate(shipPrefab, spawnPoint.position, spawnPoint.rotation);
        RomanShip romanShip = ship.GetComponent<RomanShip>();
        if (romanShip == null)
        {
            Debug.LogError("Spawned ship prefab is missing the RomanShip component.");
            return;
        }

        romanShip.AssignShip(shipData);
        romanShip.SetCombatDecision(willAttack, willPreferTowers);

        RomanShipHealth health = ship.GetComponent<RomanShipHealth>();
        if (health == null)
            health = ship.AddComponent<RomanShipHealth>();
        health.Configure(shipData.crewSize, shipData.bounty);

        SpawnPopEffect.Apply(ship);
        SfxManager.Instance?.PlayShipSpawned();
    }
}
