using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private readonly List<Light> missionAlarmLights = new List<Light>();
    private Transform missionRoot;
    private Transform borderBeacon;
    private Transform redStarAlarm;
    private Material missionRedGlassMaterial;
    private Material missionBorderMaterial;
    private Material missionPalaceMaterial;
    private Material missionSmokeMaterial;
    private float missionElapsed;
    private float missionCommsTimer;
    private int missionBeatIndex;
    private bool missionDirectorInitialized;
    private bool borderReached;
    private string missionTitle = "Mission 01: The Stolen Consort";
    private string missionObjective = "Secure extraction, build an air force, forge the desert alliance, and destroy Mandarinka's mobile fortress.";
    private string missionComms = "Red palace alarms are still burning behind you.";
    private string missionCommsSpeaker = "PYRAMID";
    private string missionCommsBody = "Red palace alarms are still burning behind you.";
    private Color missionCommsColor = new Color(1f, 0.78f, 0.25f, 1f);

    private void InitializeMissionDirector()
    {
        if (missionDirectorInitialized)
            return;

        missionDirectorInitialized = true;
        missionRedGlassMaterial = CreateMaterial("Red Elemental Alarm Glass", new Color(0.86f, 0.05f, 0.035f, 0.72f));
        missionBorderMaterial = CreateMaterial("Earth Border Beacon Light", new Color(0.25f, 0.85f, 1f, 1f));
        missionPalaceMaterial = CreateMaterial("Distant Red Palace Metal", new Color(0.09f, 0.025f, 0.02f, 1f));
        missionSmokeMaterial = CreateMaterial("Distant Burning Smoke", new Color(0.12f, 0.055f, 0.035f, 0.42f));
        SetEmission(missionRedGlassMaterial, new Color(1f, 0.04f, 0.02f, 1f), 1.35f);
        SetEmission(missionBorderMaterial, new Color(0.22f, 0.92f, 1f, 1f), 1.65f);
        ConfigureTransparent(missionRedGlassMaterial);
        ConfigureTransparent(missionSmokeMaterial);

        GameObject existing = GameObject.Find("SandRunners_Mission01_Runtime");
        if (existing != null)
            Destroy(existing);

        missionRoot = new GameObject("SandRunners_Mission01_Runtime").transform;
        CreateRedPalaceBackline();
        CreateBorderBeacon();
        CreateRedStarAlarm();
        ApplyMissionLighting();
    }

    private void UpdateMissionDirector(float dt)
    {
        missionElapsed += dt;

        if (borderBeacon != null && battlePyramid != null && !borderReached)
        {
            float distance = FlatDistance(battlePyramid.position, borderBeacon.position);
            if (distance < 18f)
            {
                borderReached = true;
                lastEvent = "Earth border beacon reached. Optional asylum relay grants 80 gold and 45 wind.";
                SetMissionDialogue("ROBERT: That blue light is Earth jurisdiction? SEBEK: It is a door. Doors can still close.", 7f);
                gold += 80f;
                wind += 45f;
            }
        }

        UpdateMissionBeat();
        UpdateMissionLandmarks(dt);
        missionCommsTimer -= dt;
    }

    private void DrawMissionDirectorGUI()
    {
        if (labelStyle == null || hudHidden || UseStrategicCanvasHud())
            return;

        float width = Mathf.Min(hudExpanded ? 620f : 470f, Screen.width - 40f);
        float height = hudExpanded ? 112f : 72f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 18f;
        GUI.Box(new Rect(x, y, width, height), GUIContent.none);
        GUI.Label(new Rect(x + 14f, y + 8f, width - 28f, 24f), missionTitle, titleStyle);
        GUI.Label(new Rect(x + 14f, y + 34f, width - 28f, 20f), "Objective: " + missionObjective, hudExpanded ? labelStyle : smallStyle);
        if (hudExpanded)
            GUI.Label(new Rect(x + 14f, y + 60f, width - 28f, 42f), missionComms, missionCommsTimer > 0f ? warningStyle : smallStyle);
    }

    private void UpdateMissionBeat()
    {
        if (missionBeatIndex == 0 && missionElapsed > 2.5f)
            SetMissionBeat("ROBERT: Why is the sky red? What did you do?", 7f);
        else if (missionBeatIndex == 1 && missionElapsed > 8f)
            SetMissionBeat("SEBEK-NU-ANKHA: I stole you from a dead court. Decide later whether that was mercy.", 8f);
        else if (missionBeatIndex == 2 && missionElapsed > 16f)
            SetMissionBeat("PYRAMID SPIRIT: Internal factories awake. Hangar womb is ready to birth runners.", 8f);
        else if (missionBeatIndex == 3 && hangarStored > 0)
            SetMissionBeat("ROBERT: There are machines inside the walls. SEBEK: There are cities inside the walls.", 8f);
        else if (missionBeatIndex == 4 && enemies.Count > 0)
            SetMissionBeat("RED FLEET RELAY: Return the Earth consort and kneel beneath the star.", 7f);
        else if (missionBeatIndex == 5 && missionElapsed > 42f)
            SetMissionBeat("SEBEK-NU-ANKHA: The border is ahead. Do not mistake refuge for safety.", 7f);
    }

    private void SetMissionBeat(string text, float duration)
    {
        missionBeatIndex++;
        SetMissionDialogue(text, duration);
    }

    private void SetMissionDialogue(string text, float duration)
    {
        missionComms = text;
        missionCommsTimer = duration;
        missionCommsSpeaker = "PYRAMID";
        missionCommsBody = text;

        int split = text.IndexOf(':');
        if (split > 0 && split < text.Length - 1)
        {
            missionCommsSpeaker = text.Substring(0, split).Trim();
            missionCommsBody = text.Substring(split + 1).Trim();
        }

        missionCommsColor = GetMissionSpeakerColor(missionCommsSpeaker);
        sandRunnerRadioDuckTimer = Mathf.Max(sandRunnerRadioDuckTimer, Mathf.Min(duration, 5f));
    }

    private Color GetMissionSpeakerColor(string speaker)
    {
        if (speaker.Contains("SEBEK"))
            return new Color(1f, 0.78f, 0.25f, 1f);
        if (speaker.Contains("MANDARINKA") || speaker.Contains("RED"))
            return new Color(1f, 0.16f, 0.08f, 1f);
        if (speaker.Contains("ROBERT"))
            return new Color(0.35f, 0.92f, 1f, 1f);
        if (speaker.Contains("SPIRIT") || speaker.Contains("PYRAMID"))
            return new Color(0.22f, 1f, 0.68f, 1f);
        return new Color(1f, 0.82f, 0.36f, 1f);
    }

    private void UpdateMissionLandmarks(float dt)
    {
        if (redStarAlarm != null)
        {
            redStarAlarm.Rotate(Vector3.up, 5f * dt, Space.World);
            float pulse = 1f + Mathf.Sin(Time.time * 1.7f) * 0.055f;
            redStarAlarm.localScale = Vector3.one * pulse;
        }

        for (int i = 0; i < missionAlarmLights.Count; i++)
        {
            Light alarm = missionAlarmLights[i];
            if (alarm != null)
                alarm.intensity = 0.85f + Mathf.Sin(Time.time * 2.9f + i) * 0.28f;
        }

        if (borderBeacon != null)
            borderBeacon.Rotate(Vector3.up, 35f * dt, Space.World);
    }

    private void CreateRedPalaceBackline()
    {
        float z = -mapHalfSize + 12f;
        for (int i = 0; i < 9; i++)
        {
            float x = -58f + i * 14.5f;
            float height = 8f + (i % 3) * 5f;
            GameObject tower = CreateBox(missionRoot, "Distant_Red_Palace_Tower_" + i, new Vector3(x, height * 0.5f, z + Mathf.Sin(i) * 4f), Quaternion.identity, new Vector3(3.2f, height, 3.2f), missionPalaceMaterial);
            CreateBox(tower.transform, "Tower_Red_Window_Slit", new Vector3(0f, 0.28f, -0.52f), Quaternion.identity, new Vector3(0.18f, 0.42f, 0.03f), missionRedGlassMaterial);

            if (i % 2 == 0)
                CreateSmokeColumn(new Vector3(x + 2.5f, height + 2f, z - 2f), 4f + i * 0.35f);
        }

        CreateBox(missionRoot, "Red_Palace_Gate_Silhouette", new Vector3(0f, 5f, z - 2.5f), Quaternion.identity, new Vector3(22f, 10f, 2.2f), missionPalaceMaterial);
        CreateBox(missionRoot, "Red_Palace_Alarm_Slit", new Vector3(0f, 10.5f, z - 3.7f), Quaternion.identity, new Vector3(20f, 0.35f, 0.2f), missionRedGlassMaterial);
    }

    private void CreateBorderBeacon()
    {
        float z = mapHalfSize - 16f;
        borderBeacon = new GameObject("Earth_Empire_Border_Beacon").transform;
        borderBeacon.SetParent(missionRoot, false);
        borderBeacon.position = new Vector3(0f, 0f, z);

        CreateCylinder(borderBeacon, "Border_Beacon_Base", new Vector3(0f, 0.12f, 0f), Quaternion.identity, new Vector3(2.8f, 0.1f, 2.8f), missionBorderMaterial);
        CreateCylinder(borderBeacon, "Border_Beacon_Ring", new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(1.35f, 0.05f, 1.35f), missionBorderMaterial);
        CreateCylinder(borderBeacon, "Border_Beacon_Light_Column", new Vector3(0f, 4.2f, 0f), Quaternion.identity, new Vector3(0.18f, 4.2f, 0.18f), missionBorderMaterial);
        CreatePointLight(borderBeacon, "Border_Beacon_Point_Light", new Vector3(0f, 3.4f, 0f), new Color(0.22f, 0.82f, 1f, 1f), 2.1f, 24f);
    }

    private void CreateRedStarAlarm()
    {
        redStarAlarm = new GameObject("Red_Elemental_Star_Alarm").transform;
        redStarAlarm.SetParent(missionRoot, false);
        redStarAlarm.position = new Vector3(-42f, 30f, -72f);

        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f;
            CreateBox(redStarAlarm, "Red_Star_Ray_" + i, Vector3.zero, Quaternion.Euler(0f, angle, 0f), new Vector3(1.2f, 0.04f, 13f), missionRedGlassMaterial);
        }

        CreatePointLight(redStarAlarm, "Red_Star_Alarm_Light", Vector3.zero, new Color(1f, 0.04f, 0.02f, 1f), 2.2f, 42f);
    }

    private void CreateSmokeColumn(Vector3 position, float height)
    {
        for (int i = 0; i < 4; i++)
        {
            float y = i * height * 0.32f;
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Distant_Burning_Smoke";
            puff.transform.SetParent(missionRoot, false);
            puff.transform.position = position + new Vector3(Mathf.Sin(i * 1.7f) * 0.8f, y, Mathf.Cos(i) * 0.7f);
            puff.transform.localScale = Vector3.one * (2.8f + i * 0.85f);
            Renderer renderer = puff.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = missionSmokeMaterial;
            Collider collider = puff.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }
    }

    private void ApplyMissionLighting()
    {
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.18f, 0.07f, 0.045f, 1f);
            mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 520f);
        }

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                lights[i].color = new Color(1f, 0.72f, 0.46f, 1f);
                lights[i].intensity = Mathf.Max(lights[i].intensity, 1.05f);
                missionAlarmLights.Add(lights[i]);
            }
        }
    }
}
