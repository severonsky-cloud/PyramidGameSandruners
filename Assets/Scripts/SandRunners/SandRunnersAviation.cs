using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum AirUnitState
    {
        HangarReady,
        Launching,
        Formation,
        Patrol,
        AttackRun,
        BreakOff,
        Returning,
        Landing,
        Rearming,
        Repairing,
        Destroyed
    }

    private enum AirRole
    {
        Interceptor,
        Strike,
        HeavyBomber,
        LongRangePlatform,
        Transport
    }

    private enum AirMission
    {
        Move,
        Attack,
        Intercept,
        Patrol,
        Escort,
        Hold,
        ReturnToBase
    }

    private sealed class AirBaseState
    {
        public Transform root;
        public GoldenStructure structure;
        public bool pyramid;
        public bool carrier;
        public int launchSlots;
        public int serviceSlots;
    }

    private sealed class AirWingState
    {
        public UnitSquad squad;
        public AirBaseState baseState;
        public AirRole role;
        public AirMission mission;
        public AirUnitState state;
        public EnemyUnit target;
        public Vector3 lastHeading = Vector3.forward;
        public float stateTimer;
        public float fireTimer;
        public float serviceTimer;
        public float endurance = 85f;
        public float maxEndurance = 85f;
        public int ammo;
        public int maxAmmo;
        public float sortieDistance;
        public bool announcedLaunch;
    }

    private readonly List<AirBaseState> aviationBases = new List<AirBaseState>();
    private readonly List<AirWingState> aviationWings = new List<AirWingState>();
    private float aviationAnnouncementTimer;
    private float aviationHudRefreshTimer;

    private void UpdateSandRunnersAviation(float dt)
    {
        EnsureAviationBases();
        RegisterNewAirWings();

        aviationAnnouncementTimer -= dt;
        for (int i = aviationWings.Count - 1; i >= 0; i--)
        {
            AirWingState wing = aviationWings[i];
            if (wing == null || wing.squad == null || GetAirWingAliveCount(wing) == 0)
            {
                aviationWings.RemoveAt(i);
                continue;
            }

            UpdateAirWing(wing, dt);
        }

        aviationHudRefreshTimer -= dt;
        if (aviationHudRefreshTimer <= 0f)
        {
            aviationHudRefreshTimer = 0.25f;
            UpdateAviationHudText();
        }
    }

    private void EnsureAviationBases()
    {
        for (int i = aviationBases.Count - 1; i >= 0; i--)
        {
            AirBaseState baseState = aviationBases[i];
            if (baseState == null || baseState.pyramid || baseState.structure == null)
                continue;
            if (baseState.structure.transform == null)
                aviationBases.RemoveAt(i);
        }

        if (battlePyramid != null && FindAviationBase(null, true) == null)
        {
            AirBaseState pyramidBase = new AirBaseState();
            pyramidBase.root = battlePyramid;
            pyramidBase.pyramid = true;
            pyramidBase.launchSlots = 2;
            pyramidBase.serviceSlots = 1;
            aviationBases.Add(pyramidBase);
        }

        for (int i = 0; i < goldenStructures.Count; i++)
        {
            GoldenStructure structure = goldenStructures[i];
            if (structure == null || structure.transform == null || structure.kind != StructureKind.Aerodrome)
                continue;
            if (FindAviationBase(structure, false) != null)
                continue;

            AirBaseState aerodrome = new AirBaseState();
            aerodrome.root = structure.transform;
            aerodrome.structure = structure;
            aerodrome.launchSlots = 2;
            aerodrome.serviceSlots = 2;
            aviationBases.Add(aerodrome);
        }
    }

    private AirBaseState FindAviationBase(GoldenStructure structure, bool pyramid)
    {
        for (int i = 0; i < aviationBases.Count; i++)
        {
            AirBaseState baseState = aviationBases[i];
            if (baseState != null && baseState.pyramid == pyramid && (pyramid || baseState.structure == structure))
                return baseState;
        }
        return null;
    }

    private bool IsAviationBaseOperational(AirBaseState baseState)
    {
        if (baseState == null || baseState.root == null)
            return false;
        return baseState.pyramid || baseState.carrier || (baseState.structure != null && baseState.structure.health > 0f);
    }

    private AirBaseState FindNearestOperationalAviationBase(Vector3 position)
    {
        AirBaseState best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < aviationBases.Count; i++)
        {
            AirBaseState baseState = aviationBases[i];
            if (!IsAviationBaseOperational(baseState))
                continue;
            float distance = FlatDistance(position, baseState.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = baseState;
            }
        }
        return best;
    }

    private AirBaseState ResolveAirWingBase(UnitSquad squad)
    {
        Transform representative = GetSquadRepresentative(squad);
        Vector3 position = representative != null ? representative.position : battlePyramid.position;
        AirBaseState nearest = FindNearestOperationalAviationBase(position);
        if (nearest != null && FlatDistance(position, nearest.root.position) <= 90f)
            return nearest;
        return FindAviationBase(null, true);
    }

    private void RegisterNewAirWings()
    {
        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            if (squad == null || !squad.airborne || FindAirWing(squad) != null)
                continue;

            AirWingState wing = new AirWingState();
            wing.squad = squad;
            wing.baseState = ResolveAirWingBase(squad);
            wing.role = GetAirRole(squad.displayName);
            wing.mission = AirMission.Move;
            wing.state = AirUnitState.Launching;
            wing.stateTimer = 2.2f;
            wing.maxEndurance = GetAirEndurance(wing.role);
            wing.endurance = wing.maxEndurance;
            wing.maxAmmo = GetAirAmmo(wing.role);
            wing.ammo = wing.maxAmmo;
            squad.autoEngage = true;
            squad.doctrine = SquadDoctrine.EscortPyramid;
            squad.doctrineRadius = 170f;
            squad.orderType = OrderType.Move;
            squad.orderPosition = GetAirLaunchPoint(wing.baseState) + Vector3.up * 8f;
            aviationWings.Add(wing);
        }
    }

    private AirWingState FindAirWing(UnitSquad squad)
    {
        for (int i = 0; i < aviationWings.Count; i++)
        {
            if (aviationWings[i] != null && aviationWings[i].squad == squad)
                return aviationWings[i];
        }
        return null;
    }

    private AirRole GetAirRole(string displayName)
    {
        string name = displayName ?? string.Empty;
        if (name.Contains("Heavy"))
            return AirRole.HeavyBomber;
        if (name.Contains("Thoth"))
            return AirRole.LongRangePlatform;
        if (name.Contains("Abydos"))
            return AirRole.Strike;
        if (name.Contains("Vimana"))
            return AirRole.Interceptor;
        return AirRole.Strike;
    }

    private float GetAirEndurance(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return balanceProfile.interceptorEndurance;
            case AirRole.HeavyBomber: return balanceProfile.heavyEndurance;
            case AirRole.LongRangePlatform: return balanceProfile.carrierEndurance;
            default: return balanceProfile.strikeEndurance;
        }
    }

    private int GetAirAmmo(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return balanceProfile.interceptorAmmo;
            case AirRole.HeavyBomber: return balanceProfile.heavyAmmo;
            case AirRole.LongRangePlatform: return balanceProfile.carrierAmmo;
            default: return balanceProfile.strikeAmmo;
        }
    }

    private void UpdateAirWing(AirWingState wing, float dt)
    {
        wing.fireTimer -= dt;
        if (wing.state != AirUnitState.HangarReady && wing.state != AirUnitState.Landing &&
            wing.state != AirUnitState.Repairing && wing.state != AirUnitState.Rearming)
        {
            wing.endurance = Mathf.Max(0f, wing.endurance - dt * 0.24f);
        }

        if (!IsAviationBaseOperational(wing.baseState))
            wing.baseState = FindNearestOperationalAviationBase(GetAirWingCentroid(wing));

        if (wing.baseState == null)
        {
            wing.state = AirUnitState.BreakOff;
            MoveAirWing(wing, battlePyramid.position + Vector3.up * 18f, dt);
            return;
        }

        if (wing.state != AirUnitState.Returning && wing.state != AirUnitState.Landing && wing.state != AirUnitState.Repairing &&
            (wing.ammo <= 0 || wing.endurance <= 3f || GetAirWingHealthRatio(wing) < balanceProfile.emergencyReturnHealth))
        {
            wing.state = AirUnitState.Returning;
            wing.mission = AirMission.ReturnToBase;
            wing.target = null;
            AnnounceAviation(wing, "Air wing returning for service.");
        }

        switch (wing.state)
        {
            case AirUnitState.Launching:
                MoveAirWing(wing, GetAirLaunchPoint(wing.baseState) + wing.baseState.root.forward * 24f + Vector3.up * 9f, dt);
                wing.stateTimer -= dt;
                if (wing.stateTimer <= 0f)
                {
                    wing.state = AirUnitState.Formation;
                    wing.mission = AirMission.Hold;
                    PlaySandRunnerSound(SandRunnerSound.AircraftTakeoff, GetAirWingCentroid(wing), 0.72f);
                    AnnounceAviation(wing, "Air wing formed and ready.");
                }
                break;
            case AirUnitState.Formation:
            case AirUnitState.Patrol:
                UpdateAirFormation(wing, dt);
                break;
            case AirUnitState.AttackRun:
                UpdateAirAttackRun(wing, dt);
                break;
            case AirUnitState.Returning:
            case AirUnitState.BreakOff:
                UpdateAirReturn(wing, dt);
                break;
            case AirUnitState.Landing:
                MoveAirWing(wing, GetAirLandingPoint(wing.baseState), dt);
                wing.stateTimer -= dt;
                if (wing.stateTimer <= 0f)
                {
                    wing.state = AirUnitState.Repairing;
                    wing.serviceTimer = balanceProfile.aircraftServiceSeconds;
                    AnnounceAviation(wing, "Air wing on pad. Repair and rearm started.");
                }
                break;
            case AirUnitState.Repairing:
            case AirUnitState.Rearming:
                UpdateAirService(wing, dt);
                break;
        }
    }

    private Vector3 GetAirDoctrineAnchor(AirWingState wing)
    {
        if (wing == null || wing.squad == null)
            return battlePyramid.position;
        if (wing.squad.doctrine != SquadDoctrine.EscortPyramid)
            return wing.squad.doctrineAnchor;
        if (wing.baseState != null && wing.baseState.carrier && wing.baseState.root != null)
            return wing.baseState.root.position;
        return battlePyramid.position;
    }

    private void UpdateAirFormation(AirWingState wing, float dt)
    {
        UnitSquad squad = wing.squad;
        if (squad.orderType == OrderType.Attack && IsLiveDoctrineTarget(squad.attackTarget))
        {
            wing.target = squad.attackTarget;
            wing.mission = AirMission.Attack;
            wing.state = AirUnitState.AttackRun;
            return;
        }

        if (squad.orderType == OrderType.Attack)
        {
            squad.orderType = OrderType.None;
            squad.attackTarget = null;
        }

        if (squad.orderType == OrderType.Move)
        {
            Vector3 movePoint = squad.orderPosition + Vector3.up * GetWingFlightHeight(wing);
            if (FlatDistance(GetAirWingCentroid(wing), squad.orderPosition) <= 10f)
            {
                squad.orderType = OrderType.None;
                wing.mission = AirMission.Hold;
            }
            else
            {
                wing.mission = AirMission.Move;
                MoveAirWing(wing, movePoint, dt);
                return;
            }
        }

        Vector3 anchor = GetAirDoctrineAnchor(wing);
        EnemyUnit doctrineTarget = ResolveSquadDoctrineTarget(squad, GetAirWingCentroid(wing), anchor);
        if (doctrineTarget != null && wing.ammo > 0)
        {
            wing.target = doctrineTarget;
            wing.mission = squad.doctrine == SquadDoctrine.EscortPyramid ? AirMission.Escort : AirMission.Patrol;
            wing.state = AirUnitState.AttackRun;
            return;
        }

        float orbitRadius = squad.doctrine == SquadDoctrine.SearchAndDestroy
            ? Mathf.Min(92f, squad.doctrineRadius * 0.42f)
            : squad.doctrine == SquadDoctrine.HoldArea ? 30f : 38f;
        float angle = Time.time * (squad.doctrine == SquadDoctrine.SearchAndDestroy ? 0.12f : 0.2f) + squad.id * 1.37f;
        Vector3 orbitPoint = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        orbitPoint.y = GetPlayableGroundHeight(orbitPoint) + GetWingFlightHeight(wing);
        wing.mission = squad.doctrine == SquadDoctrine.EscortPyramid ? AirMission.Escort : AirMission.Patrol;
        wing.state = AirUnitState.Patrol;
        MoveAirWing(wing, orbitPoint, dt);
    }

    private void UpdateAirAttackRun(AirWingState wing, float dt)
    {
        bool explicitAttack = wing.squad.orderType == OrderType.Attack;
        Vector3 doctrineAnchor = GetAirDoctrineAnchor(wing);
        if (!IsLiveDoctrineTarget(wing.target) ||
            (!explicitAttack && !IsTargetInsideDoctrine(wing.squad, wing.target, doctrineAnchor)))
        {
            wing.target = null;
            wing.squad.attackTarget = null;
            wing.squad.doctrineTarget = null;
            wing.squad.orderType = OrderType.None;
            wing.state = AirUnitState.Formation;
            return;
        }

        Vector3 targetPosition = wing.target.transform.position;
        Vector3 toTarget = targetPosition - GetAirWingCentroid(wing);
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude < 0.1f)
            flat = wing.lastHeading;
        wing.lastHeading = flat.normalized;

        float range = GetAirAttackRange(wing.role);
        if (flat.magnitude > range * 0.72f)
        {
            MoveAirWing(wing, targetPosition - wing.lastHeading * Mathf.Min(18f, range * 0.45f) + Vector3.up * GetWingFlightHeight(wing), dt);
            return;
        }

        MoveAirWing(wing, targetPosition - wing.lastHeading * 9f + Vector3.up * GetWingFlightHeight(wing), dt);
        if (wing.fireTimer > 0f || wing.ammo <= 0)
            return;

        wing.fireTimer = GetAirAttackCooldown(wing.role);
        wing.ammo--;
        float damage = GetAirDamage(wing.role);
        wing.target.health -= damage;
        bool heavy = wing.role == AirRole.HeavyBomber || wing.role == AirRole.LongRangePlatform;
        CreateReadableHit(GetAirWingCentroid(wing), targetPosition + Vector3.up * 0.8f, heavy);
        PlaySandRunnerSound(heavy ? SandRunnerSound.HeavyArtillery : SandRunnerSound.MissileLaunch, targetPosition, heavy ? 0.8f : 0.55f);
        TrackCombatTarget(wing.target, "AIR WING STRIKE", 3.5f);
        AnnounceAviation(wing, "Air strike delivered. Ammo " + wing.ammo + "/" + wing.maxAmmo + ".");

        if (wing.ammo <= 0)
        {
            wing.state = AirUnitState.Returning;
            wing.mission = AirMission.ReturnToBase;
        }
    }

    private void UpdateAirReturn(AirWingState wing, float dt)
    {
        if (!IsAviationBaseOperational(wing.baseState))
            wing.baseState = FindNearestOperationalAviationBase(GetAirWingCentroid(wing));
        if (wing.baseState == null)
            return;

        Vector3 landingPoint = GetAirLandingPoint(wing.baseState);
        MoveAirWing(wing, landingPoint + wing.baseState.root.forward * 14f + Vector3.up * 8f, dt);
        if (FlatDistance(GetAirWingCentroid(wing), landingPoint) <= 18f)
        {
            wing.state = AirUnitState.Landing;
            wing.stateTimer = 1.6f;
            PlaySandRunnerSound(SandRunnerSound.AircraftReturn, landingPoint, 0.55f);
        }
    }

    private void UpdateAirService(AirWingState wing, float dt)
    {
        Vector3 servicePoint = GetAirLandingPoint(wing.baseState) + Vector3.up * 2f;
        MoveAirWing(wing, servicePoint, dt);
        wing.serviceTimer -= dt;
        for (int i = 0; i < wing.squad.units.Count; i++)
        {
            RunnerUnit unit = wing.squad.units[i];
            if (unit != null)
                unit.health = Mathf.MoveTowards(unit.health, unit.maxHealth, unit.maxHealth * 0.22f * dt);
        }
        if (wing.serviceTimer <= 0f)
        {
            wing.ammo = wing.maxAmmo;
            wing.endurance = wing.maxEndurance;
            wing.state = AirUnitState.Formation;
            wing.mission = AirMission.Hold;
            wing.squad.orderType = OrderType.None;
            wing.squad.attackTarget = null;
            PlaySandRunnerSound(SandRunnerSound.AircraftRepair, servicePoint, 0.5f);
            AnnounceAviation(wing, "Air wing serviced and ready.");
        }
    }

    private void MoveAirWing(AirWingState wing, Vector3 center, float dt)
    {
        if (wing.squad == null)
            return;

        Vector3 centroid = GetAirWingCentroid(wing);
        Vector3 heading = center - centroid;
        heading.y = 0f;
        if (heading.sqrMagnitude > 0.25f)
            wing.lastHeading = heading.normalized;
        if (wing.lastHeading.sqrMagnitude < 0.1f)
            wing.lastHeading = Vector3.forward;

        Vector3 forward = wing.lastHeading.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        int count = Mathf.Max(1, wing.squad.units.Count);
        float spacing = wing.role == AirRole.HeavyBomber ? 4.4f : 3.4f;
        float speed = GetAirSpeed(wing.role);
        for (int i = 0; i < wing.squad.units.Count; i++)
        {
            RunnerUnit unit = wing.squad.units[i];
            if (unit == null || unit.transform == null || unit.health <= 0f)
                continue;

            float centerIndex = (count - 1) * 0.5f;
            Vector3 offset = right * ((i - centerIndex) * spacing) - forward * Mathf.Abs(i - centerIndex) * 1.2f;
            Vector3 desired = center + offset;
            float ground = GetPlayableGroundHeight(desired);
            desired.y = Mathf.Max(desired.y, ground + GetWingFlightHeight(wing));
            unit.transform.position = Vector3.MoveTowards(unit.transform.position, desired, speed * dt);

            Vector3 look = desired - unit.transform.position;
            if (look.sqrMagnitude > 0.1f)
                unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), Mathf.Clamp01(dt * 5f));
        }
    }

    private Vector3 GetAirWingCentroid(AirWingState wing)
    {
        Vector3 result = Vector3.zero;
        int count = 0;
        if (wing != null && wing.squad != null)
        {
            for (int i = 0; i < wing.squad.units.Count; i++)
            {
                RunnerUnit unit = wing.squad.units[i];
                if (unit != null && unit.transform != null && unit.health > 0f)
                {
                    result += unit.transform.position;
                    count++;
                }
            }
        }
        return count > 0 ? result / count : battlePyramid.position;
    }

    private int GetAirWingAliveCount(AirWingState wing)
    {
        int count = 0;
        if (wing == null || wing.squad == null)
            return count;
        for (int i = 0; i < wing.squad.units.Count; i++)
        {
            RunnerUnit unit = wing.squad.units[i];
            if (unit != null && unit.transform != null && unit.health > 0f)
                count++;
        }
        return count;
    }

    private float GetAirWingHealthRatio(AirWingState wing)
    {
        float current = 0f;
        float maximum = 0f;
        if (wing != null && wing.squad != null)
        {
            for (int i = 0; i < wing.squad.units.Count; i++)
            {
                RunnerUnit unit = wing.squad.units[i];
                if (unit != null && unit.health > 0f)
                {
                    current += unit.health;
                    maximum += unit.maxHealth;
                }
            }
        }
        return maximum > 0f ? current / maximum : 0f;
    }

    private float GetAirSpeed(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return 23f;
            case AirRole.HeavyBomber: return 12f;
            case AirRole.LongRangePlatform: return 9f;
            default: return 18f;
        }
    }

    private float GetAirAttackRange(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return 30f;
            case AirRole.HeavyBomber: return 40f;
            case AirRole.LongRangePlatform: return 58f;
            default: return 36f;
        }
    }

    private float GetAirAttackCooldown(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return 0.8f;
            case AirRole.HeavyBomber: return 2.7f;
            case AirRole.LongRangePlatform: return 3.4f;
            default: return 1.35f;
        }
    }

    private float GetAirDamage(AirRole role)
    {
        switch (role)
        {
            case AirRole.Interceptor: return 18f;
            case AirRole.HeavyBomber: return 82f;
            case AirRole.LongRangePlatform: return 108f;
            default: return 42f;
        }
    }

    private float GetWingFlightHeight(AirWingState wing)
    {
        switch (wing.role)
        {
            case AirRole.Interceptor: return 10f;
            case AirRole.HeavyBomber: return 15f;
            case AirRole.LongRangePlatform: return 19f;
            default: return 12f;
        }
    }

    private Vector3 GetAirLaunchPoint(AirBaseState baseState)
    {
        if (baseState == null || baseState.root == null)
            return battlePyramid.position + battlePyramid.forward * 8f;
        if (baseState.pyramid)
            return GetHangarExitPosition() + baseState.root.forward * 2f + Vector3.up * 4.5f;
        return baseState.root.TransformPoint(new Vector3(0f, 0.65f, 1.2f)) + Vector3.up * 4.5f;
    }

    private Vector3 GetAirLandingPoint(AirBaseState baseState)
    {
        if (baseState == null || baseState.root == null)
            return battlePyramid.position + Vector3.up * 4f;
        if (baseState.pyramid)
            return GetHangarExitPosition() + Vector3.up * 3f;
        return baseState.root.TransformPoint(new Vector3(0f, 0.72f, 1.2f)) + Vector3.up * 1.8f;
    }

    private void AnnounceAviation(AirWingState wing, string message)
    {
        if (aviationAnnouncementTimer > 0f)
            return;
        aviationAnnouncementTimer = 1.2f;
        lastEvent = (wing != null && wing.squad != null ? wing.squad.displayName : "Air wing") + ": " + message;
        PlaySandRunnerSound(SandRunnerSound.HangarRelease, GetAirWingCentroid(wing), 0.32f);
    }

    private void UpdateAviationHudText()
    {
        if (strategicSelectedStatsText == null || selectedSquads == null || selectedSquads.Count == 0)
            return;
        UnitSquad selected = selectedSquads[0];
        AirWingState wing = FindAirWing(selected);
        if (wing == null)
            return;

        string baseLabel = wing.baseState == null || wing.baseState.root == null
            ? "NO BASE"
            : wing.baseState.carrier ? "THOTH" : wing.baseState.pyramid ? "PYRAMID" : "AERODROME";
        string routeLabel = wing.target != null && wing.target.transform != null
            ? Mathf.RoundToInt(FlatDistance(GetAirWingCentroid(wing), wing.target.transform.position)) + "m to target"
            : Mathf.RoundToInt(wing.sortieDistance) + "m sortie";
        strategicSelectedStatsText.text =
            "State " + wing.state + "   Mission " + wing.mission + "\n" +
            "Active " + GetAirWingAliveCount(wing) + "/" + selected.units.Count +
            "   Ammo " + wing.ammo + "/" + wing.maxAmmo + "\n" +
            "Endurance " + Mathf.RoundToInt(wing.endurance) + "s   Hull " + Mathf.RoundToInt(GetAirWingHealthRatio(wing) * 100f) + "%\n" +
            "Return " + baseLabel + "   Route " + routeLabel;
    }
}