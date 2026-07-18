using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SandRunnersScarabArtBuilder
{
    private const string BasePrefabPath = "Assets/Resources/SandRunners/Models/ArtPass/SR_GoldenScarab_Authored.prefab";
    private const string OutputFolder = "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants";
    private const string MaterialRoot = "Assets/Resources/SandRunners/Models/ArtPass/";
    private static readonly string[] VariantNames = { "ScarabTank", "SalvageScarab", "ScarabWalker", "ScarabCarrierDrone", "ScarabHowitzer" };

    [MenuItem("SandRunners/Art/Build Scarab Art Pass")]
    public static void BuildAll()
    {
        EnsureFolder(OutputFolder);
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError("Scarab art pass: missing base prefab " + BasePrefabPath);
            return;
        }

        for (int i = 0; i < VariantNames.Length; i++)
            BuildVariant(basePrefab, VariantNames[i]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAll();
    }

    [MenuItem("SandRunners/Art/Validate Scarab Art Pass")]
    public static void ValidateAll()
    {
        for (int i = 0; i < VariantNames.Length; i++)
        {
            string path = OutputFolder + "/SR_" + VariantNames[i] + "_Art.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("Scarab art pass: missing " + path);
                continue;
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            HashSet<Material> materials = new HashSet<Material>();
            int triangles = 0;
            int lodGroups = prefab.GetComponentsInChildren<LODGroup>(true).Length;
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null) continue;
                Material[] shared = renderer.sharedMaterials;
                for (int m = 0; m < shared.Length; m++)
                    if (shared[m] != null) materials.Add(shared[m]);
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    triangles += filter.sharedMesh.triangles.Length / 3;
            }
            Debug.Log(string.Format("Scarab budget {0}: renderers={1}, materials={2}, triangles={3}, LODGroups={4}", path, renderers.Length, materials.Count, triangles, lodGroups));
        }
    }

    private static void BuildVariant(GameObject basePrefab, string variantName)
    {
        string prefabPath = OutputFolder + "/SR_" + variantName + "_Art.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = "SR_" + variantName + "_Art";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SandRunnersScarabArtAdapter adapter = instance.GetComponent<SandRunnersScarabArtAdapter>();
        if (adapter == null)
            adapter = instance.AddComponent<SandRunnersScarabArtAdapter>();
        adapter.ConfigureVariant(ToVariant(variantName));

        Transform artRoot = NewChild(instance.transform, "Scarab_Art_Modules");
        Transform lod0 = NewChild(artRoot, "Scarab_LOD0_Detail");
        Transform lod1 = NewChild(artRoot, "Scarab_LOD1_Silhouette");
        Transform lod2 = NewChild(artRoot, "Scarab_LOD2_Distance");

        Material gold = LoadMaterial("SR_Art_Gold.mat");
        Material paleGold = LoadMaterial("SR_Art_PaleGold.mat");
        Material dark = LoadMaterial("SR_Art_DarkMetal.mat");
        Material gunmetal = LoadMaterial("SR_Art_Gunmetal.mat");
        Material cyan = LoadMaterial("SR_Art_CyanGlass.mat");
        Material red = LoadMaterial("SR_Art_ImperialRed.mat");

        if (variantName == "ScarabTank") BuildTank(lod0, lod1, lod2, gold, paleGold, dark, gunmetal, cyan);
        if (variantName == "SalvageScarab") BuildSalvage(lod0, lod1, lod2, gold, paleGold, dark, gunmetal, cyan);
        if (variantName == "ScarabWalker") BuildWalker(lod0, lod1, lod2, gold, paleGold, dark, gunmetal, cyan);
        if (variantName == "ScarabCarrierDrone") BuildCarrier(lod0, lod1, lod2, gold, paleGold, dark, gunmetal, cyan);
        if (variantName == "ScarabHowitzer") BuildHowitzer(lod0, lod1, lod2, gold, paleGold, dark, gunmetal, cyan, red);

        HideBaseRendererForRole(instance, artRoot, variantName);
        CreateCommonAnchors(artRoot);
        CreateLodGroup(instance, lod0, lod1, lod2);
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void BuildTank(Transform lod0, Transform lod1, Transform lod2, Material gold, Material paleGold, Material dark, Material gunmetal, Material cyan)
    {
        Box(lod0, "Tank_Armored_Track_Housing_Left", new Vector3(-2.05f, 0.22f, 0f), Vector3.one, new Vector3(0.58f, 0.46f, 4.85f), dark);
        Box(lod0, "Tank_Armored_Track_Housing_Right", new Vector3(2.05f, 0.22f, 0f), Vector3.one, new Vector3(0.58f, 0.46f, 4.85f), dark);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            for (int i = 0; i < 6; i++)
                Cylinder(lod0, "Tank_TrackWheel_" + label + "_" + i, new Vector3(side * 2.16f, 0.18f, -1.8f + i * 0.72f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.08f, 0.32f), gunmetal);
        }
        Box(lod0, "Tank_Wedge_Carapace", new Vector3(0f, 0.92f, -0.05f), Vector3.one, new Vector3(3.55f, 0.58f, 3.65f), gold);
        Box(lod0, "Tank_Turret_Mantlet", new Vector3(0f, 1.42f, 0.68f), Vector3.one, new Vector3(1.35f, 0.48f, 1.38f), paleGold);
        Cylinder(lod0, "Tank_Main_Cannon", new Vector3(0f, 1.44f, 2.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.2f, 1.15f, 0.2f), gunmetal);
        Box(lod0, "Tank_Command_Optic", new Vector3(0f, 1.84f, -0.18f), Vector3.one, new Vector3(0.52f, 0.2f, 0.55f), cyan);
        Box(lod1, "Tank_LOD1_TrackBody", new Vector3(0f, 0.55f, 0f), Vector3.one, new Vector3(4.45f, 0.85f, 4.9f), dark);
        Box(lod1, "Tank_LOD1_Turret", new Vector3(0f, 1.25f, 0.72f), Vector3.one, new Vector3(1.4f, 0.5f, 1.65f), gold);
        Box(lod2, "Tank_LOD2_Silhouette", new Vector3(0f, 0.82f, 0f), Vector3.one, new Vector3(4.35f, 1.25f, 4.8f), gold);
    }

    private static void BuildSalvage(Transform lod0, Transform lod1, Transform lod2, Material gold, Material paleGold, Material dark, Material gunmetal, Material cyan)
    {
        Box(lod0, "Salvage_Cargo_Hull", new Vector3(0f, 0.92f, -0.35f), Vector3.one, new Vector3(3.85f, 1.15f, 4.25f), gold);
        Box(lod0, "Salvage_Heavy_Undercarriage", new Vector3(0f, 0.28f, 0f), Vector3.one, new Vector3(4.45f, 0.42f, 4.85f), dark);
        Box(lod0, "Salvage_Cargo_Module", new Vector3(0f, 1.75f, -0.65f), Vector3.one, new Vector3(2.65f, 0.72f, 2.1f), paleGold);
        Box(lod0, "Salvage_Cargo_Ribs", new Vector3(0f, 2.2f, -0.65f), Vector3.one, new Vector3(2.85f, 0.12f, 2.25f), gunmetal);
        Box(lod0, "Salvage_Crane_Mast", new Vector3(0f, 2.15f, 1.5f), Vector3.one, new Vector3(0.3f, 2.0f, 0.3f), gunmetal);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Transform arm = NewChild(lod0, "Salvage_Arm_" + label);
            Box(arm, "Salvage_Arm_Upper", new Vector3(side * 0.9f, 1.4f, 1.72f), Quaternion.Euler(0f, side * 18f, side * 18f), new Vector3(0.26f, 0.26f, 1.45f), gunmetal);
            Box(arm, "Salvage_Arm_Forearm", new Vector3(side * 1.28f, 1.0f, 2.55f), Quaternion.Euler(0f, side * 8f, side * 32f), new Vector3(0.22f, 0.22f, 1.25f), paleGold);
            Box(arm, "Salvage_Arm_Claw", new Vector3(side * 1.35f, 0.64f, 3.28f), Quaternion.Euler(0f, side * 12f, side * 42f), new Vector3(0.52f, 0.16f, 0.3f), cyan);
        }
        Box(lod1, "Salvage_LOD1_CargoBody", new Vector3(0f, 0.92f, -0.15f), Vector3.one, new Vector3(4.0f, 1.25f, 4.35f), gold);
        Box(lod1, "Salvage_LOD1_Crane", new Vector3(0f, 1.8f, 1.4f), Vector3.one, new Vector3(0.9f, 1.8f, 1.1f), gunmetal);
        Box(lod2, "Salvage_LOD2_Silhouette", new Vector3(0f, 0.95f, -0.1f), Vector3.one, new Vector3(4.25f, 1.55f, 4.6f), gold);
    }

    private static void BuildWalker(Transform lod0, Transform lod1, Transform lod2, Material gold, Material paleGold, Material dark, Material gunmetal, Material cyan)
    {
        Box(lod0, "Walker_High_Carapace", new Vector3(0f, 2.55f, 0f), Vector3.one, new Vector3(3.25f, 1.25f, 3.55f), gold);
        Box(lod0, "Walker_Central_Reactor", new Vector3(0f, 2.0f, 0.45f), Vector3.one, new Vector3(1.1f, 0.52f, 1.25f), cyan);
        Cylinder(lod0, "Walker_Main_Cannon", new Vector3(0f, 2.55f, 2.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.18f, 0.95f, 0.18f), gunmetal);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            for (int i = 0; i < 3; i++)
            {
                float z = -1.25f + i * 1.25f;
                Transform leg = NewChild(lod0, "Walker_Leg_" + label + "_" + i);
                Box(leg, "Walker_Leg_Upper", new Vector3(side * 1.52f, 1.3f, z), Quaternion.Euler(0f, side * 10f, side * 14f), new Vector3(0.32f, 1.35f, 0.32f), dark);
                Box(leg, "Walker_Leg_Knee", new Vector3(side * 1.78f, 0.52f, z + side * 0.18f), Quaternion.Euler(0f, side * 12f, side * 8f), new Vector3(0.38f, 0.3f, 0.42f), paleGold);
                Box(leg, "Walker_Leg_Foot", new Vector3(side * 1.92f, 0.17f, z + side * 0.28f), Quaternion.Euler(0f, side * 8f, 0f), new Vector3(0.65f, 0.18f, 0.85f), gunmetal);
            }
        }
        Box(lod1, "Walker_LOD1_HighBody", new Vector3(0f, 1.75f, 0f), Vector3.one, new Vector3(3.4f, 2.2f, 3.7f), gold);
        Box(lod2, "Walker_LOD2_Silhouette", new Vector3(0f, 1.7f, 0f), Vector3.one, new Vector3(3.65f, 3.3f, 3.95f), gold);
    }

    private static void BuildCarrier(Transform lod0, Transform lod1, Transform lod2, Material gold, Material paleGold, Material dark, Material gunmetal, Material cyan)
    {
        Box(lod0, "Carrier_Airborne_Core", new Vector3(0f, 1.85f, 0f), Vector3.one, new Vector3(2.5f, 0.7f, 3.8f), gold);
        Box(lod0, "Carrier_Cargo_Pod", new Vector3(0f, 1.62f, -0.9f), Vector3.one, new Vector3(1.8f, 0.5f, 1.35f), paleGold);
        Box(lod0, "Carrier_Command_Canopy", new Vector3(0f, 2.35f, 0.55f), Vector3.one, new Vector3(1.4f, 0.34f, 0.9f), cyan);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            for (int i = 0; i < 2; i++)
            {
                float z = -1.15f + i * 2.3f;
                Transform rotor = NewChild(lod0, "Carrier_Rotor_" + label + "_" + i);
                Cylinder(rotor, "Carrier_Rotor_Hub", new Vector3(side * 2.0f, 1.75f, z), Quaternion.identity, new Vector3(0.3f, 0.18f, 0.3f), gunmetal);
                Box(rotor, "Carrier_Rotor_Blade_A", new Vector3(side * 2.0f, 1.95f, z), Vector3.one, new Vector3(1.5f, 0.06f, 0.16f), paleGold);
                Box(rotor, "Carrier_Rotor_Blade_B", new Vector3(side * 2.0f, 1.95f, z), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.5f, 0.06f, 0.16f), paleGold);
            }
        }
        Box(lod0, "Carrier_Forward_Lance", new Vector3(0f, 1.85f, 2.2f), Vector3.one, new Vector3(0.24f, 0.24f, 1.1f), gunmetal);
        Box(lod1, "Carrier_LOD1_Airframe", new Vector3(0f, 1.8f, 0f), Vector3.one, new Vector3(4.8f, 0.8f, 4.2f), gold);
        Box(lod1, "Carrier_LOD1_Pods", new Vector3(0f, 1.65f, -0.8f), Vector3.one, new Vector3(3.7f, 0.4f, 1.45f), paleGold);
        Box(lod2, "Carrier_LOD2_Silhouette", new Vector3(0f, 1.85f, 0f), Vector3.one, new Vector3(5.2f, 1.0f, 4.4f), gold);
    }

    private static void BuildHowitzer(Transform lod0, Transform lod1, Transform lod2, Material gold, Material paleGold, Material dark, Material gunmetal, Material cyan, Material red)
    {
        Box(lod0, "Howitzer_Cursed_Armored_Hull", new Vector3(0f, 0.9f, -0.1f), Vector3.one, new Vector3(4.55f, 1.35f, 4.75f), dark);
        Box(lod0, "Howitzer_Cursed_Casemate", new Vector3(0f, 1.85f, 0.2f), Vector3.one, new Vector3(2.9f, 1.25f, 2.6f), red);
        Box(lod0, "Howitzer_Cursed_Spinal_Rib", new Vector3(0f, 2.6f, 0.3f), Vector3.one, new Vector3(0.42f, 0.32f, 3.1f), dark);
        Transform recoil = NewChild(lod0, "Howitzer_Recoil_Slide");
        Box(recoil, "Howitzer_Barrel_Mantlet", new Vector3(0f, 2.1f, 1.55f), Vector3.one, new Vector3(1.45f, 0.78f, 1.0f), gunmetal);
        Cylinder(recoil, "Howitzer_Heavy_Barrel", new Vector3(0f, 2.12f, 3.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.38f, 1.45f, 0.38f), dark);
        Cylinder(recoil, "Howitzer_Muzzle_Crown", new Vector3(0f, 2.12f, 4.35f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.62f, 0.3f, 0.62f), red);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Box(lod0, "Howitzer_Stabilizer_" + label, new Vector3(side * 2.55f, 0.28f, 0.75f), Vector3.one, new Vector3(0.35f, 0.2f, 2.1f), dark);
            for (int i = 0; i < 5; i++)
                Cylinder(lod0, "Howitzer_TrackWheel_" + label + "_" + i, new Vector3(side * 2.35f, 0.35f, -1.7f + i * 0.8f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.36f, 0.1f, 0.36f), gunmetal);
        }
        Box(lod0, "Howitzer_Cursed_Eye", new Vector3(0f, 2.65f, -1.15f), Vector3.one, new Vector3(0.6f, 0.22f, 0.3f), red);
        Box(lod1, "Howitzer_LOD1_Casemate", new Vector3(0f, 1.3f, 0f), Vector3.one, new Vector3(4.5f, 2.1f, 4.8f), dark);
        Box(lod1, "Howitzer_LOD1_Barrel", new Vector3(0f, 2.1f, 2.2f), Vector3.one, new Vector3(0.6f, 0.65f, 3.2f), gunmetal);
        Box(lod2, "Howitzer_LOD2_Silhouette", new Vector3(0f, 1.35f, 0.25f), Vector3.one, new Vector3(4.9f, 2.4f, 5.1f), dark);
    }

    private static SandRunnersScarabArtAdapter.Variant ToVariant(string variantName)
    {
        if (variantName == "SalvageScarab") return SandRunnersScarabArtAdapter.Variant.Salvage;
        if (variantName == "ScarabWalker") return SandRunnersScarabArtAdapter.Variant.Walker;
        if (variantName == "ScarabCarrierDrone") return SandRunnersScarabArtAdapter.Variant.Carrier;
        if (variantName == "ScarabHowitzer") return SandRunnersScarabArtAdapter.Variant.Howitzer;
        return SandRunnersScarabArtAdapter.Variant.Tank;
    }

    private static void HideBaseRendererForRole(GameObject instance, Transform authoredModuleRoot, string variantName)
    {
        if (variantName == "SalvageScarab")
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && !renderer.transform.IsChildOf(authoredModuleRoot))
                renderer.enabled = false;
        }
    }

    private static void CreateCommonAnchors(Transform root)
    {
        Transform anchors = NewChild(root, "Scarab_Art_Anchors");
        Marker(anchors, "Muzzle_Main", new Vector3(0f, 1.6f, 2.9f));
        Marker(anchors, "Muzzle_Left", new Vector3(-1.3f, 1.3f, 2.2f));
        Marker(anchors, "Muzzle_Right", new Vector3(1.3f, 1.3f, 2.2f));
        Marker(anchors, "Muzzle_Howitzer_Recoil", new Vector3(0f, 2.1f, 3.0f));
        Marker(anchors, "Engine_Main", new Vector3(0f, 0.52f, -1.55f));
        Marker(anchors, "Door_Cargo", new Vector3(0f, 1.45f, -2.1f));
        Marker(anchors, "Hardpoint_Main", new Vector3(0f, 1.3f, 0.65f));
        Marker(anchors, "Hardpoint_Left", new Vector3(-1.35f, 1.2f, 0.1f));
        Marker(anchors, "Hardpoint_Right", new Vector3(1.35f, 1.2f, 0.1f));
    }

    private static void CreateLodGroup(GameObject root, Transform lod0, Transform lod1, Transform lod2)
    {
        LODGroup group = root.GetComponent<LODGroup>();
        if (group == null) group = root.AddComponent<LODGroup>();
        Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
        List<Renderer> detail = new List<Renderer>();
        for (int i = 0; i < all.Length; i++)
        {
            Renderer renderer = all[i];
            if (renderer == null) continue;
            if (!renderer.transform.IsChildOf(lod1) && !renderer.transform.IsChildOf(lod2))
                detail.Add(renderer);
        }
        Renderer[] medium = lod1.GetComponentsInChildren<Renderer>(true);
        Renderer[] distance = lod2.GetComponentsInChildren<Renderer>(true);
        group.SetLODs(new[]
        {
            new LOD(0.62f, detail.ToArray()),
            new LOD(0.28f, medium),
            new LOD(0.08f, distance)
        });
        group.RecalculateBounds();
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

    private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cube, name, position, Quaternion.Euler(rotation), scale, material);
    }

    private static GameObject Box(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cube, name, position, rotation, scale, material);
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        return Primitive(parent, PrimitiveType.Cylinder, name, position, rotation, scale, material);
    }

    private static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static Material LoadMaterial(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + fileName);
    }

    private static void EnsureFolder(string path)
    {
        string[] pieces = path.Split('/');
        string current = pieces[0];
        for (int i = 1; i < pieces.Length; i++)
        {
            string next = current + "/" + pieces[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, pieces[i]);
            current = next;
        }
    }
}