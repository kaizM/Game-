#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Harborlight
{
    public sealed class HarborlightCameraQa : MonoBehaviour
    {
        private string _outputDirectory;
        private bool _failed;
        private int _checks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-harborlight-camera-qa");
            if (index < 0 || index + 1 >= args.Length) return;
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HARBORLIGHT_TEST_PROFILE")))
            {
                Debug.LogError("HL_CAMERA_QA_REQUIRES_DISPOSABLE_PROFILE");
                Application.Quit(1);
                return;
            }
            new GameObject("Harborlight camera QA").AddComponent<HarborlightCameraQa>()._outputDirectory = args[index + 1];
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(2f);
            var player = FindAnyObjectByType<HarborlightPlayer>();
            var camera = Camera.main;
            var follow = camera != null ? camera.GetComponent<HarborlightCamera>() : null;
            if (player == null || follow == null)
            {
                Debug.LogError("HL_CAMERA_QA_FAIL missing player or follow camera");
                Application.Quit(1);
                yield break;
            }
            player.enabled = false;
            var origin = player.transform.position;
            var rotation = player.transform.rotation;
            var focus = origin + Vector3.up * 1.25f;
            Check("walking-framing", DistanceBetween(camera.transform.position, focus, 3.9f, 4.5f));
            Directory.CreateDirectory(_outputDirectory);
            yield return Capture("walking-follow");

            var movementTime = 0f;
            var clearTracking = true;
            var capturedMovement = false;
            while (movementTime < 0.8f)
            {
                player.ApplyAuthoritativeMovement(Vector3.back, false);
                movementTime += Time.deltaTime;
                yield return null;
                var movingFocus = player.transform.position + Vector3.up * 1.25f;
                var separation = Vector3.Distance(camera.transform.position, movingFocus);
                clearTracking &= separation >= 3.2f && separation <= 5.3f &&
                    !Physics.Linecast(movingFocus, camera.transform.position, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (!capturedMovement && movementTime >= 0.4f)
                {
                    yield return Capture("moving-follow");
                    capturedMovement = true;
                }
            }
            Check("moving-follow-no-clipping", clearTracking);
            var stopTime = 0f;
            while (stopTime < 0.6f)
            {
                player.ApplyAuthoritativeMovement(Vector3.zero, false);
                stopTime += Time.deltaTime;
                yield return null;
            }
            var playerController = player.GetComponent<CharacterController>();
            playerController.enabled = false;
            player.transform.SetPositionAndRotation(origin, rotation);
            playerController.enabled = true;
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.8f);
            Check("movement-stop-recovery", DistanceBetween(camera.transform.position, focus, 3.9f, 4.5f));

            // This wall lies inside the former 0.65m cast-origin gap.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "QA thin camera wall";
            wall.transform.position = focus + Vector3.back * 0.45f;
            wall.transform.localScale = new Vector3(4f, 4f, 0.1f);
            wall.GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Check("thin-wall-no-crossing", camera.transform.position.z > origin.z - 0.24f);
            var renderers = player.GetComponentsInChildren<Renderer>();
            var hidden = true;
            foreach (var renderer in renderers) hidden &= renderer.forceRenderingOff;
            Check("close-camera-hides-subject", hidden);
            Destroy(wall);
            yield return new WaitForSecondsRealtime(1f);
            var visible = true;
            foreach (var renderer in renderers) visible &= !renderer.forceRenderingOff;
            Check("obstruction-recovery", visible && DistanceBetween(camera.transform.position, focus, 3.9f, 4.5f));

            var replacement = new GameObject("QA replacement camera target");
            replacement.transform.position = origin + new Vector3(50f, 200f, 0f);
            follow.target = replacement.transform;
            yield return null;
            yield return null;
            Check("target-rebind", DistanceBetween(camera.transform.position, replacement.transform.position + Vector3.up * 1.25f, 3.9f, 4.5f));
            replacement.transform.position += Vector3.right * 50f;
            yield return null;
            yield return null;
            Check("teleport-recovery", DistanceBetween(camera.transform.position, replacement.transform.position + Vector3.up * 1.25f, 3.9f, 4.5f));
            follow.target = player.transform;
            Destroy(replacement);
            yield return new WaitForSecondsRealtime(0.8f);

            HarborlightVehicle vehicle = null;
            foreach (var candidate in FindObjectsByType<HarborlightVehicle>())
                if (candidate.GarageIndex == 0) { vehicle = candidate; break; }
            if (vehicle == null) Check("vehicle-present", false);
            else
            {
                var vehiclePosition = vehicle.transform.position;
                var vehicleRotation = vehicle.transform.rotation;
                vehicle.transform.SetPositionAndRotation(origin, Quaternion.identity);
                Physics.SyncTransforms();
                player.SendMessage("EnterVehicle", vehicle, SendMessageOptions.RequireReceiver);
                yield return new WaitForSecondsRealtime(0.8f);
                Check("parked-vehicle-camera", player.IsDriving && DistanceBetween(camera.transform.position,
                    vehicle.transform.position + Vector3.up * 1.1f, 6.8f, 7.6f));
                yield return Capture("parked-vehicle-follow");
                player.SendMessage("ExitVehicle", SendMessageOptions.RequireReceiver);
                vehicle.transform.SetPositionAndRotation(vehiclePosition, vehicleRotation);
                var controller = player.GetComponent<CharacterController>();
                controller.enabled = false;
                player.transform.SetPositionAndRotation(origin, rotation);
                controller.enabled = true;
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(0.8f);
                Check("exit-restores-walking-camera", !player.IsDriving && DistanceBetween(camera.transform.position, focus, 3.9f, 4.5f));
            }
            Debug.Log($"HL_CAMERA_QA_{(_failed ? "FAIL" : "PASS")} checks={_checks}");
            Application.Quit(_failed ? 1 : 0);
        }

        private static bool DistanceBetween(Vector3 a, Vector3 b, float minimum, float maximum)
        {
            var distance = Vector3.Distance(a, b);
            Debug.Log($"HL_CAMERA_DISTANCE measured={distance:F3} expected={minimum:F1}..{maximum:F1}");
            return distance >= minimum && distance <= maximum;
        }

        private void Check(string name, bool passed)
        {
            _checks++;
            _failed |= !passed;
            Debug.Log($"HL_CAMERA_CHECK name={name} result={(passed ? "PASS" : "FAIL")}");
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
