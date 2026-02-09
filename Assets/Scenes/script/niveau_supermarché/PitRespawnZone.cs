using UnityEngine;

/// <summary>
/// Zone de fosse : si le fauteuil (ou le joueur) entre dedans, il est téléporté au point de respawn pour éviter de rester bloqué.
/// Résout le problème "prendre seulement le trampoline, sauter et tomber dans la fosse".
/// Mettre un BoxCollider2D (Trigger) dans la fosse.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PitRespawnZone : MonoBehaviour
{
    [Tooltip("Tag des objets à téléporter (ex: Player pour le fauteuil)")]
    public string respawnTag = "Player";

    [Tooltip("Point de respawn (si vide, cherche SupermarketQuestManager.wheelchairRespawnPoint)")]
    public Transform respawnPoint;

    [Header("Optionnel")]
    public bool logRespawns = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(respawnTag)) return;

        Transform point = respawnPoint;
        if (point == null && SupermarketQuestManager.Instance != null)
        {
            point = SupermarketQuestManager.Instance.wheelchairRespawnPoint;
        }
        if (point == null)
        {
            Debug.LogWarning("PitRespawnZone: Aucun point de respawn assigné.");
            return;
        }

        // Déplacer la racine du personnage (Rigidbody2D), pas le collider enfant
        Rigidbody2D rb = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponent<Rigidbody2D>();
        Transform root = rb != null ? rb.transform : other.transform;
        root.position = point.position;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (logRespawns) Debug.Log("[PitRespawnZone] Respawn: " + root.name);
    }
}
