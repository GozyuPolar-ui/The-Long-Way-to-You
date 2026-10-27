using UnityEngine;
using UnityEngine.EventSystems;

namespace Prince
{
    public sealed class MapUiSurface : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IScrollHandler,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum Surface { Minimap, FullMap }

        private MapSystem mapSystem;
        private Surface surface;

        public void Initialize(MapSystem owner, Surface mapSurface)
        {
            mapSystem = owner;
            surface = mapSurface;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            mapSystem?.OnPointerEnterMap(surface);

        public void OnPointerExit(PointerEventData eventData) =>
            mapSystem?.OnPointerExitMap(surface);

        public void OnScroll(PointerEventData eventData) =>
            mapSystem?.OnMapScroll(eventData, surface);

        public void OnPointerDown(PointerEventData eventData) =>
            mapSystem?.OnMapPointerDown(eventData, surface);

        public void OnDrag(PointerEventData eventData) =>
            mapSystem?.OnMapDrag(eventData, surface);

        public void OnPointerUp(PointerEventData eventData) =>
            mapSystem?.OnMapPointerUp(eventData, surface);
    }
}
