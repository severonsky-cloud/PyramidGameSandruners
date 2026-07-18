using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SandRunnersResourcePrice
{
    public float sand;
    public float gold;
    public float wind;

    public SandRunnersResourcePrice(float sandValue, float goldValue, float windValue)
    {
        sand = sandValue;
        gold = goldValue;
        wind = windValue;
    }
}

[Serializable]
public sealed class SandRunnersBalanceProfile
{
    [Header("Economy")]
    public float sandNodeIncome = 0.95f;
    public float goldNodeIncome = 0.72f;
    public float windNodeIncome = 0.34f;
    public float builderGatherSeconds = 4.8f;
    public float builderCargoNeutral = 22f;
    public float builderCargoControlled = 34f;
    public float windCargoMultiplier = 0.42f;

    [Header("Resource development")]
    public float resourceCapturePerDeveloper = 0.16f;
    public float resourceDevelopmentTierMultiplier = 1.72f;
    public float mineIncomePerLevel = 0.5f;
    public float mineConvoyBaseSeconds = 36f;
    public float mineConvoyTierReduction = 6f;
    public int mineConvoyLimit = 8;
    public float mineConvoyEscortRange = 72f;
    public float mineConvoyEscortDamage = 9f;
    public float cargoFlyerSpeed = 21f;
    public float cargoFlyerDefenseRange = 84f;
    public float cargoFlyerDefenseDamage = 18f;
    public float resourceFortBaseRange = 48f;
    public float nanoTreeBaseRepairRadius = 36f;
    public float nanoTreeSolarStormSeconds = 32f;

    [Header("Production")]
    public float productionTimeScale = 1.35f;
    public SandRunnersResourcePrice vimana = new SandRunnersResourcePrice(36f, 48f, 0f);
    public SandRunnersResourcePrice combatFlyer = new SandRunnersResourcePrice(110f, 140f, 22f);
    public SandRunnersResourcePrice scarabTank = new SandRunnersResourcePrice(120f, 130f, 16f);
    public SandRunnersResourcePrice builderTruck = new SandRunnersResourcePrice(48f, 45f, 5f);
    public SandRunnersResourcePrice abydosFlyer = new SandRunnersResourcePrice(100f, 145f, 26f);
    public SandRunnersResourcePrice heavyFlyer = new SandRunnersResourcePrice(160f, 185f, 30f);
    public SandRunnersResourcePrice wrathOfRa = new SandRunnersResourcePrice(250f, 280f, 32f);
    public SandRunnersResourcePrice fortressCrusher = new SandRunnersResourcePrice(290f, 310f, 28f);
    public SandRunnersResourcePrice thothEmbrace = new SandRunnersResourcePrice(460f, 490f, 60f);
    public SandRunnersResourcePrice sandSkimmer = new SandRunnersResourcePrice(38f, 40f, 4f);
    public SandRunnersResourcePrice siegeScarab = new SandRunnersResourcePrice(72f, 95f, 8f);
    public SandRunnersResourcePrice salvageScarab = new SandRunnersResourcePrice(55f, 65f, 4f);

    [Header("Salvage")]
    public SandRunnersResourcePrice hostileVehicleSalvage = new SandRunnersResourcePrice(8f, 5f, 0f);
    public SandRunnersResourcePrice juzzherBargeSalvage = new SandRunnersResourcePrice(16f, 10f, 2f);
    public SandRunnersResourcePrice structureSalvage = new SandRunnersResourcePrice(14f, 8f, 0f);
    public SandRunnersResourcePrice fortressSalvage = new SandRunnersResourcePrice(18f, 18f, 4f);
    public float salvagePickupSeconds = 0.8f;
    public float salvageCarryHeight = 1.65f;

    [Header("Structures")]
    public SandRunnersResourcePrice resourceDepot = new SandRunnersResourcePrice(70f, 45f, 4f);
    public SandRunnersResourcePrice twin30 = new SandRunnersResourcePrice(82f, 65f, 5f);
    public SandRunnersResourcePrice mirrorBeam = new SandRunnersResourcePrice(120f, 120f, 12f);
    public SandRunnersResourcePrice gepard = new SandRunnersResourcePrice(150f, 140f, 16f);
    public SandRunnersResourcePrice anubis = new SandRunnersResourcePrice(180f, 175f, 20f);
    public SandRunnersResourcePrice aerodrome = new SandRunnersResourcePrice(200f, 160f, 28f);
    public SandRunnersResourcePrice missileSilo = new SandRunnersResourcePrice(170f, 190f, 18f);
    public float aerodromeBuildSeconds = 34f;

    [Header("Aviation")]
    public float interceptorEndurance = 110f;
    public float strikeEndurance = 92f;
    public float heavyEndurance = 78f;
    public float carrierEndurance = 130f;
    public float aircraftServiceSeconds = 7f;
    public float emergencyReturnHealth = 0.32f;
    public int interceptorAmmo = 5;
    public int strikeAmmo = 4;
    public int heavyAmmo = 3;
    public int carrierAmmo = 4;

    [Header("Living World")]
    public int raidQuotaMin = 2;
    public int raidQuotaMax = 5;
    public float raidIntervalMin = 42f;
    public float raidIntervalMax = 65f;
    public float caravanSpawnSeconds = 45f;
    public float mirrorGridRepairGold = 25f;
    public float mirrorGridRepairWind = 15f;

    [Header("Mandarinka strategic AI")]
    public float aiThreatScanInterval = 0.25f;
    public float aiPlanInterval = 2f;
    public float aiThreatClusterRadius = 36f;
    public float aiRallySeconds = 6f;
    public float navigationStuckSeconds = 4f;
    public float navigationMinimumProgress = 2f;
    public int navigationRecoveryAttempts = 3;

    [Header("Mandarinka territory and occupation")]
    public float mandarinkaResourceCaptureSeconds = 70f;
    public float mandarinkaOccupationCaptureSeconds = 90f;
    public float mandarinkaOccupationFortifySeconds = 180f;
    public float mandarinkaOccupationExhaustSeconds = 360f;
    public float mandarinkaSandExtractionPerSecond = 0.52f;
    public float mandarinkaGoldExtractionPerSecond = 0.44f;
    public float mandarinkaWindExtractionPerSecond = 0.3f;
    public float mandarinkaSandCastleThreshold = 120f;
    public float mandarinkaGoldCastleThreshold = 100f;
    public float mandarinkaWindCastleThreshold = 80f;
    public float mandarinkaFortressInfluenceRadius = 190f;
    public float mandarinkaOutpostInfluenceRadius = 105f;
    public float mandarinkaOccupationInfluenceRadius = 135f;
    public float mandarinkaOutpostTierTwoSeconds = 45f;
    public float mandarinkaOutpostTierThreeSeconds = 140f;
    public float mandarinkaConvoyIntervalSeconds = 75f;
    public float mandarinkaConvoyGroundSpeed = 15f;
    public float mandarinkaConvoyAirSpeed = 24f;
    public int mandarinkaOutpostGarrisonLimit = 4;

    [Header("Imperial retaliation")]
    public float imperialFirstStrikeSeconds = 1800f;
    public float imperialRepeatStrikeSeconds = 360f;
    public float imperialMissileFlightSeconds = 25f;
    public int imperialGroundUnitLimit = 18;
    public int imperialAirUnitLimit = 10;
    public int imperialEngineerLimit = 4;

    [Header("Diplomacy contracts")]
    public SandRunnersResourcePrice elementalWalkerContract = new SandRunnersResourcePrice(90f, 240f, 36f);
    public SandRunnersResourcePrice grounderGradContract = new SandRunnersResourcePrice(230f, 150f, 18f);
    public float factionContractRestockSeconds = 360f;
    public float diplomacyTimeScale = 0.15f;

    [Header("Visible milestone rewards")]
    public SandRunnersResourcePrice aerodromeMilestone = new SandRunnersResourcePrice(35f, 25f, 0f);
    public SandRunnersResourcePrice firstSortieMilestone = new SandRunnersResourcePrice(0f, 45f, 10f);
    public SandRunnersResourcePrice tradeMilestone = new SandRunnersResourcePrice(30f, 25f, 0f);
    public SandRunnersResourcePrice powerMilestone = new SandRunnersResourcePrice(0f, 45f, 35f);
    public SandRunnersResourcePrice thothMilestone = new SandRunnersResourcePrice(120f, 140f, 25f);
    public SandRunnersResourcePrice raidMilestone = new SandRunnersResourcePrice(80f, 90f, 20f);
    public SandRunnersResourcePrice assaultMilestone = new SandRunnersResourcePrice(100f, 120f, 30f);

    [Header("Mission pacing per stage")]
    public float[] stageTargetSeconds = { 150f, 180f, 180f, 180f, 240f, 300f, 360f, 330f };
    public float recoveryDelayMultiplier = 1.35f;

    public float GetStageTarget(int stage)
    {
        if (stageTargetSeconds == null || stageTargetSeconds.Length == 0)
            return 240f;
        return stageTargetSeconds[Mathf.Clamp(stage, 0, stageTargetSeconds.Length - 1)];
    }
}

[Serializable]
public sealed class PresentationBudget
{
    public int maxTracers = 56;
    public int maxParticleBursts = 18;
    public int maxDynamicLights = 18;
    public int maxImpactMarkers = 20;
    public int maxCombatLabels = 7;
    public int maxScorches = 16;
    public int maxWorldEvents = 3;
    public int maxSalvageFields = 24;
    public int maxSalvagePieces = 72;
    public int maxSalvageMarkers = 6;
    public int maxSalvageLights = 4;
    public float salvageUpdateInterval = 0.1f;
    public float salvageMergeDistance = 12f;
    public float effectCullDistance = 280f;
    public float detailCullDistance = 220f;
    public float lightCullDistance = 180f;
    public float largeEffectScale = 0.72f;
}

[Serializable]
public sealed class MissionPacingState
{
    public int stageIndex;
    public float totalElapsed;
    public float stageElapsed;
    public bool recoveryIssued;
    public bool[] tutorialShown = new bool[9];
}

public partial class SandRunnersPrototype
{
    private enum PresentationKind
    {
        Particle,
        Light,
        Marker,
        Label,
        Scorch
    }

    private sealed class PresentationLease
    {
        public GameObject gameObject;
        public string poolKey;
        public PresentationKind kind;
        public float remaining;
    }

    [SerializeField] private SandRunnersBalanceProfile balanceProfile = new SandRunnersBalanceProfile();
    [SerializeField] private PresentationBudget presentationBudget = new PresentationBudget();
    private MissionPacingState missionPacingState = new MissionPacingState();
    private readonly Dictionary<string, Stack<GameObject>> presentationPools = new Dictionary<string, Stack<GameObject>>();
    private readonly List<PresentationLease> presentationLeases = new List<PresentationLease>();
    private readonly int[] activePresentationCounts = new int[5];
    private bool releaseCandidateReady;
    private float releaseMasterVolume = 0.85f;
    private float releaseCameraShakeStrength = 0.72f;
    private float releaseSettingsSaveTimer;
    private Transform releaseRoadRoot;

    private void InitializeReleaseCandidate()
    {
        if (releaseCandidateReady)
            return;

        if (balanceProfile == null)
            balanceProfile = new SandRunnersBalanceProfile();
        if (presentationBudget == null)
            presentationBudget = new PresentationBudget();
        if (missionPacingState == null)
            missionPacingState = new MissionPacingState();
        if (missionPacingState.tutorialShown == null || missionPacingState.tutorialShown.Length < 9)
            missionPacingState.tutorialShown = new bool[9];

        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        LoadReleaseCandidateSettings();
        releaseCandidateReady = true;
    }

    private void UpdateReleaseCandidate(float dt)
    {
        if (!releaseCandidateReady)
            InitializeReleaseCandidate();

        UpdatePresentationPool(dt);
        UpdateMissionPacing(dt);
        releaseSettingsSaveTimer -= dt;
        ApplyReleaseCandidateSettings();
    }

    private void UpdateMissionPacing(float dt)
    {
        if (!verticalSliceMissionInitialized || verticalSliceStage == VerticalSliceStage.Complete)
            return;

        int stage = (int)verticalSliceStage;
        missionPacingState.totalElapsed += dt;
        if (missionPacingState.stageIndex != stage)
        {
            missionPacingState.stageIndex = stage;
            missionPacingState.stageElapsed = 0f;
            missionPacingState.recoveryIssued = false;
        }
        else
        {
            missionPacingState.stageElapsed += dt;
        }

        ShowReleaseCandidateTutorial(stage);
        TryResolveMissionDeadlock(stage);
    }

    private void ShowReleaseCandidateTutorial(int stage)
    {
        if (stage < 0 || stage >= missionPacingState.tutorialShown.Length || missionPacingState.tutorialShown[stage] || missionPacingState.stageElapsed < 4f)
            return;

        missionPacingState.tutorialShown[stage] = true;
        string tutorial;
        switch (stage)
        {
            case 0: tutorial = "TACTICAL: Press Tab for command mode, select the pyramid or a builder, then secure a marked extraction site."; break;
            case 1: tutorial = "ENGINEERING: Produce a Solar Builder Truck, place an Aerodrome blueprint, then RMB the builder onto it."; break;
            case 2: tutorial = "AIR COMMAND: Select the aerodrome before production to use its runway; the pyramid remains the reserve launch base."; break;
            case 3: tutorial = "BLUE ELEMENTALS: Approach a settlement and press T. Every honest deal strengthens its defenses."; break;
            case 4: tutorial = "MIRROR GRID: Control wind, approach a settlement and press Y to connect or repair its energy relays."; break;
            case 5: tutorial = "THOTH: Select the carrier to manage two Combat and two Heavy Flyer slots, launch aircraft, or recall them for service."; break;
            case 6: tutorial = "TRADE ROAD: Protect caravan cargo. Q/E enter the four howitzers; aviation can intercept raiders before contact."; break;
            default: tutorial = "FINAL ASSAULT: Break the jade shield, intercept siege shells, then combine Thoth, missiles and all four howitzers."; break;
        }
        SetMissionDialogue(tutorial, 8f);
        PlaySandRunnerSound(SandRunnerSound.UiSelection, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.42f);
    }

    private void TryResolveMissionDeadlock(int stage)
    {
        if (missionPacingState.recoveryIssued)
            return;
        float recoveryTime = balanceProfile.GetStageTarget(stage) * balanceProfile.recoveryDelayMultiplier;
        if (missionPacingState.stageElapsed < recoveryTime)
            return;

        bool issued = false;
        if (verticalSliceStage == VerticalSliceStage.BuildAerodrome && CountLivingBuilders() == 0)
        {
            EnsureMinimumResources(balanceProfile.builderTruck);
            BuildGoldenResourceBuilder();
            lastEvent = "Recovery convoy funded one replacement Solar Builder Truck.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.LaunchAirWing && !HasOperationalMissionAirWing() && productionQueue.Count == 0)
        {
            EnsureMinimumResources(balanceProfile.combatFlyer);
            BuildCombatFlyerSquad();
            lastEvent = "Pyramid reserve automatically queued a replacement Combat Flyer wing.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.TradeSettlement && tradeCaravans.Count == 0)
        {
            SpawnTradeCaravan();
            lastEvent = "Blue Traders reopened the route with a replacement caravan.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.PowerSettlement && !HasOnlineSettlementGrid())
        {
            gold = Mathf.Max(gold, 60f);
            wind = Mathf.Max(wind, 90f);
            lastEvent = "Settlement engineers marked the nearest viable mirror-grid route and supplied the missing relays.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.FieldThoth && !thothCarrierReady && CountRunnersContaining("Thoth's Embrace") == 0)
        {
            EnsureMinimumResources(balanceProfile.thothEmbrace);
            BuildThothEmbrace();
            lastEvent = "Alliance reserve completed the missing carrier funding and queued Thoth's Embrace.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.SurviveCaravanRaids && tradeCaravans.Count == 0)
        {
            SpawnTradeCaravan();
            mandarinkaCaravanRaidTimer = Mathf.Min(mandarinkaCaravanRaidTimer, 8f);
            lastEvent = "A replacement escorted caravan entered the trade road crisis.";
            issued = true;
        }
        else if (verticalSliceStage == VerticalSliceStage.DestroyMandarinka)
        {
            cruiseMissiles = Mathf.Max(cruiseMissiles, 3);
            lastEvent = "Final reserve unlocked three cruise missiles. The fortress remains the only objective.";
            issued = true;
        }

        if (issued)
        {
            missionPacingState.recoveryIssued = true;
            ShowBanner("MISSION RECOVERY PACKAGE DEPLOYED", 3f);
            SetMissionDialogue("PYRAMID SPIRIT: The route remains difficult, but it will not become impossible.", 7f);
        }
    }

    private int CountLivingBuilders()
    {
        int count = 0;
        for (int i = 0; i < goldenBuilders.Count; i++)
            if (goldenBuilders[i] != null && goldenBuilders[i].transform != null && goldenBuilders[i].health > 0f)
                count++;
        return count;
    }

    private void EnsureMinimumResources(SandRunnersResourcePrice price)
    {
        sand = Mathf.Max(sand, price.sand);
        gold = Mathf.Max(gold, price.gold);
        wind = Mathf.Max(wind, price.wind);
    }

    private SandRunnersResourcePrice GetReleaseMilestoneReward(VerticalSliceStage enteredStage)
    {
        switch (enteredStage)
        {
            case VerticalSliceStage.BuildAerodrome: return balanceProfile.aerodromeMilestone;
            case VerticalSliceStage.LaunchAirWing: return balanceProfile.firstSortieMilestone;
            case VerticalSliceStage.TradeSettlement: return balanceProfile.tradeMilestone;
            case VerticalSliceStage.PowerSettlement: return balanceProfile.powerMilestone;
            case VerticalSliceStage.FieldThoth: return balanceProfile.thothMilestone;
            case VerticalSliceStage.SurviveCaravanRaids: return balanceProfile.raidMilestone;
            case VerticalSliceStage.DestroyMandarinka: return balanceProfile.assaultMilestone;
            default: return new SandRunnersResourcePrice(0f, 0f, 0f);
        }
    }

    private SandRunnersResourcePrice GetReleaseProductionPrice(ProductionKind kind)
    {
        switch (kind)
        {
            case ProductionKind.CombatFlyer: return balanceProfile.combatFlyer;
            case ProductionKind.ScarabTank: return balanceProfile.scarabTank;
            case ProductionKind.BuilderTruck: return balanceProfile.builderTruck;
            case ProductionKind.AbydosFlyer: return balanceProfile.abydosFlyer;
            case ProductionKind.HeavyFlyer: return balanceProfile.heavyFlyer;
            case ProductionKind.WrathOfRa: return balanceProfile.wrathOfRa;
            case ProductionKind.FortressCrusher: return balanceProfile.fortressCrusher;
            case ProductionKind.ThothEmbrace: return balanceProfile.thothEmbrace;
            case ProductionKind.SandSkimmer: return balanceProfile.sandSkimmer;
            case ProductionKind.SiegeScarab: return balanceProfile.siegeScarab;
            case ProductionKind.SalvageScarab: return balanceProfile.salvageScarab;
            default: return balanceProfile.vimana;
        }
    }

    private SandRunnersResourcePrice GetReleaseStructurePrice(StructureKind kind)
    {
        switch (kind)
        {
            case StructureKind.Twin30mmTurret: return balanceProfile.twin30;
            case StructureKind.MirrorBeamTurret: return balanceProfile.mirrorBeam;
            case StructureKind.GepardAALauncher: return balanceProfile.gepard;
            case StructureKind.AnubisStrikeLauncher: return balanceProfile.anubis;
            case StructureKind.Aerodrome: return balanceProfile.aerodrome;
            case StructureKind.CruiseMissileSilo: return balanceProfile.missileSilo;
            default: return balanceProfile.resourceDepot;
        }
    }

    private float GetReleaseStructureBuildTime(StructureKind kind)
    {
        if (kind == StructureKind.Aerodrome)
            return balanceProfile.aerodromeBuildSeconds;
        if (kind == StructureKind.ResourceDepot)
            return 14f;
        if (kind == StructureKind.Twin30mmTurret)
            return 16f;
        if (kind == StructureKind.MirrorBeamTurret)
            return 21f;
        if (kind == StructureKind.AnubisStrikeLauncher)
            return 24f;
        if (kind == StructureKind.CruiseMissileSilo)
            return 27f;
        return 25f;
    }

    private string GetReleasePacingSuffix()
    {
        if (!releaseCandidateReady || verticalSliceStage == VerticalSliceStage.Complete)
            return string.Empty;
        int elapsed = Mathf.Max(0, Mathf.FloorToInt(missionPacingState.stageElapsed));
        int target = Mathf.Max(1, Mathf.RoundToInt(balanceProfile.GetStageTarget((int)verticalSliceStage)));
        return " | " + (elapsed / 60) + ":" + (elapsed % 60).ToString("00") + " / ~" + Mathf.CeilToInt(target / 60f) + "m";
    }

    private int GetPresentationLimit(PresentationKind kind)
    {
        switch (kind)
        {
            case PresentationKind.Particle: return presentationBudget.maxParticleBursts;
            case PresentationKind.Light: return presentationBudget.maxDynamicLights;
            case PresentationKind.Marker: return presentationBudget.maxImpactMarkers;
            case PresentationKind.Label: return presentationBudget.maxCombatLabels;
            default: return presentationBudget.maxScorches;
        }
    }

    private bool IsPresentationVisible(Vector3 position)
    {
        return mainCamera == null || Vector3.SqrMagnitude(mainCamera.transform.position - position) <= presentationBudget.effectCullDistance * presentationBudget.effectCullDistance;
    }

    private GameObject AcquirePresentationObject(string poolKey, PresentationKind kind, Vector3 position, float life, Func<GameObject> factory)
    {
        if (!IsPresentationVisible(position) || activePresentationCounts[(int)kind] >= GetPresentationLimit(kind))
            return null;

        Stack<GameObject> pool;
        if (!presentationPools.TryGetValue(poolKey, out pool))
        {
            pool = new Stack<GameObject>();
            presentationPools.Add(poolKey, pool);
        }

        GameObject result = null;
        while (pool.Count > 0 && result == null)
            result = pool.Pop();
        if (result == null)
            result = factory();
        result.transform.SetParent(null, true);
        result.transform.position = position;
        result.SetActive(true);
        activePresentationCounts[(int)kind]++;
        presentationLeases.Add(new PresentationLease { gameObject = result, poolKey = poolKey, kind = kind, remaining = Mathf.Max(0.05f, life) });
        return result;
    }

    private void UpdatePresentationPool(float dt)
    {
        for (int i = presentationLeases.Count - 1; i >= 0; i--)
        {
            PresentationLease lease = presentationLeases[i];
            lease.remaining -= dt;
            if (lease.remaining > 0f)
                continue;

            if (lease.gameObject != null)
            {
                ParticleSystem particles = lease.gameObject.GetComponent<ParticleSystem>();
                if (particles != null)
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                lease.gameObject.SetActive(false);
                Stack<GameObject> pool;
                if (!presentationPools.TryGetValue(lease.poolKey, out pool))
                {
                    pool = new Stack<GameObject>();
                    presentationPools.Add(lease.poolKey, pool);
                }
                pool.Push(lease.gameObject);
            }
            activePresentationCounts[(int)lease.kind] = Mathf.Max(0, activePresentationCounts[(int)lease.kind] - 1);
            presentationLeases.RemoveAt(i);
        }
    }

    private Light SpawnPooledPresentationLight(string key, Vector3 position, Color color, float intensity, float range, float life)
    {
        GameObject lightObject = AcquirePresentationObject(key, PresentationKind.Light, position, life, () =>
        {
            GameObject created = new GameObject(key);
            created.AddComponent<Light>().type = LightType.Point;
            return created;
        });
        if (lightObject == null)
            return null;
        Light light = lightObject.GetComponent<Light>();
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    private void RegisterReleaseHeavyImpact(float visualRadius, bool nuclear)
    {
        if (visualRadius < 5f && !nuclear)
            return;
        RegisterWeaponImpulse(Mathf.Clamp(visualRadius * (nuclear ? 0.012f : 0.006f), 0.035f, nuclear ? 0.32f : 0.16f));
        releaseSettingsSaveTimer = Mathf.Max(releaseSettingsSaveTimer, 0.1f);
    }

    private void LoadReleaseCandidateSettings()
    {
        releaseMasterVolume = PlayerPrefs.GetFloat("SR_MasterVolume", 0.85f);
        musicVolume = PlayerPrefs.GetFloat("SR_MusicVolume", 0.35f);
        benchmarkMusicVolume = PlayerPrefs.GetFloat("SR_BenchmarkMusicVolume", 0.74f);
        sandRunnerAudioMaster = PlayerPrefs.GetFloat("SR_EffectsVolume", 0.45f);
        sandRunnerAmbienceVolume = PlayerPrefs.GetFloat("SR_AmbienceVolume", 0.45f);
        sandRunnerRadioVolume = PlayerPrefs.GetFloat("SR_RadioVolume", 0.8f);
        releaseCameraShakeStrength = PlayerPrefs.GetFloat("SR_CameraShake", 0.72f);
        ApplyReleaseCandidateSettings();
    }

    private void SaveReleaseCandidateSettings()
    {
        PlayerPrefs.SetFloat("SR_MasterVolume", releaseMasterVolume);
        PlayerPrefs.SetFloat("SR_MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SR_BenchmarkMusicVolume", benchmarkMusicVolume);
        PlayerPrefs.SetFloat("SR_EffectsVolume", sandRunnerAudioMaster);
        PlayerPrefs.SetFloat("SR_AmbienceVolume", sandRunnerAmbienceVolume);
        PlayerPrefs.SetFloat("SR_RadioVolume", sandRunnerRadioVolume);
        PlayerPrefs.SetFloat("SR_CameraShake", releaseCameraShakeStrength);
        PlayerPrefs.Save();
    }

    private void ApplyReleaseCandidateSettings()
    {
        AudioListener.volume = Mathf.Clamp01(releaseMasterVolume);
        ApplyBenchmarkMusicVolume();
    }

    private void DrawReleaseCandidateSettings(Rect panel)
    {
        float oldMaster = releaseMasterVolume;
        float oldMusic = musicVolume;
        float oldBenchmarkMusic = benchmarkMusicVolume;
        float oldEffects = sandRunnerAudioMaster;
        float oldAmbience = sandRunnerAmbienceVolume;
        float oldRadio = sandRunnerRadioVolume;
        float oldShake = releaseCameraShakeStrength;

        GUI.Label(new Rect(panel.x + 72f, panel.y + 82f, 150f, 22f), "MASTER", menuBodyStyle);
        releaseMasterVolume = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 90f, 230f, 18f), releaseMasterVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 116f, 150f, 22f), "MUSIC", menuBodyStyle);
        musicVolume = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 124f, 230f, 18f), musicVolume, 0f, 0.7f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 150f, 150f, 22f), "BENCHMARK MUSIC", menuBodyStyle);
        benchmarkMusicVolume = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 158f, 230f, 18f), benchmarkMusicVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 184f, 150f, 22f), "EFFECTS", menuBodyStyle);
        sandRunnerAudioMaster = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 192f, 230f, 18f), sandRunnerAudioMaster, 0f, 1f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 218f, 150f, 22f), "AMBIENCE", menuBodyStyle);
        sandRunnerAmbienceVolume = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 226f, 230f, 18f), sandRunnerAmbienceVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 252f, 150f, 22f), "RADIO", menuBodyStyle);
        sandRunnerRadioVolume = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 260f, 230f, 18f), sandRunnerRadioVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 286f, 150f, 22f), "CAMERA SHAKE", menuBodyStyle);
        releaseCameraShakeStrength = GUI.HorizontalSlider(new Rect(panel.x + 214f, panel.y + 294f, 230f, 18f), releaseCameraShakeStrength, 0f, 1f);

        GUI.Label(new Rect(panel.x + 214f, panel.y + 320f, 260f, 20f),
            "M " + Mathf.RoundToInt(releaseMasterVolume * 100f) + "  MU " + Mathf.RoundToInt(musicVolume / 0.7f * 100f) +
            "  BM " + Mathf.RoundToInt(benchmarkMusicVolume * 100f) + "  FX " + Mathf.RoundToInt(sandRunnerAudioMaster * 100f) +
            "  A " + Mathf.RoundToInt(sandRunnerAmbienceVolume * 100f) + "  R " + Mathf.RoundToInt(sandRunnerRadioVolume * 100f), menuBodyStyle);

        if (!Mathf.Approximately(oldMaster, releaseMasterVolume) || !Mathf.Approximately(oldMusic, musicVolume) || !Mathf.Approximately(oldBenchmarkMusic, benchmarkMusicVolume) ||
            !Mathf.Approximately(oldEffects, sandRunnerAudioMaster) || !Mathf.Approximately(oldAmbience, sandRunnerAmbienceVolume) ||
            !Mathf.Approximately(oldRadio, sandRunnerRadioVolume) || !Mathf.Approximately(oldShake, releaseCameraShakeStrength))
        {
            ApplyReleaseCandidateSettings();
            SaveReleaseCandidateSettings();
        }
    }

    private void CreateReleaseCandidateRoadNetwork()
    {
        if (releaseRoadRoot != null || battlePyramid == null)
            return;
        releaseRoadRoot = new GameObject("SandRunners_RC_Route_Network").transform;
        Material roadMaterial = CreateMaterial("SandRunners Route Patina", new Color(0.08f, 0.05f, 0.03f, 0.14f));
        ConfigureTransparent(roadMaterial);

        int routeIndex = 0;
        for (int i = 0; i < resourceNodes.Count && i < 6; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null)
                continue;
            CreateReleaseRouteLine("Extraction_Route_" + routeIndex++, battlePyramid.position, node.transform.position, roadMaterial);
        }
        for (int i = 0; i < neutralSettlements.Count && i < 2; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement == null || settlement.root == null)
                continue;
            CreateReleaseRouteLine("Settlement_Route_" + routeIndex++, battlePyramid.position, settlement.root.position, roadMaterial);
        }
    }

    private void CreateReleaseRouteLine(string label, Vector3 start, Vector3 end, Material material)
    {
        GameObject routeObject = new GameObject(label);
        routeObject.transform.SetParent(releaseRoadRoot, false);
        LineRenderer line = routeObject.AddComponent<LineRenderer>();
        line.positionCount = 4;
        Vector3 delta = end - start;
        Vector3 side = Vector3.Cross(Vector3.up, delta.normalized) * 10f;
        Vector3 p0 = new Vector3(start.x, GetPlayableGroundHeight(start) + 0.12f, start.z);
        Vector3 p3 = new Vector3(end.x, GetPlayableGroundHeight(end) + 0.12f, end.z);
        Vector3 p1Raw = Vector3.Lerp(start, end, 0.34f) + side;
        Vector3 p2Raw = Vector3.Lerp(start, end, 0.67f) - side * 0.5f;
        Vector3 p1 = new Vector3(p1Raw.x, GetPlayableGroundHeight(p1Raw) + 0.12f, p1Raw.z);
        Vector3 p2 = new Vector3(p2Raw.x, GetPlayableGroundHeight(p2Raw) + 0.12f, p2Raw.z);
        line.SetPositions(new[] { p0, p1, p2, p3 });
        line.startWidth = 0.9f;
        line.endWidth = 0.58f;
        line.material = material;
        line.startColor = new Color(0.16f, 0.11f, 0.07f, 0.14f);
        line.endColor = new Color(0.08f, 0.06f, 0.04f, 0.08f);
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Tile;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }
}