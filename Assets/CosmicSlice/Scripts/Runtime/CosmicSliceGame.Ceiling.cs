using UnityEngine;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private bool _ceilingScanComplete;
        private bool _ceilingPreviewSelected;
        private float _portalCeilingScale = 1f;
        private bool CanUsePortal => _hasDetectedCeiling || _ceilingPreviewSelected;

        public bool ApplyDetectedCeiling(Vector3 trackingPosition, Quaternion trackingRotation, Vector2[] boundary)
        {
            if (_portalRoot == null) return false;
            var worldPosition = _rigTransform != null ? _rigTransform.TransformPoint(trackingPosition) : trackingPosition;
            var worldRotation = _rigTransform != null ? _rigTransform.rotation * trackingRotation : trackingRotation;
            var eye = _camera != null ? _camera.transform.position : Vector3.up * 1.65f;
            var preferred = CosmicSliceDesign.ArenaOrigin;
            if (_camera != null)
            {
                var normal = worldRotation * Vector3.forward;
                if (normal.y < 0f) normal = -normal;
                var horizontal = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up);
                if (horizontal.sqrMagnitude < 0.01f) horizontal = Vector3.forward;
                horizontal.Normalize();
                preferred = eye + horizontal * 3.4f;
                var denominator = Vector3.Dot(_camera.transform.forward, normal);
                if (Mathf.Abs(denominator) > 0.05f)
                {
                    var distance = Vector3.Dot(worldPosition - eye, normal) / denominator;
                    if (distance > 0f && distance < 15f)
                        preferred = eye + _camera.transform.forward * distance;
                }
                preferred.y = worldPosition.y;
            }
            if (!TryFitCeilingPolygon(worldPosition, worldRotation, boundary, eye, preferred, out var point, out var rotation, out var scale)) return false;
            _hasDetectedCeiling = true;
            _ceilingScanComplete = true;
            _ceilingPreviewSelected = false;
            _portalCeilingScale = scale;
            _detectedCeilingPosition = point;
            _portalRoot.SetPositionAndRotation(point, rotation);
            _portalRoot.localScale = new Vector3(scale, 1f, scale);
            FitNebulaToCeiling(worldPosition, worldRotation, boundary, point, scale);
            if (_ceilingGridRoot != null) _ceilingGridRoot.SetPositionAndRotation(point + rotation * Vector3.up * 0.015f, rotation);
            if (_state == GameState.Ready) { _portalRoot.gameObject.SetActive(true); RefreshCeilingReadyStatus(); }
            UpdateMeteorShowerAnchors();
            _portalPulse = 0.45f;
            Debug.Log($"COSMIC_CEILING_LOCKED boundaryVertices={boundary.Length} worldPosition={point} scale={scale:F3} normal={rotation * Vector3.up}");
            return true;
        }

        // Scene Capture plane vertices use the anchor's local XY plane. Reserve a radius
        // around the selected point, including the portal's maximum breathing/pulse extent.
        internal static bool TryFitCeilingPolygon(Vector3 origin, Quaternion anchorRotation, Vector2[] polygon, Vector3 eye, Vector3 preferred, out Vector3 position, out Quaternion rotation, out float scale)
        {
            position = Vector3.zero; rotation = Quaternion.identity; scale = 1f;
            if (polygon == null || polygon.Length < 3 || !Finite(origin.x) || !Finite(origin.y) || !Finite(origin.z)) return false;
            var normal = anchorRotation * Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.9f) return false;
            if (normal.y < 0f) normal = -normal;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var centroid = Vector2.zero;
            for (var i = 0; i < polygon.Length; i++)
            {
                if (!Finite(polygon[i].x) || !Finite(polygon[i].y)) return false;
                min = Vector2.Min(min, polygon[i]); max = Vector2.Max(max, polygon[i]); centroid += polygon[i];
            }
            centroid /= polygon.Length;
            var localPreferred3 = Quaternion.Inverse(anchorRotation) * (preferred - origin);
            var localPreferred = new Vector2(localPreferred3.x, localPreferred3.y);
            var best = Vector2.zero; var bestRadius = 0f; var bestScore = float.NegativeInfinity;
            void Consider(Vector2 candidate)
            {
                if (!PointInsidePolygon(candidate, polygon)) return;
                var radius = float.PositiveInfinity;
                for (var i = 0; i < polygon.Length; i++)
                {
                    var a = polygon[i]; var edge = polygon[(i + 1) % polygon.Length] - a;
                    var t = edge.sqrMagnitude < 0.00001f ? 0f : Mathf.Clamp01(Vector2.Dot(candidate - a, edge) / edge.sqrMagnitude);
                    radius = Mathf.Min(radius, Vector2.Distance(candidate, a + edge * t));
                }
                radius -= 0.08f;
                if (radius < 0.55f) return;
                var world = origin + anchorRotation * new Vector3(candidate.x, candidate.y, 0f);
                if (world.y < eye.y + 0.45f || Vector3.Distance(world, eye) > 7f) return;
                var score = Mathf.Min(radius, 2f) * 2f - Vector2.Distance(candidate, localPreferred) * 0.35f;
                if (score <= bestScore) return;
                best = candidate; bestRadius = radius; bestScore = score;
            }
            Consider(localPreferred); Consider(centroid);
            for (var x = 1; x < 12; x++)
                for (var y = 1; y < 12; y++)
                    Consider(new Vector2(Mathf.Lerp(min.x, max.x, x / 12f), Mathf.Lerp(min.y, max.y, y / 12f)));
            if (bestRadius <= 0f) return false;
            // Positive Y normal points out of the room. Put the visible surface 18 mm below it.
            position = origin + anchorRotation * new Vector3(best.x, best.y, 0f) - normal * 0.018f;
            rotation = Quaternion.FromToRotation(Vector3.up, normal);
            scale = Mathf.Min(1f, bestRadius / 2f);
            return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool PointInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i]; var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
        public void ResetDetectedCeilingToDesignPose()
        {
            _hasDetectedCeiling = false;
            _ceilingPreviewSelected = false;
            _ceilingScanComplete = false;
#if !UNITY_ANDROID || UNITY_EDITOR
            _ceilingScanComplete = true;
#endif
            _portalCeilingScale = 1f;
            SetNebulaRadius(NebulaMaxRadius);
            _detectedCeilingPosition = CosmicSliceDesign.ArenaOrigin + Vector3.up * CosmicSliceDesign.PortalHeightAboveArena;
            if (_portalRoot != null) { _portalRoot.SetPositionAndRotation(_detectedCeilingPosition, Quaternion.identity); _portalRoot.gameObject.SetActive(false); }
            if (_ceilingGridRoot != null) _ceilingGridRoot.SetPositionAndRotation(_detectedCeilingPosition + Vector3.up * 0.015f, Quaternion.identity);
            UpdateMeteorShowerAnchors();
        }
        public void ReportCeilingScanFinished()
        {
            _ceilingScanComplete = true;
            if (_state == GameState.Ready) RefreshCeilingReadyStatus();
        }
        private void SelectCeilingPreview()
        {
            if (_hasDetectedCeiling || !_ceilingScanComplete) return;
            _ceilingPreviewSelected = true;
            _portalRoot.gameObject.SetActive(true);
            _portalRoot.localScale = Vector3.one;
            RefreshCeilingReadyStatus();
            Debug.Log("COSMIC_CEILING_PREVIEW_SELECTED realCeiling=false");
        }
        private void RefreshCeilingReadyStatus()
        {
            if (_statusText == null) return;
            _statusText.text = _hasDetectedCeiling ? "CEILING ANCHORED\nREADY TO PLAY" : _ceilingPreviewSelected ? "PREVIEW POSITION\nNOT ATTACHED TO YOUR CEILING" : _ceilingScanComplete ? "CEILING NOT FOUND\nSCAN YOUR ROOM OR TRY A PREVIEW" : "LOOK UP TO FIND THE CEILING\nROOM SCAN IN PROGRESS";
            RefreshConsolePresentation();
        }
    }
}
