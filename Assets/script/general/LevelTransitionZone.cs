using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Détecte quand le joueur et/ou le fauteuil roulant entrent dans une zone trigger
/// pour charger une nouvelle scène et débloquer des niveaux.
/// Nécessite un BoxCollider2D en mode Trigger.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelTransitionZone : MonoBehaviour
{
    /// <summary>Scène à charger quand la condition est remplie.</summary>
    [SerializeField] private string sceneName = "niveau2";
    
    /// <summary>Scène à débloquer en progression.</summary>
    [SerializeField] private string nextSceneToUnlock = "niveau2";
    
    /// <summary>Débloque aussi la scène de destination (sceneName).</summary>
    [SerializeField] private bool unlockTargetSceneToo = true;
    
    /// <summary>Référence au composant de mouvement du joueur.</summary>
    [SerializeField] private PlayerMovement player;
    
    /// <summary>Référence au composant de mouvement du fauteuil.</summary>
    [SerializeField] private WheelchairMovement wheelchair;
    
    /// <summary>Si true, les DEUX personnages doivent être présents. Si false, UN seul suffit.</summary>
    [SerializeField] private bool requireBothInside = true;
    
    /// <summary>Active les messages de debug.</summary>
    [SerializeField] private bool logEvents = true;

    /// <summary>Indique si le joueur est dans la zone.</summary>
    private bool playerInside;
    
    /// <summary>Indique si le fauteuil est dans la zone.</summary>
    private bool wheelchairInside;
    
    /// <summary>Empêche les chargements multiples simultanés.</summary>
    private bool loading;

    /// <summary>Appelé au reset: force le collider en mode Trigger.</summary>
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>Appelé à la validation de l'inspecteur: maintient le collider en Trigger.</summary>
    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>Détecte l'entrée dans la zone trigger.</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePresence(other, true);
    }

    /// <summary>Détecte la sortie de la zone trigger.</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePresence(other, false);
    }

    /// <summary>
    /// Met à jour la présence du joueur/fauteuil et vérifie si la condition de transition est remplie.
    /// </summary>
    private void UpdatePresence(Collider2D other, bool isInside)
    {
        // Vérifie si c'est le joueur
        var foundPlayer = other.GetComponentInParent<PlayerMovement>();
        if (foundPlayer != null && (player == null || foundPlayer == player))
        {
            player = foundPlayer;
            playerInside = isInside;
            if (logEvents)
            {
                Debug.Log($"[LevelTransitionZone] Player {(isInside ? "entre" : "sort")}.");
            }
        }

        // Vérifie si c'est le fauteuil
        var foundWheelchair = other.GetComponentInParent<WheelchairMovement>();
        if (foundWheelchair != null && (wheelchair == null || foundWheelchair == wheelchair))
        {
            wheelchair = foundWheelchair;
            wheelchairInside = isInside;
            if (logEvents)
            {
                Debug.Log($"[LevelTransitionZone] Fauteuil {(isInside ? "entre" : "sort")}.");
            }
        }

        // Compte les personnages actifs
        bool playerExists = player != null && player.gameObject.activeInHierarchy;
        bool wheelchairExists = wheelchair != null && wheelchair.gameObject.activeInHierarchy;

        // Détermine si la condition de transition est remplie
        bool conditionMet = (playerExists && wheelchairExists)
            ? (requireBothInside ? playerInside && wheelchairInside : playerInside || wheelchairInside)
            : (playerExists && playerInside) || (wheelchairExists && wheelchairInside);

        // Lance le chargement si la condition est remplie
        if (conditionMet)
        {
            LoadTargetScene();
        }
    }

    /// <summary>
    /// Charge la scène cible et déverrouille les niveaux en progression.
    /// </summary>
    private void LoadTargetScene()
    {
        // Empêche les chargements multiples
        if (loading || string.IsNullOrWhiteSpace(sceneName))
            return;

        loading = true;

        // Déverrouille la scène suivante
        if (!string.IsNullOrWhiteSpace(nextSceneToUnlock))
        {
            LevelProgressManager.Unlock(nextSceneToUnlock);
        }

        // Déverrouille aussi la scène de destination
        if (unlockTargetSceneToo)
        {
            LevelProgressManager.Unlock(sceneName);
        }

        // Charge la scène
        SceneManager.LoadScene(sceneName);
    }
}
