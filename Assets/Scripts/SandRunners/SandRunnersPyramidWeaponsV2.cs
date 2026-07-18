using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class SandRunnersPrototype
{
    private readonly List<Transform> frontHowitzerAimRoots = new List<Transform>();
    private readonly List<Transform> frontHowitzerBarrels = new List<Transform>();
    private readonly List<Transform> frontHowitzerMuzzles = new List<Transform>();
    private readonly List<float> frontHowitzerRecoil = new List<float>();
    private readonly List<Transform> sideAutoCannonAimRoots = new List<Transform>();
    private readonly List<Transform> sideAutoCannonMuzzles = new List<Transform>();
    private readonly List<Transform> apexMirrorPivots = new List<Transform>();
    private readonly List<Quaternion> apexMirrorRestRotations = new List<Quaternion>();

    private Transform pyramidWeaponRigV2;
    private Transform apexFocusPoint;
    private Transform apexEmitter;
    private Light apexFocusLight;
    private AudioSource apexChargeSource;
    private AudioSource apexShotSource;
    private AudioClip apexDischargeClip;
    private EnemyUnit apexLockedEnemy;
    private Vector3 apexAimPoint;
    private float apexTargetDistance;
    private bool apexTargetingMode;
    private bool apexCommandModeBefore;
    private float apexTargetYaw;
    private float apexTargetPitch;
    private float apexTargetFov;
    private float apexCameraFovBefore;
    private float apexCameraNearBefore;
    private LineRenderer apexRechargeBeam;
    private LineRenderer apexRechargeCoreBeam;
    private AudioSource howitzerMechanicalSource;
    private AudioClip howitzerReloadClip;
    private float howitzerReloadSoundTimer;
    private bool howitzerReloadSoundArmed;
    private Vector3 apexCameraStartPosition;
    private Quaternion apexCameraStartRotation;
    private float apexCameraBlend;
    private readonly List<Renderer> apexViewOccluders = new List<Renderer>();
    private readonly List<bool> apexViewOccluderStates = new List<bool>();

    private void InitializePyramidWeaponsV2()
    {
        EnsurePyramidWeaponsV2();
    }

    private void EnsurePyramidWeaponsV2()
    {
        if (battlePyramid == null || pyramidWeaponRigV2 != null)
            return;

        if (authoredBattlePyramidInstance == null)
        {
            Transform existingArt = battlePyramid.Find("SR_Authored_Battle_Pyramid");
            if (existingArt != null)
                authoredBattlePyramidInstance = existingArt;
        }

        // Weapon coordinates must stay in the moving pyramid's canonical space.
        // Authored art can have its own scale/pivot and previously displaced the Apex focus.
        Transform parent = battlePyramid;
        Transform existing = parent.Find("SR_Authored_Pyramid_Weapon_Rig_V2");
        if (existing != null)
        {
            pyramidWeaponRigV2 = existing;
            CachePyramidWeaponsV2();
            return;
        }

        GameObject rigObject = new GameObject("SR_Authored_Pyramid_Weapon_Rig_V2");
        pyramidWeaponRigV2 = rigObject.transform;
        pyramidWeaponRigV2.SetParent(parent, false);
        pyramidWeaponRigV2.localPosition = Vector3.zero;
        pyramidWeaponRigV2.localRotation = Quaternion.identity;
        pyramidWeaponRigV2.localScale = Vector3.one;

        Material gold = Resources.Load<Material>("SandRunners/Models/ArtPass/SR_Art_PaleGold");
        Material dark = Resources.Load<Material>("SandRunners/Models/ArtPass/SR_Art_Gunmetal");
        Material glow = Resources.Load<Material>("SandRunners/Models/ArtPass/SR_Art_CyanGlass");
        if (gold == null) gold = pyramidGoldMaterial;
        if (dark == null) dark = pyramidDarkArmorMaterial;
        if (glow == null) glow = pyramidGlowMaterial;

        Material focusGlow = pyramidMuzzleFlashMaterial != null ? pyramidMuzzleFlashMaterial : glow;
        DisableLegacyApexColumn();
        BuildApexMirrorFocus(gold, dark, focusGlow);
        BuildFrontHowitzerBattery(gold, dark);
        BuildSideAutoCannonBattery(gold, dark);

        beamMuzzle = apexEmitter;
        ReplaceAutocannonMuzzlesWithSideBattery();
        hangarSystemsCached = false;
    }

    private void BuildApexMirrorFocus(Material gold, Material dark, Material glow)
    {
        Transform apexRoot = new GameObject("Apex_Mirror_Focus_Rig").transform;
        apexRoot.SetParent(pyramidWeaponRigV2, false);
        // The procedural hull peaks at 2.53 local metres. The mirror crown sits
        // directly on that cap instead of floating above it.
        apexRoot.localPosition = new Vector3(0f, 2.62f, 0f);

        CreateWeaponRigPrimitive(PrimitiveType.Cylinder, "Apex_Inset_Base", apexRoot,
            new Vector3(0f, -0.13f, 0f), Quaternion.identity, new Vector3(0.42f, 0.055f, 0.42f), dark);

        Vector3[] pivotPositions =
        {
            new Vector3(-0.31f, 0f, 0f),
            new Vector3(0.31f, 0f, 0f),
            new Vector3(0f, 0f, 0.31f),
            new Vector3(0f, 0f, -0.31f)
        };
        Quaternion[] restRotations =
        {
            Quaternion.Euler(0f, 0f, -31f),
            Quaternion.Euler(0f, 0f, 31f),
            Quaternion.Euler(31f, 0f, 0f),
            Quaternion.Euler(-31f, 0f, 0f)
        };
        Vector3[] panelScales =
        {
            new Vector3(0.48f, 0.045f, 0.62f),
            new Vector3(0.48f, 0.045f, 0.62f),
            new Vector3(0.62f, 0.045f, 0.48f),
            new Vector3(0.62f, 0.045f, 0.48f)
        };

        for (int i = 0; i < 4; i++)
        {
            Transform pivot = new GameObject("Apex_Mirror_Pivot_" + i).transform;
            pivot.SetParent(apexRoot, false);
            pivot.localPosition = pivotPositions[i];
            pivot.localRotation = restRotations[i];
            apexMirrorPivots.Add(pivot);
            apexMirrorRestRotations.Add(restRotations[i]);
            CreateWeaponRigPrimitive(PrimitiveType.Cube, "Apex_Mirror_Facet_" + i, pivot,
                Vector3.zero, Quaternion.identity, panelScales[i], gold);
        }

        apexFocusPoint = CreateWeaponRigPrimitive(PrimitiveType.Sphere, "Apex_Focused_Sun_Point", apexRoot,
            new Vector3(0f, 0.08f, 0f), Quaternion.identity, Vector3.one * 0.18f, glow);
        apexEmitter = new GameObject("beam_cannon_muzzle_v2").transform;
        apexEmitter.SetParent(apexFocusPoint, false);
        apexEmitter.localPosition = Vector3.zero;
        apexEmitter.localRotation = Quaternion.identity;

        GameObject lightObject = new GameObject("Apex_Focus_Light_V2");
        lightObject.transform.SetParent(apexFocusPoint, false);
        apexFocusLight = lightObject.AddComponent<Light>();
        apexFocusLight.type = LightType.Point;
        apexFocusLight.color = new Color(1f, 0.86f, 0.28f, 1f);
        apexFocusLight.range = 8f;
        apexFocusLight.intensity = 0.65f;
        apexFocusLight.shadows = LightShadows.None;

        apexChargeSource = apexRoot.gameObject.AddComponent<AudioSource>();
        apexChargeSource.playOnAwake = false;
        apexChargeSource.loop = true;
        apexChargeSource.spatialBlend = 1f;
        apexChargeSource.minDistance = 10f;
        apexChargeSource.maxDistance = 260f;
        apexChargeSource.dopplerLevel = 0f;
        apexChargeSource.clip = CreateApexSolarChargeClip();

        apexShotSource = apexRoot.gameObject.AddComponent<AudioSource>();
        apexShotSource.playOnAwake = false;
        apexShotSource.spatialBlend = 1f;
        apexShotSource.minDistance = 18f;
        apexShotSource.maxDistance = 720f;
        apexShotSource.dopplerLevel = 0f;
        apexDischargeClip = CreateApexSolarDischargeClip();

        Shader sunBeamShader = Shader.Find("Sprites/Default");
        Material sunBeamMaterial = sunBeamShader != null ? new Material(sunBeamShader) : glow;
        if (sunBeamMaterial != null)
        {
            sunBeamMaterial.name = "SR_Apex_Sunlight_Volume";
            sunBeamMaterial.color = Color.white;
        }

        GameObject rechargeObject = new GameObject("Apex_Descending_Sunlight");
        rechargeObject.transform.SetParent(apexRoot, false);
        apexRechargeBeam = rechargeObject.AddComponent<LineRenderer>();
        apexRechargeBeam.positionCount = 2;
        apexRechargeBeam.useWorldSpace = true;
        apexRechargeBeam.sharedMaterial = sunBeamMaterial;
        apexRechargeBeam.startWidth = 4.2f;
        apexRechargeBeam.endWidth = 0.24f;
        apexRechargeBeam.numCapVertices = 8;
        apexRechargeBeam.enabled = false;

        GameObject coreObject = new GameObject("Apex_Descending_Sunlight_Core");
        coreObject.transform.SetParent(apexRoot, false);
        apexRechargeCoreBeam = coreObject.AddComponent<LineRenderer>();
        apexRechargeCoreBeam.positionCount = 2;
        apexRechargeCoreBeam.useWorldSpace = true;
        apexRechargeCoreBeam.sharedMaterial = sunBeamMaterial;
        apexRechargeCoreBeam.startWidth = 1.25f;
        apexRechargeCoreBeam.endWidth = 0.075f;
        apexRechargeCoreBeam.numCapVertices = 8;
        apexRechargeCoreBeam.enabled = false;

        howitzerMechanicalSource = pyramidWeaponRigV2.gameObject.AddComponent<AudioSource>();
        howitzerMechanicalSource.playOnAwake = false;
        howitzerMechanicalSource.spatialBlend = 1f;
        howitzerMechanicalSource.minDistance = 12f;
        howitzerMechanicalSource.maxDistance = 360f;
        howitzerMechanicalSource.dopplerLevel = 0f;
        howitzerReloadClip = CreateHowitzerReloadClip();
    }

    private void BuildFrontHowitzerBattery(Material gold, Material dark)
    {
        Vector3[] positions =
        {
            new Vector3(-3.18f, 2.12f, 1.45f),
            new Vector3(-2.00f, 3.02f, 0.72f),
            new Vector3(2.00f, 3.02f, 0.72f),
            new Vector3(3.18f, 2.12f, 1.45f)
        };
        float[] barrelLengths = { 2.35f, 1.92f, 1.92f, 2.35f };
        float[] barrelRadii = { 0.22f, 0.13f, 0.13f, 0.22f };

        for (int i = 0; i < positions.Length; i++)
        {
            Transform aimRoot = new GameObject("300mm_front_aim_" + i.ToString("00")).transform;
            aimRoot.SetParent(pyramidWeaponRigV2, false);
            aimRoot.localPosition = positions[i];
            aimRoot.localRotation = Quaternion.identity;

            CreateWeaponRigPrimitive(PrimitiveType.Cylinder, "300mm_front_mount_" + i.ToString("00"), aimRoot,
                Vector3.zero, Quaternion.identity,
                new Vector3(barrelRadii[i] * 2.8f, 0.18f, barrelRadii[i] * 2.8f), dark);

            Transform barrel = CreateWeaponRigPrimitive(PrimitiveType.Cylinder, "300mm_front_barrel_" + i.ToString("00"), aimRoot,
                new Vector3(0f, 0.16f, barrelLengths[i] * 0.5f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(barrelRadii[i], barrelLengths[i] * 0.5f, barrelRadii[i]), gold);

            Transform muzzle = new GameObject("300mm_front_muzzle_" + i.ToString("00")).transform;
            muzzle.SetParent(aimRoot, false);
            muzzle.localPosition = new Vector3(0f, 0.16f, barrelLengths[i]);
            muzzle.localRotation = Quaternion.identity;

            frontHowitzerAimRoots.Add(aimRoot);
            frontHowitzerBarrels.Add(barrel);
            frontHowitzerMuzzles.Add(muzzle);
            frontHowitzerRecoil.Add(0f);
        }
    }

    private void DisableLegacyApexColumn()
    {
        if (battlePyramid == null)
            return;
        Transform[] children = battlePyramid.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string n = children[i].name;
            if (n == "Apex_Charged_Beam_Cannon" ||
                n == "Apex_Beam_Capacitor_Ring_Glow" ||
                n == "Beam_Cannon_Muzzle")
                children[i].gameObject.SetActive(false);
        }
    }

    private void BuildSideAutoCannonBattery(Material gold, Material dark)
    {
        sideAutoCannonAimRoots.Clear();
        sideAutoCannonMuzzles.Clear();
        for (int side = -1; side <= 1; side += 2)
        {
            for (int station = 0; station < 3; station++)
            {
                int index = side < 0 ? station : station + 3;
                Transform aimRoot = new GameObject("Side_30mm_Aim_" + index.ToString("00")).transform;
                aimRoot.SetParent(pyramidWeaponRigV2, false);
                aimRoot.localPosition = new Vector3(side * 2.44f, 0.78f + station * 0.1f,
                    -1.42f + station * 1.36f);
                aimRoot.localRotation = Quaternion.identity;

                CreateWeaponRigPrimitive(PrimitiveType.Cube, "Side_30mm_Sponson_" + index.ToString("00"),
                    aimRoot, Vector3.zero, Quaternion.identity, new Vector3(0.34f, 0.24f, 0.5f), gold);
                CreateWeaponRigPrimitive(PrimitiveType.Cylinder, "Side_30mm_Barrel_" + index.ToString("00"),
                    aimRoot, new Vector3(0f, 0.04f, 0.42f), Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(0.065f, 0.42f, 0.065f), dark);

                Transform muzzle = new GameObject("Side_30mm_Muzzle_" + index.ToString("00")).transform;
                muzzle.SetParent(aimRoot, false);
                muzzle.localPosition = new Vector3(0f, 0.04f, 0.86f);
                sideAutoCannonAimRoots.Add(aimRoot);
                sideAutoCannonMuzzles.Add(muzzle);
            }
        }
    }

    private void ReplaceAutocannonMuzzlesWithSideBattery()
    {
        if (sideAutoCannonMuzzles.Count == 0)
            return;
        autoCannonMuzzles.Clear();
        autoCannonMuzzles.AddRange(sideAutoCannonMuzzles);
    }

    private void UpdateSideAutoCannonAnimation(float dt)
    {
        if (sideAutoCannonAimRoots.Count == 0 || battlePyramid == null)
            return;
        EnemyUnit target = FindNearestEnemy(battlePyramid.position, Mathf.Max(62f, pyramidWeaponRange));
        Vector3 fallback = battlePyramid.position + battlePyramid.forward * 90f + Vector3.up * 1.2f;
        Vector3 targetPoint = target != null && target.transform != null
            ? target.transform.position + Vector3.up * 0.8f
            : fallback;

        for (int i = 0; i < sideAutoCannonAimRoots.Count; i++)
        {
            Transform root = sideAutoCannonAimRoots[i];
            if (root == null || root.parent == null)
                continue;
            Vector3 localDirection = root.parent.InverseTransformDirection((targetPoint - root.position).normalized);
            Quaternion desired = Quaternion.LookRotation(localDirection, Vector3.up);
            root.localRotation = Quaternion.Slerp(root.localRotation, desired, dt * 7.5f);
        }
    }

    private Transform CreateWeaponRigPrimitive(PrimitiveType type, string objectName, Transform parent,
        Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localRotation = localRotation;
        primitive.transform.localScale = localScale;
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        return primitive.transform;
    }

    private void CachePyramidWeaponsV2()
    {
        frontHowitzerAimRoots.Clear();
        frontHowitzerBarrels.Clear();
        frontHowitzerMuzzles.Clear();
        frontHowitzerRecoil.Clear();
        sideAutoCannonAimRoots.Clear();
        sideAutoCannonMuzzles.Clear();
        apexMirrorPivots.Clear();
        apexMirrorRestRotations.Clear();

        Transform[] all = pyramidWeaponRigV2.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform child = all[i];
            if (child.name.StartsWith("300mm_front_aim_"))
            {
                frontHowitzerAimRoots.Add(child);
                frontHowitzerRecoil.Add(0f);
            }
            else if (child.name.StartsWith("300mm_front_barrel_"))
                frontHowitzerBarrels.Add(child);
            else if (child.name.StartsWith("300mm_front_muzzle_"))
                frontHowitzerMuzzles.Add(child);
            else if (child.name.StartsWith("Side_30mm_Aim_"))
                sideAutoCannonAimRoots.Add(child);
            else if (child.name.StartsWith("Side_30mm_Muzzle_"))
                sideAutoCannonMuzzles.Add(child);
            else if (child.name.StartsWith("Apex_Mirror_Pivot_"))
            {
                apexMirrorPivots.Add(child);
                apexMirrorRestRotations.Add(child.localRotation);
            }
            else if (child.name == "Apex_Focused_Sun_Point")
                apexFocusPoint = child;
            else if (child.name == "beam_cannon_muzzle_v2")
                apexEmitter = child;
        }
        frontHowitzerAimRoots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        frontHowitzerBarrels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        frontHowitzerMuzzles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        sideAutoCannonAimRoots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        sideAutoCannonMuzzles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        apexMirrorPivots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        ReplaceAutocannonMuzzlesWithSideBattery();
    }

    private void ReplaceHowitzerMuzzlesWithFrontBattery()
    {
        if (frontHowitzerMuzzles.Count != 4)
            return;
        howitzerMuzzles.Clear();
        howitzerMuzzles.AddRange(frontHowitzerMuzzles);
        beamMuzzle = apexEmitter;
    }

    private void UpdatePyramidWeaponsV2(float dt)
    {
        EnsurePyramidWeaponsV2();
        if (pyramidWeaponRigV2 == null)
            return;

        UpdateApexManualAim(dt);
        UpdateApexTargeting();
        UpdateApexFocusAnimation(dt);
        UpdateFrontBatteryAnimation(dt);
        UpdateSideAutoCannonAnimation(dt);
        UpdateHowitzerReloadAudio(dt);
    }

    private void UpdateApexTargeting()
    {
        if (mainCamera == null || apexEmitter == null)
            return;

        Ray aimRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        EnemyUnit best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit candidate = enemies[i];
            if (candidate == null || candidate.transform == null || candidate.health <= 0f)
                continue;
            Vector3 targetPoint = candidate.transform.position + Vector3.up * 1.25f;
            Vector3 toTarget = targetPoint - aimRay.origin;
            float along = Vector3.Dot(toTarget, aimRay.direction);
            if (along < 4f || along > 280f)
                continue;
            float offAxis = Vector3.Cross(aimRay.direction, toTarget).magnitude;
            float allowed = Mathf.Lerp(2.2f, 12f, along / 280f);
            if (offAxis > allowed)
                continue;
            float score = offAxis * 5f + along * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        apexLockedEnemy = best;
        if (best != null && best.transform != null)
        {
            apexAimPoint = best.transform.position + Vector3.up * 1.25f;
        }
        else
        {
            apexAimPoint = aimRay.origin + aimRay.direction * 180f;
            RaycastHit[] hits = Physics.RaycastAll(aimRay, 280f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null || (battlePyramid != null && hitTransform.IsChildOf(battlePyramid)))
                    continue;
                apexAimPoint = hits[i].point;
                break;
            }
        }

        Vector3 fromApex = apexAimPoint - apexEmitter.position;
        apexTargetDistance = fromApex.magnitude;
        if (fromApex.sqrMagnitude > 0.01f)
            apexEmitter.rotation = Quaternion.LookRotation(fromApex.normalized, Vector3.up);
    }

    private void UpdateApexFocusAnimation(float dt)
    {
        float charge = Mathf.Clamp01(beamCharge);
        float focusAngle = Mathf.Lerp(0f, 16f, charge);
        for (int i = 0; i < apexMirrorPivots.Count && i < apexMirrorRestRotations.Count; i++)
        {
            float sign = i == 0 || i == 3 ? 1f : -1f;
            Quaternion target = apexMirrorRestRotations[i] * Quaternion.Euler(sign * focusAngle, 0f, sign * focusAngle * 0.35f);
            apexMirrorPivots[i].localRotation = Quaternion.Slerp(apexMirrorPivots[i].localRotation, target, dt * 8f);
        }

        if (apexFocusPoint != null)
        {
            float pulse = 0.25f + charge * 0.28f +
                          Mathf.Sin(Time.unscaledTime * (5f + charge * 14f)) * (0.015f + charge * 0.035f);
            apexFocusPoint.localScale = Vector3.one * Mathf.Max(0.2f, pulse);
        }
        if (apexFocusLight != null)
        {
            apexFocusLight.intensity = Mathf.Lerp(0.65f, 5.5f, charge);
            apexFocusLight.range = Mathf.Lerp(8f, 19f, charge);
        }

        bool charging = charge > 0.01f && beamCooldownTimer <= 0f;
        if (apexRechargeBeam != null)
        {
            bool recharging = beamCooldownTimer > 0f && !apexTargetingMode;
            apexRechargeBeam.enabled = recharging;
            if (apexRechargeCoreBeam != null)
                apexRechargeCoreBeam.enabled = recharging;
            if (recharging && apexFocusPoint != null)
            {
                float recharge01 = 1f - Mathf.Clamp01(beamCooldownTimer / 4.5f);
                Vector3 end = apexFocusPoint.position;
                Vector3 start = end + Vector3.up * 96f;
                apexRechargeBeam.SetPosition(0, start);
                apexRechargeBeam.SetPosition(1, end);
                float breathe = 0.88f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.08f;
                apexRechargeBeam.startWidth = 4.2f * breathe;
                apexRechargeBeam.endWidth = Mathf.Lerp(0.34f, 0.16f, recharge01);
                apexRechargeBeam.startColor = new Color(1f, 0.76f, 0.18f, 0.11f);
                apexRechargeBeam.endColor = new Color(1f, 0.48f, 0.025f, 0.72f);

                if (apexRechargeCoreBeam != null)
                {
                    apexRechargeCoreBeam.SetPosition(0, start);
                    apexRechargeCoreBeam.SetPosition(1, end);
                    apexRechargeCoreBeam.startWidth = Mathf.Lerp(1.4f, 0.72f, recharge01);
                    apexRechargeCoreBeam.endWidth = 0.065f;
                    apexRechargeCoreBeam.startColor = new Color(1f, 0.9f, 0.42f, 0.28f);
                    apexRechargeCoreBeam.endColor = new Color(1f, 0.66f, 0.06f, 0.96f);
                }
            }
        }

        if (apexChargeSource != null)
        {
            apexChargeSource.volume = charging ? Mathf.Lerp(0.04f, 0.38f, charge) * sandRunnerAudioMaster : 0f;
            apexChargeSource.pitch = Mathf.Lerp(0.55f, 1.55f, charge);
            if (charging && !apexChargeSource.isPlaying && apexChargeSource.clip != null)
                apexChargeSource.Play();
            else if (!charging && apexChargeSource.isPlaying)
                apexChargeSource.Stop();
        }
    }

    private void UpdateFrontBatteryAnimation(float dt)
    {
        for (int i = 0; i < frontHowitzerRecoil.Count; i++)
        {
            frontHowitzerRecoil[i] = Mathf.MoveTowards(frontHowitzerRecoil[i], 0f, dt * 2.7f);
            if (i < frontHowitzerBarrels.Count && frontHowitzerBarrels[i] != null)
            {
                Vector3 local = frontHowitzerBarrels[i].localPosition;
                local.z = Mathf.Max(0.18f, GetFrontBarrelRestZ(i) - frontHowitzerRecoil[i] * 0.48f);
                frontHowitzerBarrels[i].localPosition = local;
            }
        }

        if (gunnerSide != 0 || battlePyramid == null)
            return;

        Vector3 worldTarget = apexLockedEnemy != null && apexLockedEnemy.transform != null
            ? apexLockedEnemy.transform.position + Vector3.up
            : battlePyramid.position + battlePyramid.forward * 110f + Vector3.up * 5f;

        for (int i = 0; i < frontHowitzerAimRoots.Count; i++)
        {
            Transform root = frontHowitzerAimRoots[i];
            Vector3 localDir = root.parent.InverseTransformDirection((worldTarget - root.position).normalized);
            float yaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -48f, 48f);
            float pitch = Mathf.Clamp(Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg, -8f, 38f);
            Quaternion targetRotation = Quaternion.Euler(-pitch, yaw, 0f);
            root.localRotation = Quaternion.Slerp(root.localRotation, targetRotation, dt * 2.8f);
        }
    }

    private float GetFrontBarrelRestZ(int index)
    {
        return index == 0 || index == 3 ? 1.175f : 0.96f;
    }

    private void RegisterFrontHowitzerRecoil(int index)
    {
        if (index >= 0 && index < frontHowitzerRecoil.Count)
            frontHowitzerRecoil[index] = 1f;
    }

    private void GetApexFireSolution(float charge, out Vector3 start, out Vector3 end)
    {
        UpdateApexTargeting();
        start = apexEmitter != null ? apexEmitter.position :
            (beamMuzzle != null ? beamMuzzle.position : battlePyramid.position + Vector3.up * 5f);
        float maxRange = Mathf.Lerp(110f, 260f, Mathf.Clamp01(charge));
        Vector3 direction = apexAimPoint - start;
        if (direction.sqrMagnitude < 0.01f)
            direction = battlePyramid.forward;
        direction.Normalize();
        end = start + direction * Mathf.Min(maxRange, Mathf.Max(12f, apexTargetDistance));
        if (apexLockedEnemy != null && apexLockedEnemy.transform != null && apexTargetDistance <= maxRange + 5f)
            end = apexLockedEnemy.transform.position + Vector3.up * 1.25f;
    }

    private void PlayApexDischargeSound(float charge)
    {
        if (apexChargeSource != null && apexChargeSource.isPlaying)
            apexChargeSource.Stop();
        if (apexShotSource == null)
            return;

        if (apexDischargeClip == null)
            apexDischargeClip = CreateApexSolarDischargeClip();
        if (apexDischargeClip == null)
            return;

        apexShotSource.pitch = Mathf.Lerp(0.94f, 1.08f, charge);
        apexShotSource.PlayOneShot(apexDischargeClip,
            Mathf.Lerp(0.62f, 1f, charge) * sandRunnerAudioMaster);
    }

    private void DrawApexTargetingReticle()
    {
        if (!apexTargetingMode || mainCamera == null)
            return;

        float width = Screen.width;
        float height = Screen.height;
        float cx = width * 0.5f;
        float cy = height * 0.5f;
        float radius = Mathf.Lerp(34f, 18f, beamCharge);
        Color color = apexLockedEnemy != null
            ? new Color(1f, 0.72f, 0.12f, 0.95f)
            : new Color(0.35f, 0.9f, 1f, 0.9f);

        GUI.color = new Color(0.01f, 0.025f, 0.035f, 0.78f);
        GUI.DrawTexture(new Rect(0f, 0f, width, 62f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, height - 92f, width, 92f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.68f, 0.12f, 0.86f);
        GUI.DrawTexture(new Rect(0f, 60f, width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, height - 94f, width, 2f), Texture2D.whiteTexture);

        float insetX = Mathf.Max(48f, width * 0.07f);
        float insetY = Mathf.Max(84f, height * 0.11f);
        float arm = Mathf.Clamp(width * 0.055f, 62f, 116f);
        DrawApexCorner(insetX, insetY, arm, 1f, 1f, color);
        DrawApexCorner(width - insetX, insetY, arm, -1f, 1f, color);
        DrawApexCorner(insetX, height - insetY, arm, 1f, -1f, color);
        DrawApexCorner(width - insetX, height - insetY, arm, -1f, -1f, color);

        GUI.color = new Color(0.25f, 0.88f, 1f, 0.18f);
        for (int i = -2; i <= 2; i++)
            GUI.DrawTexture(new Rect(cx - 260f, cy + 86f + i * 26f, 520f, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, 72f, 2f, height - 174f), Texture2D.whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - radius, cy - 1f, radius * 0.62f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + radius * 0.38f, cy - 1f, radius * 0.62f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy - radius, 2f, radius * 0.62f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1f, cy + radius * 0.38f, 2f, radius * 0.62f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = Mathf.Clamp(Mathf.RoundToInt(height / 78f), 12, 18);
        label.fontStyle = FontStyle.Bold;
        label.normal.textColor = color;
        string targetName = apexLockedEnemy != null && apexLockedEnemy.transform != null
            ? "APEX LOCK // " + apexLockedEnemy.transform.name.Replace('_', ' ')
            : "APEX MANUAL FOCUS";

        GUI.Label(new Rect(cx - 270f, 14f, 540f, 30f),
            "APEX // HELIOSTATIC FIRE CONTROL", label);
        GUI.Label(new Rect(cx - 270f, cy + 38f, 540f, 28f),
            targetName + " // " + Mathf.RoundToInt(apexTargetDistance) + " m", label);

        string state = beamCooldownTimer > 0f
            ? "SOLAR INTAKE // " + Mathf.CeilToInt(beamCooldownTimer) + "s"
            : beamCharge >= 0.98f ? "FOCUS LOCKED // READY" : "CONDENSING SUNLIGHT";
        GUI.Label(new Rect(46f, 14f, 390f, 30f), state, label);
        GUI.Label(new Rect(width - 520f, 14f, 474f, 30f),
            "R HOLD: FOCUS  |  RMB: PRECISION  |  WHEEL: ZOOM  |  ESC: EXIT", label);

        float gaugeX = 52f;
        float gaugeY = height - 58f;
        float gaugeW = Mathf.Min(420f, width * 0.25f);
        GUI.color = new Color(0.05f, 0.12f, 0.15f, 0.92f);
        GUI.DrawTexture(new Rect(gaugeX, gaugeY, gaugeW, 18f), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(gaugeX + 2f, gaugeY + 2f,
            (gaugeW - 4f) * Mathf.Clamp01(beamCharge), 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(gaugeX, gaugeY - 28f, gaugeW, 25f),
            "SUNLIGHT FOCUS  " + Mathf.RoundToInt(beamCharge * 100f) + "%", label);

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit candidate = enemies[i];
            if (candidate == null || candidate.transform == null || candidate.health <= 0f)
                continue;
            float worldDistance = Vector3.Distance(apexEmitter != null ? apexEmitter.position : mainCamera.transform.position,
                candidate.transform.position);
            if (worldDistance > 285f)
                continue;
            Vector3 screen = mainCamera.WorldToScreenPoint(candidate.transform.position + Vector3.up * 1.2f);
            if (screen.z <= 0f || screen.x < 38f || screen.x > width - 38f ||
                screen.y < 110f || screen.y > height - 72f)
                continue;

            float sy = height - screen.y;
            float size = candidate == apexLockedEnemy ? 46f : 30f;
            Color marker = candidate == apexLockedEnemy
                ? new Color(1f, 0.65f, 0.08f, 0.98f)
                : new Color(0.25f, 0.92f, 1f, 0.68f);
            DrawApexTargetBox(screen.x, sy, size, marker);

            GUIStyle markerLabel = new GUIStyle(label);
            markerLabel.fontSize = Mathf.Max(10, label.fontSize - 2);
            markerLabel.alignment = TextAnchor.UpperCenter;
            markerLabel.normal.textColor = marker;
            string cleanName = candidate.transform.name.Replace('_', ' ');
            if (cleanName.Length > 22)
                cleanName = cleanName.Substring(0, 22);
            int distance = Mathf.RoundToInt(Vector3.Distance(mainCamera.transform.position, candidate.transform.position));
            GUI.Label(new Rect(screen.x - 95f, sy + size * 0.55f + 3f, 190f, 22f),
                cleanName + "  " + distance + "m", markerLabel);
        }
        GUI.color = Color.white;
    }

    private void DrawApexCorner(float x, float y, float arm, float xDirection, float yDirection, Color color)
    {
        GUI.color = color;
        float horizontalX = xDirection > 0f ? x : x - arm;
        float verticalY = yDirection > 0f ? y : y - arm;
        GUI.DrawTexture(new Rect(horizontalX, y - 2f, arm, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 2f, verticalY, 4f, arm), Texture2D.whiteTexture);
    }

    private void DrawApexTargetBox(float x, float y, float size, Color color)
    {
        GUI.color = color;
        float half = size * 0.5f;
        float arm = size * 0.34f;
        GUI.DrawTexture(new Rect(x - half, y - half, arm, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + half - arm, y - half, arm, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - half, y + half - 2f, arm, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + half - arm, y + half - 2f, arm, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - half, y - half, 2f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + half - 2f, y - half, 2f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - half, y + half - arm, 2f, arm), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + half - 2f, y + half - arm, 2f, arm), Texture2D.whiteTexture);
    }

    private void EnterApexTargetingMode()
    {
        if (apexTargetingMode || mainCamera == null || apexFocusPoint == null || beamCooldownTimer > 0f)
            return;

        apexTargetingMode = true;
        apexCommandModeBefore = commandCursorMode;
        apexCameraFovBefore = mainCamera.fieldOfView;
        apexCameraNearBefore = mainCamera.nearClipPlane;
        apexTargetYaw = 0f;
        apexTargetPitch = 4f;
        apexTargetFov = 48f;
        apexCameraStartPosition = mainCamera.transform.position;
        apexCameraStartRotation = mainCamera.transform.rotation;
        apexCameraBlend = 0f;
        howitzerReloadSoundArmed = false;
        if (howitzerMechanicalSource != null && howitzerMechanicalSource.isPlaying)
            howitzerMechanicalSource.Stop();
        PrepareApexViewOccluders();
        if (commandCursorMode)
            SetCommandCursorMode(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lastEvent = "Apex chamber engaged. Hold R to condense sunlight; release to fire.";
    }

    private void ExitApexTargetingMode()
    {
        if (!apexTargetingMode)
            return;

        apexTargetingMode = false;
        RestoreApexViewOccluders();
        if (apexChargeSource != null && apexChargeSource.isPlaying)
            apexChargeSource.Stop();
        if (mainCamera != null)
        {
            mainCamera.nearClipPlane = apexCameraNearBefore;
            mainCamera.fieldOfView = apexCameraFovBefore;
        }
        if (apexCommandModeBefore)
            SetCommandCursorMode(true);
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateApexManualAim(float dt)
    {
        if (!apexTargetingMode || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        float precision = Mouse.current.rightButton.isPressed ? 0.42f : 1f;
        apexTargetYaw = Mathf.Clamp(apexTargetYaw + delta.x * 0.048f * precision, -76f, 76f);
        apexTargetPitch = Mathf.Clamp(apexTargetPitch - delta.y * 0.048f * precision, -12f, 40f);
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            apexTargetFov = Mathf.Clamp(apexTargetFov - Mathf.Sign(scroll) * 4f, 32f, 58f);
    }

    private bool UpdateApexDiegeticCamera(float dt)
    {
        if (!apexTargetingMode || mainCamera == null || apexFocusPoint == null || battlePyramid == null)
            return false;

        Quaternion aim = battlePyramid.rotation * Quaternion.Euler(-apexTargetPitch, apexTargetYaw, 0f);
        Vector3 targetPosition = apexFocusPoint.position + battlePyramid.forward * 1.72f -
                                 battlePyramid.up * 0.24f;
        apexCameraBlend = Mathf.MoveTowards(apexCameraBlend, 1f, dt * 1.65f);
        float blend = Mathf.SmoothStep(0f, 1f, apexCameraBlend);
        Vector3 transitionArc = battlePyramid.up * (Mathf.Sin(blend * Mathf.PI) * 2.6f);
        mainCamera.transform.position = Vector3.Lerp(apexCameraStartPosition, targetPosition, blend) + transitionArc;
        mainCamera.transform.rotation = Quaternion.Slerp(apexCameraStartRotation, aim, blend);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, apexTargetFov, dt * 7f);
        mainCamera.nearClipPlane = 0.08f;
        return true;
    }

    private void PrepareApexViewOccluders()
    {
        RestoreApexViewOccluders();
        if (battlePyramid == null || apexFocusPoint == null)
            return;

        Vector3 socket = apexFocusPoint.position + battlePyramid.forward * 1.72f -
                         battlePyramid.up * 0.24f;
        Renderer[] renderers = battlePyramid.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            Transform apexVisualRoot = apexFocusPoint.parent;
            if (renderer.transform == apexFocusPoint || renderer.name == "Apex_Focused_Sun_Point" ||
                renderer.name == "SR_Authored_Battle_Pyramid" ||
                (apexVisualRoot != null && renderer.transform.IsChildOf(apexVisualRoot)))
            {
                apexViewOccluders.Add(renderer);
                apexViewOccluderStates.Add(renderer.enabled);
                renderer.enabled = false;
                continue;
            }

            Vector3 nearest = renderer.bounds.ClosestPoint(socket);
            bool blocksSocket = renderer.bounds.Contains(socket) ||
                                (nearest - socket).sqrMagnitude < 0.18f * 0.18f;
            if (!blocksSocket)
                continue;

            apexViewOccluders.Add(renderer);
            apexViewOccluderStates.Add(renderer.enabled);
            renderer.enabled = false;
        }
    }

    private void RestoreApexViewOccluders()
    {
        for (int i = 0; i < apexViewOccluders.Count; i++)
        {
            Renderer renderer = apexViewOccluders[i];
            if (renderer != null && i < apexViewOccluderStates.Count)
                renderer.enabled = apexViewOccluderStates[i];
        }
        apexViewOccluders.Clear();
        apexViewOccluderStates.Clear();
    }

    private AudioClip CreateApexSolarChargeClip()
    {
        const int sampleRate = 22050;
        const float duration = 2.4f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float phase = t / duration;
            float envelope = Mathf.Sin(phase * Mathf.PI) * 0.72f + 0.18f;
            float harmonic = Mathf.Sin(t * Mathf.PI * 2f * 96f) * 0.11f +
                             Mathf.Sin(t * Mathf.PI * 2f * 193f) * 0.055f +
                             Mathf.Sin(t * Mathf.PI * 2f * (380f + phase * 120f)) * 0.028f;
            samples[i] = harmonic * envelope;
        }
        AudioClip clip = AudioClip.Create("SR_Apex_Solar_Condensation", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateApexSolarDischargeClip()
    {
        const int sampleRate = 22050;
        const float duration = 0.92f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        System.Random random = new System.Random(7319);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float decay = Mathf.Exp(-t * 5.4f);
            float flare = Mathf.Sin(t * Mathf.PI * 2f * (740f - t * 420f)) * 0.18f;
            float body = Mathf.Sin(t * Mathf.PI * 2f * 82f) * 0.2f;
            float shimmer = Mathf.Sin(t * Mathf.PI * 2f * 1320f) * 0.04f;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            samples[i] = (flare + body + shimmer + noise * 0.035f) * decay;
        }
        AudioClip clip = AudioClip.Create("SR_Apex_Solar_Discharge", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void ArmHowitzerReloadSound()
    {
        howitzerReloadSoundTimer = 1.25f;
        howitzerReloadSoundArmed = true;
    }

    private void UpdateHowitzerReloadAudio(float dt)
    {
        if (!howitzerReloadSoundArmed)
            return;
        howitzerReloadSoundTimer -= dt;
        if (howitzerReloadSoundTimer > 0f)
            return;

        howitzerReloadSoundArmed = false;
        if (howitzerMechanicalSource != null && howitzerReloadClip != null)
        {
            howitzerMechanicalSource.pitch = Random.Range(0.94f, 1.04f);
            howitzerMechanicalSource.PlayOneShot(howitzerReloadClip, 0.88f * sandRunnerAudioMaster);
        }
    }

    private AudioClip CreateHowitzerReloadClip()
    {
        const int sampleRate = 22050;
        const float duration = 1.45f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        float[] strikes = { 0.08f, 0.42f, 0.78f, 1.12f };
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float value = 0f;
            for (int sIndex = 0; sIndex < strikes.Length; sIndex++)
            {
                float local = t - strikes[sIndex];
                if (local < 0f || local > 0.24f)
                    continue;
                float envelope = Mathf.Exp(-local * (18f + sIndex * 2f));
                float tone = Mathf.Sin(local * Mathf.PI * 2f * (92f + sIndex * 37f));
                float clang = Mathf.Sin(local * Mathf.PI * 2f * (410f + sIndex * 55f));
                value += (tone * 0.55f + clang * 0.24f) * envelope;
            }
            samples[i] = Mathf.Clamp(value * 0.7f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("SR_Procedural_300mm_Reload", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}