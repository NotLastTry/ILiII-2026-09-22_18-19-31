using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Настройки чувствительности")]
    public float mouseSensitivity = 15f;

    [Header("Ссылки на объекты")]
    public Transform playerBody; // Сюда в инспекторе перетащите родительский объект Игрока

    private GameInput inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f; // Текущий поворот по вертикали

    private void Awake()
    {
        inputActions = new GameInput();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        // Подписываемся на чтение мыши
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        // Прячем курсор мыши и блокируем его в центре экрана, чтобы он не вылетал за пределы игры
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // Получаем значения движения мыши, умноженные на чувствительность и время
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // 1. Поворот по вертикали (Вверх / Вниз) — вращаем саму КАМЕРУ
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ограничиваем обзор, чтобы не смотреть "внутрь себя"

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Поворот по горизонтали (Влево / Вправо) — вращаем ВСЕ ТЕЛО игрока
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
