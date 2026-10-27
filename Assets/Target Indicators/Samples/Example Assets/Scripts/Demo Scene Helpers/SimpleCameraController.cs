using UnityEngine;
using UnityEngine.InputSystem;

namespace TargetIndicators.Samples
{
    public class SimpleCameraController : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField]
        InputActionReference _moveInputAction;

        [SerializeField]
        InputActionReference _lookInputAction;

        [Header("Input Options")]
        [SerializeField]
        bool _enablePitch = true;

        [SerializeField]
        bool _enableYaw = true;

        [Header("Settings")]
        [SerializeField]
        float _moveSpeed = 5f;

        [SerializeField, Range(0, 2)]
        float _rotateSensitivity = 1f;

        [SerializeField]
        float _minPitch = -89f;

        [SerializeField]
        float _maxPitch = 89f;

        float _pitch;
        float _yaw;

        Vector2 _rawInputDirection;

        void OnEnable()
        {
            _moveInputAction.action.Enable();
            _lookInputAction.action.Enable();

            _moveInputAction.action.performed += OnMoveInputPerformed;
            _moveInputAction.action.canceled += OnMoveInputCancelled;

            _lookInputAction.action.performed += OnLookInputPerformed;
        }

        void OnDisable()
        {
            _lookInputAction.action.performed -= OnLookInputPerformed;

            _moveInputAction.action.canceled -= OnMoveInputCancelled;
            _moveInputAction.action.performed -= OnMoveInputPerformed;

            _moveInputAction.action.Disable();
            _lookInputAction.action.Disable();
        }

        void Update()
        {
            if (_rawInputDirection == Vector2.zero)
                return;

            Move();
        }

        void OnMoveInputPerformed(InputAction.CallbackContext context)
        {
            if (FocusGame.Instance == null || !FocusGame.Instance.IsFocused)
                return;

            _rawInputDirection = context.ReadValue<Vector2>();
        }

        void OnMoveInputCancelled(InputAction.CallbackContext _)
        {
            _rawInputDirection = Vector2.zero;
        }

        void OnLookInputPerformed(InputAction.CallbackContext context)
        {
            if (FocusGame.Instance == null || !FocusGame.Instance.IsFocused)
                return;

            var lookDeltaInPx = context.ReadValue<Vector2>();

            if (!_enablePitch)
                lookDeltaInPx.y = 0;

            if (!_enableYaw)
                lookDeltaInPx.x = 0;

            Look(lookDeltaInPx);
        }

        void Move()
        {
            var relativeMoveDirection = GetRelativeMoveDirectionFromInput(_rawInputDirection);
            transform.position += relativeMoveDirection * (_moveSpeed * Time.deltaTime);
        }

        void Look(Vector2 lookDeltaInPx)
        {
            var xDelta = lookDeltaInPx.x;
            var yDelta = lookDeltaInPx.y;

            _pitch += -yDelta * (_rotateSensitivity * 0.1f);
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            _yaw += xDelta * (_rotateSensitivity * 0.1f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        Vector3 GetRelativeMoveDirectionFromInput(Vector2 rawInputDirection)
        {
            if (rawInputDirection == Vector2.zero)
                return Vector3.zero;

            var inputDirection = new Vector3(rawInputDirection.x, 0, rawInputDirection.y);
            var rotation = Quaternion.LookRotation(inputDirection.normalized, Vector3.up);
            var forward = Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
            return rotation * forward * inputDirection.magnitude;
        }
    }
}
