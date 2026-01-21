using UnityEngine;

// Gère l'interaction avec l'affiche des niveaux
public class LevelDisplayInteraction : MonoBehaviour, IInteractable
{
    [Header("Menu")]
    public Canvas menuCanvas;
    public float detectionDistance = 5f;
    public KeyCode interactKey = KeyCode.F; // Utilise F au lieu de E pour éviter le conflit avec ItemPickup

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip interactClip;
    [Range(0f, 1f)] public float interactVolume = 1f;
    public bool useAudioSource = true;

    private Transform currentPlayer;
    private bool isPlayerNear = false;

    void Start()
    {
        if (menuCanvas == null)
        {
            LevelSelectionMenu menu = UnityEngine.Object.FindAnyObjectByType<LevelSelectionMenu>();
            if (menu != null)
            {
                menuCanvas = menu.GetComponent<Canvas>();
            }
        }

        if (useAudioSource)
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            audioSource.playOnAwake = false;
        }

        if (menuCanvas != null)
        {
            menuCanvas.enabled = false;
        }

        Debug.Log("LevelDisplayInteraction initialized. Press " + interactKey + " to open level menu.");
    }

    void Update()
    {
        // Vérifier si le joueur est près et appuie sur la touche d'interaction
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            Debug.Log("Opening level menu!");
            OpenMenu();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Chercher n'importe quel objet avec un script (joueur)
        if (collision.CompareTag("Player") || collision.GetComponent<ItemPickup>() != null || collision.GetComponent<PlayerMovement>() != null)
        {
            isPlayerNear = true;
            currentPlayer = collision.transform;
            Debug.Log("Player detected near level display!");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform == currentPlayer)
        {
            isPlayerNear = false;
            currentPlayer = null;
            
            // Fermer le menu si le joueur s'éloigne
            CloseMenu();
            Debug.Log("Player left level display!");
        }
    }

    public void OpenMenu()
    {
        // Jouer le son d'interaction
        PlayActionSound();

        // Ouvrir le menu des niveaux
        if (menuCanvas != null)
        {
            menuCanvas.enabled = true;
            Time.timeScale = 0f; // Mettre le jeu en pause
        }
    }

    // Pour fermer le menu
    public void CloseMenu()
    {
        if (menuCanvas != null && menuCanvas.enabled)
        {
            menuCanvas.enabled = false;
            Time.timeScale = 1f; // Reprendre le jeu
        }
    }

    // Implémentation de IInteractable si besoin
    public void Interact(ItemPickup actor)
    {
        OpenMenu();
    }

    // Méthode publique pour jouer le son (utilisable depuis UI events)
    public void PlayActionSound()
    {
        if (interactClip == null)
        {
            Debug.LogWarning("LevelDisplayInteraction: Aucun clip d'interaction assigné.", this);
            return;
        }

        if (useAudioSource && audioSource != null)
        {
            audioSource.volume = interactVolume;
            audioSource.PlayOneShot(interactClip);
            Debug.Log("LevelDisplayInteraction: Son joué via AudioSource.", this);
        }
        else
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(interactClip, pos, interactVolume);
            Debug.Log("LevelDisplayInteraction: Son joué via PlayClipAtPoint.", this);
        }
    }
}
