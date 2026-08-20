using UnityEditor;
using UnityEngine;

/// <summary>Each SidiBou1..4 model is built from many separate mesh pieces (30-134 of them for levels
/// II-IV), but only a handful carried an Outline component, so selecting/hovering the tower only lit up
/// part of the model instead of its whole silhouette. Adds Outline to every Renderer-bearing descendant
/// that's missing one, directly on the source prefabs (not just the scene instance), disabled by default —
/// TowerSelectionManager is the only thing that should turn these on. Safe to re-run.</summary>
public static class SidiBouSaidOutlineSetup
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/SidiBouSaid/SidiBou1.prefab",
        "Assets/Prefabs/SidiBouSaid/SidiBou2.prefab",
        "Assets/Prefabs/SidiBouSaid/SidiBou3.prefab",
        "Assets/Prefabs/SidiBouSaid/SidiBou4.prefab",
    };

    [MenuItem("Carthage/Outline All Sidi Bou Said Model Parts")]
    public static void Apply()
    {
        int totalAdded = 0;
        foreach (string path in PrefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogWarning("SidiBouSaidOutlineSetup: couldn't load " + path); continue; }

            int added = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<Outline>() != null) continue;
                Outline outline = renderer.gameObject.AddComponent<Outline>();
                outline.enabled = false;
                // Matches the handful of pieces that already had an Outline in these prefabs, so the whole
                // model reads as one consistent glow instead of a mix of styles.
                outline.OutlineColor = new Color(1f, 1f, 0f, 1f);
                outline.OutlineWidth = 4.01f;
                added++;
            }

            if (added > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("SidiBouSaidOutlineSetup: added " + added + " Outline component(s) to " + path);
            totalAdded += added;
        }
        Debug.Log("SidiBouSaidOutlineSetup: done — " + totalAdded + " Outline component(s) added in total.");
    }
}
