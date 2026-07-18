using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandRunnersApexArtAdapter : MonoBehaviour
{
    public enum ArtState { Idle, Charging, Firing, Recovery }

    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float chargeInput;
    [SerializeField, Range(0f, 1f)] private float fireInput;
    [SerializeField] private Transform focusPoint;
    [SerializeField] private Light focusLight;
    [SerializeField] private LineRenderer solarBeam;
    [SerializeField] private ParticleSystem chargeFlow;
    [SerializeField] private AudioSource chargeAudio;
    [SerializeField] private AudioSource fireAudio;
    [SerializeField] private AudioSource recoveryAudio;

    private float clock;

    public ArtState State => state;
    public void Configure(Transform focus, Light light, LineRenderer beam, ParticleSystem flow, AudioSource charge, AudioSource fire, AudioSource recovery)
    {
        focusPoint = focus;
        focusLight = light;
        solarBeam = beam;
        chargeFlow = flow;
        chargeAudio = charge;
        fireAudio = fire;
        recoveryAudio = recovery;
    }

    public void SetArtState(ArtState nextState) => state = nextState;
    public void SetChargeInput(float value) => chargeInput = Mathf.Clamp01(value);
    public void SetFireInput(float value) => fireInput = Mathf.Clamp01(value);
    public void PlayChargeSound() { if (chargeAudio != null) chargeAudio.Play(); }
    public void PlayFireSound() { if (fireAudio != null) fireAudio.Play(); }
    public void PlayRecoverySound() { if (recoveryAudio != null) recoveryAudio.Play(); }

    private void Update()
    {
        clock += Mathf.Min(Time.deltaTime, 0.1f);
        float charge = state == ArtState.Charging ? chargeInput : 0f;
        float fire = state == ArtState.Firing ? fireInput : 0f;
        float recovery = state == ArtState.Recovery ? 1f : 0f;

        if (focusPoint != null)
        {
            float pulse = 1f + Mathf.Sin(clock * 5.5f) * 0.08f + charge * 0.24f + fire * 0.35f;
            focusPoint.localScale = Vector3.one * pulse;
        }

        if (focusLight != null)
            focusLight.intensity = 0.35f + charge * 2.2f + fire * 3.2f - recovery * 0.12f;

        if (solarBeam != null)
        {
            bool visible = charge > 0.01f || fire > 0.01f;
            solarBeam.enabled = visible;
            float width = Mathf.Lerp(0.02f, 0.2f, Mathf.Max(charge, fire));
            solarBeam.startWidth = width;
            solarBeam.endWidth = width * 0.34f;
        }

        if (chargeFlow != null)
        {
            var emission = chargeFlow.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 80f, charge);
            if (charge > 0.01f && !chargeFlow.isPlaying) chargeFlow.Play();
            if (charge <= 0.01f && chargeFlow.isPlaying) chargeFlow.Stop();
        }
    }
}