using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private sealed class GoldenBuilderAsset
    {
        public Transform transform;
        public string displayName = "Solar Builder Truck";
        public float health;
        public float maxHealth;
        public float fireTimer;
        public ResourceNode targetNode;
        public float gatherTimer;
        public bool hasOrder;
        public Vector3 orderPosition;
        public UnitSquad squad;
        public ResourceKind cargoKind;
        public float cargoAmount;
        public bool carryingCargo;
        public BlueprintSite buildTarget;
    }

    private sealed class GoldenTurretAsset
    {
        public Transform transform;
        public string displayName = "Golden Sun Turret";
        public float health;
        public float maxHealth;
        public float fireTimer;
    }

    private readonly List<GoldenBuilderAsset> goldenBuilders = new List<GoldenBuilderAsset>();
    private readonly List<GoldenTurretAsset> goldenTurrets = new List<GoldenTurretAsset>();
    private Material goldenBuilderMaterial;
    private Material goldenTurretMaterial;
    private Material goldenTurretGlowMaterial;

    private void HandleGoldenEngineeringInput()
    {
        if (WasKeyPressedThisFrame(Key.Digit4) || WasKeyPressedThisFrame(Key.Numpad4))
            BuildGoldenResourceBuilder();


        if (WasKeyPressedThisFrame(Key.Digit5) || WasKeyPressedThisFrame(Key.Numpad5))
            DeployGoldenSunTurret();
    }

    private void UpdateGoldenEngineering(float dt)
    {
        UpdateGoldenBuilders(dt);
        UpdateGoldenTurrets(dt);
    }

    private void EnsureGoldenEngineeringMaterials()
    {
        if (goldenBuilderMaterial != null)
            return;

        goldenBuilderMaterial = CreateMaterial("Golden Engineering Builder", new Color(0.9f, 0.58f, 0.18f, 1f));
        goldenTurretMaterial = CreateMaterial("Golden Sun Turret Armor", new Color(0.78f, 0.47f, 0.14f, 1f));
        goldenTurretGlowMaterial = CreateMaterial("Golden Sun Turret Glow", new Color(1f, 0.9f, 0.28f, 1f));
        SetMetallic(goldenBuilderMaterial, 0.55f, 0.42f);
        SetMetallic(goldenTurretMaterial, 0.68f, 0.48f);
        SetEmission(goldenTurretGlowMaterial, new Color(1f, 0.8f, 0.18f, 1f), 1.55f);
    }

    private void BuildGoldenResourceBuilder()
    {
        QueueProduction(ProductionKind.BuilderTruck);
    }

    private GoldenBuilderAsset CreateGoldenResourceBuilderImmediate(Vector3? spawnOverride = null)
    {
        EnsureGoldenEngineeringMaterials();

        Vector3 position = spawnOverride ?? GetExternalFactoryPosition();
        Transform root = new GameObject("Golden_Resource_Builder_Scarab").transform;
        root.position = position;
        root.rotation = battlePyramid.rotation;

        CreateBox(root, "Builder_Gold_Hull", new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.55f, 0.52f, 2.15f), goldenBuilderMaterial);
        CreateBox(root, "Builder_Amber_Cab", new Vector3(0f, 0.92f, -0.28f), Quaternion.identity, new Vector3(0.9f, 0.62f, 0.82f), goldenTurretGlowMaterial);
        CreateBox(root, "Builder_Collector_Arm", new Vector3(0.72f, 0.86f, 0.75f), Quaternion.Euler(0f, 0f, -22f), new Vector3(0.22f, 0.16f, 2.1f), goldenTurretMaterial);
        CreateCylinder(root, "Builder_Left_Wheel", new Vector3(-0.98f, 0.24f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.42f, 0.16f, 0.42f), pyramidDarkArmorMaterial);
        CreateCylinder(root, "Builder_Right_Wheel", new Vector3(0.98f, 0.24f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.42f, 0.16f, 0.42f), pyramidDarkArmorMaterial);
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.72f, 0f);
        collider.size = new Vector3(2.4f, 1.6f, 2.8f);

        GoldenBuilderAsset builder = new GoldenBuilderAsset();
        builder.transform = root;
        builder.displayName = "Solar Builder Truck";
        builder.health = 140f;
        builder.maxHealth = 140f;
        builder.targetNode = FindNearestResourceNode(root.position);
        goldenBuilders.Add(builder);
        RegisterRTSBuilder(builder);
        lastEvent = "Golden builder scarab deployed. It will harvest and support turret lines.";
        return builder;
    }

    private void DeployGoldenSunTurret()
    {
        BeginStructureBlueprint(StructureKind.MirrorBeamTurret);
    }

    private void UpdateGoldenBuilders(float dt)
    {
        for (int i = goldenBuilders.Count - 1; i >= 0; i--)
        {
            GoldenBuilderAsset builder = goldenBuilders[i];
            if (builder.transform == null || builder.health <= 0f)
            {
                if (builder.transform != null)
                    Destroy(builder.transform.gameObject);
                goldenBuilders.RemoveAt(i);
                continue;
            }

            if (UpdateGoldenBuilderRepair(builder, dt))
                continue;

            if (builder.squad != null)
            {
                UpdateRTSBuilder(builder, dt);
                continue;
            }

            if (builder.targetNode == null || builder.targetNode.transform == null)
                builder.targetNode = FindNearestResourceNode(builder.transform.position);

            EnemyUnit threat = FindNearestEnemy(builder.transform.position, 38f);
            builder.fireTimer -= dt;
            if (threat != null && threat.transform != null && builder.fireTimer <= 0f)
            {
                builder.fireTimer = 1.4f;
                threat.health -= 14f;
                CreateBeam(builder.transform.position + Vector3.up * 1.05f, threat.transform.position + Vector3.up * 0.8f, new Color(1f, 0.82f, 0.22f, 1f), 0.04f, 0.12f);
                CleanupDeadEnemies();
            }

            if (builder.hasOrder)
            {
                Vector3 toOrder = builder.orderPosition - builder.transform.position;
                toOrder.y = 0f;
                if (toOrder.magnitude <= 2.2f)
                {
                    builder.hasOrder = false;
                    builder.targetNode = FindNearestResourceNode(builder.transform.position);
                }
                else
                {
                    builder.transform.position += toOrder.normalized * 6.6f * dt;
                    RotateFlatToward(builder.transform, toOrder, 220f * dt);
                    Vector3 orderedPosition = builder.transform.position;
                    orderedPosition.y = GetPlayableGroundHeight(orderedPosition) + 0.34f;
                    builder.transform.position = orderedPosition;
                    continue;
                }
            }

            if (builder.targetNode == null)
                continue;

            Vector3 destination = builder.targetNode.transform.position;
            Vector3 toTarget = destination - builder.transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > 5.2f)
            {
                builder.transform.position += toTarget.normalized * 6.6f * dt;
                RotateFlatToward(builder.transform, toTarget, 220f * dt);
            }
            else
            {
                builder.gatherTimer += dt;
                float bonus = builder.targetNode.controlled ? 1.4f : 0.8f;
                if (builder.targetNode.kind == ResourceKind.Gold)
                    gold += 3.6f * bonus * dt;
                else if (builder.targetNode.kind == ResourceKind.Wind)
                    wind += 1.6f * bonus * dt;
                else
                    sand += 4.8f * bonus * dt;

                if (builder.gatherTimer > 18f)
                {
                    builder.gatherTimer = 0f;
                    builder.targetNode = FindNearestResourceNode(builder.transform.position);
                }
            }

            Vector3 position = builder.transform.position;
            position.y = GetPlayableGroundHeight(position) + 0.34f;
            builder.transform.position = position;
        }
    }

    private void UpdateGoldenTurrets(float dt)
    {
        for (int i = goldenTurrets.Count - 1; i >= 0; i--)
        {
            GoldenTurretAsset turret = goldenTurrets[i];
            if (turret.transform == null || turret.health <= 0f)
            {
                if (turret.transform != null)
                    Destroy(turret.transform.gameObject);
                goldenTurrets.RemoveAt(i);
                continue;
            }

            turret.fireTimer -= dt;
            EnemyUnit target = FindNearestEnemy(turret.transform.position, 118f);
            if (target == null || target.transform == null)
                continue;

            Vector3 toTarget = target.transform.position - turret.transform.position;
            RotateFlatToward(turret.transform, toTarget, 260f * dt);
            if (turret.fireTimer <= 0f)
            {
                turret.fireTimer = 0.72f;
                target.health -= 32f;
                CreateBeam(turret.transform.position + Vector3.up * 1.25f, target.transform.position + Vector3.up * 1.4f, new Color(1f, 0.82f, 0.18f, 1f), 0.07f, 0.13f);
                CreateWeaponFlash(turret.transform.position + Vector3.up * 1.2f - turret.transform.forward * 1.75f, 0.2f, new Color(1f, 0.78f, 0.18f, 1f));
                CleanupDeadEnemies();
            }
        }
    }

    private ResourceNode FindNearestResourceNode(Vector3 origin)
    {
        ResourceNode best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < resourceNodes.Count; i++)
        {
            ResourceNode node = resourceNodes[i];
            if (node == null || node.transform == null)
                continue;

            float distance = FlatDistance(origin, node.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }
        return best;
    }
}