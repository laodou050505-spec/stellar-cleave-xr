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
        private bool _scanRunning;
        private bool _rescanRequested;
        private bool _providerStarted;
        private bool _shuttingDown;
        private int _scanGeneration;
        private bool _forceRoomCapture;
        private bool _transitionPreparationRequested;
        private bool _sessionStateKnown;
        private volatile bool _sessionFocused;
        private bool _passthroughStatusKnown;
        private bool _passthroughDisplayed;
        private float _passthroughRequestedAt;

        // This is a runtime blend-mode acknowledgement, not a camera pixel/luminance test.
        public bool PassthroughDisplayReady => _passthroughStatusKnown && _passthroughDisplayed;
        public bool PassthroughStatusKnown => _passthroughStatusKnown;

        public bool PreparationComplete
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return _scanStarted && !_scanRunning && !_rescanRequested;
#else
                return true;
#endif
            }
        }

        public bool PresentationFocused => Application.isFocused && (!_sessionStateKnown || _sessionFocused);

        private void OnEnable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PXR_Plugin.System.SessionStateChanged += OnSessionStateChanged;
            PXR_Manager.VstDisplayStatusChanged += OnPassthroughDisplayChanged;
#endif
        }

        private void OnPassthroughDisplayChanged(PxrVstStatus status)
        {
            _passthroughStatusKnown = true;
            _passthroughDisplayed = status == PxrVstStatus.Enabled;
            Debug.Log($"COSMIC_VST_DISPLAY status={status} sinceRequest={Time.realtimeSinceStartup - _passthroughRequestedAt:F3}");
        }

        private System.Collections.IEnumerator Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Warm the compositor while the camera still clears to opaque black. No room
            // pixels are exposed in the title, but START no longer cold-starts passthrough.
            yield return null;
            _passthroughRequestedAt = Time.realtimeSinceStartup;
            PXR_Manager.EnableVideoSeeThrough = true;
            Debug.Log("COSMIC_VST_WARMUP_BEHIND_OPAQUE_TITLE");
#endif
            yield break;
        }

        private void OnSessionStateChanged(XrSessionState state)
        {
            _sessionStateKnown = true;
            _sessionFocused = state == XrSessionState.Focused;
        }

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Keep the initial cover opaque; prepare passthrough after START is accepted.
            PXR_Manager.EnableVideoSeeThrough = false;
            PXR_MixedReality.EnableVideoSeeThroughEffect(false);
#endif
        }

        public void RequestRelocate(bool captureRoom = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _forceRoomCapture |= captureRoom;
            if (_scanRunning)
            {
                _rescanRequested = true;
                _cancellation?.Cancel();
                return;
            }

            _cancellation?.Dispose();
            _cancellation = null;
            _scanStarted = false;
            _rescanRequested = false;
            _shuttingDown = false;
            _scanGeneration++;
#endif
        }

        // Room capture may take focus away. Finish it behind the cover before the visible
        // opening starts. Enabling passthrough alone is NOT a first-image-ready signal.
        public void PrepareGameplayTransition()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_shuttingDown) return;
            RequestRelocate();
            _transitionPreparationRequested = true;
            PXR_Manager.EnableVideoSeeThrough = true;
            PXR_MixedReality.EnableVideoSeeThroughEffect(false);
            Debug.Log("COSMIC_MR_VST_PREWARMED launch=true");
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_scanStarted)
            {
                return;
            }
            var game = GetComponent<CosmicSliceGame>();
            if (game == null || (!game.IsGameplayStageActive && !_transitionPreparationRequested))
            {
                return;
            }
            _scanStarted = true;
            _scanRunning = true;
            _cancellation = new CancellationTokenSource();
            _cancellation.CancelAfter(TimeSpan.FromSeconds(25));
            // The title stage remains opaque because this component waits for gameplay above.
            // Once gameplay begins, keep the raw passthrough feed visible while Scene Capture
            // resolves the real room's ceiling.
            PXR_Manager.EnableVideoSeeThrough = true;
            PXR_MixedReality.EnableVideoSeeThroughEffect(false);
            Debug.Log("COSMIC_MR_VST_ENABLED gameplay=true");
            LocateCeiling(_scanGeneration, _cancellation.Token);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private async void LocateCeiling(int generation, CancellationToken token)
        {
            try
            {
                Debug.Log("COSMIC_CEILING_SCAN_STARTED");
                var forceCapture = _forceRoomCapture;
                _forceRoomCapture = false;
                var providerResult = await PXR_MixedReality.StartSenseDataProvider(
                    PxrSenseDataProviderType.SceneCapture,
                    token);
                _providerStarted = providerResult == PxrResult.SUCCESS;
                if (_shuttingDown && _providerStarted)
                {
                    StopProviderIfStarted();
                }
                if (providerResult != PxrResult.SUCCESS || token.IsCancellationRequested || generation != _scanGeneration)
                {
                    Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK provider={providerResult}");
                    return;
                }

                var query = await PXR_MixedReality.QuerySceneAnchorAsync(
                    new[] { PxrSemanticLabel.Ceiling }, token);
                if (forceCapture || query.result != PxrResult.SUCCESS || query.anchorHandleList == null || query.anchorHandleList.Count == 0)
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
                var found = false;
                foreach (var handle in query.anchorHandleList)
                {
                    if (token.IsCancellationRequested || generation != _scanGeneration) break;
                    if (PXR_MixedReality.LocateAnchor(handle, out var position, out var rotation) != PxrResult.SUCCESS) continue;
                    if (PXR_MixedReality.GetSceneSemanticLabel(handle, out var semantic) != PxrResult.SUCCESS || semantic != PxrSemanticLabel.Ceiling) continue;
                    if (PXR_MixedReality.GetScenePolygonData(handle, out var boundary) != PxrResult.SUCCESS || boundary == null || boundary.Length < 3) continue;
                    if (game != null && game.ApplyDetectedCeiling(position, rotation, boundary)) { found = true; break; }
                }
                if (!found) Debug.LogWarning("COSMIC_CEILING_SCAN_FALLBACK no-valid-ceiling-polygon");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("COSMIC_CEILING_SCAN_CANCELLED");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"COSMIC_CEILING_SCAN_FALLBACK exception={exception.Message}");
            }
            finally
            {
                var stopResult = StopProviderIfStarted();
                // Scene Capture can stop after locating the ceiling, but passthrough remains the
                // gameplay background until this gameplay object is disabled or destroyed.
                PXR_Manager.EnableVideoSeeThrough = !_shuttingDown;
                PXR_MixedReality.EnableVideoSeeThroughEffect(false);
                Debug.Log($"COSMIC_CEILING_SCAN_FINISHED mixedReality={!_shuttingDown} videoSeeThrough={!_shuttingDown} stopProvider={stopResult}");
                _scanRunning = false;
                if (!_shuttingDown && !_rescanRequested && generation == _scanGeneration) GetComponent<CosmicSliceGame>()?.ReportCeilingScanFinished();
                if (!_shuttingDown && _rescanRequested)
                {
                    _rescanRequested = false;
                    _scanStarted = false;
                    _cancellation?.Dispose();
                    _cancellation = null;
                }
            }
        }
#endif

        private void OnDisable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PXR_Plugin.System.SessionStateChanged -= OnSessionStateChanged;
            PXR_Manager.VstDisplayStatusChanged -= OnPassthroughDisplayChanged;
            ShutdownScan();
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ShutdownScan();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private PxrResult StopProviderIfStarted()
        {
            if (!_providerStarted)
            {
                return PxrResult.SUCCESS;
            }

            _providerStarted = false;
            return PXR_MixedReality.StopSenseDataProvider(PxrSenseDataProviderType.SceneCapture);
        }

        private void ShutdownScan()
        {
            _shuttingDown = true;
            _transitionPreparationRequested = false;
            _rescanRequested = false;
            _scanGeneration++;
            _cancellation?.Cancel();
            StopProviderIfStarted();
            PXR_Manager.EnableVideoSeeThrough = false;
            PXR_MixedReality.EnableVideoSeeThroughEffect(false);
        }
#endif
    }
}
