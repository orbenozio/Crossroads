using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // מסך-מפת-המסע (ספק 9.6). מציג את גרף-הצמתים בפריסת-עומק (BFS columns): המיקום הנוכחי מודגש,
    // היעד מסומן, שכנים בני-הגעה ניתנים-ללחיצה, ושאר הצמתים disabled אך נשארים גלויים (ספק 9.6).
    // בחירה בעכבר/מגע (קליק) או במקלדת (מקשי 1-9, נגישות §11.4 - מקביל ל-fallback החצים ב-swipe).
    // בנוי בקוד (find-or-create אידמפוטנטי), מתחיל מוסתר. אגנוסטי-תוכן - מקבל MapData + מצב.
    public sealed class MapView : MonoBehaviour
    {
        public event Action<string> OnSelect;

        private RectTransform _panel;
        private TMP_Text _title;
        private readonly Dictionary<string, Button> _nodes = new Dictionary<string, Button>();
        private readonly List<string> _reachable = new List<string>();

        public bool IsShown => _panel != null && _panel.gameObject.activeSelf;
        public void Hide() { if (_panel != null) _panel.gameObject.SetActive(false); }

        // נגישות מקלדת (§11.4): מקשי 1-9 בוחרים את השכן ה-N בני-ההגעה. מקביל ל-fallback החצים ב-swipe.
        private void Update()
        {
            if (!IsShown) return;
            var k = Keyboard.current;
            if (k == null) return;
            int n = Math.Min(_reachable.Count, 9);
            for (int i = 0; i < n; i++)
                if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { SelectByIndex(i); break; }
        }

        // בחירת השכן ה-index (0-מבוסס) מבין בני-ההגעה. ציבורי גם לטסטים. נתיב ה-click וה-keyboard מתאחדים כאן.
        public void SelectByIndex(int index)
        {
            if (!IsShown || index < 0 || index >= _reachable.Count) return;
            OnSelect?.Invoke(_reachable[index]);
        }

        public void Bind(MapData map, string currentId, IReadOnlyList<string> reachable)
        {
            EnsurePanel();
            if (_title != null) _title.text = "Choose your route  (click or press 1-9)";

            var all = CollectNodes(map);
            var depth = Depths(map, all);
            var columns = GroupByDepth(all, depth);
            int maxDepth = 0;
            foreach (var d in depth.Values) if (d > maxDepth) maxDepth = d;

            var reach = new HashSet<string>(reachable ?? Array.Empty<string>());
            _reachable.Clear();
            if (reachable != null) _reachable.AddRange(reachable);   // סדר קבוע למיפוי מקש-מספר -> שכן

            foreach (var id in all)
            {
                int d = depth.TryGetValue(id, out var dv) ? dv : 0;
                var col = columns[d];
                int idx = col.IndexOf(id);
                Place(EnsureNode(id), d, maxDepth, idx, col.Count);

                bool isCurrent = id == currentId;
                bool isGoal = id == map.GoalNodeId;
                bool canGo = reach.Contains(id);

                var btn = _nodes[id];
                var img = btn.GetComponent<Image>();
                img.color = isCurrent ? new Color(0.30f, 0.55f, 0.95f)
                          : isGoal ? new Color(0.85f, 0.70f, 0.25f)
                          : canGo ? new Color(0.30f, 0.40f, 0.50f)
                          : new Color(0.20f, 0.20f, 0.24f);

                btn.interactable = canGo;   // רק שכנים בני-הגעה לחיצים; השאר נשארים גלויים אך disabled
                string tag = isCurrent ? "\n(you are here)" : isGoal ? "\n(goal)"
                           : canGo ? "\n[" + (_reachable.IndexOf(id) + 1) + "]" : "";   // מספר המקש
                btn.GetComponentInChildren<TMP_Text>().text = id + tag;

                string captured = id;
                btn.onClick.RemoveAllListeners();
                if (canGo) btn.onClick.AddListener(() => OnSelect?.Invoke(captured));
            }

            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
        }

        private static List<string> CollectNodes(MapData map)
        {
            var set = new List<string>();
            void Add(string id) { if (!string.IsNullOrEmpty(id) && !set.Contains(id)) set.Add(id); }
            Add(map.StartNodeId);
            foreach (var kv in map.Edges) { Add(kv.Key); foreach (var n in kv.Value) Add(n); }
            Add(map.GoalNodeId);
            return set;
        }

        private static Dictionary<string, int> Depths(MapData map, List<string> all)
        {
            var depth = new Dictionary<string, int>();
            var q = new Queue<string>();
            depth[map.StartNodeId] = 0;
            q.Enqueue(map.StartNodeId);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                foreach (var n in map.Neighbors(cur))
                    if (!depth.ContainsKey(n)) { depth[n] = depth[cur] + 1; q.Enqueue(n); }
            }
            foreach (var id in all) if (!depth.ContainsKey(id)) depth[id] = 0; // unreached - fallback column
            return depth;
        }

        private static Dictionary<int, List<string>> GroupByDepth(List<string> all, Dictionary<string, int> depth)
        {
            var cols = new Dictionary<int, List<string>>();
            foreach (var id in all)
            {
                int d = depth[id];
                if (!cols.TryGetValue(d, out var list)) cols[d] = list = new List<string>();
                list.Add(id);
            }
            return cols;
        }

        private void Place(Button node, int depth, int maxDepth, int idx, int colCount)
        {
            var rt = (RectTransform)node.transform;
            float x = (depth + 0.5f) / (maxDepth + 1f);
            float y = 1f - (idx + 0.5f) / colCount;   // top-to-bottom within a column
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150f, 80f);
            rt.anchoredPosition = Vector2.zero;
        }

        private void EnsurePanel()
        {
            if (_panel != null) return;
            var found = transform.Find("MapView") as RectTransform;
            if (found != null) { _panel = found; _title = found.Find("MapTitle")?.GetComponent<TMP_Text>(); CacheNodes(); return; }

            var go = new GameObject("MapView", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)go.transform;
            _panel.SetParent(transform, false);
            _panel.anchorMin = Vector2.zero; _panel.anchorMax = Vector2.one;
            _panel.offsetMin = Vector2.zero; _panel.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.13f, 1f);

            var titleGo = new GameObject("MapTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            var trt = (RectTransform)titleGo.transform;
            trt.SetParent(_panel, false);
            trt.anchorMin = new Vector2(0f, 0.9f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var t = titleGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(t); t.fontSize = 30; t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white; t.text = "Choose your route  (click or press 1-9)"; t.raycastTarget = false;
            _title = t;

            _panel.gameObject.SetActive(false);
        }

        private void CacheNodes()
        {
            _nodes.Clear();
            foreach (Transform child in _panel)
                if (child.name.StartsWith("MapNode_"))
                    _nodes[child.name.Substring("MapNode_".Length)] = child.GetComponent<Button>();
        }

        private Button EnsureNode(string id)
        {
            if (_nodes.TryGetValue(id, out var cached) && cached != null) return cached;
            string name = "MapNode_" + id;
            var existing = _panel.Find(name);
            if (existing != null) { var b0 = existing.GetComponent<Button>(); _nodes[id] = b0; return b0; }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panel, false);
            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lrt = (RectTransform)lblGo.transform;
            lrt.SetParent(go.transform, false);
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(lbl); lbl.fontSize = 20; lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white; lbl.raycastTarget = false;
            lbl.enableWordWrapping = false;

            var btn = go.GetComponent<Button>();
            _nodes[id] = btn;
            return btn;
        }
    }
}
