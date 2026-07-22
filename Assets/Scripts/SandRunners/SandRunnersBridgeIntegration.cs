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
}
