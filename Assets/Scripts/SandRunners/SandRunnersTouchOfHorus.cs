using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private enum HorusRepairKind { Pyramid, Runner, Structure, Settlement, Caravan }

    private sealed class RepairTarget
    {
        public HorusRepairKind kind;
        public Transform transform;
        public string name;
        public RunnerUnit runner;
        public GoldenStructure structure;
        public NeutralSettlement settlement;
        public TradeCaravanState caravan;
    }

    private sealed class RepairRequest
    {
        public RepairTarget target;
        public float requiredRepair;
        public float repaired;
        public float remainingTime;
        public string reward;
        public float influence;
        public bool completed;
    }

    private sealed class TouchOfHorusState
    {
        public Transform root;
        public Transform core;
        public readonly List<Transform> rings = new List<Transform>();
        public readonly List<Transform> emitters = new List<Transform>();
        public readonly List<LineRenderer> streams = new List<LineRenderer>();
        public float health = 500f;
        public float maxHealth = 500f;
        public float solarDust = 400f;
        public float maxSolarDust = 400f;
        public int level = 1;
        public float trustExperience;
        public int blockedEmitters;
        public float emitterBlockTimer;
        public RepairTarget pinnedTarget;
        public Vector3 moveDestination;
        public bool hasMoveDestination;
        public float eyeTimer;
        public float eyeCooldown;
        public float reconCooldown;
        public float feathersTimer;
        public float feathersCooldown;
        public float resurrectionTimer;
        public float pyramidRepairReserve = 240f;
        public int activeStreams;
    }

    private TouchOfHorusState touchOfHorus;
    private RepairRequest activeHorusRepairRequest;
    private bool touchOfHorusRevealed;
    private float horusRequestScanTimer;
    private int horusCompletedRequests;
    private Material horusGoldMaterial;
    private Material horusGreenMaterial;
    private Material horusDarkMaterial;

    private Rect GetTouchOfHorusContextRect()
    {
        return new Rect(18f, 308f, 392f, 244f);
    }

    private bool IsPointerOverTouchOfHorusContext(Vector2 screenPosition)
    {
        return touchOfHorusRevealed && touchOfHorus != null && GetTouchOfHorusContextRect().Contains(screenPosition);
    }

    private void InitializeTouchOfHorus()
    {
        horusRequestScanTimer = 3f;
    }

    private void UpdateTouchOfHorus(float dt)
    {
        UpdateHorusDiplomacy(dt);
        if (!touchOfHorusRevealed)
            return;

        if (touchOfHorus == null || touchOfHorus.root == null)
            return;

        TouchOfHorusState h = touchOfHorus;
        h.eyeCooldown = Mathf.Max(0f, h.eyeCooldown - dt);
        h.reconCooldown = Mathf.Max(0f, h.reconCooldown - dt);
        h.feathersCooldown = Mathf.Max(0f, h.feathersCooldown - dt);
        h.eyeTimer = Mathf.Max(0f, h.eyeTimer - dt);
        h.feathersTimer = Mathf.Max(0f, h.feathersTimer - dt);
        h.pyramidRepairReserve = Mathf.Min(240f + h.level * 35f, h.pyramidRepairReserve + dt * 5f);
        if (h.emitterBlockTimer > 0f)
        {
            h.emitterBlockTimer -= dt;
            if (h.emitterBlockTimer <= 0f) h.blockedEmitters = 0;
        }

        if (IsResourceDeveloperAssigned(h))
        {
            AnimateTouchOfHorus(dt);
            UpdateHorusDust(dt);
            return;
        }

        AnimateTouchOfHorus(dt);
        HandleTouchOfHorusAbilities();
        UpdateHorusMovement(dt);
        UpdateHorusDust(dt);
        UpdateHorusRepair(dt);
        UpdateHorusRequest(dt);
        UpdateHorusProgression();
    }

    private void RevealTouchOfHorus(RepairTarget requestTarget)
    {
        if (touchOfHorusRevealed) return;
        touchOfHorusRevealed = true;
        SpawnTouchOfHorus();
        activeHorusRepairRequest = new RepairRequest
        {
            target = requestTarget,
            requiredRepair = Mathf.Max(40f, GetRepairMax(requestTarget) - GetRepairHealth(requestTarget)),
            remainingTime = 240f,
            reward = "influence, supplies and intelligence",
            influence = 20f
        };
        touchOfHorus.pinnedTarget = requestTarget;
        SelectStrategicTransform(touchOfHorus.root, "Touch of Horus");
        lastEvent = "ROYAL SECRET REVEALED: Touch of Horus is active and bound to the repair plea. RMB a damaged ally only to change priority.";
        ShowBanner("TOUCH OF HORUS — the Golden Elementals reveal their royal repair relic.", 5f);
    }

    private void SpawnTouchOfHorus()
    {
        if (touchOfHorus != null && touchOfHorus.root != null) return;
        EnsureHorusMaterials();
        TouchOfHorusState h = new TouchOfHorusState();
        GameObject root = new GameObject("Touch of Horus - Royal Repair Relic");
        h.root = root.transform;
        Vector3 p = battlePyramid != null ? battlePyramid.position + battlePyramid.right * 22f + Vector3.up * 14f : Vector3.up * 14f;
        h.root.position = p;
        SphereCollider selector = root.AddComponent<SphereCollider>();
        selector.radius = 5.5f;

        h.core = CreateHorusPrimitive(PrimitiveType.Sphere, "Post-Elemental Falcon Core", h.root, Vector3.zero, new Vector3(2.8f, 4.2f, 2.8f), horusGreenMaterial);
        Transform torso = CreateHorusPrimitive(PrimitiveType.Capsule, "Luminous Half-Human Body", h.root, new Vector3(0f, -0.4f, 0f), new Vector3(1.7f, 2.5f, 1.7f), horusGoldMaterial);
        Transform head = CreateHorusPrimitive(PrimitiveType.Sphere, "Falcon Head", h.root, new Vector3(0f, 2.5f, 0.1f), new Vector3(1.7f, 1.35f, 1.5f), horusGreenMaterial);
        CreateHorusPrimitive(PrimitiveType.Cube, "Falcon Beak", head, new Vector3(0f, -0.05f, 0.85f), new Vector3(0.45f, 0.35f, 1.4f), horusGoldMaterial);
        CreateHorusPrimitive(PrimitiveType.Sphere, "Solar Disk", h.root, new Vector3(0f, 4.1f, 0f), new Vector3(2.2f, 0.45f, 2.2f), horusGoldMaterial);
        CreateHorusPrimitive(PrimitiveType.Cylinder, "Lower Stabilizer", h.root, new Vector3(0f, -4.2f, 0f), new Vector3(1.1f, 1.8f, 1.1f), horusDarkMaterial);

        for (int i = 0; i < 3; i++)
        {
            Transform ring = new GameObject("Armillary Ring " + (i + 1)).transform;
            ring.SetParent(h.root, false);
            ring.localRotation = Quaternion.Euler(i == 0 ? 0f : 60f, i * 55f, i * 35f);
            float radius = 5.2f + i * 0.65f;
            for (int s = 0; s < 28; s++)
            {
                float a = s / 28f * Mathf.PI * 2f;
                Transform bead = CreateHorusPrimitive(PrimitiveType.Cube, "Rune", ring,
                    new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                    new Vector3(0.32f, 0.75f, 0.32f), horusGoldMaterial);
                bead.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            h.rings.Add(ring);
        }

        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Transform emitter = CreateHorusPrimitive(PrimitiveType.Sphere, "Emitter " + (i + 1), h.root,
                new Vector3(Mathf.Cos(a) * 6.8f, Mathf.Sin(a) * 2.1f, Mathf.Sin(a) * 6.8f),
                Vector3.one * 0.85f, i == 0 ? horusGreenMaterial : horusDarkMaterial);
            h.emitters.Add(emitter);
            GameObject beamObject = new GameObject("Nanostream " + (i + 1));
            beamObject.transform.SetParent(root.transform, false);
            LineRenderer beam = beamObject.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.startWidth = 0.32f;
            beam.endWidth = 0.09f;
            beam.material = horusGreenMaterial;
            beam.textureMode = LineTextureMode.Tile;
            beam.enabled = false;
            h.streams.Add(beam);
        }
        touchOfHorus = h;
    }

    private void EnsureHorusMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        horusGoldMaterial = new Material(shader) { color = new Color(1f, 0.66f, 0.08f) };
        horusGreenMaterial = new Material(shader) { color = new Color(0.08f, 1f, 0.28f) };
        horusDarkMaterial = new Material(shader) { color = new Color(0.09f, 0.08f, 0.04f) };
        SetEmission(horusGoldMaterial, new Color(1f, 0.35f, 0.02f) * 2f);
        SetEmission(horusGreenMaterial, new Color(0.02f, 1f, 0.15f) * 5f);
    }

    private void SetEmission(Material material, Color color)
    {
        if (material != null && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
        }
    }

    private Transform CreateHorusPrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = material;
        return go.transform;
    }

    private void AnimateTouchOfHorus(float dt)
    {
        TouchOfHorusState h = touchOfHorus;
        for (int i = 0; i < h.rings.Count; i++)
            h.rings[i].Rotate((i % 2 == 0 ? Vector3.up : Vector3.right), dt * (18f + i * 9f), Space.Self);
        if (h.core != null) h.core.localScale = new Vector3(2.8f, 4.2f, 2.8f) * (1f + Mathf.Sin(Time.time * 3f) * 0.04f);
        for (int i = 0; i < h.emitters.Count; i++)
        {
            bool unlocked = i < h.level && i >= h.blockedEmitters;
            Renderer r = h.emitters[i].GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = unlocked ? horusGreenMaterial : horusDarkMaterial;
        }
    }

    private void HandleTouchOfHorusAbilities()
    {
        // The royal context panel owns these actions. Number keys remain free.
    }

    private void UpdateHorusMovement(float dt)
    {
        TouchOfHorusState h = touchOfHorus;
        Vector3 destination = h.root.position;
        bool moving = false;
        if (IsRepairTargetValid(h.pinnedTarget) && GetRepairHealth(h.pinnedTarget) < GetRepairMax(h.pinnedTarget) - 0.1f)
        {
            destination = h.pinnedTarget.transform.position + Vector3.up * 10f;
            moving = Vector3.Distance(h.root.position, destination) > 20f;
        }
        else if (h.hasMoveDestination)
        {
            destination = h.moveDestination + Vector3.up * 11f;
            moving = Vector3.Distance(h.root.position, destination) > 2f;
            if (!moving) h.hasMoveDestination = false;
        }
        if (moving)
        {
            Vector3 next = Vector3.MoveTowards(h.root.position, destination, dt * 17f);
            h.root.position = next;
            Vector3 look = destination - next; look.y = 0f;
            if (look.sqrMagnitude > 1f) h.root.rotation = Quaternion.Slerp(h.root.rotation, Quaternion.LookRotation(look), dt * 4f);
        }
        h.root.position += Vector3.up * Mathf.Sin(Time.time * 2f) * dt * 0.25f;
    }

    private void UpdateHorusDust(float dt)
    {
        TouchOfHorusState h = touchOfHorus;
        if (battlePyramid != null && Vector3.Distance(h.root.position, battlePyramid.position) < 42f)
            h.solarDust = Mathf.Min(h.maxSolarDust, h.solarDust + 30f * dt);
        foreach (EnergyNetworkState network in energyNetworks)
            if (network != null && network.online && network.root != null && Vector3.Distance(h.root.position, network.root.position) < 32f)
                h.solarDust = Mathf.Min(h.maxSolarDust, h.solarDust + 18f * dt);
    }

    private void UpdateHorusRepair(float dt)
    {
        TouchOfHorusState h = touchOfHorus;
        List<RepairTarget> targets = GatherRepairTargets();
        int available = Mathf.Clamp(h.level - h.blockedEmitters, 0, 8);
        if (h.feathersTimer > 0f) available = Mathf.Max(available, 8);
        h.activeStreams = 0;
        for (int i = 0; i < h.streams.Count; i++) h.streams[i].enabled = false;
        if (available == 0 || h.solarDust <= 0f) return;

        int streamIndex = 0;
        for (int i = 0; i < targets.Count && streamIndex < available; i++)
        {
            RepairTarget t = targets[i];
            if (!IsRepairTargetValid(t) || Vector3.Distance(h.root.position, t.transform.position) > 95f) continue;
            int streamsForTarget = (t == h.pinnedTarget) ? Mathf.Min(available - streamIndex, Mathf.Max(1, available - targets.Count + 1)) : 1;
            for (int s = 0; s < streamsForTarget && streamIndex < available; s++, streamIndex++)
            {
                float efficiency = 1f / (1f + s * 0.35f);
                float rate = 12f * efficiency * (h.eyeTimer > 0f ? 1.55f : 1f) * (h.feathersTimer > 0f ? 1.25f : 1f);
                float amount = Mathf.Min(rate * dt, h.solarDust * 1.7f);
                if (t.kind == HorusRepairKind.Pyramid) amount = Mathf.Min(amount, h.pyramidRepairReserve);
                float before = GetRepairHealth(t);
                ApplyRepair(t, amount);
                float actual = Mathf.Max(0f, GetRepairHealth(t) - before);
                h.solarDust = Mathf.Max(0f, h.solarDust - actual * 0.58f);
                if (t.kind == HorusRepairKind.Pyramid) h.pyramidRepairReserve -= actual;
                if (activeHorusRepairRequest != null && activeHorusRepairRequest.target == t) activeHorusRepairRequest.repaired += actual;
                LineRenderer beam = h.streams[streamIndex];
                beam.enabled = actual > 0f;
                if (beam.enabled)
                {
                    beam.SetPosition(0, h.emitters[streamIndex].position);
                    beam.SetPosition(1, t.transform.position + Vector3.up * 2f);
                    h.activeStreams++;
                }
            }
        }
        h.trustExperience += h.activeStreams * dt * 0.8f;
    }

    private List<RepairTarget> GatherRepairTargets()
    {
        List<RepairTarget> list = new List<RepairTarget>();
        TouchOfHorusState h = touchOfHorus;
        AddRepairTarget(list, h.pinnedTarget);
        RepairTarget pyramid = MakePyramidTarget();
        if (pyramidHull < pyramidMaxHull * 0.7f) AddRepairTarget(list, pyramid);
        for (int i = 0; i < runners.Count; i++)
            if (runners[i] != null && runners[i].transform != null && runners[i].health < runners[i].maxHealth) AddRepairTarget(list, MakeRunnerTarget(runners[i]));
        for (int i = 0; i < goldenStructures.Count; i++)
            if (goldenStructures[i] != null && goldenStructures[i].transform != null && goldenStructures[i].health < goldenStructures[i].maxHealth) AddRepairTarget(list, MakeStructureTarget(goldenStructures[i]));
        if (activeHorusRepairRequest != null && !activeHorusRepairRequest.completed) AddRepairTarget(list, activeHorusRepairRequest.target);
        for (int i = 0; i < neutralSettlements.Count; i++)
            if (neutralSettlements[i] != null && neutralSettlements[i].root != null && neutralSettlements[i].health < neutralSettlements[i].maxHealth) AddRepairTarget(list, MakeSettlementTarget(neutralSettlements[i]));
        for (int i = 0; i < tradeCaravans.Count; i++)
            if (tradeCaravans[i] != null && tradeCaravans[i].root != null && tradeCaravans[i].health < 180f) AddRepairTarget(list, MakeCaravanTarget(tradeCaravans[i]));
        return list;
    }

    private void AddRepairTarget(List<RepairTarget> list, RepairTarget target)
    {
        if (!IsRepairTargetValid(target) || GetRepairHealth(target) >= GetRepairMax(target) - 0.1f) return;
        for (int i = 0; i < list.Count; i++) if (list[i].transform == target.transform) return;
        list.Add(target);
    }

    private RepairTarget FindDistressedNeutralTarget()
    {
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement s = neutralSettlements[i];
            if (s != null && s.root != null && s.health < s.maxHealth * 0.72f) return MakeSettlementTarget(s);
        }
        for (int i = 0; i < tradeCaravans.Count; i++)
        {
            TradeCaravanState c = tradeCaravans[i];
            if (c != null && c.root != null && c.health < 125f) return MakeCaravanTarget(c);
        }
        return null;
    }

    private void UpdateHorusRequest(float dt)
    {
        RepairRequest request = activeHorusRepairRequest;
        if (request == null || request.completed) return;
        request.remainingTime -= dt;
        if (GetRepairHealth(request.target) >= GetRepairMax(request.target) * 0.96f || request.repaired >= request.requiredRepair)
        {
            request.completed = true;
            horusCompletedRequests++;
            touchOfHorus.trustExperience += 80f;
            gold += 60f; wind += 35f;
            if (settlementDevelopment.TryGetValue(request.target.settlement, out SettlementDevelopmentState development))
            {
                development.playerInfluence += request.influence;
                development.developmentPoints += 2;
            }
            lastEvent = "Neutral repair fulfilled: allies grant " + request.reward + ".";
            ShowBanner("REPAIR PACT FULFILLED — influence, supplies and trust gained.", 4f);
        }
        else if (request.remainingTime <= 0f)
        {
            request.completed = true;
            lastEvent = "The neutral repair request expired. No diplomatic penalty.";
        }
    }

    private void UpdateHorusProgression()
    {
        TouchOfHorusState h = touchOfHorus;
        if (h.level >= 8) return;
        float threshold = 70f + h.level * 55f;
        int next = h.level + 1;
        bool requestGate = next < 4 || next == 5 || next == 7 || horusCompletedRequests >= (next == 4 ? 1 : next == 6 ? 2 : 3);
        if (h.trustExperience >= threshold && requestGate)
        {
            h.trustExperience -= threshold;
            h.level++;
            h.maxSolarDust += 45f;
            h.solarDust = h.maxSolarDust;
            lastEvent = "Touch of Horus attunes emitter " + h.level + " of eight.";
        }
    }

    private bool IsRepairTargetValid(RepairTarget t) { return t != null && t.transform != null && GetRepairMax(t) > 0f; }
    private float GetRepairHealth(RepairTarget t)
    {
        if (t == null) return 0f;
        switch (t.kind)
        {
            case HorusRepairKind.Pyramid: return pyramidHull;
            case HorusRepairKind.Runner: return t.runner != null ? t.runner.health : 0f;
            case HorusRepairKind.Structure: return t.structure != null ? t.structure.health : 0f;
            case HorusRepairKind.Settlement: return t.settlement != null ? t.settlement.health : 0f;
            case HorusRepairKind.Caravan: return t.caravan != null ? t.caravan.health : 0f;
        }
        return 0f;
    }
    private float GetRepairMax(RepairTarget t)
    {
        if (t == null) return 0f;
        switch (t.kind)
        {
            case HorusRepairKind.Pyramid: return pyramidMaxHull;
            case HorusRepairKind.Runner: return t.runner != null ? t.runner.maxHealth : 0f;
            case HorusRepairKind.Structure: return t.structure != null ? t.structure.maxHealth : 0f;
            case HorusRepairKind.Settlement: return t.settlement != null ? t.settlement.maxHealth : 0f;
            case HorusRepairKind.Caravan: return 180f;
        }
        return 0f;
    }
    private void ApplyRepair(RepairTarget t, float amount)
    {
        if (!IsRepairTargetValid(t) || amount <= 0f) return;
        switch (t.kind)
        {
            case HorusRepairKind.Pyramid: pyramidHull = Mathf.Min(pyramidMaxHull, pyramidHull + amount); ApplyPyramidRepair(amount); break;
            case HorusRepairKind.Runner: t.runner.health = Mathf.Min(t.runner.maxHealth, t.runner.health + amount); break;
            case HorusRepairKind.Structure: t.structure.health = Mathf.Min(t.structure.maxHealth, t.structure.health + amount); ApplyStructureRepair(t.structure, amount); break;
            case HorusRepairKind.Settlement: t.settlement.health = Mathf.Min(t.settlement.maxHealth, t.settlement.health + amount); break;
            case HorusRepairKind.Caravan: t.caravan.health = Mathf.Min(180f, t.caravan.health + amount); break;
        }
    }

    private RepairTarget MakePyramidTarget() { return new RepairTarget { kind = HorusRepairKind.Pyramid, transform = battlePyramid, name = "Battle Pyramid" }; }
    private RepairTarget MakeRunnerTarget(RunnerUnit r) { return new RepairTarget { kind = HorusRepairKind.Runner, transform = r.transform, runner = r, name = r.displayName }; }
    private RepairTarget MakeStructureTarget(GoldenStructure s) { return new RepairTarget { kind = HorusRepairKind.Structure, transform = s.transform, structure = s, name = s.displayName }; }
    private RepairTarget MakeSettlementTarget(NeutralSettlement s) { return new RepairTarget { kind = HorusRepairKind.Settlement, transform = s.root, settlement = s, name = s.displayName }; }
    private RepairTarget MakeCaravanTarget(TradeCaravanState c) { return new RepairTarget { kind = HorusRepairKind.Caravan, transform = c.root, caravan = c, name = "Neutral Caravan" }; }

    private RepairTarget FindHorusRepairTarget(Transform hit)
    {
        if (hit == null) return null;
        if (battlePyramid != null && IsTransformInHierarchy(hit, battlePyramid)) return MakePyramidTarget();
        for (int i = 0; i < runners.Count; i++) if (runners[i]?.transform != null && IsTransformInHierarchy(hit, runners[i].transform)) return MakeRunnerTarget(runners[i]);
        for (int i = 0; i < goldenStructures.Count; i++) if (goldenStructures[i]?.transform != null && IsTransformInHierarchy(hit, goldenStructures[i].transform)) return MakeStructureTarget(goldenStructures[i]);
        for (int i = 0; i < neutralSettlements.Count; i++) if (neutralSettlements[i]?.root != null && IsTransformInHierarchy(hit, neutralSettlements[i].root)) return MakeSettlementTarget(neutralSettlements[i]);
        for (int i = 0; i < tradeCaravans.Count; i++) if (tradeCaravans[i]?.root != null && IsTransformInHierarchy(hit, tradeCaravans[i].root)) return MakeCaravanTarget(tradeCaravans[i]);
        return null;
    }

    private bool TrySelectTouchOfHorus(Transform hit)
    {
        if (touchOfHorus?.root == null || hit == null || !IsTransformInHierarchy(hit, touchOfHorus.root)) return false;
        SelectStrategicTransform(touchOfHorus.root, "Touch of Horus");
        return true;
    }

    private bool TryIssueTouchOfHorusOrder(RaycastHit hit)
    {
        if (touchOfHorus?.root == null || selectedStrategicTransform != touchOfHorus.root) return false;
        RepairTarget target = FindHorusRepairTarget(hit.transform);
        if (target != null && target.kind == HorusRepairKind.Settlement && TryAcceptHorusDiplomacy(target.settlement))
            return true;
        if (target != null && GetRepairHealth(target) < GetRepairMax(target))
        {
            touchOfHorus.pinnedTarget = target;
            touchOfHorus.hasMoveDestination = false;
            lastEvent = "Touch of Horus bound to priority repair: " + target.name + ".";
        }
        else
        {
            touchOfHorus.pinnedTarget = null;
            touchOfHorus.moveDestination = hit.point;
            touchOfHorus.hasMoveDestination = true;
            PlaceCommandMarker(hit.point);
            lastEvent = "Touch of Horus glides to the commanded point.";
        }
        return true;
    }

    private void DrawTouchOfHorusHUD()
    {
        if (!touchOfHorusRevealed || touchOfHorus?.root == null) return;
        TouchOfHorusState h = touchOfHorus;
        Rect box = GetTouchOfHorusContextRect();
        GUI.Box(box, "TOUCH OF HORUS — ROYAL CONTEXT");
        bool selected = selectedStrategicTransform == h.root;
        string target = IsRepairTargetValid(h.pinnedTarget) ? h.pinnedTarget.name : "automatic";
        string request = activeHorusRepairRequest != null && !activeHorusRepairRequest.completed
            ? activeHorusRepairRequest.target.name + "  " + Mathf.CeilToInt(activeHorusRepairRequest.remainingTime) + "s"
            : "none";

        if (GUI.Button(new Rect(box.x + 12f, box.y + 28f, 175f, 28f), selected ? "SELECTED / RMB TARGET" : "SELECT TOUCH OF HORUS"))
        {
            SelectStrategicTransform(h.root, "Touch of Horus");
            lastEvent = "Touch of Horus selected. RMB a damaged ally to bind priority repair.";
        }
        if (GUI.Button(new Rect(box.x + 202f, box.y + 28f, 175f, 28f), "BIND REQUEST TARGET") &&
            activeHorusRepairRequest != null && !activeHorusRepairRequest.completed)
        {
            h.pinnedTarget = activeHorusRepairRequest.target;
            h.hasMoveDestination = false;
        }
        if (GUI.Button(new Rect(box.x + 12f, box.y + 64f, 175f, 28f), "EYE OF WADJET") && h.eyeCooldown <= 0f)
        {
            h.eyeTimer = 12f; h.eyeCooldown = 32f;
        }
        if (GUI.Button(new Rect(box.x + 202f, box.y + 64f, 175f, 28f), "RECONSECRATION") && h.reconCooldown <= 0f)
        {
            ApplyRepair(IsRepairTargetValid(h.pinnedTarget) ? h.pinnedTarget : MakePyramidTarget(), 110f);
            h.reconCooldown = 55f;
        }
        if (GUI.Button(new Rect(box.x + 12f, box.y + 100f, 175f, 28f), "PRIORITY: PYRAMID"))
        {
            h.pinnedTarget = MakePyramidTarget();
            h.hasMoveDestination = false;
        }
        if (GUI.Button(new Rect(box.x + 202f, box.y + 100f, 175f, 28f), "EIGHT FEATHERS") && h.level >= 8 && h.feathersCooldown <= 0f)
        {
            h.feathersTimer = 14f; h.feathersCooldown = 90f;
        }

        GUI.Label(new Rect(box.x + 12f, box.y + 140f, 368f, 98f),
            "Emitter level: " + h.level + "/8   Active: " + h.activeStreams + "   Blocked: " + h.blockedEmitters +
            "\nSolar dust: " + Mathf.CeilToInt(h.solarDust) + "/" + Mathf.CeilToInt(h.maxSolarDust) +
            "\nPriority: " + target + "\nNeutral request: " + request +
            "\n\nSelect via this panel, then RMB a damaged ally to bind repair. Number keys are not used.");
    }

    public void DebugRevealTouchOfHorus()
    {
        if (touchOfHorusRevealed) return;
        RepairTarget target = neutralSettlements.Count > 0 ? MakeSettlementTarget(neutralSettlements[0]) : MakePyramidTarget();
        if (target.kind == HorusRepairKind.Settlement) target.settlement.health = Mathf.Min(target.settlement.health, target.settlement.maxHealth * 0.55f);
        else pyramidHull = Mathf.Min(pyramidHull, pyramidMaxHull * 0.55f);
        RevealTouchOfHorus(target);
    }

    public string RunTouchOfHorusSmokeTest()
    {
        DebugRevealTouchOfHorus();
        if (touchOfHorus == null || touchOfHorus.root == null) return "FAIL: unit not spawned";
        float before = GetRepairHealth(activeHorusRepairRequest.target);
        touchOfHorus.root.position = activeHorusRepairRequest.target.transform.position + Vector3.up * 10f;
        for (int i = 0; i < 30; i++) UpdateHorusRepair(0.1f);
        float after = GetRepairHealth(activeHorusRepairRequest.target);
        return (after > before ? "PASS" : "FAIL") + ": streams=" + touchOfHorus.activeStreams + ", repair=" + before.ToString("F1") + "->" + after.ToString("F1") + ", emitters=" + touchOfHorus.emitters.Count;
    }
}
