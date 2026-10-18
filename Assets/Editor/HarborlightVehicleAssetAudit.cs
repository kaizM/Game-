using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightVehicleAssetAudit
    {
        private const string AssetDirectory = "Assets/Resources/VehicleModels";

        [MenuItem("Harborlight/Audit Third-Party Vehicle Models")]
        public static void AuditVehicleModels()
        {
            var paths = AssetDatabase.FindAssets("t:Model", new[] { AssetDirectory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path);

            foreach (var path in paths)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null)
                {
                    Debug.LogError("Could not load vehicle model: " + path);
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (instance == null)
                {
                    Debug.LogError("Could not instantiate vehicle model: " + path);
                    continue;
                }

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError("Vehicle model has no renderers: " + path);
                    Object.DestroyImmediate(instance);
                    continue;
                }

                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var front = transforms.FirstOrDefault(item => item.name.Contains("FrontLeftWheel") || item.name.Contains("FrontRightWheel"));
                var rear = transforms.FirstOrDefault(item => item.name.Contains("BackWheels"));
                var wheelDelta = front != null && rear != null ? front.position - rear.position : Vector3.zero;

                Debug.Log($"[HarborlightVehicleAssetAudit] {path}: renderers={renderers.Length}, bounds={bounds.size}, center={bounds.center}, min={bounds.min}, frontMinusRear={wheelDelta}");
                foreach (var item in transforms.Where(item => item.name.Contains("Wheel")))
                    Debug.Log($"[HarborlightVehicleAssetAudit] wheel={item.name}, localPosition={item.localPosition}, localRotation={item.localRotation.eulerAngles}");
                foreach (var renderer in renderers)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    var mesh = filter != null ? filter.sharedMesh : null;
                    var materials = renderer.sharedMaterials.Select(material => material != null
                        ? material.name + ":" + (material.shader != null ? material.shader.name : "<no shader>")
                        : "<null>");
                    Debug.Log($"[HarborlightVehicleAssetAudit] renderer={renderer.name}, mesh={mesh?.name ?? "<none>"}, vertices={mesh?.vertexCount ?? 0}, materials={string.Join(",", materials)}");
                }

                Object.DestroyImmediate(instance);
            }
        }
    }
}
