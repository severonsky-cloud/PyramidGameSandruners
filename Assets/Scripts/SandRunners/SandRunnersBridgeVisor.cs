using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class SandRunnersCommandBridgeController
{
    private GameObject visorPanel;
    private Text visorReadout;
    private bool bridgeApexCharging;
    private string visorRelation = "NEUTRAL";

    private void BuildBridgeVisorUi(Transform canvasRoot)
    {
        visorPanel = new GameObject("BridgeVisorPanel", typeof(RectTransform), typeof(Image));
        visorPanel.transform.SetParent(canvasRoot, false);
        RectTransform panelRect = visorPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.06f);
        panelRect.anchorMax = new Vector2(0.7f, 0.2f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        visorPanel.GetComponent<Image>().color = new Color(0.01f, 0.035f, 0.055f, 0.82f);

        GameObject readout = new GameObject("VisorReadout", typeof(RectTransform), typeof(Text));
        readout.transform.SetParent(visorPanel.transform, false);
        visorReadout = readout.GetComponent<Text>();
        visorReadout.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        visorReadout.fontSize = 18;
        visorReadout.alignment = TextAnchor.MiddleCenter;
        visorReadout.color = new Color(0.25f, 0.9f, 1f);
        RectTransform rect = visorReadout.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 8f);
        rect.offsetMax = new Vector2(-12f, -8f);
    }

    private void UpdateBridgeVisor(float dt)
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || activeCamera == null)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * 0.055f;
        pitch = Mathf.Clamp(pitch - delta.y * 0.05f, -10f, 38f);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = new Ray(activeCamera.transform.position, activeCamera.transform.forward);
            if (prototype.TrySetCommandBridgeMark(ray, out Vector3 point, out string label, out string relation))
                visorRelation = relation;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            bridgeApexCharging = prototype.BeginCommandBridgeApexCharge();
        if (keyboard != null && keyboard.rKey.isPressed && bridgeApexCharging)
            prototype.ChargeCommandBridgeApex(dt);
        if (keyboard != null && keyboard.rKey.wasReleasedThisFrame && bridgeApexCharging)
        {
            prototype.ReleaseCommandBridgeApex();
            bridgeApexCharging = false;
        }

        bool marked = prototype.TryGetCommandBridgeMark(out Vector3 markedPoint, out string markedLabel);
        float range = marked ? Vector3.Distance(activeCamera.transform.position, markedPoint) : 0f;
        float readiness = prototype.GetCommandBridgeApexReadiness();
        if (visorReadout != null)
            visorReadout.text = marked
                ? visorRelation + " // " + markedLabel + " // " + Mathf.RoundToInt(range) + "m\nLMB REMARK  |  R HOLD/RELEASE APEX  |  APEX " +
                  (readiness < 0f ? "RECHARGE " + Mathf.CeilToInt(-readiness) + "s" : Mathf.RoundToInt(readiness * 100f) + "%")
                : "LMB MARK TARGET  |  TAB EXIT VISOR";
    }
}
