using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandRunnersPyramidArtAdapter : MonoBehaviour
{
    public enum ArtState { Idle, Moving, Charging, Firing, Recovery }

    [SerializeField] private ArtState state;
    [SerializeField, Range(0f, 1f)] private float motionInput;
    [SerializeField, Range(0f, 1f)] private float chargeInput;
    [SerializeField, Range(0f, 1f)] private float fireInput;
    [SerializeField, Range(0f, 1f)] private float suspensionInput;

    private Transform[] wheels;
    private Transform[] suspensions;
    private Transform leftDoor;
    private Transform rightDoor;
    private Vector3 leftDoorClosed;
    private Vector3 rightDoorClosed;
    private Vector3[] suspensionRestPositions;
    private Light reactorLight;
    private Light apexLight;
    private float clock;
    private bool cached;

    public ArtState State => state;

    public void Configure(Transform[] wheelTransforms, Transform leftDoorTransform, Transform rightDoorTransform, Light reactor, Light apex)
    {
        Configure(wheelTransforms, null, leftDoorTransform, rightDoorTransform, reactor, apex);
    }

    public void Configure(
        Transform[] wheelTransforms,
        Transform[] suspensionTransforms,
        Transform leftDoorTransform,
        Transform rightDoorTransform,
        Light reactor,
        Light apex)
    {
        wheels = wheelTransforms;
        suspensions = suspensionTransforms;
        suspensionRestPositions = CachePositions(suspensions);
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
    public void SetSuspensionInput(float value) => suspensionInput = Mathf.Clamp01(value);

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
        var suspensionList = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            string key = all[i].name.ToLowerInvariant();
            if (key.Contains("pyramid_track_wheel") || key.Contains("pyramid_track_roller"))
                wheelList.Add(all[i]);
            if (key.Contains("pyramid_suspension"))
                suspensionList.Add(all[i]);
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
        suspensions = suspensionList.ToArray();
        suspensionRestPositions = CachePositions(suspensions);
    }

    private void Update()
    {
        if (!cached) CacheReferences();

        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        clock += dt;
        float moving = state == ArtState.Moving ? motionInput : 0f;
        float charging = state == ArtState.Charging ? chargeInput : 0f;
        float firing = state == ArtState.Firing ? fireInput : 0f;
        float doors = state == ArtState.Firing ? fireInput : 0f;
        float suspension = Mathf.Max(suspensionInput, moving * 0.45f);

        float spin = 420f * moving * dt;
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null) wheels[i].Rotate(Vector3.right, spin, Space.Self);
        }

        if (suspensions != null && suspensionRestPositions != null)
        {
            for (int i = 0; i < suspensions.Length; i++)
            {
                Transform strut = suspensions[i];
                if (strut == null) continue;
                float phase = i * 0.65f;
                float compression = (0.035f + 0.025f * Mathf.Sin(clock * 4.2f + phase)) * suspension;
                strut.localPosition = suspensionRestPositions[i] + Vector3.up * compression;
                strut.localRotation = Quaternion.Euler(Mathf.Sin(clock * 3.8f + phase) * 3f * suspension, 0f, 0f);
            }
        }

        if (leftDoor != null)
            leftDoor.localPosition = leftDoorClosed + new Vector3(-0.6f, -0.18f, 0f) * doors;
        if (rightDoor != null)
            rightDoor.localPosition = rightDoorClosed + new Vector3(0.6f, -0.18f, 0f) * doors;

        if (reactorLight != null)
            reactorLight.intensity = 0.6f + Mathf.Sin(clock * 2.2f) * 0.08f + charging * 1.1f + firing * 0.4f;
        if (apexLight != null)
            apexLight.intensity = 0.25f + charging * 1.4f + firing * 2f;
    }

    private static Vector3[] CachePositions(Transform[] transforms)
    {
        if (transforms == null) return new Vector3[0];
        Vector3[] result = new Vector3[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
            result[i] = transforms[i] != null ? transforms[i].localPosition : Vector3.zero;
        return result;
    }
}
