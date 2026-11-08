using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DNAPuzzle
{
    /// <summary>
    /// Unity 6 New Input System ve Eski Input Manager ile tam uyumlu,
    /// InvalidOperationException fırlatmayan evrensel fare ve giriş yöneticisi.
    /// </summary>
    public static class DNAInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    return Mouse.current.position.ReadValue();
                }
#endif
                try
                {
                    return Input.mousePosition;
                }
                catch
                {
                    return Vector2.zero;
                }
            }
        }

        public static bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
                if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
                if (button == 2) return Mouse.current.middleButton.wasPressedThisFrame;
            }
#endif
            try
            {
                return Input.GetMouseButtonDown(button);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 0) return Mouse.current.leftButton.isPressed;
                if (button == 1) return Mouse.current.rightButton.isPressed;
                if (button == 2) return Mouse.current.middleButton.isPressed;
            }
#endif
            try
            {
                return Input.GetMouseButton(button);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetMouseButtonUp(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 0) return Mouse.current.leftButton.wasReleasedThisFrame;
                if (button == 1) return Mouse.current.rightButton.wasReleasedThisFrame;
                if (button == 2) return Mouse.current.middleButton.wasReleasedThisFrame;
            }
#endif
            try
            {
                return Input.GetMouseButtonUp(button);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                switch (key)
                {
                    case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
                    case KeyCode.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter: return Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
                    case KeyCode.Tab: return Keyboard.current.tabKey.wasPressedThisFrame;
                    case KeyCode.Alpha1:
                    case KeyCode.Keypad1: return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
                    case KeyCode.Alpha2:
                    case KeyCode.Keypad2: return Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
                    case KeyCode.Alpha3:
                    case KeyCode.Keypad3: return Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
                    case KeyCode.Alpha4:
                    case KeyCode.Keypad4: return Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame;
                }
            }
#endif
            try
            {
                return Input.GetKeyDown(key);
            }
            catch
            {
                return false;
            }
        }
    }
}
