using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersEvolutionTests
{
    private Type rules;

    [SetUp]
    public void SetUp()
    {
        rules = Type.GetType("SandRunnersEvolutionRules, Assembly-CSharp");
        Assert.That(rules, Is.Not.Null);
    }

    [Test]
    public void GiantBloomExposesSixteenRepairEmitters()
    {
        Assert.That((int)Invoke("GiantBloomEmitterCount"), Is.EqualTo(16));
    }

    [Test]
    public void SettlementDevelopmentProducesThreeVisibleCityTiers()
    {
        Assert.That((int)Invoke("SettlementCityTier", 0), Is.EqualTo(0));
        Assert.That((int)Invoke("SettlementCityTier", 2), Is.EqualTo(1));
        Assert.That((int)Invoke("SettlementCityTier", 5), Is.EqualTo(2));
        Assert.That((int)Invoke("SettlementCityTier", 9), Is.EqualTo(3));
    }

    [Test]
    public void AllianceRequiresReachableTradeAndPowerThresholds()
    {
        Assert.That((bool)Invoke("AllianceReady", 4, 80f, 64f, true), Is.True);
        Assert.That((bool)Invoke("AllianceReady", 3, 100f, 100f, true), Is.False);
        Assert.That((bool)Invoke("AllianceReady", 4, 80f, 80f, false), Is.False);
    }

    [Test]
    public void AutonomousCrusherScansBeyondItsWeaponRange()
    {
        Assert.That((float)Invoke("StrategicAcquisitionRange", true, 36f), Is.EqualTo(180f));
        Assert.That((float)Invoke("StrategicAcquisitionRange", false, 36f), Is.EqualTo(43f));
    }

    private object Invoke(string name, params object[] args)
    {
        MethodInfo method = rules.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing evolution rule " + name);
        return method.Invoke(null, args);
    }
}
