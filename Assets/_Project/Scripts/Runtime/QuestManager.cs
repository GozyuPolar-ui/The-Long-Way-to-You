using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prince
{
    [DefaultExecutionOrder(-1000)]
    public sealed class QuestManager : MonoBehaviour
    {
        [SerializeField] private QuestDefinition[] definitions = Array.Empty<QuestDefinition>();

        private readonly Dictionary<string, QuestDefinition> definitionsById =
            new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, QuestProgress> progressById =
            new Dictionary<string, QuestProgress>(StringComparer.Ordinal);
        private readonly Dictionary<string, QuestTarget> targetsById =
            new Dictionary<string, QuestTarget>(StringComparer.Ordinal);
        private readonly HashSet<string> defeatedTargetIds =
            new HashSet<string>(StringComparer.Ordinal);

        public static QuestManager Instance { get; private set; }
        public event Action<QuestDefinition, QuestProgress> OnQuestChanged;
        public event Action<QuestDefinition, QuestProgress> OnQuestCompleted;
        public event Action<QuestDefinition, QuestObjectiveDefinition, bool> OnObjectiveChanged;

        public IReadOnlyList<QuestDefinition> Definitions => definitions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildDefinitionIndex();
            foreach (QuestDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                    continue;

                progressById[definition.id] = new QuestProgress
                {
                    questId = definition.id,
                    state = QuestState.NotStarted,
                    objectiveCompleted = new bool[definition.objectives != null ? definition.objectives.Length : 0]
                };
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void BuildDefinitionIndex()
        {
            definitionsById.Clear();
            if (definitions == null)
                return;

            foreach (QuestDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    Debug.LogWarning("QuestManager skipped a missing or ID-less quest definition.", this);
                    continue;
                }

                if (definitionsById.ContainsKey(definition.id))
                {
                    Debug.LogWarning($"Duplicate quest ID '{definition.id}' was ignored.", definition);
                    continue;
                }

                definitionsById.Add(definition.id, definition);
            }
        }

        public bool StartQuest(string questId)
        {
            if (!TryGetQuest(questId, out QuestDefinition definition, out QuestProgress progress) ||
                progress.state != QuestState.NotStarted)
                return false;

            progress.state = QuestState.Active;
            OnQuestChanged?.Invoke(definition, progress);

            if (definition.objectives != null)
            {
                foreach (QuestObjectiveDefinition objective in definition.objectives)
                {
                    if (objective != null && objective.type == QuestObjectiveType.Defeat &&
                        !string.IsNullOrWhiteSpace(objective.targetId) &&
                        defeatedTargetIds.Contains(objective.targetId))
                        SetObjectiveDone(definition.id, objective.id);
                }
            }

            return true;
        }

        public bool SetObjectiveDone(string questId, string objectiveId)
        {
            if (!TryGetQuest(questId, out QuestDefinition definition, out QuestProgress progress) ||
                progress.state == QuestState.NotStarted || progress.state == QuestState.Completed ||
                definition.objectives == null)
                return false;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective == null || !string.Equals(objective.id, objectiveId, StringComparison.Ordinal))
                    continue;
                if (progress.objectiveCompleted[i])
                    return false;

                progress.objectiveCompleted[i] = true;
                OnObjectiveChanged?.Invoke(definition, objective, true);
                UpdateQuestState(definition, progress);
                OnQuestChanged?.Invoke(definition, progress);
                return true;
            }

            return false;
        }

        public bool CompleteQuest(string questId)
        {
            if (!TryGetQuest(questId, out QuestDefinition definition, out QuestProgress progress) ||
                progress.state != QuestState.ReadyToTurnIn)
                return false;

            if (definition.objectives != null)
            {
                for (int i = 0; i < definition.objectives.Length; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || progress.objectiveCompleted[i])
                        continue;
                    if (objective.type != QuestObjectiveType.TalkTo)
                        return false;

                    progress.objectiveCompleted[i] = true;
                    OnObjectiveChanged?.Invoke(definition, objective, true);
                }
            }

            progress.state = QuestState.Completed;
            OnQuestChanged?.Invoke(definition, progress);
            OnQuestCompleted?.Invoke(definition, progress);
            return true;
        }

        public QuestState GetState(string questId)
        {
            return !string.IsNullOrEmpty(questId) &&
                progressById.TryGetValue(questId, out QuestProgress progress)
                ? progress.state
                : QuestState.NotStarted;
        }

        public IReadOnlyList<QuestObjectiveProgress> GetActiveObjectives()
        {
            var active = new List<QuestObjectiveProgress>();
            foreach (QuestDefinition definition in definitions)
            {
                if (definition == null ||
                    !progressById.TryGetValue(definition.id, out QuestProgress progress) ||
                    progress.state == QuestState.NotStarted || progress.state == QuestState.Completed ||
                    definition.objectives == null)
                    continue;

                for (int i = 0; i < definition.objectives.Length; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || progress.objectiveCompleted[i])
                        continue;
                    active.Add(new QuestObjectiveProgress
                    {
                        quest = definition,
                        objective = objective,
                        completed = false
                    });
                    break;
                }
            }

            return active;
        }

        public static string GetTrackedTargetId(QuestObjectiveProgress activeObjective)
        {
            if (activeObjective == null || activeObjective.objective == null)
                return string.Empty;

            if (activeObjective.objective.type == QuestObjectiveType.Defeat &&
                activeObjective.quest != null && activeObjective.quest.objectives != null)
            {
                foreach (QuestObjectiveDefinition objective in activeObjective.quest.objectives)
                    if (objective != null && objective.type == QuestObjectiveType.Collect)
                        return objective.targetId;
            }

            return activeObjective.objective.targetId;
        }

        public void RegisterTarget(QuestTarget target)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.targetId))
                return;

            if (targetsById.TryGetValue(target.targetId, out QuestTarget existing) &&
                existing != null && existing != target)
                Debug.LogWarning($"Multiple quest targets use ID '{target.targetId}'. The latest target is registered.", target);
            targetsById[target.targetId] = target;
        }

        public void UnregisterTarget(QuestTarget target)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.targetId))
                return;
            if (targetsById.TryGetValue(target.targetId, out QuestTarget existing) && existing == target)
                targetsById.Remove(target.targetId);
        }

        public bool TryGetTarget(string targetId, out QuestTarget target)
        {
            if (!string.IsNullOrEmpty(targetId) &&
                targetsById.TryGetValue(targetId, out target) && target != null)
                return true;
            if (!string.IsNullOrEmpty(targetId))
                targetsById.Remove(targetId);
            target = null;
            return false;
        }

        public void NotifyTargetTalkedTo(string targetId)
        {
            SetObjectivesDoneForTarget(targetId, QuestObjectiveType.TalkTo);
        }

        public void NotifyTargetCollected(string targetId)
        {
            SetObjectivesDoneForTarget(targetId, QuestObjectiveType.Collect);
        }

        public void NotifyTargetDefeated(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                return;

            defeatedTargetIds.Add(targetId);
            SetObjectivesDoneForTarget(targetId, QuestObjectiveType.Defeat);
        }

        private void SetObjectivesDoneForTarget(string targetId, QuestObjectiveType type)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                return;

            foreach (QuestDefinition definition in definitions)
            {
                if (definition == null || definition.objectives == null ||
                    !progressById.TryGetValue(definition.id, out QuestProgress progress) ||
                    progress.state == QuestState.NotStarted || progress.state == QuestState.Completed)
                    continue;

                for (int i = 0; i < definition.objectives.Length; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (progress.objectiveCompleted[i] || objective == null)
                        continue;

                    if (objective.type == type &&
                        string.Equals(objective.targetId, targetId, StringComparison.Ordinal))
                        SetObjectiveDone(definition.id, objective.id);

                    break;
                }
            }
        }

        private void UpdateQuestState(QuestDefinition definition, QuestProgress progress)
        {
            if (definition.objectives == null || definition.objectives.Length == 0)
                return;

            bool remainingNonTurnInObjective = false;
            for (int i = 0; i < definition.objectives.Length; i++)
            {
                if (progress.objectiveCompleted[i])
                    continue;

                if (definition.objectives[i] == null ||
                    definition.objectives[i].type != QuestObjectiveType.TalkTo)
                    remainingNonTurnInObjective = true;
            }

            progress.state = !remainingNonTurnInObjective
                ? QuestState.ReadyToTurnIn
                : QuestState.Active;
        }

        private bool TryGetQuest(
            string questId,
            out QuestDefinition definition,
            out QuestProgress progress)
        {
            if (questId != null && definitionsById.TryGetValue(questId, out definition) &&
                progressById.TryGetValue(questId, out progress))
                return true;

            definition = null;
            progress = null;
            return false;
        }
    }
}
