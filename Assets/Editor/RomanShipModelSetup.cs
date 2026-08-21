using UnityEditor;
using UnityEngine;

/// <summary>The old RomeShip.prefab wasn't a generic chassis — it's the actual rigged ship model with
/// RomanShip/SplineAnimate/ShipBuoyancy already baked onto its root. This splits those apart: strips those
/// three components from a copy of it to make a visual-only model prefab, then assigns that model to every
/// RomeShip ScriptableObject whose modelPrefab field is still empty. The original RomeShip.prefab is left
/// untouched. Safe to re-run.</summary>
public static class RomanShipModelSetup
{
    private const string SourcePrefabPath = "Assets/Prefabs/Roman/RomeShip.prefab";
    private const string ModelPrefabPath = "Assets/Prefabs/Roman/RomeShipModel.prefab";

    [MenuItem("Carthage/Extract Roman Ship Model")]
    public static void Apply()
    {
        GameObject source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (source == null) { Debug.LogWarning("RomanShipModelSetup: couldn't load " + SourcePrefabPath); return; }

        GameObject modelPrefab;
        try
        {
            RemoveIfPresent<RomanShip>(source);
            RemoveIfPresent<UnityEngine.Splines.SplineAnimate>(source);
            RemoveIfPresent<ShipBuoyancy>(source);

            AssetDatabase.DeleteAsset(ModelPrefabPath);
            modelPrefab = PrefabUtility.SaveAsPrefabAsset(source, ModelPrefabPath);
            Debug.Log("RomanShipModelSetup: created " + ModelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
        }

        int assigned = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:RomeShip"))
        {
            RomeShip ship = AssetDatabase.LoadAssetAtPath<RomeShip>(AssetDatabase.GUIDToAssetPath(guid));
            if (ship == null || ship.modelPrefab != null) continue;
            Undo.RecordObject(ship, "Assign Roman Ship Model");
            ship.modelPrefab = modelPrefab;
            EditorUtility.SetDirty(ship);
            assigned++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RomanShipModelSetup: assigned the model to " + assigned + " RomeShip asset(s).");
    }

    private static void RemoveIfPresent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component);
    }
}
