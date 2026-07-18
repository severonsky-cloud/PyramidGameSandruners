using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

internal static class SandRunnersWarRules
{
    internal static int[] ClusterThreatSamples(Vector2[] points, float radius)
    {
        if (points == null)
            return new int[0];

        int[] groups = new int[points.Length];
        for (int i = 0; i < groups.Length; i++)
            groups[i] = -1;

        int group = 0;
        float radiusSquared = radius * radius;
        for (int seed = 0; seed < points.Length; seed++)
        {
            if (groups[seed] >= 0)
                continue;

            groups[seed] = group;
            bool expanded;
            do
            {
                expanded = false;
                for (int i = 0; i < points.Length; i++)
                {
                    if (groups[i] >= 0)
                        continue;
                    for (int j = 0; j < points.Length; j++)
                    {
                        if (groups[j] == group && (points[i] - points[j]).sqrMagnitude <= radiusSquared)
                        {
                            groups[i] = group;
                            expanded = true;
                            break;
                        }
                    }
                }
            }
            while (expanded);
            group++;
        }
        return groups;
    }

    internal static bool IsEarlyFortressDefeat(int currentStage, int destroyStage)
    {
        return currentStage < destroyStage;
    }

    internal static bool RequiresStuckRecovery(float travelledDistance, float requiredProgress, float elapsed, float timeout)
    {
        return elapsed >= timeout && travelledDistance < requiredProgress;
    }
}

public partial class SandRunnersPrototype
{
    private enum MandarinkaAssaultStep
    {
        Recon,
        Rally,
        Suppress,
        Breach,
        Exploit
    }

    private sealed class MandarinkaThreatCluster
    {
        public readonly List<GoldenStructure> structures = new List<GoldenStructure>();
        public Vector3 center;
        public float score;
        public GoldenStructure primary;
    }

    private sealed class StrategicPathState
    {
        public readonly NavMeshPath path = new NavMeshPath();
        public Vector3 destination;
        public int cornerIndex;
        public float repathTimer;
    }

    private sealed class LargeMovementState
    {
        public Vector3 samplePosition;
        public Vector3 lastSafePosition;
        public float sampleTimer;
        public int recoveryAttempts;
    }

    private readonly List<MandarinkaThreatCluster> mandarinkaThreatClusters = new List<MandarinkaThreatCluster>();
    private readonly Dictionary<Transform, StrategicPathState> strategicPaths = new Dictionary<Transform, StrategicPathState>();
    private readonly Dictionary<Transform, LargeMovementState> largeMovementStates = new Dictionary<Transform, LargeMovementState>();

    private NavMeshData runtimeNavigationData;
    private NavMeshDataInstance runtimeNavigationInstance;
    private bool runtimeNavigationReady;
    private float mandarinkaThreatScanTimer;
    private float mandarinkaPlanTimer;
    private float mandarinkaAssaultStepTimer;
    private MandarinkaAssaultStep mandarinkaAssaultStep;
    private MandarinkaThreatCluster mandarinkaPriorityCluster;
    private GoldenStructure mandarinkaPriorityStructure;

    private void InitializeRuntimeNavigation()
    {
        if (runtimeNavigationReady)
            return;

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        if (settings.agentTypeID < 0)
        {
            Debug.LogWarning("SandRunners navigation: no default NavMesh build settings.");
            return;
        }

        settings.agentRadius = 1.1f;
        settings.agentHeight = 2.2f;
        settings.agentClimb = 0.8f;
        settings.agentSlope = 34f;
        settings.minRegionArea = 3f;

        Bounds bounds = new Bounds(Vector3.zero, new Vector3(mapHalfSize * 2f, 180f, mapHalfSize * 2f));
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
        NavMeshBuilder.CollectSources(bounds, 1, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
        runtimeNavigationData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (runtimeNavigationData == null)
        {
            Debug.LogWarning("SandRunners navigation: runtime bake returned no data.");
            return;
        }

        runtimeNavigationInstance = NavMesh.AddNavMeshData(runtimeNavigationData);
        runtimeNavigationReady = runtimeNavigationInstance.valid;
    }

    private void ShutdownRuntimeNavigation()
    {
        if (runtimeNavigationInstance.valid)
            runtimeNavigationInstance.Remove();
        runtimeNavigationReady = false;
        strategicPaths.Clear();
        largeMovementStates.Clear();
    }

    private void UpdateMandarinkaBattleDirector(float dt)
    {
        mandarinkaThreatScanTimer -= dt;
        if (mandarinkaThreatScanTimer <= 0f)
        {
            mandarinkaThreatScanTimer = Mathf.Max(0.1f, balanceProfile.aiThreatScanInterval);
            RebuildMandarinkaThreatClusters();
        }

        mandarinkaPlanTimer -= dt;
        mandarinkaAssaultStepTimer += dt;
        if (mandarinkaPlanTimer <= 0f)
        {
            mandarinkaPlanTimer = Mathf.Max(0.5f, balanceProfile.aiPlanInterval);
            ChooseMandarinkaBattlePlan();
        }
    }

    private void RebuildMandarinkaThreatClusters()
    {
        mandarinkaThreatClusters.Clear();
        float radius = Mathf.Max(12f, balanceProfile.aiThreatClusterRadius);
        float radiusSquared = radius * radius;

        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null || structure.health <= 0f)
                continue;

            MandarinkaThreatCluster cluster = null;
            for (int j = 0; j < mandarinkaThreatClusters.Count; j++)
            {
                Vector3 delta = structure.transform.position - mandarinkaThreatClusters[j].center;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radiusSquared)
                {
                    cluster = mandarinkaThreatClusters[j];
                    break;
                }
            }

            if (cluster == null)
            {
                cluster = new MandarinkaThreatCluster();
                mandarinkaThreatClusters.Add(cluster);
            }

            cluster.structures.Add(structure);
            Vector3 total = Vector3.zero;
            cluster.score = 0f;
            cluster.primary = null;
            float bestWeight = float.MinValue;
            for (int memberIndex = 0; memberIndex < cluster.structures.Count; memberIndex++)
            {
                GoldenStructure member = cluster.structures[memberIndex];
                total += member.transform.position;
                float weight = GetMandarinkaStructureThreat(member);
                cluster.score += weight;
                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    cluster.primary = member;
                }
            }
            cluster.center = total / Mathf.Max(1, cluster.structures.Count);
        }

        mandarinkaPriorityCluster = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < mandarinkaThreatClusters.Count; i++)
        {
            MandarinkaThreatCluster cluster = mandarinkaThreatClusters[i];
            float distancePenalty = battlePyramid != null ? FlatDistance(cluster.center, battlePyramid.position) * 0.0025f : 0f;
            float score = cluster.score - distancePenalty;
            if (score > bestScore)
            {
                bestScore = score;
                mandarinkaPriorityCluster = cluster;
            }
        }

        mandarinkaPriorityStructure = mandarinkaPriorityCluster != null ? mandarinkaPriorityCluster.primary : null;
    }

    private float GetMandarinkaStructureThreat(GoldenStructure structure)
    {
        if (structure == null)
            return 0f;
        switch (structure.kind)
        {
            case StructureKind.MirrorBeamTurret: return 4.2f;
            case StructureKind.GepardAALauncher: return 3.2f;
            case StructureKind.AnubisStrikeLauncher: return 3f;
            case StructureKind.Twin30mmTurret: return 2.4f;
            case StructureKind.Aerodrome: return 2.2f;
            case StructureKind.CruiseMissileSilo: return 2.8f;
            default: return 1.2f;
        }
    }

    private void ChooseMandarinkaBattlePlan()
    {
        int ground = CountMandarinkaRole(MandarinkaRole.GroundCrawler);
        int air = CountMandarinkaRole(MandarinkaRole.AirJunk);
        int artillery = CountMandarinkaRole(MandarinkaRole.Gustav);
        MandarinkaAssaultStep next;

        if (mandarinkaPriorityCluster == null)
            next = MandarinkaAssaultStep.Recon;
        else if (ground < 3 && mandarinkaAssaultStepTimer < balanceProfile.aiRallySeconds * 2f)
            next = MandarinkaAssaultStep.Rally;
        else if (artillery > 0 || mandarinkaFortressPhase > 0)
            next = MandarinkaAssaultStep.Suppress;
        else if (ground >= 3)
            next = MandarinkaAssaultStep.Breach;
        else if (air > 0)
            next = MandarinkaAssaultStep.Exploit;
        else
            next = MandarinkaAssaultStep.Rally;

        if (next != mandarinkaAssaultStep)
        {
            mandarinkaAssaultStep = next;
            mandarinkaAssaultStepTimer = 0f;
        }

        if (mandarinkaAssaultStep == MandarinkaAssaultStep.Suppress && mandarinkaPriorityCluster != null)
            TrackCombatTarget(null, "COUNTER-BATTERY // " + mandarinkaPriorityCluster.structures.Count + " EMPLACEMENTS", 3.8f);
    }

    private GoldenStructure FindMandarinkaStructureTarget(Vector3 origin, float localRange)
    {
        GoldenStructure best = null;
        float bestDistance = localRange;
        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null || structure.health <= 0f)
                continue;
            float distance = FlatDistance(origin, structure.transform.position);
            if (distance < bestDistance)
            {
                best = structure;
                bestDistance = distance;
            }
        }

        if (best != null)
            return best;
        if (mandarinkaPriorityStructure != null && mandarinkaPriorityStructure.transform != null && mandarinkaPriorityStructure.health > 0f)
            return mandarinkaPriorityStructure;
        return null;
    }

    private Vector3 GetMandarinkaAssaultObjective(Transform asset, Vector3 fallback)
    {
        if (mandarinkaPriorityCluster == null || asset == null)
            return fallback;

        if (mandarinkaAssaultStep == MandarinkaAssaultStep.Rally && mandarinkaFortressRoot != null)
        {
            Vector3 towardThreat = mandarinkaPriorityCluster.center - mandarinkaFortressRoot.position;
            towardThreat.y = 0f;
            if (towardThreat.sqrMagnitude < 0.01f)
                towardThreat = mandarinkaFortressRoot.forward;
            return GetDunePoint(mandarinkaFortressRoot.position + towardThreat.normalized * 48f);
        }

        Vector3 flank = Vector3.Cross(Vector3.up, (mandarinkaPriorityCluster.center - asset.position).normalized);
        float side = (asset.GetEntityId().GetHashCode() & 1) == 0 ? 1f : -1f;
        if (mandarinkaAssaultStep == MandarinkaAssaultStep.Breach)
            return GetDunePoint(mandarinkaPriorityCluster.center + flank * side * 24f);
        if (mandarinkaAssaultStep == MandarinkaAssaultStep.Exploit)
            return GetDunePoint(mandarinkaPriorityCluster.center + flank * side * 48f);
        return GetDunePoint(mandarinkaPriorityCluster.center);
    }

    private Vector3 GetMandarinkaStrategicAimPoint(Vector3 fallback)
    {
        if (mandarinkaPriorityCluster == null)
            return fallback;
        return GetDunePoint(mandarinkaPriorityCluster.center);
    }

    private bool CanMandarinkaFortressAdvance()
    {
        if (cinematicDirectorActive)
            return true;
        return verticalSliceStage == VerticalSliceStage.DestroyMandarinka || verticalSliceStage == VerticalSliceStage.Complete;
    }

    private void DamageMandarinkaTarget(GoldenStructure structure, float damage, Vector3 source)
    {
        if (structure == null || structure.transform == null || structure.health <= 0f)
            return;
        structure.health = Mathf.Max(0f, structure.health - damage);
        CreateBeam(source, structure.transform.position + Vector3.up * 1.1f, new Color(1f, 0.12f, 0.04f, 1f), 0.05f, 0.13f);
        if (structure.health <= 0f)
        {
            lastEvent = "Mandarinka's breach group destroyed " + structure.displayName + ".";
            CreateBattleExplosionFx(structure.transform.position, 8f, false, true);
        }
    }

    private bool TryGetStrategicNavigationDirection(Transform mover, Vector3 destination, out Vector3 direction)
    {
        direction = destination - mover.position;
        direction.y = 0f;
        if (!runtimeNavigationReady || mover == null)
            return direction.sqrMagnitude > 0.01f;

        StrategicPathState state;
        if (!strategicPaths.TryGetValue(mover, out state))
        {
            state = new StrategicPathState();
            state.destination = destination;
            strategicPaths.Add(mover, state);
        }

        state.repathTimer -= Time.deltaTime;
        if (state.repathTimer <= 0f || FlatDistance(state.destination, destination) > 10f || state.path.status == NavMeshPathStatus.PathInvalid)
        {
            state.repathTimer = 0.7f + Mathf.Abs(mover.GetEntityId().GetHashCode() % 7) * 0.04f;
            state.destination = destination;
            state.cornerIndex = 1;
            NavMeshHit startHit;
            NavMeshHit endHit;
            if (NavMesh.SamplePosition(mover.position, out startHit, 12f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(destination, out endHit, 24f, NavMesh.AllAreas))
                NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, state.path);
        }

        if (state.path.corners != null && state.path.corners.Length > 1)
        {
            state.cornerIndex = Mathf.Clamp(state.cornerIndex, 1, state.path.corners.Length - 1);
            if (FlatDistance(mover.position, state.path.corners[state.cornerIndex]) < 3f && state.cornerIndex < state.path.corners.Length - 1)
                state.cornerIndex++;
            direction = state.path.corners[state.cornerIndex] - mover.position;
            direction.y = 0f;
        }
        return direction.sqrMagnitude > 0.01f;
    }

    private Vector3 GetLargeAssetSafeDirection(Transform mover, Vector3 desiredDirection, float clearance, float dt)
    {
        if (mover == null || desiredDirection.sqrMagnitude < 0.01f)
            return Vector3.zero;

        LargeMovementState state;
        if (!largeMovementStates.TryGetValue(mover, out state))
        {
            state = new LargeMovementState();
            state.samplePosition = mover.position;
            state.lastSafePosition = mover.position;
            largeMovementStates.Add(mover, state);
        }

        state.sampleTimer += dt;
        float travelled = FlatDistance(state.samplePosition, mover.position);
        if (SandRunnersWarRules.RequiresStuckRecovery(travelled, balanceProfile.navigationMinimumProgress, state.sampleTimer, balanceProfile.navigationStuckSeconds))
        {
            state.sampleTimer = 0f;
            state.samplePosition = mover.position;
            state.recoveryAttempts++;
            if (state.recoveryAttempts >= balanceProfile.navigationRecoveryAttempts)
            {
                Vector3 recovery = state.lastSafePosition - mover.position;
                recovery.y = 0f;
                if (recovery.sqrMagnitude > 0.01f)
                    desiredDirection = recovery.normalized;
                state.recoveryAttempts = 0;
            }
        }
        else if (travelled >= balanceProfile.navigationMinimumProgress)
        {
            state.sampleTimer = 0f;
            state.samplePosition = mover.position;
            state.lastSafePosition = mover.position;
            state.recoveryAttempts = 0;
        }

        desiredDirection.Normalize();
        float[] angles = { 0f, 24f, -24f, 48f, -48f, 76f, -76f, 110f, -110f };
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 candidateDirection = Quaternion.Euler(0f, angles[i], 0f) * desiredDirection;
            Vector3 candidate = mover.position + candidateDirection * Mathf.Max(5f, clearance * 0.75f);
            if (!IsLargeAssetPositionBlocked(mover, candidate, clearance))
                return candidateDirection;
        }
        return Vector3.zero;
    }

    private bool IsLargeAssetPositionBlocked(Transform mover, Vector3 position, float clearance)
    {
        Collider[] hits = Physics.OverlapSphere(position + Vector3.up * 3f, clearance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(mover))
                continue;
            Vector3 size = hit.bounds.size;
            if (size.x > mapHalfSize || size.z > mapHalfSize)
                continue;
            string objectName = hit.transform.name;
            if (objectName.Contains("Sand") || objectName.Contains("Ground") || objectName.Contains("Terrain"))
                continue;
            return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        ShutdownRuntimeNavigation();
    }
}