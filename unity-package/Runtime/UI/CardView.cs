using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // הצגת קלף בודד (ספק 12.3). מקבל EventNodeView + Theme - חסר-ידע על תוכן ספציפי.
    // נושא גם את משוב הגרירה (§10.1): הקלף עוקב אחרי המצביע ומתנדנד מעט בזמן swipe.
    // טקסט דרך TextMeshPro (תמיכת RTL/עברית, §10.6).
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text speakerLabel;   // speaker name badge at the top (hidden for the narrator)
        [SerializeField] private Image speakerIcon;
        [SerializeField] private TMP_Text leftLabel;      // persistent left choice hint (bottom-left)
        [SerializeField] private TMP_Text rightLabel;     // persistent right choice hint (bottom-right)
        [SerializeField] private Image cardBackground;

        private const float EnterDuration = 0.18f;
        private RectTransform _rt;
        private Coroutine _enter;
        private Image _portraitImg;       // the speaker icon masked to a circle (child of speakerIcon)
        private Image _portraitRing;      // bronze medallion ring drawn on top, unmasked
        private Color _choiceColor = Color.white;   // theme text color the active hint brightens toward
        private Color _choiceHint = ThemeDefaults.ChoiceHint;   // resting choice-label color (token)
        private Color _choiceGlow = ThemeDefaults.ChoiceGlow;   // active choice glow-halo (token)
        private Image _leftBg, _rightBg;            // choice plaque backgrounds, cached for the drag glow
        private TMP_Text _leftPreview, _rightPreview;   // per-side projected-delta preview, shown above the active plaque
        private RectTransform Rt => _rt != null ? _rt : (_rt = (RectTransform)transform);

        // Pluggable selection feedback (the grow/whiten/glow when a side is dragged). A game can drop its
        // own CardChoiceFeedback component on the Card to fully replace it (e.g. a warm light sweep); with
        // none present the engine runs DefaultCardChoiceFeedback. Re-resolved on each Bind so a component
        // added in the scene is picked up. The card's slide/tilt stays engine core (a format affordance).
        private ICardChoiceFeedback _feedback;
        private DefaultCardChoiceFeedback _defaultFeedback;
        private ICardChoiceFeedback Feedback
        {
            get
            {
                if (_feedback != null) return _feedback;
                var custom = GetComponent<CardChoiceFeedback>() as ICardChoiceFeedback;
                return _feedback = custom ?? (_defaultFeedback ?? (_defaultFeedback = new DefaultCardChoiceFeedback()));
            }
        }

        // Read-only surface the feedback strategy drives: the two choice plaques, their labels, and the
        // theme text color the active hint brightens toward.
        public Image LeftPlaque => _leftBg;
        public Image RightPlaque => _rightBg;
        public TMP_Text LeftChoiceLabel => leftLabel;
        public TMP_Text RightChoiceLabel => rightLabel;
        public Color ChoiceTextColor => _choiceColor;
        public Color ChoiceHintColor => _choiceHint;   // resting choice-hint color (token; default neutral)
        public Color ChoiceGlowColor => _choiceGlow;   // active glow-halo color (token; default neutral)

        // Swipe affordance (onboarding): on a fresh card left untouched for a beat, a touch-puck drifts
        // left<->right between two chevrons to teach the genre's swipe to first-time players. It hides the
        // instant the card is dragged or a new card binds, and only ever runs at play time.
        private const float HintDelay = 1.6f;      // idle seconds before the hint fades in
        private const float HintPeriod = 1.5f;     // seconds for one full left->right->left cycle
        private const float HintAmplitude = 80f;   // px the puck travels to each side
        private RectTransform _hintPuck;
        private CanvasGroup _hintGroup;
        private Image _hintLeft, _hintRight;
        private float _idle;            // seconds since the last interaction / new card
        private bool _interactable;     // a real two-choice card is bound, at play time
        private bool _hintShown;

        public void Bind(EventNodeView view, Theme theme)
        {
            _feedback = null;   // re-resolve the pluggable feedback (a game may have added its component)
            if (bodyText != null) bodyText.text = view.Body;
            if (leftLabel != null) leftLabel.text = view.LeftLabel;
            if (rightLabel != null) rightLabel.text = view.RightLabel;

            // Speaker badge: shown for a named speaker, hidden for the narrator / empty (cleaner card).
            bool named = !string.IsNullOrEmpty(view.Speaker) && view.Speaker != "narrator";
            var speaker = theme != null ? theme.GetSpeaker(view.Speaker) : null;

            if (speakerLabel != null)
            {
                speakerLabel.gameObject.SetActive(named);
                if (named) StyleSpeakerName(speakerLabel, view.Speaker, theme);
                else { var sp = transform.Find("SpeakerPlate"); if (sp != null) sp.gameObject.SetActive(false); }
            }

            // Speaker portrait (Theme.SpeakerStyle.icon): shown when this speaker has one, as a circular
            // medallion (the icon masked to a circle inside a bronze ring) rather than a bare rectangle.
            bool portrait = named && speaker != null && speaker.icon != null;
            if (speakerIcon != null)
            {
                speakerIcon.gameObject.SetActive(portrait);
                if (portrait) ApplyRoundPortrait(speaker.icon, theme);
            }

            // Horizontal text inset keeps the body clear of the decorative frame on both card kinds.
            // Portrait card: body sits below the medallion. Narrator card: a centered band so the
            // narration reads as the centerpiece instead of pooling low with an empty top (ux re-review).
            if (bodyText != null)
            {
                var brt = bodyText.rectTransform;
                // Portrait card: a taller band just under the medallion/name, so the narration fills the
                // space instead of floating low with empty gaps above and below it (ux re-review round 2).
                brt.anchorMin = new Vector2(0.14f, portrait ? 0.24f : 0.31f);
                brt.anchorMax = new Vector2(0.86f, portrait ? 0.54f : 0.80f);
            }

            // Choice hints: an engraved stone plaque per side, with the label auto-sized + wrapped so a
            // long option always stays inside its plaque instead of spilling past it (card feedback).
            StyleChoice("ChoiceLeftBg", leftLabel, true);
            StyleChoice("ChoiceRightBg", rightLabel, false);

            if (theme != null)
            {
                _choiceColor = theme.text;
                _choiceHint = theme.ChoiceHint;   // resting / active choice-hint tokens the feedback reads
                _choiceGlow = theme.ChoiceGlow;
                if (cardBackground != null)
                {
                    // Card art when the theme has it (drawn at true colors), else the flat card color.
                    cardBackground.sprite = theme.cardArt;
                    cardBackground.color = theme.cardArt != null ? Color.white : theme.card;
                }
                if (bodyText != null) bodyText.color = theme.text;
            }

            // A real, swipeable card has two choices and we are at play time; only then teach the swipe.
            _interactable = Application.isPlaying
                && !string.IsNullOrEmpty(view.LeftLabel) && !string.IsNullOrEmpty(view.RightLabel);
            _idle = 0f;
            HideHint();

            RemoveLegacyPreviewDelta();   // drop the old on-card delta label if the scene still carries one
            ResetDrag(); // new card - centered final state (also valid in edit/tests)
            // אנימציית-כניסה ב-play בלבד (§10.1 feel). ב-edit ה-Bind משאיר את הקלף במרכז.
            if (Application.isPlaying)
            {
                if (_enter != null) StopCoroutine(_enter);
                _enter = StartCoroutine(EnterAnimation());
            }
        }

        // Drag feedback (§10.1): the card slides horizontally and tilts by the drag strength (fraction
        // 0..1). The active side's choice hint brightens so the player reads what they are choosing.
        public void ApplyDrag(ChoiceSide side, float fraction)
        {
            StopEnter();
            _idle = 0f;            // the player is interacting - drop the swipe hint and restart the idle clock
            if (_hintShown) HideHint();
            float dir = side == ChoiceSide.Left ? -1f : 1f;
            float f = Mathf.Clamp01(fraction);
            // Kept modest so a large card tilts in place without sliding off-screen. A gentle 3deg tilt
            // (was 5deg) reads as restrained/regal and cuts the per-frame resampling shimmer (ux re-review).
            Rt.anchoredPosition = new Vector2(dir * f * 120f, 0f);
            Rt.localRotation = Quaternion.Euler(0f, 0f, -dir * f * 3f);

            // The selection emphasis (which side lights up and how) is delegated so a game can replace it.
            Feedback.ApplyDrag(this, side, f);
        }

        // Projected-delta preview (§10.3), restored on the card per user feedback: the meters the chosen
        // option will change are shown right above that side's plaque (the other side stays clear). Each
        // line is colored green for a gain / red for a loss so the cost reads at a glance.
        public void ShowPreviewDeltas(string text, ChoiceSide side)
        {
            string colored = ColorizeDeltas(text);
            bool leftActive = side == ChoiceSide.Left;
            if (_leftPreview != null) _leftPreview.text = leftActive ? colored : string.Empty;
            if (_rightPreview != null) _rightPreview.text = leftActive ? string.Empty : colored;
        }

        // Wraps each "Label +N" / "Label -N" line from ViewMapper.FormatDeltas in a green/red rich-text
        // color tag (a '+' marks a gain). Returns empty for empty input.
        private static string ColorizeDeltas(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string hex = lines[i].IndexOf('+') >= 0 ? "#73E68C" : "#FF6B6B";
                lines[i] = "<color=" + hex + ">" + lines[i] + "</color>";
            }
            return string.Join("\n", lines);
        }

        public void ResetDrag()
        {
            Rt.anchoredPosition = Vector2.zero;
            Rt.localRotation = Quaternion.identity;
            Rt.localScale = Vector3.one;
            // Drop both plaques/labels back to their resting state (via the active feedback) and clear the
            // on-card delta preview.
            Feedback.Reset(this);
            if (_leftPreview != null) _leftPreview.text = string.Empty;
            if (_rightPreview != null) _rightPreview.text = string.Empty;
        }

        // Drives the idle swipe hint: once a fresh card has sat untouched past HintDelay, fade the puck in
        // and oscillate it. Cheap no-op on non-interactable / edit-mode cards (no hint objects are built).
        private void Update()
        {
            if (!_interactable) return;
            _idle += Time.deltaTime;
            if (_idle < HintDelay) return;
            if (!_hintShown) ShowHint();
            AnimateHint(_idle - HintDelay);
        }

        private void ShowHint()
        {
            EnsureSwipeHint();
            _hintGroup.gameObject.SetActive(true);
            _hintShown = true;
        }

        private void HideHint()
        {
            if (_hintGroup != null) _hintGroup.gameObject.SetActive(false);
            _hintShown = false;
        }

        // Slides the touch-puck on a sine, fading in over the first beat. The chevron toward which the puck
        // is moving brightens, so both swipe directions read clearly as the puck travels back and forth.
        private void AnimateHint(float t)
        {
            if (_hintGroup == null) return;
            float fade = Mathf.Clamp01(t / 0.35f);
            _hintGroup.alpha = fade;
            float ang = t / HintPeriod * Mathf.PI * 2f;
            _hintPuck.anchoredPosition = new Vector2(Mathf.Sin(ang) * HintAmplitude, 0f);
            float dir = Mathf.Cos(ang);   // >0 moving right, <0 moving left
            if (_hintRight != null) _hintRight.color = HintChevronColor(Mathf.Clamp01(dir), fade);
            if (_hintLeft != null) _hintLeft.color = HintChevronColor(Mathf.Clamp01(-dir), fade);
        }

        private static Color HintChevronColor(float lit, float fade) =>
            new Color(1f, 0.92f, 0.78f, (0.22f + 0.6f * lit) * fade);

        // Builds the swipe-hint overlay once (a centered touch-puck flanked by two chevrons). Sits just
        // BELOW the card so it never covers the body/choice text. Raycast-transparent so it can't intercept
        // a drag. Anchored to the card's bottom edge with a top pivot, so it hangs into the gap under the card.
        private void EnsureSwipeHint()
        {
            if (_hintGroup != null) return;

            var go = new GameObject("SwipeHint", typeof(RectTransform), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // bottom-center of the card
            rt.pivot = new Vector2(0.5f, 1f);                       // hang downward from that edge
            rt.sizeDelta = new Vector2(320f, 80f);
            rt.anchoredPosition = new Vector2(0f, -36f);            // tucked into the gap just under the card
            _hintGroup = go.GetComponent<CanvasGroup>();
            _hintGroup.interactable = false;
            _hintGroup.blocksRaycasts = false;   // never eat a swipe
            _hintGroup.alpha = 0f;

            _hintLeft = MakeChevron(rt, -120f, true);
            _hintRight = MakeChevron(rt, 120f, false);

            var puckGo = new GameObject("Puck", typeof(RectTransform), typeof(Image));
            _hintPuck = (RectTransform)puckGo.transform;
            _hintPuck.SetParent(rt, false);
            _hintPuck.sizeDelta = new Vector2(48f, 48f);
            _hintPuck.anchoredPosition = Vector2.zero;
            var pimg = puckGo.GetComponent<Image>();
            pimg.sprite = PortraitShapes.Disc;
            pimg.color = new Color(1f, 0.95f, 0.84f, 0.9f);
            pimg.raycastTarget = false;
        }

        private static Image MakeChevron(RectTransform parent, float x, bool pointLeft)
        {
            var go = new GameObject(pointLeft ? "ChevronL" : "ChevronR", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(46f, 46f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.localScale = new Vector3(pointLeft ? -1f : 1f, 1f, 1f);   // mirror the ">" sprite to point left
            var img = go.GetComponent<Image>();
            img.sprite = HintShapes.Chevron;
            img.color = HintChevronColor(0f, 0f);
            img.raycastTarget = false;
            return img;
        }

        // Title-cases a speaker id for the badge (e.g. "treasurer" -> "Treasurer").
        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        // Gives a choice a stone plaque (engraved bronze-edged panel) and lays its label out to fit inside.
        // Both the plaque AND its label are anchored here to the same rect, kept inside the card's engraved
        // border (x ~0.09..0.91) with a centre gap for the wax seal and raised clear of the bottom border,
        // so a plaque never spills over the frame metalwork or collides with the seal (card feedback).
        private void StyleChoice(string bgName, TMP_Text label, bool leftSide)
        {
            Vector2 aMin = new Vector2(leftSide ? 0.12f : 0.53f, 0.145f);
            Vector2 aMax = new Vector2(leftSide ? 0.47f : 0.88f, 0.235f);

            var t = transform.Find(bgName);
            var bg = t != null ? t.GetComponent<Image>() : null;
            if (bg != null)
            {
                var brt = bg.rectTransform;
                brt.anchorMin = aMin; brt.anchorMax = aMax;
                brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                brt.localScale = Vector3.one;
                bg.sprite = PanelShapes.Plaque;
                bg.type = Image.Type.Sliced;     // 9-sliced so the engraved border stays crisp at any width
                bg.color = Color.white;          // the plaque sprite already carries its dark fill + bronze edge
                bg.raycastTarget = false;
            }
            if (leftSide) _leftBg = bg; else _rightBg = bg;

            if (label != null)
            {
                var lrt = label.rectTransform;
                lrt.anchorMin = aMin; lrt.anchorMax = aMax;
                lrt.offsetMin = new Vector2(14f, 2f); lrt.offsetMax = new Vector2(-14f, -2f);   // padding inside the plaque
                lrt.localScale = Vector3.one;
                label.transform.SetAsLastSibling();   // keep the text above its own plaque background
                label.alignment = TextAlignmentOptions.Center;   // centered inside the plaque, not pushed to an edge
                label.enableWordWrapping = true;
                label.enableAutoSizing = true;   // shrink/wrap a long option until it fits the plaque
                label.fontSizeMin = 16f;
                label.fontSizeMax = 28f;
            }

            // The projected meter deltas for this side sit just above the plaque, so a drag shows both the
            // option text and what it will cost/gain (restored on-card preview, per card feedback).
            var preview = EnsureChoicePreview(bgName + "Preview", aMin.x, aMax.x, label);
            if (leftSide) _leftPreview = preview; else _rightPreview = preview;
        }

        // Builds (once) a per-side delta-preview label hovering just above the choice plaque. Inherits the
        // card font from the choice label so it matches, and starts empty (a drag fills it via ShowPreviewDeltas).
        private TMP_Text EnsureChoicePreview(string name, float xMin, float xMax, TMP_Text fontSource)
        {
            var found = transform.Find(name);
            TMP_Text lbl = found != null ? found.GetComponent<TMP_Text>() : null;
            if (lbl == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                lbl = go.AddComponent<TextMeshProUGUI>();
                UIFonts.Apply(lbl);
            }
            if (fontSource != null && fontSource.font != null) lbl.font = fontSource.font;
            var rt = lbl.rectTransform;
            rt.anchorMin = new Vector2(xMin, 0.243f);
            rt.anchorMax = new Vector2(xMax, 0.315f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Bottom;
            lbl.enableWordWrapping = false;
            lbl.richText = true;
            lbl.raycastTarget = false;
            lbl.fontStyle = FontStyles.Bold;
            lbl.enableAutoSizing = true;
            lbl.fontSizeMin = 13f; lbl.fontSizeMax = 21f;
            lbl.lineSpacing = -12f;   // tighten the 1-2 delta lines so they read as one compact chip
            lbl.text = string.Empty;
            lbl.transform.SetAsLastSibling();
            return lbl;
        }

        // Renders the speaker name as a carved bronze inscription: spaced bold caps on a small stone plate.
        private void StyleSpeakerName(TMP_Text label, string speaker, Theme theme)
        {
            label.text = (speaker ?? string.Empty).ToUpperInvariant();
            label.characterSpacing = 8f;
            label.fontStyle = FontStyles.Bold;
            label.color = theme != null ? theme.accent : new Color(0.62f, 0.50f, 0.28f);
            // Pin the name into the plate band and auto-shrink it so a long id (PHYSICIAN/TREASURER) with
            // the wide letter-spacing never spills past the plate's bronze edge.
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.31f, 0.553f);
            lrt.anchorMax = new Vector2(0.69f, 0.607f);
            lrt.offsetMin = new Vector2(10f, 0f); lrt.offsetMax = new Vector2(-10f, 0f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 26f;
            EnsureSpeakerPlate(label);
        }

        // A small engraved nameplate sitting behind the speaker name (drawn just under it, over the card).
        private void EnsureSpeakerPlate(TMP_Text label)
        {
            var found = transform.Find("SpeakerPlate");
            RectTransform plate;
            if (found != null) plate = (RectTransform)found;
            else
            {
                var go = new GameObject("SpeakerPlate", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = PanelShapes.Plaque;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                img.raycastTarget = false;
                plate = (RectTransform)go.transform;
            }
            // Keep the plate directly BEHIND the name, deterministically. SetSiblingIndex(nameIndex) alone
            // is order-dependent: when the plate starts earlier in the hierarchy (as a scene/prefab-baked
            // plate does) it lands in FRONT of the name and covers the inscription - the scene-vs-play swap.
            // So move the plate to the name's slot, then push the name just in front of it. The portrait
            // medallion overlaps the plate's top edge, so it must stay in front of the plate as well.
            plate.SetSiblingIndex(label.transform.GetSiblingIndex());
            label.transform.SetSiblingIndex(plate.GetSiblingIndex() + 1);
            if (speakerIcon != null && speakerIcon.transform.GetSiblingIndex() <= plate.GetSiblingIndex())
                speakerIcon.transform.SetAsLastSibling();
            plate.anchorMin = new Vector2(0.30f, 0.55f);
            plate.anchorMax = new Vector2(0.70f, 0.61f);
            plate.offsetMin = Vector2.zero; plate.offsetMax = Vector2.zero;
            plate.gameObject.SetActive(true);
        }

        // Removes a legacy scene-baked on-card delta label (superseded by the meter preview), once.
        private void RemoveLegacyPreviewDelta()
        {
            var found = transform.Find("PreviewDelta");
            if (found != null) DestroyImmediate(found.gameObject);
        }

        // Builds the circular medallion around the bare speakerIcon. The portrait is clipped by a Mask
        // that lives on an INSET child sized to the frame's inner opening (not the full medallion), so the
        // face never spills out under the ornate laurel band. The frame ring stays full-size and unmasked
        // on top. Idempotent and edit-mode safe (also migrates the legacy full-size-masked layout).
        private void ApplyRoundPortrait(Sprite portraitSprite, Theme theme)
        {
            float size = theme != null ? theme.MedallionSize : ThemeDefaults.PortraitSize;   // theme-configurable medallion diameter
            var rt = speakerIcon.rectTransform;
            // Force a square rect (centered in the upper card) so the medallion is round, not an ellipse.
            rt.anchorMin = new Vector2(0.5f, 0.72f);
            rt.anchorMax = new Vector2(0.5f, 0.72f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            // Pin the medallion's BOTTOM edge (not its center) so resizing grows/shrinks it upward into the
            // card art instead of overlapping the name plate + body below it. At the default 250 this is +22,
            // matching the original hand-tuned lift; a smaller/larger medallion keeps the same clear bottom.
            rt.anchoredPosition = new Vector2(0f, size * 0.5f - (ThemeDefaults.PortraitSize * 0.5f - 22f));

            bool ornate = theme != null && theme.speakerFrame != null;
            // Clip the portrait to the frame's clear opening. An ornate frame's laurel begins near the rim;
            // innerFraction (default 0.78) tucks the portrait edge just under its inner lip (the ring on top
            // hides the overlap) and is theme-configurable per frame asset. A plain procedural ring sits at the
            // rim, so there the portrait fills the disc.
            float innerFrac = ornate ? (theme != null ? theme.MedallionInnerFraction : ThemeDefaults.InnerFraction) : 1f;

            // speakerIcon is now just the medallion container - the mask moved to an inset child so the
            // frame ring can stay full-size and unmasked. Strip the legacy mask/disc off the container.
            var legacyMask = speakerIcon.GetComponent<Mask>();
            if (legacyMask != null) DestroyImmediate(legacyMask);
            speakerIcon.sprite = null;
            speakerIcon.color = new Color(1f, 1f, 1f, 0f);   // invisible container
            speakerIcon.raycastTarget = false;

            // Inset circular mask: a disc sized to the frame opening, clipping the portrait to that circle.
            var pmT = speakerIcon.transform.Find("PortraitMask");
            Image pmImg = pmT != null ? pmT.GetComponent<Image>()
                : new GameObject("PortraitMask", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            pmImg.transform.SetParent(speakerIcon.transform, false);
            var pmRt = pmImg.rectTransform;
            pmRt.anchorMin = new Vector2(0.5f, 0.5f); pmRt.anchorMax = new Vector2(0.5f, 0.5f);
            pmRt.pivot = new Vector2(0.5f, 0.5f);
            pmRt.sizeDelta = new Vector2(size * innerFrac, size * innerFrac);
            pmRt.anchoredPosition = Vector2.zero;
            pmRt.SetAsFirstSibling();   // behind the ring
            pmImg.sprite = PortraitShapes.Disc;
            pmImg.color = Color.white;
            pmImg.type = Image.Type.Simple;
            pmImg.preserveAspect = false;
            pmImg.raycastTarget = false;
            var mask = pmImg.GetComponent<Mask>();
            if (mask == null) mask = pmImg.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // The portrait fills the inset mask (reparented from any legacy direct-child position).
            var portraitT = pmImg.transform.Find("Portrait") ?? speakerIcon.transform.Find("Portrait");
            _portraitImg = portraitT != null ? portraitT.GetComponent<Image>()
                : new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _portraitImg.transform.SetParent(pmImg.transform, false);
            StretchFull(_portraitImg.rectTransform);
            _portraitImg.sprite = portraitSprite;
            _portraitImg.preserveAspect = false;   // fill the inner circle - no gaps at the opening
            _portraitImg.raycastTarget = false;
            _portraitImg.maskable = true;
            _portraitImg.color = Color.white;

            // The frame ring stays full-size and unmasked, drawn on top of the masked portrait.
            var ringT = speakerIcon.transform.Find("Ring");
            _portraitRing = ringT != null ? ringT.GetComponent<Image>()
                : new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _portraitRing.transform.SetParent(speakerIcon.transform, false);
            StretchFull(_portraitRing.rectTransform);
            _portraitRing.transform.SetAsLastSibling();
            // An ornate engraved medallion frame from the theme when present, else a plain procedural ring
            // whose band width + color are theme-configurable (a game can soften the default ring).
            float ringThickness = theme != null ? theme.MedallionRingThickness : ThemeDefaults.RingThickness;
            _portraitRing.sprite = ornate ? theme.speakerFrame : PortraitShapes.RingOf(ringThickness);
            _portraitRing.preserveAspect = false;
            _portraitRing.raycastTarget = false;
            _portraitRing.maskable = false;        // drawn over the mask boundary, not clipped by it
            _portraitRing.color = ornate ? Color.white : (theme != null ? theme.MedallionRingColor : ThemeDefaults.Accent);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void StopEnter()
        {
            if (_enter != null) { StopCoroutine(_enter); _enter = null; Rt.localScale = Vector3.one; }
        }

        private IEnumerator EnterAnimation()
        {
            var rt = Rt;
            float t = 0f;
            while (t < EnterDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / EnterDuration);
                float e = 1f - (1f - k) * (1f - k);   // ease-out quad
                rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-50f, 0f, e));
                rt.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, e);
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            _enter = null;
        }
    }

    // Procedural medallion shapes for the speaker portrait: a filled disc (the circular mask) and a
    // bronze ring drawn on top. Built once and cached - perfect anti-aliased alpha, no art asset to
    // import or cut. The ring stores a grey radial shade that the Image color tints to bronze metal.
    internal static class PortraitShapes
    {
        private static Sprite _disc;
        // Rings are cached per band-width (the theme can pick the thickness), keyed by the rounded fraction
        // so a handful of distinct widths never rebuild the texture each bind.
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _rings = new System.Collections.Generic.Dictionary<int, Sprite>();
        public static Sprite Disc => _disc != null ? _disc : (_disc = BuildDisc(256));
        public static Sprite Ring => RingOf(0.06f);

        // A procedural ring with the given band width as a fraction of the radius (default look = 0.06).
        public static Sprite RingOf(float thicknessFrac)
        {
            if (thicknessFrac <= 0f) thicknessFrac = 0.06f;
            int key = Mathf.RoundToInt(Mathf.Clamp(thicknessFrac, 0.01f, 0.4f) * 1000f);
            if (_rings.TryGetValue(key, out var s)) return s;
            s = BuildRing(256, key / 1000f);
            _rings[key] = s;
            return s;
        }

        private static Sprite BuildDisc(int n)
        {
            var tex = NewTex(n);
            float c = (n - 1) * 0.5f, r = n * 0.5f - 1f;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));   // 1px anti-aliased edge
                }
            tex.SetPixels(px); tex.Apply(false, false);
            return ToSprite(tex, n);
        }

        private static Sprite BuildRing(int n, float thicknessFrac)
        {
            var tex = NewTex(n);
            float c = (n - 1) * 0.5f, outer = n * 0.5f - 1f, inner = outer - n * thicknessFrac;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(Mathf.Min(outer - d, d - inner));
                    float shade = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01((d - inner) / (outer - inner)));   // metallic radial shading
                    px[y * n + x] = new Color(shade, shade, shade, a);
                }
            tex.SetPixels(px); tex.Apply(false, false);
            return ToSprite(tex, n);
        }

        private static Texture2D NewTex(int n) =>
            new Texture2D(n, n, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };

        private static Sprite ToSprite(Texture2D tex, int n) =>
            Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // A procedural 9-sliced stone plaque: a dark translucent rounded panel with a thin bronze engraved
    // edge. Used for the choice hints and the speaker nameplate so they read as inset card material.
    internal static class PanelShapes
    {
        private static Sprite _plaque;
        public static Sprite Plaque => _plaque != null ? _plaque : (_plaque = BuildPlaque(64, 16));

        private static Sprite BuildPlaque(int n, int border)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (n - 1) * 0.5f;
            float cr = 13f;              // corner radius
            float bx = (n * 0.5f - 1f) - cr;   // straight half-extent of the inner box
            float bt = 3f;              // bronze edge thickness
            var fill = new Color(0.07f, 0.06f, 0.05f, 0.62f);
            var edge = new Color(0.55f, 0.45f, 0.26f, 0.95f);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float qx = Mathf.Max(Mathf.Abs(x - c) - bx, 0f);
                    float qy = Mathf.Max(Mathf.Abs(y - c) - bx, 0f);
                    float sd = Mathf.Sqrt(qx * qx + qy * qy) - cr;   // rounded-rect SDF (negative inside)
                    Color col = sd >= -bt ? edge : fill;             // outer band -> bronze, interior -> dark
                    col.a *= Mathf.Clamp01(0.5f - sd);               // 1px anti-aliased outer edge
                    px[y * n + x] = col;
                }
            tex.SetPixels(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }

    // A procedural ">" chevron (two anti-aliased strokes meeting at a point), built once and cached.
    // The swipe hint mirrors it on X to make the matching "<". White so the Image color tints it.
    internal static class HintShapes
    {
        private static Sprite _chevron;
        public static Sprite Chevron => _chevron != null ? _chevron : (_chevron = BuildChevron(64));

        private static Sprite BuildChevron(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var top = new Vector2(n * 0.32f, n * 0.80f);
            var tip = new Vector2(n * 0.70f, n * 0.50f);
            var bot = new Vector2(n * 0.32f, n * 0.20f);
            float half = n * 0.085f;   // stroke half-width
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(SegDist(p, top, tip), SegDist(p, tip, bot));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(half - d + 0.5f));   // 1px AA edge
                }
            tex.SetPixels(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a, ap = p - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
