using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightMapAssetAudit
    {
        private static readonly string[] BuildingModelPaths =
        {
            "Assets/Art/Models/PolyHaven/ModularFactoryFacade/modular_factory_facade_2k.fbx",
            "Assets/Art/Models/PolyHaven/ModularUrbanApartmentsFacade/modular_urban_apartments_facade_2k.fbx"
        };

        [MenuItem("Harborlight/Audit Poly Haven Building Models")]
        public static void AuditBuildingModels()
        {
            foreach (var path in BuildingModelPaths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null)
                {
                    Debug.LogError("Could not load building model: " + path);
                    continue;
                }

                AuditBuildingModel(path, source);
            }
        }

        private static void AuditBuildingModel(string path, GameObject source)
        {
            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not instantiate building model: " + path);
                return;
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(instance.transform.position, Vector3.zero);
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

            Debug.Log($"[HarborlightMapAssetAudit] path={path}, renderers={renderers.Length}, worldBounds={bounds.size}, rootScale={instance.transform.localScale}");
            foreach (var renderer in renderers)
            {
                var mesh = renderer is MeshRenderer meshRenderer
                    ? meshRenderer.GetComponent<MeshFilter>()?.sharedMesh
                    : null;
                var materials = renderer.sharedMaterials.Select(material => material != null
                    ? $"{material.name}:{material.shader?.name ?? "<no shader>"}:albedo={material.mainTexture?.name ?? "<none>"}:normal={material.GetTexture("_BumpMap")?.name ?? "<none>"}"
                    : "<null>");
                Debug.Log($"[HarborlightMapAssetAudit] path={path}, object={renderer.name}, localPosition={renderer.transform.localPosition}, localRotation={renderer.transform.localRotation.eulerAngles}, localScale={renderer.transform.localScale}, mesh={mesh?.name ?? "<none>"}, vertices={mesh?.vertexCount ?? 0}, localBounds={mesh?.bounds.size ?? Vector3.zero}, worldBounds={renderer.bounds.size}, materials={string.Join(",", materials)}");
            }

            Object.DestroyImmediate(instance);
        }
    }
}
