using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private NeutralSettlement horusDiplomacySettlement;
    private SettlementDevelopmentState horusDiplomacyState;
    private bool horusDiplomacyRadioPlayed;
    private bool horusDiplomacyDiscussionComplete;
    private bool horusDiplomacyReleased;
    private GameObject horusDiplomacyPanelObject;
    private Text horusDiplomacyTitleText;
    private Text horusDiplomacyBodyText;
    private StrategicCanvasButton horusReleaseButton;
    private StrategicCanvasButton horusSelectButton;
    private StrategicCanvasButton horusHelpButton;
    private StrategicCanvasButton horusTradeButton;
    private StrategicCanvasButton horusTalkButton;

    private void UpdateHorusDiplomacy(float dt)
    {
        if (unifiedDiplomacyEnabled)
            return;
        if (touchOfHorusRevealed || horusDiplomacyReleased)
            return;

        SettlementDevelopmentState nearest = FindNearestSettlementDevelopment(
            battlePyramid != null ? battlePyramid.position : Vector3.zero, 118f);

        if (nearest == null || nearest.settlement == null || nearest.settlement.root == null)
            return;

        if (horusDiplomacySettlement != nearest.settlement)
        {
            horusDiplomacySettlement = nearest.settlement;
            horusDiplomacyState = nearest;
            horusDiplomacyRadioPlayed = false;
            horusDiplomacyDiscussionComplete = false;
            SetHorusDiplomacyPanelVisible(false);
        }

        if (!horusDiplomacyRadioPlayed)
        {
            horusDiplomacyRadioPlayed = true;
            PlaySandRunnerSound(SandRunnerSound.NeutralAlert, nearest.settlement.root.position, 0.8f);
            SetMissionDialogue(
                "SETTLEMENT RADIO: Please, do not come closer with weapons. Our wells are failing. We need help before we can discuss trade.",
                9f);
            lastEvent = nearest.settlement.displayName + " is calling on the pyramid radio.";
            ShowBanner("NEUTRAL RADIO // HELP REQUEST", 4f);
            SetHorusDiplomacyPanelVisible(true);
        }
    }

    private void InitializeHorusDiplomacyUi()
    {
        if (strategicCanvas == null || horusDiplomacyPanelObject != null)
            return;

        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Horus_Diplomacy_Dialogue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-260f, 36f), new Vector2(520f, 236f), UiGlassDeep);
        horusDiplomacyPanelObject = panel.gameObject;
        horusDiplomacyTitleText = CreateText(panel, "Horus_Dialogue_Title",
            "NEUTRAL RADIO // REQUEST FOR HELP", 16, FontStyle.Bold,
            new Vector2(16f, -10f), new Vector2(488f, 26f), UiGold);
        horusDiplomacyBodyText = CreateText(panel, "Horus_Dialogue_Body",
            "", 13, FontStyle.Normal,
            new Vector2(16f, -42f), new Vector2(488f, 72f), UiTextBody);

        horusHelpButton = CreateButton(panel, "Horus_Help",
            "HEAR SEBEK / SEKMET", new Vector2(16f, -126f),
            new Vector2(154f, 34f), HandleHorusReleaseAction);
        horusTradeButton = CreateButton(panel, "Horus_Trade",
            "TRADE", new Vector2(182f, -126f),
            new Vector2(100f, 34f), HandleHorusDiplomacyTrade);
        horusTalkButton = CreateButton(panel, "Horus_Talk",
            "TALK", new Vector2(294f, -126f),
            new Vector2(100f, 34f), HandleHorusDiplomacyTalk);
        CreateText(panel, "Horus_Dialogue_Hint",
            "Help opens the argument about releasing the pyramid spirit.\nAfter release: select Horus, then RMB the settlement to consent.",
            11, FontStyle.Normal, new Vector2(16f, -174f), new Vector2(488f, 48f), UiBlueSoft);

        if (strategicCommandPanelObject != null)
        {
            RectTransform command = strategicCommandPanelObject.GetComponent<RectTransform>();
            horusReleaseButton = CreateButton(command, "Release_Touch_Of_Horus",
                "RELEASE TOUCH OF HORUS", new Vector2(18f, -176f),
                new Vector2(250f, 32f), ReleaseTouchOfHorusForDiplomacy);
            horusSelectButton = CreateButton(command, "Select_Touch_Of_Horus",
                "SELECT HORUS", new Vector2(278f, -176f),
                new Vector2(180f, 32f), SelectReleasedHorus);
        }

        SetHorusDiplomacyPanelVisible(false);
    }

    private void SetHorusDiplomacyPanelVisible(bool visible)
    {
        if (horusDiplomacyPanelObject != null)
            horusDiplomacyPanelObject.SetActive(visible && !hudHidden && gameFlowState == SandRunnersGameFlowState.Playing);
    }

    private void UpdateHorusDiplomacyUi()
    {
        if (strategicCanvas == null)
            return;
        InitializeHorusDiplomacyUi();

        if (horusDiplomacyBodyText != null && horusDiplomacySettlement != null)
        {
            string name = horusDiplomacySettlement.displayName;
            string status = horusDiplomacyDiscussionComplete
                ? "SEBEK: We can release the spirit, but only if the gesture is understood as consent.\nSEKMET: Then make the settlement answer with its own choice."
                : "The settlement asks for help before trade. Sebek wants to answer; Sekhmet warns that Horus is not a decoration.";
            horusDiplomacyBodyText.text = name + "\n\n" + status;
        }

        if (horusHelpButton != null)
            horusHelpButton.label.text = horusDiplomacyDiscussionComplete ? "RELEASE HORUS" : "HEAR SEBEK / SEKMET";

        if (horusSelectButton != null)
            horusSelectButton.button.gameObject.SetActive(horusDiplomacyReleased && touchOfHorus != null && touchOfHorus.root != null);

        if (horusReleaseButton != null)
        {
            horusReleaseButton.button.gameObject.SetActive(horusDiplomacyDiscussionComplete && !horusDiplomacyReleased);
            horusReleaseButton.label.text = horusDiplomacyDiscussionComplete
                ? "RELEASE TOUCH OF HORUS"
                : "DISCUSSION REQUIRED";
        }
    }

    private void HandleHorusReleaseAction()
    {
        if (horusDiplomacyDiscussionComplete)
            ReleaseTouchOfHorusForDiplomacy();
        else
            BeginHorusReleaseDiscussion();
    }

    private void BeginHorusReleaseDiscussion()
    {
        if (horusDiplomacySettlement == null)
            return;

        horusDiplomacyDiscussionComplete = true;
        SetMissionDialogue(
            "SEBEK: They asked for help.\nSEKMET: Release the Touch only if you accept the burden of diplomacy.\nSEBEK: Then let the settlement answer us.",
            11f);
        lastEvent = "The pyramid is debating whether to release Touch of Horus.";
        ShowBanner("SEBEK // SEKMET // RELEASE DECISION", 4f);
        UpdateHorusDiplomacyUi();
    }

    private void HandleHorusDiplomacyTrade()
    {
        if (horusDiplomacyState == null)
            return;
        TryTradeWithNearestSettlement();
        SetMissionDialogue("SEBEK: Trade is a promise made with numbers. The settlement is listening.", 7f);
        lastEvent = "Trade channel opened with " + horusDiplomacySettlement.displayName + ".";
    }

    private void HandleHorusDiplomacyTalk()
    {
        if (horusDiplomacySettlement == null)
            return;
        SetMissionDialogue(
            "SEKMET: The settlement is frightened.\nSEBEK: Then we speak before we build.\nRADIO: We can hear you, pyramid.",
            9f);
        lastEvent = "The settlement remains on the radio.";
    }

    private void ReleaseTouchOfHorusForDiplomacy()
    {
        if (!horusDiplomacyDiscussionComplete || horusDiplomacySettlement == null || horusDiplomacySettlement.root == null)
            return;

        if (touchOfHorus == null)
            SpawnTouchOfHorus();

        touchOfHorusRevealed = true;
        horusDiplomacyReleased = true;
        touchOfHorus.pinnedTarget = null;
        touchOfHorus.hasMoveDestination = false;
        activeHorusRepairRequest = null;
        SelectStrategicTransform(touchOfHorus.root, "Touch of Horus");
        SetHorusDiplomacyPanelVisible(false);
        lastEvent = "Touch of Horus released. Select it and RMB the settlement to request diplomatic consent.";
        ShowBanner("TOUCH OF HORUS RELEASED // RMB SETTLEMENT", 5f);
        SetMissionDialogue("SEBEK: Go, Horus. No repair beam. Ask them whether they will trust us.", 8f);
        UpdateHorusDiplomacyUi();
    }

    private bool TryIssueTouchOfHorusOrderAtScreen(Vector2 screenPosition)
    {
        if (touchOfHorus == null || selectedStrategicTransform != touchOfHorus.root || mainCamera == null)
            return false;
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement == null || settlement.root == null)
                continue;
            Vector3 projected = mainCamera.WorldToScreenPoint(settlement.root.position);
            if (projected.z <= 0f || Vector2.Distance(screenPosition, new Vector2(projected.x, projected.y)) > 240f)
                continue;
            if (TryAcceptHorusDiplomacy(settlement))
                return true;
        }
        return false;
    }

    private void SelectReleasedHorus()
    {
        if (touchOfHorus != null && touchOfHorus.root != null)
        {
            SelectStrategicTransform(touchOfHorus.root, "Touch of Horus");
            lastEvent = "Touch of Horus selected. RMB a settlement to make the diplomatic gesture.";
        }
    }

    private bool TrySelectTouchOfHorusAtScreen(Vector2 screenPosition)
    {
        if (touchOfHorus == null || touchOfHorus.root == null || mainCamera == null)
            return false;
        Vector3 projected = mainCamera.WorldToScreenPoint(touchOfHorus.root.position);
        if (projected.z <= 0f || Vector2.Distance(screenPosition, new Vector2(projected.x, projected.y)) > 220f)
            return false;
        SelectStrategicTransform(touchOfHorus.root, "Touch of Horus");
        lastEvent = "Touch of Horus selected. RMB a settlement to make the diplomatic gesture.";
        return true;
    }

    private bool TryAcceptHorusDiplomacy(NeutralSettlement settlement)
    {
        if (!horusDiplomacyReleased || settlement == null || settlementDevelopment == null)
            return false;
        SettlementDevelopmentState state;
        if (!settlementDevelopment.TryGetValue(settlement, out state) || state == null)
            return false;

        state.playerInfluence = Mathf.Min(100f, state.playerInfluence + 35f);
        state.tradeTrust = Mathf.Min(100f, state.tradeTrust + 25f);
        state.stage = state.playerInfluence >= 35f
            ? SettlementDiplomacyStage.ProtectedSettlement
            : SettlementDiplomacyStage.TradingPartner;
        UpdateSettlementDiplomacy(state);
        touchOfHorus.pinnedTarget = MakeSettlementTarget(settlement);
        lastEvent = settlement.displayName + " accepts the Touch of Horus as a diplomatic gesture.";
        ShowBanner("DIPLOMATIC CONSENT // SETTLEMENT OPEN", 4f);
        SetMissionDialogue(
            "RADIO: We accept the gesture. Trade, repairs and a shared defense channel are open.\nSEKMET: Now the spirit has a purpose.",
            10f);
        return true;
    }
}