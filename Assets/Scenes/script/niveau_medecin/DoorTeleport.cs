using UnityEngine;
using UnityEngine.UI;

public class DoorTeleport : MonoBehaviour, IInteractable
{
    [Header("T�l�portation")]
    [Tooltip("Position o� Iris sera t�l�port�e")]
    public Transform teleportDestination;
  
    [Tooltip("Ou sp�cifiez directement une position")]
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

    [Header("Puzzle")]
    [Tooltip("Référence au contrôleur du puzzle")]
    public PuzzleLeverController puzzleController;
    [Tooltip("Mouvement d'Iris pour pouvoir le désactiver pendant le puzzle")]
    public PlayerMovement irisMovement;
    [Tooltip("Point de focus caméra pour afficher tout le puzzle")]
    public Transform puzzleCameraAnchor;
    [Tooltip("Taille de caméra pour voir tout le puzzle")]
    public float puzzleCameraSize = 19f;

    [Header("Feedback Visuel (Optionnel)")]
    public bool showPrompt = true;
    public string promptText = "Appuyez sur [S] pour entrer";
    [Tooltip("UI Text à afficher (si vide, on cherche un enfant 'PromptText' ou on crée un Canvas à la volée)")]
    public Text promptTextUI;
    [Tooltip("Panel/GameObject à activer quand Iris est dans la zone (optionnel)")]
    public GameObject promptPanel;

    [Header("Audio (Optionnel)")]
    public AudioSource doorSound;

    private bool playerInRange = false;
    private GameObject playerInZone = null;
    private bool puzzleActive = false;
    private Canvas promptCanvas;
    private Text promptTextRuntime;
    private GameObject irisObject;
    private float originalGravityScale = 1f;
    private Vector3 originalScale;
    private Rigidbody2D irisRigidbody;

    void Start()
    {
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

        if (showPrompt && promptTextUI == null && promptPanel == null)
        {
            TryFindOrCreatePromptUI();
        }
    }

    void TryFindOrCreatePromptUI()
    {
        Transform t = transform.Find("PromptText");
        if (t != null)
        {
            promptTextUI = t.GetComponent<Text>();
            if (promptTextUI != null) return;
        }
        promptTextUI = GetComponentInChildren<Text>(true);
        if (promptTextUI != null) return;

        CreatePromptCanvas();
    }

    void CreatePromptCanvas()
    {
        GameObject go = new GameObject("DoorTeleportPrompt");
        go.transform.SetParent(null);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        promptTextRuntime = textGo.AddComponent<Text>();
        promptTextRuntime.text = promptText;
        promptTextRuntime.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptTextRuntime.fontSize = 36;
        promptTextRuntime.alignment = TextAnchor.MiddleCenter;
        promptTextRuntime.color = Color.white;

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800, 80);

        promptCanvas = canvas;
        go.SetActive(false);
    }

    void ShowPromptUI()
    {
        if (!showPrompt) return;
        if (promptPanel != null) { promptPanel.SetActive(true); return; }
        if (promptTextUI != null)
        {
            promptTextUI.text = promptText;
            promptTextUI.gameObject.SetActive(true);
            if (promptTextUI.transform.parent != null)
                promptTextUI.transform.parent.gameObject.SetActive(true);
            return;
        }
        if (promptCanvas != null && promptTextRuntime != null)
        {
            promptTextRuntime.text = promptText;
            promptCanvas.gameObject.SetActive(true);
        }
    }

    void HidePromptUI()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
        if (promptTextUI != null)
        {
            promptTextUI.gameObject.SetActive(false);
            if (promptTextUI.transform.parent != null)
                promptTextUI.transform.parent.gameObject.SetActive(false);
        }
        if (promptCanvas != null) promptCanvas.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.name.Contains(targetPlayerName))
        {
            playerInRange = true;
            playerInZone = other.gameObject;
            if (showPrompt) ShowPromptUI();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == playerInZone)
        {
            playerInRange = false;
            playerInZone = null;
            HidePromptUI();
        }
    }

    void Update()
    {
        if (playerInZone == null && playerInRange) playerInRange = false;

        if (playerInRange && Input.GetKeyDown(interactKey) && playerInZone != null && !puzzleActive)
        {
            TeleportPlayer();
        }

        if (puzzleActive && Input.GetKeyDown(exitPuzzleKey))
        {
            ExitPuzzleMode();
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null || player.gameObject == null || !player.gameObject.name.Contains(targetPlayerName)) return;
        var col = GetComponent<Collider2D>();
        float maxDist = col != null ? Mathf.Max(col.bounds.size.x, col.bounds.size.y) * 2f : 5f;
        if (Vector3.Distance(transform.position, player.transform.position) > maxDist) return;
        playerInZone = player.gameObject;
        playerInRange = true;
        if (!puzzleActive) TeleportPlayer();
    }

    void TeleportPlayer()
    {
        if (playerInZone == null) return;

        irisObject = playerInZone;

        Vector3 targetPos;
        if (useTransform && teleportDestination != null)
        {
            targetPos = teleportDestination.position;
        }
        else
        {
            targetPos = destinationPosition;
        }

        irisObject.transform.position = targetPos;

        if (scaleDownPlayer)
        {
            originalScale = irisObject.transform.localScale;
            irisObject.transform.localScale = originalScale * scaleMultiplier;
            Debug.Log($"[DoorTeleport] Taille d'Iris réduite à {scaleMultiplier * 100}% de la taille originale.");
        }

        irisRigidbody = irisObject.GetComponent<Rigidbody2D>();
        if (irisRigidbody != null)
        {
            irisRigidbody.linearVelocity = Vector2.zero;
            irisRigidbody.angularVelocity = 0f;

            if (disableGravityDuringPuzzle)
            {
                originalGravityScale = irisRigidbody.gravityScale;
                irisRigidbody.gravityScale = 0f;
                Debug.Log("[DoorTeleport] Gravité d'Iris désactivée pour le puzzle.");
            }
        }

        if (cameraFollowScript != null)
        {
            if (puzzleCameraAnchor != null)
            {
                cameraFollowScript.SetManualFocus(puzzleCameraAnchor, puzzleCameraSize, true);
            }
            else if (overrideCameraToIris)
            {
                cameraFollowScript.OverrideCamera(true);
            }
            Debug.Log("[DoorTeleport] Caméra réglée pour le puzzle.");
        }

        if (irisMovement != null)
        {
            irisMovement.enabled = false;
        }

        if (puzzleController != null)
        {
            puzzleController.Activate();
        }

        if (doorSound != null && doorSound.clip != null)
        {
            doorSound.Play();
        }

        Debug.Log($"Iris téléportée à {targetPos} - Mode Puzzle activé");

        puzzleActive = true;
        playerInRange = false;
        playerInZone = null;
    }

    // Fonction publique pour sortir du mode puzzle (peut �tre appel�e par un autre script)
    public void ExitPuzzleMode()
    {
        if (!puzzleActive) return;

        if (scaleDownPlayer && irisObject != null)
        {
            irisObject.transform.localScale = originalScale;
            Debug.Log("[DoorTeleport] Taille d'Iris restaurée.");
        }

        if (disableGravityDuringPuzzle && irisRigidbody != null)
        {
            irisRigidbody.gravityScale = originalGravityScale;
            Debug.Log("[DoorTeleport] Gravité d'Iris restaurée.");
        }

        if (cameraFollowScript != null)
        {
            cameraFollowScript.SetManualFocus(null, 0f, false);
            if (overrideCameraToIris)
            {
                cameraFollowScript.OverrideCamera(false);
            }
            Debug.Log("[DoorTeleport] Caméra restaurée en mode duo.");
        }

        if (irisMovement != null)
        {
            irisMovement.enabled = true;
        }

        if (puzzleController != null)
        {
            puzzleController.Deactivate();
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
