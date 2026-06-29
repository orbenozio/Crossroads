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
        // Tints the bright-gold meter icon art down to the card's muted antique-bronze, so the HUD reads
        // as the same material as the medallion/card instead of "shouting gold" (ux review: less gold).
        private static readonly Color MeterIconTint = new Color(0.72f, 0.64f, 0.48f, 1f);

        private sealed class Bar { public RectTransform root; public RectTransform fill; public Image fillImg; public Image frame; public Image icon; public TextMeshProUGUI label; public string baseLabel; }

        public void SetTheme(Theme t) => _theme = t;

        public void Bind(IReadOnlyList<ResourceView> views)
        {
            EnsureContainer();
            ApplySafeArea();   // drop the HUD below the notch on device (no-op in the editor)
            for (int i = 0; i < views.Count; i++)
            {
                ResourceView v = views[i];
                Bar bar = Ensure(v.Id);
                Place(bar.root, i, views.Count);
                StyleValueLabel(bar.label);   // bold + auto-fit so "value !! +d" never spills the narrow plate (ux review)
                var rootImg = bar.root.GetComponent<Image>();
                if (rootImg != null) rootImg.color = PlateColor();   // refresh on bind so existing bars get the theme plate too

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
                    if (hasIcon) { bar.icon.sprite = v.Icon; bar.icon.color = MeterIconTint; }
                }
                if (bar.frame != null)
                {
                    bool hasFrame = hasIcon && _theme != null && _theme.meterFrame != null;
                    bar.frame.gameObject.SetActive(hasFrame);
                    if (hasFrame) bar.frame.sprite = _theme.meterFrame;
                }
                if (hasIcon) StyleMeterIcon(bar);   // size the icon + ring every bind so it is not stuck tiny (ux)
                // With an icon on the left, the value sits in the right portion so they do not overlap.
                var lrt = bar.label.rectTransform;
                lrt.anchorMin = new Vector2(hasIcon ? 0.50f : 0f, 0f);
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(6f, 0f); lrt.offsetMax = new Vector2(-12f, 0f);   // keep the value + delta off both plate edges (ux review)

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
                int d = deltas[i].Delta;
                // Colored + bold so the pending change stands out: green for a gain, red for a loss.
                string hex = d > 0 ? "#73E68C" : "#FF6B6B";
                string sign = d > 0 ? "+" : "";
                // Smaller so "5 +1" stays inside its plate instead of pushing toward the neighbour (ux re-review).
                bar.label.text = bar.baseLabel + " <size=72%><b><color=" + hex + ">" + sign + d + "</color></b></size>";
            }
        }

        // Sizes the meter icon + its ring frame larger than the cramped default so the HUD symbols read
        // clearly (they were too small). meterframe.png's ring sits at ~0.74 of its radius, so the frame
        // box is scaled past the plate height - the transparent margin overflows harmlessly - to bring the
        // ring up near the plate; the icon fills the ring's inner opening. Centered on the plate's left
        // half (x = 0.25). Applied every Bind so existing/scene-baked bars resize too, not just new ones.
        private static void StyleMeterIcon(Bar bar)
        {
            if (bar.frame != null && bar.frame.gameObject.activeSelf)
            {
                var frt = bar.frame.rectTransform;
                frt.anchorMin = frt.anchorMax = new Vector2(0.25f, 0.5f);
                frt.pivot = new Vector2(0.5f, 0.5f);
                frt.sizeDelta = new Vector2(132f, 132f);
                frt.anchoredPosition = Vector2.zero;
            }
            var irt = bar.icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.25f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(82f, 82f);
            irt.anchoredPosition = Vector2.zero;
        }

        // The meter value: bold, centered, and auto-sized so a danger mark + swipe delta ("5 !! +2")
        // shrinks to stay inside the narrow plate instead of overflowing it (ux review). A single digit
        // still renders at the max size, so the normal HUD reads exactly as before.
        private static void StyleValueLabel(TextMeshProUGUI label)
        {
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 30f;
        }

        public void ClearPreview()
        {
            foreach (var kv in _bars)
                if (kv.Value.baseLabel != null) kv.Value.label.text = kv.Value.baseLabel;
        }

        // A darker, richer meter plate (was a pale translucent track) so the icon + value read clearly.
        private Color PlateColor()
        {
            Color b = _theme != null ? _theme.background : new Color(0.10f, 0.10f, 0.13f);
            return new Color(b.r * 1.4f + 0.04f, b.g * 1.4f + 0.04f, b.b * 1.5f + 0.05f, 0.72f);
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

        // Pushes the meter content below the device notch and stretches the dark plate up to cover the
        // notch strip. Runs every Bind so it applies to scene-baked HUD elements too, not just freshly
        // created ones. Zero inset in the editor -> the layout is unchanged there.
        private void ApplySafeArea()
        {
            float top = SafeArea.TopInset(this);
            if (_container != null)
            {
                _container.anchoredPosition = new Vector2(0f, -38f - top);
                _container.sizeDelta = new Vector2(-140f, 104f);   // wider side margins so the 4 plates breathe (ux re-review)
            }
            if (transform.Find("MetersBg") is RectTransform bg)
                bg.sizeDelta = new Vector2(0f, 152f + top);
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
            _container.anchoredPosition = new Vector2(0f, -38f);
            // Inset 140 total leaves the top corners clear for the pause button (54px) plus side margin.
            // Taller so the framed icons read clearly (icon size is bounded by the bar height via preserveAspect).
            _container.sizeDelta = new Vector2(-140f, 104f);   // taller meters + wider margins (agent #2 / ux re-review)
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
            rt.sizeDelta = new Vector2(0f, 152f);
            Color c = _theme != null ? _theme.background : new Color(0.10f, 0.10f, 0.12f);
            go.GetComponent<Image>().color = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 0.9f);
            go.GetComponent<Image>().raycastTarget = false;
            // Created before the meters and after the gameplay background, so it sits behind the meters
            // but in front of the background art (no SetAsFirstSibling, which would push it behind).

            // A thin gold divider along the bottom of the HUD strip, separating meters from the card area.
            var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            var srt = (RectTransform)sep.transform;
            srt.SetParent(rt, false);
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(0f, 3f); srt.anchoredPosition = Vector2.zero;
            Color accent = _theme != null ? _theme.accent : new Color(0.85f, 0.68f, 0.28f);
            sep.GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.7f);   // a touch stronger now the accent is muted bronze (ux round 2)
            sep.GetComponent<Image>().raycastTarget = false;
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
            rootGo.GetComponent<Image>().color = PlateColor();

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fill = (RectTransform)fillGo.transform;
            fill.SetParent(root, false);
            var fillImg = fillGo.GetComponent<Image>();

            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var frameRt = (RectTransform)frameGo.transform;
            frameRt.SetParent(root, false);
            frameRt.anchorMin = new Vector2(0.0f, 0.0f);
            frameRt.anchorMax = new Vector2(0.50f, 1.0f);
            frameRt.offsetMin = Vector2.zero; frameRt.offsetMax = Vector2.zero;
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.preserveAspect = true; frameImg.raycastTarget = false; frameImg.color = Color.white;
            frameGo.SetActive(false);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.SetParent(root, false);
            // Sits inside the frame's ring (centered on the frame's center x = 0.25).
            iconRt.anchorMin = new Vector2(0.11f, 0.17f);
            iconRt.anchorMax = new Vector2(0.39f, 0.83f);
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
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            StyleValueLabel(label);

            var bar = new Bar { root = root, fill = fill, fillImg = fillImg, frame = frameImg, icon = iconImg, label = label };
            _bars[id] = bar;
            return bar;
        }

        private Bar Rebind(Transform root)
        {
            var rt = (RectTransform)root;
            var fill = (RectTransform)root.Find("Fill");
            var label = root.Find("Label").GetComponent<TextMeshProUGUI>();
            var iconT = root.Find("Icon");
            var frameT = root.Find("Frame");
            return new Bar { root = rt, fill = fill, fillImg = fill.GetComponent<Image>(),
                frame = frameT != null ? frameT.GetComponent<Image>() : null,
                icon = iconT != null ? iconT.GetComponent<Image>() : null, label = label };
        }

        private void Place(RectTransform root, int index, int count)
        {
            const float pad = 0.022f;   // clearer gap between plates so they read as 4 units (ux review round 3)
            float x0 = (float)index / count;
            float x1 = (float)(index + 1) / count;
            root.anchorMin = new Vector2(x0 + pad, 0f);
            root.anchorMax = new Vector2(x1 - pad, 1f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }
    }
}
