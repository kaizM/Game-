#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Harborlight
{
    // Short, repeatable player captures. Requires a disposable save profile and never runs in release builds.
    public sealed class HarborlightPresentationQa : MonoBehaviour
    {
        private string _outputDirectory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-harborlight-character-qa");
            if (index < 0 || index + 1 >= args.Length) return;
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HARBORLIGHT_TEST_PROFILE")))
            {
                Debug.LogError("HL_CHARACTER_QA_REQUIRES_DISPOSABLE_PROFILE");
                Application.Quit(1);
                return;
            }
            var qa = new GameObject("Harborlight character presentation QA").AddComponent<HarborlightPresentationQa>();
            qa._outputDirectory = args[index + 1];
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(2f);

            var player = FindAnyObjectByType<HarborlightPlayer>();
            var camera = Camera.main;
            if (player == null || camera == null)
            {
                Debug.LogError("HL_CHARACTER_QA_FAIL missing player or camera");
                Application.Quit(1);
                yield break;
            }
            player.enabled = false;
            var follow = camera.GetComponent<HarborlightCamera>();
            if (follow != null) follow.enabled = false;
            var animator = player.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.Play("Locomotion", 0, 0.5f);
                animator.Update(0f);
                animator.speed = 0f;
            }

            var directions = new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var ambient = new Color[directions.Length];
            RenderSettings.ambientProbe.Evaluate(directions, ambient);
            Debug.Log($"HL_CHARACTER_LIGHTING mode={RenderSettings.ambientMode} up={ambient[0]} down={ambient[1]} front={ambient[2]} back={ambient[3]}");
            var renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Debug.Log($"HL_CHARACTER_TINT mesh={renderer.name} base={block.GetColor("_BaseColor")} source={renderer.sharedMaterial.GetColor("_BaseColor")}");
                foreach (var material in renderer.sharedMaterials)
                    Debug.Log($"HL_CHARACTER_SURFACE mesh={renderer.name} material={material.name} normal={material.GetTexture("_BumpMap")?.name ?? "<none>"} normalEnabled={material.IsKeywordEnabled("_NORMALMAP")} probes={renderer.lightProbeUsage}");
            }

            Directory.CreateDirectory(_outputDirectory);
            var origin = player.transform.position;
            var offsets = new[] { new Vector3(0f, 1.65f, -3.2f), new Vector3(0f, 1.65f, 3.2f), new Vector3(2.5f, 1.65f, 1.7f) };
            var names = new[] { "back", "front", "profile" };
            for (var i = 0; i < offsets.Length; i++)
            {
                camera.transform.position = origin + offsets[i];
                camera.transform.LookAt(origin + Vector3.up * 1.05f);
                yield return new WaitForEndOfFrame();
                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(_outputDirectory, names[i] + ".png"), screenshot.EncodeToPNG());
                Destroy(screenshot);
                Debug.Log($"HL_CHARACTER_CAPTURE view={names[i]} player={origin} resolution={Screen.width}x{Screen.height}");
            }
            if (animator == null)
            {
                Debug.LogError("HL_CHARACTER_MOTION_FAIL missing animator");
                Application.Quit(1);
                yield break;
            }
            animator.speed = 1f;
            var elapsed = 0f;
            var walkingCaptured = false;
            while (elapsed < 1f)
            {
                player.ApplyAuthoritativeMovement(Vector3.back, false);
                camera.transform.position = player.transform.position + offsets[1];
                camera.transform.LookAt(player.transform.position + Vector3.up * 1.05f);
                elapsed += Time.deltaTime;
                if (!walkingCaptured && elapsed >= 0.5f)
                {
                    yield return Capture("walking");
                    walkingCaptured = true;
                }
                yield return null;
            }
            var displacement = Vector3.Distance(origin, player.transform.position);
            var stopTime = 0f;
            while (stopTime < 0.6f)
            {
                player.ApplyAuthoritativeMovement(Vector3.zero, false);
                stopTime += Time.deltaTime;
                yield return null;
            }
            var stopSpeed = animator.GetFloat("Speed");
            if (displacement < 3.8f || displacement > 5.3f || stopSpeed > 0.02f)
            {
                Debug.LogError($"HL_CHARACTER_MOTION_FAIL distance={displacement:F3} stopSpeed={stopSpeed:F4}");
                Application.Quit(1);
                yield break;
            }
            Debug.Log($"HL_CHARACTER_MOTION_PASS distance={displacement:F3} elapsed={elapsed:F3} stopSpeed={stopSpeed:F4}");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = origin;
            controller.enabled = true;
            animator.Play("Locomotion", 0, 0.5f);
            animator.Update(0f);
            animator.speed = 0f;
            camera.transform.position = origin + offsets[1];
            camera.transform.LookAt(origin + Vector3.up * 1.05f);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-harborlight-character-diagnostics") < 0)
            {
                Debug.Log("HL_CHARACTER_QA_PASS captures=4 motion=true");
                Application.Quit(0);
                yield break;
            }

            // A diagnostic comparison distinguishes surface defects from self-shadowing.
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                Debug.Log($"HL_CHARACTER_SUN intensity={sun.intensity} mask={sun.cullingMask} renderingLayer={sun.renderingLayerMask}");
                var shadows = sun.shadows;
                sun.shadows = LightShadows.None;
                camera.transform.position = origin + offsets[1];
                camera.transform.LookAt(origin + Vector3.up * 1.05f);
                yield return new WaitForEndOfFrame();
                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(_outputDirectory, "front-no-shadows.png"), screenshot.EncodeToPNG());
                Destroy(screenshot);
                sun.shadows = shadows;
            }
            foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.SetPropertyBlock(null);
            yield return new WaitForEndOfFrame();
            var noProperties = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_outputDirectory, "front-no-properties.png"), noProperties.EncodeToPNG());
            Destroy(noProperties);

            var neutral = new Material(Resources.Load<Material>("HarborlightRuntimeMaterial"));
            neutral.SetColor("_BaseColor", new Color(0.65f, 0.55f, 0.45f));
            foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.sharedMaterial = neutral;
            yield return new WaitForEndOfFrame();
            var neutralCapture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_outputDirectory, "front-neutral.png"), neutralCapture.EncodeToPNG());
            Destroy(neutralCapture);
            var probe = new MaterialPropertyBlock();
            probe.CopySHCoefficientArraysFrom(new[] { RenderSettings.ambientProbe });
            foreach (var renderer in renderers)
            {
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
                renderer.SetPropertyBlock(probe);
            }
            yield return Capture("front-explicit-ambient");

            var bakedMeshes = new System.Collections.Generic.List<Mesh>();
            foreach (var renderer in renderers)
            {
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                var normals = baked.normals;
                var minimum = float.MaxValue;
                var maximum = 0f;
                foreach (var normal in normals)
                {
                    minimum = Mathf.Min(minimum, normal.magnitude);
                    maximum = Mathf.Max(maximum, normal.magnitude);
                }
                Debug.Log($"HL_CHARACTER_BAKED_NORMALS mesh={renderer.name} length={minimum:G4}..{maximum:G4} bounds={baked.bounds}");
                bakedMeshes.Add(baked);
                var visual = new GameObject("QA baked " + renderer.name);
                visual.transform.SetParent(renderer.transform, false);
                var scale = renderer.transform.lossyScale;
                visual.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                visual.AddComponent<MeshFilter>().sharedMesh = baked;
                var staticRenderer = visual.AddComponent<MeshRenderer>();
                staticRenderer.sharedMaterial = neutral;
                staticRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
                staticRenderer.SetPropertyBlock(probe);
                renderer.enabled = false;
            }
            yield return Capture("front-baked");
            foreach (var mesh in bakedMeshes) mesh.RecalculateNormals();
            yield return Capture("front-baked-recalculated");
            Debug.Log("HL_CHARACTER_QA_PASS captures=10 motion=true");
            Destroy(neutral);
            Application.Quit(0);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_outputDirectory, name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
        }
    }
}
#endif
