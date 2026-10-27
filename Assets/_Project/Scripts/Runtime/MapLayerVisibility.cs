using UnityEngine;

namespace Prince
{
    public sealed class MapLayerVisibility : MonoBehaviour
    {
        [SerializeField] private Transform[] affectedObjects;
        [SerializeField] private int[] originalLayers;
        [SerializeField] private bool treeGroup;

        public bool IsTreeGroup => treeGroup;
        public int ChangedObjectCount => affectedObjects != null ? affectedObjects.Length : 0;

        public void Apply(int hiddenLayer, int treeLayer, bool isTreeGroup)
        {
            Restore();
            treeGroup = isTreeGroup;
            affectedObjects = GetComponentsInChildren<Transform>(true);
            originalLayers = new int[affectedObjects.Length];
            for (int i = 0; i < affectedObjects.Length; i++)
            {
                Transform child = affectedObjects[i];
                if (child == null)
                    continue;

                originalLayers[i] = child.gameObject.layer;
                child.gameObject.layer = treeGroup ? treeLayer : hiddenLayer;
            }
        }

        public void Restore()
        {
            if (affectedObjects == null || originalLayers == null)
                return;

            int count = Mathf.Min(affectedObjects.Length, originalLayers.Length);
            for (int i = 0; i < count; i++)
            {
                if (affectedObjects[i] != null)
                    affectedObjects[i].gameObject.layer = originalLayers[i];
            }
            affectedObjects = null;
            originalLayers = null;
        }
    }
}
