using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Crossroads.UI
{
    // overlay כללי לכותרת + גוף + כפתור אופציונלי (ספק 9.5). משמש את מסך-הפתיחה (כפתור Start)
    // ואת מסך-שגיאת-הדאטה (בלי כפתור - מציג למה הטעינה נכשלה במקום כשל-שקט). בנוי בקוד
    // (find-or-create אידמפוטנטי), מתחיל מוסתר. טקסט דרך TMP (RTL/עברית).
    public sealed class MessageOverlay : MonoBehaviour
    {
        private RectTransform _panel;
        private TMP_Text _title;
        private TMP_Text _body;
        private RectTransform _button;
        private TMP_Text _buttonLabel;
        private Action _onButton;

        // buttonLabel ריק/null => בלי כפתור (מסך-שגיאה). אחרת כפתור שמפעיל onButton.
        public void Show(string title, string body, string buttonLabel, Action onButton)
        {
            Ensure();
            _title.text = title;
            _body.text = body;
            _onButton = onButton;

            bool hasButton = !string.IsNullOrEmpty(buttonLabel);
            _button.gameObject.SetActive(hasButton);
            if (hasButton) _buttonLabel.text = buttonLabel;

            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        public bool IsShown => _panel != null && _panel.gameObject.activeSelf;

        private void Ensure()
        {
            if (_panel != null) return;

            var found = transform.Find("MessageOverlay") as RectTransform;
            if (found != null)
            {
                _panel = found;
                _title = found.Find("Title").GetComponent<TMP_Text>();
                _body = found.Find("Body").GetComponent<TMP_Text>();
                _button = found.Find("Button") as RectTransform;
                _buttonLabel = _button.Find("Label").GetComponent<TMP_Text>();
                var existing = _button.GetComponent<Button>();
                existing.onClick.RemoveListener(OnButtonClicked); // למנוע הצטברות-listener
                existing.onClick.AddListener(OnButtonClicked);
                return;
            }

            var panelGo = new GameObject("MessageOverlay", typeof(RectTransform), typeof(Image));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            panelGo.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.09f, 0.96f);

            _title = MakeText("Title", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.8f), 56, FontStyles.Bold);
            _body = MakeText("Body", new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.6f), 30, FontStyles.Normal);

            var btnGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            _button = (RectTransform)btnGo.transform;
            _button.SetParent(_panel, false);
            _button.anchorMin = new Vector2(0.5f, 0.18f); _button.anchorMax = new Vector2(0.5f, 0.18f);
            _button.pivot = new Vector2(0.5f, 0.5f);
            _button.sizeDelta = new Vector2(340f, 100f); _button.anchoredPosition = Vector2.zero;
            btnGo.GetComponent<Image>().color = new Color(0.30f, 0.55f, 0.95f, 1f);
            btnGo.GetComponent<Button>().onClick.AddListener(OnButtonClicked);

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(_button, false); Stretch(lblRt);
            _buttonLabel = lblGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(_buttonLabel); _buttonLabel.fontSize = 34; _buttonLabel.alignment = TextAlignmentOptions.Center;
            _buttonLabel.color = Color.white; _buttonLabel.text = "Start"; _buttonLabel.raycastTarget = false;

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
            UIFonts.Apply(t); t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private void OnButtonClicked()
        {
            Hide();
            _onButton?.Invoke();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
