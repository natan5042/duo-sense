using System.Collections.Generic;
using UnityEngine;

// À attacher sur le joueur (ou n'importe quel agent ramasseur)
public class ItemPickup : MonoBehaviour
{
    [Header("Interaction")] 
    public KeyCode pickupKey = KeyCode.E;       // touche pour ramasser / lâcher
    public float pickupRadius = 1.0f;           // rayon de détection
    public LayerMask pickableLayer;             // layer des objets ramassables

    [Header("Attach Point")] 
    public Transform handPoint;                 // où l'objet sera attaché

    [Header("UI/Feedback (optionnel)")] 
    public bool showGizmo = true;

    [Header("Mode Inventaire")] 
    [Tooltip("Si vrai, on collecte directement dans l'inventaire au lieu de tenir l'objet en main.")]
    public bool collectToInventory = true;
    [Tooltip("Si vrai, l'objet est simplement désactivé dans la scène après collecte (il n'est pas détruit)." )]
    public bool hideCollectedObject = true;

    [Header("Collecte")]
    [Tooltip("Historique simple des identifiants ramassés. Peut être utilisé par un inventaire plus tard." )]
    public List<string> collectedItemIds = new List<string>();

    public IReadOnlyList<string> CollectedItems => collectedItemIds; // accès en lecture seule pour l'extérieur

    public System.Action<PickableItem> onItemCollected;

    PickableItem heldItem;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItem == null)
            {
                TryPickupNearest();
            }
            else
            {
                DropHeldItem();
            }
        }

        // Optionnel: aligner l'objet porté sur la main à chaque frame
        if (heldItem != null && handPoint != null)
        {
            heldItem.transform.position = handPoint.position;
        }
    }

    void TryPickupNearest()
    {
        // Cherche le plus proche collider sur le layer ramassable
        Collider2D nearest = null;
        float nearestDist = float.MaxValue;
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickableLayer);
        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = h;
            }
        }

        if (nearest != null)
        {
            var item = nearest.GetComponent<PickableItem>();
            if (item != null)
            {
                Transform holder = handPoint != null ? handPoint : transform;
                bool attachToHolder = !collectToInventory; // si inventaire, on ne l'attache pas à la main

                item.OnPicked(holder, attachToHolder);

                // Stocke ce qui a été ramassé pour un futur inventaire/quête
                collectedItemIds.Add(item.itemId);
                onItemCollected?.Invoke(item);

                if (collectToInventory)
                {
                    // On ne garde rien en main, on range dans le "sac"
                    heldItem = null;
                    if (hideCollectedObject)
                    {
                        item.gameObject.SetActive(false); // l'objet disparaît de la scène mais reste en mémoire
                    }
                }
                else
                {
                    // Comportement précédent: porter l'objet
                    heldItem = item;
                }
            }
        }
    }

    void DropHeldItem()
    {
        if (heldItem == null) return;
        heldItem.OnDropped();
        heldItem = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
