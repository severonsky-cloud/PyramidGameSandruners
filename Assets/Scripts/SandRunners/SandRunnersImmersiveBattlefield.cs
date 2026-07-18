using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class SandRunnersPrototype
{
    private Transform immersiveBattlefieldRoot;
    private Material horizonHazeMaterial;
    private Material windStreakMaterial;
    private ParticleSystem rollingSandParticles;
    private bool immersiveBattlefieldInitialized;
    private bool cameraTrackingInitialized;
    private Vector3 previousPyramidCameraPosition;
    private float cameraSpeedBlend;
    private float combatCameraBlend;
    private Vector3 combatCameraThreatPoint;

    private void InitializeImmersiveBattlefield()
    {
        cameraDistance = Mathf.Clamp(cameraDistance, 62f, 78f);
        cameraHeight = Mathf.Clamp(cameraHeight, 13f, 17f);
        cameraPitch = Mathf.Clamp(cameraPitch, 22f, 36f);
        cameraSmooth = Mathf.Clamp(cameraSmooth, 6.5f, 8.5f);
        mouseLookSensitivity = Mathf.Max(mouseLookSensitivity, 0.2f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0062f;
        RenderSettings.fogColor = new Color(0.46f, 0.25f, 0.16f, 1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.135f, 0.08f, 1f);

        if (mainCamera != null)
        {
            mainCamera.nearClipPlane = 0.08f;
            mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 2200f);
            mainCamera.allowHDR = true;
        }

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional)
                continue;

            lights[i].transform.rotation = Quaternion.Euler(43f, -32f, 0f);
            lights[i].color = new Color(1f, 0.54f, 0.28f, 1f);
            lights[i].intensity = Mathf.Max(lights[i].intensity, 2.05f);
            lights[i].shadowStrength = 0.78f;
        }

        if (immersiveBattlefieldInitialized)
            return;

        GameObject existing = GameObject.Find("SandRunners_Immersive_Battlefield_Runtime");
        if (existing != null)
            Destroy(existing);

        immersiveBattlefieldRoot = new GameObject("SandRunners_Immersive_Battlefield_Runtime").transform;
        horizonHazeMaterial = CreateMaterial("SandRunners Red Horizon Haze", new Color(0.72f, 0.44f, 0.28f, 0.065f));
        windStreakMaterial = CreateMaterial("SandRunners Wind Streaks", new Color(0.86f, 0.62f, 0.36f, 0.105f));
        ConfigureTransparent(horizonHazeMaterial);
        ConfigureTransparent(windStreakMaterial);
        SetEmission(horizonHazeMaterial, new Color(0.95f, 0.36f, 0.16f, 1f), 0.14f);
        SetEmission(windStreakMaterial, new Color(1f, 0.64f, 0.22f, 1f), 0.18f);

        CreateHorizonHazeBands();
        CreateRollingSandParticles();
        CreatePostProcessingVolume();

        immersiveBattlefieldInitialized = true;
    }

    private void UpdateImmersiveBattlefield(float dt)
    {
        if (battlePyramid == null)
            return;

        if (!cameraTrackingInitialized)
        {
            previousPyramidCameraPosition = battlePyramid.position;
            cameraTrackingInitialized = true;
        }

        float speed = (battlePyramid.position - previousPyramidCameraPosition).magnitude / Mathf.Max(0.001f, dt);
        cameraSpeedBlend = Mathf.Lerp(cameraSpeedBlend, Mathf.Clamp01(speed / 7f), dt * 2.4f);
        previousPyramidCameraPosition = battlePyramid.position;

        RenderSettings.fogDensity = Mathf.Lerp(0.0046f, 0.0072f, cameraSpeedBlend);
        UpdatePyramidCombatFraming(dt);

        if (rollingSandParticles != null)
        {
            rollingSandParticles.transform.position = battlePyramid.position + Vector3.up * 2.4f - battlePyramid.forward * 9f;
            ParticleSystem.EmissionModule emission = rollingSandParticles.emission;
            emission.rateOverTime = Mathf.Lerp(18f, 120f, cameraSpeedBlend);
        }
    }

    private void UpdatePyramidCombatFraming(float dt)
    {
        Vector3 weightedThreat = Vector3.zero;
        float totalWeight = 0f;
        Vector3 pyramidPosition = battlePyramid.position;
        Vector3 pyramidForward = battlePyramid.forward;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || enemy.transform == null || enemy.health <= 0f || enemy.isStrategicMissile)
                continue;

            Vector3 delta = enemy.transform.position - pyramidPosition;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 4f || distance > 260f)
                continue;

            float forwardDot = Vector3.Dot(pyramidForward, delta / distance);
            if (forwardDot < -0.2f)
                continue;

            float proximity = 1f - Mathf.InverseLerp(55f, 260f, distance);
            float weight = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01((forwardDot + 0.2f) / 1.2f)) *
                           Mathf.Lerp(0.28f, 1f, proximity);
            if (enemy.isAssaultHeadquarters)
                weight *= 1.8f;
            weightedThreat += enemy.transform.position * weight;
            totalWeight += weight;
        }

        float desiredBlend = totalWeight > 0.01f ? 1f : 0f;
        combatCameraBlend = Mathf.MoveTowards(combatCameraBlend, desiredBlend,
            dt * (desiredBlend > combatCameraBlend ? 1.8f : 0.75f));
        if (totalWeight > 0.01f)
        {
            Vector3 desiredThreat = weightedThreat / totalWeight;
            if (combatCameraThreatPoint == Vector3.zero)
                combatCameraThreatPoint = desiredThreat;
            else
                combatCameraThreatPoint = Vector3.Lerp(combatCameraThreatPoint, desiredThreat, dt * 2.4f);
        }
    }

    private Vector3 GetImmersiveCameraFocus(float dt)
    {
        if (battlePyramid == null)
            return Vector3.zero;

        if (cinematicDirectorActive)
            return GetCinematicDirectorCameraFocus(dt);

        if (TryGetRTSCameraFollowTarget(out Transform followTarget, out cameraFollowLabel))
        {
            float heightNear;
            float heightFar;
            float lookNear;
            float lookFar;
            float fovNear;
            float fovFar;
            float startDistance;
            float minDistance;
            float maxDistance;
            GetRTSFollowCameraProfile(followTarget, out heightNear, out heightFar, out lookNear, out lookFar, out fovNear, out fovFar, out startDistance, out minDistance, out maxDistance);

            Vector3 current = GetRTSCameraFollowPoint(followTarget);
            if (!cameraFollowInitialized)
            {
                cameraFollowLastPosition = current;
                cameraFollowInitialized = true;
            }

            Vector3 motion = current - cameraFollowLastPosition;
            motion.y = 0f;
            float followSpeed = motion.magnitude / Mathf.Max(0.001f, dt);
            cameraFollowSpeedBlend = Mathf.Lerp(cameraFollowSpeedBlend, Mathf.Clamp01(followSpeed / 10f), dt * 3.2f);
            if (motion.sqrMagnitude > 0.04f && cameraFollowManualOrbitTimer <= 0f)
            {
                float movementYaw = Mathf.Atan2(motion.x, motion.z) * Mathf.Rad2Deg;
                cameraYaw = Mathf.LerpAngle(cameraYaw, movementYaw, dt * 1.45f);
            }

            cameraFollowLastPosition = current;
            Vector3 forward = motion.sqrMagnitude > 0.04f ? motion.normalized : Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
            float groundY = GetPlayableGroundHeight(current);
            float airborneBias = Mathf.Clamp(current.y - groundY, 0f, 10f) * 0.35f;

            // Calculate target physical center height based on tags
            string labelLower = cameraFollowLabel != null ? cameraFollowLabel.ToLowerInvariant() : "";
            bool flyer = labelLower.Contains("flyer") || labelLower.Contains("abydos") || labelLower.Contains("thoth") || labelLower.Contains("vimana");
            bool builder = labelLower.Contains("builder");
            bool scarab = labelLower.Contains("scarab") || labelLower.Contains("skimmer");
            bool heavy = labelLower.Contains("wrath") || labelLower.Contains("crusher") || labelLower.Contains("siege") || labelLower.Contains("heavy");
            bool structure = labelLower.Contains("turret") || labelLower.Contains("depot") || labelLower.Contains("blueprint") || labelLower.Contains("launcher");
            bool fortress = labelLower.Contains("pyramid") || labelLower.Contains("fortress") || labelLower.Contains("castle") || labelLower.Contains("mandarinka");

            float centerHeight = 0.7f;
            if (builder) centerHeight = 0.4f;
            else if (flyer) centerHeight = 0.1f;
            else if (heavy) centerHeight = 1.0f;
            else if (scarab) centerHeight = 0.5f;
            else if (structure) centerHeight = 1.2f;
            else if (fortress) centerHeight = 3.0f;

            // Blend based on zoom level: zoomFactor is 0 when zoomed in close, 1 when far
            float zoomFactor = Mathf.InverseLerp(minDistance, maxDistance, cameraDistance);

            // Interpolate height and look-ahead
            float followHeight = Mathf.Lerp(centerHeight, Mathf.Lerp(heightNear, heightFar, cameraFollowSpeedBlend), zoomFactor) + airborneBias;
            float lookAheadAmt = Mathf.Lerp(0f, Mathf.Lerp(lookNear, lookFar, cameraFollowSpeedBlend), zoomFactor);
            Vector3 followLookAhead = forward * lookAheadAmt;

            return current + Vector3.up * followHeight + followLookAhead;
        }

        Vector3 lookAhead = battlePyramid.forward * Mathf.Lerp(9f, 20f, cameraSpeedBlend);
        Vector3 shoulder = battlePyramid.right * Mathf.Sin(Time.time * 0.55f) * Mathf.Lerp(0.35f, 1.2f, cameraSpeedBlend);
        Vector3 focus = battlePyramid.position + Vector3.up * cameraHeight + lookAhead + shoulder;

        // The player still drives the pyramid. During combat we only frame more of
        // the road ahead, keeping the hull in the lower third instead of replacing
        // it with a detached RTS camera.
        if (combatCameraBlend > 0.001f && combatCameraThreatPoint != Vector3.zero)
        {
            Vector3 towardThreat = combatCameraThreatPoint - battlePyramid.position;
            towardThreat.y = 0f;
            focus += Vector3.ClampMagnitude(towardThreat, 46f) * (0.34f * combatCameraBlend);
            focus += Vector3.up * (4.5f * combatCameraBlend);
        }
        return focus;
    }

    private float GetImmersiveCameraFieldOfView()
    {
        if (cinematicDirectorActive)
            return GetCinematicDirectorFieldOfView();

        if (cameraFollowSelectionMode)
        {
            float heightNear;
            float heightFar;
            float lookNear;
            float lookFar;
            float fovNear;
            float fovFar;
            float startDistance;
            float minDistance;
            float maxDistance;
            GetRTSFollowCameraProfile(cameraFollowTarget, out heightNear, out heightFar, out lookNear, out lookFar, out fovNear, out fovFar, out startDistance, out minDistance, out maxDistance);
            return Mathf.Lerp(fovNear, fovFar, cameraFollowSpeedBlend) + beamCharge * 3f;
        }
        return Mathf.Lerp(54f, 62f, cameraSpeedBlend) + combatCameraBlend * 5f + beamCharge * 4f;
    }

    private void GetRTSFollowCameraProfile(Transform target, out float heightNear, out float heightFar, out float lookNear, out float lookFar, out float fovNear, out float fovFar, out float startDistance, out float minDistance, out float maxDistance)
    {
        string label = GetRTSCameraFollowLabel(target).ToLowerInvariant();
        bool flyer = label.Contains("flyer") || label.Contains("abydos") || label.Contains("thoth") || label.Contains("vimana");
        bool builder = label.Contains("builder");
        bool scarab = label.Contains("scarab") || label.Contains("skimmer");
        bool heavy = label.Contains("wrath") || label.Contains("crusher") || label.Contains("siege") || label.Contains("heavy");
        bool structure = label.Contains("turret") || label.Contains("depot") || label.Contains("blueprint") || label.Contains("launcher");
        bool fortress = label.Contains("pyramid") || label.Contains("fortress") || label.Contains("castle") || label.Contains("mandarinka");

        if (builder)
        {
            heightNear = 1.15f;
            heightFar = 1.85f;
            lookNear = 0.4f;
            lookFar = 2.7f;
            fovNear = 36f;
            fovFar = 43f;
            startDistance = 8.5f;
            minDistance = 5.5f;
            maxDistance = 22f;
            return;
        }

        if (flyer)
        {
            heightNear = 3.8f;
            heightFar = 7.5f;
            lookNear = 3.8f;
            lookFar = 13f;
            fovNear = 48f;
            fovFar = 62f;
            startDistance = 19f;
            minDistance = 11f;
            maxDistance = 48f;
            return;
        }

        if (heavy)
        {
            heightNear = 2.0f;
            heightFar = 3.4f;
            lookNear = 1.5f;
            lookFar = 6.5f;
            fovNear = 40f;
            fovFar = 52f;
            startDistance = 14f;
            minDistance = 8f;
            maxDistance = 38f;
            return;
        }

        if (scarab)
        {
            heightNear = 1.6f;
            heightFar = 2.8f;
            lookNear = 1.2f;
            lookFar = 5.6f;
            fovNear = 39f;
            fovFar = 51f;
            startDistance = 11.5f;
            minDistance = 6.5f;
            maxDistance = 32f;
            return;
        }

        if (structure)
        {
            heightNear = 2.8f;
            heightFar = 5.6f;
            lookNear = 0.8f;
            lookFar = 4.0f;
            fovNear = 41f;
            fovFar = 51f;
            startDistance = 18f;
            minDistance = 9f;
            maxDistance = 50f;
            return;
        }

        if (fortress)
        {
            heightNear = 8f;
            heightFar = 14f;
            lookNear = 4f;
            lookFar = 16f;
            fovNear = 48f;
            fovFar = 60f;
            startDistance = 44f;
            minDistance = 22f;
            maxDistance = 90f;
            return;
        }

        heightNear = 1.8f;
        heightFar = 3.8f;
        lookNear = 1.5f;
        lookFar = 7.5f;
        fovNear = 40f;
        fovFar = 53f;
        startDistance = 14f;
        minDistance = 7f;
        maxDistance = 38f;
    }

    private float GetRTSCameraFollowStartDistance(Transform target)
    {
        float heightNear;
        float heightFar;
        float lookNear;
        float lookFar;
        float fovNear;
        float fovFar;
        float startDistance;
        float minDistance;
        float maxDistance;
        GetRTSFollowCameraProfile(target, out heightNear, out heightFar, out lookNear, out lookFar, out fovNear, out fovFar, out startDistance, out minDistance, out maxDistance);
        return startDistance;
    }

    private void GetRTSCameraFollowZoomRange(Transform target, out float minDistance, out float maxDistance)
    {
        float heightNear;
        float heightFar;
        float lookNear;
        float lookFar;
        float fovNear;
        float fovFar;
        float startDistance;
        GetRTSFollowCameraProfile(target, out heightNear, out heightFar, out lookNear, out lookFar, out fovNear, out fovFar, out startDistance, out minDistance, out maxDistance);
    }

    private void CreateHorizonHazeBands()
    {
        float far = mapHalfSize - 34f;
        CreateMirageVeil("Red_Horizon_Mirage_North", Vector3.forward, far);
        CreateMirageVeil("Red_Horizon_Mirage_South", Vector3.back, far);
        CreateMirageVeil("Red_Horizon_Mirage_East", Vector3.right, far);
        CreateMirageVeil("Red_Horizon_Mirage_West", Vector3.left, far);

        for (int i = 0; i < 36; i++)
        {
            float angle = i * 10f * Mathf.Deg2Rad;
            float radius = mapHalfSize - 90f - (i % 5) * 18f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = ComputeDuneHeight(x, z) + 1.4f + (i % 4) * 0.55f;
            Quaternion rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            CreateBox(immersiveBattlefieldRoot, "Distant_Mega_Dune_Silhouette_" + i, new Vector3(x, y, z), rotation, new Vector3(42f + (i % 6) * 14f, 3.2f + (i % 5), 1.1f), horizonHazeMaterial);
        }

        for (int i = 0; i < 34; i++)
        {
            float angle = i * 47.3f * Mathf.Deg2Rad;
            float radius = 74f + (i % 9) * 31f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = ComputeDuneHeight(x, z) + 0.42f;
            CreateBox(immersiveBattlefieldRoot, "Wind_Sand_Streak_" + i, new Vector3(x, y, z), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 70f, 0f), new Vector3(40f + (i % 4) * 12f, 0.025f, 0.24f), windStreakMaterial);
        }
    }

    private void CreateMirageVeil(string name, Vector3 direction, float distance)
    {
        bool northSouth = Mathf.Abs(direction.z) > 0.5f;
        Vector3 position = direction * distance + Vector3.up * 9f;
        Quaternion rotation = northSouth ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        Vector3 scale = new Vector3(mapHalfSize * 1.22f, 8f, 0.24f);
        CreateBox(immersiveBattlefieldRoot, name, position, rotation, scale, horizonHazeMaterial);
    }

    private void CreateRollingSandParticles()
    {
        GameObject go = new GameObject("Camera_Rolling_Sand_Particles");
        go.transform.SetParent(immersiveBattlefieldRoot, false);
        rollingSandParticles = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = rollingSandParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 2.4f;
        main.startSpeed = 4.6f;
        main.startSize = 0.32f;
        main.startColor = new Color(0.88f, 0.61f, 0.36f, 0.24f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 420;

        ParticleSystem.EmissionModule emission = rollingSandParticles.emission;
        emission.rateOverTime = 34f;

        ParticleSystem.ShapeModule shape = rollingSandParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22f, 3.2f, 10f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = windStreakMaterial;
    }

    private void CreatePostProcessingVolume()
    {
        GameObject existing = GameObject.Find("SandRunners_URP_Atmosphere_Volume");
        if (existing != null)
            Destroy(existing);

        GameObject volumeObject = new GameObject("SandRunners_URP_Atmosphere_Volume");
        volumeObject.transform.SetParent(immersiveBattlefieldRoot, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 32f;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Bloom bloom = volume.profile.Add<Bloom>(true);
        bloom.intensity.Override(0.38f);
        bloom.threshold.Override(1.02f);
        bloom.scatter.Override(0.48f);

        ColorAdjustments color = volume.profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.02f);
        color.contrast.Override(10f);
        color.saturation.Override(-3f);
        color.colorFilter.Override(new Color(1f, 0.96f, 0.9f, 1f));

        Vignette vignette = volume.profile.Add<Vignette>(true);
        vignette.intensity.Override(0.14f);
        vignette.smoothness.Override(0.48f);

        DepthOfField depth = volume.profile.Add<DepthOfField>(true);
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        depth.gaussianStart.Override(95f);
        depth.gaussianEnd.Override(390f);
        depth.gaussianMaxRadius.Override(0.25f);
    }
}