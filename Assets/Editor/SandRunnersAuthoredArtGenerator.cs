using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SandRunnersAuthoredArtGenerator
{
    private const string Root = "Assets/Resources/SandRunners/Models/ArtPass";
    private const int Gold = 0;
    private const int Dark = 1;
    private const int Glow = 2;
    private const int Trim = 3;
    private const int Accent = 4;

    private static readonly string[] PrefabNames =
    {
        "SR_BattlePyramid_Authored.prefab",
        "SR_GoldenScarab_Authored.prefab",
        "SR_ImperialAssault_Authored.prefab",
        "SR_MandarinkaPalace_Authored.prefab"
    };

    [InitializeOnLoadMethod]
    private static void QueueGeneration()
    {
        EditorApplication.delayCall += GenerateIfMissing;
    }

    [MenuItem("SandRunners/Art Pass/Regenerate Authored Models")]
    public static void GenerateAll()
    {
        EnsureFolder(Root);
        Texture2D armour = GetArmourTexture();

        Material gold = GetMaterial("SR_Art_Gold", new Color(0.63f, 0.44f, 0.13f), 0.86f, 0.24f, armour);
        Material dark = GetMaterial("SR_Art_DarkMetal", new Color(0.025f, 0.032f, 0.04f), 0.88f, 0.2f, armour);
        Material cyan = GetMaterial("SR_Art_CyanGlass", new Color(0.015f, 0.2f, 0.26f), 0.42f, 0.64f, null, new Color(0.02f, 1.25f, 1.7f));
        Material red = GetMaterial("SR_Art_ImperialRed", new Color(0.38f, 0.028f, 0.018f), 0.72f, 0.2f, armour);
        Material jade = GetMaterial("SR_Art_Jade", new Color(0.012f, 0.2f, 0.095f), 0.48f, 0.42f, null, new Color(0.01f, 0.52f, 0.2f));
        Material ivory = GetMaterial("SR_Art_Ivory", new Color(0.56f, 0.49f, 0.35f), 0.38f, 0.22f, armour);
        Material trim = GetMaterial("SR_Art_PaleGold", new Color(0.82f, 0.62f, 0.24f), 0.9f, 0.35f, armour);
        Material gun = GetMaterial("SR_Art_Gunmetal", new Color(0.1f, 0.115f, 0.125f), 0.92f, 0.3f, armour);

        CreateBattlePyramid(gold, dark, cyan, trim, gun);
        CreateGoldenScarab(gold, dark, cyan, trim, gun);
        CreateImperialAssault(red, dark, jade, trim, gun);
        CreateMandarinkaPalace(red, dark, jade, trim, ivory);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SandRunners ArtPass] Authored model pass v2 generated.");
    }

    private static void GenerateIfMissing()
    {
        for (int i = 0; i < PrefabNames.Length; i++)
        {
            if (!File.Exists(Path.Combine(Root, PrefabNames[i])))
            {
                GenerateAll();
                return;
            }
        }
    }

    private static void CreateBattlePyramid(Material gold, Material dark, Material cyan, Material trim, Material gun)
    {
        ModelBuilder b = new ModelBuilder("SR_BattlePyramid_Authored", gold, dark, cyan, trim, gun);

        b.Frustum(new Vector3(0f, 0.08f, 0f), new Vector2(7.9f, 9.1f), new Vector2(7.35f, 8.65f), 0.62f, Dark);
        b.Frustum(new Vector3(0f, 0.52f, -0.1f), new Vector2(6.7f, 7.7f), new Vector2(5.5f, 6.45f), 1.15f, Gold);
        b.Frustum(new Vector3(0f, 1.5f, -0.15f), new Vector2(5.55f, 6.3f), new Vector2(3.35f, 3.75f), 1.95f, Gold);
        b.Frustum(new Vector3(0f, 3.25f, -0.25f), new Vector2(3.45f, 3.85f), new Vector2(1.45f, 1.55f), 1.5f, Trim);
        b.Frustum(new Vector3(0f, 4.58f, -0.25f), new Vector2(1.5f, 1.6f), new Vector2(0.42f, 0.45f), 0.78f, Gold);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 3.62f;
            b.Frustum(new Vector3(x, 0.34f, -0.05f), new Vector2(1.15f, 7.95f), new Vector2(0.92f, 7.45f), 0.92f, Dark);
            for (int i = 0; i < 7; i++)
            {
                float z = -2.85f + i * 0.96f;
                b.Cylinder(new Vector3(side * 3.95f, 0.78f, z), 0.66f, 0.44f, Quaternion.Euler(0f, 0f, 90f), Dark);
                b.Cylinder(new Vector3(side * 4.19f, 0.78f, z), 0.29f, 0.48f, Quaternion.Euler(0f, 0f, 90f), Trim);
            }

            b.Frustum(new Vector3(side * 3.1f, 1.4f, 0.35f), new Vector2(0.95f, 3.9f), new Vector2(0.58f, 3.25f), 0.65f, Dark);
            b.Box(new Vector3(side * 2.95f, 1.05f, -2.95f), new Vector3(0.2f, 0.8f, 1.15f), Quaternion.Euler(0f, 0f, side * 12f), Trim);
            b.Box(new Vector3(side * 2.55f, 2.08f, -2.0f), new Vector3(0.12f, 1.15f, 1.85f), Quaternion.Euler(0f, 0f, side * 18f), Accent);
        }

        b.Frustum(new Vector3(0f, 0.72f, 3.42f), new Vector2(3.45f, 0.45f), new Vector2(2.95f, 0.35f), 1.3f, Dark);
        b.Box(new Vector3(0f, 1.38f, 3.68f), new Vector3(2.75f, 1.02f, 0.12f), Quaternion.identity, Accent);
        for (int i = -4; i <= 4; i++)
            b.Box(new Vector3(i * 0.28f, 1.39f, 3.76f), new Vector3(0.055f, 0.84f, 0.055f), Quaternion.identity, i == 0 ? Glow : Trim);

        b.Cylinder(new Vector3(0f, 2.65f, 2.92f), 0.92f, 0.18f, Quaternion.Euler(90f, 0f, 0f), Dark);
        b.Cylinder(new Vector3(0f, 2.65f, 3.03f), 0.46f, 0.2f, Quaternion.Euler(90f, 0f, 0f), Glow);
        // The authored hull ends in its own pyramid tip. The animated mirror
        // facets and focus point are attached as the weapon rig at runtime.

        for (int i = -2; i <= 2; i++)
        {
            b.Box(new Vector3(i * 0.58f, 1.02f, -3.48f), new Vector3(0.25f, 0.42f, 0.52f), Quaternion.Euler(-12f, 0f, 0f), Accent);
            b.Cylinder(new Vector3(i * 0.58f, 1.18f, -3.86f), 0.14f, 0.56f, Quaternion.Euler(90f, 0f, 0f), Dark);
        }

        SaveModel(b, PrefabNames[0]);
    }

    private static void CreateGoldenScarab(Material gold, Material dark, Material cyan, Material trim, Material gun)
    {
        ModelBuilder b = new ModelBuilder("SR_GoldenScarab_Authored", gold, dark, cyan, trim, gun);

        b.Frustum(new Vector3(0f, 0.28f, -0.2f), new Vector2(3.5f, 4.9f), new Vector2(3.0f, 4.4f), 0.55f, Dark);
        b.Sphere(new Vector3(0f, 1.05f, -0.7f), new Vector3(3.25f, 1.15f, 3.5f), Gold);
        b.Sphere(new Vector3(-0.74f, 1.42f, -0.75f), new Vector3(1.42f, 0.34f, 2.85f), Quaternion.Euler(0f, -5f, -10f), Trim);
        b.Sphere(new Vector3(0.74f, 1.42f, -0.75f), new Vector3(1.42f, 0.34f, 2.85f), Quaternion.Euler(0f, 5f, 10f), Gold);
        b.Frustum(new Vector3(0f, 0.62f, 1.45f), new Vector2(2.6f, 1.75f), new Vector2(1.75f, 1.15f), 1.15f, Gold);
        b.Sphere(new Vector3(0f, 1.18f, 2.12f), new Vector3(1.75f, 0.85f, 1.2f), Dark);
        b.Sphere(new Vector3(0f, 1.45f, 2.58f), new Vector3(1.05f, 0.42f, 0.5f), Glow);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 3; i++)
            {
                float z = -1.55f + i * 1.5f;
                float sweep = i == 0 ? -24f : i == 1 ? 0f : 24f;
                b.Box(new Vector3(side * 2.15f, 0.74f, z), new Vector3(1.9f, 0.22f, 0.3f), Quaternion.Euler(0f, side * (28f + sweep * 0.25f), side * 16f), Accent);
                b.Box(new Vector3(side * 3.02f, 0.38f, z + sweep * 0.012f), new Vector3(1.25f, 0.18f, 0.27f), Quaternion.Euler(0f, side * (34f + sweep * 0.32f), side * -24f), Dark);
                b.Cylinder(new Vector3(side * 3.58f, 0.16f, z + sweep * 0.018f), 0.28f, 0.5f, Quaternion.Euler(0f, 0f, 90f), Trim);
            }

            b.Box(new Vector3(side * 0.88f, 1.72f, 0.05f), new Vector3(0.18f, 0.34f, 2.85f), Quaternion.Euler(0f, 0f, side * 8f), Dark);
            b.Cylinder(new Vector3(side * 0.72f, 1.32f, 2.92f), 0.12f, 1.18f, Quaternion.Euler(70f, 0f, side * 14f), Accent);
            b.Diamond(new Vector3(side * 0.82f, 1.08f, 3.46f), new Vector3(0.24f, 0.2f, 0.62f), Trim);
        }

        b.Frustum(new Vector3(0f, 1.5f, -0.65f), new Vector2(1.45f, 1.8f), new Vector2(1.15f, 1.45f), 0.52f, Dark);
        b.Cylinder(new Vector3(0f, 2.12f, -0.35f), 0.7f, 0.28f, Quaternion.identity, Accent);
        b.Cylinder(new Vector3(0f, 2.25f, 1.05f), 0.18f, 2.55f, Quaternion.Euler(90f, 0f, 0f), Accent);
        b.Box(new Vector3(0f, 1.02f, -2.55f), new Vector3(1.25f, 0.62f, 0.2f), Quaternion.identity, Glow);

        SaveModel(b, PrefabNames[1]);
    }

    private static void CreateImperialAssault(Material red, Material dark, Material jade, Material trim, Material gun)
    {
        ModelBuilder b = new ModelBuilder("SR_ImperialAssault_Authored", red, dark, jade, trim, gun);

        b.Frustum(new Vector3(0f, 0.12f, 0f), new Vector2(6.4f, 8.25f), new Vector2(5.95f, 7.75f), 0.68f, Dark);
        for (int side = -1; side <= 1; side += 2)
        {
            b.Frustum(new Vector3(side * 2.75f, 0.38f, -0.1f), new Vector2(1.25f, 7.35f), new Vector2(1.02f, 6.8f), 1.05f, Dark);
            for (int i = 0; i < 6; i++)
            {
                float z = -2.65f + i * 1.08f;
                b.Cylinder(new Vector3(side * 3.22f, 0.78f, z), 0.78f, 0.48f, Quaternion.Euler(0f, 0f, 90f), Dark);
                b.Cylinder(new Vector3(side * 3.48f, 0.78f, z), 0.3f, 0.5f, Quaternion.Euler(0f, 0f, 90f), Trim);
            }
        }

        b.Frustum(new Vector3(0f, 0.62f, -0.15f), new Vector2(5.25f, 6.9f), new Vector2(3.95f, 5.65f), 1.45f, Gold);
        b.Frustum(new Vector3(0f, 1.88f, 0.28f), new Vector2(4.0f, 5.15f), new Vector2(2.75f, 3.55f), 1.25f, Gold);
        b.Frustum(new Vector3(0f, 1.05f, 3.55f), new Vector2(4.8f, 0.7f), new Vector2(3.15f, 0.35f), 0.82f, Dark);
        b.Diamond(new Vector3(0f, 1.55f, 4.18f), new Vector3(1.15f, 0.5f, 1.5f), Trim);

        b.Cylinder(new Vector3(0f, 3.32f, 0.15f), 2.05f, 0.5f, Quaternion.identity, Dark);
        b.Frustum(new Vector3(0f, 3.48f, 0.4f), new Vector2(2.6f, 3.2f), new Vector2(1.85f, 2.35f), 0.88f, Gold);
        b.Cylinder(new Vector3(0f, 4.05f, 3.15f), 0.42f, 5.2f, Quaternion.Euler(90f, 0f, 0f), Accent);
        b.Cylinder(new Vector3(0f, 4.05f, 5.65f), 0.55f, 0.32f, Quaternion.Euler(90f, 0f, 0f), Trim);

        for (int side = -1; side <= 1; side += 2)
        {
            b.Frustum(new Vector3(side * 1.85f, 2.48f, -0.55f), new Vector2(1.05f, 2.4f), new Vector2(0.78f, 1.9f), 0.82f, Dark);
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    b.Cylinder(new Vector3(side * 2.05f, 2.82f + row * 0.28f, 0.62f + col * 0.35f), 0.16f, 0.26f, Quaternion.Euler(90f, 0f, 0f), Accent);
        }

        b.Frustum(new Vector3(0f, 2.62f, -2.75f), new Vector2(1.8f, 1.15f), new Vector2(1.15f, 0.72f), 1.25f, Dark);
        b.Sphere(new Vector3(0f, 3.32f, -3.32f), new Vector3(0.92f, 0.48f, 0.28f), Glow);
        b.Box(new Vector3(0f, 1.38f, -3.68f), new Vector3(2.2f, 0.18f, 0.82f), Quaternion.Euler(18f, 0f, 0f), Trim);

        SaveModel(b, PrefabNames[2]);
    }

    private static void CreateMandarinkaPalace(Material red, Material dark, Material jade, Material trim, Material ivory)
    {
        ModelBuilder b = new ModelBuilder("SR_MandarinkaPalace_Authored", red, dark, jade, trim, ivory);

        b.Frustum(new Vector3(0f, 0.2f, 0f), new Vector2(23.5f, 20.5f), new Vector2(21.5f, 18.7f), 1.2f, Dark);
        b.Frustum(new Vector3(0f, 1.15f, 0f), new Vector2(20.8f, 17.6f), new Vector2(17.2f, 14.2f), 2.25f, Gold);
        b.Frustum(new Vector3(0f, 3.18f, 0f), new Vector2(17.4f, 14.4f), new Vector2(14.8f, 12.0f), 1.5f, Gold);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int zside = -1; zside <= 1; zside += 2)
            {
                Vector3 pod = new Vector3(side * 9.8f, 0.18f, zside * 6.85f);
                b.Frustum(pod, new Vector2(4.2f, 5.1f), new Vector2(3.55f, 4.4f), 1.55f, Dark);
                for (int i = -1; i <= 1; i++)
                    b.Cylinder(pod + new Vector3(side * 2.12f, 0.72f, i * 1.35f), 1.05f, 0.72f, Quaternion.Euler(0f, 0f, 90f), Trim);
            }

            b.Box(new Vector3(side * 10.95f, 3.45f, 0f), new Vector3(0.22f, 4.1f, 12.5f), Quaternion.Euler(0f, 0f, side * 14f), Trim);
            b.Box(new Vector3(side * 11.35f, 4.2f, 0f), new Vector3(0.16f, 3.3f, 10.8f), Quaternion.Euler(0f, 0f, side * 17f), Glow);
            b.Cylinder(new Vector3(side * 7.2f, 5.25f, 5.3f), 1.05f, 0.5f, Quaternion.identity, Dark);
            b.Cylinder(new Vector3(side * 7.2f, 5.45f, 8.1f), 0.34f, 5.25f, Quaternion.Euler(90f, 0f, 0f), Dark);
        }

        PalaceTower(b, new Vector3(0f, 4.58f, -0.4f), 1.6f, 5);
        PalaceTower(b, new Vector3(-5.8f, 4.5f, -4.7f), 0.95f, 3);
        PalaceTower(b, new Vector3(5.8f, 4.5f, -4.7f), 0.95f, 3);
        PalaceTower(b, new Vector3(-5.8f, 4.5f, 4.7f), 0.95f, 3);
        PalaceTower(b, new Vector3(5.8f, 4.5f, 4.7f), 0.95f, 3);

        b.Frustum(new Vector3(0f, 3.75f, 7.0f), new Vector2(6.8f, 0.85f), new Vector2(5.2f, 0.48f), 3.0f, Dark);
        b.Box(new Vector3(0f, 5.18f, 7.52f), new Vector3(4.8f, 2.1f, 0.16f), Quaternion.identity, Accent);
        for (int i = -5; i <= 5; i++)
            b.Box(new Vector3(i * 0.39f, 5.2f, 7.64f), new Vector3(0.07f, 1.75f, 0.07f), Quaternion.identity, i == 0 ? Glow : Trim);

        b.Cylinder(new Vector3(0f, 12.25f, 0.25f), 1.4f, 0.55f, Quaternion.identity, Dark);
        b.Cylinder(new Vector3(0f, 12.5f, 4.55f), 0.48f, 8.25f, Quaternion.Euler(90f, 0f, 0f), Dark);
        b.Cylinder(new Vector3(0f, 12.5f, 8.48f), 0.64f, 0.42f, Quaternion.Euler(90f, 0f, 0f), Trim);

        for (int i = -3; i <= 3; i++)
        {
            b.Frustum(new Vector3(i * 2.2f, 4.42f, -6.45f), new Vector2(1.25f, 1.15f), new Vector2(0.82f, 0.76f), 1.2f, Dark);
            b.Diamond(new Vector3(i * 2.2f, 5.86f, -6.45f), new Vector3(0.36f, 0.65f, 0.36f), i == 0 ? Glow : Trim);
        }

        SaveModel(b, PrefabNames[3]);
    }

    private static void PalaceTower(ModelBuilder b, Vector3 basePos, float scale, int levels)
    {
        for (int i = 0; i < levels; i++)
        {
            float width = Mathf.Max(1.25f, 4.0f - i * 0.48f) * scale;
            float y = basePos.y + i * 1.42f * scale;
            b.Frustum(new Vector3(basePos.x, y, basePos.z), new Vector2(width, width), new Vector2(width * 0.78f, width * 0.78f), 0.92f * scale, Gold);
            b.Frustum(new Vector3(basePos.x, y + 0.84f * scale, basePos.z), new Vector2(width * 1.42f, width * 1.42f), new Vector2(width * 0.68f, width * 0.68f), 0.42f * scale, Trim);
        }
        float top = basePos.y + levels * 1.42f * scale;
        b.Cylinder(new Vector3(basePos.x, top, basePos.z), 0.22f * scale, 1.1f * scale, Quaternion.identity, Trim);
        b.Diamond(new Vector3(basePos.x, top + 0.72f * scale, basePos.z), Vector3.one * 0.38f * scale, Glow);
    }

    private static void SaveModel(ModelBuilder builder, string prefabName)
    {
        string meshPath = Path.Combine(Root, prefabName.Replace(".prefab", ".asset")).Replace("\\", "/");
        string prefabPath = Path.Combine(Root, prefabName).Replace("\\", "/");
        AssetDatabase.DeleteAsset(meshPath);
        Mesh mesh = builder.Build(out Material[] materials);
        AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject root = new GameObject(builder.Name);
        MeshFilter filter = root.AddComponent<MeshFilter>();
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }

    private static Texture2D GetArmourTexture()
    {
        string path = Root + "/SR_Art_ArmourSurface.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        const int size = 128;
        if (texture == null)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "SR_Art_ArmourSurface",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.085f + 3.1f, y * 0.085f + 7.7f);
                float value = Mathf.Lerp(0.72f, 1f, noise);
                bool seam = x % 64 < 2 || y % 64 < 2;
                bool bolt = ((x + 4) % 64 < 3 && (y + 4) % 64 < 3);
                if (seam) value *= 0.58f;
                if (bolt) value = 0.42f;
                pixels[y * size + x] = new Color(value, value, value, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Material GetMaterial(string name, Color color, float metallic, float smoothness, Texture texture, Color? emission = null)
    {
        string path = Root + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (texture != null && material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one);
        }

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private sealed class ModelBuilder
    {
        private readonly List<CombineInstance>[] groups;
        private readonly Material[] materials;
        private static Mesh cube;
        private static Mesh cylinder;
        private static Mesh sphere;
        public string Name { get; }

        public ModelBuilder(string name, params Material[] modelMaterials)
        {
            Name = name;
            materials = modelMaterials;
            groups = new List<CombineInstance>[materials.Length];
            for (int i = 0; i < groups.Length; i++) groups[i] = new List<CombineInstance>();
            EnsurePrimitives();
        }

        public void Box(Vector3 pos, Vector3 size, Quaternion rot, int mat)
        {
            Add(cube, Matrix4x4.TRS(pos, rot, size), mat);
        }

        public void Cylinder(Vector3 pos, float diameter, float height, Quaternion rot, int mat)
        {
            Add(cylinder, Matrix4x4.TRS(pos, rot, new Vector3(diameter, height * 0.5f, diameter)), mat);
        }

        public void Sphere(Vector3 pos, Vector3 size, int mat)
        {
            Sphere(pos, size, Quaternion.identity, mat);
        }

        public void Sphere(Vector3 pos, Vector3 size, Quaternion rot, int mat)
        {
            Add(sphere, Matrix4x4.TRS(pos, rot, size), mat);
        }

        public void Frustum(Vector3 bottomCenter, Vector2 bottomSize, Vector2 topSize, float height, int mat)
        {
            Add(CreateFrustumMesh(bottomSize, topSize, height), Matrix4x4.TRS(bottomCenter, Quaternion.identity, Vector3.one), mat);
        }

        public void Diamond(Vector3 pos, Vector3 size, int mat)
        {
            Add(CreateDiamondMesh(), Matrix4x4.TRS(pos, Quaternion.identity, size), mat);
        }

        private void Add(Mesh mesh, Matrix4x4 matrix, int material)
        {
            groups[Mathf.Clamp(material, 0, groups.Length - 1)].Add(new CombineInstance { mesh = mesh, transform = matrix });
        }

        public Mesh Build(out Material[] usedMaterials)
        {
            List<CombineInstance> finalGroups = new List<CombineInstance>();
            List<Material> finalMaterials = new List<Material>();
            List<Mesh> temporary = new List<Mesh>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].Count == 0) continue;
                Mesh groupMesh = new Mesh { indexFormat = IndexFormat.UInt32, name = Name + "_Group_" + i };
                groupMesh.CombineMeshes(groups[i].ToArray(), true, true, false);
                temporary.Add(groupMesh);
                finalGroups.Add(new CombineInstance { mesh = groupMesh, transform = Matrix4x4.identity });
                finalMaterials.Add(materials[i]);
            }

            Mesh final = new Mesh { indexFormat = IndexFormat.UInt32, name = Name + "_Mesh" };
            final.CombineMeshes(finalGroups.ToArray(), false, false, false);
            final.RecalculateBounds();
            final.RecalculateNormals();
            final.RecalculateTangents();
            usedMaterials = finalMaterials.ToArray();
            for (int i = 0; i < temporary.Count; i++) Object.DestroyImmediate(temporary[i]);
            return final;
        }

        private static void EnsurePrimitives()
        {
            if (cube != null) return;
            cube = GetPrimitiveMesh(PrimitiveType.Cube);
            cylinder = GetPrimitiveMesh(PrimitiveType.Cylinder);
            sphere = GetPrimitiveMesh(PrimitiveType.Sphere);
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh source = temp.GetComponent<MeshFilter>().sharedMesh;
            Mesh copy = Object.Instantiate(source);
            copy.name = "SR_" + type + "_Source";
            Object.DestroyImmediate(temp);
            return copy;
        }

        private static Mesh CreateFrustumMesh(Vector2 bottom, Vector2 top, float height)
        {
            float bx = bottom.x * 0.5f;
            float bz = bottom.y * 0.5f;
            float tx = top.x * 0.5f;
            float tz = top.y * 0.5f;
            Vector3 b0 = new Vector3(-bx, 0f, -bz);
            Vector3 b1 = new Vector3(bx, 0f, -bz);
            Vector3 b2 = new Vector3(bx, 0f, bz);
            Vector3 b3 = new Vector3(-bx, 0f, bz);
            Vector3 t0 = new Vector3(-tx, height, -tz);
            Vector3 t1 = new Vector3(tx, height, -tz);
            Vector3 t2 = new Vector3(tx, height, tz);
            Vector3 t3 = new Vector3(-tx, height, tz);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            AddQuad(vertices, triangles, uvs, b0, b1, b2, b3);
            AddQuad(vertices, triangles, uvs, t0, t3, t2, t1);
            AddQuad(vertices, triangles, uvs, b3, b2, t2, t3);
            AddQuad(vertices, triangles, uvs, b1, b0, t0, t1);
            AddQuad(vertices, triangles, uvs, b2, b1, t1, t2);
            AddQuad(vertices, triangles, uvs, b0, b3, t3, t0);
            Mesh mesh = new Mesh { name = "SR_Frustum_Source" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateDiamondMesh()
        {
            Vector3 top = new Vector3(0f, 0.5f, 0f);
            Vector3 bottom = new Vector3(0f, -0.5f, 0f);
            Vector3 left = new Vector3(-0.5f, 0f, 0f);
            Vector3 right = new Vector3(0.5f, 0f, 0f);
            Vector3 front = new Vector3(0f, 0f, 0.5f);
            Vector3 back = new Vector3(0f, 0f, -0.5f);
            Vector3[] vertices = { top, left, front, right, back, bottom };
            int[] triangles =
            {
                0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
                5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1
            };
            Mesh mesh = new Mesh { name = "SR_Diamond_Source", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
        }
    }
}