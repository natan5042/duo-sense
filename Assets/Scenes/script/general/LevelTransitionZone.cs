using UnityEngine;
using UnityEngine.SceneManagement;


/// Détecte quand le joueur et/ou le fauteuil roulant entrent dans une zone trigger
/// pour charger une nouvelle scène et débloquer des niveaux.
/// Nécessite un BoxCollider2D en mode Trigger.
[RequireComponent(typeof(BoxCollider2D))]
public class LevelTransitionZone : MonoBehaviour
{
    /// Scène à charger quand la condition est remplie.
    [SerializeField] private string sceneName = "niveau2";
    
    /// Scène à débloquer en progression.
    [SerializeField] private string nextSceneToUnlock = "niveau2";
    
    /// Débloque aussi la scène de destination (sceneName).
    [SerializeField] private bool unlockTargetSceneToo = true;
    
    [SerializeField] private PlayerMovement player;
    [SerializeField] private WheelchairMovement wheelchair;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject wheelchairObject;
    [SerializeField] private bool requireBothInside = true;
    
    /// Active les messages de debug.
    [SerializeField] private bool logEvents = true;

    private bool playerInside;
    private bool wheelchairInside;
    private bool topDownPlayerInside;
    private bool topDownWheelchairInside;
    private bool customPlayerInside;
    private bool customWheelchairInside;
    private bool loading;

    /// Appelé au reset: force le collider en mode Trigger.
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }


    /// Appelé à la validation de l'inspecteur: maintient le collider en Trigger.
    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }


    /// Détecte l'entrée dans la zone trigger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePresence(other, true);
    }

    /// Détecte la sortie de la zone trigger.
    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePresence(other, false);
    }


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

        var foundWheelchair = other.GetComponentInParent<WheelchairMovement>();
        if (foundWheelchair != null && (wheelchair == null || foundWheelchair == wheelchair))
        {
            wheelchair = foundWheelchair;
            wheelchairInside = isInside;
            if (logEvents) Debug.Log($"[LevelTransitionZone] Fauteuil {(isInside ? "entre" : "sort")}.");
        }

        if (other.GetComponentInParent<TopDownMovement>() != null || other.GetComponentInParent<TopDownPlayer>() != null)
        {
            topDownPlayerInside = isInside;
            if (logEvents) Debug.Log($"[LevelTransitionZone] TopDown joueur {(isInside ? "entre" : "sort")}.");
        }
        if (other.GetComponentInParent<TopDownWheelchair>() != null)
        {
            topDownWheelchairInside = isInside;
            if (logEvents) Debug.Log($"[LevelTransitionZone] TopDown fauteuil {(isInside ? "entre" : "sort")}.");
        }
        if (playerObject != null && (other.transform == playerObject.transform || other.transform.IsChildOf(playerObject.transform) || playerObject.transform.IsChildOf(other.transform)))
        {
            customPlayerInside = isInside;
            if (logEvents) Debug.Log($"[LevelTransitionZone] Player object {(isInside ? "entre" : "sort")}.");
        }
        if (wheelchairObject != null && (other.transform == wheelchairObject.transform || other.transform.IsChildOf(wheelchairObject.transform) || wheelchairObject.transform.IsChildOf(other.transform)))
        {
            customWheelchairInside = isInside;
            if (logEvents) Debug.Log($"[LevelTransitionZone] Wheelchair object {(isInside ? "entre" : "sort")}.");
        }

        bool anyPlayer = playerInside || topDownPlayerInside || customPlayerInside;
        bool anyWheelchair = wheelchairInside || topDownWheelchairInside || customWheelchairInside;
        bool conditionMet = requireBothInside ? (anyPlayer && anyWheelchair) : (anyPlayer || anyWheelchair);

        // Lance le chargement si la condition est remplie
        if (conditionMet)
        {
            LoadTargetScene();
        }
    }

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
