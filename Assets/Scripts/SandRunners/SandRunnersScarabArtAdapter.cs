using System.Collections.Generic;
using UnityEngine;

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
    private Transform cargoEmpty;
    private Transform cargoLoaded;
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
        cargoEmpty = null;
        cargoLoaded = null;
        howitzerRecoilAnchor = null;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            string name = candidate.name.ToLowerInvariant();
            if (name.Contains("trackwheel") || name.Contains("track_roller") || name.Contains("wheel"))
                wheelAnchors.Add(candidate);
            if (name.Contains("salvage_arm"))
                salvageArmAnchors.Add(candidate);
            if (name.Contains("walker_leg"))
                walkerLegAnchors.Add(candidate);
            if (name.Contains("carrier_rotor") || name.Contains("drone_rotor"))
                carrierRotorAnchors.Add(candidate);
            if (name == "salvage_cargo_empty_frame")
                cargoEmpty = candidate;
            if (name == "salvage_cargo_loaded")
                cargoLoaded = candidate;
            if (name == "howitzer_recoil_slide")
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
        AnimateSalvageCargo(salvaging);
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
            float phase = i * Mathf.PI * 0.34f;
            float lift = Mathf.Sin(clock * 2.2f + phase) * 8f * amount;
            arm.localRotation = Quaternion.Euler(-8f - amount * 24f, 0f, lift);
        }
    }

    private void AnimateSalvageCargo(float amount)
    {
        if (cargoEmpty != null) cargoEmpty.gameObject.SetActive(amount < 0.45f);
        if (cargoLoaded != null) cargoLoaded.gameObject.SetActive(amount >= 0.45f);
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
        howitzerRecoilAnchor.localPosition = howitzerRestPosition + Vector3.back * (0.32f * pulse);
    }
}
