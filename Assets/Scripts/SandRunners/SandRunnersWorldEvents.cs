using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private enum SettlementDiplomacyStage
    {
        Neutral,
        TradingPartner,
        ProtectedSettlement,
        AlliedSettlement
    }

    private enum LivingWorldEventType
    {
        Caravan,
        Trade,
        SettlementDevelopment,
        EnergyNetwork,
        Raid
    }

    private sealed class SettlementDevelopmentState
    {
        public NeutralSettlement settlement;
        public SettlementDiplomacyStage stage;
        public int playerDeals;
        public int developmentPoints;
        public int builtStructures;
        public int deployedUnits;
        public float tradeTrust;
        public float playerInfluence;
        public float crimeScore;
        public float defenseLevel;
        public float previousHealth;
        public bool underRaid;
        public bool defendedFromRaid;
        public TextMesh statusLabel;
        public EnergyNetworkState network;
        public int contractStock = 1;
        public float contractRestockTimer;
        public int cityTier;
        public float autonomousGrowthTimer = 150f;
        public Transform urbanRoot;
        public string urbanSpecialization = "DESERT CITY";
        public bool horusPower;
    }

    private sealed class TradeCaravanState
    {
        public Transform root;
        public NeutralFactionKind faction;
        public NeutralSettlement origin;
        public NeutralSettlement destination;
        public LineRenderer routeLine;
        public float health = 180f;
        public float speed = 10.5f;
        public float cargo = 1f;
        public int eventId;
        public bool raided;
    }

    private sealed class EnergyNetworkState
    {
        public ResourceNode source;
        public SettlementDevelopmentState destination;
        public Transform root;
        public readonly List<Transform> relays = new List<Transform>();
        public readonly List<float> relayHealth = new List<float>();
        public LineRenderer beamLine;
        public bool online = true;
        public float pulse;
    }

    private sealed class LivingWorldEvent
    {
        public int id;
        public LivingWorldEventType type;
        public string label;
        public float life;
        public bool crisis;
    }

    private readonly Dictionary<NeutralSettlement, SettlementDevelopmentState> settlementDevelopment = new Dictionary<NeutralSettlement, SettlementDevelopmentState>();
    private readonly List<TradeCaravanState> tradeCaravans = new List<TradeCaravanState>();
    private readonly List<EnergyNetworkState> energyNetworks = new List<EnergyNetworkState>();
    private readonly List<LivingWorldEvent> livingWorldEvents = new List<LivingWorldEvent>();
    private Transform livingWorldRoot;
    private bool livingWorldInitialized;
    private float tradeCaravanSpawnTimer = 8f;
    private float settlementStatusTimer;
    private int nextLivingWorldEventId = 1;
    private int nextTradeCaravanId = 1;

    private void InitializeWorldEvents()
    {
        if (livingWorldInitialized)
            return;

        livingWorldInitialized = true;
        GameObject oldRoot = GameObject.Find("SandRunners_Living_World_Runtime");
        if (oldRoot != null)
            Destroy(oldRoot);
        livingWorldRoot = new GameObject("SandRunners_Living_World_Runtime").transform;
        SynchronizeSettlementDevelopment();
    }

    private void UpdateWorldEvents(float dt)
    {
        if (!livingWorldInitialized)
            InitializeWorldEvents();
        if (livingWorldRoot == null)
            return;

        SynchronizeSettlementDevelopment();
        UpdateUrbanEvolution(dt);
        if (!unifiedDiplomacyEnabled && WasKeyPressedThisFrame(Key.T))
            TryTradeWithNearestSettlement();
        if (commandCursorMode && WasKeyPressedThisFrame(Key.Y))
            TryBuildOrRepairEnergyNetwork();
        if (commandCursorMode && WasKeyPressedThisFrame(Key.K))
            TryDeclareHostilityAgainstNearestSettlement();
        if (commandCursorMode && WasKeyPressedThisFrame(Key.L))
            TryLootNashornWreck();

        tradeCaravanSpawnTimer -= dt;
        if (tradeCaravanSpawnTimer <= 0f && tradeCaravans.Count < 3)
        {
            tradeCaravanSpawnTimer = balanceProfile.caravanSpawnSeconds;
            SpawnTradeCaravan();
        }

        UpdateTradeCaravans(dt);
        UpdateEnergyNetworks(dt);
        UpdateHostileSettlementDefense(dt);
        UpdateNeutralRetaliation(dt);
        UpdateMandarinkaCaravanRaids(dt);
        UpdateSettlementRaidWitnesses();
        UpdateLivingWorldEvents(dt);

        settlementStatusTimer -= dt;
        if (settlementStatusTimer <= 0f)
        {
            settlementStatusTimer = 0.5f;
            RefreshSettlementStatusLabels();
        }
    }

    private void SynchronizeSettlementDevelopment()
    {
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement == null || settlement.root == null || settlementDevelopment.ContainsKey(settlement))
                continue;

            SettlementDevelopmentState state = new SettlementDevelopmentState();
            state.settlement = settlement;
            state.stage = SettlementDiplomacyStage.Neutral;
            state.previousHealth = settlement.health;
            state.statusLabel = CreateSettlementStatusLabel(settlement);
            settlementDevelopment.Add(settlement, state);
        }
    }

    private TextMesh CreateSettlementStatusLabel(NeutralSettlement settlement)
    {
        TextMesh label = new GameObject("Living_World_Status").AddComponent<TextMesh>();
        label.transform.SetParent(settlement.root, false);
        label.transform.localPosition = new Vector3(0f, settlement.kind == NeutralFactionKind.Grounders ? 4.2f : 6.7f, -3.5f);
        label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        label.characterSize = 0.34f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = settlement.kind == NeutralFactionKind.BlackElementals
            ? new Color(0.64f, 0.78f, 1f, 1f)
            : new Color(0.35f, 0.88f, 1f, 1f);
        return label;
    }

    private void RefreshSettlementStatusLabels()
    {
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.statusLabel == null)
                continue;
            string occupation;
            if (TryGetMandarinkaOccupationStatus(state, out occupation))
            {
                state.statusLabel.color = new Color(1f, 0.22f, 0.06f, 1f);
                state.statusLabel.text = occupation + " // MARKET OFFLINE";
                continue;
            }
            state.statusLabel.color = state.settlement.kind == NeutralFactionKind.BlackElementals
                ? new Color(0.64f, 0.78f, 1f, 1f)
                : new Color(0.35f, 0.88f, 1f, 1f);
            string power = state.horusPower ? "HORUS GRID" :
                state.network != null ? (state.network.online ? "POWER ONLINE" : "POWER OFFLINE") : "NO GRID";
            string city = state.cityTier > 0 ? state.urbanSpecialization + " T" + state.cityTier : "OUTPOST";
            state.statusLabel.text = state.stage + " // " + city + " // DEV " + state.developmentPoints +
                " // DEF " + Mathf.RoundToInt(state.defenseLevel) + " // " + power;
        }
    }

    private SettlementDevelopmentState FindNearestSettlementDevelopment(Vector3 origin, float maxDistance)
    {
        SettlementDevelopmentState best = null;
        float bestDistance = maxDistance;
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || state.settlement.root == null ||
                state.settlement.health <= 0f || IsSettlementUnavailableToPlayer(state))
                continue;
            float distance = FlatDistance(origin, state.settlement.root.position);
            if (distance < bestDistance)
            {
                best = state;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void TryTradeWithNearestSettlement()
    {
        SettlementDevelopmentState state = FindNearestSettlementDevelopment(battlePyramid.position, 220f);
        if (state == null)
        {
            lastEvent = "No neutral market in trade range. Move the pyramid within 220 metres of a settlement.";
            ShowBanner("NO MARKET IN RANGE", 2f);
            return;
        }
        if (!TrySpendResources(35f, 25f, 8f, "neutral trade contract"))
            return;

        CompleteSettlementTrade(state, true);
    }

    private void CompleteSettlementTrade(SettlementDevelopmentState state, bool playerDeal)
    {
        if (state == null || state.settlement == null || state.settlement.root == null)
            return;

        state.developmentPoints++;
        state.tradeTrust = Mathf.Min(100f, state.tradeTrust + (playerDeal ? 20f : 7f));
        if (playerDeal)
        {
            state.playerDeals++;
            state.playerInfluence = Mathf.Min(100f, state.playerInfluence + 16f);
        }
        PerformSettlementDevelopment(state);
        UpdateSettlementDiplomacy(state);
        PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, state.settlement.root.position, 0.68f);
        PushLivingWorldEvent(LivingWorldEventType.Trade, state.settlement.displayName + " completed a trade and expanded.", 8f, false);
        lastEvent = state.settlement.displayName + " trade complete. Development " + state.developmentPoints + ", relation " + state.stage + ".";
        ShowBanner("TRADE COMPLETE // SETTLEMENT EXPANDS", 2.4f);
    }

    private void UpdateSettlementDiplomacy(SettlementDevelopmentState state)
    {
        SettlementDiplomacyStage previous = state.stage;
        if (state.playerDeals >= 2)
            state.stage = SettlementDiplomacyStage.TradingPartner;
        if (state.playerDeals >= 4 || state.network != null)
            state.stage = SettlementDiplomacyStage.ProtectedSettlement;
        bool powerOnline = state.horusPower || (state.network != null && state.network.online);
        if (SandRunnersEvolutionRules.AllianceReady(
            state.playerDeals, state.tradeTrust, state.playerInfluence, powerOnline))
            state.stage = SettlementDiplomacyStage.AlliedSettlement;

        if (state.stage != previous)
        {
            PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, state.settlement.displayName + " is now " + state.stage + ".", 10f, false);
            ShowBanner(state.settlement.displayName + " // " + state.stage, 2.8f);
        }
    }

    private void PerformSettlementDevelopment(SettlementDevelopmentState state)
    {
        bool buildDefense = state.defenseLevel <= state.deployedUnits * 1.5f || state.builtStructures <= state.deployedUnits;
        if (buildDefense)
            BuildSettlementDefense(state);
        else
            DeploySettlementTechnicalUnit(state);
    }

    private void BuildSettlementDefense(SettlementDevelopmentState state)
    {
        int index = state.builtStructures++;
        float angle = (index * 113f + 28f) * Mathf.Deg2Rad;
        Vector3 local = new Vector3(Mathf.Cos(angle) * (10f + index * 1.1f), 0.25f, Mathf.Sin(angle) * (10f + index * 1.1f));
        Transform root = new GameObject("Settlement_Defense_" + index).transform;
        root.SetParent(state.settlement.root, false);
        root.localPosition = local;
        Material material = state.settlement.accentMaterial;
        CreateCylinder(root, "Defense_Foundation", Vector3.zero, Quaternion.identity, new Vector3(2.2f, 0.18f, 2.2f), material, true);
        CreateBox(root, "Defense_Tower", new Vector3(0f, 1.4f, 0f), Quaternion.identity, new Vector3(1.1f, 2.6f, 1.1f), material, true);
        CreateBox(root, "Defense_Weapon", new Vector3(0f, 2.7f, 0.8f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.28f, 0.28f, 1.4f), material);
        CreatePointLight(root, "Defense_Status", new Vector3(0f, 3.1f, 0f), new Color(0.2f, 0.7f, 1f, 1f), 0.55f, 12f);
        state.defenseLevel += state.settlement.kind == NeutralFactionKind.BlackElementals ? 2.5f : 2f;
        PlaySandRunnerSound(SandRunnerSound.Construction, root.position, 0.58f);
    }

    private void DeploySettlementTechnicalUnit(SettlementDevelopmentState state)
    {
        int index = state.deployedUnits++;
        float angle = (index * 79f + 46f) * Mathf.Deg2Rad;
        Vector3 local = new Vector3(Mathf.Cos(angle) * 8.5f, 0.35f, Mathf.Sin(angle) * 8.5f);
        Transform unit = CreateNeutralGuard(state.settlement.root, "Developed_Technical_" + index, local, state.settlement.accentMaterial, state.settlement.kind == NeutralFactionKind.Grounders);
        state.settlement.guards.Add(unit);
        state.defenseLevel += state.settlement.kind == NeutralFactionKind.BlackElementals ? 2f : 1.4f;
        PlaySandRunnerSound(SandRunnerSound.HangarRelease, unit.position, 0.48f);
    }

    private void SpawnTradeCaravan()
    {
        NeutralSettlement origin = null;
        NeutralSettlement destination = null;
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement candidate = neutralSettlements[(i + nextTradeCaravanId) % neutralSettlements.Count];
            if (candidate == null || candidate.root == null || candidate.health <= 0f)
                continue;
            SettlementDevelopmentState candidateState;
            if (settlementDevelopment.TryGetValue(candidate, out candidateState) &&
                IsSettlementUnavailableToPlayer(candidateState))
                continue;
            if (origin == null && candidate.kind != NeutralFactionKind.Grounders)
                origin = candidate;
            else if (origin != null && candidate != origin)
            {
                destination = candidate;
                break;
            }
        }
        if (origin == null || destination == null)
            return;

        TradeCaravanState caravan = new TradeCaravanState();
        caravan.origin = origin;
        caravan.destination = destination;
        caravan.faction = origin.kind;
        caravan.eventId = nextTradeCaravanId++;
        Transform root = new GameObject("Trade_Caravan_" + caravan.eventId).transform;
        root.SetParent(livingWorldRoot, false);
        root.position = origin.root.position + origin.root.forward * 13f + Vector3.up * 1.1f;
        caravan.root = root;
        BuildTradeCaravanVisual(caravan);
        tradeCaravans.Add(caravan);
        PushLivingWorldEvent(LivingWorldEventType.Caravan, origin.displayName + " caravan departing for " + destination.displayName + ".", 12f, false);
        lastEvent = origin.displayName + " caravan is moving toward " + destination.displayName + ".";
    }

    private void BuildTradeCaravanVisual(TradeCaravanState caravan)
    {
        Material material = caravan.faction == NeutralFactionKind.BlackElementals ? elementalBlackMaterial : traderBlueMaterial;
        CreateBox(caravan.root, "Caravan_Chassis", new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(2.8f, 0.75f, 5.2f), material, true);
        CreateBox(caravan.root, "Caravan_Cargo", new Vector3(0f, 1.45f, -0.5f), Quaternion.identity, new Vector3(2.2f, 1.2f, 2.5f), material, true);
        CreateBox(caravan.root, "Caravan_Guard_Turret", new Vector3(0f, 1.75f, 1.45f), Quaternion.identity, new Vector3(0.9f, 0.55f, 0.9f), material, true);
        CreateBox(caravan.root, "Caravan_Guard_Cannon", new Vector3(0f, 1.82f, 2.35f), Quaternion.identity, new Vector3(0.18f, 0.18f, 1.3f), material);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCylinder(caravan.root, "Caravan_Wheel_Front_" + side, new Vector3(side * 1.55f, 0.35f, 1.45f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.55f, 0.28f, 0.55f), elementalBlackMaterial, true);
            CreateCylinder(caravan.root, "Caravan_Wheel_Rear_" + side, new Vector3(side * 1.55f, 0.35f, -1.45f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.55f, 0.28f, 0.55f), elementalBlackMaterial, true);
        }
        CreatePointLight(caravan.root, "Caravan_Navigation", new Vector3(0f, 2.1f, 2f), caravan.faction == NeutralFactionKind.BlackElementals ? new Color(0.55f, 0.7f, 1f, 1f) : new Color(0.1f, 0.4f, 1f, 1f), 0.75f, 16f);

        LineRenderer route = new GameObject("Caravan_Route").AddComponent<LineRenderer>();
        route.transform.SetParent(caravan.root, false);
        route.useWorldSpace = true;
        route.positionCount = 2;
        route.startWidth = 0.12f;
        route.endWidth = 0.03f;
        route.sharedMaterial = material;
        route.startColor = new Color(0.2f, 0.65f, 1f, 0.55f);
        route.endColor = new Color(0.2f, 0.65f, 1f, 0.08f);
        caravan.routeLine = route;
    }

    private void UpdateTradeCaravans(float dt)
    {
        for (int i = tradeCaravans.Count - 1; i >= 0; i--)
        {
            TradeCaravanState caravan = tradeCaravans[i];
            if (caravan == null || caravan.root == null || caravan.destination == null || caravan.destination.root == null)
            {
                tradeCaravans.RemoveAt(i);
                continue;
            }

            Vector3 destination = caravan.destination.root.position;
            Vector3 toDestination = destination - caravan.root.position;
            toDestination.y = 0f;
            if (toDestination.magnitude <= 9f)
            {
                DeliverTradeCaravan(caravan);
                tradeCaravans.RemoveAt(i);
                continue;
            }

            Vector3 move = toDestination.normalized * caravan.speed * dt;
            caravan.root.position += move;
            Vector3 position = caravan.root.position;
            position.y = GetPlayableGroundHeight(position) + 1.05f;
            caravan.root.position = position;
            RotateToward(caravan.root, toDestination, 85f * dt);
            if (caravan.routeLine != null)
            {
                caravan.routeLine.SetPosition(0, caravan.root.position + Vector3.up * 0.3f);
                caravan.routeLine.SetPosition(1, destination + Vector3.up * 0.3f);
            }

            EnemyUnit raider = FindNearestJuzzherBarge(caravan.root.position, 22f);
            if (raider != null && raider.transform != null)
            {
                caravan.raided = true;
                caravan.health -= 12f * dt;
                raider.health -= 7f * dt;
                CreateBeam(caravan.root.position + Vector3.up * 1.8f, raider.transform.position + Vector3.up * 1.1f, new Color(0.18f, 0.55f, 1f, 1f), 0.035f, 0.08f);
                if (caravan.health <= 0f)
                {
                    PushLivingWorldEvent(LivingWorldEventType.Raid, caravan.origin.displayName + " caravan was destroyed by Juzzher raiders.", 12f, false);
                    lastEvent = "Juzzher raiders destroyed a neutral caravan. The destination will not develop.";
                    Destroy(caravan.root.gameObject);
                    tradeCaravans.RemoveAt(i);
                }
            }
        }
    }

    private void DeliverTradeCaravan(TradeCaravanState caravan)
    {
        SettlementDevelopmentState state;
        if (settlementDevelopment.TryGetValue(caravan.destination, out state))
            CompleteSettlementTrade(state, false);
        PushLivingWorldEvent(LivingWorldEventType.Caravan, caravan.destination.displayName + " received a trade caravan.", 8f, false);
        if (caravan.root != null)
            Destroy(caravan.root.gameObject);
    }

    private void TryBuildOrRepairEnergyNetwork()
    {
        SettlementDevelopmentState destination = FindNearestSettlementDevelopment(battlePyramid.position, 300f);
        if (destination == null)
        {
            lastEvent = "No settlement in mirror-grid range. Move within 300 metres.";
            ShowBanner("NO SETTLEMENT IN GRID RANGE", 2f);
            return;
        }
        if (destination.network != null)
        {
            if (destination.network.online)
            {
                lastEvent = destination.settlement.displayName + " already has an online mirror grid.";
                return;
            }
            if (!TrySpendResources(0f, balanceProfile.mirrorGridRepairGold, balanceProfile.mirrorGridRepairWind, "mirror-grid repair"))
                return;
            for (int i = 0; i < destination.network.relayHealth.Count; i++)
            {
                destination.network.relayHealth[i] = 180f;
                if (destination.network.relays[i] != null)
                    destination.network.relays[i].gameObject.SetActive(true);
            }
            destination.network.online = true;
            if (destination.network.beamLine != null)
                destination.network.beamLine.enabled = true;
            PlaySandRunnerSound(SandRunnerSound.Construction, destination.settlement.root.position, 0.7f);
            lastEvent = destination.settlement.displayName + " mirror grid repaired.";
            return;
        }

        ResourceNode windSource = FindNearestControlledWindNode(destination.settlement.root.position);
        if (windSource == null)
        {
            lastEvent = "Capture a wind resource point before building a mirror grid.";
            ShowBanner("CONTROLLED WIND NODE REQUIRED", 2.4f);
            return;
        }
        if (!TrySpendResources(0f, 60f, 90f, "settlement mirror grid"))
            return;

        EnergyNetworkState network = CreateEnergyNetwork(windSource, destination);
        destination.network = network;
        destination.playerInfluence = Mathf.Min(100f, destination.playerInfluence + 35f);
        destination.tradeTrust = Mathf.Min(100f, destination.tradeTrust + 15f);
        energyNetworks.Add(network);
        UpdateSettlementDiplomacy(destination);
        PlaySandRunnerSound(SandRunnerSound.Construction, destination.settlement.root.position, 0.82f);
        PushLivingWorldEvent(LivingWorldEventType.EnergyNetwork, destination.settlement.displayName + " connected to the wind mirror grid.", 12f, false);
        lastEvent = destination.settlement.displayName + " has power. Wind income and settlement growth are accelerated.";
        ShowBanner("MIRROR GRID ONLINE", 2.8f);
    }

    private ResourceNode FindNearestControlledWindNode(Vector3 destination)
    {
        ResourceNode best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null || node.kind != ResourceKind.Wind || !node.controlled)
                continue;
            float distance = FlatDistance(node.transform.position, destination);
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }
        return best;
    }

    private EnergyNetworkState CreateEnergyNetwork(ResourceNode source, SettlementDevelopmentState destination)
    {
        EnergyNetworkState network = new EnergyNetworkState();
        network.source = source;
        network.destination = destination;
        network.root = new GameObject("Mirror_Grid_" + destination.settlement.displayName).transform;
        network.root.SetParent(livingWorldRoot, false);
        Vector3 start = source.transform.position;
        Vector3 end = destination.settlement.root.position;
        for (int i = 0; i < 3; i++)
        {
            float t = (i + 1f) / 4f;
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y = GetPlayableGroundHeight(position) + 0.2f;
            Transform relay = new GameObject("Mirror_Relay_" + (i + 1)).transform;
            relay.SetParent(network.root, false);
            relay.position = position;
            CreateCylinder(relay, "Relay_Base", Vector3.zero, Quaternion.identity, new Vector3(1.4f, 0.18f, 1.4f), commandMaterial, true);
            CreateBox(relay, "Relay_Mast", new Vector3(0f, 2.2f, 0f), Quaternion.identity, new Vector3(0.34f, 4.2f, 0.34f), commandMaterial, true);
            CreateBox(relay, "Relay_Mirror", new Vector3(0f, 4.4f, 0f), Quaternion.Euler(18f, i * 27f, 35f), new Vector3(2.2f, 0.12f, 1.4f), traderBlueMaterial);
            CreatePointLight(relay, "Relay_Light", new Vector3(0f, 4.5f, 0f), new Color(0.2f, 0.8f, 1f, 1f), 0.72f, 16f);
            network.relays.Add(relay);
            network.relayHealth.Add(180f);
        }

        LineRenderer line = new GameObject("Mirror_Energy_Beam").AddComponent<LineRenderer>();
        line.transform.SetParent(network.root, false);
        line.useWorldSpace = true;
        line.positionCount = 5;
        line.startWidth = 0.26f;
        line.endWidth = 0.26f;
        line.sharedMaterial = commandMaterial != null ? commandMaterial : traderBlueMaterial;
        line.startColor = new Color(0.35f, 0.95f, 1f, 0.92f);
        line.endColor = new Color(0.72f, 0.95f, 1f, 0.92f);
        network.beamLine = line;
        RefreshEnergyNetworkLine(network);
        return network;
    }

    private void UpdateEnergyNetworks(float dt)
    {
        for (int i = 0; i < energyNetworks.Count; i++)
        {
            EnergyNetworkState network = energyNetworks[i];
            if (network == null || network.source == null || network.source.transform == null || network.destination == null || network.destination.settlement.root == null)
                continue;
            if (!network.online)
                continue;

            network.pulse += dt;
            wind += dt * 0.18f;
            bool broken = false;
            for (int relayIndex = 0; relayIndex < network.relays.Count; relayIndex++)
            {
                Transform relay = network.relays[relayIndex];
                if (relay == null)
                    continue;
                relay.Rotate(Vector3.up, (12f + relayIndex * 4f) * dt, Space.Self);
                EnemyUnit raider = FindNearestJuzzherBarge(relay.position, 15f);
                if (raider != null)
                {
                    network.relayHealth[relayIndex] -= 18f * dt;
                    if (network.relayHealth[relayIndex] <= 0f)
                    {
                        relay.gameObject.SetActive(false);
                        broken = true;
                    }
                }
            }
            if (broken)
            {
                network.online = false;
                if (network.beamLine != null)
                    network.beamLine.enabled = false;
                lastEvent = network.destination.settlement.displayName + " mirror grid was cut by Juzzher raiders. Press Y nearby to repair it.";
                ShowBanner("MIRROR GRID OFFLINE", 2.6f);
                PushLivingWorldEvent(LivingWorldEventType.Raid, "Juzzher raiders disabled a mirror-grid relay.", 12f, false);
                continue;
            }
            RefreshEnergyNetworkLine(network);
        }
    }

    private void RefreshEnergyNetworkLine(EnergyNetworkState network)
    {
        if (network == null || network.beamLine == null)
            return;
        network.beamLine.SetPosition(0, network.source.transform.position + Vector3.up * 4f);
        for (int i = 0; i < network.relays.Count; i++)
            network.beamLine.SetPosition(i + 1, network.relays[i].position + Vector3.up * 4.4f);
        network.beamLine.SetPosition(4, network.destination.settlement.root.position + Vector3.up * 6f);
    }

    private void UpdateSettlementRaidWitnesses()
    {
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || state.settlement.root == null || state.settlement.health <= 0f)
                continue;
            if (IsSettlementUnderMandarinkaOccupation(state))
            {
                state.underRaid = true;
                state.previousHealth = state.settlement.health;
                continue;
            }
            bool attacked = FindNearestJuzzherBarge(state.settlement.root.position, 55f) != null;
            if (attacked)
                state.underRaid = true;
            else if (state.underRaid)
            {
                state.underRaid = false;
                state.defendedFromRaid = true;
                state.tradeTrust = Mathf.Min(100f, state.tradeTrust + 10f);
                state.playerInfluence = Mathf.Min(100f, state.playerInfluence + 10f);
                UpdateSettlementDiplomacy(state);
                PushLivingWorldEvent(LivingWorldEventType.Raid, state.settlement.displayName + " survived a Juzzher raid.", 10f, false);
            }
            state.previousHealth = state.settlement.health;
        }
    }

    private void PushLivingWorldEvent(LivingWorldEventType type, string label, float life, bool crisis)
    {
        if (crisis)
        {
            for (int i = 0; i < livingWorldEvents.Count; i++)
                if (livingWorldEvents[i].crisis)
                    return;
        }
        while (livingWorldEvents.Count >= presentationBudget.maxWorldEvents)
            livingWorldEvents.RemoveAt(0);
        LivingWorldEvent worldEvent = new LivingWorldEvent();
        worldEvent.id = nextLivingWorldEventId++;
        worldEvent.type = type;
        worldEvent.label = label;
        worldEvent.life = life;
        worldEvent.crisis = crisis;
        livingWorldEvents.Add(worldEvent);
    }

    private void UpdateLivingWorldEvents(float dt)
    {
        for (int i = livingWorldEvents.Count - 1; i >= 0; i--)
        {
            livingWorldEvents[i].life -= dt;
            if (livingWorldEvents[i].life <= 0f)
                livingWorldEvents.RemoveAt(i);
        }
    }
}