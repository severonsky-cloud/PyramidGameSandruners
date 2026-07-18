using UnityEngine;

public enum SandRunnersDiplomacyBlockReason
{
    None,
    NoSettlement,
    OutOfRange,
    SettlementDestroyed,
    SettlementOccupied,
    AllianceRequired,
    OutOfStock,
    InsufficientSand,
    InsufficientGold,
    InsufficientWind
}

public static class SandRunnersDiplomacyContractCatalog
{
    public const int BlueElementalWalkerCount = 3;
    public const int GrounderGradCount = 5;
}

public sealed class SandRunnersDiplomacyPanelState
{
    public const float OpenRadius = 220f;
    public const float CloseRadius = 240f;
    public string Stage;
    public string NextStageRequirement;
    public string ContractPrice;
    public string StockStatus;
    public string BlockReason;
    public float Distance;
    public bool CanOpen;
    public bool CanPurchase;
    public SandRunnersDiplomacyBlockReason PurchaseBlockReason;
}

public sealed class SandRunnersDiplomacyPanelController
{
    private float previousTimeScale = 1f;
    private bool open;

    public bool IsOpen { get { return open; } }

    public void Open(float currentTimeScale)
    {
        if (open)
            return;
        previousTimeScale = currentTimeScale > 0f ? currentTimeScale : 1f;
        open = true;
    }

    public float Close(float currentTimeScale)
    {
        if (!open)
            return currentTimeScale;
        open = false;
        float restore = previousTimeScale > 0f ? previousTimeScale : 1f;
        previousTimeScale = 1f;
        return restore;
    }

    public SandRunnersDiplomacyPanelState BuildState(
        float distance, int deals, float trust, float influence, bool powerOnline, bool allied,
        bool settlementAlive, bool occupied, int stock, float restockSeconds,
        float sand, float gold, float wind, float priceSand, float priceGold, float priceWind)
    {
        SandRunnersDiplomacyPanelState state = new SandRunnersDiplomacyPanelState();
        state.Distance = distance;
        state.CanOpen = settlementAlive && !occupied && distance <= SandRunnersDiplomacyPanelState.OpenRadius;
        state.Stage = allied ? "ALLIED SETTLEMENT" : deals >= 4 || powerOnline ? "PROTECTED SETTLEMENT" : deals >= 2 ? "TRADING PARTNER" : "NEUTRAL";
        state.NextStageRequirement = GetNextStageRequirement(deals, trust, influence, powerOnline, allied);
        state.ContractPrice = Mathf.RoundToInt(priceSand) + " SAND / " + Mathf.RoundToInt(priceGold) + " GOLD / " + Mathf.RoundToInt(priceWind) + " WIND";
        state.StockStatus = stock > 0 ? "IN STOCK" : "RESTOCK " + Mathf.Max(0, Mathf.CeilToInt(restockSeconds)) + "s";
        state.PurchaseBlockReason = GetPurchaseBlockReason(allied, settlementAlive, occupied, stock, sand, gold, wind, priceSand, priceGold, priceWind);
        state.CanPurchase = state.PurchaseBlockReason == SandRunnersDiplomacyBlockReason.None;
        state.BlockReason = Describe(state.PurchaseBlockReason);
        return state;
    }

    public static SandRunnersDiplomacyBlockReason GetPurchaseBlockReason(
        bool allied, bool settlementAlive, bool occupied, int stock,
        float sand, float gold, float wind, float priceSand, float priceGold, float priceWind)
    {
        if (!settlementAlive) return SandRunnersDiplomacyBlockReason.SettlementDestroyed;
        if (occupied) return SandRunnersDiplomacyBlockReason.SettlementOccupied;
        if (!allied) return SandRunnersDiplomacyBlockReason.AllianceRequired;
        if (stock <= 0) return SandRunnersDiplomacyBlockReason.OutOfStock;
        if (sand < priceSand) return SandRunnersDiplomacyBlockReason.InsufficientSand;
        if (gold < priceGold) return SandRunnersDiplomacyBlockReason.InsufficientGold;
        if (wind < priceWind) return SandRunnersDiplomacyBlockReason.InsufficientWind;
        return SandRunnersDiplomacyBlockReason.None;
    }

    public static string GetNextStageRequirement(int deals, float trust, float influence, bool powerOnline, bool allied)
    {
        if (allied) return "MAXIMUM RELATION // ALLIANCE ACTIVE";
        if (deals < 2) return "NEXT: TRADING PARTNER // " + deals + "/2 DEVELOPMENT TRADES";
        if (deals < 4 && !powerOnline) return "NEXT: PROTECTED SETTLEMENT // " + deals + "/4 TRADES OR ESTABLISH A POWER LINK";
        return "NEXT: ALLIANCE // " + deals + "/4 TRADES, " + Mathf.RoundToInt(trust) + "/60 TRUST, " + Mathf.RoundToInt(influence) + "/60 INFLUENCE, " + (powerOnline ? "POWER ONLINE" : "POWER OFFLINE");
    }

    public static string Describe(SandRunnersDiplomacyBlockReason reason)
    {
        switch (reason)
        {
            case SandRunnersDiplomacyBlockReason.None: return "AVAILABLE";
            case SandRunnersDiplomacyBlockReason.SettlementDestroyed: return "BLOCKED: SETTLEMENT DESTROYED";
            case SandRunnersDiplomacyBlockReason.SettlementOccupied: return "BLOCKED: MARKET OCCUPIED";
            case SandRunnersDiplomacyBlockReason.AllianceRequired: return "BLOCKED: ALLIANCE REQUIRED";
            case SandRunnersDiplomacyBlockReason.OutOfStock: return "BLOCKED: CONTRACT OUT OF STOCK";
            case SandRunnersDiplomacyBlockReason.InsufficientSand: return "BLOCKED: INSUFFICIENT SAND";
            case SandRunnersDiplomacyBlockReason.InsufficientGold: return "BLOCKED: INSUFFICIENT GOLD";
            case SandRunnersDiplomacyBlockReason.InsufficientWind: return "BLOCKED: INSUFFICIENT WIND";
            default: return "BLOCKED: SETTLEMENT OUT OF RANGE";
        }
    }
}

public sealed class SandRunnersDiplomacyContractTransaction
{
    public float Sand;
    public float Gold;
    public float Wind;
    public int Stock;
    public int GrantedContracts;

    public bool TryPurchase(bool allied, bool settlementAlive, bool occupied, float priceSand, float priceGold, float priceWind)
    {
        if (SandRunnersDiplomacyPanelController.GetPurchaseBlockReason(allied, settlementAlive, occupied, Stock,
                Sand, Gold, Wind, priceSand, priceGold, priceWind) != SandRunnersDiplomacyBlockReason.None)
            return false;
        Sand -= priceSand;
        Gold -= priceGold;
        Wind -= priceWind;
        Stock = 0;
        GrantedContracts++;
        return true;
    }
}
