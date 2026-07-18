using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandRunnersResourceMineArtAdapter : MonoBehaviour
{
    public enum ArtState { Idle, Extracting, Depleted }

    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float extractionInput;
    private Transform[] drills;
    private Light signalLight;
    private float clock;
    private bool cached;

    public ArtState State => state;
    public void Configure(Transform[] drillTransforms, Light light)
    {
        drills = drillTransforms;
        signalLight = light;
        cached = true;
    }

    public void SetArtState(ArtState nextState) => state = nextState;
    public void SetExtractionInput(float value) => extractionInput = Mathf.Clamp01(value);

    private void Awake() { if (!cached) CacheReferences(); }
    private void OnEnable() { if (!cached) CacheReferences(); }

    private void CacheReferences()
    {
        cached = true;
        var list = new System.Collections.Generic.List<Transform>();
        var all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string key = all[i].name.ToLowerInvariant();
            if (key.Contains("mine_drill") || key.Contains("mine_rotor")) list.Add(all[i]);
            if (key == "mine_signal_light") signalLight = all[i].GetComponent<Light>();
        }
        drills = list.ToArray();
    }

    private void Update()
    {
        if (!cached) CacheReferences();
        clock += Mathf.Min(Time.deltaTime, 0.1f);
        float active = state == ArtState.Extracting ? extractionInput : 0f;
        float spin = 540f * active * Time.deltaTime;
        if (drills != null)
            for (int i = 0; i < drills.Length; i++)
                if (drills[i] != null) drills[i].Rotate(Vector3.up, spin, Space.Self);
        if (signalLight != null)
            signalLight.intensity = state == ArtState.Depleted ? 0.08f : 0.7f + Mathf.Sin(clock * 3f) * 0.18f + active * 0.9f;
    }
}
