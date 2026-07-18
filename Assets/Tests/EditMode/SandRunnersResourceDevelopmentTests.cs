using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersResourceDevelopmentTests
{
    private Type profileType;
    private Type rulesType;

    [SetUp]
    public void SetUp()
    {
        profileType = RequireType("SandRunnersBalanceProfile");
        rulesType = RequireType("SandRunnersResourceDevelopmentRules");
    }

    [Test]
    public void ResourceDevelopmentBalanceIsExplicit()
    {
        object profile = Activator.CreateInstance(profileType);
        Assert.That(ReadFloat(profile, "resourceCapturePerDeveloper"), Is.EqualTo(0.16f));
        Assert.That(ReadFloat(profile, "resourceDevelopmentTierMultiplier"), Is.EqualTo(1.72f));
        Assert.That(ReadFloat(profile, "mineIncomePerLevel"), Is.EqualTo(0.5f));
        Assert.That(ReadFloat(profile, "mineConvoyBaseSeconds"), Is.EqualTo(36f));
        Assert.That((int)profileType.GetField("mineConvoyLimit").GetValue(profile), Is.EqualTo(8));
        Assert.That(ReadFloat(profile, "mineConvoyEscortRange"), Is.EqualTo(72f));
        Assert.That(ReadFloat(profile, "mineConvoyEscortDamage"), Is.EqualTo(9f));
        Assert.That(ReadFloat(profile, "cargoFlyerSpeed"), Is.EqualTo(21f));
        Assert.That(ReadFloat(profile, "cargoFlyerDefenseRange"), Is.EqualTo(84f));
        Assert.That(ReadFloat(profile, "cargoFlyerDefenseDamage"), Is.EqualTo(18f));
    }

    [Test]
    public void MultipleDevelopersCaptureFasterWithoutChangingPyramidCapture()
    {
        Assert.That((float)Invoke("CaptureRate", 1f, 0.16f), Is.EqualTo(0.16f));
        Assert.That((float)Invoke("CaptureRate", 3f, 0.16f), Is.EqualTo(0.48f).Within(0.0001f));
        Assert.That((float)Invoke("CaptureRate", -2f, 0.16f), Is.EqualTo(0f));
    }

    [Test]
    public void MineLevelsIncreasePassiveIncome()
    {
        Assert.That((float)Invoke("IncomeMultiplier", 0, 0.5f), Is.EqualTo(1f));
        Assert.That((float)Invoke("IncomeMultiplier", 1, 0.5f), Is.EqualTo(1.5f));
        Assert.That((float)Invoke("IncomeMultiplier", 3, 0.5f), Is.EqualTo(2.5f));
    }

    [Test]
    public void PermanentInfrastructureHasThreeLevelsAndTransformationsAreOneShot()
    {
        Assert.That((int)Invoke("MaxLevel", 0), Is.EqualTo(3), "Mine");
        Assert.That((int)Invoke("MaxLevel", 1), Is.EqualTo(3), "Scarab fort");
        Assert.That((int)Invoke("MaxLevel", 2), Is.EqualTo(3), "Horus tree");
        Assert.That((int)Invoke("MaxLevel", 3), Is.EqualTo(1), "Crusher ascension");
        Assert.That((int)Invoke("MaxLevel", 4), Is.EqualTo(1), "Thoth blessing");
        Assert.That((int)Invoke("MaxLevel", 5), Is.EqualTo(1), "Salvage hive");
    }

    [Test]
    public void SandGoldAndWindKeepDistinctUpgradeKits()
    {
        Assert.That((int)Invoke("ResourceKit", 0), Is.EqualTo(0));
        Assert.That((int)Invoke("ResourceKit", 1), Is.EqualTo(1));
        Assert.That((int)Invoke("ResourceKit", 2), Is.EqualTo(2));
    }

    [Test]
    public void TransformedCarriersAndHiveExposePromisedClassCounts()
    {
        Assert.That((int)Invoke("ThothAircraftClassCount"), Is.EqualTo(4));
        Assert.That((int)Invoke("ThothDeckCount", false), Is.EqualTo(4));
        Assert.That((int)Invoke("ThothDeckCount", true), Is.EqualTo(8));
        Assert.That((int)Invoke("CurseHiveClassCount"), Is.EqualTo(5));
    }

    [Test]
    public void DevelopedMineConvoysGainBoundedAirEscort()
    {
        Assert.That((int)Invoke("ConvoyEscortCount", 0), Is.EqualTo(1));
        Assert.That((int)Invoke("ConvoyEscortCount", 1), Is.EqualTo(1));
        Assert.That((int)Invoke("ConvoyEscortCount", 2), Is.EqualTo(2));
        Assert.That((int)Invoke("ConvoyEscortCount", 3), Is.EqualTo(2));
    }

    [Test]
    public void DevelopmentTimeGrowsByConfiguredTierMultiplier()
    {
        Assert.That((float)Invoke("TierSeconds", 22f, 0, 1.72f), Is.EqualTo(22f));
        Assert.That((float)Invoke("TierSeconds", 22f, 1, 1.72f), Is.EqualTo(37.84f).Within(0.001f));
        Assert.That((float)Invoke("TierSeconds", 22f, 2, 1.72f), Is.GreaterThan(60f));
    }

    private float ReadFloat(object profile, string field)
    {
        return (float)profileType.GetField(field).GetValue(profile);
    }

    private object Invoke(string method, params object[] args)
    {
        MethodInfo info = rulesType.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing resource development rule " + method);
        return info.Invoke(null, args);
    }

    private static Type RequireType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }
}