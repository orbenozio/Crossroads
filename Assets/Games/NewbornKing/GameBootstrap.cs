using System;
using System.Collections.Generic;
using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Game.NewbornKing
{
    // The single bootstrap that solders the three layers (spec 12.2): load Content -> inject into the
    // Engine -> wire up the UI. It writes no loop logic. Cloning = copy the game folder + swap
    // Content/Theme, without touching this code (M7).
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Content (per-game)")]
        [SerializeField] private TextAsset storyJson;
        [SerializeField] private ResourceSet resources;
        [SerializeField] private Theme theme;
        [SerializeField] private int seed = 12345;

        [Header("UI (from Crossroads.UI)")]
        [SerializeField] private CardView cardView;
        [SerializeField] private ResourceBarView resourceBar;
        [SerializeField] private SwipeInput swipeInput;
        [SerializeField] private EndScreen endScreen;
        [SerializeField] private MessageOverlay messageOverlay;   // data-error screen (spec 9.5)
        [SerializeField] private MenuOverlay menu;                // main menu + pause + confirm (spec 9.5)
        [SerializeField] private PauseButton pauseButton;         // pointer affordance to open the pause menu
        [SerializeField] private AudioDirector audioDirector;     // looping music + swipe SFX (clips from the theme)

        [Header("Title / menu text")]
        [SerializeField] private string title = "Crossroads";
        [SerializeField] [TextArea] private string intro = "A short story of choices. Swipe left or right to decide.";

        private EventEngine _engine;
        private StoryData _story;

        private void Start()
        {
            if (storyJson == null || resources == null)
            {
                Debug.LogError("[Crossroads] Missing content (storyJson/resources).");
                return;
            }

            _story = StoryLoader.Parse(storyJson.text);

            // Mandatory validation before entering the loop (spec 15.2, M9). An error stops cleanly.
            var issues = StoryValidator.Validate(_story, resources);
            foreach (var issue in issues) Debug.LogWarning($"[Crossroads] {issue}");
            var errors = issues.FindAll(i => i.Severity == IssueSeverity.Error);
            if (errors.Count > 0)
            {
                Debug.LogError("[Crossroads] story validation failed - aborting load.");
                if (messageOverlay != null) messageOverlay.Show("Data Error", BuildErrorText(errors), null, null);
                return;
            }

            UIFonts.RightToLeft = theme != null && theme.rightToLeft;   // Hebrew/RTL before building the UI (§10.6)
            UIFonts.UseThemeFont(theme);                                // per-game font (TMP type stays in the UI layer)
            if (resourceBar != null) resourceBar.SetTheme(theme);
            if (menu != null) menu.SetTheme(theme);
            if (swipeInput != null)
            {
                swipeInput.OnCommit += HandleCommit;
                swipeInput.OnPreview += HandlePreview;
                swipeInput.OnCancel += HandleCancel;
                swipeInput.OnMenu += OpenPause;        // Esc opens the pause menu
            }
            if (pauseButton != null) pauseButton.OnPressed += OpenPause;
            if (audioDirector != null && theme != null) audioDirector.ConfigureUiClick(theme.clickSfx);

            ShowMainMenu();
        }

        // Title screen (spec 9.5). Continue resumes a valid save; New Game starts fresh (confirming an
        // overwrite when a save exists). With no menu wired, fall back to the old auto resume-or-fresh.
        private void ShowMainMenu()
        {
            if (menu != null) menu.Hide();
            if (endScreen != null) endScreen.Hide();
            if (pauseButton != null) pauseButton.SetVisible(false);
            ClearCurrent();

            if (menu == null) { Begin(); return; }

            bool hasSave = SaveSystem.Load() != null;
            var items = new List<MenuOverlay.MenuItem>();
            if (hasSave) items.Add(new MenuOverlay.MenuItem("Continue", ContinueRun, true));
            items.Add(new MenuOverlay.MenuItem("New Game", hasSave ? (Action)ConfirmNewGame : StartRun, !hasSave));
            items.Add(new MenuOverlay.MenuItem("Quit", QuitApp));
            if (audioDirector != null && theme != null)
                audioDirector.PlayMusic(theme.musicMenu != null ? theme.musicMenu : theme.music);   // menu track
            menu.Show(title, intro, items, true);   // useLogo: title wordmark if the theme has one
        }

        private void ConfirmNewGame()
        {
            menu.Show("Start over?", "Your saved progress will be lost.", new[]
            {
                new MenuOverlay.MenuItem("Yes, start over", StartRun, true),
                new MenuOverlay.MenuItem("Back", ShowMainMenu)
            });
        }

        // load-on-start (J4/M5): resume a valid save, else a fresh run. Used as the no-menu fallback.
        private void Begin()
        {
            if (messageOverlay != null) messageOverlay.Hide();
            var resumed = EventEngine.Resume(_story, resources, new Deck(_story), SaveSystem.Load());
            if (resumed != null) BeginRun(resumed);
            else StartRun();
        }

        private void ContinueRun()
        {
            var resumed = EventEngine.Resume(_story, resources, new Deck(_story), SaveSystem.Load());
            if (resumed != null) BeginRun(resumed);
            else StartRun();   // save vanished/incompatible -> fresh
        }

        private static string BuildErrorText(List<ValidationIssue> errors)
        {
            var sb = new System.Text.StringBuilder("The story data could not be loaded:\n\n");
            int shown = Math.Min(errors.Count, 6);
            for (int i = 0; i < shown; i++) sb.Append("- ").Append(errors[i].Message).Append('\n');
            if (errors.Count > shown) sb.Append("...and ").Append(errors.Count - shown).Append(" more.");
            return sb.ToString();
        }

        // Builds a fresh run. Also the Restart path (end screen / pause menu).
        private void StartRun()
        {
            SaveSystem.Delete();   // clear any old save on every fresh start (do not resume into it)
            BeginRun(new EventEngine(_story, resources, new Deck(_story), seed));
        }

        // Uniform event wiring for any engine instance (fresh or resumed) + initial render.
        private void BeginRun(EventEngine engine)
        {
            _engine = engine;
            _engine.OnGameOver += HandleGameOver;
            if (endScreen != null) endScreen.Hide();
            if (menu != null) menu.Hide();
            if (pauseButton != null) pauseButton.SetVisible(true);
            if (audioDirector != null && theme != null) audioDirector.PlayMusic(theme.music);   // switch to the gameplay track
            RenderCurrent();
        }

        // Pause (spec 9.5): reachable mid-run via Esc or the pause button. Save stays on disk so the
        // player can leave to the main menu and Continue later.
        private void OpenPause()
        {
            if (menu == null || menu.IsShown) return;
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            menu.Show("Paused", null, new[]
            {
                new MenuOverlay.MenuItem("Resume", null, true),   // Invoke() hides the menu; that is the resume
                new MenuOverlay.MenuItem("Restart", StartRun),
                new MenuOverlay.MenuItem("Main Menu", ShowMainMenu)
            });
        }

        private void QuitApp() => Application.Quit();   // no-op in the editor, harmless

        private bool MenuBlocking => menu != null && menu.IsShown;

        private void HandleCommit(ChoiceSide side)
        {
            if (MenuBlocking) return;
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            if (audioDirector != null && theme != null) audioDirector.PlaySfx(theme.swipeSfx);
            _engine.Resolve(side);              // apply the choice only (spec 12.4)
            if (_engine.Status == GameStatus.Running)
            {
                _engine.Advance();              // request the next event separately - may itself end (NoMoreEvents / Survived)
                RenderCurrent();
                // save-on-commit (J4/M5): only if still running after Advance. If Advance ended the run,
                // HandleGameOver already deleted the save - do not rewrite it for a finished run.
                if (_engine.Status == GameStatus.Running) SaveSystem.Save(_engine.State);
                Debug.Log($"[Crossroads] swipe {side} -> {_engine.Current?.Id}: {_engine.Current?.Body}");
            }
        }

        private void HandlePreview(ChoiceSide side, float fraction)
        {
            if (MenuBlocking) return;
            if (cardView != null) cardView.ApplyDrag(side, fraction);
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            var deltas = _engine.Preview(side).Deltas;                    // projected delta (spec 10.3)
            if (resourceBar != null) resourceBar.ShowPreview(deltas);     // on the meters
            if (cardView != null) cardView.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, resources, theme), side); // and on the card
        }

        private void HandleCancel()
        {
            if (cardView != null) cardView.ResetDrag();
            if (resourceBar != null) resourceBar.ClearPreview();
        }

        private void RenderCurrent()
        {
            if (cardView != null) cardView.Bind(ViewMapper.BuildNodeView(_engine.Current), theme);
            if (resourceBar != null) resourceBar.Bind(ViewMapper.BuildResourceViews(_engine.State, resources, theme));
            if (audioDirector != null && theme != null) audioDirector.PlaySfx(theme.cardSfx);   // new card appears
        }

        // Clears the card/meters behind the main menu so the title screen is not backed by stale content.
        private void ClearCurrent()
        {
            if (cardView != null) cardView.Bind(ViewMapper.BuildNodeView(null), theme);
        }

        private void HandleGameOver(GameOverInfo info)
        {
            SaveSystem.Delete();   // do not resume into a finished run (J4/M5)
            if (pauseButton != null) pauseButton.SetVisible(false);
            Debug.Log($"[Crossroads] Game over ({info.Reason}): {info.Text}");
            if (endScreen != null) endScreen.Show(info.Text, StartRun, menu != null ? (Action)ShowMainMenu : null);
        }
    }
}
