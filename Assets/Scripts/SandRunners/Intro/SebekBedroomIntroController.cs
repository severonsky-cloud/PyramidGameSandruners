using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class SebekBedroomInteractable : MonoBehaviour
{
    public enum InteractionKind
    {
        Bed,
        Window,
        Wardrobe,
        Vanity,
        BathroomDoor,
        Radio,
        Desk,
        Table,
        BedroomExit,
        SecurityConsole,
        PrisonerDoor
    }

    public InteractionKind kind;
    public string displayName = "Object";
    [TextArea(2, 4)] public string examineText = "Sebek studies the object.";
    public Renderer highlightRenderer;

    private Material runtimeMaterial;
    private Color baseColor = Color.white;
    private bool hasBaseColor;

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderer == null)
            highlightRenderer = GetComponentInChildren<Renderer>();
        if (highlightRenderer == null)
            return;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = highlightRenderer.material;
            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                baseColor = runtimeMaterial.GetColor("_BaseColor");
                hasBaseColor = true;
            }
            else if (runtimeMaterial.HasProperty("_Color"))
            {
                baseColor = runtimeMaterial.GetColor("_Color");
                hasBaseColor = true;
            }
        }

        Color target = highlighted ? Color.Lerp(baseColor, new Color(1f, 0.88f, 0.35f, 1f), 0.45f) : baseColor;
        if (hasBaseColor && runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", target);
        else if (hasBaseColor && runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", target);

        if (runtimeMaterial.HasProperty("_EmissionColor"))
            runtimeMaterial.SetColor("_EmissionColor", highlighted ? new Color(0.45f, 0.35f, 0.08f, 1f) : Color.black);
    }
}

public sealed class SebekBedroomIntroController : MonoBehaviour
{
    private enum IntroStage
    {
        Waking,
        Explore,
        RadioCalling,
        RadioFound,
        LeaveBedroom,
        FindCheckpoint,
        PrisonerDoorReached,
        Complete
    }

    [Header("Player")]
    public Transform playerRoot;
    public Transform playerSpawn;
    public Transform sebekVisualRoot;
    public string sebekWalkingResource = "SandRunners/Models/Sebek/Sebek_Walking";
    public float sebekVisualScale = 1f;
    public float sebekVisualYawOffset = 0f;
    public float moveSpeed = 2.15f;
    public float turnSpeed = 540f;
    public Vector2 roomHalfExtents = new Vector2(8f, 5.2f);
    public float bedroomHalfWidth = 8.1f;
    public float bedroomForwardLimit = 5.2f;
    public float corridorEntryZ = -5.35f;
    public float corridorHalfWidth = 1.45f;
    public float corridorBackLimit = -17.25f;

    [Header("Camera")]
    public Camera sceneCamera;
    public Vector3 cameraOffset = new Vector3(0.62f, 1.55f, -2.85f);
    public float cameraLookHeight = 1.45f;
    public float cameraSmooth = 8.5f;
    public float cameraDistance = 3.05f;
    public float cameraMinDistance = 1.65f;
    public float cameraMaxDistance = 5.4f;
    public float cameraPitch = 13f;
    public float cameraShoulderOffset = 0.62f;
    public float cameraMouseSensitivity = 0.13f;
    public float cameraZoomSensitivity = 0.42f;

    [Header("Interaction")]
    public SebekBedroomInteractable[] interactables;
    public float interactionRange = 2.85f;
    public float radioFallbackRange = 5.4f;
    public Light radioSignalLight;
    public Renderer[] radioPulseRenderers;

    private readonly HashSet<SebekBedroomInteractable> examined = new HashSet<SebekBedroomInteractable>();
    private IntroStage stage = IntroStage.Waking;
    private SebekBedroomInteractable currentInteractable;
    private Animation sebekAnimation;
    private AnimationClip walkClip;
    private float wakeTimer = 4.2f;
    private float exploreTimer;
    private float radioDialogueTimer;
    private float messageTimer;
    private float referenceRefreshTimer;
    private float radioAutoAnswerTimer;
    private float storyAutoInteractTimer;
    private bool bedroomExitOpened;
    private bool checkpointCleared;
    private string objectiveText = "Wake up.";
    private string messageText = "Heavy silence. Blue moonlight cuts across the room.";
    private string promptText = string.Empty;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle promptStyle;
    private GUIStyle objectiveStyle;
    private float cameraYaw;
    private bool cameraYawInitialized;
    private float prisonerRevealTimer;
    private bool prisonerRevealBuilt;
    private bool prisonerTransitionStarted;
    private Transform robertVisualRoot;

    private void Awake()
    {
        EnsurePlayer();
        EnsureCamera();
        CacheInteractables();
        ConfigureSebekVisual();
        SetRadioPulse(false, 0f);
    }

    private void Start()
    {
        if (playerSpawn != null && playerRoot != null)
        {
            playerRoot.position = playerSpawn.position;
            playerRoot.rotation = playerSpawn.rotation;
        }

        cameraYawInitialized = false;
        UpdateCamera(99f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        ResolveRuntimeReferences(dt);
        UpdateStage(dt);
        Vector3 move = stage == IntroStage.Waking || prisonerTransitionStarted ? Vector3.zero : ReadMoveVector();
        MovePlayer(move, dt);
        UpdateSebekAnimation(move.sqrMagnitude > 0.01f);
        if (stage != IntroStage.Complete)
            UpdateInteraction(dt);
        else
            UpdatePrisonerReveal(dt);
        UpdateRadioPulse(dt);

        if (messageTimer > 0f)
            messageTimer -= dt;
    }

    private void LateUpdate()
    {
        UpdateCamera(Time.deltaTime);
    }

    private void ResolveRuntimeReferences(float dt)
    {
        referenceRefreshTimer -= dt;

        if (playerRoot == null)
        {
            GameObject player = GameObject.Find("Sebek_Player");
            if (player != null)
                playerRoot = player.transform;
        }

        if (sceneCamera == null)
            sceneCamera = Camera.main;
        if (sceneCamera != null && sceneCamera.depth < 99f)
            ConfigureIntroCamera();

        if (referenceRefreshTimer <= 0f || interactables == null || interactables.Length == 0 || HasMissingInteractables())
        {
            referenceRefreshTimer = 0.65f;
            CacheInteractables();
        }
    }

    private bool HasMissingInteractables()
    {
        if (interactables == null)
            return true;

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] == null)
                return true;
        }

        return false;
    }

    private void EnsurePlayer()
    {
        if (playerRoot != null)
            return;

        GameObject player = new GameObject("Sebek_Player");
        player.transform.position = new Vector3(-1.6f, 0f, 2.35f);
        playerRoot = player.transform;
    }

    private void EnsureCamera()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneCamera == null)
        {
            GameObject namedCamera = GameObject.Find("Main Camera");
            if (namedCamera != null)
                sceneCamera = namedCamera.GetComponent<Camera>();
        }

        if (sceneCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cameras.Length > 0)
                sceneCamera = cameras[0];
        }

        if (sceneCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            sceneCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        ConfigureIntroCamera();
    }

    private void ConfigureIntroCamera()
    {
        if (sceneCamera == null)
            return;

        sceneCamera.gameObject.name = "Main Camera";
        sceneCamera.gameObject.tag = "MainCamera";
        sceneCamera.enabled = true;
        sceneCamera.depth = 100f;
        sceneCamera.fieldOfView = 48f;
        sceneCamera.nearClipPlane = 0.03f;
        sceneCamera.farClipPlane = 150f;
        sceneCamera.targetDisplay = 0;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != sceneCamera)
                cameras[i].depth = -100f;
        }

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool activeListenerSet = false;
        for (int i = 0; i < listeners.Length; i++)
        {
            bool shouldEnable = listeners[i].gameObject == sceneCamera.gameObject && !activeListenerSet;
            listeners[i].enabled = shouldEnable;
            activeListenerSet |= shouldEnable;
        }

        if (!activeListenerSet)
            sceneCamera.gameObject.AddComponent<AudioListener>();
    }

    private void CacheInteractables()
    {
        List<SebekBedroomInteractable> sceneInteractables = new List<SebekBedroomInteractable>();
        Scene scene = gameObject.scene;
        if (scene.IsValid())
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                roots[i].GetComponentsInChildren(false, sceneInteractables);
        }

        interactables = sceneInteractables.ToArray();
    }

    private void ConfigureSebekVisual()
    {
        if (sebekVisualRoot == null)
        {
            GameObject prefab = Resources.Load<GameObject>(sebekWalkingResource);
            if (prefab != null && playerRoot != null)
            {
                GameObject instance = Instantiate(prefab, playerRoot);
                instance.name = "Sebek_Nu_Ankha_Bedroom_Visual";
                sebekVisualRoot = instance.transform;
            }
        }

        if (sebekVisualRoot == null)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Sebek_Placeholder_Body";
            fallback.transform.SetParent(playerRoot, false);
            fallback.transform.localScale = new Vector3(0.62f, 1.05f, 0.62f);
            sebekVisualRoot = fallback.transform;
        }

        sebekVisualRoot.localPosition = Vector3.zero;
        sebekVisualRoot.localRotation = Quaternion.Euler(0f, sebekVisualYawOffset, 0f);
        sebekVisualRoot.localScale = Vector3.one * sebekVisualScale;

        Collider[] colliders = sebekVisualRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        sebekAnimation = sebekVisualRoot.GetComponentInChildren<Animation>();
        if (sebekAnimation == null)
            sebekAnimation = sebekVisualRoot.gameObject.AddComponent<Animation>();
        sebekAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
        sebekAnimation.playAutomatically = false;

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(sebekWalkingResource);
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || clip.name.Contains("__preview__"))
                continue;

            clip.legacy = true;
            clip.wrapMode = WrapMode.Loop;
            walkClip = clip;
            sebekAnimation.AddClip(clip, clip.name);
            sebekAnimation.clip = clip;
            sebekAnimation.Play(clip.name);
            AnimationState state = sebekAnimation[clip.name];
            if (state != null)
            {
                state.wrapMode = WrapMode.Loop;
                state.weight = 1f;
                state.speed = 0f;
            }
            break;
        }
    }

    private void UpdateStage(float dt)
    {
        if (stage == IntroStage.Waking)
        {
            wakeTimer -= dt;
            objectiveText = "Wake up.";
            if (wakeTimer <= 0f)
            {
                stage = IntroStage.Explore;
                objectiveText = "Get your bearings.";
                ShowMessage("Sebek forces herself upright. The pyramid is too quiet.");
            }
            return;
        }

        if (stage == IntroStage.Explore)
        {
            exploreTimer += dt;
            if (exploreTimer > 10f || (exploreTimer > 5.5f && examined.Count >= 2))
            {
                stage = IntroStage.RadioCalling;
                objectiveText = "Find the radio.";
                ShowMessage("A clipped burst of static breaks from somewhere near the vanity.");
            }
            return;
        }

        if (stage == IntroStage.RadioFound)
        {
            radioDialogueTimer += dt;
            if (radioDialogueTimer < 2.2f)
            {
                objectiveText = "Listen.";
                ShowPersistentMessage("Radio: --krrt-- Sebek? Finally.");
            }
            else if (radioDialogueTimer < 5.2f)
                ShowPersistentMessage("Radio: Sebek, respond. Security channel is awake.");
            else if (radioDialogueTimer < 8.4f)
                ShowPersistentMessage("Radio: Check the prisoner in the lower holding room. Now.");
            else
            {
                stage = IntroStage.LeaveBedroom;
                objectiveText = "Leave the bedroom.";
                ShowMessage("The bedroom door unlocks. The lower holding room is waiting.");
            }
            return;
        }

        if (stage == IntroStage.LeaveBedroom)
        {
            objectiveText = "Open the bedroom exit.";
            if (playerRoot != null && playerRoot.position.z < corridorEntryZ - 1.25f)
            {
                stage = IntroStage.FindCheckpoint;
                ShowMessage("The private corridor hums awake. A checkpoint console blocks the holding-room route.");
            }
            return;
        }

        if (stage == IntroStage.FindCheckpoint)
        {
            objectiveText = "Use the security console.";
            return;
        }

        if (stage == IntroStage.PrisonerDoorReached)
        {
            objectiveText = "Open the holding-room door.";
            return;
        }

        if (stage == IntroStage.Complete)
        {
            objectiveText = "Segment complete: prisoner scene next.";
        }
    }

    private Vector3 ReadMoveVector()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector3.zero;

        Vector2 raw = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            raw.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            raw.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            raw.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            raw.y += 1f;

        raw = Vector2.ClampMagnitude(raw, 1f);
        if (raw.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        float yaw = cameraYawInitialized ? cameraYaw : playerRoot != null ? playerRoot.eulerAngles.y : 0f;
        Quaternion reference = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = reference * Vector3.forward;
        Vector3 right = reference * Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        return Vector3.ClampMagnitude(right * raw.x + forward * raw.y, 1f);
    }

    private void MovePlayer(Vector3 move, float dt)
    {
        if (playerRoot == null)
            return;

        if (move.sqrMagnitude > 0.001f)
        {
            playerRoot.position += move * moveSpeed * dt;
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            playerRoot.rotation = Quaternion.RotateTowards(playerRoot.rotation, targetRotation, turnSpeed * dt);
        }

        Vector3 clamped = playerRoot.position;
        bool inCorridor = clamped.z < corridorEntryZ;
        float halfWidth = inCorridor ? corridorHalfWidth : bedroomHalfWidth;
        clamped.x = Mathf.Clamp(clamped.x, -halfWidth, halfWidth);
        clamped.z = Mathf.Clamp(clamped.z, corridorBackLimit, bedroomForwardLimit);
        clamped.y = 0f;
        playerRoot.position = clamped;
    }

    private void UpdateSebekAnimation(bool moving)
    {
        if (sebekAnimation == null || walkClip == null)
            return;

        if (!sebekAnimation.IsPlaying(walkClip.name))
            sebekAnimation.Play(walkClip.name);

        AnimationState state = sebekAnimation[walkClip.name];
        if (state == null)
            return;

        state.enabled = true;
        state.wrapMode = WrapMode.Loop;
        state.weight = 1f;

        float targetSpeed = moving ? 1f : 0f;
        float accel = moving ? 5.5f : 3.2f;
        state.speed = Mathf.MoveTowards(state.speed, targetSpeed, Time.deltaTime * accel);

        if (!moving)
        {
            state.time = Mathf.Repeat(state.time, Mathf.Max(0.01f, walkClip.length));
        }
    }

    private void UpdateInteraction(float dt)
    {
        SebekBedroomInteractable next = FindNearestInteractable();
        if (next != currentInteractable)
        {
            if (currentInteractable != null)
                currentInteractable.SetHighlighted(false);
            currentInteractable = next;
            if (currentInteractable != null)
                currentInteractable.SetHighlighted(true);
        }

        promptText = currentInteractable != null ? currentInteractable.displayName : string.Empty;
        Keyboard keyboard = Keyboard.current;
        bool pressed = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pressed = true;

        bool radioStoryActive = stage == IntroStage.RadioCalling || stage == IntroStage.Explore;
        bool radioInteractableActive = currentInteractable != null && currentInteractable.kind == SebekBedroomInteractable.InteractionKind.Radio;
        bool radioFallbackActive = radioStoryActive && IsNearRadioFallbackAnchor();
        bool consoleFallbackActive = stage == IntroStage.FindCheckpoint && IsNearNamedAnchor("Security_Checkpoint_Console", 4.8f);
        bool prisonerDoorFallbackActive = stage == IntroStage.PrisonerDoorReached && IsNearNamedAnchor("Prisoner_Holding_Room_Door", 4.2f);

        if (radioStoryActive && (radioInteractableActive || radioFallbackActive))
        {
            promptText = "Answer radio";
            radioAutoAnswerTimer += dt;
            if (stage == IntroStage.RadioCalling && messageTimer <= 0.2f)
                ShowPersistentMessage("The radio hisses in front of Sebek. Press E / Space.");
            if (radioAutoAnswerTimer > 0.55f)
                pressed = true;
        }
        else
        {
            radioAutoAnswerTimer = 0f;
        }

        if (pressed && radioStoryActive && (radioInteractableActive || radioFallbackActive))
        {
            StartRadioDialogue();
            return;
        }

        if (consoleFallbackActive || prisonerDoorFallbackActive)
        {
            promptText = consoleFallbackActive ? "Use security console" : "Open holding-room door";
            storyAutoInteractTimer += dt;
            if (storyAutoInteractTimer > 0.85f)
                pressed = true;
        }
        else
        {
            storyAutoInteractTimer = 0f;
        }

        if (pressed && consoleFallbackActive)
        {
            UseSecurityConsole();
            return;
        }

        if (pressed && prisonerDoorFallbackActive)
        {
            OpenPrisonerDoor();
            return;
        }

        if (pressed && currentInteractable != null)
            Interact(currentInteractable);
    }

    private bool IsNearRadioFallbackAnchor()
    {
        if (playerRoot == null)
            return false;

        Transform anchor = GetRadioFallbackAnchor();
        if (anchor == null)
            return false;

        Vector3 playerPosition = playerRoot.position;
        Vector3 radioPosition = anchor.position;
        playerPosition.y = 0f;
        radioPosition.y = 0f;
        return (playerPosition - radioPosition).sqrMagnitude <= radioFallbackRange * radioFallbackRange;
    }

    private Transform GetRadioFallbackAnchor()
    {
        if (radioSignalLight != null)
            return radioSignalLight.transform;

        if (radioPulseRenderers != null)
        {
            for (int i = 0; i < radioPulseRenderers.Length; i++)
            {
                if (radioPulseRenderers[i] != null)
                    return radioPulseRenderers[i].transform;
            }
        }

        GameObject radioObject = GameObject.Find("Hidden_Radio_On_Bedside_Table");
        return radioObject != null ? radioObject.transform : null;
    }

    private bool IsNearNamedAnchor(string objectName, float range)
    {
        if (playerRoot == null)
            return false;

        GameObject anchor = GameObject.Find(objectName);
        if (anchor == null)
            return false;

        Vector3 playerPosition = playerRoot.position;
        Vector3 anchorPosition = anchor.transform.position;
        playerPosition.y = 0f;
        anchorPosition.y = 0f;
        return (playerPosition - anchorPosition).sqrMagnitude <= range * range;
    }

    private SebekBedroomInteractable FindNearestInteractable()
    {
        if (interactables == null || playerRoot == null)
            return null;

        SebekBedroomInteractable priority = FindPriorityInteractable();
        if (priority != null)
            return priority;

        SebekBedroomInteractable best = null;
        float bestDistance = interactionRange * interactionRange;
        for (int i = 0; i < interactables.Length; i++)
        {
            SebekBedroomInteractable item = interactables[i];
            if (item == null || !item.gameObject.activeInHierarchy)
                continue;

            Vector3 itemPosition = item.transform.position;
            Vector3 playerPosition = playerRoot.position;
            itemPosition.y = 0f;
            playerPosition.y = 0f;
            float distance = (itemPosition - playerPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
            }
        }

        return best;
    }

    private SebekBedroomInteractable FindPriorityInteractable()
    {
        SebekBedroomInteractable.InteractionKind targetKind;
        float priorityRange = interactionRange * 1.45f;

        if (stage == IntroStage.RadioCalling || stage == IntroStage.Explore)
            targetKind = SebekBedroomInteractable.InteractionKind.Radio;
        else if (stage == IntroStage.LeaveBedroom)
            targetKind = SebekBedroomInteractable.InteractionKind.BedroomExit;
        else if (stage == IntroStage.FindCheckpoint)
            targetKind = SebekBedroomInteractable.InteractionKind.SecurityConsole;
        else if (stage == IntroStage.PrisonerDoorReached)
            targetKind = SebekBedroomInteractable.InteractionKind.PrisonerDoor;
        else
            return null;

        SebekBedroomInteractable best = null;
        float bestDistance = priorityRange * priorityRange;
        Vector3 playerPosition = playerRoot.position;
        playerPosition.y = 0f;

        for (int i = 0; i < interactables.Length; i++)
        {
            SebekBedroomInteractable item = interactables[i];
            if (item == null || !item.gameObject.activeInHierarchy || item.kind != targetKind)
                continue;

            Vector3 itemPosition = item.transform.position;
            itemPosition.y = 0f;
            float distance = (itemPosition - playerPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
            }
        }

        return best;
    }

    private void Interact(SebekBedroomInteractable item)
    {
        examined.Add(item);

        if (item.kind == SebekBedroomInteractable.InteractionKind.BedroomExit)
        {
            if (stage < IntroStage.LeaveBedroom)
            {
                ShowMessage("The bedroom door stays sealed. The radio silence feels intentional.");
                return;
            }

            OpenBedroomExit();
            if (stage == IntroStage.LeaveBedroom)
                stage = IntroStage.FindCheckpoint;
            ShowMessage("The private door slides open. Cold pyramid air spills into the bedroom.");
            return;
        }

        if (item.kind == SebekBedroomInteractable.InteractionKind.SecurityConsole)
        {
            UseSecurityConsole();
            return;
        }

        if (item.kind == SebekBedroomInteractable.InteractionKind.PrisonerDoor)
        {
            OpenPrisonerDoor();
            return;
        }

        if (item.kind == SebekBedroomInteractable.InteractionKind.Radio)
        {
            if (stage == IntroStage.RadioCalling || stage == IntroStage.Explore)
            {
                StartRadioDialogue();
                return;
            }
        }

        ShowMessage(item.examineText);
    }

    private void StartRadioDialogue()
    {
        if (stage != IntroStage.RadioCalling && stage != IntroStage.Explore)
            return;

        stage = IntroStage.RadioFound;
        radioDialogueTimer = 0f;
        radioAutoAnswerTimer = 0f;
        promptText = string.Empty;
        ShowMessage("Radio: --krrt-- Sebek? Finally.");
    }

    private void UseSecurityConsole()
    {
        if (stage < IntroStage.FindCheckpoint)
        {
            ShowMessage("The console is asleep. It is waiting for an authenticated order.");
            return;
        }

        checkpointCleared = true;
        stage = IntroStage.PrisonerDoorReached;
        storyAutoInteractTimer = 0f;
        promptText = string.Empty;
        ShowMessage("Security: holding-room seal released. One prisoner registered alive.");
    }

    private void OpenPrisonerDoor()
    {
        if (!checkpointCleared)
        {
            ShowMessage("The holding-room door rejects the request. The checkpoint console must be cleared first.");
            return;
        }

        stage = IntroStage.Complete;
        storyAutoInteractTimer = 0f;
        promptText = string.Empty;
        BeginPrisonerReveal();
    }

    private void BeginPrisonerReveal()
    {
        prisonerRevealTimer = 0f;
        prisonerTransitionStarted = false;
        corridorBackLimit = -27f;
        BuildPrisonerHoldingRoom();
        GameObject door = GameObject.Find("Prisoner_Holding_Room_Door");
        if (door != null)
            door.SetActive(false);
        GameObject lockObject = GameObject.Find("Holding_Room_Red_Lock");
        if (lockObject != null)
            lockObject.SetActive(false);
        objectiveText = "Enter the holding room.";
        ShowMessage("The seal retracts. A human prisoner raises his head in the cold blue light.");
    }

    private void BuildPrisonerHoldingRoom()
    {
        if (prisonerRevealBuilt)
            return;
        prisonerRevealBuilt = true;

        GameObject rootObject = new GameObject("Sebek_Prisoner_Reveal_Runtime");
        Material wall = CreateIntroMaterial("Holding Room Dark Metal", new Color(0.055f, 0.065f, 0.08f, 1f), 0.72f);
        Material light = CreateIntroMaterial("Holding Room Blue Light", new Color(0.08f, 0.28f, 0.75f, 1f), 0.35f);
        CreateIntroBox(rootObject.transform, "Holding_Floor", new Vector3(0f, -0.15f, -22f), new Vector3(8f, 0.3f, 9f), wall);
        CreateIntroBox(rootObject.transform, "Holding_Back_Wall", new Vector3(0f, 2.4f, -26f), new Vector3(8f, 4.8f, 0.35f), wall);
        CreateIntroBox(rootObject.transform, "Holding_Left_Wall", new Vector3(-4f, 2.4f, -22f), new Vector3(0.35f, 4.8f, 8f), wall);
        CreateIntroBox(rootObject.transform, "Holding_Right_Wall", new Vector3(4f, 2.4f, -22f), new Vector3(0.35f, 4.8f, 8f), wall);
        CreateIntroBox(rootObject.transform, "Holding_Light_Strip", new Vector3(0f, 4.25f, -23.4f), new Vector3(5.5f, 0.12f, 0.3f), light);

        GameObject robert = new GameObject("Robert_Prisoner_Reveal");
        robert.transform.SetParent(rootObject.transform, false);
        robert.transform.position = new Vector3(0f, 0f, -23.1f);
        robertVisualRoot = robert.transform;
        Material cloth = CreateIntroMaterial("Robert Earth Uniform", new Color(0.15f, 0.19f, 0.24f, 1f), 0.15f);
        Material skin = CreateIntroMaterial("Robert Skin", new Color(0.66f, 0.48f, 0.36f, 1f), 0.05f);
        CreateIntroCapsule(robert.transform, "Robert_Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 1.05f, 0.62f), cloth);
        CreateIntroSphere(robert.transform, "Robert_Head", new Vector3(0f, 2.22f, 0f), new Vector3(0.52f, 0.58f, 0.52f), skin);
        CreateIntroBox(robert.transform, "Robert_Arm_L", new Vector3(-0.58f, 1.2f, 0f), new Vector3(0.22f, 1.3f, 0.24f), cloth);
        CreateIntroBox(robert.transform, "Robert_Arm_R", new Vector3(0.58f, 1.2f, 0f), new Vector3(0.22f, 1.3f, 0.24f), cloth);

        Light roomLight = new GameObject("Holding_Room_Blue_Light").AddComponent<Light>();
        roomLight.transform.SetParent(rootObject.transform, false);
        roomLight.transform.position = new Vector3(0f, 3.7f, -22f);
        roomLight.type = LightType.Point;
        roomLight.color = new Color(0.18f, 0.42f, 1f, 1f);
        roomLight.intensity = 2.2f;
        roomLight.range = 12f;
    }

    private Material CreateIntroMaterial(string materialName, Color color, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = materialName;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        return material;
    }

    private void CreateIntroBox(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void CreateIntroCapsule(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void CreateIntroSphere(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void UpdatePrisonerReveal(float dt)
    {
        prisonerRevealTimer += dt;
        if (robertVisualRoot != null && playerRoot != null)
        {
            Vector3 look = playerRoot.position - robertVisualRoot.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                robertVisualRoot.rotation = Quaternion.RotateTowards(robertVisualRoot.rotation, Quaternion.LookRotation(look), 90f * dt);
        }

        if (prisonerRevealTimer < 3.5f)
            objectiveText = "Approach the prisoner.";
        else if (prisonerRevealTimer < 7f)
        {
            objectiveText = "Listen.";
            ShowPersistentMessage("ROBERT: Sebek? The palace said you were dead.");
        }
        else if (prisonerRevealTimer < 10.5f)
            ShowPersistentMessage("SEBEK: The palace was optimistic. Come with me; the pyramid is already moving.");
        else if (prisonerRevealTimer < 13.5f)
        {
            objectiveText = "Command-deck alarm.";
            ShowPersistentMessage("PYRAMID SPIRIT: Red court signatures entering the desert. Command authority required.");
        }
        else if (!prisonerTransitionStarted)
        {
            prisonerTransitionStarted = true;
            objectiveText = "Taking command of the battle pyramid...";
            ShowPersistentMessage("ROBERT: You stole a pyramid? SEBEK: Borrowed. Run.");
            Invoke(nameof(CompletePrologueAndLoadRts), 2.8f);
        }
    }

    private void CompletePrologueAndLoadRts()
    {
        SandRunnersBootstrap.StartRtsAfterCompletedPrologue();
    }

    private void SkipPrologue()
    {
        if (prisonerTransitionStarted)
            return;
        prisonerTransitionStarted = true;
        SandRunnersBootstrap.StartSkippedPrologue();
    }

    private void OpenBedroomExit()
    {
        if (bedroomExitOpened)
            return;

        bedroomExitOpened = true;
        GameObject door = GameObject.Find("Bedroom_Exit_Door");
        if (door != null)
            door.SetActive(false);
    }

    private void UpdateCamera(float dt)
    {
        if (sceneCamera == null || playerRoot == null)
            return;

        Mouse mouse = Mouse.current;
        if (!cameraYawInitialized)
        {
            cameraYaw = playerRoot.eulerAngles.y;
            cameraYawInitialized = true;
        }

        if (mouse != null)
        {
            Vector2 scroll = mouse.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.01f)
                cameraDistance = Mathf.Clamp(cameraDistance - scroll.y * 0.01f * cameraZoomSensitivity, cameraMinDistance, cameraMaxDistance);

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                cameraYaw += delta.x * cameraMouseSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - delta.y * cameraMouseSensitivity, -4f, 28f);
            }
            else
            {
                cameraYaw = Mathf.LerpAngle(cameraYaw, playerRoot.eulerAngles.y, 1f - Mathf.Exp(-dt * 6.5f));
            }
        }
        else
        {
            cameraYaw = Mathf.LerpAngle(cameraYaw, playerRoot.eulerAngles.y, 1f - Mathf.Exp(-dt * 6.5f));
        }

        float wakeSway = stage == IntroStage.Waking ? Mathf.Sin(Time.time * 1.7f) * 0.08f : 0f;
        Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 shoulder = yawRotation * (Vector3.right * cameraShoulderOffset);
        Vector3 lookTarget = playerRoot.position + Vector3.up * cameraLookHeight + shoulder * 0.35f;
        Vector3 desiredPosition = lookTarget - cameraRotation * Vector3.forward * cameraDistance + shoulder + Vector3.up * wakeSway;

        if ((sceneCamera.transform.position - desiredPosition).sqrMagnitude > 144f)
            sceneCamera.transform.position = desiredPosition;

        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, desiredPosition, 1f - Mathf.Exp(-cameraSmooth * dt));
        sceneCamera.transform.rotation = Quaternion.Slerp(
            sceneCamera.transform.rotation,
            Quaternion.LookRotation(lookTarget - sceneCamera.transform.position, Vector3.up),
            1f - Mathf.Exp(-cameraSmooth * dt));
    }

    private void UpdateRadioPulse(float dt)
    {
        bool active = stage == IntroStage.RadioCalling || stage == IntroStage.RadioFound;
        float pulse = active ? 0.35f + Mathf.PingPong(Time.time * 2.8f, 0.65f) : 0f;
        SetRadioPulse(active, pulse);
    }

    private void SetRadioPulse(bool active, float pulse)
    {
        if (radioSignalLight != null)
        {
            radioSignalLight.enabled = active;
            radioSignalLight.intensity = active ? Mathf.Lerp(0.4f, 2.2f, pulse) : 0f;
        }

        if (radioPulseRenderers == null)
            return;

        for (int i = 0; i < radioPulseRenderers.Length; i++)
        {
            Renderer renderer = radioPulseRenderers[i];
            if (renderer == null)
                continue;

            Material material = renderer.material;
            Color color = Color.Lerp(new Color(0.2f, 0.02f, 0.01f, 1f), new Color(1f, 0.18f, 0.06f, 1f), pulse);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", active ? color * 2.2f : Color.black);
        }
    }

    private void ShowMessage(string text)
    {
        messageText = text;
        messageTimer = 5.2f;
    }

    private void ShowPersistentMessage(string text)
    {
        if (messageText != text || messageTimer < 0.35f)
            messageText = text;
        messageTimer = Mathf.Max(messageTimer, 0.75f);
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawUiPanel(new Rect(14f, 12f, 620f, 82f));
        GUI.Label(new Rect(24f, 18f, 540f, 34f), "Sebek Pyramid Intro", titleStyle);
        GUI.Label(new Rect(26f, 58f, 680f, 28f), objectiveText, objectiveStyle);

        if (messageTimer > 0f || stage == IntroStage.RadioFound)
        {
            DrawUiPanel(new Rect(18f, 88f, 820f, 78f));
            GUI.Label(new Rect(26f, 92f, 760f, 80f), messageText, bodyStyle);
        }

        if (!string.IsNullOrEmpty(promptText) && stage != IntroStage.Waking)
        {
            Rect promptRect = new Rect(Screen.width * 0.5f - 250f, Screen.height - 96f, 500f, 58f);
            GUI.Label(promptRect, "E / Space: " + promptText, promptStyle);
        }

        if (GUI.Button(new Rect(Screen.width - 190f, 16f, 170f, 34f), "SKIP PROLOGUE"))
            SkipPrologue();
    }

    private void DrawUiPanel(Rect rect)
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.018f, 0.014f, 0.72f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.86f, 0.48f, 1f) }
        };

        objectiveStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.84f, 0.95f, 1f, 1f) }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.91f, 0.84f, 1f) }
        };

        promptStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }
}
