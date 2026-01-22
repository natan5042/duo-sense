using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Header("Bounce")]
    public float bounceForce = 14f;          // Impulsion vers le haut
    public bool resetVerticalSpeed = true;   // Mettre la vitesse verticale à 0 avant l'impulsion

    [Header("Filter")]
    public string requiredTag = "Player";   // Laisser vide pour accepter tout

    [Header("Audio")]
    public AudioSource audioSource;          // Optionnel, sinon on prend celui sur l'objet
    public AudioClip bounceClip;

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleBounce(other.attachedRigidbody, other.gameObject, "OnTriggerEnter2D");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleBounce(collision.rigidbody, collision.gameObject, "OnCollisionEnter2D");
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void HandleBounce(Rigidbody2D rb, GameObject source, string via)
    {
        if (rb == null)
        {
            Debug.Log($"[Trampoline] {via} ignoré: pas de Rigidbody2D attaché sur {source?.name ?? "(null)"}");
            return;
        }

        if (!string.IsNullOrEmpty(requiredTag) && (source == null || !source.CompareTag(requiredTag)))
        {
            Debug.Log($"[Trampoline] {via} ignoré: tag requis '{requiredTag}', obtenu '{source?.tag ?? "(null)"}' sur {source?.name ?? "(null)"}");
            return;
        }

        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning($"[Trampoline] {via}: {source?.name ?? "(null)"} a un bodyType {rb.bodyType}; l'impulsion ne s'applique pas aux corps non Dynamic.");
        }

        if ((rb.constraints & RigidbodyConstraints2D.FreezePositionY) != 0)
        {
            Debug.LogWarning($"[Trampoline] {via}: {source?.name ?? "(null)"} a FreezePositionY, le saut sera bloqué.");
        }

        Debug.Log($"[Trampoline] contact par {source?.name ?? "(null)"} via {via}, impulsion {bounceForce}");

        // Remettre la vitesse verticale pour éviter d'écraser ou d'inverser l'impulsion
        if (resetVerticalSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        // Appliquer directement la vitesse verticale pour garantir le saut
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);

        PlayBounceSound();
    }

    private void PlayBounceSound()
    {
        if (audioSource != null && bounceClip != null)
        {
            audioSource.PlayOneShot(bounceClip);
        }
    }
}
