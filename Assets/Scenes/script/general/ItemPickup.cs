using UnityEngine;
using System;
using System.Collections.Generic;

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

    PickableItem heldItem;
    
    // Événement déclenché quand un objet est collecté (pour QuestObjectUnlock)
    public event Action<PickableItem> onItemCollected;
    
    // Liste des IDs des objets collectés
    private List<string> collectedItems = new List<string>();
    public List<string> CollectedItems => collectedItems;

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
                item.OnPicked(holder);
                heldItem = item;
                
                // Ajoute l'itemId à la liste des objets collectés
                if (!string.IsNullOrEmpty(item.itemId))
                {
                    collectedItems.Add(item.itemId);
                }
                
                // Déclenche l'événement de collecte
                onItemCollected?.Invoke(item);
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
