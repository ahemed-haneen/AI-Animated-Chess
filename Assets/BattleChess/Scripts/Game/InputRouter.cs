using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BattleChess
{
    /// <summary>Works with both the new Input System (Unity 6 default) and the legacy Input Manager.</summary>
    public static class InputRouter
    {
        public static bool PrimaryDown(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            pos = p != null ? p.position.ReadValue() : Vector2.zero;
            return p != null && p.press.wasPressedThisFrame;
#else
            pos = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool PrimaryHeld(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            pos = p != null ? p.position.ReadValue() : Vector2.zero;
            return p != null && p.press.isPressed;
#else
            pos = Input.mousePosition;
            return Input.GetMouseButton(0);
#endif
        }

        public static bool PrimaryUp(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            pos = p != null ? p.position.ReadValue() : Vector2.zero;
            return p != null && p.press.wasReleasedThisFrame;
#else
            pos = Input.mousePosition;
            return Input.GetMouseButtonUp(0);
#endif
        }

        /// <summary>True while two fingers are down; returns their positions.</summary>
        public static bool TwoFingers(out Vector2 a, out Vector2 b)
        {
            a = b = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts == null) return false;
            int n = 0;
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                if (n == 0) a = t.position.ReadValue(); else if (n == 1) b = t.position.ReadValue();
                n++;
            }
            return n >= 2;
#else
            if (Input.touchCount < 2) return false;
            a = Input.GetTouch(0).position; b = Input.GetTouch(1).position;
            return true;
#endif
        }

        public static float Scroll()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null ? m.scroll.ReadValue().y / 120f : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }
    }
}
