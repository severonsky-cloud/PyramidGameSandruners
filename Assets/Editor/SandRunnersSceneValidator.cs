using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SandRunnersSceneValidationReport
{
    public readonly List<string> errors = new List<string>();
    public readonly List<string> warnings = new List<string>();

    public bool Passed
    {
        get { return errors.Count == 0; }
    }

    public string Summary
    {
        get { return "Errors: " + errors.Count + ", warnings: " + warnings.Count; }
    }
}

public static class SandRunnersSceneValidator
{
    private const string IntroScenePath = "Assets/Scenes/SandRunners/SebekBedroomIntro.unity";
    private const string RtsScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Sand Runners/Validation/Validate Startup Scenes (Read Only)")]
    public static void ValidateStartupScenesFromMenu()
    {
        SandRunnersSceneValidationReport report = ValidateStartupScenes();
        if (report.Passed)
        {
            Debug.Log("SandRunners startup scene validation passed. " + report.Summary);
            return;
        }

        for (int i = 0; i < report.errors.Count; i++)
            Debug.LogError(report.errors[i]);
        for (int i = 0; i < report.warnings.Count; i++)
            Debug.LogWarning(report.warnings[i]);
    }

    public static SandRunnersSceneValidationReport ValidateStartupScenes()
    {
        SandRunnersSceneValidationReport report = new SandRunnersSceneValidationReport();
        ValidateScene(report, RtsScenePath, typeof(SandRunnersPrototype));

        if (IsSceneEnabledInBuildSettings(IntroScenePath))
            ValidateScene(report, IntroScenePath, typeof(SebekBedroomIntroController));
        else
            report.warnings.Add("Sebek prologue is excluded from this RTS release profile and was not validated.");

        return report;
    }

    private static bool IsSceneEnabledInBuildSettings(string path)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled && scenes[i].path == path)
                return true;
        }

        return false;
    }

    private static void ValidateScene(SandRunnersSceneValidationReport report, string path, System.Type expectedController)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
        {
            report.errors.Add("Missing scene asset or GUID: " + path);
            return;
        }

        ValidateEditorBuildSettingsGuid(report, path, guid);

        Scene scene = EditorSceneManager.GetSceneByPath(path);
        bool openedByValidator = !scene.IsValid() || !scene.isLoaded;
        if (openedByValidator)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.errors.Add("Scene could not be loaded read-only for validation: " + path);
                return;
            }

            ValidateExpectedController(report, scene, expectedController);
            ValidateCrossSceneReferences(report, scene);
        }
        finally
        {
            if (openedByValidator && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateEditorBuildSettingsGuid(SandRunnersSceneValidationReport report, string path, string guid)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path != path)
                continue;

            string buildGuid = scenes[i].guid.ToString();
            if (buildGuid != guid)
                report.errors.Add("EditorBuildSettings GUID mismatch for " + path + ": expected " + guid + ", got " + buildGuid);
            return;
        }

        report.warnings.Add("Scene is not present in EditorBuildSettings: " + path);
    }

    private static void ValidateExpectedController(SandRunnersSceneValidationReport report, Scene scene, System.Type expectedController)
    {
        if (expectedController == null)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponentInChildren(expectedController, true) != null)
                return;
        }

        report.errors.Add("Missing expected controller " + expectedController.Name + " in " + scene.path);
    }

    private static void ValidateCrossSceneReferences(SandRunnersSceneValidationReport report, Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < transforms.Length; t++)
            {
                Component[] components = transforms[t].GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                    {
                        report.errors.Add("Missing script/component on " + GetTransformPath(transforms[t]) + " in " + scene.path);
                        continue;
                    }

                    ValidateComponentReferences(report, scene, components[c]);
                }
            }
        }
    }

    private static void ValidateComponentReferences(SandRunnersSceneValidationReport report, Scene ownerScene, Component component)
    {
        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(component);
        }
        catch (System.Exception ex)
        {
            report.errors.Add("Cannot inspect " + component.GetType().Name + " in " + ownerScene.path + ": " + ex.Message);
            return;
        }

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            Object referenced = property.objectReferenceValue;
            if (referenced == null || EditorUtility.IsPersistent(referenced))
                continue;

            Scene referencedScene = default;
            GameObject referencedGameObject = referenced as GameObject;
            if (referencedGameObject != null)
                referencedScene = referencedGameObject.scene;
            else
            {
                Component referencedComponent = referenced as Component;
                if (referencedComponent != null)
                    referencedScene = referencedComponent.gameObject.scene;
            }

            if (referencedScene.IsValid() && referencedScene != ownerScene)
            {
                report.errors.Add(
                    "Cross-scene reference in " + ownerScene.path + ": " +
                    component.gameObject.name + "." + property.propertyPath + " -> " + referenced.name +
                    " from " + referencedScene.path);
            }
        }
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return "<missing>";

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
