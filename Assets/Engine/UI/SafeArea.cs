using UnityEngine;

namespace Crossroads.UI
{
    // Device safe-area helper. On notched phones (e.g. Galaxy S23+, 19.5:9) the status bar / camera
    // punch-hole eats the top strip of the screen. Full-bleed art (background, card) stays edge-to-edge,
    // but HUD content (meters, pause button) must drop below the notch. Returns the TOP inset converted
    // to canvas units, so a top-anchored RectTransform can be pushed down by it. Returns 0 in the editor
    // and on notchless screens, so it is a no-op there.
    public static class SafeArea
    {
        public static float TopInset(Component context)
        {
            if (Screen.height <= 0) return 0f;
            var sa = Screen.safeArea;
            float topPx = Screen.height - (sa.y + sa.height);   // pixels cut off at the top
            if (topPx <= 0f) return 0f;
            float scale = 1f;
            var canvas = context != null ? context.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.scaleFactor > 0f) scale = canvas.scaleFactor;
            return topPx / scale;
        }
    }
}
