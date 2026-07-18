using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private sealed class MandarinkaGrowthPart
    {
        public Transform transform;
        public Vector3 targetScale;
        public float speed;
    }

    private sealed class MandarinkaOutpostRuntime
    {
        public MandarinkaResourceHolding holding;
        public float age;
        public int tier = 1;
        public float convoyTimer;
        public float fireTimer;
        public float motionSeed;
        public Transform mechanism;
        public Transform windDrone;
        public readonly List<EnemyUnit> garrison = new List<EnemyUnit>();
        public readonly List<Transform> muzzles = new List<Transform>();
    }

    private sealed class MandarinkaOccupationRuntime
    {
        public MandarinkaOccupation occupation;
        public float fireTimer;
        public int doctrineKind;
        public readonly List<EnemyUnit> garrison = new List<EnemyUnit>();
        public readonly List<Transform> muzzles = new List<Transform>();
    }

    private sealed class MandarinkaLogisticsConvoy
    {
        public MandarinkaResourceHolding holding;
        public Transform root;
        public EnemyUnit enemy;
        public ResourceKind kind;
        public Vector3 start;
        public Vector3 end;
        public float progress;
        public float travelSeconds;
        public float payload;
        public bool airborne;
    }

    private sealed class MandarinkaCastleMotion
    {
        public Transform transform;
        public Vector3 baseScale;
        public Vector3 basePosition;
        public Quaternion baseRotation;
        public float seed;
        public int mode;
    }

    private readonly Dictionary<MandarinkaResourceHolding, MandarinkaOutpostRuntime> mandarinkaOutpostRuntime =
        new Dictionary<MandarinkaResourceHolding, MandarinkaOutpostRuntime>();
    private readonly Dictionary<MandarinkaOccupation, MandarinkaOccupationRuntime> mandarinkaOccupationRuntime =
        new Dictionary<MandarinkaOccupation, MandarinkaOccupationRuntime>();
    private readonly List<MandarinkaLogisticsConvoy> mandarinkaLogisticsConvoys =
        new List<MandarinkaLogisticsConvoy>();
    private readonly List<MandarinkaGrowthPart> mandarinkaStructureGrowth =
        new List<MandarinkaGrowthPart>();
    private readonly List<MandarinkaCastleMotion> mandarinkaCastleMotion =
        new List<MandarinkaCastleMotion>();

    private float mandarinkaSandDoctrineTimer = 4f;

    private void ResetMandarinkaInfrastructure()
    {
        mandarinkaOutpostRuntime.Clear();
        mandarinkaOccupationRuntime.Clear();
        mandarinkaLogisticsConvoys.Clear();
        mandarinkaStructureGrowth.Clear();
        mandarinkaCastleMotion.Clear();
        mandarinkaSandDoctrineTimer = 4f;
    }

    private void InitializeMandarinkaOutpostInfrastructure(MandarinkaResourceHolding holding)
    {
        if (holding == null || holding.outpostRoot == null || mandarinkaOutpostRuntime.ContainsKey(holding))
            return;

        MandarinkaOutpostRuntime runtime = new MandarinkaOutpostRuntime();
        runtime.holding = holding;
        runtime.convoyTimer = Mathf.Max(12f, balanceProfile.mandarinkaConvoyIntervalSeconds * 0.45f);
        runtime.motionSeed = Random.Range(0f, 100f);
        mandarinkaOutpostRuntime.Add(holding, runtime);

        Transform root = holding.outpostRoot;
        if (holding.node.kind == ResourceKind.Sand)
        {
            GameObject wheel = CreateCylinder(root, "Sand_Excavator_Bucket_Wheel", new Vector3(0f, 4.8f, 0f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(2.5f, 0.45f, 2.5f), mandarinkaGoldMaterial, true);
            runtime.mechanism = wheel.transform;
            CreateBox(root, "Sand_Excavator_Arm", new Vector3(0f, 3.2f, -3.4f),
                Quaternion.Euler(28f, 0f, 0f), new Vector3(1f, 0.8f, 5.5f), mandarinkaDarkMaterial, true);
        }
        else if (holding.node.kind == ResourceKind.Gold)
        {
            GameObject crown = CreateCylinder(root, "Gold_Refinery_Crown", new Vector3(0f, 7.4f, 0f),
                Quaternion.identity, new Vector3(2.8f, 0.42f, 2.8f), mandarinkaGoldMaterial, true);
            runtime.mechanism = crown.transform;
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                CreateBox(root, "Gold_Smelter_Chimney_" + i,
                    Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 4.4f, 4.1f), Quaternion.identity,
                    new Vector3(0.8f, 6.2f, 0.8f), mandarinkaDarkMaterial, true);
            }
        }
        else
        {
            GameObject rotor = CreateCylinder(root, "Wind_Harvester_Rotor", new Vector3(0f, 8.2f, 0f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(3.2f, 0.35f, 3.2f), mandarinkaJadeMaterial, true);
            runtime.mechanism = rotor.transform;
            for (int blade = 0; blade < 4; blade++)
                CreateBox(rotor.transform, "Wind_Rotor_Blade_" + blade,
                    Quaternion.Euler(0f, blade * 90f, 0f) * new Vector3(0f, 0f, 3.2f),
                    Quaternion.Euler(0f, blade * 90f, 0f), new Vector3(0.55f, 0.18f, 5.4f), mandarinkaRedMaterial);
        }

        CreateMandarinkaOutpostGun(runtime, new Vector3(-7.5f, 1.2f, 0f));
        CreateMandarinkaOutpostGun(runtime, new Vector3(7.5f, 1.2f, 0f));
        RegisterMandarinkaStructureGrowth(root, 2.8f);
    }

    private void UpdateMandarinkaOutpostInfrastructure(MandarinkaResourceHolding holding, float dt)
    {
        MandarinkaOutpostRuntime runtime;
        if (holding == null || !mandarinkaOutpostRuntime.TryGetValue(holding, out runtime) || holding.outpostRoot == null)
            return;

        runtime.age += dt;
        int targetTier = SandRunnersOccupationRules.OutpostTier(runtime.age,
            balanceProfile.mandarinkaOutpostTierTwoSeconds, balanceProfile.mandarinkaOutpostTierThreeSeconds);
        while (runtime.tier < targetTier)
        {
            runtime.tier++;
            BuildMandarinkaOutpostTier(runtime, runtime.tier);
        }

        if (runtime.mechanism != null)
        {
            float direction = holding.node.kind == ResourceKind.Wind ? 170f : 72f;
            runtime.mechanism.Rotate(Vector3.up, direction * dt, Space.Self);
            Vector3 p = runtime.mechanism.localPosition;
            p.y += Mathf.Sin(Time.time * 2.2f + runtime.motionSeed) * 0.006f;
            runtime.mechanism.localPosition = p;
        }
        if (runtime.windDrone != null)
        {
            float angle = Time.time * 42f + runtime.motionSeed;
            runtime.windDrone.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 9f, 10f);
            runtime.windDrone.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);
        }

        UpdateMandarinkaOutpostDoctrine(runtime, dt);
        runtime.convoyTimer -= dt;
        if (runtime.convoyTimer <= 0f)
        {
            SpawnMandarinkaLogisticsConvoy(runtime);
            float interval = balanceProfile.mandarinkaConvoyIntervalSeconds;
            if (holding.node.kind == ResourceKind.Wind)
                interval *= 0.72f;
            runtime.convoyTimer = Mathf.Max(18f, interval);
        }
    }

    private void BuildMandarinkaOutpostTier(MandarinkaOutpostRuntime runtime, int tier)
    {
        Transform root = runtime.holding.outpostRoot;
        if (root == null)
            return;

        if (tier == 2)
        {
            CreateBox(root, "Outpost_Tier2_Armored_Depot", new Vector3(0f, 2.2f, 9.5f),
                Quaternion.identity, new Vector3(8f, 4f, 6f), mandarinkaDarkMaterial, true);
            CreateBox(root, "Outpost_Tier2_Red_Gate", new Vector3(0f, 2f, 6.3f),
                Quaternion.identity, new Vector3(4.8f, 3.5f, 0.7f), mandarinkaRedMaterial, true);
            CreateMandarinkaOutpostGun(runtime, new Vector3(0f, 1.2f, -8.5f));
        }
        else
        {
            CreateBox(root, "Outpost_Tier3_Extraction_Citadel", new Vector3(0f, 6f, 0f),
                Quaternion.identity, new Vector3(8.5f, 10f, 8.5f), mandarinkaRedMaterial, true);
            CreateCylinder(root, "Outpost_Tier3_Signal_Spire", new Vector3(0f, 15f, 0f),
                Quaternion.identity, new Vector3(1.2f, 5.5f, 1.2f), mandarinkaJadeMaterial);
            CreateMandarinkaOutpostGun(runtime, new Vector3(0f, 1.2f, 10f));
            if (runtime.holding.node.kind == ResourceKind.Wind)
                CreateMandarinkaOutpostWindDrone(runtime);
        }

        RegisterMandarinkaGrowthByPrefix(root, "Outpost_Tier" + tier);
        ShowBanner("IMPERIAL OUTPOST EVOLUTION // " + runtime.holding.node.kind.ToString().ToUpperInvariant() +
            " TIER " + tier, 2.8f);
    }

    private void CreateMandarinkaOutpostGun(MandarinkaOutpostRuntime runtime, Vector3 localPosition)
    {
        if (runtime == null || runtime.holding == null || runtime.holding.outpostRoot == null ||
            runtime.garrison.Count >= balanceProfile.mandarinkaOutpostGarrisonLimit)
            return;

        Transform root = new GameObject("Mandarinka_Outpost_Garrison_" + runtime.holding.node.kind + "_" +
            runtime.garrison.Count).transform;
        root.SetParent(runtime.holding.outpostRoot, false);
        root.localPosition = localPosition;
        CreateCylinder(root, "Garrison_Turret_Base", Vector3.zero, Quaternion.identity,
            new Vector3(1.7f, 0.65f, 1.7f), mandarinkaDarkMaterial, true);
        CreateBox(root, "Garrison_Turret_Housing", new Vector3(0f, 1.35f, 0f), Quaternion.identity,
            new Vector3(2.4f, 1.5f, 2.4f), mandarinkaRedMaterial, true);
        GameObject barrel = CreateCylinder(root, "Garrison_Turret_Barrel", new Vector3(0f, 1.7f, -2.2f),
            Quaternion.Euler(82f, 0f, 0f), new Vector3(0.42f, 2.8f, 0.42f),
            runtime.holding.node.kind == ResourceKind.Gold ? mandarinkaGoldMaterial : mandarinkaDarkMaterial);
        runtime.garrison.Add(RegisterMandarinkaEnemy(root, 150f + runtime.tier * 45f));
        runtime.muzzles.Add(barrel.transform);
        RegisterMandarinkaStructureGrowth(root, 2.2f);
    }

    private void CreateMandarinkaOutpostWindDrone(MandarinkaOutpostRuntime runtime)
    {
        Transform root = new GameObject("Mandarinka_Wind_Outpost_Interceptor").transform;
        root.SetParent(runtime.holding.outpostRoot, false);
        root.localPosition = new Vector3(0f, 9f, 10f);
        CreateBox(root, "Drone_Core", Vector3.zero, Quaternion.identity,
            new Vector3(2.4f, 0.8f, 3f), mandarinkaDarkMaterial, true);
        CreateBox(root, "Drone_Left_Sail", new Vector3(-2.2f, 0f, 0f), Quaternion.Euler(0f, 0f, 8f),
            new Vector3(3.4f, 0.15f, 3.8f), mandarinkaRedMaterial);
        CreateBox(root, "Drone_Right_Sail", new Vector3(2.2f, 0f, 0f), Quaternion.Euler(0f, 0f, -8f),
            new Vector3(3.4f, 0.15f, 3.8f), mandarinkaRedMaterial);
        runtime.windDrone = root;
        runtime.garrison.Add(RegisterMandarinkaEnemy(root, 135f));
        runtime.muzzles.Add(root);
        RegisterMandarinkaStructureGrowth(root, 2.4f);
    }

    private void UpdateMandarinkaOutpostDoctrine(MandarinkaOutpostRuntime runtime, float dt)
    {
        if (runtime.holding.commandEnemy == null || runtime.holding.commandEnemy.health <= 0f)
            return;

        if (runtime.holding.node.kind == ResourceKind.Gold)
            runtime.holding.commandEnemy.health = Mathf.Min(480f + runtime.tier * 140f,
                runtime.holding.commandEnemy.health + dt * (3f + runtime.tier * 1.5f));

        runtime.fireTimer -= dt;
        if (runtime.fireTimer > 0f)
            return;

        float range = runtime.holding.node.kind == ResourceKind.Sand ? 155f :
            runtime.holding.node.kind == ResourceKind.Gold ? 88f : 112f;
        RunnerUnit target = FindNearestRunner(runtime.holding.outpostRoot.position, range);
        if (target == null || target.transform == null)
            return;

        int resourceKind = runtime.holding.node.kind == ResourceKind.Sand ? 0 :
            runtime.holding.node.kind == ResourceKind.Gold ? 1 : 2;
        float damage = SandRunnersOccupationRules.DoctrineDamage(resourceKind, runtime.tier);
        float cooldown = resourceKind == 0 ? 4.2f : resourceKind == 1 ? 1.55f : 0.85f;
        runtime.fireTimer = cooldown;

        for (int i = 0; i < runtime.garrison.Count; i++)
        {
            EnemyUnit gun = runtime.garrison[i];
            if (gun == null || gun.health <= 0f || gun.transform == null)
                continue;
            Transform muzzle = i < runtime.muzzles.Count ? runtime.muzzles[i] : gun.transform;
            RotateFlatToward(gun.transform, target.transform.position - gun.transform.position, 140f * cooldown);
            target.health -= damage / Mathf.Max(1, runtime.garrison.Count);
            Color color = resourceKind == 0 ? new Color(1f, 0.42f, 0.05f, 1f) :
                resourceKind == 1 ? new Color(1f, 0.82f, 0.18f, 1f) : new Color(0.15f, 1f, 0.72f, 1f);
            CreateBeam(muzzle.position, target.transform.position + Vector3.up, color,
                resourceKind == 0 ? 0.1f : 0.055f, 0.18f);
        }
    }

    private void ReleaseMandarinkaOutpostInfrastructure(MandarinkaResourceHolding holding)
    {
        if (holding == null)
            return;
        mandarinkaOutpostRuntime.Remove(holding);
        for (int i = mandarinkaLogisticsConvoys.Count - 1; i >= 0; i--)
        {
            MandarinkaLogisticsConvoy convoy = mandarinkaLogisticsConvoys[i];
            if (convoy != null && convoy.holding == holding)
            {
                if (convoy.root != null)
                    Destroy(convoy.root.gameObject);
                mandarinkaLogisticsConvoys.RemoveAt(i);
            }
        }
    }

    private void SpawnMandarinkaLogisticsConvoy(MandarinkaOutpostRuntime runtime)
    {
        if (runtime == null || runtime.holding == null || runtime.holding.outpostRoot == null ||
            mandarinkaFortressRoot == null)
            return;

        MandarinkaLogisticsConvoy convoy = new MandarinkaLogisticsConvoy();
        convoy.holding = runtime.holding;
        convoy.kind = runtime.holding.node.kind;
        convoy.airborne = convoy.kind == ResourceKind.Wind;
        convoy.start = runtime.holding.outpostRoot.position;
        convoy.end = mandarinkaFortressRoot.position;
        float distance = FlatDistance(convoy.start, convoy.end);
        float speed = convoy.airborne ? balanceProfile.mandarinkaConvoyAirSpeed :
            balanceProfile.mandarinkaConvoyGroundSpeed;
        convoy.travelSeconds = SandRunnersOccupationRules.ConvoyTravelSeconds(distance, speed);
        convoy.payload = (convoy.kind == ResourceKind.Sand ? 16f :
            convoy.kind == ResourceKind.Gold ? 13f : 10f) * (0.7f + runtime.tier * 0.3f);

        Transform root = new GameObject("Mandarinka_Logistics_Convoy_" + convoy.kind).transform;
        root.SetParent(mandarinkaTerritoryRoot, true);
        root.position = convoy.start;
        convoy.root = root;
        CreateBox(root, "Convoy_Armored_Hull", Vector3.zero, Quaternion.identity,
            convoy.airborne ? new Vector3(4.8f, 1.2f, 7f) : new Vector3(3.6f, 1.8f, 6f),
            mandarinkaDarkMaterial, true);
        CreateBox(root, "Convoy_Resource_Capsule", new Vector3(0f, 1.4f, 0.4f), Quaternion.identity,
            new Vector3(2.5f, 1.8f, 3.4f),
            convoy.kind == ResourceKind.Gold ? mandarinkaGoldMaterial :
            convoy.kind == ResourceKind.Wind ? mandarinkaJadeMaterial : mandarinkaRedMaterial, true);
        for (int side = -1; side <= 1; side += 2)
        {
            if (convoy.airborne)
                CreateBox(root, "Convoy_Sail_" + side, new Vector3(side * 3.6f, 0.5f, 0f),
                    Quaternion.Euler(0f, 0f, side * -8f), new Vector3(4.8f, 0.18f, 5.6f),
                    mandarinkaRedMaterial);
            else
                CreateBox(root, "Convoy_Track_" + side, new Vector3(side * 2f, -0.75f, 0f),
                    Quaternion.identity, new Vector3(0.7f, 0.9f, 6.4f), mandarinkaRedMaterial, true);
        }
        bool importedTruck = !convoy.airborne && TryAttachMandarinkaImportedVehicle(root,
            "SR_MineConvoyTruck", 8f, Quaternion.identity, Vector3.zero,
            mandarinkaDarkMaterial, true);
        if (importedTruck)
        {
            CreateBox(root, "Convoy_Imported_Resource_Module", new Vector3(0f, 1.65f, 1.65f),
                Quaternion.identity, new Vector3(2.55f, 1.25f, 2.7f),
                convoy.kind == ResourceKind.Gold ? mandarinkaGoldMaterial : mandarinkaRedMaterial);
            CreateBox(root, "Convoy_Imported_Red_Armor_Band", new Vector3(0f, 1.2f, -1.85f),
                Quaternion.identity, new Vector3(3.2f, 0.25f, 0.65f), mandarinkaRedMaterial);
        }
        convoy.enemy = RegisterMandarinkaEnemy(root, convoy.airborne ? 175f : 240f);
        mandarinkaLogisticsConvoys.Add(convoy);
        RegisterMandarinkaStructureGrowth(root, 1.3f);
    }

    private void UpdateMandarinkaLogisticsConvoys(float dt)
    {
        for (int i = mandarinkaLogisticsConvoys.Count - 1; i >= 0; i--)
        {
            MandarinkaLogisticsConvoy convoy = mandarinkaLogisticsConvoys[i];
            if (convoy == null || convoy.root == null || convoy.enemy == null || convoy.enemy.health <= 0f)
            {
                if (convoy != null && convoy.root != null)
                    Destroy(convoy.root.gameObject);
                mandarinkaLogisticsConvoys.RemoveAt(i);
                continue;
            }

            if (mandarinkaFortressRoot != null)
                convoy.end = mandarinkaFortressRoot.position;
            convoy.progress = Mathf.Clamp01(convoy.progress + dt / convoy.travelSeconds);
            Vector3 position = Vector3.Lerp(convoy.start, convoy.end, convoy.progress);
            if (convoy.airborne)
                position.y = GetPlayableGroundHeight(position) + 11f + Mathf.Sin(convoy.progress * Mathf.PI) * 9f;
            else
                position.y = GetPlayableGroundHeight(position) + 1.1f;
            Vector3 direction = position - convoy.root.position;
            convoy.root.position = position;
            RotateFlatToward(convoy.root, direction, 170f * dt);

            if (convoy.progress < 1f)
                continue;

            if (convoy.kind == ResourceKind.Sand) mandarinkaSandStock += convoy.payload;
            else if (convoy.kind == ResourceKind.Gold) mandarinkaGoldStock += convoy.payload;
            else mandarinkaWindStock += convoy.payload;
            mandarinkaSupply = Mathf.Min(720f, mandarinkaSupply + convoy.payload * 1.4f);
            CreateBeam(convoy.root.position + Vector3.up, mandarinkaFortressRoot.position + Vector3.up * 8f,
                new Color(1f, 0.62f, 0.12f, 1f), 0.08f, 0.28f);
            Destroy(convoy.root.gameObject);
            mandarinkaLogisticsConvoys.RemoveAt(i);
        }
    }

    private void InitializeMandarinkaOccupationInfrastructure(MandarinkaOccupation occupation)
    {
        if (occupation == null || occupation.occupationRoot == null || mandarinkaOccupationRuntime.ContainsKey(occupation))
            return;

        MandarinkaOccupationRuntime runtime = new MandarinkaOccupationRuntime();
        runtime.occupation = occupation;
        string faction = occupation.settlement.settlement.kind.ToString();
        runtime.doctrineKind = faction.Contains("Ground") ? 0 : faction.Contains("Black") ? 2 : 1;
        mandarinkaOccupationRuntime.Add(occupation, runtime);
        CreateBox(occupation.occupationRoot, "Occupation_Rising_Banner", new Vector3(0f, 15f, 0f),
            Quaternion.identity, new Vector3(0.35f, 10f, 0.35f), mandarinkaGoldMaterial);
        CreateBox(occupation.occupationRoot, "Occupation_Command_Flag", new Vector3(2.8f, 18f, 0f),
            Quaternion.Euler(0f, 0f, -4f), new Vector3(5.5f, 3.2f, 0.18f), mandarinkaRedMaterial);
        ExpandMandarinkaOccupationGarrison(occupation, 2);
        RegisterMandarinkaStructureGrowth(occupation.occupationRoot, 3.5f);
    }

    private void ExpandMandarinkaOccupationGarrison(MandarinkaOccupation occupation, int amount)
    {
        MandarinkaOccupationRuntime runtime;
        if (occupation == null || occupation.occupationRoot == null ||
            !mandarinkaOccupationRuntime.TryGetValue(occupation, out runtime))
            return;

        for (int n = 0; n < amount; n++)
        {
            int index = runtime.garrison.Count;
            float angle = index * 137.5f;
            Vector3 local = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 1.2f, 13.5f);
            Transform root = new GameObject("Mandarinka_Occupation_Garrison_" + index).transform;
            root.SetParent(occupation.occupationRoot, false);
            root.localPosition = local;
            root.localRotation = Quaternion.Euler(0f, angle + 180f, 0f);
            CreateCylinder(root, "Occupation_Gun_Base", Vector3.zero, Quaternion.identity,
                new Vector3(1.8f, 0.7f, 1.8f), mandarinkaDarkMaterial, true);
            CreateBox(root, "Occupation_Gun_Casemate", new Vector3(0f, 1.5f, 0f), Quaternion.identity,
                new Vector3(2.7f, 1.8f, 2.5f), mandarinkaRedMaterial, true);
            GameObject barrel = CreateCylinder(root, "Occupation_Gun_Barrel", new Vector3(0f, 1.8f, -2.8f),
                Quaternion.Euler(82f, 0f, 0f), new Vector3(0.42f, 3.2f, 0.42f), mandarinkaGoldMaterial);
            runtime.garrison.Add(RegisterMandarinkaEnemy(root, 190f + index * 28f));
            runtime.muzzles.Add(barrel.transform);
            RegisterMandarinkaStructureGrowth(root, 2.4f);
        }
    }

    private void UpdateMandarinkaOccupationInfrastructure(MandarinkaOccupation occupation, float dt)
    {
        MandarinkaOccupationRuntime runtime;
        if (occupation == null || occupation.occupationRoot == null ||
            !mandarinkaOccupationRuntime.TryGetValue(occupation, out runtime))
            return;

        runtime.fireTimer -= dt;
        if (runtime.fireTimer > 0f)
            return;

        float range = runtime.doctrineKind == 0 ? 145f : runtime.doctrineKind == 1 ? 92f : 118f;
        RunnerUnit target = FindNearestRunner(occupation.occupationRoot.position, range);
        if (target == null || target.transform == null)
            return;

        int tier = occupation.stage >= MandarinkaOccupationStage.Exhausted ? 3 :
            occupation.stage >= MandarinkaOccupationStage.Fortified ? 2 : 1;
        float damage = SandRunnersOccupationRules.DoctrineDamage(runtime.doctrineKind, tier);
        runtime.fireTimer = runtime.doctrineKind == 0 ? 3.8f : runtime.doctrineKind == 1 ? 1.45f : 0.78f;
        int alive = 0;
        for (int i = 0; i < runtime.garrison.Count; i++)
            if (runtime.garrison[i] != null && runtime.garrison[i].health > 0f && runtime.garrison[i].transform != null)
                alive++;
        if (alive == 0)
            return;

        for (int i = 0; i < runtime.garrison.Count; i++)
        {
            EnemyUnit gun = runtime.garrison[i];
            if (gun == null || gun.health <= 0f || gun.transform == null)
                continue;
            Transform muzzle = i < runtime.muzzles.Count ? runtime.muzzles[i] : gun.transform;
            RotateFlatToward(gun.transform, target.transform.position - gun.transform.position, 150f * dt);
            target.health -= damage / alive;
            Color color = runtime.doctrineKind == 0 ? new Color(1f, 0.3f, 0.03f, 1f) :
                runtime.doctrineKind == 1 ? new Color(1f, 0.78f, 0.16f, 1f) : new Color(0.1f, 1f, 0.62f, 1f);
            CreateBeam(muzzle.position, target.transform.position + Vector3.up, color, 0.065f, 0.17f);
        }
    }

    private void ReleaseMandarinkaOccupationInfrastructure(MandarinkaOccupation occupation)
    {
        if (occupation != null)
            mandarinkaOccupationRuntime.Remove(occupation);
    }

    private void RegisterMandarinkaStructureGrowth(Transform root, float duration)
    {
        if (root == null)
            return;
        Transform[] parts = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i];
            if (part == null || part == root || part.name.Contains("Influence_Ring") ||
                part.name.Contains("Territory_Label"))
                continue;
            RegisterMandarinkaGrowth(part, duration);
        }
    }

    private void RegisterMandarinkaGrowthByPrefix(Transform root, string prefix)
    {
        if (root == null)
            return;
        Transform[] parts = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
            if (parts[i] != null && parts[i].name.StartsWith(prefix))
                RegisterMandarinkaGrowth(parts[i], 3f);
    }

    private void RegisterMandarinkaGrowth(Transform part, float duration)
    {
        for (int i = 0; i < mandarinkaStructureGrowth.Count; i++)
            if (mandarinkaStructureGrowth[i].transform == part)
                return;
        MandarinkaGrowthPart growth = new MandarinkaGrowthPart();
        growth.transform = part;
        growth.targetScale = part.localScale;
        growth.speed = 1f / Mathf.Max(0.1f, duration);
        part.localScale = new Vector3(growth.targetScale.x * 0.08f,
            growth.targetScale.y * 0.04f, growth.targetScale.z * 0.08f);
        mandarinkaStructureGrowth.Add(growth);
    }

    private void UpdateMandarinkaStructureGrowth(float dt)
    {
        for (int i = mandarinkaStructureGrowth.Count - 1; i >= 0; i--)
        {
            MandarinkaGrowthPart growth = mandarinkaStructureGrowth[i];
            if (growth == null || growth.transform == null)
            {
                mandarinkaStructureGrowth.RemoveAt(i);
                continue;
            }
            growth.transform.localScale = Vector3.MoveTowards(growth.transform.localScale,
                growth.targetScale, growth.targetScale.magnitude * growth.speed * dt);
            if ((growth.transform.localScale - growth.targetScale).sqrMagnitude <= 0.0004f)
            {
                growth.transform.localScale = growth.targetScale;
                mandarinkaStructureGrowth.RemoveAt(i);
            }
        }
    }

    private void RegisterMandarinkaCastleStructure(Transform root)
    {
        if (root == null)
            return;
        RegisterMandarinkaStructureGrowth(root, 4.5f);
        Transform[] parts = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i];
            if (part == null || part == root)
                continue;
            int mode = part.name.Contains("Wind_Turbine") ? 1 :
                part.name.Contains("Storm_Sail_") ? 2 :
                part.name.Contains("Gold_Shield_Pylon") ? 3 :
                part.name.Contains("Sand_Mortar") ? 4 : 0;
            if (mode == 0)
                continue;
            MandarinkaCastleMotion motion = new MandarinkaCastleMotion();
            motion.transform = part;
            motion.baseScale = part.localScale;
            motion.basePosition = part.localPosition;
            motion.baseRotation = part.localRotation;
            motion.seed = Random.Range(0f, 10f);
            motion.mode = mode;
            mandarinkaCastleMotion.Add(motion);
        }
    }

    private void UpdateMandarinkaCastleInfrastructure(float dt)
    {
        for (int i = mandarinkaCastleMotion.Count - 1; i >= 0; i--)
        {
            MandarinkaCastleMotion motion = mandarinkaCastleMotion[i];
            if (motion == null || motion.transform == null)
            {
                mandarinkaCastleMotion.RemoveAt(i);
                continue;
            }
            if (motion.mode == 1)
                motion.transform.localRotation = motion.baseRotation * Quaternion.Euler(0f, 0f, Time.time * 190f);
            else if (motion.mode == 2)
                motion.transform.localRotation = motion.baseRotation *
                    Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.4f + motion.seed) * 7f, 0f);
            else if (motion.mode == 3)
                motion.transform.localScale = motion.baseScale *
                    (1f + Mathf.Sin(Time.time * 3f + motion.seed) * 0.08f);
            else if (motion.mode == 4)
                motion.transform.localPosition = motion.basePosition +
                    Vector3.forward * Mathf.Max(0f, Mathf.Sin(Time.time * 1.2f + motion.seed)) * 0.35f;
        }

        if (!mandarinkaCastleSandKit || mandarinkaFortressRoot == null || mandarinkaDefeated)
            return;
        mandarinkaSandDoctrineTimer -= dt;
        if (mandarinkaSandDoctrineTimer > 0f)
            return;
        RunnerUnit target = FindNearestRunner(mandarinkaFortressRoot.position, 210f);
        if (target == null || target.transform == null)
            return;
        mandarinkaSandDoctrineTimer = 6.5f;
        target.health -= 42f;
        CreateBeam(mandarinkaFortressRoot.position + Vector3.up * 13f,
            target.transform.position + Vector3.up, new Color(1f, 0.38f, 0.03f, 1f), 0.12f, 0.24f);
    }

    private string GetMandarinkaDoctrineHud()
    {
        string result = mandarinkaCastleSandKit ? "SIEGE" : "-";
        result += mandarinkaCastleGoldKit ? "/MINT" : "/-";
        result += mandarinkaCastleWindKit ? "/STORM" : "/-";
        return result;
    }
}