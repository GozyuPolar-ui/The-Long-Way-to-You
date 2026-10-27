using System;
using UnityEngine;

namespace Prince
{
    public enum QuestObjectiveType
    {
        Collect,
        TalkTo,
        Defeat
    }

    [Serializable]
    public sealed class QuestObjectiveDefinition
    {
        public string id;
        [TextArea] public string description;
        public QuestObjectiveType type;
        public string targetId;
    }

    [CreateAssetMenu(menuName = "Prince/Quest Definition", fileName = "Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public QuestObjectiveDefinition[] objectives = Array.Empty<QuestObjectiveDefinition>();
    }

    public enum QuestState
    {
        NotStarted,
        Active,
        ReadyToTurnIn,
        Completed
    }

    [Serializable]
    public sealed class QuestProgress
    {
        public string questId;
        public QuestState state;
        public bool[] objectiveCompleted = Array.Empty<bool>();
    }

    public sealed class QuestObjectiveProgress
    {
        public QuestDefinition quest;
        public QuestObjectiveDefinition objective;
        public bool completed;
    }
}
