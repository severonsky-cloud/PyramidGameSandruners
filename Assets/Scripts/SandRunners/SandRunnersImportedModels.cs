using UnityEngine;

internal static class SandRunnersImportedModelRules
{
    internal static float NormalizeScale(float targetLength, Vector3 sourceBounds)
    {
        float horizontal = Mathf.Max(Mathf.Abs(sourceBounds.x), Mathf.Abs(sourceBounds.z));
        return Mathf.Max(0.01f, targetLength) / Mathf.Max(0.0001f, horizontal);
    }
}

public partial class SandRunnersPrototype
{
    private const string MandarinkaImportedModelRoot =
        "SandRunners/Models/ImportedCandidates/";

    private bool TryAttachMandarinkaImportedVehicle(
        Transform parent,
        string assetBaseName,
        float targetLength,
        Quaternion rotation,
        Vector3 localPosition,
        Material primaryMaterial,
        bool hideExistingRenderers)
    {
        if (parent == null || string.IsNullOrEmpty(assetBaseName))
            return false;

        GameObject lod0Prefab = Resources.Load<GameObject>(
            MandarinkaImportedModelRoot + assetBaseName + "_LOD0");
        if (lod0Prefab == null)
            return false;

        if (hideExistingRenderers)
        {
            Renderer[] legacy = parent.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < legacy.Length; i++)
                if (legacy[i] != null)
                    legacy[i].enabled = false;
        }

        Transform modelRoot = new GameObject("Imported_Model_" + assetBaseName).transform;
        modelRoot.SetParent(parent, false);
        modelRoot.localPosition = Vector3.zero;
        modelRoot.localRotation = Quaternion.identity;
        modelRoot.localScale = Vector3.one;

        Renderer[] lod0 = InstantiateMandarinkaModelLod(modelRoot, lod0Prefab, "LOD0",
            targetLength, rotation, localPosition, primaryMaterial);
        GameObject lod1Prefab = Resources.Load<GameObject>(
            MandarinkaImportedModelRoot + assetBaseName + "_LOD1");
        GameObject lod2Prefab = Resources.Load<GameObject>(
            MandarinkaImportedModelRoot + assetBaseName + "_LOD2");
        Renderer[] lod1 = lod1Prefab != null
            ? InstantiateMandarinkaModelLod(modelRoot, lod1Prefab, "LOD1",
                targetLength, rotation, localPosition, primaryMaterial)
            : lod0;
        Renderer[] lod2 = lod2Prefab != null
            ? InstantiateMandarinkaModelLod(modelRoot, lod2Prefab, "LOD2",
                targetLength, rotation, localPosition, primaryMaterial)
            : lod1;

        if (lod0.Length == 0)
        {
            Destroy(modelRoot.gameObject);
            return false;
        }

        LODGroup group = modelRoot.gameObject.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = true;
        group.SetLODs(new[]
        {
            new LOD(0.58f, lod0),
            new LOD(0.24f, lod1),
            new LOD(0.055f, lod2)
        });
        group.RecalculateBounds();
        RegisterMandarinkaGrowth(modelRoot, 1.6f);
        return true;
    }

    private Renderer[] InstantiateMandarinkaModelLod(
        Transform modelRoot,
        GameObject prefab,
        string lodName,
        float targetLength,
        Quaternion rotation,
        Vector3 localPosition,
        Material primaryMaterial)
    {
        GameObject instance = Instantiate(prefab, modelRoot);
        instance.name = lodName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        Bounds bounds;
        if (!TryCalculateRendererBounds(renderers, out bounds))
            return renderers;

        float scale = SandRunnersImportedModelRules.NormalizeScale(targetLength, bounds.size);
        instance.transform.localScale = Vector3.one * scale;
        renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (TryCalculateRendererBounds(renderers, out bounds))
        {
            Vector3 desiredAnchor = modelRoot.parent.TransformPoint(localPosition);
            Vector3 currentAnchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            instance.transform.position += desiredAnchor - currentAnchor;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (primaryMaterial != null)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                    materials[m] = primaryMaterial;
                renderer.sharedMaterials = materials;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        return renderers;
    }

    private static bool TryCalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = new Bounds();
        bool initialized = false;
        if (renderers == null)
            return false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return initialized;
    }
}