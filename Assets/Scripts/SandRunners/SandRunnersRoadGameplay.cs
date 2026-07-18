using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private enum RoadRouteResult { None, OldWall, WindPass, SafeCanyon }

    private Transform roadGameplayCaravan;
    private bool roadCaravanChoiceActive;
    private bool roadCaravanResolved;
    private bool roadAmbushSpawned;
    private bool roadAmbushRewarded;
    private readonly List<Transform> roadGameplayRaiders = new List<Transform>();
    private bool roadRockfallBuilt;
    private RoadRouteResult roadRouteResult;
    private float roadGameplayTravel;
    private Vector3 roadGameplayPreviousPosition;
    private float roadGameplayQuietTime;
    private bool roadRepairRequestIssued;

    private void InitializeRoadGameplay()
    {
        roadGameplayPreviousPosition = battlePyramid != null ? battlePyramid.position : Vector3.zero;
    }

    private void UpdateRoadGameplay(float dt)
    {
        if (battlePyramid == null || guidedMissileActive || gunnerSide != 0) return;
        if (roadGameplayCaravan == null)
        {
            GameObject go = GameObject.Find("Visible damaged caravan");
            if (go != null) roadGameplayCaravan = go.transform;
        }

        float moved = FlatDistance(battlePyramid.position, roadGameplayPreviousPosition);
        roadGameplayTravel += moved;
        roadGameplayQuietTime += moved > 0.05f ? dt : dt * 0.2f;
        roadGameplayPreviousPosition = battlePyramid.position;

        UpdateRoadCaravanChoice();
        UpdateRoadOasisRequest();
        UpdateRoadAmbush();
        UpdateRoadRockfall();
        UpdateRoadRouteResult();
    }

    private void UpdateRoadCaravanChoice()
    {
        if (roadCaravanResolved || roadGameplayCaravan == null) return;
        float distance = FlatDistance(battlePyramid.position, roadGameplayCaravan.position);
        if (!roadCaravanChoiceActive && (distance < 68f || roadGameplayQuietTime > 55f))
        {
            roadCaravanChoiceActive = true;
            roadGameplayQuietTime = 0f;
            lastEvent = "CARAVAN DISTRESS: [1] promise repair, [2] emergency trade, [3] leave.";
            ShowBanner("DAMAGED CARAVAN // 1 REPAIR  2 TRADE  3 LEAVE", 5f);
        }
        if (!roadCaravanChoiceActive || distance > 90f || Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            roadCaravanResolved = true;
            roadCaravanChoiceActive = false;
            DamageFirstRoadOasis();
            gold += 25f;
            lastEvent = "The caravan accepts the royal repair oath and guides the pyramid to the oasis workshop.";
            ShowBanner("REPAIR OATH // +TRUST +25 GOLD", 4f);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (gold < 20f)
            {
                lastEvent = "Emergency caravan parts cost 20 gold.";
                return;
            }
            gold -= 20f;
            wind = Mathf.Min(windCapacity, wind + 90f);
            roadCaravanResolved = true;
            roadCaravanChoiceActive = false;
            lastEvent = "Emergency trade complete. The caravan moves, but no repair trust was earned.";
            ShowBanner("CARAVAN TRADE // -20 GOLD +90 WIND", 4f);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            roadCaravanResolved = true;
            roadCaravanChoiceActive = false;
            lastEvent = "The pyramid leaves the caravan behind. The road remains politically silent.";
            ShowBanner("CARAVAN LEFT BEHIND", 3f);
        }
    }

    private void DamageFirstRoadOasis()
    {
        if (starterOasisSettlement != null)
            starterOasisSettlement.health = Mathf.Min(starterOasisSettlement.health, starterOasisSettlement.maxHealth * 0.55f);
        horusRequestScanTimer = 0f;
        roadRepairRequestIssued = true;
    }

    private void UpdateRoadOasisRequest()
    {
        if (!roadCaravanResolved || roadRepairRequestIssued || starterOasis == null) return;
        if (FlatDistance(battlePyramid.position, starterOasis.position) > 82f) return;
        DamageFirstRoadOasis();
        lastEvent = "FIRST ROAD OASIS: the pump and caravan workshop need royal repair.";
        ShowBanner("OASIS REPAIR REQUEST // TOUCH OF HORUS", 5f);
    }

    private void UpdateRoadAmbush()
    {
        Vector3 ambushPoint = new Vector3(-402f, 0f, -384f);
        if (!roadAmbushSpawned && FlatDistance(battlePyramid.position, ambushPoint) < 78f)
        {
            roadAmbushSpawned = true;
            SpawnRoadGameplayRaiders(ambushPoint, 4);
            lastEvent = "Raiders rise from cover around the old caravan wall.";
            ShowBanner("OLD-WALL AMBUSH // PROTECT THE ROAD", 4f);
        }
        if (!roadAmbushSpawned || roadAmbushRewarded) return;
        for (int i = 0; i < roadGameplayRaiders.Count; i++)
            if (roadGameplayRaiders[i] != null) return;

        roadAmbushRewarded = true;
        gold += 35f;
        wind = Mathf.Min(windCapacity, wind + 30f);
        lastEvent = "The ambush is broken. Caravan salvage yields gold and compressed wind.";
        ShowBanner("AMBUSH DEFEATED // +35 GOLD +30 WIND", 4f);
    }

    private void SpawnRoadGameplayRaiders(Vector3 focus, int count)
    {
        roadGameplayRaiders.Clear();
        for (int i = 0; i < count; i++)
        {
            float a = (30f + i * 88f) * Mathf.Deg2Rad;
            Vector3 p = focus + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (36f + (i % 2) * 10f);
            p.y = GetPlayableGroundHeight(p) + 0.6f;
            GameObject enemy = new GameObject("First_Road_Raider");
            enemy.transform.position = p;
            Vector3 look = battlePyramid.position - p; look.y = 0f;
            if (look.sqrMagnitude > 0.1f) enemy.transform.rotation = Quaternion.LookRotation(look);
            BuildRedPursuerVisual(enemy.transform);
            EnemyUnit unit = new EnemyUnit { transform = enemy.transform, health = 54f + i * 4f, maxHealth = 54f + i * 4f };
            enemies.Add(unit);
            roadGameplayRaiders.Add(enemy.transform);
        }
    }

    private void UpdateRoadRockfall()
    {
        Vector3 point = new Vector3(-362f, 0f, -344f);
        if (roadRockfallBuilt || FlatDistance(battlePyramid.position, point) > 88f) return;
        roadRockfallBuilt = true;
        for (int i = -2; i <= 2; i++)
        {
            Vector3 p = point + new Vector3(i * 7f, 0f, Mathf.Abs(i) * 2f);
            CreateRouteObstacle("Breakable canyon rockfall", p, new Vector3(6f, 5f + Mathf.Abs(i), 5f),
                PyramidObstacleKind.HeavyBreakable, 85f, 4.8f, gobiRockMaterial);
        }
        lastEvent = "The road divides: breach the wall, spend wind on the pass, or take the safe canyon.";
        ShowBanner("THREE ROUTES // WALL  WIND PASS  SAFE CANYON", 5f);
    }

    private void UpdateRoadRouteResult()
    {
        if (!roadRockfallBuilt || roadRouteResult != RoadRouteResult.None) return;
        Vector3 start = new Vector3(-627f, 0f, -589f);
        Vector3 r = battlePyramid.position - start;
        if (r.x < 255f || r.z < 205f) return;

        if (r.x > 330f && r.z > 270f)
        {
            if (wind < 65f)
            {
                lastEvent = "The wind pass needs 65 reserve. Recharge or choose another road.";
                return;
            }
            wind -= 65f;
            gold += 55f;
            roadRouteResult = RoadRouteResult.WindPass;
            lastEvent = "The pyramid conquers the pass and takes the high-ground cache.";
            ShowBanner("WIND PASS // -65 WIND +55 GOLD", 5f);
        }
        else if (r.x > 275f && r.z < 240f)
        {
            wind = Mathf.Min(windCapacity, wind + 35f);
            gold += 20f;
            roadRouteResult = RoadRouteResult.SafeCanyon;
            lastEvent = "The safe canyon costs time, but its workshop replenishes the expedition.";
            ShowBanner("SAFE CANYON // TRADE AND REPAIR", 5f);
        }
        else
        {
            gold += 30f;
            roadRouteResult = RoadRouteResult.OldWall;
            lastEvent = "The old wall lies broken. The shortest road is open.";
            ShowBanner("OLD WALL BREACHED // SHORT ROUTE", 5f);
        }
    }

    private void DrawRoadGameplayHUD()
    {
        if (roadCaravanChoiceActive)
        {
            Rect r = new Rect(Screen.width * 0.5f - 260f, Screen.height - 116f, 520f, 82f);
            GUI.Box(r, "DAMAGED CARAVAN");
            GUI.Label(new Rect(r.x + 16f, r.y + 28f, 490f, 48f), "[1] Promise royal repair    [2] Trade 20 gold for 90 wind    [3] Leave");
        }
        if (roadRouteResult != RoadRouteResult.None)
            GUI.Label(new Rect(Screen.width - 420f, 448f, 390f, 30f), "ROUTE COMPLETED: " + roadRouteResult);
    }

    public string RunRoadGameplaySmokeTest()
    {
        bool caravan = GameObject.Find("Visible damaged caravan") != null;
        bool oasis = starterOasisSettlement != null;
        int before = enemies.Count;
        SpawnRoadGameplayRaiders(battlePyramid.position + battlePyramid.forward * 60f, 2);
        bool raiders = enemies.Count == before + 2 && roadGameplayRaiders.Count == 2;
        for (int i = 0; i < roadGameplayRaiders.Count; i++)
            if (roadGameplayRaiders[i] != null) Destroy(roadGameplayRaiders[i].gameObject);
        return (caravan && oasis && raiders ? "PASS" : "FAIL") +
               ": caravan=" + caravan + ", oasis=" + oasis + ", raiders=" + raiders;
    }
}
