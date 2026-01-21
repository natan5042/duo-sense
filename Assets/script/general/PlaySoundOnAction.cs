using UnityEngine;

// Script pour jouer un son lors d'une action (ouverture de menu, canvas, etc.)
// Attache ce script sur un GameObject (ex: Canvas, GameManager, ou objet vide)
// Ensuite dans l'inspecteur d'un bouton ou événement, appelle la méthode PlaySound()
public class PlaySoundOnAction : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Son à jouer lors de l'action.")]
    public AudioClip actionSound;

    [Range(0f, 1f)]
    [Tooltip("Volume du son (0 à 1).")]
    public float volume = 1f;

    [Tooltip("Utiliser AudioSource au lieu de PlayClipAtPoint (meilleur pour UI).")]
    public bool useAudioSource = true;

    private AudioSource audioSource;

    void Awake()
    {
        if (useAudioSource)
        {
            // Créer un AudioSource sur cet objet s'il n'en a pas
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }
    }

    // Méthode à appeler depuis l'inspecteur (boutons UI, événements, etc.)
    public void PlaySound()
    {
        if (actionSound == null)
        {
            Debug.LogWarning("PlaySoundOnAction: Aucun son assigné dans actionSound!", this);
            return;
        }

        if (useAudioSource && audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.PlayOneShot(actionSound);
            Debug.Log("PlaySoundOnAction: Son joué avec AudioSource", this);
        }
        else
        {
            Vector3 soundPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(actionSound, soundPosition, volume);
            Debug.Log("PlaySoundOnAction: Son joué avec PlayClipAtPoint", this);
        }
    }
}
