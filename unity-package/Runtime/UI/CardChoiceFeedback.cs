using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // The choice-selection emphasis a CardView shows while a side is dragged (which plaque lights up and
    // how). The engine ships a default; a game overrides the *look* of the selection without touching engine
    // code by dropping a CardChoiceFeedback component on the same GameObject as the CardView. The card's
    // slide/tilt stays in CardView - that is a format affordance, not a theme decision.
    public interface ICardChoiceFeedback
    {
        // Called each drag frame. fraction is the 0..1 drag strength toward `side`.
        void ApplyDrag(CardView card, ChoiceSide side, float fraction);
        // Called when the card returns to rest (drag cancelled / new card) - restore the neutral look.
        void Reset(CardView card);
    }

    // Author a game-specific selection effect by subclassing this and adding it to the Card GameObject.
    // CardView discovers it via GetComponent and uses it in place of DefaultCardChoiceFeedback.
    public abstract class CardChoiceFeedback : MonoBehaviour, ICardChoiceFeedback
    {
        public abstract void ApplyDrag(CardView card, ChoiceSide side, float fraction);
        public abstract void Reset(CardView card);
    }

    // The built-in effect (unchanged from what CardView used to do inline): the chosen plaque tints warm
    // gold, swells, and gains a bronze glow-outline that grows with the drag; the other side recedes; the
    // active choice label brightens toward the theme text color. Used whenever no CardChoiceFeedback is set.
    internal sealed class DefaultCardChoiceFeedback : ICardChoiceFeedback
    {
        // Colors come from the card's resolved theme tokens (ChoiceHint / ChoiceGlow) so a skin repaints the
        // effect as data; the plaque lift/recede tints stay engine constants (neutral, subtle).

        public void ApplyDrag(CardView card, ChoiceSide side, float fraction)
        {
            float f = Mathf.Clamp01(fraction);
            Color hint = card.ChoiceHintColor, glow = card.ChoiceGlowColor;
            bool leftActive = side == ChoiceSide.Left;
            TMP_Text active = leftActive ? card.LeftChoiceLabel : card.RightChoiceLabel;
            TMP_Text other = leftActive ? card.RightChoiceLabel : card.LeftChoiceLabel;
            if (active != null) active.color = Color.Lerp(hint, card.ChoiceTextColor, f);
            if (other != null) other.color = hint;

            ApplyPlaqueGlow(card.LeftPlaque, card.LeftChoiceLabel, glow, leftActive ? f : 0f, !leftActive && f > 0.001f);
            ApplyPlaqueGlow(card.RightPlaque, card.RightChoiceLabel, glow, leftActive ? 0f : f, leftActive && f > 0.001f);
        }

        public void Reset(CardView card)
        {
            Color hint = card.ChoiceHintColor, glow = card.ChoiceGlowColor;
            if (card.LeftChoiceLabel != null) card.LeftChoiceLabel.color = hint;
            if (card.RightChoiceLabel != null) card.RightChoiceLabel.color = hint;
            ApplyPlaqueGlow(card.LeftPlaque, card.LeftChoiceLabel, glow, 0f, false);
            ApplyPlaqueGlow(card.RightPlaque, card.RightChoiceLabel, glow, 0f, false);
        }

        // Lights a single choice plaque by its drag fraction: tint toward the warm-lift, scale up, and a
        // glow-outline (in the theme's choiceGlow) whose spread grows with the drag. A dimmed (non-active
        // during a drag) plaque recedes.
        private static void ApplyPlaqueGlow(Image bg, TMP_Text label, Color glow, float active, bool dimmed)
        {
            float s = 1f + 0.09f * active;
            if (bg != null)
            {
                bg.color = dimmed ? ThemeDefaults.DimPlaque : Color.Lerp(Color.white, ThemeDefaults.WarmPlaque, active);
                bg.rectTransform.localScale = new Vector3(s, s, 1f);
                var ol = bg.GetComponent<Outline>();
                if (active > 0.001f)
                {
                    // The glow is the Outline's offset copies drawn in the theme glow color: a wide halo so
                    // the chosen plaque clearly lights up (tinting the dark plaque itself can only darken it).
                    if (ol == null) ol = bg.gameObject.AddComponent<Outline>();
                    ol.effectColor = new Color(glow.r, glow.g, glow.b, 0.9f * active);
                    float d = 8f * active;
                    ol.effectDistance = new Vector2(d, -d);
                    ol.enabled = true;
                }
                else if (ol != null) ol.enabled = false;
            }
            if (label != null) label.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }
}
