using StarterAssets;
using UnityEngine;

namespace Prince
{
    public sealed class DialogueInputLock : MonoBehaviour
    {
        private ThirdPersonController movement;
        private StarterAssetsInputs inputs;
        private PlayerCombat combat;
        private float originalMoveSpeed;
        private float originalSprintSpeed;
        private bool originalCameraLock;
        private bool originalCombatEnabled;
        private bool locked;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        private void Awake()
        {
            movement = GetComponent<ThirdPersonController>() ?? GetComponentInChildren<ThirdPersonController>();
            inputs = GetComponent<StarterAssetsInputs>() ?? GetComponentInChildren<StarterAssetsInputs>();
            combat = GetComponent<PlayerCombat>() ?? GetComponentInChildren<PlayerCombat>();
        }

        public void SetLocked(bool value)
        {
            if (locked == value)
                return;
            locked = value;

            if (value)
            {
                if (movement != null)
                {
                    originalMoveSpeed = movement.MoveSpeed;
                    originalSprintSpeed = movement.SprintSpeed;
                    originalCameraLock = movement.LockCameraPosition;
                    movement.MoveSpeed = 0f;
                    movement.SprintSpeed = 0f;
                    movement.LockCameraPosition = true;
                }

                if (combat != null)
                {
                    originalCombatEnabled = combat.enabled;
                    combat.enabled = false;
                }

                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ClearMovementInput();
            }
            else
            {
                if (movement != null)
                {
                    movement.MoveSpeed = originalMoveSpeed;
                    movement.SprintSpeed = originalSprintSpeed;
                    movement.LockCameraPosition = originalCameraLock;
                }

                if (combat != null)
                    combat.enabled = originalCombatEnabled;

                ClearMovementInput();
                Cursor.lockState = previousCursorLockMode;
                Cursor.visible = previousCursorVisible;
            }
        }

        private void Update()
        {
            if (locked)
                ClearMovementInput();
        }

        private void OnDisable()
        {
            if (locked)
                SetLocked(false);
        }

        private void ClearMovementInput()
        {
            if (inputs == null)
                return;
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.jump = false;
            inputs.sprint = false;
        }
    }
}
