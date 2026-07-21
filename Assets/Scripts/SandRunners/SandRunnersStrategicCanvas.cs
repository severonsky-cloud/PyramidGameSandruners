using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private sealed class StrategicCanvasButton
    {
        public Button button;
        public Text label;
    }

    private sealed class SalvageMarkerUi
    {
        public GameObject root;
        public RectTransform rect;
        public Text text;
    }

    private Canvas strategicCanvas;
    private Font strategicFont;
    private Text strategicStatusText;
    private Text strategicEnemyText;
    private Text strategicSelectedTitleText;
    private Text strategicSelectedStatsText;
    private Text strategicEventText;
    private Text strategicModeText;
    private Text strategicApexText;
    private Text strategicArmyText;
    private Text strategicProductionText;
    private Text strategicMapText;
    private GameObject strategicStatusPanelObject;
    private GameObject strategicModePanelObject;
    private GameObject strategicEnemyPanelObject;
    private GameObject strategicSelectionPanelObject;
    private GameObject strategicArmyPanelObject;
    private GameObject strategicCommandPanelObject;
    private StrategicCanvasButton rosterFlyersButton;
    private StrategicCanvasButton rosterScarabButton;
    private StrategicCanvasButton rosterBuildersButton;
    private StrategicCanvasButton rosterTurretsButton;
    private StrategicCanvasButton rosterHeavyButton;
    private StrategicCanvasButton rosterSpecialButton;
    private StrategicCanvasButton apexButton;
    private StrategicCanvasButton cruiseButton;
    private StrategicCanvasButton sunCoreButton;
    private StrategicCanvasButton anubisManualButton;
    private StrategicCanvasButton doctrineEscortButton;
    private StrategicCanvasButton doctrineHoldButton;
    private StrategicCanvasButton doctrineSearchButton;
    private Transform selectedStrategicTransform;
    private string selectedStrategicName;
    private Material strategicSelectionMaterial;
    private LineRenderer strategicSelectionRing;
    private bool strategicCanvasReady;
    private bool apexUiCharging;
    private Image strategicHullBarFill;
    private Image strategicApexBarFill;
    private GameObject strategicSalvagePanelObject;
    private Text strategicSalvageStatusText;
    private readonly List<SalvageMarkerUi> strategicSalvageMarkers = new List<SalvageMarkerUi>(6);
    private readonly List<SalvageField> strategicSalvageMarkerFields = new List<SalvageField>(6);
    private float strategicSalvageUpdateTimer;

    private static readonly Color UiGlassDeep = new Color(0.016f, 0.035f, 0.075f, 0.9f);
    private static readonly Color UiGlassPanel = new Color(0.028f, 0.058f, 0.115f, 0.86f);
    private static readonly Color UiGold = new Color(1f, 0.76f, 0.23f, 1f);
    private static readonly Color UiGoldDim = new Color(1f, 0.76f, 0.23f, 0.55f);
    private static readonly Color UiBlue = new Color(0.25f, 0.62f, 1f, 1f);
    private static readonly Color UiBlueSoft = new Color(0.62f, 0.8f, 1f, 1f);
    private static readonly Color UiBlueDim = new Color(0.25f, 0.62f, 1f, 0.5f);
    private static readonly Color UiTextBody = new Color(0.9f, 0.94f, 1f, 1f);

    private Image CreateUiStrip(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject stripObject = new GameObject(name, typeof(RectTransform));
        stripObject.transform.SetParent(parent, false);
        Image image = stripObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = stripObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return image;
    }

    private void AddUiBorder(RectTransform rect, Color color)
    {
        CreateUiStrip(rect, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), new Vector2(0f, 0f), color);
        CreateUiStrip(rect, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), color);
        CreateUiStrip(rect, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), color);
        CreateUiStrip(rect, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), new Vector2(0f, 0f), color);
    }

    private void AddPanelTrim(RectTransform panel)
    {
        CreateUiStrip(panel, "Trim_Top_Gold", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -2f), new Vector2(-10f, 0f), UiGold);
        CreateUiStrip(panel, "Trim_Bottom_Blue", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 0f), new Vector2(-10f, 2f), UiBlueDim);
        float arm = 16f;
        CreateUiStrip(panel, "Corner_TL_H", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(arm, 0f), UiGold);
        CreateUiStrip(panel, "Corner_TL_V", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -arm), new Vector2(2f, 0f), UiGold);
        CreateUiStrip(panel, "Corner_TR_H", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-arm, -2f), new Vector2(0f, 0f), UiGold);
        CreateUiStrip(panel, "Corner_TR_V", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-2f, -arm), new Vector2(0f, 0f), UiGold);
        CreateUiStrip(panel, "Corner_BL_H", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(arm, 2f), UiBlue);
        CreateUiStrip(panel, "Corner_BL_V", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(2f, arm), UiBlue);
        CreateUiStrip(panel, "Corner_BR_H", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-arm, 0f), new Vector2(0f, 2f), UiBlue);
        CreateUiStrip(panel, "Corner_BR_V", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-2f, 0f), new Vector2(0f, arm), UiBlue);
    }

    private Image CreateUiBar(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color fillColor)
    {
        GameObject barObject = new GameObject(name, typeof(RectTransform));
        barObject.transform.SetParent(parent, false);
        Image background = barObject.AddComponent<Image>();
        background.color = new Color(0f, 0.02f, 0.06f, 0.75f);
        background.raycastTarget = false;
        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        AddUiBorder(rect, UiGoldDim);

        GameObject fillObject = new GameObject(name + "_Fill", typeof(RectTransform));
        fillObject.transform.SetParent(rect, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(1f, -1f);
        fillRect.sizeDelta = new Vector2(sizeDelta.x - 2f, 0f);
        return fill;
    }

    private void SetUiBarRatio(Image fill, float ratio, float fullWidth)
    {
        if (fill == null)
            return;
        RectTransform rect = fill.rectTransform;
        rect.sizeDelta = new Vector2(Mathf.Max(0f, (fullWidth - 2f) * Mathf.Clamp01(ratio)), rect.sizeDelta.y);
    }

    private void InitializeStrategicCanvas()
    {
        if (strategicCanvasReady)
            return;

        EnsureStrategicEventSystem();
        strategicFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (strategicFont == null)
            strategicFont = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Liberation Sans" }, 14);

        GameObject canvasObject = new GameObject("SandRunners_Strategic_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        strategicCanvas = canvasObject.GetComponent<Canvas>();
        strategicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        strategicCanvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.45f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform statusPanel = CreatePanel(root, "Status_Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(390f, 112f), UiGlassDeep);
        strategicStatusPanelObject = statusPanel.gameObject;
        CreateText(statusPanel, "Title", "BATTLE FOR UNIVERSE // SAND RUNNERS", 15, FontStyle.Bold, new Vector2(14f, -7f), new Vector2(360f, 22f), UiGold);
        strategicStatusText = CreateText(statusPanel, "Status", "", 12, FontStyle.Normal, new Vector2(14f, -29f), new Vector2(360f, 42f), UiTextBody);
        CreateText(statusPanel, "Hull_Caption", "HULL", 10, FontStyle.Bold, new Vector2(14f, -76f), new Vector2(42f, 13f), UiGold);
        strategicHullBarFill = CreateUiBar(statusPanel, "Hull_Bar", new Vector2(58f, -76f), new Vector2(314f, 10f), UiGold);
        CreateText(statusPanel, "Apex_Caption", "APEX", 10, FontStyle.Bold, new Vector2(14f, -94f), new Vector2(42f, 13f), UiBlueSoft);
        strategicApexBarFill = CreateUiBar(statusPanel, "Apex_Bar", new Vector2(58f, -94f), new Vector2(314f, 7f), UiBlue);

        RectTransform modePanel = CreatePanel(root, "Mode_Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-220f, -16f), new Vector2(440f, 58f), UiGlassDeep);
        strategicModePanelObject = modePanel.gameObject;
        strategicModeText = CreateText(modePanel, "Mode", "", 13, FontStyle.Bold, new Vector2(14f, -7f), new Vector2(410f, 22f), UiBlueSoft);
        strategicApexText = CreateText(modePanel, "Apex", "", 11, FontStyle.Normal, new Vector2(14f, -30f), new Vector2(410f, 20f), UiTextBody);

        RectTransform enemyPanel = CreatePanel(root, "Enemy_Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(330f, 126f), new Color(0.16f, 0.03f, 0.05f, 0.85f));
        strategicEnemyPanelObject = enemyPanel.gameObject;
        CreateText(enemyPanel, "Enemy_Title", "MANDARINKA MOBILE FORTRESS", 14, FontStyle.Bold, new Vector2(12f, -8f), new Vector2(300f, 24f), new Color(1f, 0.46f, 0.36f, 1f));
        strategicEnemyText = CreateText(enemyPanel, "Enemy_Status", "", 13, FontStyle.Normal, new Vector2(12f, -36f), new Vector2(300f, 78f), UiTextBody);

        RectTransform selectionPanel = CreatePanel(root, "Selection_Panel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 204f), new Vector2(420f, 158f), UiGlassDeep);
        strategicSelectionPanelObject = selectionPanel.gameObject;
        strategicSelectedTitleText = CreateText(selectionPanel, "Selected_Title", "No unit selected", 16, FontStyle.Bold, new Vector2(14f, -8f), new Vector2(390f, 24f), UiBlueSoft);
        strategicSelectedStatsText = CreateText(selectionPanel, "Selected_Stats", "Tab: command mode | click unit to select", 13, FontStyle.Normal, new Vector2(14f, -38f), new Vector2(390f, 48f), UiTextBody);
        doctrineEscortButton = CreateButton(selectionPanel, "Doctrine_Escort", "ESCORT PYRAMID", new Vector2(12f, -91f), new Vector2(126f, 34f), SetSelectedSquadsEscortDoctrine);
        doctrineHoldButton = CreateButton(selectionPanel, "Doctrine_Hold", "HOLD 85M", new Vector2(146f, -91f), new Vector2(116f, 34f), ArmSelectedSquadsHoldDoctrine);
        doctrineSearchButton = CreateButton(selectionPanel, "Doctrine_Search", "SEARCH 260M", new Vector2(270f, -91f), new Vector2(136f, 34f), ArmSelectedSquadsSearchDoctrine);

        RectTransform armyPanel = CreatePanel(root, "Army_Panel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 204f), new Vector2(510f, 122f), UiGlassDeep);
        strategicArmyPanelObject = armyPanel.gameObject;
        strategicArmyText = CreateText(armyPanel, "Army_Title", "ARMY ROSTER", 14, FontStyle.Bold, new Vector2(12f, -8f), new Vector2(480f, 22f), UiGold);
        rosterFlyersButton = CreateButton(armyPanel, "Roster_Flyers", "Flyers", new Vector2(12f, -38f), new Vector2(112f, 32f), () => SelectFirstStrategicUnit("Flyer"));
        rosterScarabButton = CreateButton(armyPanel, "Roster_Scarabs", "Scarabs", new Vector2(132f, -38f), new Vector2(112f, 32f), () => SelectFirstStrategicUnit("Scarab"));
        rosterBuildersButton = CreateButton(armyPanel, "Roster_Builders", "Builders", new Vector2(252f, -38f), new Vector2(112f, 32f), () => SelectFirstStrategicUnit("Builder"));
        rosterTurretsButton = CreateButton(armyPanel, "Roster_Turrets", "Turrets", new Vector2(372f, -38f), new Vector2(112f, 32f), () => SelectFirstStrategicUnit("Turret"));
        rosterHeavyButton = CreateButton(armyPanel, "Roster_Heavy", "Heavy", new Vector2(12f, -76f), new Vector2(232f, 32f), () => SelectFirstStrategicUnit("Heavy"));
        rosterSpecialButton = CreateButton(armyPanel, "Roster_Special", "Special", new Vector2(252f, -76f), new Vector2(232f, 32f), () => SelectFirstStrategicUnit("Special"));

        RectTransform commandPanel = CreatePanel(root, "Command_Panel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 194f), UiGlassDeep);
        strategicCommandPanelObject = commandPanel.gameObject;
        strategicEventText = CreateText(commandPanel, "Event_Text", "", 13, FontStyle.Bold, new Vector2(18f, -10f), new Vector2(560f, 26f), UiGold);
        strategicProductionText = CreateText(commandPanel, "Production_Text", "", 13, FontStyle.Bold, new Vector2(600f, -10f), new Vector2(720f, 26f), UiBlueSoft);
        strategicMapText = CreateText(commandPanel, "Battlefield_Strip", "", 12, FontStyle.Bold, new Vector2(1340f, -10f), new Vector2(520f, 26f), UiBlue);

        float x = 18f;
        float y = -44f;
        float w = 142f;
        float h = 38f;
        CreateButton(commandPanel, "Build_Combat_Flyer", "Combat Flyer x5", new Vector2(x, y), new Vector2(w, h), BuildCombatFlyerSquad); x += w + 8f;
        CreateButton(commandPanel, "Build_Scarab_Tank", "Scarab Tank x3", new Vector2(x, y), new Vector2(w, h), BuildScarabTankSquad); x += w + 8f;
        CreateButton(commandPanel, "Build_Builder", "Builder Truck", new Vector2(x, y), new Vector2(w, h), BuildGoldenResourceBuilder); x += w + 8f;
        CreateButton(commandPanel, "Build_Abydos_Flyer", "Abydos Flyer", new Vector2(x, y), new Vector2(w, h), BuildAbydosFlyer); x += w + 8f;
        CreateButton(commandPanel, "Build_Heavy_Flyer", "Heavy Flyer", new Vector2(x, y), new Vector2(w, h), BuildHeavyFlyer); x += w + 8f;
        CreateButton(commandPanel, "Build_Wrath_Ra", "Wrath of Ra", new Vector2(x, y), new Vector2(w, h), BuildWrathOfRa);

        x = 18f;
        y = -88f;
        CreateButton(commandPanel, "Build_Crusher", "Fortress Crusher", new Vector2(x, y), new Vector2(w, h), BuildFortressCrusher); x += w + 8f;
        CreateButton(commandPanel, "Build_Thoth", "Thoth's Embrace", new Vector2(x, y), new Vector2(w, h), BuildThothEmbrace); x += w + 8f;
        CreateButton(commandPanel, "Build_Turret", "Mirror Turret", new Vector2(x, y), new Vector2(w, h), DeployGoldenSunTurret); x += w + 8f;
        apexButton = CreateButton(commandPanel, "Apex_Button", "Apex Beam", new Vector2(x, y), new Vector2(w, h), HandleApexButton); x += w + 8f;
        cruiseButton = CreateButton(commandPanel, "Cruise_Button", "Pyramid TV", new Vector2(x, y), new Vector2(w, h), LaunchGuidedMissile); x += w + 8f;
        sunCoreButton = CreateButton(commandPanel, "SunCore_Button", "Sun-core TV", new Vector2(x, y), new Vector2(w, h), LaunchGuidedSunCoreMissile); x += w + 8f;
        anubisManualButton = CreateButton(commandPanel, "Anubis_TV_Button", "Anubis TV", new Vector2(x, y), new Vector2(w, h), LaunchGuidedAnubisMissile);

        x = 18f;
        y = -132f;
        CreateButton(commandPanel, "Build_Depot", "Depot", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.ResourceDepot)); x += w + 8f;
        CreateButton(commandPanel, "Build_Twin30", "30-mm Turret", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.Twin30mmTurret)); x += w + 8f;
        CreateButton(commandPanel, "Build_MirrorBeam", "Mirror Beam", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.MirrorBeamTurret)); x += w + 8f;
        CreateButton(commandPanel, "Build_Gepard", "Gepard AA", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.GepardAALauncher)); x += w + 8f;
        StrategicCanvasButton anubis = CreateButton(commandPanel, "Build_Anubis", "Anubis Strike", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.AnubisStrikeLauncher)); x += w + 8f;
        StrategicCanvasButton aerodrome = CreateButton(commandPanel, "Build_Aerodrome", "Aerodrome", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.Aerodrome)); x += w + 8f;
        CreateButton(commandPanel, "Build_Missile_Silo", "Missile Silo", new Vector2(x, y), new Vector2(w, h), () => BeginStructureBlueprint(StructureKind.CruiseMissileSilo)); x += w + 8f;
        CreateButton(commandPanel, "Build_Salvage_Scarab", "Salvage Scarab", new Vector2(x, y), new Vector2(w, h), BuildSalvageScarab);

        InitializeSalvageCanvas(root);

        strategicSelectionMaterial = CreateMaterial("Strategic Selection Blue", new Color(0.25f, 0.62f, 1f, 0.9f));
        SetEmission(strategicSelectionMaterial, new Color(0.15f, 0.5f, 1f, 1f), 1.5f);
        strategicCanvasReady = true;
        strategicUiEvents -= HandleStrategicUiEvent;
        strategicUiEvents += HandleStrategicUiEvent;
        InitializeThothCarrierUi();
        InitializeCurseHiveUi();
        InitializeVerticalSliceMissionUi();
        InitializeHorusDiplomacyUi();
    }

    private void PublishStrategicUiEvent(StrategicUiEventKind kind, bool visible)
    {
        System.Action<StrategicUiEvent> handler = strategicUiEvents;
        if (handler == null)
            return;

        StrategicUiEvent uiEvent = new StrategicUiEvent();
        uiEvent.kind = kind;
        uiEvent.visible = visible;
        handler(uiEvent);
    }

    private void HandleStrategicUiEvent(StrategicUiEvent uiEvent)
    {
        if (uiEvent.kind == StrategicUiEventKind.StrategicCanvasVisibility && strategicCanvas != null)
            strategicCanvas.gameObject.SetActive(uiEvent.visible);
    }

    private void EnsureStrategicEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            eventSystem = new GameObject("SandRunners_EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            Destroy(legacyModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = Color.Lerp(UiGlassPanel, color, 0.2f);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        AddUiBorder(rect, UiGoldDim);
        AddPanelTrim(rect);
        return rect;
    }

    private Text CreateText(RectTransform parent, string name, string value, int size, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = strategicFont;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return text;
    }

    private StrategicCanvasButton CreateButton(RectTransform parent, string name, string value, Vector2 anchoredPosition, Vector2 sizeDelta, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.1f, 0.19f, 0.95f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            PlaySandRunnerSound(SandRunnerSound.UiConfirm, battlePyramid != null ? battlePyramid.position : Vector3.zero, 0.28f);
            action.Invoke();
        });
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.05f, 0.1f, 0.19f, 0.95f);
        colors.highlightedColor = new Color(0.11f, 0.25f, 0.46f, 1f);
        colors.pressedColor = new Color(0.78f, 0.58f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        AddUiBorder(rect, UiGoldDim);
        CreateUiStrip(rect, "Button_Blue_Underline", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 0f), new Vector2(-4f, 2f), UiBlueDim);

        Text label = CreateText(rect, "Label", value, 12, FontStyle.Bold, new Vector2(8f, -7f), new Vector2(sizeDelta.x - 16f, sizeDelta.y - 8f), new Color(0.93f, 0.96f, 1f, 1f));
        label.alignment = TextAnchor.MiddleCenter;

        StrategicCanvasButton binding = new StrategicCanvasButton();
        binding.button = button;
        binding.label = label;
        return binding;
    }

    private void UpdateDoctrineButtons()
    {
        bool hasSquadSelection = selectedSquads.Count > 0;
        if (doctrineEscortButton != null && doctrineEscortButton.button != null)
            doctrineEscortButton.button.interactable = hasSquadSelection;
        if (doctrineHoldButton != null && doctrineHoldButton.button != null)
            doctrineHoldButton.button.interactable = hasSquadSelection;
        if (doctrineSearchButton != null && doctrineSearchButton.button != null)
            doctrineSearchButton.button.interactable = hasSquadSelection;

        if (pendingSquadDoctrine.HasValue)
        {
            if (pendingSquadDoctrine.Value == SquadDoctrine.HoldArea)
                SetButtonText(doctrineHoldButton, "RMB: SET HOLD");
            else if (pendingSquadDoctrine.Value == SquadDoctrine.SearchAndDestroy)
                SetButtonText(doctrineSearchButton, "RMB: SET SEARCH");
        }
        else
        {
            SetButtonText(doctrineHoldButton, "HOLD 85M");
            SetButtonText(doctrineSearchButton, "SEARCH 260M");
        }
    }

    private void UpdateStrategicInterface(float dt)
    {
        if (!strategicCanvasReady)
            InitializeStrategicCanvas();

        if (strategicCanvas == null)
            return;

        bool gunnerView = gunnerSide != 0 || apexTargetingMode;
        strategicCanvas.gameObject.SetActive(!hudHidden && !gunnerView);
        bool showCommandPanels = !hudHidden && !gunnerView && commandCursorMode;
        bool showDrivePanels = !hudHidden && !gunnerView && !commandCursorMode;
        SetStrategicPanelActive(strategicStatusPanelObject, !hudHidden && !gunnerView);
        SetStrategicPanelActive(strategicModePanelObject, !hudHidden && !gunnerView && Time.timeSinceLevelLoad < 12f);
        SetStrategicPanelActive(strategicEnemyPanelObject, showCommandPanels);
        SetStrategicPanelActive(strategicSelectionPanelObject, showCommandPanels);
        SetStrategicPanelActive(strategicArmyPanelObject, showCommandPanels);
        SetStrategicPanelActive(strategicCommandPanelObject, showCommandPanels);
        UpdateApexUiCharge(dt);
        UpdateStrategicSelection(dt);
        UpdateDoctrineButtons();
        UpdateAviationHudText();
        UpdateThothCarrierUi();
        UpdateCurseHiveUi();
        UpdateVerticalSliceMissionUi();
        UpdateHorusDiplomacyUi();
        UpdateHorusDevelopmentCanvas();
        UpdateSalvageCanvas(dt);

        if (hudHidden)
            return;

        float hullRatio = pyramidMaxHull > 0f ? Mathf.Clamp01(pyramidHull / pyramidMaxHull) : 1f;
        SetUiBarRatio(strategicHullBarFill, hullRatio, 314f);
        if (strategicHullBarFill != null)
            strategicHullBarFill.color = hullRatio > 0.6f ? UiGold : hullRatio > 0.25f ? new Color(1f, 0.55f, 0.16f, 1f) : new Color(1f, 0.2f, 0.12f, 1f);
        SetUiBarRatio(strategicApexBarFill, beamCooldownTimer > 0f ? 0f : beamCharge, 314f);

        string inputState = pyramidMoveInputActive ? "yes" : "no";
        string modeName = commandCursorMode ? "COMMAND" : "DRIVE";
        string followState = cameraFollowSelectionMode
            ? "Following: " + (cameraFollowLabel ?? "selection") + " | scroll zoom | C release"
            : "FOLLOW off | C follow selected";
        if (showDrivePanels)
        {
            strategicStatusText.text =
                "Mode " + modeName + " | Input WASD: " + inputState +
                " | Speed " + pyramidVelocity.magnitude.ToString("0.0") + "\n" +
                "Hull " + Mathf.RoundToInt(pyramidHull) + "/" + Mathf.RoundToInt(pyramidMaxHull) +
                "   Sand " + Mathf.RoundToInt(sand) +
                "   Gold " + Mathf.RoundToInt(gold) +
                "   Wind " + Mathf.RoundToInt(wind) + "\n" +
                "Hangar " + hangarStored + " ready | Tab strategy | Home reset";
        }
        else
        {
            strategicStatusText.text =
                "Mode " + modeName + " | Input WASD: " + inputState +
                " | Speed " + pyramidVelocity.magnitude.ToString("0.0") + "\n" +
                "Hull " + Mathf.RoundToInt(pyramidHull) + "/" + Mathf.RoundToInt(pyramidMaxHull) +
                "   Sand " + Mathf.RoundToInt(sand) +
                "   Gold " + Mathf.RoundToInt(gold) +
                "   Wind " + Mathf.RoundToInt(wind) + "\n" +
                followState + " | LMB/RMB orders | Army " + runners.Count;
        }

        bool openingHint = Time.timeSinceLevelLoad < 5f;
        strategicModeText.text = openingHint
            ? "Click Game View, WASD moves pyramid, Tab strategy HUD"
            : commandCursorMode
                ? "COMMAND MODE: LMB click/drag selects squads, RMB orders, RMB drag or MMB orbits. Tab returns drive"
                : "DRIVE MODE: WASD/arrows drive the pyramid, mouse rotates, scroll zooms";

        string targetName = GetApexTargetName();
        strategicApexText.text = openingHint
            ? "Hold W for one second: Input should become yes and Speed should rise"
            : commandCursorMode
            ? "Apex beam " + GetApexStateText() + " | target: " + targetName + " | R hold/release or button charge/fire"
            : "Q/E howitzers | R apex charge | Z/X missiles | Space Ankh pulse";
        if (strategicEventText != null)
            strategicEventText.text = missionCommsTimer > 0f ? missionComms : lastEvent;
        if (strategicProductionText != null)
            strategicProductionText.text = GetProductionSummary();
        if (strategicMapText != null)
            strategicMapText.text = GetBattlefieldStripSummary();

        UpdateEnemyCanvasText();
        if (showCommandPanels)
        {
            UpdateSelectionCanvasText();
            UpdateRosterButtons();
            UpdateWeaponButtons();
        }
    }

    private void InitializeSalvageCanvas(RectTransform root)
    {
        RectTransform panel = CreatePanel(root, "Salvage_Panel", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -156f), new Vector2(320f, 150f), new Color(0.12f, 0.075f, 0.018f, 0.92f));
        strategicSalvagePanelObject = panel.gameObject;
        CreateText(panel, "Title", "SALVAGE SCARAB // FIELD CONTROL", 14, FontStyle.Bold,
            new Vector2(12f, -8f), new Vector2(296f, 22f), new Color(1f, 0.68f, 0.16f, 1f));
        strategicSalvageStatusText = CreateText(panel, "Status", "", 12, FontStyle.Normal,
            new Vector2(12f, -34f), new Vector2(296f, 47f), UiTextBody);
        CreateButton(panel, "Collect_Nearest", "COLLECT NEAREST", new Vector2(12f, -86f), new Vector2(144f, 26f), SetSelectedSalvageCollectNearest);
        CreateButton(panel, "Return_Pyramid", "RETURN", new Vector2(164f, -86f), new Vector2(68f, 26f), SetSelectedSalvageReturn);
        CreateButton(panel, "Auto_Scavenge", "AUTO", new Vector2(240f, -86f), new Vector2(68f, 26f), SetSelectedSalvageAuto);
        CreateText(panel, "Hint", "RMB field: manual order  •  one field per run", 10, FontStyle.Normal,
            new Vector2(12f, -119f), new Vector2(296f, 20f), UiGoldDim);
        strategicSalvagePanelObject.SetActive(false);

        for (int i = 0; i < MaxSalvageMarkers; i++)
        {
            GameObject markerObject = new GameObject("Salvage_Marker_" + i, typeof(RectTransform));
            markerObject.transform.SetParent(root, false);
            Image image = markerObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.045f, 0.012f, 0.78f);
            image.raycastTarget = false;
            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(116f, 34f);
            AddUiBorder(markerRect, new Color(1f, 0.58f, 0.08f, 0.72f));
            Text markerText = CreateText(markerRect, "Label", "SALVAGE", 10, FontStyle.Bold,
                new Vector2(4f, -3f), new Vector2(108f, 28f), new Color(1f, 0.72f, 0.2f, 1f));
            markerText.alignment = TextAnchor.MiddleCenter;
            strategicSalvageMarkers.Add(new SalvageMarkerUi { root = markerObject, rect = markerRect, text = markerText });
            markerObject.SetActive(false);
        }
    }

    private void UpdateSalvageCanvas(float dt)
    {
        RunnerUnit runner = GetSelectedSalvageRunner();
        bool showPanel = runner != null && !hudHidden && gunnerSide == 0;
        SetStrategicPanelActive(strategicSalvagePanelObject, showPanel);
        if (showPanel && strategicSalvageStatusText != null)
            strategicSalvageStatusText.text = GetSalvageRunnerStatus(runner);

        strategicSalvageUpdateTimer -= dt;
        if (strategicSalvageUpdateTimer > 0f || mainCamera == null || strategicCanvas == null)
            return;
        strategicSalvageUpdateTimer = SalvageUpdateInterval;

        GetMarkerSalvageFields(strategicSalvageMarkerFields, mainCamera.transform.position);
        RectTransform canvasRect = strategicCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;
        float edgeX = canvasSize.x * 0.5f - 78f;
        float edgeY = canvasSize.y * 0.5f - 46f;
        for (int i = 0; i < strategicSalvageMarkers.Count; i++)
        {
            SalvageMarkerUi marker = strategicSalvageMarkers[i];
            bool visible = i < strategicSalvageMarkerFields.Count && !hudHidden && gunnerSide == 0;
            SetStrategicPanelActive(marker.root, visible);
            if (!visible)
                continue;

            SalvageField field = strategicSalvageMarkerFields[i];
            Vector3 screen = mainCamera.WorldToScreenPoint(field.root.position + Vector3.up * 2.3f);
            bool behind = screen.z <= 0f;
            if (behind)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            bool offscreen = behind || Mathf.Abs(local.x) > edgeX || Mathf.Abs(local.y) > edgeY;
            Vector2 clamped = new Vector2(Mathf.Clamp(local.x, -edgeX, edgeX), Mathf.Clamp(local.y, -edgeY, edgeY));
            marker.rect.anchoredPosition = clamped;

            float distance = FlatDistance(mainCamera.transform.position, field.root.position);
            float fadeDistance = presentationBudget != null ? presentationBudget.detailCullDistance : 220f;
            float alpha = Mathf.Lerp(0.38f, 1f, 1f - Mathf.Clamp01(distance / Mathf.Max(1f, fadeDistance)));
            string arrow = string.Empty;
            if (offscreen)
            {
                if (Mathf.Abs(local.x) > Mathf.Abs(local.y))
                    arrow = local.x < 0f ? "◀ " : "▶ ";
                else
                    arrow = local.y < 0f ? "▼ " : "▲ ";
            }
            int clusterCount = GetSalvageMarkerClusterCount(field);
            string cluster = clusterCount > 1 ? " x" + clusterCount : string.Empty;
            marker.text.text = arrow + "SALVAGE" + cluster + "  " + Mathf.RoundToInt(distance) + " m";
            marker.text.color = new Color(1f, 0.72f, 0.2f, alpha);
            Image background = marker.root.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.08f, 0.045f, 0.012f, 0.42f + alpha * 0.35f);
        }
    }

    private void SetStrategicPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
            panel.SetActive(active);
    }

    private bool UseStrategicCanvasHud()
    {
        return strategicCanvasReady;
    }

    private bool IsStrategicPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void StopApexUiCharge()
    {
        apexUiCharging = false;
    }

    private void HandleApexButton()
    {
        if (beamCooldownTimer > 0f)
        {
            lastEvent = "Apex sunlight focus is rebuilding: " + Mathf.CeilToInt(beamCooldownTimer) + "s.";
            return;
        }

        if (!apexTargetingMode)
        {
            EnterApexTargetingMode();
            apexUiCharging = true;
            lastEvent = "Diegetic Apex chamber engaged. Aim with the mouse; press Apex again to fire.";
            return;
        }

        if (beamCharge < 0.25f)
        {
            lastEvent = "Apex focus is only " + Mathf.RoundToInt(beamCharge * 100f) + "%. Let sunlight condense.";
            return;
        }

        FireChargedBeam();
        beamCharge = 0f;
        apexUiCharging = false;
        ExitApexTargetingMode();
    }

    private void UpdateApexUiCharge(float dt)
    {
        if (!apexUiCharging || !apexTargetingMode)
            return;

        if (beamCooldownTimer > 0f)
        {
            apexUiCharging = false;
            return;
        }

        beamCharge = Mathf.Clamp01(beamCharge + dt / 2.4f);
        if (beamCharge >= 1f)
            lastEvent = "Apex beam fully charged. Press Apex or release R to fire.";
    }

    private string GetApexStateText()
    {
        if (beamCooldownTimer > 0f)
            return "cooldown " + Mathf.CeilToInt(beamCooldownTimer) + "s";
        if (apexTargetingMode)
            return "diegetic focus " + Mathf.RoundToInt(beamCharge * 100f) + "%";
        return "ready";
    }

    private string GetApexTargetName()
    {
        EnemyUnit target = FindNearestEnemy(battlePyramid != null ? battlePyramid.position : Vector3.zero, 140f);
        if (target == null || target.transform == null)
            return "nearest enemy when fired";
        if (target.transform.name.Contains("Mandarinka"))
            return "Mandarinka fortress group";
        if (target.transform.name.Contains("Gustav"))
            return "Karl Gustav";
        return target.transform.name.Replace('_', ' ');
    }

    private void UpdateEnemyCanvasText()
    {
        if (strategicEnemyText == null)
            return;

        if (mandarinkaDefeated || mandarinkaFortressEnemy == null)
        {
            strategicEnemyText.text = "Fortress defeated\nRoute to Earth Empire border open";
            return;
        }

        int ground = CountMandarinkaRole(MandarinkaRole.GroundCrawler);
        int air = CountMandarinkaRole(MandarinkaRole.AirJunk);
        int builders = CountMandarinkaRole(MandarinkaRole.Builder);
        int turrets = CountMandarinkaRole(MandarinkaRole.FieldTurret);
        strategicEnemyText.text =
            "Hull " + Mathf.RoundToInt(mandarinkaFortressEnemy.health) + "/" + Mathf.RoundToInt(MandarinkaFortressMaxHull) +
            "   Shield " + Mathf.RoundToInt(mandarinkaFortressShield) + "\n" +
            "AI " + GetMandarinkaStrategyName() + "   Supply " + Mathf.RoundToInt(mandarinkaSupply) + "\n" +
            "Units: ground " + ground + " / air " + air + " / builders " + builders + " / turrets " + turrets + "\n" +
            GetMandarinkaTerritoryHudLine();
    }

    private string GetBattlefieldStripSummary()
    {
        if (battlePyramid == null || mandarinkaFortressRoot == null)
            return "SW GOLDEN PYRAMID  |  CENTER RUINS  |  NE RED PALACE";

        float distance = FlatDistance(battlePyramid.position, mandarinkaFortressRoot.position);
        int held = 0;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            if (resourceNodes[i].controlled)
                held++;
        }
        return "SW Pyramid  << " + Mathf.RoundToInt(distance) + "m " + GetMandarinkaFrontSummary() +
            " >>  NE Palace   Golden nodes " + held + "/" + resourceNodes.Count +
            "   Imperial nodes " + CountMandarinkaHeldResourceNodes();
    }

    private void UpdateSelectionCanvasText()
    {
        if (TryWriteRTSSelectionCanvas())
            return;

        if (selectedStrategicTransform == null)
        {
            strategicSelectedTitleText.text = "No unit selected";
            strategicSelectedStatsText.text = "Tab: command mode | select a developer | right-click a resource to capture and develop it";
            return;
        }

        strategicSelectedTitleText.text = selectedStrategicName;
        RunnerUnit runner = FindRunnerByRoot(selectedStrategicTransform);
        if (runner != null)
        {
            string command = runner.autonomous ? "Autonomous: direct orders locked" : "Orders: click ground in command mode";
            strategicSelectedStatsText.text =
                "Hull " + Mathf.RoundToInt(runner.health) + "/" + Mathf.RoundToInt(runner.maxHealth) +
                "   Range " + Mathf.RoundToInt(runner.range) +
                "   Damage " + Mathf.RoundToInt(runner.damage) + "\n" +
                (runner.airborne ? "Airborne platform" : "Ground vehicle") + "   " + command +
                GetResourceDevelopmentOrderSummary(runner);
            return;
        }

        GoldenBuilderAsset builder = FindBuilderByRoot(selectedStrategicTransform);
        if (builder != null)
        {
            strategicSelectedStatsText.text =
                "Hull " + Mathf.RoundToInt(builder.health) + "/" + Mathf.RoundToInt(builder.maxHealth) + "\n" +
                "Builds, harvests, repairs and develops permanent resource mines" +
                GetResourceDevelopmentOrderSummary(builder);
            return;
        }

        GoldenTurretAsset turret = FindTurretByRoot(selectedStrategicTransform);
        if (turret != null)
        {
            strategicSelectedStatsText.text =
                "Hull " + Mathf.RoundToInt(turret.health) + "/" + Mathf.RoundToInt(turret.maxHealth) + "\n" +
                "Static solar turret. Auto-targets Mandarinka assets.";
        }
    }

    private void UpdateRosterButtons()
    {
        SetButtonText(rosterFlyersButton, "Flyers " + CountRunnersContaining("Flyer"));
        SetButtonText(rosterScarabButton, "Scarabs " + CountRunnersContaining("Scarab"));
        SetButtonText(rosterBuildersButton, "Builders " + goldenBuilders.Count);
        SetButtonText(rosterTurretsButton, "Turrets " + goldenTurrets.Count);
        SetButtonText(rosterHeavyButton, "Heavy " + (CountRunnersContaining("Wrath") + CountRunnersContaining("Heavy")));
        SetButtonText(rosterSpecialButton, "Special " + (CountRunnersContaining("Thoth") + CountRunnersContaining("Crusher")));
    }

    private void UpdateWeaponButtons()
    {
        SetButtonText(apexButton, "Apex\n" + GetApexStateText());
        SetButtonText(cruiseButton, "Pyramid TV\n" + cruiseMissiles + " | " + Mathf.Max(0, Mathf.CeilToInt(missileTimer)) + "s");
        SetButtonText(sunCoreButton, "Sun-core TV\n" + nuclearMissiles + " | " + Mathf.Max(0, Mathf.CeilToInt(nuclearTimer)) + "s");
        GoldenStructure anubisLauncher = FindNearestStructure(battlePyramid != null ? battlePyramid.position : Vector3.zero,
            StructureKind.AnubisStrikeLauncher, float.MaxValue);
        SetButtonText(anubisManualButton, anubisLauncher == null ? "Anubis TV\nBUILD FIRST" : "Anubis TV\nREADY " + anubisLauncher.ammo);
    }

    private void SetButtonText(StrategicCanvasButton binding, string value)
    {
        if (binding != null && binding.label != null)
            binding.label.text = value;
    }

    private bool TrySpendResources(float sandCost, float goldCost, float windCost, string label)
    {
        if (sand < sandCost || gold < goldCost || wind < windCost)
        {
            lastEvent = label + " needs " + Mathf.RoundToInt(sandCost) + " sand, " + Mathf.RoundToInt(goldCost) + " gold, " + Mathf.RoundToInt(windCost) + " wind.";
            return false;
        }

        sand -= sandCost;
        gold -= goldCost;
        wind -= windCost;
        return true;
    }

    private Vector3 GetSquadSpawnPosition(int index, int count, bool airborne)
    {
        Vector3 basePosition;
        if (airborne)
        {
            GoldenStructure aerodrome = selectedStructure != null && selectedStructure.kind == StructureKind.Aerodrome && selectedStructure.health > 0f
                ? selectedStructure
                : FindNearestAerodrome(battlePyramid != null ? battlePyramid.position : Vector3.zero);
            basePosition = aerodrome != null && aerodrome.transform != null
                ? aerodrome.transform.TransformPoint(new Vector3(0f, 0.6f, 1.4f)) + Vector3.up * 4.5f
                : GetHangarExitPosition() + Vector3.up * 4.5f;
        }
        else
        {
            basePosition = GetExternalFactoryPosition();
        }
        float center = (count - 1) * 0.5f;
        Vector3 offset = battlePyramid.right * ((index - center) * 2.1f) - battlePyramid.forward * (airborne ? 1.2f : 2.4f);
        Vector3 position = basePosition + offset;
        position.y = GetPlayableGroundHeight(position) + (airborne ? 7f : 0.4f);
        return position;
    }

    private void BuildCombatFlyerSquad()
    {
        QueueProduction(ProductionKind.CombatFlyer);
    }

    private void BuildScarabTankSquad()
    {
        QueueProduction(ProductionKind.ScarabTank);
    }

    private void BuildAbydosFlyer()
    {
        QueueProduction(ProductionKind.AbydosFlyer);
    }

    private void BuildHeavyFlyer()
    {
        QueueProduction(ProductionKind.HeavyFlyer);
    }

    private void BuildWrathOfRa()
    {
        if (CountRunnersContaining("Wrath of Ra") >= 4)
        {
            lastEvent = "Wrath of Ra platforms are already saturating the line.";
            return;
        }
        QueueProduction(ProductionKind.WrathOfRa);
    }

    private void BuildFortressCrusher()
    {
        if (CountRunnersContaining("Fortress Crusher") >= 3)
        {
            lastEvent = "Fortress Crusher limit reached: 3.";
            return;
        }
        QueueProduction(ProductionKind.FortressCrusher);
    }

    private void BuildThothEmbrace()
    {
        if (CountRunnersContaining("Thoth's Embrace") > 0)
        {
            lastEvent = "Only one Thoth's Embrace can be fielded.";
            return;
        }
        QueueProduction(ProductionKind.ThothEmbrace);
    }

    private int CountRunnersContaining(string text)
    {
        int count = 0;
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner != null && runner.transform != null && runner.displayName.Contains(text))
                count++;
        }
        return count;
    }

    private bool TrySelectStrategicObject(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        if (TrySelectTouchOfHorus(hitTransform))
            return true;

        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner != null && runner.transform != null && IsTransformInHierarchy(hitTransform, runner.transform))
            {
                SelectStrategicTransform(runner.transform, runner.displayName);
                return true;
            }
        }

        for (int i = 0; i < goldenBuilders.Count; i++)
        {
            GoldenBuilderAsset builder = goldenBuilders[i];
            if (builder != null && builder.transform != null && IsTransformInHierarchy(hitTransform, builder.transform))
            {
                SelectStrategicTransform(builder.transform, builder.displayName);
                return true;
            }
        }

        for (int i = 0; i < goldenTurrets.Count; i++)
        {
            GoldenTurretAsset turret = goldenTurrets[i];
            if (turret != null && turret.transform != null && IsTransformInHierarchy(hitTransform, turret.transform))
            {
                SelectStrategicTransform(turret.transform, turret.displayName);
                return true;
            }
        }

        return false;
    }

    private bool IsTransformInHierarchy(Transform hit, Transform root)
    {
        Transform current = hit;
        while (current != null)
        {
            if (current == root)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void SelectStrategicTransform(Transform root, string displayName)
    {
        selectedStrategicTransform = root;
        selectedStrategicName = displayName;
        lastEvent = "Selected: " + displayName + ". Click ground in command mode to issue an order.";
        EnsureSelectionRing();
    }

    private bool HasStrategicSelection()
    {
        return selectedStrategicTransform != null;
    }

    private void IssueSelectedStrategicOrder(Vector3 worldPoint)
    {
        if (selectedStrategicTransform == null)
            return;

        if (touchOfHorus != null && selectedStrategicTransform == touchOfHorus.root)
        {
            touchOfHorus.pinnedTarget = null;
            touchOfHorus.moveDestination = worldPoint;
            touchOfHorus.hasMoveDestination = true;
            PlaceCommandMarker(worldPoint);
            lastEvent = "Touch of Horus glides to the commanded point.";
            return;
        }

        Vector3 order = worldPoint;
        order.y = GetPlayableGroundHeight(order);

        RunnerUnit runner = FindRunnerByRoot(selectedStrategicTransform);
        if (runner != null)
        {
            if (runner.autonomous)
            {
                lastEvent = runner.displayName + " is autonomous and refuses direct orders.";
                return;
            }

            runner.hasOrder = true;
            runner.orderPosition = order;
            PlaceCommandMarker(order);
            lastEvent = runner.displayName + " ordered to move.";
            return;
        }

        GoldenBuilderAsset builder = FindBuilderByRoot(selectedStrategicTransform);
        if (builder != null)
        {
            builder.hasOrder = true;
            builder.orderPosition = order;
            builder.targetNode = null;
            PlaceCommandMarker(order);
            lastEvent = "Solar Builder Truck ordered to work point.";
            return;
        }

        if (FindTurretByRoot(selectedStrategicTransform) != null)
            lastEvent = "Sun turrets are static. Use builders for new positions.";
    }

    private void SelectFirstStrategicUnit(string group)
    {
        if (SelectFirstRTSSquad(group))
            return;

        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner == null || runner.transform == null)
                continue;

            if ((group == "Flyer" && HasCapability(runner, UnitCapability.Flyer)) ||
                (group == "Scarab" && HasCapability(runner, UnitCapability.Scarab)) ||
                (group == "Heavy" && HasCapability(runner, UnitCapability.Heavy)) ||
                (group == "Special" && (HasCapability(runner, UnitCapability.ThothUnit) || HasCapability(runner, UnitCapability.FortressCrusherUnit))))
            {
                SelectStrategicTransform(runner.transform, runner.displayName);
                return;
            }
        }

        if (group == "Builder" && goldenBuilders.Count > 0)
        {
            for (int i = 0; i < goldenBuilders.Count; i++)
            {
                if (goldenBuilders[i].transform != null)
                {
                    SelectStrategicTransform(goldenBuilders[i].transform, goldenBuilders[i].displayName);
                    return;
                }
            }
        }

        if (group == "Turret" && goldenTurrets.Count > 0)
        {
            for (int i = 0; i < goldenTurrets.Count; i++)
            {
                if (goldenTurrets[i].transform != null)
                {
                    SelectStrategicTransform(goldenTurrets[i].transform, goldenTurrets[i].displayName);
                    return;
                }
            }
        }

        lastEvent = "No active " + group + " unit to select.";
    }

    private RunnerUnit FindRunnerByRoot(Transform root)
    {
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit runner = runners[i];
            if (runner != null && runner.transform == root)
                return runner;
        }
        return null;
    }

    private GoldenBuilderAsset FindBuilderByRoot(Transform root)
    {
        for (int i = 0; i < goldenBuilders.Count; i++)
        {
            GoldenBuilderAsset builder = goldenBuilders[i];
            if (builder != null && builder.transform == root)
                return builder;
        }
        return null;
    }

    private GoldenTurretAsset FindTurretByRoot(Transform root)
    {
        for (int i = 0; i < goldenTurrets.Count; i++)
        {
            GoldenTurretAsset turret = goldenTurrets[i];
            if (turret != null && turret.transform == root)
                return turret;
        }
        return null;
    }

    private void EnsureSelectionRing()
    {
        if (strategicSelectionRing != null)
            return;

        GameObject ringObject = new GameObject("Strategic_Blue_Selection_Ring");
        strategicSelectionRing = ringObject.AddComponent<LineRenderer>();
        strategicSelectionRing.positionCount = 80;
        strategicSelectionRing.loop = true;
        strategicSelectionRing.useWorldSpace = false;
        strategicSelectionRing.startWidth = 0.08f;
        strategicSelectionRing.endWidth = 0.08f;
        strategicSelectionRing.sharedMaterial = strategicSelectionMaterial;
        strategicSelectionRing.startColor = new Color(0.25f, 0.62f, 1f, 1f);
        strategicSelectionRing.endColor = strategicSelectionRing.startColor;
        for (int i = 0; i < strategicSelectionRing.positionCount; i++)
        {
            float angle = i / (float)strategicSelectionRing.positionCount * Mathf.PI * 2f;
            strategicSelectionRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
        }
    }

    private void UpdateStrategicSelection(float dt)
    {
        if (!commandCursorMode)
        {
            if (strategicSelectionRing != null)
                strategicSelectionRing.gameObject.SetActive(false);
            return;
        }

        if (selectedStrategicTransform == null)
        {
            if (strategicSelectionRing != null)
                strategicSelectionRing.gameObject.SetActive(false);
            return;
        }

        bool horus = touchOfHorus != null && selectedStrategicTransform == touchOfHorus.root;
        RunnerUnit runner = FindRunnerByRoot(selectedStrategicTransform);
        GoldenBuilderAsset builder = FindBuilderByRoot(selectedStrategicTransform);
        GoldenTurretAsset turret = FindTurretByRoot(selectedStrategicTransform);
        if (!horus && runner == null && builder == null && turret == null)
        {
            selectedStrategicTransform = null;
            if (strategicSelectionRing != null)
                strategicSelectionRing.gameObject.SetActive(false);
            return;
        }

        EnsureSelectionRing();
        strategicSelectionRing.gameObject.SetActive(true);
        Vector3 position = selectedStrategicTransform.position;
        strategicSelectionRing.transform.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.12f, position.z);
        float radius = horus ? 7.5f : HasCapability(runner, UnitCapability.ThothUnit) ? 7.2f : HasCapability(runner, UnitCapability.FortressCrusherUnit) ? 4.2f : 2.7f;
        strategicSelectionRing.transform.localScale = new Vector3(radius, 1f, radius);
        strategicSelectionRing.transform.rotation = Quaternion.Euler(0f, Time.time * 22f, 0f);
    }
}
