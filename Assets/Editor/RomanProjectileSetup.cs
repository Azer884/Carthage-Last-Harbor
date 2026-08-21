using UnityEditor;
using UnityEngine;

/// <summary>Builds a placeholder Roman projectile (a plain grey stone — RomanProjectile itself is pure
/// transform logic, no physics/mesh requirements, same as the Dragon's fireball) and assigns it to every
/// RomeShip asset whose projectilePrefab is still empty, so Roman ships fire something visible instead of
/// hit-scanning instantly. Safe to re-run.</summary>
public static class RomanProjectileSetup
{
    private const string ProjectilePrefabPath = "Assets/Prefabs/Roman/RomanProjectile.prefab";
    private const string ProjectileMaterialPath = "Assets/Prefabs/Roman/RomanProjectile_Mat.mat";

    [MenuItem("Carthage/Create Roman Projectile")]
    public static void Apply()
    {
        GameObject projectilePrefab = CreateProjectilePrefab();
        if (projectilePrefab == null) return;

        int assigned = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:RomeShip"))
        {
            RomeShip ship = AssetDatabase.LoadAssetAtPath<RomeShip>(AssetDatabase.GUIDToAssetPath(guid));
            if (ship == null || ship.projectilePrefab != null) continue;
            Undo.RecordObject(ship, "Assign Roman Projectile");
            ship.projectilePrefab = projectilePrefab;
            EditorUtility.SetDirty(ship);
            assigned++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RomanProjectileSetup: assigned the projectile to " + assigned + " RomeShip asset(s).");
    }

    private static GameObject CreateProjectilePrefab()
    {
        AssetDatabase.DeleteAsset(ProjectilePrefabPath);
        AssetDatabase.DeleteAsset(ProjectileMaterialPath);

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "RomanProjectile";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.transform.localScale = Vector3.one * .4f;

        // Dull stone-grey, unlit-ish (low smoothness/no emission) — reads as a hurled rock, not a magic
        // effect, and stays visually distinct from the Dragon's glowing orange fireball.
        Material material = new Material(FindShader());
        Color stoneColor = new Color(.42f, .40f, .37f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", stoneColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", stoneColor);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .1f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .1f);
        AssetDatabase.CreateAsset(material, ProjectileMaterialPath);
        root.GetComponent<MeshRenderer>().sharedMaterial = material;

        root.AddComponent<RomanProjectile>();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("RomanProjectileSetup: created " + ProjectilePrefabPath);
        return savedPrefab;
    }

    private static Shader FindShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");
    }
}
