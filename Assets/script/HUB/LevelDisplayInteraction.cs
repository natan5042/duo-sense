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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
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
        if (audioSource != null && interactClip != null)
        {
            audioSource.PlayOneShot(interactClip);
        }

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
}
