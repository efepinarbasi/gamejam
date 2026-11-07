using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Lacuna
{
    /// <summary>
    /// Tiny wrapper so the loop scripts work with both the old Input Manager
    /// and the new Input System package.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Top-row number keys 1, 2 and 3.</summary>
        public static bool NumberPressed(int number)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            switch (number)
            {
                case 1: return k.digit1Key.wasPressedThisFrame;
                case 2: return k.digit2Key.wasPressedThisFrame;
                case 3: return k.digit3Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(KeyCode.Alpha0 + number);
#endif
        }

        /// <summary>Backspace is the stub "fail the current screen" key.</summary>
        public static bool BackspacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.backspaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        /// <summary>Space skips the Landlord's typewriter text.</summary>
        public static bool SkipPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
