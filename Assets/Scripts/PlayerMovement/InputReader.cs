using LabGame;
using UnityEngine;

public class InputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }   // x = влево/вправо, y = вперёд/назад
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool CrouchHeld { get; private set; }

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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v);

        JumpPressed = Input.GetButtonDown("Jump");
        JumpHeld = Input.GetButton("Jump");
        JumpReleased = Input.GetButtonUp("Jump");
        CrouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }
}
