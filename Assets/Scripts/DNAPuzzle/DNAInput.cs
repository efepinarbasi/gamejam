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
    }
}
