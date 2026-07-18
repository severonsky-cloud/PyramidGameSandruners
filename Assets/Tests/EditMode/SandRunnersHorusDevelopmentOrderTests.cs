using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersHorusDevelopmentOrderTests
{
    private Type rulesType;

    [SetUp]
    public void SetUp()
    {
        rulesType = Type.GetType("SandRunnersHorusDevelopmentRules, Assembly-CSharp");
        Assert.That(rulesType, Is.Not.Null);
    }

    [Test]
    public void HorusProgressesFromMovementToDevelopment()
    {
        Assert.That(Evaluate(true, true, true, false, false, 0f, 0f, 0, 3), Is.EqualTo("Moving"));
        Assert.That(Evaluate(true, true, true, true, true, 1f, 1f, 0, 3), Is.EqualTo("Developing"));
    }

    [Test]
    public void WaitingForCaptureKeepsOrderState()
    {
        Assert.That(Evaluate(true, true, true, true, false, 0f, 0f, 0, 3), Is.EqualTo("WaitingForControl"));
        Assert.That(Evaluate(true, true, true, true, false, 0.2f, 1f, 0, 3), Is.EqualTo("Capturing"));
    }

    [Test]
    public void LostPointBecomesInvalid()
    {
        Assert.That(Evaluate(true, true, false, true, true, 1f, 1f, 0, 3), Is.EqualTo("Invalid"));
    }

    [Test]
    public void FortressCrusherUsesCapabilityNotDisplayName()
    {
        Assert.That((bool)Invoke("IsFortressCrusherDeveloper", 1 << 6), Is.True);
        Assert.That((bool)Invoke("IsFortressCrusherDeveloper", 0), Is.False);
    }

    [Test]
    public void FortressCrusherTransformationIsOneShot()
    {
        Assert.That((bool)Invoke("ShouldApplyFortressCrusherTransformation", true, false), Is.True);
        Assert.That((bool)Invoke("ShouldApplyFortressCrusherTransformation", true, true), Is.False);
    }

    [Test]
    public void MultipleDevelopersKeepSharedProjectAndIndividualRewardsSafe()
    {
        Assert.That(Evaluate(true, true, true, true, true, 1f, 2f, 0, 3), Is.EqualTo("Developing"));
        Assert.That(Evaluate(true, true, true, true, true, 1f, 2f, 3, 3), Is.EqualTo("Complete"));
        Assert.That((bool)Invoke("ShouldApplyFortressCrusherTransformation", true, true), Is.False);
    }

    private string Evaluate(bool hasOrder, bool alive, bool targetValid, bool stationed, bool controlled,
        float capture, float captureStrength, int level, int maxLevel)
    {
        return Invoke("EvaluateState", hasOrder, alive, targetValid, stationed, controlled,
            capture, captureStrength, level, maxLevel).ToString();
    }

    private object Invoke(string method, params object[] args)
    {
        MethodInfo info = rulesType.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing Horus rule " + method);
        return info.Invoke(null, args);
    }
}
