using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform wheelchairTarget;       // Achille (Le fauteuil roulant)
    public Transform playerTarget;           // Iris (La femme/joueur aveugle)

    [Header("Camera Settings")]
    public float smoothSpeed = 0.125f;       // Vitesse de suivi standard
    public Vector3 offset = new Vector3(0, 0, -10); // Décalage caméra

    [Header("Distance Settings")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float minOrthographicSize = 5f;
    public float maxOrthographicSize = 10f;
    public float zoomSmoothSpeed = 2f;

    [Header("Single Target Override")]
    // Active ou désactive le mode de suivi unique (déclenché par le Trigger)
    public bool followSinglePlayer = false;
    // Taille de la caméra quand elle suit Iris seule (plus serré pour la fin)
    public float singlePlayerZoom = 6f; 
    public float singlePlayerSmoothSpeed = 0.05f; // Rendre le suivi solo très fluide

    private Camera cam;
    private Transform currentTarget; // La cible que la caméra doit suivre (milieu ou Iris)
    // Focus manuel temporaire (ex: puzzle)
    private bool manualFocusActive = false;
    private Transform manualFocusTarget = null;
    private float manualFocusSize = 6f;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollow nécessite un composant Camera!");
        }
        // La cible de base est Achille (priorité)
        currentTarget = wheelchairTarget; 
    }

    // Fonction publique pour être appelée par un Trigger Zone
    public void OverrideCamera(bool shouldOverride)
    {
        followSinglePlayer = shouldOverride;
    }

    // Permet de forcer un focus manuel sur un Transform avec une taille donnée
    // enable = true active le focus manuel, false le désactive
    public void SetManualFocus(Transform anchor, float size, bool enable)
    {
        manualFocusActive = enable;
        manualFocusTarget = anchor;
        manualFocusSize = size;
        if (enable && manualFocusTarget != null)
        {
            // quand on active le focus manuel, on priorise le suivi solo
            currentTarget = manualFocusTarget;
        }
    }

    void LateUpdate()
    {
        if (wheelchairTarget == null || playerTarget == null) return;

        Vector3 desiredPosition;
        float desiredSize;
        float currentSmoothSpeed;

        // =========================================================
        // LOGIQUE DE SUIVI 
        // =========================================================
        
        if (manualFocusActive && manualFocusTarget != null)
        {
            // MODE MANUEL : focus sur un point précis (ex: puzzle)
            currentTarget = manualFocusTarget;
            desiredPosition = currentTarget.position + offset;
            desiredSize = manualFocusSize;
            currentSmoothSpeed = singlePlayerSmoothSpeed;
        }
        else if (followSinglePlayer)
        {
            // MODE 1 : SUIVI D'IRIS SEULE (Fin de niveau)
            currentTarget = playerTarget;
            desiredPosition = currentTarget.position + offset;
            desiredSize = singlePlayerZoom;
            currentSmoothSpeed = singlePlayerSmoothSpeed;
        }
        else
        {
            // MODE 2 : SUIVI DUO (Mode normal)
            
            // Calculer la distance entre les deux joueurs
            float distance = Vector2.Distance(
                new Vector2(wheelchairTarget.position.x, wheelchairTarget.position.y),
                new Vector2(playerTarget.position.x, playerTarget.position.y)
            );
            
            currentSmoothSpeed = smoothSpeed;

            // Si la distance est trop grande, re-focus sur le fauteuil (Achille)
            if (distance > maxDistance)
            {
                currentTarget = wheelchairTarget;
                desiredPosition = currentTarget.position + offset;
                desiredSize = minOrthographicSize;
            }
            else
            {
                // Si la distance est acceptable, suivre le point milieu
                Vector3 midpoint = (wheelchairTarget.position + playerTarget.position) / 2f;
                desiredPosition = midpoint + offset;

                // Ajuster le zoom
                float distanceRatio = Mathf.InverseLerp(minDistance, maxDistance, distance);
                desiredSize = Mathf.Lerp(minOrthographicSize, maxOrthographicSize, distanceRatio);
            }
        }

        // =========================================================
        // APPLICATION DE LA POSITION ET DU ZOOM
        // =========================================================

        // Interpolation pour un suivi fluide
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            currentSmoothSpeed // Utilise la vitesse du mode actuel
        );
        // Applique la position, en conservant le Z de la caméra
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);

        // Ajuster le zoom (orthographique)
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize, 
                desiredSize, 
                zoomSmoothSpeed * Time.deltaTime
            );
        }
    }

    // ... (La fonction OnDrawGizmosSelected reste inchangée) ...
}