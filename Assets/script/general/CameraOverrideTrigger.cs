using UnityEngine;

public class CameraOverrideTrigger : MonoBehaviour
{
    [Header("Cible Caméra")]
    public CameraFollow cameraFollowScript; // Glissez le script CameraFollow (sur la caméra) ici

    [Header("Cible du Déclencheur")]
    // Déterminez quel joueur doit entrer dans la zone pour activer le changement (Iris ou Achille)
    public string targetPlayerName = "Iris"; 
    
    // Le déclencheur ne doit s'activer qu'une seule fois
    private bool activated = false; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Vérifie si le Tag est "Player" (Achille ou Iris)
        if (other.CompareTag("Player") && !activated)
        {
            // 2. Vérifie si c'est le joueur correct (Iris) qui entre dans le trigger
            if (other.name.Contains(targetPlayerName))
            {
                if (cameraFollowScript != null)
                {
                    Debug.Log("Déclenchement du suivi unique sur " + targetPlayerName + " !");
                    
                    // Appelle la fonction de basculement dans le script de la caméra
                    cameraFollowScript.OverrideCamera(true); 
                    
                    activated = true;
                    // Désactive le Collider pour ne pas le déclencher à nouveau
                    GetComponent<Collider2D>().enabled = false;
                }
                else
                {
                    Debug.LogError("CameraOverrideTrigger manque de la référence CameraFollowScript ! Glissez la caméra dans l'inspecteur.");
                }
            }
        }
    }
}