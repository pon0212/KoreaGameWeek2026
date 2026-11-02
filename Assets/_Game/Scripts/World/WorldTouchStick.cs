using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TastePassport.World
{
    public sealed class WorldTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform controlRect;
        private RectTransform handle;
        private Action<Vector2> onDirectionChanged;
        private int activePointerId = int.MinValue;

        public void Initialize(RectTransform stickHandle, Action<Vector2> directionChanged)
        {
            controlRect = transform as RectTransform;
            handle = stickHandle;
            onDirectionChanged = directionChanged;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue) return;
            activePointerId = eventData.pointerId;
            UpdateDirection(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId) UpdateDirection(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = int.MinValue;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            onDirectionChanged?.Invoke(Vector2.zero);
        }

        private void UpdateDirection(PointerEventData eventData)
        {
            if (controlRect == null) controlRect = transform as RectTransform;
            if (controlRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    controlRect, eventData.position, eventData.pressEventCamera, out var localPoint)) return;

            float radius = Mathf.Max(1f, Mathf.Min(controlRect.rect.width, controlRect.rect.height) * 0.38f);
            Vector2 direction = Vector2.ClampMagnitude(localPoint / radius, 1f);
            if (handle != null) handle.anchoredPosition = direction * radius;
            onDirectionChanged?.Invoke(direction);
        }
    }
}
