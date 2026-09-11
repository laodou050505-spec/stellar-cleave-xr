using UnityEngine;
using UnityEngine.UI;

namespace CosmicSlice
{
    // THESIS: One quiet emerald console language connects launch, play and debrief.
    // OWN-WORLD: Pine-black surfaces, slender coral corners, white type and sparse CRT grain.
    // STORY: Locate a real ceiling, understand four signals, play, then read a clear result.
    // FIRST VIEWPORT: Inset score/time, compact lives, an open play lane and a calm ready console.
    // FORM: Extends the user's approved nebula cover; color marks rules, never entire HUD cards.
    // FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and DESIGN.md
    public sealed partial class CosmicSliceGame
    {
        private Font _uiFont;
        private RectTransform _hudFrameRoot, _scorePanelRoot, _timerPanelRoot, _lifePanelRoot, _ruleStripRoot;
        private RectTransform _statusPanelRoot, _eventPanelRoot;
        private Text _ceilingStateText, _resultRankText, _resultScoreText, _resultDetailText;
        private Texture2D _consoleNebula;
        private static readonly Color ConsoleMuted = new(0.59f, 0.75f, 0.68f, 1f);

        private void BuildHud(Transform missionConsoleRoot, bool includeCover = true)
        {
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("Stellar Cleave World Console").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 24;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1760f, 990f);
            rect.SetParent(missionConsoleRoot, false);
            rect.localScale = Vector3.one * CosmicSliceDesign.MissionConsoleScale;
            _hudRoot = rect;
            if (includeCover) { rect.localScale *= 1.35f; BuildCover(rect); return; }
            _consoleNebula = Resources.Load<Texture2D>("CosmicSliceMenu/Cover");
            BuildHudFrame(rect);
            _scorePanelRoot = CreateConsolePanel(rect, "Score Instrument", new Vector2(-560f, 337f), new Vector2(320f, 154f));
            ConsoleText(_scorePanelRoot, "Score Label", "SCORE", new Vector2(0f, 47f), new Vector2(260f, 28f), 21, ConsoleMuted);
            _scoreText = ConsoleText(_scorePanelRoot, "Score", "000000", new Vector2(0f, 0f), new Vector2(260f, 64f), 54, MenuPaper);
            _comboText = ConsoleText(_scorePanelRoot, "Chain", "CHAIN 00", new Vector2(0f, -48f), new Vector2(260f, 28f), 22, MenuAccent);
            _timerPanelRoot = CreateConsolePanel(rect, "Timer Instrument", new Vector2(560f, 337f), new Vector2(320f, 154f));
            ConsoleText(_timerPanelRoot, "Timer Label", "TIME REMAINING", new Vector2(0f, 47f), new Vector2(260f, 28f), 21, ConsoleMuted, TextAnchor.MiddleRight);
            _timerText = ConsoleText(_timerPanelRoot, "Time", "01:15", new Vector2(0f, 0f), new Vector2(260f, 64f), 54, MenuPaper, TextAnchor.MiddleRight);
            ConsoleText(_timerPanelRoot, "Round Label", "ANOMALY PROTOCOL", new Vector2(0f, -48f), new Vector2(260f, 28f), 20, ConsoleMuted, TextAnchor.MiddleRight);
            _lifePanelRoot = CreateConsolePanel(rect, "Life Link Instrument", new Vector2(-560f, 215f), new Vector2(320f, 64f));
            _livesText = ConsoleText(_lifePanelRoot, "Lives Label", "LIVES  3 / 3", new Vector2(-40f, 0f), new Vector2(210f, 32f), 24, MenuPaper);
            for (var i = 0; i < _lifePips.Length; i++)
            {
                _lifePips[i] = CreateImage(_lifePanelRoot, "Life Node " + i, new Vector2(66f + i * 29f, 0f), new Vector2(9f, 20f), MenuAccent);
                _lifePips[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            }
            _statusPanelRoot = CreateConsolePanel(rect, "Ceiling Status", new Vector2(0f, 202f), new Vector2(730f, 98f));
            _statusText = ConsoleText(_statusPanelRoot, "Status", "", Vector2.zero, new Vector2(670f, 82f), 27, MenuPaper, TextAnchor.MiddleCenter);
            BuildTutorial(rect);
            _eventPanelRoot = CreateConsolePanel(rect, "Event Notice", new Vector2(0f, -334f), new Vector2(690f, 52f));
            _eventText = ConsoleText(_eventPanelRoot, "Event", "", Vector2.zero, new Vector2(630f, 38f), 23, MenuPaper, TextAnchor.MiddleCenter);
            BuildRuleStrip(rect);
            BuildResultPanel(rect);
            _flashImage = CreateImage(rect, "Impact Flash", Vector2.zero, rect.sizeDelta, Color.clear);
            _flashImage.raycastTarget = false;
            _flashImage.transform.SetAsLastSibling();
        }

        private Text ConsoleText(RectTransform parent, string name, string copy, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = CreateText(parent, name, position, size, fontSize, alignment, color);
            text.fontStyle = FontStyle.Normal;
            text.lineSpacing = 1.12f;
            text.text = copy;
            text.GetComponent<Shadow>().enabled = false;
            return text;
        }

        private RectTransform CreateConsolePanel(RectTransform parent, string name, Vector2 position, Vector2 size, bool nebula = false)
        {
            var rect = NewMenuRect(parent, name, position, size);
            var body = rect.gameObject.AddComponent<CosmicPanelGraphic>();
            body.CornerCut = 9f;
            body.color = new Color(MenuInk.r, MenuInk.g, MenuInk.b, 0.97f);
            body.raycastTarget = false;
            if (nebula && _consoleNebula != null)
            {
                var art = NewMenuRect(rect, "Quiet Nebula Grain", Vector2.zero, size - Vector2.one * 26f).gameObject.AddComponent<RawImage>();
                art.texture = _consoleNebula;
                // A title-free strip of the existing approved cover; no additional image asset.
                art.uvRect = new Rect(0.035f, 0.20f, 0.07f, 0.58f);
                art.color = new Color(0.55f, 0.72f, 0.62f, 0.16f);
                art.raycastTarget = false;
            }
            var rim = new Color(MenuAccent.r, MenuAccent.g, MenuAccent.b, 0.7f);
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * (size.x * 0.5f - 10f);
                var y = -side * (size.y * 0.5f - 10f);
                CreateImage(rect, "Coral Corner Horizontal", new Vector2(x - side * 13f, y), new Vector2(26f, 1.5f), rim);
                CreateImage(rect, "Coral Corner Vertical", new Vector2(x, y + side * 8f), new Vector2(1.5f, 16f), rim);
            }
            return rect;
        }

        private void BuildHudFrame(RectTransform parent)
        {
            _hudFrameRoot = NewMenuRect(parent, "World Console Identity", Vector2.zero, parent.sizeDelta);
            ConsoleText(_hudFrameRoot, "Brand", "STELLAR CLEAVE", new Vector2(-490f, 453f), new Vector2(460f, 30f), 22, MenuAccent);
            _ceilingStateText = ConsoleText(_hudFrameRoot, "Ceiling Mode", "CEILING NOT MAPPED", new Vector2(440f, 453f), new Vector2(560f, 30f), 21, ConsoleMuted, TextAnchor.MiddleRight);
        }

        private void BuildTutorial(RectTransform parent)
        {
            var rect = CreateConsolePanel(parent, "First Contact Tutorial", new Vector2(0f, -69f), new Vector2(940f, 382f), true);
            _tutorialGroup = rect.gameObject.AddComponent<CanvasGroup>();
            _tutorialGroup.alpha = 0f;
            _tutorialTitle = ConsoleText(rect, "Ready Heading", "TWO BLADES. FOUR SIGNALS.", new Vector2(0f, 126f), new Vector2(824f, 44f), 32, MenuPaper);
            var xs = new[] { -324f, -108f, 108f, 324f };
            var titles = new[] { "CYAN", "AMBER", "VIOLET", "RED" };
            var bodies = new[] { "Left blade", "Right blade", "Both hands", "Do not cut" };
            var colors = new[] { CosmicPalette.Cyan, CosmicPalette.Amber, new Color(0.78f, 0.52f, 1f), new Color(1f, 0.38f, 0.32f) };
            for (var i = 0; i < 4; i++)
            {
                ConsoleText(rect, titles[i], titles[i], new Vector2(xs[i], 47f), new Vector2(175f, 30f), 23, colors[i]);
                ConsoleText(rect, titles[i] + " Rule", bodies[i], new Vector2(xs[i], 12f), new Vector2(175f, 34f), 25, MenuPaper);
            }
            _tutorialBody = ConsoleText(rect, "Dual Timing", "Violet: alternate hands within 0.9s.\nWrong hand or missed non-red signal: lose 1 life.", new Vector2(0f, -45f), new Vector2(824f, 64f), 24, ConsoleMuted);
            CreateImage(rect, "Ready Divider", new Vector2(0f, -87f), new Vector2(824f, 1f), new Color(0.22f, 0.42f, 0.34f, 0.8f));
            BuildStageButton(rect, "START GAME", StageAction.StartOrPreview, new Vector2(252f, -133f), 290f);
            BuildStageButton(rect, "SCAN ROOM", StageAction.Rescan, new Vector2(-252f, -133f), 260f);
            rect.gameObject.SetActive(false);
        }

        private void BuildRuleStrip(RectTransform parent)
        {
            _ruleStripRoot = CreateConsolePanel(parent, "Signal Key", new Vector2(0f, -428f), new Vector2(800f, 54f));
            ConsoleText(_ruleStripRoot, "Left Rule", "LEFT / CYAN", new Vector2(-252f, 0f), new Vector2(224f, 30f), 22, CosmicPalette.Cyan);
            ConsoleText(_ruleStripRoot, "Dual Rule", "DUAL / 0.9s", new Vector2(0f, 0f), new Vector2(224f, 30f), 22, new Color(0.78f, 0.52f, 1f), TextAnchor.MiddleCenter);
            ConsoleText(_ruleStripRoot, "Right Rule", "RIGHT / AMBER", new Vector2(252f, 0f), new Vector2(224f, 30f), 22, CosmicPalette.Amber, TextAnchor.MiddleRight);
        }

        private void BuildResultPanel(RectTransform parent)
        {
            var rect = CreateConsolePanel(parent, "Result Debrief", new Vector2(0f, 18f), new Vector2(920f, 610f), true);
            _resultGroup = rect.gameObject.AddComponent<CanvasGroup>();
            _resultGroup.alpha = 0f;
            _resultText = ConsoleText(rect, "Debrief Heading", "", new Vector2(0f, 218f), new Vector2(784f, 58f), 39, MenuPaper);
            CreateImage(rect, "Debrief Divider", new Vector2(0f, 151f), new Vector2(784f, 1f), new Color(0.27f, 0.46f, 0.37f, 0.7f));
            _resultRankText = ConsoleText(rect, "Rank", "", new Vector2(-235f, 57f), new Vector2(310f, 120f), 92, MenuAccent);
            ConsoleText(rect, "Rank Caption", "RANK", new Vector2(-235f, -13f), new Vector2(310f, 32f), 22, ConsoleMuted);
            _resultScoreText = ConsoleText(rect, "Final Score", "", new Vector2(159f, 70f), new Vector2(466f, 80f), 64, MenuPaper, TextAnchor.MiddleRight);
            ConsoleText(rect, "Score Caption", "FINAL SCORE", new Vector2(159f, 3f), new Vector2(466f, 36f), 22, ConsoleMuted, TextAnchor.MiddleRight);
            _resultDetailText = ConsoleText(rect, "Round Detail", "", new Vector2(0f, -81f), new Vector2(784f, 60f), 26, MenuPaper);
            CreateImage(rect, "Debrief Footer Rule", new Vector2(0f, -144f), new Vector2(784f, 1f), new Color(0.27f, 0.46f, 0.37f, 0.7f));
            BuildStageButton(rect, "PLAY AGAIN", StageAction.Rescan, new Vector2(229f, -220f), 304f);
            BuildStageButton(rect, "EXIT", StageAction.Exit, new Vector2(-251f, -220f), 280f);
            rect.gameObject.SetActive(false);
        }

        private void UpdateHudFacing(float deltaTime) { }
        private void SetCoreHudVisible(bool visible)
        {
            if (_hudFrameRoot != null) _hudFrameRoot.gameObject.SetActive(visible);
            if (_scorePanelRoot != null) _scorePanelRoot.gameObject.SetActive(visible);
            if (_timerPanelRoot != null) _timerPanelRoot.gameObject.SetActive(visible);
            if (_lifePanelRoot != null) _lifePanelRoot.gameObject.SetActive(visible);
            if (_ruleStripRoot != null) _ruleStripRoot.gameObject.SetActive(visible && _state == GameState.Playing);
        }
        private void SetTutorialVisible(bool visible)
        {
            if (_tutorialGroup == null) return;
            _tutorialGroup.alpha = visible ? 1f : 0f;
            _tutorialGroup.gameObject.SetActive(visible);
            _tutorialGroup.blocksRaycasts = false;
        }
        private void RefreshConsolePresentation()
        {
            if (_statusPanelRoot != null) _statusPanelRoot.gameObject.SetActive(!string.IsNullOrEmpty(_statusText.text) && _state != GameState.Results);
            if (_eventPanelRoot != null) _eventPanelRoot.gameObject.SetActive(!string.IsNullOrEmpty(_eventText.text) && _eventText.color.a > 0.01f && _state != GameState.Results);
            if (_ceilingStateText != null)
            {
                _ceilingStateText.text = _hasDetectedCeiling ? "CEILING ANCHORED" : _ceilingPreviewSelected ? "PREVIEW / CEILING NOT MAPPED" : "CEILING NOT MAPPED";
                _ceilingStateText.color = _hasDetectedCeiling ? ConsoleMuted : MenuAccent;
            }
            if (_stageButtons.Count > 0 && _stageButtons[0].Control.Label != null)
                _stageButtons[0].Control.Label.text = CanUsePortal ? "START GAME" : _ceilingScanComplete ? "TRY PREVIEW" : "SCANNING...";
        }
        private void UpdateHud()
        {
            _scoreText.text = _score.ToString("000000");
            _comboText.text = _overdriveTimer > 0f ? $"OVERDRIVE {_overdriveTimer:0.0}" : $"CHAIN {_combo:00}";
            var seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
            _timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            _timerText.color = _timeRemaining < 10f ? MenuAccent : MenuPaper;
            _livesText.text = $"LIVES  {_lives} / {MaxLives}";
            for (var i = 0; i < _lifePips.Length; i++)
                if (_lifePips[i] != null) _lifePips[i].color = i < _lives ? MenuAccent : new Color(0.20f, 0.31f, 0.26f, 1f);
        }
        private void SetEvent(string text, Color color, float duration)
        {
            if (_eventText == null) return;
            _eventText.text = text;
            _eventText.color = MenuPaper;
            _eventTimer = duration;
            if (_eventPanelRoot != null) _eventPanelRoot.gameObject.SetActive(!string.IsNullOrEmpty(text));
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
