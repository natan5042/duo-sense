using System;
using UnityEngine;

/// <summary>
/// Script unique pour gérer la pose d'affiche. À attacher sur l'objet 2D transparent.
/// Détecte automatiquement le joueur et gère l'interaction.
/// </summary>
public class AfficheInteractive : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le tag du joueur (par défaut 'Player')")]
    public string tagJoueur = "Player";
    
    [Tooltip("La touche pour poser l'affiche")]
    public KeyCode toucheInteraction = KeyCode.E;
    
    [Tooltip("Le rayon dans lequel le joueur peut poser l'affiche")]
    public float rayonInteraction = 2.5f;
    
    [Tooltip("Le sprite de l'affiche à poser (à assigner dans l'Inspector)")]
    public Sprite spriteAffiche;
    
    [Header("Visuel")]
    [Tooltip("Couleur quand l'emplacement est vide (transparent par défaut)")]
    public Color couleurVide = new Color(1, 1, 1, 0.1f);
    
    [Tooltip("Couleur quand l'affiche est posée (opaque)")]
    public Color couleurPleine = new Color(1, 1, 1, 1f);
    
    [Header("UI Optionnel")]
    [Tooltip("Texte à afficher quand le joueur peut interagir")]
    public string messageInteraction = "Appuyez sur E pour poser l'affiche";

    public event Action<AfficheInteractive> onAffichePosee;
    public bool EstPosee => affichePositionnee;
    
    private SpriteRenderer spriteRenderer;
    private bool affichePositionnee = false;
    private Transform joueur;
    private bool joueurDansRayon = false;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("AfficheInteractive : Aucun SpriteRenderer trouvé sur " + gameObject.name);
            enabled = false;
            return;
        }
        
        // Trouver le joueur
        GameObject joueurObj = GameObject.FindGameObjectWithTag(tagJoueur);
        if (joueurObj != null)
        {
            joueur = joueurObj.transform;
        }
        else
        {
            Debug.LogWarning("AfficheInteractive : Aucun objet avec le tag '" + tagJoueur + "' trouvé.");
        }
        
        // Initialiser l'apparence
        spriteRenderer.color = couleurVide;
    }
    
    void Update()
    {
        // Si l'affiche est déjà posée, ne rien faire
        if (affichePositionnee || joueur == null)
            return;
        
        // Vérifier la distance avec le joueur
        float distance = Vector3.Distance(transform.position, joueur.position);
        joueurDansRayon = distance <= rayonInteraction;
        
        // Si le joueur est dans le rayon et appuie sur la touche
        if (joueurDansRayon && Input.GetKeyDown(toucheInteraction))
        {
            PoserAffiche();
        }
    }
    
    /// <summary>
    /// Pose l'affiche sur l'emplacement
    /// </summary>
    void PoserAffiche()
    {
        if (spriteAffiche == null)
        {
            Debug.LogWarning("Aucun sprite d'affiche assigné sur " + gameObject.name);
            return;
        }
        
        // Changer le sprite et la couleur
        spriteRenderer.sprite = spriteAffiche;
        spriteRenderer.color = couleurPleine;
        affichePositionnee = true;

        onAffichePosee?.Invoke(this);
        
        Debug.Log("Affiche posée avec succès sur " + gameObject.name);
    }
    
    /// <summary>
    /// Affiche un message dans la console quand le joueur peut interagir
    /// </summary>
    void OnGUI()
    {
        if (!affichePositionnee && joueurDansRayon && joueur != null)
        {
            // Afficher un message à l'écran
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30), 
                      messageInteraction, 
                      new GUIStyle() { 
                          alignment = TextAnchor.MiddleCenter, 
                          normal = new GUIStyleState() { textColor = Color.white },
                          fontSize = 14
                      });
        }
    }
    
    /// <summary>
    /// Visualiser le rayon d'interaction dans l'éditeur Unity
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = affichePositionnee ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rayonInteraction);
        
        if (!affichePositionnee && joueur != null)
        {
            float distance = Vector3.Distance(transform.position, joueur.position);
            if (distance <= rayonInteraction)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, joueur.position);
            }
        }
    }
}
