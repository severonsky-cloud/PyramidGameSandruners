using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum NeutralRetaliationPhase
    {
        Dormant,
        Warning,
        Advancing,
        Firing,
        Destroyed
    }

    private readonly Dictionary<EnemyUnit, NeutralSettlement> hostileNeutralTargets = new Dictionary<EnemyUnit, NeutralSettlement>();
    private float neutralCrimeScore;
    private NeutralRetaliationPhase neutralRetaliationPhase;
    private float neutralRetaliationTimer;
    private int neutralRetaliationAnnouncementStage;
    private Transform nashornRoot;
    private Transform nashornMuzzle;
    private EnemyUnit nashornEnemy;
    private float nashornFireTimer;
    private int nashornShotsFired;
    private int nashornPyramidHits;
    private Transform nashornTrophyRoot;
    private bool nashornTrophyClaimed;

    private void TryDeclareHostilityAgainstNearestSettlement()
    {
        SettlementDevelopmentState state = FindNearestSettlementDevelopment(battlePyramid.position, 220f);
        if (state == null)
        {
            lastEvent = "No neutral settlement in hostility range.";
            ShowBanner("NO NEUTRAL TARGET IN RANGE", 2f);
            return;
        }
        foreach (KeyValuePair<EnemyUnit, NeutralSettlement> pair in hostileNeutralTargets)
        {
            if (pair.Value == state.settlement)
            {
                lastEvent = state.settlement.displayName + " is already hostile.";
                return;
            }
        }

        EnemyUnit target = new EnemyUnit();
        target.transform = state.settlement.root;
        target.health = state.settlement.health;
        target.maxHealth = state.settlement.maxHealth;
        target.factionTag = "NEUTRAL_SETTLEMENT";
        enemies.Add(target);
        hostileNeutralTargets.Add(target, state.settlement);
        RegisterNeutralCrime(state.settlement, 25f, "unprovoked declaration of war");
        state.tradeTrust = 0f;
        state.playerInfluence = 0f;
        state.stage = SettlementDiplomacyStage.Neutral;
        PlaySandRunnerSound(SandRunnerSound.NeutralAlert, state.settlement.root.position, 0.92f);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "The player declared war on " + state.settlement.displayName + ".", 14f, true);
        lastEvent = "Hostility declared against " + state.settlement.displayName + ". Neutral guards are engaging; the traders will remember this.";
        ShowBanner("NEUTRAL HOSTILITY DECLARED", 3f);
    }

    private void RegisterNeutralCrime(NeutralSettlement victim, float amount, string reason)
    {
        neutralCrimeScore += amount;
        SettlementDevelopmentState state;
        if (victim != null && settlementDevelopment.TryGetValue(victim, out state))
        {
            state.crimeScore += amount;
            state.tradeTrust = Mathf.Max(0f, state.tradeTrust - amount * 0.7f);
            state.playerInfluence = Mathf.Max(0f, state.playerInfluence - amount);
        }

        if (neutralCrimeScore >= 100f && neutralRetaliationPhase == NeutralRetaliationPhase.Dormant)
        {
            neutralRetaliationPhase = NeutralRetaliationPhase.Warning;
            neutralRetaliationTimer = 45f;
            neutralRetaliationAnnouncementStage = 0;
            PlaySandRunnerSound(SandRunnerSound.NeutralAlert, battlePyramid.position, 1f);
            PushLivingWorldEvent(LivingWorldEventType.Raid, "Black Elementals are preparing retaliation for " + reason + ".", 45f, true);
            lastEvent = "BLACK ELEMENTAL WARNING: punishment column assembling. Estimated arrival 45 seconds.";
            ShowBanner("NEUTRAL RETALIATION // 45 SECONDS", 3.4f);
        }
    }

    private void UpdateNeutralRetaliation(float dt)
    {
        if (neutralRetaliationPhase != NeutralRetaliationPhase.Warning)
            return;

        neutralRetaliationTimer -= dt;
        if (neutralRetaliationAnnouncementStage == 0 && neutralRetaliationTimer <= 30f)
        {
            neutralRetaliationAnnouncementStage = 1;
            lastEvent = "Black Elemental column formed: Nashorn, escort armor, repair and anti-air vehicles.";
            ShowBanner("NASHORN COLUMN FORMED", 2.6f);
            PlaySandRunnerSound(SandRunnerSound.GustavWarning, battlePyramid.position, 0.8f);
        }
        if (neutralRetaliationAnnouncementStage == 1 && neutralRetaliationTimer <= 15f)
        {
            neutralRetaliationAnnouncementStage = 2;
            lastEvent = "Nashorn is entering the map. Its antimatter shells can destroy the pyramid in three hits.";
            ShowBanner("ANTIMATTER ARTILLERY INBOUND", 3f);
        }
        if (neutralRetaliationTimer <= 0f)
            SpawnNashornRetaliationColumn();
    }

    private void SpawnNashornRetaliationColumn()
    {
        if (nashornRoot != null)
            return;

        Vector3 spawn = FindNashornSpawnPosition();
        nashornRoot = new GameObject("Black_Elemental_Nashorn_Battery").transform;
        if (livingWorldRoot != null)
            nashornRoot.SetParent(livingWorldRoot, false);
        nashornRoot.position = spawn;
        Vector3 forward = battlePyramid.position - spawn;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.1f)
            nashornRoot.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        BuildNashornVisual(nashornRoot);

        nashornEnemy = new EnemyUnit();
        nashornEnemy.transform = nashornRoot;
        nashornEnemy.health = 1150f;
        nashornEnemy.maxHealth = 1150f;
        nashornEnemy.factionTag = "NASHORN_RETALIATION";
        enemies.Add(nashornEnemy);
        neutralRetaliationPhase = NeutralRetaliationPhase.Advancing;
        nashornFireTimer = 5f;
        nashornShotsFired = 0;
        nashornPyramidHits = 0;
        lastEvent = "Nashorn punishment battery has entered the desert. Destroy it before the third shot.";
        ShowBanner("NASHORN BATTERY ON MAP", 3.2f);
        PlaySandRunnerSound(SandRunnerSound.GustavWarning, spawn, 1f);
    }

    private Vector3 FindNashornSpawnPosition()
    {
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement != null && settlement.root != null && settlement.kind == NeutralFactionKind.BlackElementals && settlement.health > 0f)
                return settlement.root.position + settlement.root.right * 42f;
        }
        Vector3 away = battlePyramid.position.sqrMagnitude > 1f ? -battlePyramid.position.normalized : Vector3.forward;
        Vector3 spawn = away * (mapHalfSize - 70f);
        spawn.y = GetPlayableGroundHeight(spawn) + 0.4f;
        return spawn;
    }

    private void BuildNashornVisual(Transform root)
    {
        Material black = elementalBlackMaterial != null ? elementalBlackMaterial : siegeUnitMaterial;
        Material glow = traderBlueMaterial != null ? traderBlueMaterial : commandMaterial;
        CreateBox(root, "Nashorn_Tracked_Chassis", new Vector3(0f, 0.8f, 0f), Quaternion.identity, new Vector3(5.8f, 1.2f, 8.5f), black, true);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBox(root, "Nashorn_Track_" + side, new Vector3(side * 3.1f, 0.5f, 0f), Quaternion.identity, new Vector3(0.9f, 0.9f, 8.8f), black, true);
            for (int wheel = -2; wheel <= 2; wheel++)
                CreateCylinder(root, "Nashorn_Wheel_" + side + "_" + wheel, new Vector3(side * 3.25f, 0.48f, wheel * 1.55f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.72f, 0.3f, 0.72f), glow, true);
        }
        CreateBox(root, "Nashorn_Fighting_Compartment", new Vector3(0f, 2.4f, -0.5f), Quaternion.identity, new Vector3(4.7f, 2.6f, 4.8f), black, true);
        CreateBox(root, "Nashorn_Antimatter_Breech", new Vector3(0f, 3.1f, 1.2f), Quaternion.identity, new Vector3(1.7f, 1.5f, 2.5f), glow, true);
        CreateCylinder(root, "Nashorn_Antimatter_Barrel", new Vector3(0f, 3.55f, 6.6f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.48f, 5.5f, 0.48f), black, true);
        nashornMuzzle = new GameObject("Nashorn_Muzzle").transform;
        nashornMuzzle.SetParent(root, false);
        nashornMuzzle.localPosition = new Vector3(0f, 3.55f, 12f);
        CreatePointLight(root, "Nashorn_Antimatter_Core", new Vector3(0f, 3.35f, 1.4f), new Color(0.16f, 0.38f, 1f, 1f), 1.8f, 28f);

        CreateNashornEscortVisual(root, "Escort_Left", new Vector3(-8f, 0f, -2f), false);
        CreateNashornEscortVisual(root, "Escort_Right", new Vector3(8f, 0f, -2f), false);
        CreateNashornEscortVisual(root, "Repair_Vehicle", new Vector3(-6f, 0f, -10f), true);
        CreateNashornEscortVisual(root, "Anti_Air_Vehicle", new Vector3(6f, 0f, -10f), false);
        for (int i = 0; i < 5; i++)
        {
            Transform elemental = new GameObject("Black_Elemental_Guard_" + i).transform;
            elemental.SetParent(root, false);
            elemental.localPosition = new Vector3(-5f + i * 2.5f, 0f, -7f - (i % 2) * 2f);
            CreateBox(elemental, "Body", new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(0.75f, 1.7f, 0.75f), black, true);
            CreatePointLight(elemental, "Eyes", new Vector3(0f, 1.8f, 0.4f), new Color(0.2f, 0.55f, 1f, 1f), 0.25f, 5f);
        }
    }

    private void CreateNashornEscortVisual(Transform parent, string name, Vector3 localPosition, bool repair)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = localPosition;
        Material black = elementalBlackMaterial != null ? elementalBlackMaterial : siegeUnitMaterial;
        CreateBox(root, "Escort_Chassis", new Vector3(0f, 0.7f, 0f), Quaternion.identity, new Vector3(3.5f, 1.1f, 5.2f), black, true);
        CreateBox(root, repair ? "Repair_Crane" : "Escort_Turret", new Vector3(0f, 1.8f, 0f), Quaternion.identity, repair ? new Vector3(0.35f, 2.4f, 0.35f) : new Vector3(1.3f, 0.8f, 1.4f), traderBlueMaterial, true);
        if (!repair)
            CreateBox(root, "Escort_Cannon", new Vector3(0f, 2f, 1.6f), Quaternion.identity, new Vector3(0.22f, 0.22f, 2f), traderBlueMaterial);
    }

    private void UpdateNashornRetaliationEnemy(EnemyUnit enemy, float dt)
    {
        if (enemy == null || enemy.transform == null || battlePyramid == null)
            return;
        Vector3 toPyramid = battlePyramid.position - enemy.transform.position;
        toPyramid.y = 0f;
        float distance = toPyramid.magnitude;
        if (distance > 315f)
        {
            neutralRetaliationPhase = NeutralRetaliationPhase.Advancing;
            enemy.transform.position += toPyramid.normalized * 3.1f * dt;
            RotateToward(enemy.transform, toPyramid, 42f * dt);
            Vector3 position = enemy.transform.position;
            position.y = GetPlayableGroundHeight(position) + 0.45f;
            enemy.transform.position = position;
            return;
        }

        neutralRetaliationPhase = NeutralRetaliationPhase.Firing;
        RotateToward(enemy.transform, toPyramid, 22f * dt);
        nashornFireTimer -= dt;
        if (nashornFireTimer <= 0f && nashornShotsFired < 3)
        {
            nashornFireTimer = 12f;
            FireNashornAntimatterShell();
        }
    }

    private void FireNashornAntimatterShell()
    {
        if (nashornMuzzle == null || battlePyramid == null)
            return;
        nashornShotsFired++;
        Vector3 target = battlePyramid.position + battlePyramid.forward * Mathf.Lerp(2f, -2f, nashornShotsFired / 3f);
        CreateHostileShell("Nashorn_Antimatter_Shell_" + nashornShotsFired, nashornMuzzle.position, target, 420f, 24f, true, true, 3.4f, 88f, true);
        lastEvent = "Nashorn fired antimatter shell " + nashornShotsFired + "/3. Intercept the shell or destroy the battery.";
        ShowBanner("NASHORN SHOT " + nashornShotsFired + "/3", 2.8f);
    }

    private void ResolveNashornPyramidHit(HostileShellVisual shell)
    {
        nashornPyramidHits++;
        beamCharge = 0f;
        if (nashornPyramidHits == 1)
        {
            cruiseMissiles = Mathf.Max(0, cruiseMissiles - 2);
            lastEvent = "Nashorn antimatter strike ruptured internal systems. Missile racks damaged.";
            ShowBanner("INTERNAL SYSTEMS RUPTURED", 3f);
        }
        else if (nashornPyramidHits == 2)
        {
            nuclearMissiles = Mathf.Max(0, nuclearMissiles - 1);
            hangarOpen = false;
            pyramidHull -= 220f;
            lastEvent = "Second Nashorn hit: critical internal damage. Hangar and weapon systems are failing.";
            ShowBanner("PYRAMID CRITICAL // THIRD HIT FATAL", 3.6f);
        }
        else
        {
            pyramidHull = 0f;
            lastEvent = "Third Nashorn hit triggered an antimatter detonation inside the pyramid.";
            ShowBanner("INTERNAL ANTIMATTER DETONATION", 4f);
        }
        pyramidHull = Mathf.Clamp(pyramidHull, 0f, pyramidMaxHull);
    }

    private bool TryInterceptNashornShell(HostileShellVisual shell)
    {
        if (shell == null || shell.transform == null)
            return false;
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileVisual projectile = projectiles[i];
            if (projectile == null || projectile.transform == null)
                continue;
            if (Vector3.Distance(projectile.transform.position, shell.transform.position) > 4.2f)
                continue;
            Vector3 position = shell.transform.position;
            Destroy(projectile.transform.gameObject);
            projectiles.RemoveAt(i);
            DestroyInterceptedNashornShell(shell, position);
            return true;
        }
        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            MissileVisual missile = missiles[i];
            if (missile == null || missile.transform == null)
                continue;
            if (Vector3.Distance(missile.transform.position, shell.transform.position) > 5.2f)
                continue;
            Vector3 position = shell.transform.position;
            Destroy(missile.transform.gameObject);
            missiles.RemoveAt(i);
            DestroyInterceptedNashornShell(shell, position);
            return true;
        }
        return false;
    }

    private void DestroyInterceptedNashornShell(HostileShellVisual shell, Vector3 position)
    {
        if (shell.warningRing != null)
            Destroy(shell.warningRing.gameObject);
        if (shell.transform != null)
            Destroy(shell.transform.gameObject);
        CreateHostileImpactVisual(position, 6f, false);
        PlaySandRunnerSound(SandRunnerSound.LargeExplosion, position, 0.78f);
        lastEvent = "Nashorn antimatter shell intercepted in flight.";
        ShowBanner("ANTIMATTER SHELL INTERCEPTED", 2.6f);
    }

    private void UpdateHostileNeutralSettlementEnemy(EnemyUnit enemy, float dt)
    {
        NeutralSettlement settlement;
        if (enemy == null || !hostileNeutralTargets.TryGetValue(enemy, out settlement) || settlement == null)
            return;
        settlement.health = Mathf.Clamp(enemy.health, 0f, settlement.maxHealth);
    }

    private void UpdateHostileSettlementDefense(float dt)
    {
        foreach (KeyValuePair<EnemyUnit, NeutralSettlement> pair in hostileNeutralTargets)
        {
            EnemyUnit target = pair.Key;
            NeutralSettlement settlement = pair.Value;
            if (target == null || target.health <= 0f || settlement == null || settlement.root == null)
                continue;
            for (int i = 0; i < settlement.guards.Count; i++)
            {
                Transform guard = settlement.guards[i];
                if (guard == null)
                    continue;
                RunnerUnit runner = FindNearestRunner(guard.position, 45f);
                if (runner != null && runner.transform != null)
                {
                    runner.health -= (11f + i) * dt;
                    CreateBeam(guard.position + Vector3.up * 1.3f, runner.transform.position + Vector3.up * 0.6f, new Color(0.2f, 0.55f, 1f, 1f), 0.035f, 0.08f);
                }
                else if (FlatDistance(guard.position, battlePyramid.position) <= 55f)
                {
                    pyramidHull -= (4f + i * 0.4f) * dt;
                    CreateBeam(guard.position + Vector3.up * 1.3f, battlePyramid.position + Vector3.up * 2f, new Color(0.2f, 0.55f, 1f, 1f), 0.035f, 0.08f);
                }
            }
        }
    }

    private void HandleLivingWorldEnemyDestroyed(EnemyUnit enemy)
    {
        if (enemy == null)
            return;
        NeutralSettlement settlement;
        if (hostileNeutralTargets.TryGetValue(enemy, out settlement))
        {
            Vector3 position = settlement != null && settlement.root != null ? settlement.root.position : Vector3.zero;
            if (settlement != null)
            {
                settlement.health = 0f;
                RegisterNeutralCrime(settlement, 100f, "destruction of " + settlement.displayName);
            }
            hostileNeutralTargets.Remove(enemy);
            CreateHostileImpactVisual(position, 18f, true);
            lastEvent = "A neutral settlement was destroyed. Black Elemental retaliation is now inevitable.";
            ShowBanner("NEUTRAL SETTLEMENT DESTROYED", 3.4f);
            return;
        }
        if (enemy == nashornEnemy)
            HandleNashornDestroyed();
    }

    private void HandleNashornDestroyed()
    {
        Vector3 position = nashornRoot != null ? nashornRoot.position : battlePyramid.position;
        neutralRetaliationPhase = NeutralRetaliationPhase.Destroyed;
        nashornEnemy = null;
        nashornMuzzle = null;
        nashornRoot = null;
        SpawnNashornTrophyZone(position);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "Nashorn battery destroyed. Antimatter salvage remains.", 20f, true);
        lastEvent = "Nashorn destroyed. A dangerous antimatter trophy zone remains; approach and press L to loot it.";
        ShowBanner("NASHORN DESTROYED // TROPHIES AVAILABLE", 3.2f);
    }

    private void SpawnNashornTrophyZone(Vector3 position)
    {
        nashornTrophyRoot = new GameObject("Nashorn_Trophy_Zone").transform;
        if (livingWorldRoot != null)
            nashornTrophyRoot.SetParent(livingWorldRoot, false);
        position.y = GetPlayableGroundHeight(position) + 0.2f;
        nashornTrophyRoot.position = position;
        CreateBox(nashornTrophyRoot, "Destroyed_Nashorn_Hull", new Vector3(0f, 0.8f, 0f), Quaternion.Euler(12f, 24f, 8f), new Vector3(5.5f, 1.2f, 7.4f), elementalBlackMaterial, true);
        CreateCylinder(nashornTrophyRoot, "Antimatter_Chamber", new Vector3(2.7f, 1.2f, -1.4f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.2f, 1.8f, 1.2f), traderBlueMaterial, true);
        CreateBox(nashornTrophyRoot, "Ammunition_Crate", new Vector3(-3f, 0.65f, 1.5f), Quaternion.identity, new Vector3(2.4f, 1.1f, 2.2f), elementalBlackMaterial, true);
        CreatePointLight(nashornTrophyRoot, "Residual_Antimatter_Field", new Vector3(0f, 2.5f, 0f), new Color(0.14f, 0.32f, 1f, 1f), 2.2f, 32f);
        nashornTrophyClaimed = false;
    }

    private void TryLootNashornWreck()
    {
        if (nashornTrophyRoot == null || nashornTrophyClaimed)
            return;
        if (FlatDistance(battlePyramid.position, nashornTrophyRoot.position) > 45f)
        {
            lastEvent = "Move within 45 metres of the Nashorn wreck to loot it.";
            return;
        }
        nashornTrophyClaimed = true;
        gold += 260f;
        sand += 140f;
        wind += 120f;
        neutralCrimeScore += 15f;
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            state.tradeTrust = Mathf.Max(0f, state.tradeTrust - 15f);
            state.playerInfluence = Mathf.Max(0f, state.playerInfluence - 10f);
            if (state.stage == SettlementDiplomacyStage.AlliedSettlement)
                state.stage = SettlementDiplomacyStage.ProtectedSettlement;
        }
        PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, nashornTrophyRoot.position, 0.92f);
        lastEvent = "Nashorn wreck looted: +260 gold, +140 sand, +120 wind. Neutral trust fell.";
        ShowBanner("ANTIMATTER TROPHIES RECOVERED", 3f);
    }
}
