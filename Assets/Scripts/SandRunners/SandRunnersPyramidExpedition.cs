using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PyramidObstacleKind { Static, Breakable, HeavyBreakable, Protected }

public sealed class PyramidObstacle : MonoBehaviour
{
    public PyramidObstacleKind kind = PyramidObstacleKind.Breakable;
    public float durability = 100f;
    public float requiredImpact = 3f;
    public string diplomaticOwner = "";
    public bool destroyed;

    public bool ApplyImpact(float impact, out float speedRetention)
    {
        speedRetention = 0f;
        if (destroyed) return true;
        if (kind == PyramidObstacleKind.Static) return false;
        if (kind == PyramidObstacleKind.Protected && impact < requiredImpact * 1.5f) return false;
        durability -= Mathf.Max(0f, impact - requiredImpact * 0.35f) * (kind == PyramidObstacleKind.HeavyBreakable ? 8f : 18f);
        speedRetention = kind == PyramidObstacleKind.Breakable ? 0.72f : 0.3f;
        if (impact < requiredImpact || durability > 0f) return false;
        destroyed = true;
        gameObject.SetActive(false);
        return true;
    }
}

public partial class SandRunnersPrototype
{
    private enum PyramidMarchMode { Economy, Normal, Forced }
    private enum PyramidSurfaceKind { Stone, Sand, DeepSand, Slope }
    private enum RoadActivityKind { DamagedCaravan, Ambush, ResourceFind, RepairRequest, TradeStop, Rockfall, WeatherWarning }

    private sealed class WindDriveProfile
    {
        public float capacity = 420f;
        public float efficiency = 1f;
        public float boostDrain = 15f;
        public float slopeDrain = 7f;
        public float deepSandDrain = 4f;
        public float ramPower = 1f;
    }

    private sealed class PyramidTraversalState
    {
        public PyramidMarchMode marchMode = PyramidMarchMode.Normal;
        public PyramidSurfaceKind surface = PyramidSurfaceKind.Stone;
        public float slope;
        public float speedModifier = 1f;
        public float windDrainPerSecond;
        public float predictedWindCost;
        public string routeHint = "";
    }

    private sealed class RoadActivity
    {
        public RoadActivityKind kind;
        public Vector3 position;
        public float triggerRadius;
        public bool triggered;
        public bool completed;
        public Transform marker;
    }

    [Header("Pyramid expedition")]
    public float windCapacity = 420f;
    public float windEfficiency = 1f;
    public float windPressure = 1f;

    private WindDriveProfile windDriveProfile;
    private PyramidTraversalState pyramidTraversal;
    private readonly List<RoadActivity> roadActivities = new List<RoadActivity>();
    private Transform gobiRouteRoot;
    private float roadDirectorQuietTime;
    private float roadDirectorDistance;
    private Vector3 roadDirectorPreviousPosition;
    private bool firstRoadActivityTriggered;
    private bool oasisDefenseTriggered;
    private bool oasisRepairCompleted;
    private Transform starterOasis;
    private NeutralSettlement starterOasisSettlement;
    private Material gobiRockMaterial;
    private Material gobiOasisMaterial;
    private Material gobiTentMaterial;
    private float legacyWallRegistrationTimer = 0.5f;
    private int legacyWallsRegistered;

    private void InitializePyramidExpedition()
    {
        windDriveProfile = new WindDriveProfile { capacity = Mathf.Max(250f, windCapacity), efficiency = Mathf.Max(0.25f, windEfficiency) };
        windCapacity = windDriveProfile.capacity;
        wind = Mathf.Clamp(wind, 0f, windCapacity);
        pyramidTraversal = new PyramidTraversalState();
        roadDirectorPreviousPosition = battlePyramid != null ? battlePyramid.position : Vector3.zero;
        BuildGobiStarterRoute();
    }

    private void UpdatePyramidExpedition(float dt)
    {
        if (windDriveProfile == null || pyramidTraversal == null) return;
        windDriveProfile.capacity = Mathf.Max(50f, windCapacity);
        windDriveProfile.efficiency = Mathf.Max(0.25f, windEfficiency);
        wind = Mathf.Clamp(wind, 0f, windDriveProfile.capacity);
        UpdateTraversalState(dt);
        UpdateRoadActivityDirector(dt);
        UpdateStarterOasisStory();
        legacyWallRegistrationTimer -= dt;
        if (legacyWallRegistrationTimer <= 0f && legacyWallsRegistered < 12)
        {
            legacyWallRegistrationTimer = 1f;
            UpgradeLegacyStarterWalls(new Vector3(-627f, 0f, -589f));
        }
    }

    private void UpdateTraversalState(float dt)
    {
        if (battlePyramid == null) return;
        Vector3 p = battlePyramid.position;
        float h = GetPlayableGroundHeight(p);
        float hx = GetPlayableGroundHeight(p + Vector3.right * 8f);
        float hz = GetPlayableGroundHeight(p + Vector3.forward * 8f);
        pyramidTraversal.slope = Mathf.Atan(Mathf.Sqrt(Mathf.Pow(hx - h, 2f) + Mathf.Pow(hz - h, 2f)) / 8f) * Mathf.Rad2Deg;

        float deepNoise = Mathf.PerlinNoise((p.x + 1800f) * 0.0045f, (p.z + 1800f) * 0.0045f);
        bool stoneRegion = p.x < -470f || Mathf.Abs(p.z - p.x * 0.35f) < 95f;
        if (pyramidTraversal.slope > 10f) pyramidTraversal.surface = PyramidSurfaceKind.Slope;
        else if (!stoneRegion && deepNoise > 0.64f) pyramidTraversal.surface = PyramidSurfaceKind.DeepSand;
        else if (stoneRegion) pyramidTraversal.surface = PyramidSurfaceKind.Stone;
        else pyramidTraversal.surface = PyramidSurfaceKind.Sand;

        switch (pyramidTraversal.surface)
        {
            case PyramidSurfaceKind.Stone: pyramidTraversal.speedModifier = 1.05f; break;
            case PyramidSurfaceKind.Sand: pyramidTraversal.speedModifier = 0.94f; break;
            case PyramidSurfaceKind.DeepSand: pyramidTraversal.speedModifier = 0.68f; break;
            default: pyramidTraversal.speedModifier = Mathf.Lerp(0.78f, 0.48f, Mathf.InverseLerp(10f, 25f, pyramidTraversal.slope)); break;
        }

        if (pyramidTraversal.marchMode == PyramidMarchMode.Economy) pyramidTraversal.speedModifier *= 0.72f;
        if (pyramidTraversal.marchMode == PyramidMarchMode.Forced && wind > 1f) pyramidTraversal.speedModifier *= 1.32f;

        float drain = 0f;
        if (pyramidTraversal.marchMode == PyramidMarchMode.Forced) drain += windDriveProfile.boostDrain;
        if (pyramidTraversal.surface == PyramidSurfaceKind.DeepSand) drain += windDriveProfile.deepSandDrain;
        if (pyramidTraversal.surface == PyramidSurfaceKind.Slope && pyramidTraversal.slope > 12f) drain += windDriveProfile.slopeDrain * Mathf.InverseLerp(10f, 25f, pyramidTraversal.slope);
        pyramidTraversal.windDrainPerSecond = drain / windDriveProfile.efficiency;
        pyramidTraversal.predictedWindCost = hasCommandDestination ? pyramidTraversal.windDrainPerSecond * FlatDistance(p, commandDestination) / Mathf.Max(1f, moveSpeed * pyramidTraversal.speedModifier) : 0f;

        if (IsKeyPressed(Key.LeftShift) || IsKeyPressed(Key.RightShift)) pyramidTraversal.marchMode = PyramidMarchMode.Forced;
        else if (IsKeyPressed(Key.LeftCtrl) || IsKeyPressed(Key.RightCtrl)) pyramidTraversal.marchMode = PyramidMarchMode.Economy;
        else pyramidTraversal.marchMode = PyramidMarchMode.Normal;

        if (pyramidVelocity.sqrMagnitude > 0.1f && drain > 0f)
            wind = Mathf.Max(0f, wind - drain * dt);
    }

    private float GetPyramidTraversalSpeedMultiplier()
    {
        return pyramidTraversal != null ? pyramidTraversal.speedModifier : 1f;
    }

    private float GetPyramidTraversalAccelerationMultiplier()
    {
        if (pyramidTraversal == null) return 1f;
        if (pyramidTraversal.surface == PyramidSurfaceKind.DeepSand) return 0.62f;
        if (pyramidTraversal.surface == PyramidSurfaceKind.Slope) return 0.72f;
        return pyramidTraversal.surface == PyramidSurfaceKind.Stone ? 1.08f : 0.92f;
    }

    private bool TryTraversePyramidTo(Vector3 desiredPosition, float dt, out Vector3 resolvedPosition)
    {
        resolvedPosition = desiredPosition;
        if (battlePyramid == null) return true;
        Vector3 delta = desiredPosition - battlePyramid.position;
        if (delta.sqrMagnitude < 0.0001f) return true;

        Vector3 center = desiredPosition + Vector3.up * 5f;
        Vector3 half = new Vector3(8.5f, 5.5f, 11f);
        Collider[] hits = Physics.OverlapBox(center, half, battlePyramid.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null || c.transform == battlePyramid || c.transform.IsChildOf(battlePyramid)) continue;
            PyramidObstacle obstacle = c.GetComponentInParent<PyramidObstacle>();
            if (obstacle == null || obstacle.destroyed) continue;
            float impact = pyramidVelocity.magnitude * windDriveProfile.ramPower * (pyramidTraversal != null && pyramidTraversal.marchMode == PyramidMarchMode.Forced ? 1.65f : 1f);
            bool broken = obstacle.ApplyImpact(impact, out float retention);
            if (broken)
            {
                SpawnPyramidObstacleBreakVfx(c.transform.position, obstacle.kind);
                pyramidVelocity *= retention;
                lastEvent = obstacle.kind == PyramidObstacleKind.Protected
                    ? "Protected structure crushed. Neutral relations deteriorate."
                    : "The battle pyramid smashes through the obstacle.";
                return true;
            }

            resolvedPosition = battlePyramid.position;
            pyramidVelocity *= obstacle.kind == PyramidObstacleKind.Static ? 0f : 0.18f;
            lastEvent = obstacle.kind == PyramidObstacleKind.Static
                ? "Impassable Gobi rock. Choose another route."
                : obstacle.kind == PyramidObstacleKind.Protected
                    ? "Emergency brake: allied structure ahead. Forced march can override it."
                    : "Obstacle resists the pyramid. Use forced march or weapons.";
            return false;
        }
        return true;
    }

    private void SpawnPyramidObstacleBreakVfx(Vector3 position, PyramidObstacleKind kind)
    {
        Material m = kind == PyramidObstacleKind.Protected ? gobiTentMaterial : gobiRockMaterial;
        for (int i = 0; i < 8; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Pyramid impact debris";
            shard.transform.position = position + Random.insideUnitSphere * 3f + Vector3.up * 2f;
            shard.transform.localScale = Vector3.one * Random.Range(0.4f, 1.3f);
            Renderer r = shard.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = m;
            Rigidbody body = shard.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            body.AddExplosionForce(180f, position, 12f, 4f);
            Destroy(shard, 5f);
        }
    }

    private void BuildGobiStarterRoute()
    {
        GameObject old = GameObject.Find("SandRunners_Gobi_Starter_Route");
        if (old != null) Destroy(old);
        gobiRouteRoot = new GameObject("SandRunners_Gobi_Starter_Route").transform;

        gobiRockMaterial = CreateMaterial("Gobi iron rock", new Color(0.25f, 0.16f, 0.105f));
        gobiOasisMaterial = CreateMaterial("Oasis living green", new Color(0.08f, 0.31f, 0.16f));
        gobiTentMaterial = CreateMaterial("Caravan ochre cloth", new Color(0.48f, 0.21f, 0.09f));

        Vector3 start = new Vector3(-627f, 0f, -589f);
        BuildRockGate(start + new Vector3(135f, 0f, 105f));
        BuildDryRiver(start + new Vector3(145f, 0f, 125f));
        BuildOldWall(start + new Vector3(235f, 0f, 210f));
        BuildStarterOasis(start + new Vector3(185f, 0f, 155f));
        BuildWindPass(start + new Vector3(300f, 0f, 285f));
        BuildRoadVisualDetails(start);
        CreateRoadActivities(start);
        UpgradeLegacyStarterWalls(start);
        ClearResourceNodesFromExpeditionObstacles();
    }

    private void BuildRockGate(Vector3 center)
    {
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = center + new Vector3(side * (32f + i * 7f), 0f, i * 18f - 45f);
                CreateRouteObstacle("Gobi basalt pillar", p, new Vector3(8f + i * 1.2f, 12f + i * 2.6f, 11f + i), PyramidObstacleKind.Static, 9999f, 99f, gobiRockMaterial);
            }
    }

    private void BuildDryRiver(Vector3 center)
    {
        for (int i = 0; i < 12; i++)
        {
            float a = i / 11f;
            Vector3 p = center + new Vector3(Mathf.Sin(a * 8f) * 10f, 0f, (a - 0.5f) * 130f);
            Transform stone = CreateHorusPrimitive(PrimitiveType.Sphere, "Dry river stone", gobiRouteRoot, Vector3.zero, new Vector3(3f, 0.7f, 2f), gobiRockMaterial);
            stone.position = GroundPoint(p) + Vector3.up * 0.35f;
        }
    }

    private void BuildOldWall(Vector3 center)
    {
        for (int i = -5; i <= 5; i++)
        {
            Vector3 p = center + new Vector3(i * 8f, 0f, 0f);
            CreateRouteObstacle(i == 0 ? "Old caravan gate" : "Old desert wall", p,
                new Vector3(7.5f, i == 0 ? 9f : 6f, 3f),
                i == 0 ? PyramidObstacleKind.HeavyBreakable : PyramidObstacleKind.Breakable,
                i == 0 ? 150f : 45f, i == 0 ? 5.5f : 3.2f, duneRidgeMaterial);
        }
    }

    private void BuildStarterOasis(Vector3 center)
    {
        center = GroundPoint(center);
        starterOasis = new GameObject("Little Oasis of the First Road").transform;
        starterOasis.SetParent(gobiRouteRoot, false);
        starterOasis.position = center;
        CreateHorusPrimitive(PrimitiveType.Cylinder, "Oasis pool", starterOasis, new Vector3(0f, 0.15f, 0f), new Vector3(13f, 0.12f, 9f), gobiOasisMaterial);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Vector3 local = new Vector3(Mathf.Cos(a) * 22f, 1.5f, Mathf.Sin(a) * 17f);
            Transform tent = CreateHorusPrimitive(PrimitiveType.Cube, "Caravan tent", starterOasis, local, new Vector3(6f, 3f, 4f), gobiTentMaterial);
            PyramidObstacle po = tent.gameObject.AddComponent<PyramidObstacle>();
            po.kind = PyramidObstacleKind.Protected; po.durability = 35f; po.requiredImpact = 4f; po.diplomaticOwner = "FIRST ROAD OASIS";
            BoxCollider box = tent.gameObject.AddComponent<BoxCollider>();
        }
        Transform pump = CreateHorusPrimitive(PrimitiveType.Cylinder, "Damaged oasis pump", starterOasis, new Vector3(5f, 2.5f, -4f), new Vector3(2f, 3f, 2f), horusDarkMaterial != null ? horusDarkMaterial : gobiRockMaterial);
        PyramidObstacle protectedPump = pump.gameObject.AddComponent<PyramidObstacle>();
        protectedPump.kind = PyramidObstacleKind.Protected; protectedPump.durability = 120f; protectedPump.requiredImpact = 9f; protectedPump.diplomaticOwner = "FIRST ROAD OASIS";
        pump.gameObject.AddComponent<CapsuleCollider>();

        CreateNeutralSettlement("First_Road_Oasis_Settlement", "FIRST ROAD OASIS",
            NeutralFactionKind.BlueTraders, center + new Vector3(32f, 0f, -18f),
            traderBlueMaterial != null ? traderBlueMaterial : gobiTentMaterial, 560f);
        if (neutralSettlements.Count > 0)
            starterOasisSettlement = neutralSettlements[neutralSettlements.Count - 1];
    }

    private void BuildWindPass(Vector3 center)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 left = center + new Vector3(-38f - i * 4f, 0f, i * 18f);
            Vector3 right = center + new Vector3(38f + i * 4f, 0f, i * 18f);
            CreateRouteObstacle("Wind pass cliff L", left, new Vector3(22f, 20f + i * 2f, 22f), PyramidObstacleKind.Static, 9999f, 99f, gobiRockMaterial);
            CreateRouteObstacle("Wind pass cliff R", right, new Vector3(22f, 20f + i * 2f, 22f), PyramidObstacleKind.Static, 9999f, 99f, gobiRockMaterial);
        }
    }

    private void CreateRouteObstacle(string name, Vector3 position, Vector3 scale, PyramidObstacleKind kind, float durability, float impact, Material material)
    {
        position = GroundPoint(position) + Vector3.up * scale.y * 0.5f;
        GameObject go;
        if (kind == PyramidObstacleKind.Static)
        {
            go = CreateProceduralGobiRock(name, position, scale, material);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(gobiRouteRoot, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(-5f, 5f), 0f);
            Renderer blockRenderer = go.GetComponent<Renderer>();
            if (blockRenderer != null) blockRenderer.sharedMaterial = material;
        }
        PyramidObstacle obstacle = go.AddComponent<PyramidObstacle>();
        obstacle.kind = kind; obstacle.durability = durability; obstacle.requiredImpact = impact;
    }

    private GameObject CreateProceduralGobiRock(string name, Vector3 position, Vector3 scale, Material material)
    {
        const int sides = 9;
        const int levels = 4;
        Vector3[] vertices = new Vector3[sides * levels + 2];
        int seed = Mathf.Abs(name.GetHashCode() ^ Mathf.RoundToInt(position.x * 13f + position.z * 7f));
        for (int level = 0; level < levels; level++)
        {
            float y = level / (float)(levels - 1);
            float taper = Mathf.Lerp(1f, 0.34f, y);
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                float noise = 0.78f + Mathf.PerlinNoise(seed * 0.013f + side * 0.31f, level * 0.47f) * 0.42f;
                float leanX = (y - 0.5f) * (((seed % 7) - 3) * 0.035f);
                float leanZ = (y - 0.5f) * ((((seed / 7) % 7) - 3) * 0.035f);
                vertices[level * sides + side] = new Vector3(Mathf.Cos(angle) * taper * noise + leanX, y - 0.5f, Mathf.Sin(angle) * taper * noise + leanZ);
            }
        }
        vertices[sides * levels] = new Vector3(0f, -0.5f, 0f);
        vertices[sides * levels + 1] = new Vector3(0f, 0.52f, 0f);
        List<int> triangles = new List<int>();
        for (int level = 0; level < levels - 1; level++)
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                int a = level * sides + side, b = level * sides + next;
                int c = (level + 1) * sides + side, d = (level + 1) * sides + next;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        int bottom = sides * levels, top = bottom + 1;
        for (int side = 0; side < sides; side++)
        {
            int next = (side + 1) % sides;
            triangles.Add(bottom); triangles.Add(next); triangles.Add(side);
            int upper = (levels - 1) * sides;
            triangles.Add(top); triangles.Add(upper + side); triangles.Add(upper + next);
        }
        Mesh mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = triangles.ToArray() };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GameObject go = new GameObject(name);
        go.transform.SetParent(gobiRouteRoot, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 180f), Random.Range(-4f, 4f));
        MeshFilter filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        MeshCollider collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = mesh;
        return go;
    }

    private void BuildRoadVisualDetails(Vector3 start)
    {
        Material roadMaterial = CreateMaterial("Gobi caravan track", new Color(0.31f, 0.22f, 0.15f));
        Vector3 direction = new Vector3(1f, 0f, 0.82f).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        for (int i = 0; i < 28; i++)
        {
            Vector3 center = start + direction * (i * 17f + 20f);
            for (int track = -1; track <= 1; track += 2)
            {
                Transform mark = CreateHorusPrimitive(PrimitiveType.Cube, "Caravan wheel track", gobiRouteRoot, Vector3.zero,
                    new Vector3(0.42f, 0.025f, 7.4f), roadMaterial);
                mark.position = GroundPoint(center + side * track * 4.2f) + Vector3.up * 0.08f;
                mark.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        Vector3 caravanPosition = GroundPoint(start + new Vector3(125f, 0f, 110f));
        Transform caravan = new GameObject("Visible damaged caravan").transform;
        caravan.SetParent(gobiRouteRoot, false); caravan.position = caravanPosition;
        Transform body = CreateHorusPrimitive(PrimitiveType.Cube, "Broken caravan body", caravan, new Vector3(0f, 2.4f, 0f), new Vector3(7f, 2.2f, 11f), gobiTentMaterial);
        body.localRotation = Quaternion.Euler(0f, 28f, 6f);
        for (int i = -1; i <= 1; i += 2)
        {
            Transform wheel = CreateHorusPrimitive(PrimitiveType.Cylinder, "Broken wheel", caravan, new Vector3(i * 4f, 1.1f, 2f), new Vector3(2.2f, 0.45f, 2.2f), gobiRockMaterial);
            wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        CreateHorusPrimitive(PrimitiveType.Cube, "Scattered cargo", caravan, new Vector3(7f, 0.8f, -2f), new Vector3(2f, 1.4f, 2f), duneRidgeMaterial);

        if (starterOasis != null)
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                CreateOasisPalm(starterOasis, new Vector3(Mathf.Cos(a) * 17f, 0f, Mathf.Sin(a) * 13f));
            }
    }

    private void CreateOasisPalm(Transform parent, Vector3 local)
    {
        Transform palm = new GameObject("Oasis palm").transform;
        palm.SetParent(parent, false); palm.localPosition = local;
        CreateHorusPrimitive(PrimitiveType.Cylinder, "Palm trunk", palm, new Vector3(0f, 4.2f, 0f), new Vector3(0.65f, 4.2f, 0.65f), gobiTentMaterial);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Transform leaf = CreateHorusPrimitive(PrimitiveType.Cube, "Palm leaf", palm,
                new Vector3(Mathf.Cos(a) * 2.4f, 8.5f, Mathf.Sin(a) * 2.4f), new Vector3(0.7f, 0.16f, 5f), gobiOasisMaterial);
            leaf.localRotation = Quaternion.Euler(18f, -a * Mathf.Rad2Deg, 0f);
        }
    }

    private Vector3 GroundPoint(Vector3 p) { p.y = GetPlayableGroundHeight(p); return p; }

    private void ClearResourceNodesFromExpeditionObstacles()
    {
        PyramidObstacle[] obstacles = Object.FindObjectsByType<PyramidObstacle>(FindObjectsSortMode.None);
        for (int n = 0; n < resourceNodes.Count; n++)
        {
            ResourceNode node = resourceNodes[n];
            if (node == null || node.transform == null) continue;
            Vector3 original = node.transform.position;
            bool blocked = false;
            for (int i = 0; i < obstacles.Length; i++)
            {
                PyramidObstacle obstacle = obstacles[i];
                if (obstacle != null && obstacle.kind == PyramidObstacleKind.Static &&
                    FlatDistance(original, obstacle.transform.position) < 32f)
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked) continue;
            Vector3 roadDirection = new Vector3(1f, 0f, 0.82f).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, roadDirection) * (n % 2 == 0 ? 52f : -52f);
            Vector3 moved = original + side;
            moved.y = GetPlayableGroundHeight(moved) + 0.25f;
            node.transform.position = moved;
        }
    }

    private void UpgradeLegacyStarterWalls(Vector3 start)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            string n = renderer.name.ToLowerInvariant();
            bool legacyBarrier = n.Contains("chokepoint_boulder_barrier") || n.Contains("chokepoint_wall");
            if (!legacyBarrier && FlatDistance(renderer.transform.position, start) > 430f) continue;
            if (!legacyBarrier && !n.Contains("wall") && !n.Contains("barrier") && !n.Contains("causeway")) continue;

            // Legacy chokepoint barriers can be created after the first expedition setup.
            // Give their rendered object a concrete collision surface before checking for
            // an existing parent obstacle, so repeat registration is safe and complete.
            Collider col = renderer.GetComponent<Collider>();
            if (col == null) col = renderer.gameObject.AddComponent<BoxCollider>();
            if (renderer.GetComponentInParent<PyramidObstacle>() != null) continue;
            PyramidObstacle obstacle = renderer.gameObject.AddComponent<PyramidObstacle>();
            obstacle.kind = PyramidObstacleKind.Breakable;
            obstacle.durability = 55f;
            obstacle.requiredImpact = 3.4f;
            legacyWallsRegistered++;
        }
    }

    private void CreateRoadActivities(Vector3 start)
    {
        roadActivities.Clear();
        AddRoadActivity(RoadActivityKind.WeatherWarning, start + new Vector3(45f, 0f, 38f), 55f);
        AddRoadActivity(RoadActivityKind.DamagedCaravan, start + new Vector3(125f, 0f, 110f), 65f);
        AddRoadActivity(RoadActivityKind.ResourceFind, start + new Vector3(165f, 0f, 135f), 48f);
        AddRoadActivity(RoadActivityKind.RepairRequest, start + new Vector3(185f, 0f, 155f), 78f);
        AddRoadActivity(RoadActivityKind.Ambush, start + new Vector3(225f, 0f, 205f), 65f);
        AddRoadActivity(RoadActivityKind.Rockfall, start + new Vector3(265f, 0f, 245f), 55f);
        AddRoadActivity(RoadActivityKind.TradeStop, start + new Vector3(310f, 0f, 290f), 65f);
    }

    private void AddRoadActivity(RoadActivityKind kind, Vector3 position, float radius)
    {
        position = GroundPoint(position);
        Transform marker = new GameObject("Road activity - " + kind).transform;
        marker.SetParent(gobiRouteRoot, true); marker.position = position;
        roadActivities.Add(new RoadActivity { kind = kind, position = position, triggerRadius = radius, marker = marker });
    }

    private void UpdateRoadActivityDirector(float dt)
    {
        if (battlePyramid == null || guidedMissileActive || gunnerSide != 0) return;
        float moved = FlatDistance(battlePyramid.position, roadDirectorPreviousPosition);
        roadDirectorDistance += moved;
        roadDirectorQuietTime += moved > 0.05f ? dt : dt * 0.25f;
        roadDirectorPreviousPosition = battlePyramid.position;
        bool combatBusy = enemies.Count > 0 && FindNearestEnemy(battlePyramid.position, 130f) != null;
        if (combatBusy) return;

        for (int i = 0; i < roadActivities.Count; i++)
        {
            RoadActivity a = roadActivities[i];
            if (a.triggered || FlatDistance(battlePyramid.position, a.position) > a.triggerRadius) continue;
            TriggerRoadActivity(a);
            return;
        }

        if (!firstRoadActivityTriggered && roadDirectorQuietTime >= 55f && roadActivities.Count > 0)
            TriggerRoadActivity(roadActivities[0]);
    }

    private void TriggerRoadActivity(RoadActivity activity)
    {
        if (activity == null || activity.triggered) return;
        activity.triggered = true;
        firstRoadActivityTriggered = true;
        roadDirectorQuietTime = 0f;
        switch (activity.kind)
        {
            case RoadActivityKind.WeatherWarning:
                lastEvent = "ROAD RADIO: crosswind rises beyond the stone gate. Forced march will consume reserves.";
                ShowBanner("GOBI CROSSWIND // CHECK WIND RESERVE", 4f); break;
            case RoadActivityKind.DamagedCaravan:
                DamageNearestCaravanOrSettlement();
                lastEvent = "CARAVAN DISTRESS: running gear destroyed. Survivors request royal repair.";
                ShowBanner("DAMAGED CARAVAN ON THE FIRST ROAD", 4f); break;
            case RoadActivityKind.ResourceFind:
                wind = Mathf.Min(windCapacity, wind + 55f); gold += 22f;
                lastEvent = "The scouts recover sealed wind cells and caravan gold.";
                ShowBanner("ROAD CACHE // +WIND +GOLD", 3f); activity.completed = true; break;
            case RoadActivityKind.RepairRequest:
                DamageNearestCaravanOrSettlement();
                lastEvent = "FIRST ROAD OASIS: our pump is failing. Repair it and the road will remember.";
                ShowBanner("OASIS REPAIR REQUEST", 4f); break;
            case RoadActivityKind.Ambush:
                lastEvent = "AMBUSH WARNING: raiders move among the old walls.";
                ShowBanner("RAIDERS AT THE OLD WALL", 3f);
                break;
            case RoadActivityKind.Rockfall:
                lastEvent = "The canyon route is blocked. Clear the rubble or spend wind on the pass.";
                ShowBanner("ROCKFALL // ROUTE CHOICE", 4f); break;
            case RoadActivityKind.TradeStop:
                windCapacity += 35f; wind = Mathf.Min(windCapacity, wind + 35f);
                lastEvent = "Caravan engineers expand the pyramid wind reservoirs.";
                ShowBanner("CARAVAN UPGRADE // WIND CAPACITY +35", 4f); activity.completed = true; break;
        }
    }

    private void DamageNearestCaravanOrSettlement()
    {
        if (starterOasisSettlement != null)
            starterOasisSettlement.health = Mathf.Min(starterOasisSettlement.health, starterOasisSettlement.maxHealth * 0.55f);
        else if (tradeCaravans.Count > 0 && tradeCaravans[0] != null)
            tradeCaravans[0].health = Mathf.Min(tradeCaravans[0].health, 95f);
        else if (neutralSettlements.Count > 0 && neutralSettlements[0] != null)
            neutralSettlements[0].health = Mathf.Min(neutralSettlements[0].health, neutralSettlements[0].maxHealth * 0.55f);
        if (!touchOfHorusRevealed) horusRequestScanTimer = 0f;
    }

    private void UpdateStarterOasisStory()
    {
        if (activeHorusRepairRequest != null && activeHorusRepairRequest.completed && !oasisRepairCompleted)
        {
            oasisRepairCompleted = true;
            lastEvent = "The First Road Oasis lives again. Three routes to the plateau are now known.";
            ShowBanner("OASIS RESTORED // THREE ROUTES OPEN", 4f);
        }
        if (oasisRepairCompleted && !oasisDefenseTriggered && battlePyramid != null && starterOasis != null && FlatDistance(battlePyramid.position, starterOasis.position) < 95f)
        {
            oasisDefenseTriggered = true;
            lastEvent = "Oasis scouts report raiders closing through the red rocks.";
            ShowBanner("DEFEND THE FIRST ROAD OASIS", 4f);
        }
    }

    private void DrawPyramidExpeditionHUD()
    {
        if (pyramidTraversal == null || IsGameFlowOverlayBlocking()) return;
        Rect r = new Rect(Screen.width - 430f, 320f, 410f, 124f);
        GUI.Box(r, "PYRAMID EXPEDITION");
        GUI.Label(new Rect(r.x + 12f, r.y + 26f, 390f, 92f),
            "Wind " + Mathf.CeilToInt(wind) + "/" + Mathf.CeilToInt(windCapacity) +
            "   Pressure " + windPressure.ToString("F1") + "   Efficiency " + windEfficiency.ToString("F2") +
            "\nMarch: " + pyramidTraversal.marchMode + "   Surface: " + pyramidTraversal.surface + "   Slope " + pyramidTraversal.slope.ToString("F1") + "°" +
            "\nDrain " + pyramidTraversal.windDrainPerSecond.ToString("F1") + "/s   Route estimate " + Mathf.CeilToInt(pyramidTraversal.predictedWindCost) +
            "\n[Ctrl] economy   [Shift] forced march / ram");
    }

    public string RunPyramidObstacleSmokeTest()
    {
        if (windDriveProfile == null) InitializePyramidExpedition();
        PyramidObstacle[] all = Object.FindObjectsByType<PyramidObstacle>(FindObjectsSortMode.None);
        PyramidObstacle target = null;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name.ToLowerInvariant().Contains("chokepoint_boulder_barrier")) { target = all[i]; break; }
        if (target == null) return "FAIL:no legacy Chokepoint_Boulder_Barrier obstacle";
        Collider collider = target.GetComponent<Collider>();
        if (collider == null) return "FAIL:legacy barrier has no collider";
        if (target.kind != PyramidObstacleKind.Breakable || !Mathf.Approximately(target.requiredImpact, 3.4f))
            return "FAIL:legacy barrier profile is incorrect";
        float before = target.durability;
        bool broken = target.ApplyImpact(12f, out float retention);
        return (target.durability < before ? "PASS" : "FAIL") +
               ": legacy=" + target.name + ", durability=" + before.ToString("F0") + "->" + target.durability.ToString("F0") +
               ", broken=" + broken + ", retention=" + retention.ToString("F2");
    }

    public string RunPyramidExpeditionSmokeTest()
    {
        if (windDriveProfile == null) InitializePyramidExpedition();
        float oldCapacity = windCapacity;
        windCapacity = 777f;
        UpdatePyramidExpedition(0.1f);
        bool capacityPass = Mathf.Approximately(windDriveProfile.capacity, 777f);
        bool routePass = gobiRouteRoot != null && roadActivities.Count >= 7;
        PyramidObstacle[] obstacles = Object.FindObjectsByType<PyramidObstacle>(FindObjectsSortMode.None);
        bool legacyPass = legacyWallsRegistered > 0;
        return (capacityPass && routePass && obstacles.Length > 10 && legacyPass ? "PASS" : "FAIL") +
               ": capacity=" + windDriveProfile.capacity + ", activities=" + roadActivities.Count + ", obstacles=" + obstacles.Length + ", legacyWalls=" + legacyWallsRegistered;
    }
}
