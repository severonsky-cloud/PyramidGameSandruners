public enum SandRunnersBootstrapRoute
{
    StartMenu,
    FullPrologue,
    SkipPrologue,
    DirectRts
}

public static class SandRunnersSessionBootstrap
{
    private static bool startRtsAfterPrologue;
    private static bool prologueSkipped;
    private static bool returnToStartMenu;
    private static bool directRtsStart;
    private static SandRunnersBootstrapRoute requestedRoute = SandRunnersBootstrapRoute.StartMenu;
    private static string lastTransitionError;

    public static bool PrologueSkipped
    {
        get { return prologueSkipped; }
    }

    public static bool DirectRtsStart
    {
        get { return directRtsStart; }
    }

    public static SandRunnersBootstrapRoute RequestedRoute
    {
        get { return requestedRoute; }
    }

    public static string LastTransitionError
    {
        get { return lastTransitionError; }
    }

    public static void RequestFullPrologue()
    {
        startRtsAfterPrologue = false;
        prologueSkipped = false;
        directRtsStart = false;
        returnToStartMenu = false;
        requestedRoute = SandRunnersBootstrapRoute.FullPrologue;
        lastTransitionError = null;
    }

    public static void RequestRtsStart(bool skipped)
    {
        startRtsAfterPrologue = true;
        prologueSkipped = skipped;
        directRtsStart = false;
        returnToStartMenu = false;
        requestedRoute = skipped ? SandRunnersBootstrapRoute.SkipPrologue : SandRunnersBootstrapRoute.FullPrologue;
        lastTransitionError = null;
    }

    public static void RequestDirectRtsStart()
    {
        startRtsAfterPrologue = true;
        prologueSkipped = false;
        directRtsStart = true;
        returnToStartMenu = false;
        requestedRoute = SandRunnersBootstrapRoute.DirectRts;
        lastTransitionError = null;
    }

    public static void RequestStartMenu(string transitionError = null)
    {
        startRtsAfterPrologue = false;
        prologueSkipped = false;
        directRtsStart = false;
        returnToStartMenu = true;
        requestedRoute = SandRunnersBootstrapRoute.StartMenu;
        lastTransitionError = transitionError;
    }

    public static bool ConsumeRtsStart()
    {
        bool value = startRtsAfterPrologue;
        startRtsAfterPrologue = false;
        return value;
    }

    public static bool ConsumeStartMenuReturn()
    {
        bool value = returnToStartMenu;
        returnToStartMenu = false;
        return value;
    }

    public static void Reset()
    {
        startRtsAfterPrologue = false;
        prologueSkipped = false;
        returnToStartMenu = false;
        directRtsStart = false;
        requestedRoute = SandRunnersBootstrapRoute.StartMenu;
        lastTransitionError = null;
    }
}
