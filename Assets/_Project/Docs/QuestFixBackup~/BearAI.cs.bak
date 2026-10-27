using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class BearAI : MonoBehaviour, IDamageable
    {
        private enum BearState
        {
            Patrol,
            Alert,
            Chase,
            Attack,
            Return,
            Dead
        }

        [Header("Health and combat")]
        [SerializeField, Min(1f)] private float maxHealth = 120f;
        [SerializeField, Min(0f)] private float attackDamage = 18f;
        [SerializeField, Min(0f)] private float attackCooldown = 2.4f;
        [SerializeField, Min(0f)] private float attackRange = 2.1f;
        [SerializeField, Min(0f)] private float attackTelegraph = 0.8f;
        [SerializeField, Min(0f)] private float attackRecovery = 0.7f;
        [SerializeField, Min(0f)] private float hitPoise = 0.45f;
        [SerializeField, Min(0f)] private float knockbackForce = 2.2f;
        [SerializeField, Min(0f)] private float attackHitRadius = 0.8f;

        [Header("Awareness and movement")]
        [SerializeField, Min(0f)] private float detectionDistance = 15f;
        [SerializeField, Range(0f, 180f)] private float viewAngle = 110f;
        [SerializeField, Min(0f)] private float closeDetectionDistance = 3.5f;
        [SerializeField, Min(0f)] private float alertDelay = 1.2f;
        [SerializeField, Min(0f)] private float walkSpeed = 1.35f;
        [SerializeField, Min(0f)] private float chaseSpeed = 2.7f;
        [SerializeField, Min(0f)] private float patrolRadius = 5f;
        [SerializeField, Min(0f)] private float leashDistance = 24f;
        [SerializeField, Min(0f)] private float healthRegeneration = 4f;
        [SerializeField, Min(0f)] private float knockbackRecovery = 7f;
        [SerializeField, Min(0f)] private float turnSpeed = 360f;
        [SerializeField] private Terrain groundTerrain;

        [Header("Optional 3D audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip roarClip;
        [SerializeField] private AudioClip attackClip;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip deathClip;

        private readonly Collider[] _attackHits = new Collider[16];
        private readonly RaycastHit[] _obstacleHits = new RaycastHit[12];
        private readonly RaycastHit[] _sightHits = new RaycastHit[24];
        private readonly Dictionary<int, AnimatorControllerParameterType> _animatorParameters =
            new Dictionary<int, AnimatorControllerParameterType>();
        private Animator _animator;
        private NavMeshAgent _agent;
        private BearHealthBar _healthBar;
        private PlayerHealth _player;
        private Collider[] _colliders;
        private BearState _state;
        private Vector3 _homePosition;
        private Vector3 _patrolDestination;
        private Vector3 _knockbackVelocity;
        private float _health;
        private float _stateTimer;
        private float _nextAttackTime;
        private float _nextHitTime;
        private float _nextPlayerSearchTime;
        private float _nextPatrolTime;
        private float _showHealthUntil;
        private bool _attackResolved;
        private int _attackVariant;
        private bool _hasPatrolDestination;
        private bool _warnedMissingPlayer;
        private bool _warnedMissingTerrain;
        private bool _warnedMissingNavMesh;
        private bool _agentDriving;
        private bool _isDead;

        public float CurrentHealth => _health;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public bool IsEngaged => _state == BearState.Alert || _state == BearState.Chase ||
                                 _state == BearState.Attack;
        public Vector3 HomePosition => _homePosition;
        public Terrain GroundTerrain
        {
            get => groundTerrain;
            set => groundTerrain = value;
        }

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);
            _agent = GetComponent<NavMeshAgent>();
            _healthBar = GetComponentInChildren<BearHealthBar>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            _homePosition = transform.position;
            _patrolDestination = _homePosition;
            _health = Mathf.Max(1f, maxHealth);
            _state = BearState.Patrol;

            if (_animator != null)
                foreach (AnimatorControllerParameter parameter in _animator.parameters)
                    _animatorParameters[parameter.nameHash] = parameter.type;

            if (_agent != null)
            {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
                _agent.autoRepath = true;
            }
            if (_animator == null)
                Debug.LogWarning($"BearAI on '{name}' has no Animator; movement and combat still work without animations.", this);
            if (_healthBar != null)
                _healthBar.Initialize(this);
        }

        private void Update()
        {
            if (_isDead)
                return;

            FindPlayerIfNeeded();
            switch (_state)
            {
                case BearState.Patrol:
                    UpdatePatrol();
                    break;
                case BearState.Alert:
                    UpdateAlert();
                    break;
                case BearState.Chase:
                    UpdateChase();
                    break;
                case BearState.Attack:
                    UpdateAttack();
                    break;
                case BearState.Return:
                    UpdateReturn();
                    break;
            }

            ApplyKnockback();
            UpdateHealthBar();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            attackRange = Mathf.Max(0f, attackRange);
            detectionDistance = Mathf.Max(0f, detectionDistance);
            closeDetectionDistance = Mathf.Max(0f, closeDetectionDistance);
            walkSpeed = Mathf.Max(0f, walkSpeed);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            leashDistance = Mathf.Max(0f, leashDistance);
        }

        public void SetHomePosition(Vector3 position)
        {
            _homePosition = position;
            _patrolDestination = position;
            _hasPatrolDestination = false;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, GameObject source)
        {
            if (_isDead || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
                return;

            _health = Mathf.Max(0f, _health - amount);
            _showHealthUntil = Time.time + 8f;
            _healthBar?.Show();

            if (_health <= 0f)
            {
                Die();
                return;
            }

            if (Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitPoise;
                PlayClip(hurtClip);
                if (_animator != null && HasParameter("Hit", AnimatorControllerParameterType.Trigger))
                    _animator.SetTrigger("Hit");

                Vector3 away = source != null ? transform.position - source.transform.position : transform.forward;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    _knockbackVelocity += away.normalized * knockbackForce;
            }
        }

        private void FindPlayerIfNeeded()
        {
            if (_player != null && !_player.IsDefeated)
                return;
            if (Time.time < _nextPlayerSearchTime)
                return;

            _nextPlayerSearchTime = Time.time + 1f;
            _player = Object.FindAnyObjectByType<PlayerHealth>();
            if (_player == null && !_warnedMissingPlayer)
            {
                Debug.LogWarning($"BearAI on '{name}' cannot find a PlayerHealth target; it will patrol until one appears.", this);
                _warnedMissingPlayer = true;
            }
            else if (_player != null)
            {
                _warnedMissingPlayer = false;
            }
        }

        private void UpdatePatrol()
        {
            if (CanSeePlayer())
            {
                BeginAlert();
                return;
            }

            if (!_hasPatrolDestination || Time.time >= _nextPatrolTime ||
                FlatDistance(transform.position, _patrolDestination) < 0.65f)
            {
                ChoosePatrolDestination();
                return;
            }

            MoveTowards(_patrolDestination, walkSpeed);
        }

        private void ChoosePatrolDestination()
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = _homePosition + new Vector3(offset.x, 0f, offset.y);
            _patrolDestination = SampleGround(candidate);
            _hasPatrolDestination = true;
            _nextPatrolTime = Time.time + Random.Range(2.5f, 5f);
        }

        private void BeginAlert()
        {
            _state = BearState.Alert;
            _stateTimer = alertDelay;
            _hasPatrolDestination = false;
            StopAgent();
            SetTrigger("Roar");
            PlayClip(roarClip);
        }

        private void UpdateAlert()
        {
            if (_player == null || _player.IsDefeated)
            {
                ReturnHome();
                return;
            }
            FaceTowards(_player.transform.position);
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                _state = BearState.Chase;
        }

        private void UpdateChase()
        {
            if (_player == null || _player.IsDefeated)
            {
                ReturnHome();
                return;
            }
            if (FlatDistance(_homePosition, _player.transform.position) > leashDistance)
            {
                ReturnHome();
                return;
            }

            float distance = FlatDistance(transform.position, _player.transform.position);
            if (distance <= attackRange && Time.time >= _nextAttackTime)
            {
                BeginAttack();
                return;
            }

            MoveTowards(_player.transform.position, chaseSpeed);
        }

        private void BeginAttack()
        {
            _state = BearState.Attack;
            _stateTimer = attackTelegraph;
            _attackResolved = false;
            StopAgent();
            SetInteger("AttackVariant", _attackVariant++ % 4);
            if (_player != null)
                FaceTowards(_player.transform.position);
            SetTrigger("Attack");
            PlayClip(attackClip);
            _nextAttackTime = Time.time + attackCooldown;
        }

        private void UpdateAttack()
        {
            if (_player == null || _player.IsDefeated)
            {
                ReturnHome();
                return;
            }

            FaceTowards(_player.transform.position);
            _stateTimer -= Time.deltaTime;
            if (!_attackResolved && _stateTimer <= 0f)
            {
                _attackResolved = true;
                ResolveAttackHit();
                _stateTimer = attackRecovery;
            }
            else if (_attackResolved && _stateTimer <= 0f)
            {
                _state = BearState.Chase;
            }
        }

        private void ResolveAttackHit()
        {
            Vector3 hitCenter = transform.position + transform.forward * 1.1f + Vector3.up * 0.9f;
            int hitCount = Physics.OverlapSphereNonAlloc(
                hitCenter, attackHitRadius, _attackHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _attackHits[i];
                if (hit == null)
                    continue;
                PlayerHealth target = hit.GetComponentInParent<PlayerHealth>();
                if (target == null || target != _player || target.IsDefeated)
                    continue;

                target.TakeDamage(attackDamage, hit.ClosestPoint(hitCenter), gameObject);
                return;
            }
        }

        private void ReturnHome()
        {
            _state = BearState.Return;
            _hasPatrolDestination = false;
            SetBool("Dead", false);
            StopAgent();
        }

        private void UpdateReturn()
        {
            if (CanSeePlayer())
            {
                BeginAlert();
                return;
            }

            _health = Mathf.MoveTowards(_health, maxHealth, healthRegeneration * Time.deltaTime);
            if (FlatDistance(transform.position, _homePosition) > 0.8f)
            {
                MoveTowards(_homePosition, walkSpeed);
                return;
            }

            _state = BearState.Patrol;
            _health = Mathf.MoveTowards(_health, maxHealth, healthRegeneration * Time.deltaTime);
            SetSpeed(0f);
            if (_health >= maxHealth && Time.time >= _showHealthUntil)
                _healthBar?.Hide();
        }

        private void MoveTowards(Vector3 destination, float speed)
        {
            destination.y = transform.position.y;
            Vector3 desiredDirection = destination - transform.position;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude < 0.0025f)
            {
                SetSpeed(0f);
                StopAgent();
                return;
            }

            FaceDirection(desiredDirection);
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                if (!_agentDriving)
                {
                    _agent.nextPosition = transform.position;
                    _agent.updatePosition = true;
                    _agentDriving = true;
                }
                _agent.speed = speed;
                _agent.isStopped = false;
                if (!_agent.hasPath || (_agent.destination - destination).sqrMagnitude > 0.25f)
                    _agent.SetDestination(destination);
                SetSpeed(_agent.velocity.magnitude);
                return;
            }

            if (_agent != null && _agent.enabled && !_warnedMissingNavMesh)
            {
                Debug.LogWarning(
                    $"BearAI on '{name}' has no baked NavMesh at its position; using manual terrain movement. " +
                    "Run Tools > Prince > Bake Forest NavMesh to enable pathfinding.",
                    this);
                _warnedMissingNavMesh = true;
            }

            Vector3 direction = AvoidObstacle(desiredDirection.normalized);
            Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;
            transform.position = SampleGround(nextPosition);
            SetSpeed(speed);
        }

        private Vector3 AvoidObstacle(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            int count = Physics.RaycastNonAlloc(origin, direction, _obstacleHits, 1.6f);
            float nearestDistance = float.MaxValue;
            RaycastHit nearestHit = default;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = _obstacleHits[i].collider;
                if (hitCollider == null || hitCollider.transform == transform ||
                    hitCollider.transform.IsChildOf(transform) ||
                    (_player != null && hitCollider.transform.IsChildOf(_player.transform)))
                    continue;
                if (_obstacleHits[i].distance < nearestDistance)
                {
                    nearestDistance = _obstacleHits[i].distance;
                    nearestHit = _obstacleHits[i];
                }
            }
            if (nearestDistance >= 0.9f)
                return direction;

            Vector3 slide = Vector3.ProjectOnPlane(direction, nearestHit.normal);
            return slide.sqrMagnitude > 0.001f ? slide.normalized : Vector3.Cross(nearestHit.normal, Vector3.up).normalized;
        }

        private Vector3 SampleGround(Vector3 position)
        {
            if (groundTerrain == null || groundTerrain.terrainData == null)
            {
                if (!_warnedMissingTerrain)
                {
                    Debug.LogWarning($"BearAI on '{name}' has no Terrain reference; manual movement will keep its current height.", this);
                    _warnedMissingTerrain = true;
                }
                return position;
            }

            position.y = groundTerrain.transform.position.y + groundTerrain.SampleHeight(position);
            return position;
        }

        private void StopAgent()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.updatePosition = false;
            }
            _agentDriving = false;
            SetSpeed(0f);
        }

        private void ApplyKnockback()
        {
            if (_knockbackVelocity.sqrMagnitude < 0.001f)
            {
                _knockbackVelocity = Vector3.zero;
                return;
            }

            Vector3 displacement = _knockbackVelocity * Time.deltaTime;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _agentDriving)
                _agent.Move(displacement);
            else
            {
                transform.position = SampleGround(transform.position + displacement);
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                    _agent.nextPosition = transform.position;
            }
            _knockbackVelocity = Vector3.MoveTowards(
                _knockbackVelocity, Vector3.zero, knockbackRecovery * Time.deltaTime);
        }

        private bool CanSeePlayer()
        {
            if (_player == null || _player.IsDefeated)
                return false;
            Vector3 target = _player.transform.position + Vector3.up;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 toPlayer = target - origin;
            float distance = toPlayer.magnitude;
            if (distance > detectionDistance)
                return false;
            if (distance <= 0.001f)
                return true;
            if (distance > closeDetectionDistance &&
                Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f)
                return false;

            int hitCount = Physics.RaycastNonAlloc(
                origin, toPlayer / distance, _sightHits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            PlayerHealth nearestPlayer = null;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _sightHits[i].collider;
                if (hitCollider == null || hitCollider.transform == transform ||
                    hitCollider.transform.IsChildOf(transform))
                    continue;

                if (_sightHits[i].distance < nearestDistance)
                {
                    nearestDistance = _sightHits[i].distance;
                    nearestPlayer = hitCollider.GetComponentInParent<PlayerHealth>();
                }
            }

            return nearestDistance == float.MaxValue || nearestPlayer == _player;
        }

        private void FaceTowards(Vector3 target)
        {
            FaceDirection(target - transform.position);
        }

        private void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void SetSpeed(float speed)
        {
            if (_animator != null && HasParameter("Speed", AnimatorControllerParameterType.Float))
                _animator.SetFloat("Speed", speed);
        }

        private void SetTrigger(string parameter)
        {
            if (_animator != null && HasParameter(parameter, AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(parameter);
        }

        private void ResetTrigger(string parameter)
        {
            if (_animator != null && HasParameter(parameter, AnimatorControllerParameterType.Trigger))
                _animator.ResetTrigger(parameter);
        }

        private void SetBool(string parameter, bool value)
        {
            if (_animator != null && HasParameter(parameter, AnimatorControllerParameterType.Bool))
                _animator.SetBool(parameter, value);
        }

        private void SetInteger(string parameter, int value)
        {
            if (_animator != null && HasParameter(parameter, AnimatorControllerParameterType.Int))
                _animator.SetInteger(parameter, value);
        }

        private bool HasParameter(string parameter, AnimatorControllerParameterType type)
        {
            return _animatorParameters.TryGetValue(Animator.StringToHash(parameter), out var actualType) &&
                   actualType == type;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }

        private void UpdateHealthBar()
        {
            if (_healthBar == null)
                return;
            bool visible = IsEngaged || Time.time < _showHealthUntil;
            if (visible)
                _healthBar.Show();
            else
                _healthBar.Hide();
        }

        private void Die()
        {
            _isDead = true;
            _state = BearState.Dead;
            _healthBar?.Show();
            StopAgent();
            if (_agent != null)
                _agent.enabled = false;
            foreach (Collider collider in _colliders)
                if (collider != null)
                    collider.enabled = false;

            if (_animator != null)
            {
                ResetTrigger("Attack");
                ResetTrigger("Hit");
                ResetTrigger("Roar");
                SetBool("Dead", true);
                int deathState = Animator.StringToHash("Death");
                if (_animator.HasState(0, deathState))
                    _animator.CrossFadeInFixedTime(deathState, 0.08f, 0, 0f);
            }
            PlayClip(deathClip);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 home = Application.isPlaying ? _homePosition : transform.position;
            Gizmos.color = new Color(0.72f, 0.22f, 0.16f, 0.8f);
            Gizmos.DrawWireSphere(home, leashDistance);
            Gizmos.color = new Color(0.9f, 0.67f, 0.22f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 1.1f + Vector3.up * 0.9f, attackHitRadius);
        }
    }
}
