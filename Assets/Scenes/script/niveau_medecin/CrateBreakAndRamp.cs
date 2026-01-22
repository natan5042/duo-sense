using UnityEngine;

/// sent
/// Placé sur la caisse : détecte un impact fort, "casse" la caisse et fait apparaître la rampe cachée à l'intérieur.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CrateBreakAndRamp : MonoBehaviour
{
    [Header("Réglages de casse")]
    [SerializeField] private float breakVelocity = 6f;           // vitesse relative minimale pour déclencher la casse
    [SerializeField] private float requiredDownwardSpeed = 3f;    // vitesse verticale descendante minimale
    [SerializeField] private bool requireArmForBreak = false;      // par défaut casse libre; si true il faut un drop pour armer
    [SerializeField] private bool destroySpawnedRampOnReset = true;
    [SerializeField] private LayerMask breakLayers = ~0;          // couches autorisées pour casser (par défaut tout)

    [Header("Rampe à révéler")]
    [SerializeField] private GameObject rampPrefab;               // si fourni, instancié à la casse
    [SerializeField] private Transform rampSpawnPoint;            // position pour la rampe
    [SerializeField] private GameObject rampToEnable;             // alternative : activer une rampe déjà placée (optionnel)

    [Header("Feedbacks visuels/sonores")]
    [SerializeField] private ParticleSystem breakFx;
    [SerializeField] private AudioSource breakAudio;

    [Header("Communication grue")]
    [SerializeField] private CraneHoistController hoistController;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private bool broken;
    private bool armedForFall;
    private GameObject spawnedRamp;

    public bool IsBroken => broken;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float impact = collision.relativeVelocity.magnitude;
        Debug.Log($"[CrateBreak] Collision layer={collision.collider.gameObject.layer} impact={impact:F2} armed={armedForFall} broken={broken}");
        TryBreakOnContact(collision.collider.gameObject.layer, impact);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[CrateBreak] Trigger layer={other.gameObject.layer} armed={armedForFall} broken={broken}");
        TryBreakOnContact(other.gameObject.layer, 0f);
    }

    private void TryBreakOnContact(int otherLayer, float impactMagnitude)
    {
        if (broken) return;

        // Si armé après un drop, on casse immédiatement au premier contact (on ignore le filtre de couche pour garantir la casse)
        if (armedForFall)
        {
            Break();
            return;
        }

        // Si l'on impose l'armement et qu'on n'est pas armé, on applique le filtre de couche + seuils classiques
        if (requireArmForBreak && !armedForFall)
        {
            if (!IsLayerAllowed(otherLayer)) return;
        }
        else
        {
            // casse libre : pas de filtre de couche
        }

        float verticalSpeed = rb != null ? rb.linearVelocity.y : 0f;
        bool downwardHit = verticalSpeed <= -requiredDownwardSpeed;
        bool strongImpact = impactMagnitude >= breakVelocity;

        if (downwardHit || strongImpact)
        {
            Break();
        }
    }

    private bool IsLayerAllowed(int layer)
    {
        return ((1 << layer) & breakLayers.value) != 0;
    }

    private void Break()
    {
        Debug.Log("[CrateBreak] BREAK triggered");
        broken = true;

        if (breakFx != null)
        {
            Instantiate(breakFx, transform.position, Quaternion.identity);
        }

        if (breakAudio != null)
        {
            breakAudio.Play();
        }

        if (rampPrefab != null)
        {
            Vector3 spawnPos = rampSpawnPoint != null ? rampSpawnPoint.position : transform.position;
            Quaternion baseRot = rampSpawnPoint != null ? rampSpawnPoint.rotation : Quaternion.identity;
            Quaternion rot180Y = Quaternion.Euler(0f, 180f, 0f);
            spawnedRamp = Instantiate(rampPrefab, spawnPos, baseRot * rot180Y);
        }
        else if (rampToEnable != null)
        {
            Quaternion baseRot = rampSpawnPoint != null ? rampSpawnPoint.rotation : rampToEnable.transform.rotation;
            Quaternion rot180Y = Quaternion.Euler(0f, 180f, 0f);
            rampToEnable.transform.rotation = baseRot * rot180Y;
            rampToEnable.SetActive(true);
        }

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (hoistController != null)
        {
            hoistController.NotifyCrateBroken();
        }

        armedForFall = false;
    }

    /// sent
    /// Réactive la caisse (utile pour le bouton reset).
    public void ResetCrateVisual()
    {
        Debug.Log("[CrateBreak] Reset visuals");
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (col != null) col.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        if (spawnedRamp != null && destroySpawnedRampOnReset)
        {
            Destroy(spawnedRamp);
            spawnedRamp = null;
        }

        broken = false;
        armedForFall = false;
    }

    /// sent
    /// Arme la casse pour la prochaine chute (appelé au drop).
    public void ArmBreakOnFall()
    {
        Debug.Log("[CrateBreak] Armed for fall");
        armedForFall = true;
    }

    /// sent
    /// Désarme la casse (appelé au reset).
    public void DisarmBreak()
    {
        Debug.Log("[CrateBreak] Disarmed");
        armedForFall = false;
    }
}
