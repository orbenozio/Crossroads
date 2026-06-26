using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // קלט swipe/drag אגנוסטי (ספק 13, G6/M4). מגע ועכבר דרך אותו Pointer abstraction של
    // Input System החדש (זהים אוטומטית, J5), פלוס מקלדת (חצים) כ-flow בדיד נגיש (ספק 10.2).
    //
    // מחזור-החיים (§10.1): neutral -> drag -> preview (רציף) -> commit / cancel.
    public sealed class SwipeInput : MonoBehaviour
    {
        [SerializeField] private float commitThreshold = 0.35f;  // שבר-מסך לצורך commit

        public event Action<ChoiceSide, float> OnPreview;  // כיוון + עוצמה 0..1 (ה-CardView מציג משוב)
        public event Action<ChoiceSide> OnCommit;
        public event Action OnCancel;
        public event Action OnMenu;   // Esc with no pending preview -> open the pause menu (keyboard parity to PauseButton)

        private bool _down;
        private bool _previewing;
        private Vector2 _start;
        private ChoiceSide? _kbPreview;   // הצד שב-preview דרך מקלדת (זרימה בדידה §10.2)

        private void Update()
        {
            var p = Pointer.current;
            if (p != null) UpdatePointer(p);
            UpdateKeyboard();
        }

        private void UpdatePointer(Pointer p)
        {
            bool pressed = p.press.isPressed;
            Vector2 pos = p.position.ReadValue();
            float span = Screen.width * 0.4f;

            if (pressed && !_down)
            {
                _down = true;
                _start = pos;
            }
            else if (pressed && _down)
            {
                float dx = pos.x - _start.x;
                float frac = Mathf.Clamp01(Mathf.Abs(dx) / span);
                if (frac > 0.02f)
                {
                    _previewing = true;
                    OnPreview?.Invoke(dx < 0 ? ChoiceSide.Left : ChoiceSide.Right, frac);  // מרחבי-פיזי (Q2)
                }
            }
            else if (!pressed && _down)
            {
                _down = false;
                float dx = pos.x - _start.x;
                float frac = Mathf.Abs(dx) / span;
                if (frac >= commitThreshold) Commit(dx < 0 ? ChoiceSide.Left : ChoiceSide.Right);
                else if (_previewing) OnCancel?.Invoke();
                _previewing = false;
            }
        }

        // זרימת-מקלדת בדידה נגישה (§10.2): חץ ראשון לצד = preview (השחקן רואה מה ישתנה),
        // חץ שני לאותו צד או Enter = commit, חץ נגדי = החלפת-preview, Escape = cancel.
        private void UpdateKeyboard()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rightArrowKey.wasPressedThisFrame) KeyboardSide(ChoiceSide.Right);
            else if (k.leftArrowKey.wasPressedThisFrame) KeyboardSide(ChoiceSide.Left);
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
            {
                if (_kbPreview.HasValue) { var s = _kbPreview.Value; _kbPreview = null; Commit(s); }
            }
            else if (k.escapeKey.wasPressedThisFrame)
            {
                // Esc cancels a pending preview; with nothing pending it opens the pause menu.
                if (_kbPreview.HasValue) { _kbPreview = null; OnCancel?.Invoke(); }
                else OnMenu?.Invoke();
            }
        }

        private void KeyboardSide(ChoiceSide side)
        {
            if (_kbPreview == side) { _kbPreview = null; Commit(side); }   // לחיצה שנייה על אותו חץ = commit
            else { _kbPreview = side; OnPreview?.Invoke(side, 1f); }       // ראשונה / החלפה = preview מלא
        }

        // ציבורי כדי שטסטים / auto-driver יוכלו להפעיל בדיוק את מסלול ה-commit.
        public void Commit(ChoiceSide side) { _kbPreview = null; OnCommit?.Invoke(side); }  // commit (§10.2 - discrete flow)
    }
}
