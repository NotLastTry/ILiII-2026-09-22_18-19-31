using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PhysicsModule : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 6f;
    public float acceleration = 40f;
    public float deceleration = 50f;
    public float rotationSpeed = 12f;   // скорость поворота модели

    [Header("Jump")]
    public float jumpForce = 12f;
    public float gravity = 30f;
    public float terminalVelocity = 20f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Crouch")]
    public float crouchSpeedMultiplier = 0.4f;
    public float crouchHeight = 1.0f;
    public float standHeight = 1.8f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.15f;
    public float ceilingCheckDistance = 0.15f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // Состояние (только чтение снаружи)
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool HasCeiling { get; private set; }
    public bool IsCrouching { get; private set; }

    private float coyoteTimer;
    private float jumpBufferTimer;
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Transform modelTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Ищем модель среди детей
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) modelTransform = anim.transform;
        else
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null) modelTransform = mr.transform;
        }

        rb.useGravity = false;              // гравитацию считаем вручную
        rb.freezeRotation = true;           // чтобы физика не крутила персонажа
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // ─────────────────────────────────────────────
    //  Проверка окружения
    // ─────────────────────────────────────────────
    public void CheckEnvironment()
    {
        // Точка старта — центр капсулы
        Vector3 origin = capsule.bounds.center;
        float radius = capsule.radius * 0.9f;   // чуть меньше радиуса, чтобы не задевать стены
        float halfHeight = Mathf.Max(capsule.height * 0.5f - capsule.radius, 0.01f);

        // SphereCast вниз
        IsGrounded = Physics.SphereCast(
            origin, radius, Vector3.down,
            out _, halfHeight + groundCheckDistance,
            groundLayer, QueryTriggerInteraction.Ignore);

        // SphereCast вверх
        HasCeiling = Physics.SphereCast(
            origin, radius, Vector3.up,
            out _, halfHeight + ceilingCheckDistance,
            groundLayer, QueryTriggerInteraction.Ignore);
    }

    // ─────────────────────────────────────────────
    //  Таймеры coyote time / jump buffer
    // ─────────────────────────────────────────────
    public void TickTimers(float dt, bool jumpPressed)
    {
        if (IsGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;

        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= dt;

        coyoteTimer = Mathf.Max(coyoteTimer, 0f);
        jumpBufferTimer = Mathf.Max(jumpBufferTimer, 0f);
    }

    public bool CanJump() => jumpBufferTimer > 0f && coyoteTimer > 0f;

    // ─────────────────────────────────────────────
    //  Гравитация
    // ─────────────────────────────────────────────
    public void ApplyGravity(float dt)
    {
        if (IsGrounded && Velocity.y <= 0f)
        {
            Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
            return;
        }

        float newY = Velocity.y - gravity * dt;
        if (newY < -terminalVelocity) newY = -terminalVelocity;
        Velocity = new Vector3(Velocity.x, newY, Velocity.z);
    }

    // ─────────────────────────────────────────────
    //  Прыжок
    // ─────────────────────────────────────────────
    public void ApplyJump()
    {
        Velocity = new Vector3(Velocity.x, jumpForce, Velocity.z);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        IsGrounded = false;
    }

    public void ApplyJumpCut()
    {
        if (Velocity.y > 0f)
            Velocity = new Vector3(Velocity.x, Velocity.y * jumpCutMultiplier, Velocity.z);
    }

    // ─────────────────────────────────────────────
    //  Горизонтальное движение + поворот модели
    //  moveInput — Vector2 (x = горизонталь, y = вперёд/назад)
    //  cameraForward — направление "вперёд" относительно камеры
    // ─────────────────────────────────────────────
    public void ApplyHorizontal(Vector2 moveInput, Vector3 cameraForward, float dt)
    {
        // Направление движения в мировых координатах
        Vector3 camForward = cameraForward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = new Vector3(camForward.z, 0f, -camForward.x);

        Vector3 moveDir = camRight * moveInput.x + camForward * moveInput.y;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float targetSpeedScale = IsCrouching ? crouchSpeedMultiplier : 1f;
        Vector3 targetVelocity = moveDir * maxSpeed * targetSpeedScale;

        float rate = moveDir.sqrMagnitude > 0.001f ? acceleration : deceleration;
        Vector3 current = new Vector3(Velocity.x, 0f, Velocity.z);
        Vector3 newHorizontal = Vector3.MoveTowards(current, targetVelocity, rate * dt);

        Velocity = new Vector3(newHorizontal.x, Velocity.y, newHorizontal.z);

        // Поворот модели в сторону движения
        if (moveDir.sqrMagnitude > 0.001f && modelTransform != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation, targetRot, rotationSpeed * dt);
        }
    }

    // ─────────────────────────────────────────────
    //  Перемещение
    // ─────────────────────────────────────────────
    public void Move(float dt)
    {
        rb.MovePosition(rb.position + Velocity * dt);
    }

    public void ResetVerticalVelocity()
    {
        Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
    }

    // ─────────────────────────────────────────────
    //  Приседание (изменение высоты капсулы)
    // ─────────────────────────────────────────────
    public void SetCrouch(bool crouch)
    {
        if (IsCrouching == crouch) return;

        if (crouch)
        {
            IsCrouching = true;
            capsule.height = crouchHeight;
            capsule.center = new Vector3(0f, crouchHeight * 0.5f, 0f);

            if (modelTransform != null)
                modelTransform.localScale = new Vector3(1f, crouchHeight / standHeight, 1f);
        }
        else
        {
            // Не вставать, если над головой потолок
            if (HasCeiling) return;

            IsCrouching = false;
            capsule.height = standHeight;
            capsule.center = new Vector3(0f, standHeight * 0.5f, 0f);

            if (modelTransform != null)
                modelTransform.localScale = Vector3.one;
        }
    }

    // ─────────────────────────────────────────────
    //  Отладка
    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || capsule == null) return;

        Vector3 origin = capsule.bounds.center;
        float halfHeight = Mathf.Max(capsule.height * 0.5f - capsule.radius, 0.01f);

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * (halfHeight + groundCheckDistance));

        Gizmos.color = HasCeiling ? Color.yellow : Color.cyan;
        Gizmos.DrawLine(origin, origin + Vector3.up * (halfHeight + ceilingCheckDistance));
    }
}