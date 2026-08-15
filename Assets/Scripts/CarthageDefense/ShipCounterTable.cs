/// <summary>Simple rock-paper-scissors triangle: Warship beats Skirmisher, Skirmisher beats Heavy, Heavy beats Warship.
/// Assign a class per ship (RomeShip / CarthaginianShipCombat) in the Inspector to opt into the matchup;
/// everything defaults to Warship, so nothing changes until you start assigning classes.</summary>
public enum ShipCombatClass { Skirmisher, Warship, Heavy }

public static class ShipCounterTable
{
    private const float CounterDamageMultiplier = 1.5f;

    public static float GetDamageMultiplier(ShipCombatClass attacker, ShipCombatClass defender)
    {
        bool counters = (attacker == ShipCombatClass.Skirmisher && defender == ShipCombatClass.Heavy)
            || (attacker == ShipCombatClass.Heavy && defender == ShipCombatClass.Warship)
            || (attacker == ShipCombatClass.Warship && defender == ShipCombatClass.Skirmisher);
        return counters ? CounterDamageMultiplier : 1f;
    }

    public static ShipCombatClass GetCountered(ShipCombatClass shipClass)
    {
        switch (shipClass)
        {
            case ShipCombatClass.Skirmisher: return ShipCombatClass.Heavy;
            case ShipCombatClass.Heavy: return ShipCombatClass.Warship;
            default: return ShipCombatClass.Skirmisher;
        }
    }

    // "Class: Skirmisher — 1.5x damage vs Heavy"
    public static string Describe(ShipCombatClass shipClass)
    {
        return "Class: " + shipClass + " — " + CounterDamageMultiplier + "x damage vs " + GetCountered(shipClass);
    }
}
