public static class SandRunnersSessionBootstrap
{
    private static bool startRtsAfterPrologue;
    private static bool prologueSkipped;

    public static bool PrologueSkipped
    {
        get { return prologueSkipped; }
    }

    public static void RequestRtsStart(bool skipped)
    {
        startRtsAfterPrologue = true;
        prologueSkipped = skipped;
    }

    public static bool ConsumeRtsStart()
    {
        bool value = startRtsAfterPrologue;
        startRtsAfterPrologue = false;
        return value;
    }

    public static void Reset()
    {
        startRtsAfterPrologue = false;
        prologueSkipped = false;
    }
}
