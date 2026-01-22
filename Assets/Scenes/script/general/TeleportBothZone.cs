using UnityEngine;

// Zone qui téléporte les deux personnages quand ils sont dedans
[RequireComponent(typeof(Collider2D))]
public class TeleportBothZone : MonoBehaviour
{
    [Header("Références persos")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private WheelchairMovement wheelchair;

    [Header("Cibles de téléportation")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform wheelchairTarget;

    [Header("Options")]
    [SerializeField] private bool requireBothInside = true;
    [SerializeField] private bool disableColliderAfterTeleport = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip teleportClip;

    private bool playerInside;
    private bool wheelchairInside;
    private bool alreadyTeleported;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true; // pour laisser passer les persos
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        UpdatePresence(other, true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        UpdatePresence(other, false);
    }

    void UpdatePresence(Collider2D other, bool inside)
    {
        var foundPlayer = other.GetComponentInParent<PlayerMovement>();
        if (foundPlayer != null && (player == null || foundPlayer == player))
        {
            player = foundPlayer;
            playerInside = inside;
        }

        var foundWheelchair = other.GetComponentInParent<WheelchairMovement>();
        if (foundWheelchair != null && (wheelchair == null || foundWheelchair == wheelchair))
        {
            wheelchair = foundWheelchair;
            wheelchairInside = inside;
        }

        bool condition = requireBothInside ? (playerInside && wheelchairInside) : (playerInside || wheelchairInside);
        if (condition)
        {
            TryTeleport();
        }
    }

    void TryTeleport()
    {
        if (alreadyTeleported) return;
        if (requireBothInside && (player == null || wheelchair == null)) return;
        if (playerTarget == null || wheelchairTarget == null) return;

        // Téléportation avec remise à zéro des vitesses
        if (player != null)
        {
            player.transform.position = playerTarget.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (wheelchair != null)
        {
            wheelchair.transform.position = wheelchairTarget.position;
            var rb = wheelchair.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (audioSource != null && teleportClip != null)
        {
            audioSource.PlayOneShot(teleportClip);
        }

        alreadyTeleported = true;

        if (disableColliderAfterTeleport)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}
