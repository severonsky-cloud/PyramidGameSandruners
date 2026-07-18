internal enum HorusDevelopmentOrderState
{
    Moving,
    Capturing,
    Developing,
    WaitingForControl,
    Complete,
    Invalid
}

internal static class SandRunnersHorusDevelopmentRules
{
    private const int FortressCrusherDeveloperCapability = 1 << 6;

    internal static HorusDevelopmentOrderState EvaluateState(
        bool hasOrder, bool horusAlive, bool targetValid, bool stationed, bool controlled,
        float capture, float captureStrength, int level, int maxLevel)
    {
        if (!hasOrder || !horusAlive || !targetValid)
            return HorusDevelopmentOrderState.Invalid;
        if (level >= maxLevel)
            return HorusDevelopmentOrderState.Complete;
        if (!stationed)
            return HorusDevelopmentOrderState.Moving;
        if (!controlled)
            return captureStrength > 0f || capture > 0f
                ? HorusDevelopmentOrderState.Capturing
                : HorusDevelopmentOrderState.WaitingForControl;
        return HorusDevelopmentOrderState.Developing;
    }

    internal static bool IsFortressCrusherDeveloper(int capabilityMask)
    {
        return (capabilityMask & FortressCrusherDeveloperCapability) != 0;
    }

    internal static bool ShouldApplyFortressCrusherTransformation(bool hasDeveloperCapability, bool alreadyTransformed)
    {
        return hasDeveloperCapability && !alreadyTransformed;
    }
}
