using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Game.RefugeeRoad
{
    // bootstrap של פורמט המסע (ספק 7.5): מלחים את אותן שלוש השכבות, אבל מתזמן מפה<->קלף במקום
    // הלולאה הרצופה של Reigns. אותו EventEngine, אותו CardView/ResourceBarView/SwipeInput/EndScreen -
    // ההבדל היחיד הוא ש-MapGraph מחליף את Deck וש-EnterNode (בחירת-מפה) מחליף את Advance (ספק 12.4).
    // אין כאן לוגיקת-מנוע - רק תזמון מסכים. אין save/resume ב-slice (מוכח ברמת-מנוע בבדיקות).
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Content (מתחלף פר-משחק)")]
        [SerializeField] private TextAsset storyJson;
        [SerializeField] private TextAsset mapJson;     // overlay-המסע (ספק 14.3)
        [SerializeField] private ResourceSet resources;
        [SerializeField] private Theme theme;
        [SerializeField] private int seed = 12345;

        [Header("UI (מ-Crossroads.UI)")]
        [SerializeField] private CardView cardView;
        [SerializeField] private ResourceBarView resourceBar;
        [SerializeField] private SwipeInput swipeInput;
        [SerializeField] private EndScreen endScreen;
        [SerializeField] private MessageOverlay messageOverlay;
        [SerializeField] private MapView mapView;

        [Header("Opening screen (ספק 9.5)")]
        [SerializeField] private string title = "Refugee Road";
        [SerializeField] [TextArea] private string intro = "Cross the border to safety. Choose your route; survive each stop.";

        private EventEngine _engine;
        private StoryData _story;
        private MapData _map;
        private MapGraph _source;

        private void Start()
        {
            if (storyJson == null || mapJson == null || resources == null)
            {
                Debug.LogError("[Crossroads] Missing journey content (story/map/resources).");
                return;
            }

            _story = StoryLoader.Parse(storyJson.text);
            _map = MapLoader.Parse(mapJson.text);

            var issues = StoryValidator.Validate(_story, resources);
            issues.AddRange(MapValidator.Validate(_story, _map));   // ולידציית overlay-המפה (M9)
            foreach (var issue in issues) Debug.LogWarning($"[Crossroads] {issue}");
            var errors = issues.FindAll(i => i.Severity == IssueSeverity.Error);
            if (errors.Count > 0)
            {
                Debug.LogError("[Crossroads] story validation failed - aborting load.");
                if (messageOverlay != null)
                    messageOverlay.Show("Data Error", "The journey data could not be loaded.", null, null);
                return;
            }

            UIFonts.RightToLeft = theme != null && theme.rightToLeft;   // עברית/RTL לפני בניית ה-UI (§10.6)
            if (resourceBar != null) resourceBar.SetTheme(theme);
            if (swipeInput != null)
            {
                swipeInput.OnCommit += HandleCommit;
                swipeInput.OnPreview += HandlePreview;
                swipeInput.OnCancel += HandleCancel;
            }
            if (mapView != null) mapView.OnSelect += HandleSelect;

            if (messageOverlay != null) messageOverlay.Show(title, intro, "Start", Begin);
            else Begin();
        }

        // ריצה טרייה. משמש גם כ-Restart (כפתור מסך-הסיום).
        private void Begin()
        {
            if (messageOverlay != null) messageOverlay.Hide();
            _source = new MapGraph(_story, _map);
            _engine = new EventEngine(_story, resources, _source, seed);
            _engine.OnGameOver += HandleGameOver;
            if (endScreen != null) endScreen.Hide();
            ShowCard();   // ה-Current הוא צומת-ההתחלה
        }

        // אירוע בצומת: מציג את הקלף + מדים, מסתיר את המפה.
        private void ShowCard()
        {
            if (mapView != null) mapView.Hide();
            RenderCurrent();
        }

        // אחרי החלת-בחירה - חוזרים למפה לבחור את הצומת הבא (ספק 7.5, 12.4).
        private void ShowMap()
        {
            if (mapView != null) mapView.Bind(_map, _engine.Current.Id, _source.NeighborsOf(_engine.State));
        }

        private void HandleCommit(ChoiceSide side)
        {
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            _previewSide = null;
            _engine.Resolve(side);                 // החלת הבחירה בלבד (ספק 12.4)
            if (_engine.Status == GameStatus.Running) ShowMap();  // מסע: ניווט דרך המפה, לא Advance
        }

        private ChoiceSide? _previewSide;   // the on-card/meter preview only changes when the dragged side changes

        private void HandlePreview(ChoiceSide side, float fraction)
        {
            if (cardView != null) cardView.ApplyDrag(side, fraction);    // per-frame, must stay smooth
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            if (_previewSide == side) return;   // skip the per-frame Preview/FormatDeltas/string churn while the side is unchanged
            _previewSide = side;
            var deltas = _engine.Preview(side).Deltas;
            if (resourceBar != null) resourceBar.ShowPreview(deltas);
            if (cardView != null) cardView.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, resources, theme), side);
        }

        private void HandleCancel()
        {
            _previewSide = null;
            if (cardView != null) cardView.ResetDrag();
            if (resourceBar != null) resourceBar.ClearPreview();
        }

        // בחירת-צומת במפה -> כניסה אליו. הגעה ליעד = ניצחון (OnGameOver), אחרת מציגים את הקלף הבא.
        private void HandleSelect(string nodeId)
        {
            if (_engine == null || _engine.Status != GameStatus.Running) return;
            if (mapView != null) mapView.Hide();
            _engine.EnterNode(nodeId);
            if (_engine.Status == GameStatus.Running) ShowCard();
        }

        private void RenderCurrent()
        {
            if (cardView != null) cardView.Bind(ViewMapper.BuildNodeView(_engine.Current), theme);
            if (resourceBar != null) resourceBar.Bind(ViewMapper.BuildResourceViews(_engine.State, resources, theme));
        }

        // הפסד (שבירת-משאב) או ניצחון (הגעה ליעד) - אותו מסך-סיום, הטקסט נבחר ע"י המנוע (ספק 9.4).
        private void HandleGameOver(GameOverInfo info)
        {
            if (mapView != null) mapView.Hide();
            Debug.Log($"[Crossroads] Journey over ({info.Reason}): {info.Text}");
            if (endScreen != null) endScreen.Show(info.Text, Begin);
        }
    }
}
