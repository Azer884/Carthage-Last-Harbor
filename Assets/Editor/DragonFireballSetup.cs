using UnityEditor;
using UnityEngine;

/// <summary>Builds a placeholder Dragon fireball (glowing sphere + point light — DragonFireball itself is
/// pure transform logic, no physics/mesh requirements), then wires it and a new Muzzle child into
/// dragon.prefab's own CarthaginianDragonTower component — both the muzzle reference and the per-level
/// projectilePrefab fields live there, not on the "Dragon of Carthage" definition asset. The muzzle
/// position is a rough guess (a small offset from the root, not hand-placed at the model's actual mouth) —
/// nudge it in the Inspector once you can see it in Play mode. Safe to re-run.</summary>
public static class DragonFireballSetup
{
    private const string FireballPrefabPath = "Assets/Prefabs/Carthage/Towers/DragonFireball.prefab";
    private const string FireballMaterialPath = "Assets/Prefabs/Carthage/Towers/DragonFireball_Mat.mat";
    private const string DragonPrefabPath = "Assets/Prefabs/Carthage/Towers/dragon.prefab";

    [MenuItem("Carthage/Create Dragon Fireball")]
    public static void Apply()
    {
        GameObject fireballPrefab = CreateFireballPrefab();
        if (fireballPrefab == null) return;

        WireDragonPrefab(fireballPrefab);

        Debug.Log("DragonFireballSetup: done.");
    }

    private static GameObject CreateFireballPrefab()
    {
        // Re-runnable: start clean rather than trying to patch a possibly-differently-shaped prior version.
        AssetDatabase.DeleteAsset(FireballPrefabPath);
        AssetDatabase.DeleteAsset(FireballMaterialPath);

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "DragonFireball";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.transform.localScale = Vector3.one * .6f;

        Material material = new Material(FindShader());
        Color fireColor = new Color(1f, .35f, .05f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", fireColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", fireColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", fireColor * 4f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(material, FireballMaterialPath);
        root.GetComponent<MeshRenderer>().sharedMaterial = material;

        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(root.transform, false);
        Light glow = glowObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, .45f, .1f);
        glow.range = 6f;
        glow.intensity = 3f;
        glow.shadows = LightShadows.None;

        root.AddComponent<DragonFireball>();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, FireballPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("DragonFireballSetup: created " + FireballPrefabPath);
        return savedPrefab;
    }

    private static Shader FindShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");
    }

    // Both the muzzle reference and the per-level projectilePrefab fields live on CarthaginianDragonTower,
    // a component on the dragon PREFAB itself — not on the "Dragon of Carthage" CarthaginianTowerDefinition
    // ScriptableObject, which only holds upgrade cost/crew/ship data (an earlier version of this tool tried
    // to set projectilePrefab there instead, which doesn't exist on that type and threw a NullReference).
    private static void WireDragonPrefab(GameObject fireballPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(DragonPrefabPath);
        if (root == null) { Debug.LogWarning("DragonFireballSetup: couldn't load " + DragonPrefabPath); return; }

        try
        {
            Transform muzzle = root.transform.Find("Muzzle");
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("Muzzle");
                muzzleObject.transform.SetParent(root.transform, false);
                // Rough placeholder — a small forward/up offset from the tower's own pivot, not hand-placed
                // at the model's actual mouth (that needs eyes on the model). At least it now moves and
                // rotates with the tower instead of the old fixed "2 units above the root" world fallback.
                muzzleObject.transform.localPosition = new Vector3(0f, 1.2f, 1.5f);
                muzzle = muzzleObject.transform;
            }

            CarthaginianDragonTower dragonTower = root.GetComponent<CarthaginianDragonTower>();
            if (dragonTower == null) { Debug.LogWarning("DragonFireballSetup: " + DragonPrefabPath + " has no CarthaginianDragonTower on its root."); return; }

            SerializedObject so = new SerializedObject(dragonTower);
            so.FindProperty("muzzle").objectReferenceValue = muzzle;

            SerializedProperty levels = so.FindProperty("levels");
            for (int i = 0; i < levels.arraySize; i++)
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("projectilePrefab").objectReferenceValue = fireballPrefab;

            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(root, DragonPrefabPath);
            Debug.Log("DragonFireballSetup: wired Muzzle and " + levels.arraySize + " level(s) of projectilePrefab on " + DragonPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
