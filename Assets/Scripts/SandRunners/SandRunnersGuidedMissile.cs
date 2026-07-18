using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private enum GuidedPhase { Eject, Boost, Cruise }
    private enum GuidedWeaponMode { PyramidCruise, Anubis, SunCore }

    private bool guidedMissileActive;
    private Transform guidedMissileTransform;
    private Vector3 guidedMissileVelocity;
    private float guidedMissileLife;
    private float guidedYaw;
    private float guidedPitch;
    private GuidedPhase guidedPhase;
    private GuidedWeaponMode guidedWeaponMode;
    private float guidedPhaseTimer;
    private Vector3 guidedLaunchPos;
    private Vector3 guidedCameraPositionBefore;
    private Quaternion guidedCameraRotationBefore;
    private bool guidedCameraPoseCached;
    private float guidedSpeed;
    private float guidedTurnRate;
    private float guidedDamage;
    private float guidedBlastRadius;
    private bool guidedInfiniteFlight;
    private float guidedDistanceTravelled;
    private float guidedCriticalMass;
    private bool guidedStrikeValid;
    private Vector3 guidedStrikePoint;
    private EnemyUnit guidedHighlightedEnemy;

    private const float GuidedMouseSensitivity = 0.12f;
    private const float GuidedEjectDuration = 0.7f;
    private const float GuidedBoostDuration = 1.1f;
    private static readonly Color GuidedIrTint = new Color(0.02f, 0.07f, 0.15f, 0.7f);

    private void LaunchGuidedMissile()
    {
        LaunchGuidedWeapon(GuidedWeaponMode.PyramidCruise);
    }

    private void LaunchGuidedAnubisMissile()
    {
        LaunchGuidedWeapon(GuidedWeaponMode.Anubis);
    }

    private void LaunchGuidedSunCoreMissile()
    {
        LaunchGuidedWeapon(GuidedWeaponMode.SunCore);
    }

    private void LaunchGuidedWeapon(GuidedWeaponMode mode)
    {
        if (guidedMissileActive)
        {
            lastEvent = "TV-link already controls an active missile.";
            return;
        }
        if (gunnerSide != 0)
            ExitHowitzerGunner();

        Vector3 start;
        if (mode == GuidedWeaponMode.Anubis)
        {
            Vector3 origin = battlePyramid != null ? battlePyramid.position : Vector3.zero;
            GoldenStructure launcher = FindNearestStructure(origin, StructureKind.AnubisStrikeLauncher, float.MaxValue);
            if (launcher == null || launcher.transform == null)
            {
                lastEvent = "Build an Anubis Strike Launcher before opening TV-link.";
                return;
            }
            if (launcher.ammo <= 0)
            {
                lastEvent = "Anubis launcher has no ready missile.";
                return;
            }
            launcher.ammo--;
            launcher.fireTimer = 3f;
            start = launcher.transform.position + Vector3.up * 1.8f;
        }
        else if (mode == GuidedWeaponMode.SunCore)
        {
            if (nuclearTimer > 0f || nuclearMissiles <= 0)
            {
                lastEvent = nuclearMissiles <= 0 ? "No sun-core missiles loaded." : "Sun-core safeties are cycling.";
                return;
            }
            nuclearMissiles--;
            nuclearTimer = 16f;
            start = GetCruiseLaunchPoint(true);
        }
        else
        {
            if (missileTimer > 0f || cruiseMissiles <= 0)
            {
                lastEvent = cruiseMissiles <= 0 ? "No cruise missiles loaded." : "Cruise missile bay is reloading.";
                return;
            }
            cruiseMissiles--;
            missileTimer = 6f;
            start = GetCruiseLaunchPoint(false);
        }

        if (mainCamera != null)
        {
            guidedCameraPositionBefore = mainCamera.transform.position;
            guidedCameraRotationBefore = mainCamera.transform.rotation;
            guidedCameraPoseCached = true;
        }

        guidedWeaponMode = mode;
        guidedSpeed = mode == GuidedWeaponMode.Anubis ? 58f : mode == GuidedWeaponMode.SunCore ? 42f : 52f;
        guidedTurnRate = mode == GuidedWeaponMode.Anubis ? 7.2f : mode == GuidedWeaponMode.SunCore ? 2.4f : 5.5f;
        guidedDamage = mode == GuidedWeaponMode.Anubis ? 180f : mode == GuidedWeaponMode.SunCore ? 420f : 145f;
        guidedBlastRadius = mode == GuidedWeaponMode.Anubis ? 16f : mode == GuidedWeaponMode.SunCore ? 34f : 14f;
        guidedInfiniteFlight = mode != GuidedWeaponMode.PyramidCruise;
        guidedMissileLife = mode == GuidedWeaponMode.PyramidCruise ? 22f : float.PositiveInfinity;
        guidedDistanceTravelled = 0f;
        guidedCriticalMass = 0f;

        GameObject obj = GameObject.CreatePrimitive(mode == GuidedWeaponMode.Anubis ? PrimitiveType.Sphere : PrimitiveType.Capsule);
        obj.name = mode == GuidedWeaponMode.Anubis ? "Anubis_TV_Missile" : mode == GuidedWeaponMode.SunCore ? "Sun_Core_TV_Missile" : "Pyramid_TV_Cruise_Missile";
        obj.transform.position = start;
        obj.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        obj.transform.localScale = mode == GuidedWeaponMode.SunCore ? new Vector3(0.8f, 1.9f, 0.8f) :
                                   mode == GuidedWeaponMode.Anubis ? Vector3.one * 0.42f : new Vector3(0.42f, 1.25f, 0.42f);
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = mode == GuidedWeaponMode.SunCore ? nuclearMaterial : missileMaterial;
        Color trail = mode == GuidedWeaponMode.SunCore ? new Color(1f, 0.18f, 0.04f, 1f) :
                      mode == GuidedWeaponMode.Anubis ? new Color(1f, 0.5f, 0.06f, 1f) : new Color(1f, 0.78f, 0.2f, 1f);
        AttachProjectileTrail(obj, trail, mode == GuidedWeaponMode.SunCore ? 0.46f : 0.22f, mode == GuidedWeaponMode.SunCore ? 1.2f : 0.72f);

        guidedMissileTransform = obj.transform;
        guidedLaunchPos = start;
        Vector3 launchForward = battlePyramid != null ? battlePyramid.forward : Vector3.forward;
        guidedMissileVelocity = Vector3.up * 18f + launchForward * 8f;
        Vector3 dir = guidedMissileVelocity.normalized;
        guidedYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        guidedPitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        guidedPhase = GuidedPhase.Eject;
        guidedPhaseTimer = 0f;
        guidedMissileActive = true;

        CreateWeaponFlash(start, mode == GuidedWeaponMode.SunCore ? 0.85f : 0.5f, trail);
        RegisterWeaponImpulse(mode == GuidedWeaponMode.SunCore ? 0.28f : 0.12f);
        PlaySandRunnerSound(SandRunnerSound.MissileLaunch, start, mode == GuidedWeaponMode.SunCore ? 1.05f : 0.85f);
        lastEvent = GetGuidedModeName() + " launched. TV-link acquiring.";
    }

    private void UpdateGuidedMissile(float dt)
    {
        if (!guidedMissileActive || mainCamera == null)
            return;
        if (guidedMissileTransform == null)
        {
            guidedMissileActive = false;
            RestoreGuidedCamera();
            return;
        }

        if (!guidedInfiniteFlight)
            guidedMissileLife -= dt;
        guidedPhaseTimer += dt;

        if (WasKeyPressedThisFrame(Key.Escape))
        {
            ExitGuidedMissile();
            return;
        }

        if (guidedPhase == GuidedPhase.Eject)
        {
            guidedMissileTransform.position += Vector3.up * 22f * dt;
            guidedMissileVelocity = Vector3.up * 18f;
            Vector3 targetCamPos = guidedLaunchPos + Vector3.up * (guidedPhaseTimer * 11f) + (mainCamera.transform.position - guidedLaunchPos) * 0.4f;
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamPos, dt * 3f);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation,
                Quaternion.LookRotation((guidedMissileTransform.position - mainCamera.transform.position).normalized, Vector3.up), dt * 3f);
            if (guidedPhaseTimer >= GuidedEjectDuration)
            {
                guidedPhase = GuidedPhase.Boost;
                guidedPhaseTimer = 0f;
            }
            return;
        }

        if (guidedPhase == GuidedPhase.Boost)
        {
            float t = Mathf.Clamp01(guidedPhaseTimer / GuidedBoostDuration);
            Vector3 launchForward = battlePyramid != null ? battlePyramid.forward : Vector3.forward;
            Vector3 boostDir = (Vector3.up * (1f - t) + launchForward * t).normalized;
            float speed = Mathf.Lerp(22f, guidedSpeed, t);
            guidedMissileVelocity = Vector3.Lerp(guidedMissileVelocity, boostDir * speed, t * 2.5f * dt);
            guidedMissileTransform.position += guidedMissileVelocity * dt;
            OrientGuidedMissile();
            ApplyGuidedNoseCamera();
            if (guidedPhaseTimer >= GuidedBoostDuration)
            {
                guidedPhase = GuidedPhase.Cruise;
                guidedPhaseTimer = 0f;
                Vector3 dir = guidedMissileVelocity.normalized;
                guidedYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                guidedPitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
                lastEvent = GetGuidedModeName() + ": mouse/WASD steer, LMB/Space strike, Esc abort.";
            }
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float sensitivity = guidedWeaponMode == GuidedWeaponMode.SunCore ? GuidedMouseSensitivity * 0.55f : GuidedMouseSensitivity;
            guidedYaw += delta.x * sensitivity;
            guidedPitch = Mathf.Clamp(guidedPitch + delta.y * sensitivity, -72f, 72f);
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float steerSpeed = (guidedWeaponMode == GuidedWeaponMode.SunCore ? 42f : 76f) * dt;
            if (keyboard.aKey.isPressed) guidedYaw -= steerSpeed;
            if (keyboard.dKey.isPressed) guidedYaw += steerSpeed;
            if (keyboard.wKey.isPressed) guidedPitch -= steerSpeed;
            if (keyboard.sKey.isPressed) guidedPitch += steerSpeed;
        }

        Vector3 desiredDirection = Quaternion.Euler(-guidedPitch, guidedYaw, 0f) * Vector3.forward;
        Vector3 desiredVelocity = desiredDirection.normalized * guidedSpeed;
        guidedMissileVelocity = Vector3.RotateTowards(guidedMissileVelocity, desiredVelocity, guidedTurnRate * dt, guidedSpeed * dt);
        Vector3 previousPosition = guidedMissileTransform.position;
        Vector3 nextPosition = previousPosition + guidedMissileVelocity * dt;
        guidedDistanceTravelled += Vector3.Distance(previousPosition, nextPosition);
        if (guidedWeaponMode == GuidedWeaponMode.SunCore)
            guidedCriticalMass = Mathf.Clamp01(guidedDistanceTravelled / 420f);

        EnemyUnit hitEnemy = FindGuidedMissileHit(previousPosition, nextPosition, guidedWeaponMode == GuidedWeaponMode.SunCore ? 4.8f : 3.2f);
        if (hitEnemy != null && hitEnemy.transform != null)
        {
            FinishGuidedMissile(hitEnemy.transform.position, true);
            return;
        }

        guidedMissileTransform.position = nextPosition;
        OrientGuidedMissile();
        UpdateGuidedStrikeSolution();

        bool manualStrike = (mouse != null && mouse.leftButton.wasPressedThisFrame) || WasKeyPressedThisFrame(Key.Space);
        bool sunCoreArmed = guidedWeaponMode != GuidedWeaponMode.SunCore || guidedCriticalMass >= 0.25f;
        if (manualStrike && guidedStrikeValid && sunCoreArmed)
        {
            FinishGuidedMissile(guidedStrikePoint, true);
            return;
        }
        if (manualStrike && !sunCoreArmed)
            lastEvent = "SUN-CORE critical mass below 25%. Strike interlock active.";

        ApplyGuidedNoseCamera();

        float ground = GetPlayableGroundHeight(guidedMissileTransform.position);
        if (guidedMissileTransform.position.y <= ground + 0.35f)
        {
            FinishGuidedMissile(guidedMissileTransform.position, true);
            return;
        }
        if (!guidedInfiniteFlight && guidedMissileLife <= 0f)
            FinishGuidedMissile(guidedMissileTransform.position, true);
    }

    private void ApplyGuidedNoseCamera()
    {
        if (mainCamera == null || guidedMissileTransform == null || guidedMissileVelocity.sqrMagnitude <= 0.01f)
            return;

        Vector3 flightDirection = guidedMissileVelocity.normalized;
        // Camera is rigidly mounted in the seeker head, not trailing behind the model.
        mainCamera.transform.position = guidedMissileTransform.position + flightDirection * 0.82f + Vector3.up * 0.08f;
        mainCamera.transform.rotation = Quaternion.LookRotation(flightDirection, Vector3.up);
        mainCamera.fieldOfView = guidedWeaponMode == GuidedWeaponMode.SunCore ? 68f : 74f;
    }

    private void OrientGuidedMissile()
    {
        if (guidedMissileTransform != null && guidedMissileVelocity.sqrMagnitude > 0.01f)
            guidedMissileTransform.rotation = Quaternion.LookRotation(guidedMissileVelocity.normalized, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
    }

    private void UpdateGuidedStrikeSolution()
    {
        guidedHighlightedEnemy = null;
        guidedStrikeValid = false;
        guidedStrikePoint = guidedMissileTransform.position + guidedMissileVelocity.normalized * 120f;
        Vector3 origin = guidedMissileTransform.position;
        Vector3 forward = guidedMissileVelocity.normalized;
        float bestAngle = 8f;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || enemy.transform == null || enemy.health <= 0f)
                continue;
            Vector3 toEnemy = enemy.transform.position - origin;
            if (toEnemy.sqrMagnitude > 700f * 700f)
                continue;
            float angle = Vector3.Angle(forward, toEnemy);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                guidedHighlightedEnemy = enemy;
                guidedStrikePoint = enemy.transform.position;
                guidedStrikeValid = true;
            }
        }

        if (!guidedStrikeValid && forward.y < -0.015f)
        {
            float ground = GetPlayableGroundHeight(origin);
            float distance = (ground + 0.2f - origin.y) / forward.y;
            if (distance > 0f && distance < 900f)
            {
                guidedStrikePoint = origin + forward * distance;
                guidedStrikeValid = true;
            }
        }
    }

    private EnemyUnit FindGuidedMissileHit(Vector3 start, Vector3 end, float radius)
    {
        EnemyUnit best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null || enemy.transform == null || enemy.health <= 0f)
                continue;
            float distance = DistancePointToSegment(enemy.transform.position + Vector3.up * 0.8f, start, end);
            if (distance <= radius && distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }
        return best;
    }

    private float DistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f)
            return Vector3.Distance(point, start);
        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * t);
    }

    private void FinishGuidedMissile(Vector3 position, bool detonate)
    {
        if (guidedMissileTransform != null)
            Destroy(guidedMissileTransform.gameObject);
        guidedMissileActive = false;
        guidedMissileTransform = null;
        RestoreGuidedCamera();
        if (detonate)
        {
            float radius = guidedBlastRadius;
            float damage = guidedDamage;
            bool nuclear = guidedWeaponMode == GuidedWeaponMode.SunCore;
            if (nuclear)
            {
                radius = Mathf.Lerp(24f, 52f, guidedCriticalMass);
                damage = Mathf.Lerp(280f, 620f, guidedCriticalMass);
            }
            CreateExplosion(position, radius, damage, nuclear);
        }
    }

    private void RestoreGuidedCamera()
    {
        if (guidedCameraPoseCached && mainCamera != null)
        {
            mainCamera.transform.position = guidedCameraPositionBefore;
            mainCamera.transform.rotation = guidedCameraRotationBefore;
            cameraFollowInitialized = false;
        }
        guidedCameraPoseCached = false;
    }

    private void ExitGuidedMissile()
    {
        if (!guidedMissileActive)
            return;
        if (guidedMissileTransform != null)
            Destroy(guidedMissileTransform.gameObject);
        guidedMissileActive = false;
        guidedMissileTransform = null;
        RestoreGuidedCamera();
        lastEvent = GetGuidedModeName() + " TV-link terminated.";
    }

    private string GetGuidedModeName()
    {
        return guidedWeaponMode == GuidedWeaponMode.Anubis ? "ANUBIS JACKAL-LINK" :
               guidedWeaponMode == GuidedWeaponMode.SunCore ? "SUN-CORE HORUS-LINK" : "PYRAMID SAPPHIRE-LINK";
    }

    private void DrawGuidedMissileOverlay()
    {
        if (!guidedMissileActive || guidedPhase < GuidedPhase.Cruise)
            return;

        Rect fs = new Rect(0, 0, Screen.width, Screen.height);
        Color tint = guidedWeaponMode == GuidedWeaponMode.SunCore ? new Color(0.18f, 0.015f, 0.005f, 0.68f) :
                     guidedWeaponMode == GuidedWeaponMode.Anubis ? new Color(0.08f, 0.035f, 0.005f, 0.65f) : GuidedIrTint;
        GUI.DrawTexture(fs, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, tint, 0f, 0f);

        float pulse = Mathf.Sin(Time.unscaledTime * 7f) * 3f;
        float cx = Screen.width * 0.5f + pulse;
        float cy = Screen.height * 0.5f + Mathf.Cos(Time.unscaledTime * 5f) * 2f;
        float size = guidedWeaponMode == GuidedWeaponMode.SunCore ? 24f : 15f;
        Color reticle = guidedStrikeValid ? new Color(0.2f, 1f, 0.4f, 0.95f) : new Color(1f, 0.7f, 0.18f, 0.9f);
        GUI.color = reticle;
        GUI.DrawTexture(new Rect(cx - 1f, cy - size - 7f, 2f, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy + 7f, 2f, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - size - 7f, cy - 1f, size, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + 7f, cy - 1f, size, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 4f, cy - 4f, 8f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float inset = Mathf.Min(Screen.width, Screen.height) * 0.075f;
        GUIStyle hs = new GUIStyle(GUI.skin.label);
        hs.fontSize = 12;
        hs.fontStyle = FontStyle.Bold;
        hs.normal.textColor = reticle;
        string endurance = guidedInfiniteFlight ? "LINK ∞" : "FUEL " + Mathf.Max(0f, guidedMissileLife).ToString("F1");
        string target = guidedHighlightedEnemy != null ? "LOCK: " + guidedHighlightedEnemy.transform.name : guidedStrikeValid ? "GROUND SOLUTION" : "NO STRIKE SOLUTION";
        string special = guidedWeaponMode == GuidedWeaponMode.SunCore ? "\nCRITICAL MASS " + Mathf.RoundToInt(guidedCriticalMass * 100f) + "%\nBLAST " + Mathf.RoundToInt(Mathf.Lerp(24f, 52f, guidedCriticalMass)) : "";
        string hud = GetGuidedModeName() + "\n" + endurance + "  SPD " + Mathf.RoundToInt(guidedMissileVelocity.magnitude) +
                     "\n" + target + special + "\nMouse/WASD steer  LMB/Space STRIKE  Esc abort";
        GUI.Label(new Rect(inset, inset, 360f, 130f), hud, hs);
    }
}