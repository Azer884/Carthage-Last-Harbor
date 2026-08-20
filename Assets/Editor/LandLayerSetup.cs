using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Land-environment resource towers (Silver Mine, Oil Extractor) validate placement by
/// raycasting/overlapping against the "Land" physics layer, but nothing in GameScene is actually assigned
/// to it — the Terrain GameObject's TerrainCollider, the one collider that spans the whole map, is still on
/// Default. Without this, those towers can't be placed anywhere at all, zone or no zone. Safe to re-run.</summary>
public static class LandLayerSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string LandLayerName = "Land";
    private const string TerrainObjectName = "Terrain";

    [MenuItem("Carthage/Assign Land Layer To Terrain")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        int landLayer = LayerMask.NameToLayer(LandLayerName);
        if (landLayer < 0) { Debug.LogError("LandLayerSetup: no layer named \"" + LandLayerName + "\" exists in this project."); return; }

        GameObject terrain = GameObject.Find(TerrainObjectName);
        if (terrain == null) { Debug.LogWarning("LandLayerSetup: no GameObject named \"" + TerrainObjectName + "\" found in " + ScenePath); return; }

        if (terrain.layer == landLayer)
        {
            Debug.Log("LandLayerSetup: Terrain is already on the Land layer — nothing to do.");
            return;
        }

        terrain.layer = landLayer;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LandLayerSetup: Terrain moved onto the Land layer.");
    }
}
