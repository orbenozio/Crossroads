using System;
using UnityEngine;
using UnityEngine.UI;

namespace Crossroads.UI
{
    // A small always-visible "open menu" affordance for pointer/touch players (spec 9.5). Sits in the
    // top corner (flips for RTL) and fires OnPressed - the bootstrap opens the pause menu. Keyboard
    // players reach the same menu via Esc on SwipeInput, so this is the pointer parallel.
    //
    // Built procedurally (find-or-create) like the other UI components, so the wire tool only needs to
    // AddComponent it - no manual GameObject. The icon is three bars (language-neutral, font-independent).
    public sealed class PauseButton : MonoBehaviour
    {
        public event Action OnPressed;

        private RectTransform _root;

        public void SetVisible(bool visible)
        {
            Ensure();
            _root.gameObject.SetActive(visible);
        }

        private void Ensure()
        {
            if (_root != null) return;

            var found = transform.Find("PauseButton") as RectTransform;
            if (found != null)
            {
                _root = found;
                var b0 = found.GetComponent<Button>();
                b0.onClick.RemoveListener(Fire);   // avoid listener pile-up on the find-existing path
                b0.onClick.AddListener(Fire);
                return;
            }

            bool rtl = UIFonts.RightToLeft;
            var go = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            _root = (RectTransform)go.transform;
            _root.SetParent(transform, false);
            // Top corner: right for LTR, left for RTL.
            float ax = rtl ? 0f : 1f;
            _root.anchorMin = new Vector2(ax, 1f);
            _root.anchorMax = new Vector2(ax, 1f);
            _root.pivot = new Vector2(ax, 1f);
            _root.sizeDelta = new Vector2(54f, 54f);
            _root.anchoredPosition = new Vector2(rtl ? 10f : -10f, -10f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            go.GetComponent<Button>().onClick.AddListener(Fire);

            // Three bars (hamburger), centered.
            for (int i = 0; i < 3; i++)
            {
                var barGo = new GameObject("Bar" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)barGo.transform;
                rt.SetParent(_root, false);
                rt.anchorMin = new Vector2(0.22f, 0f);
                rt.anchorMax = new Vector2(0.78f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(0f, 5f);
                rt.anchoredPosition = new Vector2(0f, 18f + i * 9f);  // bottom-anchored; rows at 18/27/36
                var img = barGo.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.85f);
                img.raycastTarget = false;
            }
        }

        private void Fire() { AudioDirector.PlayClick(); OnPressed?.Invoke(); }
    }
}
