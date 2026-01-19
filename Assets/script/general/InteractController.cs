using UnityEngine;

// À mettre sur le joueur pour déclencher les IInteractable proches avec une touche (ex: S).
public class InteractController : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.S;
    public float interactRadius = 1.5f;
    public LayerMask interactLayers; // layers des objets interactifs (mettre Default si besoin)
    public bool debugLog = false;

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayers);
        if (hits == null || hits.Length == 0)
        {
            if (debugLog) Debug.Log("[InteractController] Aucun interactable dans le rayon.");
            return;
        }

        Collider2D nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = h;
            }
        }

        if (nearest == null) return;

        var interactable = nearest.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            if (debugLog) Debug.LogWarning("[InteractController] Collider trouvé mais pas d'IInteractable sur l'objet: " + nearest.name);
            return;
        }

        var itemPickup = GetComponent<ItemPickup>();
        interactable.Interact(itemPickup);
        if (debugLog) Debug.Log("[InteractController] Interact avec " + nearest.name);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
