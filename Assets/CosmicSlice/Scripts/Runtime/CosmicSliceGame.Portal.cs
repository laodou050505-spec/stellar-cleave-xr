using UnityEngine;
using UnityEngine.Rendering;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private const float WormholeMouthRadius = 1.48f;
        private const float WormholeDepth = 1.85f;
        private Mesh _wormholeInteriorMesh;
        private Material _wormholeMaterial;
        private Material _wormholeRimMaterial;
        private Mesh _wormholeRimMesh;

        // The ceiling's positive local Y points out of the room. All interior geometry
        // recedes along that axis. There is no opaque disc across the entrance.
        private void BuildPortal()
        {
            _portalRoot = new GameObject("Ceiling Anomaly Portal").transform;
            _portalRoot.position = CosmicSliceDesign.ArenaOrigin + Vector3.up * CosmicSliceDesign.PortalHeightAboveArena;
            if (_ceilingGridRoot != null)
                _ceilingGridRoot.position = _portalRoot.position + Vector3.up * 0.025f;

            var surface = new GameObject("Upward Spiral Wormhole Interior");
            surface.transform.SetParent(_portalRoot, false);
            _wormholeInteriorMesh = CreateWormholeInterior();
            surface.AddComponent<MeshFilter>().sharedMesh = _wormholeInteriorMesh;
            var renderer = surface.AddComponent<MeshRenderer>();
            var material = new Material(portalShader) { name = "Blue Violet Inward Vortex" };
            _wormholeMaterial = material;
            material.SetFloat("_MouthRadius", WormholeMouthRadius);
            material.SetFloat("_Depth", WormholeDepth);
            material.SetFloat("_Speed", 0.32f);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var rim = new GameObject("Feathered Nebula Mouth");
            rim.transform.SetParent(_portalRoot, false);
            _wormholeRimMesh = CreateWormholeRim();
            rim.AddComponent<MeshFilter>().sharedMesh = _wormholeRimMesh;
            var rimRenderer = rim.AddComponent<MeshRenderer>();
            _wormholeRimMaterial = new Material(material) { name = "Soft Dissolving Nebula Rim", renderQueue = 2990 };
            _wormholeRimMaterial.SetFloat("_RimOnly", 1f);
            rimRenderer.sharedMaterial = _wormholeRimMaterial;
            rimRenderer.shadowCastingMode = ShadowCastingMode.Off;
            rimRenderer.receiveShadows = false;
            Camera.onPreCull += PrepareWormholeForCamera;
            BuildPortalNebula();

            // Keep the existing beat/light animation contract, without bringing back
            // the old stacked neon rings or a dangling core.
            _portalSpinA = new GameObject("Portal Animation A").transform;
            _portalSpinA.SetParent(_portalRoot, false);
            _portalSpinB = new GameObject("Portal Animation B").transform;
            _portalSpinB.SetParent(_portalRoot, false);
            _portalLight = new GameObject("Portal Light").AddComponent<Light>();
            _portalLight.transform.SetParent(_portalRoot, false);
            _portalLight.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            _portalLight.type = LightType.Point;
            _portalLight.color = new Color(0.25f, 0.48f, 1f);
            _portalLight.range = 7.2f;
            _portalLight.intensity = 2.8f;
        }

        // One shared head-centre shear preserves binocular disparity. Only the deep
        // interior is refracted; neither the ceiling anchor nor the mouth moves.
        internal static Vector3 WormholeViewShear(Vector3 localEye)
        {
            if (localEye.y >= -0.05f) return Vector3.zero;
            return Vector3.ClampMagnitude(new Vector3(localEye.x / localEye.y, 0f, localEye.z / localEye.y), 4f);
        }

        private void PrepareWormholeForCamera(Camera renderingCamera)
        {
            if (renderingCamera != _camera || _portalRoot == null || _wormholeMaterial == null) return;
            var shear = WormholeViewShear(_portalRoot.InverseTransformPoint(_camera.transform.position));
            _wormholeMaterial.SetVector("_TunnelShear", new Vector4(shear.x, 0f, shear.z, 0f));
        }

        private void OnDestroy()
        {
            Camera.onPreCull -= PrepareWormholeForCamera;
            DestroyPortalNebula();
            if (_wormholeInteriorMesh != null) Destroy(_wormholeInteriorMesh);
            if (_wormholeRimMesh != null) Destroy(_wormholeRimMesh);
            if (_wormholeMaterial != null) Destroy(_wormholeMaterial);
            if (_wormholeRimMaterial != null) Destroy(_wormholeRimMaterial);
        }

        private static Mesh CreateWormholeRim()
        {
            const float outerRadius = 1.74f; // Fits the existing 2m pulse-reserved footprint.
            const int sectors = 160;
            var vertices = new Vector3[(sectors + 1) * 2];
            var triangles = new int[sectors * 6];
            for (var i = 0; i <= sectors; i++)
            {
                var angle = i * Mathf.PI * 2f / sectors;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * (WormholeMouthRadius * 0.72f) + Vector3.down * 0.003f;
                vertices[i * 2 + 1] = direction * outerRadius + Vector3.down * 0.003f;
                if (i == sectors) continue;
                var t = i * 6; var v = i * 2;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            return new Mesh { name = "Feathered Mouth Annulus", vertices = vertices, triangles = triangles };
        }

        private static Mesh CreateWormholeInterior()
        {
            const int sectors = 160;
            const int rings = 48;
            var vertices = new Vector3[(sectors + 1) * (rings + 1) + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[sectors * rings * 6 + sectors * 3];
            for (var row = 0; row <= rings; row++)
            {
                var depth = row / (float)rings;
                var radius = 0.18f + (WormholeMouthRadius - 0.18f) * Mathf.Pow(1f - depth, 2.1f);
                var height = WormholeDepth * Mathf.Pow(depth, 1.18f);
                for (var col = 0; col <= sectors; col++)
                {
                    var angle = col / (float)sectors * Mathf.PI * 2f;
                    var i = row * (sectors + 1) + col;
                    var ripple = 1f + Mathf.Sin(angle * 3f + depth * 8f) * 0.028f * Mathf.Sin(depth * Mathf.PI);
                    vertices[i] = new Vector3(Mathf.Cos(angle) * radius * ripple, height, Mathf.Sin(angle) * radius * ripple);
                    uv[i] = new Vector2(col / (float)sectors, depth);
                }
            }
            var triangle = 0;
            for (var row = 0; row < rings; row++)
                for (var col = 0; col < sectors; col++)
                {
                    var i = row * (sectors + 1) + col;
                    var j = i + sectors + 1;
                    triangles[triangle++] = i;
                    triangles[triangle++] = j;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = j;
                    triangles[triangle++] = j + 1;
                }
            // Close only the far end; the mouth is deliberately left open.
            var center = vertices.Length - 1;
            vertices[center] = new Vector3(0f, WormholeDepth, 0f);
            uv[center] = new Vector2(0.5f, 1f);
            var last = rings * (sectors + 1);
            for (var col = 0; col < sectors; col++)
            {
                triangles[triangle++] = center;
                triangles[triangle++] = last + col;
                triangles[triangle++] = last + col + 1;
            }
            var mesh = new Mesh { name = "Open Mouth Upward Curved Vortex" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            // Account for the bounded visual refraction used by the shader.
            mesh.bounds = new Bounds(Vector3.up * (WormholeDepth * 0.5f),
                new Vector3(18f, WormholeDepth + 0.02f, 18f));
            return mesh;
        }
    }
}
