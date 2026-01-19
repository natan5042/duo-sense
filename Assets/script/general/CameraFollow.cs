using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform wheelchairTarget;       // Achille (Le fauteuil roulant)
    public Transform playerTarget;           // Iris (La femme/joueur aveugle)

    [Header("Camera Settings")]
    public float smoothSpeed = 0.125f;       // Vitesse de suivi standard
    public Vector3 offset = new Vector3(0, 0, -10); // Décalage caméra
    [Tooltip("Ajuste la taille ortho globale pour reculer/zoomer la caméra (valeur positive = plus loin).")]
    public float globalZoomOut = 0f;

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

    [Header("Auto assign targets")]
    [Tooltip("Essaie de retrouver Iris/Achille si les références sont perdues (nouvelle scène, respawn, etc.).")]
    public bool autoFindTargets = true;
    public string playerTagHint = "Player";
    public string playerNameContains = "Iris";
    public string wheelchairTagHint = "Player";
    public string wheelchairNameContains = "Achille";

    [Header("Override simple (Hub)")]
    [Tooltip("Forcer la caméra à suivre une seule cible simple (ex: Hub). Ignore le duo/zoom auto.")]
    public bool forceOverrideTarget = false;
    public Transform overrideTarget;
    public float overrideOrthoSize = 7f;

    [Header("Override par tag (secours scène")]
    [Tooltip("Si activé, cherche chaque frame une cible par tag/nom et suit uniquement elle (utile niveau 2).")]
    public bool forceSingleByTag = false;
    public string singleTag = "Player";
    public string singleNameContains = "";
    public float singleOrthoSize = 7f;

    [Header("Limits")]
    [Tooltip("Ignore les limites quand on suit Iris seule (évite de " +
             "bloquer la caméra si la zone de fin est hors bornes).")]
    public bool ignoreBoundsInSingleMode = true;

    [Header("Focus manuel puzzle")]
    public bool manualFocus = false;
    public Transform manualTarget;
    public float manualSize = 19f;

    [Header("Camera Bounds")]
    public bool useCameraBounds = true;
    public Vector2 boundsMin = new Vector2(-50, -50); // Limite inferieure gauche
    public Vector2 boundsMax = new Vector2(50, 50);   // Limite superieure droite

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

    public void SetManualFocus(Transform target, float size, bool enable)
    {
        manualFocus = enable;
        manualTarget = target;
        manualSize = size;
    }

    // Fonction publique pour être appelée par un Trigger Zone
    public void OverrideCamera(bool shouldOverride)
    {
        followSinglePlayer = shouldOverride;
    }

    void LateUpdate()
    {
        if (autoFindTargets)
        {
            TryReacquireTargets(); // Evite de rester bloqué si la référence a été perdue
        }

        // Mode override simple (utile pour le Hub: une seule cible assignée)
        if (forceOverrideTarget && overrideTarget != null)
        {
            Vector3 desired = overrideTarget.position + offset;
            Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed);
            transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(
                    cam.orthographicSize,
                    overrideOrthoSize + globalZoomOut,
                    zoomSmoothSpeed * Time.deltaTime
                );
            }
            return;
        }

        // Mode override par tag (secours pour scènes où les refs ne sont pas assignées)
        if (forceSingleByTag)
        {
            if (overrideTarget == null)
            {
                overrideTarget = FindTarget(singleTag, singleNameContains);
            }
            if (overrideTarget != null)
            {
                Vector3 desired = overrideTarget.position + offset;
                Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed);
                transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);

                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Lerp(
                        cam.orthographicSize,
                        singleOrthoSize + globalZoomOut,
                        zoomSmoothSpeed * Time.deltaTime
                    );
                }
                return;
            }
        }

        bool hasWheelchair = wheelchairTarget != null && wheelchairTarget.gameObject.activeInHierarchy;
        bool hasPlayer = playerTarget != null && playerTarget.gameObject.activeInHierarchy;

        // Aucun cible valide
        if (!hasWheelchair && !hasPlayer) return;

        if (manualFocus && manualTarget != null)
        {
            Vector3 manualDesiredPosition = manualTarget.position + offset;
            Vector3 manualSmoothedPosition = Vector3.Lerp(transform.position, manualDesiredPosition, smoothSpeed);
            transform.position = new Vector3(manualSmoothedPosition.x, manualSmoothedPosition.y, transform.position.z);

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(
                    cam.orthographicSize,
                    manualSize + globalZoomOut,
                    zoomSmoothSpeed * Time.deltaTime
                );
            }
            return;
        }

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
                if (distance > maxDistance * 2f) // si l'autre est vraiment loin, on suit Iris
                {
                    currentTarget = playerTarget;
                    desiredPosition = currentTarget.position + offset;
                    desiredSize = singlePlayerZoom;
                }
                else if (distance > maxDistance)
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
        bool shouldClamp = useCameraBounds && cam != null;

        // Permet de sortir des bornes quand on suit un seul perso (mode solo ou override Iris)
        if (ignoreBoundsInSingleMode && (followSinglePlayer || !hasWheelchair))
        {
            shouldClamp = false;
        }

        if (shouldClamp)
        {
            transform.position = ClampCameraPosition(transform.position);
        }

        // Ajuster le zoom (orthographique)
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize, 
                desiredSize + globalZoomOut, 
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

    private void TryReacquireTargets()
    {
        if (playerTarget == null)
        {
            playerTarget = FindTarget(playerTagHint, playerNameContains);
        }

        if (wheelchairTarget == null)
        {
            wheelchairTarget = FindTarget(wheelchairTagHint, wheelchairNameContains);
        }
    }

    private Transform FindTarget(string tagHint, string nameHint)
    {
        Transform found = null;

        // Essai par tag (plus rapide) si le tag existe dans le projet
        if (!string.IsNullOrEmpty(tagHint))
        {
            try
            {
                var candidatesByTag = GameObject.FindGameObjectsWithTag(tagHint);
                found = FindByNameHint(candidatesByTag, nameHint);
            }
            catch (UnityException)
            {
                // Tag manquant : on passe à la recherche par nom
            }
        }

        // Fallback : scan complet par nom
        if (found == null)
        {
            var allTransforms = FindObjectsOfType<Transform>(true);
            found = FindByNameHint(allTransforms, nameHint);
        }

        return found;
    }

    private Transform FindByNameHint(GameObject[] candidates, string nameHint)
    {
        if (candidates == null || candidates.Length == 0) return null;
        if (string.IsNullOrEmpty(nameHint)) return candidates[0].transform;

        foreach (var go in candidates)
        {
            if (go != null && go.name.Contains(nameHint))
            {
                return go.transform;
            }
        }

        return candidates[0].transform;
    }

    private Transform FindByNameHint(Transform[] candidates, string nameHint)
    {
        if (candidates == null || candidates.Length == 0) return null;
        if (string.IsNullOrEmpty(nameHint)) return candidates[0];

        foreach (var tr in candidates)
        {
            if (tr != null && tr.name.Contains(nameHint))
            {
                return tr;
            }
        }

        return candidates[0];
    }
}