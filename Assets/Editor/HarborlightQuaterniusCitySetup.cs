using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightQuaterniusCitySetup
    {
        private const string ModelFolder = "Assets/Art/Models/QuaterniusDowntownCity/Exports/FBX (Unity)";
        private const string OutputFolder = "Assets/Resources/Art/Buildings";
        private const float PrefabWidth = 12.4f;
        private const float PrefabHeight = 9.12f;
        private static readonly string[] BuildingModels =
        {
            "Building_Small_1",
            "Building_Medium_2_001",
            "Building_Large_2"
        };

        [MenuItem("Harborlight/Build Quaternius Downtown City Prefabs")]
        public static void BuildDowntownCityPrefabs()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextures();

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Resources/Art", "Buildings");

            foreach (var modelName in BuildingModels)
                BuildPrefab(modelName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built three grounded Quaternius Downtown City prefabs from CC0 source models.");
        }

        [MenuItem("Harborlight/Build Downtown City Visual QA Player")]
        public static void BuildDowntownCityQaPlayer()
        {
            BuildDowntownCityPrefabs();
            HarborlightProjectSetup.BuildMacDevelopmentPlayer();
        }

        private static void ConfigureTextures()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Models/QuaterniusDowntownCity/Textures" })
                .Select(AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var normal = System.IO.Path.GetFileNameWithoutExtension(path).EndsWith("_Normal", StringComparison.OrdinalIgnoreCase);
                var changed = importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) ||
                              importer.sRGBTexture == normal || importer.maxTextureSize != 4096 ||
                              !importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear ||
                              importer.anisoLevel != 8 || importer.textureCompression != TextureImporterCompression.CompressedHQ;
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                changed |= SetMobileTextureSize(importer, "Android", 2048);
                changed |= SetMobileTextureSize(importer, "iPhone", 2048);
                if (changed) importer.SaveAndReimport();
            }
        }

        private static bool SetMobileTextureSize(TextureImporter importer, string platform, int maxSize)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            var changed = !settings.overridden || settings.maxTextureSize != maxSize ||
                          settings.textureCompression != TextureImporterCompression.CompressedHQ;
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            if (changed) importer.SetPlatformTextureSettings(settings);
            return changed;
        }

        private static void BuildPrefab(string modelName)
        {
            var modelPath = ModelFolder + "/" + modelName + ".fbx";
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) throw new InvalidOperationException("Could not load city building model " + modelPath);

            var sourceObject = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (sourceObject == null) throw new InvalidOperationException("Could not instantiate city building model " + modelPath);

            var root = new GameObject(modelName);
            try
            {
                sourceObject.transform.SetParent(root.transform, false);
                var renderers = sourceObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException(modelName + " has no renderers.");

                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
                    throw new InvalidOperationException(modelName + " has invalid rendered bounds " + bounds.size);

                var materialDetails = renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Select(material => material.name + "[" + GetTextureName(material, "_BaseMap", "_MainTex") + ";" +
                        GetTextureName(material, "_BumpMap") + "]")
                    .Distinct().ToArray();
                var meshCount = sourceObject.GetComponentsInChildren<MeshFilter>(true)
                    .Count(filter => filter.sharedMesh != null);
                var triangleCount = sourceObject.GetComponentsInChildren<MeshFilter>(true)
                    .Where(filter => filter.sharedMesh != null).Sum(filter => filter.sharedMesh.triangles.Length / 3);

                var normalization = new Vector3(PrefabWidth / bounds.size.x,
                    PrefabHeight / bounds.size.y, PrefabWidth / bounds.size.z);
                sourceObject.transform.localScale = normalization;
                sourceObject.transform.localPosition = new Vector3(-bounds.center.x * normalization.x,
                    -bounds.min.y * normalization.y, -bounds.center.z * normalization.z);

                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, PrefabHeight * 0.5f, 0f);
                collider.size = new Vector3(12.2f, 9f, 12.2f);

                var outputPath = OutputFolder + "/Harborlight" + modelName + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
                Debug.Log("HL_QUATERNIUS_BUILDING model=" + modelName +
                          " meshCount=" + meshCount + " triangles=" + triangleCount +
                          " sourceBounds=" + bounds.size + " materials=" + string.Join(",", materialDetails));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static string GetTextureName(Material material, params string[] properties)
        {
            foreach (var property in properties)
            {
                if (!material.HasProperty(property)) continue;
                var texture = material.GetTexture(property);
                if (texture != null) return texture.name;
            }
            return "-";
        }
    }
}
