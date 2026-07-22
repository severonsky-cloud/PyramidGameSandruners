public partial class SandRunnersPrototype
{
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
        hudHidden = active;
        PublishStrategicUiEvent(StrategicUiEventKind.StrategicCanvasVisibility, !active);
        if (active)
            SetCommandCursorMode(false);
    }
}