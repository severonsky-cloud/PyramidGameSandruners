using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private GameObject authoredBattlePyramidPrefab;
    private GameObject authoredGoldenScarabPrefab;
    private GameObject authoredImperialAssaultPrefab;
    private GameObject authoredMandarinkaPalacePrefab;
    private bool authoredArtAssetsLoaded;
    private float nextAuthoredArtVisibilityCheck;
    private Transform authoredBattlePyramidInstance;
    private Transform authoredMandarinkaPalaceInstance;
    private Renderer[] authoredPyramidLegacyRenderers;
    private Renderer[] authoredPalaceLegacyRenderers;

    private void InitializeAuthoredArtPass()
    {
        EnsureAuthoredArtAssets();
        AttachAuthoredBattlePyramid();
        AttachAuthoredMandarinkaPalace();
        StartCoroutine(RefreshAuthoredReplacementVisibility());
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

    private void EnsureAuthoredArtAssets()
    {
        if (authoredArtAssetsLoaded)
            return;

        authoredArtAssetsLoaded = true;
        authoredBattlePyramidPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_BattlePyramid_Authored");
        authoredGoldenScarabPrefab = Resources.Load<GameObject>("SandRunners/Models/ArtPass/SR_GoldenScarab_Authored");
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
    }

    private void ApplyAuthoredGoldenVehicleArt(Transform root, string unitName, Vector3 gameplayScale)
    {
        if (root == null || string.IsNullOrEmpty(unitName) || !unitName.Contains("Scarab"))
            return;

        EnsureAuthoredArtAssets();
        if (authoredGoldenScarabPrefab == null || root.Find("SR_Authored_Golden_Scarab") != null)
            return;

        HideAuthoredReplacementRenderers(root);
        GameObject instance = Instantiate(authoredGoldenScarabPrefab, root);
        instance.name = "SR_Authored_Golden_Scarab";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        float scale = Mathf.Clamp((gameplayScale.x + gameplayScale.z) * 0.26f, 0.58f, 1.02f);
        instance.transform.localScale = Vector3.one * scale;
    }

    private void ApplyAuthoredImperialAssaultArt(Transform root)
    {
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