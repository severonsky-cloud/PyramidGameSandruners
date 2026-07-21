using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    // The current authored presentation prefabs are showcase assets, not safe
    // drop-in gameplay replacements. Keep them available for art review, but
    // never hide the proven procedural combat geometry until a replacement has
    // passed an in-game silhouette/LOD/anchor validation pass.
    private const bool EnableRuntimeAuthoredArtReplacements = false;

    private GameObject authoredBattlePyramidPrefab;
    private GameObject authoredPyramidArtPassPrefab;
    private GameObject authoredApexArtPrefab;
    private GameObject authoredResourceMinePrefab;
    private GameObject authoredNeutralSettlementPrefab;
    private GameObject authoredGoldenScarabPrefab;
    private GameObject authoredScarabTankPrefab;
    private GameObject authoredSalvageScarabPrefab;
    private GameObject authoredScarabWalkerPrefab;
    private GameObject authoredScarabCarrierPrefab;
    private GameObject authoredScarabHowitzerPrefab;
    private GameObject authoredImperialAssaultPrefab;
    private GameObject authoredMandarinkaPalacePrefab;
    private bool authoredArtAssetsLoaded;
    private bool presentationWorldArtAttached;
    private float nextAuthoredArtVisibilityCheck;
    private Transform authoredBattlePyramidInstance;
    private Transform authoredMandarinkaPalaceInstance;
    private Renderer[] authoredPyramidLegacyRenderers;
    private Renderer[] authoredPalaceLegacyRenderers;

    private void InitializeAuthoredArtPass()
    {
        if (!EnableRuntimeAuthoredArtReplacements)
            return;

        EnsureAuthoredArtAssets();
        AttachAuthoredBattlePyramid();
        AttachAuthoredMandarinkaPalace();
        StartCoroutine(RefreshAuthoredReplacementVisibility());
        StartCoroutine(AttachPresentationWorldArtAfterWorldSetup());
    }

    private IEnumerator RefreshAuthoredReplacementVisibility()
    {
        yield return null;
        RefreshAuthoredReplacementVisibilityNow();
        yield return new WaitForSecondsRealtime(0.5f);
        RefreshAuthoredReplacementVisibilityNow();
    }

    private void RefreshAuthoredReplacementVisibilityNow()
    {
        if (authoredBattlePyramidInstance == null)
        {
            GameObject pyramidArt = GameObject.Find("SR_Authored_Battle_Pyramid");
            if (pyramidArt != null)
                authoredBattlePyramidInstance = pyramidArt.transform;
        }

        if (authoredBattlePyramidInstance != null && authoredBattlePyramidInstance.parent != null)
            HideAuthoredReplacementRenderers(authoredBattlePyramidInstance.parent);

        if (authoredMandarinkaPalaceInstance == null)
        {
            GameObject palaceArt = GameObject.Find("SR_Authored_Mandarinka_Palace");
            if (palaceArt != null)
                authoredMandarinkaPalaceInstance = palaceArt.transform;
        }

        if (authoredMandarinkaPalaceInstance != null && authoredMandarinkaPalaceInstance.parent != null)
            HideAuthoredReplacementRenderers(authoredMandarinkaPalaceInstance.parent);
    }

    private void MaintainAuthoredArtVisibility(float unscaledTime)
    {
        if (!EnableRuntimeAuthoredArtReplacements)
            return;

        if (authoredBattlePyramidInstance == null)
        {
            GameObject pyramidArt = GameObject.Find("SR_Authored_Battle_Pyramid");
            if (pyramidArt != null)
                authoredBattlePyramidInstance = pyramidArt.transform;
        }

        if (authoredMandarinkaPalaceInstance == null)
        {
            GameObject palaceArt = GameObject.Find("SR_Authored_Mandarinka_Palace");
            if (palaceArt != null)
                authoredMandarinkaPalaceInstance = palaceArt.transform;
        }

        if (unscaledTime >= nextAuthoredArtVisibilityCheck)
        {
            nextAuthoredArtVisibilityCheck = unscaledTime + 2f;
            authoredPyramidLegacyRenderers = CacheLegacyRenderers(authoredBattlePyramidInstance);
            authoredPalaceLegacyRenderers = CacheLegacyRenderers(authoredMandarinkaPalaceInstance);
        }

        DisableCachedRenderers(authoredPyramidLegacyRenderers);
        DisableCachedRenderers(authoredPalaceLegacyRenderers);
    }

    private static Renderer[] CacheLegacyRenderers(Transform authoredInstance)
    {
        if (authoredInstance == null || authoredInstance.parent == null)
            return null;

        Transform root = authoredInstance.parent;
        List<Renderer> cached = new List<Renderer>();
        MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null && !meshRenderers[i].transform.IsChildOf(authoredInstance))
                cached.Add(meshRenderers[i]);
        }

        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            if (skinnedRenderers[i] != null && !skinnedRenderers[i].transform.IsChildOf(authoredInstance))
                cached.Add(skinnedRenderers[i]);
        }

        return cached.ToArray();
    }

    private static void DisableCachedRenderers(Renderer[] renderers)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
                renderers[i].enabled = false;
        }
    }

    private IEnumerator AttachPresentationWorldArtAfterWorldSetup()
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            TryAttachPresentationWorldArt();
            if (presentationWorldArtAttached)
                yield break;
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void TryAttachPresentationWorldArt()
    {
        EnsureAuthoredArtAssets();
        AttachAuthoredBattlePyramid();

        if (battlePyramid != null && authoredApexArtPrefab != null &&
            battlePyramid.Find("SR_Authored_Apex_Art") == null)
        {
            GameObject apex = Instantiate(authoredApexArtPrefab, battlePyramid);
            apex.name = "SR_Authored_Apex_Art";
            apex.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            apex.transform.localRotation = Quaternion.identity;
            apex.transform.localScale = Vector3.one * 0.62f;
            ConfigureAuthoredRuntimeLod(apex.transform);
        }

        AttachPresentationVisualToNamedRoot("Resource_Sand_Refinery_01", authoredResourceMinePrefab, "SR_Authored_Resource_Mine_Art", Vector3.one * 0.72f);
        AttachPresentationVisualToNamedRoot("Grounder_Settlement_West", authoredNeutralSettlementPrefab, "SR_Authored_Neutral_Settlement_Art", Vector3.one * 0.78f);

        presentationWorldArtAttached =
            battlePyramid != null && battlePyramid.Find("SR_Authored_Apex_Art") != null &&
            GameObject.Find("SR_Authored_Resource_Mine_Art") != null &&
            GameObject.Find("SR_Authored_Neutral_Settlement_Art") != null;
    }

    private void AttachPresentationVisualToNamedRoot(string hostName, GameObject prefab, string instanceName, Vector3 scale)
    {
        if (prefab == null)
            return;

        GameObject host = GameObject.Find(hostName);
        if (host == null || host.transform.Find(instanceName) != null)
            return;

        HideAuthoredReplacementRenderers(host.transform);
        GameObject instance = Instantiate(prefab, host.transform);
        instance.name = instanceName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = scale;
        ConfigureAuthoredRuntimeLod(instance.transform);
    }

    private void EnsureAuthoredArtAssets()
    {
        if (authoredArtAssetsLoaded)
            return;

        authoredArtAssetsLoaded = true;
        authoredPyramidArtPassPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/Presentation/SR_BattlePyramid_ArtPass");
        authoredApexArtPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/Presentation/SR_Apex_Art");
        authoredResourceMinePrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/Presentation/SR_ResourceMine_Art");
        authoredNeutralSettlementPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/Presentation/SR_NeutralSettlement_Art");
        authoredBattlePyramidPrefab = authoredPyramidArtPassPrefab ?? Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_BattlePyramid_Authored");
        authoredGoldenScarabPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_GoldenScarab_Authored");
        authoredScarabTankPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabTank_Art");
        authoredSalvageScarabPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/ScarabVariants/SR_SalvageScarab_Art");
        authoredScarabWalkerPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabWalker_Art");
        authoredScarabCarrierPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabCarrierDrone_Art");
        authoredScarabHowitzerPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabHowitzer_Art");
        authoredImperialAssaultPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_ImperialAssault_Authored");
        authoredMandarinkaPalacePrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_MandarinkaPalace_Authored");
    }

    private void AttachAuthoredBattlePyramid()
    {
        if (battlePyramid == null || authoredBattlePyramidPrefab == null ||
            battlePyramid.Find("SR_Authored_Battle_Pyramid") != null)
            return;

        HideAuthoredReplacementRenderers(battlePyramid);
        GameObject instance = Instantiate(authoredBattlePyramidPrefab, battlePyramid);
        instance.name = "SR_Authored_Battle_Pyramid";
        authoredBattlePyramidInstance = instance.transform;
        instance.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * 0.82f;
        ConfigureAuthoredRuntimeLod(instance.transform);
    }

    private void AttachAuthoredMandarinkaPalace()
    {
        if (mandarinkaFortressRoot == null || authoredMandarinkaPalacePrefab == null ||
            mandarinkaFortressRoot.Find("SR_Authored_Mandarinka_Palace") != null)
            return;

        HideAuthoredReplacementRenderers(mandarinkaFortressRoot);
        GameObject instance = Instantiate(authoredMandarinkaPalacePrefab, mandarinkaFortressRoot);
        instance.name = "SR_Authored_Mandarinka_Palace";
        authoredMandarinkaPalaceInstance = instance.transform;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * 0.92f;
        ConfigureAuthoredRuntimeLod(instance.transform);
    }

    private void ApplyAuthoredGoldenVehicleArt(Transform root, string unitName, Vector3 gameplayScale)
    {
        if (!EnableRuntimeAuthoredArtReplacements)
            return;

        if (root == null || string.IsNullOrEmpty(unitName) || !unitName.Contains("Scarab"))
            return;

        EnsureAuthoredArtAssets();
        if (authoredGoldenScarabPrefab == null || root.Find("SR_Authored_Golden_Scarab") != null || root.Find("SR_Authored_Scarab_Art") != null)
            return;

        HideAuthoredReplacementRenderers(root);
        SandRunnersScarabArtAdapter.Variant variant = SandRunnersScarabArtAdapter.ResolveVariant(unitName);
        GameObject selectedPrefab = authoredGoldenScarabPrefab;
        switch (variant)
        {
            case SandRunnersScarabArtAdapter.Variant.Tank:
                selectedPrefab = authoredScarabTankPrefab ?? authoredGoldenScarabPrefab;
                break;
            case SandRunnersScarabArtAdapter.Variant.Salvage:
                selectedPrefab = authoredSalvageScarabPrefab ?? authoredGoldenScarabPrefab;
                break;
            case SandRunnersScarabArtAdapter.Variant.Walker:
                selectedPrefab = authoredScarabWalkerPrefab ?? authoredGoldenScarabPrefab;
                break;
            case SandRunnersScarabArtAdapter.Variant.Carrier:
                selectedPrefab = authoredScarabCarrierPrefab ?? authoredGoldenScarabPrefab;
                break;
            case SandRunnersScarabArtAdapter.Variant.Howitzer:
                selectedPrefab = authoredScarabHowitzerPrefab ?? authoredGoldenScarabPrefab;
                break;
        }

        GameObject instance = Instantiate(selectedPrefab, root);
        instance.name = "SR_Authored_Scarab_Art";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        float scale = Mathf.Clamp((gameplayScale.x + gameplayScale.z) * 0.26f, 0.58f, 1.02f);
        instance.transform.localScale = Vector3.one * scale;
        ConfigureAuthoredRuntimeLod(instance.transform);
    }

    private void ApplyAuthoredImperialAssaultArt(Transform root)
    {
        if (!EnableRuntimeAuthoredArtReplacements)
            return;

        if (root == null)
            return;

        EnsureAuthoredArtAssets();
        if (authoredImperialAssaultPrefab == null || root.Find("SR_Authored_Imperial_Assault") != null)
            return;

        HideAuthoredReplacementRenderers(root);
        GameObject instance = Instantiate(authoredImperialAssaultPrefab, root);
        instance.name = "SR_Authored_Imperial_Assault";
        instance.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * 0.55f;
        ConfigureAuthoredRuntimeLod(instance.transform);
    }

    private static void ConfigureAuthoredRuntimeLod(Transform instance)
    {
        if (instance == null)
            return;

        LODGroup[] groups = instance.GetComponentsInChildren<LODGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            LODGroup group = groups[i];
            if (group == null)
                continue;

            LOD[] lods = group.GetLODs();
            if (lods == null || lods.Length == 0)
                continue;

            int lastIndex = lods.Length - 1;
            LOD last = lods[lastIndex];
            last.screenRelativeTransitionHeight = Mathf.Min(last.screenRelativeTransitionHeight, 0.002f);
            lods[lastIndex] = last;
            group.SetLODs(lods);
            group.RecalculateBounds();
        }
    }

    private static void HideAuthoredReplacementRenderers(Transform root)
    {
        if (root == null)
            return;

        MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            if (renderer != null && !HasAuthoredArtAncestor(renderer.transform, root))
                renderer.enabled = false;
        }

        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[i];
            if (renderer != null && !HasAuthoredArtAncestor(renderer.transform, root))
                renderer.enabled = false;
        }
    }

    private static bool HasAuthoredArtAncestor(Transform candidate, Transform stopAt)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current.name.StartsWith("SR_Authored_"))
                return true;
            if (current == stopAt)
                break;
            current = current.parent;
        }

        return false;
    }
}
