using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CosmicSlice
{
    public sealed class SliceBlade : MonoBehaviour
    {
        private const float ControllerRayDistance = 8f;
        private readonly HashSet<FallingTarget> _hitThisFrame = new();
        private readonly Collider[] _sliceHits = new Collider[32];
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];
        private CosmicSliceGame _game;
        private HandSide _side;
        private Camera _camera;
        private LineRenderer _trail;
        private LineRenderer _coreTrail;
        private readonly Vector3[] _trailPoints = new Vector3[12];
        private int _trailPointCount;
        private Vector3 _previousPosition;
        private Vector3 _velocity;
        private Vector3 _desktopInteractionPosition;
        private Vector3 _previousInteractionPosition;
        private Vector3 _interactionVelocity;
        private bool _hasPrevious;
        private bool _hasPreviousInteraction;
        private bool _wasTracked;
        private UnityEngine.XR.InputDevice _device;
        private Material _trailMaterial;
        private Material _whiteCoreMaterial;
        private Transform _bladeAura;
        private Transform _emitter;
        private Transform _toolVisualRoot;
        private LineRenderer _controllerRay;
        private Transform _pointerReticle;
        private Material _pointerReticleMaterial;
        private bool _xrTriggerHeld;
        private bool _xrTriggerWasHeldForFeedback;
        private FallingTarget _rayTargetWhileHeld;
        private float _desktopInputGraceUntil;
        private Vector2 _previousMousePosition;
        private bool _hasPreviousMousePosition;

        public void Configure(CosmicSliceGame game, HandSide side, Camera camera, Material material)
        {
            _game = game;
            _side = side;
            _camera = camera;
            _trailMaterial = material;
            name = side + " Energy Blade";
            BuildVisual();
        }

        private void BuildVisual()
        {
            var color = _side == HandSide.Left ? CosmicPalette.Cyan : CosmicPalette.Amber;
            var sideSign = _side == HandSide.Left ? -1f : 1f;
            _whiteCoreMaterial = new Material(_trailMaterial) { name = name + " White Blade Core" };
            if (_whiteCoreMaterial.HasProperty("_Color"))
            {
                _whiteCoreMaterial.SetColor("_Color", new Color(0.84f, 0.94f, 1f, 1f));
            }

            // The tracked root remains the exact collision/ray origin.  Presentation lives on a
            // separate root so it can be held in a safe lower peripheral zone when a simulated
            // controller begins too close to the player's face.
            _toolVisualRoot = new GameObject(_side + " Peripheral Blade Visual").transform;
            _toolVisualRoot.SetParent(transform, false);
            _toolVisualRoot.localScale = Vector3.one * 0.72f;

            _bladeAura = new GameObject("Energy Blade Field").transform;
            _bladeAura.SetParent(_toolVisualRoot, false);
            var aura = CreateBladeCylinder("Blade Aura", _bladeAura, new Vector3(0f, 0f, 0.18f), new Vector3(0.022f, 0.18f, 0.022f), _trailMaterial);
            aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var core = CreateBladeCylinder("White Blade Edge", _bladeAura, new Vector3(0f, 0f, 0.18f), new Vector3(0.008f, 0.182f, 0.008f), _whiteCoreMaterial);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Blade Tip";
            tip.transform.SetParent(_bladeAura, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.37f);
            tip.transform.localScale = Vector3.one * 0.026f;
            Destroy(tip.GetComponent<Collider>());
            tip.GetComponent<Renderer>().sharedMaterial = _whiteCoreMaterial;

            var hiltMaterial = new Material(Shader.Find("Standard"))
            {
                name = name + " Graphite Hilt",
                color = new Color(0.045f, 0.052f, 0.065f)
            };
            if (hiltMaterial.HasProperty("_Metallic")) hiltMaterial.SetFloat("_Metallic", 0.9f);
            if (hiltMaterial.HasProperty("_Glossiness")) hiltMaterial.SetFloat("_Glossiness", 0.82f);
            var hilt = CreateBladeCylinder("Graphite Hilt", _toolVisualRoot, new Vector3(0f, 0f, -0.09f), new Vector3(0.028f, 0.075f, 0.028f), hiltMaterial);
            hilt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // A full forearm/glove is intentionally omitted in XR.  It cannot improve the
            // slicing interaction, but can dominate the center of view in the emulator or when
            // a player brings a controller close to the headset.
            var guard = CosmicSliceMeshFactory.CreateTorusObject("Blade Guard", 0.036f, 0.005f, _trailMaterial);
            guard.transform.SetParent(_toolVisualRoot, false);
            guard.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _emitter = CosmicSliceMeshFactory.CreateTorusObject("Blade Emitter", 0.025f, 0.004f, _whiteCoreMaterial).transform;
            _emitter.SetParent(_toolVisualRoot, false);
            _emitter.localPosition = new Vector3(0f, 0f, -0.005f);
            _emitter.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _trail = gameObject.AddComponent<LineRenderer>();
            _trail.useWorldSpace = true;
            _trail.positionCount = _trailPoints.Length;
            _trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.008f), new Keyframe(0.72f, 0.055f), new Keyframe(1f, 0.085f));
            _trail.numCapVertices = 6;
            _trail.numCornerVertices = 4;
            _trail.sharedMaterial = _trailMaterial;
            _trail.startColor = new Color(color.r, color.g, color.b, 0.04f);
            _trail.endColor = new Color(color.r, color.g, color.b, 0.82f);

            var coreTrailObject = new GameObject("White Core Motion Trail");
            coreTrailObject.transform.SetParent(transform, false);
            _coreTrail = coreTrailObject.AddComponent<LineRenderer>();
            _coreTrail.useWorldSpace = true;
            _coreTrail.positionCount = _trailPoints.Length;
            _coreTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.002f), new Keyframe(1f, 0.022f));
            _coreTrail.numCapVertices = 6;
            _coreTrail.numCornerVertices = 4;
            _coreTrail.sharedMaterial = _whiteCoreMaterial;
            _coreTrail.startColor = new Color(1f, 1f, 1f, 0f);
            _coreTrail.endColor = new Color(1f, 1f, 1f, 0.92f);

            var controllerRayObject = new GameObject(name + " World Interaction Ray");
            controllerRayObject.transform.SetParent(transform, false);
            _controllerRay = controllerRayObject.AddComponent<LineRenderer>();
            _controllerRay.name = name + " World Interaction Ray";
            _controllerRay.useWorldSpace = true;
            _controllerRay.positionCount = 2;
            _controllerRay.widthCurve = new AnimationCurve(new Keyframe(0f, 0.005f), new Keyframe(1f, 0.0015f));
            _controllerRay.numCapVertices = 3;
            _controllerRay.sharedMaterial = _trailMaterial;
            _controllerRay.startColor = new Color(color.r, color.g, color.b, 0.7f);
            _controllerRay.endColor = new Color(color.r, color.g, color.b, 0.08f);

            _pointerReticleMaterial = new Material(_trailMaterial) { name = name + " Pointer Reticle" };
            _pointerReticle = new GameObject(name + " World Aim Reticle").transform;
            _pointerReticle.SetParent(transform, false);
            var reticleCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleCore.name = "Aim Point";
            reticleCore.transform.SetParent(_pointerReticle, false);
            reticleCore.transform.localScale = Vector3.one * 0.036f;
            Destroy(reticleCore.GetComponent<Collider>());
            reticleCore.GetComponent<Renderer>().sharedMaterial = _pointerReticleMaterial;
            var reticleRing = CosmicSliceMeshFactory.CreateTorusObject("Aim Ring", 0.052f, 0.006f, _pointerReticleMaterial);
            reticleRing.transform.SetParent(_pointerReticle, false);
            reticleRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _pointerReticle.gameObject.SetActive(false);

            // Set an immediately comfortable visual pose before the first XR input update.
            _toolVisualRoot.localPosition = new Vector3(sideSign * 0.12f, -0.18f, 0.32f);
            _toolVisualRoot.localRotation = Quaternion.Euler(-12f, sideSign * 24f, sideSign * 6f);
        }

        private void BuildFirstPersonGlove(Color color, Material hiltMaterial)
        {
            var gloveRoot = new GameObject(_side + " First Person Flight Glove").transform;
            gloveRoot.SetParent(transform, false);

            var gloveMaterial = new Material(hiltMaterial)
            {
                name = _side + " Flight Glove Graphite",
                color = new Color(0.065f, 0.085f, 0.125f, 1f)
            };
            if (gloveMaterial.HasProperty("_Metallic")) gloveMaterial.SetFloat("_Metallic", 0.58f);
            if (gloveMaterial.HasProperty("_Glossiness")) gloveMaterial.SetFloat("_Glossiness", 0.72f);
            if (gloveMaterial.HasProperty("_EmissionColor"))
            {
                gloveMaterial.EnableKeyword("_EMISSION");
                gloveMaterial.SetColor("_EmissionColor", color * 0.085f);
            }

            var forearm = CreateGlovePrimitive(PrimitiveType.Capsule, "Armored Forearm", gloveRoot, new Vector3(0f, -0.022f, -0.355f), new Vector3(0.095f, 0.115f, 0.095f), Quaternion.Euler(90f, 0f, 0f), gloveMaterial);
            forearm.transform.localScale = new Vector3(0.095f, 0.115f, 0.095f);
            var cuff = CreateBladeCylinder("Illuminated Wrist Cuff", gloveRoot, new Vector3(0f, -0.008f, -0.265f), new Vector3(0.092f, 0.045f, 0.092f), gloveMaterial);
            cuff.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var cuffLight = CosmicSliceMeshFactory.CreateTorusObject("Wrist Signal Ring", 0.087f, 0.006f, _trailMaterial);
            cuffLight.transform.SetParent(gloveRoot, false);
            cuffLight.transform.localPosition = new Vector3(0f, -0.008f, -0.225f);
            cuffLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreateGlovePrimitive(PrimitiveType.Sphere, "Gloved Palm", gloveRoot, new Vector3(0f, -0.006f, -0.145f), new Vector3(0.105f, 0.082f, 0.125f), Quaternion.identity, gloveMaterial);
            for (var index = 0; index < 4; index++)
            {
                var fingerX = -0.057f + index * 0.038f;
                CreateGlovePrimitive(
                    PrimitiveType.Capsule,
                    "Grip Finger " + (index + 1),
                    gloveRoot,
                    new Vector3(fingerX, -0.025f, -0.115f),
                    new Vector3(0.024f, 0.062f, 0.024f),
                    Quaternion.Euler(90f, 0f, 0f),
                    gloveMaterial);
            }

            var sideSign = _side == HandSide.Left ? -1f : 1f;
            CreateGlovePrimitive(
                PrimitiveType.Capsule,
                "Wrapped Thumb",
                gloveRoot,
                new Vector3(sideSign * 0.083f, 0.003f, -0.105f),
                new Vector3(0.032f, 0.067f, 0.032f),
                Quaternion.Euler(64f, 0f, sideSign * 38f),
                gloveMaterial);

            var backPlate = CreateGlovePrimitive(
                PrimitiveType.Cube,
                "Luminous Hand Plate",
                gloveRoot,
                new Vector3(0f, 0.073f, -0.155f),
                new Vector3(0.105f, 0.012f, 0.112f),
                Quaternion.identity,
                _trailMaterial);
            backPlate.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);

            var signalNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signalNode.name = "Glove Signal Node";
            signalNode.transform.SetParent(gloveRoot, false);
            signalNode.transform.localPosition = new Vector3(sideSign * 0.045f, 0.084f, -0.14f);
            signalNode.transform.localScale = Vector3.one * 0.024f;
            Destroy(signalNode.GetComponent<Collider>());
            signalNode.GetComponent<Renderer>().sharedMaterial = _whiteCoreMaterial;
        }

        private static GameObject CreateGlovePrimitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            primitive.transform.localRotation = rotation;
            Destroy(primitive.GetComponent<Collider>());
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static GameObject CreateBladeCylinder(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = objectName;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = scale;
            Destroy(cylinder.GetComponent<Collider>());
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }

        private void Update()
        {
            // The PICO emulator can keep a controller pose alive while its host window is
            // receiving mouse input.  Let an intentional mouse gesture take ownership for a
            // moment, otherwise the valid controller pose short-circuits desktop slicing.
            var desktopMouseMoved = WasDesktopMouseMoved();
            var desktopInputPreferred = IsDesktopInputPreferred(desktopMouseMoved);
            var tracked = !desktopInputPreferred && TryUpdateFromXR();
            // On a desktop player with no XR device, keep the virtual blade projected onto the
            // target plane even before the first gesture.  This preserves its resting pose and
            // lets automated desktop QA verify the same interaction plane.
            var desktopPoseUpdated = !tracked && UpdateFromDesktop(desktopMouseMoved);
            var desktopSliceActive = desktopInputPreferred && desktopPoseUpdated;
            var active = tracked || desktopSliceActive;
            var visualCurrent = tracked ? transform.TransformPoint(0f, 0f, 0.48f) : transform.position;
            var interactionCurrent = tracked ? visualCurrent : _desktopInteractionPosition;
            var trackingChanged = tracked != _wasTracked;
            var discontinuousMove = _hasPrevious && Vector3.Distance(visualCurrent, _previousPosition) > 0.7f;

            if (!_hasPrevious || trackingChanged || discontinuousMove)
            {
                _previousPosition = visualCurrent;
                _hasPrevious = true;
                _trailPointCount = 0;
            }
            if (!_hasPreviousInteraction || trackingChanged || discontinuousMove)
            {
                _previousInteractionPosition = interactionCurrent;
                _hasPreviousInteraction = true;
            }

            _velocity = (visualCurrent - _previousPosition) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            _interactionVelocity = (interactionCurrent - _previousInteractionPosition) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            UpdateComfortVisual(tracked);
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 7.5f + (_side == HandSide.Left ? 0f : 1.7f)) * 0.035f;
            _bladeAura.localScale = new Vector3(pulse, pulse, 1f);
            _emitter.localScale = Vector3.one * (1f + (_velocity.magnitude > 0.6f ? 0.12f : 0.03f) * Mathf.Sin(Time.unscaledTime * 12f));
            UpdateTrail(visualCurrent, active);
            UpdateControllerRay(tracked);
            DetectRaySlice(tracked);
            if (active && _interactionVelocity.magnitude > (tracked ? 0.48f : 0.5f))
            {
                DetectSlices(_previousInteractionPosition, interactionCurrent);
            }

            _previousPosition = visualCurrent;
            _previousInteractionPosition = interactionCurrent;
            _wasTracked = tracked;
        }

        private bool WasDesktopMouseMoved()
        {
            if (Mouse.current == null)
            {
                return false;
            }

            var current = Mouse.current.position.ReadValue();
            var moved = _hasPreviousMousePosition &&
                        (current - _previousMousePosition).sqrMagnitude > 0.25f;
            _previousMousePosition = current;
            _hasPreviousMousePosition = true;
            return moved;
        }

        private bool IsDesktopInputPreferred(bool mouseMoved)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            var buttonHeld = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
            if (mouseMoved || buttonHeld)
            {
                // Keep one coherent virtual blade stroke even if the mouse momentarily stops
                // between host-window input events.
                _desktopInputGraceUntil = Time.unscaledTime + 0.18f;
            }

            return buttonHeld || Time.unscaledTime < _desktopInputGraceUntil;
        }

        /// <summary>
        /// Keeps the decorative blade outside the central comfort zone without changing the
        /// tracked transform used by slicing, controller rays or haptics.  The clamp only takes
        /// effect while the physical/simulated controller is close to, or directly in front of,
        /// the HMD.
        /// </summary>
        private void UpdateComfortVisual(bool tracked)
        {
            if (_toolVisualRoot == null || _camera == null)
            {
                return;
            }

            var sideSign = _side == HandSide.Left ? -1f : 1f;
            var visualPosition = transform.TransformPoint(new Vector3(sideSign * 0.08f, -0.11f, 0.22f));
            if (tracked)
            {
                var cameraSpace = _camera.transform.InverseTransformPoint(visualPosition);
                var blocksCenter = cameraSpace.z < 0.58f ||
                                   (Mathf.Abs(cameraSpace.x) < 0.24f && cameraSpace.y > -0.34f && cameraSpace.z < 1.05f);
                if (blocksCenter)
                {
                    cameraSpace.x = sideSign * Mathf.Max(0.31f, Mathf.Abs(cameraSpace.x));
                    cameraSpace.y = Mathf.Min(cameraSpace.y, -0.34f);
                    cameraSpace.z = Mathf.Max(cameraSpace.z, 0.70f);
                    visualPosition = _camera.transform.TransformPoint(cameraSpace);
                }
            }

            _toolVisualRoot.SetPositionAndRotation(
                visualPosition,
                transform.rotation * Quaternion.Euler(-12f, sideSign * 24f, sideSign * 6f));
        }

        private void UpdateControllerRay(bool tracked)
        {
            if (_controllerRay == null)
            {
                return;
            }
            var origin = tracked ? transform.TransformPoint(0f, 0f, 0.08f) : transform.position;
            var direction = tracked ? transform.forward : _camera.transform.forward;
            _controllerRay.enabled = tracked;
            if (_pointerReticle != null)
            {
                _pointerReticle.gameObject.SetActive(tracked);
            }
            if (!tracked)
            {
                _xrTriggerWasHeldForFeedback = false;
                return;
            }

            var length = ControllerRayDistance;
            FallingTarget aimedTarget = null;
            var aimedStageButton = false;
            if (FindRayTarget(origin, direction, length, out var targetHit))
            {
                length = targetHit.distance;
                aimedTarget = targetHit.collider.GetComponentInParent<FallingTarget>();
            }
            else if (Physics.Raycast(origin, direction, out var hit, length, ~0, QueryTriggerInteraction.Collide))
            {
                length = hit.distance;
                var stageOption = hit.collider.GetComponent<CosmicWorldStageOption>();
                aimedStageButton = stageOption != null && _game.IsStageButtonAvailable(stageOption.Index);
            }

            var sideColor = _side == HandSide.Left ? CosmicPalette.Cyan : CosmicPalette.Amber;
            var feedbackColor = aimedStageButton ? Color.white : sideColor;
            if (aimedTarget != null)
            {
                feedbackColor = !aimedTarget.IsCompatibleWith(_side)
                    ? CosmicPalette.Red
                    : Color.white;
            }

            var heldBoost = _xrTriggerHeld ? 1f : 0f;
            _controllerRay.widthMultiplier = Mathf.Lerp(1f, 1.7f, heldBoost);
            _controllerRay.startColor = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, _xrTriggerHeld ? 0.98f : 0.72f);
            _controllerRay.endColor = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, aimedTarget != null || aimedStageButton ? 0.92f : 0.22f);
            _controllerRay.SetPosition(0, origin);
            _controllerRay.SetPosition(1, origin + direction * length);

            if (_pointerReticle != null)
            {
                _pointerReticle.position = origin + direction * length;
                _pointerReticle.rotation = Quaternion.LookRotation(_camera.transform.forward, _camera.transform.up);
                var targetScale = aimedTarget != null || aimedStageButton ? 1.25f : 0.72f;
                if (_xrTriggerHeld) targetScale *= 0.72f;
                targetScale *= 1f + Mathf.Sin(Time.unscaledTime * 10f) * (aimedTarget != null ? 0.08f : 0.025f);
                _pointerReticle.localScale = Vector3.one * targetScale;
                if (_pointerReticleMaterial != null && _pointerReticleMaterial.HasProperty("_Color"))
                {
                    _pointerReticleMaterial.SetColor("_Color", feedbackColor);
                }
            }

            if (_xrTriggerHeld && !_xrTriggerWasHeldForFeedback)
            {
                Haptic(aimedTarget != null ? 0.24f : 0.08f, 0.025f);
            }
            _xrTriggerWasHeldForFeedback = _xrTriggerHeld;
        }

        private void DetectRaySlice(bool tracked)
        {
            if (!tracked)
            {
                _xrTriggerHeld = false;
                _rayTargetWhileHeld = null;
                return;
            }

            if (!_xrTriggerHeld)
            {
                _rayTargetWhileHeld = null;
                return;
            }

            var origin = transform.TransformPoint(0f, 0f, 0.08f);
            var direction = transform.forward;
            if (!FindRayTarget(origin, direction, ControllerRayDistance, out var hit))
            {
                return;
            }

            var target = hit.collider.GetComponentInParent<FallingTarget>();
            if (target == null || target == _rayTargetWhileHeld)
            {
                return;
            }

            _rayTargetWhileHeld = target;
            var directionVector = _interactionVelocity.sqrMagnitude > 0.001f
                ? _interactionVelocity.normalized
                : direction;
            _game.HandleBladeHit(target, _side, directionVector, hit.point);
        }

        private bool FindRayTarget(Vector3 origin, Vector3 direction, float length, out RaycastHit targetHit)
        {
            targetHit = default;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _rayHits,
                length,
                ~0,
                QueryTriggerInteraction.Collide);
            var nearestDistance = float.PositiveInfinity;
            var found = false;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = _rayHits[index];
                var target = hit.collider != null ? hit.collider.GetComponentInParent<FallingTarget>() : null;
                if (target == null || target.Resolved || !target.CanReceiveHit() || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                targetHit = hit;
                found = true;
            }

            return found;
        }

        private bool TryUpdateFromXR()
        {
            if (!_device.isValid)
            {
                _device = InputDevices.GetDeviceAtXRNode(_side == HandSide.Left ? XRNode.LeftHand : XRNode.RightHand);
            }

            if (!_device.isValid || !_device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position))
            {
                _xrTriggerHeld = false;
                return false;
            }

            _device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
            _device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out _xrTriggerHeld);
            transform.SetPositionAndRotation(_game.RigTransform.TransformPoint(position), _game.RigTransform.rotation * rotation);
            return true;
        }

        private bool UpdateFromDesktop(bool mouseMoved)
        {
            if (Mouse.current == null || _camera == null)
            {
                return false;
            }

            var mouse = Mouse.current.position.ReadValue();
            var viewport = _camera.ScreenToViewportPoint(mouse);
            var sideAnchor = _side == HandSide.Left ? 0.31f : 0.69f;
            viewport.x = _game.IsQaMode
                ? sideAnchor
                : Mathf.Lerp(Mathf.Clamp(viewport.x, 0.14f, 0.86f), sideAnchor, 0.46f);
            if (_game.IsQaMode) viewport.y = 0.31f;
            viewport.y = Mathf.Clamp(viewport.y, 0.28f, 0.82f);
            var ray = _camera.ViewportPointToRay(viewport);
            var visualPlaneCenter = _camera.transform.position + _camera.transform.forward * 1.72f;
            var visualPlane = new Plane(_camera.transform.forward, visualPlaneCenter);
            if (visualPlane.Raycast(ray, out var visualDistance))
            {
                var sideSign = _side == HandSide.Left ? -1f : 1f;
                var target = ray.GetPoint(visualDistance) + _camera.transform.right * sideSign * 0.17f - _camera.transform.up * 0.13f;
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-28f * Time.unscaledDeltaTime));
                var upright = Vector3.Slerp(_camera.transform.up, _velocity.sqrMagnitude > 0.08f ? _velocity.normalized : _camera.transform.up, 0.28f);
                var targetRotation = Quaternion.LookRotation(_camera.transform.forward, upright) * Quaternion.Euler(0f, sideSign * 4f, sideSign * 2f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
            }

            var slicePlane = new Plane(_camera.transform.forward, _game.SlicePlaneCenter);
            if (slicePlane.Raycast(ray, out var sliceDistance))
            {
                _desktopInteractionPosition = ray.GetPoint(sliceDistance);
            }

            var primaryMouseSwing = _side == HandSide.Left && mouseMoved;
            return primaryMouseSwing ||
                   (_side == HandSide.Left
                       ? Mouse.current.leftButton.isPressed
                       : Mouse.current.rightButton.isPressed);
        }

        private void UpdateTrail(Vector3 current, bool active)
        {
            var color = _side == HandSide.Left ? CosmicPalette.Cyan : CosmicPalette.Amber;
            if (_trailPointCount == 0)
            {
                for (var index = 0; index < _trailPoints.Length; index++)
                {
                    _trailPoints[index] = current;
                }
                _trailPointCount = _trailPoints.Length;
            }
            else
            {
                for (var index = 0; index < _trailPoints.Length - 1; index++)
                {
                    _trailPoints[index] = _trailPoints[index + 1];
                }
                _trailPoints[^1] = current;
            }

            _trail.SetPositions(_trailPoints);
            _coreTrail.SetPositions(_trailPoints);
            var intensity = active ? Mathf.Clamp01(_velocity.magnitude / 1.8f) : 0.08f;
            _trail.endColor = new Color(color.r, color.g, color.b, Mathf.Lerp(0.24f, 0.98f, intensity));
            _coreTrail.endColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.16f, 0.96f, intensity));
        }

        private void DetectSlices(Vector3 start, Vector3 end)
        {
            _hitThisFrame.Clear();
            var hitCount = Physics.OverlapCapsuleNonAlloc(
                start, end, 0.13f, _sliceHits, ~0, QueryTriggerInteraction.Collide);
            for (var index = 0; index < hitCount; index++)
            {
                var collider = _sliceHits[index];
                var target = collider.GetComponentInParent<FallingTarget>();
                if (target == null || !_hitThisFrame.Add(target) || !target.CanReceiveHit())
                {
                    continue;
                }

                target.RegisterHit();
                var direction = _interactionVelocity.sqrMagnitude > 0.001f
                    ? _interactionVelocity.normalized
                    : (end - start).normalized;
                _game.HandleBladeHit(target, _side, direction, collider.ClosestPoint(end));
            }
        }

        internal bool IsDesktopSlicePlaneAligned(Vector3 planeCenter, Vector3 planeNormal)
        {
            return Mathf.Abs(Vector3.Dot(_desktopInteractionPosition - planeCenter, planeNormal.normalized)) < 0.035f;
        }

        internal void QaSlice(Vector3 start, Vector3 end)
        {
            if (!_game.IsQaMode)
            {
                return;
            }
            _interactionVelocity = (end - start) / 0.016f;
            DetectSlices(start, end);
        }

        internal void QaRaySlice(Vector3 origin, Vector3 direction)
        {
            if (!_game.IsQaMode || !FindRayTarget(origin, direction.normalized, ControllerRayDistance, out var hit))
            {
                return;
            }

            var target = hit.collider.GetComponentInParent<FallingTarget>();
            if (target != null)
            {
                _game.HandleBladeHit(target, _side, direction.normalized, hit.point);
            }
        }

        internal bool HasWorldPointerFeedback()
        {
            return _controllerRay != null && _pointerReticle != null &&
                   _pointerReticle.Find("Aim Point") != null && _pointerReticle.Find("Aim Ring") != null;
        }

        public void Haptic(float amplitude, float duration)
        {
            if (_device.isValid)
            {
                _device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
