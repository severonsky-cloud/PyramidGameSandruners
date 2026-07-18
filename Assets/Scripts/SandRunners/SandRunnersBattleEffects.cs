using UnityEngine;

public partial class SandRunnersPrototype
{
    private Material battleTracerMaterial;
    private Material battleSparkMaterial;
    private Material battleSmokeMaterial;
    private Material battleScorchMaterial;
    private MaterialPropertyBlock battleEffectPropertyBlock;

    private void EnsureBattleEffectMaterials()
    {
        if (battleEffectPropertyBlock == null)
            battleEffectPropertyBlock = new MaterialPropertyBlock();

        if (battleTracerMaterial != null)
            return;

        battleTracerMaterial = CreateMaterial("SandRunners Hot Tracer FX", new Color(1f, 0.66f, 0.18f, 0.82f));
        battleSparkMaterial = CreateMaterial("SandRunners Spark Burst FX", new Color(1f, 0.45f, 0.12f, 0.92f));
        battleSmokeMaterial = CreateMaterial("SandRunners Smoke FX", new Color(0.16f, 0.09f, 0.065f, 0.34f));
        battleScorchMaterial = CreateMaterial("SandRunners Scorched Sand FX", new Color(0.065f, 0.038f, 0.025f, 0.42f));
        ConfigureTransparent(battleTracerMaterial);
        ConfigureTransparent(battleSparkMaterial);
        ConfigureTransparent(battleSmokeMaterial);
        ConfigureTransparent(battleScorchMaterial);
        Texture2D scorchTexture = Resources.Load<Texture2D>("SandRunners/Textures/SR_ScorchCracks");
        AssignTextureToMaterial(battleScorchMaterial, scorchTexture, Vector2.one);
        SetEmission(battleTracerMaterial, new Color(1f, 0.55f, 0.12f, 1f), 1.8f);
        SetEmission(battleSparkMaterial, new Color(1f, 0.28f, 0.06f, 1f), 2.2f);
    }

    private void CreateWeaponTracer(Vector3 start, Vector3 end, Color color, float width, float life)
    {
        EnsureBattleEffectMaterials();
        if (presentationBudget != null && beams.Count >= presentationBudget.maxTracers)
            return;
        GameObject tracerObject = new GameObject("SandRunners_Hot_Tracer");
        LineRenderer line = tracerObject.AddComponent<LineRenderer>();
        float readableWidth = Mathf.Max(width * 3.2f, 0.06f);
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = readableWidth;
        line.endWidth = readableWidth * 0.35f;
        line.material = battleTracerMaterial;
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0f);
        line.useWorldSpace = true;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;

        SpawnPooledPresentationLight("RC_Tracer_Glint", Vector3.Lerp(start, end, 0.5f), color,
            Mathf.Clamp(readableWidth * 14f, 1f, 4f), Mathf.Clamp(readableWidth * 42f, 6f, 24f), Mathf.Max(0.12f, life));

        BeamVisual beam = new BeamVisual();
        beam.line = line;
        beam.life = life;
        beams.Add(beam);
    }

    private void AttachProjectileTrail(GameObject projectileObject, Color color, float width, float time)
    {
        if (projectileObject == null)
            return;

        EnsureBattleEffectMaterials();
        TrailRenderer trail = projectileObject.AddComponent<TrailRenderer>();
        float readableWidth = Mathf.Max(width * 1.35f, 0.16f);
        trail.time = Mathf.Max(time * 1.2f, 0.55f);
        trail.startWidth = readableWidth;
        trail.endWidth = 0f;
        trail.material = battleTracerMaterial;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.autodestruct = false;

        if (readableWidth >= 0.42f)
            SpawnPooledPresentationLight("RC_Projectile_Glint", projectileObject.transform.position, color,
                Mathf.Clamp(readableWidth * 4.2f, 0.8f, 2.8f), Mathf.Clamp(readableWidth * 18f, 4f, 14f), Mathf.Max(0.25f, time));
    }

    private void CreateBeamImpactFx(Vector3 start, Vector3 end, Color color, float width)
    {
        EnsureBattleEffectMaterials();
        float sparkSize = Mathf.Clamp(width * 32f, 1.2f, 12f);
        CreateSparkBurst(end, color, sparkSize, 0.28f);
        CreateImpactMarker(end, color, Mathf.Clamp(width * 22f, 1.8f, 10f), 0.85f);

        SpawnPooledPresentationLight("RC_Beam_Impact", end, color, Mathf.Clamp(width * 16f, 1.2f, 6f), Mathf.Clamp(width * 28f, 6f, 24f), 0.22f);
    }

    private void CreateBattleExplosionFx(Vector3 position, float radius, bool nuclear, bool hostile)
    {
        EnsureBattleEffectMaterials();
        Color flashColor = nuclear ? new Color(1f, 0.28f, 0.08f, 1f) : hostile ? new Color(1f, 0.08f, 0.035f, 1f) : new Color(1f, 0.62f, 0.12f, 1f);
        float visualRadius = radius * (presentationBudget != null ? presentationBudget.largeEffectScale : 0.72f);
        float scale = nuclear ? visualRadius * 0.18f : visualRadius * 0.12f;

        CreateSparkBurst(position + Vector3.up * 0.55f, flashColor, Mathf.Max(2f, scale), nuclear ? 0.5f : 0.35f);
        CreateSmokeBurst(position + Vector3.up * 0.45f, visualRadius, nuclear);
        CreateImpactMarker(position, flashColor, Mathf.Clamp(visualRadius * 0.68f, 3f, nuclear ? 20f : 11f), nuclear ? 2.5f : 1.3f);
        if (visualRadius >= 5f || nuclear)
            CreateFloatingCombatLabel(position + Vector3.up * 2.6f, nuclear ? "SUN-CORE HIT" : "HEAVY HIT", flashColor, nuclear ? 1.5f : 1.1f);

        GameObject scorch = AcquirePresentationObject("RC_Scorch", PresentationKind.Scorch, position, nuclear ? 12f : 6.5f, () =>
        {
            GameObject created = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider createdCollider = created.GetComponent<Collider>();
            if (createdCollider != null)
                Destroy(createdCollider);
            return created;
        });
        if (scorch != null)
        {
            scorch.name = hostile ? "Hostile_Scorched_Sand" : "Golden_Scorched_Sand";
            scorch.transform.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.055f, position.z);
            scorch.transform.localScale = new Vector3(visualRadius * 0.9f, 0.02f, visualRadius * 0.9f);
            Renderer renderer = scorch.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = battleScorchMaterial;
        }

        SpawnPooledPresentationLight(hostile ? "RC_Hostile_Explosion" : "RC_Golden_Explosion", position + Vector3.up * 3f,
            flashColor, nuclear ? 9f : 4.2f, nuclear ? visualRadius * 3f : visualRadius * 2.2f, nuclear ? 0.65f : 0.38f);
        RegisterReleaseHeavyImpact(visualRadius, nuclear);
    }

    private void CreateShieldImpactFx(Vector3 position, float intensity)
    {
        EnsureBattleEffectMaterials();
        Color color = new Color(0.12f, 1f, 0.68f, 1f);
        CreateSparkBurst(position + Vector3.up * 7f, color, Mathf.Clamp(intensity * 0.02f, 1.2f, 5.8f), 0.3f);
        CreateImpactMarker(position, color, Mathf.Clamp(intensity * 0.035f, 2.2f, 8f), 0.72f);
        CreateFloatingCombatLabel(position + Vector3.up * 8.2f, "SHIELD", color, 0.75f);
        SpawnPooledPresentationLight("RC_Shield_Impact", position + Vector3.up * 8.5f, color, Mathf.Clamp(intensity * 0.012f, 1.2f, 5.5f), 20f, 0.26f);
    }

    private void CreateFortressPhaseFx(Vector3 position, Color color)
    {
        EnsureBattleEffectMaterials();
        CreateSparkBurst(position + Vector3.up * 10f, color, 8f, 0.75f);
        SpawnPooledPresentationLight("RC_Fortress_Phase", position + Vector3.up * 9f, color, 6f, 36f, 0.8f);
    }

    private void CreateRepairBeamFx(Vector3 start, Vector3 end)
    {
        CreateWeaponTracer(start, end, new Color(0.12f, 1f, 0.58f, 0.9f), 0.075f, 0.18f);
        CreateBeamImpactFx(start, end, new Color(0.12f, 1f, 0.58f, 1f), 0.08f);
    }

    private void CreateSparkBurst(Vector3 position, Color color, float size, float life)
    {
        GameObject go = AcquirePresentationObject("RC_Spark_Burst", PresentationKind.Particle, position, life + 1.2f, () =>
        {
            GameObject created = new GameObject("SandRunners_Spark_Burst");
            created.AddComponent<ParticleSystem>();
            return created;
        });
        if (go == null)
            return;
        ParticleSystem particles = go.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = life;
        main.loop = false;
        main.startLifetime = life;
        main.startSpeed = Mathf.Lerp(2.6f, 11f, Mathf.Clamp01(size / 8f));
        main.startSize = Mathf.Max(0.08f, size * 0.12f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(size * 12f, 14f, 88f)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.2f, size * 0.18f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = battleSparkMaterial;

        particles.Play();
    }

    private void CreateSmokeBurst(Vector3 position, float radius, bool nuclear)
    {
        float poolLife = nuclear ? 5.5f : 3.4f;
        GameObject go = AcquirePresentationObject(nuclear ? "RC_Nuclear_Smoke" : "RC_Battle_Smoke", PresentationKind.Particle, position, poolLife, () =>
        {
            GameObject created = new GameObject("Battle_Smoke_Burst");
            created.AddComponent<ParticleSystem>();
            return created;
        });
        if (go == null)
            return;
        go.name = nuclear ? "Sun_Core_Smoke_Column" : "Battle_Smoke_Burst";
        ParticleSystem particles = go.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = nuclear ? 2.1f : 1.2f;
        main.loop = false;
        main.startLifetime = nuclear ? 3.2f : 1.8f;
        main.startSpeed = nuclear ? 6.5f : 3.8f;
        main.startSize = Mathf.Max(0.6f, radius * (nuclear ? 0.22f : 0.16f));
        main.startColor = nuclear ? new Color(0.17f, 0.07f, 0.035f, 0.5f) : new Color(0.14f, 0.08f, 0.055f, 0.38f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = nuclear ? 180 : 85;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(nuclear ? 92 : 42)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = nuclear ? 26f : 34f;
        shape.radius = Mathf.Max(0.6f, radius * 0.18f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = battleSmokeMaterial;

        particles.Play();
    }

    private void CreateImpactMarker(Vector3 position, Color color, float size, float life)
    {
        EnsureBattleEffectMaterials();
        GameObject marker = AcquirePresentationObject("RC_Impact_Marker", PresentationKind.Marker, position, life, () =>
        {
            GameObject created = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider createdCollider = created.GetComponent<Collider>();
            if (createdCollider != null)
                Destroy(createdCollider);
            return created;
        });
        if (marker == null || !marker)
            return;
        marker.name = "Readable_Impact_Marker";
        Transform markerTransform = marker.transform;
        if (markerTransform == null)
            return;
        markerTransform.position = new Vector3(position.x, GetPlayableGroundHeight(position) + 0.075f, position.z);
        markerTransform.localScale = new Vector3(size, 0.014f, size);
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null && battleTracerMaterial != null)
        {
            if (battleEffectPropertyBlock == null)
                battleEffectPropertyBlock = new MaterialPropertyBlock();

            renderer.sharedMaterial = battleTracerMaterial;
            battleEffectPropertyBlock.Clear();
            battleEffectPropertyBlock.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.34f));
            battleEffectPropertyBlock.SetColor("_Color", new Color(color.r, color.g, color.b, 0.34f));
            battleEffectPropertyBlock.SetColor("_EmissionColor", color * 0.95f);
            renderer.SetPropertyBlock(battleEffectPropertyBlock);
        }
    }

    private void CreateFloatingCombatLabel(Vector3 position, string label, Color color, float life)
    {
        GameObject textObject = AcquirePresentationObject("RC_Combat_Label", PresentationKind.Label, position, life, () =>
        {
            GameObject created = new GameObject("Readable_Combat_Label");
            created.AddComponent<TextMesh>();
            return created;
        });
        if (textObject == null)
            return;
        if (mainCamera != null)
            textObject.transform.rotation = Quaternion.LookRotation(textObject.transform.position - mainCamera.transform.position, Vector3.up);

        TextMesh text = textObject.GetComponent<TextMesh>();
        text.text = label;
        text.fontSize = 42;
        text.characterSize = 0.16f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = battleTracerMaterial;

    }
}
