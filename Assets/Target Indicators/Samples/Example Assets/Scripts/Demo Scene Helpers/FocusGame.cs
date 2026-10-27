using System;
#if UNITY_6000_5_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Allows the focus of the game to be toggled. This is helpful for disabling character controller input
    /// without having to stop the game for debugging. Shows a focus indicator with a square on the screen. When the
    /// game is in focus the indicator will be white, and when the game is unfocused it will be gray. The game starts
    /// unfocused.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class FocusGame : MonoBehaviour
    {
        /// <summary>
        /// The instance of the `FocusGame` singleton.
        /// </summary>
#if UNITY_6000_5_OR_NEWER
        [AutoStaticsCleanup]
#endif
        public static FocusGame Instance { get; private set; }

        [SerializeField]
        InputActionReference _inputActionReference;

        [SerializeField]
        Image _focusVisualizer;

        /// <summary>
        /// The current focus state of the game.
        /// </summary>
        public bool IsFocused { get; private set; }

        /// <summary>
        /// The event raised when the focus state changes passing the new state as the parameter.
        /// </summary>
        public event Action<bool> FocusChanged;

        /// <summary>
        /// Sets the focus of the game. If focus is enabled the `Cursor.lockState` is set to `CursorLockMode.Locked` to
        /// hide the cursor otherwise it is set to `CursorLockMode.Unbounded` to show the cursor.
        /// </summary>
        /// <param name="isFocused">The focus state to set.</param>
        public void SetFocus(bool isFocused)
        {
            if (isFocused == IsFocused)
                return;

            IsFocused = isFocused;
            Cursor.lockState = IsFocused ? CursorLockMode.Locked : CursorLockMode.None;
            _focusVisualizer.color = IsFocused ? Color.white : Color.gray;
            FocusChanged?.Invoke(isFocused);
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _focusVisualizer.color = IsFocused ? Color.white : Color.gray;

            _inputActionReference.action.performed += OnChangeFocusActionPerformed;
        }

        void OnEnable()
        {
            _inputActionReference.action.Enable();
        }

        void OnDisable()
        {
            _inputActionReference.action.Disable();
        }

        void OnDestroy()
        {
            _inputActionReference.action.performed -= OnChangeFocusActionPerformed;
        }

        void OnChangeFocusActionPerformed(InputAction.CallbackContext context)
        {
            SetFocus(!IsFocused);
        }

#if !UNITY_6000_5_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticValues()
        {
            Instance = null;
        }
#endif
    }
}
