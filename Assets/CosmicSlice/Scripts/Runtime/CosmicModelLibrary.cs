using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace CosmicSlice
{
    public sealed class CosmicModelLibrary : MonoBehaviour
    {
        private readonly Dictionary<string, Task<GltfImport>> _loading = new();
        private readonly Dictionary<string, Material> _repairedMaterials = new();
        private readonly Dictionary<TargetKind, string> _targetFiles = new()
        {
            { TargetKind.Plasma, "PlasmaCore.glb" },
            { TargetKind.Meteor, "StylizedMeteor.glb" },
            { TargetKind.Quantum, "QuantumPyramid.glb" },
            { TargetKind.Bomb, "UnstableBomb.glb" }
        };

        private void Start()
        {
            _ = Load("CeilingPortalRing.glb");
            foreach (var file in _targetFiles.Values)
            {
                _ = Load(file);
            }
        }

        public void AttachPortal(Transform parent, float size)
        {
            _ = Attach("CeilingPortalRing.glb", parent, size, Quaternion.Euler(0f, 0f, 90f));
        }

        public void AttachTarget(TargetKind kind, Transform parent, float size)
        {
            if (_targetFiles.TryGetValue(kind, out var file))
            {
                _ = Attach(file, parent, size, Quaternion.identity);
            }
        }

        private async Task Attach(string file, Transform parent, float size, Quaternion rotation)
        {
            Transform host = null;
            try
            {
                var importer = await Load(file);
                if (importer == null || parent == null)
                {
                    return;
                }

                host = new GameObject("ANGE Model - " + Path.GetFileNameWithoutExtension(file)).transform;
                host.SetParent(parent, false);
                host.localRotation = rotation;
                var success = await importer.InstantiateMainSceneAsync(host);
                if (!success || parent == null)
                {
                    if (host != null)
                    {
                        Destroy(host.gameObject);
                    }
                    Debug.LogWarning($"COSMIC_MODEL_ATTACH_FAILED file={file}");
                    return;
                }

                Normalize(host, size);
                RepairMaterials(host, file);
                var fallback = parent.Find("Fallback Visual");
                if (fallback != null)
                {
                    Destroy(fallback.gameObject);
                }
            }
            catch (Exception exception)
            {
                if (host != null)
                {
                    Destroy(host.gameObject);
                }
                Debug.LogWarning($"COSMIC_MODEL_ATTACH_FAILED file={file} message={exception.Message}");
            }
        }

        private Task<GltfImport> Load(string file)
        {
            if (!_loading.TryGetValue(file, out var task))
            {
                task = LoadInternal(file);
                _loading.Add(file, task);
            }
            return task;
        }

        private static async Task<GltfImport> LoadInternal(string file)
        {
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "CosmicSlice", "Models", file);
                var uri = path.Contains("://", StringComparison.Ordinal) ? path : new Uri(path).AbsoluteUri;
                var importer = new GltfImport();
                if (!await importer.Load(uri))
                {
                    Debug.LogWarning($"COSMIC_MODEL_LOAD_FAILED file={file} uri={uri}");
                    return null;
                }

                Debug.Log($"COSMIC_MODEL_READY file={file}");
                return importer;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"COSMIC_MODEL_LOAD_FAILED file={file} message={exception.Message}");
                return null;
            }
        }

        private static void Normalize(Transform host, float desiredSize)
        {
            var renderers = host.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.001f)
            {
                host.localScale *= desiredSize / largest;
            }

            bounds = host.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var renderer in host.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            host.position += parentCenter(host) - bounds.center;
        }

        private static Vector3 parentCenter(Transform host)
        {
            return host.parent != null ? host.parent.position : Vector3.zero;
        }

        private void RepairMaterials(Transform host, string file)
        {
            var standard = Shader.Find("Standard");
            if (standard == null)
            {
                return;
            }

            var fallbackColor = file.Contains("Plasma", StringComparison.OrdinalIgnoreCase) ? new Color(0.3f, 0.82f, 0.92f)
                : file.Contains("Meteor", StringComparison.OrdinalIgnoreCase) ? new Color(0.33f, 0.27f, 0.23f)
                : file.Contains("Quantum", StringComparison.OrdinalIgnoreCase) ? new Color(0.36f, 0.18f, 0.48f)
                : file.Contains("Bomb", StringComparison.OrdinalIgnoreCase) ? new Color(0.32f, 0.06f, 0.07f)
                : new Color(0.28f, 0.34f, 0.39f);

            foreach (var renderer in host.GetComponentsInChildren<Renderer>())
            {
                var sourceMaterials = renderer.sharedMaterials;
                var repaired = new Material[sourceMaterials.Length];
                for (var index = 0; index < sourceMaterials.Length; index++)
                {
                    var source = sourceMaterials[index];
                    var sourceName = source != null ? source.name : "Empty Material";
                    var materialKey = $"{file}/{renderer.gameObject.name}/{index}/{sourceName}";
                    if (_repairedMaterials.TryGetValue(materialKey, out var cached))
                    {
                        repaired[index] = cached;
                        continue;
                    }

                    var material = new Material(standard) { name = sourceName + " - PICO Compatible" };
                    var texture = source != null && source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap")
                        : source != null && source.HasProperty("_MainTex") ? source.GetTexture("_MainTex")
                        : null;
                    var color = source != null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                        : source != null && source.HasProperty("_Color") ? source.GetColor("_Color")
                        : fallbackColor;
                    if (color.maxColorComponent < 0.08f)
                    {
                        color = fallbackColor;
                    }
                    material.color = color;
                    material.SetFloat("_Metallic", 0.42f);
                    material.SetFloat("_Glossiness", 0.68f);
                    if (texture != null)
                    {
                        material.SetTexture("_MainTex", texture);
                    }
                    _repairedMaterials.Add(materialKey, material);
                    repaired[index] = material;
                }
                renderer.sharedMaterials = repaired;
            }
        }
    }
}
