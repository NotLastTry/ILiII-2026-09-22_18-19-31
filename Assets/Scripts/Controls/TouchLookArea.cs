using UnityEngine;
using UnityEngine.EventSystems;

namespace LabGame.Controls
{
    /// <summary>
    /// Зона обзора: свайп по правой части экрана вращает камеру.
    /// Накопленный за кадр сдвиг забирает InputRouter.
    /// </summary>
    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        [Tooltip("Перевод пикселей свайпа в единицы чувствительности камеры.")]
        public float sensitivity = 0.1f;

        private Vector2 accumulated;

        public void OnDrag(PointerEventData eventData)
        {
            accumulated += eventData.delta * sensitivity;
        }

        /// <summary>Отдаёт накопленный сдвиг и обнуляет его.</summary>
        public Vector2 ConsumeLook()
        {
            Vector2 value = accumulated;
            accumulated = Vector2.zero;
            return value;
        }

        public void ResetInput()
        {
            accumulated = Vector2.zero;
        }
    }
}
