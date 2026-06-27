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
        private Theme _theme;
        private Action _onRestart;
        private Action _onMenu;

        public void SetTheme(Theme t) => _theme = t;

        public void Show(string text, Action onRestart) => Show(text, onRestart, null);

        // onMenu != null adds a secondary "Main Menu" button under Restart (spec 9.5). Null keeps the
        // original single-button end screen (back-compat for existing callers/tests).
        public void Show(string text, Action onRestart, Action onMenu)
        {
            Ensure();
            _onRestart = onRestart;
            _onMenu = onMenu;
            _message.text = text;
            _menuButton.gameObject.SetActive(onMenu != null);
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling(); // מעל הקלף והמדים
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
                return;
            }

            var panelGo = new GameObject("EndScreen", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

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
            btnRt.sizeDelta = new Vector2(380f, 116f); btnRt.anchoredPosition = Vector2.zero;
            var btn = btnGo.GetComponent<Button>();
            MenuOverlay.ConfigureButtonVisual(btnGo.GetComponent<Image>(), btn, _theme, primary);   // same plate/states as the menu
            btn.onClick.AddListener(onClick);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(btnRt, false); Stretch(lblRt);
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(lbl); lbl.fontSize = 32; lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white; lbl.text = text; lbl.raycastTarget = false;
            return btnRt;
        }

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
