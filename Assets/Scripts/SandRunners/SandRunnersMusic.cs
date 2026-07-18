using UnityEngine;

public partial class SandRunnersPrototype
{
    private const string MusicIntroPath = "SandRunners/Audio/SR_Title";
    private const string MusicClimax90Path = "SandRunners/Audio/SR_Exploration";
    private const string MusicClimax180Path = "SandRunners/Audio/SR_Crisis";
    private const string MusicVictoryPath = "SandRunners/Audio/SR_Victory";
    private const string MusicEndingPath = "SandRunners/Audio/SR_Ending";

    private AudioSource musicSource;
    private AudioClip musicIntroClip;
    private AudioClip musicClimax90Clip;
    private AudioClip musicClimax180Clip;
    private AudioClip musicVictoryClip;
    private AudioClip musicEndingClip;
    private float musicVolume = 0.35f;
    private float benchmarkMusicVolume = 0.74f;
    private float musicCrossfadeTime = 2.5f;
    private string currentMusicState = "none";
    private string targetMusicState = "none";
    private float musicFadeTimer;
    private bool musicSystemReady;

    private enum MusicTrackId
    {
        None,
        Intro,
        Climax90,
        Climax180,
        Victory,
        Ending
    }

    private MusicTrackId currentPlayingTrack = MusicTrackId.None;
    private MusicTrackId targetTrack = MusicTrackId.None;
    private float musicTransitionTimer;
    private float musicCombatHoldTimer;
    private const float MusicTransitionDuration = 3f;
    private const float MusicCombatReleaseDelay = 6f;

    private void InitializeMusicSystem()
    {
        if (musicSystemReady)
            return;

        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null)
            return;

        musicSource = camera.gameObject.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f;

        musicIntroClip = Resources.Load<AudioClip>(MusicIntroPath);
        musicClimax90Clip = Resources.Load<AudioClip>(MusicClimax90Path);
        musicClimax180Clip = Resources.Load<AudioClip>(MusicClimax180Path);
        musicVictoryClip = Resources.Load<AudioClip>(MusicVictoryPath);
        musicEndingClip = Resources.Load<AudioClip>(MusicEndingPath);

        if (musicIntroClip == null)
            musicIntroClip = GenerateMusicFallbackClip("Music_Intro_Fallback", 45f, 220f, 0.08f);
        if (musicClimax90Clip == null)
            musicClimax90Clip = GenerateMusicFallbackClip("Music_Climax90_Fallback", 48f, 160f, 0.12f);
        if (musicClimax180Clip == null)
            musicClimax180Clip = GenerateMusicFallbackClip("Music_Climax180_Fallback", 54f, 120f, 0.16f);
        if (musicVictoryClip == null)
            musicVictoryClip = musicIntroClip;
        if (musicEndingClip == null)
            musicEndingClip = musicIntroClip;

        musicSystemReady = true;
        RequestMusicTrack(MusicTrackId.Intro);
    }

    private void UpdateMusicSystem(float dt)
    {
        if (!musicSystemReady)
        {
            InitializeMusicSystem();
            return;
        }

        if (cinematicDirectorActive || sandRunnersIntroActive)
        {
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                musicSource.volume = 0f;
            }
            currentPlayingTrack = MusicTrackId.None;
            targetTrack = MusicTrackId.None;
            musicTransitionTimer = 0f;
            return;
        }

        if (targetTrack != currentPlayingTrack)
        {
            musicTransitionTimer += dt;
            float t = Mathf.Clamp01(musicTransitionTimer / MusicTransitionDuration);

            if (currentPlayingTrack == MusicTrackId.None)
            {
                if (!musicSource.isPlaying)
                    SwapMusicTrack(targetTrack);
                musicSource.volume = Mathf.Lerp(0f, GetReleaseMusicVolume(), t);
                if (t >= 1f)
                {
                    currentPlayingTrack = targetTrack;
                    musicTransitionTimer = 0f;
                }
            }
            else
            {
                if (t < 0.5f)
                {
                    float fadeOut = t / 0.5f;
                    musicSource.volume = Mathf.Lerp(GetReleaseMusicVolume(), 0f, fadeOut);
                }
                else
                {
                    if (musicSource.isPlaying && musicSource.volume <= 0.01f)
                        SwapMusicTrack(targetTrack);

                    float fadeIn = (t - 0.5f) / 0.5f;
                    musicSource.volume = Mathf.Lerp(0f, GetReleaseMusicVolume(), fadeIn);
                }

                if (t >= 1f)
                {
                    currentPlayingTrack = targetTrack;
                    musicTransitionTimer = 0f;
                }
            }
        }
        else if (musicSource != null && targetTrack != MusicTrackId.None)
        {
            musicSource.volume = GetReleaseMusicVolume();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0f;
        }
    }

    private void SwapMusicTrack(MusicTrackId track)
    {
        musicSource.Stop();
        musicSource.volume = 0f;
        if (track == MusicTrackId.None)
        {
            musicSource.clip = null;
            currentPlayingTrack = MusicTrackId.None;
            return;
        }

        switch (track)
        {
            case MusicTrackId.Intro:
                musicSource.clip = musicIntroClip;
                break;
            case MusicTrackId.Climax90:
                musicSource.clip = musicClimax90Clip;
                break;
            case MusicTrackId.Climax180:
                musicSource.clip = musicClimax180Clip;
                break;
            case MusicTrackId.Victory:
                musicSource.clip = musicVictoryClip;
                break;
            case MusicTrackId.Ending:
                musicSource.clip = musicEndingClip;
                break;
        }
        if (musicSource.clip != null)
            musicSource.Play();
    }

    private void RequestMusicTrack(MusicTrackId track)
    {
        if (track == targetTrack)
            return;
        targetTrack = track;
        musicTransitionTimer = 0f;
    }

    private void OnGameFlowMusicChanged(SandRunnersGameFlowState newState)
    {
        switch (newState)
        {
            case SandRunnersGameFlowState.MainMenu:
                RequestMusicTrack(MusicTrackId.Intro);
                break;
            case SandRunnersGameFlowState.Playing:
                musicCombatHoldTimer = 0f;
                RequestMusicTrack(MusicTrackId.Climax90);
                break;
            case SandRunnersGameFlowState.Paused:
                break;
            case SandRunnersGameFlowState.Victory:
                RequestMusicTrack(MusicTrackId.Victory);
                break;
            case SandRunnersGameFlowState.Defeat:
                RequestMusicTrack(MusicTrackId.Ending);
                break;
        }
    }

    private void SetMusicIntensity(bool intense, float dt)
    {
        if (intense)
        {
            musicCombatHoldTimer = MusicCombatReleaseDelay;
            RequestMusicTrack(MusicTrackId.Climax180);
            return;
        }

        musicCombatHoldTimer = Mathf.Max(0f, musicCombatHoldTimer - dt);
        if (musicCombatHoldTimer <= 0f &&
            (currentPlayingTrack == MusicTrackId.Climax180 || targetTrack == MusicTrackId.Climax180))
        {
            RequestMusicTrack(MusicTrackId.Climax90);
        }
    }

    private float GetReleaseMusicVolume()
    {
        return musicVolume * (sandRunnerRadioDuckTimer > 0f ? 0.58f : 1f);
    }

    private float GetBenchmarkMusicVolume()
    {
        return benchmarkMusicVolume;
    }

    private void ApplyBenchmarkMusicVolume()
    {
        if (cinematicDirectorMusicSource != null)
            cinematicDirectorMusicSource.volume = cinematicDirectorMusicSource.isPlaying
                ? 0.74f * benchmarkMusicVolume
                : 0f;
    }

    private void StopRegularMusicForBenchmark()
    {
        RequestMusicTrack(MusicTrackId.None);
        currentPlayingTrack = MusicTrackId.None;
        musicTransitionTimer = 0f;
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0f;
        }
    }

    private void ResumeRegularMusicAfterBenchmark()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0f;
        }
        currentPlayingTrack = MusicTrackId.None;
        RequestMusicTrack(MusicTrackId.Climax90);
    }

    private AudioClip GenerateMusicFallbackClip(string name, float duration, float freq, float noiseAmt)
    {
        int sampleRate = 22050;
        int samples = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float life = samples > 1 ? i / (float)(samples - 1) : 1f;
            float envelope = Mathf.Sin(life * Mathf.PI) * (1f - life * 0.3f);
            float tone = Mathf.Sin(t * freq * Mathf.PI * 2f) * 0.4f;
            float bass = Mathf.Sin(t * freq * 0.25f * Mathf.PI * 2f) * 0.3f;
            float pad = Mathf.Sin(t * freq * 1.5f * Mathf.PI * 2f + Mathf.Sin(t * 0.5f) * 2f) * 0.2f;
            float noise = (Mathf.Sin(i * 12.9898f) * 43758.5453f - Mathf.Floor(Mathf.Sin(i * 12.9898f) * 43758.5453f)) * 2f - 1f;
            data[i] = (tone + bass + pad + noise * noiseAmt * 0.2f) * envelope * 0.5f;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}