using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Объект, с которым можно взаимодействовать клавишей E.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Мировая точка, по которой считается расстояние.</summary>
        Vector3 InteractionPoint { get; }

        /// <summary>Текст подсказки, который видит игрок.</summary>
        string GetPrompt(PlayerInventory player);

        bool CanInteract(PlayerInventory player);

        void Interact(PlayerInventory player);
    }
}
