using UnityEngine;

// Script de debug pour tester les triggers 2D
// Attachez ce script à un objet et il affichera tous les objets qui entrent en collision
public class TestTriggerDebug : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning("=== [TestTriggerDebug] Script actif sur: " + gameObject.name + " ===");
        
        // Affiche tous les composants
        Component[] components = GetComponents<Component>();
        Debug.Log("[TestTriggerDebug] Composants:");
        foreach (Component comp in components)
        {
            Debug.Log("  - " + comp.GetType().Name);
        }
        
        // Vérifie le Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log("[TestTriggerDebug] Rigidbody2D: BodyType=" + rb.bodyType + ", GravityScale=" + rb.gravityScale);
        }
        else
        {
            Debug.LogWarning("[TestTriggerDebug] Pas de Rigidbody2D trouvé");
        }
        
        // Vérifie les colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length > 0)
        {
            Debug.Log("[TestTriggerDebug] Colliders 2D trouvés: " + colliders.Length);
            foreach (Collider2D col in colliders)
            {
                Debug.Log("  - " + col.GetType().Name + " (IsTrigger: " + col.isTrigger + ")");
            }
        }
        else
        {
            Debug.LogWarning("[TestTriggerDebug] Aucun Collider2D trouvé");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.LogWarning("[TestTriggerDebug] *** TRIGGER ENTER *** avec: " + other.name + " (Tag: '" + other.tag + "')");
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        Debug.LogWarning("[TestTriggerDebug] *** TRIGGER EXIT *** avec: " + other.name + " (Tag: '" + other.tag + "')");
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.LogWarning("[TestTriggerDebug] *** COLLISION ENTER *** avec: " + collision.gameObject.name);
    }
}
