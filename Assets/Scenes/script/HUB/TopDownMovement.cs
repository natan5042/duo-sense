using UnityEngine;

// Déplacement 2D vue du dessus (top-down) sans gravité.
// Attache ce script sur chaque personnage (Iris, Achille/fauteuil).
// Choisis des touches différentes pour chacun si besoin.
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMovement : MonoBehaviour
{
    [Header("Touches de contrôle")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Déplacement")]
    public float moveSpeed = 5f;            // Vitesse max (appliquée instantanément)
    public bool snapZToZero = true;         // Force Z=0 pour rester en 2D
    public bool freezeRotation = true;      // Empêche la rotation physique

    [Header("Animator (optionnel)")]
    public Animator animator;               // Si utilisé: paramètres MoveX/MoveY/Speed
    public bool useAnimatorFacing = true;   // Oriente via Animator (4 directions)
    public string animatorDirectionParam = "Direction"; // int: 0=Bas,1=Gauche,2=Droite,3=Haut
    public bool rotateTransformIfNoAnimator = true; // Oriente le Transform si pas d'Animator
    [Tooltip("Décalage de rotation en degrés pour aligner le sprite (ex: -90 ou +90 si les directions sont décalées)")]
    public float rotationOffsetDegrees = 0f;

    [Header("Animation manuelle (Sprite Swap, comme WheelchairMovement)")]
    [Tooltip("Si true, cycle de sprites à la place de l'Animator (comme en grotte)")]
    public bool useSpriteCycleAnimation = false;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Sprites du cycle en mouvement (ordre de lecture)")]
    public Sprite[] moveCycle;
    [Tooltip("Sprite à l'arrêt (optionnel, sinon premier de moveCycle)")]
    public Sprite idleSprite;
    public float moveFrameRate = 8f;

    private Rigidbody2D rb;
    private Vector2 currentInput;
    private Vector2 lastFacing = Vector2.down; // Orientation par défaut
    private int moveFrameIndex;
    private float moveFrameTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Pas de gravité en top-down
        rb.linearDamping = 0f;         // Pas de frottement artificiel
        if (freezeRotation)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // évite la rotation lors des collisions
    }

    void Start()
    {
        if (useSpriteCycleAnimation)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator != null) animator.enabled = false; // Éviter conflit avec le cycle manuel
        }
    }

    void Update()
    {
        currentInput = ReadInput();

        if (currentInput.sqrMagnitude > 0f)
        {
            lastFacing = currentInput.normalized;
        }

        UpdateFacing();

        if (useSpriteCycleAnimation)
        {
            UpdateSpriteCycle();
            return;
        }

        if (animator)
        {
            animator.SetFloat("MoveX", currentInput.x);
            animator.SetFloat("MoveY", currentInput.y);
            animator.SetFloat("Speed", currentInput.sqrMagnitude);
        }
    }

    void UpdateSpriteCycle()
    {
        if (spriteRenderer == null) return;
        bool isMoving = currentInput.sqrMagnitude > 0.01f;
        if (isMoving && moveCycle != null && moveCycle.Length > 0)
        {
            moveFrameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, moveFrameRate);
            while (moveFrameTimer >= frameDuration)
            {
                moveFrameTimer -= frameDuration;
                moveFrameIndex = (moveFrameIndex + 1) % moveCycle.Length;
            }
            spriteRenderer.sprite = moveCycle[moveFrameIndex];
        }
        else
        {
            moveFrameIndex = 0;
            moveFrameTimer = 0f;
            if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;
            else if (moveCycle != null && moveCycle.Length > 0)
                spriteRenderer.sprite = moveCycle[0];
        }
    }

    void FixedUpdate()
    {
        if (currentInput.sqrMagnitude > 0f)
        {
            rb.linearVelocity = currentInput.normalized * moveSpeed; // Vmax immédiate
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Arrêt instantané
        }

        if (snapZToZero)
        {
            var p = transform.position;
            if (p.z != 0f)
                transform.position = new Vector3(p.x, p.y, 0f);
        }
    }

    // Met à jour l'orientation visuelle (Animator ou rotation)
    private void UpdateFacing()
    {
        // Choix d'une direction cardinale dominante
        Vector2 d = lastFacing;
        int dirIndex; // 0=Bas,1=Gauche,2=Droite,3=Haut
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
        {
            dirIndex = d.x < 0 ? 1 : 2;
        }
        else
        {
            dirIndex = d.y < 0 ? 0 : 3;
        }

        if (animator && useAnimatorFacing)
        {
            animator.SetInteger(animatorDirectionParam, dirIndex);
            animator.SetFloat("MoveX", currentInput.x);
            animator.SetFloat("MoveY", currentInput.y);
            animator.SetFloat("Speed", currentInput.sqrMagnitude);
            return;
        }

        if (!animator && rotateTransformIfNoAnimator)
        {
            float angle = Mathf.Atan2(lastFacing.y, lastFacing.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private Vector2 ReadInput()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(leftKey))  x -= 1f;
        if (Input.GetKey(rightKey)) x += 1f;
        if (Input.GetKey(downKey))  y -= 1f;
        if (Input.GetKey(upKey))    y += 1f;
        return new Vector2(x, y);
    }
}