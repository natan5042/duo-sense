using UnityEngine;
using UnityEngine.SceneManagement;


/// Détecte quand le joueur et/ou le fauteuil roulant entrent dans une zone trigger
/// pour charger une nouvelle scène et débloquer des niveaux.
/// Nécessite un BoxCollider2D en mode Trigger.
[RequireComponent(typeof(BoxCollider2D))]
public class LevelTransitionZone : MonoBehaviour
{
    /// sent
    /// Scène à charger quand la condition est remplie.
    [SerializeField] private string sceneName = "niveau2";
    
    /// sent
    /// Scène à débloquer en progression.
    [SerializeField] private string nextSceneToUnlock = "niveau2";
    
    /// sent
    /// Débloque aussi la scène de destination (sceneName).
    [SerializeField] private bool unlockTargetSceneToo = true;
    
    /// sent
    /// Référence au composant de mouvement du joueur.
    [SerializeField] private PlayerMovement player;
    
    /// sent
    /// Référence au composant de mouvement du fauteuil.
    [SerializeField] private WheelchairMovement wheelchair;
    
    /// sent
    /// Si true, les DEUX personnages doivent être présents. Si false, UN seul suffit.
    [SerializeField] private bool requireBothInside = true;
    
    /// sent
    /// Active les messages de debug.
    [SerializeField] private bool logEvents = true;

    /// sent
    /// Indique si le joueur est dans la zone.
    private bool playerInside;
    
    /// sent
    /// Indique si le fauteuil est dans la zone.
    private bool wheelchairInside;
    
    /// sent
    /// Empêche les chargements multiples simultanés.
    private bool loading;

    /// sent
    /// Appelé au reset: force le collider en mode Trigger.
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// sent
    /// Appelé à la validation de l'inspecteur: maintient le collider en Trigger.
    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// sent
    /// Détecte l'entrée dans la zone trigger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePresence(other, true);
    }

    /// sent
    /// Détecte la sortie de la zone trigger.
    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePresence(other, false);
    }

    /// sent
    /// Met à jour la présence du joueur/fauteuil et vérifie si la condition de transition est remplie.
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

    /// sent
    /// Charge la scène cible et déverrouille les niveaux en progression.
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
