using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    [Header("Téléportation")]
    [Tooltip("Position où Iris sera téléportée")]
    public Transform teleportDestination;
  
    [Tooltip("Ou spécifiez directement une position")]
    public Vector3 destinationPosition;
    
    [Tooltip("Utiliser le Transform ou la position Vector3")]
    public bool useTransform = true;

    [Header("Interaction")]
    [Tooltip("Touche pour entrer dans la porte")]
    public KeyCode interactKey = KeyCode.S;

    [Tooltip("Nom du joueur qui peut utiliser la porte (Iris)")]
    public string targetPlayerName = "Iris";

    [Header("Caméra")]
    [Tooltip("Référence au script CameraFollow (sur la caméra principale)")]
    public CameraFollow cameraFollowScript;
    
    [Tooltip("Forcer la caméra à suivre uniquement Iris pendant le puzzle")]
    public bool overrideCameraToIris = true;

    [Header("Gravité")]
    [Tooltip("Désactiver la gravité d'Iris pendant le puzzle")]
    public bool disableGravityDuringPuzzle = true;
    
    [Tooltip("Touche pour sortir du puzzle et restaurer la gravité")]
    public KeyCode exitPuzzleKey = KeyCode.Escape;

    [Header("Taille du personnage")]
    [Tooltip("Réduire la taille d'Iris pendant le puzzle")]
    public bool scaleDownPlayer = true;
    
    [Tooltip("Multiplicateur de taille (0.5 = moitié, 1.0 = normal)")]
    [Range(0.1f, 1.0f)]
    public float scaleMultiplier = 0.5f;

    [Header("Feedback Visuel (Optionnel)")]
    public bool showPrompt = true;
    public string promptText = "Appuyez sur [S] pour entrer";
    
    [Header("Audio (Optionnel)")]
    public AudioSource doorSound;

    // État interne
    private bool playerInRange = false;
    private GameObject playerInZone = null;
    private bool puzzleActive = false;
    private float originalGravityScale = 1f;
    private Vector3 originalScale;
    private Rigidbody2D irisRigidbody;

    void Start()
    {
 // Trouver automatiquement le CameraFollow si non assigné
  if (cameraFollowScript == null)
   {
            Camera mainCam = Camera.main;
         if (mainCam != null)
            {
         cameraFollowScript = mainCam.GetComponent<CameraFollow>();
     if (cameraFollowScript == null)
    {
  Debug.LogWarning("[DoorTeleport] CameraFollow non trouvé sur la caméra principale. Assignez-le manuellement dans l'Inspector.");
           }
       }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifier que c'est un joueur avec le bon nom (Iris)
        if (other.CompareTag("Player") && other.name.Contains(targetPlayerName))
        {
      playerInRange = true;
            playerInZone = other.gameObject;
            
   if (showPrompt)
  {
    Debug.Log(promptText);
      // TODO: Afficher un UI prompt au-dessus de la porte si vous avez un système d'UI
  }
      }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == playerInZone)
        {
   playerInRange = false;
     playerInZone = null;
        }
}

    void Update()
    {
     // Si Iris est dans la zone et appuie sur S
        if (playerInRange && Input.GetKeyDown(interactKey) && playerInZone != null && !puzzleActive)
        {
            TeleportPlayer();
        }

    // Touche pour sortir du puzzle et restaurer les paramètres
        if (puzzleActive && Input.GetKeyDown(exitPuzzleKey))
  {
   ExitPuzzleMode();
        }
    }

    void TeleportPlayer()
    {
    if (playerInZone == null) return;

        // Déterminer la position de destination
 Vector3 targetPos;
        if (useTransform && teleportDestination != null)
        {
      targetPos = teleportDestination.position;
        }
        else
        {
targetPos = destinationPosition;
        }

        // Téléporter Iris
    playerInZone.transform.position = targetPos;

        // Sauvegarder et modifier la taille d'Iris
  if (scaleDownPlayer)
  {
         originalScale = playerInZone.transform.localScale;
            playerInZone.transform.localScale = originalScale * scaleMultiplier;
       Debug.Log($"[DoorTeleport] Taille d'Iris réduite à {scaleMultiplier * 100}% de la taille originale.");
        }

        // Réinitialiser la vélocité pour éviter qu'elle continue à bouger
   irisRigidbody = playerInZone.GetComponent<Rigidbody2D>();
        if (irisRigidbody != null)
        {
    irisRigidbody.linearVelocity = Vector2.zero;
     irisRigidbody.angularVelocity = 0f;

 // Sauvegarder la gravité originale et la désactiver si demandé
          if (disableGravityDuringPuzzle)
        {
          originalGravityScale = irisRigidbody.gravityScale;
irisRigidbody.gravityScale = 0f;
     Debug.Log("[DoorTeleport] Gravité d'Iris désactivée pour le puzzle.");
       }
        }

 // Forcer la caméra à suivre uniquement Iris
        if (overrideCameraToIris && cameraFollowScript != null)
   {
    cameraFollowScript.OverrideCamera(true);
            Debug.Log("[DoorTeleport] Caméra forcée sur Iris uniquement.");
    }

        // Jouer un son si disponible
        if (doorSound != null && doorSound.clip != null)
        {
            doorSound.Play();
        }

  Debug.Log($"Iris téléportée à {targetPos} - Mode Puzzle activé");
        
        // Marquer le puzzle comme actif
        puzzleActive = true;
        
    // Réinitialiser l'état du trigger
        playerInRange = false;
        playerInZone = null;
    }

    // Fonction publique pour sortir du mode puzzle (peut être appelée par un autre script)
    public void ExitPuzzleMode()
    {
      if (!puzzleActive) return;

      // Restaurer la taille d'Iris
 if (scaleDownPlayer && playerInZone != null)
        {
          playerInZone.transform.localScale = originalScale;
            Debug.Log("[DoorTeleport] Taille d'Iris restaurée.");
        }

     // Restaurer la gravité d'Iris
        if (disableGravityDuringPuzzle && irisRigidbody != null)
        {
       irisRigidbody.gravityScale = originalGravityScale;
        Debug.Log("[DoorTeleport] Gravité d'Iris restaurée.");
    }

        // Restaurer le comportement normal de la caméra (retour au suivi duo)
        if (overrideCameraToIris && cameraFollowScript != null)
        {
   cameraFollowScript.OverrideCamera(false);
       Debug.Log("[DoorTeleport] Caméra restaurée en mode duo.");
        }

   puzzleActive = false;
        Debug.Log("[DoorTeleport] Mode Puzzle désactivé.");
    }

    // Gizmo pour visualiser la zone de trigger et la destination
    void OnDrawGizmosSelected()
    {
   // Dessiner la zone de trigger
Gizmos.color = Color.yellow;
   BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
   {
  Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
  }

        // Dessiner la destination
  Vector3 dest = useTransform && teleportDestination != null 
     ? teleportDestination.position 
      : destinationPosition;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(dest, 0.5f);
   Gizmos.DrawLine(transform.position, dest);
    }
}
