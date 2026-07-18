using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SandRunnersSecondArtPassBuilder
{
    private const string ArtRoot = "Assets/Resources/SandRunners/Models/ArtPass/";
    private const string PresentationRoot = ArtRoot + "Presentation/";
    private const string ScarabRoot = ArtRoot + "ScarabVariants/";

    [MenuItem("SandRunners/Art/Build Second Art Pass")]
    public static void BuildAll()
    {
        EnsureFolder(PresentationRoot);
        EnsureFolder(ScarabRoot);
        BuildPyramid();
        BuildApex();
        BuildScarab("ScarabTank");
        BuildScarab("SalvageScarab");
        BuildScarab("ScarabWalker");
        BuildScarab("ScarabCarrierDrone");
        BuildScarab("ScarabHowitzer");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAll();
    }

    [MenuItem("SandRunners/Art/Validate Second Art Pass")]
    public static void ValidateAll()
    {
        ValidatePrefab(PresentationRoot + "SR_BattlePyramid_ArtPass.prefab");
        ValidatePrefab(PresentationRoot + "SR_Apex_Art.prefab");
        string[] names = { "ScarabTank", "SalvageScarab", "ScarabWalker", "ScarabCarrierDrone", "ScarabHowitzer" };
        for (int i = 0; i < names.Length; i++)
            ValidatePrefab(ScarabRoot + "SR_" + names[i] + "_Art.prefab");
    }

    private static void BuildPyramid()
    {
        Material sandstone = Mat("SR_Art_LightSandstone.mat");
        Material gold = Mat("SR_Art_Gold.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material gunmetal = Mat("SR_Art_Gunmetal.mat");
        Material blackGlass = Mat("SR_Art_DarkMetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");

        GameObject root = NewRoot("SR_BattlePyramid_ArtPass");
        Transform art = NewChild(root.transform, "Pyramid_Art_Modules");
        Transform lod0 = NewChild(art, "Pyramid_LOD0_Detail");
        Transform lod1 = NewChild(art, "Pyramid_LOD1_Silhouette");
        Transform lod2 = NewChild(art, "Pyramid_LOD2_Distance");

        Frustum(lod0, "Pyramid_Solid_Sandstone_Hull", new Vector3(0f, 1.82f, 0f), Quaternion.identity, 6.3f, 5.75f, 0.95f, 0.95f, 3.64f, sandstone);
        Frustum(lod0, "Pyramid_Gold_Command_Crown", new Vector3(0f, 3.62f, 0.08f), Quaternion.identity, 1.15f, 1.15f, 0.35f, 0.35f, 0.48f, gold);
        Box(lod0, "Pyramid_Black_Observation_Glass", new Vector3(0f, 1.62f, -2.96f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(1.8f, 0.58f, 0.08f), blackGlass);
        Box(lod0, "Pyramid_Observation_Brow", new Vector3(0f, 1.95f, -2.9f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(2.2f, 0.13f, 0.18f), brass);
        Box(lod0, "Pyramid_Brass_Door_Left", new Vector3(-0.78f, 0.84f, -3.0f), Quaternion.identity, new Vector3(0.72f, 1.0f, 0.12f), brass);
        Box(lod0, "Pyramid_Brass_Door_Right", new Vector3(0.78f, 0.84f, -3.0f), Quaternion.identity, new Vector3(0.72f, 1.0f, 0.12f), brass);
        Box(lod0, "Pyramid_Brass_Door_Frame", new Vector3(0f, 1.4f, -3.02f), Quaternion.identity, new Vector3(2.25f, 0.14f, 0.16f), brass);
        Box(lod0, "Pyramid_Brass_Keel", new Vector3(0f, 0.42f, -2.88f), Quaternion.identity, new Vector3(3.25f, 0.18f, 0.22f), brass);

        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Box(lod0, "Pyramid_Track_Housing_" + label, new Vector3(side * 2.72f, 0.25f, 0f), Quaternion.identity, new Vector3(0.72f, 0.62f, 5.35f), dark);
            Box(lod0, "Pyramid_Suspension_Armor_" + label, new Vector3(side * 2.7f, 0.57f, 0f), Quaternion.identity, new Vector3(0.84f, 0.16f, 4.9f), gunmetal);
            for (int i = 0; i < 8; i++)
            {
                float z = -2.05f + i * 0.59f;
                Cylinder(lod0, "Pyramid_Track_Wheel_" + label + "_" + i, new Vector3(side * 2.78f, 0.08f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.12f, 0.34f), gunmetal);
                Box(lod0, "Pyramid_Suspension_Arm_" + label + "_" + i, new Vector3(side * 2.7f, 0.08f, z), Quaternion.identity, new Vector3(0.9f, 0.06f, 0.16f), brass);
            }
        }

        Box(lod0, "Pyramid_Left_Hardpoint", new Vector3(-1.42f, 1.68f, 0.34f), Quaternion.Euler(0f, 0f, -16f), new Vector3(0.28f, 0.28f, 0.82f), brass);
        Box(lod0, "Pyramid_Right_Hardpoint", new Vector3(1.42f, 1.68f, 0.34f), Quaternion.Euler(0f, 0f, 16f), new Vector3(0.28f, 0.28f, 0.82f), brass);
        Box(lod0, "Pyramid_Reactor_Vent_Left", new Vector3(-1.1f, 1.35f, -2.48f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.5f, 0.12f, 0.1f), cyan);
        Box(lod0, "Pyramid_Reactor_Vent_Right", new Vector3(1.1f, 1.35f, -2.48f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.5f, 0.12f, 0.1f), cyan);

        Frustum(lod1, "Pyramid_Sandstone_LowHull", new Vector3(0f, 1.78f, 0f), Quaternion.identity, 6.15f, 5.65f, 1.05f, 1.05f, 3.5f, sandstone);
        Box(lod1, "Pyramid_Low_Track_Left", new Vector3(-2.7f, 0.2f, 0f), Quaternion.identity, new Vector3(0.76f, 0.58f, 5.1f), dark);
        Box(lod1, "Pyramid_Low_Track_Right", new Vector3(2.7f, 0.2f, 0f), Quaternion.identity, new Vector3(0.76f, 0.58f, 5.1f), dark);
        Box(lod1, "Pyramid_Low_Glass", new Vector3(0f, 1.58f, -2.86f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(1.9f, 0.42f, 0.08f), blackGlass);

        Frustum(lod2, "Pyramid_Distance_Sandstone", new Vector3(0f, 1.7f, 0f), Quaternion.identity, 6.05f, 5.55f, 1.2f, 1.2f, 3.35f, sandstone);
        Box(lod2, "Pyramid_Distance_Track_Left", new Vector3(-2.66f, 0.2f, 0f), Quaternion.identity, new Vector3(0.82f, 0.5f, 4.9f), dark);
        Box(lod2, "Pyramid_Distance_Track_Right", new Vector3(2.66f, 0.2f, 0f), Quaternion.identity, new Vector3(0.82f, 0.5f, 4.9f), dark);

        Transform anchors = NewChild(art, "Pyramid_Art_Anchors");
        Marker(anchors, "Muzzle_Main", new Vector3(0f, 2.6f, -1.2f));
        Marker(anchors, "Muzzle_Left", new Vector3(-2.95f, 1.0f, -0.8f));
        Marker(anchors, "Muzzle_Right", new Vector3(2.95f, 1.0f, -0.8f));
        Marker(anchors, "Engine_Main", new Vector3(0f, 0.35f, 2.1f));
        Marker(anchors, "Door_Cargo", new Vector3(0f, 0.85f, -3.18f));
        Marker(anchors, "Hardpoint_Main", new Vector3(0f, 2.6f, 0.2f));
        Marker(anchors, "Hardpoint_Left", new Vector3(-1.42f, 1.68f, 0.34f));
        Marker(anchors, "Hardpoint_Right", new Vector3(1.42f, 1.68f, 0.34f));
        Marker(anchors, "Apex_Focus", new Vector3(0f, 2.68f, 0f));

        Light reactor = PointLight(art, "Pyramid_Reactor_Light", new Vector3(0f, 1.35f, -2.5f), new Color(0.08f, 0.35f, 0.5f), 0.5f, 4f);
        Light apex = PointLight(art, "Pyramid_Apex_Light", new Vector3(0f, 2.6f, 0f), new Color(1f, 0.55f, 0.12f), 0.35f, 3f);
        SandRunnersPyramidArtAdapter adapter = root.AddComponent<SandRunnersPyramidArtAdapter>();
        adapter.Configure(FindAllNamed(art, "Pyramid_Track_Wheel"), FindAllNamed(art, "Pyramid_Suspension_Arm"), FindNamed(art, "Pyramid_Brass_Door_Left"), FindNamed(art, "Pyramid_Brass_Door_Right"), reactor, apex);
        AddLod(root, lod0, lod1, lod2, 11f);
        Save(root, PresentationRoot + "SR_BattlePyramid_ArtPass.prefab");
    }

    private static void BuildApex()
    {
        Material gold = Mat("SR_Art_Gold.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");
        Material solar = Mat("SR_Art_SolarBeam.mat");

        GameObject root = NewRoot("SR_Apex_Art");
        Transform art = NewChild(root.transform, "Apex_Art_Modules");
        Transform lod0 = NewChild(art, "Apex_LOD0_Detail");
        Transform lod1 = NewChild(art, "Apex_LOD1_Silhouette");
        Transform lod2 = NewChild(art, "Apex_LOD2_Distance");
        Transform fx = NewChild(art, "Apex_FX");

        Box(lod0, "Apex_Mirror_Base", new Vector3(0f, -0.02f, 0f), Quaternion.identity, new Vector3(2.1f, 0.18f, 2.1f), dark);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.12f, 0.82f);
            Box(lod0, "Apex_Mirror_Facet_" + i, p, Quaternion.Euler(18f, angle, 0f), new Vector3(1.55f, 0.08f, 0.42f), cyan);
            Box(lod0, "Apex_Mirror_Brass_Frame_" + i, p + Vector3.up * 0.08f, Quaternion.Euler(18f, angle, 0f), new Vector3(1.72f, 0.08f, 0.5f), brass);
        }
        Sphere(lod0, "Apex_Focus_Point", new Vector3(0f, 0.34f, 0f), new Vector3(0.28f, 0.28f, 0.28f), solar);
        Light focusLight = PointLight(lod0, "Apex_Focus_Light", new Vector3(0f, 0.34f, 0f), new Color(0.2f, 0.8f, 1f), 1.2f, 3.5f);

        LineRenderer beam = fx.gameObject.AddComponent<LineRenderer>();
        beam.name = "Apex_Normal_Solar_Beam";
        beam.sharedMaterial = solar;
        beam.positionCount = 2;
        beam.useWorldSpace = false;
        beam.SetPosition(0, new Vector3(0f, 4.4f, 0f));
        beam.SetPosition(1, new Vector3(0f, 0.36f, 0f));
        beam.startWidth = 0.06f;
        beam.endWidth = 0.02f;
        beam.enabled = false;

        GameObject flowObject = new GameObject("Apex_Descending_Solar_Charge_Flow");
        flowObject.transform.SetParent(fx, false);
        ParticleSystem flow = flowObject.AddComponent<ParticleSystem>();
        var main = flow.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.75f;
        main.startSpeed = -2.2f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.78f, 0.2f, 0.92f);
        main.maxParticles = 120;
        var emission = flow.emission;
        emission.rateOverTime = 0f;
        var shape = flow.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 3f;
        shape.radius = 0.12f;
        shape.position = new Vector3(0f, 3.8f, 0f);
        var particleRenderer = flowObject.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null) particleRenderer.sharedMaterial = brass;

        Transform anchors = NewChild(art, "Apex_Art_Anchors");
        Marker(anchors, "Apex_Focus_Anchor", new Vector3(0f, 0.34f, 0f));
        Marker(anchors, "Solar_Charge_Origin", new Vector3(0f, 4.4f, 0f));
        Marker(anchors, "Solar_Beam_Target", new Vector3(0f, 0.36f, 0f));
        Marker(anchors, "Apex_Mirror_Left", new Vector3(-0.82f, 0.12f, 0f));
        Marker(anchors, "Apex_Mirror_Right", new Vector3(0.82f, 0.12f, 0f));
        Marker(anchors, "Apex_Fire_Muzzle", new Vector3(0f, 0.42f, 0f));

        Box(lod1, "Apex_Mirror_Silhouette", new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(2.35f, 0.2f, 2.35f), brass);
        Sphere(lod1, "Apex_Focus_Silhouette", new Vector3(0f, 0.34f, 0f), new Vector3(0.34f, 0.34f, 0.34f), solar);
        Box(lod2, "Apex_Distance_Crown", new Vector3(0f, 0.12f, 0f), Quaternion.identity, new Vector3(2.2f, 0.22f, 2.2f), gold);

        AudioSource charge = Audio(lod0, "Apex_Charge_Audio", "SandRunners/Audio/SR_Pyramid_Engine", 0.38f);
        AudioSource fire = Audio(lod0, "Apex_Fire_Audio", "SandRunners/Audio/SR_Howitzer", 0.48f);
        AudioSource recovery = Audio(lod0, "Apex_Recovery_Audio", "SandRunners/Audio/SR_AmbiencePulse", 0.28f);
        SandRunnersApexArtAdapter adapter = root.AddComponent<SandRunnersApexArtAdapter>();
        adapter.Configure(FindNamed(lod0, "Apex_Focus_Point"), focusLight, beam, flow, charge, fire, recovery);
        AddLod(root, lod0, lod1, lod2, 3.5f);
        Save(root, PresentationRoot + "SR_Apex_Art.prefab");
    }

    private static void BuildScarab(string name)
    {
        Material gold = Mat("SR_Art_Gold.mat");
        Material brass = Mat("SR_Art_PaleGold.mat");
        Material dark = Mat("SR_Art_DarkMetal.mat");
        Material gunmetal = Mat("SR_Art_Gunmetal.mat");
        Material cyan = Mat("SR_Art_CyanGlass.mat");
        Material red = Mat("SR_Art_ImperialRed.mat");

        GameObject root = NewRoot("SR_" + name + "_Art");
        SandRunnersScarabArtAdapter adapter = root.AddComponent<SandRunnersScarabArtAdapter>();
        adapter.ConfigureVariant(ToVariant(name));
        Transform art = NewChild(root.transform, "Scarab_Art_Modules");
        Transform lod0 = NewChild(art, "Scarab_LOD0_Detail");
        Transform lod1 = NewChild(art, "Scarab_LOD1_Silhouette");
        Transform lod2 = NewChild(art, "Scarab_LOD2_Distance");

        if (name == "ScarabTank") BuildTank(lod0, lod1, lod2, gold, brass, dark, gunmetal, cyan);
        if (name == "SalvageScarab") BuildSalvage(lod0, lod1, lod2, gold, brass, dark, gunmetal, cyan);
        if (name == "ScarabWalker") BuildWalker(lod0, lod1, lod2, gold, brass, dark, gunmetal, cyan);
        if (name == "ScarabCarrierDrone") BuildCarrier(lod0, lod1, lod2, gold, brass, dark, gunmetal, cyan);
        if (name == "ScarabHowitzer") BuildHowitzer(lod0, lod1, lod2, gold, brass, dark, gunmetal, cyan, red);

        Transform anchors = NewChild(art, "Scarab_Art_Anchors");
        Marker(anchors, "Muzzle_Main", new Vector3(0f, 1.5f, 2.8f));
        Marker(anchors, "Muzzle_Left", new Vector3(-1.3f, 1.2f, 2.1f));
        Marker(anchors, "Muzzle_Right", new Vector3(1.3f, 1.2f, 2.1f));
        Marker(anchors, "Muzzle_Howitzer_Recoil", new Vector3(0f, 2.1f, 3.2f));
        Marker(anchors, "Engine_Main", new Vector3(0f, 0.45f, -1.7f));
        Marker(anchors, "Door_Cargo", new Vector3(0f, 1.4f, -2.1f));
        Marker(anchors, "Hardpoint_Main", new Vector3(0f, 1.35f, 0.65f));
        Marker(anchors, "Hardpoint_Left", new Vector3(-1.4f, 1.2f, 0.1f));
        Marker(anchors, "Hardpoint_Right", new Vector3(1.4f, 1.2f, 0.1f));

        AddLod(root, lod0, lod1, lod2, 6f);
        Save(root, ScarabRoot + "SR_" + name + "_Art.prefab");
    }

    private static void BuildTank(Transform a, Transform b, Transform c, Material gold, Material brass, Material dark, Material gunmetal, Material cyan)
    {
        Box(a, "Tank_Low_Armored_Carapace", new Vector3(0f, 0.85f, -0.2f), Quaternion.identity, new Vector3(3.5f, 0.8f, 4.35f), gold);
        Frustum(a, "Tank_Armored_Front_Wedge", new Vector3(0f, 1.15f, 1.35f), Quaternion.identity, 3.3f, 1.9f, 2.7f, 1.25f, 0.7f, brass);
        for (int side = -1; side <= 1; side += 2)
        {
            string s = side < 0 ? "Left" : "Right";
            Box(a, "Tank_Track_Belt_" + s, new Vector3(side * 2.05f, 0.3f, 0f), Quaternion.identity, new Vector3(0.55f, 0.55f, 4.9f), dark);
            for (int i = 0; i < 7; i++)
                Cylinder(a, "Tank_TrackWheel_" + s + "_" + i, new Vector3(side * 2.11f, 0.25f, -1.85f + i * 0.62f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.31f, 0.1f, 0.31f), gunmetal);
        }
        Box(a, "Tank_Turret_Ring", new Vector3(0f, 1.42f, 0.1f), Quaternion.identity, new Vector3(1.7f, 0.2f, 1.7f), dark);
        Box(a, "Tank_Turret_Armor", new Vector3(0f, 1.7f, 0.28f), Quaternion.identity, new Vector3(1.45f, 0.55f, 1.5f), brass);
        Cylinder(a, "Tank_Main_Cannon", new Vector3(0f, 1.72f, 2.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 1.35f, 0.22f), gunmetal);
        Sphere(a, "Tank_Command_Optic", new Vector3(0f, 2.08f, -0.25f), new Vector3(0.25f, 0.18f, 0.25f), cyan);
        Box(b, "Tank_LOD1_LowHull", new Vector3(0f, 0.8f, 0f), Quaternion.identity, new Vector3(4.35f, 1.1f, 4.8f), gold);
        Box(b, "Tank_LOD1_Turret", new Vector3(0f, 1.55f, 0.2f), Quaternion.identity, new Vector3(1.7f, 0.6f, 1.8f), brass);
        Box(c, "Tank_LOD2_Treaded_Silhouette", new Vector3(0f, 0.85f, 0f), Quaternion.identity, new Vector3(4.55f, 1.35f, 5f), gold);
    }

    private static void BuildSalvage(Transform a, Transform b, Transform c, Material gold, Material brass, Material dark, Material gunmetal, Material cyan)
    {
        Frustum(a, "Salvage_Cargo_Body", new Vector3(0f, 1.15f, -0.2f), Quaternion.identity, 3.8f, 4.2f, 2.8f, 3.3f, 1.9f, gold);
        Box(a, "Salvage_Heavy_Deck", new Vector3(0f, 0.45f, 0.1f), Quaternion.identity, new Vector3(4.4f, 0.45f, 4.7f), dark);
        Box(a, "Salvage_Cargo_Empty_Frame", new Vector3(0f, 2.0f, -0.25f), Quaternion.identity, new Vector3(2.8f, 0.22f, 2.3f), brass);
        Box(a, "Salvage_Cargo_Loaded", new Vector3(0f, 2.35f, -0.25f), Quaternion.identity, new Vector3(2.3f, 0.55f, 1.9f), brass);
        Box(a, "Salvage_Crane_Mast", new Vector3(0f, 1.6f, 1.5f), Quaternion.identity, new Vector3(0.35f, 2.4f, 0.35f), gunmetal);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 1.7f, 0.75f, Mathf.Sin(angle) * 1.7f);
            Transform arm = NewChild(a, "Salvage_Arm_" + i);
            Box(arm, "Salvage_Arm_Upper", p + Vector3.up * 0.55f, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 18f), new Vector3(0.22f, 0.85f, 0.22f), gunmetal);
            Box(arm, "Salvage_Arm_Forearm", p + Vector3.up * 0.02f + new Vector3(Mathf.Cos(angle) * 0.35f, 0f, Mathf.Sin(angle) * 0.35f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, -22f), new Vector3(0.18f, 0.7f, 0.18f), brass);
            Box(arm, "Salvage_Arm_Claw", p + new Vector3(Mathf.Cos(angle) * 0.68f, -0.52f, Mathf.Sin(angle) * 0.68f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Vector3(0.5f, 0.14f, 0.22f), cyan);
        }
        Box(b, "Salvage_LOD1_CargoBody", new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(4.15f, 1.45f, 4.5f), gold);
        Box(b, "Salvage_LOD1_Crane", new Vector3(0f, 1.6f, 1.3f), Quaternion.identity, new Vector3(0.8f, 2.2f, 0.8f), gunmetal);
        Box(c, "Salvage_LOD2_LoadCarrier", new Vector3(0f, 1.15f, 0f), Quaternion.identity, new Vector3(4.4f, 1.7f, 4.8f), gold);
    }

    private static void BuildWalker(Transform a, Transform b, Transform c, Material gold, Material brass, Material dark, Material gunmetal, Material cyan)
    {
        Frustum(a, "Walker_High_Central_Carapace", new Vector3(0f, 2.65f, 0f), Quaternion.identity, 3.25f, 3.65f, 2.35f, 2.8f, 2.35f, gold);
        Sphere(a, "Walker_Chest_Core", new Vector3(0f, 2.35f, 0.7f), new Vector3(0.42f, 0.42f, 0.42f), cyan);
        Cylinder(a, "Walker_Main_Cannon", new Vector3(0f, 2.72f, 2.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.18f, 1.1f, 0.18f), gunmetal);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 1.55f, 1.55f, Mathf.Sin(angle) * 1.55f);
            Transform leg = NewChild(a, "Walker_Leg_" + i);
            Box(leg, "Walker_Leg_Upper", p + Vector3.up * 0.62f, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 12f), new Vector3(0.28f, 1.1f, 0.28f), dark);
            Box(leg, "Walker_Leg_Knee", p + Vector3.down * 0.32f, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, -18f), new Vector3(0.38f, 0.3f, 0.42f), brass);
            Box(leg, "Walker_Leg_Foot", p + new Vector3(Mathf.Cos(angle) * 0.25f, -1.05f, Mathf.Sin(angle) * 0.25f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f), new Vector3(0.7f, 0.18f, 0.85f), gunmetal);
        }
        Box(b, "Walker_LOD1_HighBody", new Vector3(0f, 1.8f, 0f), Quaternion.identity, new Vector3(3.5f, 2.4f, 3.8f), gold);
        Box(b, "Walker_LOD1_Legs", new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(4.0f, 1.5f, 4.2f), dark);
        Box(c, "Walker_LOD2_TallSilhouette", new Vector3(0f, 1.8f, 0f), Quaternion.identity, new Vector3(3.8f, 3.8f, 4f), gold);
    }

    private static void BuildCarrier(Transform a, Transform b, Transform c, Material gold, Material brass, Material dark, Material gunmetal, Material cyan)
    {
        Frustum(a, "Carrier_Airborne_Core", new Vector3(0f, 1.9f, 0f), Quaternion.identity, 2.9f, 4.1f, 1.9f, 2.8f, 1.6f, gold);
        Box(a, "Carrier_Underbelly_Cargo", new Vector3(0f, 1.35f, -0.55f), Quaternion.identity, new Vector3(2.0f, 0.46f, 1.8f), brass);
        Box(a, "Carrier_Command_Canopy", new Vector3(0f, 2.55f, 0.45f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(1.5f, 0.25f, 0.95f), cyan);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(a, "Carrier_Wing_" + (side < 0 ? "Left" : "Right"), new Vector3(side * 2.0f, 1.95f, 0.1f), Quaternion.Euler(0f, side * 8f, side * 12f), new Vector3(2.1f, 0.12f, 1.2f), brass);
            for (int i = 0; i < 2; i++)
            {
                float z = -1.2f + i * 2.4f;
                Transform rotor = NewChild(a, "Carrier_Rotor_" + (side < 0 ? "Left" : "Right") + "_" + i);
                Cylinder(rotor, "Carrier_Rotor_Hub", new Vector3(side * 2.25f, 1.9f, z), Quaternion.identity, new Vector3(0.26f, 0.16f, 0.26f), gunmetal);
                Box(rotor, "Carrier_Rotor_Blade_A", new Vector3(side * 2.25f, 2.12f, z), Quaternion.identity, new Vector3(1.25f, 0.06f, 0.12f), cyan);
                Box(rotor, "Carrier_Rotor_Blade_B", new Vector3(side * 2.25f, 2.12f, z), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.25f, 0.06f, 0.12f), cyan);
            }
        }
        Box(a, "Carrier_Forward_Lance", new Vector3(0f, 1.92f, 2.25f), Quaternion.identity, new Vector3(0.2f, 0.2f, 1.1f), gunmetal);
        Box(b, "Carrier_LOD1_WingedAirframe", new Vector3(0f, 1.85f, 0f), Quaternion.identity, new Vector3(6.2f, 0.8f, 4.5f), gold);
        Box(b, "Carrier_LOD1_Cargo", new Vector3(0f, 1.35f, -0.5f), Quaternion.identity, new Vector3(2.4f, 0.5f, 2f), brass);
        Box(c, "Carrier_LOD2_AirSilhouette", new Vector3(0f, 1.9f, 0f), Quaternion.identity, new Vector3(6.6f, 1.1f, 4.9f), gold);
    }

    private static void BuildHowitzer(Transform a, Transform b, Transform c, Material gold, Material brass, Material dark, Material gunmetal, Material cyan, Material red)
    {
        Frustum(a, "Howitzer_Cursed_Siege_Casemate", new Vector3(0f, 1.15f, -0.1f), Quaternion.identity, 4.5f, 4.9f, 3.3f, 3.7f, 2.3f, dark);
        Box(a, "Howitzer_Cursed_Ribbed_Mantlet", new Vector3(0f, 2.15f, 0.75f), Quaternion.identity, new Vector3(2.7f, 1.0f, 1.5f), red);
        Transform recoil = NewChild(a, "Howitzer_Recoil_Slide");
        Box(recoil, "Howitzer_Heavy_Barrel_Mantlet", new Vector3(0f, 2.18f, 1.6f), Quaternion.identity, new Vector3(1.5f, 0.8f, 1.1f), gunmetal);
        Cylinder(recoil, "Howitzer_Heavy_Barrel", new Vector3(0f, 2.18f, 3.25f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.38f, 1.55f, 0.38f), dark);
        Cylinder(recoil, "Howitzer_Muzzle_Crown", new Vector3(0f, 2.18f, 4.72f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.65f, 0.32f, 0.65f), red);
        Box(a, "Howitzer_Cursed_Eye", new Vector3(0f, 1.75f, -2.32f), Quaternion.identity, new Vector3(0.75f, 0.22f, 0.28f), red);
        for (int side = -1; side <= 1; side += 2)
        {
            string s = side < 0 ? "Left" : "Right";
            Box(a, "Howitzer_Track_Belt_" + s, new Vector3(side * 2.35f, 0.32f, 0f), Quaternion.identity, new Vector3(0.6f, 0.6f, 4.8f), dark);
            for (int i = 0; i < 6; i++)
                Cylinder(a, "Howitzer_TrackWheel_" + s + "_" + i, new Vector3(side * 2.43f, 0.25f, -1.7f + i * 0.68f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.34f, 0.1f, 0.34f), gunmetal);
        }
        Box(b, "Howitzer_LOD1_CursedBody", new Vector3(0f, 1.25f, 0f), Quaternion.identity, new Vector3(4.8f, 2.4f, 5.1f), dark);
        Box(b, "Howitzer_LOD1_LongBarrel", new Vector3(0f, 2.1f, 2.5f), Quaternion.identity, new Vector3(0.7f, 0.7f, 3.6f), gunmetal);
        Box(c, "Howitzer_LOD2_SiegeSilhouette", new Vector3(0f, 1.35f, 0.2f), Quaternion.identity, new Vector3(5.1f, 2.8f, 5.4f), dark);
    }

    private static SandRunnersScarabArtAdapter.Variant ToVariant(string name)
    {
        if (name == "SalvageScarab") return SandRunnersScarabArtAdapter.Variant.Salvage;
        if (name == "ScarabWalker") return SandRunnersScarabArtAdapter.Variant.Walker;
        if (name == "ScarabCarrierDrone") return SandRunnersScarabArtAdapter.Variant.Carrier;
        if (name == "ScarabHowitzer") return SandRunnersScarabArtAdapter.Variant.Howitzer;
        return SandRunnersScarabArtAdapter.Variant.Tank;
    }

    private static GameObject NewRoot(string name)
    {
        string path = (name.Contains("Pyramid") || name.Contains("Apex")) ? PresentationRoot + name + ".prefab" : ScarabRoot + name + ".prefab";
        Delete(path);
        return new GameObject(name);
    }

    private static void Save(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        if (prefab != null)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                    AssetDatabase.AddObjectToAsset(mesh, prefab);
            }
            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        Object.DestroyImmediate(root);
    }

    private static Material Mat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + name);
    }

    private static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static GameObject Box(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cube, name, pos, rot, scale, material);
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cylinder, name, pos, rot, scale, material);
    }

    private static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Sphere, name, pos, Quaternion.identity, scale, material);
    }

    private static GameObject Frustum(Transform parent, string name, Vector3 pos, Quaternion rot, float bottomWidth, float bottomDepth, float topWidth, float topDepth, float height, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh { name = name + "_Mesh" };
        float by = height * -0.5f;
        float ty = height * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-bottomWidth * 0.5f, by, -bottomDepth * 0.5f),
            new Vector3(bottomWidth * 0.5f, by, -bottomDepth * 0.5f),
            new Vector3(-bottomWidth * 0.5f, by, bottomDepth * 0.5f),
            new Vector3(bottomWidth * 0.5f, by, bottomDepth * 0.5f),
            new Vector3(-topWidth * 0.5f, ty, -topDepth * 0.5f),
            new Vector3(topWidth * 0.5f, ty, -topDepth * 0.5f),
            new Vector3(-topWidth * 0.5f, ty, topDepth * 0.5f),
            new Vector3(topWidth * 0.5f, ty, topDepth * 0.5f)
        };
        mesh.triangles = new[]
        {
            0,4,6, 0,6,2, 1,3,7, 1,7,5,
            0,1,5, 0,5,4, 2,6,7, 2,7,3,
            4,5,7, 4,7,6, 0,2,3, 0,3,1
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        return go;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Marker(Transform parent, string name, Vector3 position)
    {
        Transform marker = NewChild(parent, name);
        marker.localPosition = position;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == name) return all[i];
        return null;
    }

    private static Transform[] FindAllNamed(Transform root, string token)
    {
        List<Transform> result = new List<Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name.Contains(token)) result.Add(all[i]);
        return result.ToArray();
    }

    private static void AddLod(GameObject root, Transform lod0, Transform lod1, Transform lod2, float size)
    {
        LODGroup group = root.AddComponent<LODGroup>();
        Renderer[] r0 = lod0.GetComponentsInChildren<Renderer>(true);
        Renderer[] r1 = lod1.GetComponentsInChildren<Renderer>(true);
        Renderer[] r2 = lod2.GetComponentsInChildren<Renderer>(true);
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = false;
        group.size = size;
        group.SetLODs(new[] { new LOD(0.62f, r0), new LOD(0.25f, r1), new LOD(0.015f, r2) });
        group.RecalculateBounds();
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
        light.shadows = LightShadows.None;
        return light;
    }

    private static AudioSource Audio(Transform parent, string name, string clipPath, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.volume = volume;
        source.clip = Resources.Load<AudioClip>(clipPath);
        return source;
    }

    private static void EnsureFolder(string path)
    {
        string clean = path.TrimEnd('/');
        string[] parts = clean.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void Delete(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static void ValidatePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("SECOND_ART_MISSING " + path);
            return;
        }
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        HashSet<Material> materials = new HashSet<Material>();
        int triangles = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            Material[] shared = r.sharedMaterials;
            for (int j = 0; j < shared.Length; j++)
                if (shared[j] != null) materials.Add(shared[j]);
            MeshFilter f = r.GetComponent<MeshFilter>();
            if (f != null && f.sharedMesh != null) triangles += f.sharedMesh.triangles.Length / 3;
        }
        Debug.Log("SECOND_ART_BUDGET " + path + " renderers=" + renderers.Length + " materials=" + materials.Count + " triangles=" + triangles + " lodGroups=" + prefab.GetComponentsInChildren<LODGroup>(true).Length);
    }
}