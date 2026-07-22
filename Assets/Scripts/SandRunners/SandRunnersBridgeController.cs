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
    private SandRunnersPrototype prototype;
    private Transform bridgeRoot;
    private Transform sebekRoot;
    private Transform sebekVisual;
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
    private bool initialized;
    private static bool enterBridgeAfterSceneLoad;

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
                {
                    enterBridgeAfterSceneLoad = true;
                    prototype.StartDirectRtsForCommandBridge();
                    return;
                }
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
        if (enterBridgeAfterSceneLoad)
        {
            enterBridgeAfterSceneLoad = false;
            EnterState(InternalState.BridgeThirdPerson);
        }
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
        bridgeRoot = new GameObject("CommandBridge_RuntimeSlice").transform;
        bridgeRoot.SetParent(prototype.battlePyramid, false);
        bridgeRoot.localPosition = new Vector3(0f, 7.5f, 0f);
        bridgeRoot.localRotation = Quaternion.identity;

        Material basalt = MakeMaterial("Bridge Basalt", new Color(0.018f, 0.025f, 0.04f));
        Material gold = MakeMaterial("Bridge Gold", new Color(0.48f, 0.27f, 0.035f));
        Material glass = MakeMaterial("Bridge Window", new Color(0.025f, 0.18f, 0.3f, 0.25f));
        Material holo = MakeMaterial("Bridge Hologram", new Color(0.04f, 0.55f, 1f, 0.72f));

        Primitive(PrimitiveType.Cube, "Deck", new Vector3(0f, -0.15f, 0f), new Vector3(18f, 0.3f, 13f), basalt);
        Primitive(PrimitiveType.Cube, "RearBulkhead", new Vector3(0f, 3.2f, -6.35f), new Vector3(18f, 6.4f, 0.3f), basalt);
        Primitive(PrimitiveType.Cube, "LeftBulkhead", new Vector3(-8.85f, 3.2f, 0f), new Vector3(0.3f, 6.4f, 13f), basalt);
        Primitive(PrimitiveType.Cube, "RightBulkhead", new Vector3(8.85f, 3.2f, 0f), new Vector3(0.3f, 6.4f, 13f), basalt);
        Primitive(PrimitiveType.Cube, "Ceiling", new Vector3(0f, 6.25f, 0f), new Vector3(18f, 0.25f, 13f), basalt);

        Primitive(PrimitiveType.Cube, "PanoramicWindow", new Vector3(0f, 3.1f, 6.3f), new Vector3(13.8f, 5.5f, 0.12f), glass, false);
        Primitive(PrimitiveType.Cube, "WindowLeftPillar", new Vector3(-7.3f, 3.1f, 6.15f), new Vector3(0.7f, 6f, 0.45f), gold);
        Primitive(PrimitiveType.Cube, "WindowRightPillar", new Vector3(7.3f, 3.1f, 6.15f), new Vector3(0.7f, 6f, 0.45f), gold);
        Primitive(PrimitiveType.Cube, "WindowHeader", new Vector3(0f, 5.85f, 6.15f), new Vector3(15.2f, 0.55f, 0.45f), gold);

        Primitive(PrimitiveType.Cube, "NavigatorPost", new Vector3(-5.4f, 0.8f, 2.3f), new Vector3(3f, 1.6f, 1.8f), gold);
        Primitive(PrimitiveType.Cube, "WeaponsConsole", new Vector3(5.4f, 0.8f, 2.3f), new Vector3(3f, 1.6f, 1.8f), gold);
        Primitive(PrimitiveType.Cylinder, "RadioStation", new Vector3(-6.5f, 0.8f, -3.5f), new Vector3(1.2f, 0.8f, 1.2f), gold);
        Primitive(PrimitiveType.Cube, "FutureLiftDoors", new Vector3(0f, 2.1f, -6.12f), new Vector3(3.8f, 4.2f, 0.2f), gold);

        Transform holomap = Primitive(PrimitiveType.Cylinder, "HolomapConsole", new Vector3(0f, 0.55f, 1f), new Vector3(2.8f, 0.55f, 2.8f), gold);
        Transform projection = Primitive(PrimitiveType.Sphere, "HolomapProjection", new Vector3(0f, 2.15f, 1f), new Vector3(1.35f, 1.35f, 1.35f), holo, false);
        projection.SetParent(holomap.parent, true);

        for (int i = 0; i < 4; i++)
        {
            Transform point = new GameObject("CrewPoint_" + (i + 1)).transform;
            point.SetParent(bridgeRoot, false);
            point.localPosition = new Vector3(i < 2 ? -5.2f : 5.2f, 0f, i % 2 == 0 ? -1.5f : 3.5f);
        }
    }

    private void BuildSebek()
    {
        sebekRoot = new GameObject("Sebek_Bridge_Controller").transform;
        sebekRoot.SetParent(bridgeRoot, false);
        sebekRoot.localPosition = new Vector3(0f, 0f, -3.8f);

        GameObject prefab = Resources.Load<GameObject>(SebekWalkingResource);
        if (prefab != null)
        {
            GameObject visual = Instantiate(prefab, sebekRoot);
            visual.name = "Sebek_Authentic_Visual";
            sebekVisual = visual.transform;
            sebekVisual.localPosition = Vector3.zero;
            sebekVisual.localRotation = Quaternion.identity;
            sebekVisual.localScale = Vector3.one * 0.32f;
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Animation animation = visual.GetComponent<Animation>();
            if (animation != null)
            {
                animation.cullingType = AnimationCullingType.AlwaysAnimate;
                animation.Play();
            }
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
        }

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 direction = forward * move.y + right * move.x;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector3 localDelta = bridgeRoot.InverseTransformDirection(direction) * ((run ? 5.2f : 2.8f) * dt);
        Vector3 next = sebekRoot.localPosition + localDelta;
        next.x = Mathf.Clamp(next.x, -7.6f, 7.6f);
        next.z = Mathf.Clamp(next.z, -5.2f, 5.2f);
        next.y = 0f;
        sebekRoot.localPosition = next;

        if (direction.sqrMagnitude > 0.01f)
            sebekRoot.rotation = Quaternion.Slerp(sebekRoot.rotation, Quaternion.LookRotation(direction, bridgeRoot.up), dt * 12f);
    }

    private void UpdateBridgeCamera()
    {
        if (activeCamera == null || sebekRoot == null)
            return;

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 target = sebekRoot.position + bridgeRoot.up * 1.55f;
        Vector3 desired = target - orbit * Vector3.forward * (state == InternalState.BridgeVisor ? 1.15f : 3.8f);
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
        if (!controller.IsInsideBridge && !controller.prototype.IsCommandBridgeGameplayAvailable())
        {
            enterBridgeAfterSceneLoad = true;
            controller.prototype.StartDirectRtsForCommandBridge();
            return;
        }
        controller.EnterState(controller.IsInsideBridge ? InternalState.Strategic : InternalState.BridgeThirdPerson);
    }

}