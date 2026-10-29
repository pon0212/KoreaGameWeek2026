using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TastePassport.UI
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class FragmentDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int PieceIndex { get; set; }
        public bool Placed { get; private set; }
        private Canvas rootCanvas;
        private RectTransform rect;
        private CanvasGroup group;
        private Transform originalParent;
        private int originalSibling;

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Placed) return;
            originalParent = transform.parent;
            originalSibling = transform.GetSiblingIndex();
            transform.SetParent(rootCanvas.transform, true);
            group.blocksRaycasts = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Placed || originalParent == null) return;
            var canvasRect = (RectTransform)rootCanvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out var point))
                rect.anchoredPosition = point;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            group.blocksRaycasts = true;
            if (Placed || originalParent == null) return;
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSibling);
            originalParent = null;
        }

        public void Accept(Transform slot)
        {
            Placed = true;
            transform.SetParent(slot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            group.blocksRaycasts = false;
        }
    }
}
