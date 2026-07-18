using UnityEngine;

// Allocation-free, deterministic rules for the Mandarinka strategic loop.
internal static class SandRunnersMandarinkaStrategicRules
{
    internal enum AttackPhase { Recon, Suppression, Breach, Exploitation, Occupation }

    internal static float TargetScore(float value, float intel, float distance, float defense, float hostileInfluence, float routeSafety, bool reserved)
    {
        return value + Mathf.Clamp01(intel) * 30f + Mathf.Clamp01(routeSafety) * 24f - Mathf.Max(0f, distance) * 0.035f - Mathf.Max(0f, defense) * 2.4f - Mathf.Max(0f, hostileInfluence) * 0.18f - (reserved ? 42f : 0f);
    }

    internal static bool CastleMayAdvance(int forwardCoverage, bool routeOperational, float preparation, float localThreat)
    {
        return forwardCoverage >= 2 && routeOperational && preparation >= 1f && localThreat <= 1f;
    }

    internal static AttackPhase SelectAttackPhase(float intel, int mirrorTurrets, int counterBatteryTargets, int artillery, int breachUnits, bool settlementEncircled)
    {
        if (intel < 0.65f) return AttackPhase.Recon;
        if ((mirrorTurrets > 0 || counterBatteryTargets > 0) && artillery > 0) return AttackPhase.Suppression;
        if (breachUnits >= 3 && !settlementEncircled) return AttackPhase.Breach;
        return settlementEncircled ? AttackPhase.Occupation : AttackPhase.Exploitation;
    }

    internal static bool CanDeliverLogistics(bool sourceAlive, bool routeOperational, bool convoyAlive)
    {
        return sourceAlive && routeOperational && convoyAlive;
    }

    internal static bool CanAfford(float sand, float gold, float wind, float supply, float sandCost, float goldCost, float windCost, float supplyCost)
    {
        return sand >= sandCost && gold >= goldCost && wind >= windCost && supply >= supplyCost;
    }

    internal static float LiberationRecovery(float population)
    {
        return Mathf.Lerp(0.28f, 0.72f, Mathf.Clamp01(population / 100f));
    }
}
