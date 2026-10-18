using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightCharacterAssetAudit
    {
        private const string ModelPath = "Assets/Art/Characters/AnimationSources/UAL2_Standard.fbx";
        private const string ResidentModelPath = "Assets/Resources/Characters/HarborlightResident/HarborlightResident.fbx";

        [MenuItem("Harborlight/Audit Character Animation Asset")]
        public static void AuditCharacterAnimationAsset()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Could not load character model importer: " + ModelPath);

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();

            var assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            var avatar = assets.OfType<Avatar>().FirstOrDefault();
            var clips = assets.OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];

            Debug.Log($"[HarborlightCharacterAssetAudit] {ModelPath}: avatar={avatar?.name ?? "<none>"}, valid={avatar?.isValid.ToString() ?? "false"}, humanoid={avatar?.isHuman.ToString() ?? "false"}, skinnedMeshes={renderers.Length}, clips={string.Join(",", clips.Select(clip => clip.name))}");
            foreach (var renderer in renderers)
                Debug.Log($"[HarborlightCharacterAssetAudit] mesh={renderer.name}, vertices={renderer.sharedMesh?.vertexCount ?? 0}, bones={renderer.bones.Length}, bounds={renderer.localBounds.size}");
        }

        [MenuItem("Harborlight/Audit MakeHuman Resident")]
        public static void AuditMakeHumanResident()
        {
            var importer = AssetImporter.GetAtPath(ResidentModelPath) as ModelImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Could not load resident model importer: " + ResidentModelPath);

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();

            var assets = AssetDatabase.LoadAllAssetsAtPath(ResidentModelPath);
            var avatar = assets.OfType<Avatar>().FirstOrDefault();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(ResidentModelPath);
            var renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
            var materials = assets.OfType<Material>().ToArray();
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Characters/HarborlightResident" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Where(texture => texture != null)
                .ToArray();

            Debug.Log($"[HarborlightCharacterAssetAudit] {ResidentModelPath}: avatar={avatar?.name ?? "<none>"}, valid={avatar?.isValid.ToString() ?? "false"}, humanoid={avatar?.isHuman.ToString() ?? "false"}, skinnedMeshes={renderers.Length}, materials={materials.Length}, textures={textures.Length}");
            foreach (var renderer in renderers)
                Debug.Log($"[HarborlightCharacterAssetAudit] residentMesh={renderer.name}, vertices={renderer.sharedMesh?.vertexCount ?? 0}, bones={renderer.bones.Length}, materials={string.Join(",", renderer.sharedMaterials.Select(material => material != null ? material.name + ":" + material.shader.name : "<none>"))}, bounds={renderer.localBounds.size}");
            foreach (var texture in textures)
                Debug.Log($"[HarborlightCharacterAssetAudit] residentTexture={AssetDatabase.GetAssetPath(texture)}, dimensions={texture.width}x{texture.height}");
        }
    }
}
