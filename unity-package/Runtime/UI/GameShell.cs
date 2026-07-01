using System;
using System.Collections.Generic;
using UnityEngine;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // The engine-owned game shell: the full standard experience around the EventEngine - load + validate,
    // main menu / pause / Settings (music + sound) / confirm, save & continue, music orchestration, the
    // branded loading reveal, the end screen, and the data-error overlay. It supports both formats: Reigns
    // (a continuous deck) and Journey (a node map). A game's GameBootstrap is now just declarative wiring:
    // fill a Config (content + Theme + the UI refs from the scene) and call Run(). Improving the shell here
    // upgrades every game on the next engine version bump - no per-game shell code to copy and drift.
    public sealed class GameShell
    {
        public enum Format { Reigns, Journey }

        public sealed class Config
        {
            // Content (per game)
            public TextAsset storyJson;
            public TextAsset mapJson;          // Journey only
            public ResourceSet resources;
            public Theme theme;
            public int seed = 12345;
            public Format format = Format.Reigns;

            // Opening / menu text
            public string title = "Crossroads";
            public string intro = "A short story of choices. Swipe left or right to decide.";
            public string loadingCaption = "";

            // UI (from Crossroads.UI; all optional - null-safe)
            public CardView cardView;
            public ResourceBarView resourceBar;
            public SwipeInput swipeInput;
            public EndScreen endScreen;
            public MessageOverlay messageOverlay;
            public MenuOverlay menu;
            public PauseButton pauseButton;
            public AudioDirector audioDirector;
            public LoadingScreen loadingScreen;
            public MapView mapView;            // Journey only

            // Optional gameplay hooks a game injects (uniform wiring - preferred over scene GetComponent).
            public ICardChoiceFeedback choiceFeedback;   // custom choice-selection effect; null = engine default
        }

        private Config _c;
        private EventEngine _engine;
        private StoryData _story;
        private MapData _map;
        private MapGraph _mapSource;
        private ChoiceSide? _previewSide;

        private bool IsJourney => _c.format == Format.Journey;
        private bool SavesEnabled => _c.format == Format.Reigns;   // Journey has no save/resume in this slice

        public void Run(Config config)
        {
            _c = config;

            if (_c.storyJson == null || _c.resources == null ||
                (IsJourney && _c.mapJson == null))
            {
                Debug.LogError("[Crossroads] Missing content (story/resources" + (IsJourney ? "/map" : "") + ").");
                return;
            }

            _story = StoryLoader.Parse(_c.storyJson.text);
            if (IsJourney) _map = MapLoader.Parse(_c.mapJson.text);

            // Mandatory validation before entering the loop (M9). An error stops cleanly with a message.
            var issues = StoryValidator.Validate(_story, _c.resources);
            if (IsJourney) issues.AddRange(MapValidator.Validate(_story, _map));
            foreach (var issue in issues) Debug.LogWarning($"[Crossroads] {issue}");
            var errors = issues.FindAll(i => i.Severity == IssueSeverity.Error);
            if (errors.Count > 0)
            {
                Debug.LogError("[Crossroads] story validation failed - aborting load.");
                if (_c.messageOverlay != null) _c.messageOverlay.Show("Data Error", BuildErrorText(errors), null, null);
                return;
            }

            UIFonts.RightToLeft = _c.theme != null && _c.theme.rightToLeft;   // Hebrew/RTL before building the UI
            UIFonts.UseThemeFont(_c.theme);
            if (_c.resourceBar != null) _c.resourceBar.SetTheme(_c.theme);
            if (_c.cardView != null && _c.choiceFeedback != null) _c.cardView.SetChoiceFeedback(_c.choiceFeedback);
            if (_c.menu != null) _c.menu.SetTheme(_c.theme);
            if (_c.endScreen != null) _c.endScreen.SetTheme(_c.theme);
            if (_c.swipeInput != null)
            {
                _c.swipeInput.OnCommit += HandleCommit;
                _c.swipeInput.OnPreview += HandlePreview;
                _c.swipeInput.OnCancel += HandleCancel;
                _c.swipeInput.OnMenu += OpenPause;
            }
            if (_c.pauseButton != null) _c.pauseButton.OnPressed += OpenPause;
            if (_c.mapView != null) _c.mapView.OnSelect += HandleSelect;
            if (_c.audioDirector != null && _c.theme != null) _c.audioDirector.ConfigureUiClick(_c.theme.clickSfx);

            // Branded loading reveal first (if wired), then the title screen.
            if (_c.loadingScreen != null)
            {
                _c.loadingScreen.SetTheme(_c.theme);
                _c.loadingScreen.SetCaption(_c.loadingCaption);
                _c.loadingScreen.Run(ShowMainMenu);
            }
            else ShowMainMenu();
        }

        // Title screen. Continue resumes a valid save (Reigns); New Game starts fresh (confirming an
        // overwrite when a save exists). With no menu wired, fall back to auto resume-or-fresh.
        private void ShowMainMenu()
        {
            if (_c.menu != null) _c.menu.Hide();
            if (_c.endScreen != null) _c.endScreen.Hide();
            if (_c.pauseButton != null) _c.pauseButton.SetVisible(false);
            if (_c.mapView != null) _c.mapView.Hide();
            ClearCurrent();

            // No main-menu wired in the scene: fall back to a simple intro screen with a Start button
            // (or straight in if there's no overlay either). This preserves the lighter journey opening.
            if (_c.menu == null)
            {
                if (_c.messageOverlay != null) _c.messageOverlay.Show(_c.title, _c.intro, "Start", Begin);
                else Begin();
                return;
            }

            bool hasSave = SavesEnabled && SaveSystem.Load() != null;
            var items = new List<MenuOverlay.MenuItem>();
            if (hasSave) items.Add(new MenuOverlay.MenuItem("Continue", ContinueRun, true));
            items.Add(new MenuOverlay.MenuItem("New Game", hasSave ? (Action)ConfirmNewGame : StartRun, !hasSave));
            items.Add(new MenuOverlay.MenuItem("Settings", () => ShowSettings(ShowMainMenu)));
            items.Add(new MenuOverlay.MenuItem("Quit", QuitApp));
            if (_c.audioDirector != null && _c.theme != null)
                _c.audioDirector.PlayMusic(_c.theme.musicMenu != null ? _c.theme.musicMenu : _c.theme.music);
            _c.menu.Show(_c.title, _c.intro, items, true);   // useLogo: title wordmark if the theme has one
        }

        private void ConfirmNewGame()
        {
            _c.menu.Show("Start over?", "Your saved progress will be lost.", new[]
            {
                new MenuOverlay.MenuItem("Yes, start over", StartRun, true),
                new MenuOverlay.MenuItem("Back", ShowMainMenu)
            });
        }

        // No-menu fallback: resume a valid save (Reigns), else a fresh run.
        private void Begin()
        {
            if (_c.messageOverlay != null) _c.messageOverlay.Hide();
            if (SavesEnabled)
            {
                var resumed = EventEngine.Resume(_story, _c.resources, new Deck(_story), SaveSystem.Load());
                if (resumed != null) { BeginRun(resumed); return; }
            }
            StartRun();
        }

        private void ContinueRun()
        {
            var resumed = EventEngine.Resume(_story, _c.resources, new Deck(_story), SaveSystem.Load());
            if (resumed != null) BeginRun(resumed);
            else StartRun();   // save vanished/incompatible -> fresh
        }

        // A fresh run. Also the Restart path (end screen / pause menu).
        private void StartRun()
        {
            if (SavesEnabled) SaveSystem.Delete();   // clear any old save on every fresh start
            IEventSource source;
            if (IsJourney) { _mapSource = new MapGraph(_story, _map); source = _mapSource; }
            else source = new Deck(_story);
            BeginRun(new EventEngine(_story, _c.resources, source, _c.seed));
        }

        // Uniform event wiring for any engine instance (fresh or resumed) + initial render.
        private void BeginRun(EventEngine engine)
        {
            _engine = engine;
            _engine.OnGameOver += HandleGameOver;
            if (_c.endScreen != null) _c.endScreen.Hide();
            if (_c.menu != null) _c.menu.Hide();
            if (_c.pauseButton != null) _c.pauseButton.SetVisible(true);
            if (_c.audioDirector != null && _c.theme != null) _c.audioDirector.PlayMusic(_c.theme.music);
            ShowCard();   // Current is the start node
        }

        // Pause: reachable mid-run via Esc or the pause button. The save (Reigns) stays on disk so the
        // player can leave to the main menu and Continue later.
        private void OpenPause()
        {
            if (_c.menu == null || _c.menu.IsShown) return;
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            _previewSide = null;
            _c.menu.Show("Paused", null, new[]
            {
                new MenuOverlay.MenuItem("Resume", null, true),   // Invoke() hides the menu; that is the resume
                new MenuOverlay.MenuItem("Restart", StartRun),
                new MenuOverlay.MenuItem("Settings", () => ShowSettings(OpenPause)),
                new MenuOverlay.MenuItem("Main Menu", ShowMainMenu)
            });
        }

        // Settings sub-menu: music + sound toggles (persisted in PlayerPrefs via AudioDirector), plus the
        // version + anonymous player id. Toggles flip the flag and re-show with the updated label.
        private void ShowSettings(Action back)
        {
            if (_c.menu == null) { back?.Invoke(); return; }
            bool music = _c.audioDirector == null || _c.audioDirector.MusicEnabled;
            bool sfx = _c.audioDirector == null || _c.audioDirector.SfxEnabled;
            string info = "Version " + Application.version + "      Player " + PlayerId.Short;
            _c.menu.Show("Settings", info, new[]
            {
                new MenuOverlay.MenuItem("Music: " + (music ? "On" : "Off"),
                    () => { _c.audioDirector?.SetMusicEnabled(!music); ShowSettings(back); }, true),
                new MenuOverlay.MenuItem("Sound: " + (sfx ? "On" : "Off"),
                    () => { _c.audioDirector?.SetSfxEnabled(!sfx); ShowSettings(back); }),
                new MenuOverlay.MenuItem("Back", back)
            });
        }

        private void QuitApp()
        {
            if (_c.loadingScreen != null) _c.loadingScreen.ShowBlackout();
            Application.Quit();   // no-op in the editor, harmless
        }

        private bool MenuBlocking => _c.menu != null && _c.menu.IsShown;

        private void HandleCommit(ChoiceSide side)
        {
            if (MenuBlocking) return;
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            _previewSide = null;
            if (_c.audioDirector != null && _c.theme != null) _c.audioDirector.PlaySfx(_c.theme.swipeSfx);
            _engine.Resolve(side);              // apply the choice only

            if (_engine.Status != GameStatus.Running) return;   // Resolve itself ended the run -> HandleGameOver fired

            if (IsJourney)
            {
                ShowMap();                      // Journey: navigate via the map, not Advance
                return;
            }

            _engine.Advance();                  // Reigns: request the next event separately (may itself end)
            RenderCurrent();
            if (_engine.Status == GameStatus.Running) SaveSystem.Save(_engine.State);   // save-on-commit
        }

        private void HandlePreview(ChoiceSide side, float fraction)
        {
            if (MenuBlocking) return;
            if (_c.cardView != null) _c.cardView.ApplyDrag(side, fraction);    // per-frame, must stay smooth
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            if (_previewSide == side) return;   // skip per-frame churn while the side is unchanged
            _previewSide = side;
            var deltas = _engine.Preview(side).Deltas;
            if (_c.resourceBar != null) _c.resourceBar.ShowPreview(deltas);
            if (_c.cardView != null) _c.cardView.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, _c.resources, _c.theme), side);
        }

        private void HandleCancel()
        {
            _previewSide = null;
            if (_c.cardView != null) _c.cardView.ResetDrag();
            if (_c.resourceBar != null) _c.resourceBar.ClearPreview();
        }

        // Journey: after a choice is applied, return to the map to pick the next node.
        private void ShowMap()
        {
            if (_c.mapView != null) _c.mapView.Bind(_map, _engine.Current.Id, _mapSource.NeighborsOf(_engine.State));
        }

        // Journey: a node was chosen on the map -> enter it. Reaching the goal wins (OnGameOver).
        private void HandleSelect(string nodeId)
        {
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            if (_c.mapView != null) _c.mapView.Hide();
            _engine.EnterNode(nodeId);
            if (_engine.Status == GameStatus.Running) ShowCard();
        }

        private void ShowCard()
        {
            if (_c.mapView != null) _c.mapView.Hide();
            RenderCurrent();
        }

        private void RenderCurrent()
        {
            if (_c.cardView != null) _c.cardView.Bind(ViewMapper.BuildNodeView(_engine.Current), _c.theme);
            if (_c.resourceBar != null) _c.resourceBar.Bind(ViewMapper.BuildResourceViews(_engine.State, _c.resources, _c.theme));
            if (_c.audioDirector != null && _c.theme != null) _c.audioDirector.PlaySfx(_c.theme.cardSfx);
        }

        // Clears the card/meters behind the main menu so the title screen is not backed by stale content.
        private void ClearCurrent()
        {
            if (_c.cardView != null) _c.cardView.Bind(ViewMapper.BuildNodeView(null), _c.theme);
        }

        private void HandleGameOver(GameOverInfo info)
        {
            if (SavesEnabled) SaveSystem.Delete();   // do not resume into a finished run
            if (_c.pauseButton != null) _c.pauseButton.SetVisible(false);
            if (_c.mapView != null) _c.mapView.Hide();
            Debug.Log($"[Crossroads] Game over ({info.Reason}): {info.Text}");
            if (_c.endScreen != null)
                _c.endScreen.Show(info.Text, info.Image, StartRun, _c.menu != null ? (Action)ShowMainMenu : null);
        }

        private static string BuildErrorText(List<ValidationIssue> errors)
        {
            var sb = new System.Text.StringBuilder("The story data could not be loaded:\n\n");
            int shown = Math.Min(errors.Count, 6);
            for (int i = 0; i < shown; i++) sb.Append("- ").Append(errors[i].Message).Append('\n');
            if (errors.Count > shown) sb.Append("...and ").Append(errors.Count - shown).Append(" more.");
            return sb.ToString();
        }

        // Anonymous, stable per-install player id (for support / future log correlation). Kept in PlayerPrefs.
        private static class PlayerId
        {
            private const string Key = "cr.player.id";
            public static string Full
            {
                get
                {
                    string id = PlayerPrefs.GetString(Key, "");
                    if (string.IsNullOrEmpty(id))
                    {
                        id = System.Guid.NewGuid().ToString("N");
                        PlayerPrefs.SetString(Key, id); PlayerPrefs.Save();
                    }
                    return id;
                }
            }
            public static string Short => Full.Substring(0, 8);
        }
    }
}
