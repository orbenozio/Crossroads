using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // הצגת מדי-המשאבים (ספק 12.3). בונה מד אחד לכל ResourceView מבוסס-דאטה (לא מספר/שמות קבועים),
    // כך שכל משחק מקבל את המדים שלו בלי קוד (M7/M8). מצב-סכנה מסומן לא-תלוי-צבע (ספק 11.1): סימן !/!!.
    // find-or-create לפי שם -> אידמפוטנטי גם אחרי domain reload (לא יוצר כפילויות).
    public sealed class ResourceBarView : MonoBehaviour
    {
        private Theme _theme;
        private RectTransform _container;
        private readonly Dictionary<string, Bar> _bars = new Dictionary<string, Bar>();

        private sealed class Bar { public RectTransform root; public RectTransform fill; public Image fillImg; public Image icon; public TextMeshProUGUI label; public string baseLabel; }

        public void SetTheme(Theme t) => _theme = t;

        public void Bind(IReadOnlyList<ResourceView> views)
        {
            EnsureContainer();
            for (int i = 0; i < views.Count; i++)
            {
                ResourceView v = views[i];
                Bar bar = Ensure(v.Id);
                Place(bar.root, i, views.Count);

                float range = v.Max - v.Min;
                float pct = range > 0f ? Mathf.Clamp01((v.Value - v.Min) / range) : 0f;
                bar.fill.anchorMin = new Vector2(0f, 0f);
                bar.fill.anchorMax = new Vector2(pct, 1f);
                bar.fill.offsetMin = Vector2.zero;
                bar.fill.offsetMax = Vector2.zero;
                bar.fillImg.color = FillColor(v.Danger);

                // With a HUD icon the bar shows icon + value; without one it keeps the text name + value.
                bool hasIcon = v.Icon != null;
                if (bar.icon != null)
                {
                    bar.icon.gameObject.SetActive(hasIcon);
                    if (hasIcon) { bar.icon.sprite = v.Icon; bar.icon.color = Color.white; }
                }

                string mark = v.Danger == DangerLevel.WillBreak ? "  !!" : v.Danger == DangerLevel.Approaching ? "  !" : "";
                bar.baseLabel = (hasIcon ? v.Value.ToString() : v.DisplayName + " " + v.Value) + mark;
                bar.label.text = bar.baseLabel;
            }
        }

        // משוב delta בזמן swipe (ספק 10.3): כל מד מושפע מציג את השינוי הצפוי (למשל "Sleep 6 -2").
        // נומינלי בכוונה - מראה את כוונת הבחירה לפני ההחלה. ClearPreview מחזיר לתווית הבסיס.
        public void ShowPreview(IReadOnlyList<ResourceDelta> deltas)
        {
            ClearPreview();
            if (deltas == null) return;
            for (int i = 0; i < deltas.Count; i++)
            {
                if (deltas[i].Delta == 0) continue;
                if (!_bars.TryGetValue(deltas[i].ResourceId, out Bar bar)) continue;
                string sign = deltas[i].Delta > 0 ? "+" : "";
                bar.label.text = bar.baseLabel + "   " + sign + deltas[i].Delta;
            }
        }

        public void ClearPreview()
        {
            foreach (var kv in _bars)
                if (kv.Value.baseLabel != null) kv.Value.label.text = kv.Value.baseLabel;
        }

        private Color FillColor(DangerLevel d)
        {
            // הצבע ערוץ-משנה בלבד; הסכנה מסומנת גם ב-!/!! (לא-תלוי-צבע).
            if (_theme != null)
            {
                if (d == DangerLevel.WillBreak) return _theme.willBreak;
                if (d == DangerLevel.Approaching) return _theme.approaching;
                return _theme.card;
            }
            if (d == DangerLevel.WillBreak) return new Color(0.85f, 0.25f, 0.25f);
            if (d == DangerLevel.Approaching) return new Color(0.95f, 0.75f, 0.20f);
            return new Color(0.35f, 0.55f, 0.85f);
        }

        private void EnsureContainer()
        {
            if (_container != null) return;
            Transform existing = transform.Find("Meters");
            if (existing != null) { _container = (RectTransform)existing; return; }

            _bars.Clear();   // building a fresh container - any cached bars belong to a destroyed one
            EnsureHudBackground();

            var go = new GameObject("Meters", typeof(RectTransform));
            _container = (RectTransform)go.transform;
            _container.SetParent(transform, false);
            _container.anchorMin = new Vector2(0f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(0.5f, 1f);
            _container.anchoredPosition = new Vector2(0f, -30f);
            // Inset 120 total leaves the top corners clear for the pause button (54px).
            _container.sizeDelta = new Vector2(-120f, 56f);
        }

        // A full-width top strip behind the meters, forming a HUD bar (and a backdrop for the corner
        // pause button). Created once, behind everything. Themed when a theme is set.
        private void EnsureHudBackground()
        {
            if (transform.Find("MetersBg") != null) return;
            var go = new GameObject("MetersBg", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 94f);
            Color c = _theme != null ? _theme.background : new Color(0.10f, 0.10f, 0.12f);
            go.GetComponent<Image>().color = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 0.85f);
            go.GetComponent<Image>().raycastTarget = false;
            rt.SetAsFirstSibling();
        }

        private Bar Ensure(string id)
        {
            // Drop a stale cache entry whose GameObject was destroyed (e.g. an editor re-wire calling
            // CleanOldUi) so it rebuilds instead of dereferencing a destroyed RectTransform.
            if (_bars.TryGetValue(id, out Bar cached))
            {
                if (cached.root != null) return cached;
                _bars.Remove(id);
            }

            string barName = "Bar_" + id;
            Transform found = _container.Find(barName);
            if (found != null)
            {
                Bar rebound = Rebind(found);
                _bars[id] = rebound;
                return rebound;
            }

            var rootGo = new GameObject(barName, typeof(RectTransform), typeof(Image));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(_container, false);
            rootGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f); // track

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fill = (RectTransform)fillGo.transform;
            fill.SetParent(root, false);
            var fillImg = fillGo.GetComponent<Image>();

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.SetParent(root, false);
            iconRt.anchorMin = new Vector2(0.04f, 0.1f);
            iconRt.anchorMax = new Vector2(0.28f, 0.9f);
            iconRt.offsetMin = Vector2.zero; iconRt.offsetMax = Vector2.zero;
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true; iconImg.raycastTarget = false; iconImg.color = Color.white;
            iconGo.SetActive(false);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(root, false);
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(label);
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var bar = new Bar { root = root, fill = fill, fillImg = fillImg, icon = iconImg, label = label };
            _bars[id] = bar;
            return bar;
        }

        private Bar Rebind(Transform root)
        {
            var rt = (RectTransform)root;
            var fill = (RectTransform)root.Find("Fill");
            var label = root.Find("Label").GetComponent<TextMeshProUGUI>();
            var iconT = root.Find("Icon");
            return new Bar { root = rt, fill = fill, fillImg = fill.GetComponent<Image>(),
                icon = iconT != null ? iconT.GetComponent<Image>() : null, label = label };
        }

        private void Place(RectTransform root, int index, int count)
        {
            const float pad = 0.01f;
            float x0 = (float)index / count;
            float x1 = (float)(index + 1) / count;
            root.anchorMin = new Vector2(x0 + pad, 0f);
            root.anchorMax = new Vector2(x1 - pad, 1f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }
    }
}
