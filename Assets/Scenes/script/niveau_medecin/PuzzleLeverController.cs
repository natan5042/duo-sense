using UnityEngine;

public class PuzzleLeverController : MonoBehaviour
{
    [Header("Références scène")]
    [SerializeField] private Transform boardRoot;            // parent qui porte tous les blocs
    [SerializeField] private Transform rotationPivot;        // pivot de rotation (centre du puzzle). Si null, boardRoot est utilisé
    [SerializeField] private Rigidbody2D leverRb;            // rigidbody du levier
    [SerializeField] private Transform irisPuzzle;           // Iris placée dans la zone puzzle
    [SerializeField] private Transform irisReturnTop;        // point de retour en haut
    [SerializeField] private Transform leverTopSpawn;        // point d'apparition du levier en haut
    [SerializeField] private GameObject rampToEnable;        // rampe activée après réussite
    [SerializeField] private DoorTeleport doorTeleport;      // pour sortir du mode puzzle

    [Header("Contrôles")]
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;     // gauche (AZERTY/QWERTY)
    [SerializeField] private KeyCode rotateLeftAltKey = KeyCode.A;  // alternative si clavier remappé
    [SerializeField] private KeyCode rotateRightKey = KeyCode.D;    // droite
    [SerializeField] private float rotationStep = 90f;
    [SerializeField] private float rotationSpeed = 120f;            // rotation lente
    [SerializeField] private float customGravity = 25f;             // force projetée sur l'axe vertical local
    [SerializeField] private float minRotationInterval = 3f;        // délai mini entre deux rotations
    [SerializeField] private float unstickCheckInterval = 0.35f;    // période de vérif anti-collage
    [SerializeField] private float unstickVelocityEpsilon = 0.05f;  // seuil de vitesse considérée comme collée
    [SerializeField] private float unstickNudgeDistance = 0.05f;    // décalage appliqué pour décoller
    [SerializeField] private float unstickImpulse = 2f;             // petite impulsion après décollement

    private bool active;
    private float currentAngle;
    private float targetAngle;
    private bool isRotating;
    private float lastRotateStartTime;
    private Transform pivot; // pivot effectif
    private Transform irisOriginalParent;
    private bool irisOriginalSimulated = true;
    private RigidbodyType2D irisOriginalBodyType;
    private RigidbodyConstraints2D irisOriginalConstraints;
    private float irisOriginalGravityScale;
    private Rigidbody2D irisRb;
    private Vector2 lastGravityDir;
    private Rigidbody2D boardRb;
    private bool leverWarned;
    private float lastUnstickCheck;

    private void Awake()
    {
        EnsureLeverRb();
        SetupBoardRigidbody();
    }

    private void OnEnable()
    {
        pivot = rotationPivot != null ? rotationPivot : boardRoot;
        if (boardRoot != null)
        {
            currentAngle = boardRoot.eulerAngles.z;
            targetAngle = currentAngle;
            lastGravityDir = -(Vector2)boardRoot.up;
        }
        isRotating = false;
        lastRotateStartTime = -minRotationInterval;
    }

    public void Activate()
    {
        active = true;
        EnsureLeverRb();
        pivot = rotationPivot != null ? rotationPivot : boardRoot;
        if (boardRoot != null)
        {
            currentAngle = boardRoot.eulerAngles.z;
            targetAngle = currentAngle;
            lastGravityDir = -(Vector2)boardRoot.up;
        }
        SetupBoardRigidbody();
        isRotating = false;
        lastRotateStartTime = -minRotationInterval;

        if (leverRb != null)
        {
            leverRb.bodyType = RigidbodyType2D.Dynamic;
            leverRb.gravityScale = 0f; // gérée manuellement
            leverRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            leverRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            leverRb.constraints = RigidbodyConstraints2D.None;
            leverRb.linearVelocity = Vector2.zero;
            leverRb.angularVelocity = 0f;
            leverRb.simulated = true;
            leverRb.WakeUp();
        }

        // Parent Iris au board pour qu'elle tourne avec, et désactiver sa physique
        if (irisPuzzle != null)
        {
            irisRb = irisPuzzle.GetComponent<Rigidbody2D>();
            irisOriginalParent = irisPuzzle.parent;
            irisPuzzle.SetParent(boardRoot, true); // conserve la position monde
            if (irisRb != null)
            {
                irisOriginalBodyType = irisRb.bodyType;
                irisOriginalConstraints = irisRb.constraints;
                irisOriginalGravityScale = irisRb.gravityScale;
                irisOriginalSimulated = irisRb.simulated;
                irisRb.bodyType = RigidbodyType2D.Kinematic; // conserve les triggers
                irisRb.constraints = RigidbodyConstraints2D.FreezeAll;
                irisRb.gravityScale = 0f;
                irisRb.simulated = true;
                irisRb.linearVelocity = Vector2.zero;
                irisRb.angularVelocity = 0f;
            }
        }
    }

    public void Deactivate()
    {
        active = false;

        if (irisPuzzle != null)
        {
            if (irisReturnTop != null)
            {
                irisPuzzle.position = irisReturnTop.position;
            }

            irisPuzzle.SetParent(irisOriginalParent, true);
            RestoreIrisPhysicsAndMovement();
        }
    }

    private void Update()
    {
        if (!active || pivot == null) return;

        bool rotationInProgress = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) > 0.01f;
        isRotating = rotationInProgress;

        // On ne lance une rotation que si :
        // - rien n'est en train de tourner
        // - l'intervalle minimum depuis la dernière rotation est respecté
        bool canStartNewRotation = !rotationInProgress && (Time.time - lastRotateStartTime >= minRotationInterval);

        if (canStartNewRotation)
        {
            if (Input.GetKeyDown(rotateLeftKey) || Input.GetKeyDown(rotateLeftAltKey))
            {
                StartRotation(+1);
            }
            else if (Input.GetKeyDown(rotateRightKey))
            {
                StartRotation(-1);
            }
        }

        // Rotation douce du board autour du pivot
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        float delta = newAngle - currentAngle;
        Vector3 pivotPos = pivot != null ? pivot.position : boardRoot.position;
        boardRoot.RotateAround(pivotPos, Vector3.forward, delta);
        currentAngle = newAngle;

        // Quand on est arrivé à l'angle cible, marquer la rotation comme terminée
        if (rotationInProgress && Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= 0.01f)
        {
            isRotating = false;
        }

        // Si la direction de gravité change beaucoup, on donne un léger coup pour décoller le levier
        Vector2 newGravityDir = -(Vector2)boardRoot.up;
        if (Vector2.Dot(newGravityDir, lastGravityDir) < 0.99f && leverRb != null)
        {
            leverRb.constraints = RigidbodyConstraints2D.None;
            leverRb.linearVelocity = Vector2.zero;
            leverRb.angularVelocity = 0f;
            leverRb.position += newGravityDir * 0.02f; // léger décollement pour sortir du mur
            leverRb.AddForce(newGravityDir * (customGravity * 1.2f), ForceMode2D.Impulse);
            leverRb.WakeUp();
        }
        lastGravityDir = newGravityDir;
    }

    private void FixedUpdate()
    {
        if (!active || leverRb == null || boardRoot == null) return;

        Vector2 dir = -(Vector2)boardRoot.up; // gravité locale projetée selon l'orientation du board
        leverRb.simulated = true;
        leverRb.constraints = RigidbodyConstraints2D.None;
        leverRb.AddForce(dir * customGravity, ForceMode2D.Force);
        leverRb.WakeUp();

        // Si le levier est collé (vitesse quasi nulle) on le décolle légèrement dans la direction de la gravité
        if (Time.time - lastUnstickCheck >= unstickCheckInterval)
        {
            lastUnstickCheck = Time.time;
            if (leverRb.linearVelocity.sqrMagnitude <= unstickVelocityEpsilon * unstickVelocityEpsilon)
            {
                Vector2 nudge = dir.normalized * unstickNudgeDistance;
                leverRb.position += nudge;
                leverRb.linearVelocity = Vector2.zero;
                leverRb.angularVelocity = 0f;
                leverRb.AddForce(dir * unstickImpulse, ForceMode2D.Impulse);
                leverRb.WakeUp();
            }
        }
    }

    private void StartRotation(int direction)
    {
        lastRotateStartTime = Time.time;
        targetAngle = currentAngle + (rotationStep * direction);
        isRotating = true;

        // S'assurer que le levier n'est pas figé avant la bascule
        if (leverRb != null)
        {
            leverRb.constraints = RigidbodyConstraints2D.None;
            leverRb.linearVelocity = Vector2.zero;
            leverRb.angularVelocity = 0f;
            leverRb.WakeUp();
        }
    }

    private void EnsureLeverRb()
    {
        if (leverRb != null) return;

        leverRb = GetComponent<Rigidbody2D>();
        if (leverRb == null)
        {
            leverRb = GetComponentInChildren<Rigidbody2D>();
        }

        if (leverRb == null && !leverWarned)
        {
            leverWarned = true;
            Debug.LogWarning("[PuzzleLeverController] Aucun Rigidbody2D trouvé pour le levier. Assigne 'Lever Rb' dans l'inspector.");
        }
    }

    private void SetupBoardRigidbody()
    {
        if (boardRoot == null) return;

        if (boardRb == null)
        {
            boardRb = boardRoot.GetComponent<Rigidbody2D>();
        }

        if (boardRb == null)
        {
            boardRb = boardRoot.gameObject.AddComponent<Rigidbody2D>();
        }

        boardRb.bodyType = RigidbodyType2D.Kinematic;
        boardRb.simulated = true;
        boardRb.useFullKinematicContacts = true; // pour que les colliders en rotation poussent bien les dynamiques
        boardRb.gravityScale = 0f;
        boardRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        boardRb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Appelé par un trigger sur le levier quand il touche Iris
    public void CompletePuzzle()
    {
        if (!active) return;
        active = false;

        if (irisPuzzle != null)
        {
            if (irisReturnTop != null)
            {
                irisPuzzle.position = irisReturnTop.position;
            }

            irisPuzzle.SetParent(irisOriginalParent, true);
            RestoreIrisPhysicsAndMovement();
        }

        if (leverRb != null && leverTopSpawn != null)
        {
            leverRb.transform.position = leverTopSpawn.position;
            leverRb.transform.rotation = Quaternion.identity;
            leverRb.linearVelocity = Vector2.zero;
            leverRb.angularVelocity = 0f;
        }

        if (rampToEnable != null)
        {
            rampToEnable.SetActive(true);
        }

        if (doorTeleport != null)
        {
            doorTeleport.ExitPuzzleMode();
        }
    }

    private void RestoreIrisPhysicsAndMovement()
    {
        if (irisPuzzle == null) return;

        var rb = irisPuzzle.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.None; // reset tout
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // on libère X/Y, on fige seulement la rotation
            rb.gravityScale = irisOriginalGravityScale > 0f ? irisOriginalGravityScale : 1f;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        var move = irisPuzzle.GetComponent<PlayerMovement>();
        if (move != null) move.enabled = true;
        var chair = irisPuzzle.GetComponent<WheelchairMovement>();
        if (chair != null) chair.enabled = true;
    }
}
