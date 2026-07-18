using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum DamageSubsystem
    {
        Mobility,
        Weapons,
        Production,
        Defense
    }

    private enum DamageState
    {
        Operational,
        Damaged,
        Disabled,
        Destroyed
    }

    private enum DamageHitType
    {
        Generic,
        Kinetic,
        Energy,
        Siege,
        Missile
    }

    private sealed class PyramidDamageState
    {
        public float mobility = 1f;
        public float weapons = 1f;
        public float production = 1f;
        public float defense = 1f;
        public float lastHull;
        public bool initialized;
    }

    private sealed class RepairableDamageTarget
    {
        public Transform transform;
        public GoldenStructure structure;
        public DamageSubsystem subsystem;
        public bool pyramid;
    }

    private readonly PyramidDamageState pyramidDamageState = new PyramidDamageState();
    private readonly Dictionary<GoldenStructure, PyramidDamageState> structureDamageStates = new Dictionary<GoldenStructure, PyramidDamageState>();
    private readonly Dictionary<GoldenBuilderAsset, RepairableDamageTarget> builderRepairTargets = new Dictionary<GoldenBuilderAsset, RepairableDamageTarget>();
    private bool damageSystemsInitialized;
    private float damageMobilityMultiplier = 1f;
    private float damageWeaponCooldownMultiplier = 1f;
    private float damageProductionMultiplier = 1f;
    private float damageDefenseMultiplier = 1f;
    private float damageFeedbackTimer;

    private void InitializeDamageSystems()
    {
        damageSystemsInitialized = false;
        pyramidDamageState.initialized = false;
        builderRepairTargets.Clear();
    }

    private void UpdateDamageSystems(float dt)
    {
        UpdateDamageBenchmark(dt);
        UpdateDamagePresentation(dt);
        if (!damageSystemsInitialized)
        {
            damageSystemsInitialized = true;
            pyramidDamageState.lastHull = pyramidHull;
            pyramidDamageState.initialized = true;
        }

        float hullDelta = pyramidHull - pyramidDamageState.lastHull;
        if (hullDelta < -0.01f)
            ApplyPyramidSubsystemDamage(-hullDelta, DamageHitType.Generic);
        pyramidDamageState.lastHull = pyramidHull;

        damageMobilityMultiplier = GetSubsystemMultiplier(pyramidDamageState.mobility, 0.35f, 0.7f);
        damageWeaponCooldownMultiplier = GetSubsystemMultiplier(pyramidDamageState.weapons, 0.45f, 0.78f);
        damageProductionMultiplier = GetSubsystemMultiplier(pyramidDamageState.production, 0.4f, 0.72f);
        damageDefenseMultiplier = GetSubsystemMultiplier(pyramidDamageState.defense, 0.35f, 0.7f);

        UpdateStructureDamageStates();
        UpdateBuilderRepairs(dt);
        damageFeedbackTimer -= dt;
    }

    private float GetSubsystemMultiplier(float health01, float disabledAt, float damagedAt)
    {
        if (health01 <= disabledAt)
            return 0f;
        if (health01 <= damagedAt)
            return Mathf.Lerp(0.45f, 0.7f, Mathf.InverseLerp(disabledAt, damagedAt, health01));
        return 1f;
    }

    private void ApplyPyramidSubsystemDamage(float amount, DamageHitType hitType)
    {
        if (amount <= 0f)
            return;

        float mobilityShare = hitType == DamageHitType.Kinetic ? 0.34f : 0.22f;
        float weaponShare = hitType == DamageHitType.Energy ? 0.34f : 0.24f;
        float productionShare = hitType == DamageHitType.Siege ? 0.34f : 0.22f;
        float defenseShare = Mathf.Max(0.1f, 1f - mobilityShare - weaponShare - productionShare);

        pyramidDamageState.mobility = Mathf.Clamp01(pyramidDamageState.mobility - amount / Mathf.Max(1f, pyramidMaxHull) * mobilityShare);
        pyramidDamageState.weapons = Mathf.Clamp01(pyramidDamageState.weapons - amount / Mathf.Max(1f, pyramidMaxHull) * weaponShare);
        pyramidDamageState.production = Mathf.Clamp01(pyramidDamageState.production - amount / Mathf.Max(1f, pyramidMaxHull) * productionShare);
        pyramidDamageState.defense = Mathf.Clamp01(pyramidDamageState.defense - amount / Mathf.Max(1f, pyramidMaxHull) * defenseShare);

        if (damageFeedbackTimer <= 0f)
        {
            damageFeedbackTimer = 1.2f;
            lastEvent = GetPyramidDamageStatus();
            ShowBanner(lastEvent, 2.2f);
            PlaySandRunnerSound(SandRunnerSound.PyramidDamage, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.55f);
        }
    }

    private string GetPyramidDamageStatus()
    {
        DamageSubsystem subsystem = GetLowestPyramidSubsystem();
        return "PYRAMID " + subsystem.ToString().ToUpperInvariant() + " DAMAGED";
    }

    private DamageSubsystem GetLowestPyramidSubsystem()
    {
        float lowest = pyramidDamageState.mobility;
        DamageSubsystem result = DamageSubsystem.Mobility;
        if (pyramidDamageState.weapons < lowest) { lowest = pyramidDamageState.weapons; result = DamageSubsystem.Weapons; }
        if (pyramidDamageState.production < lowest) { lowest = pyramidDamageState.production; result = DamageSubsystem.Production; }
        if (pyramidDamageState.defense < lowest) result = DamageSubsystem.Defense;
        return result;
    }

    private DamageState GetPyramidDamageState()
    {
        if (pyramidHull <= 0f)
            return DamageState.Destroyed;
        float lowest = Mathf.Min(Mathf.Min(pyramidDamageState.mobility, pyramidDamageState.weapons), Mathf.Min(pyramidDamageState.production, pyramidDamageState.defense));
        if (lowest <= 0.35f)
            return DamageState.Disabled;
        if (lowest < 0.7f || pyramidHull < pyramidMaxHull * 0.7f)
            return DamageState.Damaged;
        return DamageState.Operational;
    }

    private float GetPyramidMoveDamageMultiplier()
    {
        return Mathf.Lerp(0.55f, 1f, damageMobilityMultiplier);
    }

    private float GetPyramidWeaponDamageMultiplier()
    {
        return Mathf.Lerp(0.45f, 1f, damageDefenseMultiplier);
    }

    private void UpdateStructureDamageStates()
    {
        for (int i = goldenStructures.Count - 1; i >= 0; i--)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null)
                continue;
            if (!structureDamageStates.ContainsKey(structure))
                structureDamageStates.Add(structure, new PyramidDamageState { lastHull = structure.health, initialized = true });
            PyramidDamageState state = structureDamageStates[structure];
            if (state.lastHull - structure.health > 0.01f)
            {
                float hit = state.lastHull - structure.health;
                state.defense = Mathf.Clamp01(state.defense - hit / Mathf.Max(1f, structure.maxHealth) * 0.4f);
                state.weapons = Mathf.Clamp01(state.weapons - hit / Mathf.Max(1f, structure.maxHealth) * 0.6f);
            }
            state.lastHull = structure.health;
            if (structure.health <= 0f)
                continue;
            if (state.weapons <= 0.35f || state.defense <= 0.35f)
                lastEvent = structure.displayName + " DISABLED — repair required.";
        }
    }

    private bool IsGoldenStructureDisabled(GoldenStructure structure)
    {
        if (structure == null)
            return true;
        if (!structureDamageStates.TryGetValue(structure, out PyramidDamageState state))
            return false;
        return state.weapons <= 0.35f || state.defense <= 0.35f;
    }

    private bool TryIssueBuilderRepairOrderAtHit(RaycastHit hit)
    {
        if (selectedSquads.Count == 0 || hit.transform == null)
            return false;
        RepairableDamageTarget target = FindRepairableDamageTarget(hit.transform);
        if (target == null)
            return false;

        bool assigned = false;
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null || squad.builder == null || squad.builder.transform == null)
                continue;
            builderRepairTargets[squad.builder] = target;
            assigned = true;
        }
        if (assigned)
            lastEvent = "REPAIR ORDER: " + (target.pyramid ? "Battle Pyramid" : target.structure.displayName);
        return assigned;
    }

    private RepairableDamageTarget FindRepairableDamageTarget(Transform hit)
    {
        if (battlePyramid != null && IsTransformInHierarchy(hit, battlePyramid) && GetPyramidDamageState() != DamageState.Operational)
            return new RepairableDamageTarget { transform = battlePyramid, pyramid = true, subsystem = GetLowestPyramidSubsystem() };

        GoldenStructure structure = FindStructureByHit(hit);
        if (structure != null && structure.health < structure.maxHealth)
            return new RepairableDamageTarget { transform = structure.transform, structure = structure, subsystem = DamageSubsystem.Defense };

        return null;
    }

    private bool UpdateGoldenBuilderRepair(GoldenBuilderAsset builder, float dt)
    {
        if (builder == null || builder.transform == null || !builderRepairTargets.TryGetValue(builder, out RepairableDamageTarget target) || target == null || target.transform == null)
        {
            if (builder != null)
                builderRepairTargets.Remove(builder);
            return false;
        }

        Vector3 toTarget = target.transform.position - builder.transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > 7f)
        {
            builder.transform.position += toTarget.normalized * 6.6f * dt;
            RotateFlatToward(builder.transform, toTarget, 220f * dt);
            Vector3 p = builder.transform.position;
            p.y = GetPlayableGroundHeight(p) + 0.34f;
            builder.transform.position = p;
            return true;
        }

        if (target.pyramid)
        {
            pyramidHull = Mathf.Min(pyramidMaxHull, pyramidHull + 14f * dt);
            pyramidDamageState.mobility = Mathf.MoveTowards(pyramidDamageState.mobility, 1f, dt * 0.018f);
            pyramidDamageState.weapons = Mathf.MoveTowards(pyramidDamageState.weapons, 1f, dt * 0.014f);
            pyramidDamageState.production = Mathf.MoveTowards(pyramidDamageState.production, 1f, dt * 0.012f);
            pyramidDamageState.defense = Mathf.MoveTowards(pyramidDamageState.defense, 1f, dt * 0.016f);
        }
        else
        {
            target.structure.health = Mathf.Min(target.structure.maxHealth, target.structure.health + 10f * dt);
            if (structureDamageStates.TryGetValue(target.structure, out PyramidDamageState state))
            {
                state.weapons = Mathf.MoveTowards(state.weapons, 1f, dt * 0.04f);
                state.defense = Mathf.MoveTowards(state.defense, 1f, dt * 0.04f);
            }
        }

        if ((target.pyramid && GetPyramidDamageState() == DamageState.Operational) || (!target.pyramid && target.structure.health >= target.structure.maxHealth - 0.1f))
        {
            builderRepairTargets.Remove(builder);
            lastEvent = "REPAIR COMPLETE";
        }
        return true;
    }

    private void ApplyPyramidRepair(float amount)
    {
        if (amount <= 0f)
            return;
        pyramidDamageState.mobility = Mathf.MoveTowards(pyramidDamageState.mobility, 1f, amount / Mathf.Max(1f, pyramidMaxHull));
        pyramidDamageState.weapons = Mathf.MoveTowards(pyramidDamageState.weapons, 1f, amount / Mathf.Max(1f, pyramidMaxHull));
        pyramidDamageState.production = Mathf.MoveTowards(pyramidDamageState.production, 1f, amount / Mathf.Max(1f, pyramidMaxHull));
        pyramidDamageState.defense = Mathf.MoveTowards(pyramidDamageState.defense, 1f, amount / Mathf.Max(1f, pyramidMaxHull));
    }

    private void ApplyStructureRepair(GoldenStructure structure, float amount)
    {
        if (structure == null || amount <= 0f)
            return;
        if (structureDamageStates.TryGetValue(structure, out PyramidDamageState state))
        {
            float amount01 = amount / Mathf.Max(1f, structure.maxHealth);
            state.weapons = Mathf.MoveTowards(state.weapons, 1f, amount01);
            state.defense = Mathf.MoveTowards(state.defense, 1f, amount01);
        }
    }

    private void UpdateBuilderRepairs(float dt)
    {
        // Orders are advanced from UpdateGoldenBuilders so normal builder logic cannot fight the repair route.
    }
}