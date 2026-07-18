using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private readonly List<ParticleSystem> pyramidTrackDustSystems = new List<ParticleSystem>();
    private readonly List<Light> pyramidGlowLights = new List<Light>();
    private readonly List<Renderer> pyramidGlowRenderers = new List<Renderer>();
    private readonly List<Transform> pyramidRoadWheels = new List<Transform>();
    private readonly List<Transform> pyramidTrackLinks = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> pyramidTrackLinkOrigins = new Dictionary<Transform, Vector3>();
    private Transform detailedPyramidArtRoot;
    private Transform leftHangarDoorPanel;
    private Transform rightHangarDoorPanel;
    private Vector3 leftHangarDoorClosedPosition;
    private Vector3 rightHangarDoorClosedPosition;

    private Material pyramidGoldMaterial;
    private Material pyramidDarkArmorMaterial;
    private Material pyramidPanelMaterial;
    private Material pyramidGlowMaterial;
    private Material pyramidMuzzleFlashMaterial;
    private Material pyramidDustMaterial;
    private Material pyramidHangarGlassMaterial;

    private bool cohesivePyramidArtBuilt;
    private float pyramidMovementJuice;
    private float pyramidCameraImpulse;

    private void CreatePyramidArtMaterials()
    {
        pyramidGoldMaterial = CreateMaterial("Pyramid Sun-Worn Gold Hull", new Color(0.92f, 0.64f, 0.22f, 1f));
        pyramidDarkArmorMaterial = CreateMaterial("Pyramid Dark Bronze Undercarriage", new Color(0.12f, 0.105f, 0.085f, 1f));
        pyramidPanelMaterial = CreateMaterial("Pyramid Deep Engraved Panel Lines", new Color(0.34f, 0.23f, 0.12f, 1f));
        pyramidGlowMaterial = CreateMaterial("Pyramid Cyan Reactor Glass", new Color(0.2f, 0.92f, 1f, 1f));
        pyramidMuzzleFlashMaterial = CreateMaterial("Pyramid Weapon Flash", new Color(1f, 0.65f, 0.18f, 1f));
        pyramidDustMaterial = CreateMaterial("Pyramid Track Dust", new Color(0.72f, 0.56f, 0.34f, 0.62f));
        pyramidHangarGlassMaterial = CreateMaterial("Pyramid Hangar Dark Glass", new Color(0.035f, 0.07f, 0.08f, 0.68f));

        SetMetallic(pyramidGoldMaterial, 0.72f, 0.55f);
        SetMetallic(pyramidDarkArmorMaterial, 0.55f, 0.35f);
        SetMetallic(pyramidPanelMaterial, 0.4f, 0.25f);
        SetEmission(pyramidGlowMaterial, new Color(0.16f, 0.95f, 1f, 1f), 1.7f);
        SetEmission(pyramidMuzzleFlashMaterial, new Color(1f, 0.48f, 0.1f, 1f), 2.4f);
        ConfigureTransparent(pyramidHangarGlassMaterial);
        ConfigureTransparent(pyramidDustMaterial);
    }

    private void EnsureCohesivePyramidArt()
    {
        if (battlePyramid == null || cohesivePyramidArtBuilt)
            return;

        if (battlePyramid.Find("Cohesive_Pyramid_Art_Runtime") != null)
        {
            cohesivePyramidArtBuilt = true;
            return;
        }

        Transform root = new GameObject("Cohesive_Pyramid_Art_Runtime").transform;
        root.SetParent(battlePyramid, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        CreateBox(root, "Integrated_Dark_Belly_Armor", new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(4.8f, 0.28f, 4.8f), pyramidDarkArmorMaterial);
        CreatePyramidHull(root, "Unified_Golden_Pyramid_Hull", new Vector3(0f, 0.18f, 0f), 2.35f, 2.65f, pyramidGoldMaterial);
        CreateArmorBanding(root);
        CreateCrawlerChassis(root);
        CreateHangarAssembly(root);
        CreateVisibleInternalSystems(root);
        CreateWeaponAssembly(root);
        CreateExternalFactoryPads(root);
        CreateScaleDetails(root);
        CreateTrackDustEmitter(root, "Left_Track_Dust", new Vector3(-2.65f, 0.03f, -1.55f));
        CreateTrackDustEmitter(root, "Right_Track_Dust", new Vector3(2.65f, 0.03f, -1.55f));
        CreatePointLight(root, "Sun_Reactor_Glow_Light", new Vector3(0f, 1.2f, -0.72f), new Color(0.26f, 0.95f, 1f, 1f), 1.15f, 8.5f);
        CreatePointLight(root, "Apex_Beam_Glow_Light", new Vector3(0f, 3.18f, 0f), new Color(1f, 0.84f, 0.32f, 1f), 0.75f, 7f);

        cohesivePyramidArtBuilt = true;
    }

    private void ResetPyramidArtCache()
    {
        pyramidTrackDustSystems.Clear();
        pyramidGlowLights.Clear();
        pyramidGlowRenderers.Clear();
        pyramidRoadWheels.Clear();
        pyramidTrackLinks.Clear();
        pyramidTrackLinkOrigins.Clear();
        detailedPyramidArtRoot = null;
        leftHangarDoorPanel = null;
        rightHangarDoorPanel = null;
    }

    private void CachePyramidArtChild(Transform child, string lowerName)
    {
        ParticleSystem dust = child.GetComponent<ParticleSystem>();
        if (dust != null && lowerName.Contains("track_dust"))
            pyramidTrackDustSystems.Add(dust);

        Light glow = child.GetComponent<Light>();
        if (glow != null && (lowerName.Contains("glow") || lowerName.Contains("reactor") || lowerName.Contains("beam")))
            pyramidGlowLights.Add(glow);

        Renderer renderer = child.GetComponent<Renderer>();
        if (renderer != null && (lowerName.Contains("reactor") || lowerName.Contains("glow") || lowerName.Contains("glass")))
            pyramidGlowRenderers.Add(renderer);

        if (lowerName.Contains("crawler_roadwheel"))
            pyramidRoadWheels.Add(child);

        if (lowerName.Contains("track_link"))
        {
            pyramidTrackLinks.Add(child);
            pyramidTrackLinkOrigins[child] = child.localPosition;
        }

        if (lowerName == "cohesive_pyramid_art_runtime")
            detailedPyramidArtRoot = child;
        else if (lowerName.Contains("front_hangar_door_left"))
        {
            leftHangarDoorPanel = child;
            leftHangarDoorClosedPosition = child.localPosition;
        }
        else if (lowerName.Contains("front_hangar_door_right"))
        {
            rightHangarDoorPanel = child;
            rightHangarDoorClosedPosition = child.localPosition;
        }
    }

    private void UpdatePyramidMotionJuice(float target, float dt)
    {
        pyramidMovementJuice = Mathf.MoveTowards(pyramidMovementJuice, target, dt * 3.6f);
    }

    private void RegisterWeaponImpulse(float amount)
    {
        pyramidCameraImpulse = Mathf.Max(pyramidCameraImpulse, amount);
    }

    private Vector3 GetPyramidCameraShakeOffset(float dt)
    {
        float amount = (pyramidCameraImpulse + pyramidMovementJuice * 0.035f) * releaseCameraShakeStrength;
        pyramidCameraImpulse = Mathf.MoveTowards(pyramidCameraImpulse, 0f, dt * 0.7f);
        if (mainCamera == null || amount <= 0.001f)
            return Vector3.zero;

        float x = (Mathf.PerlinNoise(Time.time * 31.7f, 4.13f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(8.41f, Time.time * 37.9f) - 0.5f) * 2f;
        return (mainCamera.transform.right * x + mainCamera.transform.up * y) * amount;
    }

    private void UpdatePyramidArtJuice(float dt, float hangarAmount, float beamChargeAmount)
    {
        float dustRate = Mathf.Lerp(0f, 95f, pyramidMovementJuice);
        for (int i = 0; i < pyramidTrackDustSystems.Count; i++)
            SetParticleRate(pyramidTrackDustSystems[i], dustRate);

        float wheelSpin = Mathf.Lerp(0f, 420f, pyramidMovementJuice) * dt;
        for (int i = 0; i < pyramidRoadWheels.Count; i++)
            pyramidRoadWheels[i].Rotate(Vector3.right, wheelSpin, Space.Self);

        float glowPulse = 0.75f + Mathf.Sin(Time.time * 2.1f) * 0.12f + pyramidMovementJuice * 0.22f + beamChargeAmount * 0.55f;
        for (int i = 0; i < pyramidGlowLights.Count; i++)
            pyramidGlowLights[i].intensity = Mathf.Lerp(0.65f, 2.4f, Mathf.Clamp01(glowPulse));

        Color reactor = Color.Lerp(new Color(0.1f, 0.55f, 0.7f, 1f), new Color(1f, 0.86f, 0.26f, 1f), beamChargeAmount);
        for (int i = 0; i < pyramidGlowRenderers.Count; i++)
            SetEmission(pyramidGlowRenderers[i].sharedMaterial, reactor, 1.2f + glowPulse);

        if (hangarAmount > 0.01f && hangarDoor != null)
            RegisterWeaponImpulse(0.003f * hangarAmount);

        UpdateDetailedPyramidMechanics(dt, hangarAmount);
    }

    private void UpdateDetailedPyramidMechanics(float dt, float hangarAmount)
    {
        float linkTravel = Time.time * Mathf.Lerp(0f, 1.9f, pyramidMovementJuice);
        for (int i = 0; i < pyramidTrackLinks.Count; i++)
        {
            Transform link = pyramidTrackLinks[i];
            if (link == null || !pyramidTrackLinkOrigins.TryGetValue(link, out Vector3 origin))
                continue;
            float lane = Mathf.Repeat(origin.z + 1.8f + linkTravel, 3.6f) - 1.8f;
            link.localPosition = new Vector3(origin.x, origin.y, lane);
        }

        if (detailedPyramidArtRoot != null)
        {
            float heave = Mathf.Sin(Time.time * 4.2f) * 0.025f * pyramidMovementJuice;
            float pitch = Mathf.Sin(Time.time * 2.1f) * 0.32f * pyramidMovementJuice;
            float roll = Mathf.Sin(Time.time * 1.55f + 1.7f) * 0.24f * pyramidMovementJuice;
            detailedPyramidArtRoot.localPosition = Vector3.up * heave;
            detailedPyramidArtRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        float easedDoor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(hangarAmount));
        if (leftHangarDoorPanel != null)
            leftHangarDoorPanel.localPosition = leftHangarDoorClosedPosition + new Vector3(-0.82f, -0.28f, 0f) * easedDoor;
        if (rightHangarDoorPanel != null)
            rightHangarDoorPanel.localPosition = rightHangarDoorClosedPosition + new Vector3(0.82f, -0.28f, 0f) * easedDoor;
    }

    private void CreateWeaponFlash(Vector3 position, float scale, Color color)
    {
        float actualScale = Mathf.Max(scale, 0.1f);
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "Pyramid_Transient_Weapon_Flash";
        flash.transform.position = position;
        flash.transform.localScale = Vector3.one * actualScale;

        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = pyramidMuzzleFlashMaterial;
            SetEmission(renderer.sharedMaterial, color, 3.4f);
        }

        RingVisual visual = new RingVisual();
        visual.transform = flash.transform;
        visual.life = 0.22f;
        visual.maxLife = 0.22f;
        visual.startScale = Vector3.one * actualScale;
        visual.endScale = Vector3.one * actualScale * 3.2f;
        rings.Add(visual);

        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = color;
        flashLight.intensity = Mathf.Clamp(actualScale * 8f, 1.5f, 12f);
        flashLight.range = Mathf.Clamp(actualScale * 30f, 6f, 50f);
        Destroy(flash, 0.18f);
    }

    private void CreateArmorBanding(Transform root)
    {
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            float y = Mathf.Lerp(0.42f, 2.35f, t);
            float half = Mathf.Lerp(2.05f, 0.26f, t);
            float width = half * 1.72f;
            Material material = i % 2 == 0 ? pyramidPanelMaterial : pyramidGoldMaterial;

            CreateBox(root, "Front_Engraved_Armor_Band_" + i, new Vector3(0f, y, -half - 0.035f), Quaternion.identity, new Vector3(width, 0.035f, 0.045f), material);
            CreateBox(root, "Rear_Engraved_Armor_Band_" + i, new Vector3(0f, y, half + 0.035f), Quaternion.identity, new Vector3(width, 0.035f, 0.045f), material);
            CreateBox(root, "Left_Engraved_Armor_Band_" + i, new Vector3(-half - 0.035f, y, 0f), Quaternion.identity, new Vector3(0.045f, 0.035f, width), material);
            CreateBox(root, "Right_Engraved_Armor_Band_" + i, new Vector3(half + 0.035f, y, 0f), Quaternion.identity, new Vector3(0.045f, 0.035f, width), material);
        }

        for (int i = 0; i < 4; i++)
        {
            float x = -1.15f + i * 0.76f;
            CreateBox(root, "Front_Vertical_Bronze_Rib_" + i, new Vector3(x, 1.1f, -1.28f), Quaternion.Euler(-17f, 0f, 0f), new Vector3(0.05f, 1.35f, 0.04f), pyramidPanelMaterial);
            CreateBox(root, "Rear_Vertical_Bronze_Rib_" + i, new Vector3(x, 1.1f, 1.28f), Quaternion.Euler(17f, 0f, 0f), new Vector3(0.05f, 1.35f, 0.04f), pyramidPanelMaterial);
        }
    }

    private void CreateCrawlerChassis(Transform root)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            CreateBox(root, label + "_Monolithic_Track_Housing", new Vector3(side * 2.42f, -0.12f, 0f), Quaternion.identity, new Vector3(0.62f, 0.46f, 4.05f), pyramidDarkArmorMaterial);
            CreateBox(root, label + "_Gold_Track_Side_Armor", new Vector3(side * 2.42f, 0.24f, 0f), Quaternion.identity, new Vector3(0.72f, 0.18f, 3.65f), pyramidGoldMaterial);

            for (int i = 0; i < 7; i++)
            {
                float z = -1.55f + i * 0.52f;
                CreateCylinder(root, "Crawler_RoadWheel_" + label + "_" + i, new Vector3(side * 2.5f, -0.16f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.26f, 0.08f, 0.26f), pyramidPanelMaterial);
                CreateBox(root, label + "_Track_Link_Top_" + i, new Vector3(side * 2.5f, 0.08f, z), Quaternion.identity, new Vector3(0.72f, 0.045f, 0.22f), pyramidDarkArmorMaterial);
                CreateBox(root, label + "_Track_Link_Bottom_" + i, new Vector3(side * 2.5f, -0.38f, z), Quaternion.identity, new Vector3(0.72f, 0.05f, 0.24f), pyramidDarkArmorMaterial);
            }
        }
    }

    private void CreateHangarAssembly(Transform root)
    {
        CreateBox(root, "Hangar_Interior_Bay", new Vector3(0f, 0.58f, -2.1f), Quaternion.identity, new Vector3(1.55f, 0.86f, 0.18f), pyramidHangarGlassMaterial, true);
        CreateBox(root, "Front_Hangar_Door_Left", new Vector3(-0.72f, 0.62f, -2.22f), Quaternion.identity, new Vector3(0.69f, 0.78f, 0.08f), pyramidPanelMaterial, true);
        CreateBox(root, "Front_Hangar_Door_Right", new Vector3(0.72f, 0.62f, -2.22f), Quaternion.identity, new Vector3(0.69f, 0.78f, 0.08f), pyramidPanelMaterial, true);
        CreateBox(root, "Hangar_Cyan_Light_Slit", new Vector3(0f, 1.07f, -2.27f), Quaternion.identity, new Vector3(1.28f, 0.045f, 0.045f), pyramidGlowMaterial);
        CreateBox(root, "Hangar_Click_Collider", new Vector3(0f, 0.66f, -2.32f), Quaternion.identity, new Vector3(1.75f, 1.08f, 0.16f), pyramidHangarGlassMaterial, true);

        Transform exit = new GameObject("Hangar_Exit").transform;
        exit.SetParent(root, false);
        exit.localPosition = new Vector3(0f, 0.18f, -3.12f);

        Transform display = new GameObject("Internal_Unit_Display").transform;
        display.SetParent(root, false);
        display.localPosition = new Vector3(0f, 0.48f, -2.42f);
        CreateBox(display, "Stored_Vimana_Core", Vector3.zero, Quaternion.identity, new Vector3(0.34f, 0.13f, 0.55f), externalUnitMaterial);
        CreateBox(display, "Stored_Vimana_Left_Wing", new Vector3(-0.34f, 0f, 0.02f), Quaternion.identity, new Vector3(0.34f, 0.035f, 0.36f), pyramidGoldMaterial);
        CreateBox(display, "Stored_Vimana_Right_Wing", new Vector3(0.34f, 0f, 0.02f), Quaternion.identity, new Vector3(0.34f, 0.035f, 0.36f), pyramidGoldMaterial);
        display.gameObject.SetActive(false);
    }

    private void CreateVisibleInternalSystems(Transform root)
    {
        CreateBox(root, "Cutaway_Reactor_Window_Front", new Vector3(0f, 1.16f, -1.57f), Quaternion.Euler(-18f, 0f, 0f), new Vector3(0.86f, 0.44f, 0.04f), pyramidGlowMaterial);
        CreateBox(root, "Cutaway_Reactor_Window_Left", new Vector3(-1.57f, 1.16f, 0f), Quaternion.Euler(0f, 0f, 18f), new Vector3(0.04f, 0.44f, 0.86f), pyramidGlowMaterial);
        CreateBox(root, "Cutaway_Reactor_Window_Right", new Vector3(1.57f, 1.16f, 0f), Quaternion.Euler(0f, 0f, -18f), new Vector3(0.04f, 0.44f, 0.86f), pyramidGlowMaterial);
        CreateCylinder(root, "Central_Sun_Reactor_Core_Glow", new Vector3(0f, 1.1f, -0.72f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f, 0.1f, 0.42f), pyramidGlowMaterial);
        CreateBox(root, "Internal_Fabricator_Left_Glow", new Vector3(-0.62f, 0.72f, -1.48f), Quaternion.identity, new Vector3(0.42f, 0.12f, 0.05f), pyramidGlowMaterial);
        CreateBox(root, "Internal_Fabricator_Right_Glow", new Vector3(0.62f, 0.72f, -1.48f), Quaternion.identity, new Vector3(0.42f, 0.12f, 0.05f), pyramidGlowMaterial);
    }

    private void CreateWeaponAssembly(Transform root)
    {
        Vector3[] autocannonPoints =
        {
            new Vector3(-1.65f, 0.92f, -1.55f), new Vector3(1.65f, 0.92f, -1.55f),
            new Vector3(-1.92f, 0.68f, -0.45f), new Vector3(1.92f, 0.68f, -0.45f),
            new Vector3(-1.92f, 0.68f, 0.72f), new Vector3(1.92f, 0.68f, 0.72f),
            new Vector3(-1.3f, 1.35f, 1.05f), new Vector3(1.3f, 1.35f, 1.05f)
        };

        for (int i = 0; i < autocannonPoints.Length; i++)
        {
            Vector3 p = autocannonPoints[i];
            CreateCylinder(root, "AutoCannon_30mm_Turret_Base_" + i, p, Quaternion.identity, new Vector3(0.16f, 0.07f, 0.16f), pyramidPanelMaterial);
            Vector3 muzzleLocal = p + new Vector3(Mathf.Sign(p.x) * 0.18f, 0f, p.z < 0f ? -0.24f : 0.24f);
            CreateCylinder(root, "AutoCannon_30mm_Barrel_" + i, Vector3.Lerp(p, muzzleLocal, 0.52f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.045f, 0.22f, 0.045f), pyramidDarkArmorMaterial);
            CreateMarker(root, "AutoCannon_30mm_Muzzle_" + i, muzzleLocal);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Port" : "Starboard";
            for (int i = 0; i < 2; i++)
            {
                float z = -0.62f + i * 1.18f;
                Vector3 basePos = new Vector3(side * 2.12f, 0.78f, z);
                CreateBox(root, label + "_300mm_Howitzer_Casemate_" + i, basePos, Quaternion.identity, new Vector3(0.24f, 0.28f, 0.48f), pyramidPanelMaterial);
                CreateCylinder(root, label + "_300mm_Howitzer_Barrel_" + i, basePos + new Vector3(side * 0.34f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.13f, 0.62f, 0.13f), pyramidDarkArmorMaterial);
                CreateMarker(root, label + "_300mm_Howitzer_Muzzle_" + i, basePos + new Vector3(side * 0.82f, 0f, 0f));
            }
        }

        CreateCylinder(root, "Apex_Beam_Capacitor_Ring_Glow", new Vector3(0f, 2.78f, 0f), Quaternion.identity, new Vector3(0.38f, 0.05f, 0.38f), pyramidGlowMaterial);
        CreateCylinder(root, "Apex_Charged_Beam_Cannon", new Vector3(0f, 3.0f, 0f), Quaternion.identity, new Vector3(0.18f, 0.45f, 0.18f), pyramidDarkArmorMaterial);
        CreateMarker(root, "Beam_Cannon_Muzzle", new Vector3(0f, 3.34f, 0f));

        for (int i = 0; i < 4; i++)
        {
            float x = -1.1f + i * 0.73f;
            CreateBox(root, "Cruise_Missile_Bay_Hatch_" + i, new Vector3(x, 1.45f, 1.32f), Quaternion.Euler(20f, 0f, 0f), new Vector3(0.42f, 0.06f, 0.68f), pyramidPanelMaterial);
            CreateMarker(root, "Cruise_Missile_Launcher_" + i, new Vector3(x, 1.72f, 1.5f));
        }

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.46f : 0.46f;
            CreateBox(root, "SunCore_Missile_Armored_Hatch_" + i, new Vector3(x, 1.78f, 0.82f), Quaternion.Euler(20f, 0f, 0f), new Vector3(0.48f, 0.08f, 0.58f), nuclearMaterial);
            CreateMarker(root, "SunCore_Missile_Launcher_" + i, new Vector3(x, 2.06f, 0.98f));
        }
    }

    private void CreateExternalFactoryPads(Transform root)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Transform pad = CreateBox(root, "External_Factory_Pad_" + label, new Vector3(side * 3.02f, 0.03f, -1.05f), Quaternion.identity, new Vector3(1.05f, 0.08f, 1.26f), pyramidPanelMaterial, true).transform;
            CreateBox(pad, label + "_Factory_Cyan_Rails", new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(0.82f, 0.03f, 0.96f), pyramidGlowMaterial);
        }
    }

    private void CreateScaleDetails(Transform root)
    {
        for (int i = 0; i < 10; i++)
        {
            float x = -2.05f + i * 0.46f;
            CreateBox(root, "Tiny_Service_Window_" + i, new Vector3(x, 0.92f, -2.18f), Quaternion.identity, new Vector3(0.08f, 0.04f, 0.025f), pyramidGlowMaterial);
        }

        for (int i = 0; i < 5; i++)
            CreateBox(root, "Scale_Tiny_Service_Crawler_" + i, new Vector3(-1.4f + i * 0.34f, -0.39f, -2.82f), Quaternion.identity, new Vector3(0.16f, 0.08f, 0.28f), pyramidDarkArmorMaterial);
    }

    private GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider = false)
    {
        return CreatePrimitive(parent, PrimitiveType.Cube, name, localPosition, localRotation, localScale, material, keepCollider);
    }

    private GameObject CreateCylinder(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider = false)
    {
        return CreatePrimitive(parent, PrimitiveType.Cylinder, name, localPosition, localRotation, localScale, material, keepCollider);
    }

    private GameObject CreatePrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = go.GetComponent<Collider>();
        if (!keepCollider && collider != null)
            Destroy(collider);

        return go;
    }

    private void CreatePyramidHull(Transform parent, string name, Vector3 localPosition, float halfBase, float height, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        Mesh mesh = new Mesh();
        mesh.name = name + "_Mesh";
        Vector3 apex = new Vector3(0f, height, 0f);
        Vector3 a = new Vector3(-halfBase, 0f, -halfBase);
        Vector3 b = new Vector3(halfBase, 0f, -halfBase);
        Vector3 c = new Vector3(halfBase, 0f, halfBase);
        Vector3 d = new Vector3(-halfBase, 0f, halfBase);
        mesh.vertices = new[] { a, b, c, d, apex };
        mesh.triangles = new[]
        {
            0, 4, 1,
            1, 4, 2,
            2, 4, 3,
            3, 4, 0,
            0, 1, 2,
            0, 2, 3
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private void CreateMarker(Transform parent, string name, Vector3 localPosition)
    {
        Transform marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.localPosition = localPosition;
    }

    private void CreatePointLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private void CreateTrackDustEmitter(Transform parent, string name, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.65f;
        main.startSpeed = 2.4f;
        main.startSize = 0.34f;
        main.startColor = new Color(0.72f, 0.56f, 0.34f, 0.48f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.45f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = pyramidDustMaterial;
    }

    private void SetParticleRate(ParticleSystem particles, float rate)
    {
        if (particles == null)
            return;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        if (rate > 0.5f && !particles.isPlaying)
            particles.Play();
        else if (rate <= 0.5f && particles.isPlaying)
            particles.Stop();
    }

    private void SetMetallic(Material material, float metallic, float smoothness)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
    }

    private void SetEmission(Material material, Color color, float intensity)
    {
        if (material == null)
            return;

        Color emission = color * intensity;
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", emission);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    private void ConfigureTransparent(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
    }
}