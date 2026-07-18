using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum MandarinkaRole
    {
        Fortress,
        GroundCrawler,
        AirJunk,
        Builder,
        FieldTurret,
        Gustav
    }

    private enum MandarinkaStrategyPhase
    {
        BuildUp,
        ScoutRaid,
        ResourceRaid,
        SiegeProbe,
        FortressAdvance
    }

    private sealed class MandarinkaAsset
    {
        public EnemyUnit enemy;
        public MandarinkaRole role;
        public Transform transform;
        public Transform visual;
        public float fireTimer;
        public float buildTimer;
        public float gatherTimer;
        public float wobbleSeed;
        public Vector3 objective;
        public bool hasObjective;
    }

    private sealed class HostileShellVisual
    {
        public Transform transform;
        public Vector3 start;
        public Vector3 target;
        public float age;
        public float duration;
        public float arcHeight;
        public float damage;
        public float blastRadius;
        public bool gustav;
        public bool nashorn;
        public bool intendedHit;
        public Transform warningRing;
    }

    private readonly List<MandarinkaAsset> mandarinkaAssets = new List<MandarinkaAsset>();
    private readonly List<HostileShellVisual> hostileShells = new List<HostileShellVisual>();
    private readonly List<Transform> mandarinkaFortressWheels = new List<Transform>();
    private readonly List<Transform> mandarinkaFortressMuzzles = new List<Transform>();
    private readonly List<LineRenderer> mandarinkaShieldLines = new List<LineRenderer>();

    private Transform mandarinkaFortressRoot;
    private Transform mandarinkaFortressVisual;
    private EnemyUnit mandarinkaFortressEnemy;
    private Material mandarinkaRedMaterial;
    private Material mandarinkaGoldMaterial;
    private Material mandarinkaDarkMaterial;
    private Material mandarinkaJadeMaterial;
    private Material mandarinkaShellMaterial;
    private Material mandarinkaShieldMaterial;
    private Transform mandarinkaShieldVisual;
    private const float MandarinkaFortressMaxHull = 9200f;
    private const float MandarinkaFortressMaxShield = 3600f;
    private float mandarinkaFortressShield;
    private float mandarinkaFortressPreviousHealth;
    private float mandarinkaFortressShieldCooldown;
    private float mandarinkaFortressRepairPulseTimer;
    private float mandarinkaSupply;
    private float mandarinkaStrategyTimer;
    private float mandarinkaGroundTimer;
    private float mandarinkaAirTimer;
    private float mandarinkaBuilderTimer;
    private float mandarinkaGustavTimer;
    private float mandarinkaFortressFireTimer;
    private int mandarinkaTauntIndex;
    private int mandarinkaFortressPhase;
    private MandarinkaStrategyPhase mandarinkaStrategyPhase;
    private Vector3 mandarinkaHomePosition;
    private bool mandarinkaEncounterInitialized;
    private bool mandarinkaDefeated;
    private bool mandarinkaFinaleTriggered;
    private bool mandarinkaFinaleDialoguePlayed;
    private float mandarinkaFinaleTimer;
    private Vector3 mandarinkaFinalePosition;
    private Transform mandarinkaEscapeRocket;

    private readonly string[] mandarinkaTaunts =
    {
        "SEBEK: Mandarinka, your palace is aiming like a tea tray on a camel.",
        "SEBEK: Another perfect miss. The Empress must be proud of her furniture.",
        "SEBEK: Robert, observe court artillery. Very loud. Very approximate.",
        "SEBEK: Her suspension is doing more strategy than her officers.",
        "SEBEK: If that was a warning shot, warn the empty dune again."
    };

    private void InitializeMandarinkaEncounter()
    {
        if (mandarinkaEncounterInitialized)
            return;

        mandarinkaEncounterInitialized = true;
        mandarinkaDefeated = false;
        mandarinkaFinaleTriggered = false;
        mandarinkaFinaleTimer = 0f;
        mandarinkaEscapeRocket = null;
        mandarinkaFortressPhase = 0;
        mandarinkaFortressShield = MandarinkaFortressMaxShield;
        mandarinkaFortressShieldCooldown = 0f;
        mandarinkaFortressPreviousHealth = MandarinkaFortressMaxHull;
        mandarinkaStrategyPhase = MandarinkaStrategyPhase.BuildUp;
        mandarinkaStrategyTimer = 0f;
        mandarinkaSupply = 85f;
        mandarinkaGroundTimer = 35f;
        mandarinkaAirTimer = 80f;
        mandarinkaBuilderTimer = 25f;
        mandarinkaGustavTimer = 180f;
        mandarinkaFortressFireTimer = 12f;

        mandarinkaRedMaterial = CreateMaterial("Mandarinka Lacquer Red Armor", new Color(0.62f, 0.035f, 0.025f, 1f));
        mandarinkaGoldMaterial = CreateMaterial("Mandarinka Imperial Gold Trim", new Color(1f, 0.61f, 0.18f, 1f));
        mandarinkaDarkMaterial = CreateMaterial("Mandarinka Iron Wheelworks", new Color(0.055f, 0.045f, 0.04f, 1f));
        mandarinkaJadeMaterial = CreateMaterial("Mandarinka Jade Signal Glass", new Color(0.12f, 0.95f, 0.66f, 0.72f));
        mandarinkaShellMaterial = CreateMaterial("Mandarinka Black Powder Shell", new Color(0.15f, 0.08f, 0.055f, 1f));
        mandarinkaShieldMaterial = CreateMaterial("Mandarinka Jade Fortress Shield", new Color(0.08f, 1f, 0.62f, 0.16f));
        SetMetallic(mandarinkaGoldMaterial, 0.62f, 0.52f);
        SetMetallic(mandarinkaDarkMaterial, 0.5f, 0.28f);
        ConfigureTransparent(mandarinkaJadeMaterial);
        ConfigureTransparent(mandarinkaShieldMaterial);
        SetEmission(mandarinkaJadeMaterial, new Color(0.08f, 1f, 0.58f, 1f), 1.25f);
        SetEmission(mandarinkaShieldMaterial, new Color(0.08f, 1f, 0.58f, 1f), 0.75f);

        GameObject existing = GameObject.Find("Mandarinka_Mobile_Fortress");
        if (existing != null)
            Destroy(existing);

        CreateMandarinkaFortress();
        SetMandarinkaRadio("MANDARINKA: Daughter of gold, return the Earth boy and kneel. SEBEK: Come take him from the sand.");
        lastEvent = "Mandarinka's wheeled palace has entered the desert.";
    }

    private void UpdateMandarinkaEncounter(float dt)
    {
        if (!mandarinkaEncounterInitialized || battlePyramid == null)
            return;

        UpdateMandarinkaTerritory(dt);
        if (mandarinkaDefeated)
        {
            UpdateMandarinkaFinale(dt);
            return;
        }

        UpdateMandarinkaBattleDirector(dt);
        UpdateMandarinkaStrategy(dt);
        UpdateMandarinkaProduction(dt);
        UpdateMandarinkaFortress(dt);
        UpdateMandarinkaAssets(dt);
        UpdateHostileShells(dt);
        mandarinkaSupply = Mathf.Clamp(mandarinkaSupply + dt * GetMandarinkaSupplyRate(), 0f, 720f);
    }

    private void UpdateMandarinkaStrategy(float dt)
    {
        mandarinkaStrategyTimer += dt;
        MandarinkaStrategyPhase previous = mandarinkaStrategyPhase;
        float distance = mandarinkaFortressRoot != null ? FlatDistance(mandarinkaFortressRoot.position, battlePyramid.position) : mapHalfSize * 2f;

        if (CanMandarinkaFortressAdvance() && (mandarinkaFortressPhase >= 2 || distance < 360f || mandarinkaStrategyTimer > 420f))
            mandarinkaStrategyPhase = MandarinkaStrategyPhase.FortressAdvance;
        else if (mandarinkaStrategyTimer > 270f)
            mandarinkaStrategyPhase = MandarinkaStrategyPhase.SiegeProbe;
        else if (mandarinkaStrategyTimer > 165f)
            mandarinkaStrategyPhase = MandarinkaStrategyPhase.ResourceRaid;
        else if (mandarinkaStrategyTimer > 90f)
            mandarinkaStrategyPhase = MandarinkaStrategyPhase.ScoutRaid;
        else
            mandarinkaStrategyPhase = MandarinkaStrategyPhase.BuildUp;

        if (previous != mandarinkaStrategyPhase)
        {
            lastEvent = "Mandarinka strategy: " + GetMandarinkaStrategyName() + ".";
            if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ResourceRaid)
                SetMandarinkaRadio("MANDARINKA: Take the middle wells. Let her pyramid starve elegantly.");
            else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.SiegeProbe)
                SetMandarinkaRadio("SEBEK: She is probing for a siege line. Deny her the center.");
            else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance)
                SetMandarinkaRadio("MANDARINKA: Palace wheels forward. The desert will kneel.");
        }
    }

    private float GetMandarinkaSupplyRate()
    {
        float rate = 0.72f + mandarinkaFortressPhase * 0.22f + GetMandarinkaTerritorySupplyRate();
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ScoutRaid)
            rate += 0.18f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ResourceRaid)
            rate += 0.3f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.SiegeProbe)
            rate += 0.42f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance)
            rate += 0.55f;
        return rate + GetMandarinkaTerritoryProductionBonus();
    }

    private void LateUpdate()
    {
        ReconcileMandarinkaFortressDamage(Time.deltaTime);
        MaintainAuthoredArtVisibility(Time.unscaledTime);
    }

    private void ReconcileMandarinkaFortressDamage(float dt)
    {
        if (!mandarinkaEncounterInitialized || mandarinkaDefeated || mandarinkaFortressEnemy == null || mandarinkaFortressEnemy.transform == null)
            return;

        float currentHealth = Mathf.Clamp(mandarinkaFortressEnemy.health, 0f, MandarinkaFortressMaxHull);
        if (currentHealth > mandarinkaFortressPreviousHealth)
            mandarinkaFortressPreviousHealth = currentHealth;

        if (currentHealth < mandarinkaFortressPreviousHealth - 0.01f)
        {
            float rawDamage = mandarinkaFortressPreviousHealth - currentHealth;
            float remainingDamage = rawDamage;
            if (mandarinkaFortressShield > 0f)
            {
                float absorb = Mathf.Min(mandarinkaFortressShield, rawDamage * 0.86f);
                mandarinkaFortressShield -= absorb;
                remainingDamage -= absorb;
                CreateShieldImpactFx(mandarinkaFortressRoot.position, absorb);
                if (mandarinkaFortressShield <= 1f)
                    SetMandarinkaRadio("SEBEK: Her jade shield cracked. Now make the palace remember weight.");
            }

            float armorMultiplier = Mathf.Lerp(0.36f, 0.64f, mandarinkaFortressPhase / 3f) *
                GetMandarinkaTerritoryArmorMultiplier();
            float finalDamage = Mathf.Max(remainingDamage * armorMultiplier, rawDamage * 0.08f);
            mandarinkaFortressEnemy.health = Mathf.Clamp(mandarinkaFortressPreviousHealth - finalDamage, 0f, MandarinkaFortressMaxHull);
            mandarinkaFortressShieldCooldown = 4.5f;
            mandarinkaFortressPreviousHealth = mandarinkaFortressEnemy.health;
            CheckMandarinkaFortressPhases();
        }
        else
        {
            mandarinkaFortressEnemy.health = currentHealth;
        }

        int builderCount = CountMandarinkaRole(MandarinkaRole.Builder);
        if (builderCount > 0 && mandarinkaFortressEnemy.health > 0f)
        {
            float repair = builderCount * (8f + mandarinkaFortressPhase * 2.5f) * dt;
            if (mandarinkaFortressEnemy.health < MandarinkaFortressMaxHull)
                mandarinkaFortressEnemy.health = Mathf.Min(MandarinkaFortressMaxHull, mandarinkaFortressEnemy.health + repair);

            mandarinkaFortressRepairPulseTimer -= dt;
            if (mandarinkaFortressRepairPulseTimer <= 0f && mandarinkaFortressRoot != null)
            {
                mandarinkaFortressRepairPulseTimer = 1.2f;
                for (int i = 0; i < mandarinkaAssets.Count; i++)
                {
                    MandarinkaAsset asset = mandarinkaAssets[i];
                    if (asset != null && asset.role == MandarinkaRole.Builder && asset.transform != null)
                    {
                        CreateRepairBeamFx(asset.transform.position + Vector3.up * 1f, mandarinkaFortressRoot.position + Vector3.up * 7f);
                        break;
                    }
                }
            }
        }

        mandarinkaFortressShieldCooldown -= dt;
        if (mandarinkaFortressShieldCooldown <= 0f && mandarinkaFortressEnemy.health > 0f)
            mandarinkaFortressShield = Mathf.Min(MandarinkaFortressMaxShield,
                mandarinkaFortressShield + (58f + builderCount * 18f + mandarinkaFortressPhase * 24f +
                (mandarinkaCastleGoldKit ? 34f : 0f)) * dt);

        mandarinkaFortressPreviousHealth = mandarinkaFortressEnemy.health;
        if (mandarinkaFortressEnemy.health <= 0f)
            HandleMandarinkaEnemyDestroyed(mandarinkaFortressRoot);
    }

    private void CheckMandarinkaFortressPhases()
    {
        if (mandarinkaFortressEnemy == null || mandarinkaFortressEnemy.health <= 0f)
            return;

        float health01 = mandarinkaFortressEnemy.health / MandarinkaFortressMaxHull;
        if (mandarinkaFortressPhase == 0 && health01 <= 0.7f)
        {
            mandarinkaFortressPhase = 1;
            mandarinkaFortressShield = Mathf.Min(MandarinkaFortressMaxShield, mandarinkaFortressShield + 1400f);
            mandarinkaSupply += 120f;
            mandarinkaGroundTimer = 0f;
            mandarinkaBuilderTimer = 0f;
            SpawnMandarinkaGroundCrawler();
            SpawnMandarinkaBuilder();
            CreateFortressPhaseFx(mandarinkaFortressRoot.position, new Color(0.08f, 1f, 0.62f, 1f));
            SetMandarinkaRadio("MANDARINKA: First gate protocol. Wheels kneel, guns rise. SEBEK: She finally found the war button.");
        }
        else if (mandarinkaFortressPhase == 1 && health01 <= 0.4f)
        {
            mandarinkaFortressPhase = 2;
            mandarinkaFortressShield = Mathf.Min(MandarinkaFortressMaxShield, mandarinkaFortressShield + 2100f);
            mandarinkaSupply += 190f;
            mandarinkaGustavTimer = 0f;
            mandarinkaAirTimer = 0f;
            SpawnMandarinkaTurret(mandarinkaFortressRoot.position + mandarinkaFortressRoot.right * 24f);
            SpawnMandarinkaTurret(mandarinkaFortressRoot.position - mandarinkaFortressRoot.right * 24f);
            CreateFortressPhaseFx(mandarinkaFortressRoot.position, new Color(1f, 0.08f, 0.035f, 1f));
            SetMandarinkaRadio("SEBEK: Second gate. She will try to make this a siege. Do not let the Gustav breathe.");
        }
        else if (mandarinkaFortressPhase == 2 && health01 <= 0.18f)
        {
            mandarinkaFortressPhase = 3;
            mandarinkaFortressShield = Mathf.Min(MandarinkaFortressMaxShield, mandarinkaFortressShield + 2600f);
            mandarinkaSupply += 260f;
            mandarinkaGroundTimer = 0f;
            mandarinkaAirTimer = 0f;
            mandarinkaBuilderTimer = 0f;
            mandarinkaFortressFireTimer = 0.5f;
            CreateFortressPhaseFx(mandarinkaFortressRoot.position, new Color(1f, 0.42f, 0.08f, 1f));
            SetMandarinkaRadio("MANDARINKA: Final court decree. Bury the pyramid. SEBEK: Final court joke. Miss me first.");
        }
    }

    private void CreateMandarinkaFortress()
    {
        Vector3 position = new Vector3(mapHalfSize * 0.72f, 0f, mapHalfSize * 0.68f);
        position.y = GetPlayableGroundHeight(position) + 0.3f;
        mandarinkaHomePosition = position;

        mandarinkaFortressRoot = new GameObject("Mandarinka_Mobile_Fortress").transform;
        mandarinkaFortressRoot.position = position;
        mandarinkaFortressRoot.rotation = Quaternion.Euler(0f, 222f, 0f);

        BoxCollider collider = mandarinkaFortressRoot.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 8.5f, 0f);
        collider.size = new Vector3(24f, 18f, 22f);

        mandarinkaFortressVisual = new GameObject("Mandarinka_Suspension_Castle_Visual").transform;
        mandarinkaFortressVisual.SetParent(mandarinkaFortressRoot, false);

        CreateBox(mandarinkaFortressVisual, "Eight_Wheel_Iron_Chassis", new Vector3(0f, 1.25f, 0f), Quaternion.identity, new Vector3(18.5f, 1.4f, 15.5f), mandarinkaDarkMaterial);
        CreateBox(mandarinkaFortressVisual, "Red_Mobile_Castle_Base", new Vector3(0f, 3.15f, 0f), Quaternion.identity, new Vector3(15.5f, 3.4f, 12.5f), mandarinkaRedMaterial);
        CreateBox(mandarinkaFortressVisual, "Gold_Balcony_Ring_Front", new Vector3(0f, 5.1f, -6.65f), Quaternion.identity, new Vector3(16.2f, 0.26f, 0.45f), mandarinkaGoldMaterial);
        CreateBox(mandarinkaFortressVisual, "Gold_Balcony_Ring_Rear", new Vector3(0f, 5.1f, 6.65f), Quaternion.identity, new Vector3(16.2f, 0.26f, 0.45f), mandarinkaGoldMaterial);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -6.5f + i * 3.25f;
                GameObject wheel = CreateCylinder(mandarinkaFortressVisual, "Mandarinka_Suspension_Wheel_" + side + "_" + i, new Vector3(side * 9.5f, 0.8f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(1.45f, 0.52f, 1.45f), mandarinkaDarkMaterial);
                mandarinkaFortressWheels.Add(wheel.transform);
                CreateBox(mandarinkaFortressVisual, "Mandarinka_Gold_Suspension_Arm_" + side + "_" + i, new Vector3(side * 8.15f, 1.7f, z), Quaternion.Euler(0f, 0f, side * 13f), new Vector3(2.1f, 0.22f, 0.32f), mandarinkaGoldMaterial);
            }
        }

        CreatePagodaTower(new Vector3(0f, 7.3f, 0f), 1.25f, 4);
        CreatePagodaTower(new Vector3(-5.5f, 6.25f, -4.6f), 0.85f, 3);
        CreatePagodaTower(new Vector3(5.5f, 6.25f, -4.6f), 0.85f, 3);
        CreatePagodaTower(new Vector3(-5.5f, 6.25f, 4.6f), 0.85f, 3);
        CreatePagodaTower(new Vector3(5.5f, 6.25f, 4.6f), 0.85f, 3);

        CreateBox(mandarinkaFortressVisual, "Jade_Command_Window", new Vector3(0f, 8.3f, -6.92f), Quaternion.identity, new Vector3(3.2f, 0.74f, 0.18f), mandarinkaJadeMaterial);
        CreatePointLight(mandarinkaFortressVisual, "Jade_Command_Light", new Vector3(0f, 8.3f, -7.2f), new Color(0.1f, 1f, 0.58f, 1f), 1.45f, 18f);

        CreateFortressCannon("Left_Palace_Artillery", new Vector3(-4.8f, 5.4f, -7.4f));
        CreateFortressCannon("Right_Palace_Artillery", new Vector3(4.8f, 5.4f, -7.4f));
        CreateFortressCannon("High_Roof_Artillery", new Vector3(0f, 10.2f, -4.4f));

        CreateMandarinkaShieldVisual();

        mandarinkaFortressEnemy = RegisterMandarinkaEnemy(mandarinkaFortressRoot, MandarinkaFortressMaxHull);
        mandarinkaFortressPreviousHealth = mandarinkaFortressEnemy.health;
        MandarinkaAsset asset = new MandarinkaAsset();
        asset.enemy = mandarinkaFortressEnemy;
        asset.role = MandarinkaRole.Fortress;
        asset.transform = mandarinkaFortressRoot;
        asset.visual = mandarinkaFortressVisual;
        asset.wobbleSeed = Random.Range(0f, 50f);
        mandarinkaAssets.Add(asset);
    }

    private void CreatePagodaTower(Vector3 localBase, float scale, int levels)
    {
        for (int i = 0; i < levels; i++)
        {
            float y = localBase.y + i * 1.25f * scale;
            float width = (3.4f - i * 0.42f) * scale;
            CreateBox(mandarinkaFortressVisual, "Pagoda_Red_Level_" + localBase.x + "_" + i, new Vector3(localBase.x, y, localBase.z), Quaternion.identity, new Vector3(width, 0.95f * scale, width), mandarinkaRedMaterial);
            CreateBox(mandarinkaFortressVisual, "Pagoda_Gold_Roof_" + localBase.x + "_" + i, new Vector3(localBase.x, y + 0.58f * scale, localBase.z), Quaternion.identity, new Vector3(width + 0.9f * scale, 0.22f * scale, width + 0.9f * scale), mandarinkaGoldMaterial);
        }
    }

    private void CreateFortressCannon(string name, Vector3 localPosition)
    {
        CreateCylinder(mandarinkaFortressVisual, name + "_Base", localPosition + Vector3.up * 0.02f, Quaternion.identity, new Vector3(0.62f, 0.22f, 0.62f), mandarinkaDarkMaterial);
        CreateCylinder(mandarinkaFortressVisual, name + "_Barrel", localPosition + new Vector3(0f, 0.05f, -1.25f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 1.35f, 0.22f), mandarinkaDarkMaterial);
        Transform muzzle = new GameObject("Mandarinka_" + name + "_Muzzle").transform;
        muzzle.SetParent(mandarinkaFortressVisual, false);
        muzzle.localPosition = localPosition + new Vector3(0f, 0.05f, -2.58f);
        mandarinkaFortressMuzzles.Add(muzzle);
    }

    private void CreateMandarinkaShieldVisual()
    {
        mandarinkaShieldLines.Clear();
        GameObject shield = new GameObject("Mandarinka_Jade_Shield_Field");
        shield.transform.SetParent(mandarinkaFortressRoot, false);
        shield.transform.localPosition = new Vector3(0f, 7.2f, 0f);
        shield.transform.localScale = new Vector3(1f, 0.64f, 1f);
        mandarinkaShieldVisual = shield.transform;

        CreateMandarinkaShieldRing("Shield_Horizon_Ring", Quaternion.identity, 16.2f, 0f, 0.11f);
        CreateMandarinkaShieldRing("Shield_Polar_Ring_A", Quaternion.Euler(90f, 0f, 0f), 15.3f, 0f, 0.09f);
        CreateMandarinkaShieldRing("Shield_Polar_Ring_B", Quaternion.Euler(90f, 0f, 90f), 15.3f, 0f, 0.09f);
        CreateMandarinkaShieldRing("Shield_Diagonal_Ring_A", Quaternion.Euler(55f, 0f, 42f), 15.8f, 0f, 0.07f);
        CreateMandarinkaShieldRing("Shield_Diagonal_Ring_B", Quaternion.Euler(55f, 0f, -42f), 15.8f, 0f, 0.07f);
    }

    private void CreateMandarinkaShieldRing(string name, Quaternion localRotation, float radius, float localY, float width)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(mandarinkaShieldVisual, false);
        ring.transform.localPosition = new Vector3(0f, localY, 0f);
        ring.transform.localRotation = localRotation;

        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.positionCount = 96;
        line.loop = true;
        line.useWorldSpace = false;
        line.startWidth = width;
        line.endWidth = width;
        line.sharedMaterial = mandarinkaShieldMaterial;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
        mandarinkaShieldLines.Add(line);
    }

    private EnemyUnit RegisterMandarinkaEnemy(Transform target, float health)
    {
        EnemyUnit enemy = new EnemyUnit();
        enemy.transform = target;
        enemy.health = health;
        enemy.fireCooldown = Random.Range(0.2f, 1.2f);
        enemies.Add(enemy);
        return enemy;
    }

    private void UpdateMandarinkaFortress(float dt)
    {
        if (mandarinkaFortressEnemy == null || mandarinkaFortressEnemy.transform == null || mandarinkaFortressEnemy.health <= 0f)
            return;

        Vector3 toPyramid = battlePyramid.position - mandarinkaFortressRoot.position;
        toPyramid.y = 0f;
        float distance = toPyramid.magnitude;
        Vector3 desired = Vector3.zero;
        bool advancing = CanMandarinkaFortressAdvance() && (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance || distance < 380f || mandarinkaFortressPhase >= 2);
        if (advancing)
        {
            if (distance > 235f)
                desired = toPyramid.normalized;
            else if (distance < 178f)
                desired = -toPyramid.normalized;
            else
                desired = Vector3.Cross(Vector3.up, toPyramid.normalized) * 0.45f;
        }
        else
        {
            Vector3 toHome = mandarinkaHomePosition - mandarinkaFortressRoot.position;
            toHome.y = 0f;
            if (toHome.magnitude > 22f)
                desired = toHome.normalized;
        }

        if (desired.sqrMagnitude > 0.01f)
        {
            float speed = (advancing ? 2.15f : 0.85f) + GetMandarinkaTerritorySpeedBonus();
            Vector3 safeDirection = GetLargeAssetSafeDirection(mandarinkaFortressRoot, desired, 18f, dt);
            mandarinkaFortressRoot.position += safeDirection * speed * dt;
            Vector3 look = advancing ? toPyramid : desired;
            if (look.sqrMagnitude > 0.01f)
                mandarinkaFortressRoot.rotation = Quaternion.RotateTowards(mandarinkaFortressRoot.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), (advancing ? 18f : 8f) * dt);
        }

        Vector3 position = mandarinkaFortressRoot.position;
        position.x = Mathf.Clamp(position.x, -mapHalfSize + 32f, mapHalfSize - 32f);
        position.z = Mathf.Clamp(position.z, -mapHalfSize + 32f, mapHalfSize - 32f);
        position.y = GetPlayableGroundHeight(position) + 0.3f;
        mandarinkaFortressRoot.position = position;

        float wobble = Mathf.Sin(Time.time * 2.15f + 0.4f) * 1.8f + Mathf.Sin(Time.time * 3.7f) * 0.8f;
        mandarinkaFortressVisual.localPosition = new Vector3(0f, 0.25f + Mathf.Abs(wobble) * 0.035f, 0f);
        mandarinkaFortressVisual.localRotation = Quaternion.Euler(wobble, 0f, Mathf.Sin(Time.time * 1.73f) * 2.4f);
        UpdateMandarinkaShieldVisual();
        for (int i = 0; i < mandarinkaFortressWheels.Count; i++)
            mandarinkaFortressWheels[i].Rotate(Vector3.right, 126f * dt, Space.Self);

        mandarinkaFortressFireTimer -= dt;
        if (mandarinkaFortressFireTimer <= 0f && distance <= 330f)
        {
            mandarinkaFortressFireTimer = Random.Range(6.4f, 9.2f) - mandarinkaFortressPhase * 0.65f;
            FireMandarinkaFortressArtillery(distance);
        }
    }

    private void UpdateMandarinkaShieldVisual()
    {
        if (mandarinkaShieldVisual == null)
            return;

        float shield01 = Mathf.Clamp01(mandarinkaFortressShield / MandarinkaFortressMaxShield);
        mandarinkaShieldVisual.gameObject.SetActive(shield01 > 0.02f);
        float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.02f + (1f - shield01) * 0.055f;
        mandarinkaShieldVisual.localScale = new Vector3(pulse, 0.64f * pulse, pulse);
        Color shieldColor = Color.Lerp(new Color(0.08f, 1f, 0.62f, 0.22f), new Color(0.2f, 1f, 0.72f, 0.72f), shield01);
        for (int i = 0; i < mandarinkaShieldLines.Count; i++)
        {
            LineRenderer line = mandarinkaShieldLines[i];
            if (line == null)
                continue;
            float flicker = 0.8f + Mathf.Sin(Time.time * (3.4f + i * 0.37f)) * 0.2f;
            line.startWidth = Mathf.Lerp(0.045f, 0.13f, shield01) * flicker;
            line.endWidth = line.startWidth;
            line.startColor = shieldColor;
            line.endColor = shieldColor;
            if (line.sharedMaterial != null)
            {
                ApplyColor(line.sharedMaterial, shieldColor);
                SetEmission(line.sharedMaterial, new Color(0.08f, 1f, 0.62f, 1f), 0.65f + shield01 * 1.8f);
            }
        }
    }

    private void FireMandarinkaFortressArtillery(float distance)
    {
        if (mandarinkaFortressMuzzles.Count == 0)
            return;

        Transform muzzle = mandarinkaFortressMuzzles[Random.Range(0, mandarinkaFortressMuzzles.Count)];
        bool intendedHit = Random.value < Mathf.Lerp(0.32f, 0.56f, Mathf.InverseLerp(280f, 80f, distance));
        Vector2 error = intendedHit ? Random.insideUnitCircle * 3.8f : Random.insideUnitCircle.normalized * Random.Range(18f, 42f);
        Vector3 target = GetMandarinkaStrategicAimPoint(battlePyramid.position) + new Vector3(error.x, 0f, error.y);
        target.y = GetPlayableGroundHeight(target) + 0.25f;

        CreateHostileShell("Mandarinka_Palace_Artillery_Shell", muzzle.position, target, 72f + mandarinkaFortressPhase * 18f, 12.5f + mandarinkaFortressPhase * 1.5f, false, intendedHit, 1.55f, 38f);
        CreateWeaponFlash(muzzle.position, 0.46f, new Color(1f, 0.2f, 0.05f, 1f));
        if (mandarinkaFortressPhase >= 1 && Random.value < 0.55f)
        {
            Vector3 secondTarget = GetMandarinkaStrategicAimPoint(battlePyramid.position) + new Vector3(Random.Range(-24f, 24f), 0f, Random.Range(-24f, 24f));
            secondTarget.y = GetPlayableGroundHeight(secondTarget) + 0.25f;
            CreateHostileShell("Mandarinka_Palace_Barrage_Shell", muzzle.position + mandarinkaFortressRoot.right * Random.Range(-4f, 4f), secondTarget, 56f, 11f, false, false, 1.75f, 44f);
        }
        if (!intendedHit)
            SetMandarinkaRadio(mandarinkaTaunts[mandarinkaTauntIndex++ % mandarinkaTaunts.Length]);
        else
            lastEvent = "Mandarinka's palace artillery bracketed the pyramid.";
    }

    private void UpdateMandarinkaProduction(float dt)
    {
        float productionRate = GetMandarinkaProductionRate();
        mandarinkaGroundTimer -= dt * productionRate;
        mandarinkaAirTimer -= dt * productionRate;
        mandarinkaBuilderTimer -= dt * productionRate;
        mandarinkaGustavTimer -= dt * productionRate;

        int phaseIndex = (int)mandarinkaStrategyPhase;
        int visiblePlayerForce = runners.Count + goldenStructures.Count * 2;
        int pressureTier = Mathf.Clamp(visiblePlayerForce / 12, 0, 6);
        int groundLimit = Mathf.Clamp(3 + phaseIndex + mandarinkaFortressPhase + pressureTier, 3, 12);
        int airLimit = mandarinkaStrategyPhase < MandarinkaStrategyPhase.ResourceRaid ? 0 :
            Mathf.Clamp(1 + mandarinkaFortressPhase + GetMandarinkaTerritoryAirLimitBonus() + pressureTier / 2, 1, 6);
        int builderLimit = mandarinkaStrategyPhase == MandarinkaStrategyPhase.BuildUp ? 2 : Mathf.Clamp(3 + pressureTier / 3, 3, 5);
        int turretLimit = mandarinkaStrategyPhase < MandarinkaStrategyPhase.SiegeProbe ? 1 : 1 + mandarinkaFortressPhase + pressureTier / 2;

        if (mandarinkaGroundTimer <= 0f && mandarinkaSupply >= 24f)
        {
            if (CountMandarinkaRole(MandarinkaRole.GroundCrawler) < groundLimit)
            {
                mandarinkaSupply -= 24f;
                mandarinkaGroundTimer = Mathf.Max(9f,
                    Random.Range(22f, 31f) - phaseIndex * 2.4f - pressureTier * 1.35f);
                SpawnMandarinkaGroundCrawler();
            }
            else
            {
                mandarinkaGroundTimer = Random.Range(16f, 24f);
            }
        }

        if (mandarinkaAirTimer <= 0f && mandarinkaSupply >= 38f)
        {
            if (CountMandarinkaRole(MandarinkaRole.AirJunk) < airLimit)
            {
                mandarinkaSupply -= 38f;
                mandarinkaAirTimer = Mathf.Max(16f,
                    Random.Range(32f, 46f) - phaseIndex * 3.2f - pressureTier * 1.4f);
                SpawnMandarinkaAirJunk();
            }
            else
            {
                mandarinkaAirTimer = Random.Range(18f, 28f);
            }
        }

        if (mandarinkaBuilderTimer <= 0f && CountMandarinkaRole(MandarinkaRole.Builder) < builderLimit && mandarinkaSupply >= 34f)
        {
            mandarinkaSupply -= 34f;
            mandarinkaBuilderTimer = Random.Range(42f, 58f);
            SpawnMandarinkaBuilder();
        }

        if (mandarinkaStrategyPhase >= MandarinkaStrategyPhase.SiegeProbe && mandarinkaGustavTimer <= 0f && CountMandarinkaRole(MandarinkaRole.Gustav) < 1 && mandarinkaSupply >= 126f)
        {
            mandarinkaSupply -= 126f;
            mandarinkaGustavTimer = Random.Range(85f, 110f);
            SpawnMandarinkaGustav();
        }

        if (CountMandarinkaRole(MandarinkaRole.FieldTurret) >= turretLimit)
            mandarinkaSupply = Mathf.Min(mandarinkaSupply, 420f);
    }

    private float GetMandarinkaProductionRate()
    {
        float rate = 0.5f + mandarinkaFortressPhase * 0.16f;
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ScoutRaid)
            rate += 0.14f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ResourceRaid)
            rate += 0.28f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.SiegeProbe)
            rate += 0.42f;
        else if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance)
            rate += 0.55f;
        return rate;
    }

    private void SpawnMandarinkaGroundCrawler()
    {
        Vector3 position = GetMandarinkaSpawnPoint(-10f);
        Transform root = new GameObject("Mandarinka_Ground_Crawler").transform;
        root.position = position;
        root.rotation = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.rotation : Quaternion.identity;
        CreateBox(root, "Red_Crawler_Hull", new Vector3(0f, 0.6f, 0f), Quaternion.identity, new Vector3(2.4f, 0.72f, 3.1f), mandarinkaRedMaterial);
        CreateBox(root, "Gold_Crawler_Ram", new Vector3(0f, 0.7f, -1.72f), Quaternion.identity, new Vector3(1.6f, 0.32f, 0.32f), mandarinkaGoldMaterial);
        CreateBox(root, "Crawler_Red_Command_Cab", new Vector3(0f, 1.08f, -0.25f), Quaternion.identity, new Vector3(1.2f, 0.62f, 1.05f), mandarinkaRedMaterial);
        CreateCylinder(root, "Crawler_Jade_Lance_Turret", new Vector3(0f, 1.48f, -0.92f), Quaternion.Euler(83f, 0f, 0f), new Vector3(0.14f, 1.05f, 0.14f), mandarinkaDarkMaterial);
        CreateBox(root, "Crawler_Gold_Spine", new Vector3(0f, 1.1f, 0.78f), Quaternion.identity, new Vector3(0.36f, 0.18f, 1.38f), mandarinkaGoldMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 3; i++)
            {
                float z = -0.92f + i * 0.92f;
                CreateCylinder(root, "Crawler_Wheel_" + side + "_" + i, new Vector3(side * 1.45f, 0.28f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.43f, 0.17f, 0.43f), mandarinkaDarkMaterial);
            }
            CreateBox(root, "Crawler_Gold_Side_Armor_" + side, new Vector3(side * 1.33f, 0.58f, 0f), Quaternion.identity, new Vector3(0.18f, 0.26f, 2.45f), mandarinkaGoldMaterial);
        }
        CreatePointLight(root, "Crawler_Jade_Targeter", new Vector3(0f, 1.42f, -1.65f), new Color(0.1f, 1f, 0.58f, 1f), 0.6f, 6f);
        ApplyAuthoredImperialAssaultArt(root);
        MandarinkaAsset asset = AddMandarinkaAsset(root, MandarinkaRole.GroundCrawler, 125f);
        asset.objective = PickMandarinkaRaidObjective();
        asset.hasObjective = true;
        lastEvent = "Mandarinka deployed red ground crawlers.";
    }

    private void SpawnMandarinkaAirJunk()
    {
        Vector3 position = GetMandarinkaSpawnPoint(8f) + Vector3.up * 9f;
        Transform root = new GameObject("Mandarinka_Air_Junk").transform;
        root.position = position;
        CreateBox(root, "Air_Junk_Hull", Vector3.zero, Quaternion.identity, new Vector3(2.1f, 0.42f, 3.2f), mandarinkaRedMaterial);
        CreateBox(root, "Air_Junk_Left_Wing", new Vector3(-1.8f, 0f, 0f), Quaternion.identity, new Vector3(1.9f, 0.08f, 2.4f), mandarinkaGoldMaterial);
        CreateBox(root, "Air_Junk_Right_Wing", new Vector3(1.8f, 0f, 0f), Quaternion.identity, new Vector3(1.9f, 0.08f, 2.4f), mandarinkaGoldMaterial);
        CreateBox(root, "Air_Junk_Red_Sail_Front", new Vector3(0f, 0.78f, -0.82f), Quaternion.Euler(-18f, 0f, 0f), new Vector3(0.12f, 1.36f, 1.35f), mandarinkaRedMaterial);
        CreateBox(root, "Air_Junk_Red_Sail_Rear", new Vector3(0f, 0.72f, 0.9f), Quaternion.Euler(18f, 0f, 0f), new Vector3(0.12f, 1.1f, 1.1f), mandarinkaRedMaterial);
        CreateBox(root, "Air_Junk_Gold_Keel", new Vector3(0f, -0.18f, 0.35f), Quaternion.identity, new Vector3(0.34f, 0.16f, 2.9f), mandarinkaGoldMaterial);
        CreateCylinder(root, "Air_Junk_Jade_Engine_Ring", new Vector3(0f, 0f, 1.82f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f, 0.08f, 0.42f), mandarinkaJadeMaterial);
        CreatePointLight(root, "Air_Junk_Jade_Light", new Vector3(0f, 0.25f, -1.3f), new Color(0.1f, 1f, 0.58f, 1f), 0.65f, 7f);
        MandarinkaAsset asset = AddMandarinkaAsset(root, MandarinkaRole.AirJunk, 92f);
        asset.objective = PickMandarinkaRaidObjective();
        asset.hasObjective = true;
        lastEvent = "Mandarinka launched jade-lit air junks.";
    }

    private void SpawnMandarinkaBuilder()
    {
        Vector3 position = GetMandarinkaSpawnPoint(12f);
        Transform root = new GameObject("Mandarinka_Resource_Builder").transform;
        root.position = position;
        CreateBox(root, "Builder_Cart", new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.8f, 0.62f, 2.2f), mandarinkaDarkMaterial);
        CreateBox(root, "Builder_Red_Cab", new Vector3(0f, 0.95f, -0.3f), Quaternion.identity, new Vector3(1.1f, 0.74f, 1.0f), mandarinkaRedMaterial);
        CreateBox(root, "Builder_Crane_Arm", new Vector3(0.75f, 1.45f, 0.8f), Quaternion.Euler(0f, 0f, -24f), new Vector3(0.28f, 0.18f, 2.4f), mandarinkaGoldMaterial);
        CreateBox(root, "Builder_Gold_Resource_Hopper", new Vector3(-0.42f, 0.98f, 0.82f), Quaternion.identity, new Vector3(0.8f, 0.62f, 0.78f), mandarinkaGoldMaterial);
        CreateCylinder(root, "Builder_Jade_Scanner_Dish", new Vector3(0.84f, 1.78f, 1.72f), Quaternion.Euler(75f, 0f, 0f), new Vector3(0.42f, 0.07f, 0.42f), mandarinkaJadeMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCylinder(root, "Builder_Wheel_Front_" + side, new Vector3(side * 1.05f, 0.26f, -0.72f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.13f, 0.32f), mandarinkaDarkMaterial);
            CreateCylinder(root, "Builder_Wheel_Rear_" + side, new Vector3(side * 1.05f, 0.26f, 0.86f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.13f, 0.32f), mandarinkaDarkMaterial);
        }
        CreatePointLight(root, "Builder_Jade_Work_Light", new Vector3(0.8f, 1.55f, 1.6f), new Color(0.1f, 1f, 0.58f, 1f), 0.55f, 6f);
        bool importedRover = TryAttachMandarinkaImportedVehicle(root, "SR_MineEngineerRover", 5.6f,
            Quaternion.Euler(0f, 90f, 0f), Vector3.zero, mandarinkaDarkMaterial, true);
        if (importedRover)
        {
            CreateBox(root, "Builder_Imported_Red_Command_Stripe", new Vector3(0f, 1.75f, -0.25f),
                Quaternion.identity, new Vector3(2.7f, 0.16f, 0.5f), mandarinkaRedMaterial);
            CreateCylinder(root, "Builder_Imported_Gold_Beacon", new Vector3(0f, 2.35f, 0.3f),
                Quaternion.identity, new Vector3(0.25f, 0.38f, 0.25f), mandarinkaGoldMaterial);
        }
        MandarinkaAsset asset = AddMandarinkaAsset(root, MandarinkaRole.Builder, 95f);
        asset.buildTimer = Random.Range(7f, 12f);
        SetMandarinkaRadio("MANDARINKA: Builders, drink the desert dry and raise guns in my name.");
    }

    private void SpawnMandarinkaGustav()
    {
        Vector3 position = GetMandarinkaSpawnPoint(18f);
        Transform root = new GameObject("Mandarinka_Karl_Gustav_Siege_Gun").transform;
        root.position = position;
        root.rotation = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.rotation : Quaternion.identity;
        CreateBox(root, "Gustav_Tracked_Carriage", new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(4.5f, 0.82f, 5.8f), mandarinkaDarkMaterial);
        CreateBox(root, "Gustav_Red_Armor_Shield", new Vector3(0f, 1.38f, -0.55f), Quaternion.identity, new Vector3(3.2f, 1.6f, 0.38f), mandarinkaRedMaterial);
        CreateBox(root, "Gustav_Gold_Recoil_Sled", new Vector3(0f, 1.12f, -1.95f), Quaternion.identity, new Vector3(1.22f, 0.34f, 3.25f), mandarinkaGoldMaterial);
        CreateBox(root, "Gustav_Red_Crew_Casemate", new Vector3(0f, 1.22f, 1.58f), Quaternion.identity, new Vector3(2.6f, 1.15f, 1.45f), mandarinkaRedMaterial);
        CreateCylinder(root, "Gustav_Barrel", new Vector3(0f, 1.75f, -3.25f), Quaternion.Euler(82f, 0f, 0f), new Vector3(0.34f, 3.3f, 0.34f), mandarinkaDarkMaterial);
        CreateCylinder(root, "Gustav_Gold_Muzzle_Brake", new Vector3(0f, 2.2f, -6.15f), Quaternion.Euler(82f, 0f, 0f), new Vector3(0.5f, 0.16f, 0.5f), mandarinkaGoldMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -1.85f + i * 1.18f;
                CreateCylinder(root, "Gustav_Roadwheel_" + side + "_" + i, new Vector3(side * 2.42f, 0.34f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.48f, 0.16f, 0.48f), mandarinkaDarkMaterial);
            }
        }
        Transform muzzle = new GameObject("Mandarinka_Gustav_Muzzle").transform;
        muzzle.SetParent(root, false);
        muzzle.localPosition = new Vector3(0f, 2.1f, -6.45f);
        MandarinkaAsset asset = AddMandarinkaAsset(root, MandarinkaRole.Gustav, 540f);
        asset.visual = muzzle;
        asset.fireTimer = 8f;
        SetMandarinkaRadio("SEBEK: That siege gun is not decorative. Robert, when it speaks, do not admire it.");
    }

    private MandarinkaAsset AddMandarinkaAsset(Transform root, MandarinkaRole role, float health)
    {
        EnemyUnit enemy = RegisterMandarinkaEnemy(root, health);
        ConfigureMandarinkaTargetCollider(root, role);
        MandarinkaAsset asset = new MandarinkaAsset();
        asset.enemy = enemy;
        asset.role = role;
        asset.transform = root;
        asset.visual = root;
        asset.wobbleSeed = Random.Range(0f, 80f);
        mandarinkaAssets.Add(asset);
        return asset;
    }

    private void ConfigureMandarinkaTargetCollider(Transform root, MandarinkaRole role)
    {
        if (root == null)
            return;

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null)
            collider = root.gameObject.AddComponent<BoxCollider>();

        if (role == MandarinkaRole.Gustav)
        {
            collider.center = new Vector3(0f, 1.25f, -0.85f);
            collider.size = new Vector3(6.4f, 3.2f, 8.5f);
        }
        else if (role == MandarinkaRole.GroundCrawler)
        {
            collider.center = new Vector3(0f, 0.78f, 0f);
            collider.size = new Vector3(3.8f, 2.0f, 4.4f);
        }
        else if (role == MandarinkaRole.AirJunk)
        {
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.size = new Vector3(5.6f, 2.2f, 4.6f);
        }
        else if (role == MandarinkaRole.Builder)
        {
            collider.center = new Vector3(0f, 0.9f, 0.3f);
            collider.size = new Vector3(3.0f, 2.2f, 3.4f);
        }
        else if (role == MandarinkaRole.FieldTurret)
        {
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(2.6f, 2.3f, 2.6f);
        }
    }

    private Vector3 GetMandarinkaSpawnPoint(float sideOffset)
    {
        Vector3 center = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position : new Vector3(120f, 0f, 120f);
        Vector3 right = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.right : Vector3.right;
        Vector3 forward = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.forward : Vector3.forward;
        Vector3 position = center + right * sideOffset - forward * 14f + Random.insideUnitSphere * 2.5f;
        position.y = GetPlayableGroundHeight(position) + 0.35f;
        return position;
    }

    private void UpdateMandarinkaAssets(float dt)
    {
        for (int i = mandarinkaAssets.Count - 1; i >= 0; i--)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset == null || asset.transform == null || asset.enemy == null || asset.enemy.health <= 0f)
            {
                mandarinkaAssets.RemoveAt(i);
                continue;
            }

            if (asset.role == MandarinkaRole.Fortress)
                continue;

            if (asset.role == MandarinkaRole.GroundCrawler)
                UpdateMandarinkaGroundCrawler(asset, dt);
            else if (asset.role == MandarinkaRole.AirJunk)
                UpdateMandarinkaAirJunk(asset, dt);
            else if (asset.role == MandarinkaRole.Builder)
                UpdateMandarinkaBuilder(asset, dt);
            else if (asset.role == MandarinkaRole.FieldTurret)
                UpdateMandarinkaTurret(asset, dt);
            else if (asset.role == MandarinkaRole.Gustav)
                UpdateMandarinkaGustav(asset, dt);
        }
    }

    private void UpdateMandarinkaGroundCrawler(MandarinkaAsset asset, float dt)
    {
        RunnerUnit runnerTarget = FindNearestRunner(asset.transform.position, 34f);
        Vector3 target = runnerTarget != null && runnerTarget.transform != null ? runnerTarget.transform.position : asset.hasObjective ? asset.objective : battlePyramid.position;
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance || FlatDistance(asset.transform.position, battlePyramid.position) < 130f)
            target = runnerTarget != null && runnerTarget.transform != null ? runnerTarget.transform.position : battlePyramid.position;

        MoveMandarinkaGroundAsset(asset.transform, target, 5.8f, dt, 16f);
        if (asset.hasObjective && FlatDistance(asset.transform.position, asset.objective) <= 18f)
            asset.objective = PickMandarinkaRaidObjective();

        asset.fireTimer -= dt;
        float distance = FlatDistance(asset.transform.position, target);
        if (asset.fireTimer <= 0f && distance <= 24f)
        {
            asset.fireTimer = 1.25f;
            if (runnerTarget != null && runnerTarget.transform != null)
                runnerTarget.health -= 18f;
            else
                pyramidHull -= 18f;
            CreateBeam(asset.transform.position + Vector3.up * 0.8f, target + Vector3.up * 1.0f, new Color(0.1f, 1f, 0.58f, 1f), 0.045f, 0.12f);
        }
    }

    private void UpdateMandarinkaAirJunk(MandarinkaAsset asset, float dt)
    {
        Vector3 center = mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance || FlatDistance(asset.transform.position, battlePyramid.position) < 160f
            ? battlePyramid.position
            : asset.hasObjective ? asset.objective : PickMandarinkaRaidObjective();
        Vector3 orbit = center + Quaternion.Euler(0f, Time.time * 17f + asset.wobbleSeed, 0f) * Vector3.forward * 38f;
        orbit.y = GetPlayableGroundHeight(orbit) + 10.5f + Mathf.Sin(Time.time * 2.2f + asset.wobbleSeed) * 1.3f;
        asset.transform.position = Vector3.Lerp(asset.transform.position, orbit, dt * 0.8f);
        RotateFlatToward(asset.transform, center - asset.transform.position, 130f * dt);

        asset.fireTimer -= dt;
        if (asset.fireTimer <= 0f && FlatDistance(asset.transform.position, battlePyramid.position) <= 86f)
        {
            asset.fireTimer = 2.2f;
            pyramidHull -= 13f;
            CreateBeam(asset.transform.position, battlePyramid.position + Vector3.up * 2.4f, new Color(0.1f, 1f, 0.58f, 1f), 0.035f, 0.16f);
        }
    }

    private void UpdateMandarinkaBuilder(MandarinkaAsset asset, float dt)
    {
        if (!asset.hasObjective)
        {
            asset.objective = PickMandarinkaExpansionObjective(asset.transform.position);
            asset.hasObjective = true;
        }

        MoveMandarinkaGroundAsset(asset.transform, asset.objective, 5.4f, dt, 4.4f);
        if (FlatDistance(asset.transform.position, asset.objective) <= 4.8f)
        {
            bool strategicWork = UpdateMandarinkaBuilderExpansion(asset, dt);
            asset.gatherTimer += dt;
            if (!strategicWork)
                mandarinkaSupply += 0.8f * dt;
            asset.buildTimer -= dt;
            if (asset.buildTimer <= 0f && mandarinkaSupply >= 52f)
            {
                int turretLimit = 2 + mandarinkaFortressPhase;
                if (CountMandarinkaRole(MandarinkaRole.FieldTurret) < turretLimit)
                {
                    mandarinkaSupply -= 52f;
                    asset.buildTimer = Random.Range(22f, 32f);
                    SpawnMandarinkaTurret(asset.transform.position + Random.insideUnitSphere * 8f);
                    SetMandarinkaRadio("SEBEK: She is planting guns in my desert. Rude. Efficient, but rude.");
                }
                else
                {
                    asset.buildTimer = Random.Range(12f, 18f);
                }
            }

            if (asset.gatherTimer >= 12f)
            {
                asset.gatherTimer = 0f;
                asset.hasObjective = false;
            }
        }
    }

    private void UpdateMandarinkaTurret(MandarinkaAsset asset, float dt)
    {
        asset.fireTimer -= dt;
        Vector3 target = battlePyramid.position;
        RunnerUnit runnerTarget = FindNearestRunner(asset.transform.position, 46f);
        if (runnerTarget != null && runnerTarget.transform != null)
            target = runnerTarget.transform.position;

        RotateFlatToward(asset.transform, target - asset.transform.position, 180f * dt);
        if (asset.fireTimer <= 0f && FlatDistance(asset.transform.position, target) <= 128f)
        {
            asset.fireTimer = 2.6f;
            if (runnerTarget != null && runnerTarget.transform != null)
                runnerTarget.health -= 24f;
            else
                pyramidHull -= 16f;
            CreateBeam(asset.transform.position + Vector3.up * 1.4f, target + Vector3.up * 1.1f, new Color(1f, 0.12f, 0.04f, 1f), 0.055f, 0.14f);
        }
    }

    private void UpdateMandarinkaGustav(MandarinkaAsset asset, float dt)
    {
        Vector3 toPyramid = battlePyramid.position - asset.transform.position;
        float distance = FlatDistance(asset.transform.position, battlePyramid.position);
        if (distance > 185f)
            MoveMandarinkaGroundAsset(asset.transform, battlePyramid.position, 3.8f, dt, 175f);
        else
            RotateFlatToward(asset.transform, toPyramid, 75f * dt);

        asset.fireTimer -= dt;
        if (asset.fireTimer <= 0f && distance <= 300f)
        {
            asset.fireTimer = Random.Range(12f, 15f);
            Transform muzzle = asset.visual != null ? asset.visual : asset.transform;
            bool intendedHit = Random.value < 0.66f;
            Vector2 error = intendedHit ? Random.insideUnitCircle * 3.2f : Random.insideUnitCircle.normalized * Random.Range(18f, 34f);
            Vector3 target = battlePyramid.position + new Vector3(error.x, 0f, error.y);
            target.y = GetPlayableGroundHeight(target) + 0.25f;
            CreateHostileShell("Karl_Gustav_Heavy_Shell", muzzle.position, target, 135f, 13.5f, true, intendedHit, 2.15f, 55f);
            CreateWeaponFlash(muzzle.position, 0.72f, new Color(1f, 0.22f, 0.06f, 1f));
            lastEvent = "Karl Gustav siege gun fired. Four clean hits can cripple the pyramid.";
            if (!intendedHit)
                SetMandarinkaRadio(mandarinkaTaunts[mandarinkaTauntIndex++ % mandarinkaTaunts.Length]);
        }
    }

    private void MoveMandarinkaGroundAsset(Transform target, Vector3 destination, float speed, float dt, float stopDistance)
    {
        Vector3 toDestination = destination - target.position;
        toDestination.y = 0f;
        if (toDestination.magnitude > stopDistance)
            target.position += toDestination.normalized * speed * dt;
        RotateFlatToward(target, toDestination, 220f * dt);
        Vector3 position = target.position;
        position.y = GetPlayableGroundHeight(position) + 0.35f;
        target.position = position;
    }

    private void RotateFlatToward(Transform target, Vector3 direction, float maxDegrees)
    {
        direction.y = 0f;
        if (target == null || direction.sqrMagnitude < 0.01f)
            return;
        target.rotation = Quaternion.RotateTowards(target.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), maxDegrees);
    }

    private Vector3 PickMandarinkaRaidObjective()
    {
        Vector3 territoryObjective;
        if (TryGetMandarinkaStrategicTerritoryObjective(out territoryObjective) &&
            mandarinkaAssaultStep != MandarinkaAssaultStep.Suppress)
            return territoryObjective;
        if (mandarinkaPriorityCluster != null && mandarinkaAssaultStep != MandarinkaAssaultStep.Recon)
            return GetMandarinkaAssaultObjective(mandarinkaFortressRoot != null ? mandarinkaFortressRoot : battlePyramid, mandarinkaPriorityCluster.center);
        if (mandarinkaAvoidsCaravanRoutes)
            return GetMandarinkaCaravanBypassObjective();
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ScoutRaid)
            return GetDunePoint(new Vector3(-145f, 0f, -80f) + Random.insideUnitSphere * 55f);
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ResourceRaid)
            return GetDunePoint(new Vector3(40f, 0f, 120f) + Random.insideUnitSphere * 95f);
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.SiegeProbe)
            return GetDunePoint(battlePyramid.position + (mandarinkaFortressRoot.position - battlePyramid.position).normalized * 220f + Random.insideUnitSphere * 65f);
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.FortressAdvance)
            return GetDunePoint(battlePyramid.position + Random.insideUnitSphere * 45f);
        return GetDunePoint(mandarinkaHomePosition + Random.insideUnitSphere * 80f);
    }

    private Vector3 GetDunePoint(Vector3 point)
    {
        point.x = Mathf.Clamp(point.x, -mapHalfSize + 45f, mapHalfSize - 45f);
        point.z = Mathf.Clamp(point.z, -mapHalfSize + 45f, mapHalfSize - 45f);
        point.y = GetPlayableGroundHeight(point) + 0.35f;
        return point;
    }

    private string GetMandarinkaStrategyName()
    {
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.BuildUp)
            return "Build-up";
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ScoutRaid)
            return "Scout raid";
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.ResourceRaid)
            return "Resource raid";
        if (mandarinkaStrategyPhase == MandarinkaStrategyPhase.SiegeProbe)
            return "Siege probe";
        return "Fortress advance";
    }

    private Vector3 PickBuilderObjective(Vector3 origin)
    {
        return PickMandarinkaExpansionObjective(origin);
    }

    private void SpawnMandarinkaTurret(Vector3 position)
    {
        position.y = GetPlayableGroundHeight(position) + 0.2f;
        Transform root = new GameObject("Mandarinka_Field_Turret").transform;
        root.position = position;
        CreateCylinder(root, "Turret_Red_Base", new Vector3(0f, 0.35f, 0f), Quaternion.identity, new Vector3(1.2f, 0.28f, 1.2f), mandarinkaRedMaterial);
        CreateBox(root, "Turret_Red_Armor_Face", new Vector3(0f, 0.95f, -0.48f), Quaternion.identity, new Vector3(1.3f, 0.8f, 0.22f), mandarinkaRedMaterial);
        CreateBox(root, "Turret_Gold_Side_Plate_L", new Vector3(-0.72f, 0.72f, 0.08f), Quaternion.identity, new Vector3(0.18f, 0.52f, 0.95f), mandarinkaGoldMaterial);
        CreateBox(root, "Turret_Gold_Side_Plate_R", new Vector3(0.72f, 0.72f, 0.08f), Quaternion.identity, new Vector3(0.18f, 0.52f, 0.95f), mandarinkaGoldMaterial);
        CreateCylinder(root, "Turret_Jade_Cannon", new Vector3(0f, 1.05f, -0.78f), Quaternion.Euler(85f, 0f, 0f), new Vector3(0.18f, 1.05f, 0.18f), mandarinkaDarkMaterial);
        CreateCylinder(root, "Turret_Jade_Reactor_Core", new Vector3(0f, 0.92f, 0.35f), Quaternion.identity, new Vector3(0.32f, 0.15f, 0.32f), mandarinkaJadeMaterial);
        CreatePointLight(root, "Turret_Jade_Light", new Vector3(0f, 1.25f, -1.45f), new Color(0.1f, 1f, 0.58f, 1f), 0.7f, 8f);
        AddMandarinkaAsset(root, MandarinkaRole.FieldTurret, 180f);
    }

    private void CreateHostileShell(string name, Vector3 start, Vector3 target, float damage, float radius, bool gustav, bool intendedHit, float duration, float arcHeight, bool nashorn = false)
    {
        GameObject shellObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shellObject.name = name;
        shellObject.transform.position = start;
        shellObject.transform.localScale = gustav ? new Vector3(0.72f, 0.72f, 0.72f) : new Vector3(0.44f, 0.44f, 0.44f);
        Renderer renderer = shellObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mandarinkaShellMaterial;
        Collider collider = shellObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        AttachProjectileTrail(shellObject, gustav ? new Color(1f, 0.12f, 0.04f, 1f) : new Color(1f, 0.24f, 0.08f, 1f), gustav ? 0.36f : 0.22f, gustav ? 1f : 0.72f);

        HostileShellVisual shell = new HostileShellVisual();
        shell.transform = shellObject.transform;
        shell.start = start;
        shell.target = target;
        shell.damage = damage;
        shell.blastRadius = radius;
        shell.gustav = gustav;
        shell.nashorn = nashorn;
        shell.intendedHit = intendedHit;
        shell.duration = duration;
        shell.arcHeight = arcHeight;
        shell.warningRing = CreateIncomingImpactWarning(target, radius, gustav, nashorn);
        hostileShells.Add(shell);
        PlaySandRunnerSound(gustav ? SandRunnerSound.HeavyArtillery : SandRunnerSound.EnemyShot, start, gustav ? 1.2f : 0.82f);
        if (gustav)
            PlaySandRunnerSound(SandRunnerSound.GustavWarning, start, 1f);
    }

    private void UpdateHostileShells(float dt)
    {
        for (int i = hostileShells.Count - 1; i >= 0; i--)
        {
            HostileShellVisual shell = hostileShells[i];
            if (shell.transform == null)
            {
                hostileShells.RemoveAt(i);
                continue;
            }
            if (shell.nashorn && TryInterceptNashornShell(shell))
            {
                hostileShells.RemoveAt(i);
                continue;
            }

            shell.age += dt;
            float t = Mathf.Clamp01(shell.age / Mathf.Max(0.01f, shell.duration));
            Vector3 position = Vector3.Lerp(shell.start, shell.target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * shell.arcHeight;
            shell.transform.position = position;
            if (shell.warningRing != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * (shell.gustav ? 13f : 10f)) * 0.08f;
                shell.warningRing.localScale = new Vector3(pulse, pulse, pulse);
            }

            if (t >= 1f)
            {
                ResolveHostileShellImpact(shell);
                if (shell.warningRing != null)
                    Destroy(shell.warningRing.gameObject);
                Destroy(shell.transform.gameObject);
                hostileShells.RemoveAt(i);
            }
        }
    }

    private void ResolveHostileShellImpact(HostileShellVisual shell)
    {
        float pyramidDistance = FlatDistance(shell.target, battlePyramid.position);
        bool hitPyramid = pyramidDistance <= shell.blastRadius;
        if (hitPyramid)
        {
            float falloff = Mathf.Lerp(1f, 0.42f, pyramidDistance / shell.blastRadius);
            pyramidHull -= shell.damage * falloff;
            CreateFloatingCombatLabel(
                battlePyramid.position + Vector3.up * (shell.gustav ? 13f : 10f),
                "-" + Mathf.RoundToInt(shell.damage * falloff) + " HULL",
                shell.gustav ? new Color(1f, 0.08f, 0.035f, 1f) : new Color(1f, 0.28f, 0.08f, 1f),
                shell.gustav ? 1.2f : 0.95f);
            RegisterWeaponImpulse(shell.gustav ? 0.34f : 0.18f);
            PlaySandRunnerSound(SandRunnerSound.PyramidDamage, battlePyramid.position, shell.gustav ? 1f : 0.7f);
            if (shell.gustav)
                PlaySandRunnerSound(SandRunnerSound.Pyralert, battlePyramid.position, 0.9f);
            lastEvent = shell.gustav ? "Karl Gustav shell smashed the pyramid hull." : "Mandarinka artillery hit the pyramid armor.";
            if (shell.nashorn)
                ResolveNashornPyramidHit(shell);
        }
        else if (!shell.intendedHit)
        {
            SetMandarinkaRadio(mandarinkaTaunts[mandarinkaTauntIndex++ % mandarinkaTaunts.Length]);
        }

        for (int i = runners.Count - 1; i >= 0; i--)
        {
            RunnerUnit runner = runners[i];
            if (runner.transform == null)
                continue;
            float distance = FlatDistance(shell.target, runner.transform.position);
            if (distance <= shell.blastRadius)
                runner.health -= shell.damage * 0.85f;
        }

        CreateHostileImpactVisual(shell.target, shell.blastRadius, shell.gustav);
        PlaySandRunnerSound(shell.gustav ? SandRunnerSound.LargeExplosion : SandRunnerSound.Explosion, shell.target, shell.gustav ? 1.2f : 0.88f);
        pyramidHull = Mathf.Clamp(pyramidHull, 0f, pyramidMaxHull);
    }

    private Transform CreateIncomingImpactWarning(Vector3 target, float radius, bool gustav, bool nashorn = false)
    {
        return CreateReadableImpactWarning(
            target,
            radius,
            nashorn ? new Color(0.12f, 0.28f, 1f, 1f) : gustav ? new Color(1f, 0.08f, 0.035f, 1f) : new Color(0.1f, 1f, 0.58f, 1f),
            nashorn ? 3.1f : gustav ? 2.45f : 1.9f,
            nashorn ? "ANTIMATTER IMPACT" : gustav ? "GUSTAV IMPACT" : "INCOMING ARTILLERY",
            true);
    }

    private void CreateHostileImpactVisual(Vector3 position, float radius, bool gustav)
    {
        CreateBattleExplosionFx(position, radius, gustav, true);

        RingVisual ring = new RingVisual();
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = gustav ? "Karl_Gustav_Impact_Ring" : "Mandarinka_Impact_Ring";
        ringObject.transform.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.08f, position.z);
        ringObject.transform.localScale = new Vector3(1f, 0.03f, 1f);
        Collider collider = ringObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = ringObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = gustav ? nuclearMaterial : enemyMaterial;

        ring.transform = ringObject.transform;
        ring.life = gustav ? 0.85f : 0.5f;
        ring.maxLife = ring.life;
        ring.startScale = new Vector3(1f, 0.03f, 1f);
        ring.endScale = new Vector3(radius * 2.4f, 0.03f, radius * 2.4f);
        rings.Add(ring);

        Light flash = new GameObject(gustav ? "Karl_Gustav_Impact_Flash" : "Mandarinka_Impact_Flash").AddComponent<Light>();
        flash.transform.position = position + Vector3.up * 3f;
        flash.type = LightType.Point;
        flash.color = gustav ? new Color(1f, 0.28f, 0.08f, 1f) : new Color(1f, 0.08f, 0.03f, 1f);
        flash.intensity = gustav ? 7f : 3f;
        flash.range = radius * 2.8f;
        Destroy(flash.gameObject, gustav ? 0.7f : 0.4f);

        if (gustav)
        {
            CreateFloatingCombatLabel(position + Vector3.up * 5f + Random.insideUnitSphere * 3f, "KARL GUSTAV", new Color(1f, 0.15f, 0.05f, 1f), 1.5f);
            CreateFloatingCombatLabel(position + Vector3.up * 3f + Random.insideUnitSphere * 2f, "SIEGE HIT", new Color(1f, 0.5f, 0.1f, 1f), 1.2f);
        }
    }

    private int CountMandarinkaRole(MandarinkaRole role)
    {
        int count = 0;
        for (int i = 0; i < mandarinkaAssets.Count; i++)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset != null && asset.role == role && asset.enemy != null && asset.enemy.health > 0f && asset.transform != null)
                count++;
        }
        return count;
    }

    private bool IsMandarinkaControlledEnemy(Transform enemyTransform)
    {
        return enemyTransform != null && enemyTransform.name.StartsWith("Mandarinka_");
    }

    private void HandleMandarinkaEnemyDestroyed(Transform enemyTransform)
    {
        if (!IsMandarinkaControlledEnemy(enemyTransform))
            return;

        if (!mandarinkaDefeated && enemyTransform.name.Contains("Mobile_Fortress"))
        {
            bool earlyDefeat = SandRunnersWarRules.IsEarlyFortressDefeat(
                (int)verticalSliceStage, (int)VerticalSliceStage.DestroyMandarinka);
            mandarinkaDefeated = true;
            gold += 140f;
            wind += 60f;
            sand += 180f;

            if (earlyDefeat)
            {
                mandarinkaFinaleTriggered = true;
                mandarinkaFinaleTimer = 0f;
                mandarinkaFinaleDialoguePlayed = false;
                mandarinkaFinalePosition = enemyTransform.position;
                CreateBattleExplosionFx(enemyTransform.position + Vector3.up * 4f, 24f, false, true);
                CreateBattleExplosionFx(enemyTransform.position + new Vector3(8f, 3f, -6f), 16f, false, true);
                CreateFortressPhaseFx(enemyTransform.position, new Color(1f, 0.15f, 0.05f, 1f));
                CreateMandarinkaEscapeRocket(enemyTransform.position + Vector3.up * 9f);
                StartImperialRetaliation(enemyTransform.position);
                SetMandarinkaRadio("MANDARINKA: You destroyed a court before its appointed battle. Empress... I require the red protocol.");
                lastEvent = "The palace fell too early. Imperial retaliation forces are entering the desert.";
                return;
            }

            TriggerMandarinkaFinale(enemyTransform.position);
            SetMandarinkaRadio("MANDARINKA: This court has not finished with you. I will return from orbit.");
            lastEvent = "Mandarinka's mobile fortress is burning. The desert route is open.";
        }
    }

    private void TriggerMandarinkaFinale(Vector3 position)
    {
        if (mandarinkaFinaleTriggered)
            return;

        mandarinkaFinaleTriggered = true;
        mandarinkaFinaleTimer = 0f;
        mandarinkaFinaleDialoguePlayed = false;
        mandarinkaFinalePosition = position;
        CreateBattleExplosionFx(position + Vector3.up * 4f, 24f, false, true);
        CreateBattleExplosionFx(position + new Vector3(8f, 3f, -6f), 16f, false, true);
        CreateBattleExplosionFx(position + new Vector3(-7f, 6f, 5f), 18f, false, true);
        CreateFortressPhaseFx(position, new Color(1f, 0.15f, 0.05f, 1f));
        CreateMandarinkaEscapeRocket(position + Vector3.up * 9f);
        missionObjective = "Victory. Mandarinka escaped, but her mobile palace is destroyed.";
        MarkVictory();
    }

    private void CreateMandarinkaEscapeRocket(Vector3 position)
    {
        Transform rocket = new GameObject("Mandarinka_Emergency_Escape_Rocket").transform;
        rocket.position = position;
        rocket.rotation = Quaternion.Euler(-18f, 25f, 0f);
        CreateCylinder(rocket, "Escape_Rocket_Red_Hull", Vector3.zero, Quaternion.identity, new Vector3(0.48f, 1.75f, 0.48f), mandarinkaRedMaterial);
        CreateCylinder(rocket, "Escape_Rocket_Jade_Window", new Vector3(0f, 0.62f, 0.38f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 0.04f, 0.22f), mandarinkaJadeMaterial);
        CreateBox(rocket, "Escape_Rocket_Gold_Fin_L", new Vector3(-0.5f, -0.75f, 0f), Quaternion.identity, new Vector3(0.12f, 0.62f, 0.42f), mandarinkaGoldMaterial);
        CreateBox(rocket, "Escape_Rocket_Gold_Fin_R", new Vector3(0.5f, -0.75f, 0f), Quaternion.identity, new Vector3(0.12f, 0.62f, 0.42f), mandarinkaGoldMaterial);
        CreatePointLight(rocket, "Escape_Rocket_Jade_Light", new Vector3(0f, -1.05f, 0f), new Color(0.1f, 1f, 0.58f, 1f), 2.2f, 22f);
        AttachProjectileTrail(rocket.gameObject, new Color(1f, 0.22f, 0.05f, 1f), 0.55f, 1.4f);
        mandarinkaEscapeRocket = rocket;
    }

    private void UpdateMandarinkaFinale(float dt)
    {
        if (!mandarinkaFinaleTriggered)
            return;

        mandarinkaFinaleTimer += dt;
        if (mandarinkaEscapeRocket != null)
        {
            Vector3 velocity = new Vector3(8f, 34f + mandarinkaFinaleTimer * 10f, 18f);
            mandarinkaEscapeRocket.position += velocity * dt;
            mandarinkaEscapeRocket.Rotate(Vector3.forward, 95f * dt, Space.Self);
            if (mandarinkaFinaleTimer > 5.8f)
                Destroy(mandarinkaEscapeRocket.gameObject);
        }

        if (!mandarinkaFinaleDialoguePlayed && mandarinkaFinaleTimer > 2.2f && missionCommsTimer <= 0f)
        {
            mandarinkaFinaleDialoguePlayed = true;
            SetMissionDialogue("SEBEK-NU-ANKHA: Let her run. A fleeing advisor teaches the Empress fear.", 7f);
        }
    }

    private void SetMandarinkaRadio(string text)
    {
        SetMissionDialogue(text, 7.5f);
    }

    private void DrawMandarinkaGUI()
    {
        if (labelStyle == null || hudHidden || mandarinkaFortressEnemy == null || mandarinkaDefeated)
            return;

        float width = 276f;
        float x = Screen.width - width - 18f;
        float y = hudExpanded ? 300f : 104f;
        GUI.Box(new Rect(x, y, width, 98f), GUIContent.none);
        GUI.Label(new Rect(x + 12f, y + 8f, width - 24f, 20f), "Mandarinka mobile fortress", smallStyle);

        float health01 = Mathf.Clamp01(mandarinkaFortressEnemy.health / MandarinkaFortressMaxHull);
        float shield01 = Mathf.Clamp01(mandarinkaFortressShield / MandarinkaFortressMaxShield);
        GUI.Box(new Rect(x + 12f, y + 32f, width - 24f, 14f), GUIContent.none);
        GUI.color = new Color(0.82f, 0.04f, 0.035f, 1f);
        GUI.DrawTexture(new Rect(x + 14f, y + 34f, (width - 28f) * health01, 10f), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 1f, 0.62f, 0.85f);
        GUI.DrawTexture(new Rect(x + 14f, y + 47f, (width - 28f) * shield01, 5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + 12f, y + 54f, width - 24f, 20f), "Hull " + Mathf.RoundToInt(mandarinkaFortressEnemy.health) + " | Shield " + Mathf.RoundToInt(mandarinkaFortressShield), smallStyle);
        GUI.Label(new Rect(x + 12f, y + 68f, width - 24f, 20f), "Phase " + (mandarinkaFortressPhase + 1) + " | supply " + Mathf.RoundToInt(mandarinkaSupply), smallStyle);
    }
}