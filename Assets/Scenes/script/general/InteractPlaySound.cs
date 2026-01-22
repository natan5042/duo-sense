using UnityEngine;

// Interaction simple qui joue un son quand on appuie sur S
public class InteractPlaySound : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    public AudioSource audioSource;   // optionnel, sinon on prend celui du joueur
    public AudioClip clip;            // son à jouer

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void Interact(PlayerInteraction player)
    {
        var source = audioSource;
        if (source == null && player != null)
        {
            source = player.GetComponent<AudioSource>();
        }

        if (source != null && !source.enabled)
        {
            source.enabled = true; // s'assurer qu'on peut jouer le son
        }

        bool played = false;

        if (clip != null)
        {
            if (source != null && source.enabled && source.gameObject.activeInHierarchy)
            {
                source.PlayOneShot(clip);
                played = true;
            }

            if (!played)
            {
                // Fallback pour éviter les warnings si l'audiosource est désactivée
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }
    }
}
