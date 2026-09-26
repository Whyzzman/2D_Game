namespace ForgottenKingdom
{
    /// <summary>
    /// Anything the player can activate with the Interact action: altars now, and later doors,
    /// chests, levers and NPCs.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract { get; }

        /// <summary>Short line shown while the player stands in range.</summary>
        string Prompt { get; }

        void Interact(PlayerCharacter player);
    }
}
