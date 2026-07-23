using UnityEngine;

public partial class SandRunnersPrototype
{
    public enum CommandBridgeIntent
    {
        Move,
        EscortPyramid,
        Hold,
        SearchAndDestroy,
        DefendArea,
        FireMission
    }

    private bool commandBridgeMarkValid;
    private Vector3 commandBridgeMarkedPoint;
    private string commandBridgeMarkedLabel = "NO MARK";
    private bool commandBridgePresentationActive;

    public bool IsCommandBridgeGameplayAvailable()
    {
        return gameFlowState == SandRunnersGameFlowState.Playing;
    }

    public void StartDirectRtsForCommandBridge()
    {
        if (gameFlowState == SandRunnersGameFlowState.MainMenu)
            StartDirectRtsFromMenu();
    }

    public void SetCommandBridgePresentationActive(bool active)
    {
        commandBridgePresentationActive = active;
        hudHidden = active;
        PublishStrategicUiEvent(StrategicUiEventKind.StrategicCanvasVisibility, !active);
        if (active)
            SetCommandCursorMode(false);
    }

    public bool TrySetCommandBridgeMark(Ray ray, out Vector3 point, out string label, out string relation)
    {
        point = Vector3.zero;
        label = "NO TARGET";
        relation = "NEUTRAL";
        if (!Physics.Raycast(ray, out RaycastHit hit, 800f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        point = hit.point;
        EnemyUnit enemy = FindEnemyByHit(hit.transform) ?? FindEnemyNearCommandPoint(hit.point, 16f);
        if (enemy != null && enemy.transform != null)
        {
            point = enemy.transform.position;
            label = enemy.transform.name.Replace('_', ' ');
            relation = "ENEMY";
        }
        else
        {
            RunnerUnit ally = FindRunnerByRoot(hit.transform);
            if (ally != null)
            {
                label = ally.transform.name.Replace('_', ' ');
                relation = "ALLY";
            }
            else
            {
                label = hit.transform != null ? hit.transform.name.Replace('_', ' ') : "GROUND MARK";
                relation = "NEUTRAL";
            }
        }

        commandBridgeMarkValid = true;
        commandBridgeMarkedPoint = point;
        commandBridgeMarkedLabel = label;
        PlaceCommandMarker(point);
        lastEvent = "VISOR MARK: " + label + " // " + Mathf.RoundToInt(Vector3.Distance(battlePyramid.position, point)) + "m.";
        return true;
    }

    public bool TryGetCommandBridgeMark(out Vector3 point, out string label)
    {
        point = commandBridgeMarkedPoint;
        label = commandBridgeMarkedLabel;
        return commandBridgeMarkValid;
    }

    public int GetCommandBridgeSelectedSquadCount()
    {
        return selectedSquads.Count;
    }

    public bool IssueCommandBridgeIntent(CommandBridgeIntent intent)
    {
        if (selectedSquads.Count == 0)
        {
            lastEvent = "HOLOMAP: select an RTS squad before issuing an order.";
            return false;
        }

        Vector3 point = commandBridgeMarkValid ? commandBridgeMarkedPoint : battlePyramid.position;
        switch (intent)
        {
            case CommandBridgeIntent.EscortPyramid:
                ApplySelectedSquadDoctrine(SquadDoctrine.EscortPyramid, battlePyramid.position);
                return true;
            case CommandBridgeIntent.Hold:
            case CommandBridgeIntent.DefendArea:
                ApplySelectedSquadDoctrine(SquadDoctrine.HoldArea, point);
                return true;
            case CommandBridgeIntent.SearchAndDestroy:
                ApplySelectedSquadDoctrine(SquadDoctrine.SearchAndDestroy, point);
                return true;
            case CommandBridgeIntent.FireMission:
            {
                EnemyUnit target = FindEnemyNearCommandPoint(point, 45f);
                if (target == null)
                {
                    lastEvent = "FIRE MISSION: no enemy inside the marked area.";
                    return false;
                }
                for (int i = 0; i < selectedSquads.Count; i++)
                    if (selectedSquads[i] != null && !selectedSquads[i].autonomous)
                        IssueSquadAttackOrder(selectedSquads[i], target);
                PlaceCommandMarker(point);
                return true;
            }
            default:
                for (int i = 0; i < selectedSquads.Count; i++)
                    if (selectedSquads[i] != null && !selectedSquads[i].autonomous)
                        IssueSquadMoveOrder(selectedSquads[i], point, i);
                return true;
        }
    }

    public bool BeginCommandBridgeApexCharge()
    {
        if (!commandBridgeMarkValid || beamCooldownTimer > 0f)
            return false;
        beamCharge = 0f;
        Vector3 start = apexEmitter != null ? apexEmitter.position :
            (beamMuzzle != null ? beamMuzzle.position : battlePyramid.position + Vector3.up * 5f);
        CreateBeam(start, commandBridgeMarkedPoint, new Color(1f, 0.45f, 0.08f, 0.8f), 0.055f, 1.2f);
        CreateReadableImpactWarning(commandBridgeMarkedPoint, 14f, new Color(1f, 0.25f, 0.08f, 1f), 2.4f, "APEX DANGER AREA", true);
        lastEvent = "APEX: firing line transferred from visor. Hold R to condense sunlight.";
        return true;
    }

    public float ChargeCommandBridgeApex(float dt)
    {
        if (beamCooldownTimer > 0f || !commandBridgeMarkValid)
            return 0f;
        beamCharge = Mathf.Clamp01(beamCharge + dt / 2.4f);
        return beamCharge;
    }

    public bool ReleaseCommandBridgeApex()
    {
        if (!commandBridgeMarkValid || beamCooldownTimer > 0f || beamCharge < 0.25f)
        {
            beamCharge = 0f;
            return false;
        }

        float charge = beamCharge;
        beamCharge = 0f;
        beamCooldownTimer = 4.5f;
        Vector3 start = apexEmitter != null ? apexEmitter.position :
            (beamMuzzle != null ? beamMuzzle.position : battlePyramid.position + Vector3.up * 5f);
        Vector3 end = commandBridgeMarkedPoint;
        CreateBeam(start, end, new Color(1f, 0.95f, 0.38f, 1f), Mathf.Lerp(0.18f, 0.55f, charge), 0.32f);
        DamageEnemiesAlongLine(start, end, Mathf.Lerp(5f, 11f, charge), Mathf.Lerp(110f, 260f, charge));
        CreateExplosion(end, Mathf.Lerp(8f, 18f, charge), Mathf.Lerp(80f, 180f, charge), false);
        CreateWeaponFlash(start, Mathf.Lerp(0.8f, 1.45f, charge), new Color(1f, 0.95f, 0.42f, 1f));
        RegisterWeaponImpulse(Mathf.Lerp(0.16f, 0.34f, charge));
        PlayApexDischargeSound(charge);
        lastEvent = "СЕБЕК: ПЛИ! Apex impact confirmed at visor mark.";
        return true;
    }

    public float GetCommandBridgeApexReadiness()
    {
        return beamCooldownTimer > 0f ? -beamCooldownTimer : beamCharge;
    }

    public bool DebugPrepareCommandBridgeSmoke()
    {
        if (unitSquads.Count == 0)
            CreateSquad("Command Bridge Smoke Squad", false);
        SelectOnlySquad(unitSquads[0]);
        EnemyUnit target = FindNearestEnemy(battlePyramid.position, 260f);
        commandBridgeMarkedPoint = target != null && target.transform != null
            ? target.transform.position
            : battlePyramid.position + battlePyramid.forward * 45f;
        commandBridgeMarkedLabel = target != null && target.transform != null ? target.transform.name : "SMOKE MARK";
        commandBridgeMarkValid = true;
        PlaceCommandMarker(commandBridgeMarkedPoint);
        return true;
    }

    public bool DebugRunCommandBridgeMoveSmoke()
    {
        if (!DebugPrepareCommandBridgeSmoke())
            return false;
        bool issued = IssueCommandBridgeIntent(CommandBridgeIntent.Move);
        return issued && selectedSquads.Count > 0 && selectedSquads[0].orderType == OrderType.Move;
    }

    public bool DebugRunCommandBridgeApexSmoke()
    {
        if (!DebugPrepareCommandBridgeSmoke())
            return false;
        beamCooldownTimer = 0f;
        if (!BeginCommandBridgeApexCharge())
            return false;
        ChargeCommandBridgeApex(2.4f);
        return ReleaseCommandBridgeApex();
    }
}
