using UnityEngine;

// À attacher sur chaque objet ramassable
public class PickableItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string itemId = "generic";        // Identifiant logique (optionnel)
    public bool destroyOnPickup = false;      // Détruire l'objet au ramassage
    public bool disableColliderOnPickup = true; // Mettre en Trigger pendant le port

    [Header("No Collision Options")]
    public bool noCollisionEvenWhenDropped = false;   // si vrai, l'objet est toujours en Trigger
    public bool moveToIgnoreRaycastLayerWhenHeld = true; // passe en couche Ignore Raycast pendant le port

    [Header("Audio Settings")]
    [Tooltip("Son à jouer lors du ramassage (optionnel).")]
    public AudioClip pickupSound;

    [Tooltip("Point d'attache local (si vide, centre du Transform).")]
    public Transform attachPoint;

    Collider2D[] allColliders;
    bool[] originalIsTrigger;
    Rigidbody2D rb2d;
    int originalLayer;
    RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
    float originalGravityScale = 1f;

    void Awake()
    {
        allColliders = GetComponentsInChildren<Collider2D>(true);
        if (allColliders != null && allColliders.Length > 0)
        {
            originalIsTrigger = new bool[allColliders.Length];
            for (int i = 0; i < allColliders.Length; i++)
            {
                originalIsTrigger[i] = allColliders[i].isTrigger;
            }
        }

        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            originalBodyType = rb2d.bodyType;
            originalGravityScale = rb2d.gravityScale;
        }

        originalLayer = gameObject.layer;

        if (noCollisionEvenWhenDropped)
        {
            SetAllCollidersTrigger(true);
        }
    }

    // Appelé par le script du joueur lorsqu'il ramasse
    // attachToHolder=false permet de simplement marquer l'objet comme ramassé sans l'accrocher (inventaire)
    public void OnPicked(Transform holder, bool attachToHolder = true)
    {
        // Jouer le son de ramassage si défini
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        if (disableColliderOnPickup)
        {
            // Garder détectable mais sans collisions physiques
            SetAllCollidersTrigger(true);
        }

        if (moveToIgnoreRaycastLayerWhenHeld)
        {
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0) gameObject.layer = ignoreLayer;
        }

        if (rb2d)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.gravityScale = 0f;
        }

        if (attachToHolder)
        {
            // Attacher à la main/holder
            transform.SetParent(holder);
            if (attachPoint == null)
            {
                transform.localPosition = Vector3.zero;
            }
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    // Appelé par le joueur lorsqu'il lâche
    public void OnDropped()
    {
        transform.SetParent(null);

        if (disableColliderOnPickup && !noCollisionEvenWhenDropped)
        {
            RestoreAllCollidersTrigger();
        }

        // restaurer le layer d'origine
        gameObject.layer = originalLayer;

        if (rb2d)
        {
            rb2d.bodyType = originalBodyType;
            rb2d.gravityScale = originalGravityScale;
        }
    }

    void SetAllCollidersTrigger(bool isTrigger)
    {
        if (allColliders == null) return;
        foreach (var c in allColliders)
        {
            if (c) c.isTrigger = isTrigger;
        }
    }

    void RestoreAllCollidersTrigger()
    {
        if (allColliders == null) return;
        for (int i = 0; i < allColliders.Length; i++)
        {
            var c = allColliders[i];
            if (c)
            {
                bool trig = (originalIsTrigger != null && i < originalIsTrigger.Length) ? originalIsTrigger[i] : false;
                c.isTrigger = trig;
            }
        }
    }
}
