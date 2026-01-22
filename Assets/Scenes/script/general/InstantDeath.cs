using UnityEngine;
using UnityEngine.SceneManagement; // Nécessaire pour gérer le rechargement de scène

public class InstantDeath : MonoBehaviour
{
    // Important : assurez-vous que l'objet qui porte ce script a un Collider 2D avec 'Is Trigger' coché.

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. On vérifie si l'objet qui entre en collision est le joueur.
        //    (Pour cela, vos personnages Achille et Iris doivent avoir le Tag "Player").
        if (other.CompareTag("Player"))
        {
            Debug.Log("Game Over! L'objet " + gameObject.name + " a touché le joueur.");

            // 2. On exécute le Game Over en rechargeant la scène actuelle.
            //    SceneManager.GetActiveScene().name récupère le nom de la scène en cours.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}