using System.Collections.Generic;
using UnityEngine;

namespace Harborlight
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class HarborlightVehicle : MonoBehaviour
    {
        private Rigidbody _body;
        private bool _occupied;
        private float _throttle;
        private float _steering;
        private float _speed;
        private Color _factoryPaintColor = Color.white;
        private Renderer[] _paintRenderers = new Renderer[0];
        private WheelVisual[] _wheelVisuals = new WheelVisual[0];
        private float _wheelSpinDegrees;
        private float _frontWheelSteeringAngle;

        private sealed class WheelVisual
        {
            public Transform Pivot;
            public Quaternion BaseRotation;
            public Vector3 SpinAxis;
            public bool Steers;
        }

        public string DisplayName { get; private set; }
        public int GarageIndex { get; private set; }
        public float Speed => _speed;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        public void Initialize(string displayName, Color factoryPaintColor, int garageIndex)
        {
            DisplayName = displayName;
            _factoryPaintColor = factoryPaintColor;
            GarageIndex = garageIndex;
        }

        public void CachePaintableParts()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var paintable = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in renderers)
            {
                var partName = renderer.gameObject.name;
                if (partName.Contains("paintable") ||
                    (!partName.Contains("glass") && !partName.Contains("window") &&
                     (partName.Contains("lower body") || partName.Contains("cabin") || partName.Contains("roof"))))
                    paintable.Add(renderer);
            }
            _paintRenderers = paintable.ToArray();

            var wheels = new List<WheelVisual>();
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                if (part == transform) continue;
                var partName = part.name.ToLowerInvariant();
                var isFrontWheel = partName.Contains("frontleftwheel") || partName.Contains("frontrightwheel");
                var isRearWheelGroup = partName.Contains("backwheels");
                var isTire = partName.Contains("tire");
                var isAlloyFace = partName.Contains("alloy wheel face");
                if (!isFrontWheel && !isRearWheelGroup && !isTire && !isAlloyFace) continue;

                wheels.Add(new WheelVisual
                {
                    Pivot = part,
                    BaseRotation = part.localRotation,
                    SpinAxis = isTire ? Vector3.up : Vector3.right,
                    Steers = isFrontWheel || ((isTire || isAlloyFace) && part.localPosition.z > 0f)
                });
            }
            _wheelVisuals = wheels.ToArray();
        }

        public void RestoreFactoryPaint()
        {
            SetPaintColor(_factoryPaintColor);
        }

        public void SetPaintColor(Color color)
        {
            foreach (var renderer in _paintRenderers)
            {
                var material = renderer.material;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                else material.color = color;
            }
        }

        public void SetOccupied(bool occupied)
        {
            _occupied = occupied;
            _throttle = 0f;
            _steering = 0f;
            _speed = 0f;

            if (occupied)
            {
                _body.isKinematic = false;
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            else
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.isKinematic = true;
            }
        }

        public void SetDriveInput(float throttle, float steering)
        {
            _throttle = Mathf.Clamp(throttle, -1f, 1f);
            _steering = Mathf.Clamp(steering, -1f, 1f);
        }

        private void FixedUpdate()
        {
            if (!_occupied) return;

            var maximumSpeed = GarageIndex == 3 ? 16f : GarageIndex == 4 ? 13f : GarageIndex == 5 ? 21f : 15f;
            var accelerationRate = GarageIndex == 3 ? 9f : GarageIndex == 4 ? 7f : GarageIndex == 5 ? 12f : 8f;
            var forwardSpeed = Vector3.Dot(_body.linearVelocity, transform.forward);
            var oppositeDirection = Mathf.Abs(_throttle) > 0.01f && Mathf.Abs(forwardSpeed) > 0.25f &&
                                   Mathf.Sign(_throttle) != Mathf.Sign(forwardSpeed);
            var targetSpeed = _throttle >= 0f ? _throttle * maximumSpeed : _throttle * 5f;
            if (oppositeDirection) targetSpeed = 0f;

            var speedError = targetSpeed - forwardSpeed;
            var forwardAcceleration = Mathf.Abs(_throttle) > 0.01f ? accelerationRate : 3.5f;
            var brakingAcceleration = oppositeDirection ? 18f : 3.5f;
            var acceleration = Mathf.Clamp(speedError / Time.fixedDeltaTime,
                -brakingAcceleration, forwardAcceleration);
            _body.AddForce(transform.forward * acceleration, ForceMode.Acceleration);

            var lateralSpeed = Vector3.Dot(_body.linearVelocity, transform.right);
            _body.AddForce(-transform.right * lateralSpeed * 5f, ForceMode.Acceleration);

            var speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maximumSpeed);
            var steeringAngle = Mathf.Lerp(32f, 5f, speedRatio);
            _frontWheelSteeringAngle = _steering * steeringAngle;
            var yawRate = forwardSpeed / 2.65f * Mathf.Tan(steeringAngle * Mathf.Deg2Rad) *
                          _steering * Mathf.Rad2Deg;
            yawRate = Mathf.Clamp(yawRate, -72f, 72f);
            _body.MoveRotation(_body.rotation * Quaternion.Euler(0f, yawRate * Time.fixedDeltaTime, 0f));

            var velocity = _body.linearVelocity;
            var safeSpeed = Mathf.Clamp(forwardSpeed, -5f, maximumSpeed);
            _body.linearVelocity = velocity + transform.forward * (safeSpeed - forwardSpeed);
            _speed = safeSpeed;

            var position = _body.position;
            var requestedLandPosition = new Vector2(position.x, Mathf.Min(position.z, HarborlightDistrictSurfaceNetwork.VehicleMaxZ));
            var safeLandPosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(requestedLandPosition);
            if ((safeLandPosition - requestedLandPosition).sqrMagnitude > 0.0001f)
            {
                position.x = safeLandPosition.x;
                position.z = safeLandPosition.y;
                _body.position = position;
                var boundedVelocity = _body.linearVelocity;
                if (Mathf.Abs(safeLandPosition.x - requestedLandPosition.x) > 0.001f) boundedVelocity.x = 0f;
                if (Mathf.Abs(safeLandPosition.y - requestedLandPosition.y) > 0.001f) boundedVelocity.z = 0f;
                _body.linearVelocity = boundedVelocity;
            }

            _wheelSpinDegrees = Mathf.Repeat(_wheelSpinDegrees + _speed / 0.36f * Mathf.Rad2Deg * Time.fixedDeltaTime, 360f);
            foreach (var wheel in _wheelVisuals)
            {
                var steering = wheel.Steers
                    ? Quaternion.AngleAxis(_frontWheelSteeringAngle, Vector3.up)
                    : Quaternion.identity;
                var spin = Quaternion.AngleAxis(_wheelSpinDegrees, wheel.SpinAxis);
                wheel.Pivot.localRotation = steering * wheel.BaseRotation * spin;
            }
        }
    }
}
