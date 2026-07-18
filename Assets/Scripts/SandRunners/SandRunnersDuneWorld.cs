using UnityEngine;

public partial class SandRunnersPrototype
{
    private Transform duneWorldRoot;
    private Material duneSandMaterial;
    private Material duneRidgeMaterial;
    private Material duneRockMaterial;
    private Texture2D duneSandTexture;
    private Texture2D duneRidgeTexture;
    private bool duneWorldBuilt;

    private void EnsureLargeDuneWorld()
    {
        mapHalfSize = Mathf.Max(mapHalfSize, 950f);
        moveSpeed = Mathf.Clamp(moveSpeed, 4.2f, 5.4f);
        turnSpeed = Mathf.Clamp(turnSpeed, 48f, 58f);
        captureRadius = Mathf.Max(captureRadius, 42f);
        cameraDistance = Mathf.Max(cameraDistance, 58f);
        cameraHeight = Mathf.Max(cameraHeight, 12f);

        if (mainCamera != null)
            mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 3200f);

        if (duneWorldBuilt)
            return;

        duneSandMaterial = CreateMaterial("New Galikarnass Neutral Layered Sand", new Color(0.48f, 0.43f, 0.34f, 1f));
        duneRidgeMaterial = CreateMaterial("New Galikarnass Wind Ridge Highlights", new Color(0.68f, 0.61f, 0.48f, 1f));
        duneRockMaterial = CreateMaterial("New Galikarnass Dark Desert Stone", new Color(0.14f, 0.135f, 0.125f, 1f));
        SetMetallic(duneSandMaterial, 0f, 0.34f);
        SetMetallic(duneRidgeMaterial, 0f, 0.28f);
        SetMetallic(duneRockMaterial, 0.05f, 0.28f);
        ApplyProceduralDuneTextures();

        HideOldFlatDesertRenderers();

        GameObject existing = GameObject.Find("New_Galikarnass_Dune_World_Runtime");
        if (existing != null)
            Destroy(existing);

        duneWorldRoot = new GameObject("New_Galikarnass_Dune_World_Runtime").transform;
        CreateDuneTerrainMesh();
        CreateDuneRidges();
        CreateAshgabatRuinClusters();
        CreatePathLandmarks();
        CreateBorderCausewayHints();
        duneWorldBuilt = true;
    }

    private float GetPlayableGroundHeight(Vector3 position)
    {
        if (!duneWorldBuilt)
            return 0f;

        return ComputeDuneHeight(position.x, position.z) - 0.08f;
    }

    private void SpreadScenarioPointsForLargeMap()
    {
        PlacePlayerStartForLargeMap();

        if (resourceNodes.Count > 0)
        {
            Vector3[] positions =
            {
                new Vector3(-705f, 0f, -625f),
                new Vector3(-585f, 0f, -535f),
                new Vector3(-760f, 0f, -455f),
                new Vector3(-245f, 0f, -155f),
                new Vector3(-35f, 0f, 40f),
                new Vector3(185f, 0f, -90f),
                new Vector3(110f, 0f, 280f),
                new Vector3(520f, 0f, 500f),
                new Vector3(675f, 0f, 625f),
                new Vector3(735f, 0f, 420f),
                new Vector3(-145f, 0f, -285f),
                new Vector3(320f, 0f, 85f),
                new Vector3(-820f, 0f, -180f),
                new Vector3(-520f, 0f, 260f),
                new Vector3(-40f, 0f, 520f),
                new Vector3(380f, 0f, -470f),
                new Vector3(780f, 0f, -140f),
                new Vector3(-660f, 0f, 420f),
                new Vector3(480f, 0f, 130f),
                new Vector3(-180f, 0f, 560f),
                new Vector3(800f, 0f, 300f)
            };
            ResourceKind[] kinds =
            {
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind,
                ResourceKind.Sand,
                ResourceKind.Gold,
                ResourceKind.Wind
            };

            for (int i = 0; i < resourceNodes.Count; i++)
            {
                Vector3 position;
                if (i < positions.Length)
                {
                    position = positions[i];
                }
                else
                {
                    float angle = i * 137.5f * Mathf.Deg2Rad;
                    float radius = Mathf.Min(mapHalfSize - 92f, 420f + i * 34f);
                    position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                }

                position.y = ComputeDuneHeight(position.x, position.z) + 0.25f;
                ResourceNode node = resourceNodes[i];
                node.transform.position = position;
                node.kind = kinds[i % kinds.Length];
                node.label = BuildNodeLabel(node.kind, i + 1);
                UpgradeResourceNodeVisual(node, i);
            }
        }

        CreateStartRouteMarker();

        for (int i = 0; i < enemySpawnPoints.Count; i++)
        {
            float angle = Mathf.Lerp(20f, 86f, i / Mathf.Max(1f, enemySpawnPoints.Count - 1f)) * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(angle) * (mapHalfSize - 36f), 0f, Mathf.Sin(angle) * (mapHalfSize - 36f));
            position.y = ComputeDuneHeight(position.x, position.z) + 0.1f;
            enemySpawnPoints[i].position = position;
        }
    }

    private void PlacePlayerStartForLargeMap()
    {
        if (battlePyramid == null)
            return;

        Vector3 start = new Vector3(-mapHalfSize * 0.66f, 0f, -mapHalfSize * 0.62f);
        start.y = GetPlayableGroundHeight(start) + pyramidGroundClearance;
        battlePyramid.position = start;
        battlePyramid.rotation = Quaternion.Euler(0f, 42f, 0f);
        pyramidVelocity = Vector3.zero;
        pyramidThrottleBlend = 0f;
        hasCommandDestination = false;
        pyramidStartPosition = start;
        pyramidStartRotation = battlePyramid.rotation;
        pyramidStartPoseCached = true;
    }

    private void CreateStartRouteMarker()
    {
        GameObject existing = GameObject.Find("SandRunners_Start_Route_Marker");
        if (existing != null)
            Destroy(existing);

        Transform root = new GameObject("SandRunners_Start_Route_Marker").transform;
        if (duneWorldRoot != null)
            root.SetParent(duneWorldRoot, false);
        root.position = new Vector3(-665f, GetPlayableGroundHeight(new Vector3(-665f, 0f, -610f)) + 0.2f, -610f);
        Material markerMaterial = commandMaterial != null ? commandMaterial : duneRidgeMaterial;
        CreateCylinder(root, "First_Resource_Ring", Vector3.zero, Quaternion.identity, new Vector3(7.2f, 0.035f, 7.2f), markerMaterial, true);
        CreateBox(root, "First_Resource_Arrow", new Vector3(-6.8f, 0.22f, -1.4f), Quaternion.Euler(0f, -18f, 0f), new Vector3(18f, 0.08f, 0.55f), markerMaterial);
        CreatePointLight(root, "First_Resource_Glow", new Vector3(0f, 2.2f, 0f), new Color(1f, 0.72f, 0.22f, 1f), 0.85f, 22f);

        TextMesh label = new GameObject("First_Resource_Label").AddComponent<TextMesh>();
        label.transform.SetParent(root, false);
        label.transform.localPosition = new Vector3(0f, 4.6f, 0f);
        label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        label.text = "FIRST SAND NODE";
        label.characterSize = 0.62f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.78f, 0.32f, 1f);
    }

    private void CreateDuneTerrainMesh()
    {
        const int resolution = 176;
        int vertexCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[resolution * resolution * 6];
        float size = mapHalfSize * 2f;

        int v = 0;
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float px = -mapHalfSize + size * x / resolution;
                float pz = -mapHalfSize + size * z / resolution;
                vertices[v] = new Vector3(px, ComputeDuneHeight(px, pz) - 0.08f, pz);
                uv[v] = new Vector2(x / (float)resolution * 42f, z / (float)resolution * 42f);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * (resolution + 1) + x;
                triangles[t++] = i;
                triangles[t++] = i + resolution + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + resolution + 1;
                triangles[t++] = i + resolution + 2;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "New_Galikarnass_Dune_Field_Mesh";
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject terrain = new GameObject("New_Galikarnass_Dune_Field");
        terrain.transform.SetParent(duneWorldRoot, false);
        MeshFilter filter = terrain.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = terrain.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = duneSandMaterial;
        MeshCollider collider = terrain.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    private float ComputeDuneHeight(float x, float z)
    {
        float longWave = Mathf.Sin(x * 0.0105f + z * 0.0075f) * 5.2f;
        float crossWave = Mathf.Sin(z * 0.017f - x * 0.0045f) * 2.4f;
        float duneRoll = Mathf.Sin((x + z) * 0.0065f + Mathf.PerlinNoise(x * 0.0035f, z * 0.0035f) * 4.2f) * 3.6f;
        float basinNoise = (Mathf.PerlinNoise(x * 0.005f + 17.3f, z * 0.005f + 41.9f) - 0.5f) * 4.4f;
        float fineRidges = Mathf.Sin(x * 0.14f + z * 0.035f) * 0.34f;
        float height = longWave + crossWave + duneRoll + basinNoise + fineRidges + 2.2f;
        float centralRouteFlatten = Mathf.Clamp01(Mathf.Abs(x) / 78f);
        height *= Mathf.Lerp(0.3f, 1f, centralRouteFlatten);
        return Mathf.Max(0f, height);
    }

    private void HideOldFlatDesertRenderers()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        for (int i = 0; i < renderers.Length; i++)
        {
            string lower = renderers[i].name.ToLowerInvariant();
            if (lower.Contains("desert_floor") || lower.Contains("new_galikarnass_desert_floor"))
                renderers[i].enabled = false;
        }
    }

    private void CreateChokepointTerrain()
    {
        Vector3[] chokepoints =
        {
            new Vector3(-220f, 0f, -50f),
            new Vector3(-80f, 0f, 65f),
            new Vector3(60f, 0f, -30f),
            new Vector3(300f, 0f, 230f),
            new Vector3(420f, 0f, 320f)
        };
        for (int i = 0; i < chokepoints.Length; i++)
        {
            float y = ComputeDuneHeight(chokepoints[i].x, chokepoints[i].z) + 0.3f;
            for (int j = -1; j <= 1; j += 2)
            {
                Quaternion rotation = Quaternion.Euler(0f, i * 37f + j * 18f, 0f);
                float offsetX = j * 18f + Mathf.Sin(i * 1.3f + j) * 5f;
                CreateBox(duneWorldRoot, "Chokepoint_Boulder_Barrier_" + i + "_" + j,
                    new Vector3(chokepoints[i].x + offsetX, y + 0.8f, chokepoints[i].z + j * 6f),
                    rotation, new Vector3(14f + i * 2f, 1.4f + (i % 3) * 0.4f, 2.8f + (i % 2) * 0.6f), duneRockMaterial);
            }
            CreateBox(duneWorldRoot, "Chokepoint_Ring_Marker_" + i,
                new Vector3(chokepoints[i].x, y - 0.05f, chokepoints[i].z),
                Quaternion.identity, new Vector3(16f, 0.03f, 16f), duneRidgeMaterial);
        }
    }

    private void CreateDuneRidges()
    {
        CreateChokepointTerrain();
        for (int i = 0; i < 170; i++)
        {
            float angle = i * 41.7f * Mathf.Deg2Rad;
            float radius = 72f + (i % 47) * 18.5f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = ComputeDuneHeight(x, z) + 0.06f;
            Quaternion rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 15f + Mathf.Sin(i * 0.77f) * 16f, 0f);
            GameObject ridge = CreateBox(duneWorldRoot, "Wind_Carved_Dune_Ridge_" + i, new Vector3(x, y, z), rotation, new Vector3(28f + (i % 9) * 7f, 0.045f, 0.5f + (i % 4) * 0.16f), duneRidgeMaterial);
            ridge.transform.position = new Vector3(x, y, z);
        }

        for (int i = 0; i < 76; i++)
        {
            float angle = i * 73.3f * Mathf.Deg2Rad;
            float radius = 130f + (i % 29) * 28f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = ComputeDuneHeight(x, z) + 0.35f;
            CreateBox(duneWorldRoot, "Dark_Stone_Outcrop_" + i, new Vector3(x, y, z), Quaternion.Euler(0f, i * 31f, 0f), new Vector3(4f + (i % 4), 0.9f + (i % 5) * 0.24f, 2.4f + (i % 5) * 0.9f), duneRockMaterial);
        }
    }

    private void CreateAshgabatRuinClusters()
    {
        CreateRuinCluster("Buried_White_City", "BURIED CITY", new Vector3(-350f, 0f, -285f), 18, 52f, 8.2f);
        CreateRuinCluster("Broken_Caravan_Causeway", "BROKEN ROAD", new Vector3(-60f, 0f, 210f), 14, 72f, 5.4f);
        CreateRuinCluster("Ancient_Desert_Port", "ANCIENT PORT", new Vector3(335f, 0f, -260f), 16, 64f, 7.4f);
        CreateRuinCluster("Earth_Border_Beacon_Ruins", "BORDER BEACON", new Vector3(580f, 0f, 210f), 12, 44f, 9.5f);
        CreateCentralBattleArena();
    }

    private void CreateCentralBattleArena()
    {
        Vector3 center = new Vector3(-35f, 0f, 40f);
        float y = ComputeDuneHeight(center.x, center.z);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            float radius = 32f + (i % 3) * 6f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            float h = ComputeDuneHeight(x, z) + 0.3f;
            CreateBox(duneWorldRoot, "Arena_Pillar_" + i,
                new Vector3(x, h + 2.5f, z),
                Quaternion.Euler(0f, i * 42f, 0f),
                new Vector3(2.2f + (i % 3), 4.5f + (i % 4), 2.2f), duneRockMaterial);
            CreateBox(duneWorldRoot, "Arena_Pillar_Cap_" + i,
                new Vector3(x, h + 5f + (i % 4), z),
                Quaternion.Euler(0f, i * 22f, 0f),
                new Vector3(3.2f + (i % 2), 0.25f, 3.2f), duneRidgeMaterial);
        }
        CreateBox(duneWorldRoot, "Arena_Floor",
            new Vector3(center.x, y - 0.04f, center.z),
            Quaternion.identity, new Vector3(48f, 0.03f, 48f), duneRidgeMaterial);
        CreatePointLight(duneWorldRoot, "Arena_Center_Light",
            new Vector3(center.x, y + 8f, center.z),
            new Color(1f, 0.62f, 0.22f, 1f), 1.2f, 35f);
    }

    private void CreateRuinCluster(string prefix, string labelText, Vector3 center, int count, float spread, float baseHeight)
    {
        float centerY = ComputeDuneHeight(center.x, center.z);
        CreateRuinPoiBeacon(prefix, labelText, new Vector3(center.x, centerY, center.z));

        for (int i = 0; i < count; i++)
        {
            float angle = i * 137.5f * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(8f, spread, (i % 9) / 8f);
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius * 0.72f;
            float y = ComputeDuneHeight(x, z) + 1.4f;
            float height = baseHeight + (i % 6) * 2.7f;
            GameObject ruin = CreateBox(duneWorldRoot, prefix + "_Marble_Tower_" + i, new Vector3(x, y + height * 0.5f, z), Quaternion.Euler(0f, i * 29f, 0f), new Vector3(3.4f + (i % 3), height, 3.4f), duneRockMaterial);
            CreateBox(ruin.transform, "Faded_Gold_Ruin_Cap", new Vector3(0f, 0.52f, 0f), Quaternion.identity, new Vector3(1.3f, 0.08f, 1.3f), duneRidgeMaterial);
            if (i % 4 == 0)
                CreateBox(duneWorldRoot, prefix + "_Collapsed_Arcade_" + i, new Vector3(x + 6f, y + 0.45f, z - 4f), Quaternion.Euler(0f, i * 17f, 0f), new Vector3(9f, 0.42f, 1.8f), duneRockMaterial);
        }
    }

    private void CreateRuinPoiBeacon(string prefix, string labelText, Vector3 position)
    {
        Transform beacon = new GameObject(prefix + "_POI_Beacon").transform;
        beacon.SetParent(duneWorldRoot, false);
        beacon.position = position + Vector3.up * 0.2f;
        CreateCylinder(beacon, "POI_Ground_Ring", new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(9f, 0.025f, 9f), duneRidgeMaterial, true);
        CreateBox(beacon, "POI_Obelisk_Silhouette", new Vector3(0f, 7.2f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(1.2f, 13.5f, 1.2f), duneRockMaterial, true);
        CreatePointLight(beacon, "POI_Warm_Label_Light", new Vector3(0f, 9.2f, 0f), new Color(1f, 0.58f, 0.2f, 1f), 0.62f, 24f);

        TextMesh label = new GameObject("POI_Floating_Label").AddComponent<TextMesh>();
        label.transform.SetParent(beacon, false);
        label.transform.localPosition = new Vector3(0f, 15.4f, 0f);
        label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        label.text = labelText;
        label.characterSize = 0.7f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.72f, 0.32f, 1f);
    }

    private void UpgradeResourceNodeVisual(ResourceNode node, int index)
    {
        if (node == null || node.transform == null)
            return;

        Transform old = node.transform.Find("Resource_Readable_Visual");
        if (old != null)
            Destroy(old.gameObject);

        Renderer[] existingRenderers = node.transform.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < existingRenderers.Length; i++)
            existingRenderers[i].enabled = false;

        Transform root = new GameObject("Resource_Readable_Visual").transform;
        root.SetParent(node.transform, false);
        root.localPosition = Vector3.zero;

        Material material = node.kind == ResourceKind.Gold ? controlledMaterial : node.kind == ResourceKind.Wind ? contestedMaterial : enemyMaterial;
        if (node.kind == ResourceKind.Sand)
        {
            CreateBox(root, "Quicksand_Basin", new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(8.5f, 0.16f, 7.2f), duneRidgeMaterial, true);
            CreateCylinder(root, "Quicksand_Sink", new Vector3(0f, 0.19f, 0f), Quaternion.identity, new Vector3(2.8f, 0.08f, 2.8f), duneSandMaterial, true);
            CreateBox(root, "Red_Elemental_Mining_Station", new Vector3(0f, 1.25f, 0f), Quaternion.identity, new Vector3(4.6f, 2.2f, 2.6f), enemyMaterial, true);
            CreateBox(root, "Chinese_Mine_Roof", new Vector3(0f, 2.48f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(3.65f, 0.25f, 3.65f), enemyMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                CreateBox(root, "Mine_Derrick_Pillar_" + side, new Vector3(side * 2.25f, 2.35f, 0f), Quaternion.identity, new Vector3(0.22f, 4.2f, 0.22f), enemyMaterial);
                CreateBox(root, "Mine_Conveyor_" + side, new Vector3(side * 3.6f, 0.72f, -0.72f), Quaternion.Euler(0f, side * 18f, 0f), new Vector3(2.5f, 0.18f, 0.62f), enemyMaterial);
            }
            CreateCylinder(root, "Mine_Derrick_Crown", new Vector3(0f, 4.4f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.25f, 0.16f, 1.25f), enemyMaterial);
            CreatePointLight(root, "Red_Mining_Beacon", new Vector3(0f, 3.4f, 0f), new Color(1f, 0.04f, 0.02f, 1f), 1.3f, 18f);
        }
        else if (node.kind == ResourceKind.Gold)
        {
            CreateBox(root, "Gold_Vein_Base", new Vector3(0f, 0.24f, 0f), Quaternion.identity, new Vector3(8.6f, 0.42f, 7.2f), duneRockMaterial, true);
            for (int vein = 0; vein < 4; vein++)
            {
                float angle = (index * 31f + vein * 43f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 2.3f, 0.9f + (vein % 2) * 0.45f, Mathf.Sin(angle) * 1.8f);
                CreateBox(root, "Massive_Gold_Vein_" + vein, offset, Quaternion.Euler(0f, vein * 37f, (vein % 2 == 0 ? -18f : 21f)), new Vector3(1.1f + vein * 0.18f, 2.8f + (vein % 3) * 0.7f, 0.7f), controlledMaterial, true);
            }
            CreateBox(root, "Red_Construction_Excavator", new Vector3(0f, 1.2f, -3.25f), Quaternion.Euler(0f, 18f, 0f), new Vector3(2.6f, 1.6f, 2.2f), enemyMaterial, true);
            CreateBox(root, "Excavator_Boom", new Vector3(1.15f, 2.8f, -2.25f), Quaternion.Euler(-28f, 18f, 0f), new Vector3(0.46f, 3.6f, 0.46f), enemyMaterial);
            CreateBox(root, "Excavator_Bucket", new Vector3(2.08f, 3.75f, -1.2f), Quaternion.Euler(-20f, 18f, 0f), new Vector3(1.3f, 0.65f, 0.95f), enemyMaterial);
            CreateBox(root, "Red_Construction_Crane", new Vector3(-3.1f, 2.6f, 1.8f), Quaternion.identity, new Vector3(0.48f, 5.2f, 0.48f), enemyMaterial);
            CreateBox(root, "Crane_Arm", new Vector3(-1.65f, 5f, 1.8f), Quaternion.Euler(0f, 0f, -8f), new Vector3(3.3f, 0.28f, 0.28f), enemyMaterial);
            CreatePointLight(root, "Gold_Worksite_Light", new Vector3(0f, 3.1f, 0f), new Color(1f, 0.62f, 0.1f, 1f), 1.2f, 18f);
        }
        else
        {
            CreateBox(root, "Wind_Farm_Base", new Vector3(0f, 0.25f, 0f), Quaternion.identity, new Vector3(7.8f, 0.48f, 6.8f), duneRockMaterial, true);
            for (int turbine = -1; turbine <= 1; turbine++)
            {
                float x = turbine * 2.2f;
                CreateBox(root, "Windmill_Tower_" + turbine, new Vector3(x, 3.3f, 0f), Quaternion.identity, new Vector3(0.42f, 6.1f, 0.42f), contestedMaterial, true);
                CreateCylinder(root, "Windmill_Hub_" + turbine, new Vector3(x, 6.5f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.72f, 0.34f, 0.72f), contestedMaterial);
                for (int blade = 0; blade < 4; blade++)
                    CreateBox(root, "Windmill_Blade_" + turbine + "_" + blade, new Vector3(x, 6.5f, 0f), Quaternion.Euler(0f, 0f, blade * 90f + index * 13f), new Vector3(0.14f, 2.1f, 0.28f), contestedMaterial);
                CreateBox(root, "Wind_Generator_" + turbine, new Vector3(x, 0.82f, 1.4f), Quaternion.identity, new Vector3(1.15f, 0.85f, 1.15f), contestedMaterial, true);
            }
            CreatePointLight(root, "Wind_Generator_Light", new Vector3(0f, 3.2f, 0f), new Color(0.25f, 0.9f, 1f, 1f), 1.15f, 22f);
        }

        TextMesh label = new GameObject("Resource_Floating_Label").AddComponent<TextMesh>();
        label.transform.SetParent(root, false);
        label.transform.localPosition = new Vector3(0f, node.kind == ResourceKind.Wind ? 6.1f : 3.4f, 0f);
        label.transform.localRotation = Quaternion.Euler(64f, 0f, 0f);
        label.text = node.kind == ResourceKind.Gold ? "GOLD VEIN" : node.kind == ResourceKind.Wind ? "WIND FARM" : "QUICKSAND";
        label.characterSize = 0.42f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = node.kind == ResourceKind.Gold ? new Color(1f, 0.78f, 0.22f, 1f) : node.kind == ResourceKind.Wind ? new Color(0.45f, 1f, 1f, 1f) : new Color(1f, 0.76f, 0.42f, 1f);

        node.markerRenderer = root.GetComponentInChildren<Renderer>();
        if (node.markerRenderer != null)
            node.markerRenderer.sharedMaterial = material;
    }

    private void EnsureAdditionalResourcePoints(Transform resourceRoot)
    {
        const int desiredCount = 21;
        if (resourceRoot.childCount >= desiredCount)
            return;

        int start = resourceRoot.childCount;
        for (int i = start; i < desiredCount; i++)
        {
            int slot = i - 12;
            ResourceKind kind = (slot % 3 == 0) ? ResourceKind.Sand : (slot % 3 == 1 ? ResourceKind.Gold : ResourceKind.Wind);
            GameObject node = new GameObject("Runtime_Resource_" + kind + "_" + (i + 1));
            node.transform.SetParent(resourceRoot, false);
            node.transform.position = Vector3.zero;
        }
    }

    private void CreatePathLandmarks()
    {
        Vector3[] pathPoints =
        {
            new Vector3(-510f, 0f, -420f),
            new Vector3(-280f, 0f, -100f),
            new Vector3(120f, 0f, 160f),
            new Vector3(380f, 0f, 380f)
        };
        for (int i = 0; i < pathPoints.Length; i++)
        {
            float y = ComputeDuneHeight(pathPoints[i].x, pathPoints[i].z) + 0.2f;
            Transform landmark = new GameObject("Path_Landmark_Tower_" + i).transform;
            landmark.SetParent(duneWorldRoot, false);
            landmark.position = new Vector3(pathPoints[i].x, y, pathPoints[i].z);
            CreateBox(landmark, "Watchtower_Base", new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(2.6f, 2.2f, 2.6f), duneRockMaterial);
            CreateBox(landmark, "Watchtower_Pillar", new Vector3(0f, 4.5f, 0f), Quaternion.identity, new Vector3(0.6f, 6.4f, 0.6f), duneRidgeMaterial);
            CreateCylinder(landmark, "Watchtower_Ring", new Vector3(0f, 7.6f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.8f, 0.1f, 1.8f), duneRidgeMaterial);
            CreatePointLight(landmark, "Watchtower_Beacon", new Vector3(0f, 8.2f, 0f), new Color(1f, 0.72f, 0.22f, 1f), 0.6f + i * 0.1f, 16f + i * 2f);

            TextMesh label = new GameObject("Tower_Label_" + i).AddComponent<TextMesh>();
            label.transform.SetParent(landmark, false);
            label.transform.localPosition = new Vector3(0f, 10.4f, 0f);
            label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
            string[] labels = { "OUTPOST TORA", "MARCHENKO CROSSING", "GOLDEN GATE", "MANDARINKA APPROACH" };
            label.text = labels[i];
            label.characterSize = 0.5f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 0.72f, 0.32f, 1f);
        }
    }

    private void CreateBorderCausewayHints()
    {
        for (int i = 0; i < 16; i++)
        {
            float z = -mapHalfSize + 70f + i * ((mapHalfSize * 2f - 140f) / 15f);
            float x = Mathf.Sin(i * 0.7f) * 8f;
            float y = ComputeDuneHeight(x, z) + 0.16f;
            CreateBox(duneWorldRoot, "Buried_Earth_Border_Causeway_Slab_" + i, new Vector3(x, y, z), Quaternion.Euler(0f, Mathf.Sin(i) * 9f, 0f), new Vector3(10f, 0.12f, 3.4f), duneRockMaterial);
        }
    }

    private void ApplyProceduralDuneTextures()
    {
        duneSandTexture = Resources.Load<Texture2D>("SandRunners/Textures/SR_NeutralSand");
        if (duneSandTexture == null)
            duneSandTexture = CreateDuneTexture("New_Galikarnass_Neutral_Sand_Texture", new Color(0.36f, 0.32f, 0.25f, 1f), new Color(0.69f, 0.61f, 0.47f, 1f), 256, 1f);
        duneRidgeTexture = CreateDuneTexture("New_Galikarnass_Ridge_Texture", new Color(0.43f, 0.39f, 0.31f, 1f), new Color(0.78f, 0.7f, 0.55f, 1f), 128, 1.45f);
        AssignTextureToMaterial(duneSandMaterial, duneSandTexture, new Vector2(18f, 18f));
        AssignTextureToMaterial(duneRidgeMaterial, duneRidgeTexture, new Vector2(6f, 1.5f));
    }

    private Texture2D CreateDuneTexture(string textureName, Color low, Color high, int size, float ridgeStrength)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 6;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float grain = Mathf.PerlinNoise(u * 22f + 9.4f, v * 22f + 71.1f);
                float soft = Mathf.PerlinNoise(u * 5.2f + 18.7f, v * 5.2f + 4.3f);
                float windLines = Mathf.Sin((u * 28f + v * 9f + soft * 2.7f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float mix = Mathf.Clamp01(soft * 0.55f + grain * 0.25f + windLines * 0.2f * ridgeStrength);
                Color color = Color.Lerp(low, high, mix);
                color *= 0.92f + grain * 0.16f;
                color.a = 1f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    private void AssignTextureToMaterial(Material material, Texture2D texture, Vector2 tiling)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", tiling);
        }
    }
}
