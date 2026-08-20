using UnityEngine;

namespace CosmicSlice
{
    public sealed class CosmicCruisingProp : MonoBehaviour
    {
        private Vector3 _origin;
        private Vector3 _travelDirection;
        private Vector3 _driftAxis;
        private Vector3 _spinAxis;
        private float _speed;
        private float _span;
        private float _driftAmplitude;
        private float _driftSpeed;
        private float _spinSpeed;
        private float _phase;

        public void Configure(
            Vector3 travelDirection,
            float speed,
            float span,
            Vector3 driftAxis,
            float driftAmplitude,
            float driftSpeed,
            Vector3 spinAxis,
            float spinSpeed,
            float phase)
        {
            _origin = transform.position;
            _travelDirection = travelDirection.sqrMagnitude > 0.001f ? travelDirection.normalized : Vector3.forward;
            _driftAxis = driftAxis.sqrMagnitude > 0.001f ? driftAxis.normalized : Vector3.up;
            _spinAxis = spinAxis.sqrMagnitude > 0.001f ? spinAxis.normalized : Vector3.up;
            _speed = Mathf.Max(0.01f, speed);
            _span = Mathf.Max(2f, span);
            _driftAmplitude = Mathf.Max(0f, driftAmplitude);
            _driftSpeed = Mathf.Max(0.01f, driftSpeed);
            _spinSpeed = spinSpeed;
            _phase = phase;
        }

        private void Update()
        {
            var cruise = Mathf.Repeat(_phase + Time.unscaledTime * _speed, _span) - _span * 0.5f;
            var drift = _driftAxis * (Mathf.Sin(Time.unscaledTime * _driftSpeed + _phase) * _driftAmplitude);
            transform.position = _origin + _travelDirection * cruise + drift;
            transform.Rotate(_spinAxis, _spinSpeed * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
