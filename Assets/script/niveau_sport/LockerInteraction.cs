using UnityEngine;

// Gère l'interaction avec un casier : son simple ou casier spécial qui s'ouvre
public class LockerInteraction : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    public AudioSource audioSource;   // optionnel, sinon on prend celui du joueur
    public AudioClip knockClip;       // son pour les casiers standards
    public AudioClip openClip;        // son pour le casier spécial

    [Header("Casier spécial")]
    public bool isSpecialLocker = false;
    public GameObject blockToDisable; // bloc à enlever quand on ouvre le casier spécial

    bool opened;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // forcer en 2D pour être toujours audible
        }
    }

    public void Interact(ItemPickup actor)
    {
        var source = audioSource;
        if (source == null && actor != null)
        {
            source = actor.GetComponent<AudioSource>();
        }

        if (source != null && !source.enabled)
        {
            source.enabled = true; // réactiver l'AudioSource si désactivé
        }

        bool sourceUsable = source != null && source.enabled && source.gameObject.activeInHierarchy;

        if (isSpecialLocker)
        {
            if (opened) return;
            opened = true;

            if (blockToDisable != null)
            {
                blockToDisable.SetActive(false);
            }

            if (openClip != null)
            {
                if (sourceUsable)
                {
                    source.PlayOneShot(openClip);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(openClip, Camera.main != null ? Camera.main.transform.position : transform.position);
                }
                Debug.Log("[LockerInteraction] Casier spécial ouvert + son joué");
            }
            else
            {
                Debug.LogWarning("[LockerInteraction] Pas de source ou d'openClip pour jouer le son d'ouverture");
            }

            return;
        }

        if (knockClip != null)
        {
            if (sourceUsable)
            {
                source.PlayOneShot(knockClip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(knockClip, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
            Debug.Log("[LockerInteraction] Casier knock son joué");
        }
        else
        {
            Debug.LogWarning("[LockerInteraction] Pas de source ou de knockClip pour le casier standard");
        }
    }
}
