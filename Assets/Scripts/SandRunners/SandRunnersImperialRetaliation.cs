using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal static class SandRunnersImperialRules
{
    internal static float NextStrikeDelay(bool firstStrike, float firstDelay, float repeatDelay)
    {
        // Older serialized nested profiles can deserialize newly added fields as zero.
        // Preserve the campaign promise: 30 minutes before the first strike, six minutes thereafter.
        return firstStrike ? Mathf.Max(1800f, firstDelay) : Mathf.Max(360f, repeatDelay);
    }

    internal static bool HasAlternativeVictory(int destroyedHeadquarters, int requiredHeadquarters)
    {
        return requiredHeadquarters > 0 && destroyedHeadquarters >= requiredHeadquarters;
    }

    internal static bool AllSettlementsLost(int livingSettlements)
    {
        return livingSettlements <= 0;
    }
}

public partial class SandRunnersPrototype
{
    private enum AssaultHeadquartersKind
    {
        BeetleHowitzer,
        HumanoidMortar,
        MothCarrier
    }

    private enum ImperialRetaliationState
    {
        Inactive,
        Deployment,
        Hunt,
        NuclearEscalation,
        Victory,
        Defeat
    }

    private sealed class AssaultHeadquarters
    {
        public AssaultHeadquartersKind kind;
        public Transform root;
        public EnemyUnit enemy;
        public float productionTimer;
        public float weaponTimer;
        public float deploymentTimer;
        public int waveIndex;
        public Vector3 siegeAnchor;
        public bool destroyed;
    }

    private sealed class HostileStrategicMissile
    {
        public Transform root;
        public EnemyUnit enemy;
        public SettlementDevelopmentState target;
        public Vector3 start;
        public Vector3 destination;
        public float age;
        public float duration;
        public bool resolved;
    }

    private readonly List<AssaultHeadquarters> imperialHeadquarters = new List<AssaultHeadquarters>();
    private readonly List<HostileStrategicMissile> hostileStrategicMissiles = new List<HostileStrategicMissile>();

    private ImperialRetaliationState imperialRetaliationState;
    private Transform imperialRetaliationRoot;
    private float imperialDeploymentTimer;
    private float imperialStrikeTimer;
    private bool imperialFirstStrikePending;
    private SettlementDevelopmentState imperialStrikeTarget;
    private int imperialHeadquartersDestroyed;
    private bool imperialAlternativeVictory;
    private GameObject imperialCrisisPanelObject;
    private Text imperialCrisisTitleText;
    private Text imperialCrisisTimerText;
    private Text imperialCrisisTargetText;
    private Text imperialCrisisHeadquartersText;

    private void InitializeImperialRetaliation()
    {
        if (imperialRetaliationRoot == null)
        {
            GameObject old = GameObject.Find("SandRunners_Imperial_Retaliation_Runtime");
            if (old != null)
                Destroy(old);
            imperialRetaliationRoot = new GameObject("SandRunners_Imperial_Retaliation_Runtime").transform;
        }

        imperialRetaliationState = ImperialRetaliationState.Inactive;
        InitializeImperialCrisisUi();
    }

    private void InitializeImperialCrisisUi()
    {
        if (strategicCanvas == null || imperialCrisisPanelObject != null)
            return;
        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Imperial_Retaliation_Crisis",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-260f, -18f), new Vector2(520f, 106f), UiGlassDeep);
        imperialCrisisPanelObject = panel.gameObject;
        imperialCrisisTitleText = CreateText(panel, "Crisis_Title", "IMPERIAL RETALIATION", 14, FontStyle.Bold,
            new Vector2(14f, -8f), new Vector2(492f, 20f), new Color(1f, 0.22f, 0.08f, 1f));
        imperialCrisisTimerText = CreateText(panel, "Crisis_Timer", "", 20, FontStyle.Bold,
            new Vector2(14f, -31f), new Vector2(150f, 28f), UiGold);
        imperialCrisisTargetText = CreateText(panel, "Crisis_Target", "", 11, FontStyle.Bold,
            new Vector2(170f, -33f), new Vector2(336f, 32f), UiTextBody);
        imperialCrisisHeadquartersText = CreateText(panel, "Crisis_Hq", "", 10, FontStyle.Normal,
            new Vector2(14f, -70f), new Vector2(492f, 25f), UiBlueSoft);
        imperialCrisisPanelObject.SetActive(false);
    }

    private void StartImperialRetaliation(Vector3 fortressPosition)
    {
        if (imperialRetaliationState != ImperialRetaliationState.Inactive)
            return;

        InitializeImperialRetaliation();
        imperialRetaliationState = ImperialRetaliationState.Deployment;
        imperialDeploymentTimer = 0f;
        imperialStrikeTimer = SandRunnersImperialRules.NextStrikeDelay(true,
            balanceProfile.imperialFirstStrikeSeconds, balanceProfile.imperialRepeatStrikeSeconds);
        imperialFirstStrikePending = true;
        imperialHeadquartersDestroyed = 0;
        imperialAlternativeVictory = false;
        imperialStrikeTarget = ChooseImperialStrikeTarget();

        SetMandarinkaRadio("MANDARINKA: It was one castle. A mobile castle. RED EMPRESS: You lost an entire court and call it one?");
        SetMissionDialogue("RED EMPRESS: Deploy the three assault thrones. Sebek will learn what follows disobedience.", 12f);
        ShowBanner("IMPERIAL RETALIATION // ORBITAL DEPLOYMENT", 5f);
        lastEvent = "Mandarinka lost her palace. The Red Empress has assumed direct command.";

        Vector3 demonstration = GetDunePoint(new Vector3(-mapHalfSize * 0.78f, 0f, mapHalfSize * 0.74f));
        CreateBattleExplosionFx(demonstration + Vector3.up * 2f, 72f, true, true);
        PlaySandRunnerSound(SandRunnerSound.NuclearExplosion, demonstration, 1f);
        CreateFloatingCombatLabel(demonstration + Vector3.up * 14f, "DEMONSTRATION STRIKE", new Color(1f, 0.24f, 0.04f, 1f), 2f);

        SpawnImperialHeadquarters(AssaultHeadquartersKind.BeetleHowitzer,
            FindClearImperialLanding(new Vector3(-mapHalfSize * 0.58f, 0f, mapHalfSize * 0.26f)));
        SpawnImperialHeadquarters(AssaultHeadquartersKind.HumanoidMortar,
            FindClearImperialLanding(new Vector3(mapHalfSize * 0.58f, 0f, mapHalfSize * 0.34f)));
        SpawnImperialHeadquarters(AssaultHeadquartersKind.MothCarrier,
            FindClearImperialLanding(new Vector3(0f, 0f, mapHalfSize * 0.69f)));

        if (imperialCrisisPanelObject != null)
            imperialCrisisPanelObject.SetActive(true);
    }

    private Vector3 FindClearImperialLanding(Vector3 preferred)
    {
        preferred = GetDunePoint(preferred);
        for (int ring = 0; ring < 6; ring++)
        {
            float radius = ring * 28f;
            for (int step = 0; step < 10; step++)
            {
                Vector3 candidate = preferred + Quaternion.Euler(0f, step * 36f, 0f) * Vector3.forward * radius;
                candidate = GetDunePoint(candidate);
                if (!IsLargeAssetPositionBlocked(imperialRetaliationRoot, candidate, 18f))
                    return candidate;
            }
        }
        return preferred;
    }

    private void SpawnImperialHeadquarters(AssaultHeadquartersKind kind, Vector3 position)
    {
        Transform root = new GameObject("Imperial_Assault_HQ_" + kind).transform;
        root.SetParent(imperialRetaliationRoot, false);
        root.position = position;
        root.rotation = Quaternion.LookRotation((battlePyramid.position - position).normalized, Vector3.up);

        if (kind == AssaultHeadquartersKind.BeetleHowitzer)
            BuildImperialBeetle(root);
        else if (kind == AssaultHeadquartersKind.HumanoidMortar)
            BuildImperialHumanoid(root);
        else
            BuildImperialMoth(root);

        BoxCollider targetCollider = root.gameObject.AddComponent<BoxCollider>();
        targetCollider.center = kind == AssaultHeadquartersKind.MothCarrier ? new Vector3(0f, 8f, 0f) : new Vector3(0f, 5f, 0f);
        targetCollider.size = kind == AssaultHeadquartersKind.MothCarrier
            ? new Vector3(36f, 16f, 24f)
            : new Vector3(22f, 15f, 24f);

        EnemyUnit enemy = new EnemyUnit();
        enemy.transform = root;
        enemy.health = kind == AssaultHeadquartersKind.MothCarrier ? 7600f : 8200f;
        enemy.maxHealth = enemy.health;
        enemy.factionTag = "IMPERIAL_ASSAULT_HQ";
        enemy.isAssaultHeadquarters = true;
        enemies.Add(enemy);

        AssaultHeadquarters headquarters = new AssaultHeadquarters();
        headquarters.kind = kind;
        headquarters.root = root;
        headquarters.enemy = enemy;
        headquarters.productionTimer = kind == AssaultHeadquartersKind.MothCarrier ? 5f : 7f;
        headquarters.weaponTimer = 6f + imperialHeadquarters.Count * 2f;
        headquarters.deploymentTimer = 0f;
        Vector3 fromPyramid = position - battlePyramid.position;
        fromPyramid.y = 0f;
        headquarters.siegeAnchor = GetDunePoint(battlePyramid.position + fromPyramid.normalized * 255f);
        imperialHeadquarters.Add(headquarters);

        // Every throne lands as a defended military formation. The production
        // cycle then replaces losses up to the shared presentation limits.
        if (kind == AssaultHeadquartersKind.BeetleHowitzer)
        {
            SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 4);
            SpawnImperialRolePack(headquarters, MandarinkaRole.Builder, 1);
        }
        else if (kind == AssaultHeadquartersKind.HumanoidMortar)
        {
            SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 3);
            SpawnImperialRolePack(headquarters, MandarinkaRole.AirJunk, 2);
        }
        else
        {
            SpawnImperialRolePack(headquarters, MandarinkaRole.AirJunk, 5);
            SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 2);
        }

        CreateFortressPhaseFx(position + Vector3.up * 4f, new Color(1f, 0.12f, 0.03f, 1f));
        PlaySandRunnerSound(SandRunnerSound.HeavyImpact, position, 1f);
    }

    private void BuildImperialBeetle(Transform root)
    {
        CreateBox(root, "Beetle_Lower_Hull", new Vector3(0f, 3.1f, 0f), Quaternion.identity, new Vector3(13f, 4.8f, 17f), mandarinkaDarkMaterial, true);
        CreateCylinder(root, "Beetle_Carapace", new Vector3(0f, 6.4f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(7.2f, 8.2f, 7.2f), mandarinkaRedMaterial, true);
        CreateCylinder(root, "Beetle_Howitzer", new Vector3(0f, 9.2f, -9.5f), Quaternion.Euler(83f, 0f, 0f), new Vector3(1.05f, 8.5f, 1.05f), mandarinkaDarkMaterial);
        CreateCylinder(root, "Beetle_Gold_Muzzle", new Vector3(0f, 10.1f, -17.3f), Quaternion.Euler(83f, 0f, 0f), new Vector3(1.55f, 0.45f, 1.55f), mandarinkaGoldMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int leg = 0; leg < 3; leg++)
            {
                float z = -6f + leg * 6f;
                CreateBox(root, "Beetle_Leg_" + side + "_" + leg, new Vector3(side * 9.5f, 2.1f, z),
                    Quaternion.Euler(0f, 0f, side * -18f), new Vector3(7f, 1.2f, 1.4f), mandarinkaGoldMaterial, true);
            }
        }
        CreatePointLight(root, "Beetle_Jade_Eyes", new Vector3(0f, 7.4f, -8.2f), new Color(0.1f, 1f, 0.58f, 1f), 1.8f, 28f);
    }

    private void BuildImperialHumanoid(Transform root)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox(root, "Humanoid_Leg_" + side, new Vector3(side * 4.4f, 3.4f, 0f), Quaternion.identity, new Vector3(5.2f, 7f, 6f), mandarinkaDarkMaterial, true);
            CreateBox(root, "Humanoid_Shoulder_" + side, new Vector3(side * 7f, 11.5f, 0f), Quaternion.Euler(0f, 0f, side * 8f), new Vector3(6f, 3.6f, 6.4f), mandarinkaRedMaterial, true);
            CreateCylinder(root, "Chest_Gun_" + side, new Vector3(side * 2.4f, 9f, -5.8f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.55f, 4.8f, 0.55f), mandarinkaDarkMaterial);
        }
        CreateBox(root, "Humanoid_Torso", new Vector3(0f, 9f, 0f), Quaternion.identity, new Vector3(10f, 8f, 7f), mandarinkaRedMaterial, true);
        CreateBox(root, "Humanoid_Head", new Vector3(0f, 14.5f, -0.4f), Quaternion.identity, new Vector3(4.2f, 3.2f, 3.8f), mandarinkaGoldMaterial, true);
        CreateCylinder(root, "Humanoid_Mortar", new Vector3(0f, 15.2f, 2.6f), Quaternion.Euler(28f, 0f, 0f), new Vector3(1.2f, 5.5f, 1.2f), mandarinkaDarkMaterial);
        CreatePointLight(root, "Humanoid_Jade_Visor", new Vector3(0f, 14.7f, -2.4f), new Color(0.1f, 1f, 0.58f, 1f), 2f, 30f);
    }

    private void BuildImperialMoth(Transform root)
    {
        root.position += Vector3.up * 18f;
        CreateBox(root, "Moth_Central_Hangar", Vector3.zero, Quaternion.identity, new Vector3(12f, 6f, 18f), mandarinkaRedMaterial, true);
        CreateBox(root, "Moth_Factory_Keel", new Vector3(0f, -4f, 1f), Quaternion.identity, new Vector3(7f, 4f, 13f), mandarinkaDarkMaterial, true);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox(root, "Moth_Sail_Inner_" + side, new Vector3(side * 10f, 2f, 0f),
                Quaternion.Euler(12f, side * -8f, side * 18f), new Vector3(13f, 0.8f, 19f), mandarinkaGoldMaterial, true);
            CreateBox(root, "Moth_Sail_Outer_" + side, new Vector3(side * 21f, 4f, 2f),
                Quaternion.Euler(18f, side * -12f, side * 27f), new Vector3(15f, 0.65f, 22f), mandarinkaRedMaterial, true);
        }
        CreateBox(root, "Moth_Hangar_Mouth", new Vector3(0f, -0.5f, -9.4f), Quaternion.identity, new Vector3(7f, 3.2f, 1f), mandarinkaJadeMaterial);
        CreatePointLight(root, "Moth_Hangar_Light", new Vector3(0f, -0.5f, -10f), new Color(0.1f, 1f, 0.58f, 1f), 2.5f, 36f);
    }

    private void UpdateImperialRetaliation(float dt)
    {
        if (imperialRetaliationState == ImperialRetaliationState.Inactive ||
            imperialRetaliationState == ImperialRetaliationState.Victory ||
            imperialRetaliationState == ImperialRetaliationState.Defeat)
            return;

        if (imperialCrisisPanelObject == null)
            InitializeImperialCrisisUi();

        imperialDeploymentTimer += dt;
        if (imperialRetaliationState == ImperialRetaliationState.Deployment && imperialDeploymentTimer >= 7f)
        {
            imperialRetaliationState = ImperialRetaliationState.Hunt;
            SetMissionDialogue("RED EMPRESS: Thirty minutes, Sebek. Then I burn one settlement after another and leave the ash on your hands.", 13f);
            ShowBanner("30:00 // DESTROY ALL THREE ASSAULT HEADQUARTERS", 5f);
        }

        UpdateImperialHeadquarters(dt);
        UpdateMandarinkaAssets(dt);
        UpdateHostileShells(dt);
        UpdateHostileStrategicMissiles(dt);

        imperialStrikeTimer -= dt;
        if (imperialStrikeTimer <= 0f && hostileStrategicMissiles.Count == 0)
        {
            imperialRetaliationState = ImperialRetaliationState.NuclearEscalation;
            LaunchImperialStrategicMissile();
            imperialFirstStrikePending = false;
            imperialStrikeTimer = SandRunnersImperialRules.NextStrikeDelay(false,
                balanceProfile.imperialFirstStrikeSeconds, balanceProfile.imperialRepeatStrikeSeconds);
        }

        UpdateImperialCrisisUi();
        if (SandRunnersImperialRules.AllSettlementsLost(CountLivingNeutralSettlements()))
            TriggerImperialRetaliationDefeat();
    }

    private void UpdateImperialHeadquarters(float dt)
    {
        for (int i = 0; i < imperialHeadquarters.Count; i++)
        {
            AssaultHeadquarters headquarters = imperialHeadquarters[i];
            if (headquarters == null || headquarters.destroyed || headquarters.root == null || headquarters.enemy == null || headquarters.enemy.health <= 0f)
                continue;

            headquarters.deploymentTimer += dt;
            if (headquarters.deploymentTimer < 12f)
            {
                // Orbital landing shields prevent the player's existing death-ball
                // from deleting a headquarters before its escort can deploy.
                headquarters.enemy.health = headquarters.enemy.maxHealth;
            }

            Vector3 toAnchor = headquarters.siegeAnchor - headquarters.root.position;
            toAnchor.y = 0f;
            if (headquarters.kind == AssaultHeadquartersKind.MothCarrier)
            {
                if (toAnchor.magnitude > 45f)
                    headquarters.root.position += toAnchor.normalized * 1.35f * dt;
                Vector3 mothPosition = headquarters.root.position;
                mothPosition.y = GetPlayableGroundHeight(mothPosition) + 18f + Mathf.Sin(Time.time * 0.65f + i) * 1.2f;
                headquarters.root.position = mothPosition;
            }
            else if (toAnchor.magnitude > 28f)
            {
                Vector3 direction = GetLargeAssetSafeDirection(headquarters.root, toAnchor, 17f, dt);
                headquarters.root.position += direction * 0.72f * dt;
                Vector3 position = headquarters.root.position;
                position.y = GetPlayableGroundHeight(position) + 0.35f;
                headquarters.root.position = position;
            }
            RotateFlatToward(headquarters.root, battlePyramid.position - headquarters.root.position, 16f * dt);

            headquarters.weaponTimer -= dt;
            if (headquarters.weaponTimer <= 0f)
            {
                headquarters.weaponTimer = headquarters.kind == AssaultHeadquartersKind.HumanoidMortar ? 7.5f : 10.5f;
                FireImperialHeadquartersWeapon(headquarters);
            }

            headquarters.productionTimer -= dt;
            if (headquarters.productionTimer <= 0f)
            {
                headquarters.productionTimer = Random.Range(18f, 28f);
                ProduceImperialMixedWave(headquarters);
            }
        }
    }

    private void FireImperialHeadquartersWeapon(AssaultHeadquarters headquarters)
    {
        Vector3 target = GetMandarinkaStrategicAimPoint(battlePyramid.position);
        if (headquarters.kind == AssaultHeadquartersKind.MothCarrier)
        {
            GoldenStructure structure = FindMandarinkaStructureTarget(headquarters.root.position, 260f);
            if (structure != null)
                DamageMandarinkaTarget(structure, 34f, headquarters.root.position + Vector3.down * 2f);
            else
            {
                pyramidHull -= 26f;
                CreateBeam(headquarters.root.position + Vector3.down * 2f, battlePyramid.position + Vector3.up * 2f,
                    new Color(0.1f, 1f, 0.58f, 1f), 0.12f, 0.25f);
            }
            return;
        }

        float damage = headquarters.kind == AssaultHeadquartersKind.BeetleHowitzer ? 96f : 72f;
        float radius = headquarters.kind == AssaultHeadquartersKind.BeetleHowitzer ? 15f : 12f;
        Vector3 muzzle = headquarters.root.position + Vector3.up * (headquarters.kind == AssaultHeadquartersKind.BeetleHowitzer ? 10f : 15f);
        CreateHostileShell("Imperial_Assault_HQ_Shell", muzzle, target, damage, radius, false, true, 2.1f, 52f);
        CreateWeaponFlash(muzzle, 0.8f, new Color(1f, 0.18f, 0.04f, 1f));
    }

    private void ProduceImperialMixedWave(AssaultHeadquarters headquarters)
    {
        int ground = CountMandarinkaRole(MandarinkaRole.GroundCrawler);
        int air = CountMandarinkaRole(MandarinkaRole.AirJunk);
        int engineers = CountMandarinkaRole(MandarinkaRole.Builder) + CountMandarinkaRole(MandarinkaRole.FieldTurret);
        float roll = Random.value;

        if (headquarters.kind == AssaultHeadquartersKind.BeetleHowitzer)
        {
            if (ground < balanceProfile.imperialGroundUnitLimit && roll < 0.62f)
                SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 3);
            else if (engineers < balanceProfile.imperialEngineerLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.Builder, 1);
            else if (air < balanceProfile.imperialAirUnitLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.AirJunk, 2);
        }
        else if (headquarters.kind == AssaultHeadquartersKind.HumanoidMortar)
        {
            if (ground < balanceProfile.imperialGroundUnitLimit && roll < 0.52f)
                SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 2);
            else if (air < balanceProfile.imperialAirUnitLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.AirJunk, 2);
            else if (engineers < balanceProfile.imperialEngineerLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.Builder, 1);
        }
        else
        {
            if (air < balanceProfile.imperialAirUnitLimit && roll < 0.65f)
                SpawnImperialRolePack(headquarters, MandarinkaRole.AirJunk, 5);
            else if (ground < balanceProfile.imperialGroundUnitLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.GroundCrawler, 3);
            else if (engineers < balanceProfile.imperialEngineerLimit)
                SpawnImperialRolePack(headquarters, MandarinkaRole.Builder, 1);
        }

        headquarters.waveIndex++;
        lastEvent = headquarters.kind + " deployed mixed assault wave " + headquarters.waveIndex + ".";
    }

    private void SpawnImperialRolePack(AssaultHeadquarters headquarters, MandarinkaRole role, int count)
    {
        int allowed = count;
        if (role == MandarinkaRole.GroundCrawler)
            allowed = Mathf.Min(count, Mathf.Max(0, balanceProfile.imperialGroundUnitLimit - CountMandarinkaRole(role)));
        else if (role == MandarinkaRole.AirJunk)
            allowed = Mathf.Min(count, Mathf.Max(0, balanceProfile.imperialAirUnitLimit - CountMandarinkaRole(role)));
        else if (role == MandarinkaRole.Builder)
            allowed = Mathf.Min(count, Mathf.Max(0, balanceProfile.imperialEngineerLimit - CountMandarinkaRole(role)));

        for (int i = 0; i < allowed; i++)
        {
            int before = mandarinkaAssets.Count;
            if (role == MandarinkaRole.GroundCrawler)
                SpawnMandarinkaGroundCrawler();
            else if (role == MandarinkaRole.AirJunk)
                SpawnMandarinkaAirJunk();
            else if (role == MandarinkaRole.Builder)
                SpawnMandarinkaBuilder();

            if (mandarinkaAssets.Count > before)
            {
                MandarinkaAsset asset = mandarinkaAssets[mandarinkaAssets.Count - 1];
                Vector3 offset = headquarters.root.right * ((i - (allowed - 1) * 0.5f) * 6f) - headquarters.root.forward * 18f;
                asset.transform.position = headquarters.root.position + offset;
                asset.objective = GetMandarinkaAssaultObjective(asset.transform, battlePyramid.position);
                asset.hasObjective = true;
            }
        }
    }

    private SettlementDevelopmentState ChooseImperialStrikeTarget()
    {
        SettlementDevelopmentState best = null;
        float bestDefense = float.MaxValue;
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.settlement == null || state.settlement.root == null || state.settlement.health <= 0f)
                continue;
            if (state.defenseLevel < bestDefense)
            {
                bestDefense = state.defenseLevel;
                best = state;
            }
        }
        return best;
    }

    private void LaunchImperialStrategicMissile()
    {
        if (imperialStrikeTarget == null || imperialStrikeTarget.settlement == null || imperialStrikeTarget.settlement.root == null ||
            imperialStrikeTarget.settlement.health <= 0f)
            imperialStrikeTarget = ChooseImperialStrikeTarget();
        if (imperialStrikeTarget == null)
            return;

        Vector3 destination = imperialStrikeTarget.settlement.root.position;
        Vector3 outward = destination.normalized;
        if (outward.sqrMagnitude < 0.01f)
            outward = Vector3.forward;
        Vector3 start = destination + outward * 520f + Vector3.up * 145f;

        GameObject missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missileObject.name = "Imperial_Strategic_Nuclear_Missile";
        missileObject.transform.SetParent(imperialRetaliationRoot, false);
        missileObject.transform.position = start;
        missileObject.transform.localScale = new Vector3(1.4f, 5.8f, 1.4f);
        Collider collider = missileObject.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
        Renderer renderer = missileObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = nuclearMaterial;
        AttachProjectileTrail(missileObject, new Color(1f, 0.12f, 0.025f, 1f), 0.8f, 2f);

        EnemyUnit enemy = new EnemyUnit();
        enemy.transform = missileObject.transform;
        enemy.health = 240f;
        enemy.maxHealth = 240f;
        enemy.factionTag = "IMPERIAL_STRATEGIC_MISSILE";
        enemy.isStrategicMissile = true;
        enemies.Add(enemy);

        HostileStrategicMissile missile = new HostileStrategicMissile();
        missile.root = missileObject.transform;
        missile.enemy = enemy;
        missile.target = imperialStrikeTarget;
        missile.start = start;
        missile.destination = destination;
        missile.duration = balanceProfile.imperialMissileFlightSeconds;
        hostileStrategicMissiles.Add(missile);

        ShowBanner("NUCLEAR LAUNCH // " + imperialStrikeTarget.settlement.displayName + " // 25 SECONDS", 5f);
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, start, 1f);
        lastEvent = "Imperial nuclear missile inbound to " + imperialStrikeTarget.settlement.displayName + ".";
    }

    private void UpdateHostileStrategicMissiles(float dt)
    {
        for (int i = hostileStrategicMissiles.Count - 1; i >= 0; i--)
        {
            HostileStrategicMissile missile = hostileStrategicMissiles[i];
            if (missile == null || missile.resolved || missile.root == null || missile.enemy == null)
            {
                hostileStrategicMissiles.RemoveAt(i);
                continue;
            }

            if (missile.enemy.health <= 0f)
            {
                ResolveStrategicMissileIntercept(missile);
                hostileStrategicMissiles.RemoveAt(i);
                continue;
            }

            AutoInterceptStrategicMissileWithAircraft(missile, dt);
            missile.age += dt;
            float t = Mathf.Clamp01(missile.age / Mathf.Max(1f, missile.duration));
            Vector3 position = Vector3.Lerp(missile.start, missile.destination, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 105f;
            missile.root.position = position;
            Vector3 tangent = (missile.destination - missile.start) + Vector3.down * Mathf.Lerp(-50f, 120f, t);
            if (tangent.sqrMagnitude > 0.01f)
                missile.root.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);

            if (t >= 1f)
            {
                ResolveStrategicMissileImpact(missile);
                hostileStrategicMissiles.RemoveAt(i);
            }
        }
    }

    private void AutoInterceptStrategicMissileWithAircraft(HostileStrategicMissile missile, float dt)
    {
        missile.enemy.fireCooldown -= dt;
        if (missile.enemy.fireCooldown > 0f)
            return;
        RunnerUnit interceptor = null;
        float bestDistance = 82f;
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner == null || runner.transform == null || !runner.airborne || runner.health <= 0f)
                continue;
            float distance = Vector3.Distance(runner.transform.position, missile.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                interceptor = runner;
            }
        }
        if (interceptor == null)
            return;

        missile.enemy.fireCooldown = 0.72f;
        missile.enemy.health -= 38f;
        CreateBeam(interceptor.transform.position, missile.root.position, new Color(0.3f, 0.75f, 1f, 1f), 0.07f, 0.18f);
        TrackCombatTarget(missile.enemy, "NUCLEAR INTERCEPT", 2f);
    }

    private bool TryEngageImperialStrategicMissile(GoldenStructure structure)
    {
        if (structure == null || structure.transform == null || structure.kind != StructureKind.GepardAALauncher)
            return false;

        HostileStrategicMissile target = null;
        float bestDistance = 190f;
        for (int i = 0; i < hostileStrategicMissiles.Count; i++)
        {
            HostileStrategicMissile missile = hostileStrategicMissiles[i];
            if (missile == null || missile.root == null || missile.enemy == null || missile.enemy.health <= 0f)
                continue;
            float distance = Vector3.Distance(structure.transform.position, missile.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                target = missile;
            }
        }

        if (target == null)
            return false;
        if (structure.fireTimer > 0f || structure.ammo <= 0)
            return true;

        structure.fireTimer = 1.1f;
        structure.ammo--;
        target.enemy.health -= 62f;
        CreateWeaponTracer(structure.transform.position + Vector3.up * 1.25f, target.root.position,
            new Color(1f, 0.78f, 0.2f, 1f), 0.1f, 0.28f);
        CreateWeaponFlash(structure.transform.position + Vector3.up * 1.25f, 0.45f, UiGold);
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, structure.transform.position, 0.8f);
        return true;
    }

    private void ResolveStrategicMissileIntercept(HostileStrategicMissile missile)
    {
        if (missile == null || missile.resolved)
            return;
        missile.resolved = true;
        Vector3 position = missile.root != null ? missile.root.position : missile.destination + Vector3.up * 80f;
        CreateBattleExplosionFx(position, 18f, false, true);
        CreateFloatingCombatLabel(position + Vector3.up * 4f, "WARHEAD INTERCEPTED", new Color(0.35f, 0.8f, 1f, 1f), 1.4f);
        PlaySandRunnerSound(SandRunnerSound.HeavyImpact, position, 0.9f);
        enemies.Remove(missile.enemy);
        if (missile.root != null)
            Destroy(missile.root.gameObject);
        lastEvent = "Imperial nuclear warhead intercepted. Next launch remains scheduled.";
    }

    private void ResolveStrategicMissileImpact(HostileStrategicMissile missile)
    {
        if (missile == null || missile.resolved)
            return;
        missile.resolved = true;
        Vector3 impact = missile.destination;
        CreateBattleExplosionFx(impact, 68f, true, true);
        PlaySandRunnerSound(SandRunnerSound.NuclearExplosion, impact, 1f);
        if (missile.target != null && missile.target.settlement != null)
        {
            missile.target.settlement.health = 0f;
            missile.target.underRaid = false;
            missile.target.stage = SettlementDiplomacyStage.Neutral;
            if (missile.target.settlement.root != null)
                missile.target.settlement.root.localScale = Vector3.one * 0.72f;
            PushLivingWorldEvent(LivingWorldEventType.Raid, missile.target.settlement.displayName + " was destroyed by an imperial nuclear strike.", 18f, true);
        }
        enemies.Remove(missile.enemy);
        if (missile.root != null)
            Destroy(missile.root.gameObject);
        imperialStrikeTarget = ChooseImperialStrikeTarget();
    }

    private int CountLivingNeutralSettlements()
    {
        int count = 0;
        for (int i = 0; i < neutralSettlements.Count; i++)
            if (neutralSettlements[i] != null && neutralSettlements[i].root != null && neutralSettlements[i].health > 0f)
                count++;
        return count;
    }

    private void HandleImperialEnemyDestroyed(EnemyUnit enemy)
    {
        if (enemy == null || !enemy.isAssaultHeadquarters)
            return;

        for (int i = 0; i < imperialHeadquarters.Count; i++)
        {
            AssaultHeadquarters headquarters = imperialHeadquarters[i];
            if (headquarters == null || headquarters.enemy != enemy || headquarters.destroyed)
                continue;
            headquarters.destroyed = true;
            imperialHeadquartersDestroyed++;
            CreateBattleExplosionFx(headquarters.root.position + Vector3.up * 6f, 32f, false, true);
            ShowBanner(headquarters.kind + " DESTROYED // " + imperialHeadquartersDestroyed + "/3", 4f);
            break;
        }

        if (SandRunnersImperialRules.HasAlternativeVictory(imperialHeadquartersDestroyed, 3))
            TriggerImperialAlternativeVictory();
    }

    private void TriggerImperialAlternativeVictory()
    {
        if (imperialAlternativeVictory)
            return;
        imperialAlternativeVictory = true;
        imperialRetaliationState = ImperialRetaliationState.Victory;
        for (int i = hostileStrategicMissiles.Count - 1; i >= 0; i--)
            ResolveStrategicMissileIntercept(hostileStrategicMissiles[i]);
        hostileStrategicMissiles.Clear();
        if (imperialCrisisPanelObject != null)
            imperialCrisisPanelObject.SetActive(false);
        missionObjective = "Alternative victory. The three imperial assault headquarters are destroyed.";
        SetMissionDialogue("SEBEK: Three thrones down. EMPRESS: This desert has only postponed its sentence. ROBERT: Postponed is enough for today.", 12f);
        PlaySandRunnerSound(SandRunnerSound.VictoryFanfare, battlePyramid.position, 0.9f);
        ShowBanner("ALTERNATIVE VICTORY // IMPERIAL ASSAULT BROKEN", 5f);
        SetGameFlowState(SandRunnersGameFlowState.Victory);
    }

    private void TriggerImperialRetaliationDefeat()
    {
        if (imperialRetaliationState == ImperialRetaliationState.Defeat)
            return;
        imperialRetaliationState = ImperialRetaliationState.Defeat;
        if (imperialCrisisPanelObject != null)
            imperialCrisisPanelObject.SetActive(false);
        missionObjective = "Defeat. Every neutral settlement has been destroyed.";
        SetMissionDialogue("RED EMPRESS: Count the ashes, Sebek. SEBEK: I will count the machines that carried your order.", 10f);
        PlaySandRunnerSound(SandRunnerSound.DefeatSting, battlePyramid.position, 0.9f);
        SetGameFlowState(SandRunnersGameFlowState.Defeat);
    }

    private void UpdateImperialCrisisUi()
    {
        if (imperialCrisisPanelObject == null)
            return;
        bool visible = imperialRetaliationState != ImperialRetaliationState.Inactive &&
                       imperialRetaliationState != ImperialRetaliationState.Victory &&
                       imperialRetaliationState != ImperialRetaliationState.Defeat &&
                       !hudHidden;
        imperialCrisisPanelObject.SetActive(visible);
        if (!visible)
            return;

        float display = Mathf.Max(0f, imperialStrikeTimer);
        imperialCrisisTimerText.text = Mathf.FloorToInt(display / 60f).ToString("00") + ":" + Mathf.FloorToInt(display % 60f).ToString("00");
        imperialCrisisTargetText.text = imperialStrikeTarget != null && imperialStrikeTarget.settlement != null
            ? "NEXT TARGET // " + imperialStrikeTarget.settlement.displayName
            : "NO LIVING SETTLEMENT TARGET";
        imperialCrisisHeadquartersText.text = "ASSAULT HQ " + (3 - imperialHeadquartersDestroyed) + "/3 ACTIVE  //  INBOUND WARHEADS " + hostileStrategicMissiles.Count +
                                             "  //  LIVING SETTLEMENTS " + CountLivingNeutralSettlements();
    }
}