using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Harborlight.Editor
{
    public static class HarborlightResidentCharacterSetup
    {
        private const string ResidentModelPath = "Assets/Resources/Characters/HarborlightResident/HarborlightResident.fbx";
        private const string AnimationSourcePath = "Assets/Art/Characters/AnimationSources/UAL2_Standard.fbx";
        private const string ControllerPath = "Assets/Resources/Characters/HarborlightResidentAnimator.controller";
        private const string PrefabPath = "Assets/Resources/Characters/HarborlightResident.prefab";
        private const string SpeedParameter = "Speed";

        [MenuItem("Harborlight/Build Resident Character Assets")]
        public static void BuildAssets()
        {
            ConfigureResidentImporter();
            var idleClipName = "Idle_No_Loop";
            var walkClipName = "Walk_Carry_Loop";
            ConfigureAnimationLoops(idleClipName, walkClipName);

            var avatar = AssetDatabase.LoadAllAssetsAtPath(ResidentModelPath).OfType<Avatar>().FirstOrDefault();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ResidentModelPath);
            var idle = FindAnimationClip(idleClipName);
            var walk = FindAnimationClip(walkClipName);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("The Harborlight resident FBX must import as a valid Humanoid Avatar.");
            if (model == null)
                throw new InvalidOperationException("Could not load resident model: " + ResidentModelPath);

            var controller = BuildAnimatorController(idle, walk);
            BuildResidentPrefab(model, avatar, controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HarborlightResidentCharacterSetup] Built resident prefab with humanoid idle/walk animation: avatar={avatar.name}, idle={idle.name}, walk={walk.name}, prefab={PrefabPath}");
        }

        private static void ConfigureResidentImporter()
        {
            var importer = AssetImporter.GetAtPath(ResidentModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not load resident model importer: " + ResidentModelPath);

            if (importer.animationType == ModelImporterAnimationType.Human &&
                importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel && !importer.importAnimation)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureAnimationLoops(string idleClipName, string walkClipName)
        {
            var importer = AssetImporter.GetAtPath(AnimationSourcePath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not load animation source importer: " + AnimationSourcePath);

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations;
            var idleFound = false;
            var walkFound = false;
            foreach (var clip in clips)
            {
                if (clip.name.IndexOf(idleClipName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                    idleFound = true;
                }
                if (clip.name.IndexOf(walkClipName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                    walkFound = true;
                }
            }
            if (!idleFound || !walkFound)
                throw new InvalidOperationException($"Required animation clips were not found: idle={idleFound}, walk={walkFound}.");

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip FindAnimationClip(string namePart)
        {
            return AssetDatabase.LoadAllAssetsAtPath(AnimationSourcePath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                    clip.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? throw new InvalidOperationException("Could not find animation clip containing: " + namePart);
        }

        private static AnimatorController BuildAnimatorController(AnimationClip idle, AnimationClip walk)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states)
                stateMachine.RemoveState(state.state);
            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>())
            {
                AssetDatabase.RemoveObjectFromAsset(tree);
                UnityEngine.Object.DestroyImmediate(tree, true);
            }
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);

            var blendTree = new BlendTree
            {
                name = "Resident Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = SpeedParameter,
                useAutomaticThresholds = false
            };
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, 1f);
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var locomotion = stateMachine.AddState("Locomotion", new Vector3(260f, 120f, 0f));
            locomotion.motion = blendTree;
            stateMachine.defaultState = locomotion;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildResidentPrefab(GameObject model, Avatar avatar, AnimatorController controller)
        {
            GameObject character = null;
            try
            {
                character = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (character == null)
                    throw new InvalidOperationException("Could not instantiate the resident FBX.");
                character.name = "Harborlight Resident";

                var animator = character.GetComponent<Animator>();
                if (animator == null) animator = character.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                var renderers = character.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new InvalidOperationException("The resident FBX has no renderers.");

                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1))
                    bounds.Encapsulate(renderer.bounds);
                var sourceHeight = bounds.size.y;
                if (sourceHeight < 0.1f)
                    throw new InvalidOperationException("The resident FBX has invalid world bounds: " + bounds);

                var scale = 1.78f / sourceHeight;
                character.transform.localScale = Vector3.one * scale;
                character.transform.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
                character.transform.localRotation = Quaternion.identity;

                var saved = PrefabUtility.SaveAsPrefabAsset(character, PrefabPath);
                if (saved == null)
                    throw new InvalidOperationException("Could not save resident prefab: " + PrefabPath);
                Debug.Log($"[HarborlightResidentCharacterSetup] Calibrated resident model height from {sourceHeight:0.###} to 1.78m (scale={scale:0.####}, footOffset={-bounds.min.y * scale:0.###}m).");
            }
            finally
            {
                if (character != null) UnityEngine.Object.DestroyImmediate(character);
            }
        }
    }
}
