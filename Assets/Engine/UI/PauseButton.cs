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
        [SerializeField] private Sprite menuIcon;   // wired from Theme.menuIcon; null = the built-in three bars

        public void SetVisible(bool visible)
        {
            Ensure();
            ApplyInset();   // keep the button clear of the device notch (no-op in the editor)
            _root.gameObject.SetActive(visible);
        }

        // Drops the top-corner button below the status bar / notch on device. Re-applied on every
        // SetVisible so a scene-baked button gets it too.
        private void ApplyInset()
        {
            if (_root == null) return;
            bool rtl = UIFonts.RightToLeft;
            // Centre the button vertically on the meter row instead of floating in the very corner: the
            // meters sit at container y -38 with height 104, so their centre is ~-90 from the top; with the
            // 64px button pivoted at its top edge that lands at y -58. Shares the same notch inset as the HUD.
            _root.anchoredPosition = new Vector2(rtl ? 10f : -10f, -58f - SafeArea.TopInset(this));
        }

        private void Ensure()
        {
            if (_root != null) { BuildVisual(); return; }

            var found = transform.Find("PauseButton") as RectTransform;
            if (found != null)
            {
                _root = found;
                var b0 = found.GetComponent<Button>();
                if (b0 != null)
                {
                    b0.onClick.RemoveListener(Fire);   // avoid listener pile-up on the find-existing path
                    b0.onClick.AddListener(Fire);
                }
                BuildVisual();
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
            _root.sizeDelta = new Vector2(64f, 64f);   // 64 >= 48dp tap target (agent); ApplyInset clears the notch
            _root.anchoredPosition = new Vector2(rtl ? 10f : -10f, -58f);   // aligned to the meter row (see ApplyInset)
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            go.GetComponent<Button>().onClick.AddListener(Fire);
            BuildVisual();
        }

        // Shows the themed menu icon when one is wired, otherwise the built-in three bars. Re-applied on
        // every Ensure so a scene-baked button picks up a newly-wired icon and the old bars are hidden.
        private void BuildVisual()
        {
            bool useIcon = menuIcon != null;

            var iconT = _root.Find("Icon") as RectTransform;
            if (useIcon)
            {
                if (iconT == null)
                {
                    var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconT = (RectTransform)go.transform;
                    iconT.SetParent(_root, false);
                    iconT.anchorMin = new Vector2(0.12f, 0.12f);
                    iconT.anchorMax = new Vector2(0.88f, 0.88f);
                    iconT.offsetMin = Vector2.zero; iconT.offsetMax = Vector2.zero;
                }
                var img = iconT.GetComponent<Image>();
                img.sprite = menuIcon; img.preserveAspect = true; img.color = Color.white; img.raycastTarget = false;
                iconT.gameObject.SetActive(true);
            }
            else if (iconT != null)
            {
                iconT.gameObject.SetActive(false);
            }

            // Three bars (hamburger), centered - only when there is no themed icon.
            for (int i = 0; i < 3; i++)
            {
                var bar = _root.Find("Bar" + i) as RectTransform;
                if (!useIcon && bar == null)
                {
                    var barGo = new GameObject("Bar" + i, typeof(RectTransform), typeof(Image));
                    bar = (RectTransform)barGo.transform;
                    bar.SetParent(_root, false);
                    bar.anchorMin = new Vector2(0.22f, 0f);
                    bar.anchorMax = new Vector2(0.78f, 0f);
                    bar.pivot = new Vector2(0.5f, 0.5f);
                    bar.sizeDelta = new Vector2(0f, 5f);
                    bar.anchoredPosition = new Vector2(0f, 21f + i * 11f);
                    var img = barGo.GetComponent<Image>();
                    img.color = new Color(1f, 1f, 1f, 0.85f);
                    img.raycastTarget = false;
                }
                if (bar != null) bar.gameObject.SetActive(!useIcon);
            }
        }

        private void Fire() { AudioDirector.PlayClick(); OnPressed?.Invoke(); }
    }
}
