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
            var targetSpeed = _throttle >= 0f ? _throttle * maximumSpeed : _throttle * 5f;
            var acceleration = Mathf.Abs(_throttle) > 0.01f ? accelerationRate : 11f;
            _speed = Mathf.MoveTowards(_speed, targetSpeed, acceleration * Time.fixedDeltaTime);

            var steeringStrength = Mathf.Clamp01(Mathf.Abs(_speed) / 2f);
            var steeringRate = GarageIndex == 4 ? 61f : GarageIndex == 5 ? 72f : 68f;
            var turn = _steering * steeringRate * steeringStrength * Mathf.Sign(_speed) * Time.fixedDeltaTime;
            var rotation = _body.rotation * Quaternion.Euler(0f, turn, 0f);
            var position = _body.position + rotation * Vector3.forward * _speed * Time.fixedDeltaTime;
            var safePosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(position.x, Mathf.Min(position.z, HarborlightDistrictSurfaceNetwork.VehicleMaxZ)));
            position.x = safePosition.x;
            position.z = safePosition.y;

            _body.MoveRotation(rotation);
            _body.MovePosition(position);
        }
    }
}
