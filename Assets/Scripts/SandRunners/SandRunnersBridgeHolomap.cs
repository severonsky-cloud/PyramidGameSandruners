using UnityEngine;
using UnityEngine.UI;

public sealed partial class SandRunnersCommandBridgeController
{
    private GameObject holomapPanel;
    private Text holomapReadout;

    private void BuildBridgeHolomapUi(Transform canvasRoot)
    {
        holomapPanel = new GameObject("BridgeHolomapPanel", typeof(RectTransform), typeof(Image));
        holomapPanel.transform.SetParent(canvasRoot, false);
        RectTransform panel = holomapPanel.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.22f, 0.08f);
        panel.anchorMax = new Vector2(0.78f, 0.42f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        holomapPanel.GetComponent<Image>().color = new Color(0.01f, 0.025f, 0.05f, 0.9f);

        holomapReadout = CreateHolomapText(holomapPanel.transform, "Readout", new Vector2(0.03f, 0.76f), new Vector2(0.97f, 0.97f), 18);
        string[] labels = { "MOVE", "ESCORT PYRAMID", "HOLD", "SEARCH & DESTROY", "DEFEND AREA", "FIRE MISSION" };
        SandRunnersPrototype.CommandBridgeIntent[] intents = {
            SandRunnersPrototype.CommandBridgeIntent.Move,
            SandRunnersPrototype.CommandBridgeIntent.EscortPyramid,
            SandRunnersPrototype.CommandBridgeIntent.Hold,
            SandRunnersPrototype.CommandBridgeIntent.SearchAndDestroy,
            SandRunnersPrototype.CommandBridgeIntent.DefendArea,
            SandRunnersPrototype.CommandBridgeIntent.FireMission
        };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            GameObject buttonObject = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(holomapPanel.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            int row = i / 3;
            int col = i % 3;
            rect.anchorMin = new Vector2(0.03f + col * 0.32f, 0.4f - row * 0.29f);
            rect.anchorMax = new Vector2(0.31f + col * 0.32f, 0.63f - row * 0.29f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.25f, 0.38f, 0.94f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => prototype.IssueCommandBridgeIntent(intents[index]));
            Text text = CreateHolomapText(buttonObject.transform, "Label", Vector2.zero, Vector2.one, 15);
            text.text = labels[i];
        }
    }

    private Text CreateHolomapText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.55f, 0.9f, 1f);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return text;
    }

    private void UpdateBridgeHolomap(float dt)
    {
        bool marked = prototype.TryGetCommandBridgeMark(out Vector3 point, out string label);
        holomapReadout.text = "SELECTED SQUADS: " + prototype.GetCommandBridgeSelectedSquadCount() +
                             "  |  MARK: " + (marked ? label : "NONE") +
                             "\nCommands execute through the existing RTS order system.";
    }

    private void UpdateBridgeModePanels()
    {
        if (visorPanel != null)
            visorPanel.SetActive(state == InternalState.BridgeVisor);
        if (holomapPanel != null)
            holomapPanel.SetActive(state == InternalState.BridgeHolomap);
    }
}
