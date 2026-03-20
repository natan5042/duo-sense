using UnityEngine;

public class ProximitySoundTrigger : MonoBehaviour
{
    [Header("Paramètres d'interaction")]
    [Tooltip("Touche à appuyer pour déclencher le son")]
    public KeyCode toucheInteraction = KeyCode.E;
    
    [Tooltip("GameObject du joueur qui peut interagir")]
    public GameObject joueurCible;
    
    [Header("Audio")]
    [Tooltip("Son à jouer lors de l'interaction")]
    public AudioClip sonInteraction;
    
    [Tooltip("Volume du son (0 à 1)")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("Rayon d'interaction")]
    [Tooltip("Rayon autour de l'objet pour permettre l'interaction")]
    public float rayonInteraction = 3f;
    
    [Header("Optionnel")]
    [Tooltip("Peut être utilisé plusieurs fois")]
    public bool reutilisable = true;
    
    [Tooltip("Message à afficher quand le joueur est à proximité")]
    public string messageProximite = "Appuyez sur E pour interagir";
    
    private bool joueurDansZone = false;
    private GameObject joueurActuel = null;
    private AudioSource audioSource;
    private bool dejaUtilise = false;
    
    void Awake()
    {
        Debug.LogWarning("=== [ProximitySoundTrigger] AWAKE appelé sur: " + gameObject.name + " ===");
    }
    
    void Start()
    {
        Debug.LogWarning("=== [ProximitySoundTrigger] START appelé sur: " + gameObject.name + " ===");
        
        // Récupère ou ajoute un AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("[ProximitySoundTrigger] AudioSource ajouté");
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Son en 2D (non spatial)
        
        // Crée un trigger circulaire 2D si aucun collider 2D n'existe
        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = rayonInteraction;
            Debug.LogWarning("[ProximitySoundTrigger] CircleCollider2D créé automatiquement - Rayon: " + rayonInteraction);
        }
        else
        {
            Debug.Log("[ProximitySoundTrigger] CircleCollider2D trouvé - IsTrigger: " + trigger.isTrigger + " - Rayon: " + trigger.radius);
            // Force le mode trigger
            trigger.isTrigger = true;
        }
        
        // Vérifier et ajouter un Rigidbody2D si nécessaire (requis pour les triggers 2D)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            Debug.LogWarning("[ProximitySoundTrigger] Rigidbody2D ajouté en mode Kinematic");
        }
        else
        {
            Debug.Log("[ProximitySoundTrigger] Rigidbody2D déjà présent");
        }
        
        // Vérifier les paramètres
        if (joueurCible == null)
        {
            Debug.LogError("[ProximitySoundTrigger] *** AUCUN JOUEUR ASSIGNÉ sur " + gameObject.name + " ! Glissez le joueur dans l'inspecteur. ***");
        }
        else
        {
            Debug.Log("[ProximitySoundTrigger] Joueur assigné: " + joueurCible.name);
        }
        
        if (sonInteraction == null)
        {
            Debug.LogError("[ProximitySoundTrigger] *** AUCUN AUDIOCLIP ASSIGNÉ sur " + gameObject.name + " ! ***");
        }
        else
        {
            Debug.Log("[ProximitySoundTrigger] Son assigné: " + sonInteraction.name);
        }
        
        Debug.LogWarning("[ProximitySoundTrigger] Configuration terminée - Touche: " + toucheInteraction + " - Rayon: " + rayonInteraction);
        Debug.Log("[ProximitySoundTrigger] Position: " + transform.position);
    }
    
    void Update()
    {
        // Vérifie si le joueur est dans la zone et appuie sur la touche
        if (joueurDansZone && !dejaUtilise)
        {
            if (Input.GetKeyDown(toucheInteraction))
            {
                Debug.LogWarning("[ProximitySoundTrigger] *** TOUCHE " + toucheInteraction + " APPUYÉE ! ***");
                JouerSon();
            }
        }
        
        // Debug continu (commentez ces lignes une fois que ça fonctionne)
        // if (Time.frameCount % 60 == 0) // Affiche chaque seconde environ
        // {
        //     Debug.Log("[ProximitySoundTrigger] État: JoueurDansZone=" + joueurDansZone + " DejaUtilise=" + dejaUtilise);
        // }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.LogWarning("[ProximitySoundTrigger] *** TRIGGER 2D DÉTECTÉ *** avec: '" + other.name + "'");
        
        // Vérifie si l'objet qui entre est le joueur cible
        if (joueurCible != null && other.gameObject == joueurCible)
        {
            joueurDansZone = true;
            joueurActuel = other.gameObject;
            
            Debug.LogWarning("[ProximitySoundTrigger] *** JOUEUR DÉTECTÉ ! *** Appuyez sur " + toucheInteraction + " pour interagir.");
            
            // Affiche un message (optionnel, nécessite un UI)
            if (!string.IsNullOrEmpty(messageProximite) && !dejaUtilise)
            {
                Debug.Log(messageProximite);
            }
        }
        else
        {
            Debug.Log("[ProximitySoundTrigger] Objet détecté mais ce n'est pas le joueur cible. Objet: " + other.name);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // Détecte la sortie du joueur de la zone
        if (joueurCible != null && other.gameObject == joueurCible)
        {
            joueurDansZone = false;
            joueurActuel = null;
            Debug.Log("[ProximitySoundTrigger] Joueur sorti de la zone.");
        }
    }
    
    void JouerSon()
    {
        Debug.LogWarning("[ProximitySoundTrigger] === TENTATIVE DE LECTURE DU SON ===");
        
        if (sonInteraction != null)
        {
            if (audioSource == null)
            {
                Debug.LogError("[ProximitySoundTrigger] AudioSource est null !");
                return;
            }
            
            // Joue le son
            audioSource.PlayOneShot(sonInteraction, volume);
            
            Debug.LogWarning("[ProximitySoundTrigger] *** SON JOUÉ : " + sonInteraction.name + " *** (Volume: " + volume + ")");
            Debug.Log("[ProximitySoundTrigger] AudioSource.isPlaying: " + audioSource.isPlaying);
            Debug.Log("[ProximitySoundTrigger] AudioSource.volume: " + audioSource.volume);
            Debug.Log("[ProximitySoundTrigger] AudioSource.mute: " + audioSource.mute);
            
            // Marque comme utilisé si non réutilisable
            if (!reutilisable)
            {
                dejaUtilise = true;
                Debug.Log("[ProximitySoundTrigger] Interaction désactivée (non réutilisable)");
            }
        }
        else
        {
            Debug.LogError("[ProximitySoundTrigger] *** ERREUR : AUCUN SON ASSIGNÉ sur " + gameObject.name + " ! ***");
        }
    }
    
    // Pour visualiser le rayon dans l'éditeur (2D)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Dessine un cercle (pour un jeu 2D)
        float angle = 0f;
        Vector3 lastPos = transform.position + new Vector3(rayonInteraction, 0, 0);
        for (int i = 0; i <= 36; i++)
        {
            angle = i * 10f * Mathf.Deg2Rad;
            Vector3 newPos = transform.position + new Vector3(Mathf.Cos(angle) * rayonInteraction, Mathf.Sin(angle) * rayonInteraction, 0);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
        }
    }
}
