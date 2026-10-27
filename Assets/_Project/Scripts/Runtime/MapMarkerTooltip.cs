using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Prince
{
    public sealed class MapMarkerTooltip : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        private MapSystem mapSystem;
        private MapMarker marker;
        private TextMeshProUGUI tooltip;

        public void Initialize(MapSystem owner, MapMarker mapMarker, TextMeshProUGUI label)
        {
            mapSystem = owner;
            marker = mapMarker;
            tooltip = label;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null && marker != null)
            {
                tooltip.text = mapSystem != null ? mapSystem.GetMarkerTooltip(marker) : marker.DisplayName;
                tooltip.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }
    }
}
