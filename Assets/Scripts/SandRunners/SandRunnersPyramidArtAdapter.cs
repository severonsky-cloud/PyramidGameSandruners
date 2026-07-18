using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandRunnersPyramidArtAdapter : MonoBehaviour
{
    public enum ArtState { Idle, Moving, Charging, Firing, Recovery }

    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float motionInput;
    [SerializeField, Range(0f, 1f)] private float chargeInput;
    [SerializeField, Range(0f, 1f)] private float fireInput;

    private Transform[] wheels;
    private Transform leftDoor;
    private Transform rightDoor;
    private Vector3 leftDoorClosed;
    private Vector3 rightDoorClosed;
    private Light reactorLight;
    private Light apexLight;
    private float clock;
    private bool cached;

    public ArtState State => state;

    public void Configure(Transform[] wheelTransforms, Transform leftDoorTransform, Transform rightDoorTransform, Light reactor, Light apex)
    {
        wheels = wheelTransforms;
        leftDoor = leftDoorTransform;
        rightDoor = rightDoorTransform;
        leftDoorClosed = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
        rightDoorClosed = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
        reactorLight = reactor;
        apexLight = apex;
        cached = true;
    }

    public void SetArtState(ArtState nextState) => state = nextState;
    public void SetMotionInput(float value) => motionInput = Mathf.Clamp01(value);
    public void SetChargeInput(float value) => chargeInput = Mathf.Clamp01(value);
    public void SetFireInput(float value) => fireInput = Mathf.Clamp01(value);

    private void Awake()
    {
        if (!cached) CacheReferences();
    }

    private void OnEnable()
    {
        if (!cached) CacheReferences();
    }

    private void CacheReferences()
    {
        cached = true;
        var all = GetComponentsInChildren<Transform>(true);
        var wheelList = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            string key = all[i].name.ToLowerInvariant();
            if (key.Contains("pyramid_track_wheel") || key.Contains("pyramid_suspension"))
                wheelList.Add(all[i]);
            if (key == "pyramid_door_left")
            {
                leftDoor = all[i];
                leftDoorClosed = leftDoor.localPosition;
            }
            if (key == "pyramid_door_right")
            {
                rightDoor = all[i];
                rightDoorClosed = rightDoor.localPosition;
            }
            if (key == "pyramid_reactor_light") reactorLight = all[i].GetComponent<Light>();
            if (key == "pyramid_apex_light") apexLight = all[i].GetComponent<Light>();
        }
        wheels = wheelList.ToArray();
    }

    private void Update()
    {
        if (!cached) CacheReferences();

        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        clock += dt;
        float moving = state == ArtState.Moving ? motionInput : 0f;
        float charging = state == ArtState.Charging ? chargeInput : 0f;
        float firing = state == ArtState.Firing ? fireInput : 0f;
        float doorAmount = state == ArtState.Firing ? fireInput : 0f;

        float spin = 420f * moving * dt;
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null) wheels[i].Rotate(Vector3.right, spin, Space.Self);
        }

        if (leftDoor != null)
            leftDoor.localPosition = leftDoorClosed + new Vector3(-0.75f, -0.25f, 0f) * doorAmount;
        if (rightDoor != null)
            rightDoor.localPosition = rightDoorClosed + new Vector3(0.75f, -0.25f, 0f) * doorAmount;

        if (reactorLight != null)
            reactorLight.intensity = 0.9f + Mathf.Sin(clock * 2.2f) * 0.12f + charging * 1.35f + firing * 0.55f;
        if (apexLight != null)
            apexLight.intensity = 0.4f + charging * 1.8f + firing * 0.8f;
    }
}