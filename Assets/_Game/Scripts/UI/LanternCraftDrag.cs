using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TastePassport.UI
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class LanternCraftDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string MaterialId { get; private set; }
        public bool Accepted { get; private set; }

        private Canvas rootCanvas;
        private RectTransform rect;
        private CanvasGroup group;
        private Transform originalParent;
        private int originalSibling;

        public void Initialize(string materialId)
        {
            MaterialId = materialId;
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            Canvas canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas == null ? null : canvas.rootCanvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Accepted || rootCanvas == null) return;
            originalParent = transform.parent;
            originalSibling = transform.GetSiblingIndex();
            transform.SetParent(rootCanvas.transform, true);
            group.blocksRaycasts = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Accepted || originalParent == null) return;
            RectTransform canvasRect = (RectTransform)rootCanvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position,
                    eventData.pressEventCamera, out Vector2 point))
                rect.anchoredPosition = point;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Accepted)
            {
                if (group != null) group.blocksRaycasts = false;
                return;
            }
            if (group != null) group.blocksRaycasts = true;
            if (originalParent == null) return;
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSibling);
            originalParent = null;
        }

        public void Accept(Transform slot)
        {
            if (Accepted || slot == null) return;
            Accepted = true;
            originalParent = null;
            transform.SetParent(slot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
            group.blocksRaycasts = false;
        }
    }

    [RequireComponent(typeof(Image))]
    public sealed class LanternCraftDropZone : MonoBehaviour, IDropHandler
    {
        public string SlotId { get; private set; }
        public Action<string, string, LanternCraftDrag, Transform> Dropped { get; private set; }

        public void Initialize(string slotId, Action<string, string, LanternCraftDrag, Transform> dropped)
        {
            SlotId = slotId;
            Dropped = dropped;
            GetComponent<Image>().raycastTarget = true;
        }

        public void OnDrop(PointerEventData eventData)
        {
            LanternCraftDrag drag = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<LanternCraftDrag>();
            if (drag != null && !drag.Accepted) Dropped?.Invoke(SlotId, drag.MaterialId, drag, transform);
        }
    }
}
