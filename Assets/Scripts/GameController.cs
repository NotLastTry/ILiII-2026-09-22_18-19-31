using UnityEngine;

namespace Assets.Scripts
{
    public class GameController : MonoBehaviour
    {
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // Если открыт инвентарь — ESC закрывает его, а не выходит из игры.
            if (LabGame.UI.InventoryUI.Instance != null && LabGame.UI.InventoryUI.IsOpen) return;

            Application.Quit();
        }
    }
}
