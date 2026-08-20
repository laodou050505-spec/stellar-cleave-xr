using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private const float MrSpaceVeilOpacity = 0.38f;
        private const float InitialGalaxyForwardYaw = 82f;

        private void BuildMaterials()
        {
            _cyanGlow = MakeGlowMaterial("Cyan Energy", CosmicPalette.Cyan);
            _amberGlow = MakeGlowMaterial("Amber Energy", CosmicPalette.Amber);
            _violetGlow = MakeGlowMaterial("Violet Energy", CosmicPalette.Violet);
            _redGlow = MakeGlowMaterial("Red Energy", CosmicPalette.Red);
            _cyanParticle = MakeParticleMaterial("Cyan Star Dust", CosmicPalette.Cyan);
            _amberParticle = MakeParticleMaterial("Amber Star Dust", CosmicPalette.Amber);
            _violetParticle = MakeParticleMaterial("Violet Star Dust", CosmicPalette.Violet);
            _redParticle = MakeParticleMaterial("Red Star Dust", CosmicPalette.Red);
            _darkMetal = MakeStandardMaterial("Anomaly Graphite", new Color(0.075f, 0.085f, 0.105f), 0.82f, 0.78f);
            _floorMaterial = MakeTransparentMaterial("Spatial Glass", new Color(0.025f, 0.12f, 0.17f, 0.055f), 0.1f, 0.64f);
            _planetMaterial = MakeStandardMaterial("Cerulean Planet", new Color(0.025f, 0.12f, 0.22f), 0.16f, 0.68f, new Color(0.015f, 0.17f, 0.34f));
            _moonMaterial = MakeStandardMaterial("Rose Moon", new Color(0.2f, 0.055f, 0.12f), 0.12f, 0.52f, new Color(0.21f, 0.025f, 0.1f));
            _targetMaterials[TargetKind.Plasma] = MakeStandardMaterial("Cobalt Core", new Color(0.025f, 0.17f, 0.24f), 0.5f, 0.92f, CosmicPalette.Cyan * 2.2f);
            _targetMaterials[TargetKind.Meteor] = MakeStandardMaterial("Solar Core", new Color(0.29f, 0.11f, 0.025f), 0.55f, 0.78f, CosmicPalette.Amber * 1.55f);
            _targetMaterials[TargetKind.Quantum] = MakeStandardMaterial("Violet Core", new Color(0.105f, 0.035f, 0.24f), 0.62f, 0.9f, CosmicPalette.Violet * 1.9f);
            _targetMaterials[TargetKind.Bomb] = MakeStandardMaterial("Singularity Shell", new Color(0.006f, 0.003f, 0.008f), 0.96f, 0.94f, CosmicPalette.Red * 0.22f);
        }

        private void BuildCameraRig()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var rig = new GameObject("XR Origin - Runtime Fallback");
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(rig.transform, false);
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            _rigTransform = _camera.transform.root;
            BuildDesignPlayerStart();
            ResetToDesignPlayerStart(resetHeadPose: true);
            _camera.fieldOfView = 66f;
            _camera.nearClipPlane = 0.06f;
            _camera.farClipPlane = 80f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.004f, 0.006f, 0.009f, Application.platform == RuntimePlatform.Android ? 0f : 1f);
            _camera.allowHDR = true;

            var trackedPoseDriver = _camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (trackedPoseDriver != null && Application.platform != RuntimePlatform.Android)
            {
                trackedPoseDriver.enabled = false;
            }
        }

        private void BuildDesignPlayerStart()
        {
            _titlePlayerStart = new GameObject("Design Player Start - Title Cover").transform;
            _titlePlayerStart.position = CosmicSliceDesign.TitlePlayerRootPosition;
            _titlePlayerStart.rotation = Quaternion.Euler(0f, CosmicSliceDesign.TitlePlayerYaw, 0f);
            _gameplayPlayerStart = new GameObject("Design Player Start - Gameplay Arena").transform;
            _gameplayPlayerStart.position = CosmicSliceDesign.GameplayPlayerRootPosition;
            _gameplayPlayerStart.rotation = Quaternion.Euler(0f, CosmicSliceDesign.GameplayPlayerYaw, 0f);
        }

        /// <summary>
        /// Restores the XR Origin to the same authored pose used by the desktop Game View.
        /// Head tracking is intentionally local to this root: an HMD offset is preserved after
        /// reset, while the authored forward direction and arena distance remain stable.
        /// </summary>
        private void ResetToDesignPlayerStart(bool resetHeadPose)
        {
            ResetToCurrentStageStart(resetHeadPose);
        }

        private void ResetToCurrentStageStart(bool resetHeadPose)
        {
            var start = _gameplayStageActive ? _gameplayPlayerStart : _titlePlayerStart;
            var yaw = _gameplayStageActive ? CosmicSliceDesign.GameplayPlayerYaw : CosmicSliceDesign.TitlePlayerYaw;
            var eyeHeight = _gameplayStageActive ? CosmicSliceDesign.GameplayEyeHeight : CosmicSliceDesign.TitleEyeHeight;
            if (_rigTransform == null || start == null)
            {
                return;
            }

            _rigTransform.SetPositionAndRotation(start.position, start.rotation);
            _desktopYaw = yaw;
            _desktopPitch = 0f;
            if (resetHeadPose)
            {
                _camera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
                _camera.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateHeadTracking()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var trackedPoseDriver = _camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (trackedPoseDriver == null && head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position))
            {
                head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
                _camera.transform.localPosition = position;
                _camera.transform.localRotation = rotation;
            }

            if (Application.platform != RuntimePlatform.Android && !head.isValid && !_qaMode)
            {
                UpdateDesktopFirstPerson(Time.unscaledDeltaTime);
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetToDesignPlayerStart(!head.isValid);
            }
        }

        private void UpdateDesktopFirstPerson(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var look = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed) look.x -= 1f;
            if (keyboard.rightArrowKey.isPressed) look.x += 1f;
            if (keyboard.upArrowKey.isPressed) look.y += 1f;
            if (keyboard.downArrowKey.isPressed) look.y -= 1f;
            _desktopYaw += look.x * 58f * deltaTime;
            _desktopPitch = Mathf.Clamp(_desktopPitch - look.y * 46f * deltaTime, -58f, 68f);

            var mouse = Mouse.current;
            var mouseLookHeld = mouse != null && mouse.middleButton.isPressed;
            var alternateLookHeld = mouse != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            if (mouseLookHeld || alternateLookHeld)
            {
                var mouseDelta = mouse.delta.ReadValue();
                _desktopYaw += mouseDelta.x * 0.12f;
                _desktopPitch = Mathf.Clamp(_desktopPitch - mouseDelta.y * 0.1f, -58f, 68f);
            }

            _rigTransform.rotation = Quaternion.Euler(0f, _desktopYaw, 0f);
            _camera.transform.localRotation = Quaternion.Euler(_desktopPitch, 0f, 0f);

            var move = Vector3.zero;
            if (keyboard.wKey.isPressed) move.z += 1f;
            if (keyboard.sKey.isPressed) move.z -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            if (keyboard.eKey.isPressed) move.y += 1f;
            if (keyboard.qKey.isPressed) move.y -= 1f;
            if (move.sqrMagnitude > 1f) move.Normalize();

            var speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 3.2f : 1.65f;
            var horizontalForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            var horizontalRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            _rigTransform.position += (horizontalForward * move.z + horizontalRight * move.x + Vector3.up * move.y) * speed * deltaTime;
        }

        private void BuildEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.085f, 0.11f, 0.17f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.065f, 0.105f);
            RenderSettings.ambientGroundColor = new Color(0.012f, 0.026f, 0.045f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.003f, 0.008f, 0.017f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 62f;

            var isHeadsetMixedReality = Application.platform == RuntimePlatform.Android;
            var previewMixedRealityInEditor = Application.isEditor || HasCommandLineFlag("-cosmic-mr-preview");
            ConfigureSpaceBackdrop(isHeadsetMixedReality, previewMixedRealityInEditor);
            BuildTransparentSpatialGrid();
            BuildDeepSpaceVista();
            BuildUserSpaceSetDressing();
            BuildSpatialScanIntro();

            var key = new GameObject("Cool Key Light").AddComponent<Light>();
            key.transform.SetParent(_gameplayRoot, true);
            key.type = LightType.Directional;
            key.color = new Color(0.61f, 0.68f, 0.9f);
            key.intensity = isHeadsetMixedReality ? 0.42f : 0.85f;
            key.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var rim = new GameObject("Warm Rim Light").AddComponent<Light>();
            rim.transform.SetParent(_gameplayRoot, true);
            rim.type = LightType.Point;
            rim.color = new Color(0.92f, 0.36f, 0.46f);
            rim.intensity = 1.15f;
            rim.range = 4.5f;
            rim.transform.position = new Vector3(2.8f, 1.4f, 1.4f);

            BuildRoomScaleCosmicDust();
        }

        private Transform BuildMissionConsole(string name, Vector3 position, Vector3 playerRootPosition, Transform parent)
        {
            var consoleRoot = new GameObject(name).transform;
            consoleRoot.SetParent(parent, true);
            consoleRoot.position = position;
            var authoredEye = playerRootPosition + Vector3.up * CosmicSliceDesign.TitleEyeHeight;
            // Unity's world-space Canvas front points opposite the logical console forward.
            // Point this root away from the authored eye so its text and physical option faces
            // are readable from the player start rather than mirrored from their back sides.
            consoleRoot.rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(consoleRoot.position - authoredEye, Vector3.up).normalized,
                Vector3.up);

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Mission Console Physical Backing";
            backing.transform.SetParent(consoleRoot, false);
            backing.transform.localPosition = new Vector3(0f, 0f, 0.055f);
            backing.transform.localScale = new Vector3(2.72f, 1.54f, 0.10f);
            var material = new Material(_darkMetal) { name = "Mission Console Graphite" };
            backing.GetComponent<Renderer>().sharedMaterial = material;
            var collider = backing.GetComponent<BoxCollider>();
            collider.isTrigger = false;

            var cyanRail = CosmicSliceMeshFactory.CreateTorusObject("Mission Console Cyan Halo", 1.22f, 0.008f, _cyanGlow);
            cyanRail.transform.SetParent(consoleRoot, false);
            cyanRail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cyanRail.transform.localScale = new Vector3(1f, 0.58f, 1f);
            return consoleRoot;
        }

        private void ConfigureSpaceBackdrop(bool preservePassthrough, bool previewMixedRealityInEditor)
        {
            var useMixedRealityVeil = preservePassthrough || previewMixedRealityInEditor;
            if (useMixedRealityVeil)
            {
                RenderSettings.skybox = null;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = preservePassthrough
                    ? Color.clear
                    : new Color(0.16f, 0.17f, 0.18f, 1f);
            }

            if (spacePanorama == null || spaceBackdropShader == null)
            {
                return;
            }

            var material = new Material(spaceBackdropShader)
            {
                name = useMixedRealityVeil
                    ? "MR Adaptive Deep Space Veil - 38 Percent Peak"
                    : "Generated Deep Space Panorama"
            };
            material.SetTexture("_MainTex", spacePanorama);
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", useMixedRealityVeil ? 1.18f : 0.82f);
            if (material.HasProperty("_Opacity")) material.SetFloat("_Opacity", useMixedRealityVeil ? MrSpaceVeilOpacity : 1f);
            if (material.HasProperty("_Softness")) material.SetFloat("_Softness", useMixedRealityVeil ? 0.72f : 0.18f);
            if (material.HasProperty("_AdaptiveAlpha")) material.SetFloat("_AdaptiveAlpha", useMixedRealityVeil ? 1f : 0f);
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", useMixedRealityVeil ? new Color(0.64f, 0.78f, 1f, 1f) : new Color(0.76f, 0.9f, 1f, 1f));
            var skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skySphere.name = useMixedRealityVeil
                ? "MR Adaptive Deep Space Veil - 38 Percent Peak"
                : "Desktop Generated Deep Space Sky Sphere";
            skySphere.transform.position = _camera.transform.position;
            skySphere.transform.localScale = Vector3.one * 118f;
            skySphere.transform.rotation = Quaternion.Euler(0f, InitialGalaxyForwardYaw, 0f);
            Destroy(skySphere.GetComponent<Collider>());
            skySphere.GetComponent<Renderer>().sharedMaterial = material;
            _desktopSpaceBackdrop = skySphere.transform;
            RenderSettings.skybox = null;
            _camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void BuildTransparentSpatialGrid()
        {
            _floorGridRoot = new GameObject("Transparent Spatial Floor Grid").transform;
            const float halfWidth = 8.4f;
            const float backZ = -9.8f;
            const float frontZ = 9.8f;
            const float ceilingY = 3.67f;
            const float spacing = 0.7f;
            var floorY = 0.005f;
            for (var index = -12; index <= 12; index++)
            {
                var major = index % 3 == 0;
                CreateDoubleGridLine(_floorGridRoot, new Vector3(index * spacing, floorY, backZ), new Vector3(index * spacing, floorY, frontZ), major ? 0.15f : 0.045f, major ? 0.0105f : 0.0038f, major ? "Floor Major Double Circuit" : "Floor Micro Double Circuit", major ? CosmicPalette.Cyan : new Color(0.32f, 0.7f, 1f));
            }
            for (var index = -14; index <= 14; index++)
            {
                var major = index % 3 == 0;
                CreateDoubleGridLine(_floorGridRoot, new Vector3(-halfWidth, floorY, index * spacing), new Vector3(halfWidth, floorY, index * spacing), major ? 0.15f : 0.045f, major ? 0.0105f : 0.0038f, major ? "Floor Major Double Circuit" : "Floor Micro Double Circuit", major ? CosmicPalette.Cyan : new Color(0.32f, 0.7f, 1f));
            }

            _ceilingGridRoot = new GameObject("Transparent Ceiling Sensor Grid").transform;
            _ceilingGridRoot.position = new Vector3(0f, ceilingY, 0.55f);
            const float ceilingSpacing = 1.05f;
            for (var index = -8; index <= 8; index++)
            {
                var major = index % 3 == 0;
                CreateDoubleGridLine(_ceilingGridRoot, new Vector3(index * ceilingSpacing, 0f, backZ - 0.55f), new Vector3(index * ceilingSpacing, 0f, frontZ - 0.55f), major ? 0.105f : 0.034f, major ? 0.0068f : 0.0026f, major ? "Ceiling Major Double Sensor" : "Ceiling Micro Double Sensor", major ? CosmicPalette.Violet : new Color(0.28f, 0.56f, 0.92f), false);
            }
            for (var index = -9; index <= 9; index++)
            {
                var major = index % 3 == 0;
                CreateDoubleGridLine(_ceilingGridRoot, new Vector3(-halfWidth, 0f, index * ceilingSpacing), new Vector3(halfWidth, 0f, index * ceilingSpacing), major ? 0.105f : 0.034f, major ? 0.0068f : 0.0026f, major ? "Ceiling Major Double Sensor" : "Ceiling Micro Double Sensor", major ? CosmicPalette.Violet : new Color(0.28f, 0.56f, 0.92f), false);
            }

            _containmentGridRoot = new GameObject("Transparent Spatial Containment Cube").transform;
            BuildSpatialGridWall(_containmentGridRoot, "Left Phase Wall", new Vector3(-halfWidth, 0f, 0f), true, backZ, frontZ, ceilingY);
            BuildSpatialGridWall(_containmentGridRoot, "Right Phase Wall", new Vector3(halfWidth, 0f, 0f), true, backZ, frontZ, ceilingY);
            BuildSpatialGridWall(_containmentGridRoot, "Forward Phase Wall", new Vector3(0f, 0f, frontZ), false, -halfWidth, halfWidth, ceilingY);
            BuildSpatialGridWall(_containmentGridRoot, "Rear Phase Wall", new Vector3(0f, 0f, backZ), false, -halfWidth, halfWidth, ceilingY);
            BuildContainmentRails(_containmentGridRoot, halfWidth, backZ, frontZ, ceilingY);
            BuildGridBoundaryFlares(_floorGridRoot, _ceilingGridRoot, halfWidth, backZ, frontZ, ceilingY);
        }

        private void BuildSpatialScanIntro()
        {
            _scanIntroRoot = new GameObject("Opening Spatial Survey").transform;
            _scanIntroRoot.position = (_rigTransform != null ? _rigTransform.position : Vector3.zero) + Vector3.up * 0.028f;
            _scanIntroLines.Clear();
            _scanIntroBaseAlphas.Clear();

            _scanGridRoot = new GameObject("Advancing Survey Grid").transform;
            _scanGridRoot.SetParent(_scanIntroRoot, false);
            const float halfWidth = 8.4f;
            const float halfDepth = 9.8f;
            const float spacing = 0.7f;
            for (var index = -12; index <= 12; index++)
            {
                var x = index * spacing;
                var line = CreateScanLine(_scanGridRoot, "Survey Longitudinal " + index, new Vector3(x, 0f, -halfDepth), new Vector3(x, 0f, halfDepth), index % 3 == 0 ? 0.17f : 0.055f, index % 3 == 0 ? 0.008f : 0.0026f);
                _scanIntroLines.Add(line);
            }
            for (var index = -14; index <= 14; index++)
            {
                var z = index * spacing;
                var line = CreateScanLine(_scanGridRoot, "Survey Transverse " + index, new Vector3(-halfWidth, 0f, z), new Vector3(halfWidth, 0f, z), index % 3 == 0 ? 0.17f : 0.055f, index % 3 == 0 ? 0.008f : 0.0026f);
                _scanIntroLines.Add(line);
            }

            _scanCurtainRoot = new GameObject("Survey Curtain").transform;
            _scanCurtainRoot.SetParent(_scanIntroRoot, false);
            for (var index = -10; index <= 10; index++)
            {
                var x = index / 10f * halfWidth;
                var height = 0.16f + Mathf.Abs(Mathf.Sin(index * 2.37f)) * 0.26f;
                var line = CreateScanLine(_scanCurtainRoot, "Curtain Datum " + index, new Vector3(x, 0f, 0f), new Vector3(x, height, 0f), index % 4 == 0 ? 0.13f : 0.045f, index % 4 == 0 ? 0.006f : 0.0022f);
                _scanIntroLines.Add(line);
            }
            _scanIntroLines.Add(CreateScanLine(_scanCurtainRoot, "Curtain Ground Edge", new Vector3(-halfWidth, 0f, 0f), new Vector3(halfWidth, 0f, 0f), 0.52f, 0.018f));
            _scanIntroLines.Add(CreateScanLine(_scanCurtainRoot, "Curtain Upper Trace", new Vector3(-halfWidth, 0.32f, 0f), new Vector3(halfWidth, 0.32f, 0f), 0.045f, 0.003f));

            _scanPulseRoot = new GameObject("Survey Pulse Rings").transform;
            _scanPulseRoot.SetParent(_scanIntroRoot, false);
            _scanPulseRoot.localPosition = new Vector3(0f, 0.018f, 0f);
            for (var index = 0; index < 3; index++)
            {
                var pulse = CosmicSliceMeshFactory.CreateTorusObject("Survey Pulse " + (index + 1), 0.92f + index * 0.04f, 0.0012f - index * 0.00018f, index == 1 ? _violetGlow : _cyanGlow);
                pulse.transform.SetParent(_scanPulseRoot, false);
            }

            BuildSurveyWave();
            _scanIntroRoot.gameObject.SetActive(false);
        }

        private void BuildSurveyWave()
        {
            _scanWaveRoot = new GameObject("Irregular Particle Survey Wave").transform;
            _scanWaveRoot.SetParent(_scanIntroRoot, false);

            var bandObject = new GameObject("Translucent Survey Wave Band");
            bandObject.transform.SetParent(_scanWaveRoot, false);
            var filter = bandObject.AddComponent<MeshFilter>();
            var renderer = bandObject.AddComponent<MeshRenderer>();
            _scanWaveMaterial = CreatePortalGlow("Survey Wave Band", new Color(0.1f, 0.78f, 1f), 0.18f, 0.74f);
            renderer.sharedMaterial = _scanWaveMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _scanWaveMesh = new Mesh { name = "Dynamic Irregular Survey Wave" };
            _scanWaveMesh.MarkDynamic();
            filter.sharedMesh = _scanWaveMesh;

            _scanWaveCrest = CreateSurveyWaveLine("Particle Wave Crest", 0.022f, CosmicPalette.Cyan, 0.88f);
            _scanWaveEcho = CreateSurveyWaveLine("Particle Wave Echo", 0.008f, CosmicPalette.Violet, 0.42f);

            var particleObject = new GameObject("Survey Wave Spark Particles");
            particleObject.transform.SetParent(_scanWaveRoot, false);
            _scanWaveParticles = particleObject.AddComponent<ParticleSystem>();
            var main = _scanWaveParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 520;
            main.startSpeed = 0f;
            main.startLifetime = 0.62f;
            main.startSize = 0.045f;
            main.startColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.7f);
            var emission = _scanWaveParticles.emission;
            emission.enabled = false;
            var shape = _scanWaveParticles.shape;
            shape.enabled = false;
            var particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.18f;
            particleRenderer.lengthScale = 2.2f;
            particleRenderer.maxParticleSize = 0.075f;
            particleRenderer.sharedMaterial = _cyanParticle;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            _scanWaveParticles.Play(true);
        }

        private LineRenderer CreateSurveyWaveLine(string objectName, float width, Color color, float alpha)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(_scanWaveRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 128;
            line.widthMultiplier = width;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.sharedMaterial = _cyanGlow;
            line.startColor = new Color(color.r, color.g, color.b, alpha);
            line.endColor = new Color(color.r, color.g, color.b, alpha * 0.72f);
            return line;
        }

        private LineRenderer CreateScanLine(Transform parent, string objectName, Vector3 start, Vector3 end, float alpha, float width)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.widthMultiplier = width;
            line.numCapVertices = 3;
            line.sharedMaterial = _cyanGlow;
            line.startColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, alpha);
            line.endColor = new Color(0.42f, 0.36f, 1f, alpha * 0.58f);
            _scanIntroBaseAlphas[line] = new Vector2(alpha, alpha * 0.58f);
            return line;
        }

        private void UpdateSpatialScanIntro(float progress)
        {
            if (_scanIntroRoot == null)
            {
                return;
            }

            var sweep = Mathf.SmoothStep(0.018f, 1f, Mathf.InverseLerp(0.04f, 0.64f, progress));
            _scanGridRoot.localScale = new Vector3(Mathf.Max(0.018f, sweep), 1f, Mathf.Max(0.018f, sweep));
            _scanCurtainRoot.localPosition = new Vector3(0f, 0f, Mathf.Lerp(0.35f, 13.4f, sweep));
            _scanCurtainRoot.localScale = new Vector3(Mathf.Lerp(0.16f, 1f, sweep), Mathf.Lerp(0.35f, 1f, sweep), 1f);
            var pulseScale = Mathf.Lerp(0.12f, 13.2f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.015f, 0.6f, progress)));
            _scanPulseRoot.localScale = Vector3.one * pulseScale;
            _scanPulseRoot.gameObject.SetActive(progress < 0.64f);
            UpdateSurveyWave(progress, pulseScale);

            var fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.66f, 0.92f, progress));
            foreach (var line in _scanIntroLines)
            {
                if (line == null) continue;
                var start = line.startColor;
                var end = line.endColor;
                var baseAlpha = _scanIntroBaseAlphas.TryGetValue(line, out var stored) ? stored : new Vector2(0.18f, 0.1f);
                start.a = baseAlpha.x * fade;
                end.a = baseAlpha.y * fade;
                line.startColor = start;
                line.endColor = end;
            }
        }

        private void UpdateSurveyWave(float progress, float radius)
        {
            if (_scanWaveRoot == null || _scanWaveMesh == null)
            {
                return;
            }

            if (progress <= 0.001f)
            {
                _scanParticleAccumulator = 0f;
                _lastScanVisualProgress = 0f;
                if (_scanWaveParticles != null) _scanWaveParticles.Clear(true);
            }

            var active = progress < 0.68f;
            _scanWaveRoot.gameObject.SetActive(active);
            if (!active)
            {
                _lastScanVisualProgress = progress;
                return;
            }

            const int segments = 128;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            var uvs = new Vector2[segments * 2];
            var waveFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.54f, 0.68f, progress));
            var bandWidth = Mathf.Lerp(0.22f, 0.92f, Mathf.Clamp01(radius / 5f));
            var time = Time.unscaledTime;
            for (var index = 0; index < segments; index++)
            {
                var normalized = index / (float)segments;
                var angle = normalized * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var irregularity = Mathf.Sin(angle * 7f + time * 3.2f) * 0.085f + Mathf.Sin(angle * 17f - time * 1.8f) * 0.035f;
                var crestRadius = Mathf.Max(0.05f, radius + irregularity);
                var innerRadius = Mathf.Max(0.01f, crestRadius - bandWidth);
                var crestHeight = 0.025f + (Mathf.Sin(angle * 11f + time * 4.1f) * 0.5f + 0.5f) * 0.085f;
                vertices[index * 2] = radial * innerRadius + Vector3.up * 0.008f;
                vertices[index * 2 + 1] = radial * crestRadius + Vector3.up * crestHeight;
                uvs[index * 2] = new Vector2(normalized, 0f);
                uvs[index * 2 + 1] = new Vector2(normalized, 1f);

                var next = (index + 1) % segments;
                var triangle = index * 6;
                triangles[triangle] = index * 2;
                triangles[triangle + 1] = next * 2 + 1;
                triangles[triangle + 2] = index * 2 + 1;
                triangles[triangle + 3] = index * 2;
                triangles[triangle + 4] = next * 2;
                triangles[triangle + 5] = next * 2 + 1;

                _scanWaveCrest.SetPosition(index, vertices[index * 2 + 1]);
                _scanWaveEcho.SetPosition(index, radial * Mathf.Max(0.02f, innerRadius - 0.2f) + Vector3.up * 0.014f);
            }
            _scanWaveMesh.Clear(false);
            _scanWaveMesh.vertices = vertices;
            _scanWaveMesh.triangles = triangles;
            _scanWaveMesh.uv = uvs;
            _scanWaveMesh.RecalculateBounds();

            var crestColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.88f * waveFade);
            _scanWaveCrest.startColor = crestColor;
            _scanWaveCrest.endColor = new Color(0.34f, 0.58f, 1f, 0.62f * waveFade);
            _scanWaveEcho.startColor = new Color(CosmicPalette.Violet.r, CosmicPalette.Violet.g, CosmicPalette.Violet.b, 0.36f * waveFade);
            _scanWaveEcho.endColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.18f * waveFade);
            if (_scanWaveMaterial != null && _scanWaveMaterial.HasProperty("_Color"))
            {
                _scanWaveMaterial.SetColor("_Color", new Color(0.08f, 0.68f, 1f, 0.24f * waveFade));
            }

            EmitSurveyWaveParticles(progress, radius, waveFade);
            _lastScanVisualProgress = progress;
        }

        private void EmitSurveyWaveParticles(float progress, float radius, float waveFade)
        {
            if (_scanWaveParticles == null || progress <= 0.015f || waveFade <= 0.01f)
            {
                return;
            }

            var elapsed = Mathf.Max(0f, progress - _lastScanVisualProgress) * 4.6f;
            _scanParticleAccumulator += elapsed * 260f;
            var count = Mathf.Clamp(Mathf.FloorToInt(_scanParticleAccumulator), 0, 160);
            _scanParticleAccumulator -= count;
            for (var index = 0; index < count; index++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var tangent = new Vector3(-radial.z, 0f, radial.x);
                var offsetRadius = radius + Random.Range(-0.34f, 0.22f);
                var emit = new ParticleSystem.EmitParams
                {
                    position = radial * Mathf.Max(0.04f, offsetRadius) + Vector3.up * Random.Range(0.02f, 0.22f),
                    velocity = tangent * Random.Range(-0.2f, 0.2f) + radial * Random.Range(-0.08f, 0.14f) + Vector3.up * Random.Range(0.12f, 0.52f),
                    startLifetime = Random.Range(0.36f, 0.96f),
                    startSize = Random.Range(0.026f, 0.11f),
                    startColor = Random.value > 0.18f
                        ? new Color(0.34f, 0.88f, 1f, Random.Range(0.28f, 0.72f) * waveFade)
                        : new Color(0.58f, 0.36f, 1f, Random.Range(0.18f, 0.44f) * waveFade)
                };
                _scanWaveParticles.Emit(emit, 1);
            }
        }

        private void SetScanEnvironmentVisible(bool floor, bool ceilingAndWalls, bool space, bool models, bool dust)
        {
            if (_floorGridRoot != null) _floorGridRoot.gameObject.SetActive(floor);
            if (_ceilingGridRoot != null) _ceilingGridRoot.gameObject.SetActive(ceilingAndWalls);
            if (_containmentGridRoot != null) _containmentGridRoot.gameObject.SetActive(ceilingAndWalls);
            if (_desktopSpaceBackdrop != null) _desktopSpaceBackdrop.gameObject.SetActive(space);
            if (_spaceVistaRoot != null) _spaceVistaRoot.gameObject.SetActive(space);
            if (_userSpaceModelRoot != null) _userSpaceModelRoot.gameObject.SetActive(models);
            if (_cosmicDustRoot != null) _cosmicDustRoot.gameObject.SetActive(dust);
        }

        private void BuildSpatialGridWall(Transform parent, string name, Vector3 anchor, bool alongZ, float minimum, float maximum, float height)
        {
            var wall = new GameObject(name).transform;
            wall.SetParent(parent, false);
            const int horizontalCount = 9;
            for (var index = 0; index <= horizontalCount; index++)
            {
                var y = index / (float)horizontalCount * height;
                var major = index == 0 || index == horizontalCount || index % 3 == 0;
                var start = alongZ ? new Vector3(anchor.x, y, minimum) : new Vector3(minimum, y, anchor.z);
                var end = alongZ ? new Vector3(anchor.x, y, maximum) : new Vector3(maximum, y, anchor.z);
                CreateDoubleGridLine(wall, start, end, major ? 0.052f : 0.017f, major ? 0.0048f : 0.0017f, name + (major ? " Major Double Bus" : " Faint Double Bus"), major ? new Color(0.28f, 0.72f, 1f) : new Color(0.2f, 0.48f, 0.82f));
            }

            const int verticalCount = 18;
            for (var index = 0; index <= verticalCount; index++)
            {
                var coordinate = Mathf.Lerp(minimum, maximum, index / (float)verticalCount);
                var major = index % 6 == 0;
                var start = alongZ ? new Vector3(anchor.x, 0f, coordinate) : new Vector3(coordinate, 0f, anchor.z);
                var end = alongZ ? new Vector3(anchor.x, height, coordinate) : new Vector3(coordinate, height, anchor.z);
                CreateDoubleGridLine(wall, start, end, major ? 0.048f : 0.015f, major ? 0.0045f : 0.00165f, name + (major ? " Major Double Column" : " Ghost Double Column"), major ? new Color(0.28f, 0.72f, 1f) : new Color(0.2f, 0.48f, 0.82f));
            }
        }

        private void BuildContainmentRails(Transform parent, float halfWidth, float backZ, float frontZ, float height)
        {
            var railColor = new Color(0.48f, 0.82f, 1f);
            foreach (var x in new[] { -halfWidth, halfWidth })
            {
                foreach (var z in new[] { backZ, frontZ })
                {
                    CreateGridLine(parent, new Vector3(x, 0f, z), new Vector3(x, height, z), 0.075f, 0.008f, "Containment Corner Rail", true, railColor);
                    for (var index = 1; index <= 4; index++)
                    {
                        var node = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        node.name = "Containment Telemetry Node";
                        node.transform.SetParent(parent, false);
                        node.transform.position = new Vector3(x, index / 5f * height, z);
                        node.transform.localScale = new Vector3(0.035f, 0.035f, 0.035f);
                        Destroy(node.GetComponent<Collider>());
                        node.GetComponent<Renderer>().sharedMaterial = index % 2 == 0 ? _cyanGlow : _amberGlow;
                    }
                }
            }
            CreateGridLine(parent, new Vector3(-halfWidth, height, backZ), new Vector3(halfWidth, height, backZ), 0.055f, 0.006f, "Ceiling Edge Rail", true, railColor);
            CreateGridLine(parent, new Vector3(-halfWidth, height, frontZ), new Vector3(halfWidth, height, frontZ), 0.055f, 0.006f, "Ceiling Edge Rail", true, railColor);
        }

        private void BuildGridBoundaryFlares(Transform floorRoot, Transform ceilingRoot, float halfWidth, float backZ, float frontZ, float ceilingY)
        {
            var randomState = Random.state;
            Random.InitState(4107);
            var floorCorners = new[]
            {
                new Vector3(-halfWidth, 0.012f, backZ), new Vector3(halfWidth, 0.012f, backZ),
                new Vector3(-halfWidth, 0.012f, frontZ), new Vector3(halfWidth, 0.012f, frontZ)
            };
            foreach (var corner in floorCorners)
            {
                var outward = new Vector3(Mathf.Sign(corner.x), 0f, Mathf.Sign(corner.z)).normalized;
                for (var index = 0; index < 4; index++)
                {
                    var direction = Quaternion.Euler(0f, Random.Range(-22f, 22f), 0f) * outward;
                    var length = Random.Range(0.42f, 1.05f);
                    CreateFadingGridLine(floorRoot, corner + direction * 0.06f, corner + direction * length, 0.09f + index * 0.018f, 0.0032f, "Floor Boundary Signal", true, index == 0 ? CosmicPalette.Cyan : new Color(0.42f, 0.38f, 0.98f));
                }
            }

            var ceilingCorners = new[]
            {
                new Vector3(-halfWidth, 0f, backZ - 0.55f), new Vector3(halfWidth, 0f, backZ - 0.55f),
                new Vector3(-halfWidth, 0f, frontZ - 0.55f), new Vector3(halfWidth, 0f, frontZ - 0.55f)
            };
            foreach (var corner in ceilingCorners)
            {
                var outward = new Vector3(Mathf.Sign(corner.x), 0f, Mathf.Sign(corner.z)).normalized;
                for (var index = 0; index < 4; index++)
                {
                    var direction = Quaternion.Euler(0f, Random.Range(-24f, 24f), 0f) * outward;
                    CreateFadingGridLine(ceilingRoot, corner, corner + direction * Random.Range(0.4f, 1f), 0.052f, 0.0024f, "Ceiling Boundary Signal", false, index % 2 == 0 ? new Color(0.44f, 0.34f, 0.96f) : CosmicPalette.Cyan);
                }
            }

            foreach (var x in new[] { -halfWidth, halfWidth })
            {
                foreach (var z in new[] { backZ, frontZ })
                {
                    var outward = new Vector3(Mathf.Sign(x), 0f, Mathf.Sign(z)).normalized;
                    for (var level = 1; level <= 3; level++)
                    {
                        var origin = new Vector3(x, level / 4f * ceilingY, z);
                        var direction = (outward + Vector3.up * Random.Range(-0.18f, 0.18f)).normalized;
                        CreateFadingGridLine(_containmentGridRoot, origin, origin + direction * Random.Range(0.35f, 0.82f), 0.035f, 0.0019f, "Wall Boundary Escape", true, level % 2 == 0 ? CosmicPalette.Violet : CosmicPalette.Cyan);
                    }
                }
            }
            Random.state = randomState;
        }

        private LineRenderer CreateFadingGridLine(Transform parent, Vector3 start, Vector3 end, float alpha, float width, string objectName, bool worldSpace, Color color)
        {
            var line = CreateGridLine(parent, start, end, alpha, width, objectName, worldSpace, color);
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            return line;
        }

        private void BuildDeepSpaceVista()
        {
            _spaceVistaRoot = new GameObject("Deep Space Vista").transform;
            _spaceVistaRoot.position = _rigTransform != null
                ? _rigTransform.position + new Vector3(0f, 2.3f, 0f)
                : new Vector3(0f, 2.3f, 0f);

            var starsObject = new GameObject("Parallax Star Sphere");
            starsObject.transform.SetParent(_spaceVistaRoot, false);
            var stars = starsObject.AddComponent<ParticleSystem>();
            var main = stars.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 999f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.092f);
            main.maxParticles = 540;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = stars.emission;
            emission.enabled = false;
            var starRenderer = stars.GetComponent<ParticleSystemRenderer>();
            starRenderer.sharedMaterial = _cyanParticle;
            starRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            starRenderer.maxParticleSize = 0.025f;

            var randomState = Random.state;
            Random.InitState(7519);
            for (var index = 0; index < 460; index++)
            {
                var direction = Random.onUnitSphere;
                direction.y *= 0.78f;
                if (Mathf.Abs(direction.y) < 0.08f) direction.y += direction.y < 0f ? -0.08f : 0.08f;
                direction.Normalize();
                var emit = new ParticleSystem.EmitParams
                {
                    position = _spaceVistaRoot.position + direction * Random.Range(11f, 38f),
                    startSize = Random.Range(0.014f, index % 19 == 0 ? 0.13f : 0.062f),
                    startLifetime = 999f,
                    startColor = index % 17 == 0
                        ? new Color(1f, 0.58f, 0.76f, Random.Range(0.5f, 0.82f))
                        : index % 11 == 0
                            ? new Color(1f, 0.82f, 0.48f, Random.Range(0.42f, 0.76f))
                            : new Color(0.55f, 0.82f, 1f, Random.Range(0.2f, 0.74f))
                };
                stars.Emit(emit, 1);
            }
            Random.state = randomState;
            BuildTwinklingStarLayer();

            // Planets are placed from the user's FBX library in BuildUserSpaceSetDressing.
            // This keeps procedural spheres out of the final sky composition.
            _distantBodies.Clear();
            _distantPlanet = null;
            _distantMoon = null;
            _shootingStarTimer = 0.55f;
        }

        private void BuildTwinklingStarLayer()
        {
            var twinkleObject = new GameObject("Sparse Twinkling Star Layer");
            twinkleObject.transform.SetParent(_spaceVistaRoot, false);
            var twinkles = twinkleObject.AddComponent<ParticleSystem>();
            var main = twinkles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 4.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.76f, 1f, 0.9f), new Color(0.92f, 0.55f, 1f, 0.82f));
            main.maxParticles = 74;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = twinkles.emission;
            emission.rateOverTime = 16f;
            var shape = twinkles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 27f;
            shape.radiusThickness = 0.06f;
            var color = twinkles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(Color.white, CosmicPalette.Cyan, 0.96f));
            var renderer = twinkles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _cyanParticle;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.022f;
        }

        private Transform CreatePlanet(string objectName, Vector3 position, float radius, Material surface, Material atmosphere, float ringRadius)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(_spaceVistaRoot, false);
            root.position = position;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = objectName + " Surface";
            sphere.transform.SetParent(root, false);
            sphere.transform.localScale = Vector3.one * radius * 2f;
            Destroy(sphere.GetComponent<Collider>());
            sphere.GetComponent<Renderer>().sharedMaterial = surface;

            var atmosphereShell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmosphereShell.name = objectName + " Atmosphere";
            atmosphereShell.transform.SetParent(root, false);
            atmosphereShell.transform.localScale = Vector3.one * radius * 2.035f;
            Destroy(atmosphereShell.GetComponent<Collider>());
            var atmosphereMaterial = new Material(atmosphere) { name = objectName + " Thin Atmosphere" };
            if (atmosphereMaterial.HasProperty("_Color"))
            {
                var atmosphereColor = atmosphereMaterial.GetColor("_Color");
                atmosphereColor.a = 0.075f;
                atmosphereMaterial.SetColor("_Color", atmosphereColor);
            }
            atmosphereShell.GetComponent<Renderer>().sharedMaterial = atmosphereMaterial;

            if (ringRadius > 0f)
            {
                var ring = CosmicSliceMeshFactory.CreateTorusObject(objectName + " Orbital Ring", ringRadius, 0.055f, _amberGlow);
                ring.transform.SetParent(root, false);
                ring.transform.localRotation = Quaternion.Euler(68f, 12f, 18f);
                var telemetryRing = CosmicSliceMeshFactory.CreateTorusObject(objectName + " Sensor Ring", ringRadius * 1.12f, 0.012f, _cyanGlow);
                telemetryRing.transform.SetParent(root, false);
                telemetryRing.transform.localRotation = Quaternion.Euler(68f, 12f, 18f);
            }

            var orbit = root.gameObject.AddComponent<CosmicOrbitalProp>();
            var axis = new Vector3(position.z * 0.015f, 1f, -position.x * 0.012f).normalized;
            orbit.Configure(
                _rigTransform != null ? _rigTransform : _spaceVistaRoot,
                axis,
                position.x >= 0f ? 0.11f : -0.085f,
                Vector3.up,
                Mathf.Clamp(radius * 0.035f, 0.025f, 0.13f),
                0.16f,
                new Vector3(0.08f, 1f, 0.04f),
                position.x >= 0f ? 1.15f : -0.82f,
                radius * 1.7f);
            return root;
        }

        private void AnimateSpaceVista(float deltaTime)
        {
            if (_qaMode || _spaceVistaRoot == null || !_spaceVistaRoot.gameObject.activeInHierarchy)
            {
                return;
            }

            _shootingStarTimer -= deltaTime;
            if (_shootingStarTimer <= 0f)
            {
                var burstCount = Random.value > 0.72f ? 2 : 1;
                for (var index = 0; index < burstCount; index++) SpawnShootingStar();
            _shootingStarTimer = Random.Range(2.4f, 4.8f);
            }
        }

        private void SpawnShootingStar()
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Distant Shooting Star";
            var center = (_rigTransform != null ? _rigTransform.position : Vector3.zero) + Vector3.up * 3.5f;
            var radial = Random.onUnitSphere;
            radial.y = Mathf.Abs(radial.y) * 0.42f + 0.12f;
            radial.Normalize();
            star.transform.position = center + radial * Random.Range(11f, 23f);
            star.transform.localScale = Vector3.one * Random.Range(0.024f, 0.058f);
            Destroy(star.GetComponent<Collider>());
            star.GetComponent<Renderer>().sharedMaterial = Random.value > 0.28f ? _cyanGlow : _amberGlow;
            var body = star.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            var tangent = Vector3.Cross(radial, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.right;
            if (Random.value > 0.5f) tangent = -tangent;
            body.linearVelocity = (tangent.normalized * Random.Range(4.2f, 7.2f)) + Vector3.down * Random.Range(0.48f, 1.45f);
            var trail = star.AddComponent<TrailRenderer>();
            trail.time = 0.9f;
            trail.minVertexDistance = 0.04f;
            trail.startWidth = 0.045f;
            trail.endWidth = 0f;
            trail.numCapVertices = 4;
            trail.sharedMaterial = star.GetComponent<Renderer>().sharedMaterial;
            trail.startColor = new Color(0.75f, 0.94f, 1f, 0.6f);
            trail.endColor = new Color(0.15f, 0.62f, 1f, 0f);
            var lifetime = star.AddComponent<CosmicLifetime>();
            lifetime.lifetime = 2.6f;
            lifetime.shrink = true;
        }

        private void CreateDoubleGridLine(Transform parent, Vector3 start, Vector3 end, float alpha, float width, string objectName, Color color, bool worldSpace = true)
        {
            CreateGridLine(parent, start, end, alpha, width, objectName + " / primary", worldSpace, color);
            var perpendicular = Vector3.Cross((end - start).normalized, Vector3.up);
            if (perpendicular.sqrMagnitude < 0.001f) perpendicular = Vector3.right;
            perpendicular.Normalize();
            var separation = Mathf.Clamp(width * 2.2f, 0.014f, 0.035f);
            var secondary = Color.Lerp(color, CosmicPalette.Violet, 0.34f);
            CreateGridLine(parent, start + perpendicular * separation, end + perpendicular * separation, alpha * 0.62f, width * 0.58f, objectName + " / secondary", worldSpace, secondary);
        }

        private void BuildUserSpaceSetDressing()
        {
            _userSpaceModelRoot = new GameObject("User Space Model Constellation").transform;
            _spaceSetDressingInstances.Clear();
            if (spaceSetDressingModels == null || spaceSetDressingModels.Length < 9)
            {
                Debug.LogWarning("COSMIC_SLICE_USER_SPACE_MODELS_MISSING expected=9");
                return;
            }

            // 1/7/9 are spacecraft, 2-6 are planets, and 8 is the asteroid. The composition
            // deliberately surrounds the player; only a few hero silhouettes occupy the initial view.
            SpawnCruisingSpaceProp(0, "Survey Ship Alpha", new Vector3(-8.5f, 6.2f, 19f), 3.7f, new Vector3(8f, 132f, -12f), new Vector3(0.82f, 0.04f, 0.56f), 0.075f, 10f, Vector3.up, 0.22f, 0.13f, Vector3.up, 0.24f, 1.2f);
            SpawnCruisingSpaceProp(6, "Relay Satellite Kepler", new Vector3(12f, 7.4f, 22f), 3.1f, new Vector3(-18f, 38f, 9f), new Vector3(-0.64f, 0.02f, 0.77f), 0.055f, 8f, new Vector3(0.2f, 1f, 0.1f), 0.18f, 0.17f, Vector3.up, -0.18f, 3.8f);
            SpawnCruisingSpaceProp(8, "Orbital Habitat Morrow", new Vector3(-5f, 10.8f, -25f), 4.4f, new Vector3(4f, 214f, 14f), new Vector3(0.35f, -0.02f, 0.94f), 0.038f, 7f, new Vector3(0.3f, 1f, 0.4f), 0.24f, 0.1f, Vector3.up, 0.12f, 5.7f);
            SpawnCruisingSpaceProp(0, "Survey Ship Beta", new Vector3(13f, 4.3f, -19f), 2.9f, new Vector3(-6f, 318f, -8f), new Vector3(-0.75f, 0.03f, -0.66f), 0.068f, 9f, Vector3.up, 0.26f, 0.12f, Vector3.up, -0.27f, 7.1f);
            SpawnCruisingSpaceProp(6, "Relay Satellite Perseus", new Vector3(-20f, 8.8f, -8f), 2.5f, new Vector3(12f, 112f, -16f), new Vector3(0.92f, -0.01f, 0.38f), 0.05f, 8f, new Vector3(0.3f, 0.8f, -0.1f), 0.2f, 0.15f, Vector3.up, 0.19f, 9.2f);
            SpawnCruisingSpaceProp(8, "Orbital Habitat Meridian", new Vector3(22f, 9.6f, 5f), 3.6f, new Vector3(-8f, 36f, 11f), new Vector3(-0.3f, 0.02f, -0.95f), 0.042f, 7f, new Vector3(-0.2f, 1f, 0.3f), 0.21f, 0.11f, Vector3.up, -0.14f, 11.4f);

            SpawnSpaceProp(1, "Ember Planet", new Vector3(7f, 2f, 20f), 3.8f, new Vector3(12f, 18f, -8f), new Vector3(0.16f, 1f, -0.08f), 0.032f, Vector3.up, 0.09f, 0.08f, new Vector3(0.12f, 1f, 0.08f), 0.2f, 0.4f);
            SpawnSpaceProp(2, "Ivory Gas Giant", new Vector3(-23f, 6f, -4f), 5.4f, new Vector3(4f, 72f, 14f), new Vector3(-0.1f, 1f, 0.2f), -0.018f, Vector3.up, 0.11f, 0.055f, new Vector3(0.05f, 1f, -0.03f), -0.12f, 1.3f);
            SpawnSpaceProp(3, "Blue Ice Moon", new Vector3(8f, 10.5f, -23f), 3.2f, new Vector3(-16f, 138f, 6f), new Vector3(0.28f, 0.96f, 0.04f), 0.046f, Vector3.up, 0.08f, 0.07f, new Vector3(-0.08f, 1f, 0.12f), 0.24f, 2.4f);
            SpawnSpaceProp(4, "Copper Cloud World", new Vector3(25f, 1.8f, 6f), 4.5f, new Vector3(9f, 224f, -11f), new Vector3(-0.2f, 0.98f, -0.12f), -0.025f, Vector3.up, 0.1f, 0.06f, new Vector3(0.16f, 1f, 0.04f), -0.16f, 3.1f);
            SpawnSpaceProp(5, "Terran Observation World", new Vector3(-15f, 1.2f, 23f), 4.8f, new Vector3(-3f, 286f, 18f), new Vector3(0.08f, 0.98f, 0.28f), 0.021f, Vector3.up, 0.08f, 0.05f, new Vector3(-0.04f, 1f, 0.1f), 0.13f, 4.2f);
            SpawnSpaceProp(1, "Ember Planet Night Arc", new Vector3(-9f, 8f, -30f), 2.3f, new Vector3(24f, 148f, 11f), new Vector3(-0.24f, 0.95f, 0.18f), -0.055f, Vector3.up, 0.07f, 0.07f, new Vector3(0.2f, 1f, -0.1f), -0.22f, 5.8f);
            SpawnSpaceProp(2, "Ivory Gas Giant Far Arc", new Vector3(18f, 12f, 32f), 3.2f, new Vector3(-11f, 42f, 7f), new Vector3(0.18f, 0.96f, -0.22f), 0.014f, Vector3.up, 0.06f, 0.04f, new Vector3(-0.1f, 1f, 0.08f), 0.09f, 7.5f);
            SpawnSpaceProp(3, "Blue Ice Moon Left Arc", new Vector3(-28f, 11f, 13f), 2.1f, new Vector3(18f, 308f, 12f), new Vector3(-0.3f, 0.94f, 0.14f), -0.038f, Vector3.up, 0.05f, 0.04f, new Vector3(0.1f, 1f, -0.04f), -0.1f, 9.6f);

            SpawnSpaceProp(7, "Cyan Asteroid Front", new Vector3(-4f, 4.5f, 13f), 0.9f, new Vector3(22f, 18f, 36f), new Vector3(0.34f, 1f, 0.18f), 0.12f, new Vector3(0.6f, -0.25f, 0.5f), 0.19f, 0.18f, new Vector3(0.7f, 1f, 0.35f), 1.1f, 0.7f);
            SpawnSpaceProp(7, "Cyan Asteroid Rear", new Vector3(5f, 2f, -15f), 0.72f, new Vector3(-12f, 108f, 19f), new Vector3(-0.22f, 1f, 0.3f), -0.12f, new Vector3(-0.45f, 0.32f, 0.7f), 0.16f, 0.18f, new Vector3(0.4f, 1f, -0.6f), -1.2f, 2.1f);
            SpawnSpaceProp(7, "Cyan Asteroid Right", new Vector3(17f, 7f, -2f), 0.62f, new Vector3(46f, 238f, -8f), new Vector3(0.18f, 1f, -0.26f), 0.1f, new Vector3(0.55f, 0.1f, -0.8f), 0.18f, 0.16f, new Vector3(-0.7f, 1f, 0.2f), 1.3f, 5.2f);
            SpawnSpaceProp(7, "Cyan Asteroid Left", new Vector3(-18f, 8f, 3f), 0.52f, new Vector3(12f, 286f, 27f), new Vector3(-0.13f, 1f, -0.18f), -0.08f, new Vector3(0.4f, 0.35f, -0.6f), 0.16f, 0.15f, new Vector3(0.4f, 1f, 0.2f), -1.0f, 3.9f);
            SpawnSpaceProp(7, "Cyan Asteroid Upper Rear", new Vector3(-3f, 12f, -21f), 0.44f, new Vector3(-24f, 172f, 13f), new Vector3(0.2f, 0.95f, 0.15f), 0.07f, new Vector3(0.2f, 0.6f, -0.5f), 0.13f, 0.14f, new Vector3(-0.3f, 1f, 0.4f), 0.9f, 8.7f);
        }

        private void SpawnCruisingSpaceProp(
            int modelIndex,
            string objectName,
            Vector3 relativePosition,
            float targetSize,
            Vector3 eulerRotation,
            Vector3 travelDirection,
            float travelSpeed,
            float travelSpan,
            Vector3 driftAxis,
            float driftAmplitude,
            float driftSpeed,
            Vector3 spinAxis,
            float spinSpeed,
            float phase)
        {
            var anchor = CreateSpacePropAnchor(modelIndex, objectName, relativePosition, targetSize, eulerRotation);
            if (anchor == null) return;
            var motion = anchor.gameObject.AddComponent<CosmicCruisingProp>();
            motion.Configure(travelDirection, travelSpeed, travelSpan, driftAxis, driftAmplitude, driftSpeed, spinAxis, spinSpeed, phase);
        }

        private void SpawnSpaceProp(
            int modelIndex,
            string objectName,
            Vector3 relativePosition,
            float targetSize,
            Vector3 eulerRotation,
            Vector3 orbitAxis,
            float orbitSpeed,
            Vector3 driftAxis,
            float driftAmplitude,
            float driftSpeed,
            Vector3 spinAxis,
            float spinSpeed,
            float phase)
        {
            var center = _rigTransform != null ? _rigTransform : transform;
            var anchor = CreateSpacePropAnchor(modelIndex, objectName, relativePosition, targetSize, eulerRotation);
            if (anchor == null) return;
            var motion = anchor.gameObject.AddComponent<CosmicOrbitalProp>();
            motion.Configure(center, orbitAxis, orbitSpeed, driftAxis, driftAmplitude, driftSpeed, spinAxis, spinSpeed, phase);
        }

        private Transform CreateSpacePropAnchor(int modelIndex, string objectName, Vector3 relativePosition, float targetSize, Vector3 eulerRotation)
        {
            if (modelIndex < 0 || modelIndex >= spaceSetDressingModels.Length || spaceSetDressingModels[modelIndex] == null)
            {
                return null;
            }

            var center = _rigTransform != null ? _rigTransform : transform;
            var anchor = new GameObject(objectName).transform;
            anchor.SetParent(_userSpaceModelRoot, true);
            anchor.position = center.position + relativePosition;
            anchor.rotation = Quaternion.Euler(eulerRotation);

            var model = Instantiate(spaceSetDressingModels[modelIndex], anchor, false);
            model.name = objectName + " Model";
            NormalizeImportedModel(model.transform, anchor, targetSize);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.allowOcclusionWhenDynamic = true;
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    renderer.sharedMaterial.EnableKeyword("_EMISSION");
                    renderer.sharedMaterial.SetColor("_EmissionColor", new Color(0.025f, 0.055f, 0.09f, 1f));
                }
            }
            _spaceSetDressingInstances.Add(anchor);
            return anchor;
        }

        private static void NormalizeImportedModel(Transform model, Transform anchor, float targetSize)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            var largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (largest > 0.0001f)
            {
                model.localScale *= targetSize / largest;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            model.localPosition -= anchor.InverseTransformVector(bounds.center - anchor.position);
        }

        private void BuildPortal()
        {
            _portalRoot = new GameObject("Ceiling Anomaly Portal").transform;
            _portalRoot.position = CosmicSliceDesign.ArenaOrigin + Vector3.up * CosmicSliceDesign.PortalHeightAboveArena;
            if (_ceilingGridRoot != null)
            {
                _ceilingGridRoot.position = new Vector3(_portalRoot.position.x, _portalRoot.position.y + 0.025f, _portalRoot.position.z);
            }

            var outerRing = CosmicSliceMeshFactory.CreateTorusObject("Graphite Lensing Ring", 1.48f, 0.11f, _darkMetal);
            outerRing.transform.SetParent(_portalRoot, false);

            var voidDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidDisk.name = "Animated Event Horizon";
            voidDisk.transform.SetParent(_portalRoot, false);
            voidDisk.transform.localPosition = new Vector3(0f, -0.045f, 0f);
            voidDisk.transform.localScale = new Vector3(1.42f, 0.012f, 1.42f);
            Destroy(voidDisk.GetComponent<Collider>());
            var portalMaterial = new Material(portalShader != null ? portalShader : Shader.Find("Unlit/Color"));
            portalMaterial.name = "Animated Event Horizon";
            if (portalMaterial.HasProperty("_Accent"))
            {
                portalMaterial.SetColor("_Accent", CosmicPalette.Cyan);
            }
            if (portalMaterial.HasProperty("_Hot")) portalMaterial.SetColor("_Hot", new Color(1f, 0.28f, 0.08f, 1f));
            if (portalMaterial.HasProperty("_Speed")) portalMaterial.SetFloat("_Speed", 0.68f);
            voidDisk.GetComponent<Renderer>().sharedMaterial = portalMaterial;

            var coreDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coreDisk.name = "Event Horizon Black Core";
            coreDisk.transform.SetParent(_portalRoot, false);
            coreDisk.transform.localPosition = new Vector3(0f, -0.072f, 0f);
            coreDisk.transform.localScale = new Vector3(0.54f, 0.014f, 0.54f);
            Destroy(coreDisk.GetComponent<Collider>());
            var coreShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var coreMaterial = new Material(coreShader) { name = "Absolute Black Event Horizon" };
            if (coreMaterial.HasProperty("_Color")) coreMaterial.SetColor("_Color", new Color(0.0002f, 0.0003f, 0.0006f, 1f));
            if (coreMaterial.HasProperty("_Glossiness")) coreMaterial.SetFloat("_Glossiness", 0f);
            coreDisk.GetComponent<Renderer>().sharedMaterial = coreMaterial;

            _portalSpinA = new GameObject("Cyan Telemetry Ring").transform;
            _portalSpinA.SetParent(_portalRoot, false);
            var ringA = CosmicSliceMeshFactory.CreateTorusObject("Ring", 1.17f, 0.008f, CreatePortalGlow("Cool Photon Trace", CosmicPalette.Cyan, 0.13f, 1.1f));
            ringA.transform.SetParent(_portalSpinA, false);

            _portalSpinB = new GameObject("Violet Telemetry Ring").transform;
            _portalSpinB.SetParent(_portalRoot, false);
            var ringB = CosmicSliceMeshFactory.CreateTorusObject("Ring", 0.61f, 0.016f, CreatePortalGlow("Hot Photon Ring", new Color(1f, 0.42f, 0.12f), 0.3f, 2.2f));
            ringB.transform.SetParent(_portalSpinB, false);

            var outerLens = CosmicSliceMeshFactory.CreateTorusObject("Outer Cyan Gravity Lens", 1.73f, 0.01f, CreatePortalGlow("Outer Cool Lens", CosmicPalette.Cyan, 0.08f, 0.52f));
            outerLens.transform.SetParent(_portalRoot, false);
            outerLens.transform.localScale = new Vector3(1f, 1f, 0.63f);

            _portalLight = new GameObject("Portal Light").AddComponent<Light>();
            _portalLight.transform.SetParent(_portalRoot, false);
            _portalLight.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            _portalLight.type = LightType.Point;
            _portalLight.color = new Color(0.25f, 0.48f, 1f);
            _portalLight.range = 7.2f;
            _portalLight.intensity = 2.8f;

            BuildStarfield();
        }

        private Material CreatePortalGlow(string materialName, Color color, float alpha, float pulseSpeed)
        {
            var material = new Material(additiveShader != null ? additiveShader : Shader.Find("Unlit/Color")) { name = materialName };
            color.a = alpha;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_PulseSpeed")) material.SetFloat("_PulseSpeed", pulseSpeed);
            return material;
        }

        private void AnimatePortal(float deltaTime)
        {
            if (_portalSpinA == null)
            {
                return;
            }
            _portalSpinA.Rotate(Vector3.up, 38f * deltaTime, Space.Self);
            _portalSpinB.Rotate(Vector3.up, -61f * deltaTime, Space.Self);
            var breathing = 1f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.025f + _portalPulse * 0.1f;
            if (_state != GameState.Scanning)
            {
                _portalRoot.localScale = Vector3.one * breathing;
            }
            _portalLight.intensity = 2.6f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.46f + _portalPulse * 3.4f;

            if (_flashImage != null && _flashImage.color.a > 0f)
            {
                var color = _flashImage.color;
                color.a = Mathf.MoveTowards(color.a, 0f, deltaTime * 1.9f);
                _flashImage.color = color;
            }
        }

        private void BuildBlades()
        {
            var left = new GameObject("Left Energy Blade");
            left.transform.position = new Vector3(-0.55f, 1.35f, 0.45f);
            _leftBlade = left.AddComponent<SliceBlade>();
            _leftBlade.Configure(this, HandSide.Left, _camera, _cyanGlow);

            var right = new GameObject("Right Energy Blade");
            right.transform.position = new Vector3(0.55f, 1.35f, 0.45f);
            _rightBlade = right.AddComponent<SliceBlade>();
            _rightBlade.Configure(this, HandSide.Right, _camera, _amberGlow);
        }

        private void BuildAudio()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = music;
            _musicSource.loop = true;
            _musicSource.volume = 0.1f;
            _musicSource.playOnAwake = false;
            _musicTargetVolume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _portalSource = _portalRoot.gameObject.AddComponent<AudioSource>();
            _portalSource.clip = portalLoop;
            _portalSource.loop = true;
            _portalSource.volume = 0.32f;
            _portalSource.spatialBlend = 0.82f;
            _portalSource.minDistance = 1f;
            _portalSource.maxDistance = 10f;
        }

        private void BuildFallbackTarget(Transform root, TargetKind kind)
        {
            var visual = new GameObject("Faceted Anomaly Assembly").transform;
            visual.SetParent(root, false);
            var material = _targetMaterials[kind];
            var size = kind == TargetKind.Quantum ? 0.255f : kind == TargetKind.Bomb ? 0.225f : 0.215f;
            var ringMaterial = kind switch
            {
                TargetKind.Plasma => _cyanGlow,
                TargetKind.Meteor => _amberGlow,
                TargetKind.Quantum => _violetGlow,
                _ => _redGlow
            };

            // The targets read as constructed anomalies rather than a single primitive: an
            // irregular shell, an exposed energy heart and small orbiting fracture plates.
            var shell = CosmicSliceMeshFactory.CreateOctahedronObject("Faceted Anomaly Shell", size, material);
            shell.name = "Faceted Anomaly Shell";
            shell.transform.SetParent(visual, false);
            shell.transform.localScale = kind == TargetKind.Meteor
                ? new Vector3(0.82f, 1.42f, 0.76f)
                : kind == TargetKind.Bomb ? new Vector3(1.08f, 0.82f, 1.02f) : Vector3.one;
            shell.transform.localRotation = Quaternion.Euler(18f, 32f, 12f);

            var core = CosmicSliceMeshFactory.CreateOctahedronObject("Exposed Energy Heart", size * 0.49f, ringMaterial);
            core.transform.SetParent(visual, false);
            core.transform.localRotation = Quaternion.Euler(-12f, 48f, 28f);

            var ring = CosmicSliceMeshFactory.CreateTorusObject("Primary Containment Orbit", size * 1.16f, 0.011f, ringMaterial);
            ring.transform.SetParent(visual, false);
            ring.transform.localRotation = Quaternion.Euler(68f, 18f, 26f);

            var innerRing = CosmicSliceMeshFactory.CreateTorusObject("Secondary Containment Orbit", size * 0.78f, 0.008f, ringMaterial);
            innerRing.transform.SetParent(visual, false);
            innerRing.transform.localRotation = Quaternion.Euler(-25f, 38f, 72f);

            var sensorRing = CosmicSliceMeshFactory.CreateTorusObject("Offset Sensor Orbit", size * 1.38f, 0.0055f, ringMaterial);
            sensorRing.transform.SetParent(visual, false);
            sensorRing.transform.localRotation = Quaternion.Euler(22f, -62f, 18f);

            var shardOffsets = new[]
            {
                new Vector3(-0.58f, 0.27f, 0.24f),
                new Vector3(0.54f, 0.17f, -0.33f),
                new Vector3(-0.19f, -0.57f, -0.26f),
                new Vector3(0.25f, 0.52f, 0.42f),
                new Vector3(0.63f, -0.24f, 0.17f)
            };
            for (var index = 0; index < shardOffsets.Length; index++)
            {
                var shard = CosmicSliceMeshFactory.CreateOctahedronObject("Orbiting Fracture Plate " + (index + 1), size * (0.18f + (index % 2) * 0.035f), material);
                shard.transform.SetParent(visual, false);
                shard.transform.localPosition = shardOffsets[index] * size * 1.42f;
                shard.transform.localScale = new Vector3(0.62f, 1.5f, 0.42f);
                shard.transform.localRotation = Quaternion.Euler(27f + index * 37f, 18f + index * 61f, 42f + index * 23f);
            }

            BuildTargetEnergyTail(visual, kind);

            if (kind == TargetKind.Plasma || kind == TargetKind.Meteor)
            {
                var handMark = CreateRuleMark(kind == TargetKind.Plasma ? "LEFT" : "RIGHT", ringMaterial, size * 1.35f);
                handMark.transform.SetParent(visual, false);
                handMark.transform.localPosition = new Vector3(0f, -size * 1.25f, 0f);
            }

            if (kind == TargetKind.Quantum)
            {
                BuildQuantumDualLink(visual, size);
            }

            if (kind == TargetKind.Bomb)
            {
                var warningRing = CosmicSliceMeshFactory.CreateTorusObject("Singularity Warning Ring", size * 1.34f, 0.025f, _redGlow);
                warningRing.transform.SetParent(visual, false);
                warningRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                for (var index = 0; index < 8; index++)
                {
                    var spike = CosmicSliceMeshFactory.CreateOctahedronObject("Singularity Spike", 0.07f, _redGlow);
                    spike.name = "Singularity Spike";
                    spike.transform.SetParent(visual, false);
                    var angle = index * Mathf.PI * 2f / 8f;
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    spike.transform.localPosition = direction * size * 1.5f;
                    spike.transform.localScale = new Vector3(0.5f, 0.5f, 1.9f);
                    spike.transform.localRotation = Quaternion.LookRotation(direction);
                    Destroy(spike.GetComponent<Collider>());
                    spike.GetComponent<Renderer>().sharedMaterial = _redGlow;
                }
            }
        }

        private void BuildQuantumDualLink(Transform visual, float size)
        {
            // The paired violet cores and cyan/amber rails make the two-controller rule
            // recognizable before the player reads the tutorial text.
            var dualRoot = new GameObject("Dual Blade Link Assembly").transform;
            dualRoot.SetParent(visual, false);
            var leftCore = CosmicSliceMeshFactory.CreateOctahedronObject("Left Link Core", size * 0.42f, _cyanGlow);
            leftCore.transform.SetParent(dualRoot, false);
            leftCore.transform.localPosition = new Vector3(-size * 0.53f, 0f, 0f);
            leftCore.transform.localScale = new Vector3(0.72f, 1.05f, 0.72f);
            leftCore.transform.localRotation = Quaternion.Euler(16f, 38f, 22f);

            var rightCore = CosmicSliceMeshFactory.CreateOctahedronObject("Right Link Core", size * 0.42f, _amberGlow);
            rightCore.transform.SetParent(dualRoot, false);
            rightCore.transform.localPosition = new Vector3(size * 0.53f, 0f, 0f);
            rightCore.transform.localScale = new Vector3(0.72f, 1.05f, 0.72f);
            rightCore.transform.localRotation = Quaternion.Euler(-18f, -38f, 22f);

            var leftRail = CreateRuleMark("LEFT", _cyanGlow, size * 1.22f);
            leftRail.name = "Cyan First Blade Rail";
            leftRail.transform.SetParent(dualRoot, false);
            leftRail.transform.localPosition = new Vector3(-size * 0.15f, -size * 1.22f, 0f);

            var rightRail = CreateRuleMark("RIGHT", _amberGlow, size * 1.22f);
            rightRail.name = "Amber Second Blade Rail";
            rightRail.transform.SetParent(dualRoot, false);
            rightRail.transform.localPosition = new Vector3(size * 0.15f, -size * 1.22f, 0f);

            var bridge = new GameObject("Dual Link Energy Bridge");
            bridge.transform.SetParent(dualRoot, false);
            var line = bridge.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.widthMultiplier = 0.014f;
            line.numCapVertices = 4;
            line.sharedMaterial = _violetGlow;
            line.startColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.92f);
            line.endColor = new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.92f);
            line.SetPosition(0, new Vector3(-size * 0.54f, 0f, 0f));
            line.SetPosition(1, new Vector3(0f, size * 0.18f, -size * 0.12f));
            line.SetPosition(2, new Vector3(size * 0.54f, 0f, 0f));

            var dualOrbit = CosmicSliceMeshFactory.CreateTorusObject("Dual Link Orbit", size * 1.48f, 0.008f, _violetGlow);
            dualOrbit.transform.SetParent(dualRoot, false);
            dualOrbit.transform.localRotation = Quaternion.Euler(84f, 0f, 0f);
        }

        private void BuildTargetEnergyTail(Transform targetVisual, TargetKind kind)
        {
            var tailObject = new GameObject("Anomaly Particle Wake");
            tailObject.transform.SetParent(targetVisual, false);
            var particles = tailObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, kind == TargetKind.Bomb ? 0.032f : 0.022f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, CosmicPalette.For(kind));
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = kind == TargetKind.Bomb ? 0.23f : 0.16f;
            shape.radiusThickness = 0.28f;
            var emission = particles.emission;
            emission.rateOverTime = kind == TargetKind.Bomb ? 20f : 13f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(
                Color.white, CosmicPalette.For(kind), kind == TargetKind.Bomb ? 0.62f : 0.42f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleFor(CosmicPalette.For(kind));
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 1.8f;
        }

        private GameObject CreateRuleMark(string name, Material material, float size)
        {
            var mark = new GameObject(name + " Hand Rule");
            var line = mark.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.widthMultiplier = 0.018f;
            line.numCapVertices = 4;
            line.positionCount = 3;
            var sign = name == "LEFT" ? -1f : 1f;
            line.SetPosition(0, new Vector3(sign * size * 0.42f, 0f, 0f));
            line.SetPosition(1, new Vector3(-sign * size * 0.15f, 0f, 0f));
            line.SetPosition(2, new Vector3(-sign * size * 0.15f, 0f, size * 0.22f));
            return mark;
        }

        private void SpawnBurst(Vector3 position, Color color, Vector3 direction, bool large, int particleCount = 22)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.right;
            }
            direction.Normalize();

            var slash = new GameObject("Energy Cut Trace");
            slash.transform.position = position;
            var line = slash.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, position - direction * (large ? 0.72f : 0.44f));
            line.SetPosition(1, position + direction * (large ? 0.72f : 0.44f));
            line.widthMultiplier = large ? 0.13f : 0.075f;
            line.numCapVertices = 6;
            line.sharedMaterial = GlowFor(color);
            line.startColor = Color.white;
            line.endColor = color;
            var slashLifetime = slash.AddComponent<CosmicLifetime>();
            slashLifetime.lifetime = large ? 0.55f : 0.32f;
            slashLifetime.fadeLine = true;

            CreateImpactRing(position, color, direction, large);

            var fragments = large ? 9 : 5;
            for (var index = 0; index < fragments; index++)
            {
                var fragment = GameObject.CreatePrimitive(index % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                fragment.name = "Reusable Cut Fragment";
                fragment.transform.position = position + Random.insideUnitSphere * 0.12f;
                fragment.transform.localScale = Vector3.one * Random.Range(0.055f, large ? 0.15f : 0.1f);
                Destroy(fragment.GetComponent<Collider>());
                fragment.GetComponent<Renderer>().sharedMaterial = FragmentFor(color);
                var body = fragment.AddComponent<Rigidbody>();
                body.useGravity = true;
                body.linearVelocity = Random.insideUnitSphere * (large ? 3.6f : 2.2f) + direction * Random.Range(-0.8f, 0.8f);
                body.angularVelocity = Random.insideUnitSphere * 8f;
                var fragmentTrail = fragment.AddComponent<TrailRenderer>();
                fragmentTrail.time = large ? 0.34f : 0.22f;
                fragmentTrail.minVertexDistance = 0.025f;
                fragmentTrail.startWidth = large ? 0.045f : 0.026f;
                fragmentTrail.endWidth = 0f;
                fragmentTrail.numCapVertices = 3;
                fragmentTrail.sharedMaterial = GlowFor(color);
                fragmentTrail.startColor = new Color(color.r, color.g, color.b, 0.72f);
                fragmentTrail.endColor = new Color(color.r, color.g, color.b, 0f);
                var lifetime = fragment.AddComponent<CosmicLifetime>();
                lifetime.lifetime = large ? 1.15f : 0.72f;
                lifetime.shrink = true;
            }

            var particlesObject = new GameObject("Pooled Energy Particles");
            particlesObject.transform.position = position;
            var particles = particlesObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = large ? 0.8f : 0.52f;
            main.startSpeed = large ? 3.5f : 2.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, large ? 0.09f : 0.055f);
            main.startColor = Color.white;
            main.maxParticles = 48;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = large ? 0.18f : 0.08f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(Color.white, color, 0.92f));
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = large ? 0.42f : 0.2f;
            noise.frequency = 1.8f;
            noise.scrollSpeed = 0.7f;
            var emission = particles.emission;
            emission.enabled = false;
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = ParticleFor(color);
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.08f;
            particleRenderer.lengthScale = large ? 2.8f : 1.8f;
            particles.Emit(large ? particleCount + 12 : particleCount);
            var particleLifetime = particlesObject.AddComponent<CosmicLifetime>();
            particleLifetime.lifetime = large ? 1.3f : 0.9f;
        }

        private void Flash(Color color, float alpha)
        {
            if (_flashImage == null)
            {
                return;
            }
            color.a = alpha;
            _flashImage.color = color;
        }

        private void BuildStarfield()
        {
            var accretion = new GameObject("Orbiting Accretion Dust");
            accretion.transform.SetParent(_portalRoot, false);
            accretion.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            accretion.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var particles = accretion.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 3.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.1f, 4.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.018f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.048f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.28f, 0.76f, 1f, 0.34f), new Color(1f, 0.48f, 0.67f, 0.68f));
            main.maxParticles = 320;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.43f;
            shape.radiusThickness = 0.38f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = 0f;
            velocity.orbitalY = 0f;
            velocity.orbitalZ = 1.08f;
            velocity.radial = -0.075f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(
                new Color(0.58f, 0.88f, 1f), new Color(1f, 0.38f, 0.62f), 0.64f));
            var emission = particles.emission;
            emission.rateOverTime = 82f;
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.65f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _cyanParticle;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.04f;
            renderer.lengthScale = 2.4f;

            var rain = new GameObject("Falling Starlight Filaments");
            rain.transform.SetParent(_portalRoot, false);
            rain.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            rain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rainParticles = rain.AddComponent<ParticleSystem>();
            var rainMain = rainParticles.main;
            rainMain.loop = true;
            rainMain.playOnAwake = true;
            rainMain.duration = 2.8f;
            rainMain.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
            rainMain.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.72f);
            rainMain.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.022f);
            rainMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.18f, 0.74f, 1f, 0.25f), new Color(1f, 0.78f, 0.88f, 0.58f));
            rainMain.maxParticles = 80;
            rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var rainShape = rainParticles.shape;
            rainShape.shapeType = ParticleSystemShapeType.Circle;
            rainShape.radius = 1.08f;
            rainShape.radiusThickness = 0.9f;
            var rainEmission = rainParticles.emission;
            rainEmission.rateOverTime = 24f;
            var rainColor = rainParticles.colorOverLifetime;
            rainColor.enabled = true;
            rainColor.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(Color.white, CosmicPalette.Cyan, 0.52f));
            var rainRenderer = rainParticles.GetComponent<ParticleSystemRenderer>();
            rainRenderer.sharedMaterial = _cyanParticle;
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.velocityScale = 0.06f;
            rainRenderer.lengthScale = 4.2f;
        }

        private void BuildRoomScaleCosmicDust()
        {
            var dustRoot = new GameObject("Room Scale Cosmic Dust");
            _cosmicDustRoot = dustRoot.transform;
            if (_rigTransform != null)
            {
                dustRoot.transform.SetParent(_rigTransform, false);
            }
            dustRoot.transform.localPosition = new Vector3(0f, 1.7f, 1.15f);
            var particles = dustRoot.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.008f, 0.035f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.018f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.36f, 0.72f, 1f, 0.12f), new Color(1f, 0.72f, 0.82f, 0.26f));
            main.maxParticles = 165;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(11.5f, 4.2f, 12.2f);
            var emission = particles.emission;
            emission.rateOverTime = 12f;
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.045f;
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.08f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateParticleGradient(Color.white, CosmicPalette.Cyan, 0.22f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _cyanParticle;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.035f;
        }

        private void CreateImpactRing(Vector3 position, Color color, Vector3 direction, bool large)
        {
            var ring = new GameObject("Spatial Cut Shock Ring");
            ring.transform.position = position;
            ring.transform.rotation = _camera != null
                ? Quaternion.LookRotation(_camera.transform.position - position, direction)
                : Quaternion.LookRotation(Vector3.forward, direction);
            var line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = large ? 0.035f : 0.018f;
            line.numCornerVertices = 4;
            line.sharedMaterial = GlowFor(color);
            line.startColor = Color.white;
            line.endColor = new Color(color.r, color.g, color.b, 0.82f);
            var radius = large ? 0.11f : 0.065f;
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = index / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
            var lifetime = ring.AddComponent<CosmicLifetime>();
            lifetime.lifetime = large ? 0.62f : 0.38f;
            lifetime.expand = true;
            lifetime.expansion = large ? 7.5f : 5.2f;
            lifetime.fadeLine = true;
            lifetime.spin = new Vector3(0f, 0f, large ? 85f : -110f);
        }

        private static Gradient CreateParticleGradient(Color start, Color end, float peakAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(Color.Lerp(start, end, 0.45f), 0.38f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, 0.18f),
                    new GradientAlphaKey(peakAlpha * 0.72f, 0.68f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private LineRenderer CreateGridLine(Transform parent, Vector3 start, Vector3 end, float alpha, float width, string objectName, bool worldSpace = true, Color? lineColor = null)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = worldSpace;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.sharedMaterial = _cyanGlow;
            var color = lineColor ?? CosmicPalette.Cyan;
            line.startColor = new Color(color.r, color.g, color.b, alpha);
            line.endColor = line.startColor;
            return line;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            Object.Destroy(box.GetComponent<Collider>());
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private Material MakeGlowMaterial(string name, Color color)
        {
            var shader = additiveShader != null ? additiveShader : Shader.Find("Unlit/Color");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            return material;
        }

        private Material MakeStandardMaterial(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            var shader = standardShader != null ? standardShader : Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private Material MakeTransparentMaterial(string name, Color color, float metallic, float smoothness)
        {
            var material = MakeStandardMaterial(name, color, metallic, smoothness);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private Material MakeParticleMaterial(string name, Color color)
        {
            var shader = particleShader != null ? particleShader : Shader.Find("CosmicSlice/SoftParticleGlow");
            var material = new Material(shader != null ? shader : additiveShader) { name = name };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            return material;
        }

        private Material GlowFor(Color color)
        {
            if (color == CosmicPalette.Cyan)
            {
                return _cyanGlow;
            }
            if (color == CosmicPalette.Amber)
            {
                return _amberGlow;
            }
            if (color == CosmicPalette.Violet)
            {
                return _violetGlow;
            }
            return _redGlow;
        }

        private Material FragmentFor(Color color)
        {
            if (color == CosmicPalette.Cyan)
            {
                return _targetMaterials[TargetKind.Plasma];
            }
            if (color == CosmicPalette.Amber)
            {
                return _targetMaterials[TargetKind.Meteor];
            }
            if (color == CosmicPalette.Violet)
            {
                return _targetMaterials[TargetKind.Quantum];
            }
            return _targetMaterials[TargetKind.Bomb];
        }

        private Material ParticleFor(Color color)
        {
            if (color == CosmicPalette.Cyan)
            {
                return _cyanParticle;
            }
            if (color == CosmicPalette.Amber)
            {
                return _amberParticle;
            }
            if (color == CosmicPalette.Violet)
            {
                return _violetParticle;
            }
            return _redParticle;
        }
    }
}
