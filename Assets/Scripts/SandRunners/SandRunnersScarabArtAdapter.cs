using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual-only adapter for authored Scarab variants. Gameplay owns the root,
/// collider, health and weapons. Art state is driven through explicit inputs.
/// </summary>
[DisallowMultipleComponent]
public sealed class SandRunnersScarabArtAdapter : MonoBehaviour
{
    public enum Variant { Tank, Salvage, Walker, Carrier, Howitzer }
    public enum ArtState { Idle, Moving, Salvaging, Firing, Airborne }

    [SerializeField] private Variant variant;
    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float motionInput;
    [SerializeField, Range(0f, 1f)] private float salvageInput;
    [SerializeField, Range(0f, 1f)] private float fireInput;
    [SerializeField, Range(0f, 1f)] private float altitudeInput;

    private readonly List<Transform> wheelAnchors = new List<Transform>();
    private readonly List<Transform> salvageArmAnchors = new List<Transform>();
    private readonly List<Transform> walkerLegAnchors = new List<Transform>();
    private readonly List<Transform> carrierRotorAnchors = new List<Transform>();
    private Transform howitzerRecoilAnchor;
    private Vector3 howitzerRestPosition;
    private float clock;
    private bool cached;

    public Variant ArtVariant => variant;
    public ArtState State => state;

    public static Variant ResolveVariant(string unitName)
    {
        string key = unitName ?? string.Empty;
        if (key.Contains("Scarab_Tank")) return Variant.Tank;
        if (key.Contains("Salvage_Scarab")) return Variant.Salvage;
        if (key.Contains("Scarab_Walker")) return Variant.Walker;
        if (key.Contains("Scarab_Carrier") || key.Contains("Scarab_Drone")) return Variant.Carrier;
        if (key.Contains("Siege_Scarab") || key.Contains("Scarab_Howitzer")) return Variant.Howitzer;
        return Variant.Tank;
    }

    public void ConfigureVariant(Variant nextVariant) => variant = nextVariant;
    public void SetArtState(ArtState nextState) => state = nextState;
    public void SetMotionInput(float value) => motionInput = Mathf.Clamp01(value);
    public void SetSalvageInput(float value) => salvageInput = Mathf.Clamp01(value);
    public void SetFireInput(float value) => fireInput = Mathf.Clamp01(value);
    public void SetAltitudeInput(float value) => altitudeInput = Mathf.Clamp01(value);

    private void Awake() => CacheAnchors();
    private void OnEnable() { if (!cached) CacheAnchors(); }

    private void CacheAnchors()
    {
        cached = true;
        wheelAnchors.Clear();
        salvageArmAnchors.Clear();
        walkerLegAnchors.Clear();
        carrierRotorAnchors.Clear();
        howitzerRecoilAnchor = null;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            string name = candidate.name.ToLowerInvariant();
            if (name.Contains("trackwheel") || name.Contains("track_roller") || name.Contains("wheel"))
                wheelAnchors.Add(candidate);
            if (name.Contains("salvage_arm") || name.Contains("manipulator"))
                salvageArmAnchors.Add(candidate);
            if (name.Contains("walker_leg"))
                walkerLegAnchors.Add(candidate);
            if (name.Contains("carrier_rotor") || name.Contains("drone_rotor"))
                carrierRotorAnchors.Add(candidate);
            if (name == "muzzle_howitzer_recoil" || name == "howitzer_recoil_slide")
            {
                howitzerRecoilAnchor = candidate;
                howitzerRestPosition = candidate.localPosition;
            }
        }
    }

    private void Update()
    {
        if (!cached) CacheAnchors();
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        clock += dt;
        float moving = state == ArtState.Moving ? motionInput : 0f;
        float salvaging = state == ArtState.Salvaging ? salvageInput : 0f;
        float firing = state == ArtState.Firing ? fireInput : 0f;
        float airborne = state == ArtState.Airborne ? Mathf.Max(altitudeInput, 0.35f) : 0f;

        AnimateWheels(moving, dt);
        AnimateSalvageArms(salvaging);
        AnimateWalkerLegs(moving);
        AnimateCarrierRotors(airborne, dt);
        AnimateHowitzer(firing);
    }

    private void AnimateWheels(float amount, float dt)
    {
        float spin = 520f * amount * dt;
        for (int i = 0; i < wheelAnchors.Count; i++)
            if (wheelAnchors[i] != null) wheelAnchors[i].Rotate(Vector3.right, spin, Space.Self);
    }

    private void AnimateSalvageArms(float amount)
    {
        for (int i = 0; i < salvageArmAnchors.Count; i++)
        {
            Transform arm = salvageArmAnchors[i];
            if (arm == null) continue;
            float side = i % 2 == 0 ? -1f : 1f;
            float wave = Mathf.Sin(clock * 2.2f + i * 0.7f) * 6f * amount;
            arm.localRotation = Quaternion.Euler(-18f - amount * 22f, side * (8f + amount * 18f), wave);
        }
    }

    private void AnimateWalkerLegs(float amount)
    {
        for (int i = 0; i < walkerLegAnchors.Count; i++)
        {
            Transform leg = walkerLegAnchors[i];
            if (leg == null) continue;
            float phase = i % 2 == 0 ? 0f : Mathf.PI;
            float stride = Mathf.Sin(clock * 5.2f + phase + i * 0.2f) * 17f * amount;
            leg.localRotation = Quaternion.Euler(0f, stride, stride * 0.35f);
        }
    }

    private void AnimateCarrierRotors(float amount, float dt)
    {
        float spin = Mathf.Lerp(0f, 1100f, amount) * dt;
        for (int i = 0; i < carrierRotorAnchors.Count; i++)
            if (carrierRotorAnchors[i] != null) carrierRotorAnchors[i].Rotate(Vector3.up, spin, Space.Self);
    }

    private void AnimateHowitzer(float amount)
    {
        if (howitzerRecoilAnchor == null) return;
        float pulse = Mathf.Clamp01(amount) * (0.5f + Mathf.Abs(Mathf.Sin(clock * 18f)) * 0.5f);
        howitzerRecoilAnchor.localPosition = howitzerRestPosition + Vector3.back * (0.22f * pulse);
    }
}