using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersOccupationTests
{
    private Type rules;

    [SetUp]
    public void SetUp()
    {
        rules = Type.GetType("SandRunnersOccupationRules, Assembly-CSharp");
        Assert.That(rules, Is.Not.Null);
    }

    [Test]
    public void OccupationNeedsBuildersAndRewardsOrganizedGarrison()
    {
        float none = (float)Invoke("CaptureDelta", 10f, 0, 4, 0f, 90f);
        float lone = (float)Invoke("CaptureDelta", 10f, 1, 1, 0f, 90f);
        float group = (float)Invoke("CaptureDelta", 10f, 1, 4, 0f, 90f);
        Assert.That(none, Is.EqualTo(0f));
        Assert.That(group, Is.GreaterThan(lone));
    }

    [Test]
    public void SettlementDefenseSlowsOccupationProgress()
    {
        float open = (float)Invoke("CaptureDelta", 10f, 1, 3, 0f, 90f);
        float fortified = (float)Invoke("CaptureDelta", 10f, 1, 3, 12f, 90f);
        Assert.That(fortified, Is.LessThan(open));
    }

    [Test]
    public void PopulationAttritionEscalatesByOccupationStage()
    {
        float occupied = (float)Invoke("PopulationDrainPerSecond", 2);
        float fortified = (float)Invoke("PopulationDrainPerSecond", 3);
        float exhausted = (float)Invoke("PopulationDrainPerSecond", 4);
        Assert.That(occupied, Is.GreaterThan(0f));
        Assert.That(fortified, Is.GreaterThan(occupied));
        Assert.That(exhausted, Is.GreaterThan(fortified));
    }

    [Test]
    public void SoftInfluenceFallsWithDistanceAndStopsAtRadius()
    {
        float center = (float)Invoke("InfluenceAt", 0f, 100f, 80f);
        float middle = (float)Invoke("InfluenceAt", 50f, 100f, 80f);
        float outside = (float)Invoke("InfluenceAt", 100f, 100f, 80f);
        Assert.That(center, Is.EqualTo(80f));
        Assert.That(middle, Is.GreaterThan(0f).And.LessThan(center));
        Assert.That(outside, Is.EqualTo(0f));
    }

    [Test]
    public void StrategicDirectorPrefersWeakerUnreservedTargets()
    {
        float open = (float)Invoke("StrategicTargetScore", 100f, 100f, 1f, 10f, false);
        float defended = (float)Invoke("StrategicTargetScore", 100f, 100f, 12f, 60f, false);
        float reserved = (float)Invoke("StrategicTargetScore", 100f, 100f, 1f, 10f, true);
        Assert.That(open, Is.GreaterThan(defended));
        Assert.That(open, Is.GreaterThan(reserved));
    }

    [Test]
    public void CastleKitRequiresStockAndBuildsOnlyOnce()
    {
        Assert.That((bool)Invoke("ResourceKitReady", 120f, 120f, false), Is.True);
        Assert.That((bool)Invoke("ResourceKitReady", 119.9f, 120f, false), Is.False);
        Assert.That((bool)Invoke("ResourceKitReady", 200f, 120f, true), Is.False);
    }

    [Test]
    public void OutpostDevelopmentHasThreeDeterministicStages()
    {
        Assert.That((int)Invoke("OutpostTier", 0f, 45f, 140f), Is.EqualTo(1));
        Assert.That((int)Invoke("OutpostTier", 45f, 45f, 140f), Is.EqualTo(2));
        Assert.That((int)Invoke("OutpostTier", 140f, 45f, 140f), Is.EqualTo(3));
    }

    [Test]
    public void ConvoyTravelTimeUsesDistanceAndConfiguredSpeed()
    {
        float ground = (float)Invoke("ConvoyTravelSeconds", 150f, 15f);
        float air = (float)Invoke("ConvoyTravelSeconds", 150f, 30f);
        Assert.That(ground, Is.EqualTo(10f).Within(0.001f));
        Assert.That(air, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void ResourceDoctrinesHaveDifferentDamageProfiles()
    {
        float sand = (float)Invoke("DoctrineDamage", 0, 3);
        float gold = (float)Invoke("DoctrineDamage", 1, 3);
        float wind = (float)Invoke("DoctrineDamage", 2, 3);
        Assert.That(sand, Is.GreaterThan(gold));
        Assert.That(gold, Is.GreaterThan(wind));
    }

    [Test]
    public void BalanceProfileExposesOccupationAndInfluenceTiming()
    {
        Type profileType = Type.GetType("SandRunnersBalanceProfile, Assembly-CSharp");
        object profile = Activator.CreateInstance(profileType);
        Assert.That((float)profileType.GetField("mandarinkaResourceCaptureSeconds").GetValue(profile), Is.EqualTo(70f));
        Assert.That((float)profileType.GetField("mandarinkaOccupationCaptureSeconds").GetValue(profile), Is.EqualTo(90f));
        Assert.That((float)profileType.GetField("mandarinkaOccupationFortifySeconds").GetValue(profile), Is.EqualTo(180f));
        Assert.That((float)profileType.GetField("mandarinkaOccupationExhaustSeconds").GetValue(profile), Is.EqualTo(360f));
        Assert.That((float)profileType.GetField("mandarinkaFortressInfluenceRadius").GetValue(profile), Is.EqualTo(190f));
        Assert.That((float)profileType.GetField("mandarinkaOutpostTierTwoSeconds").GetValue(profile), Is.EqualTo(45f));
        Assert.That((float)profileType.GetField("mandarinkaConvoyIntervalSeconds").GetValue(profile), Is.EqualTo(75f));
        Assert.That((int)profileType.GetField("mandarinkaOutpostGarrisonLimit").GetValue(profile), Is.EqualTo(4));
    }

    private object Invoke(string name, params object[] args)
    {
        MethodInfo method = rules.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing occupation rule " + name);
        return method.Invoke(null, args);
    }
}