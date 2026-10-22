using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CosmicSlice
{
    // THESIS: Restore the preferred emerald CRT nebula and slender coral slash identity.
    // OWN-WORLD: Pine-black phosphor, generated STELLAR CLEAVE lettering, precise live controls.
    // STORY: Start a round or learn the four target rules, then enter the real-room MR stage.
    // FIRST VIEWPORT: Coral title and diagonal slash above live Start / How to Play / Exit controls.
    // FORM: Continuation of the user's Nano Banana cover; guide inherits the same material.
    // FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and DESIGN.md
    public sealed partial class CosmicSliceGame
    {
        private RectTransform _coverRoot;
        private RectTransform _coverStartHitArea;
        private RectTransform _coverExitHitArea;
        private CanvasGroup _coverGroup;
        private Text _coverStatusText;
        private Text _coverPromptText;
        private RectTransform _coverHomePage;
        private RectTransform _coverGuidePage;
        private RawImage _coverArtwork;
        private RawImage _guideArtwork;
        private readonly MenuControl[] _menuControls = new MenuControl[5];
        private int _coverSelection;
        private int _coverHovered = -1;
        private int _coverInputHand = -1;
        private int _coverPendingAction = -1;
        private float _coverActionDelay;
        private float _coverTransitionTimer;
        private float _coverPresentationStableTime;
        private bool _coverTransitioning;
        private bool _coverExitRequested;
        private bool _coverGuideVisible;
        private readonly LineRenderer[] _coverControllerRays = new LineRenderer[2];
        private readonly Transform[] _coverControllerReticles = new Transform[2];
        private readonly Material[] _coverPointerMaterials = new Material[2];
        private readonly bool[] _coverPointerTriggerHeld = new bool[2];
        private readonly bool[] _coverPointerTriggerArmed = new bool[2];
        private static readonly Color MenuInk = new(0.012f, 0.033f, 0.030f, 1f);
        private static readonly Color MenuPaper = new(0.87f, 0.97f, 0.93f, 1f);
        private static readonly Color MenuAccent = new(1f, 0.34f, 0.23f, 1f);

        private sealed class MenuControl
        {
            public RectTransform Rect;
            public CosmicPanelGraphic Fill;
            public Outline Outline;
            public Text Label;
            public Image Marker;
            public float Press;
        }

        private void EnterCover()
        {
            _state = GameState.Cover;
            _coverSelection = 0;
            _coverPendingAction = -1;
            _coverTransitionTimer = 0f;
            _coverTransitioning = false;
            _coverExitRequested = false;
            ClearTargets();
            Time.timeScale = 1f;
            _musicTargetVolume = 0.08f;
            SetTutorialVisible(false);
            SetCoreHudVisible(false);
            SetGameplayStageVisible(false);
            if (_resultGroup != null)
            {
                _resultGroup.alpha = 0f;
                _resultGroup.blocksRaycasts = false;
            }
            SetCoverVisible(true);
            SetCoverPage(false);
            UpdateCoverVisuals(1f);
        }

        private void UpdateCover(float deltaTime)
        {
            if (_coverGroup == null) return;
            UpdateCoverVisuals(deltaTime);
            if (AdvanceCoverAction(deltaTime)) return;
            UpdateWorldMenuRay();
            if (_coverPendingAction >= 0) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (_coverGuideVisible && keyboard.escapeKey.wasPressedThisFrame)
            {
                SelectCoverOption(3, false);
                ActivateCoverSelection();
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame ||
                     keyboard.sKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
            {
                MoveCoverSelection(keyboard.shiftKey.isPressed ? -1 : 1);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                MoveCoverSelection(-1);
            }
            else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                _coverInputHand = -1;
                ActivateCoverSelection();
            }
        }

        private void MoveCoverSelection(int direction)
        {
            var order = _coverGuideVisible ? new[] { 3, 4 } : new[] { 0, 2, 1 };
            var current = System.Array.IndexOf(order, _coverSelection);
            _coverHovered = -1;
            _coverInputHand = -1;
            SelectCoverOption(order[(current + direction + order.Length) % order.Length], true);
        }

        private bool AdvanceCoverAction(float deltaTime)
        {
            if (_coverPendingAction >= 0)
            {
                _coverActionDelay -= deltaTime;
                if (_coverActionDelay > 0f) return true;
                var action = _coverPendingAction;
                _coverPendingAction = -1;
                if (action == 2 || action == 3)
                {
                    SetCoverPage(action == 2);
                    return true;
                }
                _coverExitRequested = action == 1;
                _coverStatusText.text = _coverExitRequested ? "EXIT REQUEST ACKNOWLEDGED" : "CONNECTING TO YOUR ROOM";
                _coverPromptText.text = _coverExitRequested ? "ENDING LOCAL SESSION" : "YOUR OPENING SURVEY WILL BEGIN SHORTLY";
                _coverTransitioning = true;
                _coverPresentationStableTime = 0f;
                _coverTransitionTimer = _coverExitRequested ? 0.35f : 0.38f;
                Debug.Log(_coverExitRequested ? "COSMIC_MENU_EXIT_REQUEST" : "COSMIC_MENU_START_REQUEST");
                return true;
            }
            if (!_coverTransitioning) return false;
            if (!_coverExitRequested && !_qaMode && Application.platform == RuntimePlatform.Android)
            {
                if (!CanPresentOpening() || deltaTime > OpeningMaximumFrameGap)
                {
                    _coverPresentationStableTime = 0f;
                    _coverTransitionTimer = 0.38f;
                    _coverGroup.alpha = 1f;
                    return true;
                }
                _coverPresentationStableTime += Mathf.Min(deltaTime, OpeningMaximumStep);
                if (_coverPresentationStableTime < OpeningSettleDuration) return true;
                // Reveal the room behind the fading cover, instead of fading to opaque black.
                var clear = _camera.backgroundColor;
                clear.a = 0f;
                _camera.backgroundColor = clear;
            }
            _coverTransitionTimer -= deltaTime;
            var duration = _coverExitRequested ? 0.35f : 0.38f;
            var progress = Mathf.Clamp01(1f - _coverTransitionTimer / duration);
            _coverGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            if (_coverTransitionTimer <= 0f)
            {
                if (_coverExitRequested) QuitApplication();
                else
                {
                    _coverTransitioning = false;
                    SetCoverVisible(false);
                    EnterScanning(roomPrepared: true);
                }
            }
            return true;
        }

        private bool IsCoverOptionAvailable(int selection)
        {
            return selection >= 0 && selection < _menuControls.Length &&
                   _menuControls[selection] != null && _menuControls[selection].Rect != null &&
                   _menuControls[selection].Rect.gameObject.activeInHierarchy;
        }

        private void ActivateCoverSelection()
        {
            if (_coverTransitioning || _coverPendingAction >= 0 || !IsCoverOptionAvailable(_coverSelection)) return;
            _menuControls[_coverSelection].Press = 0.18f;
            _coverPendingAction = _coverSelection;
            _coverActionDelay = 0.16f;
            if (_coverSelection == 0 || (_coverGuideVisible && _coverSelection == 4))
            {
                ResetDetectedCeilingToDesignPose();
                GetComponent<PicoCeilingLocator>()?.PrepareGameplayTransition();
            }
            PlaySfx(positive, 0.45f, _coverSelection == 1 ? 0.8f : 1.1f);
            if (_coverInputHand >= 0)
            {
                var node = _coverInputHand == 0 ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid) device.SendHapticImpulse(0, 0.30f, 0.045f);
            }
            Debug.Log($"COSMIC_MENU_PRESS option={_coverSelection} hand={_coverInputHand}");
        }

        private void QuitApplication()
        {
            Debug.Log("COSMIC_MENU_EXIT_COMPLETE");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void SelectCoverOption(int selection, bool audible)
        {
            if (!IsCoverOptionAvailable(selection)) return;
            var changed = _coverSelection != selection;
            _coverSelection = selection;
            if (_coverStatusText != null && !_coverTransitioning && _coverPendingAction < 0)
            {
                _coverStatusText.text = selection switch
                {
                    0 => "READY",
                    1 => "EXIT TO DESKTOP",
                    2 => "FOUR SIGNALS. TWO BLADES. ONE PROTOCOL.",
                    3 => "RETURN TO MAIN MENU",
                    _ => "READY TO CONTAIN THE ANOMALY"
                };
#if UNITY_ANDROID && !UNITY_EDITOR
                if (selection == 1) _coverStatusText.text = "EXIT TO PICO HOME";
#endif
            }
            if (changed && audible) PlaySfx(positive, 0.16f, 1.35f);
        }

        private void SetCoverPage(bool guide)
        {
            _coverGuideVisible = guide;
            _coverHomePage.gameObject.SetActive(!guide);
            _coverGuidePage.gameObject.SetActive(guide);
            _coverStatusText.gameObject.SetActive(!guide);
            _coverHovered = -1;
            _coverInputHand = -1;
            SelectCoverOption(guide ? 3 : 0, false);
#if UNITY_ANDROID && !UNITY_EDITOR
            _coverPromptText.text = "POINT + TRIGGER TO SELECT";
#else
            _coverPromptText.text = guide ? "POINT + TRIGGER TO SELECT   /   ESC TO RETURN" : "POINT + TRIGGER   /   MOUSE CLICK   /   ARROWS + ENTER";
#endif
            Debug.Log(guide ? "COSMIC_MENU_GUIDE_OPEN" : "COSMIC_MENU_HOME_OPEN");
        }

        private void UpdateCoverVisuals(float deltaTime)
        {
            var blend = 1f - Mathf.Exp(-20f * deltaTime);
            for (var i = 0; i < _menuControls.Length; i++)
            {
                var button = _menuControls[i];
                if (button == null || button.Rect == null) continue;
                var pressed = button.Press > 0f;
                button.Press = Mathf.Max(0f, button.Press - deltaTime);
                var selected = _coverSelection == i;
                var hovered = _coverHovered == i;
                var fill = selected ? MenuAccent : MenuInk;
                if (hovered) fill = Color.Lerp(MenuAccent, Color.white, 0.18f);
                if (pressed) fill = new Color(1f, 0.83f, 0.53f, 1f);
                button.Fill.color = Color.Lerp(button.Fill.color, fill, blend);
                // Match text to the rendered fill, including the press-to-idle fade.
                // A selection change can happen before a bright press color has faded.
                var drawnFill = button.Fill.color;
                var luminance = 0.2126f * Mathf.GammaToLinearSpace(drawnFill.r) +
                                0.7152f * Mathf.GammaToLinearSpace(drawnFill.g) +
                                0.0722f * Mathf.GammaToLinearSpace(drawnFill.b);
                button.Label.color = luminance > 0.16f ? MenuInk : MenuPaper;
                button.Outline.effectColor = hovered ? Color.white : selected ? MenuAccent : new Color(0.21f, 0.57f, 0.47f, 1f);
                button.Marker.color = selected ? MenuInk : new Color(0.31f, 0.79f, 0.64f, 0f);
                var scale = pressed ? 0.94f : hovered ? 1.055f : selected ? 1.015f : 1f;
                button.Rect.localScale = Vector3.Lerp(button.Rect.localScale, Vector3.one * scale, blend);
            }
        }

        private void UpdateWorldMenuRay()
        {
            if (_camera == null || _coverGroup == null || _coverGroup.alpha < 0.9f)
            {
                HideCoverControllerPointers();
                return;
            }
            var mouse = Mouse.current;
            var mouseActive = mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.delta.ReadValue().sqrMagnitude > 0.01f);
            if (mouseActive)
            {
                HideCoverControllerPointers();
                _coverInputHand = -1;
                UpdateCoverOptionFromRay(_camera.ScreenPointToRay(mouse.position.ReadValue()), mouse.leftButton.wasPressedThisFrame);
                return;
            }
            var nodes = new[] { UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.XRNode.RightHand };
            var tracked = 0;
            var hoverOption = -1;
            var hoverHand = -1;
            for (var i = 0; i < 2; i++)
            {
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(nodes[i]);
                if (!device.isValid || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position) ||
                    (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var isTracked) && !isTracked))
                {
                    HideCoverControllerPointer(i);
                    continue;
                }
                tracked++;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var held);
                var ray = new Ray(_rigTransform.TransformPoint(position), _rigTransform.rotation * rotation * Vector3.forward);
                var hit = Physics.Raycast(ray, out var info, 8f, ~0, QueryTriggerInteraction.Collide);
                var option = hit ? info.collider.GetComponent<CosmicWorldMenuOption>() : null;
                var available = option != null && IsCoverOptionAvailable(option.Selection);
                UpdateCoverControllerPointer(i, ray, hit ? info.distance : 8f, available, held);
                var pressed = ConsumeCoverPointerPress(i, held);
                if (!available) continue;
                // A click owns the frame; otherwise keep the existing hovered hand to avoid flicker.
                if (pressed)
                {
                    _coverInputHand = i;
                    SetCoverHover(option.Selection);
                    ActivateCoverSelection();
                    return;
                }
                if (hoverOption < 0 || _coverInputHand == i)
                {
                    hoverOption = option.Selection;
                    hoverHand = i;
                }
            }
            if (tracked == 0)
            {
                // Only a fresh mouse gesture may take focus from keyboard navigation.
                if (_coverInputHand >= 0) SetCoverHover(-1);
                _coverInputHand = -1;
            }
            else
            {
                _coverInputHand = hoverHand;
                SetCoverHover(hoverOption);
            }
        }

        private void SetCoverHover(int selection)
        {
            if (selection == _coverHovered) return;
            _coverHovered = selection;
            if (selection >= 0) SelectCoverOption(selection, true);
        }

        private void UpdateCoverOptionFromRay(Ray ray, bool pressed)
        {
            var hit = Physics.Raycast(ray, out var info, 8f, ~0, QueryTriggerInteraction.Collide);
            var option = hit ? info.collider.GetComponent<CosmicWorldMenuOption>() : null;
            SetCoverHover(option != null && IsCoverOptionAvailable(option.Selection) ? option.Selection : -1);
            if (pressed && _coverHovered >= 0) ActivateCoverSelection();
        }

        private void SetCoverVisible(bool visible)
        {
            if (_coverGroup == null) return;
            _coverRoot.gameObject.SetActive(visible);
            _coverGroup.alpha = visible ? 1f : 0f;
            _coverGroup.blocksRaycasts = visible;
            _coverGroup.interactable = visible;
            if (!visible) HideCoverControllerPointers();
        }

        private void SetBladesVisible(bool visible)
        {
            if (_leftBlade != null) _leftBlade.gameObject.SetActive(visible);
            if (_rightBlade != null) _rightBlade.gameObject.SetActive(visible);
        }

        private void BuildCover(RectTransform parent)
        {
            _coverRoot = NewMenuRect(parent, "Stellar Cleave Cover", Vector2.zero, new Vector2(1760f, 990f));
            _coverGroup = _coverRoot.gameObject.AddComponent<CanvasGroup>();
            _coverHomePage = NewMenuRect(_coverRoot, "Home Page", Vector2.zero, _coverRoot.sizeDelta);
            _coverGuidePage = NewMenuRect(_coverRoot, "How To Play Page", Vector2.zero, _coverRoot.sizeDelta);
            _coverArtwork = BuildMenuArtwork(_coverHomePage, "Nano Banana Stellar Cleave", "CosmicSliceMenu/Cover");
            _guideArtwork = BuildMenuArtwork(_coverGuidePage, "Nano Banana Field Guide", "CosmicSliceMenu/Guide");
            _guideArtwork.uvRect = new Rect(0.067f, 0.13f, 0.365f, 0.74f);
            _guideArtwork.rectTransform.anchoredPosition = new Vector2(-501f, -20f);
            _guideArtwork.rectTransform.sizeDelta = new Vector2(438f, 500f);
            _coverStartHitArea = BuildMenuControl(_coverHomePage, "START GAME", 0, new Vector2(-360f, -300f), 300f);
            BuildMenuControl(_coverHomePage, "HOW TO PLAY", 2, new Vector2(0f, -300f), 300f);
            _coverExitHitArea = BuildMenuControl(_coverHomePage, "EXIT", 1, new Vector2(360f, -300f), 300f);
            BuildGuideContents(_coverGuidePage);
            BuildMenuControl(_coverGuidePage, "BACK", 3, new Vector2(-501f, -355f), 250f);
            BuildMenuControl(_coverGuidePage, "START GAME", 4, new Vector2(495f, -355f), 340f);
            _coverStatusText = MenuText(_coverRoot, "Cover Status", "READY", new Vector2(0f, -412f), new Vector2(1400f, 32f), 22, MenuPaper, TextAnchor.MiddleCenter);
            _coverPromptText = MenuText(_coverRoot, "Cover Prompt", "", new Vector2(0f, -455f), new Vector2(1400f, 28f), 19, new Color(0.55f, 0.76f, 0.68f), TextAnchor.MiddleCenter);
            BuildCoverControllerPointers();
            _coverRoot.SetAsLastSibling();
        }

        private RawImage BuildMenuArtwork(RectTransform parent, string name, string resource)
        {
            var surface = CreateImage(parent, "Cover Opaque Launch Surface", Vector2.zero, parent.sizeDelta, MenuInk);
            var image = NewMenuRect(parent, name, Vector2.zero, parent.sizeDelta).gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>(resource);
            image.raycastTarget = false;
            if (coverFeatherShader != null)
            {
                var feather = new Material(coverFeatherShader) { name = "Menu Feather Into Black" };
                feather.SetColor("_Tint", Color.white);
                feather.SetColor("_EdgeColor", Color.black);
                feather.SetFloat("_Feather", 0.16f);
                surface.material = feather;
                if (resource.EndsWith("/Cover")) image.material = feather;
            }
            if (image.texture == null) Debug.LogError("COSMIC_MENU_MISSING_ART " + resource);
            return image;
        }

        private static RectTransform NewMenuRect(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Text MenuText(RectTransform parent, string name, string copy, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = CreateText(parent, name, position, size, fontSize, alignment, color);
            text.text = copy;
            text.fontStyle = FontStyle.Normal;
            text.lineSpacing = 1.15f;
            return text;
        }

        private RectTransform BuildMenuControl(RectTransform parent, string label, int index, Vector2 position, float width)
        {
            var rect = NewMenuRect(parent, "Cover Option " + label, position, new Vector2(width, 76f));
            var fill = rect.gameObject.AddComponent<CosmicPanelGraphic>();
            fill.CornerCut = 15f;
            fill.color = MenuInk;
            fill.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var collider = rect.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, 76f, 40f);
            collider.center = new Vector3(0f, 0f, -20f);
            rect.gameObject.AddComponent<CosmicWorldMenuOption>().Configure(index);
            var text = MenuText(rect, "Option Label", label, new Vector2(6f, 0f), new Vector2(width - 48f, 50f), 30, MenuPaper, TextAnchor.MiddleCenter);
            text.GetComponent<Shadow>().enabled = false;
            var marker = CreateImage(rect, "Focus Mark", new Vector2(-width * 0.5f + 20f, 0f), new Vector2(5f, 24f), Color.clear);
            _menuControls[index] = new MenuControl { Rect = rect, Fill = fill, Outline = outline, Label = text, Marker = marker };
            return rect;
        }

        private void BuildGuideContents(RectTransform parent)
        {
            MenuText(parent, "Field Guide Heading", "HOW TO PLAY", new Vector2(-405f, 355f), new Vector2(630f, 72f), 52, MenuPaper);
            MenuText(parent, "Guide Goal", "75 SECONDS. 3 LIVES.", new Vector2(350f, 355f), new Vector2(630f, 52f), 32, MenuPaper, TextAnchor.MiddleRight);
            CreateImage(parent, "Guide Header Rule", new Vector2(-20f, 278f), new Vector2(1400f, 1f), new Color(0.21f, 0.40f, 0.34f, 0.65f));
            GuideRule(parent, "LEFT / CYAN", "Cut cyan signals\nwith your left blade.", new Vector2(-50f, 183f), CosmicPalette.Cyan);
            GuideRule(parent, "RIGHT / AMBER", "Cut amber signals\nwith your right blade.", new Vector2(335f, 183f), CosmicPalette.Amber);
            GuideRule(parent, "DUAL / VIOLET", "Use one hand, then the\nother within 0.9s.", new Vector2(-50f, -8f), new Color(0.78f, 0.38f, 1f));
            GuideRule(parent, "AVOID / RED", "Let red hazards pass.\nNever cut them.", new Vector2(335f, -8f), CosmicPalette.Red);
            MenuText(parent, "Guide Life Rule", "Wrong hand or missed non-red signal: lose 1 life.", new Vector2(307.5f, -182f), new Vector2(715f, 36f), 25, MenuPaper);
#if UNITY_ANDROID && !UNITY_EDITOR
            const string input = "Swing your blades, or aim + pull the trigger.";
#else
            const string input = "Left click: cyan blade   /   Right click: amber blade";
#endif
            MenuText(parent, "Guide Input", input, new Vector2(307.5f, -232f), new Vector2(715f, 36f), 24, new Color(0.60f, 0.80f, 0.72f));
        }

        private void GuideRule(RectTransform parent, string title, string copy, Vector2 position, Color color)
        {
            MenuText(parent, "Guide " + title, title, position + new Vector2(165f, 0f), new Vector2(330f, 38f), 28, color);
            MenuText(parent, "Guide Detail " + title, copy, position + new Vector2(165f, -67f), new Vector2(330f, 92f), 27, MenuPaper, TextAnchor.UpperLeft);
        }

        private void BuildCoverControllerPointers()
        {
            for (var index = 0; index < 2; index++)
            {
                var side = index == 0 ? "Left" : "Right";
                var sideColor = index == 0 ? CosmicPalette.Cyan : CosmicPalette.Amber;
                var sourceMaterial = index == 0 ? _cyanGlow : _amberGlow;
                var material = new Material(sourceMaterial) { name = side + " Cover Pointer Material" };
                _coverPointerMaterials[index] = material;

                var rayObject = new GameObject(side + " Cover World Interaction Ray");
                rayObject.transform.SetParent(_titleRoot, false);
                var line = rayObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.006f), new Keyframe(1f, 0.0018f));
                line.numCapVertices = 4;
                line.sharedMaterial = material;
                line.startColor = new Color(sideColor.r, sideColor.g, sideColor.b, 0.82f);
                line.endColor = new Color(sideColor.r, sideColor.g, sideColor.b, 0.22f);
                line.enabled = false;
                _coverControllerRays[index] = line;

                var reticle = new GameObject(side + " Cover World Aim Reticle").transform;
                reticle.SetParent(_titleRoot, false);
                var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = "Aim Point";
                point.transform.SetParent(reticle, false);
                point.transform.localScale = Vector3.one * 0.028f;
                Destroy(point.GetComponent<Collider>());
                point.GetComponent<Renderer>().sharedMaterial = material;
                var ring = CosmicSliceMeshFactory.CreateTorusObject("Aim Ring", 0.055f, 0.006f, material);
                ring.transform.SetParent(reticle, false);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                reticle.gameObject.SetActive(false);
                _coverControllerReticles[index] = reticle;
            }
        }

        private void UpdateCoverControllerPointer(int index, Ray ray, float distance, bool overOption, bool held)
        {
            var line = _coverControllerRays[index];
            var reticle = _coverControllerReticles[index];
            if (line == null || reticle == null)
            {
                return;
            }

            var sideColor = index == 0 ? CosmicPalette.Cyan : CosmicPalette.Amber;
            var color = overOption ? Color.white : sideColor;
            line.enabled = true;
            line.widthMultiplier = held ? 1.65f : 1f;
            line.startColor = new Color(color.r, color.g, color.b, held ? 1f : 0.82f);
            line.endColor = new Color(color.r, color.g, color.b, overOption ? 0.94f : 0.22f);
            line.SetPosition(0, ray.origin);
            line.SetPosition(1, ray.GetPoint(distance));

            reticle.gameObject.SetActive(true);
            reticle.position = ray.GetPoint(distance);
            reticle.rotation = Quaternion.LookRotation(_camera.transform.forward, _camera.transform.up);
            reticle.localScale = Vector3.one * (overOption ? (held ? 0.72f : 1.22f) : 0.68f);
            var material = _coverPointerMaterials[index];
            if (material != null && material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private bool ConsumeCoverPointerPress(int index, bool held)
        {
            if (!held)
            {
                _coverPointerTriggerHeld[index] = false;
                _coverPointerTriggerArmed[index] = true;
                return false;
            }

            var pressed = _coverPointerTriggerArmed[index] && !_coverPointerTriggerHeld[index];
            _coverPointerTriggerHeld[index] = true;
            return pressed && _xrStartCooldown <= 0f;
        }

        private void HideCoverControllerPointer(int index)
        {
            if (_coverControllerRays[index] != null) _coverControllerRays[index].enabled = false;
            if (_coverControllerReticles[index] != null) _coverControllerReticles[index].gameObject.SetActive(false);
            _coverPointerTriggerHeld[index] = false;
            _coverPointerTriggerArmed[index] = false;
        }

        private void HideCoverControllerPointers()
        {
            HideCoverControllerPointer(0);
            HideCoverControllerPointer(1);
        }

    }
}
