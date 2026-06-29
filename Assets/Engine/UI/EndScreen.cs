using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Crossroads.UI
{
    // מסך סיום (game-over): overlay כהה + טקסט-סיום + כפתור Restart (ספק 9.4 / 10.7).
    // בנוי בקוד (find-or-create אידמפוטנטי), מתחיל מוסתר. טקסט דרך TMP (RTL/עברית).
    public sealed class EndScreen : MonoBehaviour
    {
        private RectTransform _panel;
        private TMP_Text _message;
        private RectTransform _menuButton;
        private Image _backdrop;
        private AspectRatioFitter _backdropFit;
        private Theme _theme;
        private Action _onRestart;
        private Action _onMenu;

        public void SetTheme(Theme t) => _theme = t;

        public void Show(string text, Action onRestart) => Show(text, null, onRestart, null);

        // onMenu != null adds a secondary "Main Menu" button under Restart (spec 9.5). Null keeps the
        // original single-button end screen (back-compat for existing callers/tests).
        public void Show(string text, Action onRestart, Action onMenu) => Show(text, null, onRestart, onMenu);

        // imageKey selects a per-ending backdrop (theme.GetEndingArt); null/unmatched falls back to keyArt.
        public void Show(string text, string imageKey, Action onRestart, Action onMenu)
        {
            Ensure();
            _onRestart = onRestart;
            _onMenu = onMenu;
            _message.text = text;
            SetBackdrop(imageKey);
            _menuButton.gameObject.SetActive(onMenu != null);
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling(); // מעל הקלף והמדים
        }

        // Swaps the dimmed backdrop to the matched ending's art, falling back to the theme key-art.
        private void SetBackdrop(string imageKey)
        {
            if (_backdrop == null) return;
            Sprite art = _theme != null ? (_theme.GetEndingArt(imageKey) ?? _theme.keyArt) : null;
            _backdrop.sprite = art;
            _backdrop.enabled = art != null;
            if (art != null && _backdropFit != null)
            {
                float h = art.rect.height;
                if (h > 0f)
                {
                    _backdropFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    _backdropFit.aspectRatio = art.rect.width / h;
                }
            }
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private void Ensure()
        {
            if (_panel != null) return;

            var found = transform.Find("EndScreen") as RectTransform;
            if (found != null)
            {
                _panel = found;
                _message = found.Find("Message").GetComponent<TMP_Text>();
                var b = found.Find("RestartButton").GetComponent<Button>();
                b.onClick.RemoveListener(OnRestartClicked); // למנוע הצטברות-listener בנתיב find-existing
                b.onClick.AddListener(OnRestartClicked);
                _menuButton = found.Find("MenuButton") as RectTransform;
                var mb = _menuButton.GetComponent<Button>();
                mb.onClick.RemoveListener(OnMenuClicked);
                mb.onClick.AddListener(OnMenuClicked);
                var ka = found.Find("KeyArt");
                if (ka != null) { _backdrop = ka.GetComponent<Image>(); _backdropFit = ka.GetComponent<AspectRatioFitter>(); }
                return;
            }

            var panelGo = new GameObject("EndScreen", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            Color navy = _theme != null ? _theme.background : new Color(0.04f, 0.04f, 0.09f);
            panelGo.GetComponent<Image>().color = new Color(navy.r * 0.5f, navy.g * 0.5f, navy.b * 0.5f, 0.92f);

            // Dim backdrop behind the message (consistent with the menu / gameplay), not flat black. Built
            // unconditionally so Show() can swap in a per-ending image even when the theme has no keyArt;
            // SetBackdrop disables the Image when there is nothing to show.
            {
                var artGo = new GameObject("KeyArt", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                var artRt = (RectTransform)artGo.transform;
                artRt.SetParent(_panel, false);
                Stretch(artRt);
                _backdrop = artGo.GetComponent<Image>();
                // Dimmed so the message reads over it, but kept high enough that the moody candlelit
                // end-state paintings keep their detail (their upper region is dark anyway, where the text sits).
                _backdrop.color = new Color(0.55f, 0.55f, 0.58f, 1f);
                _backdrop.raycastTarget = false;
                _backdrop.enabled = false;   // SetBackdrop turns it on once a sprite is resolved
                _backdropFit = artGo.GetComponent<AspectRatioFitter>();
            }

            var msgGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            var msgRt = (RectTransform)msgGo.transform;
            msgRt.SetParent(_panel, false);
            msgRt.anchorMin = new Vector2(0.1f, 0.5f);
            msgRt.anchorMax = new Vector2(0.9f, 0.78f);
            msgRt.offsetMin = Vector2.zero; msgRt.offsetMax = Vector2.zero;
            _message = msgGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(_message); _message.fontSize = 40; _message.alignment = TextAlignmentOptions.Center;
            _message.color = Color.white; _message.raycastTarget = false;

            MakeButton("RestartButton", 0.34f, true, "Restart", OnRestartClicked);
            _menuButton = MakeButton("MenuButton", 0.18f, false, "Main Menu", OnMenuClicked);
            _menuButton.gameObject.SetActive(false);

            _panel.gameObject.SetActive(false);
        }

        private RectTransform MakeButton(string name, float y, bool primary, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.SetParent(_panel, false);
            btnRt.anchorMin = new Vector2(0.5f, y); btnRt.anchorMax = new Vector2(0.5f, y);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            // The primary (Restart) leads at full size; the secondary (Main Menu) is a touch smaller.
            btnRt.sizeDelta = primary ? new Vector2(420f, 120f) : new Vector2(380f, 104f);
            btnRt.anchoredPosition = Vector2.zero;
            var btn = btnGo.GetComponent<Button>();
            ConfigureEndButton(btnGo.GetComponent<Image>(), btn, primary);
            btn.onClick.AddListener(onClick);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(btnRt, false); Stretch(lblRt);
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(lbl);
            lbl.fontSize = primary ? 34 : 30;
            lbl.fontStyle = FontStyles.Bold;
            lbl.characterSpacing = 4f;   // spaced caps read as carved/regal, matching the speaker nameplate
            lbl.alignment = TextAlignmentOptions.Center;
            // Warm bronze-gold label on the dark plate; the primary a shade brighter so it leads.
            Color accent = _theme != null ? _theme.accent : new Color(0.62f, 0.50f, 0.28f);
            lbl.color = primary ? Brighten(accent, 0.35f) : accent;
            lbl.text = text.ToUpperInvariant();
            lbl.raycastTarget = false;
            return btnRt;
        }

        // Same engraved-plaque styler the menus use, so every button in the game looks and reacts the same
        // (idle / hover / pressed / selected via ColorTint). Theme is unused for the plate (dark + bronze edge).
        private static void ConfigureEndButton(Image img, Button btn, bool primary)
            => MenuOverlay.ConfigureButtonVisual(img, btn, null, primary);

        private static Color Brighten(Color c, float a) =>
            new Color(Mathf.Clamp01(c.r + a), Mathf.Clamp01(c.g + a), Mathf.Clamp01(c.b + a), c.a);

        private void OnRestartClicked()
        {
            AudioDirector.PlayClick();
            Hide();
            _onRestart?.Invoke();
        }

        private void OnMenuClicked()
        {
            AudioDirector.PlayClick();
            Hide();
            _onMenu?.Invoke();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
