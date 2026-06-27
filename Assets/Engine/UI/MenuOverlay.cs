using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Crossroads.UI
{
    // Generic full-screen menu overlay (spec 9.5): a title, an optional body, and a vertical stack
    // of N buttons. One reusable component serves the main menu (Continue / New Game / Quit), the
    // pause menu (Resume / Restart / Main Menu) and confirm dialogs (Yes / No). Content-agnostic -
    // the bootstrap supplies the items, so cloning needs no menu code (M7/M8).
    //
    // Built procedurally (find-or-create), starts hidden, text via TMP (RTL/Hebrew, §10.6).
    // Buttons are also pickable by number key 1-9, matching the map's keyboard accessibility (§11.4).
    public sealed class MenuOverlay : MonoBehaviour
    {
        // A single menu entry: a label, the action to run, and whether it is the primary (accented) action.
        public readonly struct MenuItem
        {
            public readonly string Label;
            public readonly Action OnSelect;
            public readonly bool Primary;
            public MenuItem(string label, Action onSelect, bool primary = false)
            {
                Label = label; OnSelect = onSelect; Primary = primary;
            }
        }

        private Theme _theme;
        private RectTransform _panel;
        private Image _art;       // optional key-art backdrop (Theme.keyArt)
        private AspectRatioFitter _artFitter;   // covers the screen (crops the bleed) instead of letterboxing
        private GameObject _scrim;
        private Image _logo;      // optional title wordmark (Theme.logo), replaces the title text
        private TMP_Text _title;
        private TMP_Text _body;
        private RectTransform _buttons;
        private readonly List<Action> _actions = new List<Action>();   // index -> action, for number-key picking

        public void SetTheme(Theme t) { _theme = t; ApplyArt(); }

        public bool IsShown => _panel != null && _panel.gameObject.activeSelf;

        // useLogo: show the theme's title wordmark in place of the title text (main menu only;
        // the pause/confirm screens keep their text title).
        public void Show(string title, string body, IReadOnlyList<MenuItem> items, bool useLogo = false)
        {
            Ensure();
            bool showLogo = useLogo && _logo != null && _theme != null && _theme.logo != null;
            if (_logo != null)
            {
                _logo.gameObject.SetActive(showLogo);
                if (showLogo) _logo.sprite = _theme.logo;
            }
            _title.gameObject.SetActive(!showLogo);
            _title.text = title ?? string.Empty;
            _body.text = body ?? string.Empty;
            _body.gameObject.SetActive(!string.IsNullOrEmpty(body));

            RebuildButtons(items);

            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();   // above card / meters / pause button
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsShown) return;
            var k = Keyboard.current;
            if (k == null) return;
            // Number keys 1..9 activate the matching button (same accessibility model as MapView).
            for (int i = 0; i < _actions.Count && i < 9; i++)
            {
                if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { Invoke(i); return; }
            }
        }

        private void RebuildButtons(IReadOnlyList<MenuItem> items)
        {
            // Clear previous buttons so the stack matches the new item list exactly (idempotent reshow).
            _actions.Clear();
            for (int i = _buttons.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_buttons.GetChild(i).gameObject);

            int n = items != null ? items.Count : 0;
            for (int i = 0; i < n; i++)
            {
                MenuItem item = items[i];
                _actions.Add(item.OnSelect);
                MakeButton(i, n, item);
            }
        }

        private void MakeButton(int index, int count, MenuItem item)
        {
            var btnGo = new GameObject("Button_" + index, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)btnGo.transform;
            rt.SetParent(_buttons, false);

            // Stack top-to-bottom inside the buttons container, evenly spaced.
            const float h = 0.16f, gap = 0.04f;
            float top = 1f - index * (h + gap);
            rt.anchorMin = new Vector2(0.5f, top - h);
            rt.anchorMax = new Vector2(0.5f, top);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var btn = btnGo.GetComponent<Button>();
            ApplyButtonStates(btn, item.Primary ? AccentColor() : SecondaryColor());
            int captured = index;
            btn.onClick.AddListener(() => Invoke(captured));

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(rt, false);
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(lbl);
            lbl.fontSize = 30;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white;
            lbl.raycastTarget = false;
            // "[N] Label" - number key 1-9 picks it (accessibility parity with the map).
            lbl.text = count > 1 ? "[" + (index + 1) + "] " + item.Label : item.Label;
        }

        private void Invoke(int index)
        {
            if (index < 0 || index >= _actions.Count) return;
            AudioDirector.PlayClick();
            Action a = _actions[index];
            // Hide first so an action that reshows the menu (e.g. Cancel -> main menu) wins the final state.
            Hide();
            a?.Invoke();
        }

        private void Ensure()
        {
            if (_panel != null) return;

            var found = transform.Find("MenuOverlay") as RectTransform;
            if (found != null)
            {
                _panel = found;
                _art = found.Find("Art").GetComponent<Image>();
                _artFitter = _art.GetComponent<AspectRatioFitter>();
                _scrim = found.Find("Scrim").gameObject;
                _logo = found.Find("Logo").GetComponent<Image>();
                _title = found.Find("Title").GetComponent<TMP_Text>();
                _body = found.Find("Body").GetComponent<TMP_Text>();
                _buttons = found.Find("Buttons") as RectTransform;
                ApplyArt();
                return;
            }

            var panelGo = new GameObject("MenuOverlay", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            panelGo.GetComponent<Image>().color = PanelColor();

            // Backdrop key art (behind everything), then a dark scrim for text readability.
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(_panel, false);
            Stretch((RectTransform)artGo.transform);
            _art = artGo.GetComponent<Image>();
            _art.raycastTarget = false;
            // Cover the screen (crop the bleed) rather than letterbox; aspectRatio set per sprite in ApplyArt.
            _artFitter = artGo.AddComponent<AspectRatioFitter>();
            _artFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            var scrimGo = new GameObject("Scrim", typeof(RectTransform), typeof(Image));
            scrimGo.transform.SetParent(_panel, false);
            Stretch((RectTransform)scrimGo.transform);
            scrimGo.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.42f);
            scrimGo.GetComponent<Image>().raycastTarget = false;
            _scrim = scrimGo;

            _title = MakeText("Title", new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.88f), 60, FontStyles.Bold);
            _body = MakeText("Body", new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.68f), 28, FontStyles.Normal);

            // Optional title wordmark (Theme.logo), over the title region; shown instead of the title text.
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(_panel, false);
            var logoRt = (RectTransform)logoGo.transform;
            logoRt.anchorMin = new Vector2(0.1f, 0.66f); logoRt.anchorMax = new Vector2(0.9f, 0.93f);
            logoRt.offsetMin = Vector2.zero; logoRt.offsetMax = Vector2.zero;
            _logo = logoGo.GetComponent<Image>();
            _logo.preserveAspect = true; _logo.raycastTarget = false; _logo.color = Color.white;
            logoGo.SetActive(false);

            var btnsGo = new GameObject("Buttons", typeof(RectTransform));
            _buttons = (RectTransform)btnsGo.transform;
            _buttons.SetParent(_panel, false);
            _buttons.anchorMin = new Vector2(0.1f, 0.1f);
            _buttons.anchorMax = new Vector2(0.9f, 0.5f);
            _buttons.offsetMin = Vector2.zero; _buttons.offsetMax = Vector2.zero;

            ApplyArt();   // assign/hide the backdrop now that Art + Scrim exist
            _panel.gameObject.SetActive(false);
        }

        private TMP_Text MakeText(string name, Vector2 aMin, Vector2 aMax, int size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_panel, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(t);
            t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.color = _theme != null ? _theme.text : Color.white;
            t.raycastTarget = false;
            return t;
        }

        // Shows the theme's key art behind a scrim, or hides both (flat panel) when there is none.
        private void ApplyArt()
        {
            if (_art == null) return;   // overlay not built yet
            Sprite s = _theme != null ? _theme.keyArt : null;
            bool has = s != null;
            _art.sprite = s;
            if (has && _artFitter != null) _artFitter.aspectRatio = s.rect.width / s.rect.height;
            _art.gameObject.SetActive(has);
            if (_scrim != null) _scrim.SetActive(has);
        }

        private Color PanelColor()
        {
            if (_theme != null)
            {
                Color b = _theme.background;
                return new Color(b.r * 0.5f, b.g * 0.5f, b.b * 0.5f, 0.97f);
            }
            return new Color(0.06f, 0.06f, 0.09f, 0.97f);
        }

        private Color AccentColor() => _theme != null ? _theme.accent : new Color(0.30f, 0.55f, 0.95f, 1f);
        private Color SecondaryColor() => _theme != null ? _theme.card : new Color(0.28f, 0.28f, 0.33f, 1f);

        // Configures a button's idle / hover / pressed / disabled colors (ColorTint transition). Shared
        // so the end screen uses the same feel.
        internal static void ApplyButtonStates(Button b, Color baseColor)
        {
            var cb = b.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = Shift(baseColor, 0.14f);
            cb.pressedColor = Shift(baseColor, -0.16f);
            cb.selectedColor = Shift(baseColor, 0.08f);
            cb.disabledColor = new Color(0.3f, 0.3f, 0.34f, 0.5f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            if (b.targetGraphic != null) b.targetGraphic.color = Color.white;   // ColorTint multiplies this by the state color
        }

        private static Color Shift(Color c, float amt) =>
            new Color(Mathf.Clamp01(c.r + amt), Mathf.Clamp01(c.g + amt), Mathf.Clamp01(c.b + amt), c.a);

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
