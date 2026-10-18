using System;
using Harborlight;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightNetworkPlayerSetup
    {
        private const string ResidentPrefabPath = "Assets/Resources/Characters/HarborlightResident.prefab";
        private const string NetworkPrefabPath = "Assets/Resources/Network/HarborlightNetworkPlayer.prefab";

        [MenuItem("Harborlight/Build Network Player Prefab")]
        public static void BuildNetworkPlayerPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Network"))
                AssetDatabase.CreateFolder("Assets/Resources", "Network");

            var resident = AssetDatabase.LoadAssetAtPath<GameObject>(ResidentPrefabPath);
            if (resident == null)
                throw new InvalidOperationException("Build the Harborlight resident character prefab first: " + ResidentPrefabPath);

            var root = new GameObject("Harborlight Network Player");
            try
            {
                var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                if (ignoreRaycastLayer >= 0) root.layer = ignoreRaycastLayer;
                var controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.34f;
                controller.center = new Vector3(0f, 0.9f, 0f);

                var character = PrefabUtility.InstantiatePrefab(resident) as GameObject;
                if (character == null) throw new InvalidOperationException("Could not instantiate resident character prefab.");
                character.name = "Harborlight resident model";
                character.transform.SetParent(root.transform, false);
                character.layer = root.layer;

                root.AddComponent<NetworkObject>();
                root.AddComponent<NetworkTransform>();
                root.AddComponent<HarborlightPlayer>();
                root.AddComponent<HarborlightNetworkPlayer>();

                var saved = PrefabUtility.SaveAsPrefabAsset(root, NetworkPrefabPath);
                if (saved == null) throw new InvalidOperationException("Could not save network player prefab: " + NetworkPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(NetworkPrefabPath, ImportAssetOptions.ForceUpdate);
                var networkObject = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath).GetComponent<NetworkObject>();
                var validateNetworkObject = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
                if (networkObject == null || validateNetworkObject == null)
                    throw new InvalidOperationException("Could not initialize the NGO prefab identity.");
                validateNetworkObject.Invoke(networkObject, null);
                PrefabUtility.SavePrefabAsset(networkObject.gameObject);
                AssetDatabase.SaveAssets();
                var serializedNetworkObject = new SerializedObject(networkObject);
                var prefabHash = serializedNetworkObject.FindProperty("GlobalObjectIdHash").uintValue;
                if (prefabHash == 0)
                    throw new InvalidOperationException("NGO did not generate a global identity for the network player prefab.");
                AssetDatabase.Refresh();
                Debug.Log("Built Harborlight server-authoritative network player prefab at " + NetworkPrefabPath + " with hash " + prefabHash);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
