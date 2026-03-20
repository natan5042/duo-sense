using UnityEngine;

public class ProximityBeepDetector : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("GameObject du joueur à détecter")]
    public GameObject joueurCible;
    
    [Header("Audio")]
    [Tooltip("Son du bip")]
    public AudioClip sonBip;
    
    [Tooltip("Volume du bip (0 à 1)")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    
    [Header("Distance")]
    [Tooltip("Distance maximale de détection")]
    public float distanceMax = 15f;
    
    [Tooltip("Distance minimale (bip le plus rapide)")]
    public float distanceMin = 1f;
    
    [Header("Fréquence du bip")]
    [Tooltip("Intervalle le plus lent (secondes) quand loin")]
    public float intervalMax = 2f;
    
    [Tooltip("Intervalle le plus rapide (secondes) quand proche")]
    public float intervalMin = 0.1f;
    
    private AudioSource audioSource;
    private float prochainBip = 0f;
    private bool joueurDetecte = false;
    
    void Start()
    {
        Debug.Log("[ProximityBeepDetector] Initialisation sur: " + gameObject.name);
        
        // Récupère ou ajoute un AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("[ProximityBeepDetector] AudioSource ajouté");
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Son en 2D (non spatial)
        
        // Vérifier si un joueur est assigné
        if (joueurCible == null)
        {
            Debug.LogError("[ProximityBeepDetector] *** AUCUN JOUEUR ASSIGNÉ sur " + gameObject.name + " ! Glissez le joueur dans l'inspecteur. ***");
        }
        else
        {
            Debug.Log("[ProximityBeepDetector] Joueur assigné: " + joueurCible.name);
        }
        
        // Vérifier si un son est assigné
        if (sonBip == null)
        {
            Debug.LogError("[ProximityBeepDetector] *** AUCUN AUDIOCLIP DE BIP ASSIGNÉ sur " + gameObject.name + " ! ***");
        }
        else
        {
            Debug.Log("[ProximityBeepDetector] Son de bip assigné: " + sonBip.name);
        }
        
        Debug.Log("[ProximityBeepDetector] Configuration - DistanceMax: " + distanceMax + " - DistanceMin: " + distanceMin);
        Debug.Log("[ProximityBeepDetector] Intervalles - Max: " + intervalMax + "s - Min: " + intervalMin + "s");
    }
    
    void Update()
    {
        if (joueurCible == null)
        {
            // Pas de joueur assigné
            return;
        }
        
        // Calcule la distance entre l'objet et le joueur (2D: ignore Z)
        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(joueurCible.transform.position.x, joueurCible.transform.position.y)
        );
        
        // Vérifie si le joueur est dans la zone de détection
        if (distance <= distanceMax)
        {
            if (!joueurDetecte)
            {
                joueurDetecte = true;
                Debug.Log("[ProximityBeepDetector] Joueur entré dans la zone de détection (distance: " + distance.ToString("F2") + ")");
            }
            
            // Calcule l'intervalle entre les bips en fonction de la distance
            // Plus le joueur est proche, plus l'intervalle est court
            float intervalActuel = CalculerIntervalle(distance);
            
            // Joue le bip si le temps est écoulé
            if (Time.time >= prochainBip)
            {
                JouerBip(distance);
                prochainBip = Time.time + intervalActuel;
            }
        }
        else
        {
            // Le joueur est sorti de la zone de détection
            if (joueurDetecte)
            {
                joueurDetecte = false;
                prochainBip = 0f; // Réinitialise le timer
                Debug.Log("[ProximityBeepDetector] Joueur sorti de la zone de détection");
            }
        }
    }
    
    float CalculerIntervalle(float distance)
    {
        // Clamp la distance entre min et max
        distance = Mathf.Clamp(distance, distanceMin, distanceMax);
        
        // Normalise la distance (0 = très proche, 1 = très loin)
        float t = (distance - distanceMin) / (distanceMax - distanceMin);
        
        // Interpole l'intervalle (proche = intervalMin, loin = intervalMax)
        return Mathf.Lerp(intervalMin, intervalMax, t);
    }
    
    void JouerBip(float distance)
    {
        if (sonBip != null)
        {
            audioSource.PlayOneShot(sonBip, volume);
            // Debug.Log("[ProximityBeepDetector] Bip joué - Distance: " + distance.ToString("F2"));
        }
        else
        {
            Debug.LogError("[ProximityBeepDetector] Aucun son de bip assigné à " + gameObject.name);
        }
    }
    
    // Pour visualiser la zone de détection dans l'éditeur (2D)
    void OnDrawGizmosSelected()
    {
        // Dessine des cercles pour un jeu 2D
        DrawCircle2D(transform.position, distanceMax, Color.red, 64);
        DrawCircle2D(transform.position, distanceMin, Color.green, 32);
        
        // Ligne vers le joueur si assigné
        if (joueurCible != null)
        {
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(joueurCible.transform.position.x, joueurCible.transform.position.y)
            );
            
            if (distance <= distanceMax)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, joueurCible.transform.position);
                
                // Affiche la distance
                Gizmos.color = Color.white;
                Vector3 midPoint = (transform.position + joueurCible.transform.position) / 2f;
            }
        }
    }
    
    void DrawCircle2D(Vector3 center, float radius, Color color, int segments)
    {
        Gizmos.color = color;
        float angle = 0f;
        Vector3 lastPos = center + new Vector3(radius, 0, 0);
        
        for (int i = 0; i <= segments; i++)
        {
            angle = i * 360f / segments * Mathf.Deg2Rad;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
        }
    }
}
