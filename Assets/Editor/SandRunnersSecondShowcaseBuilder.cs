using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandRunnersSecondShowcaseBuilder
{
    // Second-pass showcase generator.
    [MenuItem("SandRunners/Art/Rebuild Second Pass Showcases")]
    public static void Rebuild()
    {
        BuildPresentation();
        BuildScarabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SECOND_ART_SHOWCASES_REBUILT");
    }

    private static void BuildPresentation()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupScene("Second Pass Presentation", new Color(0.12f, 0.11f, 0.1f));

        GameObject pyramid = Instantiate("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_BattlePyramid_ArtPass.prefab", new Vector3(8f, 0f, 0f), Quaternion.identity);
        GameObject apex = Instantiate("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_Apex_Art.prefab", pyramid.transform);
        apex.name = "SR_Showcase_Apex";
        apex.transform.localPosition = new Vector3(0f, 3.72f, 0f);
        apex.transform.localScale = Vector3.one * 0.8f;

        Instantiate("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_ResourceMine_Art.prefab", new Vector3(-1.5f, 0f, 0f), Quaternion.identity);
        Instantiate("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_NeutralSettlement_Art.prefab", new Vector3(-10f, 0f, 0f), Quaternion.identity);
        Instantiate("Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabHowitzer_Art.prefab", new Vector3(1.2f, 0f, 7f), Quaternion.Euler(0f, 180f, 0f));

        GameObject camera = CameraObject(new Vector3(0f, 6.7f, 25f), new Vector3(0f, 1.5f, 1.8f), 36f);
        SaveScene(scene, "Assets/Scenes/SandRunnersPresentationShowcase.unity");
    }

    private static void BuildScarabs()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupScene("Second Pass Scarab Lineup", new Color(0.12f, 0.11f, 0.1f));

        string[] names = { "ScarabTank", "SalvageScarab", "ScarabWalker", "ScarabCarrierDrone", "ScarabHowitzer" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject unit = Instantiate("Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_" + names[i] + "_Art.prefab", new Vector3(-10f + i * 5f, 0f, 0f), Quaternion.identity);
            SandRunnersScarabArtAdapter adapter = unit.GetComponent<SandRunnersScarabArtAdapter>();
            if (adapter != null)
            {
                if (i == 1) adapter.SetArtState(SandRunnersScarabArtAdapter.ArtState.Salvaging);
                else if (i == 2) adapter.SetArtState(SandRunnersScarabArtAdapter.ArtState.Moving);
                else if (i == 3) adapter.SetArtState(SandRunnersScarabArtAdapter.ArtState.Airborne);
                else if (i == 4) adapter.SetArtState(SandRunnersScarabArtAdapter.ArtState.Firing);
                adapter.SetMotionInput(0.45f);
                adapter.SetSalvageInput(i == 1 ? 0.72f : 0f);
                adapter.SetAltitudeInput(i == 3 ? 0.8f : 0f);
                adapter.SetFireInput(i == 4 ? 0.55f : 0f);
            }
        }

        CameraObject(new Vector3(0f, 6.2f, 24.5f), new Vector3(0f, 1.6f, 0f), 34f);
        SaveScene(scene, "Assets/Scenes/SandRunnersScarabShowcase.unity");
    }

    private static GameObject Instantiate(string prefabPath, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }

    private static GameObject Instantiate(string prefabPath, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return instance;
    }

    private static GameObject CameraObject(Vector3 position, Vector3 target, float fov)
    {
        GameObject go = new GameObject("Showcase Camera");
        Camera camera = go.AddComponent<Camera>();
        camera.fieldOfView = fov;
        go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
        go.tag = "MainCamera";
        return go;
    }

    private static void SetupScene(string name, Color ambient)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fog = false;
        GameObject keyObject = new GameObject("Showcase Key Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 0.82f;
        key.color = new Color(1f, 0.88f, 0.7f);
        key.shadows = LightShadows.Soft;
        keyObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        GameObject fillObject = new GameObject("Showcase Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.12f;
        fill.color = new Color(0.55f, 0.7f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(25f, 142f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Showcase Ground";
        ground.transform.position = new Vector3(0f, -0.36f, 2f);
        ground.transform.localScale = new Vector3(4.5f, 1f, 2.2f);
        Renderer renderer = ground.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SandRunners/Models/ArtPass/SR_Art_DarkMetal.mat");
        Object.DestroyImmediate(ground.GetComponent<Collider>());
    }

    private static void SaveScene(Scene scene, string path)
    {
        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
