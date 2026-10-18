using System;
using System.Linq;
using Harborlight;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Harborlight.Editor
{
    public static class HarborlightProjectSetup
    {
        private const string RuntimeMaterialPath = "Assets/Resources/HarborlightRuntimeMaterial.mat";
        private const string RendererDataPath = "Assets/Settings/HarborlightUniversalRenderer.asset";
        private static readonly string[] HarborlightTexturePaths =
        {
            "Assets/Resources/Art/Textures/Harborlight-Asphalt.png",
            "Assets/Resources/Art/Textures/Harborlight-Brick.png",
            "Assets/Resources/Art/Textures/Harborlight-Grass.png",
            "Assets/Resources/Art/Textures/Harborlight-Concrete.png",
            "Assets/Resources/Art/Textures/Harborlight-Water.png"
        };
        private static readonly string[] HarborlightPbrColorTexturePaths =
        {
            "Assets/Resources/Art/Textures/PolyHaven/asphalt_03_diff_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/brick_wall_001_diffuse_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/concrete_wall_009_diff_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/white_stucco_02_diff_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/grass_ground_diff_2k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/coast_sand_05_diff_2k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/pavement_05_diff_2k.jpg"
        };
        private static readonly string[] HarborlightPbrNormalTexturePaths =
        {
            "Assets/Resources/Art/Textures/PolyHaven/asphalt_03_nor_dx_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/brick_wall_001_nor_dx_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/concrete_wall_009_nor_dx_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/white_stucco_02_nor_dx_4k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/grass_ground_nor_gl_2k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/coast_sand_05_nor_gl_2k.jpg",
            "Assets/Resources/Art/Textures/PolyHaven/pavement_05_nor_gl_2k.jpg"
        };
        [MenuItem("Harborlight/Create Runtime Material Template")]
        public static void CreateRuntimeMaterialTemplate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (AssetDatabase.LoadAssetAtPath<Material>(RuntimeMaterialPath) != null) return;

            var shader = Shader.Find(GraphicsSettings.defaultRenderPipeline != null
                ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null)
            {
                Debug.LogError("Could not find Unity's Standard shader to create the runtime material template.");
                return;
            }

            var material = new Material(shader) { name = "Harborlight Runtime Material" };
            AssetDatabase.CreateAsset(material, RuntimeMaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created runtime material template at " + RuntimeMaterialPath);
        }

        [MenuItem("Harborlight/Create Backend Settings")]
        public static void CreateBackendSettings()
        {
            const string settingsPath = "Assets/Resources/HarborlightBackendSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<HarborlightBackendSettings>(settingsPath) != null) return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<HarborlightBackendSettings>(), settingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created HarborlightBackendSettings. Configure the HTTPS API URL and RSA public key before enabling online services.");
        }

        [MenuItem("Harborlight/Configure Mobile URP")]
        public static void ConfigureMobileUrp()
        {
            const string settingsFolder = "Assets/Settings";
            const string pipelinePath = settingsFolder + "/HarborlightUniversalRenderPipeline.asset";
            if (!AssetDatabase.IsValidFolder(settingsFolder)) AssetDatabase.CreateFolder("Assets", "Settings");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            var createPipelineAsset = pipeline == null;
            if (pipeline == null)
                pipeline = UniversalRenderPipelineAsset.Create();

            var rendererData = pipeline.rendererDataList.Length == 0 ? null : pipeline.rendererDataList[0];
            if (rendererData == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(rendererData)))
            {
                var persistedRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
                if (persistedRenderer == null)
                {
                    var defaultPipeline = UniversalRenderPipelineAsset.Create();
                    persistedRenderer = (UniversalRendererData)defaultPipeline.rendererDataList[0];
                    var rendererPath = AssetDatabase.GenerateUniqueAssetPath(RendererDataPath);
                    AssetDatabase.CreateAsset(persistedRenderer, rendererPath);
                }

                var serializedPipeline = new SerializedObject(pipeline);
                var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
                if (rendererList.arraySize == 0) rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = persistedRenderer;
                serializedPipeline.FindProperty("m_RendererData").objectReferenceValue = persistedRenderer;
                serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue = 0;
                serializedPipeline.ApplyModifiedProperties();
            }

            if (createPipelineAsset) AssetDatabase.CreateAsset(pipeline, pipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var template = AssetDatabase.LoadAssetAtPath<Material>(RuntimeMaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (template != null && shader != null)
            {
                template.shader = shader;
                EditorUtility.SetDirty(template);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Configured Harborlight to use URP 17.5 for mobile targets.");
        }

        [MenuItem("Harborlight/Configure Art Texture Imports")]
        public static void ConfigureArtTextureImports()
        {
            var updated = false;
            foreach (var assetPath in HarborlightTexturePaths)
            {
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                if (importer.textureType == TextureImporterType.Default && importer.sRGBTexture &&
                    importer.maxTextureSize == 1024 && importer.mipmapEnabled &&
                    importer.wrapMode == TextureWrapMode.Repeat && importer.filterMode == FilterMode.Trilinear &&
                    importer.anisoLevel == 4 && importer.textureCompression == TextureImporterCompression.CompressedHQ)
                    continue;

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                updated = true;
            }

            foreach (var assetPath in HarborlightPbrColorTexturePaths)
                updated |= ConfigureHighResolutionTexture(assetPath, TextureImporterType.Default, true, false, 2048);
            foreach (var assetPath in HarborlightPbrNormalTexturePaths)
                updated |= ConfigureHighResolutionTexture(assetPath, TextureImporterType.NormalMap, false, false, 2048,
                    assetPath.EndsWith("_nor_gl_2k.jpg", StringComparison.Ordinal));
            if (updated) AssetDatabase.SaveAssets();
            Debug.Log("Configured 4K desktop PBR art, source-resolution ground and coast textures, and 2K mobile surfaces.");
        }

        private static bool ConfigureHighResolutionTexture(string assetPath, TextureImporterType textureType, bool srgb, bool hdr, int mobileSize, bool flipGreenChannel = false)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            var changed = importer.textureType != textureType || importer.sRGBTexture != srgb ||
                          importer.maxTextureSize != 4096 || !importer.mipmapEnabled ||
                          importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Trilinear ||
                          importer.anisoLevel != 8 || importer.textureCompression != TextureImporterCompression.CompressedHQ;
            if (textureType == TextureImporterType.NormalMap)
                changed |= importer.flipGreenChannel != flipGreenChannel;
            if (hdr) changed |= importer.wrapModeU != TextureWrapMode.Repeat || importer.wrapModeV != TextureWrapMode.Clamp;
            if (!changed)
            {
                changed |= SetMobileTextureSize(importer, "Android", mobileSize);
                changed |= SetMobileTextureSize(importer, "iPhone", mobileSize);
            }
            else
            {
                importer.textureType = textureType;
                importer.sRGBTexture = srgb;
                importer.maxTextureSize = 4096;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                if (textureType == TextureImporterType.NormalMap)
                    importer.flipGreenChannel = flipGreenChannel;
                if (hdr)
                {
                    importer.wrapModeU = TextureWrapMode.Repeat;
                    importer.wrapModeV = TextureWrapMode.Clamp;
                }
                SetMobileTextureSize(importer, "Android", mobileSize);
                SetMobileTextureSize(importer, "iPhone", mobileSize);
            }

            if (changed) importer.SaveAndReimport();
            return changed;
        }

        private static bool SetMobileTextureSize(TextureImporter importer, string platform, int maxSize)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            if (settings.overridden && settings.maxTextureSize == maxSize) return false;
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        [MenuItem("Harborlight/Create Starter Scene")]
        public static void CreateStarterScene()
        {
            CreateRuntimeMaterialTemplate();
            AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Harborlight World");
            root.AddComponent<HarborlightBootstrap>();
            var scenePath = "Assets/Scenes/Harborlight.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.companyName = "Meridian Games";
            PlayerSettings.productName = "Harborlight";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.meridian.harborlight");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.meridian.harborlight");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Harborlight starter scene created at " + scenePath);
        }

        [MenuItem("Harborlight/Build Mac Development Player")]
        public static void BuildMacDevelopmentPlayer()
        {
            ConfigureArtTextureImports();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("Enable at least one scene in Build Settings before building Harborlight.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Environment.GetEnvironmentVariable("HARBORLIGHT_MAC_BUILD_PATH") ?? "Builds/Harborlight-Driving.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Harborlight build failed: " + report.summary.result + " · " + report.summary.totalErrors + " errors.");

            Debug.Log("Harborlight Mac development build created at " + options.locationPathName);
        }

        [MenuItem("Harborlight/Build Mac QA Player")]
        public static void BuildMacQaPlayer()
        {
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.qa");
            try
            {
                BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
            }
        }

        [MenuItem("Harborlight/Build Authenticated Network QA Player")]
        public static void BuildAuthenticatedNetworkQaPlayer()
        {
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            var previousCompany = PlayerSettings.companyName;
            var previousProduct = PlayerSettings.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.m5qa");
            PlayerSettings.companyName = "MeridianQA";
            PlayerSettings.productName = "HarborlightM5AuthenticatedQA";
            try
            {
                BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
            }
        }

        [MenuItem("Harborlight/Build Resident Character Isolated QA Player")]
        public static void BuildMacResidentCharacterQaPlayer()
        {
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            var previousCompany = PlayerSettings.companyName;
            var previousProduct = PlayerSettings.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.characterqa");
            PlayerSettings.companyName = "Meridian Games QA";
            PlayerSettings.productName = "Harborlight Character QA";
            try
            {
                BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
            }
        }

        [MenuItem("Harborlight/Build Character Art Isolated QA Player")]
        public static void BuildMacCharacterArtQaPlayer()
        {
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            var previousCompany = PlayerSettings.companyName;
            var previousProduct = PlayerSettings.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.characterartqa");
            PlayerSettings.companyName = "Meridian Games QA";
            PlayerSettings.productName = "Harborlight Character Art QA";
            try
            {
                BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
            }
        }

        [MenuItem("Harborlight/Build Environment Isolated QA Player")]
        public static void BuildMacEnvironmentQaPlayer()
        {
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            var previousCompany = PlayerSettings.companyName;
            var previousProduct = PlayerSettings.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.coastalqa");
            PlayerSettings.companyName = "Meridian Games Environment QA";
            PlayerSettings.productName = "Harborlight Coastal QA";
            try
            {
                BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
            }
        }

        [MenuItem("Harborlight/Build iOS Development Project")]
        public static void BuildIosDevelopmentProject()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.meridian.harborlight");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "16.0";
            Build(Environment.GetEnvironmentVariable("HARBORLIGHT_IOS_BUILD_PATH") ?? "Builds/iOS",
                BuildTarget.iOS, BuildOptions.Development);
        }

        [MenuItem("Harborlight/Build Android Development APK")]
        public static void BuildAndroidDevelopmentApk()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.meridian.harborlight");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            EditorUserBuildSettings.buildAppBundle = false;
            Build(Environment.GetEnvironmentVariable("HARBORLIGHT_ANDROID_BUILD_PATH") ?? "Builds/Android/Harborlight-development.apk",
                BuildTarget.Android, BuildOptions.Development);
        }

        private static void Build(string outputPath, BuildTarget target, BuildOptions options)
        {
            ConfigureArtTextureImports();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("Enable at least one scene in Build Settings before building Harborlight.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Harborlight " + target + " build failed: " + report.summary.result + " · " + report.summary.totalErrors + " errors.");

            Debug.Log("Harborlight " + target + " development build created at " + outputPath);
        }
    }
}
