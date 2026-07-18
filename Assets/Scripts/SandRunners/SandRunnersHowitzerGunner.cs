using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private int gunnerSide;
    private int gunnerWeapon;
    private float gunnerPitch;
    private float gunnerYaw;
    private float gunnerFireTimer;
    private bool gunnerCrosshairVisible;
    private Transform gunnerCameraAnchor;
    private Transform gunnerBarrelRoot;
    private Quaternion gunnerBaseLocalRotation;
    private Vector3 gunnerCameraPositionBefore;
    private Quaternion gunnerCameraRotationBefore;
    private float gunnerCameraFovBefore;
    private float gunnerNearClipBefore;
    private bool gunnerCameraPoseCached;
    private const float GunnerMaxPitch = 42f;
    private const float GunnerMinPitch = -8f;
    private const float GunnerMaxYaw = 64f;
    private const float GunnerMouseSensitivity = 0.11f;
    private const float GunnerReloadTime = 3.2f;
    private const float GunnerShellCost = 18f;
    private const float GunnerMuzzleVelocity = 58f;
    private const float GunnerGravity = 12f;
    private const float GunnerCameraBackstep = 1.7f;
    private const float GunnerCameraRadius = 0.24f;

    private void ToggleHowitzerGunner(int side)
    {
        if (gunnerSide == side)
        {
            int nextWeapon = gunnerWeapon + 1;
            int idx = GetMuzzleIndex(side, nextWeapon);
            if (idx >= 0 && idx < howitzerMuzzles.Count)
            {
                gunnerWeapon = nextWeapon;
                EnterGunnerView();
                return;
            }
            ExitHowitzerGunner();
            return;
        }

        if (howitzerMuzzles == null || howitzerMuzzles.Count < 4)
        {
            lastEvent = "Howitzer systems not calibrated.";
            return;
        }

        gunnerSide = side;
        gunnerWeapon = 0;
        EnterGunnerView();
    }

    private int GetMuzzleIndex(int side, int weapon)
    {
        return (side < 0 ? 0 : 2) + weapon;
    }

    private void EnterGunnerView()
    {
        if (gunnerBarrelRoot != null)
            gunnerBarrelRoot.localRotation = gunnerBaseLocalRotation;

        int idx = GetMuzzleIndex(gunnerSide, gunnerWeapon);
        if (idx < 0 || idx >= howitzerMuzzles.Count)
        {
            ExitHowitzerGunner();
            return;
        }

        gunnerCameraAnchor = howitzerMuzzles[idx];
        gunnerBarrelRoot = gunnerCameraAnchor.parent;
        gunnerBaseLocalRotation = gunnerBarrelRoot.localRotation;
        gunnerPitch = 0f;
        gunnerYaw = 0f;
        gunnerFireTimer = 0f;
        gunnerCrosshairVisible = true;

        lastEvent = (gunnerSide < 0 ? "Left" : "Right") + " front battery / 300-mm gun " + (gunnerWeapon + 1) +
                    ". Mouse or WASD aim, LMB fire, Q/E cycle batteries, Esc exit.";

        if (mainCamera != null)
        {
            if (!gunnerCameraPoseCached)
            {
                gunnerCameraPositionBefore = mainCamera.transform.position;
                gunnerCameraRotationBefore = mainCamera.transform.rotation;
                gunnerCameraFovBefore = mainCamera.fieldOfView;
                gunnerNearClipBefore = mainCamera.nearClipPlane;
                gunnerCameraPoseCached = true;
            }
            mainCamera.fieldOfView = 64f;
            SnapCameraToBarrel();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ExitHowitzerGunner()
    {
        if (gunnerSide == 0)
            return;

        if (gunnerBarrelRoot != null)
            gunnerBarrelRoot.localRotation = gunnerBaseLocalRotation;

        if (gunnerCameraPoseCached && mainCamera != null)
        {
            mainCamera.transform.position = gunnerCameraPositionBefore;
            mainCamera.transform.rotation = gunnerCameraRotationBefore;
            mainCamera.fieldOfView = gunnerCameraFovBefore;
            mainCamera.nearClipPlane = gunnerNearClipBefore;
        }

        gunnerSide = 0;
        gunnerWeapon = 0;
        gunnerCrosshairVisible = false;
        gunnerCameraAnchor = null;
        gunnerBarrelRoot = null;
        gunnerCameraPoseCached = false;
        lastEvent = "Gunner view disengaged.";

        if (mainCamera != null && !commandCursorMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateHowitzerGunner(float dt)
    {
        if (gunnerSide == 0 || gunnerCameraAnchor == null || gunnerBarrelRoot == null)
            return;

        gunnerFireTimer -= dt;
        UpdateGunnerMouseAim(dt);
        RotateBarrel();
        SnapCameraToBarrel();

        if (WasKeyPressedThisFrame(Key.Escape))
        {
            ExitHowitzerGunner();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && gunnerFireTimer <= 0f)
            FireGunnerHowitzer();
    }

    private void UpdateGunnerMouseAim(float dt)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 delta = mouse.delta.ReadValue() * GunnerMouseSensitivity;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        gunnerYaw += delta.x;
        gunnerPitch += delta.y;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float keyboardAim = 42f * dt;
            if (keyboard.aKey.isPressed) gunnerYaw -= keyboardAim;
            if (keyboard.dKey.isPressed) gunnerYaw += keyboardAim;
            if (keyboard.wKey.isPressed) gunnerPitch += keyboardAim;
            if (keyboard.sKey.isPressed) gunnerPitch -= keyboardAim;
        }
        gunnerPitch = Mathf.Clamp(gunnerPitch, GunnerMinPitch, GunnerMaxPitch);
        gunnerYaw = Mathf.Clamp(gunnerYaw, -GunnerMaxYaw, GunnerMaxYaw);
    }

    private Vector3 GetGunnerAimDir()
    {
        if (gunnerCameraAnchor == null || gunnerBarrelRoot == null)
            return Vector3.forward;
        return (gunnerCameraAnchor.position - gunnerBarrelRoot.position).normalized;
    }

    private void RotateBarrel()
    {
        if (gunnerBarrelRoot == null)
            return;

        Quaternion aimRotation = Quaternion.Euler(-gunnerPitch, gunnerYaw, 0f);
        gunnerBarrelRoot.localRotation = gunnerBaseLocalRotation * aimRotation;
    }

    private void SnapCameraToBarrel()
    {
        if (mainCamera == null || gunnerBarrelRoot == null)
            return;

        Vector3 aimDir = GetGunnerAimDir();
        Vector3 eye = gunnerCameraAnchor.position - aimDir * 0.34f;
        eye += gunnerBarrelRoot.up * 0.42f;
        eye += gunnerBarrelRoot.right * (gunnerSide < 0 ? 0.18f : -0.18f);
        Vector3 camPos = gunnerCameraAnchor.position - aimDir * GunnerCameraBackstep;
        camPos += gunnerBarrelRoot.up * 0.42f;
        camPos += gunnerBarrelRoot.right * (gunnerSide < 0 ? 0.18f : -0.18f);

        Vector3 cameraDelta = camPos - eye;
        float cameraDistance = cameraDelta.magnitude;
        if (cameraDistance > 0.01f && Physics.SphereCast(eye, GunnerCameraRadius, cameraDelta.normalized, out RaycastHit cameraHit, cameraDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            camPos = cameraHit.point + cameraHit.normal * (GunnerCameraRadius + 0.06f);

        if (duneWorldBuilt)
            camPos.y = Mathf.Max(camPos.y, GetPlayableGroundHeight(camPos) + 0.48f);
        mainCamera.transform.position = camPos;
        mainCamera.transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
        mainCamera.nearClipPlane = 0.035f;
    }

    private void FireGunnerHowitzer()
    {
        if (howitzerTimer > 0f)
        {
            lastEvent = "300-mm howitzers reloading.";
            return;
        }

        if (sand < GunnerShellCost)
        {
            lastEvent = "Need " + GunnerShellCost + " sand propellant.";
            return;
        }

        sand -= GunnerShellCost;
        howitzerTimer = GunnerReloadTime;
        gunnerFireTimer = GunnerReloadTime;

        int idx = GetMuzzleIndex(gunnerSide, gunnerWeapon);
        if (idx < 0 || idx >= howitzerMuzzles.Count)
            return;

        Transform muzzleT = howitzerMuzzles[idx];
        Vector3 muzzle = muzzleT.position;
        Vector3 aimDir = GetGunnerAimDir();
        Vector3 velocity = aimDir * GunnerMuzzleVelocity + pyramidVelocity * 0.15f;

        Vector3 spread = new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.025f, 0.025f), Random.Range(-0.04f, 0.04f));
        CreateProjectile("300mm_Gunner_Shell", muzzle + spread, velocity + spread * 0.3f, shellMaterial, new Vector3(0.35f, 0.7f, 0.35f), 72f, 8f, false);
        CreateWeaponFlash(muzzle, 0.5f, new Color(1f, 0.46f, 0.14f, 1f));

        Vector3 impactPredict = muzzle + aimDir * 140f;
        impactPredict.y = Mathf.Max(impactPredict.y, 0.5f);
        CreateReadableImpactWarning(impactPredict, 8f, new Color(1f, 0.72f, 0.18f, 1f), 1.2f, "300-MM SHOT", false);

        RegisterFrontHowitzerRecoil(idx);
        RegisterWeaponImpulse(0.22f);
        PlaySandRunnerSound(SandRunnerSound.ArtilleryFire, muzzle, 1.2f);
        ArmHowitzerReloadSound();
        lastEvent = (gunnerSide < 0 ? "Left" : "Right") + " front battery gun " + (gunnerWeapon + 1) + " fired.";
    }

    private void DrawGunnerCrosshair()
    {
        if (gunnerSide == 0 || !gunnerCrosshairVisible || gunnerCameraAnchor == null)
            return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float size = 14f;
        float gap = 6f;
        float aimDist = 140f;
        Color col = gunnerFireTimer > 0f ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(1f, 0.15f, 0.05f, 0.85f);

        GUI.color = col;
        GUI.DrawTexture(new Rect(cx - 1f, cy - size - gap, 2f, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy + gap, 2f, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - size - gap, cy - 1f, size, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + gap, cy - 1f, size, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 4f, cy - 4f, 8f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string label = "ELEV " + Mathf.RoundToInt(gunnerPitch) + "\u00b0";
        if (gunnerFireTimer > 0f)
            label = "REL " + gunnerFireTimer.ToString("F1") + "s";
        GUIStyle gs = new GUIStyle(GUI.skin.label);
        gs.fontSize = 11;
        gs.normal.textColor = new Color(0.9f, 0.9f, 0.75f, 0.9f);
        GUI.Label(new Rect(cx + 16f, cy - 8f, 120f, 20f), label, gs);

        Vector3 aimDir = GetGunnerAimDir();
        Vector3 muzzlePos = gunnerCameraAnchor.position;
        Vector3 predicted = muzzlePos + aimDir * aimDist;
        predicted.y = Mathf.Max(predicted.y, 0.3f);
        float ballisticDrop = 0.5f * GunnerGravity * Mathf.Pow(aimDist / GunnerMuzzleVelocity, 2f);
        predicted.y -= ballisticDrop;
        EnemyUnit target = FindNearestEnemyInDirection(muzzlePos, aimDir, aimDist);
        string targetLabel = target != null && target.transform != null
            ? "TARGET: " + target.transform.name.Replace('_', ' ')
            : "MANUAL AIM / " + Mathf.RoundToInt(aimDist) + "m";
        GUI.Label(new Rect(cx - 110f, cy + 26f, 220f, 20f), targetLabel, gs);

        string sideLabel = (gunnerSide < 0 ? "LEFT FRONT" : "RIGHT FRONT") + " 300-MM / GUN " + (gunnerWeapon + 1);
        GUIStyle ts = new GUIStyle(GUI.skin.label);
        ts.fontSize = 14;
        ts.fontStyle = FontStyle.Bold;
        ts.normal.textColor = new Color(1f, 0.75f, 0.2f, 0.9f);
        GUI.Label(new Rect(cx - 120f, cy - 44f, 240f, 20f), sideLabel, ts);
    }
}