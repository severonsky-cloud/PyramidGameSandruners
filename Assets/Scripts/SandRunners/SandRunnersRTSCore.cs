using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private enum ProductionKind
    {
        VimanaRunner,
        CombatFlyer,
        ScarabTank,
        BuilderTruck,
        AbydosFlyer,
        HeavyFlyer,
        WrathOfRa,
        FortressCrusher,
        ThothEmbrace,
        SandSkimmer,
        SiegeScarab,
        SalvageScarab
    }

    private enum OrderType
    {
        None,
        Move,
        Attack,
        Harvest,
        Build
    }

    private enum SquadDoctrine
    {
        EscortPyramid,
        HoldArea,
        SearchAndDestroy
    }

    private enum StructureKind
    {
        ResourceDepot,
        Twin30mmTurret,
        MirrorBeamTurret,
        GepardAALauncher,
        AnubisStrikeLauncher,
        Aerodrome,
        CruiseMissileSilo
    }

    private sealed class ProductionJob
    {
        public ProductionKind kind;
        public string label;
        public int count;
        public float buildTime;
        public float progress;
        public bool airborne;
        public bool groundReady;
    }

    private sealed class UnitSquad
    {
        public int id;
        public string displayName;
        public readonly List<RunnerUnit> units = new List<RunnerUnit>();
        public GoldenBuilderAsset builder;
        public bool autoEngage = true;
        public OrderType orderType;
        public Vector3 orderPosition;
        public EnemyUnit attackTarget;
        public ResourceNode harvestTarget;
        public BlueprintSite buildTarget;
        public bool selected;
        public bool airborne;
        public bool autonomous;
        public SquadDoctrine doctrine = SquadDoctrine.EscortPyramid;
        public Vector3 doctrineAnchor;
        public float doctrineRadius = 110f;
        public EnemyUnit doctrineTarget;
        public float nextDoctrineScanTime;
    }

    private sealed class SquadOrder
    {
        public OrderType type;
        public Vector3 position;
        public EnemyUnit target;
        public ResourceNode node;
        public BlueprintSite blueprint;
    }

    private sealed class BlueprintSite
    {
        public Transform transform;
        public StructureKind kind;
        public string displayName;
        public float buildTime;
        public float progress;
        public float health = 90f;
        public bool completed;
    }

    private sealed class GoldenStructure
    {
        public Transform transform;
        public StructureKind kind;
        public string displayName;
        public float health;
        public float maxHealth;
        public float fireTimer;
        public int ammo;
        public float reloadTimer;
        public Transform mirror;
        public float mirrorHealth;
        public float storedSand;
        public float storedGold;
        public float storedWind;
        public float cargoTimer;
    }

    private sealed class ResourceCargo
    {
        public Transform transform;
        public ResourceKind kind;
        public float amount;
        public Vector3 target;
        public float speed;
    }

    private readonly List<ProductionJob> productionQueue = new List<ProductionJob>();
    private readonly List<ProductionJob> readyHangarJobs = new List<ProductionJob>();
    private readonly List<UnitSquad> unitSquads = new List<UnitSquad>();
    private readonly List<UnitSquad> selectedSquads = new List<UnitSquad>();
    private readonly List<BlueprintSite> blueprintSites = new List<BlueprintSite>();
    private readonly List<GoldenStructure> goldenStructures = new List<GoldenStructure>();
    private readonly List<ResourceCargo> resourceCargos = new List<ResourceCargo>();

    private UnitSquad rtsAssemblingSquad;
    private int nextSquadId = 1;
    private StructureKind? pendingStructureBlueprint;
    private SquadDoctrine? pendingSquadDoctrine;
    private Vector2 rtsDragStart;
    private Vector2 rtsDragEnd;
    private bool rtsDraggingSelection;
    private GoldenStructure selectedStructure;
    private BlueprintSite selectedBlueprint;

    private void UpdateRTSCore(float dt)
    {
        if (commandCursorMode && WasKeyPressedThisFrame(Key.A))
            ToggleSelectedSquadAutomation();

        UpdateProductionQueue(dt);
        UpdateBlueprintSites(dt);
        UpdateGoldenStructures(dt);
        UpdateResourceCargos(dt);
        PruneRTSSelections();
        UpdateSquadDoctrinePresentation(dt);
        hangarStored = readyHangarJobs.Count;
    }

    private void QueueProduction(ProductionKind kind)
    {
        ProductionJob job = CreateProductionJob(kind);
        if (job == null)
            return;

        float sandCost;
        float goldCost;
        float windCost;
        GetProductionCost(kind, out sandCost, out goldCost, out windCost);
        if (!TrySpendResources(sandCost, goldCost, windCost, job.label))
            return;

        productionQueue.Add(job);
        hangarOpen = true;
        PlaySandRunnerSound(SandRunnerSound.UiConfirm, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.45f);
        lastEvent = job.label + " queued. Production line " + productionQueue.Count + ".";
    }

    private ProductionJob CreateProductionJob(ProductionKind kind)
    {
        ProductionJob job = new ProductionJob();
        job.kind = kind;
        job.count = 1;
        job.buildTime = 7f;
        switch (kind)
        {
            case ProductionKind.VimanaRunner:
                job.label = "Golden Vimana Runner";
                job.buildTime = 7f;
                break;
            case ProductionKind.CombatFlyer:
                job.label = "Combat Flyer wing x5";
                job.count = 5;
                job.airborne = true;
                job.buildTime = 11f;
                break;
            case ProductionKind.ScarabTank:
                job.label = "Scarab Tank squad x3";
                job.count = 3;
                job.buildTime = 12f;
                break;
            case ProductionKind.BuilderTruck:
                job.label = "Solar Builder Truck";
                job.buildTime = 8f;
                break;
            case ProductionKind.AbydosFlyer:
                job.label = "Golden Elemental Flyer";
                job.airborne = true;
                job.buildTime = 12f;
                break;
            case ProductionKind.HeavyFlyer:
                job.label = "Heavy Golden Flyer";
                job.airborne = true;
                job.buildTime = 16f;
                break;
            case ProductionKind.WrathOfRa:
                job.label = "Wrath of Ra platform";
                job.buildTime = 19f;
                break;
            case ProductionKind.FortressCrusher:
                job.label = "Fortress Crusher";
                job.buildTime = 22f;
                break;
            case ProductionKind.ThothEmbrace:
                job.label = "Thoth's Embrace";
                job.airborne = true;
                job.buildTime = 32f;
                break;
            case ProductionKind.SandSkimmer:
                job.label = "Sand Skimmer";
                job.buildTime = 8f;
                break;
            case ProductionKind.SiegeScarab:
                job.label = "Siege Scarab";
                job.buildTime = 14f;
                break;
            case ProductionKind.SalvageScarab:
                job.label = "Salvage Scarab";
                job.buildTime = 18f;
                break;
        }
        job.buildTime *= balanceProfile.productionTimeScale;
        return job;
    }

    private void GetProductionCost(ProductionKind kind, out float sandCost, out float goldCost, out float windCost)
    {
        SandRunnersResourcePrice price = GetReleaseProductionPrice(kind);
        sandCost = price.sand;
        goldCost = price.gold;
        windCost = price.wind;
    }

    private void UpdateProductionQueue(float dt)
    {
        if (productionQueue.Count == 0)
            return;

        ProductionJob job = productionQueue[0];
        int aerodromes = job.airborne ? Mathf.Min(2, CountAerodromes()) : 0;
        if (damageProductionMultiplier <= 0.01f)
            return;
        job.progress += dt * (1f + aerodromes * 0.35f) * Mathf.Lerp(0.55f, 1f, damageProductionMultiplier);
        if (job.progress < job.buildTime)
            return;

        productionQueue.RemoveAt(0);
        if (job.airborne)
        {
            SpawnProductionSquad(job, null);
            PlaySandRunnerSound(SandRunnerSound.ProductionComplete, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.72f);
            lastEvent = aerodromes > 0
                ? job.label + " scrambled from the aerodrome tarmac."
                : job.label + " launched from the air hangar.";
            return;
        }

        job.groundReady = true;
        readyHangarJobs.Add(job);
        if (internalUnitDisplay != null)
            internalUnitDisplay.gameObject.SetActive(true);
        PlaySandRunnerSound(SandRunnerSound.ProductionComplete, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.55f);
        lastEvent = job.label + " ready in hangar. Press G to deploy.";
    }

    private bool TryReleaseReadyProductionSquad(Vector3? orderPosition)
    {
        if (readyHangarJobs.Count == 0)
            return false;

        ProductionJob job = readyHangarJobs[0];
        readyHangarJobs.RemoveAt(0);
        hangarOpen = true;
        UnitSquad squad = SpawnProductionSquad(job, orderPosition);
        if (squad != null)
            SelectOnlySquad(squad);
        Transform representative = squad != null ? GetSquadRepresentative(squad) : null;
        PlaySandRunnerSound(SandRunnerSound.HangarRelease, representative != null ? representative.position : battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.86f);

        if (internalUnitDisplay != null)
            internalUnitDisplay.gameObject.SetActive(readyHangarJobs.Count > 0);

        lastEvent = job.label + " deployed from hangar. Ready squads left: " + readyHangarJobs.Count + ".";
        return true;
    }

    private UnitSquad SpawnProductionSquad(ProductionJob job, Vector3? orderPosition)
    {
        UnitSquad squad = CreateSquad(job.label, job.airborne);
        rtsAssemblingSquad = squad;
        for (int i = 0; i < job.count; i++)
            SpawnProductionUnit(job.kind, i, job.count, job.airborne);
        rtsAssemblingSquad = null;

        if (orderPosition.HasValue)
            IssueSquadMoveOrder(squad, orderPosition.Value, 0);
        else if (!job.airborne)
            IssueSquadMoveOrder(squad, GetHangarExitPosition() + battlePyramid.right * 5f + battlePyramid.forward * 10f, 0);
        else
            IssueSquadMoveOrder(squad, battlePyramid.position + battlePyramid.right * 14f + battlePyramid.forward * 16f, 0);

        return squad;
    }

    private void SpawnProductionUnit(ProductionKind kind, int index, int count, bool airborne)
    {
        Vector3 position = GetSquadSpawnPosition(index, count, airborne);
        Quaternion rotation = battlePyramid != null ? battlePyramid.rotation : Quaternion.identity;
        switch (kind)
        {
            case ProductionKind.BuilderTruck:
                CreateGoldenResourceBuilderImmediate(position);
                break;
            case ProductionKind.CombatFlyer:
                CreateVehicleUnit("Golden_Combat_Flyer_" + index, position, rotation, new Vector3(0.92f, 0.55f, 1.18f), runnerMaterial, 95f, 15.8f, 22f, 30f, "Combat Flyer Squadron", true, 7.5f);
                break;
            case ProductionKind.ScarabTank:
                CreateVehicleUnit("Golden_Scarab_Tank_" + index, position, rotation, new Vector3(1.15f, 0.72f, 1.35f), siegeUnitMaterial, 175f, 7.6f, 40f, 24f, "Scarab Tank");
                break;
            case ProductionKind.AbydosFlyer:
                CreateVehicleUnit("Golden_Abydos_Flyer", position, rotation, new Vector3(1f, 0.66f, 1.25f), runnerMaterial, 110f, 16.2f, 34f, 38f, "Golden Elemental Flyer", true, 8.5f);
                break;
            case ProductionKind.HeavyFlyer:
                CreateVehicleUnit("Golden_Heavy_Flyer", position, rotation, new Vector3(1.22f, 0.78f, 1.42f), siegeUnitMaterial, 195f, 11.2f, 58f, 34f, "Heavy Golden Flyer", true, 10.5f);
                break;
            case ProductionKind.WrathOfRa:
                CreateVehicleUnit("Golden_Wrath_Of_Ra", position, rotation, new Vector3(1.55f, 0.9f, 1.65f), siegeUnitMaterial, 340f, 4.5f, 90f, 46f, "Wrath of Ra");
                break;
            case ProductionKind.FortressCrusher:
                CreateVehicleUnit("Golden_Fortress_Crusher", position, rotation, new Vector3(1.55f, 1f, 1.75f), siegeUnitMaterial, 440f, 4.1f, 65f, 36f, "Fortress Crusher", false, 0f, true);
                break;
            case ProductionKind.ThothEmbrace:
                CreateVehicleUnit("Golden_Thoth_Embrace", position, rotation, new Vector3(2.1f, 1.08f, 2.2f), runnerMaterial, 950f, 5.2f, 78f, 56f, "Thoth's Embrace", true, 15f);
                break;
            case ProductionKind.SandSkimmer:
                CreateVehicleUnit("External_Sand_Skimmer", position, rotation, new Vector3(0.95f, 0.36f, 1.4f), externalUnitMaterial, 70f, 14f, 20f, 13f, "Sand Skimmer");
                break;
            case ProductionKind.SiegeScarab:
                CreateVehicleUnit("External_Siege_Scarab", position, rotation, new Vector3(1.6f, 0.75f, 1.9f), siegeUnitMaterial, 180f, 6.4f, 48f, 18f, "Siege Scarab");
                break;
            case ProductionKind.SalvageScarab:
                RunnerUnit salvage = CreateVehicleUnit("Golden_Salvage_Scarab", position, rotation, new Vector3(1.55f, 0.86f, 1.95f), goldenBuilderMaterial != null ? goldenBuilderMaterial : siegeUnitMaterial, 320f, 5.6f, 0f, 0f, "Salvage Scarab");
                salvage.isSalvageScarab = true;
                salvage.autonomous = true;
                break;
            default:
                CreateVehicleUnit("Golden_Hangar_Vimana_Runner", position, rotation, new Vector3(0.85f, 0.42f, 1.25f), runnerMaterial, 85f, 11.5f, 22f, 15f, "Golden Vimana Runner");
                break;
        }
    }

    private UnitSquad CreateSquad(string displayName, bool airborne)
    {
        UnitSquad squad = new UnitSquad();
        squad.id = nextSquadId++;
        squad.displayName = displayName;
        squad.airborne = airborne;
        squad.doctrine = SquadDoctrine.EscortPyramid;
        squad.doctrineAnchor = battlePyramid != null ? battlePyramid.position : Vector3.zero;
        squad.doctrineRadius = airborne ? 150f : 110f;
        unitSquads.Add(squad);
        return squad;
    }

    private void RegisterRTSRunner(RunnerUnit unit)
    {
        if (unit == null)
            return;

        UnitSquad squad = rtsAssemblingSquad;
        if (squad == null)
            squad = CreateSquad(unit.displayName, unit.airborne);

        unit.squad = squad;
        squad.units.Add(unit);
        squad.airborne = squad.airborne || unit.airborne;
        squad.autonomous = squad.autonomous || unit.autonomous;
    }

    private void RegisterRTSBuilder(GoldenBuilderAsset builder)
    {
        if (builder == null)
            return;

        UnitSquad squad = rtsAssemblingSquad;
        if (squad == null)
            squad = CreateSquad(builder.displayName, false);

        builder.squad = squad;
        squad.builder = builder;
        squad.displayName = builder.displayName;
    }

    private void UpdateRTSRunnerUnit(RunnerUnit runner, int index, float dt)
    {
        UnitSquad squad = runner.squad;
        if (squad == null)
            return;

        if (IsResourceDeveloperAssigned(runner))
            return;

        // Air wings are driven by the dedicated 3D sortie state machine.
        if (runner.airborne)
            return;
        if (runner.isSalvageScarab)
        {
            UpdateSalvageScarabUnit(runner, dt);
            return;
        }

        int localIndex = Mathf.Max(0, squad.units.IndexOf(runner));
        EnemyUnit target = null;
        bool explicitAttack = squad.orderType == OrderType.Attack;
        if (explicitAttack)
        {
            target = squad.attackTarget;
            if (!IsLiveDoctrineTarget(target))
            {
                squad.attackTarget = null;
                squad.orderType = OrderType.None;
                explicitAttack = false;
            }
        }

        Vector3 doctrineAnchor = GetSquadDoctrineAnchor(squad);
        if (!explicitAttack && squad.autoEngage)
            target = ResolveSquadDoctrineTarget(squad, runner.transform.position, doctrineAnchor);

        if (target != null && target.transform != null)
        {
            Vector3 toTarget = target.transform.position - runner.transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            bool mayChase = explicitAttack || IsTargetInsideDoctrine(squad, target, doctrineAnchor);
            if (mayChase && distance > runner.range * 0.72f)
                runner.transform.position += toTarget.normalized * runner.speed * dt;
            RotateToward(runner.transform, toTarget, explicitAttack ? 360f * dt : 300f * dt);
            if (runner.fireCooldown <= 0f && distance <= runner.range)
            {
                bool scarabWeapon = HasCapability(runner, UnitCapability.Scarab);
                runner.fireCooldown = explicitAttack ? (scarabWeapon ? 1.25f : 0.72f) : 0.95f;
                target.health -= explicitAttack ? runner.damage : runner.damage * 0.75f;
                TrackCombatTarget(target, explicitAttack ? (scarabWeapon ? "80-MM TARGET" : "SQUAD TARGET") : "DOCTRINE ENGAGE", explicitAttack ? 3.6f : 2.8f);
                CreateReadableHit(runner.transform.position + Vector3.up * 0.7f, target.transform.position + Vector3.up * 0.85f, explicitAttack && scarabWeapon);
            }
        }
        else if (squad.orderType == OrderType.Move)
        {
            MoveRunnerToFormationPoint(runner, squad, localIndex, dt);
        }
        else
        {
            Vector3 doctrinePoint = GetSquadDoctrineFormationPoint(squad, localIndex, Mathf.Max(1, squad.units.Count));
            float followSpeed = squad.doctrine == SquadDoctrine.EscortPyramid ? runner.speed : runner.speed * 0.8f;
            MoveRunnerToward(runner, doctrinePoint, followSpeed, dt);
        }

        Vector3 position = runner.transform.position;
        float groundHeight = GetPlayableGroundHeight(position);
        position.y = groundHeight + 0.34f;
        runner.transform.position = position;
    }

    private bool IsLiveDoctrineTarget(EnemyUnit target)
    {
        return target != null && target.transform != null && target.health > 0f;
    }

    private Vector3 GetSquadDoctrineAnchor(UnitSquad squad)
    {
        if (squad == null || squad.doctrine == SquadDoctrine.EscortPyramid)
            return battlePyramid != null ? battlePyramid.position : Vector3.zero;
        return squad.doctrineAnchor;
    }

    private bool IsTargetInsideDoctrine(UnitSquad squad, EnemyUnit target, Vector3 anchor)
    {
        if (!IsLiveDoctrineTarget(target))
            return false;
        return FlatDistance(target.transform.position, anchor) <= squad.doctrineRadius * 1.12f;
    }

    private EnemyUnit ResolveSquadDoctrineTarget(UnitSquad squad, Vector3 seeker, Vector3 anchor)
    {
        if (squad == null || !squad.autoEngage)
            return null;

        if (IsTargetInsideDoctrine(squad, squad.doctrineTarget, anchor))
            return squad.doctrineTarget;

        squad.doctrineTarget = null;
        if (Time.time < squad.nextDoctrineScanTime)
            return null;

        squad.nextDoctrineScanTime = Time.time + 0.25f;
        EnemyUnit best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit candidate = enemies[i];
            if (!IsLiveDoctrineTarget(candidate))
                continue;

            float anchorDistance = FlatDistance(candidate.transform.position, anchor);
            if (anchorDistance > squad.doctrineRadius)
                continue;

            float seekerDistance = FlatDistance(candidate.transform.position, seeker);
            float score = seekerDistance + anchorDistance * 0.2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        squad.doctrineTarget = best;
        return best;
    }

    private Vector3 GetSquadDoctrineFormationPoint(UnitSquad squad, int index, int count)
    {
        Vector3 anchor = GetSquadDoctrineAnchor(squad);
        Vector3 center = anchor;
        float spacing = 4.2f;
        if (squad.doctrine == SquadDoctrine.EscortPyramid)
        {
            center -= battlePyramid.forward * (16f + (squad.id % 3) * 5f);
            spacing = 5f;
        }
        else if (squad.doctrine == SquadDoctrine.SearchAndDestroy)
        {
            float angle = Time.time * 0.18f + squad.id * 1.73f;
            float patrolRadius = Mathf.Min(52f, squad.doctrineRadius * 0.28f);
            center += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patrolRadius;
        }

        center.y = GetPlayableGroundHeight(center);
        return center + GetSquadFormationOffset(squad, index, count, spacing);
    }

    private void MoveRunnerToFormationPoint(RunnerUnit runner, UnitSquad squad, int index, float dt)
    {
        Vector3 destination = squad.orderPosition + GetSquadFormationOffset(squad, index, Mathf.Max(1, squad.units.Count), 2.6f);
        MoveRunnerToward(runner, destination, runner.speed, dt);
        if (index == 0 && IsSquadNearOrderPoint(squad, 5.5f))
            squad.orderType = OrderType.None;
    }

    private bool IsSquadNearOrderPoint(UnitSquad squad, float tolerance)
    {
        if (squad == null)
            return true;
        for (int i = 0; i < squad.units.Count; i++)
        {
            RunnerUnit unit = squad.units[i];
            if (unit != null && unit.transform != null && FlatDistance(unit.transform.position, squad.orderPosition) > tolerance)
                return false;
        }
        if (squad.builder != null && squad.builder.transform != null && FlatDistance(squad.builder.transform.position, squad.orderPosition) > tolerance)
            return false;
        return true;
    }

    private void MoveRunnerToward(RunnerUnit runner, Vector3 destination, float speed, float dt)
    {
        Vector3 toTarget = destination - runner.transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > 0.35f)
        {
            runner.transform.position += toTarget.normalized * speed * dt;
            RotateToward(runner.transform, toTarget, 260f * dt);
        }
    }

    private Vector3 GetSquadFormationOffset(UnitSquad squad, int index, int count, float spacing)
    {
        int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, 5);
        int row = index / columns;
        int column = index % columns;
        int columnsInRow = Mathf.Min(columns, count - row * columns);
        float x = (column - (columnsInRow - 1) * 0.5f) * spacing;
        float z = -row * spacing * 0.78f;
        return battlePyramid.right * x + battlePyramid.forward * z;
    }

    private void UpdateRTSBuilder(GoldenBuilderAsset builder, float dt)
    {
        UnitSquad squad = builder.squad;
        if (squad == null)
            return;

        if (IsResourceDeveloperAssigned(builder))
            return;

        EnemyUnit threat = FindNearestEnemy(builder.transform.position, 28f);
        builder.fireTimer -= dt;
        if (squad.autoEngage && threat != null && threat.transform != null && builder.fireTimer <= 0f)
        {
            builder.fireTimer = 1.55f;
            threat.health -= 10f;
            TrackCombatTarget(threat, "BUILDER DEFENSE", 2.8f);
            CreateReadableHit(builder.transform.position + Vector3.up * 1.05f, threat.transform.position + Vector3.up * 0.8f, false);
        }

        if (squad.orderType == OrderType.Build && squad.buildTarget != null && !squad.buildTarget.completed)
        {
            builder.buildTarget = squad.buildTarget;
            MoveBuilderToBuild(builder, squad.buildTarget, dt);
        }
        else if (squad.orderType == OrderType.Harvest && squad.harvestTarget != null)
        {
            UpdateBuilderHarvest(builder, squad.harvestTarget, dt);
        }
        else if (squad.orderType == OrderType.Move)
        {
            MoveBuilderToward(builder, squad.orderPosition, 6.5f, dt);
            if (FlatDistance(builder.transform.position, squad.orderPosition) <= 1.5f)
                squad.orderType = OrderType.None;
        }
        else if (!builder.carryingCargo && builder.targetNode != null)
        {
            UpdateBuilderHarvest(builder, builder.targetNode, dt);
        }
        else
        {
            int builderFormationIndex = Mathf.Max(0, squad.units.Count);
            Vector3 doctrinePoint = GetSquadDoctrineFormationPoint(squad, builderFormationIndex, builderFormationIndex + 1);
            MoveBuilderToward(builder, doctrinePoint, squad.doctrine == SquadDoctrine.EscortPyramid ? 7.2f : 5.8f, dt);
        }

        Vector3 position = builder.transform.position;
        position.y = GetPlayableGroundHeight(position) + 0.34f;
        builder.transform.position = position;
    }

    private void MoveBuilderToward(GoldenBuilderAsset builder, Vector3 destination, float speed, float dt)
    {
        Vector3 toTarget = destination - builder.transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude <= 0.4f)
            return;

        builder.transform.position += toTarget.normalized * speed * dt;
        RotateFlatToward(builder.transform, toTarget, 210f * dt);
    }

    private void MoveBuilderToBuild(GoldenBuilderAsset builder, BlueprintSite site, float dt)
    {
        if (site == null || site.transform == null)
            return;

        float distance = FlatDistance(builder.transform.position, site.transform.position);
        if (distance > 4.5f)
        {
            MoveBuilderToward(builder, site.transform.position, 6.4f, dt);
            return;
        }

        site.progress += dt;
        CreateWeaponTracer(builder.transform.position + Vector3.up * 1.2f, site.transform.position + Vector3.up * 0.7f, new Color(1f, 0.82f, 0.2f, 0.72f), 0.035f, 0.08f);
    }

    private void UpdateBuilderHarvest(GoldenBuilderAsset builder, ResourceNode node, float dt)
    {
        if (node == null || node.transform == null)
            return;

        if (builder.carryingCargo)
        {
            Vector3 dropoff = FindNearestDropoffPosition(builder.transform.position);
            if (FlatDistance(builder.transform.position, dropoff) > 4.2f)
            {
                MoveBuilderToward(builder, dropoff, 6.6f, dt);
                return;
            }

            DepositBuilderCargo(builder, dropoff);
            return;
        }

        if (FlatDistance(builder.transform.position, node.transform.position) > 5.2f)
        {
            MoveBuilderToward(builder, node.transform.position, 6.6f, dt);
            return;
        }

        builder.gatherTimer += dt;
        if (builder.gatherTimer < balanceProfile.builderGatherSeconds)
            return;

        builder.gatherTimer = 0f;
        builder.cargoKind = node.kind;
        builder.cargoAmount = node.controlled ? balanceProfile.builderCargoControlled : balanceProfile.builderCargoNeutral;
        builder.carryingCargo = true;
        PlaySandRunnerSound(SandRunnerSound.HarvestStart, builder.transform.position, 0.55f);
        lastEvent = builder.displayName + " loaded " + builder.cargoKind + " cargo. Returning to dropoff.";
    }

    private Vector3 FindNearestDropoffPosition(Vector3 origin)
    {
        GoldenStructure depot = FindNearestStructure(origin, StructureKind.ResourceDepot, 95f);
        if (depot != null && depot.transform != null)
            return depot.transform.position;
        return battlePyramid.position;
    }

    private void DepositBuilderCargo(GoldenBuilderAsset builder, Vector3 dropoff)
    {
        GoldenStructure depot = FindNearestStructure(builder.transform.position, StructureKind.ResourceDepot, 8f);
        if (depot != null)
        {
            if (builder.cargoKind == ResourceKind.Gold)
                depot.storedGold += builder.cargoAmount;
            else if (builder.cargoKind == ResourceKind.Wind)
                depot.storedWind += builder.cargoAmount;
            else
                depot.storedSand += builder.cargoAmount;
            PlaySandRunnerSound(SandRunnerSound.HarvestReturn, builder.transform.position, 0.45f);
            lastEvent = "Depot received " + Mathf.RoundToInt(builder.cargoAmount) + " " + builder.cargoKind + ". Cargo flyer preparing launch.";
        }
        else
        {
            AddResource(builder.cargoKind, builder.cargoAmount);
            PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, builder.transform.position, 0.58f);
            lastEvent = "Builder delivered " + Mathf.RoundToInt(builder.cargoAmount) + " " + builder.cargoKind + " to the pyramid.";
        }

        builder.carryingCargo = false;
        builder.cargoAmount = 0f;
    }

    private void AddResource(ResourceKind kind, float amount)
    {
        if (kind == ResourceKind.Gold)
            gold += amount;
        else if (kind == ResourceKind.Wind)
            wind += amount * balanceProfile.windCargoMultiplier;
        else
            sand += amount;
    }

    private bool HandleRTSCommandModeMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        if (pendingStructureBlueprint.HasValue)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                pendingStructureBlueprint = null;
                commandRightMouseDown = false;
                commandRightMouseOrbitDrag = false;
                lastEvent = "Blueprint placement cancelled.";
                return true;
            }

            if (mouse.leftButton.wasPressedThisFrame && TryRaycastCommand(mouse.position.ReadValue(), out RaycastHit buildHit))
            {
                PlaceBlueprintAt(pendingStructureBlueprint.Value, buildHit.point);
                pendingStructureBlueprint = null;
                return true;
            }
            return false;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            rtsDragStart = mouse.position.ReadValue();
            rtsDragEnd = rtsDragStart;
            rtsDraggingSelection = true;
            return true;
        }

        if (rtsDraggingSelection && mouse.leftButton.isPressed)
        {
            rtsDragEnd = mouse.position.ReadValue();
            return true;
        }

        if (rtsDraggingSelection && mouse.leftButton.wasReleasedThisFrame)
        {
            rtsDragEnd = mouse.position.ReadValue();
            rtsDraggingSelection = false;
            if (Vector2.Distance(rtsDragStart, rtsDragEnd) > 14f)
            {
                SelectSquadsInScreenRect(rtsDragStart, rtsDragEnd);
                return true;
            }

            if (TrySelectTouchOfHorusAtScreen(rtsDragEnd))
                return true;

            if (TryRaycastCommand(rtsDragEnd, out RaycastHit hit))
            {
                if (TrySelectRTSObject(hit.transform))
                    return true;

                ClearRTSSelection();
            }
            return true;
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            if (commandRightMouseOrbitDrag)
            {
                commandRightMouseDown = false;
                commandRightMouseOrbitDrag = false;
                return true;
            }

            if (touchOfHorus != null && selectedStrategicTransform == touchOfHorus.root)
            {
                Vector2 horusOrderScreen = mouse.position.ReadValue();
                // Resource development must win over Horus' generous screen-space
                // diplomacy radius. Previously the screen helper consumed RMB near
                // settlements before the clicked resource node was inspected.
                if (TryRaycastCommand(horusOrderScreen, out RaycastHit horusHit))
                {
                    if (!TryIssueResourceDevelopmentOrderAtHit(horusHit) &&
                        !TryIssueTouchOfHorusOrderAtScreen(horusOrderScreen))
                        TryIssueTouchOfHorusOrder(horusHit);
                }
                else
                {
                    TryIssueTouchOfHorusOrderAtScreen(horusOrderScreen);
                }
                commandRightMouseDown = false;
                commandRightMouseOrbitDrag = false;
                return true;
            }

            if (!HasRTSSelection())
                return false;

            if (TryRaycastCommand(mouse.position.ReadValue(), out RaycastHit hit))
                IssueRTSRightClickOrder(hit);
            commandRightMouseDown = false;
            commandRightMouseOrbitDrag = false;
            return true;
        }

        return false;
    }

    private bool TryRaycastCommand(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, 900f);
    }

    private bool TrySelectRTSObject(Transform hitTransform)
    {
        if (TrySelectTouchOfHorus(hitTransform))
            return true;

        UnitSquad squad = FindSquadByHit(hitTransform);
        if (squad != null)
        {
            SelectOnlySquad(squad);
            return true;
        }

        GoldenStructure structure = FindStructureByHit(hitTransform);
        if (structure != null)
        {
            ClearRTSSelection();
            selectedStructure = structure;
            lastEvent = "Selected: " + structure.displayName + ".";
            return true;
        }

        BlueprintSite site = FindBlueprintByHit(hitTransform);
        if (site != null)
        {
            ClearRTSSelection();
            selectedBlueprint = site;
            lastEvent = "Selected blueprint: " + site.displayName + ". Send builders with RMB.";
            return true;
        }

        return false;
    }

    private UnitSquad FindSquadByHit(Transform hit)
    {
        if (hit == null)
            return null;

        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            if (squad == null)
                continue;
            if (squad.builder != null && squad.builder.transform != null && IsTransformInHierarchy(hit, squad.builder.transform))
                return squad;
            for (int u = 0; u < squad.units.Count; u++)
            {
                RunnerUnit unit = squad.units[u];
                if (unit != null && unit.transform != null && IsTransformInHierarchy(hit, unit.transform))
                    return squad;
            }
        }
        return null;
    }

    private void SelectOnlySquad(UnitSquad squad)
    {
        ClearRTSSelection();
        if (squad == null)
            return;
        squad.selected = true;
        selectedSquads.Add(squad);
        selectedStrategicTransform = GetSquadRepresentative(squad);
        selectedStrategicName = squad.displayName;
        lastEvent = "Selected squad: " + squad.displayName + ". RMB ground/resource/enemy for orders.";
    }

    private void SelectSquadsInScreenRect(Vector2 a, Vector2 b)
    {
        ClearRTSSelection();
        Rect rect = MakeScreenRect(a, b);
        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            Transform rep = GetSquadRepresentative(squad);
            if (rep == null)
                continue;

            Vector3 screen = mainCamera.WorldToScreenPoint(rep.position);
            if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y)))
            {
                squad.selected = true;
                selectedSquads.Add(squad);
            }
        }

        if (selectedSquads.Count > 0)
        {
            selectedStrategicTransform = GetSquadRepresentative(selectedSquads[0]);
            selectedStrategicName = selectedSquads.Count == 1 ? selectedSquads[0].displayName : selectedSquads.Count + " squads";
            lastEvent = "Selected " + selectedSquads.Count + " squads. RMB to command formation.";
        }
        else
            lastEvent = "Selection box found no squads.";
    }

    private Rect MakeScreenRect(Vector2 a, Vector2 b)
    {
        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minY = Mathf.Min(a.y, b.y);
        float maxY = Mathf.Max(a.y, b.y);
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private Transform GetSquadRepresentative(UnitSquad squad)
    {
        if (squad == null)
            return null;
        if (squad.builder != null)
            return squad.builder.transform;
        for (int i = 0; i < squad.units.Count; i++)
        {
            if (squad.units[i] != null && squad.units[i].transform != null)
                return squad.units[i].transform;
        }
        return null;
    }

    private bool TryGetSquadCentroid(UnitSquad squad, out Vector3 centroid)
    {
        centroid = Vector3.zero;
        if (squad == null)
            return false;

        int count = 0;
        if (squad.builder != null && squad.builder.transform != null && squad.builder.health > 0f)
        {
            centroid += squad.builder.transform.position;
            count++;
        }

        for (int i = 0; i < squad.units.Count; i++)
        {
            RunnerUnit unit = squad.units[i];
            if (unit == null || unit.transform == null || unit.health <= 0f)
                continue;

            centroid += unit.transform.position;
            count++;
        }

        if (count == 0)
            return false;

        centroid /= count;
        return true;
    }

    private Vector3 GetRTSCameraFollowPoint(Transform fallback)
    {
        if (selectedSquads.Count > 0)
        {
            Vector3 centroid = Vector3.zero;
            int count = 0;
            for (int i = 0; i < selectedSquads.Count; i++)
            {
                if (TryGetSquadCentroid(selectedSquads[i], out Vector3 squadCentroid))
                {
                    centroid += squadCentroid;
                    count++;
                }
            }

            if (count > 0)
                return centroid / count;
        }

        if (selectedStructure != null && selectedStructure.transform != null)
            return selectedStructure.transform.position;
        if (selectedBlueprint != null && selectedBlueprint.transform != null)
            return selectedBlueprint.transform.position;
        if (selectedStrategicTransform != null)
            return selectedStrategicTransform.position;
        if (fallback != null)
            return fallback.position;
        return battlePyramid != null ? battlePyramid.position : Vector3.zero;
    }

    private bool HasRTSSelection()
    {
        return selectedSquads.Count > 0 || selectedStructure != null || selectedBlueprint != null;
    }

    private bool SelectFirstRTSSquad(string group)
    {
        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            if (squad == null)
                continue;

            string name = squad.displayName;
            bool match =
                (group == "Flyer" && name.Contains("Flyer")) ||
                (group == "Scarab" && name.Contains("Scarab")) ||
                (group == "Builder" && squad.builder != null) ||
                (group == "Turret" && goldenStructures.Count > 0) ||
                (group == "Heavy" && (name.Contains("Heavy") || name.Contains("Wrath"))) ||
                (group == "Special" && (name.Contains("Thoth") || name.Contains("Crusher")));

            if (match && group != "Turret")
            {
                SelectOnlySquad(squad);
                return true;
            }
        }

        if (group == "Turret" && goldenStructures.Count > 0)
        {
            ClearRTSSelection();
            selectedStructure = goldenStructures[0];
            selectedStrategicTransform = selectedStructure.transform;
            selectedStrategicName = selectedStructure.displayName;
            lastEvent = "Selected: " + selectedStructure.displayName + ".";
            return true;
        }

        return false;
    }

    private void ClearRTSSelection()
    {
        for (int i = 0; i < selectedSquads.Count; i++)
            selectedSquads[i].selected = false;
        selectedSquads.Clear();
        selectedStructure = null;
        selectedBlueprint = null;
        selectedStrategicTransform = null;
        selectedStrategicName = null;
    }

    private void IssueRTSRightClickOrder(RaycastHit hit)
    {
        if (pendingSquadDoctrine.HasValue)
        {
            ApplySelectedSquadDoctrine(pendingSquadDoctrine.Value, hit.point);
            pendingSquadDoctrine = null;
            return;
        }

        if (TryIssueResourceDevelopmentOrderAtHit(hit)) return;
        if (TryIssueSalvageOrderAtHit(hit)) return;
        if (TryIssueTouchOfHorusOrder(hit)) return;
        if (TryIssueBuilderRepairOrderAtHit(hit)) return;
        if (TryIssueTouchOfHorusOrder(hit))
            return;

        EnemyUnit enemy = FindEnemyByHit(hit.transform);
        if (enemy == null)
            enemy = FindEnemyNearCommandPoint(hit.point, 14f);
        BlueprintSite blueprint = FindBlueprintByHit(hit.transform);
        ResourceNode resource = FindResourceNodeByHit(hit.transform);

        if (selectedStructure != null && selectedStructure.kind == StructureKind.MirrorBeamTurret && selectedStructure.mirror != null)
        {
            Vector3 mirrorPosition = hit.point;
            mirrorPosition.y = GetPlayableGroundHeight(mirrorPosition) + 7.2f;
            selectedStructure.mirror.position = mirrorPosition;
            lastEvent = "Mirror beam focus moved. New solar kill-zone established.";
            return;
        }

        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null || squad.autonomous)
                continue;

            if (enemy != null)
                IssueSquadAttackOrder(squad, enemy);
            else if (blueprint != null && squad.builder != null)
                IssueSquadBuildOrder(squad, blueprint);
            else if (resource != null && squad.builder != null)
                IssueSquadHarvestOrder(squad, resource);
            else
                IssueSquadMoveOrder(squad, hit.point, i);
        }
    }

    private EnemyUnit FindEnemyByHit(Transform hit)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy != null && enemy.transform != null && IsTransformInHierarchy(hit, enemy.transform))
                return enemy;
        }
        return null;
    }

    private EnemyUnit FindEnemyNearCommandPoint(Vector3 point, float radius)
    {
        EnemyUnit best = null;
        float bestScore = radius * radius;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || enemy.transform == null || enemy.health <= 0f)
                continue;

            Vector3 delta = enemy.transform.position - point;
            delta.y = 0f;
            float score = delta.sqrMagnitude;
            if (score <= bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }

    private ResourceNode FindResourceNodeByHit(Transform hit)
    {
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node != null && node.transform != null && IsTransformInHierarchy(hit, node.transform))
                return node;
        }
        return null;
    }

    private void IssueSquadMoveOrder(UnitSquad squad, Vector3 point, int formationIndex)
    {
        point.y = GetPlayableGroundHeight(point);
        squad.orderType = OrderType.Move;
        squad.orderPosition = point + battlePyramid.right * ((formationIndex % 4) * 4f);
        squad.attackTarget = null;
        squad.harvestTarget = null;
        squad.buildTarget = null;
        PlaceCommandMarker(point);
        lastEvent = squad.displayName + " moving in formation.";
    }

    private void IssueSquadAttackOrder(UnitSquad squad, EnemyUnit target)
    {
        squad.orderType = OrderType.Attack;
        squad.attackTarget = target;
        squad.harvestTarget = null;
        squad.buildTarget = null;
        lastEvent = squad.displayName + " attacking " + target.transform.name.Replace('_', ' ') + ".";
    }

    private void IssueSquadHarvestOrder(UnitSquad squad, ResourceNode node)
    {
        squad.orderType = OrderType.Harvest;
        squad.harvestTarget = node;
        squad.attackTarget = null;
        squad.buildTarget = null;
        if (squad.builder != null)
            squad.builder.targetNode = node;
        lastEvent = squad.displayName + " harvesting " + node.label + ".";
    }

    private void IssueSquadBuildOrder(UnitSquad squad, BlueprintSite site)
    {
        squad.orderType = OrderType.Build;
        squad.buildTarget = site;
        squad.attackTarget = null;
        squad.harvestTarget = null;
        if (squad.builder != null)
            squad.builder.buildTarget = site;
        lastEvent = squad.displayName + " assigned to build " + site.displayName + ".";
    }

    private void SetSelectedSquadsEscortDoctrine()
    {
        BeginSelectedSquadDoctrine(SquadDoctrine.EscortPyramid);
    }

    private void ArmSelectedSquadsHoldDoctrine()
    {
        BeginSelectedSquadDoctrine(SquadDoctrine.HoldArea);
    }

    private void ArmSelectedSquadsSearchDoctrine()
    {
        BeginSelectedSquadDoctrine(SquadDoctrine.SearchAndDestroy);
    }

    private void BeginSelectedSquadDoctrine(SquadDoctrine doctrine)
    {
        if (selectedSquads.Count == 0)
        {
            lastEvent = "Select one or more squads before assigning a doctrine.";
            return;
        }

        if (doctrine == SquadDoctrine.EscortPyramid)
        {
            ApplySelectedSquadDoctrine(doctrine, battlePyramid.position);
            return;
        }

        pendingSquadDoctrine = doctrine;
        pendingStructureBlueprint = null;
        SetCommandCursorMode(true);
        lastEvent = doctrine == SquadDoctrine.HoldArea
            ? "HOLD AREA armed: RMB terrain to place an 85m defensive zone."
            : "SEARCH & DESTROY armed: RMB terrain to place a 260m hunt zone.";
    }

    private void ApplySelectedSquadDoctrine(SquadDoctrine doctrine, Vector3 point)
    {
        point.y = GetPlayableGroundHeight(point);
        float radius = doctrine == SquadDoctrine.SearchAndDestroy ? 260f :
            doctrine == SquadDoctrine.HoldArea ? 85f : 110f;

        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null)
                continue;

            squad.doctrine = doctrine;
            squad.doctrineAnchor = doctrine == SquadDoctrine.EscortPyramid ? battlePyramid.position : point;
            squad.doctrineRadius = squad.airborne && doctrine == SquadDoctrine.EscortPyramid ? 170f : radius;
            squad.autoEngage = true;
            squad.orderType = OrderType.None;
            squad.attackTarget = null;
            squad.harvestTarget = null;
            squad.buildTarget = null;
            squad.doctrineTarget = null;
            squad.nextDoctrineScanTime = 0f;
        }

        if (doctrine != SquadDoctrine.EscortPyramid)
            PlaceCommandMarker(point);

        lastEvent = doctrine == SquadDoctrine.EscortPyramid
            ? "ESCORT PYRAMID: selected squads will maintain formation and defend the moving fortress."
            : doctrine == SquadDoctrine.HoldArea
                ? "HOLD AREA: selected squads will defend the marked 85m zone."
                : "SEARCH & DESTROY: selected squads will patrol and clear the marked 260m zone.";
    }

    private string GetSquadDoctrineLabel(SquadDoctrine doctrine)
    {
        if (doctrine == SquadDoctrine.HoldArea)
            return "HOLD 85M";
        if (doctrine == SquadDoctrine.SearchAndDestroy)
            return "SEARCH 260M";
        return "PYRAMID ESCORT";
    }

    private void BeginStructureBlueprint(StructureKind kind)
    {
        pendingStructureBlueprint = kind;
        SetCommandCursorMode(true);
        lastEvent = "Placing " + GetStructureName(kind) + ". Left-click terrain to place, RMB to cancel.";
    }

    private void PlaceBlueprintAt(StructureKind kind, Vector3 point)
    {
        float sandCost;
        float goldCost;
        float windCost;
        GetStructureCost(kind, out sandCost, out goldCost, out windCost);
        if (!TrySpendResources(sandCost, goldCost, windCost, GetStructureName(kind)))
            return;

        point.y = GetPlayableGroundHeight(point) + 0.08f;
        BlueprintSite site = CreateBlueprintSite(kind, point);
        AssignBuildersToBlueprint(site);
        selectedBlueprint = site;
        lastEvent = site.displayName + " blueprint placed. Builders assigned: " + CountBuildersAssigned(site) + ".";
    }

    private BlueprintSite CreateBlueprintSite(StructureKind kind, Vector3 point)
    {
        EnsureGoldenEngineeringMaterials();
        Transform root = new GameObject("Blueprint_" + kind).transform;
        root.position = point;
        root.rotation = battlePyramid.rotation;
        Material ghost = CreateMaterial("Blueprint " + kind, new Color(0.2f, 1f, 0.55f, 0.42f));
        ConfigureTransparent(ghost);
        CreateBox(root, "Blueprint_Footprint", new Vector3(0f, 0.08f, 0f), Quaternion.identity, GetStructureFootprint(kind), ghost);
        CreateCylinder(root, "Blueprint_Core_Pin", new Vector3(0f, 0.52f, 0f), Quaternion.identity, new Vector3(0.28f, 0.42f, 0.28f), goldenTurretGlowMaterial);
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.35f, 0f);
        collider.size = GetStructureFootprint(kind) + new Vector3(0f, 1f, 0f);

        BlueprintSite site = new BlueprintSite();
        site.transform = root;
        site.kind = kind;
        site.displayName = GetStructureName(kind);
        site.buildTime = GetStructureBuildTime(kind);
        blueprintSites.Add(site);
        return site;
    }

    private void AssignBuildersToBlueprint(BlueprintSite site)
    {
        int assigned = 0;
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            if (selectedSquads[i].builder != null)
            {
                IssueSquadBuildOrder(selectedSquads[i], site);
                assigned++;
            }
        }

        if (assigned > 0)
            return;

        GoldenBuilderAsset nearest = FindNearestIdleBuilder(site.transform.position);
        if (nearest != null && nearest.squad != null)
            IssueSquadBuildOrder(nearest.squad, site);
    }

    private GoldenBuilderAsset FindNearestIdleBuilder(Vector3 position)
    {
        GoldenBuilderAsset best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < goldenBuilders.Count; i++)
        {
            GoldenBuilderAsset builder = goldenBuilders[i];
            if (builder == null || builder.transform == null || builder.squad == null)
                continue;
            float distance = FlatDistance(position, builder.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = builder;
            }
        }
        return best;
    }

    private int CountBuildersAssigned(BlueprintSite site)
    {
        int count = 0;
        for (int i = 0; i < goldenBuilders.Count; i++)
        {
            GoldenBuilderAsset builder = goldenBuilders[i];
            if (builder != null && builder.buildTarget == site)
                count++;
        }
        return count;
    }

    private void UpdateBlueprintSites(float dt)
    {
        for (int i = blueprintSites.Count - 1; i >= 0; i--)
        {
            BlueprintSite site = blueprintSites[i];
            if (site == null || site.transform == null)
            {
                blueprintSites.RemoveAt(i);
                continue;
            }

            float ratio = Mathf.Clamp01(site.progress / Mathf.Max(0.1f, site.buildTime));
            site.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.05f, Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f);
            if (ratio >= 1f)
            {
                Vector3 position = site.transform.position;
                StructureKind kind = site.kind;
                string name = site.displayName;
                Destroy(site.transform.gameObject);
                blueprintSites.RemoveAt(i);
                CreateGoldenStructure(kind, position);
                PlaySandRunnerSound(SandRunnerSound.BuildingComplete, position, 0.6f);
                lastEvent = name + " construction complete.";
            }
        }
    }

    private GoldenStructure CreateGoldenStructure(StructureKind kind, Vector3 position)
    {
        EnsureGoldenEngineeringMaterials();
        Transform root = new GameObject("Golden_" + kind).transform;
        root.position = position;
        root.rotation = battlePyramid.rotation;
        BuildStructureVisual(root, kind);
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.size = GetStructureFootprint(kind) + new Vector3(0f, 2.4f, 0f);

        GoldenStructure structure = new GoldenStructure();
        structure.transform = root;
        structure.kind = kind;
        structure.displayName = GetStructureName(kind);
        structure.health = GetStructureHealth(kind);
        structure.maxHealth = structure.health;
        structure.ammo = kind == StructureKind.GepardAALauncher ? 4 : kind == StructureKind.Aerodrome ? 2 : 0;
        if (kind == StructureKind.MirrorBeamTurret)
        {
            structure.mirror = CreateMirrorDrone(root, position);
            structure.mirrorHealth = 95f;
        }
        goldenStructures.Add(structure);
        CreateBattleExplosionFx(position, 8f, false, false);
        PlaySandRunnerSound(SandRunnerSound.Construction, position, 0.7f);
        lastEvent = structure.displayName + " online.";
        return structure;
    }

    private void BuildStructureVisual(Transform root, StructureKind kind)
    {
        if (kind == StructureKind.ResourceDepot)
        {
            CreateBox(root, "Depot_Gold_Platform", new Vector3(0f, 0.28f, 0f), Quaternion.identity, new Vector3(3.8f, 0.34f, 3.0f), goldenTurretMaterial);
            CreateBox(root, "Depot_Container_Rack", new Vector3(-0.85f, 0.88f, 0.15f), Quaternion.identity, new Vector3(1.1f, 0.8f, 2.35f), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Depot_Cargo_Beacon", new Vector3(1.25f, 1.25f, 0.1f), Quaternion.identity, new Vector3(0.35f, 0.55f, 0.35f), goldenTurretGlowMaterial);
            return;
        }

        if (kind == StructureKind.Twin30mmTurret)
        {
            CreateCylinder(root, "Twin30_Base", new Vector3(0f, 0.32f, 0f), Quaternion.identity, new Vector3(1.35f, 0.25f, 1.35f), goldenTurretMaterial);
            CreateBox(root, "Twin30_Turret_Block", new Vector3(0f, 0.92f, 0f), Quaternion.identity, new Vector3(1.55f, 0.58f, 1.1f), goldenTurretMaterial);
            CreateCylinder(root, "Twin30_Left_Barrel", new Vector3(-0.25f, 1.0f, 0.98f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.09f, 0.86f, 0.09f), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Twin30_Right_Barrel", new Vector3(0.25f, 1.0f, 0.98f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.09f, 0.86f, 0.09f), pyramidDarkArmorMaterial);
            return;
        }

        if (kind == StructureKind.MirrorBeamTurret)
        {
            CreateCylinder(root, "MirrorBeam_Base", new Vector3(0f, 0.32f, 0f), Quaternion.identity, new Vector3(1.65f, 0.32f, 1.65f), goldenTurretMaterial);
            CreateCylinder(root, "MirrorBeam_Projector", new Vector3(0f, 1.15f, 0f), Quaternion.identity, new Vector3(0.62f, 0.72f, 0.62f), goldenTurretMaterial);
            CreateCylinder(root, "MirrorBeam_Lens", new Vector3(0f, 1.68f, 0.65f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.28f, 0.08f, 0.28f), goldenTurretGlowMaterial);
            return;
        }

        if (kind == StructureKind.AnubisStrikeLauncher)
        {
            CreateBox(root, "Anubis_Platform", new Vector3(0f, 0.3f, 0f), Quaternion.identity, new Vector3(3.0f, 0.4f, 2.8f), goldenTurretMaterial);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                float rx = Mathf.Sin(angle) * 0.85f;
                float rz = Mathf.Cos(angle) * 0.85f;
                CreateCylinder(root, "Anubis_Tube_" + i, new Vector3(rx, 1.2f, rz), Quaternion.Euler(75f, -i * 60f, 0f), new Vector3(0.22f, 0.9f, 0.22f), pyramidDarkArmorMaterial);
            }
            CreateCylinder(root, "Anubis_Core", new Vector3(0f, 0.85f, 0f), Quaternion.identity, new Vector3(0.45f, 0.55f, 0.45f), goldenTurretGlowMaterial);
            CreatePointLight(root, "Anubis_Glow", new Vector3(0f, 1.4f, 0f), new Color(1f, 0.6f, 0.1f, 1f), 0.9f, 10f);
            return;
        }

        if (kind == StructureKind.CruiseMissileSilo)
        {
            CreateBox(root, "Silo_Gold_Platform", new Vector3(0f, 0.26f, 0f), Quaternion.identity, new Vector3(4.0f, 0.32f, 3.0f), goldenTurretMaterial);
            CreateBox(root, "Silo_Crew_Bunker", new Vector3(-1.35f, 0.82f, -0.75f), Quaternion.identity, new Vector3(1.1f, 0.85f, 1.2f), pyramidDarkArmorMaterial);
            CreateBox(root, "Silo_Ramp_Base", new Vector3(0.45f, 0.62f, -0.55f), Quaternion.identity, new Vector3(1.9f, 0.5f, 1.4f), pyramidDarkArmorMaterial);
            for (int i = 0; i < 2; i++)
            {
                float x = 0.05f + i * 0.85f;
                CreateBox(root, "Silo_Launch_Rail_" + i, new Vector3(x, 1.35f, 0.25f), Quaternion.Euler(-38f, 0f, 0f), new Vector3(0.14f, 0.14f, 3.4f), goldenTurretMaterial);
                CreateCylinder(root, "Silo_Ready_Missile_" + i, new Vector3(x, 1.52f, 0.32f), Quaternion.Euler(52f, 0f, 0f), new Vector3(0.22f, 1.15f, 0.22f), goldenTurretGlowMaterial);
            }
            CreateCylinder(root, "Silo_Fabricator_Drum", new Vector3(-1.35f, 1.62f, -0.75f), Quaternion.identity, new Vector3(0.55f, 0.35f, 0.55f), goldenTurretGlowMaterial);
            CreatePointLight(root, "Silo_Blue_Guidance_Light", new Vector3(0.45f, 2.3f, 1.4f), new Color(0.25f, 0.62f, 1f, 1f), 1.1f, 12f);
            CreatePointLight(root, "Silo_Gold_Work_Light", new Vector3(-1.35f, 2.1f, -0.75f), new Color(1f, 0.76f, 0.23f, 1f), 0.8f, 9f);
            return;
        }

        if (kind == StructureKind.Aerodrome)
        {
            CreateBox(root, "Aerodrome_Platform", new Vector3(0f, 0.25f, 0f), Quaternion.identity, new Vector3(4.2f, 0.3f, 3.4f), goldenTurretMaterial);
            CreateBox(root, "Aerodrome_Hangar", new Vector3(0f, 0.85f, -0.5f), Quaternion.identity, new Vector3(2.8f, 0.9f, 1.6f), pyramidDarkArmorMaterial);
            CreateBox(root, "Aerodrome_Tarmac", new Vector3(0f, 0.08f, 1.2f), Quaternion.identity, new Vector3(3.0f, 0.04f, 1.5f), runnerMaterial);
            for (int pad = -1; pad <= 1; pad += 2)
            {
                CreateBox(root, "Aerodrome_Launch_Pad_" + pad, new Vector3(pad * 1.25f, 0.14f, 1.25f), Quaternion.identity, new Vector3(0.9f, 0.06f, 1.3f), goldenTurretGlowMaterial, true);
                CreateBox(root, "Aerodrome_Pad_Mark_" + pad, new Vector3(pad * 1.25f, 0.2f, 1.25f), Quaternion.identity, new Vector3(0.12f, 0.03f, 0.8f), runnerMaterial, true);
                CreatePointLight(root, "Aerodrome_Pad_Light_" + pad, new Vector3(pad * 1.25f, 0.42f, 1.25f), new Color(0.25f, 0.82f, 1f, 1f), 0.32f, 5f);
            }
            CreateCylinder(root, "Aerodrome_Beacon", new Vector3(1.5f, 1.1f, 1.0f), Quaternion.identity, new Vector3(0.2f, 0.55f, 0.2f), goldenTurretGlowMaterial);
            CreatePointLight(root, "Aerodrome_Landing_Light", new Vector3(0f, 1.6f, 1.2f), new Color(0.3f, 1f, 0.3f, 1f), 0.7f, 12f);
            return;
        }

        CreateBox(root, "Gepard_Launcher_Platform", new Vector3(0f, 0.3f, 0f), Quaternion.identity, new Vector3(2.8f, 0.36f, 2.2f), goldenTurretMaterial);
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -0.55f : 0.55f;
            float z = i % 2 == 0 ? 0.35f : -0.35f;
            CreateCylinder(root, "Gepard_AI_Missile_Tube_" + i, new Vector3(x, 0.92f, z), Quaternion.Euler(68f, 0f, 0f), new Vector3(0.16f, 0.78f, 0.16f), pyramidDarkArmorMaterial);
        }
        CreatePointLight(root, "Gepard_Targeting_Light", new Vector3(0f, 1.35f, 0.85f), new Color(1f, 0.82f, 0.22f, 1f), 0.8f, 9f);
    }

    private Transform CreateMirrorDrone(Transform turretRoot, Vector3 position)
    {
        Transform mirror = new GameObject("MirrorBeam_Floating_Mirror").transform;
        mirror.position = position + Vector3.up * 7.2f + turretRoot.forward * 8f;
        CreateCylinder(mirror, "Mirror_Gold_Ring", Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.85f, 0.06f, 0.85f), goldenTurretMaterial);
        CreateBox(mirror, "Mirror_Cyan_Glass", Vector3.zero, Quaternion.identity, new Vector3(1.05f, 0.06f, 0.62f), goldenTurretGlowMaterial);
        SphereCollider collider = mirror.gameObject.AddComponent<SphereCollider>();
        collider.radius = 1.25f;
        return mirror;
    }

    private void UpdateGoldenStructures(float dt)
    {
        for (int i = goldenStructures.Count - 1; i >= 0; i--)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null || structure.health <= 0f)
            {
                if (structure != null && structure.transform != null)
                {
                    CreateStructureSalvageWreckage(structure);
                    Destroy(structure.transform.gameObject);
                }
                goldenStructures.RemoveAt(i);
                continue;
            }

            if (IsGoldenStructureDisabled(structure))
                continue;

            structure.fireTimer -= dt;
            structure.reloadTimer -= dt;
            if (structure.kind == StructureKind.ResourceDepot)
                UpdateResourceDepot(structure, dt);
            else if (structure.kind == StructureKind.Twin30mmTurret)
                UpdateTwin30Turret(structure);
            else if (structure.kind == StructureKind.MirrorBeamTurret)
                UpdateMirrorBeamTurret(structure, dt);
            else if (structure.kind == StructureKind.GepardAALauncher)
                UpdateGepardLauncher(structure);
            else if (structure.kind == StructureKind.AnubisStrikeLauncher)
                UpdateAnubisLauncher(structure);
            else if (structure.kind == StructureKind.Aerodrome)
                UpdateAerodrome(structure, dt);
            else if (structure.kind == StructureKind.CruiseMissileSilo)
                UpdateCruiseMissileSilo(structure);
        }
    }

    private void UpdateResourceDepot(GoldenStructure depot, float dt)
    {
        depot.cargoTimer -= dt;
        float total = depot.storedSand + depot.storedGold + depot.storedWind;
        if (total < 24f || depot.cargoTimer > 0f)
            return;

        depot.cargoTimer = 6f;
        if (depot.storedGold >= depot.storedSand && depot.storedGold >= depot.storedWind)
            LaunchCargoFlyer(depot, ResourceKind.Gold, Mathf.Min(35f, depot.storedGold));
        else if (depot.storedWind >= depot.storedSand)
            LaunchCargoFlyer(depot, ResourceKind.Wind, Mathf.Min(24f, depot.storedWind));
        else
            LaunchCargoFlyer(depot, ResourceKind.Sand, Mathf.Min(40f, depot.storedSand));
    }

    private void LaunchCargoFlyer(GoldenStructure depot, ResourceKind kind, float amount)
    {
        if (kind == ResourceKind.Gold)
            depot.storedGold -= amount;
        else if (kind == ResourceKind.Wind)
            depot.storedWind -= amount;
        else
            depot.storedSand -= amount;

        Transform flyer = new GameObject("Golden_Cargo_Flyer_" + kind).transform;
        flyer.position = depot.transform.position + Vector3.up * 6.5f;

        ResourceCargo cargo = new ResourceCargo();
        cargo.transform = flyer;
        cargo.kind = kind;
        cargo.amount = amount;
        cargo.target = battlePyramid.position + Vector3.up * 4f;
        cargo.speed = balanceProfile.cargoFlyerSpeed;
        resourceCargos.Add(cargo);
        ConfigureHeavyCargoFlyer(cargo);
        PlaySandRunnerSound(SandRunnerSound.HangarRelease, depot.transform.position, 0.42f);
        lastEvent = "Cargo flyer launched: " + Mathf.RoundToInt(amount) + " " + kind + ".";
    }

    private void UpdateResourceCargos(float dt)
    {
        for (int i = resourceCargos.Count - 1; i >= 0; i--)
        {
            ResourceCargo cargo = resourceCargos[i];
            if (cargo == null || cargo.transform == null)
            {
                ReleaseHeavyCargoFlyer(cargo);
                resourceCargos.RemoveAt(i);
                continue;
            }

            cargo.target = battlePyramid.position + Vector3.up * 4f;
            Vector3 toTarget = cargo.target - cargo.transform.position;
            if (toTarget.magnitude <= 2.2f)
            {
                AddResource(cargo.kind, cargo.amount);
                ReleaseHeavyCargoFlyer(cargo);
                Destroy(cargo.transform.gameObject);
                resourceCargos.RemoveAt(i);
                PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, battlePyramid != null ? battlePyramid.position : cargo.target, 0.68f);
                lastEvent = "Cargo flyer delivered " + Mathf.RoundToInt(cargo.amount) + " " + cargo.kind + " to the pyramid.";
                continue;
            }

            cargo.transform.position += toTarget.normalized * cargo.speed * dt;
            cargo.transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            UpdateHeavyCargoFlyer(cargo, dt);
        }
    }

    private void UpdateTwin30Turret(GoldenStructure structure)
    {
        EnemyUnit target = FindNearestEnemy(structure.transform.position, 92f);
        if (target == null || target.transform == null || structure.fireTimer > 0f)
            return;

        structure.fireTimer = 0.16f;
        target.health -= 13f;
        TrackCombatTarget(target, "30-MM TURRET", 3.2f);
        Vector3 start = structure.transform.position + Vector3.up * 1.2f + structure.transform.forward * 1.1f;
        CreateWeaponTracer(start, target.transform.position + Vector3.up * 0.8f, new Color(1f, 0.74f, 0.18f, 1f), 0.045f, 0.12f);
        CreateWeaponFlash(start, 0.18f, new Color(1f, 0.7f, 0.18f, 1f));
    }

    private void UpdateMirrorBeamTurret(GoldenStructure structure, float dt)
    {
        if (structure.mirror == null || structure.mirrorHealth <= 0f)
            return;

        structure.mirror.Rotate(Vector3.up, 45f * dt, Space.World);
        EnemyUnit target = FindNearestEnemy(structure.mirror.position, 118f);
        if (target == null || target.transform == null || structure.fireTimer > 0f)
            return;

        structure.fireTimer = 0.7f;
        target.health -= 42f;
        TrackCombatTarget(target, "MIRROR BEAM TARGET", 4.2f);
        CreateBeam(structure.transform.position + Vector3.up * 1.65f, structure.mirror.position, new Color(1f, 0.92f, 0.22f, 1f), 0.09f, 0.18f);
        CreateBeam(structure.mirror.position, target.transform.position + Vector3.up * 1.1f, new Color(1f, 0.92f, 0.22f, 1f), 0.12f, 0.2f);
    }

    private void UpdateGepardLauncher(GoldenStructure structure)
    {
        if (structure.reloadTimer <= 0f && structure.ammo < 4)
        {
            structure.ammo++;
            structure.reloadTimer = 4.5f;
        }

        EnemyUnit target = FindNearestEnemy(structure.transform.position, 155f);
        if (target == null || target.transform == null || structure.fireTimer > 0f || structure.ammo <= 0)
            return;

        structure.fireTimer = 1.4f;
        structure.ammo--;
        TrackCombatTarget(target, "GEPARD LOCK", 4.2f);
        FireGepardMissile(structure.transform.position + Vector3.up * 1.25f, target.transform);
    }

    private void FireGepardMissile(Vector3 start, Transform target)
    {
        GameObject missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missileObject.name = "Gepard_AI_AA_Missile";
        missileObject.transform.position = start;
        missileObject.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
        Collider collider = missileObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = missileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = missileMaterial;
        AttachProjectileTrail(missileObject, new Color(1f, 0.82f, 0.18f, 1f), 0.12f, 0.42f);

        MissileVisual missile = new MissileVisual();
        missile.transform = missileObject.transform;
        missile.target = target;
        missile.fallbackTarget = target != null ? target.position : start + Vector3.forward * 20f;
        missile.speed = 34f;
        missile.turnRate = 11f;
        missile.life = 8f;
        missile.damage = 85f;
        missile.blastRadius = 7f;
        missiles.Add(missile);
    }

    private void UpdateAnubisLauncher(GoldenStructure structure)
    {
        if (structure.reloadTimer <= 0f && structure.ammo < 3)
        {
            structure.ammo++;
            structure.reloadTimer = 5f;
        }

        // Missile stock is reserved for the player's ANUBIS TV button.
        // The launcher no longer spends its limited ammunition automatically.
    }

    private void FireAnubisMissile(Vector3 start, Transform target)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = "Anubis_Strike_Missile";
        obj.transform.position = start;
        obj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = missileMaterial;
        AttachProjectileTrail(obj, new Color(1f, 0.55f, 0.05f, 1f), 0.18f, 0.55f);

        MissileVisual missile = new MissileVisual();
        missile.transform = obj.transform;
        missile.target = target;
        missile.fallbackTarget = target != null ? target.position : start + Vector3.forward * 50f;
        missile.speed = 28f;
        missile.turnRate = 4.5f;
        missile.life = 10f;
        missile.damage = 145f;
        missile.blastRadius = 12f;
        missiles.Add(missile);

        CreateWeaponFlash(start, 0.5f, new Color(1f, 0.6f, 0.1f, 1f));
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, start, 0.7f);
    }

    private void UpdateCruiseMissileSilo(GoldenStructure structure)
    {
        if (structure.reloadTimer > 0f)
            return;

        structure.reloadTimer = 26f;
        structure.ammo++;
        Vector3 drumPoint = structure.transform.TransformPoint(new Vector3(-1.35f, 1.9f, -0.75f));
        if (structure.ammo % 4 == 0 && nuclearMissiles < 4)
        {
            nuclearMissiles++;
            CreateWeaponFlash(drumPoint, 0.55f, new Color(1f, 0.3f, 0.08f, 1f));
            lastEvent = "Silo fabricated a sun-core missile. Stock " + nuclearMissiles + "/4.";
        }
        else if (cruiseMissiles < 12)
        {
            cruiseMissiles++;
            CreateWeaponFlash(drumPoint, 0.4f, new Color(0.25f, 0.62f, 1f, 1f));
            lastEvent = "Silo fabricated a cruise missile. Stock " + cruiseMissiles + "/12.";
        }
    }

    private GoldenStructure FindCruiseMissileSilo()
    {
        Vector3 origin = battlePyramid != null ? battlePyramid.position : Vector3.zero;
        return FindNearestStructure(origin, StructureKind.CruiseMissileSilo, float.MaxValue);
    }

    private Vector3 GetCruiseLaunchPoint(bool nuclear)
    {
        GoldenStructure silo = FindCruiseMissileSilo();
        if (silo != null && silo.transform != null)
            return silo.transform.TransformPoint(new Vector3(nuclear ? 0.9f : 0.05f, 1.9f, 0.5f));
        return GetWeaponPoint(missileLaunchers, Random.Range(0, Mathf.Max(1, missileLaunchers.Count)), new Vector3(nuclear ? 2.8f : -2.8f, 7.8f, -2.5f));
    }

    private void UpdateAerodrome(GoldenStructure structure, float dt)
    {
        structure.cargoTimer -= dt;
        if (structure.cargoTimer <= 0f)
        {
            structure.cargoTimer = 3f;
            for (int i = 0; i < unitSquads.Count; i++)
            {
                UnitSquad squad = unitSquads[i];
                if (squad == null || squad.units == null || !squad.airborne) continue;

                for (int j = 0; j < squad.units.Count; j++)
                {
                    RunnerUnit unit = squad.units[j];
                    if (unit == null || unit.transform == null) continue;
                    float dist = Vector3.Distance(unit.transform.position, structure.transform.position);
                    if (dist < 26f && unit.health < unit.maxHealth)
                    {
                        unit.health = Mathf.Min(unit.maxHealth, unit.health + 10f);
                        CreateBeam(structure.transform.position + Vector3.up * 1.6f, unit.transform.position, new Color(0.25f, 0.62f, 1f, 1f), 0.03f, 0.22f);
                    }
                }
            }
        }

        if (structure.reloadTimer <= 0f && structure.ammo < 2)
        {
            structure.ammo++;
            structure.reloadTimer = 8f;
        }

        EnemyUnit intruder = FindNearestEnemy(structure.transform.position, 85f);
        if (intruder == null || intruder.transform == null || structure.fireTimer > 0f || structure.ammo <= 0)
            return;

        structure.fireTimer = 2.4f;
        structure.ammo--;
        TrackCombatTarget(intruder, "AERODROME SCRAMBLE", 4.2f);
        Vector3 tarmac = structure.transform.TransformPoint(new Vector3(0f, 0.6f, 1.2f));
        FireInterceptorDrone(tarmac, intruder.transform);
        FireInterceptorDrone(tarmac + structure.transform.right * 0.9f, intruder.transform);
    }

    private void FireInterceptorDrone(Vector3 start, Transform target)
    {
        GameObject droneObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        droneObject.name = "Aerodrome_Interceptor_Drone";
        droneObject.transform.position = start;
        droneObject.transform.localScale = new Vector3(0.16f, 0.5f, 0.16f);
        Collider collider = droneObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = droneObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = runnerMaterial;
        AttachProjectileTrail(droneObject, new Color(0.35f, 0.68f, 1f, 1f), 0.1f, 0.4f);

        MissileVisual drone = new MissileVisual();
        drone.transform = droneObject.transform;
        drone.target = target;
        drone.fallbackTarget = target != null ? target.position : start + Vector3.forward * 25f;
        drone.velocity = Vector3.up * 14f;
        drone.speed = 30f;
        drone.turnRate = 8f;
        drone.life = 7f;
        drone.damage = 55f;
        drone.blastRadius = 5f;
        missiles.Add(drone);
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, start, 0.5f);
    }

    private GoldenStructure FindNearestAerodrome(Vector3 origin)
    {
        return FindNearestStructure(origin, StructureKind.Aerodrome, float.MaxValue);
    }

    private int CountAerodromes()
    {
        int count = 0;
        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure != null && structure.transform != null && structure.kind == StructureKind.Aerodrome)
                count++;
        }
        return count;
    }

    private void CreateReadableHit(Vector3 start, Vector3 end, bool heavy)
    {
        if (heavy)
        {
            CreateWeaponTracer(start, end, new Color(1f, 0.74f, 0.2f, 1f), 0.09f, 0.22f);
            CreateBattleExplosionFx(end, 5f, false, false);
            PlaySandRunnerSound(SandRunnerSound.HeavyAutocannon, end, 0.6f);
        }
        else
        {
            CreateWeaponTracer(start, end, new Color(1f, 0.72f, 0.18f, 1f), 0.05f, 0.15f);
            CreateBeamImpactFx(start, end, new Color(1f, 0.72f, 0.18f, 1f), 0.06f);
            PlaySandRunnerSound(SandRunnerSound.BulletImpact, end, 0.45f);
        }
    }

    private GoldenStructure FindNearestStructure(Vector3 origin, StructureKind kind, float range)
    {
        GoldenStructure best = null;
        float bestDistance = range;
        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null || structure.kind != kind)
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

    private GoldenStructure FindStructureByHit(Transform hit)
    {
        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure != null && structure.transform != null && IsTransformInHierarchy(hit, structure.transform))
                return structure;
            if (structure != null && structure.mirror != null && IsTransformInHierarchy(hit, structure.mirror))
                return structure;
        }
        return null;
    }

    private BlueprintSite FindBlueprintByHit(Transform hit)
    {
        for (int i = 0; i < blueprintSites.Count; i++)
        {
            BlueprintSite site = blueprintSites[i];
            if (site != null && site.transform != null && IsTransformInHierarchy(hit, site.transform))
                return site;
        }
        return null;
    }

    private string GetStructureName(StructureKind kind)
    {
        if (kind == StructureKind.ResourceDepot)
            return "Resource Depot";
        if (kind == StructureKind.Twin30mmTurret)
            return "Twin 30-mm Turret";
        if (kind == StructureKind.MirrorBeamTurret)
            return "Mirror Beam Turret";
        if (kind == StructureKind.GepardAALauncher)
            return "Gepard AA Launcher";
        if (kind == StructureKind.AnubisStrikeLauncher)
            return "Anubis Strike Launcher";
        if (kind == StructureKind.CruiseMissileSilo)
            return "Cruise Missile Silo";
        return "Aerodrome";
    }

    private void GetStructureCost(StructureKind kind, out float sandCost, out float goldCost, out float windCost)
    {
        SandRunnersResourcePrice price = GetReleaseStructurePrice(kind);
        sandCost = price.sand;
        goldCost = price.gold;
        windCost = price.wind;
    }

    private float GetStructureBuildTime(StructureKind kind)
    {
        return GetReleaseStructureBuildTime(kind);
    }

    private float GetStructureHealth(StructureKind kind)
    {
        if (kind == StructureKind.ResourceDepot)
            return 420f;
        if (kind == StructureKind.Twin30mmTurret)
            return 340f;
        if (kind == StructureKind.MirrorBeamTurret)
            return 380f;
        if (kind == StructureKind.AnubisStrikeLauncher)
            return 440f;
        if (kind == StructureKind.Aerodrome)
            return 500f;
        if (kind == StructureKind.CruiseMissileSilo)
            return 460f;
        return 360f;
    }

    private Vector3 GetStructureFootprint(StructureKind kind)
    {
        if (kind == StructureKind.ResourceDepot)
            return new Vector3(4.5f, 0.18f, 3.6f);
        if (kind == StructureKind.GepardAALauncher)
            return new Vector3(3.6f, 0.18f, 3.2f);
        if (kind == StructureKind.AnubisStrikeLauncher)
            return new Vector3(3.8f, 0.18f, 3.6f);
        if (kind == StructureKind.Aerodrome)
            return new Vector3(5.0f, 0.18f, 4.2f);
        if (kind == StructureKind.CruiseMissileSilo)
            return new Vector3(4.4f, 0.18f, 3.4f);
        return new Vector3(3.2f, 0.18f, 3.2f);
    }

    private bool TryFocusRTSSelectionCamera()
    {
        Transform focus = selectedStructure != null ? selectedStructure.transform : selectedBlueprint != null ? selectedBlueprint.transform : null;
        if (focus == null && selectedSquads.Count > 0)
            focus = GetSquadRepresentative(selectedSquads[0]);
        if (focus == null || battlePyramid == null)
            return false;

        Vector3 focusPoint = GetRTSCameraFollowPoint(focus);
        Vector3 offset = focusPoint - battlePyramid.position;
        cameraYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        cameraDistance = Mathf.Clamp(offset.magnitude + 20f, 28f, 86f);
        cameraPitch = 25f;
        lastEvent = "Camera focused on " + (selectedStrategicName ?? focus.name) + ".";
        return true;
    }

    private bool TryStartRTSCameraFollow()
    {
        Transform focus = GetRTSCameraFollowCandidate();
        if (focus == null)
            return false;

        cameraFollowSelectionMode = true;
        cameraFollowTarget = focus;
        cameraFollowLabel = GetRTSCameraFollowLabel(focus);
        cameraFollowInitialized = false;
        cameraFollowSpeedBlend = 0f;
        cameraFollowManualOrbitTimer = 1.2f;
        cameraPitch = Mathf.Clamp(cameraPitch, 14f, 34f);
        GetRTSCameraFollowZoomRange(focus, out float minDistance, out float maxDistance);
        cameraDistance = Mathf.Clamp(GetRTSCameraFollowStartDistance(focus), minDistance, maxDistance);
        Vector3 focusPoint = GetRTSCameraFollowPoint(focus);

        if (mainCamera != null)
        {
            Vector3 offset = focusPoint - mainCamera.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 1f)
                cameraYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }
        else if (battlePyramid != null)
        {
            Vector3 offset = focusPoint - battlePyramid.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 1f)
                cameraYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        lastEvent = "Following: " + cameraFollowLabel + " | scroll zoom | RMB drag orbit | C release.";
        return true;
    }

    private void StopRTSCameraFollow(string message)
    {
        cameraFollowSelectionMode = false;
        cameraFollowTarget = null;
        cameraFollowLabel = null;
        cameraFollowInitialized = false;
        cameraFollowSpeedBlend = 0f;
        cameraFollowManualOrbitTimer = 0f;
        if (cameraDistance < 32f)
        {
            cameraDistance = 46f;
        }
        if (!string.IsNullOrEmpty(message))
            lastEvent = message;
    }

    private bool TryGetRTSCameraFollowTarget(out Transform target, out string label)
    {
        target = null;
        label = null;
        if (!cameraFollowSelectionMode)
            return false;

        target = GetRTSCameraFollowCandidate();
        if (target == null)
        {
            StopRTSCameraFollow("Camera follow lost its target.");
            return false;
        }

        cameraFollowTarget = target;
        cameraFollowLabel = GetRTSCameraFollowLabel(target);
        label = cameraFollowLabel;
        return true;
    }

    private Transform GetRTSCameraFollowCandidate()
    {
        if (selectedSquads.Count > 0)
            return GetSquadRepresentative(selectedSquads[0]);
        if (selectedStructure != null)
            return selectedStructure.transform;
        if (selectedBlueprint != null)
            return selectedBlueprint.transform;
        if (selectedStrategicTransform != null)
            return selectedStrategicTransform;
        return cameraFollowTarget;
    }

    private string GetRTSCameraFollowLabel(Transform target)
    {
        if (!string.IsNullOrEmpty(selectedStrategicName))
            return selectedStrategicName;
        if (selectedSquads.Count > 0 && !string.IsNullOrEmpty(selectedSquads[0].displayName))
            return selectedSquads[0].displayName;
        return target != null ? target.name.Replace('_', ' ') : "selection";
    }

    private void PruneRTSSelections()
    {
        for (int i = unitSquads.Count - 1; i >= 0; i--)
        {
            UnitSquad squad = unitSquads[i];
            if (squad == null)
            {
                unitSquads.RemoveAt(i);
                continue;
            }
            for (int u = squad.units.Count - 1; u >= 0; u--)
            {
                if (squad.units[u] == null || squad.units[u].transform == null || squad.units[u].health <= 0f)
                    squad.units.RemoveAt(u);
            }
            if (squad.builder != null && (squad.builder.transform == null || squad.builder.health <= 0f))
                squad.builder = null;
            if (squad.units.Count == 0 && squad.builder == null)
                unitSquads.RemoveAt(i);
        }

        for (int i = selectedSquads.Count - 1; i >= 0; i--)
        {
            if (!unitSquads.Contains(selectedSquads[i]))
                selectedSquads.RemoveAt(i);
        }
    }

    private string GetProductionSummary()
    {
        string text = "Queue ";
        if (productionQueue.Count == 0)
            text += "empty";
        else
        {
            ProductionJob first = productionQueue[0];
            text += first.label + " " + Mathf.RoundToInt(first.progress / Mathf.Max(0.1f, first.buildTime) * 100f) + "%";
            if (productionQueue.Count > 1)
                text += " +" + (productionQueue.Count - 1);
        }
        text += " | Ready hangar " + readyHangarJobs.Count;
        if (pendingStructureBlueprint.HasValue)
            text += " | Placing " + GetStructureName(pendingStructureBlueprint.Value);
        return text;
    }

    private bool TryWriteRTSSelectionCanvas()
    {
        if (touchOfHorus != null && selectedStrategicTransform == touchOfHorus.root)
        {
            strategicSelectedTitleText.text = "Touch of Horus";
            strategicSelectedStatsText.text =
                "Royal repair and diplomacy spirit\n" +
                "Active streams " + touchOfHorus.activeStreams + " | Solar dust " + Mathf.CeilToInt(touchOfHorus.solarDust) + "/" + Mathf.CeilToInt(touchOfHorus.maxSolarDust) + "\n" +
                "RMB settlement: diplomatic consent | click ground: move Horus";
            return true;
        }

        if (selectedSquads.Count == 0 && selectedStructure == null && selectedBlueprint == null)
            return false;

        if (selectedSquads.Count > 0)
        {
            int units = 0;
            float health = 0f;
            float maxHealth = 0f;
            bool auto = true;
            for (int i = 0; i < selectedSquads.Count; i++)
            {
                UnitSquad squad = selectedSquads[i];
                units += squad.units.Count + (squad.builder != null ? 1 : 0);
                auto = auto && squad.autoEngage;
                for (int u = 0; u < squad.units.Count; u++)
                {
                    health += squad.units[u].health;
                    maxHealth += squad.units[u].maxHealth;
                }
                if (squad.builder != null)
                {
                    health += squad.builder.health;
                    maxHealth += squad.builder.maxHealth;
                }
            }
            string doctrine = selectedSquads.Count == 1
                ? GetSquadDoctrineLabel(selectedSquads[0].doctrine)
                : "MIXED";
            strategicSelectedTitleText.text = selectedSquads.Count == 1 ? selectedSquads[0].displayName : selectedSquads.Count + " squads selected";
            strategicSelectedStatsText.text = "Units " + units + " | Hull " + Mathf.RoundToInt(health) + "/" + Mathf.RoundToInt(maxHealth) + "\n" +
                "Doctrine " + doctrine + " | Auto " + (auto ? "ON" : "OFF") + " (A)";
            return true;
        }

        if (selectedStructure != null)
        {
            strategicSelectedTitleText.text = selectedStructure.displayName;
            strategicSelectedStatsText.text = "Hull " + Mathf.RoundToInt(selectedStructure.health) + "/" + Mathf.RoundToInt(selectedStructure.maxHealth) + "\n" +
                (selectedStructure.kind == StructureKind.MirrorBeamTurret ? "RMB moves floating mirror kill-zone" : "Static Golden Elemental structure");
            return true;
        }

        strategicSelectedTitleText.text = selectedBlueprint.displayName + " blueprint";
        strategicSelectedStatsText.text = "Build " + Mathf.RoundToInt(selectedBlueprint.progress / Mathf.Max(0.1f, selectedBlueprint.buildTime) * 100f) + "%\nRMB with builders to add crews";
        return true;
    }

    private void ToggleSelectedSquadAutomation()
    {
        if (selectedSquads.Count == 0)
            return;

        bool newValue = !selectedSquads[0].autoEngage;
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            selectedSquads[i].autoEngage = newValue;
            selectedSquads[i].doctrineTarget = null;
            selectedSquads[i].nextDoctrineScanTime = 0f;
        }
        lastEvent = "Selected squad automation " + (newValue ? "enabled." : "disabled.");
    }

    private void DrawRTSOverlayGUI()
    {
        if (hudHidden && !rtsDraggingSelection)
            return;

        if (rtsDraggingSelection)
        {
            Rect rect = MakeGuiRect(rtsDragStart, rtsDragEnd);
            GUI.color = new Color(0.2f, 1f, 0.3f, 0.18f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 2f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.yMin, 2f, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        DrawSelectedHealthBars();
    }

    private Rect MakeGuiRect(Vector2 a, Vector2 b)
    {
        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minY = Mathf.Min(Screen.height - a.y, Screen.height - b.y);
        float maxY = Mathf.Max(Screen.height - a.y, Screen.height - b.y);
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private void DrawSelectedHealthBars()
    {
        if (mainCamera == null)
            return;

        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            Transform rep = GetSquadRepresentative(squad);
            if (rep == null)
                continue;
            float health;
            float maxHealth;
            GetSquadHealth(squad, out health, out maxHealth);
            DrawWorldHealthBar(rep.position + Vector3.up * 2.8f, health / Mathf.Max(1f, maxHealth), squad.displayName);
        }

        if (selectedStructure != null && selectedStructure.transform != null)
            DrawWorldHealthBar(selectedStructure.transform.position + Vector3.up * 3.2f, selectedStructure.health / selectedStructure.maxHealth, selectedStructure.displayName);
        if (selectedBlueprint != null && selectedBlueprint.transform != null)
            DrawWorldHealthBar(selectedBlueprint.transform.position + Vector3.up * 2.2f, selectedBlueprint.progress / Mathf.Max(1f, selectedBlueprint.buildTime), selectedBlueprint.displayName);
        DrawSelectedAttackTargetMarkers();
        DrawTrackedCombatTargets();
    }

    private void GetSquadHealth(UnitSquad squad, out float health, out float maxHealth)
    {
        health = 0f;
        maxHealth = 0f;
        for (int i = 0; i < squad.units.Count; i++)
        {
            health += squad.units[i].health;
            maxHealth += squad.units[i].maxHealth;
        }
        if (squad.builder != null)
        {
            health += squad.builder.health;
            maxHealth += squad.builder.maxHealth;
        }
    }

    private void DrawWorldHealthBar(Vector3 world, float ratio, string label)
    {
        DrawReadableWorldMarker(world, ratio, label, new Color(0.25f, 1f, 0.32f, 0.9f), true);
    }
}
