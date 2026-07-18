using System.Collections.Generic;
using UnityEngine;

internal static class SandRunnersOccupationRules
{
    internal static float CaptureDelta(float deltaTime, int builders, int garrison, float defense, float captureSeconds)
    {
        if (deltaTime <= 0f || builders <= 0)
            return 0f;
        float pressure = builders + Mathf.Max(0, garrison - builders) * 0.32f;
        float resistance = 1f + Mathf.Max(0f, defense) * 0.075f;
        return deltaTime * pressure / (Mathf.Max(1f, captureSeconds) * resistance);
    }

    internal static float PopulationDrainPerSecond(int stage)
    {
        if (stage >= 4) return 0.14f;
        if (stage >= 3) return 0.09f;
        if (stage >= 2) return 0.045f;
        return 0f;
    }

    internal static float InfluenceAt(float distance, float radius, float strength)
    {
        if (radius <= 0f || distance >= radius)
            return 0f;
        float t = 1f - Mathf.Clamp01(distance / radius);
        return Mathf.Max(0f, strength) * t * t;
    }

    internal static float StrategicTargetScore(float baseValue, float distance, float defense, float opposingInfluence, bool reserved)
    {
        return baseValue - Mathf.Max(0f, distance) * 0.035f - Mathf.Max(0f, defense) * 2.4f -
               Mathf.Max(0f, opposingInfluence) * 0.18f - (reserved ? 42f : 0f);
    }

    internal static bool ResourceKitReady(float stock, float threshold, bool alreadyBuilt)
    {
        return !alreadyBuilt && threshold > 0f && stock >= threshold;
    }

    internal static int OutpostTier(float age, float tierTwoSeconds, float tierThreeSeconds)
    {
        if (age >= Mathf.Max(tierTwoSeconds, tierThreeSeconds)) return 3;
        if (age >= Mathf.Min(tierTwoSeconds, tierThreeSeconds)) return 2;
        return 1;
    }

    internal static float ConvoyTravelSeconds(float distance, float speed)
    {
        return Mathf.Max(0.1f, Mathf.Max(0f, distance) / Mathf.Max(0.1f, speed));
    }

    internal static float DoctrineDamage(int resourceKind, int tier)
    {
        int safeTier = Mathf.Clamp(tier, 1, 3);
        if (resourceKind == 0) return 20f + safeTier * 7f;
        if (resourceKind == 1) return 9f + safeTier * 4f;
        return 7f + safeTier * 3f;
    }
}

public partial class SandRunnersPrototype
{
    private enum MandarinkaOccupationStage
    {
        Free,
        Encircled,
        Occupied,
        Fortified,
        Exhausted,
        Ruined
    }

    private sealed class MandarinkaOccupation
    {
        public SettlementDevelopmentState settlement;
        public MandarinkaOccupationStage stage;
        public float captureProgress;
        public float population = 100f;
        public float occupationAge;
        public float lastPressureTime;
        public float fireTimer;
        public Transform occupationRoot;
        public EnemyUnit commandEnemy;
    }

    private sealed class MandarinkaResourceHolding
    {
        public ResourceNode node;
        public float captureProgress;
        public bool held;
        public Transform outpostRoot;
        public EnemyUnit commandEnemy;
    }

    private readonly Dictionary<SettlementDevelopmentState, MandarinkaOccupation> mandarinkaOccupations =
        new Dictionary<SettlementDevelopmentState, MandarinkaOccupation>();
    private readonly Dictionary<ResourceNode, MandarinkaResourceHolding> mandarinkaResourceHoldings =
        new Dictionary<ResourceNode, MandarinkaResourceHolding>();

    private Transform mandarinkaTerritoryRoot;
    private bool mandarinkaTerritoryInitialized;
    private float mandarinkaTerritoryPlanTimer;
    private Vector3 mandarinkaTerritoryObjective;
    private string mandarinkaTerritoryObjectiveLabel = "SCOUTING";
    private float mandarinkaSandStock;
    private float mandarinkaGoldStock;
    private float mandarinkaWindStock;
    private bool mandarinkaCastleSandKit;
    private bool mandarinkaCastleGoldKit;
    private bool mandarinkaCastleWindKit;

    private void InitializeMandarinkaTerritory()
    {
        if (mandarinkaTerritoryInitialized)
            return;

        mandarinkaTerritoryInitialized = true;
        GameObject old = GameObject.Find("SandRunners_Mandarinka_Territory_Runtime");
        if (old != null)
            Destroy(old);
        mandarinkaTerritoryRoot = new GameObject("SandRunners_Mandarinka_Territory_Runtime").transform;
        mandarinkaOccupations.Clear();
        mandarinkaResourceHoldings.Clear();
        // Finite palace reserve; sustained production requires captured nodes
        // and their physical logistics convoys.
        mandarinkaSandStock = 42f;
        mandarinkaGoldStock = 36f;
        mandarinkaWindStock = 28f;
        mandarinkaSupply = 86f;
        mandarinkaCastleSandKit = false;
        mandarinkaCastleGoldKit = false;
        mandarinkaCastleWindKit = false;
        ResetMandarinkaInfrastructure();
        SynchronizeMandarinkaTerritoryState();
        ChooseMandarinkaTerritoryPlan();
    }

    private void UpdateMandarinkaTerritory(float dt)
    {
        if (!mandarinkaTerritoryInitialized)
            InitializeMandarinkaTerritory();
        if (mandarinkaTerritoryRoot == null)
            return;

        SynchronizeMandarinkaTerritoryState();
        mandarinkaTerritoryPlanTimer -= dt;
        if (mandarinkaTerritoryPlanTimer <= 0f)
        {
            mandarinkaTerritoryPlanTimer = Mathf.Max(0.5f, balanceProfile.aiPlanInterval);
            ChooseMandarinkaTerritoryPlan();
        }

        UpdateMandarinkaResourceHoldings(dt);
        UpdateMandarinkaLogisticsConvoys(dt);
        UpdateMandarinkaOccupations(dt);
        TryDevelopMandarinkaFortressFromTerritory();
        UpdateMandarinkaCastleInfrastructure(dt);
        UpdateMandarinkaStructureGrowth(dt);
    }

    private void SynchronizeMandarinkaTerritoryState()
    {
        SynchronizeSettlementDevelopment();
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || mandarinkaOccupations.ContainsKey(state))
                continue;
            MandarinkaOccupation occupation = new MandarinkaOccupation();
            occupation.settlement = state;
            mandarinkaOccupations.Add(state, occupation);
        }

        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || mandarinkaResourceHoldings.ContainsKey(node))
                continue;
            MandarinkaResourceHolding holding = new MandarinkaResourceHolding();
            holding.node = node;
            mandarinkaResourceHoldings.Add(node, holding);
        }
    }

    private void ChooseMandarinkaTerritoryPlan()
    {
        float bestScore = float.MinValue;
        Vector3 bestPosition = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position : Vector3.zero;
        string bestLabel = "REGROUP AT PALACE";
        int heldNodes = CountMandarinkaHeldResourceNodes();
        bool mayOccupy = heldNodes > 0 && mandarinkaStrategyPhase >= MandarinkaStrategyPhase.ResourceRaid;

        foreach (KeyValuePair<ResourceNode, MandarinkaResourceHolding> pair in mandarinkaResourceHoldings)
        {
            MandarinkaResourceHolding holding = pair.Value;
            if (holding == null || holding.node == null || holding.node.transform == null || holding.held)
                continue;

            ResourceNode node = holding.node;
            float need = node.kind == ResourceKind.Sand ? Mathf.InverseLerp(180f, 0f, mandarinkaSandStock) :
                node.kind == ResourceKind.Gold ? Mathf.InverseLerp(150f, 0f, mandarinkaGoldStock) :
                Mathf.InverseLerp(110f, 0f, mandarinkaWindStock);
            float distance = mandarinkaFortressRoot != null
                ? FlatDistance(mandarinkaFortressRoot.position, node.transform.position)
                : 0f;
            bool reserved = CountMandarinkaBuildersAimingNear(node.transform.position, 18f) > 0;
            float playerInfluence = GetPlayerInfluenceAt(node.transform.position);
            float score = SandRunnersMandarinkaStrategicRules.TargetScore(
                76f + need * 48f + (node.controlled ? 18f : 0f), 0.45f, distance, 0f,
                playerInfluence, 1f - Mathf.Clamp01(playerInfluence / 220f), reserved);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = node.transform.position;
                bestLabel = "SEIZE " + node.label.ToUpperInvariant();
            }
        }

        if (mayOccupy)
        {
            foreach (KeyValuePair<SettlementDevelopmentState, MandarinkaOccupation> pair in mandarinkaOccupations)
            {
                MandarinkaOccupation occupation = pair.Value;
                if (occupation == null || occupation.settlement == null || occupation.settlement.settlement == null ||
                    occupation.settlement.settlement.root == null || occupation.settlement.settlement.health <= 0f ||
                    occupation.stage >= MandarinkaOccupationStage.Occupied)
                    continue;

                SettlementDevelopmentState state = occupation.settlement;
                Vector3 position = state.settlement.root.position;
                float distance = mandarinkaFortressRoot != null
                    ? FlatDistance(mandarinkaFortressRoot.position, position)
                    : 0f;
                bool reserved = CountMandarinkaBuildersAimingNear(position, 22f) > 0;
                float value = 106f + state.cityTier * 16f + state.developmentPoints * 4f;
                if (state.stage == SettlementDiplomacyStage.AlliedSettlement)
                    value += 28f;
                float influence = GetPlayerInfluenceAt(position);
                float score = SandRunnersMandarinkaStrategicRules.TargetScore(
                    value, occupation.captureProgress, distance, state.defenseLevel, influence,
                    1f - Mathf.Clamp01(influence / 220f), reserved);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                    bestLabel = "OCCUPY " + state.settlement.displayName;
                }
            }
        }

        mandarinkaTerritoryObjective = GetDunePoint(bestPosition);
        mandarinkaTerritoryObjectiveLabel = bestLabel;
    }

    private int CountMandarinkaBuildersAimingNear(Vector3 position, float radius)
    {
        int count = 0;
        for (int i = 0; i < mandarinkaAssets.Count; i++)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset != null && asset.role == MandarinkaRole.Builder && asset.transform != null &&
                asset.hasObjective && FlatDistance(asset.objective, position) <= radius)
                count++;
        }
        return count;
    }

    private Vector3 PickMandarinkaExpansionObjective(Vector3 origin)
    {
        if (mandarinkaTerritoryObjectiveLabel != "SCOUTING" &&
            FlatDistance(origin, mandarinkaTerritoryObjective) < mapHalfSize * 2f)
            return mandarinkaTerritoryObjective;

        return GetDunePoint(mandarinkaFortressRoot != null
            ? mandarinkaFortressRoot.position + Random.insideUnitSphere * 80f
            : origin + Random.insideUnitSphere * 80f);
    }

    private bool TryGetMandarinkaStrategicTerritoryObjective(out Vector3 objective)
    {
        objective = mandarinkaTerritoryObjective;
        return mandarinkaTerritoryInitialized && !string.IsNullOrEmpty(mandarinkaTerritoryObjectiveLabel) &&
               mandarinkaTerritoryObjectiveLabel != "SCOUTING";
    }

    private bool UpdateMandarinkaBuilderExpansion(MandarinkaAsset asset, float dt)
    {
        if (asset == null || asset.transform == null)
            return false;

        MandarinkaResourceHolding resource = FindMandarinkaResourceHoldingNear(asset.transform.position, 10f);
        if (resource != null && !resource.held)
        {
            int garrison = CountMandarinkaGarrison(resource.node.transform.position, 55f);
            resource.captureProgress = Mathf.Clamp01(resource.captureProgress +
                SandRunnersOccupationRules.CaptureDelta(dt, 1, garrison,
                    resource.node.controlled ? 3f : 0f, balanceProfile.mandarinkaResourceCaptureSeconds));
            resource.node.capture = Mathf.Max(0f, 1f - resource.captureProgress);
            asset.gatherTimer = 0f;
            if (resource.captureProgress >= 1f)
            {
                SecureMandarinkaResourceNode(resource);
                asset.hasObjective = false;
                ChooseMandarinkaTerritoryPlan();
            }
            return true;
        }

        MandarinkaOccupation occupation = FindMandarinkaOccupationNear(asset.transform.position, 14f);
        if (occupation != null && occupation.stage < MandarinkaOccupationStage.Occupied &&
            occupation.settlement.settlement.health > 0f)
        {
            int garrison = CountMandarinkaGarrison(occupation.settlement.settlement.root.position, 72f);
            if (occupation.stage == MandarinkaOccupationStage.Free)
            {
                occupation.stage = MandarinkaOccupationStage.Encircled;
                ShowBanner("MANDARINKA ENCIRCLEMENT // " + occupation.settlement.settlement.displayName, 3f);
                SetMandarinkaRadio("MANDARINKA: Seal the roads. Their market now belongs to my court.");
            }
            occupation.lastPressureTime = Time.time;
            occupation.captureProgress = Mathf.Clamp01(occupation.captureProgress +
                SandRunnersOccupationRules.CaptureDelta(dt, 1, garrison,
                    occupation.settlement.defenseLevel, balanceProfile.mandarinkaOccupationCaptureSeconds));
            if (occupation.captureProgress >= 1f)
            {
                BeginMandarinkaOccupation(occupation);
                asset.hasObjective = false;
                ChooseMandarinkaTerritoryPlan();
            }
            return true;
        }

        return false;
    }

    private MandarinkaResourceHolding FindMandarinkaResourceHoldingNear(Vector3 position, float radius)
    {
        MandarinkaResourceHolding best = null;
        float bestDistance = radius;
        foreach (KeyValuePair<ResourceNode, MandarinkaResourceHolding> pair in mandarinkaResourceHoldings)
        {
            MandarinkaResourceHolding holding = pair.Value;
            if (holding == null || holding.node == null || holding.node.transform == null)
                continue;
            float distance = FlatDistance(position, holding.node.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = holding;
            }
        }
        return best;
    }

    private MandarinkaOccupation FindMandarinkaOccupationNear(Vector3 position, float radius)
    {
        MandarinkaOccupation best = null;
        float bestDistance = radius;
        foreach (KeyValuePair<SettlementDevelopmentState, MandarinkaOccupation> pair in mandarinkaOccupations)
        {
            MandarinkaOccupation occupation = pair.Value;
            if (occupation == null || occupation.settlement == null || occupation.settlement.settlement == null ||
                occupation.settlement.settlement.root == null)
                continue;
            float distance = FlatDistance(position, occupation.settlement.settlement.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = occupation;
            }
        }
        return best;
    }

    private int CountMandarinkaGarrison(Vector3 position, float radius)
    {
        int count = 0;
        for (int i = 0; i < mandarinkaAssets.Count; i++)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset == null || asset.transform == null || asset.enemy == null || asset.enemy.health <= 0f ||
                asset.role == MandarinkaRole.Fortress || asset.role == MandarinkaRole.FieldTurret)
                continue;
            if (FlatDistance(asset.transform.position, position) <= radius)
                count++;
        }
        return count;
    }

    private void SecureMandarinkaResourceNode(MandarinkaResourceHolding holding)
    {
        if (holding == null || holding.node == null || holding.node.transform == null || holding.held)
            return;

        holding.held = true;
        holding.captureProgress = 1f;
        holding.node.controlled = false;
        holding.node.capture = 0f;
        Transform root = new GameObject("Mandarinka_Resource_Outpost_" + holding.node.kind).transform;
        root.SetParent(mandarinkaTerritoryRoot, false);
        root.position = holding.node.transform.position;
        root.position = new Vector3(root.position.x, GetPlayableGroundHeight(root.position), root.position.z);
        holding.outpostRoot = root;

        CreateCylinder(root, "Outpost_Foundation", new Vector3(0f, 0.3f, 0f), Quaternion.identity,
            new Vector3(5.5f, 0.3f, 5.5f), mandarinkaDarkMaterial, true);
        CreateBox(root, "Outpost_Command_Block", new Vector3(0f, 2.2f, 0f), Quaternion.identity,
            new Vector3(4.2f, 3.8f, 4.2f), mandarinkaRedMaterial, true);
        CreateCylinder(root, "Outpost_Extractor", new Vector3(0f, 5.1f, 0f), Quaternion.identity,
            new Vector3(1.4f, 2.2f, 1.4f),
            holding.node.kind == ResourceKind.Gold ? mandarinkaGoldMaterial :
            holding.node.kind == ResourceKind.Wind ? mandarinkaJadeMaterial : mandarinkaDarkMaterial);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 1.4f, 6.2f);
            CreateBox(root, "Outpost_Wall_" + i, p, Quaternion.Euler(0f, angle, 0f),
                new Vector3(7f, 2.2f, 0.55f), mandarinkaRedMaterial, true);
        }
        CreateMandarinkaInfluenceRing(root, balanceProfile.mandarinkaOutpostInfluenceRadius);
        CreateMandarinkaTerritoryLabel(root, "IMPERIAL " + holding.node.kind.ToString().ToUpperInvariant() + " OUTPOST");
        holding.commandEnemy = RegisterMandarinkaEnemy(root, 480f);
        InitializeMandarinkaOutpostInfrastructure(holding);
        if (holding.node.markerRenderer != null)
            holding.node.markerRenderer.sharedMaterial = mandarinkaRedMaterial;

        PushLivingWorldEvent(LivingWorldEventType.Raid,
            "Mandarinka seized " + holding.node.label + " and raised an extraction outpost.", 12f, true);
        ShowBanner("RESOURCE LOST // " + holding.node.label.ToUpperInvariant(), 3f);
        lastEvent = "Mandarinka now draws real supply from " + holding.node.label + ". Destroy the red outpost to reclaim it.";
    }

    private void UpdateMandarinkaResourceHoldings(float dt)
    {
        foreach (KeyValuePair<ResourceNode, MandarinkaResourceHolding> pair in mandarinkaResourceHoldings)
        {
            MandarinkaResourceHolding holding = pair.Value;
            if (holding == null || !holding.held || holding.node == null)
                continue;

            if (holding.commandEnemy == null || holding.commandEnemy.health <= 0f || holding.outpostRoot == null)
            {
                ReleaseMandarinkaResourceNode(holding);
                continue;
            }

            holding.node.controlled = false;
            holding.node.capture = 0f;
            // No direct stock ticks: a destroyed route must stop delivery.
            UpdateMandarinkaOutpostInfrastructure(holding, dt);
        }
    }

    private void ReleaseMandarinkaResourceNode(MandarinkaResourceHolding holding)
    {
        if (holding == null || !holding.held)
            return;
        holding.held = false;
        holding.captureProgress = 0f;
        if (holding.node != null)
        {
            holding.node.capture = 0f;
            holding.node.controlled = false;
            if (holding.node.markerRenderer != null)
                holding.node.markerRenderer.sharedMaterial = contestedMaterial;
        }
        if (holding.outpostRoot != null)
            Destroy(holding.outpostRoot.gameObject);
        ReleaseMandarinkaOutpostInfrastructure(holding);
        holding.outpostRoot = null;
        holding.commandEnemy = null;
        ShowBanner("IMPERIAL OUTPOST DESTROYED // RESOURCE CONTESTED", 2.8f);
        lastEvent = "Mandarinka lost an extraction outpost. The resource can be captured again.";
        ChooseMandarinkaTerritoryPlan();
    }

    private void BeginMandarinkaOccupation(MandarinkaOccupation occupation)
    {
        if (occupation == null || occupation.settlement == null || occupation.settlement.settlement == null ||
            occupation.stage >= MandarinkaOccupationStage.Occupied)
            return;

        SettlementDevelopmentState state = occupation.settlement;
        occupation.stage = MandarinkaOccupationStage.Occupied;
        occupation.captureProgress = 1f;
        occupation.occupationAge = 0f;
        occupation.population = Mathf.Clamp(occupation.population, 35f, 100f);
        state.underRaid = true;
        state.stage = SettlementDiplomacyStage.Neutral;
        state.contractStock = 0;
        if (state.network != null)
            state.network.online = false;
        if (unifiedDiplomacyOpen && activeDiplomacyState == state)
            CloseUnifiedDiplomacy(false);
        CancelSettlementCaravans(state.settlement);

        for (int i = 0; i < state.settlement.guards.Count; i++)
            if (state.settlement.guards[i] != null)
                state.settlement.guards[i].gameObject.SetActive(false);

        BuildMandarinkaOccupationFortress(occupation);
        InitializeMandarinkaOccupationInfrastructure(occupation);
        PushLivingWorldEvent(LivingWorldEventType.Raid,
            state.settlement.displayName + " has been occupied by Mandarinka.", 15f, true);
        ShowBanner("SETTLEMENT OCCUPIED // DESTROY THE COMMAND NODE", 4f);
        SetMandarinkaRadio("MANDARINKA: The gates are sealed. Their streets will feed my palace.");
        lastEvent = state.settlement.displayName + " market and contracts are offline. Population is now at risk.";
    }

    private void BuildMandarinkaOccupationFortress(MandarinkaOccupation occupation)
    {
        SettlementDevelopmentState state = occupation.settlement;
        Transform root = new GameObject("Mandarinka_Occupation_Command_" + state.settlement.displayName.Replace(' ', '_')).transform;
        root.SetParent(state.settlement.root, false);
        occupation.occupationRoot = root;

        CreateCylinder(root, "Occupation_Parade_Ground", new Vector3(0f, 0.22f, 0f), Quaternion.identity,
            new Vector3(12f, 0.18f, 12f), mandarinkaDarkMaterial, true);
        CreateBox(root, "Occupation_Command_Citadel", new Vector3(0f, 4.2f, 0f), Quaternion.identity,
            new Vector3(7.5f, 7.8f, 7.5f), mandarinkaRedMaterial, true);
        CreateBox(root, "Occupation_Gold_Command_Roof", new Vector3(0f, 8.35f, 0f), Quaternion.Euler(0f, 45f, 0f),
            new Vector3(8.8f, 0.5f, 8.8f), mandarinkaGoldMaterial, true);
        CreateCylinder(root, "Occupation_Jammer", new Vector3(0f, 12.1f, 0f), Quaternion.identity,
            new Vector3(1.1f, 3.5f, 1.1f), mandarinkaJadeMaterial);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 1.7f, 17f);
            CreateBox(root, "Occupation_Perimeter_" + i, p, Quaternion.Euler(0f, angle, 0f),
                new Vector3(9f, 3f, 0.65f), mandarinkaDarkMaterial, true);
            if (i % 3 == 0)
            {
                CreateBox(root, "Occupation_Watchtower_" + i, p + Vector3.up * 4.2f, Quaternion.identity,
                    new Vector3(2.8f, 7f, 2.8f), mandarinkaRedMaterial, true);
            }
        }
        CreateMandarinkaInfluenceRing(root, balanceProfile.mandarinkaOccupationInfluenceRadius);
        CreateMandarinkaTerritoryLabel(root, "MANDARINKA OCCUPATION // COMMAND NODE");
        occupation.commandEnemy = RegisterMandarinkaEnemy(root, 760f + state.defenseLevel * 18f);
    }

    private void UpdateMandarinkaOccupations(float dt)
    {
        foreach (KeyValuePair<SettlementDevelopmentState, MandarinkaOccupation> pair in mandarinkaOccupations)
        {
            MandarinkaOccupation occupation = pair.Value;
            if (occupation == null || occupation.settlement == null || occupation.settlement.settlement == null)
                continue;

            if (occupation.stage == MandarinkaOccupationStage.Encircled)
            {
                occupation.settlement.underRaid = true;
                if (Time.time - occupation.lastPressureTime > 8f)
                {
                    occupation.captureProgress = Mathf.Max(0f, occupation.captureProgress - dt * 0.025f);
                    if (occupation.captureProgress <= 0f)
                    {
                        occupation.stage = MandarinkaOccupationStage.Free;
                        occupation.settlement.underRaid = false;
                    }
                }
                continue;
            }

            if (occupation.stage < MandarinkaOccupationStage.Occupied ||
                occupation.stage == MandarinkaOccupationStage.Ruined)
                continue;

            if (occupation.commandEnemy == null || occupation.commandEnemy.health <= 0f ||
                occupation.occupationRoot == null)
            {
                LiberateMandarinkaOccupation(occupation);
                continue;
            }

            occupation.settlement.underRaid = true;
            occupation.occupationAge += dt;
            occupation.population = Mathf.Max(0f, occupation.population -
                SandRunnersOccupationRules.PopulationDrainPerSecond((int)occupation.stage) * dt);
            ExtractOccupationResources(occupation, dt);
            UpdateOccupationFortressWeapons(occupation, dt);
            UpdateMandarinkaOccupationInfrastructure(occupation, dt);

            if (occupation.stage == MandarinkaOccupationStage.Occupied &&
                occupation.occupationAge >= balanceProfile.mandarinkaOccupationFortifySeconds)
            {
                occupation.stage = MandarinkaOccupationStage.Fortified;
                BuildMandarinkaOccupationStage(occupation, true);
                ShowBanner(occupation.settlement.settlement.displayName + " // OCCUPATION FORTIFIED", 3f);
            }
            if (occupation.stage == MandarinkaOccupationStage.Fortified &&
                occupation.occupationAge >= balanceProfile.mandarinkaOccupationExhaustSeconds)
            {
                occupation.stage = MandarinkaOccupationStage.Exhausted;
                BuildMandarinkaOccupationStage(occupation, false);
                ShowBanner(occupation.settlement.settlement.displayName + " // POPULATION COLLAPSING", 3.4f);
            }
            if (occupation.population <= 0f)
                RuinMandarinkaOccupation(occupation);
        }
    }

    private void BuildMandarinkaOccupationStage(MandarinkaOccupation occupation, bool fortified)
    {
        if (occupation.occupationRoot == null)
            return;
        string prefix = fortified ? "Fortified" : "Exhausted";
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCylinder(occupation.occupationRoot, prefix + "_Artillery_" + side,
                new Vector3(side * 9f, 5.2f, -8f), Quaternion.Euler(78f, 0f, 0f),
                new Vector3(0.7f, 4.2f, 0.7f), mandarinkaDarkMaterial);
            CreateBox(occupation.occupationRoot, prefix + "_Barracks_" + side,
                new Vector3(side * 10f, 2.1f, 7f), Quaternion.identity,
                new Vector3(6f, 3.8f, 7f), fortified ? mandarinkaRedMaterial : mandarinkaDarkMaterial, true);
        }
        if (occupation.commandEnemy != null && fortified)
            occupation.commandEnemy.health += 280f;
        RegisterMandarinkaGrowthByPrefix(occupation.occupationRoot, prefix);
        ExpandMandarinkaOccupationGarrison(occupation, fortified ? 2 : 1);
    }

    private void ExtractOccupationResources(MandarinkaOccupation occupation, float dt)
    {
        float rate = occupation.stage >= MandarinkaOccupationStage.Exhausted ? 0.75f :
            occupation.stage >= MandarinkaOccupationStage.Fortified ? 0.52f : 0.34f;
        NeutralFactionKind kind = occupation.settlement.settlement.kind;
        if (kind == NeutralFactionKind.Grounders)
            mandarinkaSandStock += rate * dt;
        else if (kind == NeutralFactionKind.BlackElementals)
            mandarinkaWindStock += rate * 0.72f * dt;
        else
            mandarinkaGoldStock += rate * dt;
        mandarinkaSupply = Mathf.Min(720f, mandarinkaSupply + rate * 0.8f * dt);
    }

    private void UpdateOccupationFortressWeapons(MandarinkaOccupation occupation, float dt)
    {
        occupation.fireTimer -= dt;
        if (occupation.fireTimer > 0f || occupation.occupationRoot == null)
            return;

        float range = occupation.stage >= MandarinkaOccupationStage.Fortified ? 125f : 90f;
        RunnerUnit target = FindNearestRunner(occupation.occupationRoot.position, range);
        if (target == null || target.transform == null)
            return;

        float damage = occupation.stage >= MandarinkaOccupationStage.Exhausted ? 25f :
            occupation.stage >= MandarinkaOccupationStage.Fortified ? 20f : 13f;
        occupation.fireTimer = occupation.stage >= MandarinkaOccupationStage.Fortified ? 2.1f : 2.8f;
        target.health -= damage;
        CreateBeam(occupation.occupationRoot.position + Vector3.up * 10f,
            target.transform.position + Vector3.up, new Color(1f, 0.12f, 0.035f, 1f), 0.06f, 0.16f);
    }

    private void RuinMandarinkaOccupation(MandarinkaOccupation occupation)
    {
        if (occupation.stage == MandarinkaOccupationStage.Ruined)
            return;

        occupation.stage = MandarinkaOccupationStage.Ruined;
        occupation.population = 0f;
        occupation.settlement.settlement.health = 0f;
        occupation.settlement.underRaid = false;
        for (int i = 0; i < occupation.settlement.settlement.guards.Count; i++)
            if (occupation.settlement.settlement.guards[i] != null)
                Destroy(occupation.settlement.settlement.guards[i].gameObject);
        if (occupation.settlement.urbanRoot != null)
            occupation.settlement.urbanRoot.localScale = new Vector3(1f, 0.42f, 1f);

        for (int i = 0; i < mandarinkaAssets.Count; i++)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset != null && asset.transform != null && asset.role != MandarinkaRole.FieldTurret &&
                FlatDistance(asset.transform.position, occupation.settlement.settlement.root.position) <= 120f)
                asset.hasObjective = false;
        }

        PushLivingWorldEvent(LivingWorldEventType.Raid,
            occupation.settlement.settlement.displayName + " was exhausted and abandoned by the occupation army.", 18f, true);
        ShowBanner("SETTLEMENT LOST // OCCUPATION ARMY MOVING ON", 4f);
        SetMandarinkaRadio("SEBEK: The streets are empty. Her army is already looking for another banner.");
        lastEvent = occupation.settlement.settlement.displayName + " is ruined. Its market and routes are permanently offline.";
        ChooseMandarinkaTerritoryPlan();
    }

    private void LiberateMandarinkaOccupation(MandarinkaOccupation occupation)
    {
        if (occupation == null || occupation.stage < MandarinkaOccupationStage.Occupied ||
            occupation.stage == MandarinkaOccupationStage.Ruined)
            return;

        SettlementDevelopmentState state = occupation.settlement;
        if (occupation.occupationRoot != null)
            Destroy(occupation.occupationRoot.gameObject);
        ReleaseMandarinkaOccupationInfrastructure(occupation);
        occupation.occupationRoot = null;
        occupation.commandEnemy = null;
        occupation.stage = MandarinkaOccupationStage.Free;
        occupation.captureProgress = 0f;
        occupation.occupationAge = 0f;
        state.underRaid = false;
        state.settlement.health = Mathf.Max(state.settlement.health,
            state.settlement.maxHealth * SandRunnersMandarinkaStrategicRules.LiberationRecovery(occupation.population));
        state.tradeTrust = Mathf.Min(100f, state.tradeTrust + 12f);
        state.playerInfluence = Mathf.Min(100f, state.playerInfluence + 16f);
        state.contractRestockTimer = Mathf.Min(state.contractRestockTimer, 90f);
        for (int i = 0; i < state.settlement.guards.Count; i++)
            if (state.settlement.guards[i] != null)
                state.settlement.guards[i].gameObject.SetActive(true);
        if (state.network != null)
            state.network.online = true;
        UpdateSettlementDiplomacy(state);

        PushLivingWorldEvent(LivingWorldEventType.Raid,
            state.settlement.displayName + " was liberated with " + Mathf.RoundToInt(occupation.population) + "% population surviving.", 14f, false);
        ShowBanner("SETTLEMENT LIBERATED // POPULATION " + Mathf.RoundToInt(occupation.population) + "%", 4f);
        lastEvent = "Occupation command destroyed. " + state.settlement.displayName + " market is recovering.";
        ChooseMandarinkaTerritoryPlan();
    }

    private void CancelSettlementCaravans(NeutralSettlement settlement)
    {
        for (int i = tradeCaravans.Count - 1; i >= 0; i--)
        {
            TradeCaravanState caravan = tradeCaravans[i];
            if (caravan == null || caravan.origin == settlement || caravan.destination == settlement)
            {
                if (caravan != null && caravan.root != null)
                    Destroy(caravan.root.gameObject);
                tradeCaravans.RemoveAt(i);
            }
        }
    }

    private bool IsSettlementUnavailableToPlayer(SettlementDevelopmentState state)
    {
        if (state == null)
            return true;
        MandarinkaOccupation occupation;
        return mandarinkaOccupations.TryGetValue(state, out occupation) &&
               occupation.stage >= MandarinkaOccupationStage.Encircled;
    }

    private bool IsSettlementUnderMandarinkaOccupation(SettlementDevelopmentState state)
    {
        MandarinkaOccupation occupation;
        return state != null && mandarinkaOccupations.TryGetValue(state, out occupation) &&
               occupation.stage >= MandarinkaOccupationStage.Encircled &&
               occupation.stage < MandarinkaOccupationStage.Ruined;
    }

    private bool TryGetMandarinkaOccupationStatus(SettlementDevelopmentState state, out string status)
    {
        status = null;
        MandarinkaOccupation occupation;
        if (state == null || !mandarinkaOccupations.TryGetValue(state, out occupation) ||
            occupation.stage == MandarinkaOccupationStage.Free)
            return false;

        if (occupation.stage == MandarinkaOccupationStage.Encircled)
            status = "ENCIRCLED " + Mathf.RoundToInt(occupation.captureProgress * 100f) + "%";
        else if (occupation.stage == MandarinkaOccupationStage.Ruined)
            status = "RUINED // POPULATION 0";
        else
            status = occupation.stage.ToString().ToUpperInvariant() + " // POP " +
                     Mathf.RoundToInt(occupation.population) + "%";
        return true;
    }

    private int CountMandarinkaHeldResourceNodes()
    {
        int count = 0;
        foreach (KeyValuePair<ResourceNode, MandarinkaResourceHolding> pair in mandarinkaResourceHoldings)
            if (pair.Value != null && pair.Value.held)
                count++;
        return count;
    }

    private int CountMandarinkaOccupiedSettlements()
    {
        int count = 0;
        foreach (KeyValuePair<SettlementDevelopmentState, MandarinkaOccupation> pair in mandarinkaOccupations)
            if (pair.Value != null && pair.Value.stage >= MandarinkaOccupationStage.Occupied &&
                pair.Value.stage < MandarinkaOccupationStage.Ruined)
                count++;
        return count;
    }

    private float GetMandarinkaTerritorySupplyRate()
    {
        // Supply arrives solely on convoy completion.
        return 0f;
    }

    private float GetMandarinkaTerritoryProductionBonus()
    {
        return (mandarinkaCastleGoldKit ? 0.12f : 0f) +
               (mandarinkaCastleWindKit ? 0.18f : 0f) +
               CountMandarinkaOccupiedSettlements() * 0.025f;
    }

    private float GetMandarinkaTerritorySpeedBonus()
    {
        return mandarinkaCastleWindKit ? 0.62f : 0f;
    }

    private float GetMandarinkaTerritoryArmorMultiplier()
    {
        return mandarinkaCastleSandKit ? 0.82f : 1f;
    }

    private int GetMandarinkaTerritoryAirLimitBonus()
    {
        return mandarinkaCastleWindKit ? 1 : 0;
    }

    private void TryDevelopMandarinkaFortressFromTerritory()
    {
        if (mandarinkaFortressVisual == null || mandarinkaDefeated)
            return;

        if (SandRunnersOccupationRules.ResourceKitReady(mandarinkaSandStock,
            balanceProfile.mandarinkaSandCastleThreshold, mandarinkaCastleSandKit))
        {
            mandarinkaSandStock -= balanceProfile.mandarinkaSandCastleThreshold;
            mandarinkaCastleSandKit = true;
            BuildMandarinkaSandCastleKit();
        }
        if (SandRunnersOccupationRules.ResourceKitReady(mandarinkaGoldStock,
            balanceProfile.mandarinkaGoldCastleThreshold, mandarinkaCastleGoldKit))
        {
            mandarinkaGoldStock -= balanceProfile.mandarinkaGoldCastleThreshold;
            mandarinkaCastleGoldKit = true;
            BuildMandarinkaGoldCastleKit();
        }
        if (SandRunnersOccupationRules.ResourceKitReady(mandarinkaWindStock,
            balanceProfile.mandarinkaWindCastleThreshold, mandarinkaCastleWindKit))
        {
            mandarinkaWindStock -= balanceProfile.mandarinkaWindCastleThreshold;
            mandarinkaCastleWindKit = true;
            BuildMandarinkaWindCastleKit();
        }
    }

    private void BuildMandarinkaSandCastleKit()
    {
        Transform root = new GameObject("Mandarinka_Castle_Kit_Sand").transform;
        root.SetParent(mandarinkaFortressVisual, false);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox(root, "Sand_Armor_Skirt_" + side, new Vector3(side * 10.2f, 2.4f, 0f),
                Quaternion.Euler(0f, 0f, side * 7f), new Vector3(2.4f, 3.8f, 15.5f), mandarinkaDarkMaterial, true);
            CreateCylinder(root, "Sand_Mortar_" + side, new Vector3(side * 6.8f, 10.2f, 1f),
                Quaternion.Euler(72f, 0f, 0f), new Vector3(0.8f, 3.8f, 0.8f), mandarinkaGoldMaterial);
        }
        RegisterMandarinkaCastleStructure(root);
        ShowBanner("MANDARINKA CASTLE EVOLUTION // DUNE SIEGE COURT", 4f);
        SetMandarinkaRadio("MANDARINKA: Sand pays for armor. Let her mirrors strike something worthy.");
    }

    private void BuildMandarinkaGoldCastleKit()
    {
        Transform root = new GameObject("Mandarinka_Castle_Kit_Gold").transform;
        root.SetParent(mandarinkaFortressVisual, false);
        CreateBox(root, "Imperial_Mint", new Vector3(0f, 10f, 3.8f), Quaternion.identity,
            new Vector3(8.5f, 5.8f, 6.5f), mandarinkaGoldMaterial, true);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 p = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 8.5f, 10f);
            CreateCylinder(root, "Gold_Shield_Pylon_" + i, p, Quaternion.identity,
                new Vector3(0.75f, 4.4f, 0.75f), mandarinkaJadeMaterial);
        }
        mandarinkaFortressShield = Mathf.Min(MandarinkaFortressMaxShield, mandarinkaFortressShield + 1200f);
        RegisterMandarinkaCastleStructure(root);
        ShowBanner("MANDARINKA CASTLE EVOLUTION // IMPERIAL MINT COURT", 4f);
        SetMandarinkaRadio("MANDARINKA: Their tribute is already becoming soldiers.");
    }

    private void BuildMandarinkaWindCastleKit()
    {
        Transform root = new GameObject("Mandarinka_Castle_Kit_Wind").transform;
        root.SetParent(mandarinkaFortressVisual, false);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox(root, "Storm_Sail_Mast_" + side, new Vector3(side * 6f, 15f, 3f),
                Quaternion.Euler(0f, 0f, side * 8f), new Vector3(0.55f, 13f, 0.55f), mandarinkaGoldMaterial);
            CreateBox(root, "Storm_Sail_" + side, new Vector3(side * 9f, 16f, 3f),
                Quaternion.Euler(0f, side * 8f, side * -6f), new Vector3(6.8f, 9.5f, 0.22f), mandarinkaRedMaterial, true);
            CreateCylinder(root, "Wind_Turbine_" + side, new Vector3(side * 8.5f, 4.4f, 7f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(2.2f, 0.5f, 2.2f), mandarinkaJadeMaterial);
        }
        RegisterMandarinkaCastleStructure(root);
        ShowBanner("MANDARINKA CASTLE EVOLUTION // STORM-SAIL PALACE", 4f);
        SetMandarinkaRadio("SEBEK: She put sails on a castle. Unfortunately, they appear to work.");
    }

    private float GetPlayerInfluenceAt(Vector3 position)
    {
        float influence = battlePyramid != null
            ? SandRunnersOccupationRules.InfluenceAt(FlatDistance(position, battlePyramid.position), 190f, 120f)
            : 0f;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node != null && node.controlled && node.transform != null)
                influence += SandRunnersOccupationRules.InfluenceAt(
                    FlatDistance(position, node.transform.position), balanceProfile.mandarinkaOutpostInfluenceRadius, 62f);
        }
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state != null && state.settlement != null && state.settlement.root != null &&
                state.stage >= SettlementDiplomacyStage.ProtectedSettlement && !IsSettlementUnavailableToPlayer(state))
                influence += SandRunnersOccupationRules.InfluenceAt(
                    FlatDistance(position, state.settlement.root.position), balanceProfile.mandarinkaOccupationInfluenceRadius, 74f);
        }
        return influence;
    }

    private float GetMandarinkaInfluenceAt(Vector3 position)
    {
        float influence = mandarinkaFortressRoot != null
            ? SandRunnersOccupationRules.InfluenceAt(FlatDistance(position, mandarinkaFortressRoot.position),
                balanceProfile.mandarinkaFortressInfluenceRadius, 130f)
            : 0f;
        foreach (KeyValuePair<ResourceNode, MandarinkaResourceHolding> pair in mandarinkaResourceHoldings)
            if (pair.Value != null && pair.Value.held && pair.Value.node != null && pair.Value.node.transform != null)
                influence += SandRunnersOccupationRules.InfluenceAt(
                    FlatDistance(position, pair.Value.node.transform.position),
                    balanceProfile.mandarinkaOutpostInfluenceRadius, 68f);
        foreach (KeyValuePair<SettlementDevelopmentState, MandarinkaOccupation> pair in mandarinkaOccupations)
            if (pair.Value != null && pair.Value.stage >= MandarinkaOccupationStage.Occupied &&
                pair.Value.settlement != null && pair.Value.settlement.settlement != null &&
                pair.Value.settlement.settlement.root != null)
                influence += SandRunnersOccupationRules.InfluenceAt(
                    FlatDistance(position, pair.Value.settlement.settlement.root.position),
                    balanceProfile.mandarinkaOccupationInfluenceRadius, 86f);
        return influence;
    }

    private string GetMandarinkaTerritoryHudLine()
    {
        int held = CountMandarinkaHeldResourceNodes();
        int occupied = CountMandarinkaOccupiedSettlements();
        return "Territory: nodes " + held + " / occupations " + occupied +
               " / convoys " + mandarinkaLogisticsConvoys.Count +
               " | S " + Mathf.FloorToInt(mandarinkaSandStock) +
               " G " + Mathf.FloorToInt(mandarinkaGoldStock) +
               " W " + Mathf.FloorToInt(mandarinkaWindStock) +
               "\nKits: " + GetMandarinkaDoctrineHud() + " | Objective: " + mandarinkaTerritoryObjectiveLabel;
    }

    private string GetMandarinkaFrontSummary()
    {
        if (battlePyramid == null || mandarinkaFortressRoot == null)
            return "front unknown";
        Vector3 midpoint = Vector3.Lerp(battlePyramid.position, mandarinkaFortressRoot.position, 0.5f);
        float player = GetPlayerInfluenceAt(midpoint);
        float enemy = GetMandarinkaInfluenceAt(midpoint);
        if (Mathf.Abs(player - enemy) < 12f)
            return "contested front";
        return player > enemy ? "golden influence" : "imperial influence";
    }

    private void CreateMandarinkaInfluenceRing(Transform parent, float radius)
    {
        LineRenderer line = new GameObject("Mandarinka_Influence_Ring").AddComponent<LineRenderer>();
        line.transform.SetParent(parent, false);
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.startWidth = 0.22f;
        line.endWidth = 0.22f;
        line.numCapVertices = 2;
        line.sharedMaterial = mandarinkaRedMaterial;
        line.startColor = new Color(1f, 0.08f, 0.035f, 0.38f);
        line.endColor = new Color(1f, 0.45f, 0.08f, 0.16f);
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48f;
            line.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, 0.28f, Mathf.Cos(angle) * radius));
        }
    }

    private void CreateMandarinkaTerritoryLabel(Transform parent, string text)
    {
        TextMesh label = new GameObject("Mandarinka_Territory_Label").AddComponent<TextMesh>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, 9.5f, -4f);
        label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.42f;
        label.fontSize = 48;
        label.color = new Color(1f, 0.28f, 0.08f, 1f);
        label.text = text;
    }
}
