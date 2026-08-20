using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>CarthaginianBuildMenu's attack/defense/resource section colors are serialized Inspector
/// fields, so they were baked into GameScene at whatever values existed when the object was placed — a
/// C# default-value change alone won't touch them. This applies the current CarthageTheme category colors
/// to the scene's own component instance. Safe to re-run any time.</summary>
public static class GameSceneThemeApplier
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Carthage/Apply In-Game UI Theme Colors")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        var buildMenu = Object.FindFirstObjectByType<CarthageDefense.CarthaginianBuildMenu>();
        if (buildMenu == null)
        {
            Debug.LogWarning("GameSceneThemeApplier: no CarthaginianBuildMenu found in " + ScenePath);
            return;
        }

        SerializedObject so = new SerializedObject(buildMenu);
        so.FindProperty("attackColor").colorValue = CarthageTheme.CategoryAttack;
        so.FindProperty("defenseColor").colorValue = CarthageTheme.CategoryDefense;
        so.FindProperty("resourceColor").colorValue = CarthageTheme.CategoryResource;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Applied in-game UI theme colors to GameScene.");
    }
}
