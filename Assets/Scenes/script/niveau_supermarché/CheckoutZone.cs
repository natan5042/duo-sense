using UnityEngine;

/// <summary>
/// Zone de caisse : quand le joueur entre avec un produit en main, le produit est "scanné"
/// et posé sur le comptoir. Quand tous les produits du liste sont sur la caisse, les joueurs sont téléportés à la sortie (porte du magasin).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CheckoutZone : MonoBehaviour
{
    [Tooltip("Tag du joueur qui peut déposer les produits")]
    public string playerTag = "Player";

    [Header("Affichage sur le comptoir")]
    [Tooltip("Parent où poser les produits scannés. Créer un GameObject vide devant la caisse et l'assigner ici.")]
    public Transform counterDisplayRoot;

    [Header("Sortie du magasin")]
    [Tooltip("Point de téléportation quand la liste est terminée (ex: vide devant la porte du magasin, à l'extérieur).")]
    public Transform storeExitPoint;

    [Header("Optionnel")]
    public bool logScans = true;
    float teleportCheckCooldown;
    bool playersTeleported;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        var pickup = other.GetComponentInParent<ItemPickup>();
        if (pickup == null) return;
        string itemId = pickup.HeldItemId;
        if (string.IsNullOrEmpty(itemId)) return;
        if (SupermarketQuestManager.Instance == null) return;

        if (counterDisplayRoot == null && logScans)
            Debug.LogWarning("[CheckoutZone] Counter Display Root non assigné : les produits seront supprimés au lieu d'être affichés sur la caisse.");

        SupermarketQuestManager.Instance.ProductScanned(itemId, pickup, counterDisplayRoot);
        if (logScans) Debug.Log($"[CheckoutZone] Produit scanné: {itemId}");

        TryTeleportPlayersToExit();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (playersTeleported) return;
        if (!other.CompareTag(playerTag)) return;
        teleportCheckCooldown -= Time.deltaTime;
        if (teleportCheckCooldown <= 0f)
        {
            teleportCheckCooldown = 0.5f;
            TryTeleportPlayersToExit();
        }
    }

    void TryTeleportPlayersToExit()
    {
        if (playersTeleported) return;
        if (SupermarketQuestManager.Instance == null || !SupermarketQuestManager.Instance.IsShoppingQuestComplete()) return;
        if (storeExitPoint == null)
        {
            if (logScans) Debug.LogWarning("[CheckoutZone] Store Exit Point non assigné : pas de téléportation à la sortie.");
            return;
        }
        playersTeleported = true;
        var players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (var go in players)
        {
            if (go == null) continue;
            var root = go.transform.root;
            root.position = storeExitPoint.position;
            var rb = root.GetComponentInChildren<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        }
        if (logScans) Debug.Log("[CheckoutZone] Liste terminée : joueur(s) téléporté(s) à la sortie du magasin.");
    }
}
