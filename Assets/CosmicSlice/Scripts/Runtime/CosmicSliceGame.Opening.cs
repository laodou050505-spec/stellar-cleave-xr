using UnityEngine;
using UnityEngine.XR;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private const float OpeningScanDuration = 6.8f;
        private const float OpeningWaveSpeed = 1.5f;
        private const float OpeningFollowDelay = 1.2f;
        private const float OpeningRoomLeadIn = 2f;
        private const float OpeningUnconfirmedLeadIn = 4f;
        private const float OpeningSettleDuration = 0.35f;
        private const float OpeningMaximumFrameGap = 0.15f;
        private const float OpeningMaximumStep = 0.05f;
        private float _openingStableTime;
        private float _openingRoomTime;
        private bool _openingRoomAnnounced;
        private int _openingPresentedFrames;
        private bool _openingPlaying;
        private bool _openingPausedByApplication;
        private int _openingLastRenderedFrame = -1;
        private readonly System.Collections.Generic.List<XRDisplaySubsystem> _openingDisplays = new();

        private void OnEnable() => Camera.onPostRender += OnOpeningCameraRendered;
        private void OnOpeningCameraRendered(Camera renderedCamera)
        {
            if (renderedCamera == _camera) _openingLastRenderedFrame = Time.frameCount;
        }

        private void OnApplicationPause(bool paused)
        {
            _openingPausedByApplication = paused;
            if (paused) ResetOpeningPresentation();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ResetOpeningPresentation();
        }

        private bool CanPresentOpening()
        {
            if (_openingPausedByApplication || !Application.isFocused) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            var locator = GetComponent<PicoCeilingLocator>();
            if (locator != null && (!locator.PreparationComplete || !locator.PresentationFocused ||
                (locator.PassthroughStatusKnown && !locator.PassthroughDisplayReady))) return false;
            SubsystemManager.GetSubsystems(_openingDisplays);
            var displayRunning = false;
            foreach (var display in _openingDisplays) displayRunning |= display.running;
            if (!displayRunning) return false;
#endif
            // A runtime focus event alone does not prove our camera has resumed rendering.
            return _openingLastRenderedFrame >= Time.frameCount - 1;
        }

        private void ResetOpeningPresentation()
        {
            _openingStableTime = 0f;
            _openingRoomTime = 0f;
            _openingRoomAnnounced = false;
            _openingPresentedFrames = 0;
            _openingPlaying = false;
            if (_state != GameState.Scanning) return;
            _scanTime = 0f;
            SetScanEnvironmentVisible(false, false, false, false, false);
            if (_portalRoot != null) _portalRoot.gameObject.SetActive(false);
            SetCoreHudVisible(false);
            CaptureOpeningOrigin();
            UpdateSpatialScanIntro(0f);
        }

        // Only presentation time advances the authored opening. System capture, loss of XR
        // focus, and the large delta on resume must never fast-forward the visible sequence.
        private void UpdatePresentedScanning(float deltaTime, bool presentationReady)
        {
            if (!presentationReady || deltaTime > OpeningMaximumFrameGap)
            {
                if (_openingPlaying) Debug.Log($"COSMIC_OPENING_INTERRUPTED visibleSeconds={_scanTime:F3}");
                ResetOpeningPresentation();
                _statusText.text = "CONNECTING TO YOUR ROOM\nOPENING SURVEY WILL FOLLOW";
                return;
            }

            var step = Mathf.Clamp(deltaTime, 0f, OpeningMaximumStep);
            if (!_openingPlaying)
            {
                _openingStableTime += step;
                _openingPresentedFrames++;
                if (_openingStableTime < OpeningSettleDuration || _openingPresentedFrames < 3) return;
                // Unity camera completion does not mean the compositor's room image is
                // already on screen. Reserve a separate room-visible lead-in, never scan time.
                var leadIn = OpeningRoomLeadIn;
#if UNITY_ANDROID && !UNITY_EDITOR
                var locator = GetComponent<PicoCeilingLocator>();
                if (locator == null || !locator.PassthroughDisplayReady) leadIn = OpeningUnconfirmedLeadIn;
#endif
                if (!_openingRoomAnnounced)
                {
                    _openingRoomAnnounced = true;
                    Debug.Log($"COSMIC_ROOM_REVEAL_HOLD seconds={leadIn:F1} scanTime=0");
                }
                _openingRoomTime += step;
                CaptureOpeningOrigin();
                _statusText.text = $"LOOK DOWN AT YOUR FEET\nSCAN BEGINS IN {Mathf.Max(1, Mathf.CeilToInt(leadIn - _openingRoomTime))}";
                if (_openingRoomTime < leadIn) return;
                _openingPlaying = true;
                _statusText.text = string.Empty;
                PlaySfx(positive, 0.35f, 0.82f);
                Debug.Log($"COSMIC_OPENING_VISIBLE_START visibleSeconds=0 origin={_scanIntroRoot.position} radius={OpeningRadius(0):F2}");
                return;
            }
            UpdateScanning(step);
        }
    }
}
