using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame : MonoBehaviour
    {
        private enum GameState
        {
            Cover,
            Scanning,
            Ready,
            Playing,
            Results
        }

        [Header("Generated UI")]
        public Texture2D hudBackdrop;

        [Header("Generated Environment")]
        public Texture2D spacePanorama;
        public GameObject[] spaceSetDressingModels;

        [Header("Runtime Shaders")]
        public Shader additiveShader;
        public Shader particleShader;
        public Shader meteorHeadShader;
        public Shader meteorTrailShader;
        public Shader coverFeatherShader;
        public Shader portalShader;
        public Shader nebulaShader;
        public Texture2D portalNebulaTexture;
        public Shader spaceBackdropShader;
        public Shader standardShader;

        [Header("ANGE Audio")]
        public AudioClip music;
        public AudioClip portalLoop;
        public AudioClip bladeWhoosh;
        public AudioClip positive;
        public AudioClip negative;
        public AudioClip explosion;

        public Transform RigTransform => _rigTransform;
        public bool IsQaMode => _qaMode;
        public bool IsGameplayStageActive => _gameplayStageActive;
        public Vector3 SlicePlaneCenter => _portalRoot != null
            ? _portalRoot.position
            : CosmicSliceDesign.ArenaOrigin + Vector3.up * CosmicSliceDesign.PortalHeightAboveArena;

        private readonly List<FallingTarget> _targets = new();
        private readonly Dictionary<TargetKind, Material> _targetMaterials = new();
        private GameState _state;
        private Transform _rigTransform;
        private Camera _camera;
        private Transform _titlePlayerStart;
        private Transform _gameplayPlayerStart;
        private Transform _titleRoot;
        private Transform _titleConsoleRoot;
        private Transform _gameplayConsoleRoot;
        private Transform _gameplayRoot;
        private bool _gameplayStageActive;
        private Transform _hudRoot;
        private Transform _desktopSpaceBackdrop;
        private Transform _floorGridRoot;
        private Transform _ceilingGridRoot;
        private Transform _containmentGridRoot;
        private Transform _spaceVistaRoot;
        private Transform _userSpaceModelRoot;
        private Transform _cosmicDustRoot;
        private Transform _leftMeteorShowerRoot;
        private Transform _rightMeteorShowerRoot;
        private ParticleSystem _leftMeteorShower;
        private ParticleSystem _rightMeteorShower;
        private Transform _scanIntroRoot;
        private Transform _scanGridRoot;
        private Transform _scanCurtainRoot;
        private Transform _scanPulseRoot;
        private Transform _scanWaveRoot;
        private Mesh _scanWaveMesh;
        private Material _scanWaveMaterial;
        private LineRenderer _scanWaveCrest;
        private LineRenderer _scanWaveEcho;
        private ParticleSystem _scanWaveParticles;
        private Transform _scanFollowRoot;
        private Mesh _scanFollowMesh;
        private Material _scanFollowMaterial;
        private LineRenderer _scanFollowCrest;
        private LineRenderer _scanFollowEcho;
        private readonly List<LineRenderer> _scanIntroLines = new();
        private readonly Dictionary<LineRenderer, Vector2> _scanIntroBaseAlphas = new();
        private Transform _distantPlanet;
        private Transform _distantMoon;
        private readonly List<Transform> _distantBodies = new();
        private readonly List<Transform> _spaceSetDressingInstances = new();
        private CosmicModelLibrary _modelLibrary;
        private Transform _portalRoot;
        private Transform _portalSpinA;
        private Transform _portalSpinB;
        private Light _portalLight;
        private Material _cyanGlow;
        private Material _amberGlow;
        private Material _violetGlow;
        private Material _redGlow;
        private Material _cyanParticle;
        private Material _amberParticle;
        private Material _violetParticle;
        private Material _redParticle;
        private Material _darkMetal;
        private Material _floorMaterial;
        private Material _planetMaterial;
        private Material _moonMaterial;
        private SliceBlade _leftBlade;
        private SliceBlade _rightBlade;
        private AudioSource _musicSource;
        private AudioSource _portalSource;
        private AudioSource _sfxSource;
        private Text _scoreText;
        private Text _comboText;
        private Text _timerText;
        private Text _livesText;
        private Text _statusText;
        private Text _tutorialTitle;
        private Text _tutorialBody;
        private Text _eventText;
        private Text _resultText;
        private readonly Image[] _lifePips = new Image[3];
        private Image _flashImage;
        private CanvasGroup _resultGroup;
        private CanvasGroup _tutorialGroup;
        private bool _gameplayHudBuilt;
        private float _scanTime;
        private float _timeRemaining;
        private float _spawnTimer;
        private long _nextSpawnBeat;
        private long _lastVisualizedBeat = -1;
        private int _lastMusicTimeSamples;
        private bool _musicBeatClockReady;
        private float _eventTimer;
        private float _overdriveTimer;
        private float _portalPulse;
        private float _shootingStarTimer;
        private float _scanParticleAccumulator;
        private float _lastScanVisualProgress;
        private float _xrStartCooldown;
        private bool _xrTriggerWasHeld;
        private bool _xrTriggerArmed;
        private float _musicTargetVolume;
        private float _desktopYaw;
        private float _desktopPitch;
        private int _score;
        private int _combo;
        private int _bestCombo;
        private int _lives;
        private bool _hasDetectedCeiling;
        private bool _qaMode;
        private Vector3 _detectedCeilingPosition;

        private const float RoundDuration = 75f;
        private const int MaxLives = 3;
        // Midnight Pulse's decoded waveform measures at approximately 127.12 BPM. Gameplay reads the live
        // AudioSource sample position, so target entrances remain locked to the audible track
        // after pauses, frame hitches and clip loops instead of drifting on a frame timer.
        private const float MusicBpm = 127.12f;
        // The low-frequency kick downbeat begins about 18 ms after the decoded clip start.
        private const float MusicBeatPhaseSeconds = 0.018f;
        private void Awake()
        {
            _qaMode = HasCommandLineFlag("-cosmic-qa");
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.isEditor)
            {
                Application.runInBackground = true;
            }
            Application.targetFrameRate = 90;
            QualitySettings.vSyncCount = 0;
            Physics.defaultContactOffset = 0.01f;
            _modelLibrary = gameObject.AddComponent<CosmicModelLibrary>();
            BuildMaterials();
            BuildCameraRig();
            BuildTitleStage();
            BuildGameplayStage();
            SetGameplayStageVisible(false);
            EnterCover();
        }

        private void BuildTitleStage()
        {
            _titleRoot = new GameObject("TitleRoot - Isolated Launch Cover").transform;
            _titleConsoleRoot = BuildMissionConsole(
                "Title Mission Console - Start and Exit",
                CosmicSliceDesign.TitleMissionConsolePosition,
                CosmicSliceDesign.TitlePlayerRootPosition,
                _titleRoot);
            BuildHud(_titleConsoleRoot);
        }

        private void BuildGameplayStage()
        {
            _gameplayRoot = new GameObject("GameplayRoot - Spatial Arena").transform;
            BuildEnvironment();
            BuildPortal();
            BuildBlades();
            BuildAudio();
            _gameplayConsoleRoot = BuildMissionConsole(
                "Gameplay Mission Console - Status and Debrief",
                CosmicSliceDesign.GameplayMissionConsolePosition,
                CosmicSliceDesign.GameplayPlayerRootPosition,
                _gameplayRoot);
            AdoptGameplayRoot(_floorGridRoot);
            AdoptGameplayRoot(_ceilingGridRoot);
            AdoptGameplayRoot(_containmentGridRoot);
            AdoptGameplayRoot(_desktopSpaceBackdrop);
            AdoptGameplayRoot(_spaceVistaRoot);
            AdoptGameplayRoot(_userSpaceModelRoot);
            AdoptGameplayRoot(_cosmicDustRoot);
            AdoptGameplayRoot(_leftMeteorShowerRoot);
            AdoptGameplayRoot(_rightMeteorShowerRoot);
            AdoptGameplayRoot(_scanIntroRoot);
            AdoptGameplayRoot(_portalRoot);
            if (_leftBlade != null) AdoptGameplayRoot(_leftBlade.transform);
            if (_rightBlade != null) AdoptGameplayRoot(_rightBlade.transform);
        }

        private void BuildGameplayHudIfNeeded()
        {
            if (_gameplayHudBuilt || _gameplayConsoleRoot == null)
            {
                return;
            }

            BuildHud(_gameplayConsoleRoot, includeCover: false);
            _gameplayHudBuilt = true;
        }

        private void AdoptGameplayRoot(Transform root)
        {
            if (root != null)
            {
                root.SetParent(_gameplayRoot, true);
            }
        }

        private void SetGameplayStageVisible(bool visible)
        {
            _gameplayStageActive = visible;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Switch passthrough in the same transition that swaps TitleRoot for GameplayRoot.
            // PicoCeilingLocator repeats this on scan start and owns provider cleanup, but
            // doing it here avoids a one-frame opaque gap after START.
            ByteDance.PICO.XR.PXR_Manager.EnableVideoSeeThrough = visible;
            ByteDance.PICO.XR.PXR_MixedReality.EnableVideoSeeThroughEffect(false);
#endif
            if (_camera != null)
            {
                var background = visible ? MrPreviewClearColor : Color.black;
                background.a = visible && Application.platform == RuntimePlatform.Android ? 0f : 1f;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = background;
                _camera.allowHDR = false;
            }
            if (_gameplayRoot != null)
            {
                _gameplayRoot.gameObject.SetActive(visible);
            }
            if (_musicSource != null)
            {
                if (visible && music != null)
                {
                    _musicSource.Play();
                }
                else
                {
                    _musicSource.Stop();
                }
            }
            if (_portalSource != null)
            {
                if (visible && portalLoop != null)
                {
                    _portalSource.Play();
                }
                else
                {
                    _portalSource.Stop();
                }
            }
            if (!visible && _sfxSource != null)
            {
                _sfxSource.Stop();
            }
        }

        private void Update()
        {
            var unscaledDelta = Time.unscaledDeltaTime;
            _portalPulse = Mathf.Max(0f, _portalPulse - unscaledDelta * 2.4f);
            AnimatePortal(unscaledDelta);
            AnimatePortalNebula(unscaledDelta);
            UpdateHeadTracking();
            if (_gameplayStageActive) UpdateMeteorShowerAnchors();
            AnimateSpaceVista(unscaledDelta);
            UpdateHudFacing(unscaledDelta);
            RefreshConsolePresentation();
            UpdateStageControlVisuals(unscaledDelta);
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.MoveTowards(_musicSource.volume, _musicTargetVolume, unscaledDelta * 0.12f);
            }

            if (_qaMode)
            {
                return;
            }

            if (_eventTimer > 0f)
            {
                _eventTimer -= unscaledDelta;
                var color = _eventText.color;
                color.a = Mathf.Clamp01(_eventTimer * 2f);
                _eventText.color = color;
            }

            _xrStartCooldown = Mathf.Max(0f, _xrStartCooldown - unscaledDelta);

            switch (_state)
            {
                case GameState.Cover:
                    UpdateCover(unscaledDelta);
                    break;
                case GameState.Scanning:
                    UpdatePresentedScanning(unscaledDelta, CanPresentOpening());
                    break;
                case GameState.Ready:
                    UpdateStageControls(unscaledDelta);
                    break;
                case GameState.Playing:
                    UpdateRound(unscaledDelta);
                    break;
                case GameState.Results:
                    UpdateStageControls(unscaledDelta);
                    break;
            }
        }

        private void OnDisable()
        {
            Camera.onPostRender -= OnOpeningCameraRendered;
            Time.timeScale = 1f;
            _musicTargetVolume = 0.11f;
        }

        private void EnterScanning(bool roomPrepared = false)
        {
            SetGameplayStageVisible(true);
            ResetToCurrentStageStart(resetHeadPose: false, relocateCeiling: !roomPrepared);
            ClearTargets();
            _state = GameState.Scanning;
            _stagePending = -1;
            _stageSelected = -1;
            SetCoverVisible(false);
            DisposeTitleRoot();
            BuildGameplayHudIfNeeded();
            SetBladesVisible(true);
            _scanTime = 0f;
            ResetOpeningPresentation();
            CaptureOpeningOrigin();
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _lives = MaxLives;
            _timeRemaining = RoundDuration;
            _overdriveTimer = 0f;
            Time.timeScale = 1f;
            _resultGroup.alpha = 0f;
            _resultGroup.gameObject.SetActive(false);
            _resultGroup.blocksRaycasts = false;
            SetTutorialVisible(false);
            SetCoreHudVisible(false);
            SetScanEnvironmentVisible(false, false, false, false, false);
            if (_portalRoot != null)
            {
                _portalRoot.gameObject.SetActive(false);
                _portalRoot.localScale = Vector3.one * 0.02f;
            }
            if (_scanIntroRoot != null)
            {
                _scanIntroRoot.gameObject.SetActive(true);
            }
            _statusText.text = "OPENING ROOM LINK\nLOOK UP TOWARD THE CEILING";
            SetEvent(string.Empty, CosmicPalette.Cyan, 0f);
            UpdateSpatialScanIntro(0f);
            UpdateHud();
        }

        private void DisposeTitleRoot()
        {
            if (_titleRoot == null)
            {
                return;
            }
            Destroy(_titleRoot.gameObject);
            _titleRoot = null;
            _titleConsoleRoot = null;
            _coverRoot = null;
            _coverGroup = null;
            _coverStartHitArea = null;
            _coverExitHitArea = null;
            _coverHomePage = null;
            _coverGuidePage = null;
            _coverArtwork = null;
            _guideArtwork = null;
            System.Array.Clear(_menuControls, 0, _menuControls.Length);
        }

        private void UpdateScanning(float deltaTime)
        {
            _scanTime = Mathf.Min(OpeningScanDuration, _scanTime + deltaTime);
            // Give the room survey enough time to read as a deliberate opening beat in MR.
            var progress = Mathf.Clamp01(_scanTime / OpeningScanDuration);
            UpdateSpatialScanIntro(progress);
            SetScanEnvironmentVisible(
                progress >= 0.88f,
                progress >= 0.90f,
                progress >= 0.92f,
                progress >= 0.94f,
                progress >= 0.96f);
            SetCoreHudVisible(progress >= 0.94f);

            _statusText.text = progress < 0.88f ? string.Empty :
                _hasDetectedCeiling ? "CEILING ANCHORED\nPREPARING THE ANOMALY" : "SURVEY COMPLETE\nCHECKING YOUR CEILING";

            if (_portalRoot != null && progress >= 0.88f && CanUsePortal)
            {
                _portalRoot.gameObject.SetActive(true);
                var portalProgress = Mathf.InverseLerp(0.88f, 1f, progress);
                var reveal = Mathf.SmoothStep(0.02f, 1f, portalProgress);
                _portalRoot.localScale = new Vector3(_portalCeilingScale * reveal, reveal, _portalCeilingScale * reveal);
                _portalLight.intensity = Mathf.Lerp(0.18f, 3.2f, portalProgress);
            }
            if (progress >= 1f)
            {
                Debug.Log($"COSMIC_OPENING_COMPLETE visibleSeconds={_scanTime:F3}");
                _state = GameState.Ready;
                if (_scanIntroRoot != null) _scanIntroRoot.gameObject.SetActive(false);
                SetScanEnvironmentVisible(true, true, true, true, true);
                SetCoreHudVisible(true);
                RefreshCeilingReadyStatus();
                SetTutorialVisible(true);
                _stageSelected = CanUsePortal || _ceilingScanComplete ? 0 : 1;
                SetEvent(string.Empty, CosmicPalette.Silver, 0f);
                PlaySfx(positive, 0.65f, 0.82f);
            }
        }

        private void BeginRound()
        {
            if (!CanUsePortal) { RefreshCeilingReadyStatus(); return; }
            _state = GameState.Playing;
            SetCoreHudVisible(true);
            _timeRemaining = RoundDuration;
            _spawnTimer = 0.72f;
            ArmMusicBeatClock();
            _statusText.text = string.Empty;
            _resultGroup.alpha = 0f;
            _resultGroup.gameObject.SetActive(false);
            _resultGroup.blocksRaycasts = false;
            SetTutorialVisible(false);
            _musicTargetVolume = 0.24f;
            SetEvent("CONTAINMENT RUN ACTIVE", CosmicPalette.Cyan, 1.4f);
            PlaySfx(positive, 0.7f, 1f);
        }

        private void UpdateRound(float deltaTime)
        {
            if (!CanUsePortal) { EnterScanning(); return; }
            _timeRemaining -= deltaTime;

            if (_overdriveTimer > 0f)
            {
                _overdriveTimer -= deltaTime;
                Time.timeScale = 0.68f;
                if (_overdriveTimer <= 0f)
                {
                    Time.timeScale = 1f;
                    SetEvent("OVERDRIVE COMPLETE", CosmicPalette.Silver, 1.2f);
                }
            }

            if (TryGetMusicBeat(out var musicBeat))
            {
                if (musicBeat != _lastVisualizedBeat)
                {
                    _lastVisualizedBeat = musicBeat;
                    _portalPulse = Mathf.Max(_portalPulse, musicBeat % 4 == 0 ? 0.72f : 0.32f);
                    EmitMeteorShowerBeat(musicBeat);
                    // Polish: during OVERDRIVE, pulse a violet border flash on every downbeat (beat-synced, lightweight)
                    if (_overdriveTimer > 0f && musicBeat % 4 == 0)
                    {
                        Flash(CosmicPalette.Violet, 0.07f);
                    }
                }

                AdvanceMusicSpawns(_lastMusicTimeSamples, _musicSource.clip.frequency);
            }
            else if (_musicSource == null || _musicSource.clip == null)
            {
                // Audio may be absent in a development scene; retain a deterministic fallback.
                _spawnTimer -= deltaTime;
                if (_spawnTimer <= 0f)
                {
                    if (_targets.Count < 3)
                    {
                        SpawnTarget();
                    }
                    var elapsed = 1f - _timeRemaining / RoundDuration;
                    var interval = Mathf.Lerp(1.28f, 0.68f, elapsed);
                    _spawnTimer = _overdriveTimer > 0f ? interval * 0.63f : interval;
                }
            }

            if (_timeRemaining <= 0f || _lives <= 0)
            {
                EndRound();
            }

            UpdateHud();
        }

        private void ArmMusicBeatClock()
        {
            _musicBeatClockReady = _musicSource != null && _musicSource.clip != null && _musicSource.isPlaying;
            _lastVisualizedBeat = -1;
            _lastMusicTimeSamples = _musicBeatClockReady ? _musicSource.timeSamples : 0;
            if (TryGetMusicBeat(out var currentBeat))
            {
                _lastVisualizedBeat = currentBeat;
                ResetMusicSpawnGrid(_lastMusicTimeSamples, _musicSource.clip.frequency);
            }
        }

        private bool TryGetMusicBeat(out long beat)
        {
            beat = 0;
            if (_musicSource == null || !_musicSource.isPlaying ||
                _musicSource.clip == null || _musicSource.clip.frequency <= 0 || _musicSource.clip.samples <= 0)
            {
                _musicBeatClockReady = false;
                return false;
            }

            var samples = _musicSource.timeSamples;
            beat = ObserveMusicSample(samples, _musicSource.clip.frequency);
            return true;
        }

        private long ObserveMusicSample(int samples, int frequency)
        {
            if (!_musicBeatClockReady || samples < _lastMusicTimeSamples)
            {
                // A loop is the same song starting again, not an arbitrary extension of its
                // beat grid. Also recover when a clip begins playing after the round starts.
                _musicBeatClockReady = true;
                _lastVisualizedBeat = -1;
                ResetMusicSpawnGrid(samples, frequency);
            }
            _lastMusicTimeSamples = samples;
            return MusicBeatAtSample(samples, frequency);
        }

        private static long NextAlignedSpawnBeat(long beat, int step)
        {
            step = System.Math.Max(1, step);
            return (System.Math.Max(0L, beat) / step + 1L) * step;
        }

        private void ResetMusicSpawnGrid(long samples, int frequency)
        {
            var leadSamples = (long)System.Math.Round(FallingTarget.EntranceDuration * frequency);
            _nextSpawnBeat = NextAlignedSpawnBeat(MusicBeatAtSample(samples + leadSamples, frequency), GetSpawnBeatStep());
        }

        private void AdvanceMusicSpawns(long samples, int frequency)
        {
            var leadSamples = (long)System.Math.Round(FallingTarget.EntranceDuration * frequency);
            var preparationBeat = MusicBeatAtSample(samples + leadSamples, frequency);
            if (preparationBeat < _nextSpawnBeat) return;

            var beatStart = System.Math.Round(MusicBeatPhaseSeconds * frequency) + _nextSpawnBeat * 60d * frequency / MusicBpm;
            var entranceElapsed = (float)((samples - (beatStart - leadSamples)) / frequency);
            // Keep the existing 180 ms reveal, but let its full-size accent land on the kick.
            // Skip stale beats after a hitch or a full arena; never replay a burst of missed notes.
            if (preparationBeat == _nextSpawnBeat && entranceElapsed >= 0f && entranceElapsed <= 0.08f && _targets.Count < 3)
            {
                var target = SpawnTarget();
                target.SynchronizeEntrance(entranceElapsed);
            }
            _nextSpawnBeat = NextAlignedSpawnBeat(preparationBeat, GetSpawnBeatStep());
        }

        private static long MusicBeatAtSample(long absoluteSamples, int frequency)
        {
            if (absoluteSamples <= 0 || frequency <= 0)
            {
                return 0;
            }

            var phaseSamples = (long)System.Math.Round(MusicBeatPhaseSeconds * frequency);
            absoluteSamples = System.Math.Max(0L, absoluteSamples - phaseSamples);
            var beatsPerSample = MusicBpm / (60d * frequency);
            return (long)System.Math.Floor(absoluteSamples * beatsPerSample);
        }

        private int GetSpawnBeatStep()
        {
            if (_overdriveTimer > 0f)
            {
                return 1;
            }

            var elapsed = 1f - Mathf.Clamp01(_timeRemaining / RoundDuration);
            return elapsed < 0.34f ? 4 : 2;
        }

        private void EndRound()
        {
            if (_state != GameState.Playing)
            {
                return;
            }

            _state = GameState.Results;
            Time.timeScale = 1f;
            _musicTargetVolume = 0.12f;
            ClearTargets();
            var contained = _lives > 0;
            // Polish: dramatic result flash — cool cyan for success, heavy red for failure
            Flash(contained ? CosmicPalette.Cyan : CosmicPalette.Red, contained ? 0.38f : 0.72f);
            var rank = !contained ? "F" : _score >= 9000 ? "S" : _score >= 6000 ? "A" : _score >= 3500 ? "B" : "C";
            var heading = contained ? "CONTAINMENT COMPLETE" : "LIFE LINK LOST";
            _resultText.text = heading;
            _resultRankText.text = rank;
            // Polish: rank letter uses its semantic color — S=white, A=cyan, B=amber, C=muted, F=red
            if (_resultRankText != null)
            {
                _resultRankText.color = rank switch
                {
                    "S" => Color.white,
                    "A" => CosmicPalette.Cyan,
                    "B" => CosmicPalette.Amber,
                    "F" => CosmicPalette.Red,
                    _ => new Color(0.72f, 0.79f, 0.84f, 1f)   // C = CosmicPalette.Silver
                };
            }
            // Polish: success heading in cyan, failure in coral/red for immediate readability
            if (_resultText != null)
                _resultText.color = contained ? CosmicPalette.Cyan : MenuAccent;
            _resultScoreText.text = _score.ToString("000000");
            _resultDetailText.text = $"BEST CHAIN  {_bestCombo:00}          LIVES  {_lives}/{MaxLives}" +
                (_ceilingPreviewSelected ? "\nPREVIEW RUN / CEILING NOT MAPPED" : "");
            _resultGroup.gameObject.SetActive(true);
            SetCoreHudVisible(false);
            _stageSelected = 2;
            _resultGroup.alpha = 1f;
            _resultGroup.blocksRaycasts = true;
            SetTutorialVisible(false);
            _statusText.text = string.Empty;
            SetEvent(string.Empty, CosmicPalette.Silver, 0f);
            UpdateHud();
            PlaySfx(contained ? positive : negative, 0.9f, rank == "S" ? 1.25f : 0.92f);
        }

        private FallingTarget SpawnTarget()
        {
            var roll = Random.value;
            var kind = roll < 0.12f ? TargetKind.Bomb
                : roll < 0.31f ? TargetKind.Quantum
                : roll < 0.65f ? TargetKind.Plasma
                : TargetKind.Meteor;

            return CreateTarget(
                kind,
                FindSeparatedSpawnPosition(),
                new Vector3(Random.Range(-0.24f, 0.24f), Random.Range(-0.32f, -0.16f), Random.Range(-0.05f, 0.08f)));
        }

        private Vector3 FindSeparatedSpawnPosition()
        {
            var candidate = _portalRoot.position;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                candidate = _portalRoot.position + _portalRoot.rotation * new Vector3(
                    Random.Range(-0.75f, 0.75f) * _portalCeilingScale,
                    Random.Range(-0.15f, -0.08f),
                    Random.Range(-0.28f, 0.28f) * _portalCeilingScale);
                var separated = true;
                foreach (var target in _targets)
                {
                    if (target != null && Vector3.Distance(candidate, target.transform.position) < 0.54f)
                    {
                        separated = false;
                        break;
                    }
                }
                if (separated)
                {
                    return candidate;
                }
            }
            return candidate;
        }

        private FallingTarget CreateTarget(TargetKind kind, Vector3 position, Vector3 velocity, bool attachModel = true)
        {
            var root = new GameObject(kind + " Target");
            root.transform.position = position;
            root.transform.localScale = Vector3.zero;
            var target = root.AddComponent<FallingTarget>();
            target.Configure(this, kind, velocity);
            BuildFallbackTarget(root.transform, kind);
            // The gameplay target language is intentionally procedural: a shared anomaly grammar
            // keeps every signal readable in passthrough and avoids mismatched source-model styles.

            var collider = root.AddComponent<SphereCollider>();
            collider.radius = kind == TargetKind.Quantum ? 0.32f : 0.27f;
            collider.isTrigger = true;
            _targets.Add(target);
            return target;
        }

        public void HandleBladeHit(FallingTarget target, HandSide hand, Vector3 direction, Vector3 point)
        {
            if (_state != GameState.Playing || target == null || target.Resolved)
            {
                return;
            }

            if (target.Kind == TargetKind.Bomb)
            {
                target.Resolve();
                _score = Mathf.Max(0, _score - 250);
                _combo = 0;
                SpawnBurst(point, CosmicPalette.Red, direction, true);
                Flash(CosmicPalette.Red, 0.5f);
                PlaySfx(explosion, 1f, Random.Range(0.88f, 1.03f));
                Blade(hand).Haptic(1f, 0.18f);
                RemoveTarget(target);
                LoseLife("SINGULARITY HIT", 0.5f);
                return;
            }

            if (target.Kind == TargetKind.Quantum)
            {
                if (!target.QuantumPrimed)
                {
                    target.PrimeQuantum(hand, Time.unscaledTime);
                    _score += 45;
                    SpawnBurst(point, CosmicPalette.Violet, direction, false, 10);
                    SetEvent("DUAL LINK CHARGED  >  SECOND BLADE", CosmicPalette.Violet, 1f);
                    PlaySfx(positive, 0.5f, 1.35f);
                    Blade(hand).Haptic(0.35f, 0.05f);
                    return;
                }

                if (!target.TryCompleteQuantum(hand, Time.unscaledTime))
                {
                    SetEvent("DUAL LINK  >  CUT WITH OTHER BLADE", CosmicPalette.Violet, 0.5f);
                    Blade(hand).Haptic(0.22f, 0.04f);
                    return;
                }
            }
            else
            {
                var correct = target.Kind == TargetKind.Plasma ? hand == HandSide.Left : hand == HandSide.Right;
                if (!correct)
                {
                    WrongHand(target, hand);
                    return;
                }
            }

            ResolveCorrect(target, hand, direction, point);
        }

        private void ResolveCorrect(FallingTarget target, HandSide hand, Vector3 direction, Vector3 point)
        {
            target.Resolve();
            _combo++;
            _bestCombo = Mathf.Max(_bestCombo, _combo);
            var baseScore = target.Kind switch
            {
                TargetKind.Plasma => 100,
                TargetKind.Meteor => 130,
                TargetKind.Quantum => 285,
                _ => 0
            };
            var multiplier = 1f + Mathf.Min(4, _combo / 4) * 0.25f;
            var awarded = Mathf.RoundToInt(baseScore * multiplier);
            _score += awarded;
            _portalPulse = 1f;
            SpawnBurst(point, CosmicPalette.For(target.Kind), direction, false);
            SetEvent($"+{awarded:000}   CHAIN {_combo:00}", CosmicPalette.For(target.Kind), 0.8f);
            PlaySfx(bladeWhoosh, 0.62f, Random.Range(0.92f, 1.12f));
            PlaySfx(positive, 0.42f, Mathf.Min(1.45f, 0.95f + _combo * 0.025f));
            Blade(hand).Haptic(Mathf.Min(0.8f, 0.34f + _combo * 0.025f), 0.06f);
            // Polish: flash intensity scales with the growing combo (0.12 at chain 1 → up to 0.34 at chain 12)
            Flash(CosmicPalette.For(target.Kind), 0.12f + Mathf.Min(0.22f, _combo * 0.018f));

            if (_combo >= 8 && _overdriveTimer <= 0f)
            {
                _overdriveTimer = 5.2f;
                SetEvent("OVERDRIVE", Color.white, 1.3f);
                // Polish: stronger OVERDRIVE activation flash + simultaneous haptic burst on both hands
                Flash(CosmicPalette.Violet, 0.55f);
                _leftBlade?.Haptic(0.9f, 0.12f);
                _rightBlade?.Haptic(0.9f, 0.12f);
            }

            RemoveTarget(target);
        }

        private void WrongHand(FallingTarget target, HandSide hand)
        {
            target.RegisterWrongHand();
            target.Resolve();
            _combo = 0;
            var position = target.transform.position;
            SpawnBurst(position, CosmicPalette.Red, Vector3.up, false, 8);
            PlaySfx(negative, 0.55f, 1.12f);
            // Polish: wrong-hand error — longer, stronger haptic to clearly distinguish from a correct hit
            Blade(hand).Haptic(0.78f, 0.16f);
            RemoveTarget(target);
            LoseLife(hand == HandSide.Left ? "WRONG BLADE  L" : "WRONG BLADE  R", 0.22f);
        }

        public void TargetMissed(FallingTarget target)
        {
            if (_state != GameState.Playing || target == null)
            {
                return;
            }

            if (target.Kind == TargetKind.Bomb)
            {
                _score += 35;
                SetEvent("HAZARD EVADED  +035", CosmicPalette.Silver, 0.7f);
            }
            else
            {
                _combo = 0;
                PlaySfx(negative, 0.4f, 0.82f);
            }
            RemoveTarget(target);
            if (target.Kind != TargetKind.Bomb)
            {
                LoseLife("SIGNAL MISSED", 0.22f);
            }
        }

        private void LoseLife(string reason, float flashAlpha)
        {
            _lives = Mathf.Max(0, _lives - 1);
            _combo = 0;
            SetEvent($"{reason}   LIFE {_lives}/{MaxLives}", CosmicPalette.Red, 1.2f);
            Flash(CosmicPalette.Red, flashAlpha);
            UpdateHud();
            if (_lives <= 0)
            {
                EndRound();
            }
        }

        private void RemoveTarget(FallingTarget target)
        {
            _targets.Remove(target);
            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }

        private void ClearTargets()
        {
            foreach (var target in _targets)
            {
                if (target != null)
                {
                    Destroy(target.gameObject);
                }
            }
            _targets.Clear();
        }

        private SliceBlade Blade(HandSide side)
        {
            return side == HandSide.Left ? _leftBlade : _rightBlade;
        }

        private bool WasStartPressed()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            var anyTriggerHeld = false;
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var device = InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var held) && held)
                {
                    anyTriggerHeld = true;
                    break;
                }
            }

            // The emulator may carry a trigger's held bit across app launches. Treat it as
            // neutral until it has been released once, then require a fresh press for
            // START/restart.
            if (!anyTriggerHeld)
            {
                _xrTriggerWasHeld = false;
                _xrTriggerArmed = true;
                return false;
            }

            var pressedThisFrame = _xrTriggerArmed && !_xrTriggerWasHeld;
            _xrTriggerWasHeld = anyTriggerHeld;
            return pressedThisFrame && _xrStartCooldown <= 0f;
        }

        private void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _sfxSource == null)
            {
                return;
            }
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
