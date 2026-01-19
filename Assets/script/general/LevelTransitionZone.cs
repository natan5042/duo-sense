using UnityEngine;
using UnityEngine.SceneManagement;

// Nécessite un collider 2D concret (ici BoxCollider2D) en mode Trigger pour détecter l'entrée
[RequireComponent(typeof(BoxCollider2D))]
public class LevelTransitionZone : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneName = "niveau2";
    [SerializeField] private string nextSceneToUnlock = "niveau2"; // Scène à débloquer quand on traverse
    [SerializeField] private bool unlockTargetSceneToo = true;     // Débloque aussi sceneName si coché

    [Header("Players")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private WheelchairMovement wheelchair;

    [Tooltip("True = charge la scène uniquement si les deux sont dans la zone.")]
    [SerializeField] private bool requireBothInside = true;

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;

    private bool playerInside;
    private bool wheelchairInside;
    private bool loading;

    private void Reset()
    {
        // Force le collider en mode Trigger pour créer la zone d'entrée
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Petit hint dans la console
        if (logEvents)
        {
            Debug.Log("[LevelTransitionZone] BoxCollider2D mis en Trigger. Assignez vos références Player/Wheelchair.");
        }
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePresence(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UpdatePresence(other, false);
    }

    private void UpdatePresence(Collider2D other, bool isInside)
    {
        // Identifie les persos par référence directe ou par leur composant
        var foundPlayer = other.GetComponentInParent<PlayerMovement>();
        if (foundPlayer != null && (player == null || foundPlayer == player))
        {
            player = foundPlayer;
            playerInside = isInside;
            if (logEvents)
            {
                Debug.Log($"[LevelTransitionZone] Player {(isInside ? "entre" : "sort")} du trigger.");
            }
        }

        var foundWheelchair = other.GetComponentInParent<WheelchairMovement>();
        if (foundWheelchair != null && (wheelchair == null || foundWheelchair == wheelchair))
        {
            wheelchair = foundWheelchair;
            wheelchairInside = isInside;
            if (logEvents)
            {
                Debug.Log($"[LevelTransitionZone] Fauteuil {(isInside ? "entre" : "sort")} du trigger.");
            }
        }

        bool playerExists = player != null && player.gameObject.activeInHierarchy;
        bool wheelchairExists = wheelchair != null && wheelchair.gameObject.activeInHierarchy;
        int availableCharacters = (playerExists ? 1 : 0) + (wheelchairExists ? 1 : 0);

        // Si un seul personnage est présent dans la scène, on valide uniquement sa présence dans la zone
        bool conditionMet;
        if (availableCharacters <= 1)
        {
            conditionMet = (playerExists && playerInside) || (wheelchairExists && wheelchairInside);
            if (logEvents)
            {
                var seul = playerExists ? "player" : "wheelchair";
                Debug.Log($"[LevelTransitionZone] Un seul personnage actif ({seul}). Passage validé sur sa présence.");
            }
        }
        else
        {
            conditionMet = requireBothInside
                ? playerInside && wheelchairInside
                : playerInside || wheelchairInside;
        }

        if (conditionMet)
        {
            LoadTargetScene();
        }
        else if (logEvents)
        {
            Debug.Log($"[LevelTransitionZone] Condition non remplie. playerInside={playerInside} wheelchairInside={wheelchairInside}");
        }
    }

    private void LoadTargetScene()
    {
        if (loading || string.IsNullOrWhiteSpace(sceneName)) return;
        loading = true;
        if (logEvents)
        {
            Debug.Log($"[LevelTransitionZone] Chargement de la scène '{sceneName}'.");
        }

        // Persiste la progression de niveaux
        if (!string.IsNullOrWhiteSpace(nextSceneToUnlock))
        {
            bool added = LevelProgressManager.Unlock(nextSceneToUnlock);
            if (logEvents && added)
            {
                Debug.Log($"[LevelTransitionZone] Déblocage de la scène suivante '{nextSceneToUnlock}'.");
            }
        }

        if (unlockTargetSceneToo && !string.IsNullOrWhiteSpace(sceneName))
        {
            bool addedTarget = LevelProgressManager.Unlock(sceneName);
            if (logEvents && addedTarget)
            {
                Debug.Log($"[LevelTransitionZone] Déblocage de la scène de destination '{sceneName}'.");
            }
        }

        SceneManager.LoadScene(sceneName);
    }
}
