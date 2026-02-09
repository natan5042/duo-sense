using UnityEngine;

// Standalone: sur l'objet ramassable. Quand le joueur appuie sur E près de l'objet, il disparaît
// et on fait apparaître une version de remplacement à l'endroit choisi (avec un des deux sprites).
[RequireComponent(typeof(Collider2D))]
public class ReplaceOnPickup : MonoBehaviour
{
    [Header("Détection joueur")]
    public bool requirePlayerTag = true;
    public string playerTag = "Player";

    [Header("Interaction")]
    public KeyCode pickupKey = KeyCode.E;
    public bool autoPickupOnEnter = false;

    [Header("Destination")]
    public Transform spawnPoint; // où faire apparaître le remplacement

    [Header("Remplacement")]
    public GameObject replacementPrefab; // optionnel, sinon un simple SpriteRenderer sera créé
    public GameObject existingReplacement; // si renseigné, on active/déplace cet objet déjà présent dans la scène
    public bool hideExistingReplacementOnStart = true;
    public Sprite spriteVariant1;
    public Sprite spriteVariant2;
    public bool useSecondSprite = false; // choisis le sprite voulu

    bool playerInTrigger;
    Collider2D lastPlayerCollider;

    void Start()
    {
        // Si un remplacement existe déjà dans la scène, on peut le cacher jusqu'au pickup
        if (existingReplacement != null && hideExistingReplacementOnStart)
        {
            existingReplacement.SetActive(false);
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInTrigger || autoPickupOnEnter) return;
        if (Input.GetKeyDown(pickupKey)) TriggerReplace();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;
        lastPlayerCollider = other;
        playerInTrigger = true;
        if (autoPickupOnEnter) TriggerReplace();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;
        playerInTrigger = false;
    }

    void TriggerReplace()
    {
        if (spawnPoint == null) return;

        // Crée le remplacement
        GameObject newObj;
        if (existingReplacement != null)
        {
            newObj = existingReplacement;
            newObj.transform.position = spawnPoint.position;
            newObj.transform.rotation = spawnPoint.rotation;
            newObj.SetActive(true);
        }
        else if (replacementPrefab != null)
        {
            newObj = Instantiate(replacementPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            newObj = new GameObject("ReplacementSprite");
            newObj.transform.position = spawnPoint.position;
            newObj.transform.rotation = spawnPoint.rotation;
            var sr = newObj.AddComponent<SpriteRenderer>();
            sr.sprite = useSecondSprite ? spriteVariant2 : spriteVariant1;
        }

        // Notifier les quêtes (ItemPickup.onItemCollected) si cet objet a un PickableItem
        var pickable = GetComponent<PickableItem>();
        if (pickable != null && lastPlayerCollider != null)
        {
            var picker = lastPlayerCollider.GetComponentInParent<ItemPickup>();
            if (picker != null)
                picker.NotifyItemCollected(pickable);
        }

        // Cache cet objet d'origine
        gameObject.SetActive(false);
        playerInTrigger = false;
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, spawnPoint.position);
        Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.2f);
    }
}
