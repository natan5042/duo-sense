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

    [Header("Camera Bounds")]
    public bool useCameraBounds = true;
    public Vector2 boundsMin = new Vector2(-50, -50); // Limite inférieure gauche
    public Vector2 boundsMax = new Vector2(50, 50);   // Limite supérieure droite

    private Camera cam;
    private Transform currentTarget; // La cible que la caméra doit suivre (milieu ou Iris)

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollow nécessite un composant Camera!");
        }
        // La cible de base est Achille si présent, sinon Iris
        currentTarget = wheelchairTarget != null ? wheelchairTarget : playerTarget; 
    }

    // Fonction publique pour être appelée par un Trigger Zone
    public void OverrideCamera(bool shouldOverride)
    {
        followSinglePlayer = shouldOverride;
    }

    void LateUpdate()
    {
        bool hasWheelchair = wheelchairTarget != null;
        bool hasPlayer = playerTarget != null;

        // Aucun cible valide
        if (!hasWheelchair && !hasPlayer) return;

        Vector3 desiredPosition;
        float desiredSize;
        float currentSmoothSpeed;

        // =========================================================
        // LOGIQUE DE SUIVI 
        // =========================================================
        
        if (!hasWheelchair && hasPlayer)
        {
            // Mode solo automatique quand seule Iris est dans la scène
            currentTarget = playerTarget;
            desiredPosition = currentTarget.position + offset;
            desiredSize = singlePlayerZoom;
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
            if (!hasWheelchair || !hasPlayer)
            {
                // Fallback si une des cibles manque: suivre celle qui existe
                currentTarget = hasWheelchair ? wheelchairTarget : playerTarget;
                desiredPosition = currentTarget.position + offset;
                desiredSize = hasWheelchair ? minOrthographicSize : singlePlayerZoom;
                currentSmoothSpeed = smoothSpeed;
            }
            else
            {
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

        // Appliquer les limites de caméra
        if (useCameraBounds && cam != null)
        {
            transform.position = ClampCameraPosition(transform.position);
        }

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
    
    /// <summary>
    /// Restreint la position de la caméra pour qu'elle reste dans les limites définies
    /// en tenant compte de la taille de la caméra (orthographique).
    /// </summary>
    private Vector3 ClampCameraPosition(Vector3 cameraPos)
    {
        if (cam == null) return cameraPos;

        // Obtenir la hauteur et la largeur visibles de la caméra
        float cameraHeight = cam.orthographicSize * 2f;
        float cameraWidth = cameraHeight * cam.aspect;

        // Calculer les demi-dimensions
        float halfWidth = cameraWidth / 2f;
        float halfHeight = cameraHeight / 2f;

        // Restreindre X
        float clampedX = Mathf.Clamp(
            cameraPos.x,
            boundsMin.x + halfWidth,
            boundsMax.x - halfWidth
        );

        // Restreindre Y
        float clampedY = Mathf.Clamp(
            cameraPos.y,
            boundsMin.y + halfHeight,
            boundsMax.y - halfHeight
        );

        return new Vector3(clampedX, clampedY, cameraPos.z);
    }
}