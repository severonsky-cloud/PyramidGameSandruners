using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class SandRunnersPrototype : MonoBehaviour
{
    private enum ResourceKind
    {
        Sand,
        Gold,
        Wind
    }

    [System.Flags]
    private enum UnitCapability
    {
        None = 0,
        Flyer = 1 << 0,
        Scarab = 1 << 1,
        Heavy = 1 << 2,
        Salvage = 1 << 3,
        ResourceDeveloper = 1 << 4,
        ScarabFortressDeveloper = 1 << 5,
        FortressCrusherDeveloper = 1 << 6,
        ThothBlessingDeveloper = 1 << 7,
        FortressCrusherUnit = 1 << 8,
        ThothUnit = 1 << 9
    }

    private sealed class ResourceNode
    {
        public Transform transform;
        public Renderer markerRenderer;
        public ResourceKind kind;
        public string label;
        public float capture;
        public bool controlled;
    }

    private sealed class RunnerUnit
    {
        public Transform transform;
        public string displayName = "Golden unit";
        public float health = 55f;
        public float maxHealth = 55f;
        public float fireCooldown;
        public float speed = 8.8f;
        public float damage = 20f;
        public float range = 10.5f;
        public bool hasOrder;
        public Vector3 orderPosition;
        public bool airborne;
        public float flightHeight;
        public bool autonomous;
        public bool siegeMode;
        public bool isSalvageScarab;
        public bool salvageAutoMode = true;
        public bool salvageReturnOrder;
        public float salvagePickupProgress;
        public SalvageField salvageTarget;
        public UnitSquad squad;
        public bool isElementalWalker;
        public bool isGradLauncher;
        public float secondaryFireCooldown;
        public int contractGroupId;
        public UnitCapability capabilities;
    }

    private sealed class EnemyUnit
    {
        public Transform transform;
        public float health = 70f;
        public float maxHealth = 70f;
        public float fireCooldown;
        public bool isJuzzherBarge;
        public Transform factionObjective;
        public string factionTag;
        public float objectiveTimer;
        public bool isStrategicMissile;
        public bool isAssaultHeadquarters;
    }

    private sealed class BeamVisual
    {
        public LineRenderer line;
        public float life;
    }

    private sealed class RingVisual
    {
        public Transform transform;
        public float life;
        public float maxLife;
        public Vector3 startScale;
        public Vector3 endScale;
    }

    private sealed class ProjectileVisual
    {
        public Transform transform;
        public Vector3 velocity;
        public float life;
        public float damage;
        public float blastRadius;
        public bool nuclear;
    }

    private sealed class MissileVisual
    {
        public Transform transform;
        public Transform target;
        public Vector3 velocity;
        public Vector3 fallbackTarget;
        public float speed;
        public float turnRate;
        public float life;
        public float damage;
        public float blastRadius;
        public bool nuclear;
    }

    [Header("Core")]
    public Transform battlePyramid;
    public float pyramidHull = 1200f;
    public float pyramidMaxHull = 1200f;
    public float moveSpeed = 4.8f;
    public float turnSpeed = 52f;

    [Header("Economy")]
    public float sand = 520f;
    public float gold = 320f;
    public float wind = 150f;
    public float captureRadius = 42f;

    [Header("Combat")]
    public float pyramidWeaponRange = 18f;
    public float pyramidWeaponDamage = 34f;
    public float pyramidWeaponCooldown = 0.5f;
    public float pulseRadius = 12f;
    public float pulseDamage = 75f;

    [Header("Camera")]
    public float mapHalfSize = 950f;
    public float cameraDistance = 56f;
    public float cameraHeight = 12f;
    public float cameraPitch = 32f;
    public float cameraYaw = 45f;
    public float mouseLookSensitivity = 0.12f;
    public float cameraSmooth = 7f;

    private readonly List<ResourceNode> resourceNodes = new List<ResourceNode>();
    private readonly List<RunnerUnit> runners = new List<RunnerUnit>();
    private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
    private readonly List<BeamVisual> beams = new List<BeamVisual>();
    private readonly List<RingVisual> rings = new List<RingVisual>();
    private readonly List<ProjectileVisual> projectiles = new List<ProjectileVisual>();
    private readonly List<MissileVisual> missiles = new List<MissileVisual>();
    private readonly List<Transform> enemySpawnPoints = new List<Transform>();
    private readonly List<Transform> autoCannonMuzzles = new List<Transform>();
    private readonly List<Transform> howitzerMuzzles = new List<Transform>();
    private readonly List<Transform> missileLaunchers = new List<Transform>();
    private readonly List<Transform> externalFactoryPads = new List<Transform>();

    private Camera mainCamera;
    private Material runnerMaterial;
    private Material enemyMaterial;
    private Material beamMaterial;
    private Material pulseMaterial;
    private Material controlledMaterial;
    private Material contestedMaterial;
    private Material commandMaterial;
    private Material shellMaterial;
    private Material missileMaterial;
    private Material nuclearMaterial;
    private Material externalUnitMaterial;
    private Material siegeUnitMaterial;
    private float pyramidFireTimer;
    private float autoCannonTimer;
    private float howitzerTimer;
    private float missileTimer;
    private float nuclearTimer;
    private float beamCharge;
    private float beamCooldownTimer;
    private int hangarStored;
    private int cruiseMissiles = 8;
    private int nuclearMissiles = 2;
    private float waveTimer = 95f;
    private float hintTimer = 7f;
    private int waveIndex;
    private bool commandCursorMode;
    private bool hasCommandDestination;
    private bool hudExpanded;
    private bool hudHidden;
    private string bannerMessage;
    private float bannerTimer;
    private float bannerPulseTimer;
    private float lastEnemyCount;
    private float enemyAlertTimer;
    private bool commandRightMouseDown;
    private bool commandRightMouseOrbitDrag;
    private const float CommandRightOrbitDragPixels = 14f;
    private Vector3 pyramidVelocity;
    private float pyramidThrottleBlend;
    private Vector3 pyramidStartPosition;
    private Quaternion pyramidStartRotation;
    private bool pyramidStartPoseCached;
    private bool pyramidMoveInputActive;
    private float pyramidGroundClearance = 0.35f;
    private Transform cameraFollowTarget;
    private string cameraFollowLabel;
    private bool cameraFollowSelectionMode;
    private bool cameraFollowInitialized;
    private Vector3 cameraFollowLastPosition;
    private float cameraFollowSpeedBlend;
    private float cameraFollowManualOrbitTimer;
    private Vector2 commandRightMouseStart;
    private Vector3 commandDestination;
    private Transform commandMarker;
    private Transform hangarDoor;
    private Transform hangarExit;
    private Transform beamMuzzle;
    private Transform internalUnitDisplay;
    private Vector3 hangarDoorClosedLocalPosition;
    private bool hangarSystemsCached;
    private bool hangarOpen;
    private float hangarOpenAmount;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private GUIStyle titleStyle;
    private GUIStyle warningStyle;
    private string lastEvent = "Sebek-nu-Anha is breaking toward the Earth Empire border.";

    private enum SandRunnersGameFlowState
    {
        MainMenu,
        Playing,
        Paused,
        Victory,
        Defeat
    }

    private static bool startPlayingAfterSceneReload;

    private enum StrategicUiEventKind
    {
        StrategicCanvasVisibility
    }

    private struct StrategicUiEvent
    {
        public StrategicUiEventKind kind;
        public bool visible;
    }

    private event System.Action<StrategicUiEvent> strategicUiEvents;

    private SandRunnersGameFlowState gameFlowState = SandRunnersGameFlowState.MainMenu;
    private GUIStyle menuTitleStyle;
    private GUIStyle menuBodyStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle menuSmallStyle;
    private GUIStyle narrativeSpeakerStyle;
    private GUIStyle narrativeBodyStyle;
    private GUIStyle bannerStyle;
    private GUIStyle hullBarBgStyle;
    private GUIStyle hullBarFillStyle;
    private GUIStyle narrativeInitialsStyle;
    private GUIStyle narrativeVictoryTitleStyle;
    private GUIStyle narrativeVictoryBodyStyle;

    private void Awake()
    {
        // Enter Play Mode can keep Time.timeScale when domain reload is disabled.
        // A stopped diagnostic frame or an interrupted modal must never freeze a fresh RTS session.
        Time.timeScale = 1f;
        EnsureReferences();
        CreateRuntimeMaterials();
        EnsureLargeDuneWorld();
        InitializeImmersiveBattlefield();
        EnsureCohesivePyramidArt();
        hangarSystemsCached = false;
        EnsureReferences();
        CacheScenarioPoints();
        SpreadScenarioPointsForLargeMap();
        InitializeRuntimeNavigation();
    }

    private void Start()
    {
        InitializeReleaseCandidate();
        InitializeMissionDirector();
        InitializeMandarinkaEncounter();
        InitializeAuthoredArtPass();
        InitializePyramidWeaponsV2();
        InitializeVerticalSliceFactions();
        InitializeWorldEvents();
        InitializeVerticalSliceMission();
        InitializeSlicePolish();
        InitializeStrategicCanvas();
        InitializeUnifiedDiplomacy();
        InitializeImperialRetaliation();
        InitializeSandRunnersAudio();
        InitializeMusicSystem();
        InitializeTouchOfHorus();
        InitializePyramidExpedition();
        InitializeRoadGameplay();
        InitializeSebekAvatar();
        SetupCameraImmediate();
        InitializeGameFlow();
        InitializeDamageSystems();
        InitializeCinematicDirector();
        if (gameFlowState == SandRunnersGameFlowState.Playing && !cinematicDirectorActive)
            SetCommandCursorMode(false);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CloseUnifiedDiplomacy(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        EnsureReferences();
        if (battlePyramid == null)
            return;

        float dt = Time.deltaTime;
        bool gameFlowConsumedInput = HandleGameFlowInput();
        bool cinematicDirectorConsumedInput = HandleCinematicDirectorHotkeys();
        if (!CanRunGameplayUpdate())
        {
            UpdateNonGameplayPresentation(dt);
            return;
        }
        if (gameFlowConsumedInput || cinematicDirectorConsumedInput)
            return;

        bool cinematicDirectorDriving = IsCinematicDirectorDrivingGameplay();
        if (cinematicDirectorDriving)
        {
            UpdateCinematicDirector(dt);
        }
        else
        {
            HandleHudInput();
            if (!guidedMissileActive)
            {
                HandleCameraModeInput();
                if (gunnerSide == 0)
                    HandlePyramidMovement(dt);
            }
            HandleCommandModeMouse();
            HandleBuildInput();
            HandleGoldenEngineeringInput();
        }
        UpdateResourceDevelopers(dt);
        UpdateRTSCore(dt);
        UpdateSpecialContractUnits(dt);
        if (!cinematicDirectorDriving)
        {
            HandleWeaponInput(dt);
            HandlePulseInput();
        }
        UpdateResources(dt);
        UpdateResourceDevelopment(dt);
        UpdateWaves(dt);
        UpdateMandarinkaEncounter(dt);
        UpdateVerticalSliceFactions(dt);
        UpdateWorldEvents(dt);
        UpdateUnifiedDiplomacy(dt);
        UpdateImperialRetaliation(dt);
        UpdateTouchOfHorus(dt);
        UpdatePyramidExpedition(dt);
        UpdateRoadGameplay(dt);
        UpdateVerticalSliceMission(dt);
        UpdateReleaseCandidate(dt);
        UpdateRunners(dt);
        UpdateGoldenEngineering(dt);
        UpdateEnemies(dt);
        UpdatePyramidWeapon(dt);
        UpdatePyramidWeaponsV2(dt);
        UpdateProjectiles(dt);
        UpdateMissiles(dt);
        UpdateGuidedMissile(dt);
        UpdateHowitzerGunner(dt);
        UpdateSandRunnersAviation(dt);
        UpdateThothCarrier(dt);
        UpdateDamageSystems(dt);
        UpdateSlicePolish(dt);
        UpdateVisuals(dt);
        UpdateImmersiveBattlefield(dt);
        UpdateSebekAvatar(dt);
        UpdateStrategicInterface(dt);
        UpdateSandRunnersAudio(dt);
        if (gameFlowState == SandRunnersGameFlowState.Playing)
        {
            int nearbyEnemyCountForMusic = 0;
            for (int enemyAudioIndex = 0; enemyAudioIndex < enemies.Count; enemyAudioIndex++)
            {
                EnemyUnit audioEnemy = enemies[enemyAudioIndex];
                if (audioEnemy != null && audioEnemy.transform != null && audioEnemy.health > 0f &&
                    FlatDistance(audioEnemy.transform.position, battlePyramid.position) <= 220f)
                    nearbyEnemyCountForMusic++;
            }
            bool activeCombatThreat = nearbyEnemyCountForMusic > 0 &&
                                      (nearbyEnemyCountForMusic > 1 || pyramidHull < pyramidMaxHull * 0.4f);
            SetMusicIntensity(activeCombatThreat, dt);
            if (enemies.Count > 0 && lastEnemyCount == 0f && waveIndex > 1)
            {
                PlaySandRunnerSound(SandRunnerSound.EnemySpotted, battlePyramid.position, 0.7f);
                ShowBanner("ENEMY CONTACTS DETECTED", 2.5f);
            }
            if (enemies.Count > 5 && lastEnemyCount <= 5f)
            {
                PlaySandRunnerSound(SandRunnerSound.ThreatDetected, battlePyramid.position, 0.8f);
                ShowBanner("MASSIVE ENEMY FORMATION APPROACHING", 3f);
            }
            if (pyramidHull < pyramidMaxHull * 0.25f && pyramidHull > 0f)
                enemyAlertTimer = 1f;
            lastEnemyCount = enemies.Count;
        }
        UpdateMusicSystem(dt);
        if (!sandRunnersIntroActive)
            FollowCamera(dt);
        UpdateMissionDirector(dt);
        hintTimer -= dt;
        MarkDefeatIfNeeded();
    }

    private void EnsureReferences()
    {
        if (battlePyramid == null)
        {
            GameObject pyramid = GameObject.Find("Codex_Pyramid");
            if (pyramid != null)
                battlePyramid = pyramid.transform;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (battlePyramid != null && duneSandMaterial != null && !duneWorldBuilt)
            EnsureLargeDuneWorld();

        if (battlePyramid != null && pyramidGoldMaterial != null && !cohesivePyramidArtBuilt)
        {
            EnsureCohesivePyramidArt();
            hangarSystemsCached = false;
        }

        if (!hangarSystemsCached && battlePyramid != null)
            CachePyramidSystems();
    }

    private void CachePyramidSystems()
    {
        autoCannonMuzzles.Clear();
        howitzerMuzzles.Clear();
        missileLaunchers.Clear();
        externalFactoryPads.Clear();
        ResetPyramidArtCache();
        hangarDoor = null;
        hangarExit = null;
        beamMuzzle = null;
        internalUnitDisplay = null;

        Transform[] children = battlePyramid.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            string name = child.name.ToLowerInvariant();
            if (name.Contains("30mm_muzzle"))
                autoCannonMuzzles.Add(child);
            if (name.Contains("300mm") && name.Contains("muzzle"))
                howitzerMuzzles.Add(child);
            if (name.Contains("missile_launcher"))
                missileLaunchers.Add(child);
            if (name.Contains("external_factory_pad"))
                externalFactoryPads.Add(child);
            if (name.Contains("front_hangar_door"))
                hangarDoor = child;
            if (name.Contains("hangar_exit"))
                hangarExit = child;
            if (name.Contains("beam_cannon_muzzle"))
                beamMuzzle = child;
            if (name.Contains("internal_unit_display"))
                internalUnitDisplay = child;
            CachePyramidArtChild(child, name);
        }

        howitzerMuzzles.Sort((a, b) => System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        ReplaceHowitzerMuzzlesWithFrontBattery();
        ReplaceAutocannonMuzzlesWithSideBattery();

        if (hangarDoor != null)
            hangarDoorClosedLocalPosition = hangarDoor.localPosition;

        if (internalUnitDisplay != null)
            internalUnitDisplay.gameObject.SetActive(hangarStored > 0);

        hangarSystemsCached = true;
    }

    private void CreateRuntimeMaterials()
    {
        runnerMaterial = CreateMaterial("Sand Runner Matte Gold", new Color(0.72f, 0.46f, 0.11f, 1f));
        enemyMaterial = CreateMaterial("Red Elemental Dark Alloy", new Color(0.58f, 0.035f, 0.02f, 1f));
        controlledMaterial = CreateMaterial("Controlled Resource Amber", new Color(0.82f, 0.52f, 0.12f, 1f));
        contestedMaterial = CreateMaterial("Unclaimed Resource Muted Cyan", new Color(0.18f, 0.55f, 0.68f, 1f));
        beamMaterial = CreateMaterial("Solar Lance Controlled Glow", new Color(0.95f, 0.64f, 0.14f, 1f));
        pulseMaterial = CreateMaterial("Ankh Pulse", new Color(1f, 0.9f, 0.35f, 0.55f));
        commandMaterial = CreateMaterial("Command Destination Amber", new Color(1f, 0.86f, 0.22f, 1f));
        shellMaterial = CreateMaterial("300mm Dark Metal Shell", new Color(0.17f, 0.16f, 0.14f, 1f));
        missileMaterial = CreateMaterial("Matte Gold Cruise Missile", new Color(0.62f, 0.43f, 0.1f, 1f));
        nuclearMaterial = CreateMaterial("Sun Core Nuclear Missile", new Color(1f, 0.18f, 0.08f, 1f));
        externalUnitMaterial = CreateMaterial("External Factory Skimmer", new Color(0.58f, 0.38f, 0.1f, 1f));
        siegeUnitMaterial = CreateMaterial("Siege Scarab Dark Bronze", new Color(0.3f, 0.2f, 0.09f, 1f));
        CreatePyramidArtMaterials();
    }

    private Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = materialName;
        ApplyColor(material, color);
        return material;
    }

    private void ApplyColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void CacheScenarioPoints()
    {
        resourceNodes.Clear();
        GameObject resourceRoot = GameObject.Find("SandRunners_ResourcePoints");
        if (resourceRoot != null)
        {
            EnsureAdditionalResourcePoints(resourceRoot.transform);
            for (int i = 0; i < resourceRoot.transform.childCount; i++)
            {
                Transform child = resourceRoot.transform.GetChild(i);
                ResourceNode node = new ResourceNode();
                node.transform = child;
                node.markerRenderer = child.GetComponentInChildren<Renderer>();
                node.kind = GuessResourceKind(child.name);
                node.label = BuildNodeLabel(node.kind, i + 1);
                resourceNodes.Add(node);
            }
        }

        enemySpawnPoints.Clear();
        GameObject spawnRoot = GameObject.Find("Red_Elemental_SpawnPoints");
        if (spawnRoot != null)
        {
            for (int i = 0; i < spawnRoot.transform.childCount; i++)
                enemySpawnPoints.Add(spawnRoot.transform.GetChild(i));
        }
    }

    private ResourceKind GuessResourceKind(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("gold"))
            return ResourceKind.Gold;
        if (lower.Contains("wind"))
            return ResourceKind.Wind;
        return ResourceKind.Sand;
    }

    private string BuildNodeLabel(ResourceKind kind, int index)
    {
        if (kind == ResourceKind.Gold)
            return "buried gold condenser " + index;
        if (kind == ResourceKind.Wind)
            return "wind obelisk " + index;
        return "sand refinery " + index;
    }

    private void HandlePyramidMovement(float dt)
    {
        Vector3 input = ReadMoveInput();
        bool directInput = input.sqrMagnitude >= 0.01f;
        pyramidMoveInputActive = directInput;
        bool hasMoveIntent = directInput;

        if (directInput)
        {
            hasCommandDestination = false;
            if (commandMarker != null)
            {
                Destroy(commandMarker.gameObject);
                commandMarker = null;
            }
        }
        else if (hasCommandDestination)
        {
            Vector3 toDestination = commandDestination - battlePyramid.position;
            toDestination.y = 0f;
            float arrivalDistance = Mathf.Max(3.8f, pyramidVelocity.magnitude * 0.95f);
            if (toDestination.magnitude <= arrivalDistance)
            {
                hasCommandDestination = false;
                if (commandMarker != null)
                {
                    Destroy(commandMarker.gameObject);
                    commandMarker = null;
                }
                lastEvent = "Battle pyramid reached the command point.";
            }
            else
            {
                input = toDestination.normalized;
                hasMoveIntent = true;
            }
        }

        if (hasMoveIntent)
        {
            input.Normalize();
            float boost = pyramidTraversal != null && pyramidTraversal.marchMode == PyramidMarchMode.Forced && wind > 0f ? 1.18f : 1f;
            float commandSpeedBias = hasCommandDestination && !directInput ? 0.82f : 1f;
            Vector3 desiredVelocity = input * moveSpeed * boost * commandSpeedBias * GetPyramidTraversalSpeedMultiplier() * GetPyramidMoveDamageMultiplier();
            float acceleration = (boost > 1f ? 4.1f : 3.2f) * GetPyramidTraversalAccelerationMultiplier();
            pyramidVelocity = Vector3.MoveTowards(pyramidVelocity, desiredVelocity, acceleration * dt);
        }
        else
        {
            pyramidVelocity = Vector3.MoveTowards(pyramidVelocity, Vector3.zero, 3.8f * dt);
        }

        if (pyramidVelocity.sqrMagnitude < 0.0004f)
            pyramidVelocity = Vector3.zero;

        Vector3 nextPosition = battlePyramid.position + pyramidVelocity * dt;
        TryTraversePyramidTo(nextPosition, dt, out nextPosition);
        float clampedX = Mathf.Clamp(nextPosition.x, -mapHalfSize, mapHalfSize);
        float clampedZ = Mathf.Clamp(nextPosition.z, -mapHalfSize, mapHalfSize);
        if (!Mathf.Approximately(clampedX, nextPosition.x))
            pyramidVelocity.x = 0f;
        if (!Mathf.Approximately(clampedZ, nextPosition.z))
            pyramidVelocity.z = 0f;

        battlePyramid.position = new Vector3(clampedX, nextPosition.y, clampedZ);
        battlePyramid.position = new Vector3(battlePyramid.position.x, GetPlayableGroundHeight(battlePyramid.position) + pyramidGroundClearance, battlePyramid.position.z);

        if (pyramidVelocity.sqrMagnitude > 0.03f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(pyramidVelocity.normalized, Vector3.up);
            float speed01 = Mathf.Clamp01(pyramidVelocity.magnitude / Mathf.Max(0.1f, moveSpeed));
            battlePyramid.rotation = Quaternion.RotateTowards(battlePyramid.rotation, targetRotation, Mathf.Lerp(turnSpeed * 0.55f, turnSpeed, speed01) * dt);
        }

        float targetThrottle = Mathf.Clamp01(pyramidVelocity.magnitude / Mathf.Max(0.1f, moveSpeed));
        pyramidThrottleBlend = Mathf.MoveTowards(pyramidThrottleBlend, targetThrottle, dt * (targetThrottle > pyramidThrottleBlend ? 2.2f : 2.8f));
        UpdatePyramidMotionJuice(pyramidThrottleBlend, dt);
    }

    private void HandleHudInput()
    {
        if (WasKeyPressedThisFrame(Key.H))
            hudHidden = !hudHidden;
        if (WasKeyPressedThisFrame(Key.F1))
        {
            hudExpanded = !hudExpanded;
            hudHidden = false;
        }
        if (WasKeyPressedThisFrame(Key.Backspace) || WasKeyPressedThisFrame(Key.Home))
            ResetPyramidToStart();
    }

    private void ResetPyramidToStart()
    {
        if (battlePyramid == null)
            return;

        if (!pyramidStartPoseCached)
        {
            pyramidStartPosition = battlePyramid.position;
            pyramidStartRotation = battlePyramid.rotation;
            pyramidStartPoseCached = true;
        }

        Vector3 resetPosition = pyramidStartPosition;
        resetPosition.y = GetPlayableGroundHeight(resetPosition) + pyramidGroundClearance;
        battlePyramid.position = resetPosition;
        battlePyramid.rotation = pyramidStartRotation;
        pyramidVelocity = Vector3.zero;
        pyramidThrottleBlend = 0f;
        hasCommandDestination = false;
        if (commandMarker != null)
        {
            Destroy(commandMarker.gameObject);
            commandMarker = null;
        }

        SetCommandCursorMode(false);
        lastEvent = "Emergency reset: pyramid returned to the southwest rally point.";
        SetupCameraImmediate();
    }

    private void HandleBuildInput()
    {
        if (WasKeyPressedThisFrame(Key.Digit1) || WasKeyPressedThisFrame(Key.Numpad1))
            BuildInternalRunner();
        if (WasKeyPressedThisFrame(Key.G))
            TryReleaseHangarUnit(null);
        if (WasKeyPressedThisFrame(Key.F))
            ToggleHangar();
        if (WasKeyPressedThisFrame(Key.Digit2) || WasKeyPressedThisFrame(Key.Numpad2))
            TrySpawnExternalUnit(false);
        if (WasKeyPressedThisFrame(Key.Digit3) || WasKeyPressedThisFrame(Key.Numpad3))
            TrySpawnExternalUnit(true);
    }

    private void BuildInternalRunner()
    {
        QueueProduction(ProductionKind.VimanaRunner);
    }

    private void TryReleaseHangarUnit(Vector3? orderPosition)
    {
        if (TryReleaseReadyProductionSquad(orderPosition))
            return;

        if (hangarStored <= 0)
        {
            hangarOpen = true;
            lastEvent = "Hangar is open, but no ground squads are ready. Queue a ground unit first.";
            return;
        }

        hangarStored--;
        hangarOpen = true;

        Vector3 spawnPosition = GetHangarExitPosition();
        RunnerUnit unit = CreateVehicleUnit(
            "Golden_Hangar_Vimana_Runner",
            spawnPosition,
            Quaternion.LookRotation(battlePyramid.forward, Vector3.up),
            new Vector3(0.85f, 0.42f, 1.25f),
            runnerMaterial,
            75f,
            11.5f,
            24f,
            15f);

        if (orderPosition.HasValue)
        {
            unit.hasOrder = true;
            unit.orderPosition = orderPosition.Value;
        }

        if (internalUnitDisplay != null)
            internalUnitDisplay.gameObject.SetActive(hangarStored > 0);

        lastEvent = "Hangar doors open. Vimana runner deployed. Stored: " + hangarStored + ".";
    }

    private void TrySpawnExternalUnit(bool siege)
    {
        QueueProduction(siege ? ProductionKind.SiegeScarab : ProductionKind.SandSkimmer);
    }

    private RunnerUnit CreateVehicleUnit(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, float health, float speed, float damage, float range, string displayName = null, bool airborne = false, float flightHeight = 0f, bool autonomous = false)
    {
        GameObject runner = new GameObject(name);
        runner.name = name;
        runner.transform.position = position;
        runner.transform.rotation = rotation;
        BuildGoldenVehicleVisual(runner.transform, name, scale, material);
        ApplyAuthoredGoldenVehicleArt(runner.transform, name, scale);
        BoxCollider selectionCollider = runner.AddComponent<BoxCollider>();
        selectionCollider.center = new Vector3(0f, Mathf.Max(0.35f, scale.y * 0.9f), 0f);
        selectionCollider.size = new Vector3(Mathf.Max(1.6f, scale.x * 2.6f), Mathf.Max(1.2f, scale.y * 2.4f), Mathf.Max(2.2f, scale.z * 2.7f));

        Light marker = new GameObject(name + "_Amber_Marker").AddComponent<Light>();
        marker.transform.SetParent(runner.transform, false);
        marker.transform.localPosition = new Vector3(0f, Mathf.Max(0.8f, scale.y * 1.55f), 0f);
        marker.type = LightType.Point;
        marker.color = new Color(1f, 0.72f, 0.2f, 1f);
        marker.intensity = 0.9f;
        marker.range = 4.5f;

        RunnerUnit unit = new RunnerUnit();
        unit.transform = runner.transform;
        unit.displayName = displayName ?? BuildFriendlyDisplayName(name);
        unit.health = health;
        unit.maxHealth = health;
        unit.speed = speed;
        unit.damage = damage;
        unit.range = range;
        unit.airborne = airborne;
        unit.flightHeight = flightHeight;
        unit.autonomous = autonomous;
        unit.capabilities = InferUnitCapabilities(name, unit.displayName, airborne);
        runners.Add(unit);
        RegisterRTSRunner(unit);
        return unit;
    }

    private string BuildFriendlyDisplayName(string objectName)
    {
        if (objectName.Contains("Combat_Flyer"))
            return "Combat Flyer Squadron";
        if (objectName.Contains("Scarab_Tank"))
            return "Scarab Tank";
        if (objectName.Contains("Abydos_Flyer"))
            return "Golden Elemental Flyer";
        if (objectName.Contains("Heavy_Flyer"))
            return "Heavy Golden Flyer";
        if (objectName.Contains("Wrath_Of_Ra"))
            return "Wrath of Ra";
        if (objectName.Contains("Fortress_Crusher"))
            return "Fortress Crusher";
        if (objectName.Contains("Thoth_Embrace"))
            return "Thoth's Embrace";
        if (objectName.Contains("Salvage_Scarab"))
            return "Salvage Scarab";
        if (objectName.Contains("Siege_Scarab"))
            return "Siege Scarab";
        if (objectName.Contains("Sand_Skimmer"))
            return "Sand Skimmer";
        return "Golden Vimana Runner";
    }

    private UnitCapability InferUnitCapabilities(string objectName, string displayName, bool airborne)
    {
        string objectKey = objectName ?? string.Empty;
        string label = displayName ?? string.Empty;
        UnitCapability capabilities = airborne ? UnitCapability.Flyer : UnitCapability.None;

        if (objectKey.Contains("Scarab") || label.Contains("Scarab"))
            capabilities |= UnitCapability.Scarab;
        if (objectKey.Contains("Heavy") || objectKey.Contains("Wrath") || label.Contains("Heavy") || label.Contains("Wrath"))
            capabilities |= UnitCapability.Heavy;
        if (objectKey.Contains("Salvage") || objectKey.Contains("Harvester") || label.Contains("Salvage") || label.Contains("Harvester"))
            capabilities |= UnitCapability.Salvage | UnitCapability.ResourceDeveloper;
        if (objectKey.Contains("Scarab_Tank") || label.Contains("Scarab Tank"))
            capabilities |= UnitCapability.ResourceDeveloper | UnitCapability.ScarabFortressDeveloper;
        if (objectKey.Contains("Fortress_Crusher") || label.Contains("Fortress Crusher"))
            capabilities |= UnitCapability.ResourceDeveloper | UnitCapability.FortressCrusherDeveloper | UnitCapability.FortressCrusherUnit | UnitCapability.Heavy;
        if (objectKey.Contains("Thoth") || label.Contains("Thoth"))
            capabilities |= UnitCapability.ResourceDeveloper | UnitCapability.ThothBlessingDeveloper | UnitCapability.ThothUnit | UnitCapability.Heavy;

        return capabilities;
    }

    private bool HasCapability(RunnerUnit runner, UnitCapability capability)
    {
        return runner != null && (runner.capabilities & capability) == capability;
    }

    private void BuildGoldenVehicleVisual(Transform root, string name, Vector3 scale, Material material)
    {
        bool combatFlyer = name.Contains("Combat_Flyer");
        bool scarabTank = name.Contains("Scarab_Tank");
        bool abydosFlyer = name.Contains("Abydos_Flyer");
        bool heavyFlyer = name.Contains("Heavy_Flyer");
        bool wrathOfRa = name.Contains("Wrath_Of_Ra");
        bool fortressCrusher = name.Contains("Fortress_Crusher");
        bool thothEmbrace = name.Contains("Thoth_Embrace");
        bool siege = name.Contains("Siege_Scarab");
        bool salvageScarab = name.Contains("Salvage_Scarab");
        bool skimmer = name.Contains("Sand_Skimmer");
        if (combatFlyer)
        {
            CreateBox(root, "Combat_Flyer_Golden_Boat_Hull", ScaleLocal(new Vector3(0f, 0.42f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.82f, 0.24f, 2.24f), scale), material);
            CreateBox(root, "Combat_Flyer_Black_Keel", ScaleLocal(new Vector3(0f, 0.24f, -0.18f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.28f, 0.16f, 2.3f), scale), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Combat_Flyer_Ring_Outer", ScaleLocal(new Vector3(0f, 0.96f, -0.18f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.78f, 0.05f, 0.78f), scale), pyramidGlowMaterial);
            CreateCylinder(root, "Combat_Flyer_Ring_Inner", ScaleLocal(new Vector3(0f, 0.96f, -0.18f), scale), Quaternion.Euler(0f, 90f, 0f), ScaleLocal(new Vector3(0.48f, 0.045f, 0.48f), scale), pyramidGlowMaterial);
            CreateCylinder(root, "Combat_Flyer_Solar_Beam_Cannon", ScaleLocal(new Vector3(0f, 0.68f, 1.22f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.1f, 0.82f, 0.1f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Combat_Flyer_Left_MG", ScaleLocal(new Vector3(-0.42f, 0.52f, 0.82f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.08f, 0.08f, 0.72f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Combat_Flyer_Right_MG", ScaleLocal(new Vector3(0.42f, 0.52f, 0.82f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.08f, 0.08f, 0.72f), scale), pyramidDarkArmorMaterial);
            return;
        }

        if (scarabTank)
        {
            CreateBox(root, "Scarab_Tank_Lift_Belly", ScaleLocal(new Vector3(0f, 0.24f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.85f, 0.22f, 2.12f), scale), pyramidGlowMaterial);
            CreateBox(root, "Scarab_Tank_Wing_Carapace", ScaleLocal(new Vector3(0f, 0.62f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.65f, 0.42f, 1.92f), scale), material);
            CreateBox(root, "Scarab_Tank_Left_Elytron", ScaleLocal(new Vector3(-0.5f, 0.9f, 0.04f), scale), Quaternion.Euler(0f, 0f, -6f), ScaleLocal(new Vector3(0.72f, 0.18f, 1.7f), scale), pyramidGoldMaterial);
            CreateBox(root, "Scarab_Tank_Right_Elytron", ScaleLocal(new Vector3(0.5f, 0.9f, 0.04f), scale), Quaternion.Euler(0f, 0f, 6f), ScaleLocal(new Vector3(0.72f, 0.18f, 1.7f), scale), pyramidGoldMaterial);
            CreateCylinder(root, "Scarab_Tank_80mm_Gun", ScaleLocal(new Vector3(0f, 1.08f, 1.24f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.12f, 0.9f, 0.12f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Scarab_Tank_127_MG_Left", ScaleLocal(new Vector3(-0.72f, 0.8f, 0.95f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.08f, 0.08f, 0.62f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Scarab_Tank_127_MG_Right", ScaleLocal(new Vector3(0.72f, 0.8f, 0.95f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.08f, 0.08f, 0.62f), scale), pyramidDarkArmorMaterial);
            return;
        }

        if (abydosFlyer)
        {
            CreateBox(root, "Abydos_Flyer_Ancient_Fuselage", ScaleLocal(new Vector3(0f, 0.5f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.54f, 0.28f, 2.3f), scale), material);
            CreateBox(root, "Abydos_Flyer_Tail_Fin", ScaleLocal(new Vector3(0f, 0.98f, -0.86f), scale), Quaternion.Euler(-12f, 0f, 0f), ScaleLocal(new Vector3(0.12f, 0.86f, 0.82f), scale), pyramidGoldMaterial);
            CreateCylinder(root, "Abydos_Flyer_Wing_Aura_Left", ScaleLocal(new Vector3(-0.88f, 0.5f, 0.12f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.52f, 0.035f, 0.52f), scale), pyramidGlowMaterial);
            CreateCylinder(root, "Abydos_Flyer_Wing_Aura_Right", ScaleLocal(new Vector3(0.88f, 0.5f, 0.12f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.52f, 0.035f, 0.52f), scale), pyramidGlowMaterial);
            CreateBox(root, "Abydos_Flyer_Missile_Rail_Left", ScaleLocal(new Vector3(-0.44f, 0.38f, 0.76f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.1f, 0.08f, 1.02f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Abydos_Flyer_Missile_Rail_Right", ScaleLocal(new Vector3(0.44f, 0.38f, 0.76f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.1f, 0.08f, 1.02f), scale), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Abydos_Flyer_Pulse_Cannon", ScaleLocal(new Vector3(0f, 0.56f, 1.25f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.1f, 0.55f, 0.1f), scale), pyramidGlowMaterial);
            return;
        }

        if (heavyFlyer)
        {
            CreateBox(root, "Heavy_Flyer_Golden_Fuselage", ScaleLocal(new Vector3(0f, 0.52f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.78f, 0.34f, 2.65f), scale), material);
            CreateBox(root, "Heavy_Flyer_Left_Wing", ScaleLocal(new Vector3(-1.55f, 0.48f, 0.1f), scale), Quaternion.Euler(0f, 0f, -4f), ScaleLocal(new Vector3(2.3f, 0.08f, 1.1f), scale), pyramidGoldMaterial);
            CreateBox(root, "Heavy_Flyer_Right_Wing", ScaleLocal(new Vector3(1.55f, 0.48f, 0.1f), scale), Quaternion.Euler(0f, 0f, 4f), ScaleLocal(new Vector3(2.3f, 0.08f, 1.1f), scale), pyramidGoldMaterial);
            CreateBox(root, "Heavy_Flyer_Bomb_Bay", ScaleLocal(new Vector3(0f, 0.18f, 0.2f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.55f, 0.18f, 1.45f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Heavy_Flyer_Tail_Wing", ScaleLocal(new Vector3(0f, 0.88f, -1.18f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.4f, 0.08f, 0.52f), scale), pyramidGoldMaterial);
            return;
        }

        if (wrathOfRa)
        {
            CreateBox(root, "Wrath_Of_Ra_Heavy_Platform", ScaleLocal(new Vector3(0f, 0.38f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.35f, 0.42f, 2.55f), scale), material);
            CreateCylinder(root, "Wrath_Of_Ra_Solar_Dish", ScaleLocal(new Vector3(0f, 1.15f, 0.42f), scale), Quaternion.Euler(72f, 0f, 0f), ScaleLocal(new Vector3(0.76f, 0.08f, 0.76f), scale), pyramidGlowMaterial);
            CreateCylinder(root, "Wrath_Of_Ra_Main_Beam_Lance", ScaleLocal(new Vector3(0f, 1.02f, 1.48f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.18f, 1.25f, 0.18f), scale), pyramidDarkArmorMaterial);
            for (int i = -1; i <= 1; i++)
                CreateCylinder(root, "Wrath_Of_Ra_20mm_Turret_" + i, ScaleLocal(new Vector3(i * 0.72f, 0.82f, -0.78f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.08f, 0.44f, 0.08f), scale), pyramidDarkArmorMaterial);
            return;
        }

        if (fortressCrusher)
        {
            CreateBox(root, "Fortress_Crusher_Armored_Slab", ScaleLocal(new Vector3(0f, 0.65f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.55f, 0.78f, 3.3f), scale), material);
            CreateBox(root, "Fortress_Crusher_Black_Belly", ScaleLocal(new Vector3(0f, 0.22f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.8f, 0.28f, 3.55f), scale), pyramidDarkArmorMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                    CreateCylinder(root, "Fortress_Crusher_Steel_Wheel_" + side + "_" + i, ScaleLocal(new Vector3(side * 1.58f, 0.32f, -1.2f + i * 0.82f), scale), Quaternion.Euler(0f, 0f, 90f), ScaleLocal(new Vector3(0.42f, 0.16f, 0.42f), scale), pyramidDarkArmorMaterial);
                CreateCylinder(root, "Fortress_Crusher_80mm_" + side, ScaleLocal(new Vector3(side * 1.12f, 0.98f, 1.45f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.12f, 0.85f, 0.12f), scale), pyramidDarkArmorMaterial);
            }
            CreateBox(root, "Fortress_Crusher_Rocket_Rack", ScaleLocal(new Vector3(0f, 1.15f, -0.75f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.45f, 0.28f, 0.72f), scale), pyramidGoldMaterial);
            return;
        }

        if (thothEmbrace)
        {
            CreateBox(root, "Thoth_Embrace_Carrier_Core", ScaleLocal(new Vector3(0f, 0.72f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.25f, 0.42f, 3.4f), scale), material);
            CreateBox(root, "Thoth_Left_Giant_Wing", ScaleLocal(new Vector3(-2.25f, 0.64f, 0.05f), scale), Quaternion.Euler(0f, 0f, -4f), ScaleLocal(new Vector3(3.8f, 0.1f, 1.55f), scale), pyramidGoldMaterial);
            CreateBox(root, "Thoth_Right_Giant_Wing", ScaleLocal(new Vector3(2.25f, 0.64f, 0.05f), scale), Quaternion.Euler(0f, 0f, 4f), ScaleLocal(new Vector3(3.8f, 0.1f, 1.55f), scale), pyramidGoldMaterial);
            CreateBox(root, "Thoth_Left_Aircraft_Deck", ScaleLocal(new Vector3(-1.7f, 0.92f, 0.25f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.2f, 0.08f, 1.05f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Thoth_Right_Aircraft_Deck", ScaleLocal(new Vector3(1.7f, 0.92f, 0.25f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.2f, 0.08f, 1.05f), scale), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Thoth_Left_Ground_Beam", ScaleLocal(new Vector3(-0.72f, 0.34f, 1.32f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.18f, 0.92f, 0.18f), scale), pyramidGlowMaterial);
            CreateCylinder(root, "Thoth_Right_Ground_Beam", ScaleLocal(new Vector3(0.72f, 0.34f, 1.32f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.18f, 0.92f, 0.18f), scale), pyramidGlowMaterial);
            return;
        }

        if (siege)
        {
            CreateBox(root, "Scarab_Heavy_Bronze_Hull", ScaleLocal(new Vector3(0f, 0.54f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.8f, 0.55f, 2.25f), scale), material);
            CreateBox(root, "Scarab_Dark_Track_Belly", ScaleLocal(new Vector3(0f, 0.22f, -0.05f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.15f, 0.28f, 2.5f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Scarab_Sun_Casemate", ScaleLocal(new Vector3(0f, 0.98f, 0.25f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.05f, 0.56f, 1.02f), scale), pyramidGoldMaterial);
            CreateCylinder(root, "Scarab_Forward_Siege_Cannon", ScaleLocal(new Vector3(0f, 1.08f, 1.35f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.15f, 0.82f, 0.15f), scale), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Scarab_Reactor_Eye", ScaleLocal(new Vector3(0f, 1.08f, -0.48f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.32f, 0.09f, 0.32f), scale), pyramidGlowMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = -0.82f + i * 0.82f;
                    CreateBox(root, "Scarab_Gold_Leg_" + side + "_" + i, ScaleLocal(new Vector3(side * 1.18f, 0.34f, z), scale), Quaternion.Euler(0f, 0f, side * 14f), ScaleLocal(new Vector3(0.68f, 0.16f, 0.2f), scale), pyramidGoldMaterial);
                }
            }
            return;
        }

        if (salvageScarab)
        {
            CreateBox(root, "Salvage_Scarab_Heavy_Cargo_Hull", ScaleLocal(new Vector3(0f, 0.62f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.45f, 0.72f, 2.85f), scale), material);
            CreateBox(root, "Salvage_Scarab_Dark_Undercarriage", ScaleLocal(new Vector3(0f, 0.25f, -0.05f), scale), Quaternion.identity, ScaleLocal(new Vector3(2.75f, 0.28f, 3.1f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Salvage_Scarab_Amber_Cargo_Bay", ScaleLocal(new Vector3(0f, 1.18f, -0.18f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.65f, 0.5f, 1.35f), scale), pyramidGoldMaterial);
            CreateBox(root, "Salvage_Scarab_Crane_Mast", ScaleLocal(new Vector3(0f, 1.55f, 1.15f), scale), Quaternion.Euler(-16f, 0f, 0f), ScaleLocal(new Vector3(0.22f, 0.22f, 1.7f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Salvage_Scarab_Crane_Arm", ScaleLocal(new Vector3(0.78f, 1.8f, 1.85f), scale), Quaternion.Euler(0f, 0f, 12f), ScaleLocal(new Vector3(0.18f, 0.18f, 1.55f), scale), pyramidGoldMaterial);
            CreateCylinder(root, "Salvage_Scarab_Collector_Core", ScaleLocal(new Vector3(0.82f, 1.72f, 2.55f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.3f, 0.34f, 0.3f), scale), pyramidGlowMaterial);
            CreateBox(root, "Salvage_Scarab_Command_Canopy", ScaleLocal(new Vector3(0f, 1.62f, -1.05f), scale), Quaternion.identity, ScaleLocal(new Vector3(1.0f, 0.22f, 0.64f), scale), pyramidGlowMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -1.18f + i * 0.78f;
                    CreateCylinder(root, "Salvage_Scarab_Track_" + side + "_" + i, ScaleLocal(new Vector3(side * 1.48f, 0.42f, z), scale), Quaternion.Euler(0f, 0f, 90f), ScaleLocal(new Vector3(0.46f, 0.2f, 0.46f), scale), pyramidDarkArmorMaterial);
                }
            }
            return;
        }

        if (skimmer)
        {
            CreateBox(root, "Skimmer_Golden_Knife_Hull", ScaleLocal(new Vector3(0f, 0.38f, 0.1f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.58f, 0.24f, 1.95f), scale), material);
            CreateBox(root, "Skimmer_Left_Pontoon", ScaleLocal(new Vector3(-0.62f, 0.26f, -0.08f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.28f, 0.18f, 1.72f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Skimmer_Right_Pontoon", ScaleLocal(new Vector3(0.62f, 0.26f, -0.08f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.28f, 0.18f, 1.72f), scale), pyramidDarkArmorMaterial);
            CreateBox(root, "Skimmer_Sun_Wing_Left", ScaleLocal(new Vector3(-0.92f, 0.44f, 0.22f), scale), Quaternion.Euler(0f, 0f, -9f), ScaleLocal(new Vector3(0.72f, 0.045f, 1.12f), scale), pyramidGoldMaterial);
            CreateBox(root, "Skimmer_Sun_Wing_Right", ScaleLocal(new Vector3(0.92f, 0.44f, 0.22f), scale), Quaternion.Euler(0f, 0f, 9f), ScaleLocal(new Vector3(0.72f, 0.045f, 1.12f), scale), pyramidGoldMaterial);
            CreateCylinder(root, "Skimmer_Cyan_Lift_Fan", ScaleLocal(new Vector3(0f, 0.54f, -0.8f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.32f, 0.045f, 0.32f), scale), pyramidGlowMaterial);
            return;
        }

        CreateBox(root, "Vimana_Golden_Core", ScaleLocal(new Vector3(0f, 0.46f, 0f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.62f, 0.25f, 1.82f), scale), material);
        CreateBox(root, "Vimana_Left_Swept_Wing", ScaleLocal(new Vector3(-0.72f, 0.4f, 0.08f), scale), Quaternion.Euler(0f, 0f, -8f), ScaleLocal(new Vector3(0.82f, 0.05f, 1.18f), scale), pyramidGoldMaterial);
        CreateBox(root, "Vimana_Right_Swept_Wing", ScaleLocal(new Vector3(0.72f, 0.4f, 0.08f), scale), Quaternion.Euler(0f, 0f, 8f), ScaleLocal(new Vector3(0.82f, 0.05f, 1.18f), scale), pyramidGoldMaterial);
        CreateBox(root, "Vimana_Dark_Nose_Ram", ScaleLocal(new Vector3(0f, 0.42f, 1.1f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.24f, 0.16f, 0.58f), scale), pyramidDarkArmorMaterial);
        CreateBox(root, "Vimana_Cyan_Cockpit_Slit", ScaleLocal(new Vector3(0f, 0.62f, 0.35f), scale), Quaternion.identity, ScaleLocal(new Vector3(0.42f, 0.08f, 0.36f), scale), pyramidGlowMaterial);
        CreateCylinder(root, "Vimana_Rear_Sun_Ring", ScaleLocal(new Vector3(0f, 0.45f, -1.02f), scale), Quaternion.Euler(90f, 0f, 0f), ScaleLocal(new Vector3(0.32f, 0.065f, 0.32f), scale), pyramidGlowMaterial);
    }

    private Vector3 ScaleLocal(Vector3 value, Vector3 scale)
    {
        return new Vector3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }

    private Vector3 GetHangarExitPosition()
    {
        if (hangarExit != null)
            return hangarExit.position;
        return battlePyramid.position + battlePyramid.forward * 8f + Vector3.up * 0.9f;
    }

    private Vector3 GetExternalFactoryPosition()
    {
        if (externalFactoryPads.Count > 0)
        {
            Transform pad = externalFactoryPads[Random.Range(0, externalFactoryPads.Count)];
            return pad.position + Vector3.up * 0.8f;
        }

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(8f, 12f);
        return battlePyramid.position + new Vector3(offset.x, 0.8f, offset.y);
    }

    private void ToggleHangar()
    {
        hangarOpen = !hangarOpen;
        lastEvent = hangarOpen ? "Main hangar opening. Click it or press G to release stored vehicles." : "Main hangar sealed.";
    }

    private void HandlePulseInput()
    {
        if (!WasKeyPressedThisFrame(Key.Space))
            return;

        if (sand < 28f || wind < 6f)
        {
            lastEvent = "Ankh pulse needs 28 sand and 6 wind.";
            return;
        }

        sand -= 28f;
        wind -= 6f;
        int hitCount = 0;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            if (FlatDistance(enemy.transform.position, battlePyramid.position) <= pulseRadius)
            {
                enemy.health -= pulseDamage;
                hitCount++;
                CreateBeam(battlePyramid.position + Vector3.up * 2.2f, enemy.transform.position + Vector3.up * 0.8f, new Color(1f, 0.82f, 0.25f, 1f), 0.08f, 0.18f);
            }
        }

        CreatePulseRing();
        CleanupDeadEnemies();
        lastEvent = "Ankh pulse discharged. Red contacts hit: " + hitCount + ".";
    }

    private Vector3 ReadMoveInput()
    {
        Vector3 rawInput = Vector3.zero;
        if (IsKeyPressed(Key.W) || IsKeyPressed(Key.UpArrow))
            rawInput.z += 1f;
        if (IsKeyPressed(Key.S) || IsKeyPressed(Key.DownArrow))
            rawInput.z -= 1f;
        if (IsKeyPressed(Key.D) || IsKeyPressed(Key.RightArrow))
            rawInput.x += 1f;
        if (IsKeyPressed(Key.A) || IsKeyPressed(Key.LeftArrow))
            rawInput.x -= 1f;

        if (rawInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        if (mainCamera == null)
            return rawInput;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        return forward * rawInput.z + right * rawInput.x;
    }

    private bool IsKeyPressed(Key key)
    {
        Keyboard keyboard = ResolveKeyboard();
        if (keyboard == null)
            return false;

        KeyControl control = keyboard[key];
        return control != null && control.isPressed;
    }

    private bool WasKeyPressedThisFrame(Key key)
    {
        Keyboard keyboard = ResolveKeyboard();
        if (keyboard == null)
            return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    private bool WasKeyReleasedThisFrame(Key key)
    {
        Keyboard keyboard = ResolveKeyboard();
        if (keyboard == null)
            return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasReleasedThisFrame;
    }

    private Keyboard ResolveKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.added && keyboard.enabled)
            return keyboard;

        for (int i = 0; i < InputSystem.devices.Count; i++)
        {
            keyboard = InputSystem.devices[i] as Keyboard;
            if (keyboard != null && keyboard.added && keyboard.enabled)
                return keyboard;
        }

        return null;
    }

    private void HandleWeaponInput(float dt)
    {
        autoCannonTimer -= dt;
        howitzerTimer -= dt;
        missileTimer -= dt;
        nuclearTimer -= dt;
        beamCooldownTimer -= dt;

        if (commandBridgePresentationActive)
            return;

        Mouse mouse = Mouse.current;
        if (gunnerSide == 0 && !apexTargetingMode && !commandCursorMode && mouse != null && !IsStrategicPointerOverUI() && mouse.leftButton.isPressed && autoCannonTimer <= 0f)
        {
            autoCannonTimer = 0.09f;
            FireAutocannons(false);
        }

        if (WasKeyPressedThisFrame(Key.Q))
            ToggleHowitzerGunner(-1);
        if (WasKeyPressedThisFrame(Key.E))
            ToggleHowitzerGunner(1);
        if (WasKeyPressedThisFrame(Key.Z))
            LaunchGuidedMissile();
        if (WasKeyPressedThisFrame(Key.X))
            LaunchGuidedSunCoreMissile();
        if (WasKeyPressedThisFrame(Key.V))
            LaunchGuidedAnubisMissile();

        if (WasKeyPressedThisFrame(Key.R))
        {
            StopApexUiCharge();
            if (beamCooldownTimer <= 0f)
                EnterApexTargetingMode();
            else
                lastEvent = "Apex sunlight focus is rebuilding: " + Mathf.CeilToInt(beamCooldownTimer) + "s.";
        }

        if (apexTargetingMode && IsKeyPressed(Key.R) && beamCooldownTimer <= 0f)
        {
            beamCharge = Mathf.Clamp01(beamCharge + dt / 2.4f);
            lastEvent = "Condensing sunlight inside the Apex: " + Mathf.RoundToInt(beamCharge * 100f) + "%.";
        }

        if (apexTargetingMode && WasKeyReleasedThisFrame(Key.R))
        {
            if (beamCharge >= 0.25f && beamCooldownTimer <= 0f)
                FireChargedBeam();
            else
                lastEvent = "Apex focus dispersed. Hold R longer.";
            beamCharge = 0f;
            ExitApexTargetingMode();
        }

        if (apexTargetingMode && apexUiCharging && mouse != null &&
            mouse.leftButton.wasPressedThisFrame && beamCharge >= 0.25f && beamCooldownTimer <= 0f)
        {
            FireChargedBeam();
            beamCharge = 0f;
            apexUiCharging = false;
            ExitApexTargetingMode();
        }
    }

    private void HandleCameraModeInput()
    {
        if (commandBridgePresentationActive)
            return;

        if (gunnerSide != 0)
            return;

        if (WasKeyPressedThisFrame(Key.Tab))
            SetCommandCursorMode(!commandCursorMode);
        if (WasKeyPressedThisFrame(Key.Escape))
            SetCommandCursorMode(true);

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (!commandCursorMode && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector2 scroll = mouse.scroll.ReadValue();
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            float wheelSteps = Mathf.Abs(scroll.y) > 10f ? scroll.y / 120f : scroll.y;
            float zoomSpeed = cameraFollowSelectionMode ? 4.2f : 9.5f;
            float minDistance = cameraFollowSelectionMode ? 18f : 36f;
            float maxDistance = 140f;
            if (cameraFollowSelectionMode)
                GetRTSCameraFollowZoomRange(cameraFollowTarget, out minDistance, out maxDistance);
            cameraDistance = Mathf.Clamp(cameraDistance - wheelSteps * zoomSpeed, minDistance, maxDistance);
        }

        if (commandCursorMode && mouse.rightButton.wasPressedThisFrame)
        {
            commandRightMouseDown = true;
            commandRightMouseStart = mouse.position.ReadValue();
            commandRightMouseOrbitDrag = false;
        }

        if (commandCursorMode && commandRightMouseDown && mouse.rightButton.isPressed)
        {
            float dragDistance = Vector2.Distance(commandRightMouseStart, mouse.position.ReadValue());
            if (dragDistance > CommandRightOrbitDragPixels)
                commandRightMouseOrbitDrag = true;
        }

        bool dragOrbit = mouse.middleButton.isPressed ||
            (!commandCursorMode && mouse.rightButton.isPressed) ||
            (commandCursorMode && commandRightMouseOrbitDrag && mouse.rightButton.isPressed);
        bool pointerOrbit = !commandCursorMode || dragOrbit;
        if (commandCursorMode && IsStrategicPointerOverUI() && !dragOrbit)
            pointerOrbit = false;

        if (WasKeyPressedThisFrame(Key.C))
        {
            if (commandCursorMode)
            {
                if (cameraFollowSelectionMode)
                {
                    StopRTSCameraFollow("Camera follow released. Select a squad and press C to follow again.");
                    return;
                }

                if (TryStartRTSCameraFollow())
                    return;

                lastEvent = "Select a squad, builder, turret, or blueprint, then press C to follow it.";
                return;
            }

            StopRTSCameraFollow(null);
            cameraYaw = battlePyramid.eulerAngles.y;
            cameraPitch = 24f;
            lastEvent = "Camera re-centered behind the pyramid.";
        }

        float yawInput = 0f;
        if (IsKeyPressed(Key.J))
            yawInput -= 1f;
        if (IsKeyPressed(Key.L))
            yawInput += 1f;

        float pitchInput = 0f;
        if (IsKeyPressed(Key.I))
            pitchInput += 1f;
        if (IsKeyPressed(Key.K))
            pitchInput -= 1f;

        float dt = Time.deltaTime;
        cameraFollowManualOrbitTimer = Mathf.Max(0f, cameraFollowManualOrbitTimer - dt);
        if (Mathf.Abs(yawInput) > 0.01f || Mathf.Abs(pitchInput) > 0.01f)
            cameraFollowManualOrbitTimer = 2.2f;

        cameraYaw += yawInput * 96f * dt;
        cameraPitch = Mathf.Clamp(cameraPitch - pitchInput * 56f * dt, 10f, 58f);

        if (!pointerOrbit)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude > 0.01f)
            cameraFollowManualOrbitTimer = 2.2f;
        cameraYaw += delta.x * mouseLookSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y * mouseLookSensitivity, 10f, 58f);
    }

    private void HandleCommandModeMouse()
    {
        if (!commandCursorMode || mainCamera == null || Mouse.current == null)
            return;
        Mouse mouse = Mouse.current;
        if (mouse.rightButton.wasReleasedThisFrame && commandRightMouseOrbitDrag)
        {
            commandRightMouseDown = false;
            commandRightMouseOrbitDrag = false;
            return;
        }
        if (IsStrategicPointerOverUI() || IsPointerOverTouchOfHorusContext(mouse.position.ReadValue()))
        {
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                commandRightMouseDown = false;
                commandRightMouseOrbitDrag = false;
            }
            return;
        }

        if (HandleRTSCommandModeMouse())
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 600f))
            {
                if (TrySelectStrategicObject(hit.transform))
                    return;

                if (IsHangarClick(hit.transform))
                {
                    TryReleaseHangarUnit(null);
                    return;
                }

                if (HasStrategicSelection())
                {
                    IssueSelectedStrategicOrder(hit.point);
                }
                else
                {
                    commandDestination = hit.point;
                    commandDestination.y = battlePyramid.position.y;
                    hasCommandDestination = true;
                    PlaceCommandMarker(hit.point);
                    lastEvent = "Command route plotted. The pyramid is rolling to the marker.";
                }
            }
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            commandRightMouseDown = true;
            commandRightMouseStart = mouse.position.ReadValue();
            commandRightMouseOrbitDrag = false;
        }

        if (mouse.rightButton.wasReleasedThisFrame && commandRightMouseDown)
        {
            Vector2 releasePosition = mouse.position.ReadValue();
            float dragDistance = Vector2.Distance(commandRightMouseStart, releasePosition);
            bool wasOrbitDrag = commandRightMouseOrbitDrag || dragDistance > CommandRightOrbitDragPixels;
            commandRightMouseDown = false;
            commandRightMouseOrbitDrag = false;
            if (wasOrbitDrag)
                return;

            if (dragDistance <= CommandRightOrbitDragPixels)
            {
                Ray ray = mainCamera.ScreenPointToRay(releasePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 600f))
                {
                    if (HasStrategicSelection())
                        IssueSelectedStrategicOrder(hit.point);
                    else
                        TryReleaseHangarUnit(hit.point);
                }
                else
                    TryReleaseHangarUnit(null);
            }
        }
    }

    private bool IsHangarClick(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("hangar"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private void SetCommandCursorMode(bool enabled)
    {
        if (!enabled)
            StopRTSCameraFollow(null);
        commandRightMouseDown = false;
        commandRightMouseOrbitDrag = false;
        commandCursorMode = enabled;
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enabled;
        lastEvent = enabled ? "Command cursor mode. Click UI or units; RMB/MMB drag rotates camera." : "Drive camera mode. Mouse rotates camera; Tab returns cursor.";
    }

    private void PlaceCommandMarker(Vector3 position)
    {
        if (commandMarker == null)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "SandRunners_Command_Move_Marker";
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
                Destroy(markerCollider);

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = commandMaterial;

            commandMarker = marker.transform;
        }

        commandMarker.position = new Vector3(position.x, 0.08f, position.z);
        commandMarker.localScale = new Vector3(2.4f, 0.025f, 2.4f);
    }

    private void FireAutocannons(bool passive)
    {
        EnemyUnit target = FindNearestEnemy(battlePyramid.position, passive ? pyramidWeaponRange : 46f);
        if (target == null || target.transform == null)
        {
            if (!passive)
                lastEvent = "30-mm cannon crews have no target.";
            return;
        }

        TrackCombatTarget(target, passive ? "AUTO-CANNON TARGET" : "30-MM TARGET", passive ? 2.2f : 3.2f);
        int shotCount = Mathf.Max(2, autoCannonMuzzles.Count);
        for (int i = 0; i < shotCount; i++)
        {
            Vector3 muzzle = GetWeaponPoint(autoCannonMuzzles, i, new Vector3(i % 2 == 0 ? -3.2f : 3.2f, 5.5f, 3.4f));
            Vector3 targetPoint = target.transform.position + Vector3.up * 0.75f + Random.insideUnitSphere * 0.35f;
            target.health -= passive ? 4f : 7f;
            CreateBeam(muzzle, targetPoint, new Color(1f, 0.76f, 0.22f, 1f), 0.045f, 0.08f);
            CreateWeaponTracer(muzzle, targetPoint, new Color(1f, 0.66f, 0.16f, 1f), 0.038f, 0.12f);
            if (!passive)
                CreateWeaponFlash(muzzle, 0.16f, new Color(1f, 0.68f, 0.22f, 1f));
        }

        RegisterWeaponImpulse(passive ? 0.018f : 0.045f);
        PlaySandRunnerSound(SandRunnerSound.Autocannon, battlePyramid.position, passive ? 0.48f : 0.82f);
        if (!passive)
            lastEvent = "30-mm batteries stitching the target.";
        CleanupDeadEnemies();
    }

    private void FireHowitzers(int side)
    {
        if (howitzerTimer > 0f)
        {
            lastEvent = "Front 300-mm batteries reloading.";
            return;
        }

        if (sand < 18f)
        {
            lastEvent = "300-mm shells need 18 sand propellant.";
            return;
        }

        sand -= 18f;
        howitzerTimer = 3.2f;
        Vector3 batteryDirection = battlePyramid.forward;
        EnemyUnit target = FindNearestEnemyInDirection(battlePyramid.position, batteryDirection, 150f);
        Vector3 targetPosition = target != null && target.transform != null
            ? target.transform.position
            : battlePyramid.position + batteryDirection * 105f + Vector3.up * 3f;
        CreateReadableImpactWarning(targetPosition, 8f, new Color(1f, 0.72f, 0.18f, 1f), 1.5f, "300-MM FRONT IMPACT", false);
        if (target != null && target.transform != null)
            TrackCombatTarget(target, "FRONT BATTERY TARGET", 4f);

        int firstIndex = side < 0 ? 0 : 2;
        for (int i = 0; i < 2; i++)
        {
            int muzzleIndex = firstIndex + i;
            Vector3 fallback = new Vector3(side < 0 ? -3.1f + i : 2.0f + i, 2.2f + i * 0.7f, 3.4f);
            Vector3 muzzle = GetWeaponPoint(howitzerMuzzles, muzzleIndex, fallback);
            Vector3 direction = (targetPosition + Random.insideUnitSphere * 2.1f - muzzle).normalized;
            Vector3 velocity = direction * 48f + Vector3.up * 5.5f;
            CreateProjectile("300mm_Front_Battery_Shell", muzzle, velocity, shellMaterial, new Vector3(0.35f, 0.7f, 0.35f), 72f, 8f, false);
            CreateWeaponFlash(muzzle, 0.5f, new Color(1f, 0.46f, 0.14f, 1f));
            RegisterFrontHowitzerRecoil(muzzleIndex);
        }

        RegisterWeaponImpulse(0.2f);
        PlaySandRunnerSound(SandRunnerSound.ArtilleryFire, battlePyramid.position, 1.12f);
        PlaySandRunnerSound(SandRunnerSound.ArtilleryShell, Vector3.Lerp(battlePyramid.position, targetPosition, 0.55f), 0.72f);
        ArmHowitzerReloadSound();
        lastEvent = side < 0 ? "Left front 300-mm battery fired." : "Right front 300-mm battery fired.";
    }

    private void FireChargedBeam()
    {
        float charge = beamCharge;
        beamCooldownTimer = 4.5f;
        GetApexFireSolution(charge, out Vector3 start, out Vector3 end);

        CreateBeam(start, end, new Color(1f, 0.95f, 0.38f, 1f), Mathf.Lerp(0.18f, 0.55f, charge), 0.32f);
        DamageEnemiesAlongLine(start, end, Mathf.Lerp(5f, 11f, charge), Mathf.Lerp(110f, 260f, charge));
        CreateExplosion(end, Mathf.Lerp(8f, 18f, charge), Mathf.Lerp(80f, 180f, charge), false);
        CreateWeaponFlash(start, Mathf.Lerp(0.8f, 1.45f, charge), new Color(1f, 0.95f, 0.42f, 1f));
        RegisterWeaponImpulse(Mathf.Lerp(0.16f, 0.34f, charge));
        PlayApexDischargeSound(charge);
        lastEvent = "Apex mirror focus discharged at " + Mathf.RoundToInt(charge * 100f) +
                    "% // range " + Mathf.RoundToInt(Vector3.Distance(start, end)) + "m.";
    }

    private void FireMissile(bool nuclear)
    {
        if (nuclear)
        {
            if (nuclearTimer > 0f)
            {
                lastEvent = "Sun-core missile safeties are cycling.";
                return;
            }
            if (nuclearMissiles <= 0)
            {
                lastEvent = "No sun-core missiles loaded.";
                return;
            }
            nuclearMissiles--;
            nuclearTimer = 16f;
        }
        else
        {
            if (missileTimer > 0f)
            {
                lastEvent = "Cruise missile bay is reloading.";
                return;
            }
            if (cruiseMissiles <= 0)
            {
                lastEvent = "No cruise missiles loaded.";
                return;
            }
            cruiseMissiles--;
            missileTimer = 4.5f;
        }

        EnemyUnit target = FindNearestEnemy(battlePyramid.position, nuclear ? 160f : 110f);
        Vector3 start = GetCruiseLaunchPoint(nuclear);
        Vector3 fallbackTarget = target != null && target.transform != null
            ? target.transform.position
            : battlePyramid.position + battlePyramid.forward * (nuclear ? 105f : 78f);

        GameObject missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missileObject.name = nuclear ? "Sun_Core_Ballistic_Missile" : "Golden_Cruise_Missile";
        missileObject.transform.position = start;
        missileObject.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        missileObject.transform.localScale = nuclear ? new Vector3(0.8f, 1.9f, 0.8f) : new Vector3(0.42f, 1.25f, 0.42f);
        Renderer renderer = missileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = nuclear ? nuclearMaterial : missileMaterial;
        AttachProjectileTrail(missileObject, nuclear ? new Color(1f, 0.2f, 0.05f, 1f) : new Color(1f, 0.78f, 0.2f, 1f), nuclear ? 0.46f : 0.22f, nuclear ? 1.15f : 0.72f);

        MissileVisual missile = new MissileVisual();
        missile.transform = missileObject.transform;
        missile.target = target != null ? target.transform : null;
        missile.velocity = Vector3.up * (nuclear ? 24f : 18f) + battlePyramid.forward * 8f;
        missile.fallbackTarget = fallbackTarget;
        missile.speed = nuclear ? 42f : 36f;
        missile.turnRate = nuclear ? 1.8f : 3.8f;
        missile.life = nuclear ? 9f : 7f;
        missile.damage = nuclear ? 420f : 145f;
        missile.blastRadius = nuclear ? 34f : 14f;
        missile.nuclear = nuclear;
        missiles.Add(missile);
        CreateWeaponFlash(start, nuclear ? 0.85f : 0.48f, nuclear ? new Color(1f, 0.2f, 0.08f, 1f) : new Color(1f, 0.72f, 0.22f, 1f));
        RegisterWeaponImpulse(nuclear ? 0.28f : 0.12f);
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, start, nuclear ? 1.05f : 0.82f);
        PlaySandRunnerSound(SandRunnerSound.MissileFlyby, start + battlePyramid.forward * 16f, nuclear ? 0.9f : 0.62f);
        lastEvent = nuclear ? "Sun-core ballistic missile launched." : "Cruise missile away.";
    }

    private Vector3 GetWeaponPoint(List<Transform> points, int index, Vector3 fallbackLocal)
    {
        if (points.Count > 0)
            return points[Mathf.Abs(index) % points.Count].position;
        return battlePyramid.TransformPoint(fallbackLocal);
    }

    private EnemyUnit FindNearestEnemyInDirection(Vector3 origin, Vector3 direction, float range)
    {
        EnemyUnit best = null;
        float bestScore = range;
        direction.y = 0f;
        direction.Normalize();
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null)
                continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;
            if (distance > range || distance < 0.01f)
                continue;

            float alignment = Vector3.Dot(direction, toEnemy.normalized);
            if (alignment < 0.25f)
                continue;

            float score = distance / Mathf.Max(0.2f, alignment);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }

    private void DamageEnemiesAlongLine(Vector3 start, Vector3 end, float radius, float damage)
    {
        Vector3 segment = end - start;
        float lengthSq = segment.sqrMagnitude;
        if (lengthSq < 0.001f)
            return;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null)
                continue;

            Vector3 enemyPoint = enemy.transform.position + Vector3.up * 0.8f;
            float t = Mathf.Clamp01(Vector3.Dot(enemyPoint - start, segment) / lengthSq);
            Vector3 closest = start + segment * t;
            if (Vector3.Distance(enemyPoint, closest) <= radius)
                enemy.health -= damage;
        }
        CleanupDeadEnemies();
    }

    private void UpdateResources(float dt)
    {
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node.transform == null)
                continue;

            float captureStrength = GetResourceCaptureStrength(node);
            if (captureStrength > 0f)
            {
                node.capture = Mathf.Clamp01(node.capture + dt *
                    SandRunnersResourceDevelopmentRules.CaptureRate(
                        captureStrength, balanceProfile.resourceCapturePerDeveloper));
                if (!node.controlled && node.capture >= 1f)
                {
                    node.controlled = true;
                    lastEvent = "Captured " + node.label + ". Developers may now raise permanent infrastructure.";
                    ShowBanner("RESOURCE SECURED // DEVELOPMENT UNLOCKED", 2.4f);
                }
            }
            else if (!node.controlled)
            {
                node.capture = Mathf.Max(0f, node.capture - dt * 0.045f);
            }

            if (node.controlled)
            {
                float incomeMultiplier = GetResourceDevelopmentIncomeMultiplier(node);
                if (node.kind == ResourceKind.Gold)
                    gold += balanceProfile.goldNodeIncome * incomeMultiplier * dt;
                else if (node.kind == ResourceKind.Wind)
                    wind += balanceProfile.windNodeIncome * incomeMultiplier * dt;
                else
                    sand += balanceProfile.sandNodeIncome * incomeMultiplier * dt;
            }

            if (node.markerRenderer != null)
                node.markerRenderer.sharedMaterial = node.controlled ? controlledMaterial : contestedMaterial;
        }

        sand = Mathf.Clamp(sand, 0f, 2200f);
        gold = Mathf.Clamp(gold, 0f, 1800f);
        wind = Mathf.Clamp(wind, 0f, Mathf.Max(50f, windCapacity));
    }

    private void UpdateWaves(float dt)
    {
        waveTimer -= dt;
        if (waveTimer > 0f)
            return;

        int activePursuers = CountStandardRedPursuers();
        if (activePursuers >= 4)
        {
            waveTimer = 28f;
            lastEvent = "Red fleet contacts are circling outside the main battle line: " + activePursuers + ".";
            return;
        }

        waveIndex++;
        int spawnCount = Mathf.Min(Mathf.Clamp(1 + waveIndex / 3, 1, 3), Mathf.Max(1, 4 - activePursuers));
        for (int i = 0; i < spawnCount; i++)
            SpawnEnemy(i);

        waveTimer = Mathf.Max(44f, 92f - waveIndex * 4.5f);
        lastEvent = "Distant Red Elemental patrol " + waveIndex + " skimmed into New Galikarnass.";
        PlaySandRunnerSound(SandRunnerSound.WaveAlert, battlePyramid.position, 0.7f);
        ShowBanner("WAVE " + waveIndex + " — RED ELEMENTALS INCOMING", 3f);
    }

    private int CountStandardRedPursuers()
    {
        int count = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy != null && enemy.transform != null && !IsMandarinkaControlledEnemy(enemy.transform))
                count++;
        }
        return count;
    }

    private void SpawnEnemy(int offsetIndex)
    {
        Vector3 spawnPosition;
        if (enemySpawnPoints.Count > 0)
        {
            Transform point = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Count)];
            Vector2 jitter = Random.insideUnitCircle * 4f;
            spawnPosition = point.position + new Vector3(jitter.x, 0.6f, jitter.y);
        }
        else
        {
            Vector2 edge = Random.insideUnitCircle.normalized * 54f;
            spawnPosition = new Vector3(edge.x, 0.6f, edge.y);
        }

        GameObject enemy = new GameObject("Red_Elemental_Pursuer");
        enemy.name = "Red_Elemental_Pursuer";
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        BuildRedPursuerVisual(enemy.transform);

        EnemyUnit unit = new EnemyUnit();
        unit.transform = enemy.transform;
        unit.health = 58f + waveIndex * 8f + offsetIndex * 1.5f;
        unit.maxHealth = unit.health;
        enemies.Add(unit);
    }

    private void BuildRedPursuerVisual(Transform root)
    {
        CreateBox(root, "Pursuer_Red_Assault_Hull", new Vector3(0f, 0.48f, 0f), Quaternion.identity, new Vector3(1.05f, 0.42f, 1.55f), enemyMaterial);
        CreateBox(root, "Pursuer_Black_Belly", new Vector3(0f, 0.23f, -0.06f), Quaternion.identity, new Vector3(1.22f, 0.18f, 1.7f), pyramidDarkArmorMaterial);
        CreateBox(root, "Pursuer_Red_Command_Fin", new Vector3(0f, 0.86f, -0.15f), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.22f, 0.72f, 0.72f), enemyMaterial);
        CreateCylinder(root, "Pursuer_Front_Cannon", new Vector3(0f, 0.66f, 0.94f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.08f, 0.46f, 0.08f), pyramidDarkArmorMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCylinder(root, "Pursuer_Wheel_Front_" + side, new Vector3(side * 0.7f, 0.22f, 0.48f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.22f, 0.08f, 0.22f), pyramidDarkArmorMaterial);
            CreateCylinder(root, "Pursuer_Wheel_Rear_" + side, new Vector3(side * 0.7f, 0.22f, -0.5f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.22f, 0.08f, 0.22f), pyramidDarkArmorMaterial);
        }

        Light marker = new GameObject("Pursuer_Red_Marker").AddComponent<Light>();
        marker.transform.SetParent(root, false);
        marker.transform.localPosition = new Vector3(0f, 0.92f, 0.48f);
        marker.type = LightType.Point;
        marker.color = new Color(1f, 0.06f, 0.035f, 1f);
        marker.intensity = 0.75f;
        marker.range = 5f;
    }

    private void UpdateRunners(float dt)
    {
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            RunnerUnit runner = runners[i];
            if (runner.transform == null || runner.health <= 0f)
            {
                if (runner.transform != null)
                    Destroy(runner.transform.gameObject);
                runners.RemoveAt(i);
                continue;
            }

            runner.fireCooldown -= dt;
            if (runner.squad != null)
            {
                UpdateRTSRunnerUnit(runner, i, dt);
                continue;
            }

            EnemyUnit target = FindNearestEnemy(runner.transform.position, runner.range + 4f);
            if (target != null && target.transform != null)
            {
                Vector3 toTarget = target.transform.position - runner.transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                if (distance > runner.range * 0.62f)
                    runner.transform.position += toTarget.normalized * runner.speed * dt;
                RotateToward(runner.transform, toTarget, 360f * dt);

                if (runner.fireCooldown <= 0f && distance <= runner.range)
                {
                    runner.fireCooldown = 0.85f;
                    target.health -= runner.damage;
                    CreateBeam(runner.transform.position + Vector3.up * 0.5f, target.transform.position + Vector3.up * 0.65f, new Color(1f, 0.68f, 0.2f, 1f), 0.04f, 0.12f);
                }
            }
            else if (runner.hasOrder && !runner.autonomous)
            {
                Vector3 toOrder = runner.orderPosition - runner.transform.position;
                toOrder.y = 0f;
                if (toOrder.magnitude <= 1.8f)
                {
                    runner.hasOrder = false;
                }
                else
                {
                    runner.transform.position += toOrder.normalized * runner.speed * dt;
                    RotateToward(runner.transform, toOrder, 260f * dt);
                }
            }
            else
            {
                float angle = Time.time * 32f + i * 55f;
                Vector3 guardOffset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (5f + (i % 3) * 1.4f);
                Vector3 guardPoint = runner.autonomous && enemies.Count > 0
                    ? battlePyramid.position + battlePyramid.forward * 22f + guardOffset * 1.8f
                    : battlePyramid.position + guardOffset + Vector3.up * 0.65f;
                Vector3 toGuard = guardPoint - runner.transform.position;
                toGuard.y = 0f;
                runner.transform.position += Vector3.ClampMagnitude(toGuard, runner.speed * 0.8f * dt);
                RotateToward(runner.transform, toGuard, 240f * dt);
            }

            Vector3 position = runner.transform.position;
            float groundHeight = GetPlayableGroundHeight(position);
            position.y = runner.airborne
                ? groundHeight + runner.flightHeight + Mathf.Sin(Time.time * 1.7f + i * 0.53f) * 0.35f
                : groundHeight + 0.34f;
            runner.transform.position = position;
        }

        CleanupDeadEnemies();
    }

    private void UpdateEnemies(float dt)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null || enemy.health <= 0f)
            {
                HandleLivingWorldEnemyDestroyed(enemy);
                HandleImperialEnemyDestroyed(enemy);
                if (enemy.transform != null)
                    CreateEnemySalvageWreckage(enemy);
                if (enemy.transform != null)
                    HandleMandarinkaEnemyDestroyed(enemy.transform);
                if (enemy.transform != null)
                    Destroy(enemy.transform.gameObject);
                enemies.RemoveAt(i);
                gold += 8f;
                sand += 5f;
                continue;
            }

            enemy.fireCooldown -= dt;
            if (enemy.isStrategicMissile || enemy.isAssaultHeadquarters)
                continue;
            if (enemy.isJuzzherBarge)
            {
                UpdateJuzzherBargeEnemy(enemy, dt);
                continue;
            }
            if (enemy.factionTag == "NEUTRAL_SETTLEMENT")
            {
                UpdateHostileNeutralSettlementEnemy(enemy, dt);
                continue;
            }
            if (enemy.factionTag == "NASHORN_RETALIATION")
            {
                UpdateNashornRetaliationEnemy(enemy, dt);
                continue;
            }
            if (IsMandarinkaControlledEnemy(enemy.transform))
                continue;

            Vector3 toPyramid = battlePyramid.position - enemy.transform.position;
            toPyramid.y = 0f;
            float distanceToPyramid = toPyramid.magnitude;

            RunnerUnit runnerTarget = FindNearestRunner(enemy.transform.position, 9f);
            if (runnerTarget != null && runnerTarget.transform != null)
            {
                Vector3 toRunner = runnerTarget.transform.position - enemy.transform.position;
                toRunner.y = 0f;
                float runnerDistance = toRunner.magnitude;
                if (runnerDistance > 4.2f)
                    enemy.transform.position += toRunner.normalized * 5.4f * dt;
                RotateToward(enemy.transform, toRunner, 260f * dt);

                if (enemy.fireCooldown <= 0f && runnerDistance <= 6.8f)
                {
                    enemy.fireCooldown = 1.1f;
                    runnerTarget.health -= 16f;
                    CreateBeam(enemy.transform.position + Vector3.up * 0.6f, runnerTarget.transform.position + Vector3.up * 0.45f, new Color(1f, 0.05f, 0.02f, 1f), 0.035f, 0.12f);
                }
            }
            else
            {
                if (distanceToPyramid > 3.7f)
                    enemy.transform.position += toPyramid.normalized * 4.7f * dt;
                RotateToward(enemy.transform, toPyramid, 220f * dt);

                if (distanceToPyramid <= 5.4f)
                {
                    pyramidHull -= 18f * dt;
                    if (enemy.fireCooldown <= 0f)
                    {
                        enemy.fireCooldown = 0.8f;
                        CreateBeam(enemy.transform.position + Vector3.up * 0.6f, battlePyramid.position + Vector3.up * 1.3f, new Color(1f, 0.04f, 0.03f, 1f), 0.045f, 0.12f);
                        PlaySandRunnerSound(SandRunnerSound.PyramidDamage, battlePyramid.position, 0.25f);
                    }
                }
            }
        }

        pyramidHull = Mathf.Clamp(pyramidHull, 0f, pyramidMaxHull);
    }

    private void UpdatePyramidWeapon(float dt)
    {
        pyramidFireTimer -= dt;
        if (pyramidFireTimer > 0f)
            return;

        pyramidFireTimer = pyramidWeaponCooldown;
        FireAutocannons(true);
    }

    private void CreateProjectile(string name, Vector3 position, Vector3 velocity, Material material, Vector3 scale, float damage, float blastRadius, bool nuclear)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = name;
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = scale;
        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        AttachProjectileTrail(projectileObject, nuclear ? new Color(1f, 0.24f, 0.08f, 1f) : new Color(1f, 0.58f, 0.14f, 1f), nuclear ? 0.4f : 0.18f, nuclear ? 0.95f : 0.5f);

        ProjectileVisual projectile = new ProjectileVisual();
        projectile.transform = projectileObject.transform;
        projectile.velocity = velocity;
        projectile.life = 5f;
        projectile.damage = damage;
        projectile.blastRadius = blastRadius;
        projectile.nuclear = nuclear;
        projectiles.Add(projectile);
    }

    private void UpdateProjectiles(float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileVisual projectile = projectiles[i];
            if (projectile.transform == null)
            {
                projectiles.RemoveAt(i);
                continue;
            }

            projectile.life -= dt;
            projectile.velocity += Vector3.down * 10f * dt;
            projectile.transform.position += projectile.velocity * dt;
            if (projectile.velocity.sqrMagnitude > 0.01f)
                projectile.transform.rotation = Quaternion.LookRotation(projectile.velocity.normalized, Vector3.up);

            EnemyUnit hitTarget = FindNearestEnemy(projectile.transform.position, projectile.blastRadius * 0.7f);
            bool hitEnemy = hitTarget != null;
            if (projectile.transform.position.y <= 0.25f || projectile.life <= 0f || hitEnemy)
            {
                Vector3 explosionPoint = projectile.transform.position;
                if (hitTarget != null)
                    TrackCombatTarget(hitTarget, projectile.nuclear ? "SUN-CORE HIT" : "DIRECT HIT", 4.5f);
                Destroy(projectile.transform.gameObject);
                projectiles.RemoveAt(i);
                CreateExplosion(explosionPoint, projectile.blastRadius, projectile.damage, projectile.nuclear);
            }
        }
    }

    private void UpdateMissiles(float dt)
    {
        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            MissileVisual missile = missiles[i];
            if (missile.transform == null)
            {
                missiles.RemoveAt(i);
                continue;
            }

            missile.life -= dt;
            Vector3 targetPosition = missile.target != null ? missile.target.position + Vector3.up * 0.8f : missile.fallbackTarget;
            Vector3 desired = (targetPosition - missile.transform.position).normalized * missile.speed;
            missile.velocity = Vector3.Lerp(missile.velocity, desired, missile.turnRate * dt);
            missile.transform.position += missile.velocity * dt;
            if (missile.velocity.sqrMagnitude > 0.01f)
                missile.transform.rotation = Quaternion.LookRotation(missile.velocity.normalized, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);

            if (Vector3.Distance(missile.transform.position, targetPosition) <= missile.blastRadius * 0.28f || missile.life <= 0f)
            {
                Vector3 explosionPoint = missile.transform.position;
                Destroy(missile.transform.gameObject);
                missiles.RemoveAt(i);
                CreateExplosion(explosionPoint, missile.blastRadius, missile.damage, missile.nuclear);
            }
        }
    }

    private void CreateExplosion(Vector3 position, float radius, float damage, bool nuclear)
    {
        CreateBattleExplosionFx(position, radius, nuclear, false);
        SandRunnerSound explosionSound = nuclear
            ? SandRunnerSound.NuclearExplosion
            : radius >= 11f ? SandRunnerSound.LargeExplosion : SandRunnerSound.Explosion;
        PlaySandRunnerSound(explosionSound, position, nuclear ? 1f : Mathf.Lerp(0.68f, 1f, Mathf.Clamp01(radius / 18f)));

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null)
                continue;

            float distance = FlatDistance(position, enemy.transform.position);
            if (distance <= radius)
            {
                float appliedDamage = damage * Mathf.Lerp(1f, 0.35f, distance / radius);
                enemy.health -= appliedDamage;
                TrackCombatTarget(enemy, nuclear ? "SUN-CORE TARGET" : "SPLASH TARGET", nuclear ? 6f : 4f);
                if (appliedDamage >= 22f)
                    CreateFloatingCombatLabel(enemy.transform.position + Vector3.up * 2.3f, "-" + Mathf.RoundToInt(appliedDamage), nuclear ? new Color(1f, 0.25f, 0.05f, 1f) : new Color(1f, 0.72f, 0.18f, 1f), 0.85f);
            }
        }

        CleanupDeadEnemies();

        RingVisual ring = new RingVisual();
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = nuclear ? "Sun_Core_Shockwave" : "Howitzer_Impact_Shockwave";
        ringObject.transform.position = new Vector3(position.x, 0.09f, position.z);
        ringObject.transform.localScale = new Vector3(1f, 0.03f, 1f);
        Collider collider = ringObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = ringObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = nuclear ? nuclearMaterial : pulseMaterial;

        ring.transform = ringObject.transform;
        ring.life = nuclear ? 0.85f : 0.42f;
        ring.maxLife = ring.life;
        ring.startScale = new Vector3(1f, 0.03f, 1f);
        ring.endScale = new Vector3(radius * 2f, 0.03f, radius * 2f);
        rings.Add(ring);

        Light flash = new GameObject(nuclear ? "Sun_Core_Flash" : "Impact_Flash").AddComponent<Light>();
        flash.transform.position = position + Vector3.up * 2f;
        flash.type = LightType.Point;
        flash.color = nuclear ? new Color(1f, 0.42f, 0.15f, 1f) : new Color(1f, 0.78f, 0.22f, 1f);
        flash.intensity = nuclear ? 7f : 2.5f;
        flash.range = nuclear ? radius * 2.5f : radius * 1.8f;
        Destroy(flash.gameObject, nuclear ? 0.75f : 0.35f);
    }

    private EnemyUnit FindNearestEnemy(Vector3 origin, float range)
    {
        EnemyUnit best = null;
        float bestDistance = range;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null || enemy.isStrategicMissile)
                continue;

            float distance = FlatDistance(origin, enemy.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }
        return best;
    }

    private RunnerUnit FindNearestRunner(Vector3 origin, float range)
    {
        RunnerUnit best = null;
        float bestDistance = range;
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner.transform == null)
                continue;

            float distance = FlatDistance(origin, runner.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = runner;
            }
        }
        return best;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy.transform == null)
            {
                enemies.RemoveAt(i);
            }
            else if (enemy.health <= 0f)
            {
                CreateEnemySalvageWreckage(enemy);
                HandleImperialEnemyDestroyed(enemy);
                HandleMandarinkaEnemyDestroyed(enemy.transform);
                Destroy(enemy.transform.gameObject);
                enemies.RemoveAt(i);
                gold += 8f;
                sand += 5f;
            }
        }
    }

    private void RotateToward(Transform target, Vector3 direction, float maxDegrees)
    {
        if (target == null || direction.sqrMagnitude < 0.001f)
            return;

        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        target.rotation = Quaternion.RotateTowards(target.rotation, desired, maxDegrees);
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void CreateBeam(Vector3 start, Vector3 end, Color color, float width, float life)
    {
        GameObject beamObject = new GameObject("SandRunners_Beam");
        LineRenderer line = beamObject.AddComponent<LineRenderer>();
        float readableWidth = Mathf.Max(width * 1.55f, 0.055f);
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = readableWidth;
        line.endWidth = readableWidth;
        line.material = beamMaterial;
        line.startColor = color;
        line.endColor = color;
        line.useWorldSpace = true;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;

        BeamVisual beam = new BeamVisual();
        beam.line = line;
        beam.life = life;
        beams.Add(beam);
        CreateBeamImpactFx(start, end, color, readableWidth);
    }

    private void CreatePulseRing()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ankh_Pulse_Ring";
        ring.transform.position = battlePyramid.position + Vector3.up * 0.05f;
        ring.transform.localScale = new Vector3(1f, 0.025f, 1f);

        Collider collider = ring.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = ring.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = pulseMaterial;

        RingVisual visual = new RingVisual();
        visual.transform = ring.transform;
        visual.life = 0.45f;
        visual.maxLife = 0.45f;
        visual.startScale = new Vector3(1f, 0.025f, 1f);
        visual.endScale = new Vector3(pulseRadius * 2f, 0.025f, pulseRadius * 2f);
        rings.Add(visual);
    }

    private void UpdateVisuals(float dt)
    {
        for (int i = beams.Count - 1; i >= 0; i--)
        {
            BeamVisual beam = beams[i];
            beam.life -= dt;
            if (beam.life <= 0f || beam.line == null)
            {
                if (beam.line != null)
                    Destroy(beam.line.gameObject);
                beams.RemoveAt(i);
            }
        }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            RingVisual ring = rings[i];
            if (ring.transform == null)
            {
                rings.RemoveAt(i);
                continue;
            }

            ring.life -= dt;
            float t = 1f - Mathf.Clamp01(ring.life / ring.maxLife);
            ring.transform.localScale = Vector3.Lerp(ring.startScale, ring.endScale, t);
            if (ring.life <= 0f)
            {
                Destroy(ring.transform.gameObject);
                rings.RemoveAt(i);
            }
        }

        if (commandMarker != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5.2f) * 0.08f;
            commandMarker.localScale = new Vector3(2.4f * pulse, 0.025f, 2.4f * pulse);
        }

        hangarOpenAmount = Mathf.MoveTowards(hangarOpenAmount, hangarOpen ? 1f : 0f, dt * 1.8f);
        if (hangarDoor != null)
            hangarDoor.localPosition = Vector3.Lerp(hangarDoorClosedLocalPosition, hangarDoorClosedLocalPosition + Vector3.down * 1.1f, hangarOpenAmount);
        if (internalUnitDisplay != null)
        {
            internalUnitDisplay.gameObject.SetActive(hangarStored > 0);
            internalUnitDisplay.localRotation *= Quaternion.Euler(0f, 80f * dt, 0f);
        }

        UpdatePyramidArtJuice(dt, hangarOpenAmount, beamCharge);
    }

    private void SetupCameraImmediate()
    {
        if (mainCamera == null || battlePyramid == null)
            return;

        Vector3 focus = GetImmersiveCameraFocus(0f);
        Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        mainCamera.transform.position = focus + orbit * new Vector3(0f, 0f, -cameraDistance);
        mainCamera.transform.rotation = Quaternion.LookRotation(focus - mainCamera.transform.position, Vector3.up);
        mainCamera.fieldOfView = GetImmersiveCameraFieldOfView();
    }

    private void FollowCamera(float dt)
    {
        if (mainCamera == null || battlePyramid == null)
            return;

        if (guidedMissileActive || gunnerSide != 0)
            return;
        if (UpdateApexDiegeticCamera(dt))
            return;

        Vector3 focus = GetImmersiveCameraFocus(dt);
        Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        float framedDistance = cameraFollowSelectionMode
            ? cameraDistance
            : cameraDistance + combatCameraBlend * 18f;
        Vector3 targetPosition = focus + orbit * new Vector3(0f, 0f, -framedDistance);
        Vector3 shakeOffset = GetPyramidCameraShakeOffset(dt);
        targetPosition += cameraFollowSelectionMode ? shakeOffset * 0.25f : shakeOffset;
        Quaternion targetRotation = Quaternion.LookRotation(focus - targetPosition, Vector3.up);
        float smooth = cameraFollowSelectionMode ? cameraSmooth * 1.35f : cameraSmooth;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, smooth * dt);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, smooth * dt);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, GetImmersiveCameraFieldOfView(), dt * 3.4f);
    }

    private void OnGUI()
    {
        if (apexTargetingMode)
        {
            DrawApexTargetingReticle();
            return;
        }

        bool gunnerView = gunnerSide != 0;
        if (!gunnerView)
        {
            DrawRTSOverlayGUI();
            DrawNarrativeCommsGUI();
            DrawGameFlowGUI();
            DrawCinematicDirectorGUI();
        }
        DrawGunnerCrosshair();
        DrawApexTargetingReticle();
        if (!gunnerView && !UseStrategicCanvasHud())
            DrawTouchOfHorusHUD();
        if (!gunnerView && !UseStrategicCanvasHud())
            DrawPyramidExpeditionHUD();
        if (!gunnerView && !UseStrategicCanvasHud())
            DrawRoadGameplayHUD();
        if (!gunnerView)
            DrawGuidedMissileOverlay();

        if (IsGameFlowOverlayBlocking())
            return;

        if (cinematicDirectorActive && hudHidden)
            return;

        EnsureGuiStyles();
        DrawSalvageScarabContextGUI();
        DrawSalvageWorldMarkers();

        if (UseStrategicCanvasHud())
            return;

        if (hudHidden)
        {
            DrawHudHiddenIndicator();
            return;
        }

        float left = 16f;
        float top = 16f;
        float panelWidth = 420f;

        DrawMainStatusPanel(left, top, panelWidth);
        DrawPyramidHealthBar(left, top + 46f, panelWidth);
        DrawResourcePanel();
        DrawMandarinkaGUI();
        DrawMissionDirectorGUI();
        DrawBannerAnnouncement();

        if (enemyAlertTimer > 0f)
            DrawEnemyAlertIndicator();

        MarkDefeatIfNeeded();
    }

    private void EnsureGuiStyles()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 12;
        smallStyle.normal.textColor = new Color(0.9f, 0.94f, 0.92f, 1f);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(1f, 0.78f, 0.28f, 1f);

        warningStyle = new GUIStyle(GUI.skin.label);
        warningStyle.fontSize = 14;
        warningStyle.fontStyle = FontStyle.Bold;
        warningStyle.normal.textColor = new Color(1f, 0.28f, 0.18f, 1f);

        bannerStyle = new GUIStyle(GUI.skin.label);
        bannerStyle.fontSize = 22;
        bannerStyle.fontStyle = FontStyle.Bold;
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.normal.textColor = new Color(1f, 0.75f, 0.18f, 1f);

        hullBarBgStyle = new GUIStyle(GUI.skin.box);
        hullBarFillStyle = new GUIStyle();
    }

    private void DrawHudHiddenIndicator()
    {
        DrawBox(new Rect(16f, 16f, 230f, 34f), new Color(0f, 0f, 0f, 0.55f), new Color(1f, 0.78f, 0.28f, 0.6f));
        GUI.Label(new Rect(28f, 22f, 210f, 22f), "HUD hidden | H show", smallStyle);
    }

    private void DrawBox(Rect rect, Color fill, Color border)
    {
        GUI.color = fill;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = border;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.yMin, 1f, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawFillBar(Rect rect, float ratio, Color fillColor, Color borderColor)
    {
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(rect.xMin - 1f, rect.yMin - 1f, rect.width + 2f, rect.height + 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width * Mathf.Clamp01(ratio), rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawMainStatusPanel(float x, float y, float width)
    {
        float panelHeight = hudExpanded ? 230f : 120f;
        DrawBox(new Rect(x, y, width, panelHeight + 20f), new Color(0f, 0f, 0f, 0.5f), new Color(1f, 0.78f, 0.28f, 0.5f));

        float hullPct = pyramidMaxHull > 0f ? Mathf.Clamp01(pyramidHull / pyramidMaxHull) : 1f;
        bool lowHull = hullPct < 0.25f;

        GUI.Label(new Rect(x + 12f, y + 6f, width - 24f, 22f), "BATTLE FOR UNIVERSE — SAND RUNNERS", titleStyle);
        GUI.Label(new Rect(x + 12f, y + 30f, width - 24f, 18f),
            "Hull " + Mathf.RoundToInt(pyramidHull) + "/" + Mathf.RoundToInt(pyramidMaxHull) +
            "  |  Hangar " + hangarStored + "/6 " + (hangarOpen ? "open" : "sealed") +
            "  |  Units " + runners.Count + "  Reds " + enemies.Count + "  Wave " + waveIndex, smallStyle);

        GUI.Label(new Rect(x + 12f, y + 52f, width - 24f, 18f),
            "Sand " + Mathf.RoundToInt(sand) + "  Gold " + Mathf.RoundToInt(gold) + "  Wind " + Mathf.RoundToInt(wind), labelStyle);

        if (hudExpanded)
        {
            GUI.Label(new Rect(x + 12f, y + 76f, width - 24f, 18f),
                "Apex " + Mathf.RoundToInt(beamCharge * 100f) + "%  |  Cruise " + cruiseMissiles + "  Sun-core " + nuclearMissiles, labelStyle);
            GUI.Label(new Rect(x + 12f, y + 96f, width - 24f, 18f),
                commandCursorMode ? "Command cursor | Tab drive camera" : "Third-person drive | Tab cursor", smallStyle);
            GUI.Label(new Rect(x + 12f, y + 116f, width - 24f, 18f),
                "WASD move  Shift boost  Space pulse  LMB 30-mm  Q/E gunner view", smallStyle);
            GUI.Label(new Rect(x + 12f, y + 136f, width - 24f, 18f),
                "Hold R beam  Z cruise  X sun-core  1 internal  F/G hangar", smallStyle);
            GUI.Label(new Rect(x + 12f, y + 156f, width - 24f, 18f),
                "2/3 vehicles  4 builder  5 turret  F1 toggle details", smallStyle);
            GUI.Label(new Rect(x + 12f, y + 176f, width - 24f, 18f),
                "F5 salvage test  F6 damage test  F7 intro", smallStyle);

            float eventY = y + 200f;
            Color eventColor = pyramidHull <= 100f ? new Color(1f, 0.2f, 0.1f, 1f) : new Color(0.9f, 0.94f, 0.92f, 1f);
            GUI.Label(new Rect(x + 12f, eventY, width - 24f, 22f), lastEvent, lowHull ? warningStyle : smallStyle);
        }
        else
        {
            float eventY = y + 76f;
            Color eventColor = pyramidHull <= 100f ? new Color(1f, 0.2f, 0.1f, 1f) : new Color(0.9f, 0.94f, 0.92f, 1f);
            GUI.Label(new Rect(x + 12f, eventY, width - 24f, 22f), lastEvent, lowHull ? warningStyle : smallStyle);
        }
    }

    private void DrawPyramidHealthBar(float x, float y, float width)
    {
        float hullPct = pyramidMaxHull > 0f ? Mathf.Clamp01(pyramidHull / pyramidMaxHull) : 1f;
        Color barColor;
        if (hullPct > 0.6f)
            barColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        else if (hullPct > 0.25f)
            barColor = new Color(1f, 0.8f, 0.1f, 0.9f);
        else
            barColor = new Color(1f, 0.15f, 0.1f, 0.9f);

        float barHeight = 8f;
        DrawFillBar(new Rect(x + 10f, y, width - 20f, barHeight), hullPct, barColor, new Color(0.6f, 0.5f, 0.3f, 0.6f));
    }

    private void DrawBannerAnnouncement()
    {
        if (bannerTimer <= 0f || string.IsNullOrEmpty(bannerMessage))
            return;

        bannerPulseTimer += Time.deltaTime * 2f;
        float alpha = Mathf.Clamp01(bannerTimer / 1.5f);
        float pulse = 1f + Mathf.Sin(bannerPulseTimer) * 0.05f;

        Color orig = GUI.color;
        Color bgColor = new Color(0f, 0f, 0f, 0.6f * alpha);
        Color textColor = new Color(1f, 0.75f, 0.18f, alpha * 0.95f);

        float bannerWidth = 600f;
        float bannerHeight = 44f;
        float bx = (Screen.width - bannerWidth) * 0.5f;
        float by = Screen.height * 0.22f;

        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(bx, by, bannerWidth, bannerHeight), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.76f, 0.23f, 0.6f * alpha);
        GUI.DrawTexture(new Rect(bx, by, bannerWidth, 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.62f, 1f, 0.55f * alpha);
        GUI.DrawTexture(new Rect(bx, by + bannerHeight - 2f, bannerWidth, 2f), Texture2D.whiteTexture);
        GUI.color = textColor;

        bannerStyle.fontSize = Mathf.RoundToInt(22f * pulse);
        GUI.Label(new Rect(bx, by, bannerWidth, bannerHeight), bannerMessage, bannerStyle);
        GUI.color = orig;

        bannerTimer -= Time.deltaTime;
    }

    private void DrawEnemyAlertIndicator()
    {
        enemyAlertTimer -= Time.deltaTime;
        float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.3f + 0.7f;
        Color alertColor = new Color(1f, 0.15f, 0.1f, pulse * 0.6f);
        GUI.color = alertColor;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void ShowBanner(string message, float duration = 3.5f)
    {
        bannerMessage = message;
        bannerTimer = duration;
        bannerPulseTimer = 0f;
    }

    private void DrawResourcePanel()
    {
        int held = 0;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            if (resourceNodes[i].controlled)
                held++;
        }

        float x = Screen.width - (hudExpanded ? 292f : 226f);
        float width = hudExpanded ? 274f : 208f;
        float height = hudExpanded ? 34f + resourceNodes.Count * 21f : 76f;
        DrawBox(new Rect(x, 16f, width, height), new Color(0f, 0f, 0f, 0.5f), new Color(1f, 0.78f, 0.28f, 0.5f));
        GUI.Label(new Rect(x + 12f, 23f, width - 20f, 22f), "Nodes " + held + "/" + resourceNodes.Count, labelStyle);
        if (!hudExpanded)
        {
            GUI.Label(new Rect(x + 12f, 46f, width - 20f, 20f), "F1 expands list", smallStyle);
            return;
        }

        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            string state = node.controlled ? "held" : Mathf.RoundToInt(node.capture * 100f) + "%";
            GUI.Label(new Rect(x + 12f, 48f + i * 21f, width - 22f, 20f), node.label + " - " + state, smallStyle);
        }
    }

    private void InitializeGameFlow()
    {
        if (SandRunnersSessionBootstrap.ConsumeStartMenuReturn())
        {
            startPlayingAfterSceneReload = false;
            SetGameFlowState(SandRunnersGameFlowState.MainMenu);
            if (!string.IsNullOrEmpty(SandRunnersSessionBootstrap.LastTransitionError))
                lastEvent = "Startup returned to menu: " + SandRunnersSessionBootstrap.LastTransitionError;
            return;
        }

        if (startPlayingAfterSceneReload || SandRunnersSessionBootstrap.ConsumeRtsStart())
        {
            startPlayingAfterSceneReload = false;
            SetGameFlowState(SandRunnersGameFlowState.Playing);
        }
        else
        {
            SetGameFlowState(SandRunnersGameFlowState.MainMenu);
        }
    }

    private bool HandleGameFlowInput()
    {
        if (WasKeyPressedThisFrame(Key.Escape))
        {
            if (gameFlowState == SandRunnersGameFlowState.Playing)
            {
                if (apexTargetingMode)
                {
                    beamCharge = 0f;
                    ExitApexTargetingMode();
                    return true;
                }
                if (gunnerSide != 0)
                {
                    ExitHowitzerGunner();
                    return true;
                }
                if (guidedMissileActive)
                {
                    ExitGuidedMissile();
                    return true;
                }
                SetGameFlowState(SandRunnersGameFlowState.Paused);
                return true;
            }
            else if (gameFlowState == SandRunnersGameFlowState.Paused)
            {
                SetGameFlowState(SandRunnersGameFlowState.Playing);
                return true;
            }
        }
        return false;
    }

    private bool CanRunGameplayUpdate()
    {
        return gameFlowState == SandRunnersGameFlowState.Playing;
    }

    private bool IsGameFlowOverlayBlocking()
    {
        return gameFlowState == SandRunnersGameFlowState.MainMenu ||
            gameFlowState == SandRunnersGameFlowState.Paused ||
            gameFlowState == SandRunnersGameFlowState.Victory ||
            gameFlowState == SandRunnersGameFlowState.Defeat;
    }

    private void SetGameFlowState(SandRunnersGameFlowState state)
    {
        gameFlowState = state;
        bool playing = state == SandRunnersGameFlowState.Playing;
        if (playing && !unifiedDiplomacyOpen)
            Time.timeScale = 1f;
        if (!playing && apexTargetingMode)
        {
            beamCharge = 0f;
            ExitApexTargetingMode();
        }
        if (!playing && gunnerSide != 0)
            ExitHowitzerGunner();
        if (!playing && guidedMissileActive)
            ExitGuidedMissile();
        Cursor.lockState = playing && !commandCursorMode ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing || commandCursorMode;
        OnGameFlowMusicChanged(state);

        if (!playing)
            CloseUnifiedDiplomacy(false);

        PublishStrategicUiEvent(StrategicUiEventKind.StrategicCanvasVisibility, playing && !hudHidden);

        if (state == SandRunnersGameFlowState.MainMenu)
            lastEvent = "Main menu. Start a new Sand runners battle.";
        else if (state == SandRunnersGameFlowState.Paused)
            lastEvent = "Paused.";
        else if (state == SandRunnersGameFlowState.Playing)
            lastEvent = "Sebek-nu-Anha is breaking toward the Earth Empire border.";
    }

    private void UpdateNonGameplayPresentation(float dt)
    {
        if (gameFlowState == SandRunnersGameFlowState.Victory)
        {
            UpdateMandarinkaEncounter(dt);
            UpdateMissionDirector(dt);
            UpdateVisuals(dt);
            UpdateImmersiveBattlefield(dt);
            UpdateSandRunnersAudio(dt);
            UpdateMusicSystem(dt);
            FollowCamera(dt);
        }
        else if (gameFlowState == SandRunnersGameFlowState.Defeat || gameFlowState == SandRunnersGameFlowState.Paused)
        {
            UpdateVisuals(dt);
            UpdateImmersiveBattlefield(dt);
            UpdateMusicSystem(dt);
            FollowCamera(dt);
        }
    }

    private void MarkDefeatIfNeeded()
    {
        if (cinematicDirectorActive)
        {
            pyramidHull = Mathf.Max(pyramidHull, pyramidMaxHull * 0.72f);
            return;
        }

        if (gameFlowState == SandRunnersGameFlowState.Playing && pyramidHull <= 0f)
        {
            pyramidHull = 0f;
            SetMissionDialogue("ROBERT: The hull is gone. SEBEK-NU-ANKHA: Then we remember the route and do it better.", 8f);
            PlaySandRunnerSound(SandRunnerSound.DefeatSting, battlePyramid.position, 0.8f);
            SetGameFlowState(SandRunnersGameFlowState.Defeat);
        }
    }

    private void MarkVictory()
    {
        if (gameFlowState != SandRunnersGameFlowState.Victory)
        {
            CompleteVerticalSliceMission();
            PlaySandRunnerSound(SandRunnerSound.VictoryFanfare, battlePyramid.position, 0.8f);
            ShowBanner("VICTORY — THE PALACE FALLS", 4f);
            SetGameFlowState(SandRunnersGameFlowState.Victory);
        }
    }

    private void DrawGameFlowGUI()
    {
        if (!IsGameFlowOverlayBlocking())
            return;

        EnsureGameFlowStyles();
        if (gameFlowState == SandRunnersGameFlowState.MainMenu)
            DrawMainMenuGUI();
        else if (gameFlowState == SandRunnersGameFlowState.Paused)
            DrawPauseMenuGUI();
        else if (gameFlowState == SandRunnersGameFlowState.Victory)
            DrawEndMatchGUI(true);
        else if (gameFlowState == SandRunnersGameFlowState.Defeat)
            DrawEndMatchGUI(false);
    }

    private void EnsureGameFlowStyles()
    {
        if (menuTitleStyle != null)
            return;

        menuTitleStyle = new GUIStyle(GUI.skin.label);
        menuTitleStyle.fontSize = 34;
        menuTitleStyle.fontStyle = FontStyle.Bold;
        menuTitleStyle.alignment = TextAnchor.MiddleCenter;
        menuTitleStyle.normal.textColor = new Color(1f, 0.78f, 0.22f, 1f);

        menuBodyStyle = new GUIStyle(GUI.skin.label);
        menuBodyStyle.fontSize = 15;
        menuBodyStyle.alignment = TextAnchor.UpperCenter;
        menuBodyStyle.wordWrap = true;
        menuBodyStyle.normal.textColor = new Color(0.9f, 0.94f, 1f, 1f);

        menuSmallStyle = new GUIStyle(menuBodyStyle);
        menuSmallStyle.fontSize = 12;
        menuSmallStyle.normal.textColor = new Color(0.62f, 0.8f, 1f, 1f);

        menuButtonStyle = new GUIStyle(GUI.skin.button);
        menuButtonStyle.fontSize = 16;
        menuButtonStyle.fontStyle = FontStyle.Bold;
        menuButtonStyle.normal.textColor = new Color(0.93f, 0.96f, 1f, 1f);
        menuButtonStyle.hover.textColor = new Color(0.45f, 0.72f, 1f, 1f);
        menuButtonStyle.active.textColor = new Color(1f, 0.76f, 0.23f, 1f);
    }

    private void DrawMainMenuGUI()
    {
        Rect panel = CenterRect(720f, 548f);
        bool prologueAvailable = SandRunnersBootstrap.IsFullPrologueAvailable();
        DrawPanelRect(panel, new Color(0.016f, 0.035f, 0.075f, 0.94f), new Color(1f, 0.76f, 0.23f, 1f));
        GUI.Label(new Rect(panel.x + 24f, panel.y + 28f, panel.width - 48f, 54f), "BATTLE FOR UNIVERSE", menuTitleStyle);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 78f, panel.width - 48f, 34f), "Sand runners", menuTitleStyle);
        string campaignDescription = prologueAvailable
            ? "Sebek-nu-Anha has stolen Robert from the Red Elemental palace. Begin inside the pyramid, then command the desert campaign against Mandarinka."
            : "Command the battle pyramid and its desert armies against Mandarinka. The Sebek prologue is reserved for a later story release.";
        GUI.Label(new Rect(panel.x + 74f, panel.y + 138f, panel.width - 148f, 86f), campaignDescription, menuBodyStyle);

        if (GUI.Button(new Rect(panel.x + 220f, panel.y + 238f, 280f, 44f), prologueAvailable ? "NEW GAME // PROLOGUE" : "NEW GAME // RTS CAMPAIGN", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, Vector3.zero, 0.6f);
            ReloadSceneForNewGame();
        }
        if (GUI.Button(new Rect(panel.x + 220f, panel.y + 294f, 280f, 44f), "SKIP PROLOGUE // RTS", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, Vector3.zero, 0.6f);
            StartRtsWithoutPrologue();
        }
        if (GUI.Button(new Rect(panel.x + 220f, panel.y + 350f, 280f, 44f), "DIRECT RTS", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, Vector3.zero, 0.6f);
            StartDirectRtsFromMenu();
        }
        if (GUI.Button(new Rect(panel.x + 220f, panel.y + 406f, 280f, 44f), "QUIT PLAY", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiButton, Vector3.zero, 0.5f);
            QuitPlayMode();
        }
        GUI.Label(new Rect(panel.x + 72f, panel.y + 480f, panel.width - 144f, 34f), "Controls: Tab strategy, C follow selected, scroll zoom, Esc pause.", menuSmallStyle);
    }

    private void DrawPauseMenuGUI()
    {
        Rect panel = CenterRect(520f, 560f);
        DrawPanelRect(panel, new Color(0.016f, 0.035f, 0.075f, 0.94f), new Color(0.25f, 0.62f, 1f, 1f));
        GUI.Label(new Rect(panel.x + 18f, panel.y + 26f, panel.width - 36f, 48f), "PAUSED", menuTitleStyle);
        DrawReleaseCandidateSettings(panel);
        if (GUI.Button(new Rect(panel.x + 150f, panel.y + 348f, 220f, 42f), "RESUME", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, Vector3.zero, 0.6f);
            SetGameFlowState(SandRunnersGameFlowState.Playing);
        }
        if (GUI.Button(new Rect(panel.x + 150f, panel.y + 402f, 220f, 42f), "MAIN MENU", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiButton, Vector3.zero, 0.5f);
            ReloadSceneToMainMenu();
        }
        if (GUI.Button(new Rect(panel.x + 150f, panel.y + 456f, 220f, 42f), "QUIT PLAY", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiButton, Vector3.zero, 0.5f);
            QuitPlayMode();
        }
    }

    private void DrawEndMatchGUI(bool victory)
    {
        Rect panel = CenterRect(720f, 420f);
        DrawPanelRect(panel, new Color(0.016f, 0.035f, 0.075f, 0.94f), victory ? new Color(1f, 0.76f, 0.23f, 1f) : new Color(1f, 0.16f, 0.08f, 1f));
        GUI.Label(new Rect(panel.x + 22f, panel.y + 26f, panel.width - 44f, 48f), victory ? "VICTORY" : "PYRAMID LOST", menuTitleStyle);
        GUI.Label(new Rect(panel.x + 74f, panel.y + 88f, panel.width - 148f, 138f), victory ? GetVictoryStatsText() : GetDefeatStatsText(), menuBodyStyle);
        if (GUI.Button(new Rect(panel.x + 118f, panel.y + 252f, 150f, 42f), "NEW GAME", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, Vector3.zero, 0.6f);
            ReloadSceneForNewGame();
        }
        if (GUI.Button(new Rect(panel.x + 286f, panel.y + 252f, 150f, 42f), "MAIN MENU", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiButton, Vector3.zero, 0.5f);
            ReloadSceneToMainMenu();
        }
        if (GUI.Button(new Rect(panel.x + 454f, panel.y + 252f, 150f, 42f), "QUIT PLAY", menuButtonStyle))
        {
            PlaySandRunnerSound(SandRunnerSound.UiButton, Vector3.zero, 0.5f);
            QuitPlayMode();
        }
    }

    private string GetDefeatStatsText()
    {
        int minutes = Mathf.FloorToInt(missionElapsed / 60f);
        int seconds = Mathf.FloorToInt(missionElapsed % 60f);
        return "The Red Elemental siege line caught the battle pyramid before asylum could be secured.\n" +
            "Time " + minutes + ":" + seconds.ToString("00") +
            " | Squads active " + unitSquads.Count +
            "\nResources remaining: Sand " + Mathf.RoundToInt(sand) + " | Gold " + Mathf.RoundToInt(gold) + " | Wind " + Mathf.RoundToInt(wind);
    }

    private Rect CenterRect(float width, float height)
    {
        width = Mathf.Min(width, Screen.width - 36f);
        height = Mathf.Min(height, Screen.height - 36f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private void ReloadSceneForNewGame()
    {
        if (SandRunnersBootstrap.IsFullPrologueAvailable())
            SandRunnersBootstrap.StartFullPrologue();
        else
            SandRunnersBootstrap.StartSkippedPrologue();
    }

    private void StartRtsWithoutPrologue()
    {
        SandRunnersBootstrap.StartSkippedPrologue();
    }

    private void StartDirectRtsFromMenu()
    {
        SandRunnersBootstrap.StartDirectRts();
    }

    private void ReloadSceneToMainMenu()
    {
        SandRunnersBootstrap.ReturnToStartMenu();
    }

    private void ReloadCurrentScene(bool startPlaying)
    {
        startPlayingAfterSceneReload = startPlaying;
        Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private void QuitPlayMode()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DrawNarrativeCommsGUI()
    {
        if (hudHidden || gameFlowState == SandRunnersGameFlowState.MainMenu)
            return;

        EnsureNarrativeGuiStyles();
        if (missionCommsTimer > 0f)
            DrawDialoguePortraitPanel();
    }

    private void EnsureNarrativeGuiStyles()
    {
        if (narrativeSpeakerStyle != null)
            return;

        narrativeSpeakerStyle = new GUIStyle(GUI.skin.label);
        narrativeSpeakerStyle.fontSize = 15;
        narrativeSpeakerStyle.fontStyle = FontStyle.Bold;
        narrativeSpeakerStyle.normal.textColor = Color.white;

        narrativeBodyStyle = new GUIStyle(GUI.skin.label);
        narrativeBodyStyle.fontSize = 14;
        narrativeBodyStyle.wordWrap = true;
        narrativeBodyStyle.normal.textColor = new Color(0.96f, 0.92f, 0.82f, 1f);

        narrativeInitialsStyle = new GUIStyle(GUI.skin.label);
        narrativeInitialsStyle.fontSize = 26;
        narrativeInitialsStyle.fontStyle = FontStyle.Bold;
        narrativeInitialsStyle.alignment = TextAnchor.MiddleCenter;
        narrativeInitialsStyle.normal.textColor = Color.black;

        narrativeVictoryTitleStyle = new GUIStyle(GUI.skin.label);
        narrativeVictoryTitleStyle.fontSize = 26;
        narrativeVictoryTitleStyle.fontStyle = FontStyle.Bold;
        narrativeVictoryTitleStyle.alignment = TextAnchor.MiddleCenter;
        narrativeVictoryTitleStyle.normal.textColor = new Color(1f, 0.78f, 0.25f, 1f);

        narrativeVictoryBodyStyle = new GUIStyle(GUI.skin.label);
        narrativeVictoryBodyStyle.fontSize = 15;
        narrativeVictoryBodyStyle.alignment = TextAnchor.UpperCenter;
        narrativeVictoryBodyStyle.wordWrap = true;
        narrativeVictoryBodyStyle.normal.textColor = new Color(0.96f, 0.92f, 0.84f, 1f);
    }

    private void DrawDialoguePortraitPanel()
    {
        float width = Mathf.Min(520f, Screen.width - 36f);
        float height = 86f;
        float x = (Screen.width - width) * 0.5f;
        float y = commandCursorMode ? Screen.height - 292f : Screen.height - 118f;
        if (Screen.height < 760f)
            y = 126f;

        DrawPanelRect(new Rect(x, y, width, height), new Color(0.035f, 0.024f, 0.018f, 0.9f), missionCommsColor);

        Rect portrait = new Rect(x + 12f, y + 12f, 62f, 62f);
        GUI.color = missionCommsColor;
        GUI.DrawTexture(portrait, Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.28f);
        GUI.DrawTexture(new Rect(portrait.x + 5f, portrait.y + 5f, portrait.width - 10f, portrait.height - 10f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(portrait, GetSpeakerInitials(missionCommsSpeaker), narrativeInitialsStyle);

        narrativeSpeakerStyle.normal.textColor = missionCommsColor;
        GUI.Label(new Rect(x + 88f, y + 9f, width - 104f, 22f), missionCommsSpeaker, narrativeSpeakerStyle);
        GUI.Label(new Rect(x + 88f, y + 32f, width - 104f, 48f), missionCommsBody, narrativeBodyStyle);
    }

    private void DrawVictoryStatsPanel()
    {
        float width = Mathf.Min(560f, Screen.width - 42f);
        float height = 198f;
        float x = (Screen.width - width) * 0.5f;
        float y = 78f;
        DrawPanelRect(new Rect(x, y, width, height), new Color(0.03f, 0.025f, 0.015f, 0.92f), new Color(1f, 0.74f, 0.22f, 1f));

        GUI.Label(new Rect(x + 18f, y + 16f, width - 36f, 34f), "VICTORY: THE PALACE FALLS", narrativeVictoryTitleStyle);
        GUI.Label(new Rect(x + 42f, y + 62f, width - 84f, 112f), GetVictoryStatsText(), narrativeVictoryBodyStyle);
    }

    private string GetVictoryStatsText()
    {
        int controlled = 0;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            if (resourceNodes[i].controlled)
                controlled++;
        }

        int minutes = Mathf.FloorToInt(missionElapsed / 60f);
        int seconds = Mathf.FloorToInt(missionElapsed % 60f);
        return "Mandarinka escaped on an emergency rocket, tearing through the last pagoda decks.\n" +
            "Time " + minutes + ":" + seconds.ToString("00") +
            " | Pyramid hull " + Mathf.RoundToInt(pyramidHull) + "/" + Mathf.RoundToInt(pyramidMaxHull) +
            "\nSquads active " + unitSquads.Count + " | Resource nodes held " + controlled + "/" + resourceNodes.Count +
            "\nSand " + Mathf.RoundToInt(sand) + " | Gold " + Mathf.RoundToInt(gold) + " | Wind " + Mathf.RoundToInt(wind);
    }

    private void DrawPanelRect(Rect rect, Color fill, Color accent)
    {
        GUI.color = fill;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 2f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.yMin, 2f, rect.height), Texture2D.whiteTexture);

        GUI.color = new Color(0.25f, 0.62f, 1f, 0.45f);
        Rect inner = new Rect(rect.xMin + 5f, rect.yMin + 5f, rect.width - 10f, rect.height - 10f);
        GUI.DrawTexture(new Rect(inner.xMin, inner.yMin, inner.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.xMin, inner.yMax - 1f, inner.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.xMin, inner.yMin, 1f, inner.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.xMax - 1f, inner.yMin, 1f, inner.height), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 0.76f, 0.23f, 0.95f);
        float arm = 16f;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, arm, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 3f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - arm, rect.yMin, arm, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.yMin, 3f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 3f, arm, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - arm, 3f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - arm, rect.yMax - 3f, arm, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.yMax - arm, 3f, arm), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private string GetSpeakerInitials(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
            return "?";
        if (speaker.Contains("SEBEK"))
            return "S";
        if (speaker.Contains("MANDARINKA"))
            return "M";
        if (speaker.Contains("ROBERT"))
            return "R";
        if (speaker.Contains("RED"))
            return "RE";
        if (speaker.Contains("SPIRIT") || speaker.Contains("PYRAMID"))
            return "KA";
        return speaker.Substring(0, Mathf.Min(2, speaker.Length)).ToUpperInvariant();
    }

    private void OnDrawGizmosSelected()
    {
        if (battlePyramid == null)
            return;

        Gizmos.color = new Color(1f, 0.75f, 0.25f, 0.35f);
        Gizmos.DrawWireSphere(battlePyramid.position, captureRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.05f, 0.25f);
        Gizmos.DrawWireSphere(battlePyramid.position, pyramidWeaponRange);
    }
}
