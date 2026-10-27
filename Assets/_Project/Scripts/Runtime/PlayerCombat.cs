using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prince
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public WeaponHolster weaponHolster;
        public WeaponHitbox weaponHitbox;
        public WeaponAudio weaponAudio;
        public ThirdPersonController movementController;
        public CharacterController characterController;
        public AnimationClip[] attackClips = new AnimationClip[3];
        public AnimationClip drawClip;
        public AnimationClip sheathClip;
        public string combatLayerName = "Combat";

        [Header("Timing")]
        [Min(0f)] public float attackCooldown = 0.12f;
        [Min(0f)] public float comboInputBuffer = 0.85f;
        [Range(0.1f, 0.95f)] public float comboTransitionNormalizedTime = 0.58f;
        [Min(0f)] public float autoSheathDelay = 5f;
        [Min(0.05f)] public float drawFadeDuration = 0.22f;
        [Min(0.05f)] public float sheathFadeDuration = 0.22f;
        [Min(0.1f)] public float drawAnimationSpeed = 1.35f;
        [Min(0.1f)] public float sheathAnimationSpeed = 1.35f;
        [Min(0.05f)] public float attackTransitionDuration = 0.07f;
        [Range(0.05f, 0.95f)] public float fallbackHitStart = 0.18f;
        [Range(0.1f, 1f)] public float fallbackHitEnd = 0.82f;

        private InputAction _attackAction;
        private InputAction _toggleWeaponAction;
        private AttackBuffer _attackBuffer;
        private CombatFeel _combatFeel;
        private int _combatLayerIndex = -1;
        private bool _isAttacking;
        private bool _drawPending;
        private bool _sheathPending;
        private bool _attackAfterDraw;
        private bool _bufferedAttack;
        private bool _fallbackWindowStarted;
        private bool _warnedMissingAnimator;
        private bool _warnedMissingHolster;
        private bool _warnedMissingGroundCheck;
        private float _bufferExpiresAt;
        private float _swapEndsAt;
        private float _nextAttackTime;
        private float _lastCombatInputTime;
        private int _currentCombo;

        public bool IsAttacking => _isAttacking;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (weaponHolster == null)
                weaponHolster = GetComponent<WeaponHolster>();
            if (weaponHitbox == null)
                weaponHitbox = GetComponentInChildren<WeaponHitbox>(true);
            if (weaponAudio == null)
                weaponAudio = GetComponent<WeaponAudio>();
            if (movementController == null)
                movementController = GetComponent<ThirdPersonController>();
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            _attackBuffer = GetComponent<AttackBuffer>();
            _combatFeel = GetComponent<CombatFeel>();

            _attackAction = new InputAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Mouse>/leftButton");
            _attackAction.AddBinding("<Gamepad>/buttonWest");
            _attackAction.performed += OnAttackInput;

            _toggleWeaponAction = new InputAction("ToggleWeapon", InputActionType.Button);
            _toggleWeaponAction.AddBinding("<Keyboard>/r");
            _toggleWeaponAction.AddBinding("<Gamepad>/buttonNorth");
            _toggleWeaponAction.performed += OnToggleWeaponInput;
        }

        private void OnEnable()
        {
            if (_attackAction != null)
                _attackAction.Enable();
            if (_toggleWeaponAction != null)
                _toggleWeaponAction.Enable();
        }

        private void OnDisable()
        {
            if (_attackAction != null)
                _attackAction.Disable();
            if (_toggleWeaponAction != null)
                _toggleWeaponAction.Disable();

            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
            if (_combatFeel != null)
                _combatFeel.EndAttack();
        }

        private void OnDestroy()
        {
            if (_attackAction != null)
            {
                _attackAction.performed -= OnAttackInput;
                _attackAction.Dispose();
            }
            if (_toggleWeaponAction != null)
            {
                _toggleWeaponAction.performed -= OnToggleWeaponInput;
                _toggleWeaponAction.Dispose();
            }
        }

        private void Start()
        {
            if (animator != null)
            {
                _combatLayerIndex = animator.GetLayerIndex(combatLayerName);
                animator.applyRootMotion = false;
            }

            if (weaponHolster != null)
                weaponHolster.Initialize();
        }

        private void Update()
        {
            if (_drawPending && Time.time >= _swapEndsAt)
            {
                _drawPending = false;
                if (weaponHolster != null)
                    weaponHolster.DrawSword();
                CrossFadeToEmpty();
                if (_attackAfterDraw)
                {
                    _attackAfterDraw = false;
                    StartAttack(1);
                }
            }

            if (_sheathPending && Time.time >= _swapEndsAt)
            {
                _sheathPending = false;
                if (weaponHolster != null)
                    weaponHolster.SheathSword();
                SetAnimatorBool("IsArmed", false);
                CrossFadeToEmpty();
            }

            UpdateAttackState();
            ProcessBufferedAttack();

            if (!_isAttacking && !_drawPending && !_sheathPending &&
                weaponHolster != null && weaponHolster.IsDrawn &&
                autoSheathDelay > 0f && Time.time - _lastCombatInputTime >= autoSheathDelay)
            {
                BeginSheath();
            }
        }

        private void OnAttackInput(InputAction.CallbackContext context)
        {
            if (!CanAttack())
                return;

            _lastCombatInputTime = Time.time;
            if (_isAttacking)
            {
                BufferAttack();
                return;
            }
            if (_drawPending || _sheathPending)
            {
                if (_drawPending)
                    _attackAfterDraw = true;
                else
                {
                    _sheathPending = false;
                    if (!weaponHolster.IsDrawn)
                        weaponHolster.DrawSword();
                    ResetAnimatorTrigger("Sheath");
                    SetAnimatorBool("IsArmed", true);
                    CrossFadeToEmpty();
                    StartAttack(1);
                }
                return;
            }

            if (Time.time < _nextAttackTime)
            {
                BufferAttack();
                return;
            }
            if (!weaponHolster.IsDrawn)
            {
                BeginDraw(true);
                return;
            }

            StartAttack(1);
        }

        private void OnToggleWeaponInput(InputAction.CallbackContext context)
        {
            if (weaponHolster == null || _isAttacking)
                return;

            _lastCombatInputTime = Time.time;
            if (_drawPending)
            {
                _drawPending = false;
                _attackAfterDraw = false;
                ResetAnimatorTrigger("Draw");
                if (weaponHolster.IsDrawn)
                {
                    BeginSheath();
                }
                else
                {
                    SetAnimatorBool("IsArmed", false);
                    CrossFadeToEmpty();
                }
                return;
            }
            if (_sheathPending)
            {
                _sheathPending = false;
                if (weaponHolster.DrawSword())
                    SetAnimatorBool("IsArmed", true);
                ResetAnimatorTrigger("Sheath");
                CrossFadeToEmpty();
                return;
            }

            if (weaponHolster.IsDrawn)
                BeginSheath();
            else
                BeginDraw(false);
        }

        private bool CanAttack()
        {
            if (animator == null)
            {
                if (!_warnedMissingAnimator)
                {
                    Debug.LogWarning($"{nameof(PlayerCombat)} on '{name}' has no Animator.", this);
                    _warnedMissingAnimator = true;
                }
                return false;
            }
            if (weaponHolster == null)
            {
                if (!_warnedMissingHolster)
                {
                    Debug.LogWarning($"{nameof(PlayerCombat)} on '{name}' has no {nameof(WeaponHolster)}.", this);
                    _warnedMissingHolster = true;
                }
                return false;
            }
            if (_combatLayerIndex < 0)
                _combatLayerIndex = animator.GetLayerIndex(combatLayerName);
            if (_combatLayerIndex < 0)
            {
                Debug.LogWarning($"Animator on '{animator.name}' has no '{combatLayerName}' layer.", animator);
                return false;
            }

            if (characterController == null && movementController == null)
            {
                if (!_warnedMissingGroundCheck)
                {
                    Debug.LogWarning(
                        $"{nameof(PlayerCombat)} on '{name}' has no CharacterController or grounded-state provider.",
                        this);
                    _warnedMissingGroundCheck = true;
                }
                return false;
            }

            bool grounded = characterController != null
                ? characterController.isGrounded
                : movementController.Grounded;
            return grounded;
        }

        private void BeginDraw(bool attackAfterDraw)
        {
            if (weaponHolster == null)
                return;

            _attackAfterDraw = attackAfterDraw;
            _drawPending = true;
            _sheathPending = false;
            bool hasDrawAnimation = drawClip != null;
            if (!hasDrawAnimation && !weaponHolster.DrawSword())
                return;

            _swapEndsAt = Time.time + (hasDrawAnimation
                ? Mathf.Max(0.05f, drawClip.length / Mathf.Max(0.1f, drawAnimationSpeed))
                : drawFadeDuration);
            ResetAnimatorTrigger("Sheath");
            SetAnimatorBool("IsArmed", true);
            SetAnimatorTrigger("Draw");
        }

        private void BeginSheath()
        {
            if (weaponHolster == null || !weaponHolster.IsDrawn)
                return;

            _sheathPending = true;
            _drawPending = false;
            _swapEndsAt = Time.time +
                          (sheathClip != null
                              ? Mathf.Max(0.05f, sheathClip.length / Mathf.Max(0.1f, sheathAnimationSpeed))
                              : sheathFadeDuration);
            ResetAnimatorTrigger("Draw");
            SetAnimatorBool("IsArmed", false);
            SetAnimatorTrigger("Sheath");
        }

        private void StartAttack(int combo)
        {
            if (animator == null || _combatLayerIndex < 0 || !CanAttack())
                return;

            _currentCombo = Mathf.Clamp(combo, 1, 3);
            _isAttacking = true;
            _fallbackWindowStarted = false;
            _nextAttackTime = Time.time + attackCooldown;
            if (_combatFeel != null)
                _combatFeel.BeginAttack(_currentCombo);
            SetAnimatorBool("IsArmed", true);
            SetAnimatorInt("ComboIndex", _currentCombo);
            CrossFadeToAttack(_currentCombo);
        }

        private void BufferAttack()
        {
            _bufferedAttack = true;
            if (_attackBuffer != null && _combatFeel != null &&
                _combatFeel.Settings != null && _combatFeel.Settings.attackBufferEnabled)
                _attackBuffer.Queue(_combatFeel.GetBufferDuration(comboInputBuffer));
            else
                _bufferExpiresAt = Time.time + comboInputBuffer;
        }

        private void UpdateAttackState()
        {
            if (!_isAttacking || animator == null || _combatLayerIndex < 0)
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(_combatLayerIndex);
            int stateCombo = GetAttackStateCombo(state);
            if (stateCombo == 0)
            {
                if (!animator.IsInTransition(_combatLayerIndex))
                    FinishAttack();
                return;
            }

            if (!animator.IsInTransition(_combatLayerIndex))
            {
                UpdateFallbackHitWindow(state.normalizedTime);

                if (stateCombo < 3 && HasBufferedAttack() &&
                    Time.time >= _nextAttackTime &&
                    state.normalizedTime >= GetComboTransitionTime())
                {
                    ClearBufferedAttack();
                    _currentCombo = stateCombo + 1;
                    _nextAttackTime = Time.time + attackCooldown;
                    _fallbackWindowStarted = false;
                    if (_combatFeel != null)
                        _combatFeel.BeginAttack(_currentCombo);
                    if (weaponHitbox != null)
                        weaponHitbox.SetHitboxActive(false);
                    SetAnimatorInt("ComboIndex", _currentCombo);
                    CrossFadeToAttack(_currentCombo);
                }
            }
        }

        private void ProcessBufferedAttack()
        {
            if (!_bufferedAttack)
                return;
            if (!HasBufferedAttack())
            {
                ClearBufferedAttack();
                return;
            }
            if (weaponHolster == null || _isAttacking || _drawPending || _sheathPending ||
                !weaponHolster.IsDrawn ||
                Time.time < _nextAttackTime || !CanAttack())
                return;

            ClearBufferedAttack();
            StartAttack(1);
        }

        public bool CancelAttackForJump()
        {
            if (!_isAttacking)
                return false;

            _isAttacking = false;
            _currentCombo = 0;
            _fallbackWindowStarted = false;
            ClearBufferedAttack();
            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
            if (_combatFeel != null)
                _combatFeel.EndAttack();
            CrossFadeToEmpty();
            return true;
        }

        private bool HasBufferedAttack()
        {
            if (!_bufferedAttack)
                return false;
            if (_attackBuffer != null && _combatFeel != null &&
                _combatFeel.Settings != null && _combatFeel.Settings.attackBufferEnabled)
                return _attackBuffer.HasBufferedAttack;
            return Time.time <= _bufferExpiresAt;
        }

        private void ClearBufferedAttack()
        {
            _bufferedAttack = false;
            if (_attackBuffer != null)
                _attackBuffer.Clear();
        }

        private float GetComboTransitionTime()
        {
            return _combatFeel != null
                ? _combatFeel.GetCancelWindowStart(comboTransitionNormalizedTime)
                : comboTransitionNormalizedTime;
        }

        private int GetAttackStateCombo(AnimatorStateInfo state)
        {
            if (state.IsName(combatLayerName + ".Attack1"))
                return 1;
            if (state.IsName(combatLayerName + ".Attack2"))
                return 2;
            if (state.IsName(combatLayerName + ".Attack3"))
                return 3;
            return 0;
        }

        private void UpdateFallbackHitWindow(float normalizedTime)
        {
            if (weaponHitbox == null)
                return;

            float normalized = Mathf.Clamp01(normalizedTime);
            float hitStart = _combatFeel != null
                ? _combatFeel.GetFallbackHitStart(fallbackHitStart)
                : fallbackHitStart;
            float hitEnd = _combatFeel != null
                ? _combatFeel.GetFallbackHitEnd(fallbackHitEnd)
                : fallbackHitEnd;
            if (!_fallbackWindowStarted && normalized >= hitStart)
            {
                _fallbackWindowStarted = true;
                weaponHitbox.OnAttackHitStart();
            }
            else if (_fallbackWindowStarted && normalized >= hitEnd)
            {
                _fallbackWindowStarted = false;
                weaponHitbox.SetHitboxActive(false);
            }
        }

        private void FinishAttack()
        {
            _isAttacking = false;
            _currentCombo = 0;
            _fallbackWindowStarted = false;
            if (_combatFeel != null)
                _combatFeel.EndAttack();
            if (weaponHitbox != null)
                weaponHitbox.SetHitboxActive(false);
        }

        private void SetAnimatorBool(string parameter, bool value)
        {
            if (animator != null && HasParameter(parameter, AnimatorControllerParameterType.Bool))
                animator.SetBool(parameter, value);
        }

        private void SetAnimatorTrigger(string parameter)
        {
            if (animator != null && HasParameter(parameter, AnimatorControllerParameterType.Trigger))
                animator.SetTrigger(parameter);
        }

        private void SetAnimatorInt(string parameter, int value)
        {
            if (animator != null && HasParameter(parameter, AnimatorControllerParameterType.Int))
                animator.SetInteger(parameter, value);
        }

        private void ResetAnimatorTrigger(string parameter)
        {
            if (animator != null && HasParameter(parameter, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(parameter);
        }

        private bool HasParameter(string parameter, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameter && parameters[i].type == type)
                    return true;
            }

            Debug.LogWarning($"Animator on '{animator.name}' is missing {type} parameter '{parameter}'.", animator);
            return false;
        }

        private void CrossFadeToEmpty()
        {
            if (animator == null || _combatLayerIndex < 0)
                return;
            animator.CrossFadeInFixedTime(combatLayerName + ".Empty", 0.08f, _combatLayerIndex);
        }

        private void CrossFadeToAttack(int combo)
        {
            if (animator == null || _combatLayerIndex < 0)
                return;
            animator.CrossFadeInFixedTime(
                $"{combatLayerName}.Attack{combo}",
                Mathf.Max(0.01f, attackTransitionDuration),
                _combatLayerIndex);
        }
    }
}
