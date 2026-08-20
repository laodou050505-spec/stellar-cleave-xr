using System;
using System.Threading;
using ByteDance.PICO.XR;
using UnityEngine;

namespace CosmicSlice
{
    public sealed class PicoCeilingLocator : MonoBehaviour
    {
        private CancellationTokenSource _cancellation;
        private bool _scanStarted;

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_scanStarted)
            {
                return;
            }
            var game = GetComponent<CosmicSliceGame>();
            if (game == null || !game.IsGameplayStageActive)
            {
                return;
            }
            _scanStarted = true;
            _cancellation = new CancellationTokenSource();
            PXR_Manager.EnableVideoSeeThrough = true;
            PXR_MixedReality.EnableVideoSeeThroughEffect(false);
            LocateCeiling(_cancellation.Token);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private async void LocateCeiling(CancellationToken token)
        {
            try
            {
                Debug.Log("COSMIC_CEILING_SCAN_STARTED");
                var providerResult = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SceneCapture);
                if (providerResult != PxrResult.SUCCESS || token.IsCancellationRequested)
                {
                    Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK provider={providerResult}");
                    return;
                }

                var query = await PXR_MixedReality.QuerySceneAnchorAsync(
                    new[] { PxrSemanticLabel.Ceiling }, token);
                if (query.result != PxrResult.SUCCESS || query.anchorHandleList == null || query.anchorHandleList.Count == 0)
                {
                    var captureResult = await PXR_MixedReality.StartSceneCaptureAsync(token);
                    if (captureResult != PxrResult.SUCCESS || token.IsCancellationRequested)
                    {
                        Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK capture={captureResult}");
                        return;
                    }

                    query = await PXR_MixedReality.QuerySceneAnchorAsync(
                        new[] { PxrSemanticLabel.Ceiling }, token);
                }

                if (query.result != PxrResult.SUCCESS || query.anchorHandleList == null || query.anchorHandleList.Count == 0 || token.IsCancellationRequested)
                {
                    Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK query={query.result}");
                    return;
                }

                var game = GetComponent<CosmicSliceGame>();
                var head = Camera.main != null ? Camera.main.transform.localPosition : Vector3.zero;
                var found = false;
                var bestPosition = Vector3.zero;
                var bestDistance = float.PositiveInfinity;
                foreach (var handle in query.anchorHandleList)
                {
                    if (PXR_MixedReality.LocateAnchor(handle, out var position, out _) != PxrResult.SUCCESS)
                    {
                        continue;
                    }

                    var horizontalDistance = Vector2.Distance(
                        new Vector2(position.x, position.z),
                        new Vector2(head.x, head.z));
                    var notAbovePlayerPenalty = position.y < head.y + 0.4f ? 100f : 0f;
                    var score = horizontalDistance + notAbovePlayerPenalty;
                    if (score < bestDistance)
                    {
                        bestDistance = score;
                        bestPosition = position;
                        found = true;
                    }
                }

                if (found && game != null && !token.IsCancellationRequested)
                {
                    game.ApplyDetectedCeiling(bestPosition);
                    Debug.Log($"COSMIC_CEILING_LOCKED trackingPosition={bestPosition}");
                }
                else
                {
                    Debug.LogWarning("COSMIC_CEILING_SCAN_FALLBACK no-valid-anchor");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("COSMIC_CEILING_SCAN_CANCELLED");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK exception={exception.Message}");
            }
        }
#endif

        private void OnDestroy()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }
    }
}
