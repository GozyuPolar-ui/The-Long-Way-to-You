using UnityEngine;

namespace Prince.Closing
{
    public static class QuestProgress
    {
        private const string CompletedKey = "Prince.Closing.QuestCompleted";

        public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

        public static void Complete()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.Save();
        }
    }
}
