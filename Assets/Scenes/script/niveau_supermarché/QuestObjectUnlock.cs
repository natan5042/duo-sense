using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// À attacher sur l'objet qui doit apparaître après avoir collecté certains items
public class QuestObjectUnlock : MonoBehaviour
{
    [Header("Configuration de la quête")]
    [Tooltip("Glisse-dépose les objets PickableItem à ramasser pour débloquer cet objet")]
    public List<PickableItem> requiredItems = new List<PickableItem>();
    
    [Tooltip("Ou utilise les IDs manuellement (si requiredItems est vide)")]
    public List<string> requiredItemIds = new List<string>();

    private List<string> effectiveRequiredIds = new List<string>();

    [Header("Références")]
    [Tooltip("Le joueur avec le script ItemPickup (sera trouvé automatiquement si non assigné)")]
    public ItemPickup playerPickup;

    [Header("Apparence")]
    [Tooltip("Objet visuel à activer/désactiver (si vide, utilise le GameObject principal)")]
    public GameObject visualObject;

    [Header("Feedback")]
    [Tooltip("Message à afficher quand la quête est complétée")]
    public string unlockMessage = "Vous avez trouvé tous les objets ! Une sortie apparaît...";
    
    [Tooltip("Son à jouer quand l'objet apparaît")]
    public AudioClip unlockSound;
    private AudioSource audioSource;

    private bool isUnlocked = false;

    void Start()
    {
        // Construire la liste effective des IDs requis
        effectiveRequiredIds.Clear();
        
        // Si on a des objets directement référencés, on utilise leurs IDs
        if (requiredItems != null && requiredItems.Count > 0)
        {
            foreach (var item in requiredItems)
            {
                if (item != null)
                {
                    effectiveRequiredIds.Add(item.itemId);
                }
            }
        }
        // Sinon on utilise la liste manuelle d'IDs
        else if (requiredItemIds != null && requiredItemIds.Count > 0)
        {
            effectiveRequiredIds.AddRange(requiredItemIds);
        }

        // Trouver le joueur automatiquement s'il n'est pas assigné
        if (playerPickup == null)
        {
            playerPickup = FindObjectOfType<ItemPickup>();
        }

        // Utiliser le GameObject principal si aucun objet visuel n'est spécifié
        if (visualObject == null)
        {
            visualObject = gameObject;
        }

        // Préparer l'audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && unlockSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Cacher l'objet au départ
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        // S'abonner à l'événement de collecte
        if (playerPickup != null)
        {
            playerPickup.onItemCollected += OnItemCollected;
        }

        Debug.Log($"QuestObjectUnlock: En attente de {effectiveRequiredIds.Count} objets pour débloquer.");
    }

    void OnDestroy()
    {
        // Se désabonner pour éviter les erreurs
        if (playerPickup != null)
        {
            playerPickup.onItemCollected -= OnItemCollected;
        }
    }

    // Appelé chaque fois qu'un objet est ramassé
    void OnItemCollected(PickableItem item)
    {
        if (isUnlocked) return; // Déjà débloqué, rien à faire

        CheckQuestCompletion();
    }

    // Vérifie si tous les objets requis ont été ramassés
    void CheckQuestCompletion()
    {
        if (isUnlocked) return;
        if (playerPickup == null) return;

        // Vérifier si tous les objets requis sont dans l'inventaire du joueur
        bool allCollected = true;
        foreach (string requiredId in effectiveRequiredIds)
        {
            if (!playerPickup.CollectedItems.Contains(requiredId))
            {
                allCollected = false;
                break;
            }
        }

        if (allCollected)
        {
            UnlockObject();
        }
    }

    // Débloque et fait apparaître l'objet
    void UnlockObject()
    {
        if (isUnlocked) return;

        isUnlocked = true;

        // Activer l'objet visuel
        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        // Jouer le son
        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        // Afficher le message
        if (!string.IsNullOrEmpty(unlockMessage))
        {
            Debug.Log(unlockMessage);
        }

        Debug.Log("QuestObjectUnlock: Objet débloqué !");
    }

    // Méthode publique pour forcer la vérification (utile pour le debug)
    public void ForceCheck()
    {
        CheckQuestCompletion();
    }

    // Méthode pour vérifier l'état de la quête (pour debug/UI)
    public string GetQuestStatus()
    {
        if (isUnlocked)
        {
            return "Quête terminée !";
        }

        if (playerPickup == null)
        {
            return "Erreur: Aucun joueur trouvé";
        }

        int collected = 0;
        foreach (string requiredId in effectiveRequiredIds)
        {
            if (playerPickup.CollectedItems.Contains(requiredId))
            {
                collected++;
            }
        }

        return $"Objets collectés: {collected}/{effectiveRequiredIds.Count}";
    }

    // Pour le debug dans l'éditeur
    void OnDrawGizmos()
    {
        if (isUnlocked)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
