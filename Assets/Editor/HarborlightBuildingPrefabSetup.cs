using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightBuildingPrefabSetup
    {
        private const string OutputFolder = "Assets/Resources/Art/Buildings";
        private const string ApartmentModelPath = "Assets/Art/Models/PolyHaven/ModularUrbanApartmentsFacade/modular_urban_apartments_facade_2k.fbx";
        private const string FactoryModelPath = "Assets/Art/Models/PolyHaven/ModularFactoryFacade/modular_factory_facade_2k.fbx";

        [MenuItem("Harborlight/Build Modular District Building Prefabs")]
        public static void BuildDistrictBuildingPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Resources/Art", "Buildings");

            var roofMaterial = CreateRoofMaterial();
            BuildPrefab(ApartmentModelPath, "Harborlight Apartment Block", "HarborlightApartmentBlock", roofMaterial);
            BuildPrefab(FactoryModelPath, "Harborlight Foundry Block", "HarborlightFoundryBlock", roofMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log("Created textured apartment and foundry prefabs from Poly Haven CC0 modular assets.");
        }

        [MenuItem("Harborlight/Build Modular Map Isolated QA Player")]
        public static void BuildModularMapQaPlayer()
        {
            BuildDistrictBuildingPrefabs();
            var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            var previousCompany = PlayerSettings.companyName;
            var previousProduct = PlayerSettings.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.meridian.harborlight.modularmapqa");
            PlayerSettings.companyName = "Meridian Games Map QA";
            PlayerSettings.productName = "Harborlight Modular Map QA";
            try
            {
                HarborlightProjectSetup.BuildMacDevelopmentPlayer();
            }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, previousIdentifier);
                PlayerSettings.companyName = previousCompany;
                PlayerSettings.productName = previousProduct;
            }
        }

        private static Material CreateRoofMaterial()
        {
            const string materialPath = OutputFolder + "/HarborlightRoof.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "Harborlight Charcoal Roof"
            };
            material.SetColor("_BaseColor", new Color(0.22f, 0.25f, 0.25f));
            material.SetFloat("_Smoothness", 0.24f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        private static void BuildPrefab(string modelPath, string buildingName, string prefabName, Material roofMaterial)
        {
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) throw new InvalidOperationException("Missing modular building model: " + modelPath);

            var sourceInstance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (sourceInstance == null) throw new InvalidOperationException("Could not instantiate modular building model: " + modelPath);

            try
            {
                var modules = sourceInstance.GetComponentsInChildren<Renderer>(true);
                var root = new GameObject(buildingName);
                root.AddComponent<BoxCollider>().size = new Vector3(12.2f, 9f, 12.2f);
                root.GetComponent<BoxCollider>().center = new Vector3(0f, 4.5f, 0f);

                var facade = new[]
                {
                    new Facade(new Vector3(0f, 0f, -6.08f), 180f),
                    new Facade(new Vector3(0f, 0f, 6.08f), 0f),
                    new Facade(new Vector3(6.08f, 0f, 0f), 90f),
                    new Facade(new Vector3(-6.08f, 0f, 0f), 270f)
                };

                for (var side = 0; side < facade.Length; side++)
                {
                    var face = facade[side];
                    var right = Quaternion.Euler(0f, face.yaw, 0f) * Vector3.right;
                    for (var column = 0; column < 4; column++)
                    {
                        var position = face.center + right * (column * 3f - 4.5f);
                        var isFrontEntry = side == 0 && column == 1;
                        if (isFrontEntry)
                        {
                            AddModule(modules, root.transform, "wall_door_centered_large_01",
                                position + Vector3.up * 1.5f, face.yaw);
                            AddModule(modules, root.transform, "door_centered_large_01", position + Vector3.up * 1.5f, face.yaw);
                        }
                        else
                        {
                            AddModule(modules, root.transform, "wall_window_centered_large_0" + (column % 3 + 1),
                                position + Vector3.up * 1.5f, face.yaw);
                            AddModule(modules, root.transform, "window_centered_large_0" + (column % 3 + 1),
                                position + Vector3.up * 1.5f, face.yaw);
                        }

                        for (var floor = 1; floor <= 2; floor++)
                        {
                            var variant = ((column + floor - 1) % 3) + 1;
                            var suffix = variant.ToString("00");
                            var windowPosition = position + Vector3.up * (1.5f + floor * 3f);
                            AddModule(modules, root.transform, "wall_window_centered_large_" + suffix, windowPosition, face.yaw);
                            AddModule(modules, root.transform, "window_centered_large_" + suffix, windowPosition, face.yaw);
                        }
                    }
                }

                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "Charcoal roof cap";
                roof.transform.SetParent(root.transform, false);
                roof.transform.localPosition = new Vector3(0f, 9f, 0f);
                roof.transform.localScale = new Vector3(12.4f, 0.24f, 12.4f);
                roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
                UnityEngine.Object.DestroyImmediate(roof.GetComponent<Collider>());

                PrefabUtility.SaveAsPrefabAsset(root, OutputFolder + "/" + prefabName + ".prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceInstance);
            }
        }

        private static void AddModule(Renderer[] modules, Transform parent, string moduleName, Vector3 position, float yaw)
        {
            var source = modules.FirstOrDefault(module => module.name == moduleName);
            if (source == null) throw new InvalidOperationException("The modular building model is missing " + moduleName);

            var piece = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            piece.name = moduleName;
            piece.transform.localPosition = position;
            piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * source.transform.localRotation;
            piece.transform.localScale = source.transform.localScale;
        }

        private readonly struct Facade
        {
            public readonly Vector3 center;
            public readonly float yaw;

            public Facade(Vector3 center, float yaw)
            {
                this.center = center;
                this.yaw = yaw;
            }
        }
    }
}
