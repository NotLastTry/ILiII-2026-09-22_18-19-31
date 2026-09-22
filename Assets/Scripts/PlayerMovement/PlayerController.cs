using UnityEngine;

[RequireComponent(typeof(InputReader), typeof(PhysicsModule))]
public class PlayerController : MonoBehaviour
{
    [Header("Land")]
    public float landDuration = 0.15f;

    [Header("Camera")]
    public Transform cameraTransform;   // назначить Main Camera

    private InputReader input;
    private PhysicsModule physics;
    private StateMachine fsm = new StateMachine();
    private Animator animator;

    void Awake()
    {
        input = GetComponent<InputReader>();
        physics = GetComponent<PhysicsModule>();
        animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        input.Read();
        fsm.Tick(Time.deltaTime);

        // Проверка земли и потолка — это raycast, ему не нужен FixedUpdate
        physics.CheckEnvironment();

        // Таймеры должны обновляться СРАЗУ после чтения ввода
        physics.TickTimers(Time.deltaTime, input.JumpPressed);

        UpdateState();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        physics.ApplyGravity(dt);

        if (input.JumpReleased) physics.ApplyJumpCut();

        Vector3 camForward = cameraTransform != null
            ? cameraTransform.forward
            : Vector3.forward;

        physics.ApplyHorizontal(input.MoveInput, camForward, dt);
        physics.Move(dt);
    }

    private void UpdateState()
    {
        // Приседание обрабатываем отдельно — оно не мешает Run/Idle
        bool wantCrouch = input.CrouchHeld && physics.IsGrounded;
        if (wantCrouch) physics.SetCrouch(true);
        else if (!physics.HasCeiling) physics.SetCrouch(false);

        switch (fsm.Current)
        {
            case CharacterState.Idle:
                if (input.JumpPressed && physics.CanJump()) { physics.ApplyJump(); fsm.ChangeState(CharacterState.Jump); }
                else if (physics.IsCrouching) fsm.ChangeState(CharacterState.Crouch);
                else if (input.MoveInput.sqrMagnitude > 0.01f) fsm.ChangeState(CharacterState.Run);
                else if (input.JumpPressed) Debug.Log($"Jump blocked: IsGrounded={physics.IsGrounded}, CanJump={physics.CanJump()}");
                break;

            case CharacterState.Run:
                if (input.JumpPressed && physics.CanJump()) { physics.ApplyJump(); fsm.ChangeState(CharacterState.Jump); }
                else if (physics.IsCrouching) fsm.ChangeState(CharacterState.Crouch);
                else if (input.MoveInput.sqrMagnitude < 0.01f) fsm.ChangeState(CharacterState.Idle);
                break;

            case CharacterState.Jump:
                if (physics.Velocity.y <= 0f) fsm.ChangeState(CharacterState.Fall);
                break;

            case CharacterState.Fall:
                if (physics.IsGrounded) fsm.ChangeState(CharacterState.Land);
                break;

            case CharacterState.Land:
                physics.ResetVerticalVelocity();
                if (fsm.StateTimer >= landDuration)
                {
                    if (physics.IsCrouching) fsm.ChangeState(CharacterState.Crouch);
                    else if (input.MoveInput.sqrMagnitude > 0.01f) fsm.ChangeState(CharacterState.Run);
                    else fsm.ChangeState(CharacterState.Idle);
                }
                break;

            case CharacterState.Crouch:
                if (!physics.IsCrouching)
                {
                    if (input.MoveInput.sqrMagnitude > 0.01f) fsm.ChangeState(CharacterState.Run);
                    else fsm.ChangeState(CharacterState.Idle);
                }
                break;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 horizontal = new Vector3(physics.Velocity.x, 0f, physics.Velocity.z);
        animator.SetFloat("Speed", horizontal.magnitude);
        animator.SetBool("IsGrounded", physics.IsGrounded);
        animator.SetFloat("VelocityY", physics.Velocity.y);
        animator.SetBool("IsCrouching", physics.IsCrouching);
        animator.SetInteger("State", (int)fsm.Current);
    }
}