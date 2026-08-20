using UnityEngine;
using UnityEngine.UI;

namespace CosmicSlice
{
    // THESIS: A portal over a deep-blue grid frames a centered choice; it refuses cockpit-like side panels.
    // OWN-WORLD: Black-hole silhouette, blue-violet space, cyan/amber hand signals and silver native type.
    // STORY: Confirm START GAME, survey the local space, then cut only the correctly signaled anomaly.
    // FIRST VIEWPORT: Portal at the upper center, centered STELLAR CLEAVE title, paired controller signals and compact menu.
    // FORM: Figma-authoritative title screen recreated with Unity native text and interactive selection brackets.
    public sealed partial class CosmicSliceGame
    {
        private Font _uiFont;
        private RectTransform _hudFrameRoot;
        private RectTransform _scorePanelRoot;
        private RectTransform _timerPanelRoot;
        private RectTransform _lifePanelRoot;
        private RectTransform _ruleStripRoot;

        private void BuildHud(Transform missionConsoleRoot, bool includeCover = true)
        {
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Orbital Containment Instrument");
            var canvas = canvasObject.AddComponent<Canvas>();
            // This is the physical mission console in the arena. It is never head-locked: the
            // cover choices, tutorial, results and score all remain walk-up world-space objects.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 24;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1760f, 990f);
            if (missionConsoleRoot != null)
            {
                canvasRect.SetParent(missionConsoleRoot, false);
                canvasRect.localPosition = Vector3.zero;
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one * CosmicSliceDesign.MissionConsoleScale;
            }
            else
            {
                canvasRect.position = CosmicSliceDesign.GameplayMissionConsolePosition;
                canvasRect.rotation = Quaternion.Euler(0f, 180f, 0f);
                canvasRect.localScale = Vector3.one * CosmicSliceDesign.MissionConsoleScale;
            }
            _hudRoot = canvasRect;

            // Title is a self-contained launch stage. It owns only the opaque cover and its
            // physical Start / Exit targets; gameplay score, rules and result UI are created
            // only after the title root has been destroyed.
            if (includeCover)
            {
                BuildCover(canvasRect);
                return;
            }

            BuildHudFrame(canvasRect);
            var scorePanel = CreateInstrumentPanel(canvasRect, "Score Instrument", new Vector2(-680f, 382f), new Vector2(300f, 146f), CosmicPalette.Cyan, 0.88f);
            _scorePanelRoot = scorePanel;
            var scoreLabel = CreateText(scorePanel, "Score Label", new Vector2(-14f, 42f), new Vector2(244f, 24f), 15, TextAnchor.MiddleLeft, CosmicPalette.Silver);
            scoreLabel.fontStyle = FontStyle.Normal;
            scoreLabel.text = "CONTAINMENT SCORE";
            _scoreText = CreateText(scorePanel, "Score", new Vector2(-14f, 0f), new Vector2(244f, 58f), 50, TextAnchor.MiddleLeft, Color.white);
            _scoreText.text = "000000";
            _comboText = CreateText(scorePanel, "Combo", new Vector2(-14f, -44f), new Vector2(244f, 26f), 17, TextAnchor.MiddleLeft, CosmicPalette.Cyan);
            _comboText.text = "CHAIN 00";

            var timerPanel = CreateInstrumentPanel(canvasRect, "Timer Instrument", new Vector2(680f, 382f), new Vector2(300f, 146f), CosmicPalette.Amber, 0.88f);
            _timerPanelRoot = timerPanel;
            var timerLabel = CreateText(timerPanel, "Timer Label", new Vector2(14f, 42f), new Vector2(244f, 24f), 15, TextAnchor.MiddleRight, CosmicPalette.Silver);
            timerLabel.fontStyle = FontStyle.Normal;
            timerLabel.text = "ANOMALY WINDOW";
            _timerText = CreateText(timerPanel, "Timer", new Vector2(14f, 0f), new Vector2(244f, 62f), 52, TextAnchor.MiddleRight, Color.white);
            _timerText.text = "01:15";
            var timerNote = CreateText(timerPanel, "Timer Note", new Vector2(14f, -44f), new Vector2(244f, 26f), 15, TextAnchor.MiddleRight, CosmicPalette.Amber);
            timerNote.fontStyle = FontStyle.Normal;
            timerNote.text = "LIVE CONTAINMENT";

            var lifePanel = CreateInstrumentPanel(canvasRect, "Life Link Instrument", new Vector2(0f, 414f), new Vector2(310f, 54f), CosmicPalette.Violet, 0.84f);
            _lifePanelRoot = lifePanel;
            _livesText = CreateText(lifePanel, "Lives Label", new Vector2(-48f, 0f), new Vector2(208f, 28f), 15, TextAnchor.MiddleLeft, CosmicPalette.Silver);
            _livesText.text = "LIFE LINK  /  3 OF 3";
            for (var index = 0; index < _lifePips.Length; index++)
            {
                var pip = CreateImage(lifePanel, "Life Node " + (index + 1), new Vector2(80f + index * 31f, 0f), new Vector2(16f, 16f), Color.white);
                pip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var outline = pip.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(CosmicPalette.Violet.r, CosmicPalette.Violet.g, CosmicPalette.Violet.b, 0.72f);
                outline.effectDistance = new Vector2(2f, -2f);
                _lifePips[index] = pip;
            }

            _statusText = CreateText(canvasRect, "Status", new Vector2(0f, 292f), new Vector2(560f, 68f), 30, TextAnchor.MiddleCenter, Color.white);
            _statusText.text = string.Empty;

            BuildTutorial(canvasRect);
            _eventText = CreateText(canvasRect, "Event", new Vector2(0f, -300f), new Vector2(760f, 42f), 21, TextAnchor.MiddleCenter, CosmicPalette.Cyan);
            _eventText.text = string.Empty;

            BuildRuleStrip(canvasRect);
            BuildResultPanel(canvasRect);
            _flashImage = CreateImage(canvasRect, "Impact Flash", Vector2.zero, canvasRect.sizeDelta, Color.clear);
            _flashImage.raycastTarget = false;
            _flashImage.rectTransform.anchorMin = Vector2.zero;
            _flashImage.rectTransform.anchorMax = Vector2.one;
            _flashImage.rectTransform.offsetMin = Vector2.zero;
            _flashImage.rectTransform.offsetMax = Vector2.zero;
            _flashImage.transform.SetAsLastSibling();
        }

        private void BuildHudFrame(RectTransform parent)
        {
            _hudFrameRoot = new GameObject("Viewport Datum Frame").AddComponent<RectTransform>();
            _hudFrameRoot.SetParent(parent, false);
            _hudFrameRoot.anchorMin = Vector2.zero;
            _hudFrameRoot.anchorMax = Vector2.one;
            _hudFrameRoot.offsetMin = Vector2.zero;
            _hudFrameRoot.offsetMax = Vector2.zero;
            var brand = CreateText(_hudFrameRoot, "Brand Mark", new Vector2(-806f, 462f), new Vector2(176f, 18f), 11, TextAnchor.MiddleLeft, CosmicPalette.Silver);
            brand.fontStyle = FontStyle.Normal;
            brand.text = "STELLAR CLEAVE  /  ANOMALY 07";
            var mode = CreateText(_hudFrameRoot, "Runtime Mode", new Vector2(806f, 462f), new Vector2(176f, 18f), 11, TextAnchor.MiddleRight, CosmicPalette.Silver);
            mode.fontStyle = FontStyle.Normal;
            mode.text = "SPATIAL CONTAINMENT";
            var brandSignal = CreateImage(_hudFrameRoot, "Brand Signal", new Vector2(-842f, 462f), new Vector2(7f, 7f), CosmicPalette.Cyan);
            var modeSignal = CreateImage(_hudFrameRoot, "Mode Signal", new Vector2(842f, 462f), new Vector2(7f, 7f), CosmicPalette.Amber);
            CreateImage(_hudFrameRoot, "Top Left Datum", new Vector2(-528f, 462f), new Vector2(46f, 2f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.34f));
            CreateImage(_hudFrameRoot, "Top Right Datum", new Vector2(528f, 462f), new Vector2(46f, 2f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.34f));
        }

        private void CreateHudArc(RectTransform parent, string name, Vector2 center, float radius, float startAngle, float endAngle, int segments, Color color, float alpha)
        {
            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.Lerp(startAngle, endAngle, index / (float)(segments - 1));
                var radians = angle * Mathf.Deg2Rad;
                var position = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                var segment = CreateImage(parent, name + " " + index, position, new Vector2(index % 5 == 0 ? 24f : 14f, index % 5 == 0 ? 4f : 2f), new Color(color.r, color.g, color.b, alpha));
                segment.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private RectTransform CreateInstrumentPanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color accent, float fillAlpha)
        {
            var panelObject = new GameObject(name);
            var rect = panelObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var shell = panelObject.AddComponent<CosmicPanelGraphic>();
            shell.CornerCut = Mathf.Clamp(size.y * 0.16f, 10f, 24f);
            shell.color = new Color(accent.r, accent.g, accent.b, 0.42f);
            shell.raycastTarget = false;

            var surfaceObject = new GameObject("Matte Instrument Surface");
            var surfaceRect = surfaceObject.AddComponent<RectTransform>();
            surfaceRect.SetParent(rect, false);
            surfaceRect.anchorMin = Vector2.zero;
            surfaceRect.anchorMax = Vector2.one;
            surfaceRect.offsetMin = new Vector2(3f, 3f);
            surfaceRect.offsetMax = new Vector2(-3f, -3f);
            var surface = surfaceObject.AddComponent<CosmicPanelGraphic>();
            surface.CornerCut = Mathf.Clamp(size.y * 0.16f - 2f, 8f, 22f);
            surface.color = new Color(0.005f, 0.018f, 0.036f, fillAlpha);
            surface.raycastTarget = false;

            CreateImage(rect, "Panel Signal Bar", new Vector2(-size.x * 0.5f + 32f, size.y * 0.5f - 11f), new Vector2(42f, 5f), new Color(accent.r, accent.g, accent.b, 0.95f));
            for (var index = 0; index < 3; index++)
            {
                CreateImage(rect, "Panel Datum " + index, new Vector2(size.x * 0.5f - 18f - index * 12f, -size.y * 0.5f + 12f), new Vector2(index == 0 ? 7f : 4f, 4f), new Color(accent.r, accent.g, accent.b, 0.55f - index * 0.12f));
            }
            return rect;
        }

        private void BuildTutorial(RectTransform parent)
        {
            var tutorialObject = new GameObject("First Contact Tutorial");
            var rect = tutorialObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = new Vector2(0f, -42f);
            rect.sizeDelta = new Vector2(760f, 218f);
            var backing = tutorialObject.AddComponent<CosmicPanelGraphic>();
            backing.CornerCut = 24f;
            backing.color = new Color(0.004f, 0.015f, 0.032f, 0.72f);
            backing.raycastTarget = false;
            _tutorialGroup = tutorialObject.AddComponent<CanvasGroup>();
            _tutorialGroup.alpha = 0f;

            CreateImage(rect, "Tutorial Cyan Header", new Vector2(-184f, 104f), new Vector2(360f, 3f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.88f));
            CreateImage(rect, "Tutorial Amber Header", new Vector2(184f, 104f), new Vector2(360f, 3f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.78f));
            CreateImage(rect, "Left Instrument Rail", new Vector2(-364f, 1f), new Vector2(2f, 132f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.5f));
            CreateImage(rect, "Right Instrument Rail", new Vector2(364f, 1f), new Vector2(2f, 132f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.44f));
            CreateImage(rect, "Left Signal Divider", new Vector2(-124f, -7f), new Vector2(1f, 112f), new Color(0.38f, 0.64f, 0.82f, 0.2f));
            CreateImage(rect, "Right Signal Divider", new Vector2(124f, -7f), new Vector2(1f, 112f), new Color(0.72f, 0.52f, 0.84f, 0.18f));

            for (var index = 0; index < 5; index++)
            {
                var y = 50f - index * 25f;
                CreateImage(rect, "Cyan Telemetry Tick " + index, new Vector2(-356f, y), new Vector2(index == 2 ? 18f : 9f, 2f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.5f - index * 0.055f));
                CreateImage(rect, "Amber Telemetry Tick " + index, new Vector2(356f, y), new Vector2(index == 2 ? 18f : 9f, 2f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.45f - index * 0.05f));
            }

            CreateHudArc(rect, "Dual Link Outer Dial", new Vector2(0f, 18f), 43f, 192f, 348f, 14, CosmicPalette.Violet, 0.38f);
            CreateHudArc(rect, "Dual Link Inner Dial", new Vector2(0f, 18f), 34f, 204f, 336f, 10, CosmicPalette.Cyan, 0.2f);
            CreateImage(rect, "Cyan Link Bus", new Vector2(-30f, 18f), new Vector2(38f, 3f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.78f));
            CreateImage(rect, "Amber Link Bus", new Vector2(30f, 18f), new Vector2(38f, 3f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.74f));
            var linkCore = CreateImage(rect, "Violet Link Core", new Vector2(0f, 18f), new Vector2(13f, 13f), CosmicPalette.Violet);
            linkCore.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var protocolId = CreateText(rect, "Protocol Id", new Vector2(-302f, 76f), new Vector2(118f, 18f), 11, TextAnchor.MiddleLeft, CosmicPalette.Cyan);
            protocolId.fontStyle = FontStyle.Normal;
            protocolId.text = "A-07 / ONLINE";
            var syncState = CreateText(rect, "Sync State", new Vector2(302f, 76f), new Vector2(118f, 18f), 11, TextAnchor.MiddleRight, CosmicPalette.Amber);
            syncState.fontStyle = FontStyle.Normal;
            syncState.text = "LINK / ARMED";
            _tutorialTitle = CreateText(rect, "Tutorial Title", new Vector2(0f, 76f), new Vector2(420f, 30f), 17, TextAnchor.MiddleCenter, CosmicPalette.Silver);
            _tutorialTitle.fontStyle = FontStyle.Normal;
            _tutorialTitle.text = "ANOMALY CONTAINMENT PROTOCOL";
            CreateTutorialSignal(rect, -238f, CosmicPalette.Cyan, "LEFT HAND", "CUT CYAN");
            CreateTutorialSignal(rect, 0f, CosmicPalette.Violet, "DUAL LINK", "BOTH HANDS");
            CreateTutorialSignal(rect, 238f, CosmicPalette.Amber, "RIGHT HAND", "CUT AMBER");
            CreateImage(rect, "Protocol Footer Rail", new Vector2(0f, -71f), new Vector2(604f, 1f), new Color(0.42f, 0.68f, 0.86f, 0.24f));
            CreateImage(rect, "Protocol Footer Cyan", new Vector2(-326f, -71f), new Vector2(42f, 2f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.68f));
            CreateImage(rect, "Protocol Footer Amber", new Vector2(326f, -71f), new Vector2(42f, 2f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.62f));
            _tutorialBody = CreateText(rect, "Tutorial Body", new Vector2(0f, -88f), new Vector2(680f, 24f), 13, TextAnchor.MiddleCenter, CosmicPalette.Silver);
            _tutorialBody.fontStyle = FontStyle.Normal;
            _tutorialBody.text = "SECOND BLADE WITHIN 0.9 SEC  //  WRONG SIGNAL OR MISS COSTS 1 LIFE";
        }

        private void CreateTutorialSignal(RectTransform parent, float x, Color color, string label, string action)
        {
            var halo = CreateImage(parent, label + " Signal Halo", new Vector2(x, 22f), new Vector2(29f, 29f), new Color(color.r, color.g, color.b, 0.2f));
            halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var marker = CreateImage(parent, label + " Signal", new Vector2(x, 22f), new Vector2(17f, 17f), color);
            marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var title = CreateText(parent, label + " Title", new Vector2(x, -12f), new Vector2(190f, 24f), 13, TextAnchor.MiddleCenter, CosmicPalette.Silver);
            title.fontStyle = FontStyle.Normal;
            title.text = label;
            var command = CreateText(parent, label + " Command", new Vector2(x, -44f), new Vector2(196f, 30f), 18, TextAnchor.MiddleCenter, color);
            command.text = action;
        }

        private void BuildRuleStrip(RectTransform parent)
        {
            var strip = new GameObject("Signal Key").AddComponent<RectTransform>();
            strip.SetParent(parent, false);
            strip.anchoredPosition = new Vector2(0f, -435f);
            strip.sizeDelta = new Vector2(860f, 58f);
            _ruleStripRoot = strip;
            var stripBacking = strip.gameObject.AddComponent<CosmicPanelGraphic>();
            stripBacking.CornerCut = 14f;
            stripBacking.color = new Color(0.004f, 0.016f, 0.032f, 0.48f);
            stripBacking.raycastTarget = false;
            CreateImage(strip, "Signal Key Header", new Vector2(0f, 27f), new Vector2(740f, 2f), new Color(0.42f, 0.74f, 0.92f, 0.38f));

            CreateSignalRule(strip, -270f, CosmicPalette.Cyan, "LEFT", "CYAN");
            CreateSignalRule(strip, 0f, CosmicPalette.Violet, "DUAL", "BOTH");
            CreateSignalRule(strip, 270f, CosmicPalette.Amber, "RIGHT", "AMBER");
        }

        private void CreateSignalRule(RectTransform parent, float x, Color color, string label, string action)
        {
            var marker = CreateImage(parent, label + " Marker", new Vector2(x - 76f, 0f), new Vector2(13f, 13f), color);
            marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var title = CreateText(parent, label + " Label", new Vector2(x + 4f, 11f), new Vector2(150f, 18f), 11, TextAnchor.MiddleLeft, CosmicPalette.Silver);
            title.fontStyle = FontStyle.Normal;
            title.text = label;
            var rule = CreateText(parent, label + " Rule", new Vector2(x + 4f, -10f), new Vector2(150f, 22f), 15, TextAnchor.MiddleLeft, color);
            rule.text = action;
        }

        private void BuildResultPanel(RectTransform parent)
        {
            var resultObject = new GameObject("Result Debrief");
            var rect = resultObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(680f, 470f);
            var panel = resultObject.AddComponent<CosmicPanelGraphic>();
            panel.CornerCut = 30f;
            panel.color = new Color(0.003f, 0.014f, 0.03f, 0.9f);
            panel.raycastTarget = false;
            _resultGroup = resultObject.AddComponent<CanvasGroup>();
            _resultGroup.alpha = 0f;

            CreateImage(rect, "Debrief Cyan Header", new Vector2(-158f, 224f), new Vector2(300f, 4f), new Color(CosmicPalette.Cyan.r, CosmicPalette.Cyan.g, CosmicPalette.Cyan.b, 0.94f));
            CreateImage(rect, "Debrief Amber Header", new Vector2(158f, 224f), new Vector2(300f, 4f), new Color(CosmicPalette.Amber.r, CosmicPalette.Amber.g, CosmicPalette.Amber.b, 0.86f));
            CreateImage(rect, "Debrief Violet Core", new Vector2(0f, 162f), new Vector2(190f, 2f), new Color(CosmicPalette.Violet.r, CosmicPalette.Violet.g, CosmicPalette.Violet.b, 0.62f));
            CreateHudArc(rect, "Debrief Orbit", new Vector2(0f, 133f), 68f, 204f, 336f, 14, CosmicPalette.Violet, 0.42f);
            _resultText = CreateText(rect, "Result Text", new Vector2(0f, -16f), new Vector2(570f, 380f), 31, TextAnchor.MiddleCenter, Color.white);
            _resultText.lineSpacing = 1.18f;
            _resultText.text = string.Empty;
        }

        private void UpdateHudFacing(float deltaTime)
        {
            // Mission console transforms only with its world parent. Head rotation must never
            // drag the game UI into the player's face.
        }

        private void SetCoreHudVisible(bool visible)
        {
            if (_hudFrameRoot != null) _hudFrameRoot.gameObject.SetActive(visible);
            if (_scorePanelRoot != null) _scorePanelRoot.gameObject.SetActive(visible);
            if (_timerPanelRoot != null) _timerPanelRoot.gameObject.SetActive(visible);
            if (_lifePanelRoot != null) _lifePanelRoot.gameObject.SetActive(visible);
            if (_ruleStripRoot != null) _ruleStripRoot.gameObject.SetActive(visible);
        }

        private static void PlaceAtScreenCorner(RectTransform rect, bool left, float horizontalMargin, float verticalMargin)
        {
            rect.anchorMin = new Vector2(left ? 0f : 1f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(left ? 0f : 1f, 1f);
            rect.anchoredPosition = new Vector2(left ? horizontalMargin : -horizontalMargin, -verticalMargin);
        }

        private void SetTutorialVisible(bool visible)
        {
            if (_tutorialGroup == null)
            {
                return;
            }
            _tutorialGroup.alpha = visible ? 1f : 0f;
            _tutorialGroup.blocksRaycasts = false;
        }

        private void UpdateHud()
        {
            _scoreText.text = _score.ToString("000000");
            _comboText.text = _overdriveTimer > 0f ? $"OVERDRIVE  {_overdriveTimer:0.0}" : $"CHAIN {_combo:00}";
            var seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
            _timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            _timerText.color = _timeRemaining < 10f ? CosmicPalette.Red : Color.white;
            _livesText.text = $"LIFE LINK  /  {_lives} OF {MaxLives}";
            for (var index = 0; index < _lifePips.Length; index++)
            {
                if (_lifePips[index] == null)
                {
                    continue;
                }
                var active = index < _lives;
                _lifePips[index].color = active ? Color.white : new Color(0.18f, 0.2f, 0.24f, 0.52f);
                _lifePips[index].rectTransform.localScale = Vector3.Lerp(
                    _lifePips[index].rectTransform.localScale,
                    active ? Vector3.one : Vector3.one * 0.72f,
                    1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            }
        }

        private void SetEvent(string text, Color color, float duration)
        {
            _eventText.text = text;
            color.a = 1f;
            _eventText.color = color;
            _eventTimer = duration;
        }

        private Text CreateText(RectTransform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            var gameObject = new GameObject(name);
            var rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = gameObject.AddComponent<Text>();
            text.font = _uiFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static Image CreateImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var gameObject = new GameObject(name);
            var rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
