using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                // сюда — Player
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); // точка, куда смотрит камера (голова)

    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 8f;

    [Header("Mouse")]
    public float mouseSensitivity = 200f;
    public bool invertY = false;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Smoothing")]
    public float positionSmooth = 12f;
    public float rotationSmooth = 18f;

    [Header("Collision")]
    public LayerMask collisionMask;         // слои стен/земли
    public float collisionRadius = 0.25f;

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        if (target == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) target = pc.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = target != null ? target.eulerAngles.y : 0f;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Ввод мыши
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        yaw += mx;
        pitch += my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 2. Целевая позиция и поворот
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 desiredPos = pivot - rotation * Vector3.forward * distance;

        // 3. Проверка столкновений со стенами
        if (Physics.SphereCast(pivot, collisionRadius, (desiredPos - pivot).normalized,
            out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = pivot - rotation * Vector3.forward * Mathf.Max(hit.distance, minDistance);
        }

        // 4. Плавное применение
        transform.position = Vector3.Lerp(transform.position, desiredPos, positionSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSmooth * Time.deltaTime);
    }
}