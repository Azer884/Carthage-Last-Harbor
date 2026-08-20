using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>SidiBouSaidTower doesn't use CarthaginianTower's child-index convention — each level shows its
/// model via an explicit SidiBouSaidLevel.model reference instead. The SidiBou1..4 prefabs are already
/// parented under the tower in GameScene, but that reference was never wired up, so ApplyModel() had
/// nothing to show/hide. This finds them by name, assigns level I-IV in order, leaves only level I active,
/// and disables any Outline found under them that was left on by default (the same bug the Dragon had —
/// Outline should only light up on hover/select, driven by TowerSelectionManager). Safe to re-run.</summary>
public static class SidiBouSaidLevelSetup
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private static readonly string[] ChildNames = { "SidiBou1", "SidiBou2", "SidiBou3", "SidiBou4" };

    [MenuItem("Carthage/Wire Sidi Bou Said Levels")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        SidiBouSaidTower tower = Object.FindFirstObjectByType<SidiBouSaidTower>();
        if (tower == null) { Debug.LogWarning("SidiBouSaidLevelSetup: no SidiBouSaidTower found in " + ScenePath); return; }

        SerializedObject so = new SerializedObject(tower);
        SerializedProperty levels = so.FindProperty("levels");
        if (levels == null || levels.arraySize < ChildNames.Length)
        {
            Debug.LogWarning("SidiBouSaidLevelSetup: expected " + ChildNames.Length + " levels on SidiBouSaidTower, found " + (levels != null ? levels.arraySize : 0) + ".");
            return;
        }

        int wired = 0;
        int outlinesFixed = 0;
        for (int i = 0; i < ChildNames.Length; i++)
        {
            Transform child = tower.transform.Find(ChildNames[i]);
            if (child == null)
            {
                Debug.LogWarning("SidiBouSaidLevelSetup: no child named \"" + ChildNames[i] + "\" under " + tower.name + ".");
                continue;
            }

            levels.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue = child.gameObject;
            wired++;

            child.gameObject.SetActive(i == 0);

            foreach (Outline outline in child.GetComponentsInChildren<Outline>(true))
            {
                if (!outline.enabled) continue;
                outline.enabled = false;
                EditorUtility.SetDirty(outline);
                outlinesFixed++;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tower);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SidiBouSaidLevelSetup: wired " + wired + " level model(s), disabled " + outlinesFixed + " stray Outline component(s).");
    }
}
