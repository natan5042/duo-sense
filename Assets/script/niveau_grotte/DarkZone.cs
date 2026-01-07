using UnityEngine;
using System.Collections;

public class DarkZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifie si le joueur (objet avec le Tag "Player") entre
        if (other.CompareTag("Player"))
        {
            // Appelle le UIManager permanent pour lancer le fondu au noir
            if (UIManager.Instance != null)
            {
                UIManager.Instance.StartFade(1f); // Noir complet (alpha 1)
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Vérifie si le joueur sort
        if (other.CompareTag("Player"))
        {
            // Appelle le UIManager permanent pour lancer le fondu transparent
            if (UIManager.Instance != null)
            {
                UIManager.Instance.StartFade(0f); // Transparent (alpha 0)
            }
        }
    }
}