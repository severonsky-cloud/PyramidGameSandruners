using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private void BuildGiantHorusBloom(ResourceNode node, ResourceDevelopmentProject project)
    {
        if (node == null || project == null || project.root == null ||
            project.root.Find("Giant_Horus_Bloom") != null)
            return;

        Transform bloom = new GameObject("Giant_Horus_Bloom").transform;
        bloom.SetParent(project.root, false);
        Material core = node.kind == ResourceKind.Wind ? pyramidGlowMaterial :
            node.kind == ResourceKind.Gold ? controlledMaterial : siegeUnitMaterial;

        CreateResourcePrimitive(PrimitiveType.Cylinder, "Bloom_Root_Dais", bloom,
            new Vector3(0f, 0.35f, 0f), Quaternion.identity, new Vector3(7.8f, 0.35f, 7.8f),
            pyramidDarkArmorMaterial);
        for (int i = 0; i < 8; i++)
        {
            float rootAngle = i * 45f;
            Vector3 rootPosition = Quaternion.Euler(0f, rootAngle, 0f) * new Vector3(0f, 0.8f, 4.2f);
            CreateResourcePrimitive(PrimitiveType.Cube, "Bloom_Root_Buttress_" + i, bloom,
                rootPosition, Quaternion.Euler(0f, rootAngle, 0f), new Vector3(1.35f, 0.75f, 5.8f), core);
        }
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Bloom_Ascended_Trunk", bloom,
            new Vector3(0f, 7f, 0f), Quaternion.identity, new Vector3(2.25f, 7f, 2.25f), core);
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Bloom_Trunk_Armor", bloom,
            new Vector3(0f, 7.3f, 0f), Quaternion.identity, new Vector3(2.7f, 5.8f, 2.7f),
            pyramidDarkArmorMaterial);
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Bloom_Solar_Collar", bloom,
            new Vector3(0f, 13.3f, 0f), Quaternion.identity, new Vector3(4.8f, 0.35f, 4.8f), core);
        CreateResourcePrimitive(PrimitiveType.Sphere, "Bloom_Solar_Heart", bloom,
            new Vector3(0f, 14.8f, 0f), Quaternion.identity, Vector3.one * 3.8f, pyramidGlowMaterial);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 branch = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 12.4f, 3.2f);
            CreateResourcePrimitive(PrimitiveType.Cube, "Bloom_Branch_" + i, bloom, branch,
                Quaternion.Euler(-12f, angle, 0f), new Vector3(0.72f, 0.72f, 6.8f),
                pyramidDarkArmorMaterial);
            Vector3 petal = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 14.2f, 6.2f);
            CreateResourcePrimitive(PrimitiveType.Sphere, "Bloom_Petal_" + i, bloom, petal,
                Quaternion.Euler(18f, angle, 0f), new Vector3(2.8f, 0.72f, 4.8f), core);
            Vector3 innerPetal = Quaternion.Euler(0f, angle + 22.5f, 0f) * new Vector3(0f, 15.1f, 3.9f);
            CreateResourcePrimitive(PrimitiveType.Sphere, "Bloom_Inner_Petal_" + i, bloom, innerPetal,
                Quaternion.Euler(-22f, angle + 22.5f, 0f), new Vector3(1.8f, 0.55f, 3.1f),
                pyramidGlowMaterial);
        }

        int emitterCount = SandRunnersEvolutionRules.GiantBloomEmitterCount();
        for (int i = 0; i < emitterCount; i++)
        {
            float angle = i * Mathf.PI * 2f / emitterCount;
            Vector3 position = new Vector3(Mathf.Sin(angle) * 6.8f, 13.2f + (i % 2) * 1.4f,
                Mathf.Cos(angle) * 6.8f);
            Transform emitter = CreateResourcePrimitive(PrimitiveType.Sphere,
                "Horus_Repair_Emitter_" + i.ToString("00"), bloom, position,
                Quaternion.identity, Vector3.one * 0.62f, pyramidGlowMaterial);
            CreateResourcePrimitive(PrimitiveType.Cylinder, "Emitter_Stem", emitter,
                new Vector3(0f, -1.65f, 0f), Quaternion.identity,
                new Vector3(0.16f, 1.65f, 0.16f), core);
            CreateResourcePrimitive(PrimitiveType.Cylinder, "Emitter_Halo", emitter,
                Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.08f, 1.1f), core);
        }

        ShowBanner("GIANT BLOOM OF HORUS // 16 REPAIR STREAMS", 4f);
    }

    private void UpdateGiantHorusBloom(ResourceDevelopmentState state, ResourceDevelopmentProject project, float dt)
    {
        if (state == null || state.node == null || project == null || project.root == null)
            return;
        Transform bloom = project.root.Find("Giant_Horus_Bloom");
        if (bloom == null)
            return;

        state.bloomRepairTimer -= dt;
        if (state.bloomRepairTimer <= 0f)
        {
            state.bloomRepairTimer = 0.38f;
            Transform[] children = bloom.GetComponentsInChildren<Transform>(true);
            List<Transform> emitters = new List<Transform>(16);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name.StartsWith("Horus_Repair_Emitter_"))
                    emitters.Add(children[i]);

            int stream = 0;
            float radius = balanceProfile.nanoTreeBaseRepairRadius + 82f;
            for (int i = 0; i < runners.Count && stream < emitters.Count; i++)
            {
                RunnerUnit runner = runners[i];
                if (runner == null || runner.transform == null || runner.health <= 0f ||
                    runner.health >= runner.maxHealth ||
                    FlatDistance(runner.transform.position, state.node.transform.position) > radius)
                    continue;
                runner.health = Mathf.Min(runner.maxHealth, runner.health + 9f);
                CreateBeam(emitters[stream++].position, runner.transform.position + Vector3.up,
                    new Color(0.24f, 1f, 0.72f, 1f), 0.035f, 0.11f);
            }

            foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
            {
                SettlementDevelopmentState city = pair.Value;
                if (city == null || city.settlement == null || city.settlement.root == null ||
                    city.settlement.health <= 0f ||
                    FlatDistance(city.settlement.root.position, state.node.transform.position) > radius)
                    continue;
                if (city.stage < SettlementDiplomacyStage.ProtectedSettlement)
                    continue;
                city.settlement.health = Mathf.Min(city.settlement.maxHealth, city.settlement.health + 7f);
                if (state.node.kind == ResourceKind.Wind &&
                    FlatDistance(city.settlement.root.position, state.node.transform.position) <= 240f)
                {
                    city.horusPower = true;
                    city.playerInfluence = Mathf.Min(100f, city.playerInfluence + 0.25f);
                    city.tradeTrust = Mathf.Min(100f, city.tradeTrust + 0.15f);
                    UpdateSettlementDiplomacy(city);
                }
                if (stream < emitters.Count)
                    CreateBeam(emitters[stream++].position, city.settlement.root.position + Vector3.up * 3f,
                        new Color(0.2f, 0.9f, 1f, 1f), 0.03f, 0.1f);
            }
        }

        state.bloomAttackTimer -= dt;
        if (state.bloomAttackTimer <= 0f)
        {
            state.bloomAttackTimer = state.node.kind == ResourceKind.Gold ? 0.7f :
                state.node.kind == ResourceKind.Wind ? 0.95f : 1.2f;
            FireGiantBloomResourceAttack(state);
        }

        state.stormTimer -= dt;
        if (state.stormTimer <= 0f)
        {
            state.stormTimer = balanceProfile.nanoTreeSolarStormSeconds;
            float radius = state.node.kind == ResourceKind.Wind ? 165f : 130f;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (enemy == null || enemy.transform == null || enemy.health <= 0f ||
                    FlatDistance(enemy.transform.position, state.node.transform.position) > radius)
                    continue;
                enemy.health -= state.node.kind == ResourceKind.Gold ? 150f :
                    state.node.kind == ResourceKind.Sand ? 120f : 105f;
                CreateBeam(bloom.position + Vector3.up * 14f, enemy.transform.position + Vector3.up,
                    GetResourceDevelopmentColor(state.node.kind), 0.085f, 0.22f);
            }
            CleanupDeadEnemies();
            ShowBanner("GIANT BLOOM // " + GetResourceKitName(state.node.kind).ToUpperInvariant() + " STORM", 2.8f);
        }
    }

    private void FireGiantBloomResourceAttack(ResourceDevelopmentState state)
    {
        float range = state.node.kind == ResourceKind.Gold ? 175f :
            state.node.kind == ResourceKind.Wind ? 150f : 125f;
        EnemyUnit target = FindNearestEnemy(state.node.transform.position, range);
        if (target == null || target.transform == null)
            return;

        Vector3 origin = state.node.transform.position + Vector3.up * 15f;
        if (state.node.kind == ResourceKind.Wind)
        {
            Vector3 chainStart = origin;
            int hits = 0;
            for (int i = 0; i < enemies.Count && hits < 4; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (enemy == null || enemy.transform == null || enemy.health <= 0f ||
                    FlatDistance(enemy.transform.position, state.node.transform.position) > range)
                    continue;
                enemy.health -= 24f;
                CreateBeam(chainStart, enemy.transform.position + Vector3.up,
                    new Color(0.25f, 0.9f, 1f, 1f), 0.055f, 0.13f);
                chainStart = enemy.transform.position + Vector3.up;
                hits++;
            }
        }
        else if (state.node.kind == ResourceKind.Sand)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (enemy != null && enemy.transform != null && enemy.health > 0f &&
                    FlatDistance(enemy.transform.position, target.transform.position) <= 13f)
                    enemy.health -= 34f;
            }
            CreateBeam(origin, target.transform.position + Vector3.up,
                new Color(1f, 0.55f, 0.16f, 1f), 0.07f, 0.16f);
        }
        else
        {
            target.health -= 52f;
            CreateBeam(origin, target.transform.position + Vector3.up,
                new Color(1f, 0.88f, 0.25f, 1f), 0.065f, 0.15f);
        }
        CleanupDeadEnemies();
    }

    private void BuildThothBlessingSuperstructure(Transform carrier, ResourceKind resourceKind)
    {
        if (carrier == null || carrier.Find("Blessing_Of_Thoth_Superstructure") != null)
            return;
        Transform root = new GameObject("Blessing_Of_Thoth_Superstructure").transform;
        root.SetParent(carrier, false);
        Material kit = resourceKind == ResourceKind.Gold ? controlledMaterial :
            resourceKind == ResourceKind.Wind ? pyramidGlowMaterial : pyramidDarkArmorMaterial;

        CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Carrier_Spine", root,
            new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(12f, 0.75f, 16f),
            pyramidDarkArmorMaterial);
        CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Forward_Ark", root,
            new Vector3(0f, 1.35f, 8.2f), Quaternion.Euler(12f, 0f, 0f), new Vector3(7.8f, 1.6f, 4.2f), kit);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Wing_Deck_" + side, root,
                new Vector3(side * 9f, 0.9f, 0f), Quaternion.Euler(0f, side * 4f, side * -3f),
                new Vector3(7.2f, 0.42f, 15.5f), kit);
            CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Hangar_Block_" + side, root,
                new Vector3(side * 5.4f, 1.9f, -4.7f), Quaternion.identity,
                new Vector3(3.8f, 1.8f, 4.2f), pyramidDarkArmorMaterial);
            CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Hangar_Mouth_" + side, root,
                new Vector3(side * 5.4f, 1.8f, -6.85f), Quaternion.identity,
                new Vector3(2.8f, 1.05f, 0.2f), pyramidGlowMaterial);
        }
        CreateResourcePrimitive(PrimitiveType.Cube, "Temple_Of_Thoth_Lower", root,
            new Vector3(0f, 2.05f, 0f), Quaternion.identity, new Vector3(5.8f, 1.0f, 5.8f),
            pyramidDarkArmorMaterial);
        CreateResourcePrimitive(PrimitiveType.Cube, "Temple_Of_Thoth_Upper", root,
            new Vector3(0f, 3.1f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(4.2f, 0.72f, 4.2f), kit);
        CreateResourcePrimitive(PrimitiveType.Cylinder, "Temple_Of_Thoth", root,
            new Vector3(0f, 5f, 0f), Quaternion.identity, new Vector3(1.9f, 2.4f, 1.9f), kit);
        CreateResourcePrimitive(PrimitiveType.Sphere, "Temple_Ibis_Crown", root,
            new Vector3(0f, 7.8f, 0f), Quaternion.identity, Vector3.one * 1.55f, pyramidGlowMaterial);
        for (int i = 0; i < 8; i++)
        {
            int column = i % 4;
            int row = i / 4;
            Vector3 position = new Vector3((column - 1.5f) * 4.2f, 1.38f, row == 0 ? 4.25f : -4.25f);
            Transform deck = CreateResourcePrimitive(PrimitiveType.Cube, "Thoth_Flight_Deck_" + (i + 1),
                root, position, Quaternion.identity, new Vector3(3.55f, 0.24f, 6.4f), kit);
            CreateResourcePrimitive(PrimitiveType.Cube, "Deck_Launch_Rail", deck,
                new Vector3(0f, 0.22f, 0f), Quaternion.identity,
                new Vector3(0.16f, 0.09f, 5.6f), pyramidGlowMaterial);
            CreateResourcePrimitive(PrimitiveType.Cube, "Deck_Edge_Left", deck,
                new Vector3(-1.65f, 0.16f, 0f), Quaternion.identity,
                new Vector3(0.1f, 0.1f, 5.9f), pyramidGlowMaterial);
            CreateResourcePrimitive(PrimitiveType.Cube, "Deck_Edge_Right", deck,
                new Vector3(1.65f, 0.16f, 0f), Quaternion.identity,
                new Vector3(0.1f, 0.1f, 5.9f), pyramidGlowMaterial);
        }
    }

    private void UpdateUrbanEvolution(float dt)
    {
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || state.settlement.root == null ||
                state.settlement.health <= 0f)
                continue;

            float growthRate = state.network != null && state.network.online || state.horusPower ? 1.7f : 1f;
            state.autonomousGrowthTimer -= dt * growthRate;
            if (state.autonomousGrowthTimer <= 0f && state.cityTier < 3 && !state.underRaid)
            {
                state.autonomousGrowthTimer = 150f + state.cityTier * 75f;
                state.developmentPoints++;
                state.tradeTrust = Mathf.Min(100f, state.tradeTrust + 4f);
                PerformSettlementDevelopment(state);
            }
            EnsureSettlementUrbanTier(state);
        }

        foreach (KeyValuePair<ResourceNode, ResourceDevelopmentState> pair in resourceDevelopmentStates)
            EnsurePlayerBaseEvolution(pair.Value);
    }

    private void EnsureSettlementUrbanTier(SettlementDevelopmentState state)
    {
        int targetTier = SandRunnersEvolutionRules.SettlementCityTier(state.developmentPoints);
        string specialization = DetermineSettlementSpecialization(state);
        while (state.cityTier < targetTier)
        {
            state.cityTier++;
            BuildSettlementUrbanTier(state, state.cityTier, specialization);
        }
        state.urbanSpecialization = specialization;
    }

    private string DetermineSettlementSpecialization(SettlementDevelopmentState city)
    {
        foreach (KeyValuePair<ResourceNode, ResourceDevelopmentState> pair in resourceDevelopmentStates)
        {
            ResourceDevelopmentState resource = pair.Value;
            if (resource == null || resource.node == null || resource.node.transform == null ||
                FlatDistance(resource.node.transform.position, city.settlement.root.position) > 250f)
                continue;
            ResourceDevelopmentProject project;
            if (resource.projects.TryGetValue(ResourceDevelopmentKind.HorusNanoTree, out project) && project.level >= 2)
                return "NANO GROVE";
            if ((resource.projects.TryGetValue(ResourceDevelopmentKind.SalvageHive, out project) ||
                resource.projects.TryGetValue(ResourceDevelopmentKind.ScarabFortress, out project)) && project.level > 0)
                return "TECH NECROPOLIS";
        }
        if (city.settlement.kind == NeutralFactionKind.Grounders)
            return "FORTIFIED CITY";
        if (city.stage == SettlementDiplomacyStage.AlliedSettlement)
            return "ALLIED CITY";
        return city.settlement.kind == NeutralFactionKind.BlueElementals ? "ELEMENTAL CITY" : "DESERT CITY";
    }

    private void BuildSettlementUrbanTier(SettlementDevelopmentState state, int tier, string specialization)
    {
        if (state.urbanRoot == null)
        {
            state.urbanRoot = new GameObject("Evolving_City").transform;
            state.urbanRoot.SetParent(state.settlement.root, false);
        }
        Transform ring = new GameObject("City_Tier_" + tier + "_" + specialization.Replace(' ', '_')).transform;
        ring.SetParent(state.urbanRoot, false);
        Material material = state.settlement.accentMaterial;
        int buildings = 4 + tier * 3;
        float radius = 14f + tier * 7f;
        CreateResourcePrimitive(PrimitiveType.Cylinder, "City_Plaza_" + tier, ring,
            new Vector3(0f, 0.12f + tier * 0.02f, 0f), Quaternion.identity,
            new Vector3(radius * 0.78f, 0.08f, radius * 0.78f), pyramidDarkArmorMaterial);
        if (tier == 1)
        {
            CreateResourcePrimitive(PrimitiveType.Cylinder, "City_Central_Spire", ring,
                new Vector3(0f, 4.6f, 0f), Quaternion.identity, new Vector3(2.4f, 4.6f, 2.4f), material);
            CreateResourcePrimitive(PrimitiveType.Sphere, "City_Central_Beacon", ring,
                new Vector3(0f, 9.4f, 0f), Quaternion.identity, Vector3.one * 1.35f, pyramidGlowMaterial);
        }
        for (int i = 0; i < buildings; i++)
        {
            float angle = i * Mathf.PI * 2f / buildings + tier * 0.37f;
            Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            Vector3 roadPosition = p * 0.52f + Vector3.up * 0.2f;
            CreateResourcePrimitive(PrimitiveType.Cube, "City_Radial_Road_" + i, ring, roadPosition,
                Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f), new Vector3(1.4f, 0.06f, radius * 0.9f),
                controlledMaterial);
            if (specialization == "NANO GROVE")
            {
                Transform trunk = CreateResourcePrimitive(PrimitiveType.Cylinder, "Nano_Tree_" + i, ring,
                    p + Vector3.up * (2f + tier), Quaternion.identity,
                    new Vector3(0.72f, 2f + tier, 0.72f), controlledMaterial);
                CreateResourcePrimitive(PrimitiveType.Sphere, "Nano_Crown", trunk,
                    new Vector3(0f, 2.1f + tier, 0f), Quaternion.identity,
                    Vector3.one * (1.5f + tier * 0.38f), pyramidGlowMaterial);
            }
            else
            {
                float height = 2.8f + tier * 1.8f + (i % 3);
                CreateResourcePrimitive(specialization == "TECH NECROPOLIS" ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    specialization == "TECH NECROPOLIS" ? "Necropolis_Obelisk_" + i : "City_Block_" + i,
                    ring, p + Vector3.up * height * 0.5f, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f),
                    new Vector3(2.4f + tier * 0.35f, height * 0.5f, 2.4f + tier * 0.35f),
                    specialization == "TECH NECROPOLIS" ? pyramidDarkArmorMaterial : material);
            }
            Vector3 wallPosition = p * 1.12f + Vector3.up * (0.65f + tier * 0.22f);
            CreateResourcePrimitive(PrimitiveType.Cube, "City_Wall_" + i, ring, wallPosition,
                Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f),
                new Vector3(5.2f + tier, 0.65f + tier * 0.22f, 0.55f),
                specialization == "TECH NECROPOLIS" ? pyramidDarkArmorMaterial : material);
        }
        ShowBanner(state.settlement.displayName + " // " + specialization + " TIER " + tier, 3f);
    }

    private void EnsurePlayerBaseEvolution(ResourceDevelopmentState state)
    {
        if (state == null || state.node == null || state.node.transform == null || !state.node.controlled)
            return;
        int total = 0;
        ResourceDevelopmentKind dominant = ResourceDevelopmentKind.GoldenMine;
        int dominantLevel = 0;
        foreach (KeyValuePair<ResourceDevelopmentKind, ResourceDevelopmentProject> pair in state.projects)
        {
            if (pair.Value == null)
                continue;
            total += pair.Value.level;
            if (pair.Value.level > dominantLevel && pair.Key != ResourceDevelopmentKind.GoldenMine)
            {
                dominantLevel = pair.Value.level;
                dominant = pair.Key;
            }
        }
        int tier = total >= 6 ? 3 : total >= 4 ? 2 : total >= 2 ? 1 : 0;
        if (tier <= 0)
            return;
        Transform root = state.node.transform.Find("Player_Base_Evolution");
        if (root == null)
        {
            root = new GameObject("Player_Base_Evolution").transform;
            root.SetParent(state.node.transform, false);
        }
        for (int t = 1; t <= tier; t++)
        {
            if (root.Find("Base_Evolution_Tier_" + t) != null)
                continue;
            Transform ring = new GameObject("Base_Evolution_Tier_" + t).transform;
            ring.SetParent(root, false);
            float radius = 18f + t * 7f;
            for (int i = 0; i < 4 + t * 2; i++)
            {
                float angle = i * Mathf.PI * 2f / (4 + t * 2);
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 1.5f + t, Mathf.Cos(angle) * radius);
                Material mat = dominant == ResourceDevelopmentKind.HorusNanoTree ? pyramidGlowMaterial :
                    dominant == ResourceDevelopmentKind.ScarabFortress || dominant == ResourceDevelopmentKind.SalvageHive
                        ? pyramidDarkArmorMaterial : controlledMaterial;
                CreateResourcePrimitive(dominant == ResourceDevelopmentKind.HorusNanoTree ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    "Base_District_" + dominant + "_" + i, ring, p, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f),
                    new Vector3(1.4f + t * 0.3f, 1.5f + t, 1.4f + t * 0.3f), mat);
            }
        }
    }
}

internal static class SandRunnersEvolutionRules
{
    internal static int GiantBloomEmitterCount()
    {
        return 16;
    }

    internal static int SettlementCityTier(int developmentPoints)
    {
        if (developmentPoints >= 9) return 3;
        if (developmentPoints >= 5) return 2;
        if (developmentPoints >= 2) return 1;
        return 0;
    }

    internal static bool AllianceReady(int playerDeals, float trust, float influence, bool powerOnline)
    {
        return powerOnline && playerDeals >= 4 && trust >= 60f && influence >= 60f;
    }

    internal static float StrategicAcquisitionRange(bool autonomous, float unitRange)
    {
        return autonomous ? Mathf.Max(180f, unitRange * 3.5f) : unitRange + 7f;
    }
}