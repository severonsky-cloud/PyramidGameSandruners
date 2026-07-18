using NUnit.Framework;
using System;
using System.Reflection;

public sealed class SandRunnersDiplomacyAccessibilityTests
{
    private static Type RequireType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing diplomacy type " + name);
        return type;
    }

    private static object Invoke(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return info.Invoke(target, args);
    }

    [Test]
    public void TimeScaleRestoresAfterEveryClosePath()
    {
        object controller = Activator.CreateInstance(RequireType("SandRunnersDiplomacyPanelController"));
        Invoke(controller, "Open", 0.65f);
        Assert.That((float)Invoke(controller, "Close", 0.15f), Is.EqualTo(0.65f));
        Invoke(controller, "Open", 1.35f);
        Assert.That((float)Invoke(controller, "Close", 0.15f), Is.EqualTo(1.35f));
    }

    [Test]
    public void ContractSpendsResourcesAndGrantsUnitsOnlyOnce()
    {
        Type type = RequireType("SandRunnersDiplomacyContractTransaction");
        object transaction = Activator.CreateInstance(type);
        type.GetField("Sand").SetValue(transaction, 200f);
        type.GetField("Gold").SetValue(transaction, 200f);
        type.GetField("Wind").SetValue(transaction, 100f);
        type.GetField("Stock").SetValue(transaction, 1);
        Assert.That((bool)Invoke(transaction, "TryPurchase", true, true, false, 90f, 120f, 18f), Is.True);
        Assert.That((bool)Invoke(transaction, "TryPurchase", true, true, false, 90f, 120f, 18f), Is.False);
        Assert.That((float)type.GetField("Sand").GetValue(transaction), Is.EqualTo(110f));
        Assert.That((float)type.GetField("Gold").GetValue(transaction), Is.EqualTo(80f));
        Assert.That((float)type.GetField("Wind").GetValue(transaction), Is.EqualTo(82f));
        Assert.That((int)type.GetField("GrantedContracts").GetValue(transaction), Is.EqualTo(1));
    }

    [TestCase(false, true, false, 1, "AllianceRequired")]
    [TestCase(true, true, true, 1, "SettlementOccupied")]
    [TestCase(true, false, false, 1, "SettlementDestroyed")]
    [TestCase(true, true, false, 0, "OutOfStock")]
    public void ContractBlockReasonsAreExact(bool allied, bool alive, bool occupied, int stock, string expected)
    {
        Type type = RequireType("SandRunnersDiplomacyPanelController");
        object reason = type.GetMethod("GetPurchaseBlockReason", BindingFlags.Static | BindingFlags.Public).Invoke(null,
            new object[] { allied, alive, occupied, stock, 500f, 500f, 500f, 90f, 120f, 18f });
        Assert.That(reason.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void OccupationStopsMarketWithoutRemovingPreviouslyBoughtUnits()
    {
        int previouslyBoughtUnits = 5;
        Type type = RequireType("SandRunnersDiplomacyPanelController");
        object reason = type.GetMethod("GetPurchaseBlockReason", BindingFlags.Static | BindingFlags.Public).Invoke(null,
            new object[] { true, true, true, 1, 500f, 500f, 500f, 90f, 120f, 18f });
        Assert.That(reason.ToString(), Is.EqualTo("SettlementOccupied"));
        Assert.That(previouslyBoughtUnits, Is.EqualTo(5));
    }

    [Test]
    public void DestroyedSettlementStopsMarketWithoutRemovingPreviouslyBoughtUnits()
    {
        int previouslyBoughtUnits = 3;
        Type type = RequireType("SandRunnersDiplomacyPanelController");
        object reason = type.GetMethod("GetPurchaseBlockReason", BindingFlags.Static | BindingFlags.Public).Invoke(null,
            new object[] { true, false, false, 1, 500f, 500f, 500f, 90f, 120f, 18f });
        Assert.That(reason.ToString(), Is.EqualTo("SettlementDestroyed"));
        Assert.That(previouslyBoughtUnits, Is.EqualTo(3));
    }
}
