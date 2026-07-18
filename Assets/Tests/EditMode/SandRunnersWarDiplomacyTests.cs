using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SandRunnersWarDiplomacyTests
{
    private static Type RequireType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }

    private static object Invoke(Type type, string method, params object[] args)
    {
        MethodInfo info = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(info, Is.Not.Null, "Missing rule method " + type.Name + "." + method);
        return info.Invoke(null, args);
    }

    [Test]
    public void StrategicAiCadenceAndRecoveryMatchDesign()
    {
        Type profileType = RequireType("SandRunnersBalanceProfile");
        object profile = Activator.CreateInstance(profileType);
        Assert.That((float)profileType.GetField("aiThreatScanInterval").GetValue(profile), Is.EqualTo(0.25f));
        Assert.That((float)profileType.GetField("aiPlanInterval").GetValue(profile), Is.EqualTo(2f));
        Assert.That((float)profileType.GetField("navigationStuckSeconds").GetValue(profile), Is.EqualTo(4f));
        Assert.That((float)profileType.GetField("navigationMinimumProgress").GetValue(profile), Is.EqualTo(2f));
        Assert.That((int)profileType.GetField("navigationRecoveryAttempts").GetValue(profile), Is.EqualTo(3));
    }

    [Test]
    public void ThreatClustersChainNearbyDefences()
    {
        Type rules = RequireType("SandRunnersWarRules");
        Vector2[] points =
        {
            new Vector2(0f, 0f),
            new Vector2(20f, 0f),
            new Vector2(39f, 0f),
            new Vector2(100f, 0f)
        };
        int[] groups = (int[])Invoke(rules, "ClusterThreatSamples", points, 24f);
        Assert.That(groups[0], Is.EqualTo(groups[1]));
        Assert.That(groups[1], Is.EqualTo(groups[2]));
        Assert.That(groups[3], Is.Not.EqualTo(groups[0]));
    }

    [Test]
    public void FortressDefeatOnlyCompletesAtFinalStage()
    {
        Type rules = RequireType("SandRunnersWarRules");
        Assert.That((bool)Invoke(rules, "IsEarlyFortressDefeat", 4, 7), Is.True);
        Assert.That((bool)Invoke(rules, "IsEarlyFortressDefeat", 7, 7), Is.False);
        Assert.That((bool)Invoke(rules, "IsEarlyFortressDefeat", 8, 7), Is.False);
    }

    [Test]
    public void StuckRecoveryRequiresBothTimeoutAndLowProgress()
    {
        Type rules = RequireType("SandRunnersWarRules");
        Assert.That((bool)Invoke(rules, "RequiresStuckRecovery", 1.9f, 2f, 4f, 4f), Is.True);
        Assert.That((bool)Invoke(rules, "RequiresStuckRecovery", 2f, 2f, 4f, 4f), Is.False);
        Assert.That((bool)Invoke(rules, "RequiresStuckRecovery", 0f, 2f, 3.9f, 4f), Is.False);
    }

    [Test]
    public void ImperialStrikeScheduleAndVictoryRulesAreExact()
    {
        Type rules = RequireType("SandRunnersImperialRules");
        Assert.That((float)Invoke(rules, "NextStrikeDelay", true, 1800f, 360f), Is.EqualTo(1800f));
        Assert.That((float)Invoke(rules, "NextStrikeDelay", false, 1800f, 360f), Is.EqualTo(360f));
        Assert.That((float)Invoke(rules, "NextStrikeDelay", true, 0f, 0f), Is.EqualTo(1800f));
        Assert.That((float)Invoke(rules, "NextStrikeDelay", false, 0f, 0f), Is.EqualTo(360f));
        Assert.That((bool)Invoke(rules, "HasAlternativeVictory", 3, 3), Is.True);
        Assert.That((bool)Invoke(rules, "HasAlternativeVictory", 2, 3), Is.False);
        Assert.That((bool)Invoke(rules, "AllSettlementsLost", 0), Is.True);
        Assert.That((bool)Invoke(rules, "AllSettlementsLost", 1), Is.False);
    }

    [Test]
    public void ImperialCapsAndMissileTimingMatchDesign()
    {
        Type profileType = RequireType("SandRunnersBalanceProfile");
        object profile = Activator.CreateInstance(profileType);
        Assert.That((float)profileType.GetField("imperialFirstStrikeSeconds").GetValue(profile), Is.EqualTo(1800f));
        Assert.That((float)profileType.GetField("imperialRepeatStrikeSeconds").GetValue(profile), Is.EqualTo(360f));
        Assert.That((float)profileType.GetField("imperialMissileFlightSeconds").GetValue(profile), Is.EqualTo(25f));
        Assert.That((int)profileType.GetField("imperialGroundUnitLimit").GetValue(profile), Is.EqualTo(18));
        Assert.That((int)profileType.GetField("imperialAirUnitLimit").GetValue(profile), Is.EqualTo(10));
        Assert.That((int)profileType.GetField("imperialEngineerLimit").GetValue(profile), Is.EqualTo(4));
    }

    [Test]
    public void GiftsHaveDiminishingTrustAndContractsRequireAllianceStock()
    {
        Type rules = RequireType("SandRunnersDiplomacyRules");
        float fresh = (float)Invoke(rules, "GiftTrust", 100f, 0f);
        float established = (float)Invoke(rules, "GiftTrust", 100f, 80f);
        Assert.That(fresh, Is.GreaterThan(established));
        Assert.That((bool)Invoke(rules, "CanPurchaseContract", true, 1, true), Is.True);
        Assert.That((bool)Invoke(rules, "CanPurchaseContract", false, 1, true), Is.False);
        Assert.That((bool)Invoke(rules, "CanPurchaseContract", true, 0, true), Is.False);
        Assert.That((bool)Invoke(rules, "CanPurchaseContract", true, 1, false), Is.False);
    }

    [Test]
    public void SpecialContractPricesAndRestockMatchDesign()
    {
        Type profileType = RequireType("SandRunnersBalanceProfile");
        Type priceType = RequireType("SandRunnersResourcePrice");
        object profile = Activator.CreateInstance(profileType);
        AssertPrice(priceType, profileType.GetField("elementalWalkerContract").GetValue(profile), 90f, 240f, 36f);
        AssertPrice(priceType, profileType.GetField("grounderGradContract").GetValue(profile), 230f, 150f, 18f);
        Assert.That((float)profileType.GetField("factionContractRestockSeconds").GetValue(profile), Is.EqualTo(360f));
        Assert.That((float)profileType.GetField("diplomacyTimeScale").GetValue(profile), Is.EqualTo(0.15f));
    }

    [Test]
    public void SessionBootstrapConsumesRtsTransitionExactlyOnce()
    {
        Type bootstrap = RequireType("SandRunnersSessionBootstrap");
        Invoke(bootstrap, "Reset");
        Invoke(bootstrap, "RequestRtsStart", true);
        Assert.That((bool)bootstrap.GetProperty("PrologueSkipped").GetValue(null), Is.True);
        Assert.That((bool)Invoke(bootstrap, "ConsumeRtsStart"), Is.True);
        Assert.That((bool)Invoke(bootstrap, "ConsumeRtsStart"), Is.False);
        Invoke(bootstrap, "Reset");
    }

    [Test]
    public void SessionBootstrapTracksDirectRtsAndSafeMenuFallback()
    {
        Type session = RequireType("SandRunnersSessionBootstrap");
        Type routeType = RequireType("SandRunnersBootstrapRoute");
        Invoke(session, "Reset");

        Invoke(session, "RequestDirectRtsStart");
        Assert.That((bool)session.GetProperty("DirectRtsStart").GetValue(null), Is.True);
        Assert.That(session.GetProperty("RequestedRoute").GetValue(null), Is.EqualTo(Enum.Parse(routeType, "DirectRts")));
        Assert.That((bool)Invoke(session, "ConsumeRtsStart"), Is.True);
        Assert.That((bool)Invoke(session, "ConsumeRtsStart"), Is.False);

        Invoke(session, "RequestStartMenu", "broken transition");
        Assert.That((bool)Invoke(session, "ConsumeStartMenuReturn"), Is.True);
        Assert.That((string)session.GetProperty("LastTransitionError").GetValue(null), Is.EqualTo("broken transition"));
        Assert.That((bool)Invoke(session, "ConsumeStartMenuReturn"), Is.False);
        Invoke(session, "Reset");
    }

    [Test]
    public void BootstrapSceneConstantsPointAtStartupScenes()
    {
        Type bootstrap = RequireType("SandRunnersBootstrap");
        Assert.That((string)bootstrap.GetField("IntroScenePath").GetValue(null), Is.EqualTo("Assets/Scenes/SandRunners/SebekBedroomIntro.unity"));
        Assert.That((string)bootstrap.GetField("RtsScenePath").GetValue(null), Is.EqualTo("Assets/Scenes/SampleScene.unity"));
    }

    private static void AssertPrice(Type priceType, object price, float sand, float gold, float wind)
    {
        Assert.That((float)priceType.GetField("sand").GetValue(price), Is.EqualTo(sand));
        Assert.That((float)priceType.GetField("gold").GetValue(price), Is.EqualTo(gold));
        Assert.That((float)priceType.GetField("wind").GetValue(price), Is.EqualTo(wind));
    }
}
