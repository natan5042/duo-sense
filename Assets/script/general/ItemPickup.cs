using UnityEngine;

// À attacher sur le joueur (ou n'importe quel agent ramasseur)
public class ItemPickup : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode pickupKey = KeyCode.S;       // touche pour ramasser / lâcher / interagir
    public float pickupRadius = 1.0f;           // rayon de détection pour ramasser
    public float interactRadius = 1.2f;         // rayon de détection pour interagir

    [Header("Attach Point")]
    public Transform handPoint;                 // où l'objet sera attaché

    [Header("UI/Feedback (optionnel)")]
    public bool showGizmo = true;

    PickableItem heldItem;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItem != null)
            {
                DropHeldItem();
            }
            else
            {
                // Priorité au ramassage, sinon interaction (panneau, pont, etc.)
                if (!TryPickupNearest())
                {
                    TryInteractNearest();
                }
            }
        }

        // Optionnel: aligner l'objet porté sur la main à chaque frame
        if (heldItem != null && handPoint != null)
        {
            heldItem.transform.position = handPoint.position;
        }
    }

    bool TryPickupNearest()
    {
        // Cherche le plus proche PickableItem
        var item = FindNearestComponent<PickableItem>(pickupRadius);
        if (item == null) return false;

        Transform holder = handPoint != null ? handPoint : transform;
        item.OnPicked(holder);
        heldItem = item;
        return true;
    }

    void DropHeldItem()
    {
        if (heldItem == null) return;
        heldItem.OnDropped();
        heldItem = null;
    }

    bool TryInteractNearest()
    {
        var interactable = FindNearestComponent<IInteractable>(interactRadius);
        if (interactable == null)
        {
            Debug.Log("[ItemPickup] Aucun interactable trouvé dans le rayon");
            return false;
        }

        Debug.Log($"[ItemPickup] Interaction avec {interactable.GetType().Name}");
        interactable.Interact(this);
        return true;
    }

    T FindNearestComponent<T>(float radius) where T : class
    {
        T nearestComponent = null;
        float nearestDist = float.MaxValue;
        var hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var h in hits)
        {
            // Cherche l'interactable sur le collider, son parent ou ses enfants
            var comp = h.GetComponent<T>() ?? h.GetComponentInParent<T>() ?? h.GetComponentInChildren<T>();
            if (comp == null) continue;

            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearestComponent = comp;
            }
        }

        return nearestComponent;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
