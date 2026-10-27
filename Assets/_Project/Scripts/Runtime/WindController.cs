using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Prince
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WindController : MonoBehaviour
    {
        private static readonly int GlobalWindDirectionId = Shader.PropertyToID("_GlobalWindDirection");
        private static readonly int GlobalWindStrengthId = Shader.PropertyToID("_GlobalWindStrength");
        private static readonly int GlobalWindSpeedId = Shader.PropertyToID("_GlobalWindSpeed");
        private static readonly int GlobalGustStrengthId = Shader.PropertyToID("_GlobalGustStrength");

        [Header("Global Wind")]
        public Vector2 direction = new Vector2(1f, 0f);
        [Min(0f)] public float strength = 0.35f;
        [Min(0f)] public float speed = 1f;

        [Header("Gusts")]
        [Min(0f)] public float gustStrength = 0.35f;
        [Min(0.001f)] public float gustFrequency = 0.08f;
        public bool animateInEditMode = true;

        private void OnEnable()
        {
            PushShaderSettings(0f);
        }

        private void OnValidate()
        {
            strength = Mathf.Max(0f, strength);
            speed = Mathf.Max(0f, speed);
            gustStrength = Mathf.Max(0f, gustStrength);
            gustFrequency = Mathf.Max(0.001f, gustFrequency);
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        }

        private void Update()
        {
            if (!Application.isPlaying && !animateInEditMode)
            {
                PushShaderSettings(0f);
                return;
            }

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            PushShaderSettings(time);

#if UNITY_EDITOR
            if (!Application.isPlaying && animateInEditMode)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
#endif
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(GlobalWindStrengthId, 0f);
            Shader.SetGlobalFloat(GlobalGustStrengthId, 0f);
        }

        private void PushShaderSettings(float time)
        {
            Vector2 windDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            float noise = Mathf.PerlinNoise(time * gustFrequency, 0.37f);
            float gust = noise * gustStrength;

            Shader.SetGlobalVector(
                GlobalWindDirectionId,
                new Vector4(windDirection.x, 0f, windDirection.y, 0f));
            Shader.SetGlobalFloat(GlobalWindStrengthId, strength);
            Shader.SetGlobalFloat(GlobalWindSpeedId, speed);
            Shader.SetGlobalFloat(GlobalGustStrengthId, gust);
        }
    }
}
