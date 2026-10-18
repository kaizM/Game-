using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightCharacterAssetAudit
    {
        private const string ModelPath = "Assets/Art/Characters/AnimationSources/UAL2_Standard.fbx";
        private const string ResidentModelPath = "Assets/Resources/Characters/HarborlightResident/HarborlightResident.fbx";

        public static void BuildVerifiedResidentQaPlayer()
        {
            HarborlightResidentCharacterSetup.ConfigureResidentNormalMaps();
            HarborlightResidentCharacterSetup.ConfigureResidentImporter();
            VerifyResidentPresentation();
            HarborlightProjectSetup.BuildMacQaPlayer();
        }

        public static void AuditResidentGeometry()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/HarborlightResident.prefab");
            foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var triangles = mesh.triangles;
                var flipped = 0;
                var minLength = float.MaxValue;
                var maxLength = 0f;
                foreach (var normal in normals)
                {
                    minLength = Mathf.Min(minLength, normal.magnitude);
                    maxLength = Mathf.Max(maxLength, normal.magnitude);
                }
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var a = triangles[i]; var b = triangles[i + 1]; var c = triangles[i + 2];
                    var face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
                    if (Vector3.Dot(face, (normals[a] + normals[b] + normals[c]).normalized) < -0.2f) flipped++;
                }
                Debug.Log($"HL_RESIDENT_GEOMETRY mesh={mesh.name} normals={normals.Length} length={minLength:F3}..{maxLength:F3} invertedFaces={flipped}/{triangles.Length / 3} layer={renderer.gameObject.layer} renderingLayer={renderer.renderingLayerMask}");
            }
        }

        [MenuItem("Harborlight/Verify Resident Presentation")]
        public static void VerifyResidentPresentation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/HarborlightResident.prefab");
            if (prefab == null) throw new System.InvalidOperationException("Resident prefab is missing.");
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(ResidentModelPath);
            if (modelImporter.importNormals != ModelImporterNormals.Calculate ||
                modelImporter.importTangents != ModelImporterTangents.CalculateMikk)
                throw new System.InvalidOperationException("Resident must calculate surface normals and Mikk tangents.");
            var materials = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials).Distinct().ToArray();
            var failures = 0;
            foreach (var material in materials)
            {
                if (material == null) throw new System.InvalidOperationException("Resident has a missing material.");
                var metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : -1f;
                var smoothness = material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness") : -1f;
                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.black;
                Debug.Log($"HL_RESIDENT_REFLECTION name={material.name} workflow={material.GetFloat("_WorkflowMode")} specular={material.GetColor("_SpecColor")} specularWorkflow={material.IsKeywordEnabled("_SPECULAR_SETUP")}");
                Debug.Log($"HL_RESIDENT_MATERIAL name={material.name} shader={material.shader.name} metallic={metallic} smoothness={smoothness} color={color} albedo={material.mainTexture?.name ?? "<none>"} normal={material.GetTexture("_BumpMap")?.name ?? "<none>"}");
                if (material.shader.name != "Universal Render Pipeline/Lit" || metallic < 0f || metallic > 0.05f ||
                    color.maxColorComponent < 0.15f || material.IsKeywordEnabled("_SPECULAR_SETUP")) failures++;
            }
            foreach (var name in new[] { "male_casualsuit01_normal", "shoes01_normal" })
            {
                var path = "Assets/Resources/Characters/HarborlightResident/textures/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.NormalMap || importer.sRGBTexture)
                {
                    Debug.LogError("HL_RESIDENT_NORMAL_INVALID path=" + path);
                    failures++;
                }
            }
            if (materials.Length != 5 || failures != 0)
                throw new System.InvalidOperationException($"Resident presentation failed: materials={materials.Length}, invalid={failures}.");
            Debug.Log("HL_RESIDENT_PRESENTATION_PASS materials=5 nonmetallic=true");
        }

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
