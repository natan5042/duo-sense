using UnityEngine;

// Interface simple pour tous les objets interactifs
// Utilise `PlayerInteraction` comme paramètre d'interaction centralisé
public interface IInteractable
{
    void Interact(PlayerInteraction player);
}
