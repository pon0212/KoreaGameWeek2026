using System;
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
        private Vector3 originalScale;

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
            originalScale = rect.localScale;
            transform.SetParent(rootCanvas.transform, true);
            rect.localScale = originalScale * 1.06f;
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
            if (Placed) { group.blocksRaycasts = false; return; }
            group.blocksRaycasts = true;
            if (originalParent == null) return;
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSibling);
            rect.localScale = originalScale;
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
            rect.localScale = Vector3.one;
            group.blocksRaycasts = false;
        }
    }

    /// <summary>Completes the Bibimbap action after the player makes a deliberate mixing swipe.</summary>
    public sealed class MixGestureHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float MinimumTravel = 110f;
        private Action completed;
        private Vector2 start;
        private Vector2 last;
        private float travel;
        private bool active;
        private bool fired;

        public void Initialize(Action onCompleted) => completed = onCompleted;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (fired) return;
            active = true;
            start = last = eventData.position;
            travel = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!active || fired) return;
            travel += (eventData.position - last).magnitude;
            last = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!active || fired) return;
            active = false;
            float horizontal = Mathf.Abs(eventData.position.x - start.x);
            float vertical = Mathf.Abs(eventData.position.y - start.y);
            if (travel < MinimumTravel || horizontal < MinimumTravel || horizontal <= vertical * 1.15f) return;
            fired = true;
            completed?.Invoke();
        }
    }
}
