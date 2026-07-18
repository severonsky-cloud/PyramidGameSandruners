using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandRunnersSettlementArtAdapter : MonoBehaviour
{
    public enum ArtState { Neutral, Trading, Allied, UnderAttack }

    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float activityInput;
    private Transform beacon;
    private Light signalLight;
    private float clock;
    private bool cached;

    public ArtState State => state;
    public void Configure(Transform beaconTransform, Light light)
    {
        beacon = beaconTransform;
        signalLight = light;
        cached = true;
    }

    public void SetArtState(ArtState nextState) => state = nextState;
    public void SetActivityInput(float value) => activityInput = Mathf.Clamp01(value);

    private void Awake() { if (!cached) CacheReferences(); }
    private void OnEnable() { if (!cached) CacheReferences(); }

    private void CacheReferences()
    {
        cached = true;
        var all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string key = all[i].name.ToLowerInvariant();
            if (key == "settlement_beacon") beacon = all[i];
            if (key == "settlement_signal_light") signalLight = all[i].GetComponent<Light>();
        }
    }

    private void Update()
    {
        if (!cached) CacheReferences();
        clock += Mathf.Min(Time.deltaTime, 0.1f);
        float active = state == ArtState.Trading || state == ArtState.Allied ? activityInput : 0f;
        if (beacon != null) beacon.Rotate(Vector3.up, (16f + active * 80f) * Time.deltaTime, Space.Self);
        if (signalLight != null)
        {
            float pulse = 0.8f + Mathf.Sin(clock * (state == ArtState.UnderAttack ? 7f : 2.1f)) * 0.18f;
            signalLight.intensity = state == ArtState.UnderAttack ? 1.5f + pulse : pulse + active * 0.75f;
        }
    }
}
