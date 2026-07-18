using UnityEngine;

public partial class SandRunnersPrototype
{
    private bool damageBenchmarkActive;
    private float damageBenchmarkTimer;
    private float damageBenchmarkOriginalHull;

    private void StartDamageBenchmark()
    {
        if (damageBenchmarkActive || cinematicDirectorActive || battlePyramid == null)
            return;

        damageBenchmarkActive = true;
        damageBenchmarkTimer = 0f;
        damageBenchmarkOriginalHull = pyramidHull;
        lastEvent = "DAMAGE BENCHMARK: staged subsystem damage test started.";
        ShowBanner("DAMAGE BENCHMARK // F6", 3f);
    }

    private void UpdateDamageBenchmark(float dt)
    {
        if (!damageBenchmarkActive)
            return;

        damageBenchmarkTimer += dt;
        if (damageBenchmarkTimer < 0.15f)
        {
            pyramidHull = pyramidMaxHull * 0.72f;
            ApplyPyramidSubsystemDamage(pyramidMaxHull * 0.22f, DamageHitType.Kinetic);
            CreateBattleExplosionFx(battlePyramid.position + Vector3.up * 4f, 5.5f, false, true);
            PlaySandRunnerSound(SandRunnerSound.PyramidDamage, battlePyramid.position, 0.8f);
            ShowBanner("BENCHMARK // MOBILITY + HULL DAMAGE", 2.5f);
        }
        else if (damageBenchmarkTimer >= 3f && damageBenchmarkTimer < 3.15f)
        {
            pyramidHull = pyramidMaxHull * 0.48f;
            ApplyPyramidSubsystemDamage(pyramidMaxHull * 0.28f, DamageHitType.Energy);
            CreateBattleExplosionFx(battlePyramid.position + Vector3.up * 5f, 7f, false, true);
            PlaySandRunnerSound(SandRunnerSound.HeavyImpact, battlePyramid.position, 0.85f);
            ShowBanner("BENCHMARK // WEAPONS + PRODUCTION DAMAGE", 2.5f);
        }
        else if (damageBenchmarkTimer >= 6f && damageBenchmarkTimer < 6.15f)
        {
            pyramidHull = pyramidMaxHull * 0.24f;
            ApplyPyramidSubsystemDamage(pyramidMaxHull * 0.34f, DamageHitType.Siege);
            CreateBattleExplosionFx(battlePyramid.position + Vector3.up * 6f, 9f, false, true);
            PlaySandRunnerSound(SandRunnerSound.LargeExplosion, battlePyramid.position, 0.9f);
            ShowBanner("BENCHMARK // CRITICAL / DISABLED STATE", 3f);
        }
        else if (damageBenchmarkTimer >= 10f && damageBenchmarkTimer < 10.15f)
        {
            pyramidHull = Mathf.Min(pyramidMaxHull, damageBenchmarkOriginalHull);
            ApplyPyramidRepair(pyramidMaxHull);
            PlaySandRunnerSound(SandRunnerSound.AircraftRepair, battlePyramid.position, 0.8f);
            ShowBanner("BENCHMARK // HORUS / BUILDER REPAIR RECOVERY", 3f);
        }
        else if (damageBenchmarkTimer >= 13f)
        {
            damageBenchmarkActive = false;
            damageBenchmarkTimer = 0f;
            lastEvent = "Damage benchmark complete. Live damage systems restored.";
            ShowBanner("DAMAGE BENCHMARK COMPLETE", 3f);
        }
    }
}
