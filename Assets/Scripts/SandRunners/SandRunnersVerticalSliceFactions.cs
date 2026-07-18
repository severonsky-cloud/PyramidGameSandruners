using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum NeutralFactionKind
    {
        BlueElementals,
        BlueTraders = BlueElementals,
        BlackElementals,
        Grounders
    }

    private sealed class NeutralSettlement
    {
        public Transform root;
        public string displayName;
        public NeutralFactionKind kind;
        public float health = 520f;
        public float maxHealth = 520f;
        public readonly List<Transform> guards = new List<Transform>();
        public Material accentMaterial;
    }

    private readonly List<NeutralSettlement> neutralSettlements = new List<NeutralSettlement>();
    private bool verticalSliceFactionsInitialized;
    private float juzzherSpawnTimer = 24f;
    private int juzzherBargeIndex;
    private Material traderBlueMaterial;
    private Material elementalBlackMaterial;
    private Material grounderShellMaterial;
    private Material juzzherRustMaterial;
    private Material juzzherGlowMaterial;
    private Transform verticalSliceFactionsRoot;

    private void InitializeVerticalSliceFactions()
    {
        if (verticalSliceFactionsInitialized)
            return;

        verticalSliceFactionsInitialized = true;
        traderBlueMaterial = CreateMaterial("Neutral Blue Elemental", new Color(0.04f, 0.25f, 0.92f, 1f));
        elementalBlackMaterial = CreateMaterial("Neutral Black Elemental", new Color(0.035f, 0.045f, 0.075f, 1f));
        grounderShellMaterial = CreateMaterial("Grounder Armored Shell", new Color(0.42f, 0.28f, 0.16f, 1f));
        juzzherRustMaterial = CreateMaterial("Juzzher Raider Rust", new Color(0.19f, 0.07f, 0.045f, 1f));
        juzzherGlowMaterial = CreateMaterial("Juzzher Raider Warning Glow", new Color(1f, 0.16f, 0.035f, 1f));
        SetMetallic(traderBlueMaterial, 0.42f, 0.34f);
        SetMetallic(elementalBlackMaterial, 0.64f, 0.2f);
        SetMetallic(grounderShellMaterial, 0.52f, 0.28f);
        SetMetallic(juzzherRustMaterial, 0.6f, 0.22f);

        GameObject old = GameObject.Find("SandRunners_Neutral_Factions_Runtime");
        if (old != null)
            Destroy(old);
        verticalSliceFactionsRoot = new GameObject("SandRunners_Neutral_Factions_Runtime").transform;

        CreateNeutralSettlement("Blue_Elemental_Camp", "BLUE ELEMENTALS", NeutralFactionKind.BlueElementals, new Vector3(155f, 0f, -345f), traderBlueMaterial, 620f);
        CreateNeutralSettlement("Black_Elemental_Exchange", "BLACK ELEMENTALS", NeutralFactionKind.BlackElementals, new Vector3(430f, 0f, 165f), elementalBlackMaterial, 680f);
        CreateGrounderSettlement("Grounder_Settlement_West", "GROUNDER SETTLEMENT WEST", new Vector3(-335f, 0f, 330f));
        CreateGrounderSettlement("Grounder_Settlement_East", "GROUNDER SETTLEMENT EAST", new Vector3(265f, 0f, 495f));

        SpawnJuzzherBarge(new Vector3(70f, 0f, -760f));
        SpawnJuzzherBarge(new Vector3(760f, 0f, 40f));
        lastEvent = "Blue Elementals and Grounder settlements are holding the desert. Juzzher barges are raiding every banner.";
    }

    private void UpdateVerticalSliceFactions(float dt)
    {
        if (!verticalSliceFactionsInitialized)
            return;

        juzzherSpawnTimer -= dt;
        if (juzzherSpawnTimer <= 0f && CountJuzzherBarges() < 5)
        {
            juzzherSpawnTimer = 58f;
            float angle = (juzzherBargeIndex * 71f + 22f) * Mathf.Deg2Rad;
            float radius = mapHalfSize - 55f;
            Vector3 spawn = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            SpawnJuzzherBarge(spawn);
            ShowBanner("JUZZHER RAID BARGE INBOUND", 2.6f);
        }

        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement.root == null)
                continue;

            for (int guardIndex = 0; guardIndex < settlement.guards.Count; guardIndex++)
            {
                Transform guard = settlement.guards[guardIndex];
                if (guard == null)
                    continue;

                guard.Rotate(Vector3.up, (18f + guardIndex * 7f) * dt, Space.Self);
                EnemyUnit barge = FindNearestJuzzherBarge(guard.position, 30f);
                if (barge != null && barge.transform != null && settlement.health > 0f)
                {
                    barge.health -= (15f + guardIndex * 2f) * dt;
                    CreateBeam(guard.position + Vector3.up * 1.2f, barge.transform.position + Vector3.up * 1.1f, settlement.accentMaterial == traderBlueMaterial ? new Color(0.12f, 0.46f, 1f, 1f) : new Color(0.7f, 0.85f, 1f, 1f), 0.035f, 0.08f);
                }
            }

            if (settlement.health <= 0f)
                settlement.root.localScale = Vector3.Lerp(settlement.root.localScale, Vector3.one * 0.9f, dt * 0.6f);
        }
    }

    private int CountJuzzherBarges()
    {
        int count = 0;
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null && enemies[i].isJuzzherBarge && enemies[i].transform != null)
                count++;
        return count;
    }

    private void CreateNeutralSettlement(string objectName, string labelText, NeutralFactionKind kind, Vector3 position, Material accent, float health)
    {
        Transform root = new GameObject(objectName).transform;
        root.SetParent(verticalSliceFactionsRoot, false);
        root.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.2f, position.z);
        NeutralSettlement settlement = new NeutralSettlement { root = root, displayName = labelText, kind = kind, health = health, maxHealth = health, accentMaterial = accent };
        neutralSettlements.Add(settlement);

        CreateCylinder(root, "Neutral_Trading_Plaza", Vector3.zero, Quaternion.identity, new Vector3(11f, 0.18f, 11f), accent, true);
        CreateBox(root, "Neutral_Exchange_Hall", new Vector3(0f, 2.2f, 0f), Quaternion.identity, new Vector3(7.5f, 4.1f, 4.4f), accent, true);
        CreateBox(root, "Neutral_Hall_Roof", new Vector3(0f, 4.55f, 0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(5.6f, 0.28f, 5.6f), accent);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 guardPosition = new Vector3(Mathf.Cos(angle) * 7.2f, 0.35f, Mathf.Sin(angle) * 7.2f);
            settlement.guards.Add(CreateNeutralGuard(root, "Trader_Guard_" + i, guardPosition, accent, false));
        }
        CreatePointLight(root, "Neutral_Signal_Light", new Vector3(0f, 6.2f, 0f), kind == NeutralFactionKind.BlueTraders ? new Color(0.08f, 0.32f, 1f, 1f) : new Color(0.55f, 0.68f, 1f, 1f), 1.25f, 28f);
        CreateNeutralLabel(root, labelText, 7.8f, accent);
    }

    private void CreateGrounderSettlement(string objectName, string labelText, Vector3 position)
    {
        Transform root = new GameObject(objectName).transform;
        root.SetParent(verticalSliceFactionsRoot, false);
        root.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.2f, position.z);
        NeutralSettlement settlement = new NeutralSettlement { root = root, displayName = labelText, kind = NeutralFactionKind.Grounders, health = 760f, maxHealth = 760f, accentMaterial = grounderShellMaterial };
        neutralSettlements.Add(settlement);

        CreateCylinder(root, "Grounder_Shell_Circle", Vector3.zero, Quaternion.identity, new Vector3(15f, 0.2f, 15f), duneRidgeMaterial, true);
        CreateBox(root, "Grounder_Stone_Hearth", new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(5.4f, 2.2f, 5.4f), grounderShellMaterial, true);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? 7.6f : 5.5f;
            Vector3 guardPosition = new Vector3(Mathf.Cos(angle) * radius, 0.3f, Mathf.Sin(angle) * radius);
            settlement.guards.Add(CreateNeutralGuard(root, "Grounder_Armored_Warden_" + i, guardPosition, grounderShellMaterial, true));
        }
        CreatePointLight(root, "Grounder_Fire_Light", new Vector3(0f, 3.2f, 0f), new Color(1f, 0.42f, 0.12f, 1f), 1.1f, 24f);
        CreateNeutralLabel(root, labelText, 5.3f, grounderShellMaterial);
    }

    private Transform CreateNeutralGuard(Transform parent, string name, Vector3 localPosition, Material material, bool grounder)
    {
        Transform guard = new GameObject(name).transform;
        guard.SetParent(parent, false);
        guard.localPosition = localPosition;
        CreateBox(guard, "Guard_Body", new Vector3(0f, grounder ? 0.85f : 0.7f, 0f), Quaternion.identity, grounder ? new Vector3(1.5f, 1.7f, 1.1f) : new Vector3(0.9f, 1.2f, 0.9f), material, true);
        CreateBox(guard, "Guard_Head", new Vector3(0f, grounder ? 2f : 1.55f, 0f), Quaternion.identity, grounder ? new Vector3(1.15f, 0.8f, 1f) : new Vector3(0.62f, 0.62f, 0.62f), material, true);
        if (grounder)
        {
            CreateBox(guard, "Guard_Shell_Ridge", new Vector3(0f, 1.1f, -0.72f), Quaternion.Euler(22f, 0f, 0f), new Vector3(1.7f, 0.55f, 0.25f), material);
            CreateBox(guard, "Guard_Spear", new Vector3(0.8f, 1.4f, 0.3f), Quaternion.Euler(-15f, 0f, 8f), new Vector3(0.12f, 1.8f, 0.12f), material);
        }
        else
        {
            CreateBox(guard, "Purchased_War_Machine_Chassis", new Vector3(0f, 0.32f, 0f), Quaternion.identity, new Vector3(1.55f, 0.42f, 2.2f), material, true);
            for (int side = -1; side <= 1; side += 2)
                CreateCylinder(guard, "War_Machine_Wheel_" + side, new Vector3(side * 0.82f, 0.28f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.36f, 0.2f, 0.36f), elementalBlackMaterial, true);
            CreateBox(guard, "War_Machine_Turret", new Vector3(0f, 1.45f, 0.25f), Quaternion.identity, new Vector3(0.72f, 0.58f, 0.82f), material, true);
            CreateBox(guard, "War_Machine_Cannon", new Vector3(0f, 1.52f, 1.15f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(0.18f, 0.18f, 1.05f), material);
        }
        return guard;
    }

    private void CreateNeutralLabel(Transform root, string labelText, float height, Material material)
    {
        TextMesh label = new GameObject("Faction_Label").AddComponent<TextMesh>();
        label.transform.SetParent(root, false);
        label.transform.localPosition = new Vector3(0f, height, 0f);
        label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        label.text = labelText;
        label.characterSize = 0.52f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = material == grounderShellMaterial ? new Color(1f, 0.72f, 0.35f, 1f) : new Color(0.58f, 0.82f, 1f, 1f);
    }

    private void SpawnJuzzherBarge(Vector3 position)
    {
        GameObject bargeObject = new GameObject("Juzzher_Barge_Raid_" + juzzherBargeIndex);
        bargeObject.transform.SetParent(verticalSliceFactionsRoot, false);
        bargeObject.transform.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 7f, position.z);
        bargeObject.transform.rotation = Quaternion.Euler(0f, juzzherBargeIndex * 47f, 0f);
        BuildJuzzherBargeVisual(bargeObject.transform);

        EnemyUnit barge = new EnemyUnit();
        barge.transform = bargeObject.transform;
        barge.health = 420f;
        barge.maxHealth = 420f;
        barge.isJuzzherBarge = true;
        barge.factionTag = "JUZZHER";
        barge.objectiveTimer = 0f;
        enemies.Add(barge);
        juzzherBargeIndex++;
    }

    private void BuildJuzzherBargeVisual(Transform root)
    {
        CreateBox(root, "Juzzher_Raid_Hull", new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(6.4f, 1.2f, 3.2f), juzzherRustMaterial, true);
        CreateBox(root, "Juzzher_Command_Cabin", new Vector3(0f, 1.35f, -0.25f), Quaternion.identity, new Vector3(2.3f, 1.35f, 1.8f), juzzherRustMaterial, true);
        CreateBox(root, "Juzzher_Bow_Ram", new Vector3(0f, 0.2f, 2.6f), Quaternion.Euler(-12f, 0f, 0f), new Vector3(2.2f, 0.72f, 2.2f), juzzherGlowMaterial);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateCylinder(root, "Juzzher_Lift_Fan_" + side, new Vector3(side * 2.5f, 1.35f, -1.1f), Quaternion.identity, new Vector3(1.2f, 0.25f, 1.2f), juzzherGlowMaterial);
            CreateBox(root, "Juzzher_Side_Rotor_" + side, new Vector3(side * 3.65f, 0.55f, 0f), Quaternion.Euler(0f, 0f, side * 18f), new Vector3(0.2f, 1.2f, 0.2f), juzzherGlowMaterial);
            CreateBox(root, "Juzzher_Weapon_" + side, new Vector3(side * 1.7f, 1.7f, 1.1f), Quaternion.Euler(-18f, 0f, side * 12f), new Vector3(0.28f, 0.28f, 1.5f), juzzherGlowMaterial);
        }
        CreatePointLight(root, "Juzzher_Red_Navigation", new Vector3(0f, 1.4f, 2.4f), new Color(1f, 0.05f, 0.02f, 1f), 1.6f, 22f);
    }

    private EnemyUnit FindNearestJuzzherBarge(Vector3 origin, float range)
    {
        EnemyUnit best = null;
        float bestDistance = range;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || !enemy.isJuzzherBarge || enemy.transform == null || enemy.health <= 0f)
                continue;
            float distance = FlatDistance(origin, enemy.transform.position);
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void UpdateJuzzherBargeEnemy(EnemyUnit barge, float dt)
    {
        if (barge.transform == null)
            return;

        barge.objectiveTimer -= dt;
        if (barge.factionObjective == null || barge.objectiveTimer <= 0f)
        {
            barge.objectiveTimer = 4f;
            barge.factionObjective = FindJuzzherObjective(barge.transform.position);
        }

        if (barge.factionObjective == null)
            return;

        Vector3 toObjective = barge.factionObjective.position - barge.transform.position;
        float distance = toObjective.magnitude;
        if (distance > 14f)
        {
            barge.transform.position += toObjective.normalized * 7.2f * dt;
            RotateToward(barge.transform, toObjective, 95f * dt);
        }
        else
        {
            if (barge.fireCooldown <= 0f)
            {
                barge.fireCooldown = 0.9f;
                DamageJuzzherObjective(barge.factionObjective, 22f);
                CreateBeam(barge.transform.position + Vector3.up * 1.1f, barge.factionObjective.position + Vector3.up * 1.3f, new Color(1f, 0.12f, 0.04f, 1f), 0.07f, 0.14f);
            }
            barge.transform.Rotate(Vector3.up, 35f * dt, Space.Self);
        }

        Vector3 position = barge.transform.position;
        position.y = GetPlayableGroundHeight(position) + 7f + Mathf.Sin(Time.time * 2.2f + barge.transform.GetHashCode()) * 0.45f;
        barge.transform.position = position;
    }

    private Transform FindJuzzherObjective(Vector3 origin)
    {
        Transform best = battlePyramid;
        float bestDistance = battlePyramid != null ? FlatDistance(origin, battlePyramid.position) : float.MaxValue;
        if (mandarinkaFortressRoot != null && FlatDistance(origin, mandarinkaFortressRoot.position) < bestDistance)
        {
            best = mandarinkaFortressRoot;
            bestDistance = FlatDistance(origin, mandarinkaFortressRoot.position);
        }
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            NeutralSettlement settlement = neutralSettlements[i];
            if (settlement.root == null || settlement.health <= 0f)
                continue;
            float distance = FlatDistance(origin, settlement.root.position);
            if (distance < bestDistance)
            {
                best = settlement.root;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void DamageJuzzherObjective(Transform objective, float damage)
    {
        if (objective == battlePyramid)
        {
            pyramidHull -= damage;
            return;
        }
        if (objective == mandarinkaFortressRoot && mandarinkaFortressEnemy != null)
        {
            mandarinkaFortressEnemy.health -= damage;
            return;
        }
        for (int i = 0; i < neutralSettlements.Count; i++)
        {
            if (neutralSettlements[i].root == objective)
            {
                neutralSettlements[i].health = Mathf.Max(0f, neutralSettlements[i].health - damage);
                return;
            }
        }
    }
}