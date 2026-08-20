using UnityEngine;

namespace CosmicSlice
{
    public sealed class FallingTarget : MonoBehaviour
    {
        public TargetKind Kind { get; private set; }
        public bool QuantumPrimed { get; private set; }
        public bool Resolved { get; private set; }

        private CosmicSliceGame _game;
        private Vector3 _velocity;
        private Vector3 _angularVelocity;
        private float _lastHitTime = -10f;
        private float _spawnTime;
        private Renderer[] _renderers;
        private Color _baseColor;
        private float _wrongFlash;
        private HandSide _quantumFirstHand;
        private float _quantumFirstHitTime;

        public void Configure(CosmicSliceGame game, TargetKind kind, Vector3 velocity)
        {
            _game = game;
            Kind = kind;
            _velocity = velocity;
            _angularVelocity = Random.onUnitSphere * Random.Range(65f, 150f);
            _spawnTime = Time.unscaledTime;
            transform.localScale = Vector3.zero;
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColor = CosmicPalette.For(kind);
        }

        private void Update()
        {
            if (Resolved)
            {
                return;
            }

            var intro = Mathf.Clamp01((Time.unscaledTime - _spawnTime) / 0.18f);
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, intro);

            _velocity += Vector3.down * (0.72f * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(_angularVelocity * Time.deltaTime, Space.World);

            if (_wrongFlash > 0f)
            {
                _wrongFlash -= Time.deltaTime;
                var pulse = Mathf.PingPong(Time.time * 14f, 1f);
                foreach (var renderer in _renderers)
                {
                    if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", Color.Lerp(_baseColor * 0.6f, Color.white, pulse));
                    }
                }
            }

            if (transform.position.y < 0.18f)
            {
                Resolved = true;
                _game.TargetMissed(this);
            }
        }

        public bool CanReceiveHit()
        {
            return !Resolved && Time.unscaledTime - _lastHitTime > 0.16f;
        }

        public void RegisterHit()
        {
            _lastHitTime = Time.unscaledTime;
        }

        public void PrimeQuantum(HandSide firstHand, float time)
        {
            QuantumPrimed = true;
            _quantumFirstHand = firstHand;
            _quantumFirstHitTime = time;
            _velocity.y = Mathf.Max(_velocity.y, -0.35f);
        }

        public bool TryCompleteQuantum(HandSide hand, float time)
        {
            if (!QuantumPrimed)
            {
                return false;
            }
            if (time - _quantumFirstHitTime > 0.9f)
            {
                QuantumPrimed = false;
                return false;
            }
            return hand != _quantumFirstHand;
        }

        public void RegisterWrongHand()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _wrongFlash = 0.45f;
            _velocity += new Vector3(Random.Range(-0.18f, 0.18f), 0.12f, 0f);
        }

        public void Resolve()
        {
            Resolved = true;
        }
    }
}
