using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SandRunnersEditorPlayLauncher
{
    private const string RtsScenePath = "Assets/Scenes/SampleScene.unity";

    [InitializeOnLoadMethod]
    private static void InstallRtsPlayScene()
    {
        QueueRtsPlayScene();
    }

    [DidReloadScripts]
    private static void AfterScriptsReloaded()
    {
        QueueRtsPlayScene();
    }

    [MenuItem("SandRunners/Play RTS Now")]
    public static void PlayRtsNow()
    {
        if (!EnsureRtsPlayScene())
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[SandRunners] Unity is still importing or compiling. Press Play again when it finishes.");
            return;
        }

        EditorApplication.EnterPlaymode();
    }

    public static bool EnsureRtsPlayScene()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RtsScenePath);
        if (scene == null)
        {
            Debug.LogError("[SandRunners] RTS scene is missing: " + RtsScenePath);
            return false;
        }

        EditorSceneManager.playModeStartScene = scene;
        return true;
    }

    private static void QueueRtsPlayScene()
    {
        EditorApplication.delayCall -= ApplyRtsPlayScene;
        EditorApplication.delayCall += ApplyRtsPlayScene;
    }

    private static void ApplyRtsPlayScene()
    {
        EnsureRtsPlayScene();
    }
}
