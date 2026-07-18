using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private enum ResourceDevelopmentKind
    {
        GoldenMine,
        ScarabFortress,
        HorusNanoTree,
        FortressCrusherAscension,
        ThothBlessing,
        SalvageHive
    }

    private sealed class ResourceDevelopmentProject
    {
        public ResourceDevelopmentKind kind;
        public int level;
        public float progress;
        public Transform root;
        public float actionTimer;
    }

    private sealed class ResourceDevelopmentState
    {
        public ResourceNode node;
        public readonly Dictionary<ResourceDevelopmentKind, ResourceDevelopmentProject> projects =
            new Dictionary<ResourceDevelopmentKind, ResourceDevelopmentProject>();
        public float convoyTimer = 12f;
        public float defenseTimer;
        public float repairTimer;
        public float stormTimer = 30f;
        public float bloomRepairTimer;
        public float bloomAttackTimer;
        public int bloomEmitterCursor;
        public float hiveDefenseTimer;
        public float hiveSpawnTimer = 10f;
        public float hiveProductionTimer;
    }

    private sealed class ResourceDevelopmentAssignment
    {
        public ResourceNode node;
        public ResourceDevelopmentKind kind;
        public int stationSeed;
    }

    private sealed class MineConvoyState
    {
        public Transform root;
        public ResourceNode source;
        public GoldenStructure depot;
        public ResourceKind kind;
        public float amount;
        public bool returning;
        public float speed;
    }

    private readonly Dictionary<ResourceNode, ResourceDevelopmentState> resourceDevelopmentStates =
        new Dictionary<ResourceNode, ResourceDevelopmentState>();
    private readonly Dictionary<GoldenBuilderAsset, ResourceDevelopmentAssignment> builderDevelopmentOrders =
        new Dictionary<GoldenBuilderAsset, ResourceDevelopmentAssignment>();
    private readonly Dictionary<RunnerUnit, ResourceDevelopmentAssignment> runnerDevelopmentOrders =
        new Dictionary<RunnerUnit, ResourceDevelopmentAssignment>();
    private readonly List<MineConvoyState> mineConvoys = new List<MineConvoyState>();
    private ResourceDevelopmentAssignment horusDevelopmentOrder;
    private GameObject curseHivePanelObject;
    private Text curseHiveStatusText;
    private readonly List<StrategicCanvasButton> curseHiveButtons = new List<StrategicCanvasButton>();

    private void UpdateResourceDevelopment(float dt)
    {
        EnsureResourceDevelopmentStates();

        foreach (KeyValuePair<ResourceNode, ResourceDevelopmentState> pair in resourceDevelopmentStates)
        {
            ResourceDevelopmentState state = pair.Value;
            if (state == null || state.node == null || state.node.transform == null || !state.node.controlled)
                continue;

            UpdateDevelopedMine(state, dt);
            UpdateScarabFortress(state, dt);
            UpdateHorusNanoTree(state, dt);
            state.hiveProductionTimer = Mathf.Max(0f, state.hiveProductionTimer - dt);
            UpdateSalvageHive(state, dt);
        }

        UpdateMineConvoys(dt);
        PruneResourceDevelopmentOrders();
    }

    private void EnsureResourceDevelopmentStates()
    {
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || resourceDevelopmentStates.ContainsKey(node))
                continue;

            ResourceDevelopmentState state = new ResourceDevelopmentState();
            state.node = node;
            resourceDevelopmentStates.Add(node, state);
        }
    }

    private ResourceDevelopmentState GetResourceDevelopmentState(ResourceNode node)
    {
        if (node == null)
            return null;

        ResourceDevelopmentState state;
        if (!resourceDevelopmentStates.TryGetValue(node, out state))
        {
            state = new ResourceDevelopmentState();
            state.node = node;
            resourceDevelopmentStates.Add(node, state);
        }
        return state;
    }

    private ResourceDevelopmentProject GetResourceDevelopmentProject(ResourceNode node, ResourceDevelopmentKind kind)
    {
        ResourceDevelopmentState state = GetResourceDevelopmentState(node);
        if (state == null)
            return null;

        ResourceDevelopmentProject project;
        if (!state.projects.TryGetValue(kind, out project))
        {
            project = new ResourceDevelopmentProject();
            project.kind = kind;
            state.projects.Add(kind, project);
        }
        return project;
    }

    private bool TryIssueResourceDevelopmentOrderAtHit(RaycastHit hit)
    {
        ResourceNode node = FindResourceNodeByHit(hit.transform);
        if (node == null)
            return false;

        int assigned = 0;
        if (touchOfHorus != null && selectedStrategicTransform == touchOfHorus.root)
        {
            horusDevelopmentOrder = CreateResourceDevelopmentAssignment(node, ResourceDevelopmentKind.HorusNanoTree, touchOfHorus.root);
            touchOfHorus.pinnedTarget = null;
            touchOfHorus.hasMoveDestination = false;
            assigned++;
        }

        RunnerUnit directlySelectedDeveloper = null;
        if (selectedStrategicTransform != null)
        {
            for (int i = 0; i < runners.Count; i++)
            {
                RunnerUnit candidate = runners[i];
                ResourceDevelopmentKind directKind;
                if (candidate == null || candidate.transform != selectedStrategicTransform ||
                    !TryGetRunnerDevelopmentKind(candidate, out directKind))
                    continue;

                directlySelectedDeveloper = candidate;
                runnerDevelopmentOrders[candidate] =
                    CreateResourceDevelopmentAssignment(node, directKind, candidate.transform);
                assigned++;
                break;
            }
        }

        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null)
                continue;

            if (squad.builder != null && squad.builder.transform != null)
            {
                builderDevelopmentOrders[squad.builder] =
                    CreateResourceDevelopmentAssignment(node, ResourceDevelopmentKind.GoldenMine, squad.builder.transform);
                squad.orderType = OrderType.Harvest;
                squad.harvestTarget = node;
                squad.attackTarget = null;
                squad.buildTarget = null;
                assigned++;
            }

            for (int u = 0; u < squad.units.Count; u++)
            {
                RunnerUnit runner = squad.units[u];
                ResourceDevelopmentKind kind;
                if (runner == directlySelectedDeveloper || !TryGetRunnerDevelopmentKind(runner, out kind))
                    continue;

                runnerDevelopmentOrders[runner] = CreateResourceDevelopmentAssignment(node, kind, runner.transform);
                squad.orderType = OrderType.Harvest;
                squad.harvestTarget = node;
                squad.attackTarget = null;
                squad.buildTarget = null;
                assigned++;
            }
        }

        if (assigned <= 0)
            return false;

        PlaceCommandMarker(node.transform.position);
        lastEvent = assigned + " developer" + (assigned == 1 ? "" : "s") + " assigned to " + node.label +
            ". They will capture it first, then build permanent infrastructure.";
        ShowBanner("RESOURCE DEVELOPMENT ORDER", 2.2f);
        PlaySandRunnerSound(SandRunnerSound.UiConfirm, node.transform.position, 0.52f);
        return true;
    }

    private ResourceDevelopmentAssignment CreateResourceDevelopmentAssignment(
        ResourceNode node, ResourceDevelopmentKind kind, Transform developer)
    {
        ResourceDevelopmentAssignment assignment = new ResourceDevelopmentAssignment();
        assignment.node = node;
        assignment.kind = kind;
        assignment.stationSeed = developer != null ? Mathf.Abs(developer.GetHashCode()) : (int)kind * 37;
        return assignment;
    }

    private bool TryGetRunnerDevelopmentKind(RunnerUnit runner, out ResourceDevelopmentKind kind)
    {
        kind = ResourceDevelopmentKind.GoldenMine;
        if (runner == null || runner.transform == null || runner.health <= 0f)
            return false;

        if (runner.isSalvageScarab || runner.displayName.Contains("Salvage Scarab"))
        {
            kind = ResourceDevelopmentKind.SalvageHive;
            return true;
        }
        if (runner.displayName.Contains("Scarab Tank"))
        {
            kind = ResourceDevelopmentKind.ScarabFortress;
            return true;
        }
        if (runner.displayName.Contains("Fortress Crusher") && !runner.displayName.Contains("2.0"))
        {
            kind = ResourceDevelopmentKind.FortressCrusherAscension;
            return true;
        }
        if (runner.displayName.Contains("Thoth") && !runner.displayName.Contains("Blessing"))
        {
            kind = ResourceDevelopmentKind.ThothBlessing;
            return true;
        }
        return false;
    }

    private bool UpdateResourceDeveloper(GoldenBuilderAsset builder, float dt)
    {
        if (builder == null)
            return false;

        ResourceDevelopmentAssignment assignment;
        if (!builderDevelopmentOrders.TryGetValue(builder, out assignment))
            return false;
        if (!IsValidDevelopmentAssignment(assignment))
        {
            builderDevelopmentOrders.Remove(builder);
            return false;
        }

        bool stationed = MoveDeveloperToResource(builder.transform, assignment, 6.4f, dt, true);
        if (stationed && assignment.node.controlled)
            ProgressResourceDevelopment(assignment.node, assignment.kind, builder.transform, null, dt);
        return true;
    }

    private bool UpdateResourceDeveloper(RunnerUnit runner, float dt)
    {
        if (runner == null)
            return false;

        ResourceDevelopmentAssignment assignment;
        if (!runnerDevelopmentOrders.TryGetValue(runner, out assignment))
            return false;
        if (!IsValidDevelopmentAssignment(assignment))
        {
            runnerDevelopmentOrders.Remove(runner);
            return false;
        }

        bool stationed = MoveDeveloperToResource(runner.transform, assignment, Mathf.Max(3.2f, runner.speed), dt, false);
        if (stationed && assignment.node.controlled)
            ProgressResourceDevelopment(assignment.node, assignment.kind, runner.transform, runner, dt);
        return true;
    }

    private bool UpdateResourceDeveloper(TouchOfHorusState horus, float dt)
    {
        if (horus == null || horusDevelopmentOrder == null)
            return false;
        if (!IsValidDevelopmentAssignment(horusDevelopmentOrder))
        {
            horusDevelopmentOrder = null;
            return false;
        }

        bool stationed = MoveDeveloperToResource(horus.root, horusDevelopmentOrder, 8.5f, dt, false);
        if (stationed && horusDevelopmentOrder.node.controlled)
            ProgressResourceDevelopment(horusDevelopmentOrder.node, horusDevelopmentOrder.kind, horus.root, null, dt);
        return true;
    }

    private bool IsValidDevelopmentAssignment(ResourceDevelopmentAssignment assignment)
    {
        return assignment != null && assignment.node != null && assignment.node.transform != null;
    }

    private bool MoveDeveloperToResource(
        Transform developer, ResourceDevelopmentAssignment assignment, float speed, float dt, bool orbit)
    {
        if (developer == null || !IsValidDevelopmentAssignment(assignment))
            return false;

        ResourceDevelopmentProject project = GetResourceDevelopmentProject(assignment.node, assignment.kind);
        int maxLevel = GetResourceDevelopmentMaxLevel(assignment.kind);
        float phase = (assignment.stationSeed % 360) * Mathf.Deg2Rad;
        if (orbit && project != null && project.level < maxLevel)
            phase += Time.time * 0.42f;

        float radius = GetResourceDevelopmentStationRadius(assignment.kind);
        Vector3 offset = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase)) * radius;
        Vector3 destination = assignment.node.transform.position + offset;
        destination.y = GetPlayableGroundHeight(destination) + GetDeveloperGroundOffset(assignment.kind);
        Vector3 toTarget = destination - developer.position;
        toTarget.y = 0f;

        if (toTarget.magnitude > 1.25f)
        {
            developer.position += toTarget.normalized * speed * dt;
            RotateFlatToward(developer, toTarget, 220f * dt);
            Vector3 grounded = developer.position;
            grounded.y = GetPlayableGroundHeight(grounded) + GetDeveloperGroundOffset(assignment.kind);
            developer.position = grounded;
            return false;
        }

        Vector3 face = assignment.node.transform.position - developer.position;
        face.y = 0f;
        RotateFlatToward(developer, face, 180f * dt);
        return true;
    }

    private float GetResourceDevelopmentStationRadius(ResourceDevelopmentKind kind)
    {
        switch (kind)
        {
            case ResourceDevelopmentKind.ScarabFortress: return 13f;
            case ResourceDevelopmentKind.HorusNanoTree: return 10f;
            case ResourceDevelopmentKind.FortressCrusherAscension: return 15f;
            case ResourceDevelopmentKind.ThothBlessing: return 17f;
            case ResourceDevelopmentKind.SalvageHive: return 12f;
            default: return 8f;
        }
    }

    private float GetDeveloperGroundOffset(ResourceDevelopmentKind kind)
    {
        return kind == ResourceDevelopmentKind.ThothBlessing ? 10f : 0.34f;
    }

    private void ProgressResourceDevelopment(
        ResourceNode node, ResourceDevelopmentKind kind, Transform developer, RunnerUnit runner, float dt)
    {
        ResourceDevelopmentProject project = GetResourceDevelopmentProject(node, kind);
        if (project == null)
            return;

        int maxLevel = GetResourceDevelopmentMaxLevel(kind);
        if (project.level >= maxLevel)
        {
            HoldCompletedDeveloper(kind, developer, runner);
            return;
        }

        float required = GetResourceDevelopmentSeconds(kind, project.level);
        float resourceFactor = node.kind == ResourceKind.Wind ? 1.12f : node.kind == ResourceKind.Sand ? 0.94f : 1f;
        project.progress += dt * resourceFactor;
        project.actionTimer -= dt;
        if (project.actionTimer <= 0f)
        {
            project.actionTimer = 0.8f;
            Vector3 start = developer != null ? developer.position + Vector3.up : node.transform.position + Vector3.up;
            CreateBeam(start, node.transform.position + Vector3.up * (1.2f + project.level * 0.45f),
                GetResourceDevelopmentColor(node.kind), 0.035f, 0.1f);
        }

        if (project.progress < required)
            return;

        project.progress = 0f;
        project.level++;
        BuildOrUpgradeResourceDevelopment(node, project);
        ApplyResourceDevelopmentCompletion(node, project, runner);
        PlaySandRunnerSound(SandRunnerSound.ProductionComplete, node.transform.position, 0.76f);
        ShowBanner(GetResourceDevelopmentTitle(project.kind) + " // LEVEL " + project.level, 2.6f);
        lastEvent = GetResourceDevelopmentTitle(project.kind) + " reached level " + project.level +
            " at " + node.label + " (" + GetResourceKitName(node.kind) + " kit).";
    }

    private void HoldCompletedDeveloper(ResourceDevelopmentKind kind, Transform developer, RunnerUnit runner)
    {
        if (kind == ResourceDevelopmentKind.GoldenMine || kind == ResourceDevelopmentKind.SalvageHive)
            return;

        if (runner != null)
            runnerDevelopmentOrders.Remove(runner);
        if (kind == ResourceDevelopmentKind.HorusNanoTree)
            horusDevelopmentOrder = null;
    }

    private int GetResourceDevelopmentMaxLevel(ResourceDevelopmentKind kind)
    {
        return SandRunnersResourceDevelopmentRules.MaxLevel((int)kind);
    }

    private float GetResourceDevelopmentSeconds(ResourceDevelopmentKind kind, int currentLevel)
    {
        float baseSeconds;
        switch (kind)
        {
            case ResourceDevelopmentKind.GoldenMine: baseSeconds = 22f; break;
            case ResourceDevelopmentKind.ScarabFortress: baseSeconds = 28f; break;
            case ResourceDevelopmentKind.HorusNanoTree: baseSeconds = 30f; break;
            case ResourceDevelopmentKind.FortressCrusherAscension: baseSeconds = 48f; break;
            case ResourceDevelopmentKind.ThothBlessing: baseSeconds = 58f; break;
            case ResourceDevelopmentKind.SalvageHive: baseSeconds = 42f; break;
            default: baseSeconds = 30f; break;
        }
        return SandRunnersResourceDevelopmentRules.TierSeconds(
            baseSeconds, currentLevel, balanceProfile.resourceDevelopmentTierMultiplier);
    }

    private float GetResourceCaptureStrength(ResourceNode node)
    {
        if (node == null || node.transform == null)
            return 0f;

        float strength = battlePyramid != null &&
            FlatDistance(node.transform.position, battlePyramid.position) <= captureRadius ? 1f : 0f;

        foreach (KeyValuePair<GoldenBuilderAsset, ResourceDevelopmentAssignment> pair in builderDevelopmentOrders)
            if (pair.Key != null && pair.Key.transform != null && pair.Value.node == node &&
                FlatDistance(pair.Key.transform.position, node.transform.position) <= 18f)
                strength += 1f;

        foreach (KeyValuePair<RunnerUnit, ResourceDevelopmentAssignment> pair in runnerDevelopmentOrders)
            if (pair.Key != null && pair.Key.transform != null && pair.Value.node == node &&
                FlatDistance(pair.Key.transform.position, node.transform.position) <= 22f)
                strength += pair.Value.kind == ResourceDevelopmentKind.ThothBlessing ? 1.5f : 1f;

        if (touchOfHorus != null && touchOfHorus.root != null && horusDevelopmentOrder != null &&
            horusDevelopmentOrder.node == node && FlatDistance(touchOfHorus.root.position, node.transform.position) <= 20f)
            strength += 1.25f;

        return Mathf.Min(4f, strength);
    }

    private float GetResourceDevelopmentIncomeMultiplier(ResourceNode node)
    {
        ResourceDevelopmentState state;
        if (node == null || !resourceDevelopmentStates.TryGetValue(node, out state))
            return 1f;

        ResourceDevelopmentProject mine;
        if (!state.projects.TryGetValue(ResourceDevelopmentKind.GoldenMine, out mine))
            return 1f;

        return SandRunnersResourceDevelopmentRules.IncomeMultiplier(
            mine.level, balanceProfile.mineIncomePerLevel);
    }

    private void BuildOrUpgradeResourceDevelopment(ResourceNode node, ResourceDevelopmentProject project)
    {
        if (node == null || node.transform == null || project == null)
            return;

        if (project.root == null)
        {
            Transform root = new GameObject("SR_" + project.kind + "_" + node.label.Replace(' ', '_')).transform;
            root.position = node.transform.position;
            root.rotation = node.transform.rotation;
            project.root = root;
        }

        if (project.kind == ResourceDevelopmentKind.GoldenMine)
            BuildMineTier(node, project);
        else if (project.kind == ResourceDevelopmentKind.ScarabFortress)
            BuildScarabFortressTier(node, project);
        else if (project.kind == ResourceDevelopmentKind.HorusNanoTree)
            BuildHorusTreeTier(node, project);
        else
            BuildTransformationAltar(node, project);
    }

    private void BuildMineTier(ResourceNode node, ResourceDevelopmentProject project)
    {
        Material armor = node.kind == ResourceKind.Gold ? controlledMaterial :
            node.kind == ResourceKind.Wind ? pyramidGlowMaterial : siegeUnitMaterial;
        string tierName = "Mine_Tier_" + project.level;
        Transform tier = new GameObject(tierName).transform;
        tier.SetParent(project.root, false);

        if (project.level == 1)
        {
            CreateResourcePrimitive(PrimitiveType.Cylinder, "Extraction_Ring", tier,
                new Vector3(0f, 0.28f, 0f), Quaternion.identity, new Vector3(4.4f, 0.25f, 4.4f), pyramidDarkArmorMaterial);
            CreateResourcePrimitive(PrimitiveType.Cube, "Processing_Core", tier,
                new Vector3(0f, 1.05f, 0f), Quaternion.identity, new Vector3(2.8f, 1.8f, 2.8f), armor);
        }
        else if (project.level == 2)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle) * 5.4f, 1.1f, Mathf.Cos(angle) * 5.4f);
                CreateResourcePrimitive(PrimitiveType.Cube, "Freight_Bay_" + i, tier, p,
                    Quaternion.Euler(0f, i * 90f, 0f), new Vector3(2.1f, 1.5f, 3.2f), armor);
            }
        }
        else
        {
            if (node.kind == ResourceKind.Wind)
            {
                for (int i = 0; i < 3; i++)
                    CreateResourcePrimitive(PrimitiveType.Cube, "Wind_Sail_" + i, tier,
                        new Vector3((i - 1) * 3.2f, 4.2f, 0f), Quaternion.Euler(0f, i * 28f, 18f),
                        new Vector3(0.18f, 5.8f, 2.1f), pyramidGlowMaterial);
            }
            else if (node.kind == ResourceKind.Gold)
            {
                for (int i = 0; i < 3; i++)
                    CreateResourcePrimitive(PrimitiveType.Cylinder, "Gold_Refinery_Obelisk_" + i, tier,
                        new Vector3((i - 1) * 2.7f, 3f, 0f), Quaternion.identity,
                        new Vector3(0.8f, 3.2f + i * 0.4f, 0.8f), controlledMaterial);
            }
            else
            {
                CreateResourcePrimitive(PrimitiveType.Cylinder, "Sand_Excavator", tier,
                    new Vector3(0f, 1.8f, 0f), Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(3.6f, 0.45f, 3.6f), siegeUnitMaterial);
            }
        }
    }

    private void BuildScarabFortressTier(ResourceNode node, ResourceDevelopmentProject project)
    {
        Material armor = node.kind == ResourceKind.Gold ? controlledMaterial :
            node.kind == ResourceKind.Wind ? pyramidGlowMaterial : pyramidDarkArmorMaterial;
        Transform tier = new GameObject("Fortification_Tier_" + project.level).transform;
        tier.SetParent(project.root, false);
        int segments = 4 + project.level * 2;
        float radius = 10f + project.level * 2f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 1.2f, Mathf.Cos(angle) * radius);
            CreateResourcePrimitive(PrimitiveType.Cube, "Heavy_Wall_" + i, tier, p,
                Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f), new Vector3(4.2f, 2.4f, 1.1f), armor);
        }

        int guns = project.level + 1;
        for (int i = 0; i < guns; i++)
        {
            float angle = i * Mathf.PI * 2f / guns + 0.5f;
            Vector3 p = new Vector3(Mathf.Sin(angle) * (radius - 1.5f), 2.4f, Mathf.Cos(angle) * (radius - 1.5f));
            CreateResourcePrimitive(PrimitiveType.Cylinder,
                project.level == 1 ? "Twin_30mm_" + i : project.level == 2 ? "Gepard_" + i : "Medium_Mortar_" + i,
                tier, p, Quaternion.identity, new Vector3(0.75f, 1.2f, 0.75f), controlledMaterial);
        }
    }

    private void BuildHorusTreeTier(ResourceNode node, ResourceDevelopmentProject project)
    {
        Transform tier = new GameObject("NanoTree_Tier_" + project.level).transform;
        tier.SetParent(project.root, false);
        float height = 3.8f + project.level * 2.1f;
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Solar_Nano_Trunk", tier,
            new Vector3(0f, height * 0.5f, 0f), Quaternion.identity,
            new Vector3(0.55f + project.level * 0.18f, height * 0.5f, 0.55f + project.level * 0.18f), controlledMaterial);
        int branches = 4 + project.level * 2;
        for (int i = 0; i < branches; i++)
        {
            float angle = i * Mathf.PI * 2f / branches;
            Vector3 p = new Vector3(Mathf.Sin(angle) * (1.8f + project.level), height, Mathf.Cos(angle) * (1.8f + project.level));
            CreateResourcePrimitive(PrimitiveType.Sphere, "Solar_Leaf_" + i, tier, p,
                Quaternion.identity, Vector3.one * (1.1f + project.level * 0.25f), pyramidGlowMaterial);
        }

        if (project.level >= 3)
            BuildGiantHorusBloom(node, project);
    }

    private void BuildTransformationAltar(ResourceNode node, ResourceDevelopmentProject project)
    {
        Material material = node.kind == ResourceKind.Gold ? controlledMaterial :
            node.kind == ResourceKind.Wind ? pyramidGlowMaterial : siegeUnitMaterial;
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Transformation_Ring", project.root,
            new Vector3(0f, 0.24f, 0f), Quaternion.identity, new Vector3(6.5f, 0.22f, 6.5f), material);
        CreateResourcePrimitive(PrimitiveType.Sphere, "Resource_Focus", project.root,
            new Vector3(0f, 1.4f, 0f), Quaternion.identity, Vector3.one * 1.15f, material);
    }

    private Transform CreateResourcePrimitive(
        PrimitiveType type, string name, Transform parent, Vector3 localPosition,
        Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localRotation = localRotation;
        primitive.transform.localScale = localScale;
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
        return primitive.transform;
    }

    private void ApplyResourceDevelopmentCompletion(
        ResourceNode node, ResourceDevelopmentProject project, RunnerUnit runner)
    {
        if (runner == null)
            return;

        if (project.kind == ResourceDevelopmentKind.FortressCrusherAscension)
        {
            ApplyFortressCrusherAscension(node, runner);
            runnerDevelopmentOrders.Remove(runner);
        }
        else if (project.kind == ResourceDevelopmentKind.ThothBlessing)
        {
            ApplyThothBlessing(node, runner);
            runnerDevelopmentOrders.Remove(runner);
        }
        else if (project.kind == ResourceDevelopmentKind.SalvageHive)
        {
            ApplySalvageHiveTransformation(node, runner);
        }
    }

    private void ApplyFortressCrusherAscension(ResourceNode node, RunnerUnit runner)
    {
        runner.displayName = "Fortress Crusher 2.0 // " + GetResourceKitName(node.kind);
        runner.maxHealth += node.kind == ResourceKind.Sand ? 420f : 260f;
        runner.health = runner.maxHealth;
        runner.damage += node.kind == ResourceKind.Gold ? 58f : 34f;
        runner.range += node.kind == ResourceKind.Gold ? 18f : 10f;
        runner.speed += node.kind == ResourceKind.Wind ? 3.2f : 1.2f;
        AddResourceTransformationKit(runner.transform, node.kind, "Crusher_2_0");
    }

    private void ApplyThothBlessing(ResourceNode node, RunnerUnit runner)
    {
        runner.displayName = "Blessing of Thoth // " + GetResourceKitName(node.kind);
        runner.maxHealth += node.kind == ResourceKind.Sand ? 520f : 340f;
        runner.health = runner.maxHealth;
        runner.damage += node.kind == ResourceKind.Gold ? 46f : 28f;
        runner.range += 16f;
        runner.speed += node.kind == ResourceKind.Wind ? 4.5f : 2f;
        runner.flightHeight += 4f;
        AddResourceTransformationKit(runner.transform, node.kind, "Eight_Deck_Thoth");
        UpgradeThothCarrierToBlessing(runner.transform, node.kind);
        BuildThothBlessingSuperstructure(runner.transform, node.kind);
    }

    private void ApplySalvageHiveTransformation(ResourceNode node, RunnerUnit runner)
    {
        runner.displayName = "Anubis Salvage Hive // " + GetResourceKitName(node.kind);
        runner.maxHealth += 480f;
        runner.health = runner.maxHealth;
        runner.damage = Mathf.Max(runner.damage, 32f);
        runner.range = Mathf.Max(runner.range, 42f);
        runner.speed = 0f;
        runner.autonomous = true;
        runner.salvageAutoMode = true;
        AddResourceTransformationKit(runner.transform, node.kind, "Curse_Hive");
    }

    private void AddResourceTransformationKit(Transform parent, ResourceKind kind, string prefix)
    {
        if (parent == null || parent.Find(prefix + "_Core") != null)
            return;

        Material material = kind == ResourceKind.Gold ? controlledMaterial :
            kind == ResourceKind.Wind ? pyramidGlowMaterial : siegeUnitMaterial;
        CreateResourcePrimitive(PrimitiveType.Sphere, prefix + "_Core", parent,
            new Vector3(0f, 1.5f, 0f), Quaternion.identity, Vector3.one * 0.72f, material);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            CreateResourcePrimitive(PrimitiveType.Cube, prefix + "_Module_" + i, parent,
                Quaternion.Euler(0f, angle, 0f) * new Vector3(1.8f, 1.05f, 0f),
                Quaternion.Euler(0f, angle, kind == ResourceKind.Wind ? 24f : 0f),
                new Vector3(0.35f, 0.7f, 1.2f), material);
        }
    }

    private void UpdateDevelopedMine(ResourceDevelopmentState state, float dt)
    {
        ResourceDevelopmentProject mine;
        if (!state.projects.TryGetValue(ResourceDevelopmentKind.GoldenMine, out mine) || mine.level <= 0)
            return;

        state.convoyTimer -= dt;
        if (state.convoyTimer > 0f)
            return;

        state.convoyTimer = Mathf.Max(16f, balanceProfile.mineConvoyBaseSeconds - mine.level * balanceProfile.mineConvoyTierReduction);
        SpawnMineConvoy(state.node, mine.level);
    }

    private void SpawnMineConvoy(ResourceNode node, int level)
    {
        if (node == null || node.transform == null || mineConvoys.Count >= balanceProfile.mineConvoyLimit)
            return;

        Transform root = CreateMineConvoyVisual();
        root.position = node.transform.position + node.transform.right * 8f;
        root.position = new Vector3(root.position.x, GetPlayableGroundHeight(root.position) + 0.42f, root.position.z);

        MineConvoyState convoy = new MineConvoyState();
        convoy.root = root;
        convoy.source = node;
        convoy.depot = FindNearestResourceDevelopmentDepot(node.transform.position);
        convoy.kind = node.kind;
        convoy.amount = (node.kind == ResourceKind.Wind ? 9f : node.kind == ResourceKind.Gold ? 18f : 24f) * level;
        convoy.speed = 7f + level * 0.7f;
        mineConvoys.Add(convoy);
        AttachMineConvoyEscorts(convoy, level);
        lastEvent = node.label + " dispatched a protected resource convoy.";
    }

    private Transform CreateMineConvoyVisual()
    {
        Transform root = new GameObject("SR_Mine_Convoy_Truck").transform;
        string basePath = "SandRunners/Models/ImportedCandidates/SR_MineConvoyTruck_LOD";
        List<LOD> lods = new List<LOD>();
        float[] thresholds = { 0.48f, 0.2f, 0.04f };

        for (int i = 0; i < 3; i++)
        {
            GameObject prefab = Resources.Load<GameObject>(basePath + i);
            if (prefab == null)
                continue;

            GameObject instance = Instantiate(prefab, root);
            instance.name = "Truck_LOD" + i;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 4.2f;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            lods.Add(new LOD(thresholds[i], renderers));
        }

        if (lods.Count > 0)
        {
            LODGroup group = root.gameObject.AddComponent<LODGroup>();
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
        }
        else
        {
            CreateBox(root, "Convoy_Hull", new Vector3(0f, 0.55f, 0f), Quaternion.identity,
                new Vector3(2.2f, 0.9f, 4f), goldenTurretMaterial);
            CreateBox(root, "Convoy_Cargo", new Vector3(0f, 1.15f, -0.6f), Quaternion.identity,
                new Vector3(1.8f, 1.2f, 2.1f), goldenTurretGlowMaterial);
        }

        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.size = new Vector3(3.2f, 2.4f, 5.2f);
        return root;
    }

    private GoldenStructure FindNearestResourceDevelopmentDepot(Vector3 origin)
    {
        GoldenStructure best = null;
        float bestDistance = 280f;
        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null ||
                structure.kind != StructureKind.ResourceDepot || structure.health <= 0f)
                continue;

            float distance = FlatDistance(origin, structure.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = structure;
            }
        }
        return best;
    }

    private void UpdateMineConvoys(float dt)
    {
        for (int i = mineConvoys.Count - 1; i >= 0; i--)
        {
            MineConvoyState convoy = mineConvoys[i];
            if (convoy == null || convoy.root == null || convoy.source == null || convoy.source.transform == null)
            {
                if (convoy != null && convoy.root != null)
                    Destroy(convoy.root.gameObject);
                RetireMineConvoyEscorts(convoy);
                mineConvoys.RemoveAt(i);
                continue;
            }

            UpdateMineConvoyEscorts(convoy, dt);

            Vector3 destination;
            if (convoy.returning)
                destination = convoy.source.transform.position + convoy.source.transform.right * 8f;
            else if (convoy.depot != null && convoy.depot.transform != null && convoy.depot.health > 0f)
                destination = convoy.depot.transform.position;
            else
                destination = battlePyramid.position + battlePyramid.right * 10f;

            Vector3 toTarget = destination - convoy.root.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > 2.6f)
            {
                convoy.root.position += toTarget.normalized * convoy.speed * dt;
                RotateFlatToward(convoy.root, toTarget, 150f * dt);
                Vector3 p = convoy.root.position;
                p.y = GetPlayableGroundHeight(p) + 0.42f;
                convoy.root.position = p;
                continue;
            }

            if (!convoy.returning)
            {
                if (convoy.depot != null && convoy.depot.transform != null && convoy.depot.health > 0f)
                {
                    if (convoy.kind == ResourceKind.Gold) convoy.depot.storedGold += convoy.amount;
                    else if (convoy.kind == ResourceKind.Wind) convoy.depot.storedWind += convoy.amount;
                    else convoy.depot.storedSand += convoy.amount;
                }
                else
                {
                    AddResource(convoy.kind, convoy.amount);
                    PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, convoy.root.position, 0.64f);
                }
                convoy.returning = true;
            }
            else
            {
                RetireMineConvoyEscorts(convoy);
                Destroy(convoy.root.gameObject);
                mineConvoys.RemoveAt(i);
            }
        }
    }

    private void UpdateScarabFortress(ResourceDevelopmentState state, float dt)
    {
        ResourceDevelopmentProject project;
        if (!state.projects.TryGetValue(ResourceDevelopmentKind.ScarabFortress, out project) || project.level <= 0)
            return;

        state.defenseTimer -= dt;
        if (state.defenseTimer > 0f)
            return;
        state.defenseTimer = Mathf.Max(0.55f, 1.2f - project.level * 0.18f);

        EnemyUnit target = FindNearestEnemy(state.node.transform.position, balanceProfile.resourceFortBaseRange + project.level * 22f);
        if (target == null || target.transform == null)
            return;

        float damage = 12f + project.level * 11f;
        target.health -= damage;
        CreateBeam(state.node.transform.position + Vector3.up * 3.2f,
            target.transform.position + Vector3.up, GetResourceDevelopmentColor(state.node.kind), 0.055f, 0.12f);
        CleanupDeadEnemies();
    }

    private void UpdateHorusNanoTree(ResourceDevelopmentState state, float dt)
    {
        ResourceDevelopmentProject project;
        if (!state.projects.TryGetValue(ResourceDevelopmentKind.HorusNanoTree, out project) || project.level <= 0)
            return;

        state.repairTimer -= dt;
        if (state.repairTimer <= 0f)
        {
            state.repairTimer = 1f;
            float radius = balanceProfile.nanoTreeBaseRepairRadius + project.level * 16f;
            for (int i = 0; i < runners.Count; i++)
            {
                RunnerUnit runner = runners[i];
                if (runner != null && runner.transform != null && runner.health > 0f &&
                    FlatDistance(runner.transform.position, state.node.transform.position) <= radius)
                    runner.health = Mathf.Min(runner.maxHealth, runner.health + 3f + project.level * 2f);
            }

            if (battlePyramid != null && FlatDistance(battlePyramid.position, state.node.transform.position) <= radius)
                pyramidHull = Mathf.Min(pyramidMaxHull, pyramidHull + 2f + project.level * 1.5f);
        }

        if (project.level < 3)
            return;

        UpdateGiantHorusBloom(state, project, dt);
    }

    private void UpdateSalvageHive(ResourceDevelopmentState state, float dt)
    {
        ResourceDevelopmentProject project;
        if (!state.projects.TryGetValue(ResourceDevelopmentKind.SalvageHive, out project) || project.level <= 0)
            return;

        state.hiveSpawnTimer -= dt;
        if (state.hiveSpawnTimer <= 0f && CountHiveCollectors(state.node) < 6)
        {
            state.hiveSpawnTimer = 18f;
            Vector3 spawn = state.node.transform.position +
                Quaternion.Euler(0f, CountHiveCollectors(state.node) * 57f, 0f) * Vector3.forward * 7f;
            spawn.y = GetPlayableGroundHeight(spawn) + 0.3f;
            RunnerUnit collector = CreateVehicleUnit("Anubis_Graveyard_Harvester", spawn,
                state.node.transform.rotation, new Vector3(0.58f, 0.38f, 0.74f),
                pyramidDarkArmorMaterial, 72f, 9.5f, 8f, 16f, "Graveyard Harvester Scarab");
            collector.isSalvageScarab = true;
            collector.salvageAutoMode = true;
            collector.contractGroupId = state.node.transform.GetHashCode();
            AddResourceTransformationKit(collector.transform, state.node.kind, "Harvester");
            PlaySandRunnerSound(SandRunnerSound.ProductionComplete, spawn, 0.42f);
        }

        state.hiveDefenseTimer -= dt;
        if (state.hiveDefenseTimer > 0f)
            return;
        state.hiveDefenseTimer = 1.1f;
        EnemyUnit target = FindNearestEnemy(state.node.transform.position, 54f);
        if (target == null || target.transform == null)
            return;
        target.health -= 24f;
        CreateBeam(state.node.transform.position + Vector3.up * 2.4f,
            target.transform.position + Vector3.up, new Color(0.48f, 1f, 0.34f, 1f), 0.04f, 0.13f);
        CleanupDeadEnemies();
    }

    private int CountHiveCollectors(ResourceNode node)
    {
        if (node == null || node.transform == null)
            return 0;
        int count = 0;
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner != null && runner.transform != null &&
                runner.displayName.Contains("Graveyard Harvester") &&
                FlatDistance(runner.transform.position, node.transform.position) < 220f)
                count++;
        }
        return count;
    }

    private void InitializeCurseHiveUi()
    {
        if (curseHivePanelObject != null || strategicCanvas == null)
            return;

        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Curse_Hive_Panel", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-16f, 554f), new Vector2(510f, 154f), UiGlassDeep);
        curseHivePanelObject = panel.gameObject;
        curseHiveStatusText = CreateText(panel, "Hive_Status", "ANUBIS SALVAGE HIVE", 12,
            FontStyle.Bold, new Vector2(12f, -8f), new Vector2(480f, 26f), new Color(0.55f, 1f, 0.42f, 1f));

        string[] labels = { "HOWITZER SCARAB", "MUMMY GUARD", "SOUL HARVESTER", "CURSE WEAVER", "TOMB WING" };
        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i;
            StrategicCanvasButton button = CreateButton(panel, "Hive_Class_" + i, labels[i],
                new Vector2(12f + (i % 3) * 162f, -38f - (i / 3) * 38f),
                new Vector2(154f, 32f), () => ProduceCurseHiveUnit(captured));
            curseHiveButtons.Add(button);
        }
        curseHivePanelObject.SetActive(false);
    }

    private void UpdateCurseHiveUi()
    {
        if (curseHivePanelObject == null)
            return;

        RunnerUnit hive = GetSelectedCurseHive();
        bool visible = commandCursorMode && hive != null;
        if (curseHivePanelObject.activeSelf != visible)
            curseHivePanelObject.SetActive(visible);
        if (!visible)
            return;

        ResourceDevelopmentAssignment assignment;
        ResourceDevelopmentState state = runnerDevelopmentOrders.TryGetValue(hive, out assignment)
            ? GetResourceDevelopmentState(assignment.node) : null;
        float cooldown = state != null ? state.hiveProductionTimer : 0f;
        if (curseHiveStatusText != null)
            curseHiveStatusText.text = "ANUBIS SALVAGE HIVE // COLLECTORS " +
                (assignment != null ? CountHiveCollectors(assignment.node) : 0) + "/6 // FORGE " +
                (cooldown > 0f ? Mathf.CeilToInt(cooldown) + "s" : "READY");
    }

    private RunnerUnit GetSelectedCurseHive()
    {
        RunnerUnit runner = FindRunnerByRoot(selectedStrategicTransform);
        return runner != null && runner.displayName.Contains("Salvage Hive") ? runner : null;
    }

    private void ProduceCurseHiveUnit(int classIndex)
    {
        RunnerUnit hive = GetSelectedCurseHive();
        if (hive == null)
            return;

        ResourceDevelopmentAssignment assignment;
        if (!runnerDevelopmentOrders.TryGetValue(hive, out assignment) || assignment.node == null)
            return;
        ResourceDevelopmentState state = GetResourceDevelopmentState(assignment.node);
        if (state == null || state.hiveProductionTimer > 0f)
        {
            lastEvent = "The curse forge is still cooling.";
            return;
        }

        float sandCost = 45f + classIndex * 9f;
        float goldCost = 70f + classIndex * 14f;
        float windCost = classIndex == 4 ? 20f : 6f + classIndex * 2f;
        string[] labels = { "Anubis Howitzer Scarab", "Mummy Guard", "Soul Harvester", "Curse Weaver", "Tomb Wing" };
        if (!TrySpendResources(sandCost, goldCost, windCost, labels[classIndex]))
            return;

        Vector3 spawn = assignment.node.transform.position + assignment.node.transform.forward * (10f + classIndex * 1.4f);
        spawn.y = GetPlayableGroundHeight(spawn) + (classIndex == 4 ? 8f : 0.34f);
        RunnerUnit created;
        switch (classIndex)
        {
            case 0:
                created = CreateVehicleUnit("Anubis_Howitzer_Scarab", spawn, assignment.node.transform.rotation,
                    new Vector3(1.45f, 0.86f, 1.72f), siegeUnitMaterial, 260f, 5.4f, 82f, 64f,
                    labels[classIndex]);
                break;
            case 1:
                created = CreateVehicleUnit("Anubis_Mummy_Guard", spawn, assignment.node.transform.rotation,
                    new Vector3(1.3f, 0.92f, 1.45f), pyramidDarkArmorMaterial, 360f, 5.8f, 46f, 28f,
                    labels[classIndex]);
                break;
            case 2:
                created = CreateVehicleUnit("Anubis_Soul_Harvester", spawn, assignment.node.transform.rotation,
                    new Vector3(0.92f, 0.58f, 1.25f), controlledMaterial, 150f, 11.5f, 30f, 24f,
                    labels[classIndex]);
                break;
            case 3:
                created = CreateVehicleUnit("Anubis_Curse_Weaver", spawn, assignment.node.transform.rotation,
                    new Vector3(1.05f, 0.74f, 1.2f), pyramidGlowMaterial, 180f, 8.2f, 26f, 38f,
                    labels[classIndex]);
                break;
            default:
                created = CreateVehicleUnit("Anubis_Tomb_Wing", spawn, assignment.node.transform.rotation,
                    new Vector3(1.15f, 0.5f, 1.65f), pyramidDarkArmorMaterial, 170f, 15.5f, 38f, 36f,
                    labels[classIndex], true, 9f);
                break;
        }

        AddResourceTransformationKit(created.transform, assignment.node.kind, "Curse_Class_" + classIndex);
        state.hiveProductionTimer = 12f + classIndex * 2f;
        PlaySandRunnerSound(SandRunnerSound.ProductionBuild, hive.transform.position, 0.65f);
        lastEvent = labels[classIndex] + " is being released from the Anubis hive.";
    }

    private void PruneResourceDevelopmentOrders()
    {
        List<GoldenBuilderAsset> deadBuilders = new List<GoldenBuilderAsset>();
        foreach (GoldenBuilderAsset builder in builderDevelopmentOrders.Keys)
            if (builder == null || builder.transform == null || builder.health <= 0f)
                deadBuilders.Add(builder);
        for (int i = 0; i < deadBuilders.Count; i++)
            builderDevelopmentOrders.Remove(deadBuilders[i]);

        List<RunnerUnit> deadRunners = new List<RunnerUnit>();
        foreach (RunnerUnit runner in runnerDevelopmentOrders.Keys)
            if (runner == null || runner.transform == null || runner.health <= 0f)
                deadRunners.Add(runner);
        for (int i = 0; i < deadRunners.Count; i++)
            runnerDevelopmentOrders.Remove(deadRunners[i]);
    }

    private string GetResourceDevelopmentSummary(ResourceNode node)
    {
        ResourceDevelopmentState state;
        if (node == null || !resourceDevelopmentStates.TryGetValue(node, out state))
            return "No development";

        string summary = "";
        foreach (KeyValuePair<ResourceDevelopmentKind, ResourceDevelopmentProject> pair in state.projects)
        {
            if (pair.Value.level <= 0 && pair.Value.progress <= 0f)
                continue;
            if (summary.Length > 0)
                summary += " | ";
            summary += GetResourceDevelopmentTitle(pair.Key) + " L" + pair.Value.level;
        }
        return summary.Length > 0 ? summary : "Awaiting developer";
    }

    private string GetResourceDevelopmentTitle(ResourceDevelopmentKind kind)
    {
        switch (kind)
        {
            case ResourceDevelopmentKind.GoldenMine: return "GOLDEN ELEMENTAL MINE";
            case ResourceDevelopmentKind.ScarabFortress: return "SCARAB BATTLE FORT";
            case ResourceDevelopmentKind.HorusNanoTree: return "HORUS NANO-TREE";
            case ResourceDevelopmentKind.FortressCrusherAscension: return "FORTRESS CRUSHER 2.0";
            case ResourceDevelopmentKind.ThothBlessing: return "BLESSING OF THOTH";
            case ResourceDevelopmentKind.SalvageHive: return "ANUBIS SALVAGE HIVE";
            default: return "RESOURCE PROJECT";
        }
    }

    private string GetResourceKitName(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Gold: return "AURIC";
            case ResourceKind.Wind: return "TEMPEST";
            default: return "DUNE";
        }
    }

    private Color GetResourceDevelopmentColor(ResourceKind kind)
    {
        if (kind == ResourceKind.Gold)
            return new Color(1f, 0.72f, 0.16f, 1f);
        if (kind == ResourceKind.Wind)
            return new Color(0.24f, 0.9f, 1f, 1f);
        return new Color(0.92f, 0.54f, 0.24f, 1f);
    }

    private string GetResourceDevelopmentOrderSummary(GoldenBuilderAsset builder)
    {
        ResourceDevelopmentAssignment assignment;
        if (builder == null || !builderDevelopmentOrders.TryGetValue(builder, out assignment) || assignment.node == null)
            return "";
        return "\nDEVELOPING " + assignment.node.label + " // " +
            GetResourceDevelopmentProgressText(assignment.node, assignment.kind);
    }

    private string GetResourceDevelopmentOrderSummary(RunnerUnit runner)
    {
        ResourceDevelopmentAssignment assignment;
        if (runner == null || !runnerDevelopmentOrders.TryGetValue(runner, out assignment) || assignment.node == null)
            return "";
        return "\nDEVELOPING " + assignment.node.label + " // " +
            GetResourceDevelopmentProgressText(assignment.node, assignment.kind);
    }

    private string GetResourceDevelopmentProgressText(ResourceNode node, ResourceDevelopmentKind kind)
    {
        if (node == null)
            return "target lost";
        if (!node.controlled)
            return "capture " + Mathf.RoundToInt(node.capture * 100f) + "%";

        ResourceDevelopmentProject project = GetResourceDevelopmentProject(node, kind);
        if (project == null)
            return "preparing";
        int maxLevel = GetResourceDevelopmentMaxLevel(kind);
        if (project.level >= maxLevel)
            return GetResourceDevelopmentTitle(kind) + " complete";

        float required = GetResourceDevelopmentSeconds(kind, project.level);
        int percent = required > 0f ? Mathf.RoundToInt(project.progress / required * 100f) : 0;
        return GetResourceDevelopmentTitle(kind) + " L" + project.level + " -> L" +
            (project.level + 1) + " " + percent + "%";
    }
}

internal static class SandRunnersResourceDevelopmentRules
{
    internal static float CaptureRate(float developerStrength, float perDeveloperRate)
    {
        return Mathf.Max(0f, developerStrength) * Mathf.Max(0f, perDeveloperRate);
    }

    internal static float IncomeMultiplier(int mineLevel, float bonusPerLevel)
    {
        return 1f + Mathf.Max(0, mineLevel) * Mathf.Max(0f, bonusPerLevel);
    }

    internal static float TierSeconds(float baseSeconds, int currentLevel, float tierMultiplier)
    {
        return Mathf.Max(0.1f, baseSeconds) *
            Mathf.Pow(Mathf.Max(1f, tierMultiplier), Mathf.Max(0, currentLevel));
    }

    internal static int MaxLevel(int developmentKind)
    {
        return developmentKind >= 0 && developmentKind <= 2 ? 3 : 1;
    }

    internal static int ResourceKit(int resourceKind)
    {
        return Mathf.Clamp(resourceKind, 0, 2);
    }

    internal static int ThothAircraftClassCount()
    {
        return 4;
    }

    internal static int ThothDeckCount(bool blessing)
    {
        return blessing ? 8 : 4;
    }

    internal static int CurseHiveClassCount()
    {
        return 5;
    }

    internal static int ConvoyEscortCount(int mineLevel)
    {
        return Mathf.Clamp(mineLevel, 1, 2);
    }
}