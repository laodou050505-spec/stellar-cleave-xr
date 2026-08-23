using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private RectTransform _coverRoot;
        private RectTransform _coverStartHitArea;
        private RectTransform _coverExitHitArea;
        private RectTransform _coverFocusBar;
        private CanvasGroup _coverGroup;
        private Text[] _coverOptionLabels;
        private Text _coverStatusText;
        private Text _coverPromptText;
        private int _coverSelection;
        private float _coverTransitionTimer;
        private bool _coverTransitioning;

        private void UpdateWorldMenuRay()
        {
            if (_camera == null || _coverGroup == null || _coverGroup.alpha < 0.9f)
            {
                return;
            }

            var pointer = GetWorldPointerRay(out var pressed);
            if (!Physics.Raycast(pointer, out var hit, 8f))
            {
                return;
            }
            var option = hit.collider.GetComponent<CosmicWorldMenuOption>();
            if (option == null)
            {
                return;
            }

            SelectCoverOption(option.Selection, false);
            if (pressed)
            {
                ActivateCoverSelection();
            }
        }

        private Ray GetWorldPointerRay(out bool pressed)
        {
            pressed = false;
            // In the emulator, controller tracking often remains valid even while the user is
            // operating the host window.  A live mouse gesture must therefore win over that
            // dormant tracked ray so START can be selected with the desktop fallback.
            if (Mouse.current != null &&
                (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed ||
                 Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f))
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                return _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            }

            foreach (var node in new[] { UnityEngine.XR.XRNode.RightHand, UnityEngine.XR.XRNode.LeftHand })
            {
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (!device.isValid || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position))
                {
                    continue;
                }

                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out pressed);
                return new Ray(_rigTransform.TransformPoint(position), _rigTransform.rotation * rotation * Vector3.forward);
            }

            if (Mouse.current != null)
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                return _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            }
            return new Ray(_camera.transform.position, _camera.transform.forward);
        }

        private void EnterCover()
        {
            _state = GameState.Cover;
            _coverSelection = 0;
            _coverTransitionTimer = 0f;
            _coverTransitioning = false;
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
            SelectCoverOption(0, false);
        }

        private void UpdateCover(float deltaTime)
        {
            if (_coverGroup == null)
            {
                return;
            }

            if (_coverTransitioning)
            {
                _coverTransitionTimer -= deltaTime;
                var progress = Mathf.Clamp01(1f - _coverTransitionTimer / 0.72f);
                _coverGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
                if (_coverTransitionTimer <= 0f)
                {
                    _coverTransitioning = false;
                    SetCoverVisible(false);
                    EnterScanning();
                }
                return;
            }

            var elapsed = Time.unscaledTime;
            if (_coverFocusBar != null)
            {
                var pulse = 0.82f + Mathf.Sin(elapsed * 4.6f) * 0.12f;
                _coverFocusBar.localScale = Vector3.one * (1f + (pulse - 0.82f) * 0.12f);
            }

            // Ray/trigger is the primary spatial action. Desktop mouse remains a fallback only.
            UpdateWorldMenuRay();
            if (_coverTransitioning)
            {
                return;
            }

            if (Mouse.current != null && _hudRoot != null && _hudRoot.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace)
            {
                var pointerPosition = Mouse.current.position.ReadValue();
                if (RectTransformUtility.RectangleContainsScreenPoint(_coverStartHitArea, pointerPosition, _camera))
                {
                    SelectCoverOption(0, false);
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(_coverExitHitArea, pointerPosition, _camera))
                {
                    SelectCoverOption(1, false);
                }

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(_coverStartHitArea, pointerPosition, _camera) ||
                        RectTransformUtility.RectangleContainsScreenPoint(_coverExitHitArea, pointerPosition, _camera))
                    {
                        ActivateCoverSelection();
                    }
                    return;
                }
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                {
                    SelectCoverOption(_coverSelection + 1, true);
                }
                else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                {
                    SelectCoverOption(_coverSelection - 1, true);
                }
                else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    ActivateCoverSelection();
                }
            }

            if (WasXrConfirmPressed())
            {
                ActivateCoverSelection();
            }
        }

        private void ActivateCoverSelection()
        {
            if (_coverTransitioning)
            {
                return;
            }

            if (_coverSelection == 1)
            {
                _coverStatusText.text = "EXIT REQUEST ACKNOWLEDGED";
                _coverPromptText.text = "ENDING LOCAL SESSION";
                _coverTransitioning = true;
                _coverTransitionTimer = 0.4f;
                Invoke(nameof(QuitApplication), 0.25f);
                return;
            }

            _coverStatusText.text = "ANOMALY LINK ESTABLISHED";
            _coverPromptText.text = "STARTING LOCAL SPACE SURVEY";
            _coverTransitioning = true;
            _coverTransitionTimer = 0.72f;
            PlaySfx(positive, 0.55f, 0.95f);
        }

        private void QuitApplication()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private bool WasXrConfirmPressed()
        {
            var anyTriggerHeld = false;
            foreach (var node in new[] { UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.XRNode.RightHand })
            {
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var held) && held)
                {
                    anyTriggerHeld = true;
                    break;
                }
            }

            // Do not let a trigger state left behind by the emulator skip the title.
            // A player must release, then make a new trigger press to enter the game.
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

        private void SelectCoverOption(int selection, bool audible)
        {
            _coverSelection = Mathf.Clamp(selection, 0, 1);
            if (_coverOptionLabels == null)
            {
                return;
            }

            for (var index = 0; index < _coverOptionLabels.Length; index++)
            {
                var selected = index == _coverSelection;
                _coverOptionLabels[index].color = selected ? CosmicPalette.Cyan : new Color(CosmicPalette.Silver.r, CosmicPalette.Silver.g, CosmicPalette.Silver.b, 0.78f);
                _coverOptionLabels[index].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }

            if (_coverFocusBar != null)
            {
                _coverFocusBar.anchoredPosition = new Vector2(0f, _coverSelection == 0 ? -106f : -176f);
            }
            if (_coverStatusText != null && !_coverTransitioning)
            {
                _coverStatusText.text = _coverSelection == 0 ? "ANOMALY PROTOCOL READY" : "EXIT TO DESKTOP";
            }
            if (audible)
            {
                PlaySfx(positive, 0.18f, _coverSelection == 0 ? 1.05f : 0.9f);
            }
        }

        private void SetCoverVisible(bool visible)
        {
            if (_coverGroup == null)
            {
                return;
            }
            _coverRoot.gameObject.SetActive(visible);
            _coverGroup.alpha = visible ? 1f : 0f;
            _coverGroup.blocksRaycasts = visible;
            _coverGroup.interactable = visible;
        }

        private void SetBladesVisible(bool visible)
        {
            if (_leftBlade != null) _leftBlade.gameObject.SetActive(visible);
            if (_rightBlade != null) _rightBlade.gameObject.SetActive(visible);
        }

        private void BuildCover(RectTransform parent)
        {
            var coverObject = new GameObject("Stellar Cleave Cover");
            _coverRoot = coverObject.AddComponent<RectTransform>();
            _coverRoot.SetParent(parent, false);
            _coverRoot.anchorMin = Vector2.zero;
            _coverRoot.anchorMax = Vector2.one;
            _coverRoot.offsetMin = Vector2.zero;
            _coverRoot.offsetMax = Vector2.zero;
            _coverGroup = coverObject.AddComponent<CanvasGroup>();
            _coverGroup.alpha = 1f;
            _coverGroup.interactable = true;
            _coverGroup.blocksRaycasts = true;

            // An opaque physical title surface: the launch phase cannot leak any gameplay stage
            // through a translucent cover, even if a future scene adds a distant renderer.
            var veil = CreateImage(_coverRoot, "Cover Opaque Launch Surface", Vector2.zero, Vector2.zero,
                new Color(0.002f, 0.008f, 0.024f, 1f));
            veil.rectTransform.anchorMin = Vector2.zero;
            veil.rectTransform.anchorMax = Vector2.one;
            veil.rectTransform.offsetMin = Vector2.zero;
            veil.rectTransform.offsetMax = Vector2.zero;

            BuildCoverCornerFrame(_coverRoot);
            BuildCoverTitle(_coverRoot);
            BuildCoverControllerSignals(_coverRoot);

            _coverStartHitArea = BuildCoverOption(_coverRoot, "START GAME", 0, -106f);
            _coverExitHitArea = BuildCoverOption(_coverRoot, "EXIT", 1, -176f);
            _coverFocusBar = BuildCoverFocusFrame(_coverRoot);

            _coverStatusText = CreateText(_coverRoot, "Cover Status", new Vector2(0f, -315f), new Vector2(550f, 21f), 12, TextAnchor.MiddleCenter, new Color(CosmicPalette.Silver.r, CosmicPalette.Silver.g, CosmicPalette.Silver.b, 0.78f));
            _coverStatusText.fontStyle = FontStyle.Normal;
            _coverStatusText.text = "ANOMALY PROTOCOL READY";
            _coverPromptText = CreateText(_coverRoot, "Cover Prompt", new Vector2(0f, -344f), new Vector2(600f, 20f), 11, TextAnchor.MiddleCenter, CosmicPalette.Cyan);
            _coverPromptText.fontStyle = FontStyle.Normal;
            _coverPromptText.text = "TRIGGER / SPACE TO DEPLOY";
            _coverOptionLabels = new[]
            {
                _coverStartHitArea.GetComponentInChildren<Text>(),
                _coverExitHitArea.GetComponentInChildren<Text>()
            };
            _coverRoot.SetAsLastSibling();
        }

        private RectTransform BuildCoverOption(RectTransform parent, string label, int index, float y)
        {
            var optionObject = new GameObject("Cover Option " + label);
            var rect = optionObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(index == 0 ? 238f : 188f, 44f);
            var backing = optionObject.AddComponent<CosmicPanelGraphic>();
            backing.CornerCut = 9f;
            backing.color = new Color(0.007f, 0.02f, 0.055f, index == 0 ? 0.7f : 0.5f);
            backing.raycastTarget = false;
            var interaction = optionObject.AddComponent<CosmicWorldMenuOption>();
            interaction.Configure(index);
            var hitbox = optionObject.AddComponent<BoxCollider>();
            hitbox.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 84f);
            hitbox.center = new Vector3(0f, 0f, -42f);
            var outline = optionObject.AddComponent<Outline>();
            outline.effectColor = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, index == 0 ? 0.52f : 0.2f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            var labelText = CreateText(rect, "Option Label", Vector2.zero, new Vector2(rect.sizeDelta.x - 22f, 34f), index == 0 ? 19 : 16, TextAnchor.MiddleCenter, CosmicPalette.Silver);
            labelText.fontStyle = FontStyle.Normal;
            labelText.text = label;
            return rect;
        }

        private void BuildCoverTitle(RectTransform parent)
        {
            var glow = CreateText(parent, "Stellar Cleave Title Glow", new Vector2(0f, 96f), new Vector2(980f, 86f), 61, TextAnchor.MiddleCenter, new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.16f));
            glow.text = "STELLAR CLEAVE";
            glow.fontStyle = FontStyle.Bold;
            var title = CreateText(parent, "Stellar Cleave Title", new Vector2(0f, 100f), new Vector2(980f, 86f), 61, TextAnchor.MiddleCenter, new Color(0.9f, 0.96f, 1f, 1f));
            title.text = "STELLAR CLEAVE";
            title.fontStyle = FontStyle.Bold;
            var subtitle = CreateText(parent, "Anomaly Protocol Subtitle", new Vector2(0f, 46f), new Vector2(560f, 22f), 15, TextAnchor.MiddleCenter, new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.82f));
            subtitle.text = "A N O M A L Y   P R O T O C O L";
            subtitle.fontStyle = FontStyle.Normal;
            CreateImage(parent, "Title Cyan Rule", new Vector2(-175f, 24f), new Vector2(190f, 2f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.62f));
            CreateImage(parent, "Title Amber Rule", new Vector2(175f, 24f), new Vector2(190f, 2f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.52f));
        }

        private void BuildCoverControllerSignals(RectTransform parent)
        {
            BuildCoverControllerSignal(parent, "Left Cyan Controller Signal", new Vector2(-386f, -158f), CosmicPalette.Cyan, -1f);
            BuildCoverControllerSignal(parent, "Right Amber Controller Signal", new Vector2(386f, -158f), CosmicPalette.Amber, 1f);
        }

        private static void BuildCoverControllerSignal(RectTransform parent, string name, Vector2 position, Color color, float direction)
        {
            var root = new GameObject(name).AddComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(160f, 126f);
            root.localRotation = Quaternion.Euler(0f, 0f, direction * -11f);

            var aura = new GameObject("Energy Aura").AddComponent<RectTransform>();
            aura.SetParent(root, false);
            aura.anchoredPosition = Vector2.zero;
            aura.sizeDelta = new Vector2(102f, 102f);
            var auraGraphic = aura.gameObject.AddComponent<CosmicPanelGraphic>();
            auraGraphic.CornerCut = 46f;
            auraGraphic.color = new Color(color.r, color.g, color.b, 0.14f);
            auraGraphic.raycastTarget = false;

            var handle = new GameObject("Controller Hilt").AddComponent<RectTransform>();
            handle.SetParent(root, false);
            handle.anchoredPosition = new Vector2(0f, -7f);
            handle.sizeDelta = new Vector2(37f, 68f);
            handle.localRotation = Quaternion.Euler(0f, 0f, direction * -18f);
            var hiltGraphic = handle.gameObject.AddComponent<CosmicPanelGraphic>();
            hiltGraphic.CornerCut = 12f;
            hiltGraphic.color = new Color(0.035f, 0.065f, 0.11f, 0.94f);
            hiltGraphic.raycastTarget = false;

            var core = new GameObject("Energy Core").AddComponent<RectTransform>();
            core.SetParent(handle, false);
            core.anchoredPosition = new Vector2(0f, 19f);
            core.sizeDelta = new Vector2(15f, 28f);
            var coreGraphic = core.gameObject.AddComponent<CosmicPanelGraphic>();
            coreGraphic.CornerCut = 7f;
            coreGraphic.color = new Color(color.r, color.g, color.b, 0.95f);
            coreGraphic.raycastTarget = false;

            var ring = new GameObject("Controller Ring").AddComponent<RectTransform>();
            ring.SetParent(root, false);
            ring.anchoredPosition = new Vector2(direction * 15f, 18f);
            ring.sizeDelta = new Vector2(84f, 8f);
            ring.localRotation = Quaternion.Euler(0f, 0f, direction * 24f);
            var ringGraphic = ring.gameObject.AddComponent<CosmicPanelGraphic>();
            ringGraphic.CornerCut = 4f;
            ringGraphic.color = new Color(color.r, color.g, color.b, 0.56f);
            ringGraphic.raycastTarget = false;

            for (var index = 0; index < 3; index++)
            {
                var trace = CreateImage(root, "Signal Trace " + index, new Vector2(direction * (38f + index * 18f), 18f + index * 16f), new Vector2(39f - index * 5f, 2f), new Color(color.r, color.g, color.b, 0.35f - index * 0.07f));
                trace.rectTransform.localRotation = Quaternion.Euler(0f, 0f, direction * 28f);
            }
        }

        private RectTransform BuildCoverFocusFrame(RectTransform parent)
        {
            var frame = new GameObject("Cover Selection Brackets").AddComponent<RectTransform>();
            frame.SetParent(parent, false);
            frame.anchoredPosition = new Vector2(0f, -106f);
            frame.sizeDelta = new Vector2(320f, 52f);
            CreateImage(frame, "Left Selection Bracket", new Vector2(-145f, 0f), new Vector2(22f, 2f), CosmicPalette.Cyan);
            CreateImage(frame, "Right Selection Bracket", new Vector2(145f, 0f), new Vector2(22f, 2f), CosmicPalette.Cyan);
            CreateImage(frame, "Left Selection Stem", new Vector2(-153f, 0f), new Vector2(2f, 20f), CosmicPalette.Cyan);
            CreateImage(frame, "Right Selection Stem", new Vector2(153f, 0f), new Vector2(2f, 20f), CosmicPalette.Cyan);
            return frame;
        }

        private static void BuildCoverCornerFrame(RectTransform parent)
        {
            var color = new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.34f);
            CreateImage(parent, "Cover Top Left Horizontal", new Vector2(-760f, 442f), new Vector2(54f, 2f), color);
            CreateImage(parent, "Cover Top Right Horizontal", new Vector2(760f, 442f), new Vector2(54f, 2f), color);
            CreateImage(parent, "Cover Bottom Left Horizontal", new Vector2(-760f, -442f), new Vector2(54f, 2f), color);
            CreateImage(parent, "Cover Bottom Right Horizontal", new Vector2(760f, -442f), new Vector2(54f, 2f), color);
            CreateImage(parent, "Cover Top Left Vertical", new Vector2(-787f, 416f), new Vector2(2f, 54f), color);
            CreateImage(parent, "Cover Top Right Vertical", new Vector2(787f, 416f), new Vector2(2f, 54f), color);
            CreateImage(parent, "Cover Bottom Left Vertical", new Vector2(-787f, -416f), new Vector2(2f, 54f), color);
            CreateImage(parent, "Cover Bottom Right Vertical", new Vector2(787f, -416f), new Vector2(2f, 54f), color);
        }

    }
}
