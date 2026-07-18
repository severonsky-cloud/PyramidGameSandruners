using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private enum SandRunnersIntroStage
    {
        Herd,
        Tremor,
        PyramidReveal,
        SebekBastet,
        Mandarinka,
        Final
    }

    private bool sandRunnersIntroActive;
    private float sandRunnersIntroTimer;
    private SandRunnersIntroStage sandRunnersIntroStage;
    private GameObject sandRunnersIntroRoot;
    private GameObject sandRunnersIntroCanvasObject;
    private Text sandRunnersIntroSpeakerText;
    private Text sandRunnersIntroDialogueText;
    private Text sandRunnersIntroHintText;
    private Vector3 sandRunnersIntroOriginalPyramidPosition;
    private Quaternion sandRunnersIntroOriginalPyramidRotation;
    private Vector3 sandRunnersIntroOriginalCameraPosition;
    private Quaternion sandRunnersIntroOriginalCameraRotation;
    private float sandRunnersIntroOriginalCameraFov;
    private Transform sandRunnersIntroShepherd;
    private Transform sandRunnersIntroDune;
    private Transform[] sandRunnersIntroYaks;
    private Transform sandRunnersIntroFortress;
    private Transform[] sandRunnersIntroBuilders;
    private Material sandRunnersIntroSandMaterial;
    private Material sandRunnersIntroDarkMaterial;
    private Material sandRunnersIntroJadeMaterial;
    private Material sandRunnersIntroWarmMaterial;
    private Vector3 sandRunnersIntroRouteForward;
    private Vector3 sandRunnersIntroRouteRight;
    private Vector3 sandRunnersIntroStartPosition;
    private Quaternion sandRunnersIntroStartRotation;
    private Vector3 sandRunnersIntroOriginalFortressPosition;
    private Quaternion sandRunnersIntroOriginalFortressRotation;
    private bool sandRunnersIntroFortressWasMoved;

    private void StartSandRunnersIntro()
    {
        EnsureReferences();
        if (battlePyramid == null)
            return;

        if (sandRunnersIntroActive)
            return;

        SetupCameraImmediate();
        mainCamera = mainCamera != null ? mainCamera : Camera.main;
        if (mainCamera == null)
            return;

        sandRunnersIntroActive = true;
        cinematicDirectorActive = true;
        cinematicDirectorSettlementMode = false;
        cinematicDirectorOriginalHudHidden = hudHidden;
        sandRunnersIntroTimer = 0f;
        sandRunnersIntroStage = SandRunnersIntroStage.Herd;
        sandRunnersIntroStartPosition = pyramidStartPoseCached ? pyramidStartPosition : battlePyramid.position;
        sandRunnersIntroStartRotation = pyramidStartPoseCached ? pyramidStartRotation : battlePyramid.rotation;
        sandRunnersIntroRouteForward = sandRunnersIntroStartRotation * Vector3.forward;
        sandRunnersIntroRouteForward.y = 0f;
        if (sandRunnersIntroRouteForward.sqrMagnitude < 0.01f)
            sandRunnersIntroRouteForward = Vector3.forward;
        sandRunnersIntroRouteForward.Normalize();
        sandRunnersIntroRouteRight = Vector3.Cross(Vector3.up, sandRunnersIntroRouteForward).normalized;
        sandRunnersIntroStartPosition.y = GetPlayableGroundHeight(sandRunnersIntroStartPosition) + pyramidGroundClearance;
        sandRunnersIntroOriginalPyramidPosition = sandRunnersIntroStartPosition;
        sandRunnersIntroOriginalPyramidRotation = sandRunnersIntroStartRotation;
        sandRunnersIntroOriginalCameraPosition = mainCamera.transform.position;
        sandRunnersIntroOriginalCameraRotation = mainCamera.transform.rotation;
        sandRunnersIntroOriginalCameraFov = mainCamera.fieldOfView;

        hudHidden = true;
        SetGameFlowState(SandRunnersGameFlowState.Playing);
        SetCommandCursorMode(false);
        ClearRTSSelection();
        CreateSandRunnersIntroSet();
        SetSandRunnersIntroDialogue("ГРАУНДЕР", "Тихо... идём, яки. Не отставайте.");
        lastEvent = "Кинематографическое интро запущено. F8 — пропустить.";
    }

    private void CreateSandRunnersIntroSet()
    {
        sandRunnersIntroRoot = new GameObject("SandRunners_Intro_Set");
        Vector3 origin = sandRunnersIntroStartPosition;
        Vector3 forward = sandRunnersIntroRouteForward;
        Vector3 right = sandRunnersIntroRouteRight;

        sandRunnersIntroSandMaterial = CreateSandRunnersIntroMaterial(new Color(0.34f, 0.18f, 0.10f), 0.15f);
        sandRunnersIntroDarkMaterial = CreateSandRunnersIntroMaterial(new Color(0.05f, 0.04f, 0.035f), 0.1f);
        sandRunnersIntroJadeMaterial = CreateSandRunnersIntroMaterial(new Color(0.06f, 0.32f, 0.26f), 0.2f);
        sandRunnersIntroWarmMaterial = CreateSandRunnersIntroMaterial(new Color(0.75f, 0.36f, 0.08f), 0.15f);

        GameObject dune = CreateSandRunnersIntroPrimitive(PrimitiveType.Sphere, "Intro Dune", SandRunnersIntroGround(origin - forward * 20f, 4f), new Vector3(20f, 6f, 15f), sandRunnersIntroSandMaterial);
        sandRunnersIntroDune = dune.transform;
        GameObject grounder = CreateSandRunnersIntroPrimitive(PrimitiveType.Capsule, "Grounder Shepherd", SandRunnersIntroGround(origin + forward * 35f + right * 12f, 1.65f), new Vector3(1.6f, 3.2f, 1.6f), sandRunnersIntroDarkMaterial);
        sandRunnersIntroShepherd = grounder.transform;
        CreateSandRunnersIntroPrimitive(PrimitiveType.Sphere, "Grounder Head", grounder.transform.position + Vector3.up * 2.1f, Vector3.one * 1.1f, sandRunnersIntroWarmMaterial, sandRunnersIntroRoot.transform);
        CreateSandRunnersIntroPrimitive(PrimitiveType.Cube, "Grounder Staff", grounder.transform.position + forward * 1.2f + Vector3.up * 0.3f, new Vector3(0.18f, 2.4f, 0.18f), sandRunnersIntroWarmMaterial, sandRunnersIntroRoot.transform);

        sandRunnersIntroYaks = new Transform[3];
        for (int i = 0; i < sandRunnersIntroYaks.Length; i++)
        {
            Vector3 p = SandRunnersIntroGround(origin + forward * (42f + i * 5f) + right * ((i - 1) * 6.5f), 1.35f);
            GameObject yak = CreateSandRunnersIntroPrimitive(PrimitiveType.Capsule, "Earth Yak " + i, p, new Vector3(2.8f, 1.7f, 1.6f), sandRunnersIntroWarmMaterial);
            sandRunnersIntroYaks[i] = yak.transform;
            CreateSandRunnersIntroPrimitive(PrimitiveType.Sphere, "Yak Head " + i, p + forward * 2.1f + Vector3.up * 0.3f, Vector3.one * 0.9f, sandRunnersIntroDarkMaterial, sandRunnersIntroRoot.transform);
            CreateSandRunnersIntroPrimitive(PrimitiveType.Cube, "Yak Horn L " + i, p + forward * 2.1f + Vector3.left * 0.9f + Vector3.up * 1.0f, new Vector3(0.14f, 0.58f, 0.14f), sandRunnersIntroWarmMaterial, sandRunnersIntroRoot.transform);
            CreateSandRunnersIntroPrimitive(PrimitiveType.Cube, "Yak Horn R " + i, p + forward * 2.1f + Vector3.right * 0.9f + Vector3.up * 1.0f, new Vector3(0.18f, 0.8f, 0.18f), sandRunnersIntroWarmMaterial, sandRunnersIntroRoot.transform);
        }

        battlePyramid.position = SandRunnersIntroGround(origin - forward * 34f, pyramidGroundClearance);
        battlePyramid.rotation = sandRunnersIntroStartRotation;
        if (mandarinkaFortressRoot != null)
        {
            sandRunnersIntroFortress = mandarinkaFortressRoot;
            sandRunnersIntroOriginalFortressPosition = sandRunnersIntroFortress.position;
            sandRunnersIntroOriginalFortressRotation = sandRunnersIntroFortress.rotation;
            sandRunnersIntroFortressWasMoved = false;
        }
        else
        {
            sandRunnersIntroFortress = CreateSandRunnersIntroPrimitive(PrimitiveType.Cube, "Mandarinka Jade Fortress", SandRunnersIntroGround(origin + forward * 210f + right * 58f, 9f), new Vector3(22f, 18f, 18f), sandRunnersIntroJadeMaterial, sandRunnersIntroRoot.transform).transform;
        }
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = SandRunnersIntroGround(sandRunnersIntroFortress.position - forward * 13f + right * ((i - 1) * 5f), 2f);
            GameObject builder = CreateSandRunnersIntroPrimitive(PrimitiveType.Cube, "Mandarinka Builder " + i, p, new Vector3(2.4f, 3f, 2.4f), sandRunnersIntroJadeMaterial, sandRunnersIntroRoot.transform);
            if (sandRunnersIntroBuilders == null)
                sandRunnersIntroBuilders = new Transform[3];
            sandRunnersIntroBuilders[i] = builder.transform;
        }

        CreateSandRunnersIntroCanvas();
        ValidateSandRunnersIntroMaterials();
        UpdateSandRunnersIntroCamera(0f);
    }

    private void UpdateSandRunnersIntro(float dt)
    {
        sandRunnersIntroTimer += Mathf.Max(0f, dt);
        hudHidden = true;
        if (strategicCanvas != null && strategicCanvas.gameObject.activeSelf)
            strategicCanvas.gameObject.SetActive(false);

        if (sandRunnersIntroDune != null)
            sandRunnersIntroDune.gameObject.SetActive(sandRunnersIntroTimer < 23f);

        if (sandRunnersIntroTimer < 8f)
        {
            sandRunnersIntroStage = SandRunnersIntroStage.Herd;
            UpdateSandRunnersIntroCamera(0f);
        }
        else if (sandRunnersIntroTimer < 15f)
        {
            if (sandRunnersIntroStage != SandRunnersIntroStage.Tremor)
                SetSandRunnersIntroDialogue("ГРАУНДЕР", "Земля трясётся! Уводим стадо!");
            sandRunnersIntroStage = SandRunnersIntroStage.Tremor;
            for (int i = 0; i < sandRunnersIntroYaks.Length; i++)
                if (sandRunnersIntroYaks[i] != null)
                    sandRunnersIntroYaks[i].position -= sandRunnersIntroRouteForward * dt * 3f;
            if (sandRunnersIntroShepherd != null)
                sandRunnersIntroShepherd.position -= sandRunnersIntroRouteForward * dt * 3f;
            UpdateSandRunnersIntroCamera(0f);
        }
        else if (sandRunnersIntroTimer < 25f)
        {
            if (sandRunnersIntroStage != SandRunnersIntroStage.PyramidReveal)
                SetSandRunnersIntroDialogue("РАДИО", "В эфире: неизвестная тяжёлая машина выходит из-за дюны.");
            sandRunnersIntroStage = SandRunnersIntroStage.PyramidReveal;
            battlePyramid.position = Vector3.Lerp(battlePyramid.position, sandRunnersIntroStartPosition, dt * 1.2f);
            UpdateSandRunnersIntroCamera(0f);
        }
        else if (sandRunnersIntroTimer < 42f)
        {
            sandRunnersIntroStage = SandRunnersIntroStage.SebekBastet;
            SetSandRunnersIntroDialogue("СЕБЕК", "Входим в предместья Старой Лидии... Выглядит неплохо для пустынной планеты!");
            if (sandRunnersIntroTimer > 31f)
                SetSandRunnersIntroDialogue("СЛУЖИТЕЛЬНИЦА БАСТЕТ", "Себек, а это точно лучший путь до границы?");
            if (sandRunnersIntroTimer > 36f)
                SetSandRunnersIntroDialogue("СЕБЕК", "Мои источники говорят, что здесь только мелкие племена да торговцы. Красные почти не контролируют эту территорию...");
            UpdateSandRunnersIntroCamera(0f);
        }
        else if (sandRunnersIntroTimer < 66f)
        {
            sandRunnersIntroStage = SandRunnersIntroStage.Mandarinka;
            SetSandRunnersIntroDialogue("МАНДАРИНКА", "Я так не думаю... Никуда ты не поедешь дальше этих оазисов, сучка. Императрица даёт последний шанс. Верни наложника — и уйдёшь с миром.");
            for (int i = 0; i < sandRunnersIntroBuilders.Length; i++)
                if (sandRunnersIntroBuilders[i] != null)
                    sandRunnersIntroBuilders[i].position += sandRunnersIntroRouteForward * dt * 2.5f;
            UpdateSandRunnersIntroCamera(0f);
        }
        else if (sandRunnersIntroTimer < 78f)
        {
            sandRunnersIntroStage = SandRunnersIntroStage.Final;
            SetSandRunnersIntroDialogue("СЕБЕК", "Всем по местам, готовимся к вооружённому проходу...");
            UpdateSandRunnersIntroCamera(0f);
        }
        else
        {
            StopSandRunnersIntro(true);
        }

        UpdateSandRunnersIntroCanvas();
    }

    private void UpdateSandRunnersIntroCamera(float unused)
    {
        if (mainCamera == null || sandRunnersIntroRoot == null || battlePyramid == null)
            return;

        Vector3 focus = battlePyramid.position + Vector3.up * 5f;
        Vector3 position;
        float fov;
        if (sandRunnersIntroStage == SandRunnersIntroStage.Herd)
        {
            Vector3 herd = sandRunnersIntroShepherd != null ? sandRunnersIntroShepherd.position : focus;
            position = herd - sandRunnersIntroRouteForward * 26f + Vector3.up * 7f + sandRunnersIntroRouteRight * 15f;
            focus = herd + Vector3.up * 1.3f;
            fov = 50f;
        }
        else if (sandRunnersIntroStage == SandRunnersIntroStage.Tremor)
        {
            Vector3 herd = sandRunnersIntroShepherd != null ? sandRunnersIntroShepherd.position : focus;
            position = herd - sandRunnersIntroRouteForward * 22f + Vector3.up * 3.5f + sandRunnersIntroRouteRight * 12f;
            focus = herd + Vector3.up * 0.9f;
            fov = 46f;
        }
        else if (sandRunnersIntroStage == SandRunnersIntroStage.PyramidReveal)
        {
            focus = sandRunnersIntroStartPosition + Vector3.up * 6f;
            position = focus - sandRunnersIntroRouteForward * 70f + Vector3.up * 28f + sandRunnersIntroRouteRight * 24f;
            fov = 55f;
        }
        else if (sandRunnersIntroStage == SandRunnersIntroStage.Mandarinka)
        {
            position = sandRunnersIntroFortress.position - sandRunnersIntroRouteForward * 34f + Vector3.up * 24f + sandRunnersIntroRouteRight * 24f;
            focus = sandRunnersIntroFortress.position + Vector3.up * 5f;
            fov = 48f;
        }
        else
        {
            position = focus - sandRunnersIntroRouteForward * 50f + Vector3.up * 24f + sandRunnersIntroRouteRight * 18f;
            fov = 52f;
        }

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, position, Time.unscaledDeltaTime * 2.6f);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(focus - mainCamera.transform.position, Vector3.up), Time.unscaledDeltaTime * 3.5f);
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, fov, Time.unscaledDeltaTime * 2.5f);
    }

    private void StopSandRunnersIntro(bool restoreHud)
    {
        if (battlePyramid != null)
        {
            battlePyramid.position = sandRunnersIntroStartPosition;
            battlePyramid.rotation = sandRunnersIntroStartRotation;
            pyramidStartPosition = sandRunnersIntroStartPosition;
            pyramidStartRotation = sandRunnersIntroStartRotation;
        }
        if (sandRunnersIntroFortressWasMoved && sandRunnersIntroFortress != null)
        {
            sandRunnersIntroFortress.position = sandRunnersIntroOriginalFortressPosition;
            sandRunnersIntroFortress.rotation = sandRunnersIntroOriginalFortressRotation;
        }
        if (mainCamera != null)
        {
            mainCamera.transform.position = sandRunnersIntroOriginalCameraPosition;
            mainCamera.transform.rotation = sandRunnersIntroOriginalCameraRotation;
            mainCamera.fieldOfView = sandRunnersIntroOriginalCameraFov;
        }
        if (sandRunnersIntroCanvasObject != null)
            Destroy(sandRunnersIntroCanvasObject);
        if (sandRunnersIntroRoot != null)
            Destroy(sandRunnersIntroRoot);
        sandRunnersIntroCanvasObject = null;
        sandRunnersIntroRoot = null;
        sandRunnersIntroDune = null;
        sandRunnersIntroActive = false;
        cinematicDirectorActive = false;
        cinematicDirectorSettlementMode = false;
        if (restoreHud)
            hudHidden = cinematicDirectorOriginalHudHidden;
        SetCommandCursorMode(false);
        SetGameFlowState(SandRunnersGameFlowState.Playing);
        SetupCameraImmediate();
        lastEvent = "Интро завершено. Вооружённый проход начинается.";
    }

    private Material CreateSandRunnersIntroMaterial(Color color, float metallic)
    {
        Material material = CreateMaterial("SandRunners Intro Material", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.35f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.35f);
        return material;
    }

    private Vector3 SandRunnersIntroGround(Vector3 position, float heightOffset)
    {
        position.y = GetPlayableGroundHeight(position) + heightOffset;
        return position;
    }

    private bool IsSandRunnersIntroMagenta(Material material)
    {
        if (material == null)
            return false;
        Color color = Color.white;
        if (material.HasProperty("_BaseColor"))
            color = material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color"))
            color = material.color;
        return color.r > 0.7f && color.b > 0.7f && color.g < 0.25f;
    }

    private void ValidateSandRunnersIntroMaterials()
    {
        if (sandRunnersIntroRoot == null)
            return;

        Renderer[] renderers = sandRunnersIntroRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            if (material == null || material.shader == null || IsSandRunnersIntroMagenta(material))
                renderers[i].sharedMaterial = sandRunnersIntroSandMaterial;
        }

        if (sandRunnersIntroFortress != null)
        {
            Renderer[] fortressRenderers = sandRunnersIntroFortress.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < fortressRenderers.Length; i++)
            {
                Material material = fortressRenderers[i].sharedMaterial;
                if (material == null || material.shader == null || IsSandRunnersIntroMagenta(material))
                    fortressRenderers[i].sharedMaterial = sandRunnersIntroJadeMaterial;
            }
        }

        if (battlePyramid != null)
        {
            Renderer[] pyramidRenderers = battlePyramid.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < pyramidRenderers.Length; i++)
            {
                Material material = pyramidRenderers[i].sharedMaterial;
                if (material == null || material.shader == null || IsSandRunnersIntroMagenta(material))
                    pyramidRenderers[i].sharedMaterial = sandRunnersIntroSandMaterial;
            }
        }
    }

    private GameObject CreateSandRunnersIntroPrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        if (parent != null)
            go.transform.SetParent(parent, true);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        return go;
    }

    private void CreateSandRunnersIntroCanvas()
    {
        sandRunnersIntroCanvasObject = new GameObject("SandRunners_Intro_Subtitles");
        Canvas canvas = sandRunnersIntroCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        sandRunnersIntroCanvasObject.AddComponent<CanvasScaler>();
        sandRunnersIntroCanvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("DialoguePanel");
        panel.transform.SetParent(sandRunnersIntroCanvasObject.transform, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.015f, 0.01f, 0.02f, 0.88f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.04f);
        panelRect.anchorMax = new Vector2(0.88f, 0.22f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        sandRunnersIntroSpeakerText = CreateSandRunnersIntroText(panel.transform, "Speaker", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.98f), 24, TextAnchor.UpperLeft);
        sandRunnersIntroDialogueText = CreateSandRunnersIntroText(panel.transform, "Dialogue", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.64f), 21, TextAnchor.UpperLeft);
        sandRunnersIntroHintText = CreateSandRunnersIntroText(sandRunnersIntroCanvasObject.transform, "Hint", new Vector2(0.04f, 0.94f), new Vector2(0.96f, 0.985f), 17, TextAnchor.UpperLeft);
        sandRunnersIntroHintText.text = "F7 — запустить интро    F8 — пропустить";
    }

    private Text CreateSandRunnersIntroText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    private void SetSandRunnersIntroDialogue(string speaker, string dialogue)
    {
        if (sandRunnersIntroSpeakerText != null)
            sandRunnersIntroSpeakerText.text = speaker;
        if (sandRunnersIntroDialogueText != null)
            sandRunnersIntroDialogueText.text = dialogue;
    }

    private void UpdateSandRunnersIntroCanvas()
    {
        if (sandRunnersIntroSpeakerText == null || sandRunnersIntroDialogueText == null)
            return;
        float pulse = 0.84f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.16f;
        sandRunnersIntroSpeakerText.color = Color.Lerp(Color.white, new Color(1f, 0.62f, 0.12f), pulse);
    }
}