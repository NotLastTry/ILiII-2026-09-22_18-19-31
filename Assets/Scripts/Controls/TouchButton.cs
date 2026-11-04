using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LabGame.Controls
{
    /// <summary>
    /// Экранная кнопка. Работает как «удержание» (прыжок, приседание)
    /// и как «нажатие за кадр» (действие, инвентарь).
    /// </summary>
    public class TouchButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Image background;
        public Color idleColor = new Color(0.78f, 0.78f, 0.78f, 0.9f);
        public Color pressedColor = new Color(0.55f, 0.55f, 0.55f, 0.95f);

        private bool pressedFlag;
        private bool releasedFlag;

        public bool IsHeld { get; private set; }

        private void Awake()
        {
            if (background != null) background.color = idleColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            pressedFlag = true;
            ApplyColor(pressedColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsHeld) return;

            IsHeld = false;
            releasedFlag = true;
            ApplyColor(idleColor);
        }

        // Палец ушёл за пределы кнопки — считаем, что отпустили.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsHeld) return;

            IsHeld = false;
            releasedFlag = true;
            ApplyColor(idleColor);
        }

        /// <summary>Было ли нажатие с прошлого опроса.</summary>
        public bool ConsumePressed()
        {
            bool value = pressedFlag;
            pressedFlag = false;
            return value;
        }

        /// <summary>Было ли отпускание с прошлого опроса.</summary>
        public bool ConsumeReleased()
        {
            bool value = releasedFlag;
            releasedFlag = false;
            return value;
        }

        public void ResetState()
        {
            IsHeld = false;
            pressedFlag = false;
            releasedFlag = false;
            ApplyColor(idleColor);
        }

        private void ApplyColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
