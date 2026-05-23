using UnityEngine;
using UnityEngine.InputSystem;

namespace StrafAdvance
{
    /// <summary>
    /// Thin facade over the new Input System for the common queries this game needs.
    /// All gameplay code goes through here — never `UnityEngine.Input` directly.
    /// </summary>
    public static class GameInput
    {
        // ─── Tap / press ────────────────────────────────────────────────────────

        /// <summary>Did the user begin a primary press (touch or left-click) this frame?</summary>
        public static bool PrimaryPressedThisFrame =>
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
            (Mouse.current      != null && Mouse.current.leftButton.wasPressedThisFrame);

        /// <summary>Did the user release the primary press this frame?</summary>
        public static bool PrimaryReleasedThisFrame =>
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) ||
            (Mouse.current      != null && Mouse.current.leftButton.wasReleasedThisFrame);

        /// <summary>Is the primary input currently held?</summary>
        public static bool PrimaryHeld =>
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) ||
            (Mouse.current      != null && Mouse.current.leftButton.isPressed);

        /// <summary>Did any key, button or touch fire this frame? Used for tap-to-start screens.</summary>
        public static bool AnyInputThisFrame =>
            PrimaryPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Gamepad.current  != null && (
                Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.startButton.wasPressedThisFrame));

        // ─── Pointer position (touch first, then mouse) ─────────────────────────

        /// <summary>Screen-space position of the active primary input, or zero if none.</summary>
        public static Vector2 PointerPosition
        {
            get
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    return Touchscreen.current.primaryTouch.position.ReadValue();
                if (Mouse.current != null) return Mouse.current.position.ReadValue();
                return Vector2.zero;
            }
        }

        /// <summary>True when a touch is currently registered (vs mouse).</summary>
        public static bool HasActiveTouch =>
            Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
    }
}
