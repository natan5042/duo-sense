using System.Collections.Generic;
using UnityEngine;

// À attacher sur l'objet qui doit apparaître après avoir collecté certains items
public class QuestObjectUnlock : MonoBehaviour
{
    [Header("Configuration de la quête")]
    [Tooltip("Glisse-dépose les objets PickableItem à ramasser pour débloquer cet objet")]
    public List<PickableItem> requiredItems = new List<PickableItem>();
    
    [Tooltip("Ou utilise les IDs manuellement (si requiredItems est vide)")]
    public List<string> requiredItemIds = new List<string>();

    [Tooltip("Nombre d'objets à ramasser si aucune liste d'IDs n'est fournie")]
    public int requiredItemCount = 0;

    private List<string> effectiveRequiredIds = new List<string>();
    private Dictionary<string, int> requiredIdCounts = new Dictionary<string, int>();

    // Debug aide pour tracer l'état
    [Header("Debug")]
    [Tooltip("Affiche des logs détaillés lors des collectes")]
    public bool verboseLogs = false;

    [Header("Références")]
    [Tooltip("Si vide, tous les ItemPickup de la scène seront trouvés. Ajoute ici les joueurs ramasseurs.")]
    public List<ItemPickup> pickupSources = new List<ItemPickup>();
    // Compatibilité ancienne version (champ Unity obsolète mais conservé si présent dans la scène)
    [SerializeField] ItemPickup playerPickupLegacy;

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
        requiredIdCounts.Clear();
        
        // Si on a des objets directement référencés, on utilise leurs IDs
        if (requiredItems != null && requiredItems.Count > 0)
        {
            foreach (var item in requiredItems)
            {
                if (item != null)
                {
                    if (string.IsNullOrWhiteSpace(item.itemId))
                    {
                        Debug.LogWarning($"QuestObjectUnlock: Un PickableItem référencé n'a pas d'itemId (objet: {item.name}). Il sera ignoré.");
                        continue;
                    }

                    effectiveRequiredIds.Add(item.itemId);
                    if (!requiredIdCounts.ContainsKey(item.itemId))
                    {
                        requiredIdCounts[item.itemId] = 0;
                    }
                    requiredIdCounts[item.itemId]++;
                }
            }
        }
        // Sinon on utilise la liste manuelle d'IDs
        else if (requiredItemIds != null && requiredItemIds.Count > 0)
        {
            foreach (var id in requiredItemIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning("QuestObjectUnlock: Une entrée vide dans Required Item Ids est ignorée.");
                    continue;
                }

                effectiveRequiredIds.Add(id);
                if (!requiredIdCounts.ContainsKey(id))
                {
                    requiredIdCounts[id] = 0;
                }
                requiredIdCounts[id]++;
            }
        }

        // Si aucune ID fournie, on utilisera un simple comptage
        if (effectiveRequiredIds.Count == 0 && requiredItemCount > 0)
        {
            Debug.Log($"QuestObjectUnlock: Mode compteur activé, besoin de {requiredItemCount} objets.");
        }
        else if (effectiveRequiredIds.Count == 0 && requiredItemCount <= 0)
        {
            Debug.LogWarning("QuestObjectUnlock: Aucun item requis n'est configuré (listes vides et compteur à 0). L'objet ne sera pas débloqué.");
        }
        else if (effectiveRequiredIds.Count > 0)
        {
            if (verboseLogs)
            {
                var parts = new List<string>();
                foreach (var kvp in requiredIdCounts)
                {
                    parts.Add($"{kvp.Key} x{kvp.Value}");
                }
                Debug.Log($"QuestObjectUnlock: Mode IDs activé, besoin de: {string.Join(", ", parts)}");
            }
        }

        // Trouver les ramasseurs automatiquement s'ils ne sont pas assignés
        if (pickupSources == null)
        {
            pickupSources = new List<ItemPickup>();
        }
        // Nettoyer les nulls éventuellement laissés dans l'inspecteur
        pickupSources.RemoveAll(p => p == null);

        // Ajouter l'ancien champ s'il est encore renseigné dans la scène
        if (playerPickupLegacy != null && !pickupSources.Contains(playerPickupLegacy))
        {
            pickupSources.Add(playerPickupLegacy);
        }

        if (pickupSources.Count == 0)
        {
            pickupSources.AddRange(FindObjectsOfType<ItemPickup>());
        }

        if (pickupSources.Count == 0)
        {
            Debug.LogWarning("QuestObjectUnlock: Aucun ItemPickup trouvé. Aucun événement de collecte ne sera reçu.");
        }
        else if (verboseLogs)
        {
            Debug.Log($"QuestObjectUnlock: {pickupSources.Count} source(s) de collecte détectée(s).");
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

        // S'abonner à l'événement de collecte pour toutes les sources
        foreach (var picker in pickupSources)
        {
            if (picker != null)
            {
                picker.onItemCollected += OnItemCollected;
            }
        }

        Debug.Log($"QuestObjectUnlock: En attente de {effectiveRequiredIds.Count} objets pour débloquer.");
    }

    void OnDestroy()
    {
        // Se désabonner pour éviter les erreurs
        if (pickupSources != null)
        {
            foreach (var picker in pickupSources)
            {
                if (picker != null)
                {
                    picker.onItemCollected -= OnItemCollected;
                }
            }
        }
    }

    // Appelé chaque fois qu'un objet est ramassé
    void OnItemCollected(PickableItem item)
    {
        if (isUnlocked) return; // Déjà débloqué, rien à faire

        if (verboseLogs && item != null)
        {
            Debug.Log($"QuestObjectUnlock: Item collecté -> {item.itemId} (obj: {item.name})");
        }

        CheckQuestCompletion();
    }

    // Vérifie si tous les objets requis ont été ramassés
    void CheckQuestCompletion()
    {
        if (isUnlocked) return;
        if (pickupSources == null || pickupSources.Count == 0) return;

        if (effectiveRequiredIds.Count > 0)
        {
            // Vérifier si tous les objets requis (avec quantités) sont dans l'inventaire du joueur
            var inventoryCounts = BuildInventoryCounts();

            bool allCollected = true;
            foreach (var kvp in requiredIdCounts)
            {
                var requiredId = kvp.Key;
                var requiredCount = kvp.Value;
                inventoryCounts.TryGetValue(requiredId, out int haveCount);
                if (haveCount < requiredCount)
                {
                    if (verboseLogs)
                    {
                        Debug.Log($"QuestObjectUnlock: Manque {requiredId} ({haveCount}/{requiredCount})");
                    }
                    allCollected = false;
                    break;
                }
            }

            if (allCollected)
            {
                UnlockObject();
            }
        }
        else if (requiredItemCount > 0)
        {
            // Mode compteur simple: débloque une fois X objets ramassés (peu importe lesquels)
            int totalCollected = 0;
            var inventoryCounts = BuildInventoryCounts();
            foreach (var kvp in inventoryCounts)
            {
                totalCollected += kvp.Value;
            }

            if (totalCollected >= requiredItemCount)
            {
                UnlockObject();
            }
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

        if (pickupSources == null || pickupSources.Count == 0)
        {
            return "Erreur: Aucun ItemPickup trouvé";
        }

        if (effectiveRequiredIds.Count > 0)
        {
            int collectedSlots = 0;
            int requiredSlots = effectiveRequiredIds.Count;
            var inventoryCounts = BuildInventoryCounts();
            foreach (var kvp in requiredIdCounts)
            {
                var id = kvp.Key;
                var need = kvp.Value;
                inventoryCounts.TryGetValue(id, out int have);
                collectedSlots += Mathf.Min(need, have);
            }
            return $"Objets collectés: {collectedSlots}/{requiredSlots}";
        }

        if (requiredItemCount > 0)
        {
            int collectedCount = 0;
            var inventoryCounts = BuildInventoryCounts();
            foreach (var kvp in inventoryCounts)
            {
                collectedCount += kvp.Value;
            }
            return $"Objets collectés: {collectedCount}/{requiredItemCount}";
        }

        return "Aucun objectif configuré";
    }

    Dictionary<string, int> BuildInventoryCounts()
    {
        var inventoryCounts = new Dictionary<string, int>();
        foreach (var picker in pickupSources)
        {
            if (picker == null) continue;
            foreach (var id in picker.CollectedItems)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!inventoryCounts.ContainsKey(id)) inventoryCounts[id] = 0;
                inventoryCounts[id]++;
            }
        }
        return inventoryCounts;
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
