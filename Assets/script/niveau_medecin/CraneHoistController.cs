using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// sent
/// Gère les trois boutons du mini-puzzle de la grue :
/// - Bouton 1 : monter la caisse si elle est dans la zone de prise (collider 2D trigger porté par ce GameObject).
/// - Bouton 2 : lâcher la caisse uniquement quand elle est arrivée en haut.
/// - Bouton 3 : réinitialiser la caisse après casse/essai.
/// Le script doit être posé sur la zone de prise (collider2D isTrigger) située en bas de la grue.
[RequireComponent(typeof(Collider2D))]
public class CraneHoistController : MonoBehaviour
{
    [Header("Références caisse")]
    [SerializeField] private Rigidbody2D crateRb;
    [SerializeField] private Collider2D crateCollider;
    [SerializeField] private float carryGravityScale = 0f; // gravité pendant la montée (on maintient la caisse plaquée au crochet)

    [Header("Points de déplacement")]
    [SerializeField] private Transform pickupAnchor; // où accrocher la caisse au départ (souvent le centre de la zone)
    [SerializeField] private Transform topAnchor;    // position cible en haut de la grue
    [SerializeField] private float liftDuration = 1.5f;
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useHeightOffset = false;      // si true, ignore topAnchor et monte d'une hauteur fixe
    [SerializeField] private float liftHeightOffset = 6f;       // hauteur supplémentaire depuis le pickup

    [Header("UI Boutons")]
    [SerializeField] private Button liftButton;   // bouton qui monte la caisse (vert)
    [SerializeField] private Button dropButton;   // bouton qui lâche la caisse quand elle est en haut (jaune)
    [SerializeField] private Button resetButton;  // bouton qui remet la caisse en bas (rouge)

    [Header("Interaction caisse cassée")]
    [SerializeField] private CrateBreakAndRamp breakHandler;

    private Collider2D pickupZone;
    private bool crateInZone;
    private bool isLifting;
    private bool atTop;
    private bool crateBroken;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private RigidbodyType2D originalBodyType;
    private RigidbodyConstraints2D originalConstraints;
    private float originalGravity;
    private Coroutine liftRoutine;
    private Vector2 lastLiftTarget;
    [SerializeField] private float dropProximityTolerance = 0.35f; // tolérance pour autoriser le drop si proche du point haut

    private void Awake()
    {
        pickupZone = GetComponent<Collider2D>();
        if (pickupZone != null)
        {
            pickupZone.isTrigger = true;
        }

        CacheOriginalPhysics();
        initialPosition = crateRb != null ? crateRb.position : Vector2.zero;
        initialRotation = crateRb != null ? crateRb.transform.rotation : Quaternion.identity;

        HookButtons();
    }

    private void OnEnable()
    {
        UpdateButtons();
    }

    private void OnDisable()
    {
        UnhookButtons();
    }

    private void HookButtons()
    {
        if (liftButton != null) liftButton.onClick.AddListener(TryLift);
        if (dropButton != null) dropButton.onClick.AddListener(Drop);
        if (resetButton != null) resetButton.onClick.AddListener(ResetCrate);
    }

    private void UnhookButtons()
    {
        if (liftButton != null) liftButton.onClick.RemoveListener(TryLift);
        if (dropButton != null) dropButton.onClick.RemoveListener(Drop);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetCrate);
    }

    private void CacheOriginalPhysics()
    {
        if (crateRb == null) return;
        originalBodyType = crateRb.bodyType;
        originalConstraints = crateRb.constraints;
        originalGravity = crateRb.gravityScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (crateRb == null) return;
        if (other.attachedRigidbody != crateRb) return;

        crateInZone = true;
        UpdateButtons();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (crateRb == null) return;
        if (other.attachedRigidbody != crateRb) return;

        crateInZone = false;
        UpdateButtons();
    }

    public void TryLift()
    {
        if (crateRb == null || topAnchor == null) return;
        if (!crateInZone || isLifting || atTop || crateBroken) return;

        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = StartCoroutine(LiftRoutine());
    }

    private IEnumerator LiftRoutine()
    {
        isLifting = true;
        atTop = false;
        crateBroken = breakHandler != null && breakHandler.IsBroken;
        UpdateButtons();

        PrepareCrateForCarry();

        Vector2 start = pickupAnchor != null ? (Vector2)pickupAnchor.position : crateRb.position;
        Vector2 end = topAnchor != null ? (Vector2)topAnchor.position : start;

        if (useHeightOffset)
        {
            end = start + Vector2.up * liftHeightOffset;
        }

        lastLiftTarget = end;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, liftDuration);
            float eased = liftCurve.Evaluate(Mathf.Clamp01(t));
            Vector2 nextPos = Vector2.Lerp(start, end, eased);
            crateRb.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }

        crateRb.MovePosition(end);
        isLifting = false;
        atTop = true;
        crateInZone = false;
        UpdateButtons();
    }

    private void PrepareCrateForCarry()
    {
        CacheOriginalPhysics();
        if (crateRb == null) return;

        crateRb.bodyType = RigidbodyType2D.Kinematic;
        crateRb.gravityScale = carryGravityScale;
        crateRb.linearVelocity = Vector2.zero;
        crateRb.angularVelocity = 0f;
        crateRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        crateRb.simulated = true;

        if (crateCollider != null)
        {
            crateCollider.enabled = true;
            crateCollider.isTrigger = false;
        }
    }

    public void Drop()
    {
        if (crateRb == null) return;
        if (isLifting || crateBroken) return;

        bool nearTop = (crateRb.position - lastLiftTarget).sqrMagnitude <= dropProximityTolerance * dropProximityTolerance;
        if (!atTop && !nearTop) return; // refuse si trop bas

        RestorePhysics();
        crateRb.position = topAnchor != null ? (Vector2)topAnchor.position : crateRb.position;
        crateRb.linearVelocity = Vector2.zero;
        crateRb.angularVelocity = 0f;

        if (breakHandler != null)
        {
            breakHandler.ResetCrateVisual();
            breakHandler.ArmBreakOnFall(); // arme la casse pour que l'impact après drop détruise la caisse
            Debug.Log("[CraneHoist] Drop: armed crate for break");
        }

        atTop = false;
        crateInZone = false;
        UpdateButtons();
    }

    private void RestorePhysics()
    {
        if (crateRb == null) return;

        crateRb.bodyType = originalBodyType;
        crateRb.constraints = originalConstraints;
        crateRb.gravityScale = originalGravity;
        crateRb.simulated = true;
    }

    public void NotifyCrateBroken()
    {
        crateBroken = true;
        atTop = false;
        isLifting = false;
        crateInZone = false;
        UpdateButtons();
        Debug.Log("[CraneHoist] Crate reported broken");
    }

    public void ResetCrate()
    {
        if (crateRb == null) return;

        if (liftRoutine != null)
        {
            StopCoroutine(liftRoutine);
            liftRoutine = null;
        }

        crateBroken = false;
        isLifting = false;
        atTop = false;
        crateInZone = false;

        RestorePhysics();
        crateRb.linearVelocity = Vector2.zero;
        crateRb.angularVelocity = 0f;
        crateRb.transform.SetPositionAndRotation(initialPosition, initialRotation);

        if (crateCollider != null)
        {
            crateCollider.enabled = true;
            crateCollider.isTrigger = false;
        }

        if (breakHandler != null)
        {
            breakHandler.ResetCrateVisual();
            breakHandler.DisarmBreak();
            Debug.Log("[CraneHoist] Reset: disarmed crate");
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (liftButton != null)
        {
            liftButton.interactable = crateInZone && !isLifting && !atTop && !crateBroken;
            liftButton.gameObject.SetActive(true);
        }

        if (dropButton != null)
        {
            dropButton.interactable = atTop && !isLifting && !crateBroken;
            dropButton.gameObject.SetActive(true);
        }

        if (resetButton != null)
        {
            resetButton.interactable = crateBroken || (!crateInZone && !isLifting && !atTop);
            resetButton.gameObject.SetActive(true);
        }
    }
}
