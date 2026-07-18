using UnityEditor;
using UnityEngine;

public sealed class SandRunnersSebekModelPostprocessor : AssetPostprocessor
{
    private const string SebekModelPath = "Assets/Resources/SandRunners/Models/Sebek/";

    private bool IsSebekModel()
    {
        return assetPath.StartsWith(SebekModelPath) && assetPath.EndsWith(".fbx");
    }

    private void OnPreprocessModel()
    {
        if (!IsSebekModel())
            return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        importer.useFileScale = true;
        importer.globalScale = 1f;
        importer.isReadable = true;
    }

    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!IsSebekModel() || clip == null)
            return;

        clip.legacy = true;
        clip.wrapMode = WrapMode.Loop;
    }
}
