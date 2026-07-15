using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RebirthProtocol.Bootstrap
{
    public sealed class InputSmokeProbe : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private Text statusLabel;

        public Vector2 MoveInput { get; private set; }
        public bool DashPressed { get; private set; }
        public bool HasKeyboard => Keyboard.current != null;
        public bool HasGamepad => Gamepad.current != null;

        private void Update()
        {
            ReadInput();
            transform.position += new Vector3(MoveInput.x, 0f, MoveInput.y) * moveSpeed * Time.deltaTime;

            if (statusLabel != null)
            {
                statusLabel.text = $"Move {MoveInput.x:0.00}, {MoveInput.y:0.00} | Dash {DashPressed}";
            }
        }

        public void SetStatusLabel(Text label)
        {
            statusLabel = label;
        }

        public void ApplyMoveForTests(Vector2 move, bool dashPressed)
        {
            MoveInput = Vector2.ClampMagnitude(move, 1f);
            DashPressed = dashPressed;
        }

        private void ReadInput()
        {
            var move = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                move += gamepad.leftStick.ReadValue();
            }

            MoveInput = Vector2.ClampMagnitude(move, 1f);
            DashPressed = (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
        }
    }
}
