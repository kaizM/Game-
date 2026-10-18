using UnityEngine;

namespace Harborlight
{
    public sealed class HarborlightTouchControls : MonoBehaviour
    {
        public static Vector2 Move { get; private set; }
        public static float Throttle { get; private set; }
        public static bool IsSupported => Application.isMobilePlatform;

        private static Vector2 _cameraLook;
        private static float _zoom;
        private static bool _interact;
        private static bool _exit;
        private static bool _lookConsumed;
        private static bool _zoomConsumed;
        private static bool _uiBlocked;

        public static void SetUiBlocked(bool blocked)
        {
            _uiBlocked = blocked;
            if (blocked)
            {
                Move = Vector2.zero;
                Throttle = 0f;
            }
        }

        private void Update()
        {
            Move = Vector2.zero;
            Throttle = 0f;
            _cameraLook = Vector2.zero;
            _zoom = 0f;
            _interact = false;
            _exit = false;
            _lookConsumed = false;
            _zoomConsumed = false;
            if (!IsSupported || _uiBlocked) return;

            var touchCount = Input.touchCount;
            var cameraTouchA = -1;
            var cameraTouchB = -1;
            for (var i = 0; i < touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var x = touch.rawPosition.x / Screen.width;
                var y = touch.rawPosition.y / Screen.height;

                if (x < 0.46f && y < 0.49f)
                {
                    var center = new Vector2(Screen.width * 0.18f, Screen.height * 0.20f);
                    var radius = Screen.width * 0.12f;
                    Move = Vector2.ClampMagnitude((touch.position - center) / radius, 1f);
                }
                else if (x >= 0.79f && x < 0.96f && y >= 0.255f && y < 0.36f)
                {
                    Throttle = Mathf.Max(Throttle, 1f);
                }
                else if (x >= 0.60f && x < 0.77f && y >= 0.255f && y < 0.36f)
                {
                    Throttle = Mathf.Min(Throttle, -1f);
                }
                else if (x >= 0.79f && x < 0.96f && y >= 0.385f && y < 0.49f)
                {
                    if (touch.phase == TouchPhase.Began) _interact = true;
                }
                else if (x >= 0.60f && x < 0.77f && y >= 0.385f && y < 0.49f)
                {
                    if (touch.phase == TouchPhase.Began) _exit = true;
                }
                else if (x > 0.34f && y > 0.56f)
                {
                    if (cameraTouchA < 0) cameraTouchA = i;
                    else if (cameraTouchB < 0) cameraTouchB = i;
                    if (touch.phase == TouchPhase.Moved)
                    {
                        _cameraLook += new Vector2(touch.deltaPosition.x / Screen.width * 360f,
                            touch.deltaPosition.y / Screen.height * 360f);
                    }
                }
            }

            if (cameraTouchA >= 0 && cameraTouchB >= 0)
            {
                var a = Input.GetTouch(cameraTouchA);
                var b = Input.GetTouch(cameraTouchB);
                if (a.phase == TouchPhase.Moved || b.phase == TouchPhase.Moved)
                {
                    var previousDistance = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                    var currentDistance = Vector2.Distance(a.position, b.position);
                    _zoom += (currentDistance - previousDistance) / Screen.height * 100f;
                }
            }
        }

        public static bool ConsumeInteract()
        {
            var value = _interact;
            _interact = false;
            return value;
        }

        public static bool ConsumeExit()
        {
            var value = _exit;
            _exit = false;
            return value;
        }

        public static Vector2 ConsumeCameraLook()
        {
            if (_lookConsumed) return Vector2.zero;
            _lookConsumed = true;
            return _cameraLook;
        }

        public static float ConsumeZoom()
        {
            if (_zoomConsumed) return 0f;
            _zoomConsumed = true;
            return _zoom;
        }
    }
}
