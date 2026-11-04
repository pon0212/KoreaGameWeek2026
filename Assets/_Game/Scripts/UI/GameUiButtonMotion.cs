using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TastePassport.UI
{
    /// <summary>Quiet child-visual feedback; leaves the selectable, hit area and click listeners untouched.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class GameUiButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Button button;
        private Transform[] visuals;
        private Vector3[] originalScales;
        private bool pressed;
        private bool hovered;
        private bool selected;
        private float scale = 1f;

        private void Awake()
        {
            button = GetComponent<Button>();
            visuals = new Transform[transform.childCount];
            originalScales = new Vector3[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i] = transform.GetChild(i);
                originalScales[i] = visuals[i].localScale;
            }
        }

        private void Update() => Animate(Time.unscaledDeltaTime);

        private void Animate(float deltaTime)
        {
            if (button == null) return;
            if (!button.IsActive() || !button.IsInteractable())
            {
                ResetVisuals();
                return;
            }
            float target = pressed ? GameUiTheme.PressedVisualScale :
                hovered || selected ? GameUiTheme.HighlightedVisualScale : 1f;
            scale = Mathf.MoveTowards(scale, target, Mathf.Max(0f, deltaTime) / GameUiTheme.ButtonMotionSeconds);
            ApplyScale();
        }

        private void ApplyScale()
        {
            if (visuals == null) return;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].localScale = originalScales[i] * scale;
        }

        private void ResetVisuals()
        {
            pressed = hovered = selected = false;
            scale = 1f;
            ApplyScale();
        }

        private void OnDisable() => ResetVisuals();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetVisuals(); }
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && button.IsInteractable()) pressed = true;
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) pressed = false;
        }
        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;
        public void OnSelect(BaseEventData data) => selected = true;
        public void OnDeselect(BaseEventData data) { selected = pressed = false; }
    }
}
