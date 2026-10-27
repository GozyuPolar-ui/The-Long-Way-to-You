using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private CombatFeelSettings settings;

        private Renderer[] renderers;
        private MaterialPropertyBlock[] originalBlocks;
        private float remaining;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalBlocks = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalBlocks[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(originalBlocks[i]);
            }
        }

        private void Update()
        {
            if (remaining <= 0f)
                return;

            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
                RestoreProperties();
        }

        private void OnDisable()
        {
            remaining = 0f;
            RestoreProperties();
        }

        public void Flash(float duration)
        {
            if (settings == null || !settings.hitFlashEnabled || !settings.visualEffectsEnabled ||
                !settings.screenFlashEnabled)
                return;

            if (renderers == null)
                Awake();

            remaining = Mathf.Max(duration, Time.unscaledDeltaTime);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                renderers[i].GetPropertyBlock(originalBlocks[i]);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.white);
                block.SetColor(ColorId, Color.white);
                renderers[i].SetPropertyBlock(block);
            }
        }

        private void RestoreProperties()
        {
            if (renderers == null || originalBlocks == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].SetPropertyBlock(originalBlocks[i]);
        }
    }
}
