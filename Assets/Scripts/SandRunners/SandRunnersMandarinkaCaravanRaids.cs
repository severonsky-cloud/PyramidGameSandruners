using UnityEngine;

public partial class SandRunnersPrototype
{
    private int mandarinkaCaravanRaidQuota;
    private int mandarinkaCaravanRaidCount;
    private float mandarinkaCaravanRaidTimer = 18f;
    private bool mandarinkaAvoidsCaravanRoutes;
    private bool mandarinkaCaravanCounterstrikeTriggered;
    private Transform neutralCounterstrikeNashornRoot;

    private void UpdateMandarinkaCaravanRaids(float dt)
    {
        if (!mandarinkaEncounterInitialized || mandarinkaDefeated || mandarinkaAvoidsCaravanRoutes)
            return;
        if (mandarinkaStrategyPhase != MandarinkaStrategyPhase.ScoutRaid && mandarinkaStrategyPhase != MandarinkaStrategyPhase.ResourceRaid)
            return;
        if (tradeCaravans.Count == 0)
            return;
        if (mandarinkaCaravanRaidQuota <= 0)
            mandarinkaCaravanRaidQuota = Random.Range(balanceProfile.raidQuotaMin, balanceProfile.raidQuotaMax + 1);

        mandarinkaCaravanRaidTimer -= dt;
        if (mandarinkaCaravanRaidTimer > 0f || mandarinkaCaravanRaidCount >= mandarinkaCaravanRaidQuota)
            return;
        mandarinkaCaravanRaidTimer = Random.Range(balanceProfile.raidIntervalMin, balanceProfile.raidIntervalMax);

        TradeCaravanState caravan = FindMandarinkaCaravanRaidTarget();
        if (caravan == null || caravan.root == null)
            return;
        mandarinkaCaravanRaidCount++;
        caravan.raided = true;
        caravan.health = Mathf.Max(1f, caravan.health - Random.Range(32f, 58f));
        caravan.cargo = Mathf.Max(0.1f, caravan.cargo - Random.Range(0.16f, 0.24f));
        AssignMandarinkaRaidersToCaravan(caravan);
        SettlementDevelopmentState destination;
        if (caravan.destination != null && settlementDevelopment.TryGetValue(caravan.destination, out destination))
            destination.tradeTrust = Mathf.Max(0f, destination.tradeTrust - 5f);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "Mandarinka raided caravan " + mandarinkaCaravanRaidCount + "/" + mandarinkaCaravanRaidQuota + ".", 12f, false);
        lastEvent = "Mandarinka caravan raid " + mandarinkaCaravanRaidCount + "/" + mandarinkaCaravanRaidQuota + ". Intercept her raiders before the cargo is lost.";
        ShowBanner("MANDARINKA CARAVAN RAID " + mandarinkaCaravanRaidCount + "/" + mandarinkaCaravanRaidQuota, 2.8f);
        PlaySandRunnerSound(SandRunnerSound.NeutralAlert, caravan.root.position, 0.75f);
        if (mandarinkaCaravanRaidCount >= mandarinkaCaravanRaidQuota)
            TriggerNeutralCaravanCounterstrike();
    }

    private TradeCaravanState FindMandarinkaCaravanRaidTarget()
    {
        TradeCaravanState best = null;
        float bestDistance = float.MaxValue;
        Vector3 origin = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position : Vector3.zero;
        for (int i = 0; i < tradeCaravans.Count; i++)
        {
            TradeCaravanState caravan = tradeCaravans[i];
            if (caravan == null || caravan.root == null || caravan.health <= 0f)
                continue;
            float distance = FlatDistance(origin, caravan.root.position);
            if (distance < bestDistance)
            {
                best = caravan;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void AssignMandarinkaRaidersToCaravan(TradeCaravanState caravan)
    {
        int assigned = 0;
        for (int i = 0; i < mandarinkaAssets.Count; i++)
        {
            MandarinkaAsset asset = mandarinkaAssets[i];
            if (asset == null || asset.transform == null || asset.enemy == null || asset.enemy.health <= 0f)
                continue;
            if (asset.role != MandarinkaRole.GroundCrawler && asset.role != MandarinkaRole.AirJunk)
                continue;
            asset.objective = caravan.root.position;
            asset.hasObjective = true;
            assigned++;
            if (assigned >= 2)
                break;
        }
        if (assigned == 0 && mandarinkaFortressRoot != null)
        {
            CreateBeam(mandarinkaFortressRoot.position + Vector3.up * 8f, caravan.root.position + Vector3.up * 1.2f, new Color(1f, 0.12f, 0.04f, 1f), 0.08f, 0.22f);
            caravan.health = Mathf.Max(1f, caravan.health - 18f);
        }
    }

    private void TriggerNeutralCaravanCounterstrike()
    {
        if (mandarinkaCaravanCounterstrikeTriggered)
            return;
        mandarinkaCaravanCounterstrikeTriggered = true;
        mandarinkaAvoidsCaravanRoutes = true;
        mandarinkaStrategyPhase = MandarinkaStrategyPhase.SiegeProbe;
        mandarinkaStrategyTimer = Mathf.Max(mandarinkaStrategyTimer, 300f);

        Vector3 strikeStart = FindNeutralCounterstrikeOrigin();
        Vector3 strikeTarget = mandarinkaFortressRoot != null ? mandarinkaFortressRoot.position : strikeStart + Vector3.forward * 40f;
        Vector3 counterstrikeMuzzle = SpawnNeutralCounterstrikeNashorn(strikeStart, strikeTarget);
        CreateBeam(counterstrikeMuzzle, strikeTarget + Vector3.up * 8f, new Color(0.12f, 0.3f, 1f, 1f), 0.18f, 0.7f);
        CreateHostileImpactVisual(strikeTarget, 22f, true);
        mandarinkaFortressShield = Mathf.Max(0f, mandarinkaFortressShield - 1200f);
        if (mandarinkaFortressEnemy != null)
            mandarinkaFortressEnemy.health = Mathf.Max(1f, mandarinkaFortressEnemy.health - 420f);
        PushLivingWorldEvent(LivingWorldEventType.Raid, "Blue and Black Elementals deployed a Nashorn warning salvo against Mandarinka.", 18f, true);
        SetMandarinkaRadio("MANDARINKA: Withdraw from their roads. Circle the desert and approach the pyramid from the flank.");
        lastEvent = "Neutral Nashorn fire drove Mandarinka off the caravan routes. She is now advancing by a long flank.";
        ShowBanner("NEUTRALS FORCE MANDARINKA OFF THE TRADE ROADS", 3.8f);
        PlaySandRunnerSound(SandRunnerSound.GustavWarning, strikeTarget, 1f);
    }

    private Vector3 SpawnNeutralCounterstrikeNashorn(Vector3 position, Vector3 target)
    {
        if (neutralCounterstrikeNashornRoot == null)
        {
            neutralCounterstrikeNashornRoot = new GameObject("Neutral_Nashorn_Counterstrike").transform;
            if (livingWorldRoot != null)
                neutralCounterstrikeNashornRoot.SetParent(livingWorldRoot, false);
            Vector3 direction = target - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.1f)
                direction = Vector3.forward;
            neutralCounterstrikeNashornRoot.position = position + direction.normalized * 18f;
            neutralCounterstrikeNashornRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Material black = elementalBlackMaterial != null ? elementalBlackMaterial : siegeUnitMaterial;
            Material glow = traderBlueMaterial != null ? traderBlueMaterial : commandMaterial;
            CreateBox(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Chassis", new Vector3(0f, 0.8f, 0f), Quaternion.identity, new Vector3(4.8f, 1.2f, 7.2f), black, true);
            CreateBox(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Casemate", new Vector3(0f, 2.2f, -0.4f), Quaternion.identity, new Vector3(4f, 2.4f, 4.1f), black, true);
            CreateCylinder(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Barrel", new Vector3(0f, 3.25f, 6.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f, 5f, 0.42f), black, true);
            for (int side = -1; side <= 1; side += 2)
                CreateBox(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Track_" + side, new Vector3(side * 2.55f, 0.5f, 0f), Quaternion.identity, new Vector3(0.72f, 0.85f, 7.4f), black, true);
            CreatePointLight(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Core", new Vector3(0f, 3f, 0.8f), new Color(0.12f, 0.32f, 1f, 1f), 1.5f, 24f);
            CreatePointLight(neutralCounterstrikeNashornRoot, "Neutral_Nashorn_Muzzle_Glow", new Vector3(0f, 3.25f, 11.2f), new Color(0.28f, 0.62f, 1f, 1f), 2f, 30f);
        }
        return neutralCounterstrikeNashornRoot.TransformPoint(new Vector3(0f, 3.25f, 11.2f));
    }

    private Vector3 FindNeutralCounterstrikeOrigin()
    {
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement != null && settlement.root != null && settlement.kind == NeutralFactionKind.BlackElementals && settlement.health > 0f)
                return settlement.root.position;
        }
        return battlePyramid.position - battlePyramid.right * 80f;
    }

    private Vector3 GetMandarinkaCaravanBypassObjective()
    {
        float side = mandarinkaFortressRoot != null && mandarinkaFortressRoot.position.x >= battlePyramid.position.x ? 1f : -1f;
        return GetDunePoint(new Vector3(side * (mapHalfSize - 130f), 0f, battlePyramid.position.z + side * 150f));
    }
}
