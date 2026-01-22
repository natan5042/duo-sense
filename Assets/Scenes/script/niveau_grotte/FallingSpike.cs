using UnityEngine;
using UnityEngine.SceneManagement;

public class FallingSpike : MonoBehaviour
{
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private bool isFalling = false;
    private AudioSource audioSource; // AJOUT AUDIO 1

    [Header("Vitesse de chute")]
    public float gravityScaleWhenFalling = 3f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>(); // AJOUT AUDIO 2 : On récupère le composant
        startPosition = transform.position;
        rb.gravityScale = 0;
    }

    public void Drop()
    {
        if (!isFalling)
        {
            isFalling = true;
            rb.gravityScale = gravityScaleWhenFalling;
            rb.bodyType = RigidbodyType2D.Dynamic;

            // AJOUT AUDIO 3 : Jouer le son
            if (audioSource != null && audioSource.clip != null)
            {
                // On change légèrement le pitch (la hauteur) pour que tous les pics 
                // n'aient pas exactement le même son robotique
                audioSource.pitch = Random.Range(0.9f, 1.1f); 
                audioSource.Play();
            }
        }
    }

    public void ResetPosition()
    {
        isFalling = false;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = startPosition;
        // On ne coupe pas le son ici, on laisse le bruit de chute se terminer naturellement
    }

 // Dans FallingSpike.cs

void OnTriggerEnter2D(Collider2D other)
{
    // Si le pic touche le joueur (Tag "Player" requis)
    if (other.CompareTag("Player"))
    {
        Debug.Log("GAME OVER !");
        
        // C'EST CECI QUI GARANTIT LA PROPRETÉ DE LA TRANSITION :
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HandleGameOver();
        }
        else
        {
            // Fallback si le UIManager n'est pas là
            Debug.LogError("UIManager.Instance est null. Rechargement direct.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
}