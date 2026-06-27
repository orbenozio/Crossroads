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
        private static readonly Color DimChoice = new Color(1f, 1f, 1f, 0.62f);   // resting choice-hint color
        private RectTransform _rt;
        private Coroutine _enter;
        private TMP_Text _previewDelta;   // on-card delta summary during swipe (§10.3)
        private Color _choiceColor = Color.white;   // theme text color the active hint brightens toward
        private RectTransform Rt => _rt != null ? _rt : (_rt = (RectTransform)transform);

        public void Bind(EventNodeView view, Theme theme)
        {
            if (bodyText != null) bodyText.text = view.Body;
            if (leftLabel != null) leftLabel.text = view.LeftLabel;
            if (rightLabel != null) rightLabel.text = view.RightLabel;

            // Speaker badge: shown for a named speaker, hidden for the narrator / empty (cleaner card).
            bool named = !string.IsNullOrEmpty(view.Speaker) && view.Speaker != "narrator";
            var speaker = theme != null ? theme.GetSpeaker(view.Speaker) : null;

            if (speakerLabel != null)
            {
                speakerLabel.gameObject.SetActive(named);
                if (named) speakerLabel.text = Capitalize(view.Speaker);
            }

            // Speaker portrait (Theme.SpeakerStyle.icon): shown when this speaker has one. Drawn at its
            // true colors (no tint) - the tint only colors the name label.
            if (speakerIcon != null)
            {
                bool hasIcon = named && speaker != null && speaker.icon != null;
                speakerIcon.gameObject.SetActive(hasIcon);
                if (hasIcon) { speakerIcon.sprite = speaker.icon; speakerIcon.color = Color.white; }
            }

            if (theme != null)
            {
                _choiceColor = theme.text;
                if (cardBackground != null)
                {
                    // Card art when the theme has it (drawn at true colors), else the flat card color.
                    cardBackground.sprite = theme.cardArt;
                    cardBackground.color = theme.cardArt != null ? Color.white : theme.card;
                }
                if (bodyText != null) bodyText.color = theme.text;
                if (named && speakerLabel != null)
                    speakerLabel.color = speaker != null ? speaker.tint : theme.accent;
            }

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
            float dir = side == ChoiceSide.Left ? -1f : 1f;
            float f = Mathf.Clamp01(fraction);
            // Kept modest so a large card tilts in place without sliding off-screen.
            Rt.anchoredPosition = new Vector2(dir * f * 120f, 0f);
            Rt.localRotation = Quaternion.Euler(0f, 0f, -dir * f * 5f);

            TMP_Text active = side == ChoiceSide.Left ? leftLabel : rightLabel;
            TMP_Text other = side == ChoiceSide.Left ? rightLabel : leftLabel;
            if (active != null) active.color = Color.Lerp(DimChoice, _choiceColor, f);
            if (other != null) other.color = DimChoice;
        }

        // תצוגת-delta על הקלף בזמן swipe (§10.3): מציג בפינה - שמאל/ימין לפי הצד שאליו גוררים -
        // אילו מדים הבחירה תשנה (איכותני: שם + סימן/ערך). נעלם ב-ResetDrag (cancel/commit/קלף חדש).
        public void ShowPreviewDeltas(string text, ChoiceSide side)
        {
            EnsurePreviewDelta();
            bool right = side == ChoiceSide.Right;
            // Sits in the lower third, above the persistent choice hints (which live at the very bottom).
            var rt = (RectTransform)_previewDelta.transform;
            rt.anchorMin = right ? new Vector2(0.52f, 0.16f) : new Vector2(0.06f, 0.16f);
            rt.anchorMax = right ? new Vector2(0.94f, 0.32f) : new Vector2(0.48f, 0.32f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _previewDelta.alignment = right ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            _previewDelta.text = text;
            _previewDelta.gameObject.SetActive(true);
        }

        public void ResetDrag()
        {
            Rt.anchoredPosition = Vector2.zero;
            Rt.localRotation = Quaternion.identity;
            Rt.localScale = Vector3.one;
            if (leftLabel != null) leftLabel.color = DimChoice;
            if (rightLabel != null) rightLabel.color = DimChoice;
            if (_previewDelta != null) _previewDelta.gameObject.SetActive(false);
        }

        // Title-cases a speaker id for the badge (e.g. "treasurer" -> "Treasurer").
        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private void EnsurePreviewDelta()
        {
            if (_previewDelta != null) return;
            var found = transform.Find("PreviewDelta");
            if (found != null) { _previewDelta = found.GetComponent<TMP_Text>(); return; }

            var go = new GameObject("PreviewDelta", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            _previewDelta = go.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(_previewDelta);
            _previewDelta.fontSize = 26;
            _previewDelta.color = new Color(1f, 1f, 1f, 0.9f);
            _previewDelta.raycastTarget = false;
            _previewDelta.enableWordWrapping = false;
            go.SetActive(false);
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
}
