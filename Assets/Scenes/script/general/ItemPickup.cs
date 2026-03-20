using System;
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

    [Header("--- MODES DE FONCTIONNEMENT ---")]
    [Tooltip("SCRIPT 1 : L'objet va directement dans l'inventaire et disparaît de la scène.")]
    public bool useInventoryMode = true;

    [Tooltip("SCRIPT 2 : Le joueur prend l'objet physiquement en main, le garde et peut le lâcher.")]
    public bool useHoldMode = false;

    [Header("Options Mode Inventaire (Script 1)")]
    [Tooltip("Si vrai, en mode inventaire, l'objet est juste désactivé (pas détruit).")]
    public bool hideCollectedObject = true;

    [Header("Données Collecte")]
    public List<string> collectedItemIds = new List<string>();
    public List<string> CollectedItems => collectedItemIds;

    // Événement déclenché quand un objet est collecté
    public event Action<PickableItem> onItemCollected;

    // --- PROPRIÉTÉS SCRIPT 2 ---
    PickableItem heldItem;
    public PickableItem HeldItem => heldItem;
    public string HeldItemId => heldItem != null ? heldItem.itemId : null;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItem == null)
            {
                TryPickupNearest();
            }
            else if (useHoldMode)
            {
                // On lâche l'objet seulement si le mode "Tenir en main" est activé
                DropHeldItem();
            }
        }

        // Aligner l'objet porté sur la main à chaque frame si on est en mode "Hold"
        if (useHoldMode && heldItem != null && handPoint != null)
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

                // On attache physiquement l'objet seulement si on est en mode "Hold"
                bool attachToHolder = useHoldMode;
                item.OnPicked(holder, attachToHolder);

                // Ajout de l'ID à l'historique
                if (!string.IsNullOrEmpty(item.itemId))
                {
                    collectedItemIds.Add(item.itemId);
                }

                onItemCollected?.Invoke(item);

                // --- LOGIQUE SCRIPT 1 (INVENTAIRE) ---
                if (useInventoryMode)
                {
                    if (hideCollectedObject)
                    {
                        item.gameObject.SetActive(false);
                    }
                }

                // --- LOGIQUE SCRIPT 2 (TENIR EN MAIN) ---
                if (useHoldMode)
                {
                    heldItem = item;
                }
                else
                {
                    heldItem = null;
                }
            }
        }
    }

    public void DropHeldItem()
    {
        if (heldItem == null) return;
        heldItem.OnDropped();
        heldItem = null;
    }

    // Permet d'enregistrer "manuellement" un objet collecté depuis un autre script
    public void NotifyItemCollected(PickableItem item)
    {
        if (item == null) return;
        if (!string.IsNullOrEmpty(item.itemId))
        {
            collectedItemIds.Add(item.itemId);
        }
        onItemCollected?.Invoke(item);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}