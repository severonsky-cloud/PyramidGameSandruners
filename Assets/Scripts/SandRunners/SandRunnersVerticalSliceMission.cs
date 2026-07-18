using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private enum VerticalSliceStage
    {
        SecureResource,
        BuildAerodrome,
        LaunchAirWing,
        TradeSettlement,
        PowerSettlement,
        FieldThoth,
        SurviveCaravanRaids,
        DestroyMandarinka,
        Complete
    }

    private VerticalSliceStage verticalSliceStage;
    private bool verticalSliceMissionInitialized;
    private bool verticalSliceSupersededByRetaliation;
    private float verticalSliceStageElapsed;
    private int verticalSliceStagesCompleted;
    private GameObject verticalSliceMissionPanelObject;
    private Text verticalSliceMissionTitleText;
    private Text verticalSliceMissionObjectiveText;
    private Text verticalSliceMissionProgressText;
    private StrategicCanvasButton verticalSlicePrimaryButton;
    private StrategicCanvasButton verticalSliceHelpButton;

    private void InitializeVerticalSliceMission()
    {
        if (verticalSliceMissionInitialized)
            return;
        verticalSliceMissionInitialized = true;
        verticalSliceStage = VerticalSliceStage.SecureResource;
        verticalSliceStageElapsed = 0f;
        verticalSliceStagesCompleted = 0;
        missionTitle = "VERTICAL SLICE // DESERT COVENANT";
        ApplyVerticalSliceStage(false);
    }

    private void UpdateVerticalSliceMission(float dt)
    {
        if (!verticalSliceMissionInitialized)
            InitializeVerticalSliceMission();

        if (imperialRetaliationState != ImperialRetaliationState.Inactive)
        {
            verticalSliceSupersededByRetaliation = true;
            UpdateVerticalSliceMissionUi();
            return;
        }

        if (verticalSliceStage == VerticalSliceStage.Complete)
            return;

        verticalSliceStageElapsed += dt;
        bool complete = false;
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource:
                complete = CountControlledResourceNodes() > 0;
                break;
            case VerticalSliceStage.BuildAerodrome:
                // The airbase is instructional, not a campaign hard-lock. If the
                // player has already reached a later strategic accomplishment, let
                // the tutorial catch up instead of displaying step 2 for the run.
                complete = CountAerodromes() > 0 || HasOperationalMissionAirWing() ||
                           HasCompletedPlayerTrade() || HasOnlineSettlementGrid() || thothCarrierReady;
                break;
            case VerticalSliceStage.LaunchAirWing:
                complete = HasOperationalMissionAirWing() || HasCompletedPlayerTrade() ||
                           HasOnlineSettlementGrid() || thothCarrierReady;
                break;
            case VerticalSliceStage.TradeSettlement:
                complete = HasCompletedPlayerTrade() || HasOnlineSettlementGrid() || thothCarrierReady;
                break;
            case VerticalSliceStage.PowerSettlement:
                complete = HasOnlineSettlementGrid() || thothCarrierReady;
                break;
            case VerticalSliceStage.FieldThoth:
                complete = thothCarrierReady && thothCarrierRoot != null;
                break;
            case VerticalSliceStage.SurviveCaravanRaids:
                complete = mandarinkaCaravanCounterstrikeTriggered && mandarinkaAvoidsCaravanRoutes;
                break;
            case VerticalSliceStage.DestroyMandarinka:
                complete = mandarinkaDefeated;
                break;
        }

        if (complete)
            AdvanceVerticalSliceStage();
    }

    private void AdvanceVerticalSliceStage()
    {
        if (verticalSliceStage >= VerticalSliceStage.Complete)
            return;
        verticalSliceStagesCompleted = Mathf.Min(8, verticalSliceStagesCompleted + 1);
        verticalSliceStage = (VerticalSliceStage)Mathf.Min((int)VerticalSliceStage.Complete, (int)verticalSliceStage + 1);
        verticalSliceStageElapsed = 0f;
        ApplyVerticalSliceStage(true);
    }

    private void ApplyVerticalSliceStage(bool rewardPreviousStage)
    {
        if (rewardPreviousStage)
            GrantVerticalSliceMilestoneReward(verticalSliceStage);

        string dialogue;
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource:
                missionObjective = "Secure any sand, gold, or wind extraction point.";
                dialogue = "PYRAMID SPIRIT: The desert is an engine. Feed the pyramid before the red court closes the road.";
                break;
            case VerticalSliceStage.BuildAerodrome:
                missionObjective = "Build a forward aerodrome with a Solar Builder Truck.";
                dialogue = "SEBEK-NU-ANKHA: We need a runway outside my hull. Build where the dunes give us room.";
                break;
            case VerticalSliceStage.LaunchAirWing:
                missionObjective = "Launch and form one operational air wing.";
                dialogue = "ROBERT: So the pyramid carries aircraft? SEBEK-NU-ANKHA: It carries answers.";
                break;
            case VerticalSliceStage.TradeSettlement:
                missionObjective = "Approach a neutral settlement and complete a trade contract.";
                dialogue = "BLUE ELEMENTALS: Bring resources, not promises. One honest contract can change a settlement.";
                break;
            case VerticalSliceStage.PowerSettlement:
                missionObjective = "Connect a controlled wind node to a settlement with a three-relay mirror grid.";
                dialogue = "PYRAMID SPIRIT: Share the wind and the settlement will grow under our light.";
                break;
            case VerticalSliceStage.FieldThoth:
                missionObjective = "Field Thoth's Embrace and ready its four carrier slots.";
                dialogue = "SEBEK-NU-ANKHA: Wake Thoth. A moving war needs a moving sky.";
                break;
            case VerticalSliceStage.SurviveCaravanRaids:
                missionObjective = "Protect the trade road through Mandarinka's 2–5 caravan raids.";
                dialogue = "MANDARINKA: I will tax every road that carries aid to your stolen kingdom.";
                PrepareVerticalSliceCaravanCrisis();
                break;
            case VerticalSliceStage.DestroyMandarinka:
                missionObjective = "Use the alliance, Thoth, aviation, missiles, and four howitzers to destroy Mandarinka's mobile fortress.";
                dialogue = "SEBEK-NU-ANKHA: Her road bends around the traders. Good. Now break the palace at the end of it.";
                cruiseMissiles = Mathf.Max(cruiseMissiles, 5);
                nuclearMissiles = Mathf.Max(nuclearMissiles, 1);
                break;
            default:
                missionObjective = "Vertical slice complete. Mandarinka's palace is destroyed and the desert alliance survives.";
                dialogue = "PYRAMID SPIRIT: Extraction, alliance, air power, and siege form one living war.";
                break;
        }

        SetMissionDialogue(dialogue, 7f);
        if (rewardPreviousStage)
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.68f);
            ShowBanner("OBJECTIVE COMPLETE // " + verticalSliceStagesCompleted + "/8", 2.6f);
        }
        UpdateVerticalSliceMissionUi();
    }

    private void GrantVerticalSliceMilestoneReward(VerticalSliceStage enteredStage)
    {
        SandRunnersResourcePrice reward = GetReleaseMilestoneReward(enteredStage);
        sand += reward.sand;
        gold += reward.gold;
        wind += reward.wind;
        if (enteredStage == VerticalSliceStage.FieldThoth)
            lastEvent = "Settlement co-funding reduced the remaining cost of Thoth's Embrace.";
    }

    private int CountControlledResourceNodes()
    {
        int count = 0;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            if (resourceNodes[i] != null && resourceNodes[i].controlled)
                count++;
        }
        return count;
    }

    private bool HasOperationalMissionAirWing()
    {
        for (int i = 0; i < aviationWings.Count; i++)
        {
            AirWingState wing = aviationWings[i];
            if (wing == null || wing.squad == null || wing.role == AirRole.LongRangePlatform)
                continue;
            if (wing.state == AirUnitState.Formation || wing.state == AirUnitState.Patrol || wing.state == AirUnitState.AttackRun || wing.state == AirUnitState.BreakOff || wing.state == AirUnitState.Returning)
                return true;
        }
        return false;
    }

    private bool HasCompletedPlayerTrade()
    {
        foreach (SettlementDevelopmentState state in settlementDevelopment.Values)
        {
            if (state != null && state.playerDeals > 0)
                return true;
        }
        return false;
    }

    private bool HasOnlineSettlementGrid()
    {
        for (int i = 0; i < energyNetworks.Count; i++)
        {
            if (energyNetworks[i] != null && energyNetworks[i].online)
                return true;
        }
        return false;
    }

    private void PrepareVerticalSliceCaravanCrisis()
    {
        if (tradeCaravans.Count == 0)
            SpawnTradeCaravan();
        mandarinkaStrategyTimer = Mathf.Max(mandarinkaStrategyTimer, 91f);
        mandarinkaStrategyPhase = MandarinkaStrategyPhase.ScoutRaid;
        mandarinkaCaravanRaidTimer = Mathf.Min(mandarinkaCaravanRaidTimer, 10f);
        if (mandarinkaCaravanRaidQuota <= 0)
            mandarinkaCaravanRaidQuota = Random.Range(balanceProfile.raidQuotaMin, balanceProfile.raidQuotaMax + 1);
        lastEvent = "Trade road crisis started: protect caravans through " + mandarinkaCaravanRaidQuota + " Mandarinka raids.";
    }

    private void CompleteVerticalSliceMission()
    {
        verticalSliceStage = VerticalSliceStage.Complete;
        verticalSliceStagesCompleted = 8;
        verticalSliceStageElapsed = 0f;
        ApplyVerticalSliceStage(false);
    }

    private void InitializeVerticalSliceMissionUi()
    {
        if (verticalSliceMissionPanelObject != null || strategicCanvas == null)
            return;
        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Vertical_Slice_Mission_Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -136f), new Vector2(390f, 106f), UiGlassDeep);
        verticalSliceMissionPanelObject = panel.gameObject;
        verticalSliceMissionTitleText = CreateText(panel, "Mission_Stage", "", 12, FontStyle.Bold, new Vector2(12f, -6f), new Vector2(366f, 18f), UiGold);
        verticalSliceMissionObjectiveText = CreateText(panel, "Mission_Objective", "", 10, FontStyle.Bold, new Vector2(12f, -25f), new Vector2(366f, 28f), UiTextBody);
        verticalSliceMissionProgressText = CreateText(panel, "Mission_Progress", "", 9, FontStyle.Normal, new Vector2(12f, -53f), new Vector2(366f, 15f), UiBlueSoft);
        verticalSlicePrimaryButton = CreateButton(panel, "Mission_Primary_Action", "ACT", new Vector2(12f, -73f), new Vector2(236f, 24f), HandleVerticalSlicePrimaryAction);
        verticalSliceHelpButton = CreateButton(panel, "Mission_Help", "HELP", new Vector2(256f, -73f), new Vector2(122f, 24f), ShowVerticalSliceHelp);
        UpdateVerticalSliceMissionUi();
    }

    private void UpdateVerticalSliceMissionUi()
    {
        if (verticalSliceMissionPanelObject == null)
            return;
        bool visible = !hudHidden && gunnerSide == 0 && !guidedMissileActive &&
                       !verticalSliceSupersededByRetaliation;
        if (verticalSliceMissionPanelObject.activeSelf != visible)
            verticalSliceMissionPanelObject.SetActive(visible);
        if (!visible)
            return;
        int displayedStep = verticalSliceStage == VerticalSliceStage.Complete ? 8 : Mathf.Min(8, (int)verticalSliceStage + 1);
        verticalSliceMissionTitleText.text = "MISSION STEP " + displayedStep + "/8 // " + GetVerticalSliceStageName();
        verticalSliceMissionObjectiveText.text = missionObjective;
        verticalSliceMissionProgressText.text = GetVerticalSliceProgressText() + GetReleasePacingSuffix();
        SetButtonText(verticalSlicePrimaryButton, GetVerticalSlicePrimaryActionLabel());
        SetButtonText(verticalSliceHelpButton, "HELP / COMMS");
    }

    private string GetVerticalSliceStageName()
    {
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource: return "EXTRACTION";
            case VerticalSliceStage.BuildAerodrome: return "FORWARD AIRBASE";
            case VerticalSliceStage.LaunchAirWing: return "FIRST SORTIE";
            case VerticalSliceStage.TradeSettlement: return "DESERT TRADE";
            case VerticalSliceStage.PowerSettlement: return "MIRROR GRID";
            case VerticalSliceStage.FieldThoth: return "MOBILE CARRIER";
            case VerticalSliceStage.SurviveCaravanRaids: return "TRADE ROAD CRISIS";
            case VerticalSliceStage.DestroyMandarinka: return "PALACE ASSAULT";
            default: return "COMPLETE";
        }
    }

    private string GetVerticalSliceProgressText()
    {
        string threat = GetVerticalSliceThreatText();
        if (!string.IsNullOrEmpty(threat))
            return threat;
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource: return "Controlled nodes " + CountControlledResourceNodes() + "/1";
            case VerticalSliceStage.BuildAerodrome: return "Aerodromes " + CountAerodromes() + "/1 | builder required";
            case VerticalSliceStage.LaunchAirWing: return "Operational wings " + CountOperationalMissionAirWings() + "/1";
            case VerticalSliceStage.TradeSettlement: return "Move within 220m | trade cost 35 sand / 25 gold / 8 wind";
            case VerticalSliceStage.PowerSettlement: return "Controlled wind node required | grid cost 60 gold / 90 wind";
            case VerticalSliceStage.FieldThoth: return thothCarrierReady ? "Carrier online" : "Carrier funding granted | one Thoth limit";
            case VerticalSliceStage.SurviveCaravanRaids: return "Raids " + mandarinkaCaravanRaidCount + "/" + Mathf.Max(2, mandarinkaCaravanRaidQuota) + " | caravans " + tradeCaravans.Count;
            case VerticalSliceStage.DestroyMandarinka: return mandarinkaFortressEnemy == null ? "Fortress target unavailable" : "Fortress hull " + Mathf.RoundToInt(mandarinkaFortressEnemy.health) + " | shield " + Mathf.RoundToInt(mandarinkaFortressShield);
            default: return "All vertical-slice objectives complete.";
        }
    }

    private int CountOperationalMissionAirWings()
    {
        int count = 0;
        for (int i = 0; i < aviationWings.Count; i++)
        {
            AirWingState wing = aviationWings[i];
            if (wing != null && wing.squad != null && wing.role != AirRole.LongRangePlatform && wing.state != AirUnitState.HangarReady && wing.state != AirUnitState.Destroyed)
                count++;
        }
        return count;
    }

    private string GetVerticalSliceThreatText()
    {
        if (neutralRetaliationPhase == NeutralRetaliationPhase.Warning)
            return "CRISIS: Black Elemental retaliation in " + Mathf.CeilToInt(neutralRetaliationTimer) + "s";
        if (nashornEnemy != null && nashornEnemy.health > 0f)
            return "CRISIS: Nashorn hull " + Mathf.RoundToInt(nashornEnemy.health) + " | shots " + nashornShotsFired + "/3";
        return string.Empty;
    }

    private string GetVerticalSlicePrimaryActionLabel()
    {
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource: return "ENTER COMMAND MODE";
            case VerticalSliceStage.BuildAerodrome: return "PLACE AERODROME";
            case VerticalSliceStage.LaunchAirWing: return "BUILD COMBAT WING";
            case VerticalSliceStage.TradeSettlement: return "TRADE WITH NEAREST";
            case VerticalSliceStage.PowerSettlement: return "BUILD / REPAIR GRID";
            case VerticalSliceStage.FieldThoth: return "BUILD THOTH'S EMBRACE";
            case VerticalSliceStage.SurviveCaravanRaids: return "START / TRACK CARAVAN";
            case VerticalSliceStage.DestroyMandarinka: return "ASSAULT GUIDANCE";
            default: return "MISSION COMPLETE";
        }
    }

    private void HandleVerticalSlicePrimaryAction()
    {
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource:
                SetCommandCursorMode(true);
                lastEvent = "Select the pyramid or a builder and move to a marked extraction site.";
                break;
            case VerticalSliceStage.BuildAerodrome:
                SetCommandCursorMode(true);
                BeginStructureBlueprint(StructureKind.Aerodrome);
                break;
            case VerticalSliceStage.LaunchAirWing:
                BuildCombatFlyerSquad();
                break;
            case VerticalSliceStage.TradeSettlement:
                TryTradeWithNearestSettlement();
                break;
            case VerticalSliceStage.PowerSettlement:
                TryBuildOrRepairEnergyNetwork();
                break;
            case VerticalSliceStage.FieldThoth:
                BuildThothEmbrace();
                break;
            case VerticalSliceStage.SurviveCaravanRaids:
                PrepareVerticalSliceCaravanCrisis();
                break;
            case VerticalSliceStage.DestroyMandarinka:
                SetCommandCursorMode(true);
                lastEvent = "Combine Thoth sorties, missiles, Q/E howitzers, and allied pressure against the mobile fortress.";
                break;
        }
        UpdateVerticalSliceMissionUi();
    }

    private void ShowVerticalSliceHelp()
    {
        string help;
        switch (verticalSliceStage)
        {
            case VerticalSliceStage.SecureResource: help = "PYRAMID SPIRIT: Resource sites have tall silhouettes. Hold the pyramid or a harvester near one until it turns controlled."; break;
            case VerticalSliceStage.BuildAerodrome: help = "PYRAMID SPIRIT: Build a Solar Builder Truck, select Aerodrome, place its blueprint, then order the builder to it."; break;
            case VerticalSliceStage.LaunchAirWing: help = "PYRAMID SPIRIT: Select the aerodrome before production to launch there; otherwise the pyramid hangar is used."; break;
            case VerticalSliceStage.TradeSettlement: help = "BLUE ELEMENTALS: Move within 220 metres. Use this button or press T in command mode."; break;
            case VerticalSliceStage.PowerSettlement: help = "PYRAMID SPIRIT: Capture wind, approach a settlement, then use this button or Y. Three relays will be built automatically."; break;
            case VerticalSliceStage.FieldThoth: help = "PYRAMID SPIRIT: The alliance has funded the carrier. Build Thoth, select it, then produce two Combat and two Heavy Flyers."; break;
            case VerticalSliceStage.SurviveCaravanRaids: help = "BLUE ELEMENTALS: Mandarinka attacks two to five times. Protect cargo until Black Elemental artillery forces her away."; break;
            case VerticalSliceStage.DestroyMandarinka: help = "SEBEK-NU-ANKHA: Strip the jade shield, intercept siege shells, then concentrate every weapon on the palace hull."; break;
            default: help = "PYRAMID SPIRIT: The vertical slice is complete."; break;
        }
        SetMissionDialogue(help, 8f);
        lastEvent = help;
    }
}