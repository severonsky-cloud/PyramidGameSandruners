using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public sealed class SandRunnersCommandBridgeController : MonoBehaviour
{
    public enum InternalState
    {
        Strategic,
        Drive,
        BridgeThirdPerson,
        BridgeVisor,
        BridgeHolomap
    }

    private const string SebekWalkingResource = "SandRunners/Models/Sebek/Sebek_Walking";
    private const string CommandBridgeResource = "SandRunners/CommandBridge/CommandBridge";
    private SandRunnersPrototype prototype;
    private Transform bridgeRoot;
    private Transform sebekRoot;
    private Transform sebekVisual;
    private Animation sebekAnimation;
    private AnimationClip sebekWalkClip;
    private AnimationClip sebekRunClip;
    private string sebekPlayingClip;
    private Camera activeCamera;
    private Canvas bridgeCanvas;
    private Canvas strategicCanvas;
    private Text bridgeStatus;
    private InternalState state = InternalState.Strategic;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedCameraFov = 60f;
    private float yaw;
    private float pitch = 12f;
    private float bridgeCameraDistance = 2.6f;
    private bool initialized;

    public InternalState State => state;
    public bool IsInsideBridge => state == InternalState.BridgeThirdPerson ||
                                  state == InternalState.BridgeVisor ||
                                  state == InternalState.BridgeHolomap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        InstallForCurrentScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForCurrentScene();
    }

    private static void InstallForCurrentScene()
    {
        SandRunnersPrototype target = Object.FindFirstObjectByType<SandRunnersPrototype>();
        if (target != null && target.GetComponent<SandRunnersCommandBridgeController>() == null)
            target.gameObject.AddComponent<SandRunnersCommandBridgeController>();
    }

    private void Awake()
    {
        prototype = GetComponent<SandRunnersPrototype>();
    }

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            if (!initialized)
                return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            if (IsInsideBridge)
            {
                EnterState(InternalState.Strategic);
            }
            else
            {
                if (!prototype.IsCommandBridgeGameplayAvailable())
                    return;
                EnterState(InternalState.BridgeThirdPerson);
            }
        }

        if (!IsInsideBridge)
            return;

        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            EnterState(state == InternalState.BridgeVisor ? InternalState.BridgeThirdPerson : InternalState.BridgeVisor);
        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            EnterState(state == InternalState.BridgeHolomap ? InternalState.BridgeThirdPerson : InternalState.BridgeHolomap);

        UpdateThirdPerson(Time.deltaTime);
        UpdateBridgeCamera();
        UpdateStatus();
    }

    private void LateUpdate()
    {
        if (initialized && IsInsideBridge)
            UpdateBridgeCamera();
    }

    private void OnDisable()
    {
        if (IsInsideBridge)
            RestoreStrategicCamera();
    }

    private void TryInitialize()
    {
        if (prototype == null)
            prototype = GetComponent<SandRunnersPrototype>();
        if (prototype == null || prototype.battlePyramid == null)
            return;

        activeCamera = Camera.main;
        if (activeCamera == null)
            activeCamera = Object.FindFirstObjectByType<Camera>();
        if (activeCamera == null)
            return;

        BuildBridge();
        BuildSebek();
        BuildCanvas();
        GameObject strategicCanvasObject = GameObject.Find("SandRunners_Strategic_Canvas");
        if (strategicCanvasObject != null)
            strategicCanvas = strategicCanvasObject.GetComponent<Canvas>();
        SetBridgeVisible(false);
        initialized = true;
    }

    private void EnterState(InternalState next)
    {
        bool wasInside = IsInsideBridge;
        bool willBeInside = next == InternalState.BridgeThirdPerson ||
                            next == InternalState.BridgeVisor ||
                            next == InternalState.BridgeHolomap;

        if (!wasInside && willBeInside)
        {
            savedCameraPosition = activeCamera.transform.position;
            savedCameraRotation = activeCamera.transform.rotation;
            savedCameraFov = activeCamera.fieldOfView;
            yaw = sebekRoot != null ? sebekRoot.eulerAngles.y : 0f;
            pitch = 12f;
        }

        state = next;
        SetBridgeVisible(willBeInside);
        prototype.SetCommandBridgePresentationActive(willBeInside);

        if (willBeInside)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UpdateBridgeCamera();
        }
        else if (wasInside)
        {
            RestoreStrategicCamera();
        }

        UpdateStatus();
    }

    private void RestoreStrategicCamera()
    {
        if (activeCamera != null)
        {
            activeCamera.transform.position = savedCameraPosition;
            activeCamera.transform.rotation = savedCameraRotation;
            activeCamera.fieldOfView = savedCameraFov;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetBridgeVisible(bool visible)
    {
        if (bridgeRoot != null)
            bridgeRoot.gameObject.SetActive(visible);
        if (bridgeCanvas != null)
            bridgeCanvas.gameObject.SetActive(visible);
        if (strategicCanvas != null)
            strategicCanvas.gameObject.SetActive(!visible);
    }

    private void BuildBridge()
    {
        GameObject bridgePrefab = Resources.Load<GameObject>(CommandBridgeResource);
        GameObject bridgeObject = bridgePrefab != null
            ? Instantiate(bridgePrefab)
            : new GameObject("CommandBridge_MissingPrefab");
        bridgeObject.name = "CommandBridge_RuntimeSlice";
        bridgeRoot = bridgeObject.transform;
        bridgeRoot.SetParent(prototype.battlePyramid, false);
        Vector3 parentScale = prototype.battlePyramid.lossyScale;
        float bridgeWorldHeight = 7.5f * Mathf.Max(1f, Mathf.Abs(parentScale.y));
        bridgeRoot.position = prototype.battlePyramid.position + prototype.battlePyramid.up * bridgeWorldHeight;
        bridgeRoot.rotation = prototype.battlePyramid.rotation;
        bridgeRoot.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));

        if (bridgePrefab == null)
            Debug.LogError("Command bridge prefab is missing at Resources/" + CommandBridgeResource);
    }

    private void BuildSebek()
    {
        sebekRoot = new GameObject("Sebek_Bridge_Controller").transform;
        sebekRoot.SetParent(bridgeRoot, false);
        Transform spawn = bridgeRoot.Find("SebekSpawn");
        sebekRoot.localPosition = spawn != null ? spawn.localPosition : new Vector3(0f, 0f, -1.2f);

        GameObject prefab = Resources.Load<GameObject>(SebekWalkingResource);
        if (prefab != null)
        {
            GameObject visual = Instantiate(prefab, sebekRoot);
            visual.name = "Sebek_Authentic_Visual";
            sebekVisual = visual.transform;
            sebekVisual.localPosition = Vector3.zero;
            sebekVisual.localRotation = Quaternion.identity;
            sebekVisual.localScale = Vector3.one;
            FitSebekVisualToHeight(1.9f);
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            ConfigureSebekAnimation(visual);
        }
        else
        {
            Transform fallback = Primitive(PrimitiveType.Capsule, "Sebek_MissingModel_Marker", Vector3.zero, new Vector3(0.7f, 1.8f, 0.7f), MakeMaterial("Missing Sebek", Color.magenta));
            fallback.SetParent(sebekRoot, false);
            fallback.localPosition = Vector3.up * 0.9f;
        }
    }

    private void UpdateThirdPerson(float dt)
    {
        if (sebekRoot == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        Vector2 move = Vector2.zero;
        bool run = false;

        if (keyboard != null)
        {
            move.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            move.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            run = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }

        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * 0.09f;
            pitch = Mathf.Clamp(pitch - delta.y * 0.075f, -8f, 42f);
            bridgeCameraDistance = Mathf.Clamp(
                bridgeCameraDistance - mouse.scroll.ReadValue().y * 0.006f,
                1.4f,
                5f);
        }

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 direction = forward * move.y + right * move.x;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector3 localDelta = bridgeRoot.InverseTransformDirection(direction) * ((run ? 5.2f : 2.8f) * dt);
        Vector3 next = sebekRoot.localPosition + localDelta;
        next.x = Mathf.Clamp(next.x, -5.2f, 5.2f);
        next.z = Mathf.Clamp(next.z, -5.2f, 5.2f);
        next.y = 0f;
        sebekRoot.localPosition = next;

        bool moving = direction.sqrMagnitude > 0.01f;
        if (moving)
            sebekRoot.rotation = Quaternion.Slerp(sebekRoot.rotation, Quaternion.LookRotation(direction, bridgeRoot.up), dt * 12f);
        UpdateSebekLocomotion(moving, run);
    }

    private void UpdateBridgeCamera()
    {
        if (activeCamera == null || sebekRoot == null)
            return;

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 target = sebekRoot.position + bridgeRoot.up * 1.25f;
        Vector3 desired = target - orbit * Vector3.forward *
                          (state == InternalState.BridgeVisor ? 0.65f : bridgeCameraDistance);
        desired += bridgeRoot.up * (state == InternalState.BridgeVisor ? 0.1f : 0.45f);
        activeCamera.transform.position = Vector3.Lerp(activeCamera.transform.position, desired, 1f - Mathf.Exp(-14f * Time.deltaTime));
        activeCamera.transform.rotation = Quaternion.LookRotation(target - activeCamera.transform.position, bridgeRoot.up);
        activeCamera.fieldOfView = state == InternalState.BridgeVisor ? 34f : 62f;
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("CommandBridge_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        bridgeCanvas = canvasObject.GetComponent<Canvas>();
        bridgeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bridgeCanvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject("BridgeStatus", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        bridgeStatus = textObject.GetComponent<Text>();
        bridgeStatus.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bridgeStatus.fontSize = 22;
        bridgeStatus.fontStyle = FontStyle.Bold;
        bridgeStatus.color = new Color(1f, 0.78f, 0.22f);
        bridgeStatus.alignment = TextAnchor.UpperCenter;
        RectTransform rect = bridgeStatus.rectTransform;
        rect.anchorMin = new Vector2(0.2f, 0.92f);
        rect.anchorMax = new Vector2(0.8f, 0.99f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void UpdateStatus()
    {
        if (bridgeStatus == null)
            return;
        bridgeStatus.text = "COMMAND BRIDGE // " + state.ToString().ToUpperInvariant() +
                            "\nF1 STRATEGIC  |  WASD MOVE  |  SHIFT RUN  |  TAB VISOR  |  H HOLOMAP";
    }

    private void FitSebekVisualToHeight(float targetHeight)
    {
        if (sebekVisual == null)
            return;
        Renderer[] renderers = sebekVisual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y < 0.01f)
            return;
        float scale = targetHeight / bounds.size.y;
        sebekVisual.localScale *= scale;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        sebekVisual.position += bridgeRoot.up * Vector3.Dot(sebekRoot.position - bounds.min, bridgeRoot.up);
    }

    private void ConfigureSebekAnimation(GameObject visual)
    {
        sebekAnimation = visual.GetComponent<Animation>();
        if (sebekAnimation == null)
            sebekAnimation = visual.AddComponent<Animation>();
        sebekAnimation.playAutomatically = false;
        sebekAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
        sebekWalkClip = LoadSebekClip("SandRunners/Models/Sebek/Sebek_Walking");
        sebekRunClip = LoadSebekClip("SandRunners/Models/Sebek/Sebek_Running");
        if (sebekWalkClip != null)
            sebekAnimation.AddClip(sebekWalkClip, "BridgeWalk");
        if (sebekRunClip != null)
            sebekAnimation.AddClip(sebekRunClip, "BridgeRun");
    }

    private static AnimationClip LoadSebekClip(string resourcePath)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null || clips[i].name.StartsWith("__preview__"))
                continue;
            clips[i].legacy = true;
            clips[i].wrapMode = WrapMode.Loop;
            return clips[i];
        }
        return null;
    }

    private void UpdateSebekLocomotion(bool moving, bool running)
    {
        if (sebekAnimation == null)
            return;
        AnimationClip clip = running ? sebekRunClip : sebekWalkClip;
        string clipName = running ? "BridgeRun" : "BridgeWalk";
        if (clip == null)
            return;
        if (sebekPlayingClip != clipName)
        {
            sebekAnimation.CrossFade(clipName, 0.18f);
            sebekPlayingClip = clipName;
        }
        AnimationState animationState = sebekAnimation[clipName];
        if (animationState != null)
            animationState.speed = moving ? (running ? 1.1f : 0.9f) : 0.08f;
    }

    private void CreateBridgeLight(string name, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name, typeof(Light));
        lightObject.transform.SetParent(bridgeRoot, false);
        lightObject.transform.localPosition = localPosition;
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private Transform Primitive(PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Material material, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(bridgeRoot, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider primitiveCollider = go.GetComponent<Collider>();
        if (!collider && primitiveCollider != null)
            Destroy(primitiveCollider);
        return go.transform;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        if (color.a < 0.99f)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.renderQueue = 3000;
        }
        return material;
    }

    public static void DebugToggleBridgeForSmoke()
    {
        SandRunnersCommandBridgeController controller = Object.FindFirstObjectByType<SandRunnersCommandBridgeController>();
        if (controller == null)
            return;
        if (!controller.initialized)
            controller.TryInitialize();
        if (!controller.prototype.IsCommandBridgeGameplayAvailable())
            return;
        controller.EnterState(controller.IsInsideBridge ? InternalState.Strategic : InternalState.BridgeThirdPerson);
    }

}
