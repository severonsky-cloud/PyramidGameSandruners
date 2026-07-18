using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandRunnersBedroomIntroSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SandRunners/SebekBedroomIntro.unity";
    private const string MaterialsFolder = "Assets/Resources/SandRunners/Materials/Intro";

    [MenuItem("Sand Runners/Intro/Ensure Sebek Bedroom Intro Scene")]
    public static void BuildFromMenu()
    {
        EnsureSceneFromMenu();
    }

    [MenuItem("Sand Runners/Intro/Rebuild Sebek Bedroom Intro Scene")]
    public static void RebuildFromMenu()
    {
        BuildScene(true);
    }

    private static void EnsureSceneFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sebek bedroom intro generation skipped while Unity is entering or running Play Mode.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            if (EnsureLoadedSceneContent(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.Refresh();
                Debug.Log("Sebek bedroom intro scene updated manually at " + ScenePath + ".");
            }
            else
            {
                Debug.Log("Sebek bedroom intro scene is already up to date.");
            }
            return;
        }

        BuildScene(true);
    }

    private static void BuildScene(bool force)
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Scenes/SandRunners");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/SandRunners");
        EnsureFolder("Assets/Resources/SandRunners/Materials");
        EnsureFolder(MaterialsFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        Material floorMaterial = MaterialAsset("Intro Polished Gold Floor", new Color(0.52f, 0.38f, 0.18f, 1f), 0.45f, 0.62f);
        Material wallMaterial = MaterialAsset("Intro Warm Stone Wall", new Color(0.36f, 0.31f, 0.25f, 1f), 0.05f, 0.38f);
        Material darkMaterial = MaterialAsset("Intro Dark Bronze Trim", new Color(0.09f, 0.075f, 0.06f, 1f), 0.5f, 0.32f);
        Material bedMaterial = MaterialAsset("Intro Ivory Round Bed", new Color(0.9f, 0.82f, 0.68f, 1f), 0.02f, 0.72f);
        Material blanketMaterial = MaterialAsset("Intro Deep Red Blanket", new Color(0.42f, 0.045f, 0.035f, 1f), 0.08f, 0.55f);
        Material glassMaterial = MaterialAsset("Intro Blue Panoramic Glass", new Color(0.12f, 0.34f, 0.42f, 0.48f), 0.02f, 0.9f, true, new Color(0.04f, 0.34f, 0.5f, 1f), 0.35f);
        Material woodMaterial = MaterialAsset("Intro Red Cedar Furniture", new Color(0.31f, 0.13f, 0.075f, 1f), 0.12f, 0.45f);
        Material mirrorMaterial = MaterialAsset("Intro Soft Mirror", new Color(0.55f, 0.68f, 0.72f, 0.72f), 0.2f, 0.95f, true, new Color(0.07f, 0.2f, 0.24f, 1f), 0.35f);
        Material radioMaterial = MaterialAsset("Intro Radio Black Bakelite", new Color(0.025f, 0.023f, 0.02f, 1f), 0.35f, 0.24f);
        Material radioGlowMaterial = MaterialAsset("Intro Radio Warning Light", new Color(0.55f, 0.04f, 0.02f, 1f), 0f, 0.5f, false, new Color(1f, 0.12f, 0.04f, 1f), 1.5f);
        Material exteriorMaterial = MaterialAsset("Intro Exterior Pyramid Silhouette", new Color(0.18f, 0.12f, 0.07f, 1f), 0.2f, 0.34f);
        Material duneMaterial = MaterialAsset("Intro Distant Desert Gold", new Color(0.68f, 0.52f, 0.29f, 1f), 0f, 0.44f);
        Material sebekPlaceholderMaterial = MaterialAsset("Intro Sebek Placeholder Nightwear", new Color(0.18f, 0.16f, 0.19f, 1f), 0.05f, 0.55f, false, new Color(0.06f, 0.05f, 0.08f, 1f), 0.15f);

        GameObject root = new GameObject("Sebek_Bedroom_Intro_Blockout");
        SceneManager.MoveGameObjectToScene(root, scene);

        BuildRoom(root.transform, floorMaterial, wallMaterial, darkMaterial, glassMaterial, exteriorMaterial, duneMaterial);
        SebekBedroomInteractable[] bedroomInteractables = BuildFurniture(root.transform, bedMaterial, blanketMaterial, woodMaterial, darkMaterial, mirrorMaterial, radioMaterial, radioGlowMaterial, out Light radioLight, out Renderer[] radioRenderers);
        SebekBedroomInteractable[] routeInteractables = BuildPlayableIntroRoute(root.transform, floorMaterial, wallMaterial, darkMaterial, mirrorMaterial, radioGlowMaterial);
        SebekBedroomInteractable[] interactables = MergeInteractables(bedroomInteractables, routeInteractables);
        BuildPinupGallery(root.transform, darkMaterial);
        GameObject player = BuildPlayer(scene, sebekPlaceholderMaterial);
        Camera camera = BuildCamera(scene);
        BuildLighting(root.transform);

        GameObject controllerObject = new GameObject("Sebek_Bedroom_Intro_Controller");
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        SebekBedroomIntroController controller = controllerObject.AddComponent<SebekBedroomIntroController>();
        controllerObject.AddComponent<SebekPinupGalleryOverlay>();
        controller.playerRoot = player.transform;
        controller.playerSpawn = GameObject.Find("Sebek_Player_Spawn").transform;
        controller.sebekVisualRoot = player.transform.childCount > 0 ? player.transform.GetChild(0) : null;
        controller.sceneCamera = camera;
        controller.interactables = interactables;
        controller.radioSignalLight = radioLight;
        controller.radioPulseRenderers = radioRenderers;
        ConfigureIntroControllerBounds(controller);
        controller.cameraOffset = new Vector3(0f, 4.35f, -6.25f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log("Built Sebek bedroom intro scene at " + ScenePath + (force ? " via menu." : " automatically."));
    }

    private static bool EnsureLoadedSceneContent(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        GameObject root = FindRootGameObject(scene, "Sebek_Bedroom_Intro_Blockout");
        if (root == null)
            return false;

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/SandRunners");
        EnsureFolder("Assets/Resources/SandRunners/Materials");
        EnsureFolder(MaterialsFolder);

        bool changed = false;
        if (root.transform.Find("Sebek_Pinup_Art_Panels") == null)
        {
            Material frameMaterial = MaterialAsset("Intro Dark Bronze Trim", new Color(0.09f, 0.075f, 0.06f, 1f), 0.5f, 0.32f);
            changed |= BuildPinupGallery(root.transform, frameMaterial) > 0;
            changed |= EnsureGalleryOverlay(scene);
        }

        if (root.transform.Find("Sebek_Pyramid_Intro_Route") == null)
        {
            Material floorMaterial = MaterialAsset("Intro Polished Gold Floor", new Color(0.52f, 0.38f, 0.18f, 1f), 0.45f, 0.62f);
            Material wallMaterial = MaterialAsset("Intro Warm Stone Wall", new Color(0.36f, 0.31f, 0.25f, 1f), 0.05f, 0.38f);
            Material darkMaterial = MaterialAsset("Intro Dark Bronze Trim", new Color(0.09f, 0.075f, 0.06f, 1f), 0.5f, 0.32f);
            Material mirrorMaterial = MaterialAsset("Intro Soft Mirror", new Color(0.55f, 0.68f, 0.72f, 0.72f), 0.2f, 0.95f, true, new Color(0.07f, 0.2f, 0.24f, 1f), 0.35f);
            Material radioGlowMaterial = MaterialAsset("Intro Radio Warning Light", new Color(0.55f, 0.04f, 0.02f, 1f), 0f, 0.5f, false, new Color(1f, 0.12f, 0.04f, 1f), 1.5f);
            changed |= BuildPlayableIntroRoute(root.transform, floorMaterial, wallMaterial, darkMaterial, mirrorMaterial, radioGlowMaterial).Length > 0;
        }

        changed |= EnsureControllerHasAllInteractables(scene);
        return changed;
    }

    private static bool EnsureControllerHasAllInteractables(Scene scene)
    {
        GameObject controllerObject = FindRootGameObject(scene, "Sebek_Bedroom_Intro_Controller");
        if (controllerObject == null)
            return false;

        SebekBedroomIntroController controller = controllerObject.GetComponent<SebekBedroomIntroController>();
        if (controller == null)
            return false;

        List<SebekBedroomInteractable> sceneInteractables = new List<SebekBedroomInteractable>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            roots[i].GetComponentsInChildren(true, sceneInteractables);

        bool changed = controller.interactables == null || controller.interactables.Length != sceneInteractables.Count;
        if (!changed)
        {
            for (int i = 0; i < sceneInteractables.Count; i++)
            {
                if (controller.interactables[i] != sceneInteractables[i])
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return false;

        controller.interactables = sceneInteractables.ToArray();
        ConfigureIntroControllerBounds(controller);
        EditorUtility.SetDirty(controller);
        return true;
    }

    private static void ConfigureIntroControllerBounds(SebekBedroomIntroController controller)
    {
        controller.roomHalfExtents = new Vector2(8.1f, 17.25f);
        controller.bedroomHalfWidth = 8.1f;
        controller.bedroomForwardLimit = 5.2f;
        controller.corridorEntryZ = -5.35f;
        controller.corridorHalfWidth = 1.45f;
        controller.corridorBackLimit = -17.25f;
    }

    private static bool EnsureGalleryOverlay(Scene scene)
    {
        GameObject controllerObject = FindRootGameObject(scene, "Sebek_Bedroom_Intro_Controller");
        if (controllerObject == null || controllerObject.GetComponent<SebekPinupGalleryOverlay>() != null)
            return false;

        controllerObject.AddComponent<SebekPinupGalleryOverlay>();
        return true;
    }

    private static GameObject FindRootGameObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }

        return null;
    }

    private static void BuildRoom(Transform root, Material floor, Material wall, Material trim, Material glass, Material exterior, Material dune)
    {
        CreateBox(root, "Wide_Bedroom_Floor", new Vector3(0f, -0.08f, 0f), Quaternion.identity, new Vector3(18f, 0.16f, 12f), floor);
        CreateBox(root, "Wide_Bedroom_Ceiling", new Vector3(0f, 4.55f, 0f), Quaternion.identity, new Vector3(18f, 0.18f, 12f), wall);
        CreateBox(root, "Left_Bedroom_Wall", new Vector3(-9f, 2.2f, 0f), Quaternion.identity, new Vector3(0.18f, 4.4f, 12f), wall);
        CreateBox(root, "Right_Bedroom_Wall", new Vector3(9f, 2.2f, 0f), Quaternion.identity, new Vector3(0.18f, 4.4f, 12f), wall);
        CreateEntryWallOpening(root, wall, trim);
        CreateBox(root, "Window_Header_Wall", new Vector3(0f, 4.05f, 6f), Quaternion.identity, new Vector3(18f, 0.9f, 0.18f), wall);
        CreateBox(root, "Window_Low_Wall", new Vector3(0f, 0.55f, 6f), Quaternion.identity, new Vector3(18f, 1.1f, 0.18f), wall);

        CreateBox(root, "Panoramic_Window_Glass", new Vector3(0f, 2.35f, 5.92f), Quaternion.identity, new Vector3(15.6f, 2.9f, 0.06f), glass);
        for (int i = -3; i <= 3; i++)
            CreateBox(root, "Panoramic_Window_Vertical_Frame_" + (i + 3), new Vector3(i * 2.2f, 2.35f, 5.84f), Quaternion.identity, new Vector3(0.08f, 3.1f, 0.12f), trim);
        CreateBox(root, "Panoramic_Window_Top_Frame", new Vector3(0f, 3.9f, 5.84f), Quaternion.identity, new Vector3(15.9f, 0.12f, 0.14f), trim);
        CreateBox(root, "Panoramic_Window_Bottom_Frame", new Vector3(0f, 0.82f, 5.84f), Quaternion.identity, new Vector3(15.9f, 0.12f, 0.14f), trim);

        CreateBox(root, "Distant_Desert_Horizon", new Vector3(0f, 0.2f, 10.8f), Quaternion.identity, new Vector3(22f, 0.4f, 3.2f), dune, false);
        CreatePyramid(root, "Exterior_Pyramid_Silhouette", new Vector3(5.8f, 0.8f, 11.7f), Quaternion.Euler(0f, -22f, 0f), 3.2f, 3.8f, exterior);
        CreateBox(root, "Exterior_Pyramid_Rib_1", new Vector3(3.5f, 2.1f, 10.6f), Quaternion.Euler(0f, -22f, -22f), new Vector3(0.12f, 4.4f, 0.12f), trim, false);
        CreateBox(root, "Exterior_Pyramid_Rib_2", new Vector3(7.7f, 2.1f, 10.5f), Quaternion.Euler(0f, -22f, 22f), new Vector3(0.12f, 4.4f, 0.12f), trim, false);
    }

    private static void CreateEntryWallOpening(Transform root, Material wall, Material trim)
    {
        if (root.Find("Entry_Wall") != null)
            root.Find("Entry_Wall").gameObject.SetActive(false);
        if (root.Find("Entry_Wall_Left") != null)
            return;

        CreateBox(root, "Entry_Wall_Left", new Vector3(-5.35f, 2.2f, -6f), Quaternion.identity, new Vector3(7.3f, 4.4f, 0.18f), wall);
        CreateBox(root, "Entry_Wall_Right", new Vector3(5.35f, 2.2f, -6f), Quaternion.identity, new Vector3(7.3f, 4.4f, 0.18f), wall);
        CreateBox(root, "Entry_Wall_Header", new Vector3(0f, 3.55f, -6f), Quaternion.identity, new Vector3(3.4f, 1.7f, 0.18f), wall);
        CreateBox(root, "Entry_Door_Frame_Left", new Vector3(-1.77f, 1.55f, -5.9f), Quaternion.identity, new Vector3(0.16f, 3.1f, 0.28f), trim);
        CreateBox(root, "Entry_Door_Frame_Right", new Vector3(1.77f, 1.55f, -5.9f), Quaternion.identity, new Vector3(0.16f, 3.1f, 0.28f), trim);
        CreateBox(root, "Entry_Door_Frame_Top", new Vector3(0f, 3.08f, -5.9f), Quaternion.identity, new Vector3(3.72f, 0.16f, 0.28f), trim);
    }

    private static SebekBedroomInteractable[] BuildPlayableIntroRoute(Transform root, Material floor, Material wall, Material trim, Material glass, Material glow)
    {
        if (root.Find("Sebek_Pyramid_Intro_Route") != null)
            return new SebekBedroomInteractable[0];

        CreateEntryWallOpening(root, wall, trim);

        Transform route = new GameObject("Sebek_Pyramid_Intro_Route").transform;
        route.SetParent(root, false);

        List<SebekBedroomInteractable> interactables = new List<SebekBedroomInteractable>();

        GameObject exitDoor = CreateBox(route, "Bedroom_Exit_Door", new Vector3(0f, 1.38f, -5.72f), Quaternion.identity, new Vector3(2.55f, 2.68f, 0.14f), trim);
        interactables.Add(AddInteractable(exitDoor, SebekBedroomInteractable.InteractionKind.BedroomExit, "Open bedroom exit", "The private bedroom door leads into the inner pyramid corridor.", exitDoor.GetComponent<Renderer>()));

        CreateBox(route, "Corridor_Floor", new Vector3(0f, -0.075f, -11.3f), Quaternion.identity, new Vector3(3.45f, 0.15f, 11.65f), floor);
        CreateBox(route, "Corridor_Ceiling", new Vector3(0f, 3.65f, -11.3f), Quaternion.identity, new Vector3(3.45f, 0.18f, 11.65f), wall);
        CreateBox(route, "Corridor_Left_Wall", new Vector3(-1.8f, 1.8f, -11.3f), Quaternion.identity, new Vector3(0.18f, 3.65f, 11.65f), wall);
        CreateBox(route, "Corridor_Right_Wall", new Vector3(1.8f, 1.8f, -11.3f), Quaternion.identity, new Vector3(0.18f, 3.65f, 11.65f), wall);

        for (int i = 0; i < 6; i++)
        {
            float z = -6.9f - i * 1.75f;
            CreateBox(route, "Corridor_Left_Gold_Rib_" + i, new Vector3(-1.69f, 1.65f, z), Quaternion.identity, new Vector3(0.12f, 2.75f, 0.14f), trim);
            CreateBox(route, "Corridor_Right_Gold_Rib_" + i, new Vector3(1.69f, 1.65f, z), Quaternion.identity, new Vector3(0.12f, 2.75f, 0.14f), trim);
            CreateBox(route, "Corridor_Ceiling_Gold_Rib_" + i, new Vector3(0f, 3.42f, z), Quaternion.identity, new Vector3(3.45f, 0.1f, 0.16f), trim);
        }

        CreateBox(route, "Checkpoint_Floor_Seal", new Vector3(0f, 0.02f, -12.05f), Quaternion.identity, new Vector3(2.65f, 0.035f, 1.05f), glow, false);
        GameObject console = CreateBox(route, "Security_Checkpoint_Console", new Vector3(1.12f, 0.74f, -12.05f), Quaternion.Euler(0f, -18f, 0f), new Vector3(0.48f, 0.86f, 0.46f), trim);
        CreateBox(route, "Security_Console_Screen", new Vector3(0.84f, 1.2f, -12.0f), Quaternion.Euler(0f, -18f, 0f), new Vector3(0.42f, 0.28f, 0.05f), glass, false);
        interactables.Add(AddInteractable(console, SebekBedroomInteractable.InteractionKind.SecurityConsole, "Use security console", "The checkpoint console controls the lower holding-room seal.", console.GetComponent<Renderer>()));

        CreateBox(route, "Holding_Room_Door_Frame", new Vector3(0f, 1.52f, -16.85f), Quaternion.identity, new Vector3(3.05f, 3.15f, 0.18f), trim);
        GameObject prisonerDoor = CreateBox(route, "Prisoner_Holding_Room_Door", new Vector3(0f, 1.42f, -16.72f), Quaternion.identity, new Vector3(2.24f, 2.78f, 0.12f), wall);
        CreateBox(route, "Holding_Room_Red_Lock", new Vector3(0f, 1.8f, -16.58f), Quaternion.identity, new Vector3(0.24f, 0.24f, 0.05f), glow, false);
        interactables.Add(AddInteractable(prisonerDoor, SebekBedroomInteractable.InteractionKind.PrisonerDoor, "Open holding-room door", "The prisoner is behind this door.", prisonerDoor.GetComponent<Renderer>()));

        GameObject checkpointLight = new GameObject("Checkpoint_Red_Warning_Light");
        checkpointLight.transform.SetParent(route, false);
        checkpointLight.transform.localPosition = new Vector3(0f, 2.95f, -12.1f);
        Light warning = checkpointLight.AddComponent<Light>();
        warning.type = LightType.Point;
        warning.color = new Color(1f, 0.18f, 0.05f, 1f);
        warning.intensity = 1.45f;
        warning.range = 5.5f;

        GameObject corridorWarmLight = new GameObject("Corridor_Warm_Line_Light");
        corridorWarmLight.transform.SetParent(route, false);
        corridorWarmLight.transform.localPosition = new Vector3(0f, 2.85f, -8.9f);
        Light warm = corridorWarmLight.AddComponent<Light>();
        warm.type = LightType.Point;
        warm.color = new Color(1f, 0.67f, 0.32f, 1f);
        warm.intensity = 1.1f;
        warm.range = 6.2f;

        return interactables.ToArray();
    }

    private static SebekBedroomInteractable[] MergeInteractables(SebekBedroomInteractable[] first, SebekBedroomInteractable[] second)
    {
        int firstLength = first == null ? 0 : first.Length;
        int secondLength = second == null ? 0 : second.Length;
        SebekBedroomInteractable[] merged = new SebekBedroomInteractable[firstLength + secondLength];
        if (firstLength > 0)
            System.Array.Copy(first, merged, firstLength);
        if (secondLength > 0)
            System.Array.Copy(second, 0, merged, firstLength, secondLength);
        return merged;
    }

    private static int BuildPinupGallery(Transform root, Material frameMaterial)
    {
        Transform gallery = new GameObject("Sebek_Pinup_Art_Panels").transform;
        gallery.SetParent(root, false);

        int created = 0;
        created += CreatePinupPanel(gallery, "Framed_Art_Waking_Bed_Panel", "SandRunners/Art/Pinups/Sebek/Sebek_08_WakingBedPose", new Vector3(-4.2f, 2.25f, -5.86f), Quaternion.identity, new Vector2(1.35f, 1.35f), frameMaterial);
        created += CreatePinupPanel(gallery, "Framed_Art_Vanity_Mirror_Panel", "SandRunners/Art/Pinups/Sebek/Sebek_03_VanityMakeupMirror", new Vector3(8.86f, 2.28f, -1.55f), Quaternion.Euler(0f, -90f, 0f), new Vector2(1.18f, 1.18f), frameMaterial);
        created += CreatePinupPanel(gallery, "Framed_Art_Bathroom_Hook_Panel", "SandRunners/Art/Pinups/Sebek/Sebek_01_Bathroom_FirstRadioHook", new Vector3(-8.86f, 2.35f, -0.35f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.18f, 1.18f), frameMaterial);
        created += CreatePinupPanel(gallery, "Framed_Art_Bedroom_Luxury_Reference", "SandRunners/Art/Pinups/Sebek/Sebek_18_BedroomLuxuryRenderReference", new Vector3(4.2f, 2.25f, -5.86f), Quaternion.identity, new Vector2(1.35f, 1.35f), frameMaterial);

        if (created == 0)
            Object.DestroyImmediate(gallery.gameObject);

        return created;
    }

    private static int CreatePinupPanel(Transform parent, string name, string resourcePath, Vector3 position, Quaternion rotation, Vector2 size, Material frameMaterial)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return 0;

        Transform panel = new GameObject(name).transform;
        panel.SetParent(parent, false);
        panel.localPosition = position;
        panel.localRotation = rotation;
        panel.localScale = Vector3.one;

        Material artMaterial = TexturedMaterialAsset("Intro " + name, texture);
        GameObject art = GameObject.CreatePrimitive(PrimitiveType.Quad);
        art.name = "Art_Texture";
        art.transform.SetParent(panel, false);
        art.transform.localPosition = new Vector3(0f, 0f, -0.022f);
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = new Vector3(size.x, size.y, 1f);
        Renderer artRenderer = art.GetComponent<Renderer>();
        if (artRenderer != null)
            artRenderer.sharedMaterial = artMaterial;
        Collider artCollider = art.GetComponent<Collider>();
        if (artCollider != null)
            Object.DestroyImmediate(artCollider);

        float border = 0.075f;
        float depth = 0.08f;
        CreateBox(panel, "Frame_Top", new Vector3(0f, size.y * 0.5f + border * 0.5f, 0f), Quaternion.identity, new Vector3(size.x + border * 2f, border, depth), frameMaterial, false);
        CreateBox(panel, "Frame_Bottom", new Vector3(0f, -size.y * 0.5f - border * 0.5f, 0f), Quaternion.identity, new Vector3(size.x + border * 2f, border, depth), frameMaterial, false);
        CreateBox(panel, "Frame_Left", new Vector3(-size.x * 0.5f - border * 0.5f, 0f, 0f), Quaternion.identity, new Vector3(border, size.y + border * 2f, depth), frameMaterial, false);
        CreateBox(panel, "Frame_Right", new Vector3(size.x * 0.5f + border * 0.5f, 0f, 0f), Quaternion.identity, new Vector3(border, size.y + border * 2f, depth), frameMaterial, false);

        return 1;
    }

    private static Material TexturedMaterialAsset(string name, Texture2D texture)
    {
        Material material = MaterialAsset(name, Color.white, 0f, 0.68f);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static SebekBedroomInteractable[] BuildFurniture(Transform root, Material bed, Material blanket, Material wood, Material trim, Material mirror, Material radio, Material radioGlow, out Light radioLight, out Renderer[] radioRenderers)
    {
        radioLight = null;

        GameObject roundBed = CreateCylinder(root, "Round_Bed_By_Panoramic_Window", new Vector3(0f, 0.32f, 3.2f), Quaternion.identity, new Vector3(4.65f, 0.32f, 4.65f), bed);
        GameObject blanketObject = CreateBox(root, "Crimson_Blanket_Rumpled_Blockout", new Vector3(0f, 0.72f, 2.45f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.55f, 0.16f, 2.15f), blanket);
        CreateCylinder(root, "Round_Bed_Gold_Rim", new Vector3(0f, 0.54f, 3.2f), Quaternion.identity, new Vector3(4.95f, 0.08f, 4.95f), trim);
        CreateBox(root, "Left_Pillow_Blockout", new Vector3(-0.82f, 0.86f, 4.12f), Quaternion.Euler(0f, 12f, 0f), new Vector3(1.15f, 0.18f, 0.58f), bed);
        CreateBox(root, "Right_Pillow_Blockout", new Vector3(0.82f, 0.86f, 4.12f), Quaternion.Euler(0f, -12f, 0f), new Vector3(1.15f, 0.18f, 0.58f), bed);

        GameObject leftWardrobe = CreateBox(root, "Left_Tall_Wardrobe", new Vector3(-7.75f, 1.65f, 2.3f), Quaternion.identity, new Vector3(1.35f, 3.25f, 1.1f), wood);
        GameObject rightWardrobe = CreateBox(root, "Right_Tall_Wardrobe", new Vector3(7.75f, 1.65f, 2.35f), Quaternion.identity, new Vector3(1.35f, 3.25f, 1.1f), wood);
        CreateBox(root, "Left_Wardrobe_Gold_Handle", new Vector3(-7.05f, 1.55f, 2.3f), Quaternion.identity, new Vector3(0.08f, 0.85f, 0.08f), trim);
        CreateBox(root, "Right_Wardrobe_Gold_Handle", new Vector3(7.05f, 1.55f, 2.35f), Quaternion.identity, new Vector3(0.08f, 0.85f, 0.08f), trim);

        GameObject vanity = CreateBox(root, "Vanity_Table_With_Mirror", new Vector3(5.65f, 0.72f, -1.35f), Quaternion.identity, new Vector3(2.35f, 0.28f, 0.82f), wood);
        CreateBox(root, "Vanity_Left_Leg", new Vector3(4.68f, 0.34f, -1.08f), Quaternion.identity, new Vector3(0.18f, 0.68f, 0.18f), wood);
        CreateBox(root, "Vanity_Right_Leg", new Vector3(6.62f, 0.34f, -1.08f), Quaternion.identity, new Vector3(0.18f, 0.68f, 0.18f), wood);
        CreateBox(root, "Vanity_Mirror", new Vector3(5.65f, 1.82f, -1.78f), Quaternion.Euler(-4f, 0f, 0f), new Vector3(1.55f, 1.6f, 0.08f), mirror);

        GameObject desk = CreateBox(root, "Low_Work_Table", new Vector3(-5.65f, 0.76f, -2.15f), Quaternion.identity, new Vector3(2.55f, 0.26f, 1.05f), wood);
        CreateBox(root, "Low_Work_Table_Leg_A", new Vector3(-6.65f, 0.36f, -1.72f), Quaternion.identity, new Vector3(0.16f, 0.72f, 0.16f), wood);
        CreateBox(root, "Low_Work_Table_Leg_B", new Vector3(-4.65f, 0.36f, -1.72f), Quaternion.identity, new Vector3(0.16f, 0.72f, 0.16f), wood);
        CreateBox(root, "Low_Work_Table_Leg_C", new Vector3(-6.65f, 0.36f, -2.58f), Quaternion.identity, new Vector3(0.16f, 0.72f, 0.16f), wood);
        CreateBox(root, "Low_Work_Table_Leg_D", new Vector3(-4.65f, 0.36f, -2.58f), Quaternion.identity, new Vector3(0.16f, 0.72f, 0.16f), wood);

        GameObject bathDoor = CreateBox(root, "Private_Bathroom_Door", new Vector3(-8.88f, 1.45f, -3.55f), Quaternion.identity, new Vector3(0.14f, 2.85f, 1.45f), trim);
        CreateBox(root, "Bathroom_Door_Glow_Slit", new Vector3(-8.79f, 1.55f, -3.55f), Quaternion.identity, new Vector3(0.05f, 1.85f, 0.08f), mirror);

        CreateBox(root, "Right_Bedside_Table", new Vector3(3.45f, 0.54f, 3.85f), Quaternion.identity, new Vector3(1.2f, 0.72f, 0.92f), wood);
        GameObject radioObject = CreateBox(root, "Hidden_Radio_On_Bedside_Table", new Vector3(3.38f, 1.05f, 3.8f), Quaternion.Euler(0f, -18f, 0f), new Vector3(0.62f, 0.24f, 0.38f), radio);
        CreateBox(root, "Radio_Antenna", new Vector3(3.12f, 1.38f, 3.67f), Quaternion.Euler(0f, 0f, -28f), new Vector3(0.035f, 0.76f, 0.035f), trim);
        GameObject glow = CreateSphere(root, "Radio_Red_Signal_Light", new Vector3(3.64f, 1.18f, 3.58f), Quaternion.identity, new Vector3(0.12f, 0.12f, 0.12f), radioGlow);
        radioLight = glow.AddComponent<Light>();
        radioLight.type = LightType.Point;
        radioLight.color = new Color(1f, 0.13f, 0.05f, 1f);
        radioLight.range = 2.8f;
        radioLight.intensity = 0f;
        radioLight.enabled = false;
        radioRenderers = new[] { glow.GetComponent<Renderer>() };

        SebekBedroomInteractable bedInteractable = AddInteractable(roundBed, SebekBedroomInteractable.InteractionKind.Bed, "Inspect the round bed", "The round bed is still warm. The pyramid moved while Sebek slept.", blanketObject.GetComponent<Renderer>());
        SebekBedroomInteractable windowInteractable = AddInteractable(GameObject.Find("Panoramic_Window_Glass"), SebekBedroomInteractable.InteractionKind.Window, "Look through the panoramic window", "Beyond the glass: a pale desert horizon and the golden ribs of the pyramid.", GameObject.Find("Panoramic_Window_Glass").GetComponent<Renderer>());
        SebekBedroomInteractable leftWardrobeInteractable = AddInteractable(leftWardrobe, SebekBedroomInteractable.InteractionKind.Wardrobe, "Open wardrobe", "Ceremonial clothing and emergency gear. No time to dress properly yet.", leftWardrobe.GetComponent<Renderer>());
        SebekBedroomInteractable rightWardrobeInteractable = AddInteractable(rightWardrobe, SebekBedroomInteractable.InteractionKind.Wardrobe, "Check wardrobe", "The wardrobe doors are heavy, polished, and too quiet.", rightWardrobe.GetComponent<Renderer>());
        SebekBedroomInteractable vanityInteractable = AddInteractable(vanity, SebekBedroomInteractable.InteractionKind.Vanity, "Inspect vanity", "Perfume, metal combs, and a mirror that makes Sebek look more awake than she feels.", vanity.GetComponent<Renderer>());
        SebekBedroomInteractable deskInteractable = AddInteractable(desk, SebekBedroomInteractable.InteractionKind.Desk, "Inspect work table", "Maps, route seals, and a half-finished command note lie under the dust.", desk.GetComponent<Renderer>());
        SebekBedroomInteractable bathInteractable = AddInteractable(bathDoor, SebekBedroomInteractable.InteractionKind.BathroomDoor, "Open private bathroom", "Her private bathroom is behind this door. This will become the grooming/nightwear beat.", bathDoor.GetComponent<Renderer>());
        SebekBedroomInteractable radioInteractable = AddInteractable(radioObject, SebekBedroomInteractable.InteractionKind.Radio, "Pick up the radio", "The radio is silent for one breath, then hisses alive.", radioObject.GetComponent<Renderer>());

        return new[]
        {
            bedInteractable,
            windowInteractable,
            leftWardrobeInteractable,
            rightWardrobeInteractable,
            vanityInteractable,
            deskInteractable,
            bathInteractable,
            radioInteractable
        };
    }

    private static GameObject BuildPlayer(Scene scene, Material placeholderMaterial)
    {
        GameObject spawn = new GameObject("Sebek_Player_Spawn");
        SceneManager.MoveGameObjectToScene(spawn, scene);
        spawn.transform.position = new Vector3(-1.85f, 0f, 2.25f);
        spawn.transform.rotation = Quaternion.Euler(0f, 145f, 0f);

        GameObject player = new GameObject("Sebek_Player");
        SceneManager.MoveGameObjectToScene(player, scene);
        player.transform.position = spawn.transform.position;
        player.transform.rotation = spawn.transform.rotation;

        GameObject prefab = Resources.Load<GameObject>("SandRunners/Models/Sebek/Sebek_Walking");
        if (prefab != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            visual.name = "Sebek_Nu_Ankha_Bedroom_Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            ApplyMaterialToRenderers(visual, placeholderMaterial);
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }
        else
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Sebek_Placeholder_Body";
            fallback.transform.SetParent(player.transform, false);
            fallback.transform.localScale = new Vector3(0.62f, 1.05f, 0.62f);
        }

        return player;
    }

    private static void ApplyMaterialToRenderers(GameObject root, Material material)
    {
        if (root == null || material == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = material;
    }

    private static Camera BuildCamera(Scene scene)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-1.85f, 4.35f, -4f);
        cameraObject.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void BuildLighting(Transform root)
    {
        RenderSettings.ambientLight = new Color(0.18f, 0.17f, 0.2f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.12f, 0.16f, 1f);
        RenderSettings.fogDensity = 0.012f;

        GameObject moon = new GameObject("Window_Moonlight_Key");
        moon.transform.SetParent(root, false);
        moon.transform.position = new Vector3(-4f, 4f, 4.8f);
        moon.transform.rotation = Quaternion.Euler(52f, -18f, 0f);
        Light moonLight = moon.AddComponent<Light>();
        moonLight.type = LightType.Spot;
        moonLight.color = new Color(0.55f, 0.78f, 1f, 1f);
        moonLight.intensity = 2.2f;
        moonLight.range = 18f;
        moonLight.spotAngle = 72f;
        moonLight.shadows = LightShadows.Soft;

        GameObject warm = new GameObject("Bedside_Warm_Practical");
        warm.transform.SetParent(root, false);
        warm.transform.position = new Vector3(3.35f, 1.62f, 3.45f);
        Light warmLight = warm.AddComponent<Light>();
        warmLight.type = LightType.Point;
        warmLight.color = new Color(1f, 0.66f, 0.34f, 1f);
        warmLight.intensity = 1.15f;
        warmLight.range = 5.2f;
        warmLight.shadows = LightShadows.Soft;

        GameObject vanity = new GameObject("Vanity_Soft_Fill");
        vanity.transform.SetParent(root, false);
        vanity.transform.position = new Vector3(5.65f, 2.1f, -1.1f);
        Light vanityLight = vanity.AddComponent<Light>();
        vanityLight.type = LightType.Point;
        vanityLight.color = new Color(1f, 0.82f, 0.62f, 1f);
        vanityLight.intensity = 0.75f;
        vanityLight.range = 4.8f;
    }

    private static SebekBedroomInteractable AddInteractable(GameObject target, SebekBedroomInteractable.InteractionKind kind, string displayName, string examineText, Renderer highlight)
    {
        SebekBedroomInteractable interactable = target.GetComponent<SebekBedroomInteractable>();
        if (interactable == null)
            interactable = target.AddComponent<SebekBedroomInteractable>();
        interactable.kind = kind;
        interactable.displayName = displayName;
        interactable.examineText = examineText;
        interactable.highlightRenderer = highlight;
        return interactable;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool keepCollider = true)
    {
        return CreatePrimitive(parent, PrimitiveType.Cube, name, position, rotation, scale, material, keepCollider);
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool keepCollider = true)
    {
        return CreatePrimitive(parent, PrimitiveType.Cylinder, name, position, rotation, scale, material, keepCollider);
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool keepCollider = true)
    {
        return CreatePrimitive(parent, PrimitiveType.Sphere, name, position, rotation, scale, material, keepCollider);
    }

    private static GameObject CreatePrimitive(Transform parent, PrimitiveType primitiveType, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;
        gameObject.transform.localScale = scale;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        if (!keepCollider)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }
        return gameObject;
    }

    private static GameObject CreatePyramid(Transform parent, string name, Vector3 position, Quaternion rotation, float halfSize, float height, Material material)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localRotation = rotation;

        Mesh mesh = new Mesh();
        mesh.name = name + "_Mesh";
        mesh.vertices = new[]
        {
            new Vector3(-halfSize, 0f, -halfSize),
            new Vector3(halfSize, 0f, -halfSize),
            new Vector3(halfSize, 0f, halfSize),
            new Vector3(-halfSize, 0f, halfSize),
            new Vector3(0f, height, 0f)
        };
        mesh.triangles = new[]
        {
            0, 4, 1,
            1, 4, 2,
            2, 4, 3,
            3, 4, 0,
            0, 1, 2,
            0, 2, 3
        };
        mesh.RecalculateNormals();
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return gameObject;
    }

    private static Material MaterialAsset(string name, Color baseColor, float metallic, float smoothness, bool transparent = false, Color emission = default, float emissionStrength = 0f)
    {
        string path = MaterialsFolder + "/" + SanitizeAssetName(name) + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (emissionStrength > 0f)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emission * emissionStrength);
        }

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static string SanitizeAssetName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name.Replace(' ', '_');
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string child = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }
}
