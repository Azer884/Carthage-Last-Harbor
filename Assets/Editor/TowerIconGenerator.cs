using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Renders a build-menu icon for every tower/resource definition straight from its own prefab —
/// no separately-authored icon art needed. Uses PreviewRenderUtility (the same machinery behind the
/// Inspector's model/material preview panes) instead of a raw Camera.Render() call or the small,
/// fixed-size AssetPreview thumbnail: it's pipeline-correct (URP-safe, and renders in a genuinely isolated
/// scene so it can't pick up unrelated content) *and* lets the render resolution be set explicitly, so
/// icons come out sharp instead of a blown-up ~128px thumbnail. Saves each capture under Assets/Icons,
/// imports it as a Sprite, and assigns it to the definition's icon field, replacing whatever was there.
/// Safe to re-run any time — e.g. after swapping a tower's prefab.</summary>
public static class TowerIconGenerator
{
    private const string IconsFolder = "Assets/Icons";
    private const int Resolution = 1024;
    private static readonly Color BackgroundColor = new Color(.22f, .22f, .24f, 1f);

    [MenuItem("Carthage/Generate Tower Icons")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(IconsFolder)) AssetDatabase.CreateFolder("Assets", "Icons");

        int generated = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:CarthaginianTowerDefinition"))
        {
            CarthaginianTowerDefinition definition = AssetDatabase.LoadAssetAtPath<CarthaginianTowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition == null || definition.prefab == null) continue;
            Sprite sprite = CaptureIcon(definition.prefab, definition.name);
            if (sprite == null) continue;
            Undo.RecordObject(definition, "Generate Tower Icon");
            definition.icon = sprite;
            EditorUtility.SetDirty(definition);
            generated++;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:CarthaginianResourceDefinition"))
        {
            CarthaginianResourceDefinition definition = AssetDatabase.LoadAssetAtPath<CarthaginianResourceDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition == null || definition.prefab == null) continue;
            Sprite sprite = CaptureIcon(definition.prefab, definition.name);
            if (sprite == null) continue;
            Undo.RecordObject(definition, "Generate Tower Icon");
            definition.icon = sprite;
            EditorUtility.SetDirty(definition);
            generated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("TowerIconGenerator: generated " + generated + " icon(s) in " + IconsFolder + ".");
    }

    private static Sprite CaptureIcon(GameObject prefab, string assetName)
    {
        PreviewRenderUtility utility = new PreviewRenderUtility();
        try
        {
            utility.camera.clearFlags = CameraClearFlags.SolidColor;
            utility.camera.backgroundColor = BackgroundColor;
            utility.camera.fieldOfView = 28f;
            utility.camera.nearClipPlane = .01f;
            utility.camera.farClipPlane = 10000f;
            utility.ambientColor = new Color(.32f, .32f, .35f);

            utility.lights[0].type = LightType.Directional;
            utility.lights[0].intensity = 1.1f;
            utility.lights[0].color = new Color(1f, .96f, .88f);
            utility.lights[0].transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            utility.lights[1].type = LightType.Directional;
            utility.lights[1].intensity = .45f;
            utility.lights[1].color = new Color(.75f, .82f, 1f);
            utility.lights[1].transform.rotation = Quaternion.Euler(25f, 150f, 0f);

            GameObject instance = Object.Instantiate(prefab);
            foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>()) behaviour.enabled = false;
            // Hands the instance over to the preview scene — PreviewRenderUtility owns and destroys it
            // from here (via Cleanup() below), so this must not also be destroyed manually.
            utility.AddSingleGO(instance);

            Bounds? bounds = ComputeBounds(instance);
            if (bounds == null)
            {
                Debug.LogWarning("TowerIconGenerator: \"" + assetName + "\" has no renderers — skipped.");
                return null;
            }
            FrameCamera(utility.camera, bounds.Value);

            utility.BeginStaticPreview(new Rect(0, 0, Resolution, Resolution));
            utility.Render(true);
            Texture2D captured = utility.EndStaticPreview();
            if (captured == null)
            {
                Debug.LogWarning("TowerIconGenerator: preview render came back empty for \"" + assetName + "\" — skipped.");
                return null;
            }

            return SaveIcon(captured, assetName);
        }
        finally
        {
            utility.Cleanup();
        }
    }

    private static Bounds? ComputeBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    // Three-quarter view, framed so the whole model fits with a little breathing room regardless of how
    // large or oddly-proportioned the prefab is.
    private static void FrameCamera(Camera camera, Bounds bounds)
    {
        float radius = Mathf.Max(bounds.extents.magnitude, .01f);
        float distance = radius / Mathf.Sin(camera.fieldOfView * .5f * Mathf.Deg2Rad) * 1.15f;
        Vector3 direction = new Vector3(1f, .85f, -1f).normalized;
        camera.transform.position = bounds.center + direction * distance;
        camera.transform.LookAt(bounds.center);
        camera.nearClipPlane = Mathf.Max(.01f, distance - radius * 2f);
        camera.farClipPlane = distance + radius * 2f;
    }

    private static Sprite SaveIcon(Texture2D captured, string assetName)
    {
        byte[] png = captured.EncodeToPNG();
        if (png == null || png.Length == 0)
        {
            Debug.LogWarning("TowerIconGenerator: \"" + assetName + "\" encoded to an empty PNG — skipped.");
            return null;
        }

        string path = IconsFolder + "/" + MakeSafeFileName(assetName) + ".png";
        File.WriteAllBytes(path, png);
        // ForceSynchronousImport matters here: without it, the importer settings below (and the
        // LoadAssetAtPath right after) can run before the initial import actually lands, which is how a
        // sprite ends up silently unassigned even though the PNG on disk is perfectly valid.
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("TowerIconGenerator: \"" + path + "\" didn't import as a texture — skipped.");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("TowerIconGenerator: \"" + path + "\" imported but no Sprite came back from it — icon not assigned.");
        return sprite;
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return name;
    }
}
