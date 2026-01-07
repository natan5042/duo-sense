using UnityEngine;

// À placer sur la pancarte avec la coupe
public class SignBridgeInteraction : MonoBehaviour, IInteractable
{
    [Header("Références visuelles")]
    public GameObject trophySign;       // la pancarte visible au début
    public GameObject arrowBehind;      // la flèche cachée derrière
    public GameObject blockToHide;      // bloc à masquer quand on interagit

    [Header("Audio")]
    public AudioSource audioSource;     // facultatif, sinon on tente d'utiliser l'AudioSource du joueur
    public AudioClip interactionClip;

    bool alreadyUsed;

    public void Interact(ItemPickup actor)
    {
        if (alreadyUsed) return;
        alreadyUsed = true;

        if (trophySign != null) trophySign.SetActive(false);
        if (arrowBehind != null) arrowBehind.SetActive(true);
        if (blockToHide != null) blockToHide.SetActive(false);

        var source = audioSource;
        if (source == null && actor != null)
        {
            source = actor.GetComponent<AudioSource>();
        }

        if (source != null)
        {
            if (!source.enabled) source.enabled = true; // s'assurer que l'AudioSource n'est pas désactivé
            source.spatialBlend = 0f; // forcer en 2D pour être audible de partout
        }

        if (interactionClip != null)
        {
            bool played = false;

            if (source != null && source.enabled && source.gameObject.activeInHierarchy)
            {
                source.PlayOneShot(interactionClip);
                played = true;
            }

            if (!played)
            {
                // Fallback pour éviter le warning "disabled audio source"
                AudioSource.PlayClipAtPoint(interactionClip, Camera.main != null ? Camera.main.transform.position : transform.position);
            }

            Debug.Log("[SignBridgeInteraction] Son joué et panneau utilisé");
        }
        else
        {
            Debug.LogWarning("[SignBridgeInteraction] Pas d'AudioSource ou de clip pour jouer le son");
        }

        // On désactive le collider pour éviter les re-triggers accidentels
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
}
