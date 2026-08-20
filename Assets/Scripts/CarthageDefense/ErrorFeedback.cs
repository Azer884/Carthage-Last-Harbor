using UnityEngine;

/// <summary>One place for "the player tried to do something that isn't allowed right now" feedback —
/// floating text explaining why, a small camera shake, and the shared invalid-action sting. Used across
/// build/upgrade/purchase failures so every dead-end gives the same clear signal instead of silently doing
/// nothing.</summary>
public static class ErrorFeedback
{
    private static readonly Color TextColor = new Color(1f, .55f, .5f);

    public static void Show(Vector3 worldPosition, string message)
    {
        FloatingCombatText.Spawn(worldPosition, message, TextColor);
        CameraShake.Shake(.2f);
        SfxManager.Instance?.PlayPlacementInvalid();
    }
}
