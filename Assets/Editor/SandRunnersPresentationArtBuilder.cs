using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SandRunnersPresentationArtBuilder
{
    private const string ArtRoot = "Assets/Resources/SandRunners/Models/ArtPass/";
    private const string OutputRoot = ArtRoot + "Presentation/";
    private const string BasePyramid = ArtRoot + "SR_BattlePyramid_Authored.prefab";
    private const string Truck0 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineConvoyTruck_LOD0.fbx";
    private const string Truck1 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineConvoyTruck_LOD1.fbx";
    private const string Truck2 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineConvoyTruck_LOD2.fbx";
    private const string Rover0 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineEngineerRover_LOD0.fbx";
    private const string Rover1 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineEngineerRover_LOD1.fbx";
    private const string Rover2 = "Assets/Resources/SandRunners/Models/ImportedCandidates/SR_MineEngineerRover_LOD2.fbx";

    [MenuItem("SandRunners/Art/Build Presentation Art Package")]
    public static void BuildAll()
    {
        EnsureFolder(OutputRoot);
        BuildPyramid();
        BuildApex();
        BuildResourceMine();
        BuildSettlement();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAll();
    }

    [MenuItem("SandRunners/Art/Validate Presentation Art Package")]
    public static void ValidateAll()
    {
        ValidatePrefab("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_BattlePyramid_ArtPass.prefab");
        ValidatePrefab("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_Apex_Art.prefab");
        ValidatePrefab("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_ResourceMine_Art.prefab");
        ValidatePrefab("Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_NeutralSettlement_Art.prefab");
    }

    private static void BuildPyramid()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePyramid);
        if (basePrefab == null)
        {
            Debug.LogError("Presentation art: missing pyramid base " + BasePyramid);
            return;
        }

        GameObject instance = StartFromPrefab(basePrefab, "SR_BattlePyramid_ArtPass");
        Transform artRoot = Child(instance.transform, "Pyramid_Art_Modules");
        Transform lod0 = Child(artRoot, "Pyramid_LOD0_Detail");
        Transform lod1 = Child(artRoot, "Pyramid_LOD1_Silhouette");
        Transform lod2 = Child(artRoot, "Pyramid_LOD2_Distance");

        Material gold = Mat("SR_Art_Gold.mat");
        Material sandstone = Mat("SR_Art_Ivory.mat");
        Material blackGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SandRunners/Materials/Intro/Intro_Blue_Panoramic_Glass.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material gunmetal = Mat("SR_Art_Gunmetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");

        Box(lod0, "Pyramid_Special_Sandstone_Shoulder_Left", new Vector3(-1.55f, 1.2f, -0.1f), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.36f, 1.6f, 2.3f), sandstone);
        Box(lod0, "Pyramid_Special_Sandstone_Shoulder_Right", new Vector3(1.55f, 1.2f, -0.1f), Quaternion.Euler(0f, 0f, 18f), new Vector3(0.36f, 1.6f, 2.3f), sandstone);
        Box(lod0, "Pyramid_Black_Observation_Glass", new Vector3(0f, 1.42f, -1.98f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(1.22f, 0.62f, 0.05f), blackGlass);
        Box(lod0, "Pyramid_Brass_Door_Left", new Vector3(-0.72f, 0.68f, -2.22f), Quaternion.identity, new Vector3(0.66f, 0.82f, 0.08f), brass);
        Box(lod0, "Pyramid_Brass_Door_Right", new Vector3(0.72f, 0.68f, -2.22f), Quaternion.identity, new Vector3(0.66f, 0.82f, 0.08f), brass);
        Box(lod0, "Pyramid_Door_Lintel", new Vector3(0f, 1.14f, -2.25f), Quaternion.identity, new Vector3(1.65f, 0.08f, 0.1f), brass);

        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Box(lod0, "Pyramid_Track_Housing_" + label, new Vector3(side * 2.45f, -0.18f, 0f), Quaternion.identity, new Vector3(0.62f, 0.48f, 4.2f), dark);
            Box(lod0, "Pyramid_Suspension_Armor_" + label, new Vector3(side * 2.48f, 0.18f, 0f), Quaternion.identity, new Vector3(0.72f, 0.2f, 3.75f), gunmetal);
            for (int i = 0; i < 6; i++)
            {
                float z = -1.5f + i * 0.6f;
                Cylinder(lod0, "Pyramid_Track_Wheel_" + label + "_" + i, new Vector3(side * 2.52f, -0.25f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.28f, 0.1f, 0.28f), brass);
                Box(lod0, "Pyramid_Suspension_Arm_" + label + "_" + i, new Vector3(side * 2.5f, -0.02f, z), Quaternion.identity, new Vector3(0.75f, 0.055f, 0.16f), dark);
            }
        }

        Box(lod0, "Pyramid_Reactor_Vent_Left", new Vector3(-1.08f, 1.14f, -1.45f), Quaternion.Euler(-15f, 0f, 0f), new Vector3(0.44f, 0.18f, 0.08f), cyan);
        Box(lod0, "Pyramid_Reactor_Vent_Right", new Vector3(1.08f, 1.14f, -1.45f), Quaternion.Euler(-15f, 0f, 0f), new Vector3(0.44f, 0.18f, 0.08f), cyan);

        Box(lod1, "Pyramid_Sandstone_Silhouette", new Vector3(0f, 1.1f, 0f), Quaternion.identity, new Vector3(3.6f, 2.1f, 3.6f), sandstone);
        Box(lod1, "Pyramid_Track_Silhouette_Left", new Vector3(-2.45f, -0.12f, 0f), Quaternion.identity, new Vector3(0.68f, 0.45f, 4.1f), dark);
        Box(lod1, "Pyramid_Track_Silhouette_Right", new Vector3(2.45f, -0.12f, 0f), Quaternion.identity, new Vector3(0.68f, 0.45f, 4.1f), dark);
        Box(lod1, "Pyramid_Glass_Silhouette", new Vector3(0f, 1.35f, -1.9f), Quaternion.identity, new Vector3(1.5f, 0.45f, 0.08f), blackGlass);

        Box(lod2, "Pyramid_Distance_Hull", new Vector3(0f, 1f, 0f), Quaternion.identity, new Vector3(4.8f, 2.4f, 4.8f), gold);
        Box(lod2, "Pyramid_Distance_Tracks_Left", new Vector3(-2.45f, -0.1f, 0f), Quaternion.identity, new Vector3(0.72f, 0.36f, 4f), dark);
        Box(lod2, "Pyramid_Distance_Tracks_Right", new Vector3(2.45f, -0.1f, 0f), Quaternion.identity, new Vector3(0.72f, 0.36f, 4f), dark);

        Transform anchors = Child(artRoot, "Pyramid_Art_Anchors");
        Marker(anchors, "Muzzle_Main", new Vector3(0f, 2.65f, -1.1f));
        Marker(anchors, "Muzzle_Left", new Vector3(-2.85f, 0.8f, -0.7f));
        Marker(anchors, "Muzzle_Right", new Vector3(2.85f, 0.8f, -0.7f));
        Marker(anchors, "Engine_Main", new Vector3(0f, 0.2f, 1.55f));
        Marker(anchors, "Door_Cargo", new Vector3(0f, 0.65f, -2.45f));
        Marker(anchors, "Hardpoint_Main", new Vector3(0f, 2.7f, 0f));
        Marker(anchors, "Hardpoint_Left", new Vector3(-1.4f, 1.6f, 0.4f));
        Marker(anchors, "Hardpoint_Right", new Vector3(1.4f, 1.6f, 0.4f));
        Marker(anchors, "Apex_Focus", new Vector3(0f, 3.45f, 0f));

        Light reactor = PointLight(artRoot, "Pyramid_Reactor_Light", new Vector3(0f, 1.15f, -0.9f), new Color(0.16f, 0.85f, 1f), 1.1f, 7f);
        Light apex = PointLight(artRoot, "Pyramid_Apex_Light", new Vector3(0f, 3.2f, 0f), new Color(1f, 0.78f, 0.25f), 0.6f, 6f);
        SandRunnersPyramidArtAdapter adapter = instance.AddComponent<SandRunnersPyramidArtAdapter>();
        adapter.Configure(FindAllNamed(artRoot, "Pyramid_Track_Wheel"), FindNamed(artRoot, "Pyramid_Brass_Door_Left"), FindNamed(artRoot, "Pyramid_Brass_Door_Right"), reactor, apex);

        AddLodGroup(instance, new[] { lod0, lod1, lod2 }, 9f);
        SavePrefab(instance, "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_BattlePyramid_ArtPass.prefab");
    }

    private static void BuildApex()
    {
        GameObject instance = NewRoot("SR_Apex_Art");
        Transform artRoot = Child(instance.transform, "Apex_Art_Modules");
        Transform lod0 = Child(artRoot, "Apex_LOD0_Detail");
        Transform lod1 = Child(artRoot, "Apex_LOD1_Silhouette");
        Transform lod2 = Child(artRoot, "Apex_LOD2_Distance");

        Material gold = Mat("SR_Art_Gold.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");
        Material ivory = Mat("SR_Art_Ivory.mat");

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 0.72f, 0f, Mathf.Sin(angle) * 0.72f);
            Box(lod0, "Apex_Mirror_Panel_" + i, p + Vector3.up * 0.15f, Quaternion.Euler(0f, -i * 90f, 18f), new Vector3(0.82f, 0.06f, 0.42f), ivory);
            Box(lod0, "Apex_Mirror_Frame_" + i, p + Vector3.up * 0.22f, Quaternion.Euler(0f, -i * 90f, 18f), new Vector3(0.92f, 0.08f, 0.5f), brass);
        }
        Cylinder(lod0, "Apex_Mirror_Base", new Vector3(0f, -0.02f, 0f), Quaternion.identity, new Vector3(1.35f, 0.14f, 1.35f), dark);
        Cylinder(lod0, "Apex_Focus_Point", new Vector3(0f, 0.58f, 0f), Quaternion.identity, new Vector3(0.22f, 0.22f, 0.22f), cyan);
        Light focusLight = PointLight(lod0, "Apex_Focus_Light", new Vector3(0f, 0.58f, 0f), new Color(0.18f, 0.9f, 1f), 1.8f, 5.5f);
        Box(lod0, "Apex_Beam_Collar", new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(0.36f, 0.22f, 0.36f), gold);
        Box(lod0, "Apex_Beam_Crown", new Vector3(0f, 1.13f, 0f), Quaternion.identity, new Vector3(0.18f, 0.28f, 0.18f), dark);

        LineRenderer beam = lod0.gameObject.AddComponent<LineRenderer>();
        beam.name = "Apex_Normal_Solar_Beam";
        beam.sharedMaterial = cyan;
        beam.positionCount = 2;
        beam.useWorldSpace = false;
        beam.SetPosition(0, new Vector3(0f, 5.8f, 0f));
        beam.SetPosition(1, new Vector3(0f, 0.6f, 0f));
        beam.startWidth = 0.08f;
        beam.endWidth = 0.025f;
        beam.enabled = false;

        GameObject flowObject = new GameObject("Apex_Descending_Solar_Charge_Flow");
        flowObject.transform.SetParent(lod0, false);
        ParticleSystem flow = flowObject.AddComponent<ParticleSystem>();
        var main = flow.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.8f;
        main.startSpeed = -1.8f;
        main.startSize = 0.11f;
        main.startColor = new Color(1f, 0.82f, 0.25f, 0.95f);
        main.maxParticles = 160;
        var emission = flow.emission;
        emission.rateOverTime = 0f;
        var shape = flow.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.18f;
        shape.position = new Vector3(0f, 4.7f, 0f);
        var particleRenderer = flowObject.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null) particleRenderer.sharedMaterial = brass;

        Transform anchors = Child(artRoot, "Apex_Art_Anchors");
        Marker(anchors, "Apex_Focus_Anchor", new Vector3(0f, 0.58f, 0f));
        Marker(anchors, "Solar_Charge_Origin", new Vector3(0f, 5.8f, 0f));
        Marker(anchors, "Solar_Beam_Target", new Vector3(0f, 0.6f, 0f));
        Marker(anchors, "Apex_Mirror_Left", new Vector3(-0.72f, 0.15f, 0f));
        Marker(anchors, "Apex_Mirror_Right", new Vector3(0.72f, 0.15f, 0f));
        Marker(anchors, "Apex_Fire_Muzzle", new Vector3(0f, 1.3f, 0f));

        Box(lod1, "Apex_Mirror_Silhouette", Vector3.zero, Quaternion.identity, new Vector3(2.2f, 0.3f, 2.2f), brass);
        Cylinder(lod1, "Apex_Focus_Silhouette", new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(0.32f, 0.22f, 0.32f), cyan);
        Box(lod2, "Apex_Distance_Crown", new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(1.8f, 0.4f, 1.8f), gold);

        AudioSource chargeAudio = Audio(lod0, "Apex_Charge_Audio", "SandRunners/Audio/SR_Pyramid_Engine", 0.45f);
        AudioSource fireAudio = Audio(lod0, "Apex_Fire_Audio", "SandRunners/Audio/SR_Howitzer", 0.5f);
        AudioSource recoveryAudio = Audio(lod0, "Apex_Recovery_Audio", "SandRunners/Audio/SR_AmbiencePulse", 0.3f);
        SandRunnersApexArtAdapter adapter = instance.AddComponent<SandRunnersApexArtAdapter>();
        adapter.Configure(FindNamed(lod0, "Apex_Focus_Point"), focusLight, beam, flow, chargeAudio, fireAudio, recoveryAudio);

        AddLodGroup(instance, new[] { lod0, lod1, lod2 }, 4f);
        SavePrefab(instance, "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_Apex_Art.prefab");
    }

    private static void BuildResourceMine()
    {
        GameObject instance = NewRoot("SR_ResourceMine_Art");
        Transform artRoot = Child(instance.transform, "ResourceMine_Art_Modules");
        Transform lod0 = Child(artRoot, "ResourceMine_LOD0_Detail");
        Transform lod1 = Child(artRoot, "ResourceMine_LOD1_Silhouette");
        Transform lod2 = Child(artRoot, "ResourceMine_LOD2_Distance");

        AddModel(lod0, Truck2, "Mine_ConvoyTruck_ImportedLOD2", new Vector3(-1.7f, 0f, 0.4f), Quaternion.identity);
        AddModel(lod0, Rover2, "Mine_EngineerRover_ImportedLOD2", new Vector3(1.7f, 0f, -0.3f), Quaternion.Euler(0f, 180f, 0f));

        Material gold = Mat("SR_Art_Gold.mat");
        Material paleGold = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");
        Material jade = Mat("SR_Art_Jade.mat");

        Box(lod0, "Mine_Extraction_Platform", new Vector3(0f, -0.22f, 0f), Quaternion.identity, new Vector3(6.2f, 0.16f, 4.2f), dark);
        Box(lod0, "Mine_Cargo_Silo", new Vector3(0f, 1.2f, 1.05f), Quaternion.identity, new Vector3(1.9f, 2f, 1.3f), jade);
        for (int side = -1; side <= 1; side += 2)
        {
            Cylinder(lod0, "Mine_Drill_" + (side < 0 ? "Left" : "Right"), new Vector3(side * 2.35f, 0.5f, -1.1f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f, 0.65f, 0.42f), paleGold);
            Box(lod0, "Mine_Drill_Frame_" + (side < 0 ? "Left" : "Right"), new Vector3(side * 2.35f, 0.4f, -1.1f), Quaternion.identity, new Vector3(0.7f, 0.18f, 0.7f), dark);
        }
        Box(lod0, "Mine_Cyan_Extraction_Core", new Vector3(0f, 1.55f, -0.35f), Quaternion.identity, new Vector3(0.75f, 0.32f, 0.08f), cyan);
        PointLight(artRoot, "Mine_Signal_Light", new Vector3(0f, 3.1f, 1.05f), new Color(0.2f, 0.95f, 1f), 1.1f, 9f);

        Box(lod1, "Mine_Silhouette_Platform", new Vector3(0f, -0.2f, 0f), Quaternion.identity, new Vector3(6.1f, 0.2f, 4.1f), dark);
        Box(lod1, "Mine_Silhouette_Silo", new Vector3(0f, 1.15f, 0.8f), Quaternion.identity, new Vector3(2.2f, 1.8f, 1.5f), jade);
        Box(lod1, "Mine_Silhouette_Drill_Left", new Vector3(-2.3f, 0.4f, -1f), Quaternion.identity, new Vector3(0.55f, 1.1f, 0.55f), paleGold);
        Box(lod1, "Mine_Silhouette_Drill_Right", new Vector3(2.3f, 0.4f, -1f), Quaternion.identity, new Vector3(0.55f, 1.1f, 0.55f), paleGold);
        Box(lod2, "Mine_Distance_Platform", new Vector3(0f, -0.2f, 0f), Quaternion.identity, new Vector3(5.8f, 0.2f, 3.8f), dark);
        Box(lod2, "Mine_Distance_Silo", new Vector3(0f, 0.9f, 0.6f), Quaternion.identity, new Vector3(2.5f, 1.3f, 1.7f), jade);

        Transform anchors = Child(artRoot, "ResourceMine_Art_Anchors");
        Marker(anchors, "Engine_Main", new Vector3(-1.7f, 0.7f, 0.4f));
        Marker(anchors, "Door_Cargo", new Vector3(0f, 1.9f, 1.7f));
        Marker(anchors, "Hardpoint_Main", new Vector3(0f, 2.6f, 0f));
        Marker(anchors, "Mine_Drill_Left", new Vector3(-2.35f, 0.5f, -1.1f));
        Marker(anchors, "Mine_Drill_Right", new Vector3(2.35f, 0.5f, -1.1f));
        Marker(anchors, "Mine_Resource_Output", new Vector3(0f, 1.4f, 1.9f));

        SandRunnersResourceMineArtAdapter adapter = instance.AddComponent<SandRunnersResourceMineArtAdapter>();
        adapter.Configure(new[] { FindNamed(lod0, "Mine_Drill_Left"), FindNamed(lod0, "Mine_Drill_Right") }, FindNamed(artRoot, "Mine_Signal_Light") != null ? FindNamed(artRoot, "Mine_Signal_Light").GetComponent<Light>() : null);
        AddLodGroup(instance, new[] { lod0, lod1, lod2 }, 8f);
        SavePrefab(instance, "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_ResourceMine_Art.prefab");
    }

    private static void BuildSettlement()
    {
        GameObject instance = NewRoot("SR_NeutralSettlement_Art");
        Transform artRoot = Child(instance.transform, "NeutralSettlement_Art_Modules");
        Transform lod0 = Child(artRoot, "NeutralSettlement_LOD0_Detail");
        Transform lod1 = Child(artRoot, "NeutralSettlement_LOD1_Silhouette");
        Transform lod2 = Child(artRoot, "NeutralSettlement_LOD2_Distance");

        Material ivory = Mat("SR_Art_Ivory.mat");
        Material jade = Mat("SR_Art_Jade.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");

        Cylinder(lod0, "Settlement_Trading_Plaza", new Vector3(0f, -0.16f, 0f), Quaternion.identity, new Vector3(7.8f, 0.18f, 7.8f), dark);
        Box(lod0, "Settlement_Exchange_Hall", new Vector3(0f, 1.7f, 0f), Quaternion.identity, new Vector3(4.8f, 3.1f, 3.6f), ivory);
        Box(lod0, "Settlement_Hall_Roof", new Vector3(0f, 3.55f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(3.7f, 0.22f, 3.7f), brass);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 5.5f, 0.85f, Mathf.Sin(angle) * 5.5f);
            Box(lod0, "Settlement_Market_Stall_" + i, p, Quaternion.Euler(0f, i * 90f, 0f), new Vector3(1.35f, 1.7f, 1.35f), jade);
            Box(lod0, "Settlement_Stall_Canopy_" + i, p + Vector3.up * 1.15f, Quaternion.Euler(0f, i * 90f, 45f), new Vector3(1.5f, 0.08f, 1.5f), brass);
        }
        Cylinder(lod0, "Settlement_Beacon", new Vector3(0f, 5.15f, 0f), Quaternion.identity, new Vector3(0.28f, 1.5f, 0.28f), dark);
        Sphere(lod0, "Settlement_Beacon_Crystal", new Vector3(0f, 6.75f, 0f), new Vector3(0.38f, 0.38f, 0.38f), cyan);
        Light signal = PointLight(artRoot, "Settlement_Signal_Light", new Vector3(0f, 6.75f, 0f), new Color(0.18f, 0.8f, 1f), 1.2f, 16f);
        for (int i = 0; i < 3; i++)
        {
            float angle = (i * 120f + 20f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 3.2f, 0.3f, Mathf.Sin(angle) * 3.2f);
            Box(lod0, "Settlement_Defense_Pad_" + i, p, Quaternion.identity, new Vector3(1.2f, 0.12f, 1.2f), dark);
            Marker(lod0, "Settlement_Trade_Anchor_" + i, p + Vector3.up * 0.3f);
        }

        Box(lod1, "Settlement_Silhouette_Plaza", new Vector3(0f, -0.15f, 0f), Quaternion.identity, new Vector3(8f, 0.2f, 8f), dark);
        Box(lod1, "Settlement_Silhouette_Hall", new Vector3(0f, 1.5f, 0f), Quaternion.identity, new Vector3(5.2f, 2.8f, 4f), ivory);
        Cylinder(lod1, "Settlement_Silhouette_Beacon", new Vector3(0f, 4.8f, 0f), Quaternion.identity, new Vector3(0.38f, 1.6f, 0.38f), dark);
        Box(lod2, "Settlement_Distance_Plaza", new Vector3(0f, -0.12f, 0f), Quaternion.identity, new Vector3(7.5f, 0.18f, 7.5f), dark);
        Box(lod2, "Settlement_Distance_Hall", new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(5.8f, 2.2f, 4.4f), ivory);

        Transform anchors = Child(artRoot, "NeutralSettlement_Art_Anchors");
        Marker(anchors, "Settlement_Trade_Anchor", new Vector3(0f, 0.4f, -4.8f));
        Marker(anchors, "Settlement_Defense_Anchor", new Vector3(4.6f, 0.3f, 0f));
        Marker(anchors, "Settlement_Caravan_Anchor", new Vector3(-4.6f, 0.3f, 0f));
        Marker(anchors, "Settlement_Diplomacy_Marker", new Vector3(0f, 7.3f, 0f));

        SandRunnersSettlementArtAdapter adapter = instance.AddComponent<SandRunnersSettlementArtAdapter>();
        adapter.Configure(FindNamed(artRoot, "Settlement_Beacon"), signal);
        AddLodGroup(instance, new[] { lod0, lod1, lod2 }, 12f);
        SavePrefab(instance, "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_NeutralSettlement_Art.prefab");
    }

    private static GameObject StartFromPrefab(GameObject source, string name)
    {
        string path = OutputRoot + name + ".prefab";
        DeleteIfExists(path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private static GameObject NewRoot(string name)
    {
        DeleteIfExists(OutputRoot + name + ".prefab");
        return new GameObject(name);
    }

    private static void SavePrefab(GameObject instance, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    private static void AddModel(Transform parent, string assetPath, string name, Vector3 position, Quaternion rotation)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (model == null)
        {
            Debug.LogWarning("Presentation art: missing model " + assetPath);
            return;
        }
        GameObject instance = (GameObject)Object.Instantiate(model);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = Vector3.one;
        Material mineBody = Mat("SR_Art_DarkMetal.mat");
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = mineBody;
    }

    private static void AddLodGroup(GameObject root, Transform[] lodRoots, float size)
    {
        LODGroup group = root.GetComponent<LODGroup>();
        if (group == null) group = root.AddComponent<LODGroup>();
        LOD[] lods = new LOD[lodRoots.Length];
        for (int i = 0; i < lodRoots.Length; i++)
            lods[i] = new LOD(i == 0 ? 0.6f : (i == 1 ? 0.25f : 0.06f), lodRoots[i].GetComponentsInChildren<Renderer>(true));
        group.fadeMode = LODFadeMode.None;
        group.size = size;
        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static Transform[] FindAllNamed(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        var matches = new List<Transform>();
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == name || all[i].name.StartsWith(name + "_")) matches.Add(all[i]);
        return matches.ToArray();
    }

    private static Transform FindNamed(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == name) return all[i];
        return null;
    }

    private static GameObject Box(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cube, name, position, rotation, scale, material);
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cylinder, name, position, rotation, scale, material);
    }

    private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Sphere, name, position, Quaternion.identity, scale, material);
    }

    private static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        return go;
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.localPosition = position;
        return marker;
    }

    private static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    private static AudioSource Audio(Transform parent, string name, string resourcePath, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>(resourcePath);
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.volume = volume;
        return source;
    }

    private static Material Mat(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + fileName);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.TrimEnd('/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static void ValidatePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Presentation budget missing " + path);
            return;
        }

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        HashSet<Material> materials = new HashSet<Material>();
        int triangles = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            for (int m = 0; m < renderer.sharedMaterials.Length; m++)
                if (renderer.sharedMaterials[m] != null) materials.Add(renderer.sharedMaterials[m]);
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                triangles += filter.sharedMesh.triangles.Length / 3;
        }

        Debug.Log(string.Format("Presentation budget {0}: renderers={1}, materials={2}, triangles={3}, LODGroups={4}",
            path, renderers.Length, materials.Count, triangles, prefab.GetComponentsInChildren<LODGroup>(true).Length));
    }
}