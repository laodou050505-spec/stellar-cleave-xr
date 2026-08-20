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
                StartCoroutine(RunAutomatedQa());
            }
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
                _coverRoot.Find("Stellar Cleave Title") != null &&
                _coverRoot.Find("Anomaly Protocol Subtitle") != null &&
                _coverRoot.Find("Cover Option START GAME") != null &&
                _coverRoot.Find("Cover Option EXIT") != null &&
                _coverRoot.Find("Cover Opaque Launch Surface") != null,
                "cover reproduces the Stellar Cleave title, subtitle and centered Start Game / Exit choices on an opaque launch surface");
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
            yield return null;
            QaCheck(_coverSelection == 1 && _coverStatusText.text == "EXIT TO DESKTOP", "cover Exit selection changes the active control feedback");
            yield return CaptureQaFrame("00b-cover-exit-focus.png");
            SelectCoverOption(0, false);
            yield return null;
            QaCheck(_coverSelection == 0 && _coverStatusText.text == "ANOMALY PROTOCOL READY", "cover Start Game selection restores the active cyan feedback");
            yield return CaptureQaFrame("00c-cover-start-focus.png");
            EnterScanning();
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
            QaCheck(_spaceVistaRoot != null && _spaceVistaRoot.Find("Parallax Star Sphere") != null, "space vista includes a restrained starfield without procedural planet stand-ins");
            var backdropMaterial = _desktopSpaceBackdrop != null ? _desktopSpaceBackdrop.GetComponent<Renderer>()?.sharedMaterial : null;
            var expectedOpacity = (Application.platform == RuntimePlatform.Android || HasCommandLineFlag("-cosmic-mr-preview"))
                ? MrSpaceVeilOpacity
                : 1f;
            var expectAdaptiveAlpha = Application.platform == RuntimePlatform.Android || HasCommandLineFlag("-cosmic-mr-preview");
            var actualOpacity = backdropMaterial != null && backdropMaterial.HasProperty("_Opacity")
                ? backdropMaterial.GetFloat("_Opacity").ToString()
                : "missing";
            var actualAdaptive = backdropMaterial != null && backdropMaterial.HasProperty("_AdaptiveAlpha")
                ? backdropMaterial.GetFloat("_AdaptiveAlpha").ToString()
                : "missing";
            Debug.Log($"COSMIC_QA_BACKDROP platform={Application.platform} expectedOpacity={expectedOpacity} actualOpacity={actualOpacity} expectedAdaptive={expectAdaptiveAlpha} actualAdaptive={actualAdaptive}");
            QaCheck(
                Application.platform != RuntimePlatform.Android ||
                (backdropMaterial != null &&
                 backdropMaterial.HasProperty("_Opacity") &&
                 Mathf.Abs(backdropMaterial.GetFloat("_Opacity") - expectedOpacity) < 0.005f &&
                 (!expectAdaptiveAlpha ||
                  (backdropMaterial.HasProperty("_AdaptiveAlpha") && backdropMaterial.GetFloat("_AdaptiveAlpha") > 0.99f))),
                "space panorama uses the expected full desktop or low-opacity MR treatment");
            QaCheck(_userSpaceModelRoot != null && _spaceSetDressingInstances.Count >= 18, "user-supplied FBX spacecraft, planets and asteroids populate the 360-degree space scene");
            QaCheck(HasSpaceModelsInEveryDirection(), "space set dressing remains visible to the front, rear, left and right of the player");
            QaCheck(_ceilingGridRoot != null && _floorGridRoot != null, "floor and ceiling use transparent spatial grids");
            QaCheck(_ceilingGridRoot != null && _ceilingGridRoot.GetComponentsInChildren<LineRenderer>(true).Length >= 80, "ceiling uses a denser double-line sensor grid with fading edge rays");
            QaCheck(_containmentGridRoot != null && _containmentGridRoot.Find("Left Phase Wall") != null && _containmentGridRoot.Find("Rear Phase Wall") != null, "extended transparent grid forms a room-scale containment cube");
            QaCheck(_containmentGridRoot != null && _containmentGridRoot.GetComponentsInChildren<LineRenderer>(true).Length >= 240, "side grids provide fine room-scale coverage with natural fading edges");
            QaCheck(GameObject.Find("Desktop Room Wall") == null, "legacy opaque room wall is absent");
            QaCheck(GameObject.Find("Left First Person Flight Glove") != null && GameObject.Find("Right First Person Flight Glove") != null, "both tracked blades include first-person glove visuals");
            QaCheck(_leftBlade.IsDesktopSlicePlaneAligned(SlicePlaneCenter, _camera.transform.forward), "desktop blade interaction is projected onto the falling-target plane");
            QaCheck(
                _portalRoot != null &&
                _portalRoot.Find("Event Horizon Black Core") != null &&
                _portalRoot.Find("Outer Cyan Gravity Lens") != null,
                "portal includes a distinct black event horizon and restrained gravity lens");
            QaCheck(
                _userSpaceModelRoot != null &&
                _userSpaceModelRoot.GetComponentsInChildren<CosmicCruisingProp>(true).Length >= 6 &&
                _userSpaceModelRoot.GetComponentsInChildren<CosmicOrbitalProp>(true).Length >= 13,
                "spacecraft cruise independently while planets and satellites use varied player-centered orbits");
            ApplyDetectedCeiling(_rigTransform.InverseTransformPoint(new Vector3(0f, 3.65f, 0.55f)));
            UpdateScanning(0.55f);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return CaptureQaFrame("00-survey-wave-origin.png");
            UpdateScanning(1f);
            QaCheck(_state == GameState.Scanning, "ceiling scan enters scanning state");
            QaCheck(_scanWaveMesh != null && _scanWaveParticles != null && _scanWaveParticles.particleCount > 0, "opening scan combines an irregular radial wave, particles and a delayed spatial grid reveal");
            yield return new WaitForSecondsRealtime(0.12f);
            yield return CaptureQaFrame("01-ceiling-scan.png");

            UpdateScanning(3.1f);
            QaCheck(_state == GameState.Ready, "ceiling scan reaches ready state");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return CaptureQaFrame("02-portal-ready.png");

            var originalRigRotation = _rigTransform.rotation;
            _rigTransform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return CaptureQaFrame("02b-space-right.png");
            _rigTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return CaptureQaFrame("02c-space-rear.png");
            _rigTransform.rotation = Quaternion.Euler(0f, 270f, 0f);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return CaptureQaFrame("02d-space-left.png");
            _rigTransform.rotation = originalRigRotation;
            yield return new WaitForSecondsRealtime(0.25f);

            BeginRound();
            QaCheck(_state == GameState.Playing && _lives == MaxLives, "round starts with three lives");

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
            QaCheck(_state == GameState.Results && _resultText.text.Contains("RANK  A"), "successful results screen assigns the expected rank");
            QaCheck(string.IsNullOrEmpty(_eventText.text), "results screen clears transient gameplay messages");
            yield return new WaitForSecondsRealtime(1.45f);
            yield return CaptureQaFrame("07-results-success.png");

            BeginRound();
            _score = 1840;
            _bestCombo = 5;
            _lives = 0;
            Flash(Color.clear, 0f);
            EndRound();
            QaCheck(_resultText.text.Contains("LIFE LINK LOST") && _resultText.text.Contains("RANK  F"), "failure results screen reports depleted lives");
            yield return new WaitForSecondsRealtime(1.45f);
            yield return CaptureQaFrame("08-results-failure.png");

            WriteQaReport();
            var passed = _qaFailures.Count == 0;
            Debug.Log($"COSMIC_QA_COMPLETE passed={passed} screenshots={_qaScreenshots.Count} checks={_qaChecks.Count} failures={_qaFailures.Count}");
            yield return null;
            Application.Quit(passed ? 0 : 2);
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

        private bool HasSpaceModelsInEveryDirection()
        {
            if (_rigTransform == null)
            {
                return false;
            }

            var front = false;
            var rear = false;
            var left = false;
            var right = false;
            foreach (var item in _spaceSetDressingInstances)
            {
                if (item == null) continue;
                var local = _rigTransform.InverseTransformPoint(item.position);
                front |= local.z > 5f;
                rear |= local.z < -5f;
                left |= local.x < -5f;
                right |= local.x > 5f;
            }
            return front && rear && left && right;
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
