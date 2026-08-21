using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds DayNightCycle to GameScene's Directional Light, pointed at itself. Safe to re-run — does
/// nothing if it's already there.</summary>
public static class DayNightCycleSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string LightObjectName = "Directional Light";

    [MenuItem("Carthage/Add Day Night Cycle")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject light = GameObject.Find(LightObjectName);
        if (light == null) { Debug.LogWarning("DayNightCycleSetup: no GameObject named \"" + LightObjectName + "\" found in " + ScenePath); return; }

        DayNightCycle cycle = light.GetComponent<DayNightCycle>();
        if (cycle != null)
        {
            Debug.Log("DayNightCycleSetup: " + LightObjectName + " already has a DayNightCycle — nothing to do.");
            return;
        }

        cycle = Undo.AddComponent<DayNightCycle>(light);
        EditorUtility.SetDirty(light);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DayNightCycleSetup: added DayNightCycle to " + LightObjectName + ".");
    }
}
