using System;
using UnityEngine.InputSystem;

namespace MicroSense
{
    [Serializable]
    public struct PlayerInputBindings
    {
        public Key Left;
        public Key Right;
        public Key Up;
        public Key Down;
        public Key AbilityPrimary;
        public Key AbilitySecondary;

        public PlayerInputBindings(
            Key left,
            Key right,
            Key up,
            Key down,
            Key abilityPrimary,
            Key abilitySecondary)
        {
            Left = left;
            Right = right;
            Up = up;
            Down = down;
            AbilityPrimary = abilityPrimary;
            AbilitySecondary = abilitySecondary;
        }

        public float ReadHorizontal()
        {
            return ReadAxis(Left, Right);
        }

        public float ReadVertical()
        {
            return ReadAxis(Down, Up);
        }

        public bool UpPressedThisFrame => WasPressedThisFrame(Up);

        public bool PrimaryPressedThisFrame => WasPressedThisFrame(AbilityPrimary);

        public bool SecondaryPressedThisFrame => WasPressedThisFrame(AbilitySecondary);

        private static float ReadAxis(Key negative, Key positive)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return 0f;
            }

            float value = 0f;
            if (keyboard[negative].isPressed)
            {
                value -= 1f;
            }

            if (keyboard[positive].isPressed)
            {
                value += 1f;
            }

            return value;
        }

        private static bool WasPressedThisFrame(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }
    }
}
