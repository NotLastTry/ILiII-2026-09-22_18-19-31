public enum CharacterState
{
    Idle, Run, Jump, Fall, Land, Crouch
}

public class StateMachine
{
    public CharacterState Current { get; private set; } = CharacterState.Idle;
    public float StateTimer { get; private set; }

    public void ChangeState(CharacterState next)
    {
        if (Current == next) return;
        Current = next;
        StateTimer = 0f;
    }

    public void Tick(float dt) => StateTimer += dt;
}