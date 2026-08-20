using UnityEngine;

namespace CosmicSlice
{
    public sealed class CosmicLifetime : MonoBehaviour
    {
        public float lifetime = 1f;
        public bool shrink;
        public bool expand;
        public bool fadeLine;
        public float expansion = 2f;
        public Vector3 spin;

        private float _age;
        private Vector3 _startScale;
        private LineRenderer _line;
        private float _startWidth;
        private Color _startColor;
        private Color _endColor;

        private void Awake()
        {
            _startScale = transform.localScale;
            _line = GetComponent<LineRenderer>();
            if (_line != null)
            {
                _startWidth = _line.widthMultiplier;
                _startColor = _line.startColor;
                _endColor = _line.endColor;
            }
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(_age / Mathf.Max(0.01f, lifetime));
            if (shrink)
            {
                transform.localScale = _startScale * (1f - progress);
            }
            else if (expand)
            {
                var eased = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = _startScale * Mathf.Lerp(1f, expansion, eased);
            }

            if (spin.sqrMagnitude > 0.001f)
            {
                transform.Rotate(spin * Time.unscaledDeltaTime, Space.Self);
            }

            if (fadeLine && _line != null)
            {
                var fade = 1f - progress;
                var start = _startColor;
                var end = _endColor;
                start.a *= fade;
                end.a *= fade;
                _line.startColor = start;
                _line.endColor = end;
                _line.widthMultiplier = _startWidth * fade;
            }

            if (_age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
