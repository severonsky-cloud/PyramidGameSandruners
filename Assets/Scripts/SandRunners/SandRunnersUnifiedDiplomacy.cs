using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal static class SandRunnersDiplomacyRules
{
    internal static float GiftTrust(float amount, float currentTrust)
    {
        float diminishing = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(currentTrust / 100f));
        return amount * 0.32f * diminishing;
    }

    internal static bool CanPurchaseContract(bool allied, int stock, bool settlementAlive)
    {
        return allied && stock > 0 && settlementAlive;
    }
}

public partial class SandRunnersPrototype
{
    private enum DiplomacyTab
    {
        Dialogue,
        Gifts,
        Trade,
        Contracts
    }

    private sealed class FactionContractOffer
    {
        public string label;
        public SandRunnersResourcePrice price;
        public int unitCount;
        public bool grounder;
    }

    private sealed class GradRocketVisual
    {
        public Transform root;
        public Vector3 start;
        public Vector3 target;
        public float age;
        public float duration;
        public float damage;
        public float radius;
    }

    private readonly List<GradRocketVisual> gradRockets = new List<GradRocketVisual>();
    private bool unifiedDiplomacyEnabled;
    private bool unifiedDiplomacyOpen;
    private DiplomacyTab diplomacyTab;
    private SettlementDevelopmentState activeDiplomacyState;
    private ResourceKind diplomacyGiftResource;
    private readonly SandRunnersDiplomacyPanelController diplomacyPanelController = new SandRunnersDiplomacyPanelController();
    private float diplomacyLastPyramidHull;
    private float diplomacyLastSettlementHealth;
    private GameObject unifiedDiplomacyPanelObject;
    private Text diplomacyTitleText;
    private Text diplomacyBodyText;
    private Text diplomacyRelationText;
    private Text diplomacyHintText;
    private StrategicCanvasButton diplomacyDialogueTabButton;
    private StrategicCanvasButton diplomacyGiftsTabButton;
    private StrategicCanvasButton diplomacyTradeTabButton;
    private StrategicCanvasButton diplomacyContractsTabButton;
    private StrategicCanvasButton diplomacyTalkButton;
    private StrategicCanvasButton diplomacyHorusButton;
    private StrategicCanvasButton diplomacyGiftResourceButton;
    private StrategicCanvasButton diplomacyGift25Button;
    private StrategicCanvasButton diplomacyGift50Button;
    private StrategicCanvasButton diplomacyGift100Button;
    private StrategicCanvasButton diplomacyTradeButton;
    private StrategicCanvasButton diplomacyContractButton;
    private StrategicCanvasButton diplomacyCloseButton;
    private int nextContractGroupId = 1;
    private NeutralSettlement diplomacyHintSettlement;

    private void InitializeUnifiedDiplomacy()
    {
        unifiedDiplomacyEnabled = true;
        if (strategicCanvas == null || unifiedDiplomacyPanelObject != null)
            return;

        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Unified_Settlement_Diplomacy",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-340f, 225f), new Vector2(680f, 450f), UiGlassDeep);
        unifiedDiplomacyPanelObject = panel.gameObject;
        diplomacyTitleText = CreateText(panel, "Diplomacy_Title", "SETTLEMENT CHANNEL", 18, FontStyle.Bold,
            new Vector2(18f, -12f), new Vector2(540f, 28f), UiGold);
        diplomacyRelationText = CreateText(panel, "Diplomacy_Relation", "", 11, FontStyle.Bold,
            new Vector2(18f, -44f), new Vector2(640f, 22f), UiBlueSoft);

        diplomacyDialogueTabButton = CreateButton(panel, "Diplomacy_Tab_Dialogue", "DIALOGUE", new Vector2(18f, -76f), new Vector2(145f, 34f), () => SetDiplomacyTab(DiplomacyTab.Dialogue));
        diplomacyGiftsTabButton = CreateButton(panel, "Diplomacy_Tab_Gifts", "GIFTS", new Vector2(173f, -76f), new Vector2(145f, 34f), () => SetDiplomacyTab(DiplomacyTab.Gifts));
        diplomacyTradeTabButton = CreateButton(panel, "Diplomacy_Tab_Trade", "TRADE", new Vector2(328f, -76f), new Vector2(145f, 34f), () => SetDiplomacyTab(DiplomacyTab.Trade));
        diplomacyContractsTabButton = CreateButton(panel, "Diplomacy_Tab_Contracts", "CONTRACTS", new Vector2(483f, -76f), new Vector2(179f, 34f), () => SetDiplomacyTab(DiplomacyTab.Contracts));

        diplomacyBodyText = CreateText(panel, "Diplomacy_Body", "", 13, FontStyle.Normal,
            new Vector2(22f, -126f), new Vector2(636f, 105f), UiTextBody);

        diplomacyTalkButton = CreateButton(panel, "Diplomacy_Talk", "OPEN RADIO DIALOGUE", new Vector2(22f, -252f), new Vector2(250f, 38f), TalkToActiveSettlement);
        diplomacyHorusButton = CreateButton(panel, "Diplomacy_Horus", "DISCUSS TOUCH OF HORUS", new Vector2(286f, -252f), new Vector2(300f, 38f), BeginUnifiedHorusDiscussion);

        diplomacyGiftResourceButton = CreateButton(panel, "Diplomacy_Gift_Resource", "RESOURCE: SAND", new Vector2(22f, -252f), new Vector2(190f, 38f), CycleDiplomacyGiftResource);
        diplomacyGift25Button = CreateButton(panel, "Diplomacy_Gift_25", "GIFT 25", new Vector2(224f, -252f), new Vector2(120f, 38f), () => GiftActiveSettlement(25f));
        diplomacyGift50Button = CreateButton(panel, "Diplomacy_Gift_50", "GIFT 50", new Vector2(356f, -252f), new Vector2(120f, 38f), () => GiftActiveSettlement(50f));
        diplomacyGift100Button = CreateButton(panel, "Diplomacy_Gift_100", "GIFT 100", new Vector2(488f, -252f), new Vector2(120f, 38f), () => GiftActiveSettlement(100f));

        diplomacyTradeButton = CreateButton(panel, "Diplomacy_Trade_Develop", "DEVELOPMENT TRADE // 35 SAND 25 GOLD 8 WIND",
            new Vector2(22f, -252f), new Vector2(520f, 42f), CompleteUnifiedSettlementTrade);
        diplomacyContractButton = CreateButton(panel, "Diplomacy_Special_Contract", "SPECIAL CONTRACT",
            new Vector2(22f, -252f), new Vector2(600f, 48f), PurchaseActiveFactionContract);

        diplomacyHintText = CreateText(panel, "Diplomacy_Hint",
            "T closes the channel. The desert continues at 15% speed; taking damage terminates negotiations.",
            10, FontStyle.Normal, new Vector2(22f, -330f), new Vector2(620f, 42f), new Color(0.72f, 0.82f, 0.92f, 1f));
        diplomacyCloseButton = CreateButton(panel, "Diplomacy_Close", "CLOSE CHANNEL", new Vector2(442f, -392f), new Vector2(200f, 38f), () => CloseUnifiedDiplomacy(true));
        unifiedDiplomacyPanelObject.SetActive(false);
        SetDiplomacyTab(DiplomacyTab.Dialogue);
    }

    private void UpdateUnifiedDiplomacy(float dt)
    {
        if (!unifiedDiplomacyEnabled)
            return;
        if (unifiedDiplomacyPanelObject == null)
            InitializeUnifiedDiplomacy();

        UpdateFactionContractRestocks(dt);
        SettlementDevelopmentState nearest = battlePyramid != null ? FindNearestSettlementDevelopment(battlePyramid.position, SandRunnersDiplomacyPanelState.OpenRadius) : null;
        if (nearest != null && nearest.settlement != null && diplomacyHintSettlement != nearest.settlement)
        {
            diplomacyHintSettlement = nearest.settlement;
            ShowBanner("DIPLOMACY RANGE // T: OPEN " + nearest.settlement.displayName, 3.2f);
            lastEvent = "Press T to open dialogue, trade, alliance progress and faction contracts with " +
                nearest.settlement.displayName + ".";
        }
        else if (nearest == null)
            diplomacyHintSettlement = null;

        if (WasKeyPressedThisFrame(Key.T))
        {
            if (unifiedDiplomacyOpen)
                CloseUnifiedDiplomacy(true);
            else if (nearest != null)
                OpenUnifiedDiplomacy(nearest);
            else
                ShowBanner("NO SETTLEMENT IN DIPLOMACY RANGE", 2.4f);
        }

        if (!unifiedDiplomacyOpen)
            return;

        if (activeDiplomacyState == null || activeDiplomacyState.settlement == null ||
            activeDiplomacyState.settlement.root == null || activeDiplomacyState.settlement.health <= 0f ||
            IsSettlementUnavailableToPlayer(activeDiplomacyState) ||
            battlePyramid == null || FlatDistance(battlePyramid.position, activeDiplomacyState.settlement.root.position) > SandRunnersDiplomacyPanelState.CloseRadius ||
            gameFlowState != SandRunnersGameFlowState.Playing || hudHidden)
        {
            CloseUnifiedDiplomacy(false);
            return;
        }

        if (pyramidHull < diplomacyLastPyramidHull - 0.01f ||
            activeDiplomacyState.settlement.health < diplomacyLastSettlementHealth - 0.01f)
        {
            ShowBanner("NEGOTIATIONS TERMINATED // UNDER ATTACK", 2.8f);
            CloseUnifiedDiplomacy(false);
            return;
        }

        diplomacyLastPyramidHull = pyramidHull;
        diplomacyLastSettlementHealth = activeDiplomacyState.settlement.health;
        RefreshUnifiedDiplomacyUi();
    }

    private void OpenUnifiedDiplomacy(SettlementDevelopmentState state)
    {
        if (state == null || state.settlement == null || state.settlement.health <= 0f ||
            IsSettlementUnavailableToPlayer(state))
            return;
        activeDiplomacyState = state;
        unifiedDiplomacyOpen = true;
        diplomacyPanelController.Open(Time.timeScale);
        Time.timeScale = Mathf.Clamp(balanceProfile.diplomacyTimeScale, 0.05f, 1f);
        diplomacyLastPyramidHull = pyramidHull;
        diplomacyLastSettlementHealth = state.settlement.health;
        if (unifiedDiplomacyPanelObject != null)
            unifiedDiplomacyPanelObject.SetActive(true);
        SetDiplomacyTab(DiplomacyTab.Dialogue);
        PlaySandRunnerSound(SandRunnerSound.UiConfirm, state.settlement.root.position, 0.65f);
    }

    private void CloseUnifiedDiplomacy(bool playSound)
    {
        if (!unifiedDiplomacyOpen && !diplomacyPanelController.IsOpen)
            return;
        unifiedDiplomacyOpen = false;
        if (unifiedDiplomacyPanelObject != null)
            unifiedDiplomacyPanelObject.SetActive(false);
        Time.timeScale = diplomacyPanelController.Close(Time.timeScale);
        if (playSound)
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.45f);
        activeDiplomacyState = null;
    }

    private void SetDiplomacyTab(DiplomacyTab tab)
    {
        diplomacyTab = tab;
        RefreshUnifiedDiplomacyUi();
    }

    private void RefreshUnifiedDiplomacyUi()
    {
        bool hasState = activeDiplomacyState != null && activeDiplomacyState.settlement != null;
        if (diplomacyTitleText == null)
            return;

        string settlementName = hasState ? activeDiplomacyState.settlement.displayName : "SETTLEMENT CHANNEL";
        diplomacyTitleText.text = settlementName;
        SandRunnersDiplomacyPanelState panelState = hasState ? BuildDiplomacyPanelState(activeDiplomacyState, null) : null;
        diplomacyRelationText.text = hasState
            ? panelState.Stage + "  //  TRUST " + Mathf.RoundToInt(activeDiplomacyState.tradeTrust) +
              "  //  INFLUENCE " + Mathf.RoundToInt(activeDiplomacyState.playerInfluence) +
              "  //  DEALS " + activeDiplomacyState.playerDeals +
              "  //  " + (activeDiplomacyState.horusPower ? "HORUS GRID" :
                  activeDiplomacyState.network != null && activeDiplomacyState.network.online ? "POWERED" : "NO POWER")
            : "NO ACTIVE SETTLEMENT";

        SetDiplomacyButtonVisible(diplomacyTalkButton, diplomacyTab == DiplomacyTab.Dialogue);
        SetDiplomacyButtonVisible(diplomacyHorusButton, diplomacyTab == DiplomacyTab.Dialogue);
        SetDiplomacyButtonVisible(diplomacyGiftResourceButton, diplomacyTab == DiplomacyTab.Gifts);
        SetDiplomacyButtonVisible(diplomacyGift25Button, diplomacyTab == DiplomacyTab.Gifts);
        SetDiplomacyButtonVisible(diplomacyGift50Button, diplomacyTab == DiplomacyTab.Gifts);
        SetDiplomacyButtonVisible(diplomacyGift100Button, diplomacyTab == DiplomacyTab.Gifts);
        SetDiplomacyButtonVisible(diplomacyTradeButton, diplomacyTab == DiplomacyTab.Trade);
        SetDiplomacyButtonVisible(diplomacyContractButton, diplomacyTab == DiplomacyTab.Contracts);

        if (!hasState)
        {
            diplomacyBodyText.text = "No active settlement.";
            return;
        }

        if (diplomacyTab == DiplomacyTab.Dialogue)
            diplomacyBodyText.text = "A single radio channel now carries aid requests, political consent, settlement news and trade. Speak before you build.";
        else if (diplomacyTab == DiplomacyTab.Gifts)
        {
            diplomacyBodyText.text = "Gifts improve trust and influence. Repeating gifts becomes less effective as trust rises.";
            SetButtonText(diplomacyGiftResourceButton, "RESOURCE: " + diplomacyGiftResource.ToString().ToUpperInvariant());
        }
        else if (diplomacyTab == DiplomacyTab.Trade)
            diplomacyBodyText.text = "DEVELOPMENT TRADE // 35 SAND / 25 GOLD / 8 WIND\n" + panelState.NextStageRequirement +
                "\nInteraction: " + Mathf.RoundToInt(panelState.Distance) + "m / " + Mathf.RoundToInt(SandRunnersDiplomacyPanelState.OpenRadius) + "m open; panel holds until " + Mathf.RoundToInt(SandRunnersDiplomacyPanelState.CloseRadius) + "m.";
        else
        {
            FactionContractOffer offer = GetFactionContractOffer(activeDiplomacyState);
            panelState = BuildDiplomacyPanelState(activeDiplomacyState, offer);
            diplomacyBodyText.text = offer == null
                ? "This faction has no military contract in the current slice."
                : "Purchased forces remain yours if this settlement is later occupied or destroyed.\n" +
                  panelState.NextStageRequirement + "\n" + panelState.ContractPrice + " // " + panelState.StockStatus + "\n" + panelState.BlockReason;
            if (offer != null)
            {
                SetButtonText(diplomacyContractButton, offer.label + " x" + offer.unitCount + "\n" +
                    panelState.ContractPrice + " // " + panelState.StockStatus + "\n" + panelState.BlockReason);
                SetDiplomacyButtonAvailable(diplomacyContractButton, panelState.CanPurchase);
            }
        }

        SetButtonText(diplomacyDialogueTabButton, diplomacyTab == DiplomacyTab.Dialogue ? "[ DIALOGUE ]" : "DIALOGUE");
        SetButtonText(diplomacyGiftsTabButton, diplomacyTab == DiplomacyTab.Gifts ? "[ GIFTS ]" : "GIFTS");
        SetButtonText(diplomacyTradeTabButton, diplomacyTab == DiplomacyTab.Trade ? "[ TRADE ]" : "TRADE");
        SetButtonText(diplomacyContractsTabButton, diplomacyTab == DiplomacyTab.Contracts ? "[ CONTRACTS ]" : "CONTRACTS");
    }

    private void SetDiplomacyButtonVisible(StrategicCanvasButton button, bool visible)
    {
        if (button != null && button.button != null)
            button.button.gameObject.SetActive(visible);
    }

    private void SetDiplomacyButtonAvailable(StrategicCanvasButton button, bool available)
    {
        if (button != null && button.button != null)
            button.button.interactable = available;
    }

    private SandRunnersDiplomacyPanelState BuildDiplomacyPanelState(SettlementDevelopmentState state, FactionContractOffer offer)
    {
        float distance = battlePyramid != null && state != null && state.settlement != null && state.settlement.root != null
            ? FlatDistance(battlePyramid.position, state.settlement.root.position) : float.MaxValue;
        bool powerOnline = state != null && (state.horusPower || (state.network != null && state.network.online));
        bool alive = state != null && state.settlement != null && state.settlement.health > 0f;
        return diplomacyPanelController.BuildState(distance, state != null ? state.playerDeals : 0,
            state != null ? state.tradeTrust : 0f, state != null ? state.playerInfluence : 0f, powerOnline,
            state != null && state.stage == SettlementDiplomacyStage.AlliedSettlement, alive,
            state == null || IsSettlementUnavailableToPlayer(state), state != null ? state.contractStock : 0,
            state != null ? state.contractRestockTimer : 0f, sand, gold, wind,
            offer != null ? offer.price.sand : 0f, offer != null ? offer.price.gold : 0f, offer != null ? offer.price.wind : 0f);
    }

    private void TalkToActiveSettlement()
    {
        if (activeDiplomacyState == null || activeDiplomacyState.settlement == null)
            return;
        SetMissionDialogue(activeDiplomacyState.settlement.displayName + ": We hear the pyramid. Bring honest trade, power and protection, and we will answer with more than words.", 9f);
        activeDiplomacyState.playerInfluence = Mathf.Min(100f, activeDiplomacyState.playerInfluence + 2f);
        PlaySandRunnerSound(SandRunnerSound.NeutralAlert, activeDiplomacyState.settlement.root.position, 0.55f);
    }

    private void BeginUnifiedHorusDiscussion()
    {
        if (activeDiplomacyState == null)
            return;
        horusDiplomacySettlement = activeDiplomacyState.settlement;
        horusDiplomacyState = activeDiplomacyState;
        BeginHorusReleaseDiscussion();
        RefreshUnifiedDiplomacyUi();
    }

    private void CycleDiplomacyGiftResource()
    {
        diplomacyGiftResource = (ResourceKind)(((int)diplomacyGiftResource + 1) % 3);
        RefreshUnifiedDiplomacyUi();
    }

    private void GiftActiveSettlement(float amount)
    {
        if (activeDiplomacyState == null)
            return;

        float sandCost = diplomacyGiftResource == ResourceKind.Sand ? amount : 0f;
        float goldCost = diplomacyGiftResource == ResourceKind.Gold ? amount : 0f;
        float windCost = diplomacyGiftResource == ResourceKind.Wind ? amount : 0f;
        if (!TrySpendResources(sandCost, goldCost, windCost, "settlement gift"))
            return;

        float trust = SandRunnersDiplomacyRules.GiftTrust(amount, activeDiplomacyState.tradeTrust);
        activeDiplomacyState.tradeTrust = Mathf.Min(100f, activeDiplomacyState.tradeTrust + trust);
        activeDiplomacyState.playerInfluence = Mathf.Min(100f, activeDiplomacyState.playerInfluence + trust * 0.65f);
        UpdateSettlementDiplomacy(activeDiplomacyState);
        PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, activeDiplomacyState.settlement.root.position, 0.65f);
        lastEvent = activeDiplomacyState.settlement.displayName + " accepted a gift of " + amount + " " + diplomacyGiftResource + ".";
        RefreshUnifiedDiplomacyUi();
    }

    private void CompleteUnifiedSettlementTrade()
    {
        if (activeDiplomacyState == null)
            return;
        if (!TrySpendResources(35f, 25f, 8f, "neutral trade contract"))
            return;
        CompleteSettlementTrade(activeDiplomacyState, true);
        RefreshUnifiedDiplomacyUi();
    }

    private FactionContractOffer GetFactionContractOffer(SettlementDevelopmentState state)
    {
        if (state == null || state.settlement == null)
            return null;

        if (state.settlement.kind == NeutralFactionKind.BlueTraders)
        {
            return new FactionContractOffer
            {
                label = "BLUE ELEMENTAL WALKERS",
                price = balanceProfile.elementalWalkerContract,
                unitCount = 3,
                grounder = false
            };
        }
        if (state.settlement.kind == NeutralFactionKind.Grounders)
        {
            return new FactionContractOffer
            {
                label = "GROUNDER GRAD BATTERY",
                price = balanceProfile.grounderGradContract,
                unitCount = 5,
                grounder = true
            };
        }
        return null;
    }

    private void PurchaseActiveFactionContract()
    {
        FactionContractOffer offer = GetFactionContractOffer(activeDiplomacyState);
        if (offer == null || activeDiplomacyState == null)
            return;

        SandRunnersDiplomacyPanelState panelState = BuildDiplomacyPanelState(activeDiplomacyState, offer);
        if (!panelState.CanPurchase)
        {
            ShowBanner(panelState.BlockReason, 2.6f);
            return;
        }

        if (!TrySpendResources(offer.price.sand, offer.price.gold, offer.price.wind, offer.label))
            return;

        activeDiplomacyState.contractStock = 0;
        activeDiplomacyState.contractRestockTimer = balanceProfile.factionContractRestockSeconds;
        if (offer.grounder)
            SpawnGrounderGradContract(activeDiplomacyState);
        else
            SpawnBlueElementalWalkerContract(activeDiplomacyState);
        PlaySandRunnerSound(SandRunnerSound.ProductionComplete, activeDiplomacyState.settlement.root.position, 0.9f);
        ShowBanner(offer.label + " DEPLOYED", 3.5f);
        RefreshUnifiedDiplomacyUi();
    }

    private void UpdateFactionContractRestocks(float dt)
    {
        foreach (KeyValuePair<NeutralSettlement, SettlementDevelopmentState> pair in settlementDevelopment)
        {
            SettlementDevelopmentState state = pair.Value;
            if (state == null || state.contractStock > 0 || state.settlement == null || state.settlement.health <= 0f ||
                IsSettlementUnavailableToPlayer(state) || state.stage != SettlementDiplomacyStage.AlliedSettlement)
                continue;
            state.contractRestockTimer -= dt;
            if (state.contractRestockTimer <= 0f)
            {
                state.contractStock = 1;
                state.contractRestockTimer = 0f;
                PushLivingWorldEvent(LivingWorldEventType.Trade, state.settlement.displayName + " special contract restocked.", 8f, false);
            }
        }
    }

    private Vector3 GetContractSpawnPosition(SettlementDevelopmentState state, int index, int count, float spacing)
    {
        Transform origin = state != null && state.settlement != null ? state.settlement.root : battlePyramid;
        float center = (count - 1) * 0.5f;
        Vector3 position = origin.position - origin.forward * 18f + origin.right * ((index - center) * spacing);
        position.y = GetPlayableGroundHeight(position) + 0.35f;
        return position;
    }

    private void SpawnBlueElementalWalkerContract(SettlementDevelopmentState state)
    {
        UnitSquad squad = CreateSquad("Blue Elemental Walker Cohort x3", false);
        rtsAssemblingSquad = squad;
        int groupId = nextContractGroupId++;
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = GetContractSpawnPosition(state, i, 3, 7f);
            RunnerUnit walker = CreateVehicleUnit("Blue_Elemental_Experimental_Walker_" + i, position,
                state.settlement.root.rotation, new Vector3(1.45f, 1.2f, 1.55f), traderBlueMaterial,
                340f, 6.6f, 48f, 92f, "Blue Elemental Experimental Walker");
            walker.isElementalWalker = true;
            walker.contractGroupId = groupId;
            CreateBox(walker.transform, "Walker_Leg_L", new Vector3(-0.95f, 1.45f, 0f), Quaternion.identity, new Vector3(0.55f, 2.5f, 0.75f), elementalBlackMaterial, true);
            CreateBox(walker.transform, "Walker_Leg_R", new Vector3(0.95f, 1.45f, 0f), Quaternion.identity, new Vector3(0.55f, 2.5f, 0.75f), elementalBlackMaterial, true);
            CreateCylinder(walker.transform, "Walker_Beam_Cannon", new Vector3(-1.15f, 3.2f, 1.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 1.8f, 0.22f), traderBlueMaterial);
            CreateCylinder(walker.transform, "Walker_Machine_Gun", new Vector3(1.15f, 3f, 1.35f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 1.35f, 0.12f), elementalBlackMaterial);
            CreatePointLight(walker.transform, "Walker_Blue_Core", new Vector3(0f, 3.1f, 0.8f), new Color(0.08f, 0.4f, 1f, 1f), 0.8f, 10f);
        }
        rtsAssemblingSquad = null;
        SelectOnlySquad(squad);
    }

    private void SpawnGrounderGradContract(SettlementDevelopmentState state)
    {
        UnitSquad squad = CreateSquad("Grounder Grad Battery x5", false);
        rtsAssemblingSquad = squad;
        int groupId = nextContractGroupId++;
        for (int i = 0; i < 5; i++)
        {
            Vector3 position = GetContractSpawnPosition(state, i, 5, 5.5f);
            RunnerUnit grad = CreateVehicleUnit("Grounder_Grad_MLRS_" + i, position,
                state.settlement.root.rotation, new Vector3(1.3f, 0.7f, 1.9f), grounderShellMaterial,
                150f, 7.2f, 0f, 125f, "Grounder Grad MLRS");
            grad.isGradLauncher = true;
            grad.contractGroupId = groupId;
            CreateBox(grad.transform, "Grad_Rocket_Rack", new Vector3(0f, 1.65f, 0.4f), Quaternion.Euler(-18f, 0f, 0f), new Vector3(2.1f, 0.8f, 2.5f), elementalBlackMaterial, true);
            for (int tube = 0; tube < 4; tube++)
            {
                float x = (tube - 1.5f) * 0.42f;
                CreateCylinder(grad.transform, "Grad_Tube_" + tube, new Vector3(x, 1.8f, 1.5f), Quaternion.Euler(72f, 0f, 0f), new Vector3(0.13f, 1.45f, 0.13f), grounderShellMaterial);
            }
        }
        rtsAssemblingSquad = null;
        SelectOnlySquad(squad);
    }

    private void UpdateSpecialContractUnits(float dt)
    {
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner == null || runner.transform == null || runner.health <= 0f)
                continue;
            runner.secondaryFireCooldown -= dt;
            if (runner.isElementalWalker)
                UpdateElementalWalkerSecondary(runner);
            else if (runner.isGradLauncher)
                UpdateGradLauncher(runner);
        }
        UpdateGradRockets(dt);
    }

    private void UpdateElementalWalkerSecondary(RunnerUnit runner)
    {
        if (runner.secondaryFireCooldown > 0f)
            return;
        EnemyUnit target = FindNearestEnemy(runner.transform.position, 48f);
        if (target == null || target.transform == null)
            return;
        runner.secondaryFireCooldown = 0.25f;
        target.health -= 7f;
        CreateWeaponTracer(runner.transform.position + new Vector3(1.1f, 3f, 0.8f), target.transform.position + Vector3.up,
            new Color(0.18f, 0.62f, 1f, 1f), 0.045f, 0.11f);
    }

    private void UpdateGradLauncher(RunnerUnit runner)
    {
        if (runner.secondaryFireCooldown > 0f)
            return;
        EnemyUnit target = runner.squad != null && runner.squad.attackTarget != null
            ? runner.squad.attackTarget
            : FindNearestEnemy(runner.transform.position, 125f);
        if (target == null || target.transform == null)
            return;
        float distance = FlatDistance(runner.transform.position, target.transform.position);
        if (distance < 35f || distance > 125f)
            return;

        runner.secondaryFireCooldown = 9f;
        for (int rocket = 0; rocket < 4; rocket++)
        {
            Vector3 scatter = new Vector3((rocket - 1.5f) * 2.2f, 0f, (rocket % 2 == 0 ? -1f : 1f) * 2.4f);
            SpawnGradRocket(runner.transform.position + Vector3.up * 2f, target.transform.position + scatter, rocket * 0.1f);
        }
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, runner.transform.position, 0.68f);
    }

    private void SpawnGradRocket(Vector3 start, Vector3 target, float delay)
    {
        GameObject rocket = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rocket.name = "Grounder_Grad_Rocket";
        rocket.transform.position = start;
        rocket.transform.localScale = new Vector3(0.14f, 0.65f, 0.14f);
        Collider collider = rocket.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = rocket.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = grounderShellMaterial;
        AttachProjectileTrail(rocket, new Color(1f, 0.42f, 0.12f, 1f), 0.16f, 0.65f);

        GradRocketVisual visual = new GradRocketVisual();
        visual.root = rocket.transform;
        visual.start = start;
        visual.target = target;
        visual.age = -delay;
        visual.duration = 1.65f;
        visual.damage = 18f;
        visual.radius = 5.5f;
        gradRockets.Add(visual);
    }

    private void UpdateGradRockets(float dt)
    {
        for (int i = gradRockets.Count - 1; i >= 0; i--)
        {
            GradRocketVisual rocket = gradRockets[i];
            if (rocket == null || rocket.root == null)
            {
                gradRockets.RemoveAt(i);
                continue;
            }
            rocket.age += dt;
            if (rocket.age < 0f)
                continue;
            float t = Mathf.Clamp01(rocket.age / rocket.duration);
            Vector3 position = Vector3.Lerp(rocket.start, rocket.target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 28f;
            Vector3 previous = rocket.root.position;
            rocket.root.position = position;
            Vector3 direction = position - previous;
            if (direction.sqrMagnitude > 0.001f)
                rocket.root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            if (t >= 1f)
            {
                CreateExplosion(rocket.target, rocket.radius, rocket.damage, false);
                Destroy(rocket.root.gameObject);
                gradRockets.RemoveAt(i);
            }
        }
    }
}
