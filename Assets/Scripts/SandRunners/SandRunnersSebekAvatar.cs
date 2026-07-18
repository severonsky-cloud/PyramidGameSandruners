using UnityEngine;

public partial class SandRunnersPrototype
{
    private const string SebekWalkingResource = "SandRunners/Models/Sebek/Sebek_Walking";
    private const string SebekRunningResource = "SandRunners/Models/Sebek/Sebek_Running";
    private const float SebekIdleWalkThreshold = 0.8f;
    private const float SebekWalkRunThreshold = 5.2f;
    private const float SebekCrossfadeDuration = 0.28f;

    private enum SebekAnimState { Idle, Walk, Run }

    private Transform sebekAvatarRoot;
    private Animation sebekAvatarAnimation;
    private AnimationClip sebekIdleClip;
    private AnimationClip sebekWalkClip;
    private AnimationClip sebekRunClip;
    private SebekAnimState sebekAnimState = SebekAnimState.Idle;
    private string sebekCurrentClipName;
    private float sebekAvatarRetryTimer;
    private float sebekIdleBreathCycle;
    private float sebekAnimSpeedSmoothing;
    private float sebekAnimSpeedCurrent;

    private void InitializeSebekAvatar()
    {
        sebekAvatarRetryTimer = 0f;
        TryLoadSebekAvatar();
    }

    private void UpdateSebekAvatar(float dt)
    {
        if (battlePyramid == null)
            return;

        if (sebekAvatarRoot == null)
        {
            sebekAvatarRetryTimer -= dt;
            if (sebekAvatarRetryTimer <= 0f)
            {
                sebekAvatarRetryTimer = 3f;
                TryLoadSebekAvatar();
            }
            return;
        }

        sebekAvatarRoot.localPosition = new Vector3(0f, 2.76f, -1.18f);
        sebekAvatarRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        sebekAvatarRoot.localScale = Vector3.one * 0.32f;

        float speed = pyramidVelocity.magnitude;
        SebekAnimState desiredState = speed < SebekIdleWalkThreshold
            ? SebekAnimState.Idle
            : speed < SebekWalkRunThreshold
                ? SebekAnimState.Walk
                : SebekAnimState.Run;

        PlaySebekState(desiredState, dt);

        if (desiredState == SebekAnimState.Idle)
        {
            sebekIdleBreathCycle += dt * 1.6f;
            float bob = Mathf.Sin(sebekIdleBreathCycle) * 0.012f;
            sebekAvatarRoot.localPosition += Vector3.up * bob;
        }
    }

    private void PlaySebekState(SebekAnimState state, float dt)
    {
        if (sebekAvatarAnimation == null)
            return;

        AnimationClip clip = state == SebekAnimState.Run
            ? sebekRunClip
            : state == SebekAnimState.Walk
                ? sebekWalkClip
                : sebekIdleClip;

        if (clip == null)
            clip = sebekWalkClip;

        if (clip == null)
            return;

        float targetSpeed = state == SebekAnimState.Idle ? 0f : state == SebekAnimState.Run ? 1.15f : 1f;
        sebekAnimSpeedSmoothing = Mathf.Lerp(sebekAnimSpeedSmoothing, targetSpeed, dt * (state == SebekAnimState.Idle ? 3.5f : 5f));
        sebekAnimSpeedCurrent = Mathf.MoveTowards(sebekAnimSpeedCurrent, sebekAnimSpeedSmoothing, dt * 6f);

        if (sebekCurrentClipName != clip.name)
        {
            sebekAvatarAnimation.CrossFade(clip.name, SebekCrossfadeDuration);
            sebekCurrentClipName = clip.name;
            sebekAnimState = state;
        }

        AnimationState animState = sebekAvatarAnimation[clip.name];
        if (animState != null)
        {
            animState.enabled = true;
            animState.wrapMode = WrapMode.Loop;
            animState.weight = 1f;
            animState.speed = sebekAnimSpeedCurrent;

            if (state == SebekAnimState.Idle)
                animState.time = Mathf.Repeat(animState.time, Mathf.Max(0.01f, clip.length));
        }
    }

    private void TryLoadSebekAvatar()
    {
        if (battlePyramid == null || sebekAvatarRoot != null)
            return;

        GameObject sebekPrefab = Resources.Load<GameObject>(SebekWalkingResource);
        if (sebekPrefab == null)
            return;

        GameObject sebekObject = Instantiate(sebekPrefab, battlePyramid);
        sebekObject.name = "Sebek_Nu_Ankha_Avatar";
        sebekAvatarRoot = sebekObject.transform;
        sebekAvatarRoot.localPosition = new Vector3(0f, 2.76f, -1.18f);
        sebekAvatarRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        sebekAvatarRoot.localScale = Vector3.one * 0.32f;
        DisableSebekRuntimeColliders(sebekAvatarRoot);
        ConfigureSebekAnimation(sebekObject);
    }

    private void ConfigureSebekAnimation(GameObject sebekObject)
    {
        sebekAvatarAnimation = sebekObject.GetComponent<Animation>();
        if (sebekAvatarAnimation == null)
            sebekAvatarAnimation = sebekObject.AddComponent<Animation>();

        sebekAvatarAnimation.playAutomatically = false;
        sebekAvatarAnimation.cullingType = AnimationCullingType.AlwaysAnimate;

        sebekIdleClip = FindSebekAnimationClip(SebekWalkingResource);
        sebekWalkClip = FindSebekAnimationClip(SebekWalkingResource);
        sebekRunClip = FindSebekAnimationClip(SebekRunningResource);

        AddSebekClip(sebekIdleClip);
        AddSebekClip(sebekWalkClip);
        AddSebekClip(sebekRunClip);

        AnimationClip firstClip = sebekIdleClip != null ? sebekIdleClip : sebekWalkClip;
        if (firstClip == null)
            return;

        sebekAvatarAnimation.clip = firstClip;
        sebekAvatarAnimation.wrapMode = WrapMode.Loop;
        sebekAvatarAnimation.Play(firstClip.name);
        sebekCurrentClipName = firstClip.name;
        sebekAnimState = SebekAnimState.Idle;
    }

    private AnimationClip FindSebekAnimationClip(string resourcePath)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath);
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || clip.name.StartsWith("__preview__"))
                continue;

            clip.legacy = true;
            clip.wrapMode = WrapMode.Loop;
            return clip;
        }
        return null;
    }

    private void AddSebekClip(AnimationClip clip)
    {
        if (sebekAvatarAnimation == null || clip == null)
            return;

        sebekAvatarAnimation.AddClip(clip, clip.name);
    }

    private void DisableSebekRuntimeColliders(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}
