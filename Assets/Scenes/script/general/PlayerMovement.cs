using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;           // point juste sous les pieds
    public float groundCheckRadius = 0.2f;  // rayon du GroundCheck
    public float groundRayLength = 0.25f;   // longueur du rayon de secours
    public LayerMask groundLayer;           // Layer du sol

    [Header("Help System")]
    public WheelchairMovement wheelchair;   // Référence au fauteuil roulant
    public float helpDistance = 2f;         // Distance pour pouvoir aider
    public KeyCode helpKey = KeyCode.E;     // Touche pour aider

    [Header("Push Wheelchair")]
    public KeyCode pushKey = KeyCode.S;     // Touche pour s'accrocher et pousser
    public float pushDistance = 1.2f;       // Distance max pour s'accrocher
    public float attachBackOffset = 0.7f;   // Décalage de base derrière le fauteuil
    public float attachYOffset = 0.1f;      // Ajustement vertical (positif = remonter les pieds)
    public float pushExtraBack = 0.45f;     // Distance supplémentaire pour rester bien derrière

    [Header("Collider sync")]
    public bool matchWheelchairColliderToPlayer = true; // Aligner temporairement les colliders

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isGrounded;
    private Animator animator;
    private float initialScaleX = 1f;
    private Vector3 initialLocalScale;
    private float currentDirection = 1f; // Direction actuelle (1 = gauche, -1 = droite)
    private bool isPushing = false;
    private Vector2 desiredAttachPosition;
    private Collider2D[] playerColliders;
    private Collider2D[] wheelchairColliders;
    private Animator wheelchairAnimator;
    private BoxCollider2D playerMainBox;
    private BoxCollider2D wheelchairMainBox;
    private Vector2 wheelchairOrigSize;
    private Vector2 wheelchairOrigOffset;
    private bool collisionsIgnored = false;
    private float lastInputX = -1f;
    private bool groundCheckLogged = false;
    
    [Header("Debug")]
    public bool debugJump = false;

    [Header("Animation manuelle (Sprite Swap)")]
    public bool controlSpriteManually = false;    // peut être activé si nécessaire
    public SpriteRenderer spriteRenderer;         // assigner le SpriteRenderer du personnage
    public Sprite[] moveCycleLeft;                // sprites d'animation de déplacement
    public Sprite idleSpriteLeft;                 // sprite idle côté gauche
    public float moveFrameRate = 8f;              // images par seconde pendant l'avance
    private int moveFrameIndex = 0;
    private float moveFrameTimer = 0f;

	[Header("Ladder")]
    public float climbSpeed = 4f;
    public LayerMask ladderLayer;

    private bool isInLadderZone = false;
    private bool isClimbing = false;
    private float defaultGravity;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Проверка по слою вместо тега
        if (((1 << other.gameObject.layer) & ladderLayer) != 0)
        {
            isInLadderZone = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & ladderLayer) != 0)
        {
            isInLadderZone = false;
            StopClimbing();
        }
    }

    void StartClimbing()
    {
        if (isClimbing) return;

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    void StopClimbing()
    {
        if (!isClimbing) return;

        isClimbing = false;
        rb.gravityScale = defaultGravity;
    }



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

        playerColliders = GetComponentsInChildren<Collider2D>();
        playerMainBox = GetComponentInChildren<BoxCollider2D>();

        // Si on contrôle le sprite manuellement, éviter les conflits avec l'Animator
        if (controlSpriteManually && animator != null)
        {
            animator.enabled = false;
        }
    }

    void Update()
    {
			  // Лестница: включается ТОЛЬКО при нажатии W
        if (isInLadderZone && Input.GetKey(KeyCode.W))
        {
            StartClimbing();
        }
        else if (isClimbing && !Input.GetKey(KeyCode.W))
        {
            StopClimbing();
        }
		
		
        // Déplacement horizontal - avec ZQSD (clavier AZERTY)
        float inputX = 0f;
        if (Input.GetKey(KeyCode.A)) inputX = -1f;  // Q sur clavier AZERTY
        if (Input.GetKey(KeyCode.D)) inputX = 1f;   // D sur clavier AZERTY

        if (Mathf.Abs(inputX) > 0.01f)
        {
            lastInputX = inputX;
        }

        if (!isPushing)
        {
            movement.x = inputX;
        }
        else
        {
            movement.x = 0f; // Iris ne se déplace pas seule lorsqu'elle pousse
        }

        // Vérifier si le joueur touche le sol
        bool groundedByCheck = false;
        if (groundCheck != null)
        {
            groundedByCheck = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else if (!groundCheckLogged)
        {
            Debug.LogWarning("[PlayerMovement] groundCheck n'est pas assigné dans l'Inspector.");
            groundCheckLogged = true;
        }

        bool groundedByCollider = false;
        if (playerColliders != null)
        {
            foreach (var col in playerColliders)
            {
                if (col != null && col.IsTouchingLayers(groundLayer))
                {
                    groundedByCollider = true;
                    break;
                }
            }
        }

        // Raycast de secours vers le bas
        bool groundedByRay = false;
        Vector2 rayOrigin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayLength, groundLayer);
        if (hit.collider != null)
        {
            groundedByRay = true;
        }

        isGrounded = groundedByCheck || groundedByCollider || groundedByRay;

        // Saut avec Z ou Espace (désactivé si elle pousse le fauteuil)
        if (!isPushing)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                if (isGrounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    if (debugJump)
                    {
                        Debug.Log($"[Jump] Triggered. groundedByCheck={groundedByCheck} groundedByCollider={groundedByCollider} groundedByRay={groundedByRay} jumpForce={jumpForce}");
                    }
                }
                else if (debugJump)
                {
                    Debug.Log($"[Jump] Ignored (not grounded). groundedByCheck={groundedByCheck} groundedByCollider={groundedByCollider} groundedByRay={groundedByRay}");
                }
            }
        }

        // Aider le fauteuil roulant s'il est renversé
        if (Input.GetKeyDown(helpKey) && wheelchair != null)
        {
            TryHelpWheelchair();
        }

        // Accroche / pousse du fauteuil
        if (Input.GetKeyDown(pushKey))
        {
            if (isPushing)
            {
                StopPush();
            }
            else
            {
                TryStartPush();
            }
        }

        // Animation: soit via Animator, soit via cycle manuel des sprites
        if (!controlSpriteManually)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", Mathf.Abs(movement.x));
                animator.SetBool("IsMoving", Mathf.Abs(movement.x) > 0.01f);
                animator.SetBool("IsGrounded", isGrounded);
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

        // Flip visuel : on ne modifie le flip que si un input existe
        if (!isPushing)
        {
            if (Mathf.Abs(movement.x) > 0.01f)
            {
                currentDirection = movement.x > 0 ? -1f : 1f;
            }
        }
        else
        {
            if (Mathf.Abs(lastInputX) > 0.01f)
            {
                currentDirection = lastInputX > 0 ? -1f : 1f;
            }
        }

        transform.localScale = new Vector3(initialScaleX * currentDirection, initialLocalScale.y, initialLocalScale.z);

        // Maintenir la position si on pousse
        if (isPushing)
        {
            MaintainPushPosition();
            // Transmet l'input horizontal pour piloter le fauteuil
            if (wheelchair != null)
            {
                wheelchair.SetExternalInput(inputX, true);
            }
        }
        else if (wheelchair != null)
        {
            wheelchair.SetExternalInput(0f, false);
        }
    }

    void FixedUpdate()
    {
		
		if (isClimbing)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    movement.y * climbSpeed
                );
                return;
            }

        if (rb == null) return;
		
		
        if (isPushing)
        {
            // Iris est “collée” : pas de déplacement propre
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            // Suivre la position attachée via la physique pour respecter les collisions
            rb.MovePosition(desiredAttachPosition);
        }
        else
        {
            // Déplacement horizontal
            rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    void TryStartPush()
    {
        if (wheelchair == null) return;

        float dist = Vector2.Distance(transform.position, wheelchair.transform.position);
        if (dist > pushDistance)
        {
            Debug.Log($"[Push] Trop loin pour s'accrocher (dist={dist:F2} > {pushDistance})");
            return;
        }

        EnsureWheelchairColliders();
        SetCollisionsIgnored(true);

        // Désactiver l'animation d'Achille pendant que Iris pousse
        if (wheelchairAnimator != null)
        {
            wheelchairAnimator.enabled = false;
        }

        // Aligner le collider du fauteuil sur celui d'Iris (même taille, même position relative derrière)
        if (matchWheelchairColliderToPlayer && playerMainBox != null && wheelchairMainBox != null)
        {
            float facing = Mathf.Sign(wheelchair.transform.localScale.x);
            float back = attachBackOffset + pushExtraBack;

            wheelchairMainBox.size = playerMainBox.size;
            // On place le collider du fauteuil là où se trouve Iris quand elle est accrochée
            wheelchairMainBox.offset = new Vector2(facing * back, attachYOffset + playerMainBox.offset.y);
            wheelchairMainBox.enabled = true;
        }

        isPushing = true;
        Debug.Log("[Push] Iris s'accroche et prend le contrôle du fauteuil.");
    }

    void StopPush()
    {
        if (!isPushing) return;
        isPushing = false;
        SetCollisionsIgnored(false);

        // Réactiver l'animation d'Achille
        if (wheelchairAnimator != null)
        {
            wheelchairAnimator.enabled = true;
        }

        // Restaurer le collider du fauteuil
        if (matchWheelchairColliderToPlayer && wheelchairMainBox != null)
        {
            wheelchairMainBox.size = wheelchairOrigSize;
            wheelchairMainBox.offset = wheelchairOrigOffset;
        }
        if (wheelchair != null)
        {
            wheelchair.SetExternalInput(0f, false);
        }
        Debug.Log("[Push] Iris lâche le fauteuil.");
    }

    void MaintainPushPosition()
    {
        if (wheelchair == null)
        {
            StopPush();
            return;
        }

        float dist = Vector2.Distance(transform.position, wheelchair.transform.position);
        if (dist > pushDistance * 1.6f)
        {
            Debug.Log($"[Push] Détaché (dist trop grande {dist:F2})");
            StopPush();
            return;
        }

        float facing = Mathf.Sign(wheelchair.transform.localScale.x);
        float back = attachBackOffset + pushExtraBack;
        // Place Iris derrière le fauteuil suivant son facing réel
        Vector3 offset = new Vector3(facing * back, attachYOffset, 0f);
        desiredAttachPosition = wheelchair.transform.position + offset;
    }

    void EnsureWheelchairColliders()
    {
        if (wheelchair == null) return;
        if (wheelchairColliders == null || wheelchairColliders.Length == 0)
        {
            wheelchairColliders = wheelchair.GetComponentsInChildren<Collider2D>();
        }
        if (wheelchairAnimator == null)
        {
            wheelchairAnimator = wheelchair.GetComponentInChildren<Animator>();
        }
        if (wheelchairMainBox == null)
        {
            wheelchairMainBox = wheelchair.GetComponentInChildren<BoxCollider2D>();
            if (wheelchairMainBox != null)
            {
                wheelchairOrigSize = wheelchairMainBox.size;
                wheelchairOrigOffset = wheelchairMainBox.offset;
            }
        }
    }

    void SetCollisionsIgnored(bool ignore)
    {
        if (collisionsIgnored == ignore) return;
        if (playerColliders == null || playerColliders.Length == 0) return;
        EnsureWheelchairColliders();
        if (wheelchairColliders == null || wheelchairColliders.Length == 0) return;

        foreach (var pc in playerColliders)
        {
            if (pc == null) continue;
            foreach (var wc in wheelchairColliders)
            {
                if (wc == null) continue;
                Physics2D.IgnoreCollision(pc, wc, ignore);
            }
        }

        collisionsIgnored = ignore;
    }

    void TryHelpWheelchair()
    {
        if (wheelchair == null) return;

        // Vérifier si le fauteuil est renversé
        if (!wheelchair.IsFlipped())
        {
            return; // Le fauteuil n'est pas renversé
        }

        // Vérifier la distance entre le joueur et le fauteuil
        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(wheelchair.transform.position.x, wheelchair.transform.position.y)
        );

        if (distance <= helpDistance)
        {
            // Aider le fauteuil à se remettre sur ses roues
            wheelchair.GetBackOnWheels();
            Debug.Log("Joueur aveugle aide le fauteuil roulant!");
        }
        else
        {
            Debug.Log("Trop loin pour aider le fauteuil roulant!");
        }
    }

    // Dessiner le cercle du GroundCheck dans la scène pour debug
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Dessiner la zone d'aide si le fauteuil est renversé
        if (wheelchair != null && wheelchair.IsFlipped())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wheelchair.transform.position, helpDistance);
            Gizmos.DrawLine(transform.position, wheelchair.transform.position);
        }
    }
}
