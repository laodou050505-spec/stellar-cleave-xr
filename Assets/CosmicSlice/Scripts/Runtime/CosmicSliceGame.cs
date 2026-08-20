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
        public Shader portalShader;
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

        public void ApplyDetectedCeiling(Vector3 trackingPosition)
        {
            if (_portalRoot == null)
            {
                return;
            }

            var worldPosition = _rigTransform != null
                ? _rigTransform.TransformPoint(trackingPosition)
                : trackingPosition;
            worldPosition.y -= 0.07f;
            _detectedCeilingPosition = worldPosition;
            _hasDetectedCeiling = true;
            // The portal is a deliberate arena landmark. Ceiling sensing informs status and the
            // ceiling grid only; it must not relocate core targets into an arbitrary room pose.
            if (_ceilingGridRoot != null)
            {
                _ceilingGridRoot.position = new Vector3(worldPosition.x, worldPosition.y + 0.025f, worldPosition.z);
            }
            _portalPulse = 1f;
            SetEvent(_state == GameState.Playing ? "CEILING MAP SAVED" : "REAL CEILING LOCKED", CosmicPalette.Cyan, 2.2f);
        }

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
        private float _eventTimer;
        private float _overdriveTimer;
        private float _portalPulse;
        private float _shootingStarTimer;
        private float _scanParticleAccumulator;
        private float _lastScanVisualProgress;
        private float _xrStartCooldown;
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
            UpdateHeadTracking();
            AnimateSpaceVista(unscaledDelta);
            UpdateHudFacing(unscaledDelta);
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
                    UpdateScanning(unscaledDelta);
                    break;
                case GameState.Ready:
                    if (WasStartPressed())
                    {
                        BeginRound();
                    }
                    break;
                case GameState.Playing:
                    UpdateRound(unscaledDelta);
                    break;
                case GameState.Results:
                    if (WasStartPressed())
                    {
                        EnterScanning();
                    }
                    break;
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            _musicTargetVolume = 0.11f;
        }

        private void EnterScanning()
        {
            SetGameplayStageVisible(true);
            ResetToCurrentStageStart(resetHeadPose: false);
            ClearTargets();
            _state = GameState.Scanning;
            SetCoverVisible(false);
            DisposeTitleRoot();
            BuildGameplayHudIfNeeded();
            SetBladesVisible(true);
            _scanTime = 0f;
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _lives = MaxLives;
            _timeRemaining = RoundDuration;
            _overdriveTimer = 0f;
            Time.timeScale = 1f;
            _resultGroup.alpha = 0f;
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
            _statusText.text = "LOCAL SPACE SURVEY\n00%";
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
            _coverFocusBar = null;
            _coverOptionLabels = null;
        }

        private void UpdateScanning(float deltaTime)
        {
            _scanTime += deltaTime;
            var progress = Mathf.Clamp01(_scanTime / 4.6f);
            UpdateSpatialScanIntro(progress);
            SetScanEnvironmentVisible(
                progress >= 0.28f,
                progress >= 0.44f,
                progress >= 0.52f,
                progress >= 0.61f,
                progress >= 0.69f);
            SetCoreHudVisible(progress >= 0.76f);

            if (progress < 0.26f)
            {
                _statusText.text = $"LOCAL SPACE SURVEY\n{Mathf.RoundToInt(progress / 0.26f * 24f):00}%";
            }
            else if (progress < 0.68f)
            {
                _statusText.text = $"GEOMETRY SWEEP\n{Mathf.RoundToInt(Mathf.InverseLerp(0.26f, 0.68f, progress) * 100f):00}%";
            }
            else
            {
                _statusText.text = $"ANOMALY FIELD LINK\n{Mathf.RoundToInt(Mathf.InverseLerp(0.68f, 1f, progress) * 100f):00}%";
            }

            if (_portalRoot != null && progress >= 0.69f)
            {
                _portalRoot.gameObject.SetActive(true);
                var portalProgress = Mathf.InverseLerp(0.69f, 1f, progress);
                _portalRoot.localScale = Vector3.one * Mathf.SmoothStep(0.02f, 1f, portalProgress);
                _portalLight.intensity = Mathf.Lerp(0.18f, 3.2f, portalProgress);
            }
            if (progress >= 1f)
            {
                _state = GameState.Ready;
                if (_scanIntroRoot != null) _scanIntroRoot.gameObject.SetActive(false);
                SetScanEnvironmentVisible(true, true, true, true, true);
                SetCoreHudVisible(true);
                _statusText.text = "PORTAL STABLE\nTRIGGER TO START";
                SetTutorialVisible(true);
                SetEvent("ANOMALY CONTAINMENT READY", CosmicPalette.Silver, 2.4f);
                PlaySfx(positive, 0.65f, 0.82f);
            }
        }

        private void BeginRound()
        {
            _state = GameState.Playing;
            _timeRemaining = RoundDuration;
            _spawnTimer = 0.72f;
            _statusText.text = string.Empty;
            _resultGroup.alpha = 0f;
            _resultGroup.blocksRaycasts = false;
            SetTutorialVisible(false);
            _musicTargetVolume = 0.24f;
            SetEvent("CONTAINMENT RUN ACTIVE", CosmicPalette.Cyan, 1.4f);
            PlaySfx(positive, 0.7f, 1f);
        }

        private void UpdateRound(float deltaTime)
        {
            _timeRemaining -= deltaTime;
            _spawnTimer -= deltaTime;

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

            if (_timeRemaining <= 0f || _lives <= 0)
            {
                EndRound();
            }

            UpdateHud();
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
            var rank = !contained ? "F" : _score >= 9000 ? "S" : _score >= 6000 ? "A" : _score >= 3500 ? "B" : "C";
            var heading = contained ? "CONTAINMENT COMPLETE" : "LIFE LINK LOST";
            _resultText.text = $"{heading}\n\nRANK  {rank}\n\n{_score:000000}\nSCORE   //   CHAIN {_bestCombo:00}\nLIFE LINK   {_lives}/{MaxLives}\n\nTRIGGER TO RESCAN";
            _resultGroup.alpha = 1f;
            _resultGroup.blocksRaycasts = true;
            SetTutorialVisible(false);
            _statusText.text = string.Empty;
            SetEvent(string.Empty, CosmicPalette.Silver, 0f);
            UpdateHud();
            PlaySfx(contained ? positive : negative, 0.9f, rank == "S" ? 1.25f : 0.92f);
        }

        private void SpawnTarget()
        {
            var roll = Random.value;
            var kind = roll < 0.12f ? TargetKind.Bomb
                : roll < 0.31f ? TargetKind.Quantum
                : roll < 0.65f ? TargetKind.Plasma
                : TargetKind.Meteor;

            CreateTarget(
                kind,
                FindSeparatedSpawnPosition(),
                new Vector3(Random.Range(-0.24f, 0.24f), Random.Range(-0.32f, -0.16f), Random.Range(-0.05f, 0.08f)));
        }

        private Vector3 FindSeparatedSpawnPosition()
        {
            var candidate = _portalRoot.position;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                candidate = _portalRoot.position + new Vector3(
                    Random.Range(-0.88f, 0.88f),
                    Random.Range(-0.15f, 0.04f),
                    Random.Range(-0.28f, 0.28f));
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
            Flash(CosmicPalette.For(target.Kind), 0.12f);

            if (_combo >= 8 && _overdriveTimer <= 0f)
            {
                _overdriveTimer = 5.2f;
                SetEvent("OVERDRIVE", Color.white, 1.3f);
                Flash(CosmicPalette.Violet, 0.28f);
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
            Blade(hand).Haptic(0.5f, 0.09f);
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
            if (_xrStartCooldown > 0f)
            {
                return false;
            }

            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var device = InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var held) && held)
                {
                    _xrStartCooldown = 0.5f;
                    return true;
                }
            }
            return false;
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
