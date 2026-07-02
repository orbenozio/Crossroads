using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Crossroads.UI
{
    // Branded loading screen shown at startup, before the main menu (spec 9.5 polish). A full-bleed
    // poster (theme.loadingArt) sits behind an engraved-stone progress channel with a molten-bronze fill,
    // a wax-seal head riding the fill edge, and a carved serif percentage that counts up. Built in code
    // (find-or-create, idempotent) and starts visible from the first frame so it covers any boot hitch.
    public sealed class LoadingScreen : MonoBehaviour
    {
        private const float Duration = 2.4f;   // total fill time; the work is instant, this is a branded reveal
        private const float FadeOut = 0.45f;

        private RectTransform _panel;
        private Image _backdrop;
        private AspectRatioFitter _backdropFit;
        private RectTransform _barInner;   // the channel interior the fill grows inside
        private RectTransform _fill;
        private RectTransform _head;       // wax-seal marker that rides the fill's leading edge
        private TMP_Text _percent;
        private TMP_Text _caption;
        private CanvasGroup _group;
        private Theme _theme;
        private bool _running;

        public void SetTheme(Theme t) => _theme = t;

        public void SetCaption(string text)
        {
            Ensure();
            if (_caption != null) _caption.text = text ?? string.Empty;
        }

        private void Awake() => Ensure();

        // Plays the 0 -> 100% reveal, then fades out and hands off to onComplete (which shows the menu).
        public void Run(Action onComplete)
        {
            Ensure();
            ApplyBackdrop();
            ApplyBranding();
            if (_running) return;
            _running = true;
            _panel.SetAsLastSibling();
            _panel.gameObject.SetActive(true);
            StartCoroutine(Reveal(onComplete));
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private IEnumerator Reveal(Action onComplete)
        {
            // Advance through eased segments (with tiny holds) so it reads as real loading milestones
            // rather than a flat linear tween.
            float[] stops = { 0.28f, 0.52f, 0.74f, 0.93f, 1f };
            float[] holds = { 0.10f, 0.08f, 0.10f, 0.06f, 0.05f };
            float from = 0f;
            float perSeg = Duration / stops.Length;

            for (int i = 0; i < stops.Length; i++)
            {
                float to = stops[i];
                float move = Mathf.Max(0.0001f, perSeg - holds[i]);
                float t = 0f;
                while (t < move)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / move));
                    SetProgress(Mathf.Lerp(from, to, k));
                    yield return null;
                }
                SetProgress(to);
                from = to;
                float h = 0f;
                while (h < holds[i]) { h += Time.unscaledDeltaTime; yield return null; }
            }

            // Build the next screen (the menu) underneath while we are still fully opaque, then re-assert
            // this panel on top and fade it out - so the fade reveals the menu, never the bare card behind
            // it (which caused a one-frame card flash when the loading screen vanished).
            onComplete?.Invoke();
            _panel.SetAsLastSibling();
            yield return null;   // let the menu lay out one frame under the opaque cover

            float f = 0f;
            while (f < FadeOut)
            {
                f += Time.unscaledDeltaTime;
                if (_group != null) _group.alpha = 1f - Mathf.Clamp01(f / FadeOut);
                yield return null;
            }
            Hide();
            if (_group != null) _group.alpha = 1f;   // reset for any later reuse
            _running = false;
        }

        // Opaque blackout used to cover the screen during app quit, so hiding the menu never flashes the
        // bare card for a frame before the process exits. Shows just the dark panel (poster + bar hidden).
        public void ShowBlackout()
        {
            Ensure();
            if (_backdrop != null) _backdrop.enabled = false;
            var bar = _panel.Find("Bar");
            if (bar != null) bar.gameObject.SetActive(false);
            if (_group != null) _group.alpha = 1f;
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
        }

        private void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (_fill != null) _fill.anchorMax = new Vector2(p, 1f);
            if (_head != null)
            {
                // Ride the leading edge across the inner channel, plus a soft idle pulse on the seal.
                _head.anchorMin = _head.anchorMax = new Vector2(p, 0.5f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
                _head.localScale = new Vector3(pulse, pulse, 1f);
            }
            if (_percent != null) _percent.text = Mathf.RoundToInt(p * 100f) + "%";
        }

        private void ApplyBackdrop()
        {
            if (_backdrop == null) return;
            Sprite art = _theme != null ? (_theme.loadingArt != null ? _theme.loadingArt : _theme.keyArt) : null;
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

        private void Ensure()
        {
            if (_panel != null) return;

            var found = transform.Find("LoadingScreen") as RectTransform;
            if (found != null) { Adopt(found); return; }

            var panelGo = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(transform, false);
            Stretch(_panel);
            _group = panelGo.GetComponent<CanvasGroup>();
            Color navy = _theme != null ? _theme.background : new Color(0.05f, 0.05f, 0.09f);
            panelGo.GetComponent<Image>().color = new Color(navy.r * 0.6f, navy.g * 0.6f, navy.b * 0.6f, 1f);

            // Full-bleed poster backdrop (the game title art).
            var artGo = new GameObject("Poster", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            var artRt = (RectTransform)artGo.transform;
            artRt.SetParent(_panel, false);
            Stretch(artRt);
            _backdrop = artGo.GetComponent<Image>();
            _backdrop.color = Color.white;
            _backdrop.raycastTarget = false;
            _backdrop.enabled = false;
            _backdropFit = artGo.GetComponent<AspectRatioFitter>();

            BuildBar();

            _panel.gameObject.SetActive(true);
            SetProgress(0f);
        }

        // Re-bind an existing scene-baked hierarchy if one is present (keeps the component idempotent).
        private void Adopt(RectTransform found)
        {
            _panel = found;
            _group = found.GetComponent<CanvasGroup>();
            var poster = found.Find("Poster");
            if (poster != null) { _backdrop = poster.GetComponent<Image>(); _backdropFit = poster.GetComponent<AspectRatioFitter>(); }
            _barInner = found.Find("Bar/Channel/Inner") as RectTransform;
            _fill = found.Find("Bar/Channel/Inner/Fill") as RectTransform;
            _head = found.Find("Bar/Channel/Head") as RectTransform;
            var pct = found.Find("Bar/Percent"); if (pct != null) _percent = pct.GetComponent<TMP_Text>();
            var cap = found.Find("Bar/Caption"); if (cap != null) _caption = cap.GetComponent<TMP_Text>();
            SetProgress(0f);
        }

        private void BuildBar()
        {
            // Bar group near the lower third, clear of the poster's baked title.
            var barGo = new GameObject("Bar", typeof(RectTransform));
            var bar = (RectTransform)barGo.transform;
            bar.SetParent(_panel, false);
            bar.anchorMin = new Vector2(0.5f, 0.13f);
            bar.anchorMax = new Vector2(0.5f, 0.13f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(640f, 120f);

            // Caption above the channel.
            var capGo = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            var capRt = (RectTransform)capGo.transform;
            capRt.SetParent(bar, false);
            capRt.anchorMin = new Vector2(0f, 1f); capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0.5f, 0f);
            capRt.sizeDelta = new Vector2(0f, 40f);
            capRt.anchoredPosition = new Vector2(0f, 8f);
            _caption = capGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(_caption);
            _caption.fontSize = 26; _caption.fontStyle = FontStyles.Bold; _caption.characterSpacing = 4f;
            _caption.alignment = TextAlignmentOptions.Center;
            _caption.raycastTarget = false;   // color set in ApplyBranding
            _caption.text = string.Empty;

            // Engraved channel (the same plaque material as buttons / panels), 9-sliced.
            var chanGo = new GameObject("Channel", typeof(RectTransform), typeof(Image));
            var chan = (RectTransform)chanGo.transform;
            chan.SetParent(bar, false);
            chan.anchorMin = new Vector2(0f, 0.5f); chan.anchorMax = new Vector2(1f, 0.5f);
            chan.pivot = new Vector2(0.5f, 0.5f);
            chan.sizeDelta = new Vector2(0f, 34f);
            chan.anchoredPosition = Vector2.zero;
            var chanImg = chanGo.GetComponent<Image>();
            chanImg.sprite = PanelShapes.Plaque;
            chanImg.type = Image.Type.Sliced;
            chanImg.color = Color.white;
            chanImg.raycastTarget = true;   // swallow taps while loading

            // Inner area the fill grows inside (inset from the bronze edge).
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            _barInner = (RectTransform)innerGo.transform;
            _barInner.SetParent(chan, false);
            _barInner.anchorMin = new Vector2(0f, 0f); _barInner.anchorMax = new Vector2(1f, 1f);
            _barInner.offsetMin = new Vector2(7f, 7f); _barInner.offsetMax = new Vector2(-7f, -7f);

            // Molten-bronze fill: left-anchored, width driven by anchorMax.x; a horizontal gradient
            // sprite brightens toward the leading edge.
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            _fill = (RectTransform)fillGo.transform;
            _fill.SetParent(_barInner, false);
            _fill.anchorMin = new Vector2(0f, 0f); _fill.anchorMax = new Vector2(0f, 1f);
            _fill.offsetMin = Vector2.zero; _fill.offsetMax = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = BarShapes.Fill;
            fillImg.type = Image.Type.Simple;
            fillImg.raycastTarget = false;   // color (theme accent) set in ApplyBranding

            // Marker riding the leading edge. The sprite + tint are theme-driven (ApplyBranding): a game's
            // loadingMarker when set, else a plain procedural seal - never a game-specific shape by default.
            var headGo = new GameObject("Head", typeof(RectTransform), typeof(Image));
            _head = (RectTransform)headGo.transform;
            _head.SetParent(chan, false);
            _head.anchorMin = _head.anchorMax = new Vector2(0f, 0.5f);
            _head.pivot = new Vector2(0.5f, 0.5f);
            _head.sizeDelta = new Vector2(52f, 52f);
            var headImg = headGo.GetComponent<Image>();
            headImg.raycastTarget = false;

            // Percent readout, carved serif, below the channel.
            var pctGo = new GameObject("Percent", typeof(RectTransform), typeof(TextMeshProUGUI));
            var pctRt = (RectTransform)pctGo.transform;
            pctRt.SetParent(bar, false);
            pctRt.anchorMin = new Vector2(0f, 0f); pctRt.anchorMax = new Vector2(1f, 0f);
            pctRt.pivot = new Vector2(0.5f, 1f);
            pctRt.sizeDelta = new Vector2(0f, 38f);
            pctRt.anchoredPosition = new Vector2(0f, -8f);
            _percent = pctGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(_percent);
            _percent.fontSize = 28; _percent.fontStyle = FontStyles.Bold; _percent.characterSpacing = 2f;
            _percent.alignment = TextAlignmentOptions.Center;
            _percent.raycastTarget = false;   // color set in ApplyBranding
            _percent.text = "0%";

            ApplyBranding();
        }

        // Themes the progress marker + fill + text from the current theme (see THEMING.md - neutral default,
        // no game-specific identity baked in). Idempotent; called from BuildBar and again from Run so a
        // theme set after build, or a scene-baked hierarchy, still gets the right look.
        private void ApplyBranding()
        {
            Color accent = _theme != null ? _theme.accent : ThemeDefaults.Accent;
            Sprite marker = _theme != null ? _theme.loadingMarker : null;

            if (_head != null)
            {
                var headImg = _head.GetComponent<Image>();
                if (headImg != null)
                {
                    // A game's own marker art (drawn at true colors), else a plain procedural seal tinted to the accent.
                    headImg.sprite = marker != null ? marker : PortraitShapes.Disc;
                    headImg.color = marker != null ? Color.white : Brighten(accent, 0.4f);
                }
            }
            // The fill is a neutral luminance gradient tinted to the accent, so it follows the theme
            // (neutral by default, bronze under the medieval skin) instead of a hardcoded bronze->gold.
            if (_fill != null)
            {
                var fillImg = _fill.GetComponent<Image>();
                if (fillImg != null) fillImg.color = Brighten(accent, 0.15f);
            }
            if (_caption != null) _caption.color = new Color(0.92f, 0.92f, 0.94f, 0.92f);
            if (_percent != null) _percent.color = Brighten(accent, 0.35f);
        }

        private static Color Brighten(Color c, float a) =>
            new Color(Mathf.Clamp01(c.r + a), Mathf.Clamp01(c.g + a), Mathf.Clamp01(c.b + a), c.a);

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }

    // Procedural horizontal gradient for the progress fill: a neutral luminance ramp (dim on the left
    // brightening to full at the leading edge) that the LoadingScreen tints to the theme accent, so the bar
    // follows the theme instead of a hardcoded bronze->gold. Built once, cached.
    internal static class BarShapes
    {
        private static Sprite _fill;
        public static Sprite Fill => _fill != null ? _fill : (_fill = BuildFill(64));

        private static Sprite BuildFill(int n)
        {
            var tex = new Texture2D(n, 4, TextureFormat.RGBA32, false, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var lo = new Color(0.35f, 0.35f, 0.35f, 1f);   // dim (trailing)
            var hi = new Color(1f, 1f, 1f, 1f);            // bright (leading) - tinted by the accent at runtime
            var px = new Color[n * 4];
            for (int x = 0; x < n; x++)
            {
                float t = x / (float)(n - 1);
                Color c = Color.Lerp(lo, hi, t * t);       // bias the glow toward the leading edge
                for (int y = 0; y < 4; y++) px[y * n + x] = c;
            }
            tex.SetPixels(px); tex.Apply(false, false);
            // HideAndDontSave so the tex + sprite survive a domain reload; else the Editor destroys them on
            // recompile and the cached Image.sprite becomes null -> a solid white bar while developing.
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, 4), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
