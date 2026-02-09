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
    
    /// <summary> Objet actuellement tenu (pour la caisse / quêtes). </summary>
    public PickableItem HeldItem => heldItem;
    /// <summary> ID de l'objet tenu, ou null si rien. </summary>
    public string HeldItemId => heldItem != null ? heldItem.itemId : null;
    
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

    public void DropHeldItem()
    {
        if (heldItem == null) return;
        heldItem.OnDropped();
        heldItem = null;
    }

    /// <summary> Appelé par ReplaceOnPickup (ou autre) quand un objet est "ramassé" sans passer par TryPickupNearest — déclenche onItemCollected. </summary>
    public void NotifyItemCollected(PickableItem item)
    {
        if (item == null) return;
        if (!string.IsNullOrEmpty(item.itemId))
            collectedItems.Add(item.itemId);
        onItemCollected?.Invoke(item);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
