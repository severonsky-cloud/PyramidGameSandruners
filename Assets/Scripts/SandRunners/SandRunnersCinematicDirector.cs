using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private const string CinematicDirectorMusicResourcePath90 = "SandRunners/Audio/GlacierAndCliff_Climax90";
    private const string CinematicDirectorMusicResourcePath180 = "SandRunners/Audio/GlacierAndCliff_Showcase180";
    private const string CinematicDirectorSettlementMusicResourcePath180 = "SandRunners/Audio/Behemoth_SettlementShowcase180";
    private const string CinematicDirectorOverviewMusicResourcePath184 = "SandRunners/Audio/SandrunnerOath_TrailerOverview184";
    private const float CinematicDirectorDefaultDuration = 90f;
    private const float CinematicDirectorExtendedDuration = 180f;
    private const float CinematicDirectorOverviewDuration = 183.5f;

    private readonly List<UnitSquad> cinematicDirectorGoldenSquads = new List<UnitSquad>();
    private readonly List<MandarinkaAsset> cinematicDirectorHostileAssets = new List<MandarinkaAsset>();
    private readonly List<EnemyUnit> cinematicDirectorSettlementRaiders = new List<EnemyUnit>();
    private readonly List<GoldenStructure> cinematicDirectorSettlementBaseStructures = new List<GoldenStructure>();
    private AudioSource cinematicDirectorMusicSource;
    private AudioClip cinematicDirectorMusicClip90;
    private AudioClip cinematicDirectorMusicClip180;
    private AudioClip cinematicDirectorSettlementMusicClip180;
    private AudioClip cinematicDirectorOverviewMusicClip184;
    private bool cinematicDirectorLaunchRequested;
    private bool cinematicDirectorActive;
    private bool cinematicDirectorSettlementMode;
    private bool cinematicDirectorOverviewMode;
    private bool cinematicDirectorOriginalHudHidden;
    private float cinematicDirectorRequestedDuration = CinematicDirectorDefaultDuration;
    private float cinematicDirectorTimer;
    private float cinematicDirectorSmoothedFov = 52f;
    private float cinematicDirectorCurrentFov = 52f;
    private float cinematicDirectorExtendedSpawnTimer;
    private float cinematicDirectorSettlementEffectTimer;
    private int cinematicDirectorBeatIndex;
    private int cinematicDirectorLastShotIndex = -1;
    private string cinematicDirectorShotLabel = "Armored parade";
    private Vector3 cinematicDirectorSmoothedFocus;
    private Vector3 cinematicDirectorFocusVelocity;
    private GUIStyle cinematicDirectorStyle;
    private SettlementDevelopmentState cinematicDirectorSettlementState;
    private TradeCaravanState cinematicDirectorSettlementCaravan;

    private void InitializeCinematicDirector()
    {
        Application.runInBackground = true;
        cinematicDirectorLaunchRequested = TryReadCinematicDirectorCommandLine(out cinematicDirectorRequestedDuration);
        if (cinematicDirectorLaunchRequested)
            StartCinematicDirector(cinematicDirectorRequestedDuration);
    }

    private bool HandleCinematicDirectorHotkeys()
    {
        if (WasKeyPressedThisFrame(Key.F5))
        {
            StartSalvageBenchmark();
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F6))
        {
            StartDamageBenchmark();
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F7))
        {
            StartSandRunnersIntro();
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F9))
        {
            StartCinematicDirector(CinematicDirectorDefaultDuration);
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F10))
        {
            StartCinematicDirector(CinematicDirectorExtendedDuration);
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F11))
        {
            StartSettlementShowcaseDirector(CinematicDirectorExtendedDuration);
            return true;
        }

        if (WasKeyPressedThisFrame(Key.F12))
        {
            StartOverviewTrailerDirector();
            return true;
        }

        if (cinematicDirectorActive && WasKeyPressedThisFrame(Key.F8))
        {
            StopCinematicDirector(true);
            return true;
        }

        return false;
    }

    private bool IsCinematicDirectorDrivingGameplay()
    {
        return cinematicDirectorActive;
    }

    private void StartCinematicDirector(float duration)
    {
        EnsureReferences();
        if (battlePyramid == null)
            return;

        cinematicDirectorActive = true;
        cinematicDirectorSettlementMode = false;
        cinematicDirectorOverviewMode = false;
        cinematicDirectorOriginalHudHidden = hudHidden;
        cinematicDirectorTimer = 0f;
        cinematicDirectorBeatIndex = 0;
        cinematicDirectorLastShotIndex = -1;
        cinematicDirectorExtendedSpawnTimer = 0f;
        cinematicDirectorSettlementEffectTimer = 0f;
        cinematicDirectorRequestedDuration = Mathf.Clamp(duration, 30f, 240f);
        cinematicDirectorGoldenSquads.Clear();
        cinematicDirectorHostileAssets.Clear();
        cinematicDirectorSettlementBaseStructures.Clear();
        cinematicDirectorSmoothedFocus = battlePyramid.position + Vector3.up * 6f;
        cinematicDirectorFocusVelocity = Vector3.zero;
        cinematicDirectorSmoothedFov = 52f;
        cinematicDirectorCurrentFov = 52f;

        hudHidden = true;
        SetGameFlowState(SandRunnersGameFlowState.Playing);
        StopRegularMusicForBenchmark();
        SetCommandCursorMode(false);
        ClearRTSSelection();

        StageCinematicDirectorBattlefield();
        PlayCinematicDirectorMusic();
        SetupCameraImmediate();
        lastEvent = "Showcase demo running: F8 stop, F9 90s cut, F10 3-minute montage.";
    }

    private void StartOverviewTrailerDirector()
    {
        EnsureReferences();
        if (battlePyramid == null)
            return;

        cinematicDirectorActive = true;
        cinematicDirectorSettlementMode = false;
        cinematicDirectorOverviewMode = true;
        cinematicDirectorOriginalHudHidden = hudHidden;
        cinematicDirectorTimer = 0f;
        cinematicDirectorBeatIndex = 0;
        cinematicDirectorLastShotIndex = -1;
        cinematicDirectorExtendedSpawnTimer = 0f;
        cinematicDirectorSettlementEffectTimer = 0f;
        cinematicDirectorRequestedDuration = CinematicDirectorOverviewDuration;
        cinematicDirectorGoldenSquads.Clear();
        cinematicDirectorHostileAssets.Clear();
        cinematicDirectorSettlementBaseStructures.Clear();
        cinematicDirectorSmoothedFocus = battlePyramid.position + Vector3.up * 6f;
        cinematicDirectorFocusVelocity = Vector3.zero;
        cinematicDirectorSmoothedFov = 52f;
        cinematicDirectorCurrentFov = 52f;

        hudHidden = true;
        SetGameFlowState(SandRunnersGameFlowState.Playing);
        SetCommandCursorMode(false);
        ClearRTSSelection();

        StageOverviewTrailerBattlefield();
        PlayCinematicDirectorMusic();
        SetupCameraImmediate();
        lastEvent = "Overview trailer running: F8 stop, F10 battle, F11 settlements, F12 overview.";
    }

    private void StartSettlementShowcaseDirector(float duration)
    {
        EnsureReferences();
        if (battlePyramid == null)
            return;

        cinematicDirectorActive = true;
        cinematicDirectorSettlementMode = true;
        cinematicDirectorOriginalHudHidden = hudHidden;
        cinematicDirectorTimer = 0f;
        cinematicDirectorBeatIndex = 0;
        cinematicDirectorLastShotIndex = -1;
        cinematicDirectorExtendedSpawnTimer = 0f;
        cinematicDirectorSettlementEffectTimer = 0f;
        cinematicDirectorRequestedDuration = Mathf.Clamp(duration, 120f, 240f);
        cinematicDirectorGoldenSquads.Clear();
        cinematicDirectorHostileAssets.Clear();
        cinematicDirectorSettlementRaiders.Clear();
        cinematicDirectorSettlementBaseStructures.Clear();
        cinematicDirectorSettlementState = null;
        cinematicDirectorSettlementCaravan = null;
        cinematicDirectorSmoothedFocus = battlePyramid.position + Vector3.up * 6f;
        cinematicDirectorFocusVelocity = Vector3.zero;
        cinematicDirectorSmoothedFov = 50f;
        cinematicDirectorCurrentFov = 50f;

        hudHidden = true;
        SetGameFlowState(SandRunnersGameFlowState.Playing);
        StopRegularMusicForBenchmark();
        SetCommandCursorMode(false);
        ClearRTSSelection();

        StageSettlementShowcaseBattlefield();
        PlayCinematicDirectorMusic();
        SetupCameraImmediate();
        lastEvent = "Settlement showcase running: F8 stop, F11 settlements, caravans, mirror grid and allied aid.";
    }

    private void StopCinematicDirector(bool restoreHud)
    {
        if (sandRunnersIntroActive)
        {
            StopSandRunnersIntro(restoreHud);
            return;
        }

        cinematicDirectorActive = false;
        cinematicDirectorSettlementMode = false;
        cinematicDirectorOverviewMode = false;
        cinematicDirectorTimer = 0f;
        cinematicDirectorBeatIndex = 0;
        cameraFollowSelectionMode = false;
        cameraFollowTarget = null;
        cameraFollowLabel = null;
        cameraFollowInitialized = false;
        if (restoreHud)
            hudHidden = cinematicDirectorOriginalHudHidden;
        if (cinematicDirectorMusicSource != null)
        {
            cinematicDirectorMusicSource.Stop();
            cinematicDirectorMusicSource.clip = null;
            cinematicDirectorMusicSource.volume = 0f;
        }
        ResumeRegularMusicAfterBenchmark();
        lastEvent = "Trailer demo finished. F9 starts the 90-second cut again.";
    }

    private void UpdateCinematicDirector(float dt)
    {
        if (!cinematicDirectorActive)
            return;

        if (sandRunnersIntroActive)
        {
            UpdateSandRunnersIntro(dt);
            return;
        }

        ApplyBenchmarkMusicVolume();

        cinematicDirectorTimer += dt;
        bool holdingFinalShot = cinematicDirectorTimer >= cinematicDirectorRequestedDuration;
        if (holdingFinalShot)
            cinematicDirectorTimer = cinematicDirectorRequestedDuration;

        pyramidHull = Mathf.Max(pyramidHull, pyramidMaxHull * 0.72f);
        cruiseMissiles = Mathf.Max(cruiseMissiles, 4);
        nuclearMissiles = Mathf.Max(nuclearMissiles, 1);
        waveTimer = Mathf.Max(waveTimer, 50f);
        hudHidden = true;
        if (strategicCanvas != null && strategicCanvas.gameObject.activeSelf)
            strategicCanvas.gameObject.SetActive(false);

        if (cinematicDirectorSettlementMode)
        {
            UpdateSettlementShowcaseDirector(dt, holdingFinalShot);
            return;
        }

        if (cinematicDirectorOverviewMode)
        {
            UpdateOverviewTrailerDirector(dt, holdingFinalShot);
            return;
        }

        RunCinematicDirectorBeats();
        DriveCinematicDirectorPyramid(dt);
        UpdateCinematicDirectorCamera(dt);

        if (!holdingFinalShot && cinematicDirectorRequestedDuration > CinematicDirectorDefaultDuration + 1f)
            UpdateCinematicDirectorExtendedBattle(dt);

        if (holdingFinalShot)
        {
            pyramidHull = Mathf.Max(pyramidHull, pyramidMaxHull * 0.72f);
            cinematicDirectorShotLabel = "Final battlefield hold";
        }
    }

    private void StageCinematicDirectorBattlefield()
    {
        sand = Mathf.Max(sand, 6000f);
        gold = Mathf.Max(gold, 6000f);
        wind = Mathf.Max(wind, 1800f);
        cruiseMissiles = Mathf.Max(cruiseMissiles, 12);
        nuclearMissiles = Mathf.Max(nuclearMissiles, 3);
        missileTimer = 0f;
        nuclearTimer = 0f;
        beamCooldownTimer = 0f;
        beamCharge = 0f;
        mandarinkaSupply = 720f;
        mandarinkaStrategyTimer = 420f;
        mandarinkaStrategyPhase = MandarinkaStrategyPhase.FortressAdvance;
        mandarinkaGroundTimer = 2f;
        mandarinkaAirTimer = 9f;
        mandarinkaBuilderTimer = 12f;
        mandarinkaGustavTimer = 18f;
        mandarinkaFortressPhase = Mathf.Max(mandarinkaFortressPhase, 1);
        mandarinkaFortressShield = Mathf.Max(mandarinkaFortressShield, MandarinkaFortressMaxShield * 0.62f);
        mandarinkaDefeated = false;
        mandarinkaFinaleTriggered = false;

        Vector3 pyramidPosition = CinematicGround(new Vector3(-175f, 0f, -180f), pyramidGroundClearance);
        Vector3 fortressPosition = CinematicGround(new Vector3(170f, 0f, 165f), 0.3f);
        Vector3 battleForward = (fortressPosition - pyramidPosition);
        battleForward.y = 0f;
        if (battleForward.sqrMagnitude < 0.01f)
            battleForward = Vector3.forward;
        battleForward.Normalize();
        Vector3 battleRight = Vector3.Cross(Vector3.up, battleForward).normalized;

        battlePyramid.position = pyramidPosition;
        battlePyramid.rotation = Quaternion.LookRotation(battleForward, Vector3.up);
        pyramidVelocity = Vector3.zero;
        pyramidThrottleBlend = 0.35f;
        hasCommandDestination = false;

        if (mandarinkaFortressRoot != null)
        {
            mandarinkaFortressRoot.position = fortressPosition;
            mandarinkaFortressRoot.rotation = Quaternion.LookRotation(-battleForward, Vector3.up);
            mandarinkaHomePosition = fortressPosition;
        }

        Vector3 goldenParadeBase = pyramidPosition - battleForward * 38f;
        SpawnCinematicSquad(ProductionKind.ScarabTank, goldenParadeBase - battleRight * 34f, battleForward, 7.5f, 7.5f);
        SpawnCinematicSquad(ProductionKind.ScarabTank, goldenParadeBase - battleRight * 18f - battleForward * 8f, battleForward, 7.5f, 7.5f);
        SpawnCinematicSquad(ProductionKind.SandSkimmer, goldenParadeBase + battleRight * 1f - battleForward * 14f, battleForward, 6.5f, 6.2f);
        SpawnCinematicSquad(ProductionKind.SiegeScarab, goldenParadeBase + battleRight * 18f - battleForward * 18f, battleForward, 8f, 7f);
        SpawnCinematicSquad(ProductionKind.FortressCrusher, goldenParadeBase + battleRight * 36f - battleForward * 20f, battleForward, 10f, 8f);
        SpawnCinematicSquad(ProductionKind.WrathOfRa, pyramidPosition - battleRight * 52f - battleForward * 10f, battleForward, 11f, 8f);
        SpawnCinematicSquad(ProductionKind.BuilderTruck, pyramidPosition + battleRight * 52f - battleForward * 24f, battleForward, 7f, 6f);
        SpawnCinematicSquad(ProductionKind.BuilderTruck, pyramidPosition + battleRight * 66f - battleForward * 30f, battleForward, 7f, 6f);
        SpawnCinematicSquad(ProductionKind.CombatFlyer, pyramidPosition - battleRight * 28f + battleForward * 16f, battleForward, 10f, 8f);
        SpawnCinematicSquad(ProductionKind.CombatFlyer, pyramidPosition + battleRight * 28f + battleForward * 10f, battleForward, 10f, 8f);
        SpawnCinematicSquad(ProductionKind.HeavyFlyer, pyramidPosition + battleRight * 6f + battleForward * 30f, battleForward, 11f, 8f);
        SpawnCinematicSquad(ProductionKind.ThothEmbrace, pyramidPosition - battleRight * 6f + battleForward * 48f, battleForward, 12f, 9f);

        CreateGoldenStructure(StructureKind.Twin30mmTurret, CinematicGround(pyramidPosition + battleForward * 70f - battleRight * 44f, 0.12f));
        CreateGoldenStructure(StructureKind.Twin30mmTurret, CinematicGround(pyramidPosition + battleForward * 80f + battleRight * 44f, 0.12f));
        CreateGoldenStructure(StructureKind.MirrorBeamTurret, CinematicGround(pyramidPosition + battleForward * 96f - battleRight * 16f, 0.12f));
        CreateGoldenStructure(StructureKind.GepardAALauncher, CinematicGround(pyramidPosition + battleForward * 102f + battleRight * 18f, 0.12f));

        Vector3 hostileBase = fortressPosition + battleForward * 34f;
        for (int i = 0; i < 6; i++)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, hostileBase - battleRight * 48f + battleRight * (i * 16f) + battleForward * (i % 2 == 0 ? 0f : 12f), -battleForward);
        for (int i = 0; i < 3; i++)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, fortressPosition - battleForward * 30f + battleRight * (-28f + i * 28f), -battleForward);
        for (int i = 0; i < 2; i++)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.Builder, fortressPosition + battleRight * (-28f + i * 56f) + battleForward * 22f, -battleForward);
        SpawnCinematicMandarinkaAsset(MandarinkaRole.Gustav, fortressPosition + battleRight * 44f + battleForward * 58f, -battleForward);
        SpawnCinematicMandarinkaAsset(MandarinkaRole.Gustav, fortressPosition - battleRight * 44f + battleForward * 58f, -battleForward);
        SpawnMandarinkaTurret(CinematicGround(fortressPosition - battleForward * 60f - battleRight * 62f, 0.2f));
        SpawnMandarinkaTurret(CinematicGround(fortressPosition - battleForward * 54f + battleRight * 62f, 0.2f));

        OrderCinematicParadeMarch();
        SetMandarinkaRadio("SEBEK: The desert has its witnesses. Roll the pyramid. Let the cameras keep up.");
        CreateBattleExplosionFx(CinematicBattleCenter(), 14f, false, true);
    }

    private void StageOverviewTrailerBattlefield()
    {
        sand = Mathf.Max(sand, 7000f);
        gold = Mathf.Max(gold, 7000f);
        wind = Mathf.Max(wind, 2200f);
        cruiseMissiles = Mathf.Max(cruiseMissiles, 10);
        nuclearMissiles = Mathf.Max(nuclearMissiles, 2);
        missileTimer = 0f;
        nuclearTimer = 0f;
        beamCooldownTimer = 0f;
        beamCharge = 0f;
        juzzherSpawnTimer = 999f;

        InitializeVerticalSliceFactions();
        InitializeWorldEvents();
        SynchronizeSettlementDevelopment();

        cinematicDirectorSettlementState = FindShowcaseSettlement();
        if (cinematicDirectorSettlementState != null)
            ResetSettlementShowcaseState(cinematicDirectorSettlementState);

        Vector3 start = CinematicGround(new Vector3(-240f, 0f, -260f), pyramidGroundClearance);
        Vector3 destination = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : new Vector3(160f, 0f, -340f);
        Vector3 forward = destination - start;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        battlePyramid.position = start;
        battlePyramid.rotation = Quaternion.LookRotation(forward, Vector3.up);
        pyramidVelocity = Vector3.zero;
        pyramidThrottleBlend = 0.32f;
        hasCommandDestination = false;

        Vector3 castlePosition = CinematicGround(destination + forward * 250f + right * 42f, 0.3f);
        if (mandarinkaFortressRoot != null)
        {
            mandarinkaFortressRoot.position = castlePosition;
            mandarinkaFortressRoot.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            mandarinkaHomePosition = castlePosition;
            mandarinkaFortressPhase = Mathf.Max(mandarinkaFortressPhase, 1);
            mandarinkaFortressShield = Mathf.Max(mandarinkaFortressShield, MandarinkaFortressMaxShield * 0.65f);
            mandarinkaDefeated = false;
            mandarinkaFinaleTriggered = false;
        }

        ShowBanner("SANDRUNNERS // TRAILER OVERVIEW", 3f);
        lastEvent = "Trailer overview begins with the pyramid crossing empty desert toward an unknown resource field.";
        cinematicDirectorShotLabel = "Desert approach";
    }

    private void UpdateOverviewTrailerDirector(float dt, bool holdingFinalShot)
    {
        RunOverviewTrailerBeats();
        DriveOverviewTrailerPyramid(dt);
        UpdateSettlementShowcaseCaravan(dt);
        UpdateSettlementShowcaseRaiders(dt);
        UpdateOverviewTrailerScriptedEffects(dt);
        UpdateEnergyNetworks(dt);
        RefreshSettlementStatusLabels();
        UpdateCinematicDirectorCamera(dt);

        if (holdingFinalShot)
        {
            pyramidHull = Mathf.Max(pyramidHull, pyramidMaxHull * 0.84f);
            cinematicDirectorShotLabel = "Full mechanics overview hold";
        }
    }

    private void RunOverviewTrailerBeats()
    {
        while (cinematicDirectorBeatIndex < 15 && cinematicDirectorTimer >= GetOverviewTrailerBeatTime(cinematicDirectorBeatIndex))
        {
            ExecuteOverviewTrailerBeat(cinematicDirectorBeatIndex);
            cinematicDirectorBeatIndex++;
        }
    }

    private float GetOverviewTrailerBeatTime(int beat)
    {
        switch (beat)
        {
            case 0: return 0f;
            case 1: return 12f;
            case 2: return 24f;
            case 3: return 38f;
            case 4: return 52f;
            case 5: return 66f;
            case 6: return 80f;
            case 7: return 94f;
            case 8: return 108f;
            case 9: return 122f;
            case 10: return 136f;
            case 11: return 150f;
            case 12: return 162f;
            case 13: return 174f;
            case 14: return 181f;
            default: return 999f;
        }
    }

    private void ExecuteOverviewTrailerBeat(int beat)
    {
        Vector3 forward = battlePyramid != null ? battlePyramid.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 settlement = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : battlePyramid.position + forward * 180f;

        switch (beat)
        {
            case 0:
                ShowBanner("1 // THE PYRAMID CROSSES THE EMPTY DESERT", 2.6f);
                break;
            case 1:
                ShowBanner("2 // RESOURCE FIELD DISCOVERED", 2.4f);
                CaptureOverviewResourceField();
                break;
            case 2:
                ShowBanner("3 // BUILDERS DEPLOY", 2.4f);
                SpawnCinematicSquad(ProductionKind.BuilderTruck, battlePyramid.position - forward * 34f - right * 18f, forward, 6f, 5f);
                SpawnCinematicSquad(ProductionKind.BuilderTruck, battlePyramid.position - forward * 42f + right * 20f, forward, 6f, 5f);
                PlaySandRunnerSound(SandRunnerSound.Construction, battlePyramid.position, 0.9f);
                break;
            case 3:
                ShowBanner("4 // SMALL GOLDEN BASE ONLINE", 2.4f);
                BuildOverviewForwardBase(settlement, forward, right);
                break;
            case 4:
                ShowBanner("5 // AERODROME BUILT // FLYERS LAUNCH", 2.5f);
                LaunchSettlementShowcaseAirPatrol();
                break;
            case 5:
                ShowBanner("6 // MAP-WIDE AIR PATROL", 2.4f);
                SpawnOverviewAirBattle(settlement - forward * 104f + right * 44f, forward);
                break;
            case 6:
                ShowBanner("7 // AIR CONTACT", 2.4f);
                PaintSettlementShowcaseAirPatrol();
                CreateBattleExplosionFx(settlement - forward * 104f + right * 44f, 12f, false, true);
                break;
            case 7:
                ShowBanner("8 // NEUTRAL SETTLEMENT SPOTTED", 2.5f);
                if (cinematicDirectorSettlementState != null)
                {
                    SpawnSettlementShowcaseCaravan();
                    RefreshSettlementStatusLabels();
                }
                break;
            case 8:
                ShowBanner("9 // PYRAMID MOVES TO CONTACT", 2.4f);
                SpawnCinematicSquad(ProductionKind.SandSkimmer, battlePyramid.position - forward * 40f - right * 26f, forward, 6f, 5f);
                SpawnCinematicSquad(ProductionKind.ScarabTank, battlePyramid.position - forward * 48f + right * 26f, forward, 7f, 6f);
                OrderCinematicParadeMarch();
                break;
            case 9:
                ShowBanner("10 // RED RAIDERS ATTACK THE SETTLEMENT", 2.7f);
                SpawnSettlementShowcaseRaiders(settlement + right * 30f, 5, false);
                SpawnSettlementShowcaseRaiders(cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null ? cinematicDirectorSettlementCaravan.root.position : settlement - forward * 42f, 3, true);
                break;
            case 10:
                ShowBanner("11 // GRID AND DEFENSES SAVE THE TOWN", 2.6f);
                BuildSettlementShowcaseGrid();
                FireAutocannons(false);
                FireHowitzers(-1);
                PaintPyramidCaravanDefense();
                break;
            case 11:
                ShowBanner("12 // ALLIED SETTLEMENT ARMS THE OFFENSIVE", 2.7f);
                if (cinematicDirectorSettlementState != null)
                {
                    cinematicDirectorSettlementState.defendedFromRaid = true;
                    CompleteSettlementTrade(cinematicDirectorSettlementState, true);
                    CompleteSettlementTrade(cinematicDirectorSettlementState, false);
                }
                SpawnCinematicSquad(ProductionKind.FortressCrusher, battlePyramid.position - forward * 62f - right * 38f, forward, 10f, 8f);
                SpawnCinematicSquad(ProductionKind.WrathOfRa, battlePyramid.position - forward * 58f + right * 40f, forward, 11f, 8f);
                break;
            case 12:
                ShowBanner("13 // CHINESE CASTLE REVEALED", 2.6f);
                SpawnOverviewHostileLine(GetOverviewCastlePosition(settlement, forward, right) - forward * 56f, -forward);
                break;
            case 13:
                ShowBanner("14 // SETTLEMENT ROCKET SUPPORT", 2.7f);
                FireOverviewSettlementRockets(settlement, GetOverviewCastlePosition(settlement, forward, right));
                FireMissile(false);
                break;
            case 14:
                ShowBanner("SANDRUNNERS // DESERT WAR ECONOMY", 3f);
                FireHowitzers(1);
                FireSettlementShowcaseGuards();
                PaintSettlementShowcaseAirPatrol();
                beamCharge = 1f;
                beamCooldownTimer = 0f;
                FireChargedBeam();
                CreatePulseRing();
                CreateBattleExplosionFx(GetOverviewCastlePosition(settlement, forward, right), 24f, true, false);
                break;
        }
    }

    private void DriveOverviewTrailerPyramid(float dt)
    {
        if (battlePyramid == null)
            return;

        Vector3 settlement = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : new Vector3(155f, 0f, -345f);
        Vector3 routeForward = settlement - new Vector3(-240f, 0f, -260f);
        routeForward.y = 0f;
        if (routeForward.sqrMagnitude < 0.01f)
            routeForward = battlePyramid.forward;
        routeForward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, routeForward).normalized;
        Vector3 resource = GetOverviewResourceFocus();
        Vector3 basePosition = GetOverviewForwardBaseFocus(settlement, routeForward, right);
        Vector3 castle = GetOverviewCastlePosition(settlement, routeForward, right);

        Vector3 target;
        if (cinematicDirectorTimer < 18f)
            target = resource - routeForward * 86f - right * 18f;
        else if (cinematicDirectorTimer < 48f)
            target = resource - routeForward * 42f + right * 8f;
        else if (cinematicDirectorTimer < 96f)
            target = basePosition - routeForward * 26f - right * 12f;
        else if (cinematicDirectorTimer < 136f)
            target = settlement - routeForward * 48f + right * 14f;
        else
            target = Vector3.Lerp(settlement, castle, 0.42f) - routeForward * 22f;

        target = CinematicGround(target, pyramidGroundClearance);
        Vector3 delta = target - battlePyramid.position;
        delta.y = 0f;
        if (delta.magnitude > 2f)
        {
            float speed = cinematicDirectorTimer < 96f ? 14f : cinematicDirectorTimer < 136f ? 11f : 10f;
            battlePyramid.position += delta.normalized * Mathf.Min(delta.magnitude, speed * dt);
            battlePyramid.rotation = Quaternion.Slerp(battlePyramid.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), dt * 2.3f);
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.68f, dt * 1.5f);
        }
        else
        {
            Vector3 look = cinematicDirectorTimer >= 136f ? castle - battlePyramid.position : settlement - battlePyramid.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.01f)
                look = routeForward;
            battlePyramid.rotation = Quaternion.Slerp(battlePyramid.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), dt * 1.8f);
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.28f, dt * 1.4f);
        }
    }

    private void UpdateOverviewTrailerScriptedEffects(float dt)
    {
        cinematicDirectorSettlementEffectTimer -= dt;
        if (cinematicDirectorSettlementEffectTimer > 0f)
            return;

        cinematicDirectorSettlementEffectTimer = 0.42f;
        if (cinematicDirectorTimer >= 12f && cinematicDirectorTimer < 36f)
            PaintOverviewResourcePulse();
        if (cinematicDirectorTimer >= 30f && cinematicDirectorTimer < 68f)
            PaintSettlementShowcaseBaseConstruction();
        if (cinematicDirectorTimer >= 54f && cinematicDirectorTimer < 106f)
            PaintSettlementShowcaseAirPatrol();
        if (cinematicDirectorTimer >= 78f && cinematicDirectorTimer < 94f)
            PaintOverviewAirBattle();
        if (cinematicDirectorTimer >= 118f && cinematicDirectorTimer < 146f)
            PaintSettlementShowcaseCaravanRaid();
        if (cinematicDirectorTimer >= 132f && cinematicDirectorTimer < 160f)
            PaintSettlementShowcaseGridPulse();
        if (cinematicDirectorTimer >= 150f)
        {
            PaintPyramidCaravanDefense();
            FireSettlementShowcaseGuards();
        }
        if (cinematicDirectorTimer >= 162f)
        {
            Vector3 settlement = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
                ? cinematicDirectorSettlementState.settlement.root.position
                : battlePyramid.position + battlePyramid.forward * 120f;
            FireOverviewSettlementRockets(settlement, GetOverviewCastlePosition(settlement, battlePyramid.forward, battlePyramid.right));
        }
    }

    private void BuildOverviewForwardBase(Vector3 settlement, Vector3 forward, Vector3 right)
    {
        Vector3 resource = GetOverviewResourceFocus();
        Vector3 baseCenter = CinematicGround(resource - forward * 28f + right * 18f, 0.12f);
        CreateSettlementShowcaseBaseStructure(StructureKind.ResourceDepot, baseCenter - right * 16f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.Twin30mmTurret, baseCenter + forward * 16f - right * 34f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.MirrorBeamTurret, baseCenter + forward * 28f + right * 4f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.GepardAALauncher, baseCenter + forward * 20f + right * 34f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.Aerodrome, baseCenter - forward * 28f + right * 52f, forward);
        SpawnCinematicSquad(ProductionKind.ScarabTank, baseCenter - forward * 42f - right * 24f, forward, 8f, 7f);
        PlaySandRunnerSound(SandRunnerSound.Construction, baseCenter, 0.95f);
        PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, "Builders raised a Golden Elemental base around the captured resource field.", 12f, false);
        lastEvent = "Resource captured. Builders convert the empty desert into a depot, towers and an aerodrome.";
    }

    private void SpawnOverviewHostileLine(Vector3 anchor, Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        for (int i = 0; i < 4; i++)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, anchor + right * (-36f + i * 24f), forward);
        SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, anchor - forward * 22f + right * 28f, forward);
        SpawnCinematicMandarinkaAsset(MandarinkaRole.Gustav, anchor + forward * 32f - right * 28f, forward);
        CreateBattleExplosionFx(anchor, 12f, false, true);
    }

    private void PaintOverviewResourcePulse()
    {
        for (int i = 0; i < resourceNodes.Count && i < 5; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null)
                continue;
            Vector3 start = battlePyramid != null ? battlePyramid.position + Vector3.up * 5f : node.transform.position + Vector3.up * 8f;
            CreateBeam(start, node.transform.position + Vector3.up * 4f, node.kind == ResourceKind.Wind ? new Color(0.3f, 0.95f, 1f, 1f) : new Color(1f, 0.74f, 0.18f, 1f), 0.04f, 0.18f);
            if (i == 0)
                CreateBattleExplosionFx(node.transform.position, 5f, false, false);
        }
    }

    private void CaptureOverviewResourceField()
    {
        ResourceNode focus = null;
        float bestScore = float.MaxValue;
        Vector3 pyramidPosition = battlePyramid != null ? battlePyramid.position : Vector3.zero;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null)
                continue;
            float score = FlatDistance(pyramidPosition, node.transform.position);
            if (score < bestScore)
            {
                bestScore = score;
                focus = node;
            }
        }

        if (focus != null)
        {
            focus.controlled = true;
            focus.capture = 1f;
            if (focus.markerRenderer != null && controlledMaterial != null)
                focus.markerRenderer.sharedMaterial = controlledMaterial;
            CreateBattleExplosionFx(focus.transform.position, 8f, false, false);
            PushLivingWorldEvent(LivingWorldEventType.Trade, "The pyramid secured a resource field and opened the first build site.", 12f, false);
        }

        PaintOverviewResourcePulse();
    }

    private Vector3 GetOverviewForwardBaseFocus(Vector3 settlement, Vector3 forward, Vector3 right)
    {
        for (int i = 0; i < cinematicDirectorSettlementBaseStructures.Count; i++)
        {
            GoldenStructure structure = cinematicDirectorSettlementBaseStructures[i];
            if (structure != null && structure.transform != null && structure.kind == StructureKind.ResourceDepot)
                return structure.transform.position;
        }
        return GetOverviewResourceFocus() - forward * 28f + right * 18f;
    }

    private Vector3 GetOverviewCastlePosition(Vector3 settlement, Vector3 forward, Vector3 right)
    {
        if (mandarinkaFortressRoot != null)
            return mandarinkaFortressRoot.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battlePyramid != null ? battlePyramid.forward : Vector3.forward;
        forward.Normalize();
        right.y = 0f;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.Cross(Vector3.up, forward).normalized;
        return CinematicGround(settlement + forward * 250f + right * 42f, 0.3f);
    }

    private void SpawnOverviewAirBattle(Vector3 focus, Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battlePyramid != null ? battlePyramid.forward : Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        for (int i = 0; i < 3; i++)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, focus + right * (-28f + i * 28f) + forward * (i * 8f), -forward);
        CreateBattleExplosionFx(focus, 10f, false, true);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "Flyer patrols intercepted hostile aircraft over the caravan routes.", 10f, true);
    }

    private void PaintOverviewAirBattle()
    {
        Transform flyer = null;
        for (int squadIndex = 0; squadIndex < cinematicDirectorGoldenSquads.Count && flyer == null; squadIndex++)
        {
            UnitSquad squad = cinematicDirectorGoldenSquads[squadIndex];
            if (squad == null)
                continue;
            for (int i = 0; i < squad.units.Count; i++)
            {
                RunnerUnit unit = squad.units[i];
                if (unit != null && unit.transform != null && unit.airborne)
                {
                    flyer = unit.transform;
                    break;
                }
            }
        }

        if (flyer == null)
            return;

        for (int i = 0; i < cinematicDirectorHostileAssets.Count; i++)
        {
            MandarinkaAsset asset = cinematicDirectorHostileAssets[i];
            if (asset == null || asset.transform == null || asset.role != MandarinkaRole.AirJunk)
                continue;
            CreateBeam(flyer.position, asset.transform.position + Vector3.up * 0.8f, new Color(1f, 0.76f, 0.2f, 1f), 0.055f, 0.2f);
            CreateBeam(asset.transform.position, flyer.position + Vector3.up * 0.4f, new Color(1f, 0.08f, 0.03f, 1f), 0.04f, 0.16f);
            if (UnityEngine.Random.value > 0.65f)
                CreateBattleExplosionFx(asset.transform.position, 6f, false, true);
            break;
        }
    }

    private void FireOverviewSettlementRockets(Vector3 settlement, Vector3 castle)
    {
        Vector3 right = battlePyramid != null ? battlePyramid.right : Vector3.right;
        Vector3[] launchers =
        {
            settlement + right * 18f + Vector3.up * 5f,
            settlement - right * 22f + Vector3.up * 6f,
            settlement + (battlePyramid != null ? battlePyramid.forward : Vector3.forward) * 12f + Vector3.up * 5.5f
        };
        for (int i = 0; i < launchers.Length; i++)
        {
            Vector3 impact = castle + UnityEngine.Random.insideUnitSphere * 10f + Vector3.up * UnityEngine.Random.Range(4f, 10f);
            CreateBeam(launchers[i], impact, new Color(0.25f, 0.72f, 1f, 1f), 0.075f, 0.22f);
            if (i == 0 || UnityEngine.Random.value > 0.55f)
                CreateBattleExplosionFx(impact, 8f + i * 2f, false, true);
        }
    }

    private void StageSettlementShowcaseBattlefield()
    {
        sand = Mathf.Max(sand, 5000f);
        gold = Mathf.Max(gold, 5000f);
        wind = Mathf.Max(wind, 1400f);
        cruiseMissiles = Mathf.Max(cruiseMissiles, 8);
        nuclearMissiles = Mathf.Max(nuclearMissiles, 1);
        missileTimer = 0f;
        nuclearTimer = 0f;
        beamCooldownTimer = 0f;
        beamCharge = 0f;
        juzzherSpawnTimer = 999f;

        InitializeVerticalSliceFactions();
        InitializeWorldEvents();
        SynchronizeSettlementDevelopment();

        cinematicDirectorSettlementState = FindShowcaseSettlement();
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;

        ResetSettlementShowcaseState(cinematicDirectorSettlementState);
        ResourceNode windSource = FindOrPrepareSettlementShowcaseWindNode(cinematicDirectorSettlementState.settlement.root.position);

        Vector3 settlementPosition = cinematicDirectorSettlementState.settlement.root.position;
        Vector3 approach = new Vector3(-1f, 0f, -0.7f).normalized;
        Vector3 pyramidPosition = CinematicGround(settlementPosition + approach * 138f, pyramidGroundClearance);
        Vector3 forward = settlementPosition - pyramidPosition;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        battlePyramid.position = pyramidPosition;
        battlePyramid.rotation = Quaternion.LookRotation(forward, Vector3.up);
        pyramidVelocity = Vector3.zero;
        pyramidThrottleBlend = 0.28f;
        hasCommandDestination = false;

        SpawnSettlementShowcaseSupport(forward);
        SpawnSettlementShowcaseCaravan();
        RefreshSettlementStatusLabels();

        if (windSource != null)
            PushLivingWorldEvent(LivingWorldEventType.EnergyNetwork, "Controlled wind node ready for mirror-grid connection.", 12f, false);
        PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, cinematicDirectorSettlementState.settlement.displayName + " waits with NO GRID and weak defenses.", 12f, false);
        ShowBanner("SETTLEMENT SHOWCASE // NO GRID", 2.8f);
        cinematicDirectorShotLabel = "Settlement without power";
    }

    private SettlementDevelopmentState FindShowcaseSettlement()
    {
        SynchronizeSettlementDevelopment();
        SettlementDevelopmentState fallback = null;
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || state.settlement.root == null || state.settlement.health <= 0f)
                continue;
            if (fallback == null)
                fallback = state;
            if (state.settlement.kind == NeutralFactionKind.BlueTraders)
                return state;
        }
        return fallback;
    }

    private void ResetSettlementShowcaseState(SettlementDevelopmentState state)
    {
        if (state == null)
            return;

        if (state.network != null)
        {
            energyNetworks.Remove(state.network);
            if (state.network.root != null)
                Destroy(state.network.root.gameObject);
            state.network = null;
        }

        state.stage = SettlementDiplomacyStage.Neutral;
        state.playerDeals = 0;
        state.developmentPoints = 0;
        state.builtStructures = 0;
        state.deployedUnits = 0;
        state.tradeTrust = 18f;
        state.playerInfluence = 12f;
        state.defenseLevel = Mathf.Max(1f, state.defenseLevel * 0.35f);
        state.underRaid = false;
        state.defendedFromRaid = false;
        state.previousHealth = state.settlement != null ? state.settlement.health : 0f;
    }

    private ResourceNode FindOrPrepareSettlementShowcaseWindNode(Vector3 destination)
    {
        ResourceNode best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null || node.kind != ResourceKind.Wind)
                continue;
            float distance = FlatDistance(node.transform.position, destination);
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }

        if (best != null)
        {
            best.controlled = true;
            best.capture = 1f;
            if (best.markerRenderer != null && controlledMaterial != null)
                best.markerRenderer.sharedMaterial = controlledMaterial;
        }
        return best;
    }

    private void SpawnSettlementShowcaseSupport(Vector3 forward)
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null)
            return;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 basePosition = battlePyramid.position - forward * 24f;
        SpawnCinematicSquad(ProductionKind.BuilderTruck, basePosition - right * 16f, forward, 6f, 5f);
        SpawnCinematicSquad(ProductionKind.ScarabTank, basePosition + right * 5f, forward, 7f, 6f);
        SpawnCinematicSquad(ProductionKind.SandSkimmer, basePosition + right * 23f - forward * 6f, forward, 6f, 5f);
        OrderCinematicParadeMarch();
    }

    private void SpawnSettlementShowcaseCaravan()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;

        NeutralSettlement origin = null;
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement candidate = neutralSettlements[i];
            if (candidate != null && candidate.root != null && candidate != cinematicDirectorSettlementState.settlement)
            {
                origin = candidate;
                break;
            }
        }
        if (origin == null)
            origin = cinematicDirectorSettlementState.settlement;

        TradeCaravanState caravan = new TradeCaravanState();
        caravan.origin = origin;
        caravan.destination = cinematicDirectorSettlementState.settlement;
        caravan.faction = origin.kind;
        caravan.eventId = nextTradeCaravanId++;
        caravan.health = 210f;
        caravan.speed = 8f;
        Transform root = new GameObject("Settlement_Showcase_Caravan").transform;
        root.SetParent(livingWorldRoot != null ? livingWorldRoot : transform, false);
        Vector3 settlementPosition = cinematicDirectorSettlementState.settlement.root.position;
        Vector3 forward = (settlementPosition - battlePyramid.position);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        root.position = CinematicGround(settlementPosition - forward * 92f + right * 34f, 1.05f);
        root.rotation = Quaternion.LookRotation(forward, Vector3.up);
        caravan.root = root;
        BuildTradeCaravanVisual(caravan);
        tradeCaravans.Add(caravan);
        cinematicDirectorSettlementCaravan = caravan;
        PushLivingWorldEvent(LivingWorldEventType.Caravan, "Local caravan is moving toward " + cinematicDirectorSettlementState.settlement.displayName + ".", 12f, false);
    }

    private void UpdateSettlementShowcaseDirector(float dt, bool holdingFinalShot)
    {
        RunSettlementShowcaseBeats();
        DriveSettlementShowcasePyramid(dt);
        UpdateSettlementShowcaseCaravan(dt);
        UpdateSettlementShowcaseRaiders(dt);
        UpdateSettlementShowcaseScriptedEffects(dt);
        UpdateEnergyNetworks(dt);
        RefreshSettlementStatusLabels();
        UpdateCinematicDirectorCamera(dt);

        if (holdingFinalShot)
        {
            pyramidHull = Mathf.Max(pyramidHull, pyramidMaxHull * 0.82f);
            cinematicDirectorShotLabel = "Allied settlement powers the pyramid";
        }
    }

    private void RunSettlementShowcaseBeats()
    {
        while (cinematicDirectorBeatIndex < 13 && cinematicDirectorTimer >= GetSettlementShowcaseBeatTime(cinematicDirectorBeatIndex))
        {
            ExecuteSettlementShowcaseBeat(cinematicDirectorBeatIndex);
            cinematicDirectorBeatIndex++;
        }
    }

    private float GetSettlementShowcaseBeatTime(int beat)
    {
        switch (beat)
        {
            case 0: return 0f;
            case 1: return 14f;
            case 2: return 28f;
            case 3: return 42f;
            case 4: return 56f;
            case 5: return 72f;
            case 6: return 88f;
            case 7: return 104f;
            case 8: return 120f;
            case 9: return 136f;
            case 10: return 152f;
            case 11: return 166f;
            case 12: return 176f;
            default: return 180f;
        }
    }

    private void ExecuteSettlementShowcaseBeat(int beat)
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;

        NeutralSettlement settlement = cinematicDirectorSettlementState.settlement;
        Vector3 settlementPosition = settlement.root.position;
        switch (beat)
        {
            case 0:
                ShowBanner("NO GRID // CARAVAN ECONOMY EXPOSED", 2.8f);
                lastEvent = settlement.displayName + " has no grid. Growth is slow and caravans are exposed.";
                break;
            case 1:
                PushLivingWorldEvent(LivingWorldEventType.Caravan, "Caravan leaves the outer route with settlement cargo.", 12f, false);
                lastEvent = "Caravan route visible. Red raiders are moving to intercept.";
                break;
            case 2:
                SpawnSettlementShowcaseRaiders(cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null ? cinematicDirectorSettlementCaravan.root.position : settlementPosition, 3, true);
                ShowBanner("RED RAIDERS HIT THE CARAVAN", 2.6f);
                break;
            case 3:
                FireAutocannons(false);
                CommandCinematicAttackWave(false);
                PushLivingWorldEvent(LivingWorldEventType.Raid, "The pyramid moves to defend the local caravan.", 12f, true);
                break;
            case 4:
                DamageSettlementShowcaseRaiders(210f);
                CreatePulseRing();
                cinematicDirectorSettlementState.defendedFromRaid = true;
                cinematicDirectorSettlementState.tradeTrust = Mathf.Min(100f, cinematicDirectorSettlementState.tradeTrust + 28f);
                cinematicDirectorSettlementState.playerInfluence = Mathf.Min(100f, cinematicDirectorSettlementState.playerInfluence + 26f);
                UpdateSettlementDiplomacy(cinematicDirectorSettlementState);
                ShowBanner("CARAVAN SAVED // TRUST RISING", 2.7f);
                break;
            case 5:
                CompleteSettlementTrade(cinematicDirectorSettlementState, true);
                lastEvent = "The saved caravan reaches the market. The settlement starts expanding.";
                break;
            case 6:
                BuildSettlementShowcaseGrid();
                break;
            case 7:
                CompleteSettlementTrade(cinematicDirectorSettlementState, true);
                CompleteSettlementTrade(cinematicDirectorSettlementState, false);
                ShowBanner("POWER ONLINE // GROWTH ACCELERATED", 2.8f);
                break;
            case 8:
                BuildSettlementShowcaseGoldenBase();
                ShowBanner("GOLDEN AIRBASE ONLINE // CARAVAN PATROLS", 3f);
                break;
            case 9:
                LaunchSettlementShowcaseAirPatrol();
                SpawnSettlementShowcaseRaiders(settlementPosition + settlement.root.right * 32f, 4, false);
                ShowBanner("AIR PATROL COVERS THE CARAVAN ROUTE", 2.8f);
                break;
            case 10:
                FireHowitzers(-1);
                FireHowitzers(1);
                FireSettlementShowcaseGuards();
                DamageSettlementShowcaseRaiders(260f);
                break;
            case 11:
                cinematicDirectorSettlementState.tradeTrust = 100f;
                cinematicDirectorSettlementState.playerInfluence = 100f;
                cinematicDirectorSettlementState.defendedFromRaid = true;
                UpdateSettlementDiplomacy(cinematicDirectorSettlementState);
                FireMissile(false);
                ShowBanner("ALLIED SETTLEMENT // RESOURCES, REPAIRS, DEFENSE", 3f);
                break;
            case 12:
                CreateBattleExplosionFx(settlementPosition + settlement.root.forward * 38f, 18f, false, true);
                PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, settlement.displayName + " now powers the front and supports the pyramid.", 14f, false);
                break;
        }
    }

    private void BuildSettlementShowcaseGrid()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;
        if (cinematicDirectorSettlementState.network != null)
            return;

        ResourceNode windSource = FindOrPrepareSettlementShowcaseWindNode(cinematicDirectorSettlementState.settlement.root.position);
        if (windSource == null)
        {
            lastEvent = "Settlement showcase could not find a wind node for mirror-grid connection.";
            return;
        }

        EnergyNetworkState network = CreateEnergyNetwork(windSource, cinematicDirectorSettlementState);
        cinematicDirectorSettlementState.network = network;
        cinematicDirectorSettlementState.playerInfluence = Mathf.Min(100f, cinematicDirectorSettlementState.playerInfluence + 42f);
        cinematicDirectorSettlementState.tradeTrust = Mathf.Min(100f, cinematicDirectorSettlementState.tradeTrust + 24f);
        energyNetworks.Add(network);
        UpdateSettlementDiplomacy(cinematicDirectorSettlementState);
        PlaySandRunnerSound(SandRunnerSound.Construction, cinematicDirectorSettlementState.settlement.root.position, 0.86f);
        PushLivingWorldEvent(LivingWorldEventType.EnergyNetwork, cinematicDirectorSettlementState.settlement.displayName + " connected to the mirror grid.", 12f, false);
        lastEvent = cinematicDirectorSettlementState.settlement.displayName + " has power. Growth, defense and pyramid support accelerate.";
        ShowBanner("MIRROR GRID ONLINE", 2.8f);
    }

    private void BuildSettlementShowcaseGoldenBase()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;

        Vector3 settlement = cinematicDirectorSettlementState.settlement.root.position;
        Vector3 forward = settlement - battlePyramid.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battlePyramid.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        CreateSettlementShowcaseBaseStructure(StructureKind.Aerodrome, settlement - forward * 28f + right * 34f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.Twin30mmTurret, settlement - forward * 10f - right * 28f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.Twin30mmTurret, settlement + forward * 18f + right * 26f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.MirrorBeamTurret, settlement + forward * 8f - right * 42f, forward);
        CreateSettlementShowcaseBaseStructure(StructureKind.GepardAALauncher, settlement - forward * 34f - right * 4f, forward);

        CompleteSettlementTrade(cinematicDirectorSettlementState, true);
        DeploySettlementTechnicalUnit(cinematicDirectorSettlementState);
        PlaySandRunnerSound(SandRunnerSound.Construction, settlement, 0.95f);
        PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, "Golden Elemental forward airbase built near " + cinematicDirectorSettlementState.settlement.displayName + ".", 14f, false);
        lastEvent = "Builders established a Golden Elemental airbase: aerodrome, towers and patrol coverage for caravans.";
    }

    private GoldenStructure CreateSettlementShowcaseBaseStructure(StructureKind kind, Vector3 position, Vector3 forward)
    {
        GoldenStructure structure = CreateGoldenStructure(kind, CinematicGround(position, 0.12f));
        if (structure != null && structure.transform != null)
        {
            structure.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            cinematicDirectorSettlementBaseStructures.Add(structure);
        }
        return structure;
    }

    private void LaunchSettlementShowcaseAirPatrol()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null)
            return;

        Vector3 settlement = cinematicDirectorSettlementState.settlement.root.position;
        Vector3 route = cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null
            ? cinematicDirectorSettlementCaravan.root.position - settlement
            : battlePyramid.position - settlement;
        route.y = 0f;
        if (route.sqrMagnitude < 0.01f)
            route = Vector3.forward;
        route.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, route).normalized;
        Vector3 airbase = GetSettlementShowcaseAirbasePosition(settlement);

        UnitSquad patrolA = SpawnCinematicSquad(ProductionKind.CombatFlyer, airbase + right * 8f, route, 9f, 7f);
        UnitSquad patrolB = SpawnCinematicSquad(ProductionKind.CombatFlyer, airbase - right * 8f - route * 8f, route, 9f, 7f);
        PositionSettlementShowcasePatrol(patrolA, settlement, route, right, 0);
        PositionSettlementShowcasePatrol(patrolB, settlement, route, right, 1);
        PlaySandRunnerSound(SandRunnerSound.HangarRelease, airbase, 0.95f);
        PushLivingWorldEvent(LivingWorldEventType.SettlementDevelopment, "Golden aircraft are patrolling the caravan roads.", 12f, false);
        lastEvent = "Aerodrome launched patrol aircraft. Caravans now have air cover.";
    }

    private Vector3 GetSettlementShowcaseAirbasePosition(Vector3 settlement)
    {
        for (int i = 0; i < cinematicDirectorSettlementBaseStructures.Count; i++)
        {
            GoldenStructure structure = cinematicDirectorSettlementBaseStructures[i];
            if (structure != null && structure.transform != null && structure.kind == StructureKind.Aerodrome)
                return structure.transform.position + Vector3.up * 6f;
        }
        return settlement + Vector3.up * 6f;
    }

    private void PositionSettlementShowcasePatrol(UnitSquad squad, Vector3 settlement, Vector3 route, Vector3 right, int lane)
    {
        if (squad == null)
            return;

        Vector3 patrolTarget = settlement + route * (lane == 0 ? 92f : 138f) + right * (lane == 0 ? 34f : -34f);
        for (int i = 0; i < squad.units.Count; i++)
        {
            RunnerUnit unit = squad.units[i];
            if (unit == null || unit.transform == null)
                continue;
            unit.hasOrder = true;
            unit.orderPosition = CinematicGround(patrolTarget + right * (i - squad.units.Count * 0.5f) * 9f, Mathf.Max(10f, unit.flightHeight));
            unit.autonomous = true;
        }
    }

    private void SpawnSettlementShowcaseRaiders(Vector3 focus, int count, bool targetCaravan)
    {
        Vector3 settlementPosition = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : focus;
        Vector3 direction = (focus - battlePyramid.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;
        direction.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

        for (int i = 0; i < count; i++)
        {
            int before = enemies.Count;
            Vector3 spawn = focus + direction * (32f + i * 8f) + right * ((i - (count - 1) * 0.5f) * 15f);
            SpawnJuzzherBarge(spawn);
            if (enemies.Count <= before)
                continue;

            EnemyUnit raider = enemies[enemies.Count - 1];
            if (raider == null || raider.transform == null)
                continue;
            raider.health = 260f;
            raider.maxHealth = 260f;
            raider.fireCooldown = 0.2f + i * 0.16f;
            raider.objectiveTimer = 999f;
            if (targetCaravan && cinematicDirectorSettlementCaravan != null)
                raider.factionObjective = cinematicDirectorSettlementCaravan.root;
            else if (cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.network != null && cinematicDirectorSettlementState.network.relays.Count > 0)
                raider.factionObjective = cinematicDirectorSettlementState.network.relays[Mathf.Clamp(i, 0, cinematicDirectorSettlementState.network.relays.Count - 1)];
            else if (cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null)
                raider.factionObjective = cinematicDirectorSettlementState.settlement.root;
            raider.transform.rotation = Quaternion.LookRotation((settlementPosition - raider.transform.position).normalized, Vector3.up);
            cinematicDirectorSettlementRaiders.Add(raider);
        }

        PlaySandRunnerSound(SandRunnerSound.JuzzherEngine, focus, 0.85f);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "Red Juzzher raiders are attacking local infrastructure.", 12f, true);
    }

    private void DamageSettlementShowcaseRaiders(float damage)
    {
        for (int i = cinematicDirectorSettlementRaiders.Count - 1; i >= 0; i--)
        {
            EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
            if (raider == null || raider.transform == null)
            {
                cinematicDirectorSettlementRaiders.RemoveAt(i);
                continue;
            }

            raider.health -= damage;
            CreateBattleExplosionFx(raider.transform.position, 9f, false, true);
            if (raider.health <= 0f)
            {
                enemies.Remove(raider);
                Destroy(raider.transform.gameObject, 0.1f);
                cinematicDirectorSettlementRaiders.RemoveAt(i);
            }
        }
    }

    private void DriveSettlementShowcasePyramid(float dt)
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null || cinematicDirectorSettlementState.settlement.root == null || battlePyramid == null)
            return;

        Vector3 settlement = cinematicDirectorSettlementState.settlement.root.position;
        Vector3 routeForward = (settlement - battlePyramid.position);
        routeForward.y = 0f;
        if (routeForward.sqrMagnitude < 0.01f)
            routeForward = battlePyramid.forward;
        routeForward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, routeForward).normalized;

        Vector3 target;
        if (cinematicDirectorTimer < 22f)
            target = settlement - routeForward * 128f;
        else if (cinematicDirectorTimer < 58f && cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null)
            target = cinematicDirectorSettlementCaravan.root.position - routeForward * 22f;
        else if (cinematicDirectorTimer < 104f)
            target = settlement - routeForward * 48f;
        else if (cinematicDirectorTimer < 142f)
            target = settlement - routeForward * 34f + right * 18f;
        else
            target = settlement - routeForward * 30f;

        target = CinematicGround(target, pyramidGroundClearance);
        Vector3 delta = target - battlePyramid.position;
        delta.y = 0f;
        if (delta.magnitude > 2f)
        {
            float speed = cinematicDirectorTimer < 58f ? 18f : 10f;
            Vector3 move = delta.normalized * Mathf.Min(delta.magnitude, speed * dt);
            battlePyramid.position += move;
            battlePyramid.rotation = Quaternion.Slerp(battlePyramid.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), dt * 2.5f);
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.72f, dt * 1.6f);
        }
        else
        {
            battlePyramid.rotation = Quaternion.Slerp(battlePyramid.rotation, Quaternion.LookRotation(routeForward, Vector3.up), dt * 1.8f);
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.25f, dt * 1.6f);
        }
    }

    private void UpdateSettlementShowcaseCaravan(float dt)
    {
        if (cinematicDirectorSettlementCaravan == null || cinematicDirectorSettlementCaravan.root == null || cinematicDirectorSettlementCaravan.destination == null || cinematicDirectorSettlementCaravan.destination.root == null)
            return;

        Vector3 destination = cinematicDirectorSettlementCaravan.destination.root.position + cinematicDirectorSettlementCaravan.destination.root.right * 10f;
        Vector3 toDestination = destination - cinematicDirectorSettlementCaravan.root.position;
        toDestination.y = 0f;
        if (toDestination.magnitude > 10f)
        {
            float speed = cinematicDirectorTimer < 60f ? 5.5f : 10.5f;
            cinematicDirectorSettlementCaravan.root.position += toDestination.normalized * speed * dt;
            Vector3 position = cinematicDirectorSettlementCaravan.root.position;
            position.y = GetPlayableGroundHeight(position) + 1.05f;
            cinematicDirectorSettlementCaravan.root.position = position;
            RotateToward(cinematicDirectorSettlementCaravan.root, toDestination, 95f * dt);
        }

        if (cinematicDirectorSettlementCaravan.routeLine != null)
        {
            cinematicDirectorSettlementCaravan.routeLine.SetPosition(0, cinematicDirectorSettlementCaravan.root.position + Vector3.up * 0.3f);
            cinematicDirectorSettlementCaravan.routeLine.SetPosition(1, destination + Vector3.up * 0.3f);
        }
    }

    private void UpdateSettlementShowcaseRaiders(float dt)
    {
        for (int i = cinematicDirectorSettlementRaiders.Count - 1; i >= 0; i--)
        {
            EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
            if (raider == null || raider.transform == null || raider.health <= 0f)
            {
                cinematicDirectorSettlementRaiders.RemoveAt(i);
                continue;
            }

            Transform objective = raider.factionObjective;
            if (objective == null && cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null)
                objective = cinematicDirectorSettlementState.settlement.root;
            if (objective == null)
                continue;

            Vector3 toObjective = objective.position - raider.transform.position;
            float distance = toObjective.magnitude;
            if (distance > 16f)
            {
                raider.transform.position += toObjective.normalized * 12f * dt;
                RotateToward(raider.transform, toObjective, 120f * dt);
            }

            raider.fireCooldown -= dt;
            if (raider.fireCooldown <= 0f)
            {
                raider.fireCooldown = 0.58f;
                CreateBeam(raider.transform.position + Vector3.up * 1.4f, objective.position + Vector3.up * 1.4f, new Color(1f, 0.12f, 0.04f, 1f), 0.07f, 0.16f);
                if (cinematicDirectorSettlementCaravan != null && objective == cinematicDirectorSettlementCaravan.root)
                    cinematicDirectorSettlementCaravan.health = Mathf.Max(35f, cinematicDirectorSettlementCaravan.health - 8f);
            }
        }
    }

    private void UpdateSettlementShowcaseScriptedEffects(float dt)
    {
        cinematicDirectorSettlementEffectTimer -= dt;
        if (cinematicDirectorSettlementEffectTimer > 0f)
            return;

        cinematicDirectorSettlementEffectTimer = 0.38f;

        if (cinematicDirectorTimer >= 24f && cinematicDirectorTimer < 58f)
        {
            if (cinematicDirectorSettlementRaiders.Count == 0 && cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null)
                SpawnSettlementShowcaseRaiders(cinematicDirectorSettlementCaravan.root.position, 3, true);

            PaintSettlementShowcaseCaravanRaid();
            if (cinematicDirectorTimer >= 36f)
                PaintPyramidCaravanDefense();
        }

        if (cinematicDirectorTimer >= 74f && cinematicDirectorTimer < 116f)
            PaintSettlementShowcaseGridPulse();

        if (cinematicDirectorTimer >= 104f && cinematicDirectorTimer < 138f)
        {
            PaintSettlementShowcaseBaseConstruction();
            PaintSettlementShowcaseAirPatrol();
        }

        if (cinematicDirectorTimer >= 136f && cinematicDirectorTimer < 174f)
        {
            if (cinematicDirectorSettlementRaiders.Count == 0 && cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null)
                SpawnSettlementShowcaseRaiders(cinematicDirectorSettlementState.settlement.root.position + cinematicDirectorSettlementState.settlement.root.right * 28f, 4, false);

            PaintSettlementShowcaseGridRaid();
            PaintSettlementShowcaseAirPatrol();
            PaintPyramidCaravanDefense();
            FireSettlementShowcaseGuards();
        }
    }

    private void PaintSettlementShowcaseCaravanRaid()
    {
        if (cinematicDirectorSettlementCaravan == null || cinematicDirectorSettlementCaravan.root == null)
            return;

        Vector3 caravan = cinematicDirectorSettlementCaravan.root.position;
        for (int i = 0; i < cinematicDirectorSettlementRaiders.Count; i++)
        {
            EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
            if (raider == null || raider.transform == null)
                continue;
            CreateBeam(raider.transform.position + Vector3.up * 1.4f, caravan + Vector3.up * 1.6f, new Color(1f, 0.08f, 0.03f, 1f), 0.075f, 0.22f);
            if (i == 0)
                CreateBattleExplosionFx(caravan + UnityEngine.Random.insideUnitSphere * 3f, 4f, false, true);
        }
    }

    private void PaintPyramidCaravanDefense()
    {
        Vector3 muzzle = battlePyramid != null ? battlePyramid.position + Vector3.up * 5.5f + battlePyramid.forward * 10f : Vector3.up * 5f;
        for (int i = 0; i < cinematicDirectorSettlementRaiders.Count; i++)
        {
            EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
            if (raider == null || raider.transform == null)
                continue;
            CreateBeam(muzzle, raider.transform.position + Vector3.up * 1.4f, new Color(1f, 0.72f, 0.18f, 1f), 0.065f, 0.22f);
            raider.health -= 12f;
            if (raider.health <= 0f)
                CreateBattleExplosionFx(raider.transform.position, 8f, false, true);
            break;
        }
    }

    private void PaintSettlementShowcaseGridPulse()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.network == null)
            return;

        EnergyNetworkState network = cinematicDirectorSettlementState.network;
        Vector3 previous = network.source != null && network.source.transform != null ? network.source.transform.position + Vector3.up * 4f : battlePyramid.position + Vector3.up * 5f;
        for (int i = 0; i < network.relays.Count; i++)
        {
            Transform relay = network.relays[i];
            if (relay == null)
                continue;
            Vector3 relayTop = relay.position + Vector3.up * 4.4f;
            CreateBeam(previous, relayTop, new Color(0.35f, 0.95f, 1f, 1f), 0.055f, 0.24f);
            previous = relayTop;
        }
        if (cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null)
            CreateBeam(previous, cinematicDirectorSettlementState.settlement.root.position + Vector3.up * 6f, new Color(0.55f, 1f, 1f, 1f), 0.065f, 0.24f);
    }

    private void PaintSettlementShowcaseGridRaid()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.network == null || cinematicDirectorSettlementState.network.relays.Count == 0)
            return;

        for (int i = 0; i < cinematicDirectorSettlementRaiders.Count; i++)
        {
            EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
            if (raider == null || raider.transform == null)
                continue;

            Transform relay = cinematicDirectorSettlementState.network.relays[Mathf.Clamp(i % cinematicDirectorSettlementState.network.relays.Count, 0, cinematicDirectorSettlementState.network.relays.Count - 1)];
            if (relay == null)
                continue;
            CreateBeam(raider.transform.position + Vector3.up * 1.6f, relay.position + Vector3.up * 4f, new Color(1f, 0.1f, 0.03f, 1f), 0.07f, 0.18f);
            break;
        }
    }

    private void PaintSettlementShowcaseBaseConstruction()
    {
        if (cinematicDirectorSettlementBaseStructures.Count == 0)
            return;

        for (int i = 0; i < cinematicDirectorSettlementBaseStructures.Count; i++)
        {
            GoldenStructure structure = cinematicDirectorSettlementBaseStructures[i];
            if (structure == null || structure.transform == null)
                continue;

            Transform builder = GetCinematicSquadRepresentative(0);
            Vector3 start = builder != null ? builder.position + Vector3.up * 1.6f : battlePyramid.position + Vector3.up * 4f;
            CreateBeam(start, structure.transform.position + Vector3.up * 1.2f, new Color(1f, 0.78f, 0.24f, 1f), 0.045f, 0.18f);
            if (i == 0)
                CreateBattleExplosionFx(structure.transform.position, 4.5f, false, false);
        }
    }

    private void PaintSettlementShowcaseAirPatrol()
    {
        if (cinematicDirectorGoldenSquads.Count == 0)
            return;

        for (int squadIndex = 0; squadIndex < cinematicDirectorGoldenSquads.Count; squadIndex++)
        {
            UnitSquad squad = cinematicDirectorGoldenSquads[squadIndex];
            if (squad == null)
                continue;
            for (int i = 0; i < squad.units.Count; i++)
            {
                RunnerUnit unit = squad.units[i];
                if (unit == null || unit.transform == null || !unit.airborne)
                    continue;

                Vector3 patrolPoint = unit.transform.position + unit.transform.forward * 18f + Vector3.up * 0.5f;
                CreateBeam(unit.transform.position, patrolPoint, new Color(0.42f, 0.9f, 1f, 1f), 0.035f, 0.16f);
                EnemyUnit target = FindNearestEnemy(unit.transform.position, 120f);
                if (target != null && target.transform != null)
                {
                    CreateBeam(unit.transform.position, target.transform.position + Vector3.up * 1.2f, new Color(1f, 0.74f, 0.18f, 1f), 0.045f, 0.18f);
                    target.health -= 16f;
                }
                return;
            }
        }
    }

    private void FireSettlementShowcaseGuards()
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.settlement == null)
            return;

        for (int guardIndex = 0; guardIndex < cinematicDirectorSettlementState.settlement.guards.Count; guardIndex++)
        {
            Transform guard = cinematicDirectorSettlementState.settlement.guards[guardIndex];
            if (guard == null)
                continue;
            for (int i = 0; i < cinematicDirectorSettlementRaiders.Count; i++)
            {
                EnemyUnit raider = cinematicDirectorSettlementRaiders[i];
                if (raider == null || raider.transform == null)
                    continue;
                if (FlatDistance(guard.position, raider.transform.position) > 130f)
                    continue;
                CreateBeam(guard.position + Vector3.up * 1.4f, raider.transform.position + Vector3.up * 1.2f, new Color(0.25f, 0.72f, 1f, 1f), 0.055f, 0.18f);
                raider.health -= 45f;
                break;
            }
        }

        if (cinematicDirectorSettlementState.network != null && cinematicDirectorSettlementState.network.beamLine != null)
            cinematicDirectorSettlementState.network.beamLine.startWidth = 0.42f;
    }

    private UnitSquad SpawnCinematicSquad(ProductionKind kind, Vector3 anchor, Vector3 forward, float lateralSpacing, float depthSpacing)
    {
        ProductionJob job = CreateProductionJob(kind);
        if (job == null)
            return null;

        UnitSquad squad = SpawnProductionSquad(job, anchor + forward * 24f);
        PositionCinematicSquad(squad, anchor, forward, lateralSpacing, depthSpacing);
        if (squad != null)
            cinematicDirectorGoldenSquads.Add(squad);
        return squad;
    }

    private void PositionCinematicSquad(UnitSquad squad, Vector3 anchor, Vector3 forward, float lateralSpacing, float depthSpacing)
    {
        if (squad == null)
            return;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        int memberCount = CountCinematicSquadMembers(squad);
        int memberIndex = 0;

        if (squad.builder != null && squad.builder.transform != null)
        {
            Vector3 position = GetCinematicFormationPosition(anchor, forward, right, memberIndex, memberCount, lateralSpacing, depthSpacing, false, 0f);
            PlaceCinematicTransform(squad.builder.transform, position, forward);
            memberIndex++;
        }

        for (int i = 0; i < squad.units.Count; i++)
        {
            RunnerUnit unit = squad.units[i];
            if (unit == null || unit.transform == null)
                continue;
            Vector3 position = GetCinematicFormationPosition(anchor, forward, right, memberIndex, memberCount, lateralSpacing, depthSpacing, unit.airborne, unit.flightHeight);
            PlaceCinematicTransform(unit.transform, position, forward);
            unit.hasOrder = true;
            unit.orderPosition = anchor + forward * 90f + right * ((memberIndex % 3) - 1) * lateralSpacing;
            memberIndex++;
        }
    }

    private int CountCinematicSquadMembers(UnitSquad squad)
    {
        if (squad == null)
            return 0;

        int count = squad.builder != null && squad.builder.transform != null ? 1 : 0;
        for (int i = 0; i < squad.units.Count; i++)
        {
            if (squad.units[i] != null && squad.units[i].transform != null)
                count++;
        }
        return Mathf.Max(1, count);
    }

    private Vector3 GetCinematicFormationPosition(Vector3 anchor, Vector3 forward, Vector3 right, int index, int count, float lateralSpacing, float depthSpacing, bool airborne, float flightHeight)
    {
        int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count)), 1, 5);
        int row = index / columns;
        int column = index % columns;
        float centeredColumn = column - (Mathf.Min(count, columns) - 1) * 0.5f;
        Vector3 position = anchor + right * (centeredColumn * lateralSpacing) - forward * (row * depthSpacing);
        float lift = airborne ? Mathf.Max(6f, flightHeight) : 0.12f;
        return CinematicGround(position, lift);
    }

    private void PlaceCinematicTransform(Transform target, Vector3 position, Vector3 forward)
    {
        if (target == null)
            return;
        target.position = position;
        if (forward.sqrMagnitude > 0.01f)
            target.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private MandarinkaAsset SpawnCinematicMandarinkaAsset(MandarinkaRole role, Vector3 anchor, Vector3 forward)
    {
        int before = mandarinkaAssets.Count;
        if (role == MandarinkaRole.GroundCrawler)
            SpawnMandarinkaGroundCrawler();
        else if (role == MandarinkaRole.AirJunk)
            SpawnMandarinkaAirJunk();
        else if (role == MandarinkaRole.Builder)
            SpawnMandarinkaBuilder();
        else if (role == MandarinkaRole.Gustav)
            SpawnMandarinkaGustav();
        else if (role == MandarinkaRole.FieldTurret)
            SpawnMandarinkaTurret(anchor);

        if (mandarinkaAssets.Count <= before)
            return null;

        MandarinkaAsset asset = mandarinkaAssets[mandarinkaAssets.Count - 1];
        if (asset == null || asset.transform == null)
            return null;

        float lift = role == MandarinkaRole.AirJunk ? 10f : 0.2f;
        PlaceCinematicTransform(asset.transform, CinematicGround(anchor, lift), forward);
        asset.objective = battlePyramid != null ? battlePyramid.position + Vector3.up * lift : anchor;
        asset.hasObjective = true;
        asset.fireTimer = Mathf.Min(asset.fireTimer, role == MandarinkaRole.Gustav ? 4.5f : 1.5f);
        cinematicDirectorHostileAssets.Add(asset);
        return asset;
    }

    private void OrderCinematicParadeMarch()
    {
        Vector3 forward = CinematicBattleForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        for (int i = 0; i < cinematicDirectorGoldenSquads.Count; i++)
        {
            UnitSquad squad = cinematicDirectorGoldenSquads[i];
            if (squad == null)
                continue;
            Vector3 target = battlePyramid.position + forward * (120f + (i % 4) * 16f) + right * ((i - cinematicDirectorGoldenSquads.Count * 0.5f) * 8f);
            IssueSquadMoveOrder(squad, target, i);
        }
    }

    private void CommandCinematicAttackWave(bool allIn)
    {
        EnemyUnit fortress = mandarinkaFortressEnemy;
        for (int i = 0; i < cinematicDirectorGoldenSquads.Count; i++)
        {
            UnitSquad squad = cinematicDirectorGoldenSquads[i];
            if (squad == null)
                continue;

            EnemyUnit target = FindNearestEnemy(GetCinematicSquadPosition(squad), allIn && fortress != null ? 900f : 170f);
            if (allIn && fortress != null)
                target = fortress;
            if (target != null)
                IssueSquadAttackOrder(squad, target);
        }
    }

    private Vector3 GetCinematicSquadPosition(UnitSquad squad)
    {
        if (TryGetSquadCentroid(squad, out Vector3 centroid))
            return centroid;
        Transform representative = GetSquadRepresentative(squad);
        return representative != null ? representative.position : battlePyramid != null ? battlePyramid.position : Vector3.zero;
    }

    private void RunCinematicDirectorBeats()
    {
        while (cinematicDirectorBeatIndex < 17 && cinematicDirectorTimer >= GetCinematicDirectorBeatTime(cinematicDirectorBeatIndex))
        {
            ExecuteCinematicDirectorBeat(cinematicDirectorBeatIndex);
            cinematicDirectorBeatIndex++;
        }
    }

    private float GetCinematicDirectorBeatTime(int beat)
    {
        switch (beat)
        {
            case 0: return 0.1f;
            case 1: return 8f;
            case 2: return 18f;
            case 3: return 28f;
            case 4: return 39f;
            case 5: return 52f;
            case 6: return 66f;
            case 7: return 78f;
            case 8: return 92f;
            case 9: return 106f;
            case 10: return 120f;
            case 11: return 134f;
            case 12: return 146f;
            case 13: return 158f;
            case 14: return 168f;
            case 15: return 176f;
            case 16: return 180f;
            default: return 999f;
        }
    }

    private void ExecuteCinematicDirectorBeat(int beat)
    {
        Vector3 forward = CinematicBattleForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = CinematicBattleCenter();

        switch (beat)
        {
            case 0:
                CreateBattleExplosionFx(center + right * 18f, 10f, false, true);
                PlaySandRunnerSound(SandRunnerSound.HangarRelease, battlePyramid.position, 1f);
                break;
            case 1:
                OrderCinematicParadeMarch();
                CreateBattleExplosionFx(center - right * 32f, 12f, false, true);
                break;
            case 2:
                SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, center + forward * 130f - right * 60f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, center + forward * 136f + right * 60f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, center + forward * 120f, -forward);
                CreateBattleExplosionFx(center + forward * 46f, 15f, false, true);
                FireAutocannons(false);
                break;
            case 3:
                CommandCinematicAttackWave(false);
                FireCinematicHostileBarrage(center, 4);
                FireHowitzers(-1);
                break;
            case 4:
                FireMissile(false);
                FireMissile(false);
                FireCinematicHostileBarrage(center + forward * 20f, 5);
                CreateBattleExplosionFx(center - forward * 12f, 16f, false, false);
                FireHowitzers(1);
                break;
            case 5:
                SpawnCinematicSquad(ProductionKind.FortressCrusher, battlePyramid.position - forward * 58f - right * 72f, forward, 10f, 8f);
                SpawnCinematicSquad(ProductionKind.HeavyFlyer, battlePyramid.position - forward * 36f + right * 72f, forward, 12f, 9f);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.Gustav, center + forward * 142f, -forward);
                CommandCinematicAttackWave(true);
                break;
            case 6:
                FireMissile(true);
                beamCharge = 1f;
                beamCooldownTimer = 0f;
                FireChargedBeam();
                CreateBattleExplosionFx(center + forward * 44f, 22f, true, false);
                break;
            case 7:
                CommandCinematicAttackWave(true);
                for (int i = 0; i < 6; i++)
                    CreateBattleExplosionFx(center + right * UnityEngine.Random.Range(-90f, 90f) + forward * UnityEngine.Random.Range(-70f, 90f), UnityEngine.Random.Range(9f, 18f), false, UnityEngine.Random.value > 0.45f);
                SetMandarinkaRadio("MANDARINKA: This is not a battle line. This is a golden weather system.");
                break;
            case 8:
                SpawnCinematicSquad(ProductionKind.BuilderTruck, battlePyramid.position - forward * 44f + right * 82f, forward, 8f, 7f);
                SpawnCinematicSquad(ProductionKind.ScarabTank, battlePyramid.position - forward * 64f - right * 92f, forward, 8f, 7f);
                CreateGoldenStructure(StructureKind.MirrorBeamTurret, CinematicGround(battlePyramid.position + forward * 126f + right * 54f, 0.12f));
                CreateGoldenStructure(StructureKind.GepardAALauncher, CinematicGround(battlePyramid.position + forward * 136f - right * 54f, 0.12f));
                FireAutocannons(false);
                break;
            case 9:
                SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, center + forward * 145f - right * 70f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, center + forward * 150f + right * 70f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.FieldTurret, center + forward * 92f + right * 96f, -forward);
                FireCinematicHostileBarrage(center, 6);
                FireMissile(false);
                break;
            case 10:
                SpawnCinematicSquad(ProductionKind.ThothEmbrace, battlePyramid.position - forward * 72f, forward, 14f, 10f);
                SpawnCinematicSquad(ProductionKind.WrathOfRa, battlePyramid.position - forward * 58f + right * 34f, forward, 12f, 9f);
                FireHowitzers(-1);
                FireHowitzers(1);
                CommandCinematicAttackWave(true);
                break;
            case 11:
                SpawnCinematicMandarinkaAsset(MandarinkaRole.Gustav, center + forward * 158f - right * 34f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, center + forward * 150f + right * 110f, -forward);
                SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, center + forward * 160f - right * 110f, -forward);
                FireCinematicHostileBarrage(center + forward * 16f, 8);
                break;
            case 12:
                pulseDamage *= 1.05f;
                CreatePulseRing();
                DamageEnemiesAlongLine(battlePyramid.position + Vector3.up * 3f, battlePyramid.position + forward * 120f + Vector3.up * 3f, 22f, 120f);
                FireMissile(false);
                break;
            case 13:
                beamCharge = 1f;
                beamCooldownTimer = 0f;
                FireChargedBeam();
                FireMissile(true);
                CreateBattleExplosionFx(center + forward * 68f, 24f, true, false);
                break;
            case 14:
                SpawnCinematicSquad(ProductionKind.FortressCrusher, battlePyramid.position - forward * 72f + right * 86f, forward, 11f, 8f);
                SpawnCinematicSquad(ProductionKind.HeavyFlyer, battlePyramid.position - forward * 42f - right * 86f, forward, 12f, 9f);
                CommandCinematicAttackWave(true);
                break;
            case 15:
                for (int i = 0; i < 10; i++)
                    CreateBattleExplosionFx(center + right * UnityEngine.Random.Range(-130f, 130f) + forward * UnityEngine.Random.Range(-85f, 115f), UnityEngine.Random.Range(10f, 24f), i % 4 == 0, UnityEngine.Random.value > 0.5f);
                SetMandarinkaRadio("SEBEK: All guns forward. Show them what a moving pyramid means.");
                break;
            case 16:
                cinematicDirectorShotLabel = "Final showcase hold";
                break;
        }
    }

    private void FireCinematicHostileBarrage(Vector3 targetCenter, int count)
    {
        if (mandarinkaFortressRoot == null)
            return;

        Vector3 forward = CinematicBattleForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        for (int i = 0; i < count; i++)
        {
            Vector3 start = mandarinkaFortressRoot.position + Vector3.up * UnityEngine.Random.Range(8f, 15f) + right * UnityEngine.Random.Range(-28f, 28f);
            Vector3 target = targetCenter + right * UnityEngine.Random.Range(-52f, 52f) + forward * UnityEngine.Random.Range(-30f, 40f);
            target = CinematicGround(target, 0.2f);
            CreateHostileShell("Cinematic_Mandarinka_Barrage_Shell", start, target, 65f, UnityEngine.Random.Range(9f, 15f), false, false, UnityEngine.Random.Range(1.35f, 2.05f), UnityEngine.Random.Range(36f, 56f));
        }
    }

    private void DriveCinematicDirectorPyramid(float dt)
    {
        if (battlePyramid == null)
            return;

        Vector3 forward = CinematicBattleForward();
        if (cinematicDirectorTimer < cinematicDirectorRequestedDuration - 14f)
        {
            float driveSpeed = cinematicDirectorRequestedDuration > CinematicDirectorDefaultDuration + 1f
                ? Mathf.Lerp(1.35f, 2.2f, Mathf.PingPong(cinematicDirectorTimer * 0.035f, 1f))
                : 1.55f;
            battlePyramid.position += forward * (driveSpeed * dt);
            Vector3 position = battlePyramid.position;
            position.y = GetPlayableGroundHeight(position) + pyramidGroundClearance;
            battlePyramid.position = position;
            battlePyramid.rotation = Quaternion.RotateTowards(battlePyramid.rotation, Quaternion.LookRotation(forward, Vector3.up), 12f * dt);
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.85f, dt * 1.2f);
        }
        else
        {
            pyramidThrottleBlend = Mathf.Lerp(pyramidThrottleBlend, 0.35f, dt * 1.1f);
        }
    }

    private void UpdateCinematicDirectorExtendedBattle(float dt)
    {
        if (cinematicDirectorTimer < CinematicDirectorDefaultDuration)
            return;

        cinematicDirectorExtendedSpawnTimer -= dt;
        if (cinematicDirectorExtendedSpawnTimer > 0f)
            return;

        cinematicDirectorExtendedSpawnTimer = 15f;
        Vector3 forward = CinematicBattleForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = CinematicBattleCenter();
        SpawnCinematicSquad(ProductionKind.ScarabTank, battlePyramid.position - forward * 52f + right * UnityEngine.Random.Range(-90f, 90f), forward, 8f, 7f);
        SpawnCinematicMandarinkaAsset(MandarinkaRole.GroundCrawler, center + forward * 150f + right * UnityEngine.Random.Range(-110f, 110f), -forward);
        if (UnityEngine.Random.value > 0.45f)
            SpawnCinematicMandarinkaAsset(MandarinkaRole.AirJunk, center + forward * 130f + right * UnityEngine.Random.Range(-80f, 80f), -forward);
        CommandCinematicAttackWave(true);
        FireCinematicHostileBarrage(center, 3);
    }

    private void UpdateCinematicDirectorCamera(float dt)
    {
        Vector3 desiredFocus;
        float desiredYaw;
        float desiredPitch;
        float desiredDistance;
        float desiredFov;
        EvaluateCinematicDirectorShot(out desiredFocus, out desiredYaw, out desiredPitch, out desiredDistance, out desiredFov);
        int shotIndex = GetCinematicDirectorShotIndex();

        if (shotIndex != cinematicDirectorLastShotIndex)
        {
            cinematicDirectorLastShotIndex = shotIndex;
            cameraYaw = desiredYaw;
            cameraPitch = desiredPitch;
            cameraDistance = desiredDistance;
            cinematicDirectorCurrentFov = desiredFov;
            cinematicDirectorSmoothedFov = desiredFov;
            cinematicDirectorSmoothedFocus = desiredFocus;
            cinematicDirectorFocusVelocity = Vector3.zero;
            if (mainCamera != null)
            {
                Quaternion cutOrbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
                Vector3 cutPosition = desiredFocus + cutOrbit * new Vector3(0f, 0f, -cameraDistance);
                mainCamera.transform.position = cutPosition;
                mainCamera.transform.rotation = Quaternion.LookRotation(desiredFocus - cutPosition, Vector3.up);
                mainCamera.fieldOfView = desiredFov;
            }
            return;
        }

        cameraYaw = Mathf.LerpAngle(cameraYaw, desiredYaw, dt * 1.45f);
        cameraPitch = Mathf.Lerp(cameraPitch, desiredPitch, dt * 1.7f);
        cameraDistance = Mathf.Lerp(cameraDistance, desiredDistance, dt * 1.55f);
        cinematicDirectorCurrentFov = desiredFov;
        cinematicDirectorSmoothedFov = Mathf.Lerp(cinematicDirectorSmoothedFov, desiredFov, dt * 2.2f);
        cinematicDirectorSmoothedFocus = Vector3.SmoothDamp(cinematicDirectorSmoothedFocus, desiredFocus, ref cinematicDirectorFocusVelocity, 0.42f, 240f, dt);
    }

    private Vector3 GetCinematicDirectorCameraFocus(float dt)
    {
        if (!cinematicDirectorActive)
            return battlePyramid != null ? battlePyramid.position : Vector3.zero;
        return cinematicDirectorSmoothedFocus;
    }

    private float GetCinematicDirectorFieldOfView()
    {
        return cinematicDirectorActive ? cinematicDirectorSmoothedFov : cinematicDirectorCurrentFov;
    }

    private void EvaluateCinematicDirectorShot(out Vector3 focus, out float yaw, out float pitch, out float distance, out float fov)
    {
        if (cinematicDirectorOverviewMode)
        {
            EvaluateOverviewTrailerShot(out focus, out yaw, out pitch, out distance, out fov);
            return;
        }

        if (cinematicDirectorSettlementMode)
        {
            EvaluateSettlementShowcaseShot(out focus, out yaw, out pitch, out distance, out fov);
            return;
        }

        float t = cinematicDirectorTimer;
        Vector3 forward = CinematicBattleForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = CinematicBattleCenter();
        float battleYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        int shot = GetCinematicDirectorShotIndex();
        Transform shoulderTarget;

        switch (shot)
        {
            case 0:
                shoulderTarget = GetCinematicSquadRepresentative(0);
                MakeShoulderShot(shoulderTarget, battlePyramid.position - forward * 26f, forward, 1.7f, 7.2f, 36f, "Scarab shoulder parade", out focus, out yaw, out pitch, out distance, out fov);
                return;
            case 1:
                focus = battlePyramid.position + Vector3.up * 10f + forward * 22f;
                yaw = battleYaw + 34f;
                pitch = 18f;
                distance = 58f;
                fov = 47f;
                cinematicDirectorShotLabel = "Pyramid rolling fortress";
                return;
            case 2:
                shoulderTarget = GetCinematicSquadRepresentative(4);
                MakeShoulderShot(shoulderTarget, battlePyramid.position - forward * 30f + right * 34f, forward, 2.2f, 9.5f, 38f, "Fortress crusher shoulder", out focus, out yaw, out pitch, out distance, out fov);
                return;
            case 3:
                focus = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position + Vector3.up * 8f : center + forward * 120f;
                yaw = battleYaw + 184f;
                pitch = 21f;
                distance = 76f;
                fov = 48f;
                cinematicDirectorShotLabel = "Mandarinka fortress reveal";
                return;
            case 4:
                shoulderTarget = GetCinematicSquadRepresentative(8);
                MakeShoulderShot(shoulderTarget, battlePyramid.position + forward * 75f - right * 18f, forward, 5.8f, 16f, 55f, "Flyer wing pass", out focus, out yaw, out pitch, out distance, out fov);
                return;
            case 5:
                focus = center + Vector3.up * 3.8f - right * 22f;
                yaw = battleYaw - 58f;
                pitch = 10f;
                distance = 34f;
                fov = 43f;
                cinematicDirectorShotLabel = "First contact low angle";
                return;
            case 6:
                focus = battlePyramid.position + Vector3.up * 6f + forward * 38f;
                yaw = battleYaw - 148f;
                pitch = 9f;
                distance = 30f;
                fov = 40f;
                cinematicDirectorShotLabel = "Pyramid gun deck";
                return;
            case 7:
                focus = GetCinematicGustavFocus(center + forward * 82f) + Vector3.up * 2.8f;
                yaw = battleYaw + 128f;
                pitch = 11f;
                distance = 34f;
                fov = 42f;
                cinematicDirectorShotLabel = "Karl Gustav siege gun";
                return;
            case 8:
                focus = center + Vector3.up * 18f;
                yaw = battleYaw + 28f;
                pitch = 27f;
                distance = 126f;
                fov = 61f;
                cinematicDirectorShotLabel = "Whole battlefield";
                return;
            case 9:
                shoulderTarget = GetCinematicSquadRepresentative(5);
                MakeShoulderShot(shoulderTarget, battlePyramid.position - forward * 22f - right * 52f, forward, 2.0f, 8.5f, 38f, "Wrath platform push", out focus, out yaw, out pitch, out distance, out fov);
                return;
            case 10:
                focus = battlePyramid.position + Vector3.up * 18f + forward * 8f;
                yaw = battleYaw + 8f;
                pitch = 34f;
                distance = 82f;
                fov = 53f;
                cinematicDirectorShotLabel = "Factory and hangar scale";
                return;
            case 11:
                focus = center + Vector3.up * 5f + right * 28f;
                yaw = battleYaw - 72f;
                pitch = 12f;
                distance = 46f;
                fov = 45f;
                cinematicDirectorShotLabel = "Missile crossing";
                return;
            case 12:
                focus = GetCinematicGustavFocus(center + forward * 95f) + Vector3.up * 3.5f;
                yaw = battleYaw + 162f;
                pitch = 13f;
                distance = 38f;
                fov = 44f;
                cinematicDirectorShotLabel = "Siege counterfire";
                return;
            case 13:
                focus = center + Vector3.up * 10f;
                yaw = battleYaw + 230f;
                pitch = 23f;
                distance = 92f;
                fov = 58f;
                cinematicDirectorShotLabel = "Turret line and air war";
                return;
            case 14:
                shoulderTarget = GetCinematicSquadRepresentative(10);
                MakeShoulderShot(shoulderTarget, battlePyramid.position + forward * 52f, forward, 6.8f, 18f, 56f, "Heavy flyer escort", out focus, out yaw, out pitch, out distance, out fov);
                return;
            case 15:
                focus = battlePyramid.position + Vector3.up * 13f + forward * 20f;
                yaw = battleYaw - 25f;
                pitch = 18f;
                distance = 64f;
                fov = 48f;
                cinematicDirectorShotLabel = "Pyramid broadside";
                return;
            case 16:
                focus = center + Vector3.up * 5f - right * 36f;
                yaw = battleYaw - 104f;
                pitch = 12f;
                distance = 42f;
                fov = 43f;
                cinematicDirectorShotLabel = "Ground melee close cut";
                return;
            case 17:
                focus = battlePyramid.position + Vector3.up * 7f + forward * 55f;
                yaw = battleYaw + 180f;
                pitch = 12f;
                distance = 38f;
                fov = 41f;
                cinematicDirectorShotLabel = "Over-shoulder pyramid charge";
                return;
            case 18:
                focus = center + Vector3.up * 22f;
                yaw = battleYaw + 54f;
                pitch = 31f;
                distance = 148f;
                fov = 63f;
                cinematicDirectorShotLabel = "Sun-core overview";
                return;
            case 19:
                focus = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position + Vector3.up * 9f : center + forward * 120f;
                yaw = battleYaw + 205f;
                pitch = 19f;
                distance = 68f;
                fov = 48f;
                cinematicDirectorShotLabel = "Shielded fortress under fire";
                return;
            case 20:
                focus = center + Vector3.up * 7f + right * 42f;
                yaw = battleYaw - 68f;
                pitch = 16f;
                distance = 58f;
                fov = 50f;
                cinematicDirectorShotLabel = "Reinforcement wave";
                return;
            case 21:
                focus = battlePyramid.position + Vector3.up * 14f + forward * 34f;
                yaw = battleYaw + 36f;
                pitch = 21f;
                distance = 74f;
                fov = 52f;
                cinematicDirectorShotLabel = "All mechanics in motion";
                return;
            case 22:
                focus = center + Vector3.up * 18f;
                yaw = battleYaw + 126f;
                pitch = 28f;
                distance = 136f;
                fov = 62f;
                cinematicDirectorShotLabel = "Final explosion field";
                return;
            default:
                focus = center + Vector3.up * 20f;
                yaw = battleYaw + 34f;
                pitch = 30f;
                distance = 150f;
                fov = 60f;
                cinematicDirectorShotLabel = "Final epic hold";
                return;
        }
    }

    private int GetCinematicDirectorShotIndex()
    {
        if (cinematicDirectorOverviewMode)
            return GetOverviewTrailerShotIndex();

        if (cinematicDirectorSettlementMode)
            return GetSettlementShowcaseShotIndex();

        float t = cinematicDirectorTimer;
        if (t < 7f) return 0;
        if (t < 14f) return 1;
        if (t < 22f) return 2;
        if (t < 31f) return 3;
        if (t < 39f) return 4;
        if (t < 47f) return 5;
        if (t < 56f) return 6;
        if (t < 65f) return 7;
        if (t < 76f) return 8;
        if (t < 86f) return 9;
        if (t < 96f) return 10;
        if (t < 106f) return 11;
        if (t < 116f) return 12;
        if (t < 126f) return 13;
        if (t < 136f) return 14;
        if (t < 146f) return 15;
        if (t < 156f) return 16;
        if (t < 164f) return 17;
        if (t < 170f) return 18;
        if (t < 174f) return 19;
        if (t < 178f) return 20;
        if (t < 180f) return 21;
        if (t < 186f) return 22;
        return 23;
    }

    private int GetSettlementShowcaseShotIndex()
    {
        float t = cinematicDirectorTimer;
        if (t < 10f) return 0;
        if (t < 22f) return 1;
        if (t < 34f) return 2;
        if (t < 46f) return 3;
        if (t < 58f) return 4;
        if (t < 72f) return 5;
        if (t < 88f) return 6;
        if (t < 104f) return 7;
        if (t < 120f) return 8;
        if (t < 136f) return 9;
        if (t < 152f) return 10;
        if (t < 166f) return 11;
        if (t < 176f) return 12;
        return 13;
    }

    private int GetOverviewTrailerShotIndex()
    {
        float t = cinematicDirectorTimer;
        if (t < 10f) return 0;
        if (t < 22f) return 1;
        if (t < 34f) return 2;
        if (t < 48f) return 3;
        if (t < 62f) return 4;
        if (t < 76f) return 5;
        if (t < 90f) return 6;
        if (t < 104f) return 7;
        if (t < 118f) return 8;
        if (t < 132f) return 9;
        if (t < 146f) return 10;
        if (t < 160f) return 11;
        if (t < 174f) return 12;
        return 13;
    }

    private void EvaluateOverviewTrailerShot(out Vector3 focus, out float yaw, out float pitch, out float distance, out float fov)
    {
        Vector3 settlement = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : battlePyramid.position + battlePyramid.forward * 180f;
        Vector3 caravan = cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null
            ? cinematicDirectorSettlementCaravan.root.position
            : Vector3.Lerp(battlePyramid.position, settlement, 0.55f);
        Vector3 forward = settlement - battlePyramid.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battlePyramid.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 resource = GetOverviewResourceFocus();
        Vector3 baseFocus = GetOverviewForwardBaseFocus(settlement, forward, right);
        Vector3 castle = GetOverviewCastlePosition(settlement, forward, right);
        Vector3 airFight = settlement - forward * 104f + right * 44f;
        float yawBase = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        int shot = GetOverviewTrailerShotIndex();

        switch (shot)
        {
            case 0:
                focus = battlePyramid.position + Vector3.up * 9f + forward * 18f;
                yaw = yawBase + 180f;
                pitch = 11f;
                distance = 38f;
                fov = 39f;
                cinematicDirectorShotLabel = "Empty desert pyramid approach";
                return;
            case 1:
                focus = resource + Vector3.up * 5f;
                yaw = yawBase + 72f;
                pitch = 17f;
                distance = 40f;
                fov = 44f;
                cinematicDirectorShotLabel = "Resource discovery";
                return;
            case 2:
                focus = Vector3.Lerp(battlePyramid.position, resource, 0.55f) + Vector3.up * 5f;
                yaw = yawBase + 150f;
                pitch = 12f;
                distance = 48f;
                fov = 42f;
                cinematicDirectorShotLabel = "Builders roll out";
                return;
            case 3:
                focus = baseFocus + Vector3.up * 2f;
                yaw = yawBase + 210f;
                pitch = 10f;
                distance = 32f;
                fov = 39f;
                cinematicDirectorShotLabel = "Resource base construction";
                return;
            case 4:
                focus = GetSettlementShowcaseAirbasePosition(settlement) + Vector3.up * 10f + right * 14f;
                yaw = yawBase - 112f;
                pitch = 20f;
                distance = 68f;
                fov = 51f;
                cinematicDirectorShotLabel = "Aerodrome patrol launch";
                return;
            case 5:
                focus = airFight + Vector3.up * 13f;
                yaw = yawBase - 18f;
                pitch = 11f;
                distance = 38f;
                fov = 38f;
                cinematicDirectorShotLabel = "Flyers sweep the map";
                return;
            case 6:
                focus = airFight + Vector3.up * 12f + right * 8f;
                yaw = yawBase + 166f;
                pitch = 9f;
                distance = 30f;
                fov = 36f;
                cinematicDirectorShotLabel = "Air battle close cut";
                return;
            case 7:
                focus = settlement + Vector3.up * 8f;
                yaw = yawBase + 38f;
                pitch = 15f;
                distance = 54f;
                fov = 43f;
                cinematicDirectorShotLabel = "Neutral settlement spotted";
                return;
            case 8:
                focus = battlePyramid.position + Vector3.up * 7f + forward * 42f;
                yaw = yawBase + 180f;
                pitch = 11f;
                distance = 36f;
                fov = 39f;
                cinematicDirectorShotLabel = "Over-shoulder drive to settlement";
                return;
            case 9:
                focus = caravan + Vector3.up * 5f + right * 8f;
                yaw = yawBase + 160f;
                pitch = 8f;
                distance = 24f;
                fov = 37f;
                cinematicDirectorShotLabel = "Red raiders attack";
                return;
            case 10:
                focus = Vector3.Lerp(battlePyramid.position, settlement, 0.55f) + Vector3.up * 9f;
                yaw = yawBase - 62f;
                pitch = 18f;
                distance = 72f;
                fov = 52f;
                cinematicDirectorShotLabel = "Grid defense and rescue";
                return;
            case 11:
                focus = GetCinematicSquadFocus(Mathf.Max(0, cinematicDirectorGoldenSquads.Count - 1), battlePyramid.position) + Vector3.up * 4f;
                yaw = yawBase + 136f;
                pitch = 12f;
                distance = 42f;
                fov = 43f;
                cinematicDirectorShotLabel = "Army assembles";
                return;
            case 12:
                focus = castle + Vector3.up * 10f;
                yaw = yawBase + 204f;
                pitch = 18f;
                distance = 70f;
                fov = 48f;
                cinematicDirectorShotLabel = "Chinese castle under threat";
                return;
            default:
                focus = Vector3.Lerp(battlePyramid.position, castle, 0.56f) + Vector3.up * 22f;
                yaw = yawBase + 28f;
                pitch = 31f;
                distance = 150f;
                fov = 62f;
                cinematicDirectorShotLabel = "Final assault overview";
                return;
        }
    }

    private Vector3 GetOverviewResourceFocus()
    {
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node != null && node.transform != null)
                return node.transform.position;
        }
        return battlePyramid != null ? battlePyramid.position + battlePyramid.forward * 80f : Vector3.zero;
    }

    private void EvaluateSettlementShowcaseShot(out Vector3 focus, out float yaw, out float pitch, out float distance, out float fov)
    {
        Vector3 settlement = cinematicDirectorSettlementState != null && cinematicDirectorSettlementState.settlement != null && cinematicDirectorSettlementState.settlement.root != null
            ? cinematicDirectorSettlementState.settlement.root.position
            : battlePyramid.position + battlePyramid.forward * 90f;
        Vector3 caravan = cinematicDirectorSettlementCaravan != null && cinematicDirectorSettlementCaravan.root != null
            ? cinematicDirectorSettlementCaravan.root.position
            : Vector3.Lerp(battlePyramid.position, settlement, 0.55f);
        Vector3 forward = settlement - battlePyramid.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = battlePyramid.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float yawBase = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        int shot = GetSettlementShowcaseShotIndex();

        switch (shot)
        {
            case 0:
                focus = settlement + Vector3.up * 4.5f;
                yaw = yawBase + 36f;
                pitch = 13f;
                distance = 42f;
                fov = 42f;
                cinematicDirectorShotLabel = "Settlement without grid";
                return;
            case 1:
                focus = caravan + Vector3.up * 3f;
                yaw = yawBase - 18f;
                pitch = 8f;
                distance = 24f;
                fov = 38f;
                cinematicDirectorShotLabel = "Local caravan route";
                return;
            case 2:
                focus = caravan + Vector3.up * 5f + right * 8f;
                yaw = yawBase + 158f;
                pitch = 7f;
                distance = 22f;
                fov = 36f;
                cinematicDirectorShotLabel = "Red raid on caravan";
                return;
            case 3:
                focus = battlePyramid.position + Vector3.up * 6.5f + forward * 34f;
                yaw = yawBase + 180f;
                pitch = 12f;
                distance = 36f;
                fov = 41f;
                cinematicDirectorShotLabel = "Pyramid intercepts";
                return;
            case 4:
                focus = Vector3.Lerp(battlePyramid.position, caravan, 0.55f) + Vector3.up * 5f;
                yaw = yawBase - 78f;
                pitch = 13f;
                distance = 48f;
                fov = 44f;
                cinematicDirectorShotLabel = "Caravan defense";
                return;
            case 5:
                focus = settlement + Vector3.up * 5f - forward * 10f;
                yaw = yawBase + 25f;
                pitch = 15f;
                distance = 46f;
                fov = 44f;
                cinematicDirectorShotLabel = "Caravan reaches market";
                return;
            case 6:
                focus = GetSettlementShowcaseNetworkFocus(settlement) + Vector3.up * 8f;
                yaw = yawBase - 24f;
                pitch = 24f;
                distance = 92f;
                fov = 54f;
                cinematicDirectorShotLabel = "Mirror grid route";
                return;
            case 7:
                focus = GetSettlementShowcaseNetworkFocus(settlement) + Vector3.up * 5f;
                yaw = yawBase + 105f;
                pitch = 13f;
                distance = 42f;
                fov = 43f;
                cinematicDirectorShotLabel = "Relays coming online";
                return;
            case 8:
                focus = GetSettlementShowcaseAirbasePosition(settlement) + Vector3.up * 1.5f;
                yaw = yawBase + 205f;
                pitch = 10f;
                distance = 28f;
                fov = 38f;
                cinematicDirectorShotLabel = "Builders raise golden airbase";
                return;
            case 9:
                focus = GetSettlementShowcaseAirbasePosition(settlement) + right * 20f + Vector3.up * 8f;
                yaw = yawBase - 112f;
                pitch = 20f;
                distance = 68f;
                fov = 50f;
                cinematicDirectorShotLabel = "Aerodrome and tower perimeter";
                return;
            case 10:
                focus = Vector3.Lerp(GetSettlementShowcaseAirbasePosition(settlement), caravan, 0.5f) + Vector3.up * 12f;
                yaw = yawBase + 142f;
                pitch = 18f;
                distance = 72f;
                fov = 52f;
                cinematicDirectorShotLabel = "Aircraft patrol caravan roads";
                return;
            case 11:
                focus = Vector3.Lerp(battlePyramid.position, settlement, 0.72f) + Vector3.up * 7f;
                yaw = yawBase + 185f;
                pitch = 13f;
                distance = 48f;
                fov = 43f;
                cinematicDirectorShotLabel = "Pyramid and settlement crossfire";
                return;
            case 12:
                focus = settlement + Vector3.up * 12f;
                yaw = yawBase + 42f;
                pitch = 25f;
                distance = 96f;
                fov = 56f;
                cinematicDirectorShotLabel = "Powered settlement supports pyramid";
                return;
            default:
                focus = Vector3.Lerp(battlePyramid.position, settlement, 0.62f) + Vector3.up * 18f;
                yaw = yawBase + 28f;
                pitch = 30f;
                distance = 132f;
                fov = 60f;
                cinematicDirectorShotLabel = "Pyramid-grid-settlement alliance";
                return;
        }
    }

    private Vector3 GetSettlementShowcaseNetworkFocus(Vector3 fallback)
    {
        if (cinematicDirectorSettlementState == null || cinematicDirectorSettlementState.network == null)
            return fallback;
        EnergyNetworkState network = cinematicDirectorSettlementState.network;
        if (network.relays.Count > 1 && network.relays[1] != null)
            return network.relays[1].position;
        if (network.source != null && network.source.transform != null)
            return Vector3.Lerp(network.source.transform.position, fallback, 0.55f);
        return fallback;
    }

    private Transform GetCinematicSquadRepresentative(int index)
    {
        if (cinematicDirectorGoldenSquads.Count == 0)
            return battlePyramid;

        UnitSquad squad = cinematicDirectorGoldenSquads[Mathf.Clamp(index, 0, cinematicDirectorGoldenSquads.Count - 1)];
        Transform representative = GetSquadRepresentative(squad);
        return representative != null ? representative : battlePyramid;
    }

    private void MakeShoulderShot(Transform target, Vector3 fallback, Vector3 forward, float height, float chaseDistance, float shotFov, string label, out Vector3 focus, out float yaw, out float pitch, out float distance, out float fov)
    {
        Vector3 targetPosition = target != null ? target.position : fallback;
        Vector3 targetForward = target != null ? target.forward : forward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude < 0.01f)
            targetForward = forward;
        if (targetForward.sqrMagnitude < 0.01f)
            targetForward = CinematicBattleForward();
        targetForward.Normalize();

        focus = targetPosition + Vector3.up * height + targetForward * Mathf.Lerp(2.5f, 8f, Mathf.Clamp01(chaseDistance / 18f));
        yaw = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;
        pitch = Mathf.Lerp(7f, 14f, Mathf.Clamp01(height / 7f));
        distance = chaseDistance;
        fov = shotFov;
        cinematicDirectorShotLabel = label;
    }

    private Vector3 GetCinematicSquadFocus(int index, Vector3 fallback)
    {
        if (cinematicDirectorGoldenSquads.Count == 0)
            return fallback;

        UnitSquad squad = cinematicDirectorGoldenSquads[Mathf.Clamp(index, 0, cinematicDirectorGoldenSquads.Count - 1)];
        if (TryGetSquadCentroid(squad, out Vector3 centroid))
            return centroid;
        return fallback;
    }

    private Vector3 GetCinematicGustavFocus(Vector3 fallback)
    {
        for (int i = cinematicDirectorHostileAssets.Count - 1; i >= 0; i--)
        {
            MandarinkaAsset asset = cinematicDirectorHostileAssets[i];
            if (asset != null && asset.role == MandarinkaRole.Gustav && asset.transform != null)
                return asset.transform.position;
        }
        return fallback;
    }

    private Vector3 CinematicBattleCenter()
    {
        if (battlePyramid != null && mandarinkaFortressRoot != null)
            return Vector3.Lerp(battlePyramid.position, mandarinkaFortressRoot.position, 0.55f);
        if (battlePyramid != null)
            return battlePyramid.position + battlePyramid.forward * 120f;
        return Vector3.zero;
    }

    private Vector3 CinematicBattleForward()
    {
        if (battlePyramid != null && mandarinkaFortressRoot != null)
        {
            Vector3 forward = mandarinkaFortressRoot.position - battlePyramid.position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
                return forward.normalized;
        }
        if (battlePyramid != null)
            return battlePyramid.forward;
        return Vector3.forward;
    }

    private Vector3 CinematicGround(Vector3 position, float clearance)
    {
        position.y = GetPlayableGroundHeight(position) + clearance;
        return position;
    }

    private bool TryReadCinematicDirectorCommandLine(out float duration)
    {
        duration = CinematicDirectorDefaultDuration;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (string.Equals(arg, "-sandTrailer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-trailer", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(arg, "-sandTrailer180", StringComparison.OrdinalIgnoreCase))
            {
                duration = CinematicDirectorExtendedDuration;
                return true;
            }

            const string durationPrefix = "-sandTrailerDuration=";
            if (arg.StartsWith(durationPrefix, StringComparison.OrdinalIgnoreCase) &&
                float.TryParse(arg.Substring(durationPrefix.Length), out float parsedDuration))
            {
                duration = Mathf.Clamp(parsedDuration, 30f, 240f);
                return true;
            }
        }
        return false;
    }

    private void PlayCinematicDirectorMusic()
    {
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null)
            return;

        if (cinematicDirectorMusicSource == null)
        {
            cinematicDirectorMusicSource = camera.gameObject.AddComponent<AudioSource>();
            cinematicDirectorMusicSource.spatialBlend = 0f;
            cinematicDirectorMusicSource.loop = false;
            cinematicDirectorMusicSource.playOnAwake = false;
        }

        bool useExtended = cinematicDirectorRequestedDuration > CinematicDirectorDefaultDuration + 1f;
        string resourcePath = cinematicDirectorOverviewMode
            ? CinematicDirectorOverviewMusicResourcePath184
            : cinematicDirectorSettlementMode
            ? CinematicDirectorSettlementMusicResourcePath180
            : useExtended ? CinematicDirectorMusicResourcePath180 : CinematicDirectorMusicResourcePath90;

        AudioClip clip;
        if (cinematicDirectorOverviewMode)
        {
            if (cinematicDirectorOverviewMusicClip184 == null)
                cinematicDirectorOverviewMusicClip184 = Resources.Load<AudioClip>(CinematicDirectorOverviewMusicResourcePath184);
            clip = cinematicDirectorOverviewMusicClip184;
        }
        else if (cinematicDirectorSettlementMode)
        {
            if (cinematicDirectorSettlementMusicClip180 == null)
                cinematicDirectorSettlementMusicClip180 = Resources.Load<AudioClip>(CinematicDirectorSettlementMusicResourcePath180);
            clip = cinematicDirectorSettlementMusicClip180;
        }
        else if (useExtended)
        {
            if (cinematicDirectorMusicClip180 == null)
                cinematicDirectorMusicClip180 = Resources.Load<AudioClip>(CinematicDirectorMusicResourcePath180);
            clip = cinematicDirectorMusicClip180;
        }
        else
        {
            if (cinematicDirectorMusicClip90 == null)
                cinematicDirectorMusicClip90 = Resources.Load<AudioClip>(CinematicDirectorMusicResourcePath90);
            clip = cinematicDirectorMusicClip90;
        }

        if (clip == null)
        {
            lastEvent = "Trailer mode could not find music at Resources/" + resourcePath + ".";
            return;
        }

        cinematicDirectorMusicSource.Stop();
        cinematicDirectorMusicSource.clip = clip;
        cinematicDirectorMusicSource.time = 0f;
        cinematicDirectorMusicSource.volume = 0.74f * GetBenchmarkMusicVolume();
        cinematicDirectorMusicSource.Play();
    }

    private void DrawCinematicDirectorGUI()
    {
        if (!cinematicDirectorActive || hudHidden)
            return;

        if (cinematicDirectorStyle == null)
        {
            cinematicDirectorStyle = new GUIStyle(GUI.skin.label);
            cinematicDirectorStyle.fontSize = 13;
            cinematicDirectorStyle.fontStyle = FontStyle.Bold;
            cinematicDirectorStyle.normal.textColor = new Color(1f, 0.82f, 0.34f, 1f);
        }

        float remaining = Mathf.Max(0f, cinematicDirectorRequestedDuration - cinematicDirectorTimer);
        GUI.Box(new Rect(18f, Screen.height - 58f, 360f, 42f), GUIContent.none);
        GUI.Label(new Rect(30f, Screen.height - 52f, 336f, 18f), "Trailer demo | " + cinematicDirectorShotLabel, cinematicDirectorStyle);
        GUI.Label(new Rect(30f, Screen.height - 32f, 336f, 18f), "Time " + Mathf.CeilToInt(remaining) + "s | F8 stop", cinematicDirectorStyle);
    }
}