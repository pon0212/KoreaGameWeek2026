using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TastePassport.UI
{
    // A tap is the primary page action; a horizontal swipe is an optional shortcut.
    public sealed class BookSwipeTurn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private Action tap;
        private Action<int> turn;
        private Vector2 start;
        private bool pressed;
        private bool swiped;

        public void Initialize(Action onTap, Action<int> onTurn = null)
        {
            tap = onTap;
            turn = onTurn;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            start = eventData.position;
            pressed = true;
            swiped = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            Vector2 travel = eventData.position - start;
            if (Mathf.Abs(travel.x) < 80f || Mathf.Abs(travel.x) < Mathf.Abs(travel.y) * 1.4f)
                return;
            swiped = true;
            turn?.Invoke(travel.x < 0 ? 1 : -1);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!swiped) tap?.Invoke();
        }

        private void OnDisable()
        {
            pressed = false;
            swiped = false;
        }
    }
}
