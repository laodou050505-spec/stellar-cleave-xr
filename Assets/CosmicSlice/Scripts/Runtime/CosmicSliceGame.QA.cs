using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        [Serializable]
        private sealed class QaReport
        {
            public string status;
            public string unityVersion;
            public string platform;
            public int width;
            public int height;
            public int finalScore;
            public int finalLives;
            public int bestCombo;
            public List<string> screenshots = new();
            public List<string> checks = new();
            public List<string> failures = new();
        }

        private readonly List<string> _qaScreenshots = new();
        private readonly List<string> _qaChecks = new();
        private readonly List<string> _qaFailures = new();
        private string _qaOutputDirectory;

        private const int QaCaptureWidth = 1600;
        private const int QaCaptureHeight = 900;

        private void Start()
        {
            if (_qaMode)
            {
                StartCoroutine(HasCommandLineFlag("-cosmic-stage-exit-qa") ? RunStageExitQa() :
                    HasCommandLineFlag("-cosmic-menu-exit-qa") ? RunCoverExitQa() : RunAutomatedQa());
            }
        }

        private Ray MenuQaRay(int option)
        {
            var destination = _menuControls[option].Rect.position;
            return new Ray(_camera.transform.position, (destination - _camera.transform.position).normalized);
        }

        private IEnumerator MenuQaFeedbackFrames(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                UpdateCoverVisuals(Time.unscaledDeltaTime);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator RunCoverInteractionQa()
        {
            // Exercise the same world-ray dispatch used by both controllers and desktop input.
            foreach (var option in new[] { 0, 2, 1 })
            {
                Physics.SyncTransforms();
                UpdateCoverOptionFromRay(MenuQaRay(option), false);
                yield return MenuQaFeedbackFrames(0.14f);
                QaCheck(_coverSelection == option && _menuControls[option].Rect.localScale.x > 1.02f &&
                    _menuControls[option].Label.color == MenuInk,
                    $"home option {option} world ray produces visible focus fill and scale feedback");
            }
            var savedCooldown = _xrStartCooldown;
            _xrStartCooldown = 0f;
            for (var hand = 0; hand < 2; hand++)
            {
                _coverPointerTriggerArmed[hand] = false;
                _coverPointerTriggerHeld[hand] = false;
                var premature = ConsumeCoverPointerPress(hand, true);
                ConsumeCoverPointerPress(hand, false);
                var freshPress = ConsumeCoverPointerPress(hand, true);
                var heldRepeat = ConsumeCoverPointerPress(hand, true);
                QaCheck(!premature && freshPress && !heldRepeat,
                    $"controller {hand} requires trigger release and rejects held-button repeats");
                HideCoverControllerPointer(hand);
            }
            _xrStartCooldown = savedCooldown;
            _coverInputHand = -1;
            UpdateCoverOptionFromRay(MenuQaRay(2), true);
            QaCheck(_coverPendingAction == 2 && !_coverGuideVisible, "How to Play click visibly presses before navigation");
            ActivateCoverSelection();
            QaCheck(_coverPendingAction == 2, "repeated activation during press animation is ignored");
            UpdateCoverVisuals(0.07f);
            QaCheck(_menuControls[2].Rect.localScale.x < 1f, "How to Play button compresses while pressed");
            yield return CaptureQaFrame("00d-how-to-play-pressed.png");
            AdvanceCoverAction(0.17f);
            UpdateCoverVisuals(0.2f);
            Physics.SyncTransforms();
            QaCheck(_coverGuideVisible && !_coverHomePage.gameObject.activeInHierarchy &&
                _coverGuidePage.gameObject.activeInHierarchy && !_gameplayStageActive,
                "guide opens inside isolated title and disables hidden home hitboxes");
            QaCheck(!Physics.Raycast(MenuQaRay(1), out var hiddenHomeHit, 8f) ||
                hiddenHomeHit.collider.GetComponent<CosmicWorldMenuOption>()?.Selection != 1,
                "hidden Exit cannot intercept rays on the guide page");
            var guideTexts = _coverGuidePage.GetComponentsInChildren<UnityEngine.UI.Text>();
            var quantumRuleFound = false;
            var lifeRuleFound = false;
            var allGuideTextFits = true;
            foreach (var text in guideTexts)
            {
                quantumRuleFound |= text.text.Contains("within 0.9s");
                lifeRuleFound |= text.text.Contains("3 LIVES");
                allGuideTextFits &= text.preferredHeight <= text.rectTransform.rect.height + 1f;
            }
            QaCheck(quantumRuleFound && lifeRuleFound && allGuideTextFits,
                "native guide text matches 0.9-second dual link and three lives with no truncated text");
            yield return CaptureQaFrame("00e-how-to-play.png");
            foreach (var option in new[] { 4, 3 })
            {
                UpdateCoverOptionFromRay(MenuQaRay(option), false);
                yield return MenuQaFeedbackFrames(0.14f);
                QaCheck(_coverSelection == option && _menuControls[option].Rect.localScale.x > 1.02f,
                    $"guide option {option} responds to world-ray hover");
            }
            UpdateCoverOptionFromRay(MenuQaRay(3), true);
            UpdateCoverVisuals(0.07f);
            yield return CaptureQaFrame("00f-back-pressed.png");
            AdvanceCoverAction(0.17f);
            Physics.SyncTransforms();
            QaCheck(!_coverGuideVisible && _coverSelection == 0 && _coverPendingAction < 0,
                "Back returns to usable home without starting gameplay");
            MoveCoverSelection(1);
            QaCheck(_coverSelection == 2, "keyboard order follows Start, How to Play, Exit");
            MoveCoverSelection(1);
            QaCheck(_coverSelection == 1, "keyboard reaches Exit");
            MoveCoverSelection(1);
            QaCheck(_coverSelection == 0, "keyboard wraps to Start");
            SelectCoverOption(2, false);
            ActivateCoverSelection();
            AdvanceCoverAction(0.17f);
            Physics.SyncTransforms();
            UpdateCoverOptionFromRay(MenuQaRay(4), true);
            UpdateCoverVisuals(0.07f);
            QaCheck(_coverPendingAction == 4 && _menuControls[4].Rect.localScale.x < 1f,
                "guide Start presses through the actual ray route");
            QaCheck(_menuControls[3].Fill.color.r > 0.8f && _menuControls[3].Label.color == MenuInk,
                "rapid Back-to-Start focus change keeps dark text on the still-bright pressed Back button");
            yield return CaptureQaFrame("00g-guide-start-pressed.png");
            AdvanceCoverAction(0.17f);
            AdvanceCoverAction(0.73f);
            QaCheck(_state == GameState.Scanning && _titleRoot == null,
                "guide Start completes transition into scanning and disposes title");
        }

        private IEnumerator RunCoverExitQa()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            Physics.SyncTransforms();
            UpdateCoverOptionFromRay(MenuQaRay(1), true);
            UpdateCoverVisuals(0.07f);
            Debug.Log($"COSMIC_EXIT_QA pressed={_coverPendingAction == 1} feedback={_menuControls[1].Rect.localScale.x < 1f}");
            AdvanceCoverAction(0.17f);
            Debug.Log($"COSMIC_EXIT_QA exiting={_coverExitRequested} gameplayActive={_gameplayStageActive}");
            AdvanceCoverAction(0.36f);
        }

        private IEnumerator RunStageExitQa()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            EnterScanning();
            UpdateScanning(6.5f);
            ReportCeilingScanFinished();
            SelectCeilingPreview();
            BeginRound();
            EndRound();
            UpdateStageControlVisuals(0.2f);
            Physics.SyncTransforms();
            var destination = _stageButtons[3].Control.Rect.position;
            var hit = StageButtonFromRay(new Ray(_camera.transform.position, (destination - _camera.transform.position).normalized), true);
            UpdateStageControlVisuals(0.07f);
            Debug.Log($"COSMIC_STAGE_EXIT_QA rayHit={hit} pressed={_stagePending == 3} feedback={_stageButtons[3].Control.Rect.localScale.x < 1f}");
            AdvanceStageAction(0.17f);
        }

        private IEnumerator RunAutomatedQa()
        {
            _qaOutputDirectory = GetCommandLineValue("-cosmic-qa-output");
            if (string.IsNullOrWhiteSpace(_qaOutputDirectory))
            {
                _qaOutputDirectory = Path.Combine(Application.persistentDataPath, "CosmicSlice-QA");
            }
            Directory.CreateDirectory(_qaOutputDirectory);
            Cursor.visible = false;
            Debug.Log($"COSMIC_QA_BEGIN output={_qaOutputDirectory}");

            yield return new WaitForSecondsRealtime(0.5f);
            QaCheck(_state == GameState.Cover && _titleRoot != null && _coverRoot != null && _coverGroup != null && _coverGroup.alpha > 0.99f, "isolated title root opens with a visible Start / Exit interaction layer");
            QaCheck(
                _titlePlayerStart != null && _gameplayPlayerStart != null &&
                Vector3.Distance(_rigTransform.position, CosmicSliceDesign.TitlePlayerRootPosition) < 0.001f &&
                Mathf.Abs(Mathf.DeltaAngle(_rigTransform.eulerAngles.y, CosmicSliceDesign.TitlePlayerYaw)) < 0.01f &&
                Mathf.Abs(_camera.transform.localPosition.y - CosmicSliceDesign.TitleEyeHeight) < 0.001f,
                "desktop Game View and XR Origin derive from the current title design player start");
            QaCheck(
                _hudRoot != null && _hudRoot.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace &&
                _hudRoot.parent == _titleConsoleRoot &&
                Vector3.Distance(_titleConsoleRoot.position, CosmicSliceDesign.TitleMissionConsolePosition) < 0.01f,
                "title choices live on their own fixed world-space title console");
            Debug.Log($"COSMIC_QA_TITLE_CONSOLE root={_hudRoot} parent={_hudRoot?.parent?.name} expectedParent={_titleConsoleRoot?.name} mode={_hudRoot?.GetComponent<Canvas>()?.renderMode} console={_titleConsoleRoot?.position} expected={CosmicSliceDesign.TitleMissionConsolePosition}");
            QaCheck(
                !_gameplayStageActive && _gameplayRoot != null && !_gameplayRoot.gameObject.activeInHierarchy &&
                _portalRoot != null && !_portalRoot.gameObject.activeInHierarchy &&
                _leftBlade != null && !_leftBlade.gameObject.activeInHierarchy && !_gameplayHudBuilt &&
                _musicSource != null && !_musicSource.isPlaying && _portalSource != null && !_portalSource.isPlaying,
                "title phase disables gameplay renderers, colliders, blades, HUD and audio rather than overlaying the game");
            QaCheck(
                _coverArtwork != null && _coverArtwork.texture != null &&
                _guideArtwork != null && _guideArtwork.texture != null &&
                _coverHomePage.Find("Cover Option START GAME") != null &&
                _coverHomePage.Find("Cover Option HOW TO PLAY") != null &&
                _coverHomePage.Find("Cover Option EXIT") != null &&
                _coverHomePage.Find("Cover Opaque Launch Surface") != null,
                "Nano Banana cover and field guide textures load with three real home controls on an opaque title");
            QaCheck(
                _coverControllerRays[0] != null && _coverControllerRays[1] != null &&
                _coverControllerReticles[0] != null && _coverControllerReticles[1] != null &&
                _coverControllerReticles[0].Find("Aim Point") != null &&
                _coverControllerReticles[1].Find("Aim Ring") != null,
                "title Start / Exit supports visible world-space pointers for both controllers");
            yield return CaptureQaFrame("00-cover.png");
            yield return new WaitForSecondsRealtime(2f);
            QaCheck(
                _state == GameState.Cover && _titleRoot != null && !_gameplayStageActive &&
                _gameplayRoot != null && !_gameplayRoot.gameObject.activeInHierarchy &&
                _coverRoot != null && _coverRoot.gameObject.activeInHierarchy,
                "title remains visually isolated and static through the required two-second launch hold");
            yield return CaptureQaFrame("00z-cover-static-2s.png");

            var initialRigPosition = _rigTransform.position;
            _rigTransform.position += new Vector3(1.1f, 0f, 0.9f);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return CaptureQaFrame("00a-cover-worldspace-side-step.png");
            QaCheck(Vector3.Distance(_hudRoot.position, _titleConsoleRoot.position) < 0.001f && Vector3.Distance(_rigTransform.position, initialRigPosition) > 1f, "walking around the player start does not pull the world title console with the camera");
            ResetToDesignPlayerStart(true);
            yield return new WaitForSecondsRealtime(0.2f);
            QaCheck(Vector3.Distance(_rigTransform.position, CosmicSliceDesign.TitlePlayerRootPosition) < 0.001f && Mathf.Abs(_camera.transform.localPosition.y - CosmicSliceDesign.TitleEyeHeight) < 0.001f, "title reset returns desktop and XR-origin preview to the authored title design player start");
            yield return CaptureQaFrame("00aa-cover-reset-design-start.png");
            SelectCoverOption(1, false);
            UpdateCoverVisuals(0.2f);
            yield return null;
            QaCheck(_coverSelection == 1 && _coverStatusText.text == "EXIT TO DESKTOP", "cover Exit selection changes the active control feedback");
            yield return CaptureQaFrame("00b-cover-exit-focus.png");
            SelectCoverOption(0, false);
            UpdateCoverVisuals(0.2f);
            yield return null;
            QaCheck(_coverSelection == 0 && _coverStatusText.text == "READY", "cover Start Game selection restores the active cyan feedback");
            yield return CaptureQaFrame("00c-cover-start-focus.png");
            yield return RunCoverInteractionQa();
            yield return new WaitForSecondsRealtime(0.12f);
            QaCheck(
                _gameplayStageActive && _gameplayRoot != null && _gameplayRoot.gameObject.activeInHierarchy &&
                _titleRoot == null && _titleConsoleRoot == null && _coverRoot == null && _coverGroup == null &&
                _hudRoot != null && _hudRoot.parent == _gameplayConsoleRoot && _gameplayHudBuilt &&
                Vector3.Distance(_rigTransform.position, CosmicSliceDesign.GameplayPlayerRootPosition) < 0.001f &&
                Mathf.Abs(Mathf.DeltaAngle(_rigTransform.eulerAngles.y, CosmicSliceDesign.GameplayPlayerYaw)) < 0.01f,
                "Start destroys TitleRoot before activating the separate GameplayRoot, HUD and gameplay design start");
            QaCheck(
                _musicSource != null && _musicSource.enabled && _musicSource.clip == music &&
                _portalSource != null && _portalSource.enabled && _portalSource.clip == portalLoop,
                "gameplay audio sources are enabled only after the title stage has been removed");
            Debug.Log($"COSMIC_QA_GAMEPLAY_CONSOLE root={_hudRoot} parent={_hudRoot?.parent?.name} expectedParent={_gameplayConsoleRoot?.name} console={_gameplayConsoleRoot?.position} expected={CosmicSliceDesign.GameplayMissionConsolePosition}");
            yield return CaptureQaFrame("00d-gameplay-root-after-start.png");
            var isAndroidPlayer = Application.platform == RuntimePlatform.Android;
            var expectedBackgroundAlpha = isAndroidPlayer ? 0f : 1f;
            var background = _camera.backgroundColor;
            var desktopPreviewIsNeutral = isAndroidPlayer ||
                (Mathf.Abs(background.r - background.g) < 0.04f &&
                 Mathf.Abs(background.g - background.b) < 0.04f);
            Debug.Log($"COSMIC_QA_MR_CAMERA platform={Application.platform} clearFlags={_camera.clearFlags} background={background} expectedAlpha={expectedBackgroundAlpha} hdr={_camera.allowHDR} skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "null")}");
            QaCheck(
                _camera.clearFlags == CameraClearFlags.SolidColor &&
                Mathf.Abs(background.a - expectedBackgroundAlpha) < 0.005f &&
                desktopPreviewIsNeutral && !_camera.allowHDR && RenderSettings.skybox == null,
                "gameplay camera uses transparent Android MR composition and an opaque neutral desktop preview without HDR or a skybox");
            QaCheck(
                _desktopSpaceBackdrop == null && _spaceBackdropMaterial == null &&
                _spaceVistaRoot == null && _userSpaceModelRoot == null && _cosmicDustRoot == null,
                "MR gameplay creates no virtual sky sphere, starfield, planets, spacecraft or room-filling cosmic dust");
            var leftMeteorRenderer = _leftMeteorShower != null
                ? _leftMeteorShower.GetComponent<ParticleSystemRenderer>()
                : null;
            var rightMeteorRenderer = _rightMeteorShower != null
                ? _rightMeteorShower.GetComponent<ParticleSystemRenderer>()
                : null;
            var bodyRight = Vector3.Cross(Vector3.up, _meteorBodyForward);
            var leftBodyOffset = _leftMeteorShowerRoot.position - _camera.transform.position;
            var rightBodyOffset = _rightMeteorShowerRoot.position - _camera.transform.position;
            QaCheck(Vector3.Dot(leftBodyOffset, bodyRight) < -1.5f && Vector3.Dot(rightBodyOffset, bodyRight) > 1.5f &&
                Mathf.Abs(Vector3.Dot(leftBodyOffset, _meteorBodyForward)) < 0.2f &&
                Mathf.Abs(Vector3.Dot(rightBodyOffset, _meteorBodyForward)) < 0.2f,
                "meteor emission volumes occupy the body's actual left and right, not the forward arena or screen edges");
            var meteorLeftBeforeLook = _leftMeteorShowerRoot.position;
            var meteorRightBeforeLook = _rightMeteorShowerRoot.position;
            var headRotationBeforeLook = _camera.transform.localRotation;
            _camera.transform.localRotation *= Quaternion.Euler(0f, 90f, 0f);
            UpdateMeteorShowerAnchors();
            QaCheck(Vector3.Distance(meteorLeftBeforeLook, _leftMeteorShowerRoot.position) < 0.001f &&
                Vector3.Distance(meteorRightBeforeLook, _rightMeteorShowerRoot.position) < 0.001f,
                "turning the head ninety degrees does not drag meteor showers around the screen");
            _camera.transform.localRotation = headRotationBeforeLook;
            QaCheck(_leftMeteorShower.main.simulationSpace == ParticleSystemSimulationSpace.World &&
                _leftMeteorShower.trails.enabled && _leftMeteorShower.trails.worldSpace &&
                leftMeteorRenderer.renderMode == ParticleSystemRenderMode.Billboard && leftMeteorRenderer.trailMaterial != null,
                "meteor cores have separate world-space tapered trails instead of stretched particle bars");
            QaCheck(
                _leftMeteorShower != null && _rightMeteorShower != null &&
                leftMeteorRenderer != null && rightMeteorRenderer != null &&
                leftMeteorRenderer.enabled && rightMeteorRenderer.enabled &&
                _leftMeteorShower.gameObject.activeInHierarchy && _rightMeteorShower.gameObject.activeInHierarchy,
                "both lateral meteor showers own active particle systems with enabled renderers");
            QaCheck(_ceilingGridRoot != null && _floorGridRoot != null, "floor and ceiling use transparent spatial grids");
            QaCheck(_ceilingGridRoot != null && _ceilingGridRoot.GetComponentsInChildren<LineRenderer>(true).Length >= 80, "ceiling uses a denser double-line sensor grid with fading edge rays");
            QaCheck(_containmentGridRoot != null && _containmentGridRoot.Find("Left Phase Wall") != null && _containmentGridRoot.Find("Rear Phase Wall") != null, "extended transparent grid forms a room-scale containment cube");
            QaCheck(_containmentGridRoot != null && _containmentGridRoot.GetComponentsInChildren<LineRenderer>(true).Length >= 240, "side grids provide fine room-scale coverage with natural fading edges");
            QaCheck(GameObject.Find("Desktop Room Wall") == null, "legacy opaque room wall is absent");
            QaCheck(
                GameObject.Find("Mission Console Physical Backing") == null &&
                GameObject.Find("Mission Console Cyan Halo") == null,
                "gameplay HUD uses only independent instrument panels without a giant console backing");
            QaCheck(
                GameObject.Find("Left First Person Flight Glove") == null &&
                GameObject.Find("Right First Person Flight Glove") == null &&
                GameObject.Find("Left Peripheral Blade Visual") != null &&
                GameObject.Find("Right Peripheral Blade Visual") != null,
                "tracked blades use compact peripheral visuals instead of view-blocking glove models");
            QaCheck(
                _leftBlade.HasWorldPointerFeedback() && _rightBlade.HasWorldPointerFeedback(),
                "left and right controllers each own a world-space ray and hit reticle");
            QaCheck(_leftBlade.IsDesktopSlicePlaneAligned(SlicePlaneCenter, _camera.transform.forward), "desktop blade interaction is projected onto the falling-target plane");
            var interior = _portalRoot.Find("Upward Spiral Wormhole Interior");
            QaCheck(interior != null && _wormholeInteriorMesh != null &&
                _wormholeInteriorMesh.bounds.min.y >= -0.02f &&
                _wormholeInteriorMesh.bounds.max.y >= 1.8f,
                "wormhole surface starts at the ceiling mouth and recedes upward at least 1.8m, with no hanging body");
            var mouthVertices = 0;
            var farVertices = 0;
            var mouthOpen = true;
            var farEndNarrow = true;
            foreach (var vertex in _wormholeInteriorMesh.vertices)
            {
                var radius = new Vector2(vertex.x, vertex.z).magnitude;
                if (vertex.y < 0.001f) { mouthVertices++; mouthOpen &= Mathf.Abs(radius - WormholeMouthRadius) < 0.001f; }
                if (vertex.y > WormholeDepth - 0.001f) { farVertices++; farEndNarrow &= radius < 0.19f; }
            }
            QaCheck(mouthVertices >= 160 && mouthOpen && farVertices >= 160 && farEndNarrow,
                "open mouth is 1.48m radius and the far end narrows to 0.18m, rather than a flat opaque entrance disc");
            QaCheck(_portalRoot.Find("Event Horizon Black Core") == null &&
                interior.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported &&
                _portalRoot.GetComponentsInChildren<MeshRenderer>(true).Length == 4 &&
                _nebulaRoot.GetComponentsInChildren<MeshRenderer>(true).Length == 2 &&
                _portalRoot.Find("Feathered Nebula Mouth") != null &&
                _wormholeRimMaterial.GetFloat("_RimOnly") == 1f,
                "continuous vortex plus a separate feathered rim replace the opaque entrance disc and stacked rings");
            var rigPositionBeforeCeilingAlignment = _rigTransform.position;
            var rigRotationBeforeCeilingAlignment = _rigTransform.rotation;
            var consolePositionBeforeCeilingAlignment = _gameplayConsoleRoot.position;
            var consoleRotationBeforeCeilingAlignment = _gameplayConsoleRoot.rotation;
            var sensedCeilingWorld = new Vector3(0f, 3.65f, 0.55f);
            var ceilingRotation = Quaternion.Euler(90f, 0f, 0f);
            var ceilingBoundary = new[] { new Vector2(-4f, -4f), new Vector2(4f, -4f), new Vector2(4f, 4f), new Vector2(-4f, 4f) };
            var accepted = ApplyDetectedCeiling(_rigTransform.InverseTransformPoint(sensedCeilingWorld), Quaternion.Inverse(_rigTransform.rotation) * ceilingRotation, ceilingBoundary);
            QaCheck(accepted && _hasDetectedCeiling && Mathf.Abs(_portalRoot.position.y - (3.65f - 0.018f)) < 0.001f &&
                Vector3.Distance(_rigTransform.position, rigPositionBeforeCeilingAlignment) < 0.001f &&
                Quaternion.Angle(_rigTransform.rotation, rigRotationBeforeCeilingAlignment) < 0.01f &&
                Vector3.Distance(_gameplayConsoleRoot.position, consolePositionBeforeCeilingAlignment) < 0.001f &&
                Quaternion.Angle(_gameplayConsoleRoot.rotation, consoleRotationBeforeCeilingAlignment) < 0.01f,
                "valid ceiling polygon places the portal 18mm below its surface without moving player or world UI");
            QaCheck(Vector3.Dot(_portalRoot.up, Vector3.up) > 0.99f &&
                _portalRoot.TransformPoint(Vector3.up * WormholeDepth).y > _portalRoot.position.y + 1.8f,
                "detected ceiling orientation sends the vortex depth upward, outside the room");
            var viewBearing = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            var viewToPortal = _portalRoot.position - _camera.transform.position;
            var horizontalToPortal = Vector3.ProjectOnPlane(viewToPortal, Vector3.up);
            var lateralOffset = horizontalToPortal - viewBearing * Vector3.Dot(horizontalToPortal, viewBearing);
            QaCheck(lateralOffset.magnitude < 0.2f,
                "portal entrance follows the current view-centre bearing while remaining ceiling anchored");
            QaCheck(!TryFitCeilingPolygon(sensedCeilingWorld, Quaternion.identity, ceilingBoundary, _camera.transform.position, CosmicSliceDesign.ArenaOrigin, out _, out _, out _) &&
                !TryFitCeilingPolygon(sensedCeilingWorld, ceilingRotation, null, _camera.transform.position, CosmicSliceDesign.ArenaOrigin, out _, out _, out _),
                "vertical surfaces and missing boundaries cannot be reported as an anchored ceiling");
            var smallCeiling = new[] { new Vector2(-1.1f,-1.1f), new Vector2(1.1f,-1.1f), new Vector2(1.1f,1.1f), new Vector2(-1.1f,1.1f) };
            QaCheck(TryFitCeilingPolygon(sensedCeilingWorld, ceilingRotation, smallCeiling, _camera.transform.position, new Vector3(20f,0f,20f), out var fitted, out _, out var fitScale) &&
                fitScale < 0.6f && Mathf.Abs(fitted.x - sensedCeilingWorld.x) < 1.1f && Mathf.Abs(fitted.z - sensedCeilingWorld.z) < 1.1f,
                "an off-room preferred point is replaced by a fitting ceiling position and scaled portal footprint");
            ResetDetectedCeilingToDesignPose();
            QaCheck(!_hasDetectedCeiling && !CanUsePortal && !_portalRoot.gameObject.activeSelf &&
                Vector3.Distance(_rigTransform.position, rigPositionBeforeCeilingAlignment) < 0.001f &&
                Vector3.Distance(_gameplayConsoleRoot.position, consolePositionBeforeCeilingAlignment) < 0.001f,
                "missing or reset ceiling hides the portal and never silently enables a fabricated real-room position");
            var hiddenNebulaClock = _nebulaTime;
            AnimatePortalNebula(0.05f);
            QaCheck(_nebulaRoot != null && !_nebulaRoot.gameObject.activeInHierarchy &&
                _nebulaTime == hiddenNebulaClock && _nebulaReveal == 0f,
                "unmapped or reset ceiling hides the nebula and stops its animation clock");
            BeginRound();
            QaCheck(_state != GameState.Playing, "a round cannot start without a mapped ceiling or explicit preview choice");
            ApplyDetectedCeiling(_rigTransform.InverseTransformPoint(sensedCeilingWorld), Quaternion.Inverse(_rigTransform.rotation) * ceilingRotation, ceilingBoundary);
            UpdateScanning(1.15f);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return CaptureQaFrame("00-survey-wave-origin.png");
            UpdateScanning(1.45f);
            QaCheck(_state == GameState.Scanning, "ceiling scan enters scanning state");
            QaCheck(_scanWaveMesh != null && _scanWaveParticles != null && _scanWaveParticles.particleCount > 0, "opening scan combines an irregular radial wave, particles and a delayed spatial grid reveal");
            yield return new WaitForSecondsRealtime(0.12f);
            yield return CaptureQaFrame("01-ceiling-scan.png");

            UpdateScanning(OpeningScanDuration);
            QaCheck(_state == GameState.Ready, "ceiling scan reaches ready state");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return CaptureQaFrame("02-portal-ready.png");
            yield return CaptureWormholeViews();
            ResetDetectedCeilingToDesignPose();
            ReportCeilingScanFinished();
            RefreshCeilingReadyStatus();
            UpdateStageControlVisuals(0.2f);
            yield return CaptureQaFrame("02e-ceiling-not-found.png");
            var previewRay = new Ray(_camera.transform.position, (_stageButtons[0].Control.Rect.position - _camera.transform.position).normalized);
            Physics.SyncTransforms();
            StageButtonFromRay(previewRay, true);
            UpdateStageControlVisuals(0.07f);
            QaCheck(_stagePending == 0 && _stageButtons[0].Control.Rect.localScale.x < 1f, "preview button receives world-ray input and visible press feedback");
            AdvanceStageAction(0.17f);
            QaCheck(_ceilingPreviewSelected && !_hasDetectedCeiling && _state == GameState.Ready && _statusText.text.Contains("NOT ATTACHED"),
                "explicit preview exposes a clearly labeled unanchored portal and does not immediately start the round");
            yield return CaptureQaFrame("02f-explicit-preview.png");
            ApplyDetectedCeiling(_rigTransform.InverseTransformPoint(sensedCeilingWorld), Quaternion.Inverse(_rigTransform.rotation) * ceilingRotation, ceilingBoundary);


            EmitMeteorShowerBeat(0L);
            yield return new WaitForSecondsRealtime(0.12f);
            QaCheck(
                _leftMeteorShower != null && _rightMeteorShower != null &&
                _leftMeteorShower.particleCount > 0 && _rightMeteorShower.particleCount > 0,
                "the deterministic QA beat emits visible meteor streaks on both sides of the real room");
            yield return CaptureQaFrame("02b-mr-lateral-meteor-showers.png");
            var meteorParticlesBeforeMove = new ParticleSystem.Particle[32];
            var meteorCountBeforeMove = _leftMeteorShower.GetParticles(meteorParticlesBeforeMove);
            var bodyPositionBeforeMove = _rigTransform.position;
            var emissionPositionBeforeMove = _leftMeteorShowerRoot.position;
            var bodyStep = new Vector3(0.7f, 0f, 0.4f);
            _rigTransform.position += bodyStep;
            UpdateMeteorShowerAnchors();
            var meteorParticlesAfterMove = new ParticleSystem.Particle[32];
            var meteorCountAfterMove = _leftMeteorShower.GetParticles(meteorParticlesAfterMove);
            QaCheck(Vector3.Distance(_leftMeteorShowerRoot.position, emissionPositionBeforeMove + bodyStep) < 0.001f &&
                meteorCountBeforeMove > 0 && meteorCountAfterMove == meteorCountBeforeMove &&
                Vector3.Distance(meteorParticlesBeforeMove[0].position, meteorParticlesAfterMove[0].position) < 0.001f,
                "walking relocates new meteor emission beside the body while existing meteors keep their world positions");
            _rigTransform.position = bodyPositionBeforeMove;
            UpdateMeteorShowerAnchors();
            var forwardView = _camera.transform.localRotation;
            _camera.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            EmitMeteorShowerBeat(4L);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return CaptureQaFrame("02c-body-left-meteors.png");
            _camera.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            EmitMeteorShowerBeat(8L);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return CaptureQaFrame("02d-body-right-meteors.png");
            _camera.transform.localRotation = forwardView;
            Physics.SyncTransforms();
            StageButtonFromRay(new Ray(_camera.transform.position, (_stageButtons[0].Control.Rect.position - _camera.transform.position).normalized), true);
            UpdateStageControlVisuals(0.07f);
            QaCheck(_stagePending == 0 && _stageButtons[0].Control.Rect.localScale.x < 1f, "mapped-ceiling Start provides press feedback through its real world collider");
            AdvanceStageAction(0.17f);
            QaCheck(_state == GameState.Playing && _lives == MaxLives, "round starts with three lives");
            var beatSamples = Mathf.RoundToInt(48000f * 60f / MusicBpm);
            var phaseSamples = Mathf.RoundToInt(48000f * MusicBeatPhaseSeconds);
            QaCheck(
                MusicBeatAtSample(phaseSamples + beatSamples - 1L, 48000) == 0 &&
                MusicBeatAtSample(phaseSamples + beatSamples, 48000) == 1 &&
                MusicBeatAtSample(phaseSamples + beatSamples * 4L, 48000) == 4 &&
                GetSpawnBeatStep() == 4,
                "music sample clock quantizes gameplay events to the measured 127.12 BPM kick grid");
            QaCheck(NextAlignedSpawnBeat(0, 4) == 4 && NextAlignedSpawnBeat(5, 4) == 8 &&
                NextAlignedSpawnBeat(9, 2) == 10 && NextAlignedSpawnBeat(9, 1) == 10,
                "target scheduling stays on downbeats and half-bars instead of drifting through three-beat offsets");
            ClearTargets();
            var fourthBeatSample = (long)System.Math.Ceiling(MusicBeatPhaseSeconds * 48000d + 4d * 60d * 48000d / MusicBpm);
            var entranceLead = (long)System.Math.Round(FallingTarget.EntranceDuration * 48000d);
            _nextSpawnBeat = 4;
            AdvanceMusicSpawns(fourthBeatSample - entranceLead - 2, 48000);
            QaCheck(_targets.Count == 0, "target entrance does not start before the music preparation window");
            AdvanceMusicSpawns(fourthBeatSample - entranceLead + 2, 48000);
            QaCheck(_targets.Count == 1 && _nextSpawnBeat == 8, "one 180ms entrance anticipates the kick so full size lands on its beat");
            AdvanceMusicSpawns(fourthBeatSample - entranceLead + 200, 48000);
            QaCheck(_targets.Count == 1, "repeated frames within one musical node never duplicate a target");
            ClearTargets();
            _nextSpawnBeat = 4;
            AdvanceMusicSpawns(fourthBeatSample + 48000, 48000);
            QaCheck(_targets.Count == 0 && _nextSpawnBeat == 8, "a frame hitch drops old notes instead of releasing a late burst");
            for (var n = 0; n < 3; n++) CreateTarget(TargetKind.Plasma, Vector3.up * 3, Vector3.zero, false);
            _nextSpawnBeat = 4;
            AdvanceMusicSpawns(fourthBeatSample - entranceLead + 2, 48000);
            QaCheck(_targets.Count == 3 && _nextSpawnBeat == 8, "the three-target safety cap consumes a beat without backlogging spawns");
            ClearTargets();
            ResetMusicSpawnGrid(0, 48000);
            QaCheck(_nextSpawnBeat == 4, "a clip loop reanchors to its own downbeat rather than carrying a nonintegral loop offset");
            var qaRoundTime = _timeRemaining;
            _timeRemaining = RoundDuration * 0.5f;
            QaCheck(GetSpawnBeatStep() == 2, "later gameplay uses every second beat");
            _overdriveTimer = 1f;
            QaCheck(GetSpawnBeatStep() == 1, "overdrive keeps one target opportunity per beat");
            _overdriveTimer = 0f;
            _timeRemaining = qaRoundTime;
            ArmMusicBeatClock();
            yield return RunMusicSyncQa();

            var collisionProbe = CreateTarget(TargetKind.Plasma, new Vector3(0f, 2.18f, 0.55f), Vector3.zero, false);
            collisionProbe.transform.localScale = Vector3.one;
            _leftBlade.QaSlice(collisionProbe.transform.position - Vector3.right * 0.45f, collisionProbe.transform.position + Vector3.right * 0.45f);
            QaCheck(collisionProbe.Resolved && _score == 100, "laser blade overlap path resolves a target through the real slice detector");
            ClearTargets();
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _lives = MaxLives;
            UpdateHud();

            var rayProbe = CreateTarget(TargetKind.Meteor, new Vector3(0f, 2.18f, 0.55f), Vector3.zero, false);
            rayProbe.transform.localScale = Vector3.one;
            _rightBlade.QaRaySlice(new Vector3(0f, 2.18f, -4.7f), Vector3.forward);
            QaCheck(rayProbe.Resolved && _score == 130, "right controller ray plus trigger path resolves a distant meteor through the world target collider");
            ClearTargets();
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _lives = MaxLives;
            UpdateHud();

            var extendedLeftRayProbe = CreateTarget(TargetKind.Plasma, new Vector3(0f, 2.18f, 0.55f), Vector3.zero, false);
            extendedLeftRayProbe.transform.localScale = Vector3.one;
            _leftBlade.QaRaySlice(new Vector3(0f, 2.18f, -6.75f), Vector3.forward);
            QaCheck(
                extendedLeftRayProbe.Resolved && _score == 100,
                "left controller extended world ray reaches a ceiling-aligned target without moving the authored arena");
            ClearTargets();
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _lives = MaxLives;
            UpdateHud();

            CreateTarget(TargetKind.Plasma, new Vector3(-1.2f, 2.62f, 0.55f), Vector3.zero);
            CreateTarget(TargetKind.Quantum, new Vector3(0f, 2.82f, 0.55f), Vector3.zero);
            CreateTarget(TargetKind.Meteor, new Vector3(0.95f, 2.62f, 0.55f), Vector3.zero);
            CreateTarget(TargetKind.Bomb, new Vector3(1.75f, 2.82f, 0.55f), Vector3.zero);
            SetEvent("LIVE SIGNAL CLASSIFICATION", CosmicPalette.Silver, 5f);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return CaptureQaFrame("03-target-types.png");
            ClearTargets();

            var plasma = CreateTarget(TargetKind.Plasma, new Vector3(0f, 2.25f, 0.55f), Vector3.zero, false);
            HandleBladeHit(plasma, HandSide.Right, Vector3.right, plasma.transform.position);
            QaCheck(plasma.Resolved && _lives == 2, "wrong blade removes one life and resolves the target");
            yield return CaptureQaFrame("04-wrong-hand.png");

            plasma = CreateTarget(TargetKind.Plasma, new Vector3(0f, 2.25f, 0.55f), Vector3.zero, false);
            HandleBladeHit(plasma, HandSide.Left, Vector3.right, plasma.transform.position);
            QaCheck(_score == 100 && _combo == 1, "cyan target accepts the left hand");
            var meteor = CreateTarget(TargetKind.Meteor, new Vector3(0.34f, 2.28f, 0.55f), Vector3.zero, false);
            HandleBladeHit(meteor, HandSide.Right, Vector3.left, meteor.transform.position);
            QaCheck(_score == 230 && _combo == 2, "amber target accepts the right hand");

            yield return CaptureQaFrame("05-correct-dual-blades.png");

            var quantum = CreateTarget(TargetKind.Quantum, new Vector3(0f, 2.55f, 0.55f), Vector3.zero, false);
            HandleBladeHit(quantum, HandSide.Left, Vector3.right, quantum.transform.position);
            QaCheck(quantum.QuantumPrimed && !quantum.Resolved, "dual-link target arms after its first blade hit");
            HandleBladeHit(quantum, HandSide.Right, Vector3.left, quantum.transform.position);
            QaCheck(quantum.Resolved && _combo == 3, "dual-link target resolves only after the opposite blade follows");
            yield return CaptureQaFrame("05b-quantum-dual-link.png");

            ClearTargets();
            _score = 250;
            _combo = 0;
            var bomb = CreateTarget(TargetKind.Bomb, new Vector3(0f, 2.2f, 0.55f), Vector3.zero, false);
            HandleBladeHit(bomb, HandSide.Left, Vector3.right, bomb.transform.position);
            QaCheck(_score == 0 && _lives == 1 && _combo == 0, "singularity hit removes one life and score");
            yield return CaptureQaFrame("06-bomb-breach.png");

            Flash(Color.clear, 0f);
            _score = 6725;
            _combo = 12;
            _bestCombo = 12;
            _lives = 1;
            EndRound();
            QaCheck(_state == GameState.Results && _resultRankText.text == "A", "successful results screen assigns the expected rank");
            QaCheck(string.IsNullOrEmpty(_eventText.text), "results screen clears transient gameplay messages");
            yield return new WaitForSecondsRealtime(1.45f);
            yield return CaptureQaFrame("07-results-success.png");

            BeginRound();
            _score = 1840;
            _bestCombo = 5;
            _lives = 0;
            Flash(Color.clear, 0f);
            EndRound();
            QaCheck(_resultText.text.Contains("LIFE LINK LOST") && _resultRankText.text == "F", "failure results screen reports depleted lives");
            yield return new WaitForSecondsRealtime(1.45f);
            yield return CaptureQaFrame("08-results-failure.png");
            UpdateStageControlVisuals(0.2f);
            Physics.SyncTransforms();
            StageButtonFromRay(new Ray(_camera.transform.position, (_stageButtons[3].Control.Rect.position - _camera.transform.position).normalized), false);
            QaCheck(_stageSelected == 3 && !_scorePanelRoot.gameObject.activeSelf && !_timerPanelRoot.gameObject.activeSelf,
                "result Exit can receive focus while duplicated gameplay score and timer panels are hidden");
            var allConsoleTextFits = true;
            foreach (var text in _hudRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                allConsoleTextFits &= text.preferredHeight <= text.rectTransform.rect.height + 1f;
            QaCheck(allConsoleTextFits, "all native console text fits its allocated panel area");
            StageButtonFromRay(new Ray(_camera.transform.position, (_stageButtons[2].Control.Rect.position - _camera.transform.position).normalized), true);
            UpdateStageControlVisuals(0.07f);
            QaCheck(_stagePending == 2, "Play Again responds through the result button collider");
            AdvanceStageAction(0.17f);
            QaCheck(_state == GameState.Scanning && !_resultGroup.gameObject.activeSelf, "Play Again rescans and removes all hidden result button hitboxes");

            yield return RunOpeningPresentationQa();
            WriteQaReport();
            var passed = _qaFailures.Count == 0;
            Debug.Log($"COSMIC_QA_COMPLETE passed={passed} screenshots={_qaScreenshots.Count} checks={_qaChecks.Count} failures={_qaFailures.Count}");
            yield return null;
            Application.Quit(passed ? 0 : 2);
        }

        private IEnumerator RunMusicSyncQa()
        {
            var frequency = 48000;
            _musicBeatClockReady = true;
            _lastMusicTimeSamples = 90 * frequency;
            ObserveMusicSample(0, frequency);
            QaCheck(_nextSpawnBeat == 4 && _lastVisualizedBeat == -1,
                "actual clock observer reanchors both target and portal on a nonintegral clip loop");
            _lastMusicTimeSamples = 12 * frequency;
            ObserveMusicSample(12 * frequency - 1000, frequency);
            QaCheck(_nextSpawnBeat % 4 == 0 && _nextSpawnBeat > MusicBeatAtSample(_lastMusicTimeSamples, frequency),
                "even a short backwards seek reschedules the next aligned musical node");

            foreach (var sampleRate in new[] { 44100, 48000 })
            foreach (var fps in new[] { 30, 72, 90, 120 })
            {
                ClearTargets();
                ResetMusicSpawnGrid(0, sampleRate);
                var count = 0;
                var latestBeat = 0L;
                var aligned = true;
                var maxLateness = 0d;
                for (var frame = 0; frame < fps * 10; frame++)
                {
                    var samples = (long)System.Math.Round((frame + 0.37d) * sampleRate / fps);
                    var scheduled = _nextSpawnBeat;
                    AdvanceMusicSpawns(samples, sampleRate);
                    if (_targets.Count == 0) continue;
                    var due = System.Math.Round(MusicBeatPhaseSeconds * sampleRate) + scheduled * 60d * sampleRate / MusicBpm;
                    var lateness = (samples - due) / sampleRate + FallingTarget.EntranceDuration;
                    maxLateness = System.Math.Max(maxLateness, lateness);
                    aligned &= scheduled % 4 == 0 && scheduled > latestBeat;
                    latestBeat = scheduled;
                    count++;
                    ClearTargets();
                }
                QaCheck(count == 5 && aligned && maxLateness <= 1d / fps + 0.001,
                    $"sample-driven entrances stay aligned at {sampleRate}Hz / {fps}fps, with no missing or duplicate notes");
            }

            if (_musicSource == null || _musicSource.clip == null)
            {
                QaCheck(false, "live music source is present for rhythm validation");
                yield break;
            }
            var savedSample = _musicSource.timeSamples;
            var savedTime = _timeRemaining;
            _musicSource.Pause();
            _spawnTimer = -1f;
            ClearTargets();
            UpdateRound(0.01f);
            QaCheck(_targets.Count == 0 && !_musicBeatClockReady,
                "pausing an existing music clip cannot invoke the silent fallback spawner");
            _musicSource.UnPause();
            _musicSource.timeSamples = 0;
            ArmMusicBeatClock();
            var deadline = Time.realtimeSinceStartup + 7f;
            var liveNodes = 0;
            var liveAligned = true;
            var maxErrorMs = 0f;
            var trace = new System.Text.StringBuilder("beat,sample,frequency,entrance_lateness_ms\n");
            while (Time.realtimeSinceStartup < deadline)
            {
                if (TryGetMusicBeat(out _))
                {
                    var beat = _nextSpawnBeat;
                    AdvanceMusicSpawns(_lastMusicTimeSamples, _musicSource.clip.frequency);
                    if (_targets.Count > 0)
                    {
                        var rate = _musicSource.clip.frequency;
                        var due = System.Math.Round(MusicBeatPhaseSeconds * rate) + beat * 60d * rate / MusicBpm;
                        var lateMs = (float)((_lastMusicTimeSamples - due) / rate + FallingTarget.EntranceDuration) * 1000f;
                        maxErrorMs = Mathf.Max(maxErrorMs, lateMs);
                        liveAligned &= beat % 4 == 0 && lateMs >= -0.1f && lateMs <= 80.1f;
                        trace.AppendLine($"{beat},{_lastMusicTimeSamples},{rate},{lateMs:F3}");
                        liveNodes++;
                        ClearTargets();
                    }
                }
                yield return null;
            }
            File.WriteAllText(Path.Combine(_qaOutputDirectory, "music-sync-live.csv"), trace.ToString());
            QaCheck(liveNodes >= 3 && liveAligned,
                $"live AudioSource produces {liveNodes} aligned downbeats; max reveal scheduling lateness {maxErrorMs:F1}ms is phase-compensated");
            _musicSource.timeSamples = savedSample;
            _timeRemaining = savedTime;
            _spawnTimer = 0.72f;
            ArmMusicBeatClock();
        }

        private IEnumerator RunOpeningPresentationQa()
        {
            var ceiling = _rigTransform.InverseTransformPoint(CosmicSliceDesign.ArenaOrigin + Vector3.up * 3.1f);
            var rotation = Quaternion.Inverse(_rigTransform.rotation) * Quaternion.Euler(-90f, 0f, 0f);
            ApplyDetectedCeiling(ceiling, rotation, new[] { new Vector2(-3, -3), new Vector2(3, -3), new Vector2(3, 3), new Vector2(-3, 3) });
            EnterScanning(roomPrepared: true);
            QaCheck(_hasDetectedCeiling, "cover handoff preserves prepared ceiling data instead of triggering capture again");
            EnterScanning();
            var eyePose = _camera.transform.localPosition;
            _camera.transform.localPosition += new Vector3(0.7f, 0f, 0.5f);
            ResetOpeningPresentation();
            QaCheck(Mathf.Abs(_scanIntroRoot.position.x - _camera.transform.position.x) < 0.001f &&
                Mathf.Abs(_scanIntroRoot.position.z - _camera.transform.position.z) < 0.001f,
                "scan origin is under the tracked player, including physical offset from XR Origin");
            var fixedOrigin = _scanIntroRoot.position;
            _camera.transform.localPosition += Vector3.right * 0.3f;
            UpdateSpatialScanIntro(0.2f);
            QaCheck(Vector3.Distance(fixedOrigin, _scanIntroRoot.position) < 0.001f,
                "once launched the wave remains in world space instead of chasing the player");
            _camera.transform.localPosition = eyePose;
            ResetOpeningPresentation();
            QaCheck(OpeningRadius(0f) <= 0.1f && OpeningRadius(0.25f) > 2f &&
                OpeningRadius(0.5f) < 5.3f && OpeningRadius(0.75f) < 7.9f,
                "radial sweep starts at 8cm and spends the full sequence travelling near to far");
            UpdateSpatialScanIntro(0f);
            QaCheck(!_scanIntroLines[0].enabled && _scanIntroLines[12].enabled &&
                _scanIntroLines[12].GetPosition(1).magnitude < 0.11f && !_scanCurtainRoot.gameObject.activeSelf,
                "initial grid is clipped to the tiny source circle with no pre-expanded curtain");
            UpdateSpatialScanIntro(1f / OpeningScanDuration);
            QaCheck(!_scanFollowRoot.gameObject.activeSelf, "following wave waits before leaving the player's origin");
            UpdateSpatialScanIntro(2.4f / OpeningScanDuration);
            QaCheck(_scanFollowRoot.gameObject.activeSelf && _scanFollowMesh.vertexCount == 256 &&
                Mathf.Abs(OpeningRadius(0.5f) - OpeningFollowRadius(0.5f) - 1.8f) < 0.001f &&
                Mathf.Abs(OpeningRadius(0.8f) - OpeningFollowRadius(0.8f) - 1.8f) < 0.001f,
                "two complete wave bands travel at equal speed and keep a 1.8m radial gap");
            ResetOpeningPresentation();
            QaCheck(!_scanFollowRoot.gameObject.activeSelf, "interruption removes the following wave and resets both origins");
            for (var i = 0; i < 600; i++) UpdatePresentedScanning(1f / 60f, false);
            QaCheck(_state == GameState.Scanning && _scanTime == 0f,
                "ten seconds of hidden room preparation consume none of the opening");
            for (var i = 0; i < 90; i++) UpdatePresentedScanning(1f / 60f, true);
            QaCheck(_scanTime == 0f && !_openingPlaying,
                "room reveal lead-in consumes no scan time even when Unity rendering has resumed");
            for (var i = 0; i < 120; i++) UpdatePresentedScanning(1f / 60f, true);
            QaCheck(_scanTime > 0.8f && _scanTime < 1.3f, "opening starts only after stable presentation and separate room lead-in");
            UpdatePresentedScanning(1f / 60f, false);
            QaCheck(_scanTime == 0f && !_openingPlaying,
                "system overlay interrupts the opening and return will replay it from the beginning");
            for (var i = 0; i < 60; i++) UpdatePresentedScanning(1f / 60f, true);
            UpdatePresentedScanning(9f, true);
            QaCheck(_scanTime == 0f && _state == GameState.Scanning,
                "a nine-second resume hitch cannot skip directly to ready");

            var checkpoints = new[] { 0f, 0.5f, 1.5f, 3f, 5f, 6.2f };
            var capture = 0;
            var frames = 0;
            while (_state == GameState.Scanning && frames++ < 800)
            {
                UpdatePresentedScanning(1f / 60f, true);
                if (_openingPlaying && capture < checkpoints.Length && _scanTime >= checkpoints[capture])
                {
                    yield return CaptureQaFrame($"09-opening-{capture:00}-{checkpoints[capture]:0.00}s.png");
                    var viewRotation = _camera.transform.localRotation;
                    _camera.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
                    yield return CaptureQaFrame($"10-self-origin-{capture:00}-{checkpoints[capture]:0.00}s.png");
                    _camera.transform.localRotation = viewRotation;
                    capture++;
                }
                yield return null;
            }
            QaCheck(_state == GameState.Ready && _scanTime >= OpeningScanDuration &&
                _scanTime < OpeningScanDuration + 0.03f && capture == checkpoints.Length,
                "full dual-wave opening presents every checkpoint and reaches ready after 6.8 scan seconds");
        }

        private IEnumerator CaptureWormholeViews()
        {
            var savedPosition = _camera.transform.position;
            var savedRotation = _camera.transform.rotation;
            var hudVisible = _gameplayConsoleRoot.gameObject.activeSelf;
            var mouth = _portalRoot.position;
            _gameplayConsoleRoot.gameObject.SetActive(false);
            try
            {
                var before = _portalRoot.position;
                var beforeRotation = _portalRoot.rotation;
                var views = new[] { new Vector3(0f,-2.4f,0f), new Vector3(1.5f,-2.2f,-1.4f), new Vector3(0f,-2f,-5.2f) };
                foreach (var view in views)
                {
                    var shear = WormholeViewShear(view);
                    var far = Vector3.up * WormholeDepth + shear * WormholeDepth;
                    var apertureCentre = view + (far-view)*(-view.y/(far.y-view.y));
                    QaCheck(new Vector2(apertureCentre.x,apertureCentre.z).magnitude < 0.001f && far.y > 1.8f,
                        "vortex dark core projects through mouth centre while depth stays upward: " + view);
                }
                QaCheck(_wormholeRimMesh.bounds.max.x <= 1.741f &&
                    _wormholeRimMesh.bounds.max.x * 1.125f < 2f,
                    "feathered rim stays inside the existing ceiling pulse-reserved footprint");
                QaCheck(_nebulaRoot != null && _nebulaRoot.parent == _portalRoot && _nebulaRoot.childCount == 2 &&
                    _nebulaMesh.vertexCount < 1300 && _nebulaRoot.GetComponentsInChildren<Collider>().Length == 0 &&
                    _nebulaMaterials[0].shader.isSupported && _nebulaMaterials[0].GetTexture("_MainTex") == portalNebulaTexture &&
                    portalNebulaTexture.width == 1024,
                    "nebula has two bounded lightweight cloud layers with the generated texture and no gameplay colliders");
                QaCheck(_nebulaOuterRadius >= NebulaInnerRadius && _nebulaOuterRadius <= NebulaMaxRadius &&
                    NebulaRadiusForClearance(1.1f,0.51f) * 0.51f * NebulaPulseReserve <= 1.02f + 0.001f &&
                    NebulaRadiusForClearance(4f,1f) == NebulaMaxRadius,
                    "nebula shrinks inside small ceilings with breathing reserve while large ceilings retain wispy outskirts");
                _camera.transform.position = mouth + new Vector3(0f, -2.4f, -0.25f);
                _camera.transform.rotation = Quaternion.LookRotation(mouth - _camera.transform.position, Vector3.forward);
                yield return CaptureQaFrame("02g-vortex-from-below.png");
                _camera.transform.position = mouth + new Vector3(1.5f, -2.2f, -1.4f);
                _camera.transform.rotation = Quaternion.LookRotation(mouth - _camera.transform.position, Vector3.up);
                yield return CaptureQaFrame("02h-vortex-oblique.png");
                QaCheck(Vector3.Distance(before,_portalRoot.position) < 0.001f && Quaternion.Angle(beforeRotation,_portalRoot.rotation) < 0.01f,
                    "view-centred vortex refraction does not move the ceiling anchor");
                var previousBackground = _camera.backgroundColor;
                _camera.backgroundColor = new Color(0.55f,0.52f,0.48f,1f);
                yield return CaptureQaFrame("02i-vortex-light-background.png");
                _camera.backgroundColor = previousBackground;
                _camera.transform.position = mouth + new Vector3(0f,-3.2f,-2.8f);
                _camera.transform.rotation = Quaternion.LookRotation(mouth-_camera.transform.position,Vector3.up);
                yield return CaptureQaFrame("02j-nebula-context.png");
                var cloudTime = _nebulaTime;
                yield return new WaitForSecondsRealtime(2.0f);
                QaCheck(_nebulaTime > cloudTime + 0.2f && _nebulaReveal > 0.99f &&
                    _nebulaMaterials[0].GetFloat("_FlowTime") == _nebulaMaterials[1].GetFloat("_FlowTime"),
                    "nebula gas and stardust animate on a visible-only shared clock after a soft arrival");
                yield return CaptureQaFrame("02k-nebula-context-moving.png");
                var savedRadius = _nebulaOuterRadius;
                SetNebulaRadius(NebulaRadiusForClearance(1.1f,0.51f));
                yield return CaptureQaFrame("02l-nebula-confined-ceiling.png");
                SetNebulaRadius(savedRadius);
                if (HasCommandLineFlag("-cosmic-nebula-animation"))
                    for (var frame = 0; frame < 20; frame++)
                    {
                        yield return CaptureQaFrame($"nebula-motion-{frame:00}.png");
                        yield return new WaitForSecondsRealtime(0.2f);
                    }
            }
            finally
            {
                _camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
                _gameplayConsoleRoot.gameObject.SetActive(hudVisible);
            }
        }

        private IEnumerator CaptureQaFrame(string fileName)
        {
            yield return new WaitForEndOfFrame();

            var renderTexture = new RenderTexture(QaCaptureWidth, QaCaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2,
                name = "Cosmic Slice QA Capture"
            };
            var texture = new Texture2D(QaCaptureWidth, QaCaptureHeight, TextureFormat.RGB24, false);
            var previousTarget = _camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                _camera.targetTexture = renderTexture;
                _camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, QaCaptureWidth, QaCaptureHeight), 0, 0, false);
                texture.Apply(false, false);
                var path = Path.Combine(_qaOutputDirectory, fileName);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                _qaScreenshots.Add(fileName);
                Debug.Log($"COSMIC_QA_SCREENSHOT file={path}");
            }
            finally
            {
                _camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Destroy(renderTexture);
                Destroy(texture);
            }
        }

        private void QaCheck(bool passed, string description)
        {
            if (passed)
            {
                _qaChecks.Add(description);
                Debug.Log($"COSMIC_QA_CHECK passed=True description={description}");
                return;
            }

            _qaFailures.Add(description);
            Debug.LogError($"COSMIC_QA_CHECK passed=False description={description}");
        }

        private void WriteQaReport()
        {
            var report = new QaReport
            {
                status = _qaFailures.Count == 0 ? "passed" : "failed",
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                width = QaCaptureWidth,
                height = QaCaptureHeight,
                finalScore = _score,
                finalLives = _lives,
                bestCombo = _bestCombo,
                screenshots = new List<string>(_qaScreenshots),
                checks = new List<string>(_qaChecks),
                failures = new List<string>(_qaFailures)
            };
            var path = Path.Combine(_qaOutputDirectory, "qa-report.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log($"COSMIC_QA_REPORT file={path}");
        }

        private static bool HasCommandLineFlag(string flag)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetCommandLineValue(string flag)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }
    }
}
