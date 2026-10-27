using UnityEngine;
using UnityEngine.Events;

namespace Prince
{
    public sealed class NPCInfo : MonoBehaviour, IInteractable
    {
        public string displayName;
        public string role;
        public string dialogueId = string.Empty;
        public UnityEvent OnInteract = new UnityEvent();

        public string PromptText => GameLocalization.Get("Press E to talk: {0}", DisplayName);
        public string DisplayName => GameLocalization.Get(
            string.IsNullOrWhiteSpace(displayName) ? name : displayName);

        public void Interact()
        {
            Debug.Log($"Interacted with {DisplayName} ({role}).", this);
            OnInteract?.Invoke();
        }
    }
}
