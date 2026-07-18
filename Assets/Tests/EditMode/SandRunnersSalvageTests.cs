using System;
using System.Reflection;
using NUnit.Framework;

public sealed class SandRunnersSalvageTests
{
    private Type profileType;
    private Type priceType;
    private Type rulesType;

    [SetUp]
    public void SetUp()
    {
        profileType = RequireType("SandRunnersBalanceProfile, Assembly-CSharp");
        priceType = RequireType("SandRunnersResourcePrice, Assembly-CSharp");
        rulesType = RequireType("SandRunnersSalvageRules, Assembly-CSharp");
    }

    [Test]
    public void SalvageScarabPriceMatchesReleaseBalance()
    {
        object profile = Activator.CreateInstance(profileType);
        AssertPrice(profileType.GetField("salvageScarab").GetValue(profile), 55f, 65f, 4f);
    }

    [Test]
    public void SalvagePayloadsMatchVehicleAndStructureClasses()
    {
        object profile = Activator.CreateInstance(profileType);
        AssertPrice(profileType.GetField("hostileVehicleSalvage").GetValue(profile), 8f, 5f, 0f);
        AssertPrice(profileType.GetField("juzzherBargeSalvage").GetValue(profile), 16f, 10f, 2f);
        AssertPrice(profileType.GetField("structureSalvage").GetValue(profile), 14f, 8f, 0f);
        AssertPrice(profileType.GetField("fortressSalvage").GetValue(profile), 18f, 18f, 4f);
    }

    [Test]
    public void MergePreservesPayloadAndHonorsPresentationLimits()
    {
        object a = Activator.CreateInstance(priceType, 8f, 5f, 0f);
        object b = Activator.CreateInstance(priceType, 16f, 10f, 2f);
        object merged = Invoke("Merge", a, b);
        AssertPrice(merged, 24f, 15f, 2f);
        Assert.That((bool)Invoke("MustMerge", 24, 24, 70, 72), Is.True);
        Assert.That((int)Invoke("AllowedPieces", 5, 70, 72), Is.EqualTo(2));
    }

    [Test]
    public void DeliveryCanOnlyConsumePayloadOnce()
    {
        object payload = Activator.CreateInstance(priceType, 14f, 8f, 0f);
        MethodInfo method = rulesType.GetMethod("TryConsume", BindingFlags.Static | BindingFlags.NonPublic);
        object[] firstArgs = { false, payload, null };
        Assert.That((bool)method.Invoke(null, firstArgs), Is.True);
        AssertPrice(firstArgs[2], 14f, 8f, 0f);
        object[] secondArgs = { firstArgs[0], payload, null };
        Assert.That((bool)method.Invoke(null, secondArgs), Is.False);
        AssertPrice(secondArgs[2], 0f, 0f, 0f);
    }

    [Test]
    public void ReservationRejectsSecondScarabAndKeepsOwnerValid()
    {
        Assert.That((bool)Invoke("CanClaim", false, false, false), Is.True);
        Assert.That((bool)Invoke("CanClaim", false, true, true), Is.True);
        Assert.That((bool)Invoke("CanClaim", false, true, false), Is.False);
        Assert.That((bool)Invoke("CanClaim", true, false, false), Is.False);
    }

    private object Invoke(string name, params object[] args)
    {
        return rulesType.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }

    private void AssertPrice(object price, float sand, float gold, float wind)
    {
        Assert.That((float)priceType.GetField("sand").GetValue(price), Is.EqualTo(sand));
        Assert.That((float)priceType.GetField("gold").GetValue(price), Is.EqualTo(gold));
        Assert.That((float)priceType.GetField("wind").GetValue(price), Is.EqualTo(wind));
    }

    private static Type RequireType(string name)
    {
        Type type = Type.GetType(name);
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }
}
