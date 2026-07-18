using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private enum SandRunnerAudioBus
    {
        Ambience,
        Combat,
        Aircraft,
        UI,
        Warnings,
        Radio
    }

    private enum SandRunnerSpatialMode
    {
        WorldClose,
        CombatStrategic,
        Global
    }

    private sealed class SandRunnerSoundDefinition
    {
        public AudioClip[] clips;
        public SandRunnerAudioBus bus;
        public SandRunnerSpatialMode spatialMode;
        public float baseVolume = 1f;
        public float pitchMin = 1f;
        public float pitchMax = 1f;
        public float minDistance = 6f;
        public float maxDistance = 420f;
        public float cooldown = 0.02f;
        public float cellSize = 28f;
        public int concurrency = 6;
        public int priority = 128;
        public bool fallback;
    }

    private enum SandRunnerSound
    {
        UiConfirm,
        UiButton,
        UiSelection,
        UiWarning,
        ProductionComplete,
        ProductionBuild,
        HangarRelease,
        AircraftTakeoff,
        AircraftPass,
        AircraftReturn,
        AircraftRepair,
        WindGenerator,
        NeutralAlert,
        JuzzherEngine,
        Autocannon,
        HeavyAutocannon,
        ArtilleryFire,
        ArtilleryShell,
        HeavyArtillery,
        SolarBeam,
        LaserBeam,
        MissileLaunch,
        MissileFlyby,
        EnemyShot,
        BulletImpact,
        HitImpact,
        HeavyImpact,
        Explosion,
        LargeExplosion,
        NuclearExplosion,
        Construction,
        ResourceDelivery,
        HarvestStart,
        HarvestReturn,
        PyramidDamage,
        Pyralert,
        GustavWarning,
        VictoryFanfare,
        DefeatSting,
        WaveAlert,
        EnemySpotted,
        ThreatDetected,
        BuildingComplete
    }

    private readonly Dictionary<SandRunnerSound, AudioClip> sandRunnerClips = new Dictionary<SandRunnerSound, AudioClip>();
    private readonly Dictionary<SandRunnerSound, SandRunnerSoundDefinition> sandRunnerSoundDefinitions = new Dictionary<SandRunnerSound, SandRunnerSoundDefinition>();
    private readonly HashSet<AudioClip> sandRunnerImportedClips = new HashSet<AudioClip>();
    private readonly Dictionary<string, float> sandRunnerSoundCellLastPlayed = new Dictionary<string, float>();
    private readonly Dictionary<AudioSource, int> sandRunnerSpatialPriorities = new Dictionary<AudioSource, int>();
    private readonly Queue<string> sandRunnerAudioMonitor = new Queue<string>();
    private const int SandRunnerAudioMonitorCapacity = 48;
    [SerializeField, Range(0f, 1f)] private float sandRunnerAudioMaster = 0.45f;
    private AudioSource sandRunnerUiSource;
    private AudioSource sandRunnerCombatSource;
    private AudioSource sandRunnerAmbienceSource;
    private AudioSource sandRunnerEngineSource;
    private AudioSource sandRunnerWarningSource;
    private Transform sandRunnerSpatialPoolRoot;
    private readonly List<AudioSource> sandRunnerSpatialPool = new List<AudioSource>();
    private int sandRunnerSpatialPoolCursor;
    [SerializeField, Range(0f, 1f)] private float sandRunnerAmbienceVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float sandRunnerRadioVolume = 0.8f;
    private readonly Dictionary<Transform, AudioSource> sandRunnerAircraftEngineSources = new Dictionary<Transform, AudioSource>();
    private AudioClip sandRunnerAircraftEngineClip;
    private float sandRunnerWarningTimer;
    private float sandRunnerRadioDuckTimer;
    private bool sandRunnerAudioReady;

    private void InitializeSandRunnersAudio()
    {
        if (sandRunnerAudioReady)
            return;

        sandRunnerClips.Clear();
        sandRunnerSoundDefinitions.Clear();
        sandRunnerImportedClips.Clear();
        AudioClip[] importedAudio = Resources.LoadAll<AudioClip>("SandRunners/Audio");
        for (int importedIndex = 0; importedIndex < importedAudio.Length; importedIndex++)
            if (importedAudio[importedIndex] != null)
                sandRunnerImportedClips.Add(importedAudio[importedIndex]);
        sandRunnerSoundCellLastPlayed.Clear();
        sandRunnerSpatialPriorities.Clear();
        sandRunnerAudioMonitor.Clear();
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera != null)
        {
            sandRunnerUiSource = camera.gameObject.AddComponent<AudioSource>();
            sandRunnerUiSource.spatialBlend = 0f;
            sandRunnerUiSource.playOnAwake = false;
            sandRunnerUiSource.volume = 1f;

            sandRunnerCombatSource = camera.gameObject.AddComponent<AudioSource>();
            sandRunnerCombatSource.spatialBlend = 0f;
            sandRunnerCombatSource.playOnAwake = false;
            sandRunnerCombatSource.volume = 1f;
            sandRunnerCombatSource.priority = 24;

            sandRunnerAmbienceSource = camera.gameObject.AddComponent<AudioSource>();
            sandRunnerAmbienceSource.spatialBlend = 0f;
            sandRunnerAmbienceSource.loop = true;
            sandRunnerAmbienceSource.volume = 0.14f * sandRunnerAmbienceVolume * sandRunnerAudioMaster;
            sandRunnerAmbienceSource.clip = Resources.Load<AudioClip>("SandRunners/Audio/SR_Ambience");
            if (sandRunnerAmbienceSource.clip == null)
                sandRunnerAmbienceSource.clip = CreateProceduralClip("SR_Desert_War_Ambience_Fallback", 7.5f, 62f, 0.08f, 0.18f, 0.8f);
            sandRunnerAmbienceSource.Play();

            sandRunnerWarningSource = camera.gameObject.AddComponent<AudioSource>();
            sandRunnerWarningSource.spatialBlend = 0f;
            sandRunnerWarningSource.playOnAwake = false;
            sandRunnerWarningSource.volume = 1f;
        }

        if (battlePyramid != null)
        {
            sandRunnerEngineSource = battlePyramid.gameObject.AddComponent<AudioSource>();
            sandRunnerEngineSource.spatialBlend = 1f;
            sandRunnerEngineSource.loop = true;
            sandRunnerEngineSource.minDistance = 18f;
            sandRunnerEngineSource.maxDistance = 180f;
            sandRunnerEngineSource.volume = 0f;
            sandRunnerEngineSource.clip = Resources.Load<AudioClip>("SandRunners/Audio/SR_Pyramid_Engine");
            if (sandRunnerEngineSource.clip == null)
                sandRunnerEngineSource.clip = CreateProceduralClip("SR_Pyramid_Crawler_Engine_Fallback", 1.8f, 38f, 0.10f, 0.12f, 0.35f);
            sandRunnerEngineSource.Play();
        }

        GameObject spatialRootObject = new GameObject("SandRunners_Spatial_Audio_Pool");
        sandRunnerSpatialPoolRoot = spatialRootObject.transform;
        for (int i = 0; i < 32; i++)
        {
            GameObject voiceObject = new GameObject("Spatial_Voice_" + i.ToString("00"));
            voiceObject.transform.SetParent(sandRunnerSpatialPoolRoot, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 6f;
            source.maxDistance = 420f;
            source.dopplerLevel = 0.12f;
            source.playOnAwake = false;
            source.priority = 128;
            sandRunnerSpatialPool.Add(source);
            sandRunnerSpatialPriorities[source] = 0;
        }

        sandRunnerClips[SandRunnerSound.UiConfirm] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_UI_Confirm", "SR_UI_Confirm_Fallback", 0.14f, 880f, 0.10f, 0.01f, 1.4f);
        sandRunnerClips[SandRunnerSound.UiButton] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_UI_Hover", "SR_UI_Button_Fallback", 0.08f, 660f, 0.07f, 0.02f, 2.0f);
        sandRunnerClips[SandRunnerSound.UiSelection] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_UI_Hover", "SR_UI_Selection_Fallback", 0.18f, 520f, 0.09f, 0.03f, 1.2f);
        sandRunnerClips[SandRunnerSound.UiWarning] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_UI_Error", "SR_UI_Warning_Fallback", 0.42f, 180f, 0.22f, 0.08f, 0.6f);

        sandRunnerClips[SandRunnerSound.ProductionComplete] = CreateProceduralClip("SR_Production_Complete", 0.55f, 330f, 0.16f, 0.04f, 0.8f);
        sandRunnerClips[SandRunnerSound.ProductionBuild] = CreateProceduralClip("SR_Production_Build", 0.28f, 480f, 0.07f, 0.06f, 1.1f);

        sandRunnerClips[SandRunnerSound.HangarRelease] = CreateProceduralClip("SR_Hangar_Release", 0.85f, 120f, 0.20f, 0.12f, 0.42f);
        sandRunnerClips[SandRunnerSound.AircraftTakeoff] = CreateProceduralClip("SR_Aircraft_Takeoff", 0.9f, 72f, 0.24f, 0.24f, 0.45f);
        sandRunnerClips[SandRunnerSound.AircraftPass] = CreateProceduralClip("SR_Aircraft_Pass", 0.42f, 210f, 0.12f, 0.26f, 0.5f);
        sandRunnerClips[SandRunnerSound.AircraftReturn] = CreateProceduralClip("SR_Aircraft_Return", 0.58f, 128f, 0.16f, 0.12f, 0.58f);
        sandRunnerClips[SandRunnerSound.AircraftRepair] = CreateProceduralClip("SR_Aircraft_Repair", 0.45f, 420f, 0.1f, 0.04f, 0.75f);
        sandRunnerClips[SandRunnerSound.WindGenerator] = CreateProceduralClip("SR_Wind_Generator", 0.6f, 96f, 0.08f, 0.18f, 0.5f);
        sandRunnerClips[SandRunnerSound.NeutralAlert] = CreateProceduralClip("SR_Neutral_Alert", 0.48f, 260f, 0.16f, 0.08f, 0.7f);
        sandRunnerClips[SandRunnerSound.JuzzherEngine] = CreateProceduralClip("SR_Juzzher_Engine", 0.75f, 54f, 0.18f, 0.24f, 0.38f);
        sandRunnerAircraftEngineClip = CreateProceduralClip("SR_Aircraft_Engine_Loop", 1.9f, 118f, 0.18f, 0.2f, 0.32f);

        sandRunnerClips[SandRunnerSound.Autocannon] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Autocannon", "SR_30mm_Burst_Fallback", 0.10f, 140f, 0.20f, 0.30f, 1.6f);
        sandRunnerClips[SandRunnerSound.HeavyAutocannon] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Autocannon", "SR_80mm_Burst_Fallback", 0.18f, 95f, 0.28f, 0.25f, 1.2f);

        sandRunnerClips[SandRunnerSound.ArtilleryFire] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Howitzer", "SR_Howitzer_Shot_Fallback", 0.38f, 68f, 0.30f, 0.18f, 0.7f);
        sandRunnerClips[SandRunnerSound.ArtilleryShell] = CreateProceduralClip("SR_Shell_Whistle", 0.52f, 180f, 0.08f, 0.04f, 0.5f);
        sandRunnerClips[SandRunnerSound.HeavyArtillery] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Howitzer", "SR_Heavy_Artillery_Fallback", 0.72f, 62f, 0.28f, 0.22f, 0.55f);

        sandRunnerClips[SandRunnerSound.SolarBeam] = CreateProceduralClip("SR_Solar_Beam", 0.46f, 620f, 0.20f, 0.08f, 0.62f);
        sandRunnerClips[SandRunnerSound.LaserBeam] = CreateProceduralClip("SR_Laser_Beam", 0.22f, 880f, 0.12f, 0.04f, 0.9f);

        sandRunnerClips[SandRunnerSound.MissileLaunch] = CreateProceduralClip("SR_Missile_Launch", 0.54f, 180f, 0.20f, 0.16f, 0.5f);
        sandRunnerClips[SandRunnerSound.MissileFlyby] = CreateProceduralClip("SR_Missile_Flyby", 0.32f, 260f, 0.10f, 0.08f, 0.6f);

        sandRunnerClips[SandRunnerSound.EnemyShot] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Autocannon", "SR_Jade_Enemy_Shot_Fallback", 0.32f, 210f, 0.16f, 0.18f, 0.7f);

        sandRunnerClips[SandRunnerSound.BulletImpact] = CreateProceduralClip("SR_Bullet_Impact", 0.14f, 180f, 0.14f, 0.28f, 1.1f);
        sandRunnerClips[SandRunnerSound.HitImpact] = CreateProceduralClip("SR_Hit_Impact", 0.34f, 82f, 0.20f, 0.22f, 0.78f);
        sandRunnerClips[SandRunnerSound.HeavyImpact] = CreateProceduralClip("SR_Heavy_Impact", 0.82f, 44f, 0.28f, 0.28f, 0.48f);

        sandRunnerClips[SandRunnerSound.Explosion] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Explosion", "SR_Explosion_Fallback", 0.55f, 52f, 0.30f, 0.24f, 0.52f);
        sandRunnerClips[SandRunnerSound.LargeExplosion] = LoadSandRunnerClipOrFallback("SandRunners/Audio/SR_Explosion_Distant", "SR_Large_Explosion_Fallback", 0.95f, 38f, 0.35f, 0.26f, 0.38f);
        sandRunnerClips[SandRunnerSound.NuclearExplosion] = CreateProceduralClip("SR_Nuclear_Explosion", 1.8f, 22f, 0.40f, 0.30f, 0.25f);

        sandRunnerClips[SandRunnerSound.Construction] = CreateProceduralClip("SR_Solar_Construction", 0.28f, 420f, 0.12f, 0.06f, 0.9f);
        sandRunnerClips[SandRunnerSound.ResourceDelivery] = CreateProceduralClip("SR_Resource_Delivery", 0.32f, 520f, 0.10f, 0.03f, 0.9f);

        sandRunnerClips[SandRunnerSound.HarvestStart] = CreateProceduralClip("SR_Harvest_Start", 0.35f, 260f, 0.08f, 0.05f, 0.7f);
        sandRunnerClips[SandRunnerSound.HarvestReturn] = CreateProceduralClip("SR_Harvest_Return", 0.40f, 220f, 0.10f, 0.04f, 0.65f);

        sandRunnerClips[SandRunnerSound.PyramidDamage] = CreateProceduralClip("SR_Pyramid_Damage", 0.62f, 72f, 0.32f, 0.20f, 0.45f);
        sandRunnerClips[SandRunnerSound.Pyralert] = CreateProceduralClip("SR_Pyralert", 0.75f, 48f, 0.28f, 0.15f, 0.35f);

        sandRunnerClips[SandRunnerSound.GustavWarning] = CreateProceduralClip("SR_Gustav_Warning", 0.55f, 96f, 0.24f, 0.12f, 0.55f);

        sandRunnerClips[SandRunnerSound.VictoryFanfare] = CreateProceduralClip("SR_Victory_Fanfare", 1.6f, 520f, 0.28f, 0.04f, 0.6f);
        sandRunnerClips[SandRunnerSound.DefeatSting] = CreateProceduralClip("SR_Defeat_Sting", 1.2f, 120f, 0.32f, 0.08f, 0.4f);
        sandRunnerClips[SandRunnerSound.WaveAlert] = CreateProceduralClip("SR_Wave_Alert", 0.65f, 280f, 0.22f, 0.06f, 0.7f);
        sandRunnerClips[SandRunnerSound.EnemySpotted] = CreateProceduralClip("SR_Enemy_Spotted", 0.35f, 380f, 0.16f, 0.04f, 0.9f);
        sandRunnerClips[SandRunnerSound.ThreatDetected] = CreateProceduralClip("SR_Threat_Detected", 0.50f, 160f, 0.26f, 0.10f, 0.55f);
        sandRunnerClips[SandRunnerSound.BuildingComplete] = CreateProceduralClip("SR_Building_Complete", 0.45f, 440f, 0.20f, 0.03f, 0.8f);

        BuildSandRunnerSoundDefinitions();
        sandRunnerAudioReady = true;
    }

    private void UpdateSandRunnersAudio(float dt)
    {
        if (!sandRunnerAudioReady)
            InitializeSandRunnersAudio();

        if (sandRunnerEngineSource != null)
        {
            float engineLoad = Mathf.SmoothStep(0f, 1f, pyramidThrottleBlend);
            float combatDuck = CountActiveSandRunnerVoicesForMonitor() > 7 ? 0.68f : 1f;
            sandRunnerEngineSource.volume = Mathf.Lerp(0f, 0.34f, engineLoad) * sandRunnerAudioMaster * combatDuck;
            sandRunnerEngineSource.pitch = Mathf.Lerp(0.72f, 1.06f, engineLoad);
        }

        if (sandRunnerAmbienceSource != null)
        {
            float ambienceTarget = 0.14f * sandRunnerAmbienceVolume * sandRunnerAudioMaster;
            sandRunnerAmbienceSource.volume = Mathf.MoveTowards(
                sandRunnerAmbienceSource.volume,
                ambienceTarget,
                dt * 0.35f);
        }

        if (sandRunnerWarningTimer > 0f)
            sandRunnerWarningTimer -= dt;
        if (sandRunnerRadioDuckTimer > 0f)
            sandRunnerRadioDuckTimer -= dt;

        UpdateSandRunnersAircraftAudio(dt);
    }

    private void UpdateSandRunnersAircraftAudio(float dt)
    {
        List<Transform> stale = null;
        foreach (KeyValuePair<Transform, AudioSource> pair in sandRunnerAircraftEngineSources)
        {
            if (pair.Key == null || pair.Value == null)
            {
                if (stale == null)
                    stale = new List<Transform>();
                stale.Add(pair.Key);
                continue;
            }
            pair.Value.volume = Mathf.MoveTowards(pair.Value.volume, 0f, dt * 0.12f);
            if (pair.Value.volume <= 0.001f)
                pair.Value.Stop();
        }
        if (stale != null)
        {
            for (int i = 0; i < stale.Count; i++)
                sandRunnerAircraftEngineSources.Remove(stale[i]);
        }

        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit unit = runners[i];
            if (unit == null || unit.transform == null || !unit.airborne)
                continue;

            if (!sandRunnerAircraftEngineSources.TryGetValue(unit.transform, out AudioSource source) || source == null)
            {
                source = unit.transform.gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 5f;
                source.maxDistance = 180f;
                source.loop = true;
                source.playOnAwake = false;
                source.clip = sandRunnerAircraftEngineClip;
                sandRunnerAircraftEngineSources[unit.transform] = source;
            }

            source.volume = Mathf.MoveTowards(source.volume, 0.055f * sandRunnerAudioMaster, dt * 0.18f);
            source.pitch = Mathf.Lerp(0.86f, 1.24f, Mathf.Clamp01(unit.speed / 24f));
            if (!source.isPlaying && source.clip != null)
                source.Play();
        }
    }

    private void BuildSandRunnerSoundDefinitions()
    {
        sandRunnerSoundDefinitions.Clear();
        foreach (KeyValuePair<SandRunnerSound, AudioClip> pair in sandRunnerClips)
        {
            SandRunnerSoundDefinition definition = new SandRunnerSoundDefinition();
            definition.clips = new[] { pair.Value };
            definition.bus = GetSandRunnerAudioBus(pair.Key);
            definition.cooldown = GetSandRunnerSoundCooldown(pair.Key);
            definition.spatialMode = definition.bus == SandRunnerAudioBus.UI ||
                                     definition.bus == SandRunnerAudioBus.Warnings ||
                                     definition.bus == SandRunnerAudioBus.Radio
                ? SandRunnerSpatialMode.Global
                : definition.bus == SandRunnerAudioBus.Combat
                    ? SandRunnerSpatialMode.CombatStrategic
                    : SandRunnerSpatialMode.WorldClose;
            definition.priority = definition.spatialMode == SandRunnerSpatialMode.Global ? 32 :
                                  definition.spatialMode == SandRunnerSpatialMode.CombatStrategic ? 72 : 128;
            definition.maxDistance = definition.spatialMode == SandRunnerSpatialMode.CombatStrategic ? 720f : 260f;
            definition.concurrency = definition.bus == SandRunnerAudioBus.Combat ? 10 : 4;
            definition.fallback = pair.Value == null || !sandRunnerImportedClips.Contains(pair.Value);
            sandRunnerSoundDefinitions[pair.Key] = definition;
        }

        ConfigureSandRunnerSound(SandRunnerSound.Autocannon, 0.9f, 0.94f, 1.07f, 0.055f, 18f, 760f, 12, 58);
        ConfigureSandRunnerSound(SandRunnerSound.HeavyAutocannon, 1f, 0.84f, 0.95f, 0.09f, 22f, 820f, 8, 52);
        ConfigureSandRunnerSound(SandRunnerSound.ArtilleryFire, 1f, 0.78f, 0.9f, 0.12f, 28f, 1000f, 6, 35);
        ConfigureSandRunnerSound(SandRunnerSound.HeavyArtillery, 1f, 0.72f, 0.84f, 0.18f, 32f, 1200f, 5, 28);
        ConfigureSandRunnerSound(SandRunnerSound.Explosion, 0.9f, 0.9f, 1.05f, 0.08f, 18f, 760f, 10, 62);
        ConfigureSandRunnerSound(SandRunnerSound.LargeExplosion, 1f, 0.84f, 1f, 0.16f, 30f, 1200f, 7, 34);
        ConfigureSandRunnerSound(SandRunnerSound.NuclearExplosion, 1f, 0.78f, 0.9f, 0.8f, 45f, 1800f, 2, 16);
        ConfigureSandRunnerSound(SandRunnerSound.BulletImpact, 0.55f, 0.92f, 1.1f, 0.06f, 8f, 520f, 12, 110);
        ConfigureSandRunnerSound(SandRunnerSound.HitImpact, 0.7f, 0.88f, 1.06f, 0.08f, 12f, 620f, 10, 92);
        ConfigureSandRunnerSound(SandRunnerSound.HeavyImpact, 0.9f, 0.82f, 1f, 0.14f, 20f, 900f, 7, 50);
        ConfigureSandRunnerSound(SandRunnerSound.MissileLaunch, 0.9f, 0.9f, 1.05f, 0.1f, 18f, 820f, 8, 48);
        ConfigureSandRunnerSound(SandRunnerSound.EnemyShot, 0.72f, 0.9f, 1.08f, 0.075f, 16f, 680f, 10, 90);
    }

    private void ConfigureSandRunnerSound(SandRunnerSound sound, float volume, float pitchMin, float pitchMax,
        float cooldown, float minDistance, float maxDistance, int concurrency, int priority)
    {
        if (!sandRunnerSoundDefinitions.TryGetValue(sound, out SandRunnerSoundDefinition definition))
            return;
        definition.baseVolume = volume;
        definition.pitchMin = pitchMin;
        definition.pitchMax = pitchMax;
        definition.cooldown = cooldown;
        definition.minDistance = minDistance;
        definition.maxDistance = maxDistance;
        definition.concurrency = concurrency;
        definition.priority = priority;
    }

    private void PlaySandRunnerSound(SandRunnerSound sound, Vector3 position, float volume = 1f)
    {
        if (!sandRunnerAudioReady)
            InitializeSandRunnersAudio();

        if (!sandRunnerSoundDefinitions.TryGetValue(sound, out SandRunnerSoundDefinition definition) ||
            definition.clips == null || definition.clips.Length == 0)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:no-definition", position, 0f, null);
            return;
        }

        if (sandRunnerAudioMaster <= 0.001f)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:effects-muted", position, 0f, null);
            return;
        }

        float now = Time.unscaledTime;
        string cooldownKey = GetSandRunnerCooldownKey(sound, definition, position);
        if (definition.cooldown > 0f &&
            sandRunnerSoundCellLastPlayed.TryGetValue(cooldownKey, out float lastPlayed) &&
            now - lastPlayed < definition.cooldown)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:cell-cooldown", position, 0f, null);
            return;
        }

        int activeSameSound = CountActiveSandRunnerVoices(definition);
        if (activeSameSound >= definition.concurrency)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:concurrency", position, 0f, null);
            return;
        }

        AudioClip clip = definition.clips[Random.Range(0, definition.clips.Length)];
        if (clip == null)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:null-clip", position, 0f, null);
            return;
        }

        sandRunnerSoundCellLastPlayed[cooldownKey] = now;
        float busVolume = GetSandRunnerBusVolume(definition.bus);
        float finalVolume = Mathf.Clamp01(volume * definition.baseVolume * sandRunnerAudioMaster * busVolume);

        if (definition.spatialMode == SandRunnerSpatialMode.Global)
        {
            AudioSource globalSource = definition.bus == SandRunnerAudioBus.Warnings ? sandRunnerWarningSource : sandRunnerUiSource;
            if (globalSource != null)
            {
                globalSource.pitch = Random.Range(definition.pitchMin, definition.pitchMax);
                globalSource.PlayOneShot(clip, finalVolume);
                RecordSandRunnerAudioMonitor(sound, definition.fallback ? "played:global-fallback" : "played:global", position, finalVolume, clip);
            }
            return;
        }

        AudioSource spatialSource = GetSandRunnerSpatialSource(position, definition);
        if (spatialSource == null)
        {
            RecordSandRunnerAudioMonitor(sound, "suppressed:no-voice", position, 0f, clip);
            return;
        }

        float selectedPitch = Random.Range(definition.pitchMin, definition.pitchMax);
        spatialSource.pitch = selectedPitch;
        spatialSource.PlayOneShot(clip, finalVolume);

        // Guaranteed strategic layer: important combat remains audible regardless of RTS camera height.
        if (definition.spatialMode == SandRunnerSpatialMode.CombatStrategic && sandRunnerCombatSource != null)
        {
            sandRunnerCombatSource.pitch = selectedPitch;
            sandRunnerCombatSource.PlayOneShot(clip, Mathf.Clamp01(finalVolume * 0.72f));
        }

        RecordSandRunnerAudioMonitor(sound, definition.fallback ? "played:spatial+strategic-fallback" : "played:spatial+strategic", position, finalVolume, clip);
    }

    private string GetSandRunnerCooldownKey(SandRunnerSound sound, SandRunnerSoundDefinition definition, Vector3 position)
    {
        if (definition.spatialMode == SandRunnerSpatialMode.Global)
            return sound.ToString();
        float cell = Mathf.Max(8f, definition.cellSize);
        int x = Mathf.FloorToInt(position.x / cell);
        int z = Mathf.FloorToInt(position.z / cell);
        return sound + ":" + x + ":" + z;
    }

    private int CountActiveSandRunnerVoices(SandRunnerSoundDefinition definition)
    {
        int count = 0;
        for (int i = 0; i < sandRunnerSpatialPool.Count; i++)
        {
            AudioSource source = sandRunnerSpatialPool[i];
            if (source != null && source.isPlaying && sandRunnerSpatialPriorities.TryGetValue(source, out int priority) && priority == definition.priority)
                count++;
        }
        return count;
    }

    private void RecordSandRunnerAudioMonitor(SandRunnerSound sound, string result, Vector3 position, float volume, AudioClip clip)
    {
        string line = Time.unscaledTime.ToString("0000.00") + " | " + sound + " | " + result +
                      " | clip=" + (clip != null ? clip.name : "<none>") +
                      " | vol=" + volume.ToString("0.00") +
                      " | pos=" + position.ToString("F0") +
                      " | voices=" + CountActiveSandRunnerVoicesForMonitor();
        sandRunnerAudioMonitor.Enqueue(line);
        while (sandRunnerAudioMonitor.Count > SandRunnerAudioMonitorCapacity)
            sandRunnerAudioMonitor.Dequeue();
    }

    private int CountActiveSandRunnerVoicesForMonitor()
    {
        int active = 0;
        for (int i = 0; i < sandRunnerSpatialPool.Count; i++)
            if (sandRunnerSpatialPool[i] != null && sandRunnerSpatialPool[i].isPlaying)
                active++;
        return active;
    }

    public string GetSandRunnerAudioMonitorReport()
    {
        return string.Join("\n", sandRunnerAudioMonitor.ToArray());
    }

    public string RunSandRunnerAudioSmokeTest()
    {
        if (!sandRunnerAudioReady)
            InitializeSandRunnersAudio();
        int real = 0;
        int fallback = 0;
        int missing = 0;
        foreach (SandRunnerSound sound in System.Enum.GetValues(typeof(SandRunnerSound)))
        {
            if (!sandRunnerSoundDefinitions.TryGetValue(sound, out SandRunnerSoundDefinition definition) ||
                definition.clips == null || definition.clips.Length == 0 || definition.clips[0] == null)
            {
                missing++;
                continue;
            }
            if (definition.fallback)
                fallback++;
            else
                real++;
        }
        return "Audio smoke: real=" + real + " fallback=" + fallback + " missing=" + missing +
               " voices=" + sandRunnerSpatialPool.Count + " independentRoots=" + CountIndependentSandRunnerVoiceRoots();
    }

    private int CountIndependentSandRunnerVoiceRoots()
    {
        HashSet<Transform> roots = new HashSet<Transform>();
        for (int i = 0; i < sandRunnerSpatialPool.Count; i++)
            if (sandRunnerSpatialPool[i] != null)
                roots.Add(sandRunnerSpatialPool[i].transform);
        return roots.Count;
    }

    private SandRunnerAudioBus GetSandRunnerAudioBus(SandRunnerSound sound)
    {
        if (sound == SandRunnerSound.UiConfirm || sound == SandRunnerSound.UiButton || sound == SandRunnerSound.UiSelection ||
            sound == SandRunnerSound.ProductionComplete || sound == SandRunnerSound.ResourceDelivery || sound == SandRunnerSound.BuildingComplete)
            return SandRunnerAudioBus.UI;
        if (sound == SandRunnerSound.UiWarning || sound == SandRunnerSound.GustavWarning || sound == SandRunnerSound.WaveAlert ||
            sound == SandRunnerSound.EnemySpotted || sound == SandRunnerSound.ThreatDetected || sound == SandRunnerSound.Pyralert)
            return SandRunnerAudioBus.Warnings;
        if (sound == SandRunnerSound.AircraftTakeoff || sound == SandRunnerSound.AircraftPass || sound == SandRunnerSound.AircraftReturn ||
            sound == SandRunnerSound.AircraftRepair || sound == SandRunnerSound.HangarRelease)
            return SandRunnerAudioBus.Aircraft;
        if (sound == SandRunnerSound.WindGenerator || sound == SandRunnerSound.JuzzherEngine)
            return SandRunnerAudioBus.Ambience;
        return SandRunnerAudioBus.Combat;
    }

    private float GetSandRunnerBusVolume(SandRunnerAudioBus bus)
    {
        switch (bus)
        {
            case SandRunnerAudioBus.Ambience: return 0.72f * sandRunnerAmbienceVolume;
            case SandRunnerAudioBus.Aircraft: return 0.92f;
            case SandRunnerAudioBus.UI: return 0.88f;
            case SandRunnerAudioBus.Warnings: return 1f;
            case SandRunnerAudioBus.Radio: return 0.95f * sandRunnerRadioVolume;
            default: return 0.9f;
        }
    }

    private AudioSource GetSandRunnerSpatialSource(Vector3 position, SandRunnerSoundDefinition definition)
    {
        if (sandRunnerSpatialPool.Count == 0)
            return null;

        AudioSource selected = null;
        int selectedPriority = -1;
        for (int offset = 0; offset < sandRunnerSpatialPool.Count; offset++)
        {
            int index = (sandRunnerSpatialPoolCursor + offset) % sandRunnerSpatialPool.Count;
            AudioSource candidate = sandRunnerSpatialPool[index];
            if (candidate == null)
                continue;
            if (!candidate.isPlaying)
            {
                selected = candidate;
                sandRunnerSpatialPoolCursor = (index + 1) % sandRunnerSpatialPool.Count;
                break;
            }

            int candidatePriority = sandRunnerSpatialPriorities.TryGetValue(candidate, out int storedPriority) ? storedPriority : 255;
            if (candidatePriority > definition.priority && candidatePriority > selectedPriority)
            {
                selected = candidate;
                selectedPriority = candidatePriority;
            }
        }

        if (selected == null)
            return null;

        if (selected.isPlaying)
            selected.Stop();
        selected.transform.position = position;
        selected.spatialBlend = definition.spatialMode == SandRunnerSpatialMode.CombatStrategic ? 0.48f : 1f;
        selected.rolloffMode = definition.spatialMode == SandRunnerSpatialMode.CombatStrategic
            ? AudioRolloffMode.Logarithmic
            : AudioRolloffMode.Linear;
        selected.minDistance = definition.minDistance;
        selected.maxDistance = definition.maxDistance;
        selected.priority = definition.priority;
        sandRunnerSpatialPriorities[selected] = definition.priority;
        return selected;
    }

    private float GetSandRunnerPitchVariation(SandRunnerSound sound)
    {
        switch (sound)
        {
            case SandRunnerSound.Autocannon:
            case SandRunnerSound.EnemyShot:
                return Random.Range(0.94f, 1.07f);
            case SandRunnerSound.HeavyAutocannon:
                return Random.Range(0.86f, 0.96f);
            case SandRunnerSound.ArtilleryFire:
            case SandRunnerSound.HeavyArtillery:
                return Random.Range(0.78f, 0.90f);
            case SandRunnerSound.Explosion:
            case SandRunnerSound.LargeExplosion:
                return Random.Range(0.88f, 1.04f);
            default:
                return 1f;
        }
    }

    private float GetSandRunnerSoundCooldown(SandRunnerSound sound)
    {
        switch (sound)
        {
            case SandRunnerSound.Autocannon:
                return 0.07f;
            case SandRunnerSound.HeavyAutocannon:
                return 0.12f;
            case SandRunnerSound.BulletImpact:
                return 0.08f;
            case SandRunnerSound.HitImpact:
                return 0.12f;
            case SandRunnerSound.HeavyImpact:
                return 0.18f;
            case SandRunnerSound.EnemyShot:
                return 0.12f;
            case SandRunnerSound.SolarBeam:
            case SandRunnerSound.LaserBeam:
                return 0.12f;
            case SandRunnerSound.Explosion:
                return 0.15f;
            case SandRunnerSound.LargeExplosion:
                return 0.3f;
            case SandRunnerSound.NuclearExplosion:
                return 1.0f;
            case SandRunnerSound.ArtilleryShell:
                return 0.1f;
            default:
                return 0.02f;
        }
    }

    private AudioClip LoadSandRunnerClipOrFallback(string resourcePath, string fallbackName, float duration, float baseFrequency, float volume, float noiseAmount, float decay)
    {
        AudioClip importedClip = Resources.Load<AudioClip>(resourcePath);
        return importedClip != null
            ? importedClip
            : CreateProceduralClip(fallbackName, duration, baseFrequency, volume, noiseAmount, decay);
    }

    private AudioClip CreateProceduralClip(string name, float duration, float baseFrequency, float volume, float noiseAmount, float decay)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float previous = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float life = sampleCount > 1 ? i / (float)(sampleCount - 1) : 1f;
            float attackTime = Mathf.Min(0.08f, Mathf.Max(0.01f, duration * 0.28f));
            float attack = Mathf.Clamp01(t / attackTime);
            float release = Mathf.Pow(1f - life, decay);
            float envelope = attack * release;
            float tone = Mathf.Sin(t * baseFrequency * Mathf.PI * 2f);
            float overtone = Mathf.Sin(t * baseFrequency * 1.93f * Mathf.PI * 2f) * 0.25f;
            float sub = Mathf.Sin(t * baseFrequency * 0.48f * Mathf.PI * 2f) * 0.18f;
            float fm = Mathf.Sin(t * baseFrequency * 0.12f * Mathf.PI * 2f + Mathf.Sin(t * baseFrequency * 0.07f * Mathf.PI * 2f) * 0.3f);
            float noise = Frac(Mathf.Sin(i * 12.9898f + baseFrequency) * 43758.5453f) * 2f - 1f;
            float raw = (tone * 0.55f + overtone + sub * 0.12f + fm * 0.08f) * (1f - noiseAmount) + noise * noiseAmount * 0.35f;
            previous = Mathf.Lerp(previous, Mathf.Clamp(raw, -1f, 1f), 0.62f);
            samples[i] = previous * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private float Frac(float value)
    {
        return value - Mathf.Floor(value);
    }
}