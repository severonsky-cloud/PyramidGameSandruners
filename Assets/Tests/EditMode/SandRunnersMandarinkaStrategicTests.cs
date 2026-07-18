using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersMandarinkaStrategicTests
{
    private Type rules;

    [SetUp]
    public void SetUp()
    {
        rules = Type.GetType("SandRunnersMandarinkaStrategicRules, Assembly-CSharp");
        Assert.That(rules, Is.Not.Null);
    }

    [Test]
    public void TargetChoicePrefersScoutedSafeResourceOverCloserHostileOne()
    {
        float scouted = (float)Invoke("TargetScore", 110f, 1f, 160f, 3f, 15f, 0.9f, false);
        float hostile = (float)Invoke("TargetScore", 110f, 0.1f, 80f, 11f, 100f, 0.1f, false);
        Assert.That(scouted, Is.GreaterThan(hostile));
    }

    [Test]
    public void AttackPlanSuppressesMirrorAndCounterBatteryClustersBeforeBreach()
    {
        object phase = Invoke("SelectAttackPhase", 1f, 3, 2, 1, 6, false);
        Assert.That(phase.ToString(), Is.EqualTo("Suppression"));
    }

    [Test]
    public void CastleRemainsInOperationalZoneUntilCoverageAndRouteExist()
    {
        Assert.That((bool)Invoke("CastleMayAdvance", 1, true, 1f, 0f), Is.False);
        Assert.That((bool)Invoke("CastleMayAdvance", 2, false, 1f, 0f), Is.False);
        Assert.That((bool)Invoke("CastleMayAdvance", 2, true, 1f, 0f), Is.True);
    }

    [Test]
    public void AttritionMakesLiberationRecoveryHarder()
    {
        float early = (float)Invoke("LiberationRecovery", 85f);
        float exhausted = (float)Invoke("LiberationRecovery", 12f);
        Assert.That(early, Is.GreaterThan(exhausted));
    }

    [Test]
    public void DestroyedRouteStopsLogisticsAndPreventsPaidProduction()
    {
        Assert.That((bool)Invoke("CanDeliverLogistics", true, false, true), Is.False);
        Assert.That((bool)Invoke("CanDeliverLogistics", true, true, false), Is.False);
        Assert.That((bool)Invoke("CanAfford", 12f, 4f, 0f, 24f, 12f, 4f, 0f, 24f), Is.True);
        Assert.That((bool)Invoke("CanAfford", 11.9f, 4f, 0f, 24f, 12f, 4f, 0f, 24f), Is.False);
    }

    private object Invoke(string name, params object[] args)
    {
        MethodInfo method = rules.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing strategic rule " + name);
        return method.Invoke(null, args);
    }
}
