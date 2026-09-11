using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        private enum StageAction { StartOrPreview, Rescan, Exit }
        private sealed class StageButton
        {
            public MenuControl Control;
            public BoxCollider Collider;
            public StageAction Action;
        }
        private readonly List<StageButton> _stageButtons = new();
        private int _stageSelected = -1;
        private int _stagePending = -1;
        private float _stageDelay;
        private void BuildStageButton(RectTransform parent, string label, StageAction action, Vector2 position, float width)
        {
            var rect = NewMenuRect(parent, "Action " + label, position, new Vector2(width, 68f));
            var fill = rect.gameObject.AddComponent<CosmicPanelGraphic>();
            fill.CornerCut = 9f; fill.color = MenuInk; fill.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = new Color(0.26f, 0.46f, 0.37f);
            var collider = rect.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, 68f, 30f); collider.center = new Vector3(0f, 0f, -15f);
            rect.gameObject.AddComponent<CosmicWorldStageOption>().Configure(_stageButtons.Count);
            var text = ConsoleText(rect, "Action Label", label, Vector2.zero, new Vector2(width - 36f, 48f), 27, MenuPaper, TextAnchor.MiddleCenter);
            _stageButtons.Add(new StageButton { Control = new MenuControl { Rect = rect, Fill = fill, Outline = outline, Label = text }, Collider = collider, Action = action });
        }
        public bool IsStageButtonAvailable(int index)
        {
            if (index < 0 || index >= _stageButtons.Count || (_state != GameState.Ready && _state != GameState.Results)) return false;
            var button = _stageButtons[index];
            if (!button.Control.Rect.gameObject.activeInHierarchy) return false;
            return button.Action != StageAction.StartOrPreview || CanUsePortal || _ceilingScanComplete;
        }
        private void FocusStageButton(int index)
        {
            if (!IsStageButtonAvailable(index) || _stageSelected == index) return;
            _stageSelected = index; PlaySfx(positive, 0.16f, 1.35f);
        }
        private void PressStageButton(int index, int hand = -1)
        {
            if (_stagePending >= 0 || !IsStageButtonAvailable(index)) return;
            FocusStageButton(index);
            _stagePending = index; _stageDelay = 0.16f;
            _stageButtons[index].Control.Press = 0.18f;
            PlaySfx(positive, 0.45f, 1.1f);
            if (hand >= 0)
            {
                var device = InputDevices.GetDeviceAtXRNode(hand == 0 ? XRNode.LeftHand : XRNode.RightHand);
                if (device.isValid) device.SendHapticImpulse(0, 0.3f, 0.045f);
            }
            Debug.Log($"COSMIC_STAGE_BUTTON_PRESS index={index} action={_stageButtons[index].Action}");
        }
        private bool StageButtonFromRay(Ray ray, bool pressed, int hand = -1)
        {
            if (!Physics.Raycast(ray, out var hit, 8f, ~0, QueryTriggerInteraction.Collide)) return false;
            var marker = hit.collider.GetComponent<CosmicWorldStageOption>();
            if (marker == null || !IsStageButtonAvailable(marker.Index)) return false;
            FocusStageButton(marker.Index);
            if (pressed) PressStageButton(marker.Index, hand);
            return true;
        }
        private void UpdateStageControlVisuals(float deltaTime)
        {
            var blend = 1f - Mathf.Exp(-20f * deltaTime);
            for (var i = 0; i < _stageButtons.Count; i++)
            {
                var data = _stageButtons[i]; var button = data.Control;
                var enabled = IsStageButtonAvailable(i); data.Collider.enabled = enabled;
                var pressed = button.Press > 0f;
                button.Press = Mathf.Max(0f, button.Press - deltaTime);
                var selected = _stageSelected == i && enabled;
                var desired = pressed ? new Color(1f, 0.83f, 0.53f) : selected ? MenuAccent : MenuInk;
                button.Fill.color = Color.Lerp(button.Fill.color, desired, blend);
                var c = button.Fill.color;
                var luminance = 0.2126f * Mathf.GammaToLinearSpace(c.r) + 0.7152f * Mathf.GammaToLinearSpace(c.g) + 0.0722f * Mathf.GammaToLinearSpace(c.b);
                button.Label.color = !enabled ? ConsoleMuted : luminance > 0.16f ? MenuInk : MenuPaper;
                button.Outline.effectColor = selected ? MenuAccent : new Color(0.26f, 0.46f, 0.37f);
                button.Rect.localScale = Vector3.Lerp(button.Rect.localScale, Vector3.one * (pressed ? 0.94f : selected ? 1.035f : 1f), blend);
            }
        }
        private void AdvanceStageAction(float deltaTime)
        {
            if (_stagePending < 0) return;
            _stageDelay -= deltaTime;
            if (_stageDelay > 0f) return;
            var index = _stagePending;
            var action = _stageButtons[index].Action;
            _stagePending = -1;
            if (action == StageAction.Exit) { QuitApplication(); return; }
            if (action == StageAction.Rescan)
            {
                EnterScanning();
                if (index == 1) GetComponent<PicoCeilingLocator>()?.RequestRelocate(true);
                return;
            }
            if (CanUsePortal) BeginRound();
            else SelectCeilingPreview();
        }
        private void UpdateStageControls(float deltaTime)
        {
            if (_stagePending >= 0) { AdvanceStageAction(deltaTime); return; }
            if (!IsStageButtonAvailable(_stageSelected))
                for (var i = 0; i < _stageButtons.Count; i++) if (IsStageButtonAvailable(i)) { _stageSelected = i; break; }
            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.01f || mouse.leftButton.wasPressedThisFrame))
            {
                StageButtonFromRay(_camera.ScreenPointToRay(mouse.position.ReadValue()), mouse.leftButton.wasPressedThisFrame);
                if (_stagePending >= 0) return;
            }
            for (var hand = 0; hand < 2; hand++)
            {
                var device = InputDevices.GetDeviceAtXRNode(hand == 0 ? XRNode.LeftHand : XRNode.RightHand);
                if (!device.isValid || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position) ||
                    (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked) && !tracked)) continue;
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var held);
                var press = ConsumeCoverPointerPress(hand, held);
                StageButtonFromRay(new Ray(_rigTransform.TransformPoint(position), _rigTransform.rotation * rotation * Vector3.forward), press, hand);
                if (_stagePending >= 0) return;
            }
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.tabKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame)
            {
                var direction = keys.leftArrowKey.wasPressedThisFrame || keys.shiftKey.isPressed ? -1 : 1;
                for (var j = 1; j <= _stageButtons.Count; j++)
                {
                    var next = (_stageSelected + direction * j + _stageButtons.Count * 2) % _stageButtons.Count;
                    if (IsStageButtonAvailable(next)) { FocusStageButton(next); break; }
                }
            }
            else if (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) PressStageButton(_stageSelected);
        }
    }
}
