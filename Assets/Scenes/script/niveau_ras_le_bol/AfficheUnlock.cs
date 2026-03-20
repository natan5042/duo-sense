using System.Collections.Generic;
using UnityEngine;

// Fait apparaître un objet quand toutes les affiches requises ont été posées.
public class AfficheUnlock : MonoBehaviour
{
    [Header("Affiches requises")]
    [Tooltip("Affiches à poser pour débloquer l'objet")]
    public List<AfficheInteractive> affichesRequises = new List<AfficheInteractive>();

    [Header("Apparence")]
    [Tooltip("Objet à afficher une fois débloqué (par défaut ce GameObject)")]
    public GameObject visualObject;

    [Header("Feedback")]
    [Tooltip("Message console quand le déblocage se produit")]
    public string messageDebloque = "Toutes les affiches sont posées !";
    [Tooltip("Son joué à l'apparition")]
    public AudioClip unlockSound;

    private AudioSource audioSource;
    private bool isUnlocked;

    void Start()
    {
        if (visualObject == null)
        {
            visualObject = gameObject;
        }

        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && unlockSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        foreach (var affiche in affichesRequises)
        {
            if (affiche != null)
            {
                affiche.onAffichePosee += OnAffichePosee;
            }
        }

        CheckCompletion();
    }

    void OnDestroy()
    {
        foreach (var affiche in affichesRequises)
        {
            if (affiche != null)
            {
                affiche.onAffichePosee -= OnAffichePosee;
            }
        }
    }

    void OnAffichePosee(AfficheInteractive _)
    {
        if (isUnlocked)
        {
            return;
        }

        CheckCompletion();
    }

    void CheckCompletion()
    {
        if (isUnlocked)
        {
            return;
        }

        foreach (var affiche in affichesRequises)
        {
            if (affiche == null || !affiche.EstPosee)
            {
                return;
            }
        }

        Unlock();
    }

    void Unlock()
    {
        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;

        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        if (!string.IsNullOrEmpty(messageDebloque))
        {
            Debug.Log(messageDebloque);
        }

        Debug.Log("AfficheUnlock: Objet débloqué.");
    }

    // Appel manuel depuis l'inspecteur pour debug
    public void ForceCheck()
    {
        CheckCompletion();
    }
}
