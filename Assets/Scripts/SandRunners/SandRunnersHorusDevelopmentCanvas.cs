using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private GameObject horusDevelopmentStatusPanelObject;
    private Text horusDevelopmentStatusText;
    private StrategicCanvasButton horusCancelDevelopmentButton;

    private void UpdateHorusDevelopmentCanvas()
    {
        if (strategicCanvas == null)
            return;

        InitializeHorusDevelopmentCanvas();

        bool horusSelected = touchOfHorus != null && touchOfHorus.root != null &&
            selectedStrategicTransform == touchOfHorus.root;
        bool visible = commandCursorMode && horusSelected && !hudHidden &&
            gameFlowState == SandRunnersGameFlowState.Playing;

        if (horusDevelopmentStatusPanelObject.activeSelf != visible)
            horusDevelopmentStatusPanelObject.SetActive(visible);
        if (!visible)
            return;

        HorusDevelopmentStatusModel model = GetHorusDevelopmentStatusModel();
        if (!model.active)
        {
            horusDevelopmentStatusText.text =
                "HORUS DEVELOPMENT // NO ACTIVE ORDER\nRMB a resource point to grow its nano-tree.";
            horusCancelDevelopmentButton.button.gameObject.SetActive(false);
            return;
        }

        string progress = model.requiredProgress > 0f
            ? Mathf.Clamp01(model.progress / model.requiredProgress).ToString("P0")
            : model.state == HorusDevelopmentOrderState.Complete ? "COMPLETE" : "WAITING";
        horusDevelopmentStatusText.text =
            "HORUS DEVELOPMENT // " + model.state.ToString().ToUpperInvariant() + "\n" +
            model.targetLabel + " // LEVEL " + model.level + "/" + model.maxLevel +
            " // " + progress;

        bool canCancel = model.state != HorusDevelopmentOrderState.Complete &&
            model.state != HorusDevelopmentOrderState.Invalid;
        horusCancelDevelopmentButton.button.gameObject.SetActive(canCancel);
        horusCancelDevelopmentButton.button.interactable = canCancel;
    }

    private void InitializeHorusDevelopmentCanvas()
    {
        if (horusDevelopmentStatusPanelObject != null)
            return;

        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Horus_Development_Status",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(16f, 370f), new Vector2(420f, 92f), UiGlassDeep);
        horusDevelopmentStatusPanelObject = panel.gameObject;
        horusDevelopmentStatusText = CreateText(panel, "Horus_Development_Text",
            "HORUS DEVELOPMENT // NO ACTIVE ORDER", 12, FontStyle.Bold,
            new Vector2(12f, -8f), new Vector2(396f, 46f), UiBlueSoft);
        horusCancelDevelopmentButton = CreateButton(panel, "Cancel_Horus_Development",
            "CANCEL DEVELOPMENT", new Vector2(12f, -58f),
            new Vector2(396f, 26f), CancelHorusDevelopmentFromCanvas);
        horusDevelopmentStatusPanelObject.SetActive(false);
    }

    private void CancelHorusDevelopmentFromCanvas()
    {
        HorusDevelopmentStatusModel model = GetHorusDevelopmentStatusModel();
        if (!model.active)
            return;

        string target = string.IsNullOrEmpty(model.targetLabel) ? "resource point" : model.targetLabel;
        CancelHorusResourceDevelopmentOrder();
        lastEvent = "Touch of Horus cancelled development at " + target + ".";
        ShowBanner("HORUS DEVELOPMENT CANCELLED", 2.4f);
        UpdateHorusDevelopmentCanvas();
    }
}
