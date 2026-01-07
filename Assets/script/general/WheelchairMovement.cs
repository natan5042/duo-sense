using UnityEngine;

public class WheelchairMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Flip Detection")]
    public Transform feetCheck;              // Point sous les pieds du fauteuil
    public Vector2 feetCheckSize = new Vector2(0.5f, 0.2f); // Taille du rectangle de détection
    public LayerMask groundLayer;            // Layer du sol

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private float initialScaleX = 1f;
    private Vector3 initialLocalScale;
    private float currentDirection = 1f; // Direction actuelle (1 = gauche, -1 = droite)
    private bool isFlipped = false;       // État de renversement
    private bool externalControl = false; // Piloté par Iris
    private float externalInput = 0f;

    [Header("Animation manuelle (Sprite Swap)")]
    public bool controlSpriteManually = true;     // active le cycle manuel
    public SpriteRenderer spriteRenderer;         // assigner le SpriteRenderer du personnage
    public Sprite[] moveCycleLeft;                // ordre: vue gauche -> gauche bras droit -> gauche bras moitié -> boucle
    public Sprite idleSpriteLeft;                 // sprite idle côté gauche
    public float moveFrameRate = 8f;              // images par seconde pendant l'avance
    private int moveFrameIndex = 0;
    private float moveFrameTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        initialLocalScale = transform.localScale;
        initialScaleX = Mathf.Abs(initialLocalScale.x);

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Si on contrôle le sprite manuellement, éviter les conflits avec l'Animator
        if (controlSpriteManually && animator != null)
        {
            animator.enabled = false;
        }
    }

    void Update()
    {
        // Vérifier si le fauteuil est renversé
        CheckIfFlipped();

        // Si renversé, désactiver les contrôles
        if (isFlipped)
        {
            movement.x = 0f;
            return;
        }

        // Déplacement horizontal - soit par entrée externe, soit via les flèches
        movement.x = 0f;
        if (externalControl)
        {
            movement.x = externalInput;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) movement.x = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) movement.x = 1f;
        }

        // Animation: soit via Animator, soit via cycle manuel des sprites
        if (!controlSpriteManually)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", Mathf.Abs(movement.x));
                animator.SetBool("IsMoving", Mathf.Abs(movement.x) > 0.01f);
            }
        }
        else
        {
            // Gestion du cycle d'images pendant l'avance
            if (spriteRenderer != null)
            {
                bool isMoving = Mathf.Abs(movement.x) > 0.01f;
                if (isMoving && moveCycleLeft != null && moveCycleLeft.Length > 0)
                {
                    moveFrameTimer += Time.deltaTime;
                    float frameDuration = 1f / Mathf.Max(1f, moveFrameRate);
                    while (moveFrameTimer >= frameDuration)
                    {
                        moveFrameTimer -= frameDuration;
                        moveFrameIndex = (moveFrameIndex + 1) % moveCycleLeft.Length;
                    }
                    spriteRenderer.sprite = moveCycleLeft[moveFrameIndex];
                }
                else
                {
                    moveFrameIndex = 0;
                    moveFrameTimer = 0f;
                    if (idleSpriteLeft != null)
                    {
                        spriteRenderer.sprite = idleSpriteLeft;
                    }
                    else if (moveCycleLeft != null && moveCycleLeft.Length > 0)
                    {
                        spriteRenderer.sprite = moveCycleLeft[0];
                    }
                }
            }
        }

        // Flip visuel suivant la direction
        if (Mathf.Abs(movement.x) > 0.01f)
        {
            currentDirection = movement.x > 0 ? -1f : 1f;
        }
        transform.localScale = new Vector3(initialScaleX * currentDirection, initialLocalScale.y, initialLocalScale.z);
    }

    void FixedUpdate()
    {
        // Si renversé, ne pas bouger
        if (isFlipped)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Pas de mouvement horizontal demandé
        if (Mathf.Abs(movement.x) < 0.01f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Distance de recherche sous les pieds (la moitié de feetCheckSize.y plus marge)
        float rayDist = (feetCheckSize.y * 0.5f) + 0.05f;
        bool onGround = false;

        if (feetCheck != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(feetCheck.position, Vector2.down, rayDist, groundLayer);
            if (hit.collider != null)
            {
                onGround = true;

                Vector2 normal = hit.normal;
                // Tangente le long de la pente (direction de déplacement)
                Vector2 tangent = new Vector2(normal.y, -normal.x).normalized;
                float dir = Mathf.Sign(movement.x);

                // Vitesse projetée le long de la pente pour que la gravité reste active
                Vector2 desiredVelocity = tangent * (dir * moveSpeed);
                rb.linearVelocity = desiredVelocity;
            }
        }

        if (!onGround)
        {
            // En l'air : conserver la gravité, ne forcer que l'axe X
            rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    void CheckIfFlipped()
    {
        if (feetCheck == null) return;

        // Vérifier si le rectangle sous les pieds touche le sol
        Collider2D groundCollider = Physics2D.OverlapBox(
            feetCheck.position,
            feetCheckSize,
            transform.eulerAngles.z, // Utiliser la rotation actuelle
            groundLayer
        );

        // Vérifier aussi la rotation - si le fauteuil est trop incliné, il est renversé
        float rotationAngle = Mathf.Abs(transform.eulerAngles.z);
        if (rotationAngle > 180f) rotationAngle = 360f - rotationAngle; // Normaliser l'angle
        
        bool isRotated = rotationAngle > 45f; // Considérer comme renversé si incliné de plus de 45 degrés
        bool noGroundContact = (groundCollider == null);

        // Le fauteuil est renversé s'il est trop incliné ET que le groundcheck ne touche plus le sol
        // (pour éviter de considérer le fauteuil comme renversé s'il est juste en l'air)
        isFlipped = isRotated && noGroundContact;
    }

    // Méthode publique pour remettre le fauteuil sur ses roues
    public void GetBackOnWheels()
    {
        if (isFlipped)
        {
            // Remettre la rotation à 0
            transform.rotation = Quaternion.identity;
            // Réinitialiser la vélocité
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // Marquer comme remis sur les roues
            isFlipped = false;
            Debug.Log("Fauteuil roulant remis sur ses roues!");
        }
    }

    // Getter pour vérifier si le fauteuil est renversé
    public bool IsFlipped()
    {
        return isFlipped;
    }

    // Dessiner le rectangle du groundcheck dans la scène pour debug
    void OnDrawGizmosSelected()
    {
        if (feetCheck != null)
        {
            Gizmos.color = isFlipped ? Color.red : Color.green;
            Gizmos.DrawWireCube(feetCheck.position, feetCheckSize);
        }
    }

    // Pilotage externe par Iris lorsqu'elle pousse
    public void SetExternalInput(float input, bool active)
    {
        externalControl = active;
        externalInput = Mathf.Clamp(input, -1f, 1f);
    }
}
