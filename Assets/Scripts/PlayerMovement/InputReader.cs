using LabGame;
using LabGame.Controls;
using UnityEngine;

/// <summary>
/// Читает намерение игрока один раз за кадр и отдаёт его в удобном виде
/// остальным системам движения. Источник ввода (клавиатура, геймпад,
/// сенсорный джойстик) определяется в <see cref="InputRouter"/>.
/// </summary>
public class InputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }   // x = влево/вправо, y = вперёд/назад
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool CrouchHeld { get; private set; }

    /// <summary>Маршрутизатор ввода на этом персонаже.</summary>
    public InputRouter Router { get; private set; }

    private void Awake()
    {
        Router = GetComponent<InputRouter>();
        if (Router == null) Router = gameObject.AddComponent<InputRouter>();
    }

    public void Read()
    {
        // Пока открыт инвентарь, персонаж стоит на месте.
        if (GameplayInput.Locked)
        {
            MoveInput = Vector2.zero;
            JumpPressed = false;
            JumpHeld = false;
            JumpReleased = false;
            CrouchHeld = false;
            return;
        }

        var state = PlayerControls.State;

        MoveInput = state.Move;
        JumpPressed = state.JumpPressed;
        JumpHeld = state.JumpHeld;
        JumpReleased = state.JumpReleased;
        CrouchHeld = state.CrouchHeld;
    }
}
