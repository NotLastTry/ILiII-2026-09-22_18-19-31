using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LabGame.Controls
{
    /// <summary>
    /// Экранный (сенсорный) джойстик: круглая зона и «шайба», которую тянет палец.
    /// Работает и мышью — это удобно для демонстрации сенсорного режима на ПК.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Размеры")]
        public float radius = 150f;
        [Range(0f, 0.5f)] public float deadZone = 0.15f;

        [Header("Ссылки")]
        public RectTransform knob;

        private RectTransform area;
        private int pointerId = -1;

        /// <summary>Нормализованное направление −1…1.</summary>
        public Vector2 Value { get; private set; }

        public bool IsHeld => pointerId >= 0;

        private void Awake()
        {
            area = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId >= 0) return;

            pointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            ResetValue();
        }

        /// <summary>Возврат в нейтраль (отпустили палец или скрыли управление).</summary>
        public void ResetValue()
        {
            pointerId = -1;
            Value = Vector2.zero;

            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (area == null) area = (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position,
                    eventData.pressEventCamera, out var local))
                return;

            Vector2 clamped = JoystickMath.ClampToCircle(local, radius);

            if (knob != null) knob.anchoredPosition = clamped;

            Value = JoystickMath.ApplyDeadZone(clamped / Mathf.Max(1f, radius), deadZone);
        }
    }
}
