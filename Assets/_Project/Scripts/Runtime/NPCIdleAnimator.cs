using UnityEngine;

namespace Prince
{
    public sealed class NPCIdleAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip[] idleClips;
        [SerializeField] private string[] idleStateNames;
        [SerializeField] private AnimationClip[] dialogueClips;
        [SerializeField] private string[] dialogueStateNames;
        [SerializeField, Min(0f)] private float talkRadius = 2.5f;
        [SerializeField, Min(0f)] private float idleChangeMin = 7f;
        [SerializeField, Min(0f)] private float idleChangeMax = 15f;
        [SerializeField, Min(0f)] private float crossFadeDuration = 0.2f;

        private Transform player;
        private float nextIdleChange;
        private float nextPlayerSearch;
        private float talkUntil;
        private bool isTalking;
        private bool wasNearPlayer;
        private bool warnedMissingClips;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            FindPlayer();
            if (animator == null)
            {
                Debug.LogWarning($"NPCIdleAnimator on '{name}' has no Animator.", this);
                enabled = false;
                return;
            }

            if (idleClips == null || idleClips.Length == 0 ||
                idleStateNames == null || idleStateNames.Length == 0)
            {
                WarnMissingClips();
                enabled = false;
                return;
            }

            PlayRandomIdle(true);
            ScheduleIdleChange();
        }

        private void Update()
        {
            if (player == null && Time.time >= nextPlayerSearch)
                FindPlayer();

            bool nearPlayer = player != null &&
                (player.position - transform.position).sqrMagnitude <= talkRadius * talkRadius;

            if (nearPlayer && !wasNearPlayer && !isTalking &&
                dialogueStateNames != null && dialogueStateNames.Length > 0)
            {
                int index = Random.Range(0, dialogueStateNames.Length);
                if (TryPlayState(dialogueStateNames[index]))
                {
                    isTalking = true;
                    float clipLength = dialogueClips != null && index < dialogueClips.Length &&
                        dialogueClips[index] != null ? dialogueClips[index].length : 2f;
                    talkUntil = Time.time + Mathf.Max(0.5f, clipLength);
                }
            }
            wasNearPlayer = nearPlayer;

            if (isTalking)
            {
                if (!nearPlayer || Time.time >= talkUntil)
                {
                    isTalking = false;
                    PlayRandomIdle(false);
                    ScheduleIdleChange();
                }
                return;
            }

            if (Time.time >= nextIdleChange)
            {
                PlayRandomIdle(false);
                ScheduleIdleChange();
            }
        }

        private void FindPlayer()
        {
            nextPlayerSearch = Time.time + 2f;
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        private void PlayRandomIdle(bool randomOffset)
        {
            if (idleStateNames == null || idleStateNames.Length == 0)
                return;

            int index = Random.Range(0, idleStateNames.Length);
            if (TryPlayState(idleStateNames[index]) && randomOffset)
                animator.Play(StateHash(idleStateNames[index]), 0, Random.value);
        }

        private bool TryPlayState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return false;

            int hash = StateHash(stateName);
            if (!animator.HasState(0, hash))
                return false;

            animator.CrossFadeInFixedTime(hash, crossFadeDuration);
            return true;
        }

        private static int StateHash(string stateName)
        {
            return Animator.StringToHash("Base Layer." + stateName);
        }

        private void ScheduleIdleChange()
        {
            float minimum = Mathf.Max(0f, idleChangeMin);
            float maximum = Mathf.Max(minimum, idleChangeMax);
            nextIdleChange = Time.time + Random.Range(minimum, maximum);
        }

        private void WarnMissingClips()
        {
            if (warnedMissingClips)
                return;

            warnedMissingClips = true;
            Debug.LogWarning($"NPCIdleAnimator on '{name}' has no usable idle clips.", this);
        }
    }
}
