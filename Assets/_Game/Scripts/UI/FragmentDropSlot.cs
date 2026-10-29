using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TastePassport.UI
{
    public sealed class FragmentDropSlot : MonoBehaviour, IDropHandler
    {
        public int SlotIndex { get; set; }
        public Action<int, FragmentDrag> TryDrop { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            var piece = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<FragmentDrag>();
            if (piece != null) TryDrop?.Invoke(SlotIndex, piece);
        }
    }
}
