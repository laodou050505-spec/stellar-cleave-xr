using System;
using System.IO;
using System.Linq;
using ByteDance.PICO.XR;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace CosmicSlice.Editor
{
    public static class CosmicSliceProjectBuilder
    {
        private const string ScenePath = "Assets/CosmicSlice/Scenes/CosmicSliceDemo.unity";
        private const string LoaderType = "ByteDance.PICO.XR.PXR_Loader";

        [MenuItem("Cosmic Slice/Regenerate Complete Demo")]
        public static void RegenerateCompleteDemo()
        {
            ReimportAngeModels();
            ConfigureProject();
            ConfigurePico();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("COSMIC_SLICE_DEMO_READY");
        }

        private static void ReimportAngeModels()
        {
            var folder = Path.Combine(Application.dataPath, "CosmicSlice", "Art", "Models");
            foreach (var file in Directory.GetFiles(folder, "*.glb"))
            {
                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        [MenuItem("Cosmic Slice/Build/macOS Demo")]
        public static void BuildMac()
        {
            RegenerateCompleteDemo();
            var output = WorkspaceOutput("CosmicSlice-macOS", "Cosmic Slice.app");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Build(ScenePath, output, BuildTarget.StandaloneOSX);
        }

        [MenuItem("Cosmic Slice/Build/PICO Android APK")]
        public static void BuildPicoApk()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            RegenerateCompleteDemo();
            var output = WorkspaceOutput("CosmicSlice-PICO", "CosmicSlice-PICO.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Build(ScenePath, output, BuildTarget.Android);
        }

        public static void BatchRegenerate()
        {
            RegenerateCompleteDemo();
        }

        public static void BatchBuildMac()
        {
            BuildMac();
        }

        public static void BatchBuildAndroid()
        {
            BuildPicoApk();
        }

        [MenuItem("Cosmic Slice/Validate PICO Baseline")]
        public static void BatchValidate()
        {
            RegenerateCompleteDemo();
            var inputHandler = ReadInputHandler();
            var androidXr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            var hasPico = androidXr != null && androidXr.AssignedSettings != null &&
                          androidXr.AssignedSettings.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == LoaderType);
            var pxr = LoadPicoSettingsAsset();
            var multiview = pxr != null && pxr.stereoRenderingModeAndroid == PXR_Settings.StereoRenderingModeAndroid.Multiview;
            var valid = inputHandler == 1 && hasPico && multiview &&
                        PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64 &&
                        PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP &&
                        PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.Activity;
            Debug.Log($"COSMIC_SLICE_VALIDATION inputSystemOnly={inputHandler == 1} picoLoader={hasPico} multiview={multiview} arm64={PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64} il2cpp={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP} activity={PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.Activity}");
            if (!valid)
            {
                throw new BuildFailedException("Cosmic Slice PICO baseline validation failed. See the preceding validation line.");
            }
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Anomaly Lab";
            PlayerSettings.productName = "Stellar Cleave - Anomaly Protocol";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.anomalylab.cosmicslice");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground = false;
            PlayerSettings.MTRendering = true;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.pixelLightCount = 3;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.globalTextureMipmapLimit = 0;
            Physics.defaultContactOffset = 0.01f;
            Physics.sleepThreshold = 0.005f;
            Physics.defaultSolverIterations = 8;
            SetInputHandler(1);
        }

        private static void ConfigurePico()
        {
            var settingsStore = LoadOrCreateXrSettingsStore();
            if (!settingsStore.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                settingsStore.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            }
            if (!settingsStore.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                settingsStore.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            var manager = settingsStore.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!XRPackageMetadataStore.AssignLoader(manager, LoaderType, BuildTargetGroup.Android) &&
                !manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == LoaderType))
            {
                throw new InvalidOperationException("Could not assign the official PICO XR loader.");
            }

            var pxrSettings = LoadPicoSettingsAsset();
            pxrSettings.stereoRenderingModeAndroid = PXR_Settings.StereoRenderingModeAndroid.Multiview;
            pxrSettings.systemDisplayFrequency = PXR_Settings.SystemDisplayFrequency.Default;
            pxrSettings.optimizeBufferDiscards = true;
            EditorUtility.SetDirty(pxrSettings);
            EditorBuildSettings.AddConfigObject("ByteDance.PICO.XR.Settings", pxrSettings, true);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settingsStore, true);

            const string projectSettingsPath = "Assets/Resources/PXR_ProjectSetting.asset";
            var projectSettings = AssetDatabase.LoadAssetAtPath<PXR_ProjectSetting>(projectSettingsPath);
            if (projectSettings == null)
            {
                projectSettings = ScriptableObject.CreateInstance<PXR_ProjectSetting>();
                AssetDatabase.CreateAsset(projectSettings, projectSettingsPath);
            }
            projectSettings.videoSeeThrough = true;
            projectSettings.sceneCapture = true;
            projectSettings.spatialMesh = true;
            projectSettings.planeDetection = true;
            projectSettings.mrSafeguard = true;
            projectSettings.meshLod = PxrMeshLod.Low;
            projectSettings.handTracking = false;
            projectSettings.isSpatialAdapter = false;
            EditorUtility.SetDirty(projectSettings);
            EditorUtility.SetDirty(settingsStore);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        private static XRGeneralSettingsPerBuildTarget LoadOrCreateXrSettingsStore()
        {
            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            const string path = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "XR", "Settings"));
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static PXR_Settings LoadPicoSettingsAsset()
        {
            var configured = PXR_Settings.GetSettings();
            if (configured != null)
            {
                return configured;
            }

            var guids = AssetDatabase.FindAssets("t:PXR_Settings");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<PXR_Settings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            const string path = "Assets/XR/Settings/PXR_Settings.asset";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "XR", "Settings"));
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<PXR_Settings>();
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CosmicSliceDemo";

            var xrOriginObject = new GameObject("XR Origin - Floor");
            // This scene pose is consumed again by CosmicSliceDesign at runtime. Desktop and XR
            // therefore inherit the authored title start before START transitions to gameplay.
            xrOriginObject.transform.SetPositionAndRotation(
                CosmicSliceDesign.TitlePlayerRootPosition,
                Quaternion.Euler(0f, CosmicSliceDesign.TitlePlayerYaw, 0f));
            var xrOrigin = xrOriginObject.AddComponent<XROrigin>();
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            xrOrigin.CameraYOffset = 0f;
            xrOriginObject.AddComponent<PXR_Manager>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(xrOriginObject.transform, false);
            xrOrigin.CameraFloorOffsetObject = offset;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, CosmicSliceDesign.TitleEyeHeight, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            var trackedPose = cameraObject.AddComponent<TrackedPoseDriver>();
            trackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPose.positionInput = new InputActionProperty(new InputAction("Head Position", binding: "<XRHMD>/centerEyePosition"));
            trackedPose.rotationInput = new InputActionProperty(new InputAction("Head Rotation", binding: "<XRHMD>/centerEyeRotation"));
            xrOrigin.Camera = camera;

            var runtime = new GameObject("Cosmic Slice - Complete Runtime");
            runtime.AddComponent<PicoDesktopRuntimeGate>();
            var game = runtime.AddComponent<CosmicSliceGame>();
            runtime.AddComponent<PicoCeilingLocator>();
            game.hudBackdrop = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CosmicSlice/Art/UI/AnomalyHUD_Spatial.png");
            game.spacePanorama = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CosmicSlice/Art/Environment/01_generated_image_url.png");
            game.spaceSetDressingModels = new[]
            {
                LoadSpaceModel("1/tripo_convert_7108f415-a864-4b34-ade0-80f0a7ae1b1b.fbx"),
                LoadSpaceModel("2/tripo_convert_b9983a86-47a8-48ac-bb44-f70ef79f5f24.fbx"),
                LoadSpaceModel("3/tripo_convert_17d02fa5-3a8b-404e-86f0-2a91fdb1d827.fbx"),
                LoadSpaceModel("4/tripo_convert_918519de-629b-4121-9857-b0e5ad5945ec.fbx"),
                LoadSpaceModel("5/tripo_convert_4f99b539-4d7b-4a67-a745-1517ac801736.fbx"),
                LoadSpaceModel("6/tripo_convert_4916fd49-3f0e-4452-814b-acb67f58f3e5.fbx"),
                LoadSpaceModel("7/tripo_convert_93c20735-cffb-4e55-a1d6-f07468a7be1e.fbx"),
                LoadSpaceModel("8/tripo_convert_e7e6fa18-39a9-40d9-90ed-c1eae992bb57.fbx"),
                LoadSpaceModel("9/tripo_convert_fa976724-2c28-4e99-8edf-1d7493ef19c3.fbx")
            };
            game.additiveShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicSlice/Shaders/AdditiveGlow.shader");
            game.particleShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicSlice/Shaders/SoftParticleGlow.shader");
            game.portalShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicSlice/Shaders/PortalVoid.shader");
            game.spaceBackdropShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicSlice/Shaders/InsideSpacePanorama.shader");
            game.standardShader = Shader.Find("Standard");
            game.music = LoadAudio("MidnightPulse.mp3");
            game.portalLoop = LoadAudio("PortalLoop.mp3");
            game.bladeWhoosh = LoadAudio("BladeWhoosh.mp3");
            game.positive = LoadAudio("Positive.mp3");
            game.negative = LoadAudio("Negative.mp3");
            game.explosion = LoadAudio("Explosion.mp3");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = runtime;
        }

        private static AudioClip LoadAudio(string file)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CosmicSlice/Art/Audio/" + file);
        }

        private static GameObject LoadSpaceModel(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CosmicSlice/Art/UserSpaceModels/" + relativePath);
        }

        private static void Build(string scene, string output, BuildTarget target)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = target,
                options = BuildOptions.CleanBuildCache
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }
            Debug.Log($"COSMIC_SLICE_BUILD_OK target={target} output={output} size={report.summary.totalSize}");
        }

        private static string WorkspaceOutput(string folder, string file)
        {
            var project = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var workspace = Directory.GetParent(project)?.FullName ?? project;
            return Path.Combine(workspace, "outputs", folder, file);
        }

        private static void SetInputHandler(int value)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0)
            {
                throw new InvalidOperationException("Could not open ProjectSettings.asset to set Input System-only mode.");
            }
            var serialized = new SerializedObject(assets[0]);
            var property = serialized.FindProperty("activeInputHandler");
            if (property == null)
            {
                throw new InvalidOperationException("Unity's activeInputHandler project setting was not found.");
            }
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int ReadInputHandler()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0)
            {
                return -1;
            }
            var serialized = new SerializedObject(assets[0]);
            return serialized.FindProperty("activeInputHandler")?.intValue ?? -1;
        }
    }
}
