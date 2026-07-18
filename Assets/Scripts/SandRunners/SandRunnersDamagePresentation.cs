using System.Collections.Generic;
using UnityEngine;

internal static class SandRunnersSalvageRules
{
    internal static SandRunnersResourcePrice Merge(SandRunnersResourcePrice a, SandRunnersResourcePrice b)
    {
        return new SandRunnersResourcePrice(a.sand + b.sand, a.gold + b.gold, a.wind + b.wind);
    }

    internal static bool MustMerge(int activeFields, int maxFields, int activePieces, int maxPieces)
    {
        return activeFields >= Mathf.Max(1, maxFields) || activePieces >= Mathf.Max(1, maxPieces);
    }

    internal static int AllowedPieces(int requested, int activePieces, int maxPieces)
    {
        return Mathf.Clamp(requested, 0, Mathf.Max(0, maxPieces - activePieces));
    }

    internal static bool CanClaim(bool collected, bool reserved, bool reservedByRequester)
    {
        return !collected && (!reserved || reservedByRequester);
    }

    internal static bool TryConsume(ref bool delivered, SandRunnersResourcePrice payload, out SandRunnersResourcePrice reward)
    {
        reward = new SandRunnersResourcePrice(0f, 0f, 0f);
        if (delivered)
            return false;
        delivered = true;
        reward = payload;
        return true;
    }
}

public partial class SandRunnersPrototype
{
    private sealed class SalvageField
    {
        public Transform root;
        public readonly List<Transform> pieces = new List<Transform>(5);
        public Light beaconLight;
        public SandRunnersResourcePrice payload;
        public string label;
        public bool collected;
        public bool delivered;
        public Transform carrier;
        public RunnerUnit reservedBy;
        public float bornTime;
    }

    private readonly List<SalvageField> salvageFields = new List<SalvageField>(24);
    private readonly Queue<GameObject> salvagePiecePool = new Queue<GameObject>(72);
    private readonly HashSet<Transform> collapsedDamageRoots = new HashSet<Transform>();
    private readonly Dictionary<Transform, List<Renderer>> damageRenderers = new Dictionary<Transform, List<Renderer>>();
    private readonly Dictionary<Renderer, Color> damageBaseColors = new Dictionary<Renderer, Color>();
    private MaterialPropertyBlock damagePropertyBlock;
    private Material salvageDebrisMaterial;
    private Material salvageGlyphMaterial;
    private float damagePresentationPulse;
    private float damageExplosionCooldown;
    private float salvagePresentationTimer;
    private bool pyramidCollapseSpawned;
    private bool fortressCollapseSpawned;

    private int MaxSalvageFields => presentationBudget != null ? Mathf.Max(1, presentationBudget.maxSalvageFields) : 24;
    private int MaxSalvagePieces => presentationBudget != null ? Mathf.Max(1, presentationBudget.maxSalvagePieces) : 72;
    private int MaxSalvageMarkers => presentationBudget != null ? Mathf.Max(1, presentationBudget.maxSalvageMarkers) : 6;
    private int MaxSalvageLights => presentationBudget != null ? Mathf.Max(0, presentationBudget.maxSalvageLights) : 4;
    private float SalvageUpdateInterval => presentationBudget != null ? Mathf.Max(0.04f, presentationBudget.salvageUpdateInterval) : 0.1f;

    private void UpdateDamagePresentation(float dt)
    {
        damagePresentationPulse += dt;
        damageExplosionCooldown = Mathf.Max(0f, damageExplosionCooldown - dt);

        if (battlePyramid != null)
        {
            float hull01 = Mathf.Clamp01(pyramidMaxHull > 0f ? pyramidHull / pyramidMaxHull : 1f);
            ApplyDamageVisualState(battlePyramid, hull01, new Color(1f, 0.28f, 0.06f, 1f));
            if (hull01 <= 0.01f && !pyramidCollapseSpawned)
            {
                pyramidCollapseSpawned = true;
                CollapseDamageRoot(battlePyramid, "Pyramid salvage field", new SandRunnersResourcePrice(18f, 0f, 0f));
            }
        }

        if (mandarinkaFortressRoot != null && mandarinkaFortressEnemy != null)
        {
            float hull01 = Mathf.Clamp01(mandarinkaFortressEnemy.health / Mathf.Max(1f, MandarinkaFortressMaxHull));
            Transform visual = mandarinkaFortressVisual != null ? mandarinkaFortressVisual : mandarinkaFortressRoot;
            ApplyDamageVisualState(visual, hull01, new Color(1f, 0.05f, 0.02f, 1f));
            if (hull01 <= 0.01f && !fortressCollapseSpawned)
            {
                fortressCollapseSpawned = true;
                SandRunnersResourcePrice reward = balanceProfile != null
                    ? balanceProfile.fortressSalvage
                    : new SandRunnersResourcePrice(18f, 18f, 4f);
                CollapseDamageRoot(visual, "Fortress salvage field", reward);
            }
        }

        UpdateSalvageFields(dt);
        UpdateSalvageBenchmark(dt);
    }

    private void ApplyDamageVisualState(Transform root, float health01, Color warningColor)
    {
        if (root == null)
            return;
        if (damagePropertyBlock == null)
            damagePropertyBlock = new MaterialPropertyBlock();

        if (!damageRenderers.TryGetValue(root, out List<Renderer> renderers))
        {
            renderers = new List<Renderer>(root.GetComponentsInChildren<Renderer>(true));
            damageRenderers[root] = renderers;
        }

        float damage01 = 1f - health01;
        float pulse = Mathf.Lerp(0f, 0.24f, damage01) * (0.5f + 0.5f * Mathf.Sin(damagePresentationPulse * 7f));
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            if (health01 > 0.01f)
                renderer.enabled = true;

            Material material = renderer.sharedMaterial;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
            if (!damageBaseColors.TryGetValue(renderer, out Color baseColor))
            {
                baseColor = colorProperty != null ? material.GetColor(colorProperty) : Color.white;
                damageBaseColors[renderer] = baseColor;
            }

            renderer.GetPropertyBlock(damagePropertyBlock);
            if (material.HasProperty("_EmissionColor"))
                damagePropertyBlock.SetColor("_EmissionColor", damage01 > 0.45f ? warningColor * pulse : Color.black);
            if (colorProperty != null)
            {
                float scorch = Mathf.Clamp01((damage01 - 0.58f) * 0.72f);
                damagePropertyBlock.SetColor(colorProperty, Color.Lerp(baseColor, warningColor * 0.42f, scorch));
            }
            renderer.SetPropertyBlock(damagePropertyBlock);
            damagePropertyBlock.Clear();
        }

        ApplyDamageSectionBreakup(root, health01, renderers);
        if (health01 <= 0.35f && damageExplosionCooldown <= 0f && Mathf.Sin(damagePresentationPulse * 5f) > 0.96f)
        {
            damageExplosionCooldown = 0.45f;
            CreateBattleExplosionFx(root.position + Vector3.up * 2.5f, 1.4f, false, false);
        }
    }

    private void ApplyDamageSectionBreakup(Transform root, float health01, List<Renderer> renderers)
    {
        if (root == null || renderers == null)
            return;
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            string name = renderer.gameObject.name.ToLowerInvariant();
            bool section = name.Contains("cannon") || name.Contains("turret") || name.Contains("tower") ||
                name.Contains("weapon") || name.Contains("hangar") || name.Contains("factory") ||
                name.Contains("balcony") || name.Contains("track") || name.Contains("wheel") || name.Contains("mast");
            if (!section)
                continue;
            bool broken = (health01 <= 0.62f && i % 4 == 0) || (health01 <= 0.38f && i % 2 == 0);
            renderer.enabled = !broken;
        }
    }

    private void CollapseDamageRoot(Transform root, string label, SandRunnersResourcePrice payload)
    {
        if (root == null || collapsedDamageRoots.Contains(root))
            return;

        collapsedDamageRoots.Add(root);
        CreateSalvageField(root.position, label, payload, 5);
        root.gameObject.SetActive(false);
        lastEvent = label + " created. Salvage can be recovered by a salvage scarab.";
        ShowBanner(lastEvent, 3f);
        PlaySandRunnerSound(SandRunnerSound.LargeExplosion, root.position, 0.9f);
    }

    private Material GetDamageDebrisMaterial()
    {
        if (salvageDebrisMaterial != null)
            return salvageDebrisMaterial;
        salvageDebrisMaterial = CreateMaterial("Salvage Dark Matte Metal", new Color(0.12f, 0.105f, 0.09f, 1f));
        SetMetallic(salvageDebrisMaterial, 0.48f, 0.24f);
        AssignTextureToMaterial(salvageDebrisMaterial, Resources.Load<Texture2D>("SandRunners/Textures/SR_DarkMetal"), new Vector2(1.8f, 1.8f));
        return salvageDebrisMaterial;
    }

    private Material GetSalvageGlyphMaterial()
    {
        if (salvageGlyphMaterial != null)
            return salvageGlyphMaterial;
        salvageGlyphMaterial = CreateMaterial("Salvage Amber Glyphs", new Color(1f, 0.58f, 0.08f, 1f));
        ConfigureTransparent(salvageGlyphMaterial);
        SetEmission(salvageGlyphMaterial, new Color(1f, 0.34f, 0.025f, 1f), 1.35f);
        AssignTextureToMaterial(salvageGlyphMaterial, Resources.Load<Texture2D>("SandRunners/Textures/SR_SalvageGlyphs"), Vector2.one);
        return salvageGlyphMaterial;
    }

    private int GetActiveSalvagePieceCount()
    {
        int count = 0;
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            if (field != null && !field.delivered)
                count += field.pieces.Count;
        }
        return count;
    }

    private SalvageField CreateSalvageField(Vector3 origin, string label, SandRunnersResourcePrice payload, int requestedPieces)
    {
        int activePieces = GetActiveSalvagePieceCount();
        if (SandRunnersSalvageRules.MustMerge(salvageFields.Count, MaxSalvageFields, activePieces, MaxSalvagePieces))
        {
            SalvageField mergeTarget = FindNearestMergeField(origin);
            if (mergeTarget != null)
            {
                mergeTarget.payload = SandRunnersSalvageRules.Merge(mergeTarget.payload, payload);
                return mergeTarget;
            }
        }

        int pieceCount = SandRunnersSalvageRules.AllowedPieces(Mathf.Clamp(requestedPieces, 3, 5), activePieces, MaxSalvagePieces);
        if (pieceCount <= 0)
        {
            SalvageField mergeTarget = FindNearestMergeField(origin);
            if (mergeTarget != null)
            {
                mergeTarget.payload = SandRunnersSalvageRules.Merge(mergeTarget.payload, payload);
                return mergeTarget;
            }
            return null;
        }

        GameObject rootObject = new GameObject(label.Replace(' ', '_') + "_SalvageField");
        rootObject.transform.position = origin;
        SalvageField field = new SalvageField
        {
            root = rootObject.transform,
            payload = payload,
            label = label,
            bornTime = Time.time
        };

        for (int i = 0; i < pieceCount; i++)
        {
            float angle = Mathf.PI * 2f * i / Mathf.Max(1, pieceCount) + Random.Range(-0.25f, 0.25f);
            float radius = 1.25f + i * 0.52f;
            Vector3 position = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            position.y = GetPlayableGroundHeight(position) + 0.35f;
            GameObject piece = GetPooledSalvagePiece(rootObject.transform, label, i, position);
            field.pieces.Add(piece.transform);
        }

        Transform anchor = rootObject.transform;
        CreateCylinder(anchor, "Salvage_Wreckage_Beacon", new Vector3(0f, 1.55f, 0f), Quaternion.identity,
            new Vector3(0.25f, 1.15f, 0.25f), GetSalvageGlyphMaterial());
        CreatePointLight(anchor, "Salvage_Wreckage_Light", new Vector3(0f, 1.3f, 0f),
            new Color(1f, 0.58f, 0.08f, 1f), 1.15f, 16f);
        Transform lightTransform = anchor.Find("Salvage_Wreckage_Light");
        field.beaconLight = lightTransform != null ? lightTransform.GetComponent<Light>() : null;
        if (field.beaconLight != null)
            field.beaconLight.enabled = false;

        salvageFields.Add(field);
        PlaySandRunnerSound(SandRunnerSound.HeavyImpact, origin, 0.28f);
        return field;
    }

    private GameObject GetPooledSalvagePiece(Transform parent, string label, int index, Vector3 position)
    {
        GameObject piece = salvagePiecePool.Count > 0 ? salvagePiecePool.Dequeue() : null;
        if (piece == null)
        {
            piece = CreateBox(null, "Salvage_Pooled_Piece", position, Quaternion.identity, Vector3.one, GetDamageDebrisMaterial());
            CreateBox(piece.transform, "Wreckage_Amber_Core", new Vector3(0f, 0.58f, 0f), Quaternion.identity,
                new Vector3(0.3f, 0.2f, 0.3f), pyramidGoldMaterial != null ? pyramidGoldMaterial : GetDamageDebrisMaterial());
        }
        piece.name = label.Replace(' ', '_') + "_Fragment_" + index;
        piece.transform.SetParent(parent, true);
        piece.transform.position = position;
        piece.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
        piece.transform.localScale = new Vector3(1.15f + (index % 2) * 0.42f, 0.62f + (index % 3) * 0.18f, 1.35f + (index % 2) * 0.32f);
        piece.SetActive(true);
        return piece;
    }

    private SalvageField FindNearestMergeField(Vector3 origin)
    {
        SalvageField best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            if (field == null || field.root == null || field.delivered)
                continue;
            float distance = FlatDistance(origin, field.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = field;
            }
        }
        return best;
    }

    private void RecycleSalvageField(SalvageField field)
    {
        if (field == null)
            return;
        for (int i = 0; i < field.pieces.Count; i++)
        {
            Transform piece = field.pieces[i];
            if (piece == null)
                continue;
            piece.SetParent(null, true);
            piece.gameObject.SetActive(false);
            salvagePiecePool.Enqueue(piece.gameObject);
        }
        field.pieces.Clear();
        if (field.root != null)
            Destroy(field.root.gameObject);
        salvageFields.Remove(field);
    }

    private void UpdateSalvageFields(float dt)
    {
        for (int i = salvageFields.Count - 1; i >= 0; i--)
        {
            SalvageField field = salvageFields[i];
            if (field == null || field.root == null)
            {
                salvageFields.RemoveAt(i);
                continue;
            }

            if (field.collected)
            {
                if (field.carrier == null)
                {
                    field.collected = false;
                    field.reservedBy = null;
                    continue;
                }
                field.root.position = field.carrier.position + Vector3.up * (balanceProfile != null ? balanceProfile.salvageCarryHeight : 1.65f);
            }
        }

        salvagePresentationTimer -= dt;
        if (salvagePresentationTimer > 0f)
            return;
        salvagePresentationTimer = SalvageUpdateInterval;

        Vector3 viewer = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        float detailDistance = presentationBudget != null ? presentationBudget.detailCullDistance : 210f;
        List<SalvageField> lightCandidates = new List<SalvageField>(salvageFields);
        lightCandidates.Sort((a, b) => DistanceToField(viewer, a).CompareTo(DistanceToField(viewer, b)));
        for (int i = 0; i < lightCandidates.Count; i++)
        {
            SalvageField field = lightCandidates[i];
            if (field == null || field.root == null)
                continue;
            float distance = FlatDistance(viewer, field.root.position);
            bool showDetails = field.collected || distance <= detailDistance;
            for (int p = 0; p < field.pieces.Count; p++)
            {
                Transform piece = field.pieces[p];
                if (piece != null)
                    piece.gameObject.SetActive(showDetails);
            }
            if (field.beaconLight != null)
                field.beaconLight.enabled = !field.collected && i < MaxSalvageLights && distance <= detailDistance;
            Transform beacon = field.root.Find("Salvage_Wreckage_Beacon");
            if (beacon != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4.5f + i * 0.7f) * 0.13f;
                beacon.localScale = new Vector3(0.25f, 1.15f, 0.25f) * pulse;
            }
        }
    }

    private static float DistanceToField(Vector3 origin, SalvageField field)
    {
        if (field == null || field.root == null)
            return float.MaxValue;
        Vector3 delta = field.root.position - origin;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    private void CreateEnemySalvageWreckage(EnemyUnit enemy)
    {
        if (enemy == null || enemy.transform == null)
            return;
        bool barge = enemy.isJuzzherBarge;
        string label = barge ? "Juzzher Barge Wreckage" : "Hostile Vehicle Wreckage";
        SandRunnersResourcePrice reward = balanceProfile != null
            ? (barge ? balanceProfile.juzzherBargeSalvage : balanceProfile.hostileVehicleSalvage)
            : (barge ? new SandRunnersResourcePrice(16f, 10f, 2f) : new SandRunnersResourcePrice(8f, 5f, 0f));
        CreateSalvageField(enemy.transform.position, label, reward, barge ? 5 : 3);
        lastEvent = label + " created. Salvage Scarab can recover it.";
        ShowBanner("SALVAGE FIELD // " + label.ToUpperInvariant(), 2.5f);
    }

    private void CreateStructureSalvageWreckage(GoldenStructure structure)
    {
        if (structure == null || structure.transform == null)
            return;
        string label = string.IsNullOrEmpty(structure.displayName) ? "Destroyed Outpost" : structure.displayName + " Wreckage";
        SandRunnersResourcePrice reward = balanceProfile != null
            ? balanceProfile.structureSalvage
            : new SandRunnersResourcePrice(14f, 8f, 0f);
        CreateSalvageField(structure.transform.position, label, reward, 5);
        lastEvent = label + " created. Salvage Scarab can recover it.";
        ShowBanner("OUTPOST WRECKAGE // SALVAGE AVAILABLE", 2.5f);
    }

    private bool TryCollectSalvageAtHit(RaycastHit hit)
    {
        return TryIssueSalvageOrderAtHit(hit);
    }

    private bool salvageBenchmarkActive;
    private float salvageBenchmarkTimer;

    private void BuildSalvageScarab()
    {
        QueueProduction(ProductionKind.SalvageScarab);
    }

    private void StartSalvageBenchmark()
    {
        if (salvageBenchmarkActive || cinematicDirectorActive || battlePyramid == null)
            return;

        salvageBenchmarkActive = true;
        salvageBenchmarkTimer = 0f;
        UnitSquad scarabSquad = null;
        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            if (squad != null && !string.IsNullOrEmpty(squad.displayName) && squad.displayName.Contains("Salvage"))
            {
                scarabSquad = squad;
                break;
            }
        }
        if (scarabSquad == null)
        {
            scarabSquad = CreateSquad("Salvage Scarab", false);
            rtsAssemblingSquad = scarabSquad;
            SpawnProductionUnit(ProductionKind.SalvageScarab, 0, 1, false);
            rtsAssemblingSquad = null;
        }

        SelectOnlySquad(scarabSquad);
        for (int i = 0; i < scarabSquad.units.Count; i++)
        {
            RunnerUnit runner = scarabSquad.units[i];
            if (runner == null || !runner.isSalvageScarab)
                continue;
            ClearSalvageAssignment(runner);
            runner.salvageAutoMode = true;
        }

        for (int i = 0; i < 6; i++)
        {
            float angle = 0.35f + i * 0.82f;
            float radius = 16f + (i % 3) * 4f;
            Vector3 position = battlePyramid.position + battlePyramid.right * (Mathf.Cos(angle) * radius) + battlePyramid.forward * (Mathf.Sin(angle) * radius);
            position.y = GetPlayableGroundHeight(position) + 0.35f;
            CreateSalvageField(position, "Salvage Benchmark Wreckage " + i, new SandRunnersResourcePrice(5f + i, 3f, 0f), 3);
        }
        lastEvent = "SALVAGE BENCHMARK: Scarab released. Collect the marked wreckage.";
        ShowBanner("SALVAGE BENCHMARK // F5", 3f);
    }

    private void UpdateSalvageBenchmark(float dt)
    {
        if (!salvageBenchmarkActive)
            return;
        salvageBenchmarkTimer += dt;
        if (salvageBenchmarkTimer < 24f)
            return;
        salvageBenchmarkActive = false;
        salvageBenchmarkTimer = 0f;
        lastEvent = "Salvage benchmark complete. Scarab control remains available.";
        ShowBanner("SALVAGE BENCHMARK COMPLETE", 3f);
    }

    private SalvageField FindNearestSalvageField(Vector3 origin, RunnerUnit requester = null)
    {
        SalvageField best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            bool reserved = field != null && field.reservedBy != null;
            if (field == null || field.root == null || field.delivered ||
                !SandRunnersSalvageRules.CanClaim(field.collected, reserved, field.reservedBy == requester))
                continue;
            float distance = FlatDistance(origin, field.root.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = field;
            }
        }
        return best;
    }

    private bool TryIssueSalvageOrderAtHit(RaycastHit hit)
    {
        if (hit.transform == null)
            return false;
        SalvageField target = null;
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            if (field != null && field.root != null && IsTransformInHierarchy(hit.transform, field.root) && !field.collected)
            {
                target = field;
                break;
            }
        }
        if (target == null)
            return false;

        bool assigned = false;
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null)
                continue;
            for (int u = 0; u < squad.units.Count; u++)
            {
                RunnerUnit runner = squad.units[u];
                if (runner == null || !runner.isSalvageScarab || (target.reservedBy != null && target.reservedBy != runner))
                    continue;
                AssignSalvageField(runner, target, false);
                assigned = true;
                break;
            }
            if (assigned)
                break;
        }
        if (assigned)
        {
            lastEvent = "SALVAGE ORDER: scarab assigned to wreckage.";
            ShowBanner(lastEvent, 2f);
        }
        return assigned;
    }

    private void AssignSalvageField(RunnerUnit runner, SalvageField field, bool autoMode)
    {
        if (runner == null)
            return;
        if (runner.salvageTarget != null && runner.salvageTarget.reservedBy == runner && runner.salvageTarget != field)
            runner.salvageTarget.reservedBy = null;
        runner.salvageTarget = field;
        runner.salvageAutoMode = autoMode;
        runner.salvageReturnOrder = false;
        runner.salvagePickupProgress = 0f;
        runner.hasOrder = field != null;
        if (field != null)
        {
            field.reservedBy = runner;
            runner.orderPosition = field.root.position;
        }
    }

    private void ClearSalvageAssignment(RunnerUnit runner)
    {
        if (runner == null)
            return;
        if (runner.salvageTarget != null && runner.salvageTarget.reservedBy == runner && runner.salvageTarget.carrier != runner.transform)
            runner.salvageTarget.reservedBy = null;
        runner.salvageTarget = null;
        runner.salvageReturnOrder = false;
        runner.salvagePickupProgress = 0f;
        runner.hasOrder = false;
    }

    private void UpdateSalvageScarabUnit(RunnerUnit runner, float dt)
    {
        if (runner == null || runner.transform == null || battlePyramid == null)
            return;

        SalvageField target = runner.salvageTarget;
        if (target != null && (target.root == null || target.delivered || (target.reservedBy != null && target.reservedBy != runner)))
        {
            ClearSalvageAssignment(runner);
            target = null;
        }

        if (runner.salvageReturnOrder && (target == null || !target.collected || target.carrier != runner.transform))
        {
            runner.salvageReturnOrder = false;
            runner.hasOrder = false;
        }

        if (target == null && runner.salvageAutoMode)
        {
            target = FindNearestSalvageField(runner.transform.position, runner);
            AssignSalvageField(runner, target, true);
        }

        if (target == null)
        {
            MoveSalvageScarabTo(runner, battlePyramid.position + battlePyramid.right * 14f + battlePyramid.forward * 12f, dt);
            return;
        }

        if (!target.collected)
        {
            MoveSalvageScarabTo(runner, target.root.position, dt);
            if (FlatDistance(runner.transform.position, target.root.position) < 3.8f)
            {
                runner.salvagePickupProgress += dt;
                float pickupSeconds = balanceProfile != null ? Mathf.Max(0.1f, balanceProfile.salvagePickupSeconds) : 0.8f;
                if (runner.salvagePickupProgress >= pickupSeconds)
                {
                    target.collected = true;
                    target.carrier = runner.transform;
                    target.reservedBy = runner;
                    runner.salvageReturnOrder = true;
                    runner.hasOrder = false;
                    PlaySandRunnerSound(SandRunnerSound.HarvestStart, runner.transform.position, 0.42f);
                    lastEvent = "SALVAGE SCARAB: field secured; returning with cargo.";
                    ShowBanner(lastEvent, 1.8f);
                }
            }
            else
            {
                runner.salvagePickupProgress = 0f;
            }
            return;
        }

        if (target.carrier != runner.transform)
        {
            ClearSalvageAssignment(runner);
            return;
        }

        MoveSalvageScarabTo(runner, battlePyramid.position + battlePyramid.forward * 6f, dt);
        if (FlatDistance(runner.transform.position, battlePyramid.position) >= 7f)
            return;

        DeliverSalvageField(runner, target);
    }

    private void DeliverSalvageField(RunnerUnit runner, SalvageField field)
    {
        if (runner == null || field == null ||
            !SandRunnersSalvageRules.TryConsume(ref field.delivered, field.payload, out SandRunnersResourcePrice reward))
            return;
        sand += reward.sand;
        gold += reward.gold;
        wind += reward.wind;
        string payload = FormatSalvagePayload(reward);
        field.carrier = null;
        field.reservedBy = null;
        runner.salvageTarget = null;
        runner.salvageReturnOrder = false;
        runner.salvagePickupProgress = 0f;
        runner.hasOrder = false;
        PlaySandRunnerSound(SandRunnerSound.ResourceDelivery, battlePyramid.position, 0.55f);
        lastEvent = "SALVAGE DELIVERED // " + payload;
        ShowBanner(lastEvent, 2f);
        RecycleSalvageField(field);
    }

    private void MoveSalvageScarabTo(RunnerUnit runner, Vector3 target, float dt)
    {
        Vector3 delta = target - runner.transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude >= 0.05f)
        {
            runner.transform.position += delta.normalized * runner.speed * dt;
            RotateFlatToward(runner.transform, delta, 180f * dt);
        }
        Vector3 grounded = runner.transform.position;
        grounded.y = GetPlayableGroundHeight(grounded) + 0.45f;
        runner.transform.position = grounded;
    }

    private RunnerUnit GetSelectedSalvageRunner()
    {
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null)
                continue;
            for (int u = 0; u < squad.units.Count; u++)
            {
                RunnerUnit runner = squad.units[u];
                if (runner != null && runner.isSalvageScarab)
                    return runner;
            }
        }
        return null;
    }

    private void SetSelectedSalvageCollectNearest()
    {
        RunnerUnit runner = GetSelectedSalvageRunner();
        if (runner == null)
            return;
        SalvageField field = FindNearestSalvageField(runner.transform.position, runner);
        AssignSalvageField(runner, field, false);
        lastEvent = field != null ? "SALVAGE SCARAB: nearest field selected." : "SALVAGE SCARAB: no available field.";
        ShowBanner(lastEvent, 1.8f);
    }

    private void SetSelectedSalvageReturn()
    {
        RunnerUnit runner = GetSelectedSalvageRunner();
        if (runner == null)
            return;
        runner.salvageAutoMode = false;
        if (runner.salvageTarget != null && runner.salvageTarget.collected && runner.salvageTarget.carrier == runner.transform)
            runner.salvageReturnOrder = true;
        else
            ClearSalvageAssignment(runner);
        lastEvent = "SALVAGE SCARAB: returning to pyramid.";
        ShowBanner(lastEvent, 1.8f);
    }

    private void SetSelectedSalvageAuto()
    {
        RunnerUnit runner = GetSelectedSalvageRunner();
        if (runner == null)
            return;
        if (runner.salvageTarget == null || runner.salvageTarget.carrier != runner.transform)
            ClearSalvageAssignment(runner);
        runner.salvageAutoMode = true;
        lastEvent = "SALVAGE SCARAB: automatic scavenging enabled.";
        ShowBanner(lastEvent, 1.8f);
    }

    private string GetSalvageRunnerStatus(RunnerUnit runner)
    {
        if (runner == null)
            return string.Empty;
        SalvageField target = runner.salvageTarget;
        if (target == null || target.root == null)
            return runner.salvageAutoMode ? "AUTO // scanning for fields" : "STANDBY // awaiting order";
        float distance = FlatDistance(runner.transform.position, target.root.position);
        if (target.collected && target.carrier == runner.transform)
            return "CARGO // " + FormatSalvagePayload(target.payload) + "\nPYRAMID " + Mathf.RoundToInt(FlatDistance(runner.transform.position, battlePyramid.position)) + " m";
        float pickupSeconds = balanceProfile != null ? Mathf.Max(0.1f, balanceProfile.salvagePickupSeconds) : 0.8f;
        int progress = Mathf.RoundToInt(Mathf.Clamp01(runner.salvagePickupProgress / pickupSeconds) * 100f);
        return "TARGET // " + target.label + "\n" + Mathf.RoundToInt(distance) + " m  •  LOAD " + progress + "%";
    }

    private static string FormatSalvagePayload(SandRunnersResourcePrice payload)
    {
        return Mathf.RoundToInt(payload.sand) + "S  " + Mathf.RoundToInt(payload.gold) + "G  " + Mathf.RoundToInt(payload.wind) + "W";
    }

    private void GetMarkerSalvageFields(List<SalvageField> output, Vector3 origin)
    {
        output.Clear();
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            if (field != null && field.root != null && !field.collected && !field.delivered)
                output.Add(field);
        }
        output.Sort((a, b) => DistanceToField(origin, a).CompareTo(DistanceToField(origin, b)));
        float clusterRadius = presentationBudget != null ? Mathf.Max(12f, presentationBudget.salvageMergeDistance * 1.5f) : 18f;
        int write = 0;
        for (int read = 0; read < output.Count; read++)
        {
            SalvageField candidate = output[read];
            bool clustered = false;
            for (int kept = 0; kept < write; kept++)
            {
                if (FlatDistance(candidate.root.position, output[kept].root.position) <= clusterRadius)
                {
                    clustered = true;
                    break;
                }
            }
            if (!clustered)
                output[write++] = candidate;
        }
        if (output.Count > write)
            output.RemoveRange(write, output.Count - write);
        if (output.Count > MaxSalvageMarkers)
            output.RemoveRange(MaxSalvageMarkers, output.Count - MaxSalvageMarkers);
    }

    private int GetSalvageMarkerClusterCount(SalvageField representative)
    {
        if (representative == null || representative.root == null)
            return 0;
        float clusterRadius = presentationBudget != null ? Mathf.Max(12f, presentationBudget.salvageMergeDistance * 1.5f) : 18f;
        int count = 0;
        for (int i = 0; i < salvageFields.Count; i++)
        {
            SalvageField field = salvageFields[i];
            if (field != null && field.root != null && !field.collected && !field.delivered &&
                FlatDistance(representative.root.position, field.root.position) <= clusterRadius)
                count++;
        }
        return count;
    }

    private void DrawSalvageWorldMarkers() { }
    private void DrawSalvageScarabContextGUI() { }
}
