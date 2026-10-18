using UnityEngine;

namespace Harborlight
{
    public sealed class HarborlightCamera : MonoBehaviour
    {
        public Transform target;
        [SerializeField, Min(2f)] private float walkingDistance = 4.2f;
        [SerializeField, Min(4f)] private float drivingDistance = 7.2f;
        [SerializeField, Range(-0.6f, 0.6f)] private float shoulderOffset = 0.25f;
        [SerializeField, Range(12f, 55f)] private float pitch = 18f;
        [SerializeField, Min(0.01f)] private float touchYawSensitivity = 0.18f;
        [SerializeField, Min(0.01f)] private float touchPitchSensitivity = 0.16f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private Transform _boundTarget;
        private HarborlightPlayer _player;
        private Renderer[] _subjectRenderers = new Renderer[0];
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _yaw;
        private float _currentYaw;
        private float _yawVelocity;
        private float _currentPitch;
        private float _pitchVelocity;
        private float _zoomOffset;
        private float _currentDistance;
        private bool _subjectHidden;

        public Vector3 PlanarForward => Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, _currentYaw, 0f) * Vector3.right;

        private void Update()
        {
            BindTarget();
            var look = HarborlightTouchControls.ConsumeCameraLook();
            if (!HarborlightTouchControls.IsSupported && Input.GetMouseButton(1))
                look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            _yaw += look.x * (HarborlightTouchControls.IsSupported ? touchYawSensitivity : 4f);
            pitch = Mathf.Clamp(pitch - look.y * (HarborlightTouchControls.IsSupported ? touchPitchSensitivity : 3f), 12f, 55f);
            var zoom = HarborlightTouchControls.ConsumeZoom();
            if (!HarborlightTouchControls.IsSupported) zoom += Input.mouseScrollDelta.y;
            _zoomOffset = Mathf.Clamp(_zoomOffset - zoom * 0.6f, -2f, 3.5f);
        }

        private void BindTarget()
        {
            if (_boundTarget == target) return;
            SetSubjectHidden(false);
            _boundTarget = target;
            if (target == null) return;
            _player = target.GetComponent<HarborlightPlayer>();
            _subjectRenderers = target.GetComponentsInChildren<Renderer>(true);
            _yaw = _currentYaw = target.eulerAngles.y;
            _currentPitch = pitch;
            _yawVelocity = _pitchVelocity = 0f;
            _focus = TargetFocus();
            _focusVelocity = Vector3.zero;
            _currentDistance = walkingDistance + _zoomOffset;
        }

        private Vector3 TargetFocus()
        {
            return _player != null && _player.IsDriving && target.parent != null
                ? target.parent.position + Vector3.up * 1.1f
                : target.position + Vector3.up * 1.25f;
        }

        private void LateUpdate()
        {
            BindTarget();
            if (target == null) return;
            var actualFocus = TargetFocus();
            if ((_focus - actualFocus).sqrMagnitude > 400f)
            {
                _focus = actualFocus;
                _focusVelocity = Vector3.zero;
            }
            _focus = Vector3.SmoothDamp(_focus, actualFocus, ref _focusVelocity, 0.08f);
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, 0.08f);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, pitch, ref _pitchVelocity, 0.08f);
            var driving = _player != null && _player.IsDriving;
            var distance = Mathf.Clamp((driving ? drivingDistance : walkingDistance) + _zoomOffset,
                driving ? 4.5f : 2.2f, driving ? 12f : 8f);
            var rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            var desired = _focus + rotation * (Vector3.back * distance + Vector3.right * shoulderOffset);
            var ray = desired - actualFocus;
            var direction = ray.normalized;
            var resolvedDistance = ray.magnitude;
            var nearestHit = float.PositiveInfinity;
            var ignoredRoot = driving && target.parent != null ? target.parent : target;
            var count = Physics.SphereCastNonAlloc(actualFocus, 0.2f, direction, _hits,
                Mathf.Max(resolvedDistance, _currentDistance), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].collider.transform.IsChildOf(ignoredRoot)) continue;
                nearestHit = Mathf.Min(nearestHit, Mathf.Max(0.05f, _hits[i].distance - 0.03f));
            }
            resolvedDistance = Mathf.Min(resolvedDistance, nearestHit);
            _currentDistance = nearestHit < _currentDistance ? resolvedDistance
                : Mathf.MoveTowards(_currentDistance, resolvedDistance, 8f * Time.deltaTime);
            transform.position = actualFocus + direction * _currentDistance;
            transform.rotation = Quaternion.LookRotation(_focus - transform.position);
            SetSubjectHidden(_currentDistance < 0.8f);
        }

        private void SetSubjectHidden(bool hidden)
        {
            if (_subjectHidden == hidden) return;
            _subjectHidden = hidden;
            foreach (var renderer in _subjectRenderers)
                if (renderer != null) renderer.forceRenderingOff = hidden;
        }

        private void OnDisable()
        {
            SetSubjectHidden(false);
        }
    }
}
