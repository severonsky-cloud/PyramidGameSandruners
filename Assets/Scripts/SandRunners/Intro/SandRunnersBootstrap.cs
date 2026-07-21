using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public sealed class SandRunnersBootstrap : MonoBehaviour
{
    public const string IntroScenePath = "Assets/Scenes/SandRunners/SebekBedroomIntro.unity";
    public const string IntroSceneName = "SebekBedroomIntro";
    public const string RtsScenePath = "Assets/Scenes/SampleScene.unity";
    public const string RtsSceneName = "SampleScene";

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    public static bool IsFullPrologueAvailable()
    {
        return Application.CanStreamedLevelBeLoaded(IntroSceneName);
    }

    public static bool StartFullPrologue()
    {
        SandRunnersSessionBootstrap.RequestFullPrologue();
        return TryLoadScene(IntroScenePath, IntroSceneName, true);
    }

    public static bool StartSkippedPrologue()
    {
        SandRunnersSessionBootstrap.RequestRtsStart(true);
        return TryLoadScene(RtsScenePath, RtsSceneName, true);
    }

    public static bool StartDirectRts()
    {
        SandRunnersSessionBootstrap.RequestDirectRtsStart();
        return TryLoadScene(RtsScenePath, RtsSceneName, true);
    }

    public static bool StartRtsAfterCompletedPrologue()
    {
        SandRunnersSessionBootstrap.RequestRtsStart(false);
        return TryLoadScene(RtsScenePath, RtsSceneName, true);
    }

    public static bool ReturnToStartMenu(string reason = null)
    {
        SandRunnersSessionBootstrap.RequestStartMenu(reason);
        return TryLoadScene(RtsScenePath, RtsSceneName, false);
    }

    private static bool TryLoadScene(string editorPath, string playerName, bool fallbackToStartMenu)
    {
        try
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(editorPath))
            {
                EditorSceneManager.LoadSceneInPlayMode(editorPath, new LoadSceneParameters(LoadSceneMode.Single));
                return true;
            }
#endif
            SceneManager.LoadScene(playerName);
            return true;
        }
        catch (System.Exception ex)
        {
            string message = "SandRunners scene transition failed: " + ex.GetType().Name + ": " + ex.Message;
            Debug.LogError(message);
            if (fallbackToStartMenu && (editorPath != RtsScenePath || playerName != RtsSceneName))
                return ReturnToStartMenu(message);

            SandRunnersSessionBootstrap.RequestStartMenu(message);
            return false;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 170f, 520f, 340f);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 44f), "SandRunners Bootstrap", titleStyle);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 70f, panel.width - 72f, 52f), "Choose the startup route for this session.", bodyStyle);

        if (GUI.Button(new Rect(panel.x + 120f, panel.y + 132f, 280f, 38f), "FULL PROLOGUE", buttonStyle))
            StartFullPrologue();
        if (GUI.Button(new Rect(panel.x + 120f, panel.y + 182f, 280f, 38f), "SKIP PROLOGUE", buttonStyle))
            StartSkippedPrologue();
        if (GUI.Button(new Rect(panel.x + 120f, panel.y + 232f, 280f, 38f), "DIRECT RTS", buttonStyle))
            StartDirectRts();

        string error = SandRunnersSessionBootstrap.LastTransitionError;
        if (!string.IsNullOrEmpty(error))
            GUI.Label(new Rect(panel.x + 28f, panel.y + 286f, panel.width - 56f, 42f), error, bodyStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.76f, 0.24f, 1f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 14,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
    }
}
