using UnityEngine;
using GameQuests;
using System.Collections.Generic;

/// <summary>
/// Gère les quêtes du niveau supermarché : planche + trampoline, liste de courses (avec quantités et validation à la caisse).
/// À placer sur un GameObject persistant (ex: vide "SupermarketQuestManager") dans la scène quete_croquettes.
/// </summary>
public class SupermarketQuestManager : MonoBehaviour
{
    public static SupermarketQuestManager Instance { get; private set; }

    [Header("IDs des objets (doivent correspondre aux PickableItem dans la scène)")]
    [Tooltip("ID du PickableItem pour la planche")]
    public string boardItemId = "planche";
    [Tooltip("ID du PickableItem pour le trampoline")]
    public string rampItemId = "trampoline";
    [Tooltip("ID du PickableItem pour la liste de courses")]
    public string shoppingListItemId = "liste_courses";

    [Header("Liste de courses (ordre affiché)")]
    [Tooltip("Nom affiché + quantité. itemId doit correspondre aux produits en scène.")]
    public List<ShopItemEntry> shoppingList = new List<ShopItemEntry>();

    [Header("Références")]
    [Tooltip("Sources de ramassage (joueurs). Si vide, trouve tous les ItemPickup.")]
    public List<ItemPickup> pickupSources = new List<ItemPickup>();

    [Header("Spawn pour la fosse (si le joueur tombe sans planche)")]
    public Transform wheelchairRespawnPoint;

    [Header("Affichage sur la caisse")]
    [Tooltip("Espacement horizontal entre les produits posés sur le comptoir")]
    public float counterItemSpacing = 0.35f;
    [Tooltip("Échelle des produits sur le comptoir (réduire si trop gros, ex: 0.15)")]
    public float counterItemScale = 0.15f;

    private Quest obstacleQuest;
    private Quest shoppingQuest;
    private int boardStepIndex = 0;
    private int rampStepIndex = 1;
    private Dictionary<string, int> itemIdToStepIndex = new Dictionary<string, int>();
    private bool shoppingQuestAdded;

    [System.Serializable]
    public class ShopItemEntry
    {
        public string itemId = "";
        public string displayName = "";
        public int requiredCount = 1;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (QuestSystem.Instance == null)
        {
            var go = new GameObject("QuestSystem");
            go.AddComponent<GameQuests.QuestSystem>();
        }
        CreateObstacleQuest();
    }

    void Start()
    {
        if (pickupSources == null || pickupSources.Count == 0)
        {
            pickupSources = new List<ItemPickup>(FindObjectsByType<ItemPickup>(FindObjectsSortMode.None));
        }
        if (pickupSources.Count == 0)
        {
            Debug.LogWarning("SupermarketQuestManager: Aucun ItemPickup trouvé.");
        }
        SubscribeToPickups();
    }

    void OnDestroy()
    {
        UnsubscribeFromPickups();
        if (Instance == this) Instance = null;
    }

    private void CreateObstacleQuest()
    {
        if (QuestSystem.Instance == null) return;
        obstacleQuest = new Quest("Traverser l'obstacle");
        obstacleQuest.AddStep("Prendre la planche", "");
        obstacleQuest.AddStep("Prendre le trampoline", "");
        QuestSystem.Instance.AddQuest(obstacleQuest);
    }

    private void SubscribeToPickups()
    {
        foreach (var p in pickupSources)
        {
            if (p != null)
            {
                p.onItemCollected += OnItemCollected;
            }
        }
    }

    private void UnsubscribeFromPickups()
    {
        foreach (var p in pickupSources)
        {
            if (p != null)
            {
                p.onItemCollected -= OnItemCollected;
            }
        }
    }

    private void OnItemCollected(PickableItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId)) return;
        if (QuestSystem.Instance == null || QuestSystem.Instance.activeQuests == null) return;

        var q = QuestSystem.Instance.activeQuests.Find(x => x != null && x.title == "Traverser l'obstacle");
        if (q != null)
        {
            if (item.itemId.Trim().Equals(boardItemId, System.StringComparison.OrdinalIgnoreCase))
                q.CompleteStep(boardStepIndex);
            else if (item.itemId.Trim().Equals(rampItemId, System.StringComparison.OrdinalIgnoreCase))
                q.CompleteStep(rampStepIndex);
        }

        if (item.itemId == shoppingListItemId && !shoppingQuestAdded)
        {
            AddShoppingListQuest();
            shoppingQuestAdded = true;
        }
    }

    /// <summary> Appelé quand le joueur ouvre le panneau liste (LevelDisplayInteraction ou autre). Ajoute la quête "Acheter les produits" une seule fois. </summary>
    public void EnsureShoppingQuestAdded()
    {
        if (shoppingQuestAdded) return;
        shoppingQuestAdded = true;
        AddShoppingListQuest();
    }

    private void AddShoppingListQuest()
    {
        if (QuestSystem.Instance == null) return;
        if (shoppingList == null || shoppingList.Count == 0)
        {
            DefaultShoppingList();
        }

        shoppingQuest = new Quest("Acheter les produits");
        itemIdToStepIndex.Clear();
        for (int i = 0; i < shoppingList.Count; i++)
        {
            var entry = shoppingList[i];
            if (string.IsNullOrWhiteSpace(entry.itemId)) continue;
            string name = string.IsNullOrWhiteSpace(entry.displayName) ? ToTitleCase(entry.itemId) : entry.displayName;
            int count = entry.requiredCount > 0 ? entry.requiredCount : 1;
            shoppingQuest.AddStepWithCount(name + (count > 1 ? " x" + count : ""), count, "");
            itemIdToStepIndex[entry.itemId.ToLowerInvariant()] = shoppingQuest.steps.Count - 1;
        }
        QuestSystem.Instance.AddQuest(shoppingQuest);
        Debug.Log("SupermarketQuestManager: Liste de courses ajoutée aux quêtes.");
    }

    private void DefaultShoppingList()
    {
        shoppingList = new List<ShopItemEntry>
        {
            new ShopItemEntry { itemId = "lait", displayName = "Lait", requiredCount = 2 },
            new ShopItemEntry { itemId = "soupe", displayName = "Soupe", requiredCount = 1 },
            new ShopItemEntry { itemId = "croquette", displayName = "Croquette", requiredCount = 1 },
            new ShopItemEntry { itemId = "eau", displayName = "Eau", requiredCount = 3 },
            new ShopItemEntry { itemId = "spaghetti", displayName = "Spaghetti", requiredCount = 1 }
        };
    }

    private static string ToTitleCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }

    /// <summary> Appelé par la zone caisse quand un produit est passé. Incrémente le compteur et place l'objet sur la caisse ou le détruit. </summary>
    /// <param name="placeOnCounter">Si non null, l'objet est déplacé sous ce Transform (visible sur la caisse) au lieu d'être détruit.</param>
    public void ProductScanned(string itemId, ItemPickup holder, Transform placeOnCounter = null)
    {
        if (shoppingQuest == null || string.IsNullOrEmpty(itemId)) return;

        string key = itemId.ToLowerInvariant();
        if (!itemIdToStepIndex.TryGetValue(key, out int stepIndex)) return;

        shoppingQuest.IncrementStepCount(stepIndex, 1);
        if (holder != null && holder.HeldItem != null && holder.HeldItem.itemId == itemId)
        {
            var obj = holder.HeldItem.gameObject;
            var pickable = holder.HeldItem;
            holder.DropHeldItem();

            if (placeOnCounter != null)
            {
                PlaceItemOnCounter(obj, pickable, placeOnCounter, null);
            }
            else
            {
                Destroy(obj);
            }
        }
    }

    /// <summary> Vérifie si la quête liste est terminée. </summary>
    public bool IsShoppingQuestComplete()
    {
        return shoppingQuest != null && shoppingQuest.IsCompleted();
    }

    private void PlaceItemOnCounter(GameObject obj, PickableItem pickable, Transform counterRoot, int? slotOverride = null)
    {
        int slot = slotOverride ?? counterRoot.childCount;
        obj.transform.SetParent(counterRoot);
        obj.transform.localPosition = new Vector3(slot * counterItemSpacing, 0f, 0f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = new Vector3(counterItemScale, counterItemScale, counterItemScale);

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        foreach (var c in obj.GetComponentsInChildren<Collider2D>(true))
            if (c != null) c.enabled = false;
        if (pickable != null) pickable.enabled = false;
    }
}
