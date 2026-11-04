using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LabGame.Controls
{
    /// <summary>
    /// Экранный (сенсорный) джойстик: круглый фон и «шайба», которая следует за пальцем.
    ///
    /// Позиция указателя опрашивается каждый кадр, пока джойстик удерживают,
    /// а не только по событиям перетаскивания. Так джойстик одинаково работает
    /// на телефоне, мышью в редакторе и в любом случае, когда uGUI не присылает
    /// drag-события (например, при заблокированном курсоре).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Размеры")]
        public float radius = 150f;
        [Range(0f, 0.5f)] public float deadZone = 0.15f;

        [Header("Ссылки")]
        public RectTransform knob;

        private RectTransform area;
        private int pointerId = -1;
        private bool held;

        /// <summary>Нормализованное направление −1…1.</summary>
        public Vector2 Value { get; private set; }

        public bool IsHeld => held;

        private void Awake()
        {
            area = (RectTransform)transform;
        }

        private void OnDisable()
        {
            ResetValue();
        }

        // ── События uGUI ──────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (held) return;

            held = true;
            pointerId = eventData.pointerId;

            UpdateValue(eventData.position, eventData.pressEventCamera);
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateValue(eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetValue();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetValue();
        }

        // ── Опрос указателя каждый кадр ───────────────────────────

        private void Update()
        {
            if (!held) return;

            if (!TryGetPointerPosition(out Vector2 screenPosition))
            {
                ResetValue();
                return;
            }

            UpdateValue(screenPosition, null);
        }

        /// <summary>Текущая позиция пальца/курсора, который держит джойстик.</summary>
        private bool TryGetPointerPosition(out Vector2 position)
        {
            if (pointerId < 0)
            {
                // Мышь: пока кнопка зажата — тянем шайбу за курсором.
                if (!Input.GetMouseButton(0))
                {
                    position = Vector2.zero;
                    return false;
                }

                position = Input.mousePosition;
                return true;
            }

            int count = Input.touchCount;
            for (int i = 0; i < count; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId != pointerId) continue;

                position = touch.position;
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        // ── Внутреннее ────────────────────────────────────────────

        public void ResetValue()
        {
            held = false;
            pointerId = -1;
            Value = Vector2.zero;

            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(Vector2 screenPosition, Camera camera)
        {
            if (area == null) area = (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, camera, out var local))
                return;

            Vector2 clamped = JoystickMath.ClampToCircle(local, radius);

            if (knob != null) knob.anchoredPosition = clamped;

            Value = JoystickMath.ApplyDeadZone(clamped / Mathf.Max(1f, radius), deadZone);
        }
    }
}
