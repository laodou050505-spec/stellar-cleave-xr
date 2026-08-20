using UnityEngine;

namespace CosmicSlice
{
    public sealed class CosmicOrbitalProp : MonoBehaviour
    {
        private Transform _center;
        private Vector3 _initialOffset;
        private Vector3 _orbitAxis;
        private Vector3 _driftAxis;
        private Vector3 _spinAxis;
        private float _orbitAngle;
        private float _orbitSpeed;
        private float _driftAmplitude;
        private float _driftSpeed;
        private float _driftPhase;
        private float _spinSpeed;

        public void Configure(
            Transform center,
            Vector3 orbitAxis,
            float orbitSpeed,
            Vector3 driftAxis,
            float driftAmplitude,
            float driftSpeed,
            Vector3 spinAxis,
            float spinSpeed,
            float phase)
        {
            _center = center;
            _initialOffset = transform.position - (center != null ? center.position : Vector3.zero);
            _orbitAxis = orbitAxis.sqrMagnitude > 0.001f ? orbitAxis.normalized : Vector3.up;
            _driftAxis = driftAxis.sqrMagnitude > 0.001f ? driftAxis.normalized : Vector3.zero;
            _spinAxis = spinAxis.sqrMagnitude > 0.001f ? spinAxis.normalized : Vector3.up;
            _orbitSpeed = orbitSpeed;
            _driftAmplitude = driftAmplitude;
            _driftSpeed = driftSpeed;
            _spinSpeed = spinSpeed;
            _driftPhase = phase;
            _orbitAngle = 0f;
        }

        private void Update()
        {
            if (_center == null)
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            _orbitAngle = Mathf.Repeat(_orbitAngle + _orbitSpeed * deltaTime, 360f);
            var orbit = Quaternion.AngleAxis(_orbitAngle, _orbitAxis) * _initialOffset;
            var drift = _driftAxis * (Mathf.Sin(Time.unscaledTime * _driftSpeed + _driftPhase) * _driftAmplitude);
            transform.position = _center.position + orbit + drift;
            transform.Rotate(_spinAxis, _spinSpeed * deltaTime, Space.Self);
        }
    }
}
