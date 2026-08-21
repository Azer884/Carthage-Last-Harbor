using UnityEditor;
using UnityEngine;

/// <summary>Tophet Fire Dock's 4 levels each "unlock" a differently-named fire ship (Fire Raft, Punic Fire
/// Ship, Veteran Fire Ship, Sacred Flame Ship), but all four pointed at the exact same FireShip.prefab —
/// the description even says "configure the ship prefabs with fire/explosion behavior later." That meant
/// the pricier, higher-crew-tier variants were mechanically IDENTICAL to the free-tier Fire Raft, just
/// strictly worse to build (more crew, longer cooldown, zero extra output) — unlike Cothon War Dock/
/// Lighthouse, whose tiers really do get stronger. Duplicates the base prefab into three scaled tiers and
/// rewires every matching-named ship option (across all 4 levels) to the right one. Safe to re-run.</summary>
public static class TophetFireShipTierSetup
{
    private const string BasePrefabPath = "Assets/Prefabs/Carthage/Ships/FireShip.prefab";
    private const string DefinitionPath = "Assets/ScriptableObjects/CarthageTowers/Attack/Tophet Fire Dock.asset";
    private const string StaleDescription = "A dangerous dock that launches expendable fire ships; configure the ship prefabs with fire/explosion behavior later.";
    private const string FinalDescription = "A dangerous dock that launches expendable fire ships to burn the Roman fleet.";

    private struct Tier
    {
        public string ShipName;
        public string Path;
        public float Damage;
        public float Cooldown;
        public float SightRange;
        public float CloseRange;
    }

    // Base Fire Raft (untouched): damage 10, cooldown 2 (DPS 5), sightRange 30, closeAttackRange 10.
    // Scaled up in roughly the same shape as Cothon War Dock's Ram Galley -> Quinquereme progression.
    private static readonly Tier[] Tiers =
    {
        new Tier { ShipName = "Punic Fire Ship", Path = "Assets/Prefabs/Carthage/Ships/PunicFireShip.prefab", Damage = 14f, Cooldown = 1.8f, SightRange = 34f, CloseRange = 11f },
        new Tier { ShipName = "Veteran Fire Ship", Path = "Assets/Prefabs/Carthage/Ships/VeteranFireShip.prefab", Damage = 18f, Cooldown = 1.6f, SightRange = 38f, CloseRange = 12f },
        new Tier { ShipName = "Sacred Flame Ship", Path = "Assets/Prefabs/Carthage/Ships/SacredFlameShip.prefab", Damage = 26f, Cooldown = 1.4f, SightRange = 42f, CloseRange = 13f },
    };

    [MenuItem("Carthage/Differentiate Tophet Fire Ships")]
    public static void Apply()
    {
        GameObject[] tierPrefabs = new GameObject[Tiers.Length];
        for (int i = 0; i < Tiers.Length; i++) tierPrefabs[i] = BuildTier(Tiers[i]);

        CarthaginianTowerDefinition definition = AssetDatabase.LoadAssetAtPath<CarthaginianTowerDefinition>(DefinitionPath);
        if (definition == null) { Debug.LogWarning("TophetFireShipTierSetup: couldn't load " + DefinitionPath); return; }

        SerializedObject so = new SerializedObject(definition);

        SerializedProperty description = so.FindProperty("description");
        if (description != null && description.stringValue == StaleDescription) description.stringValue = FinalDescription;

        int rewired = 0;
        SerializedProperty levels = so.FindProperty("levels");
        for (int levelIndex = 0; levelIndex < levels.arraySize; levelIndex++)
        {
            SerializedProperty unlockedShips = levels.GetArrayElementAtIndex(levelIndex).FindPropertyRelative("unlockedShips");
            for (int shipIndex = 0; shipIndex < unlockedShips.arraySize; shipIndex++)
            {
                SerializedProperty option = unlockedShips.GetArrayElementAtIndex(shipIndex);
                string shipName = option.FindPropertyRelative("shipName").stringValue;
                GameObject tierPrefab = ResolveTierPrefab(shipName, tierPrefabs);
                if (tierPrefab == null) continue;
                option.FindPropertyRelative("shipPrefab").objectReferenceValue = tierPrefab;
                rewired++;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        Debug.Log("TophetFireShipTierSetup: rewired " + rewired + " ship option(s) across all levels.");
    }

    private static GameObject ResolveTierPrefab(string shipName, GameObject[] tierPrefabs)
    {
        for (int i = 0; i < Tiers.Length; i++)
            if (Tiers[i].ShipName == shipName) return tierPrefabs[i];
        return null;
    }

    private static GameObject BuildTier(Tier tier)
    {
        AssetDatabase.DeleteAsset(tier.Path);
        GameObject root = PrefabUtility.LoadPrefabContents(BasePrefabPath);
        try
        {
            CarthaginianShipCombat combat = root.GetComponent<CarthaginianShipCombat>();
            if (combat != null)
            {
                SerializedObject so = new SerializedObject(combat);
                so.FindProperty("attackDamage").floatValue = tier.Damage;
                so.FindProperty("attackCooldown").floatValue = tier.Cooldown;
                so.FindProperty("sightRange").floatValue = tier.SightRange;
                so.FindProperty("closeAttackRange").floatValue = tier.CloseRange;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("TophetFireShipTierSetup: " + BasePrefabPath + " has no CarthaginianShipCombat — tier stats not applied for " + tier.ShipName);
            }

            root.name = tier.ShipName;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, tier.Path);
            Debug.Log("TophetFireShipTierSetup: created " + tier.Path);
            return saved;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
