using UnityEngine;

/// <summary>
/// Quand le Canvas "liste de courses" s'ouvre (ex: via LevelDisplayInteraction), ajoute la quête "Acheter les produits".
/// Mettre sur le même objet que LevelDisplayInteraction (ou sur le Canvas liste) et assigner le Canvas qui affiche la liste.
/// </summary>
public class ShoppingListQuestTrigger : MonoBehaviour
{
    [Tooltip("Canvas du panneau liste de courses (celui ouvert par LevelDisplayInteraction). Si vide, prend le Canvas du même objet.")]
    public Canvas listCanvas;

    private bool questAdded;

    void Start()
    {
        if (listCanvas == null)
            listCanvas = GetComponent<Canvas>();
    }

    void Update()
    {
        if (questAdded) return;
        if (listCanvas == null) return;
        if (!listCanvas.enabled) return;

        if (SupermarketQuestManager.Instance != null)
        {
            SupermarketQuestManager.Instance.EnsureShoppingQuestAdded();
            questAdded = true;
        }
    }
}
