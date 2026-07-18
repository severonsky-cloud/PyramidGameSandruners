using UnityEditor;
using UnityEngine;

public static class SandRunnersSecondArtPassMaterialPatch
{
    [MenuItem("SandRunners/Art/Patch Second Pass Materials")]
    public static void Patch()
    {
        Material oldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SandRunners/Models/ArtPass/SR_Art_Ivory.mat");
        Material sandstone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SandRunners/Models/ArtPass/SR_Art_LightSandstone.mat");
        if (sandstone == null) { Debug.LogError("SECOND_ART_SANDSTONE_MISSING"); return; }

        string[] paths =
        {
            "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_BattlePyramid_ArtPass.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/Presentation/SR_Apex_Art.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabTank_Art.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_SalvageScarab_Art.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabWalker_Art.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabCarrierDrone_Art.prefab",
            "Assets/Resources/SandRunners/Models/ArtPass/ScarabVariants/SR_ScarabHowitzer_Art.prefab"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(paths[i]);
            if (root == null) continue;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null) continue;
                Material[] shared = renderer.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < shared.Length; m++)
                {
                    if (shared[m] == oldMat)
                    {
                        shared[m] = sandstone;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = shared;
            }
            PrefabUtility.SaveAsPrefabAsset(root, paths[i]);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SECOND_ART_MATERIALS_PATCHED");
    }
}
