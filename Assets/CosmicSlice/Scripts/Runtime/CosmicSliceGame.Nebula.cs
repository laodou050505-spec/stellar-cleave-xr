using UnityEngine;
using UnityEngine.Rendering;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private const float NebulaInnerRadius = 1.32f;
        private const float NebulaMaxRadius = 2.7f;
        private const float NebulaPulseReserve = 1.13f;
        private Transform _nebulaRoot;
        private Mesh _nebulaMesh;
        private readonly Material[] _nebulaMaterials = new Material[2];
        private float _nebulaOuterRadius = NebulaMaxRadius;
        private float _nebulaTime;
        private float _nebulaReveal;

        private void BuildPortalNebula()
        {
            if (nebulaShader == null || portalNebulaTexture == null)
                throw new System.InvalidOperationException("Missing localized portal nebula shader or texture.");
            _nebulaRoot = new GameObject("Localized Drifting Cosmic Nebula").transform;
            _nebulaRoot.SetParent(_portalRoot, false);
            _nebulaMesh = CreateNebulaMesh();
            for (var layer = 0; layer < 2; layer++)
            {
                var cloud = new GameObject(layer == 0 ? "Wispy Blue Cloud" : "Violet Parallax Wisps");
                cloud.transform.SetParent(_nebulaRoot, false);
                cloud.AddComponent<MeshFilter>().sharedMesh = _nebulaMesh;
                var renderer = cloud.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var material = new Material(nebulaShader) { name = "Localized Nebula Layer " + layer, renderQueue = 2970 + layer };
                material.SetTexture("_MainTex", portalNebulaTexture);
                material.SetFloat("_Layer", layer);
                material.SetFloat("_OuterRadius", _nebulaOuterRadius);
                material.SetFloat("_Reveal", 0f);
                renderer.sharedMaterial = material;
                _nebulaMaterials[layer] = material;
            }
        }

        // Vertex motion stays within the annulus. It never billboards toward an eye,
        // moves the ceiling anchor, or extends the virtual tunnel into the room.
        private static Mesh CreateNebulaMesh()
        {
            const int sectors = 128;
            const int rows = 8;
            var vertices = new Vector3[(sectors + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[sectors * rows * 6];
            for (var row = 0; row <= rows; row++)
                for (var col = 0; col <= sectors; col++)
                {
                    var t = row / (float)rows;
                    var angle = col * Mathf.PI * 2f / sectors;
                    var i = row * (sectors + 1) + col;
                    vertices[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Lerp(NebulaInnerRadius, NebulaMaxRadius, t);
                    uv[i] = new Vector2(t, col / (float)sectors);
                }
            var triangle = 0;
            for (var row = 0; row < rows; row++)
                for (var col = 0; col < sectors; col++)
                {
                    var i = row * (sectors + 1) + col;
                    var j = i + sectors + 1;
                    triangles[triangle++] = i; triangles[triangle++] = j; triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + 1; triangles[triangle++] = j; triangles[triangle++] = j + 1;
                }
            return new Mesh
            {
                name = "Bounded Shallow Nebula Annulus", vertices = vertices, uv = uv, triangles = triangles,
                bounds = new Bounds(Vector3.down * 0.09f, new Vector3(NebulaMaxRadius * 2f, 0.24f, NebulaMaxRadius * 2f))
            };
        }

        internal static float NebulaRadiusForClearance(float clearance, float portalScale)
        {
            // Reserve the original breathing/beat expansion and a further wall margin.
            return Mathf.Clamp((clearance - 0.08f) / (Mathf.Max(portalScale, 0.01f) * NebulaPulseReserve), NebulaInnerRadius, NebulaMaxRadius);
        }

        private void FitNebulaToCeiling(Vector3 origin, Quaternion rotation, Vector2[] boundary, Vector3 portalPosition, float portalScale)
        {
            var local = Quaternion.Inverse(rotation) * (portalPosition - origin);
            var point = new Vector2(local.x, local.y);
            var clearance = float.PositiveInfinity;
            for (var i = 0; i < boundary.Length; i++)
            {
                var a = boundary[i];
                var edge = boundary[(i + 1) % boundary.Length] - a;
                var t = edge.sqrMagnitude < 0.00001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                clearance = Mathf.Min(clearance, Vector2.Distance(point, a + edge * t));
            }
            SetNebulaRadius(NebulaRadiusForClearance(clearance, portalScale));
        }

        private void SetNebulaRadius(float radius)
        {
            _nebulaOuterRadius = radius;
            foreach (var material in _nebulaMaterials)
                if (material != null) material.SetFloat("_OuterRadius", radius);
        }

        private void AnimatePortalNebula(float deltaTime)
        {
            if (_nebulaRoot == null) return;
            if (!_portalRoot.gameObject.activeInHierarchy)
            {
                _nebulaReveal = 0f;
                foreach (var material in _nebulaMaterials)
                    if (material != null) material.SetFloat("_Reveal", 0f);
                return;
            }
            // Pause clocks while hidden or unfocused; do not advance through a resume hitch.
            if (!Application.isFocused && !_qaMode) return;
            var dt = Mathf.Clamp(deltaTime, 0f, 0.05f);
            _nebulaTime += dt;
            _nebulaReveal = Mathf.MoveTowards(_nebulaReveal, 1f, dt / 0.9f);
            foreach (var material in _nebulaMaterials)
            {
                material.SetFloat("_FlowTime", _nebulaTime);
                material.SetFloat("_Reveal", _nebulaReveal);
            }
        }

        private void DestroyPortalNebula()
        {
            if (_nebulaMesh != null) Destroy(_nebulaMesh);
            foreach (var material in _nebulaMaterials)
                if (material != null) Destroy(material);
        }
    }
}
