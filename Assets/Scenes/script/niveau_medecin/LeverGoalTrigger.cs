using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LeverGoalTrigger : MonoBehaviour
{
    [SerializeField] private string irisTag = "Player";          // tag attendu pour Iris
    [SerializeField] private string irisNameContains = "Iris";   // nom doit contenir "Iris" (ou laisse vide)
    [SerializeField] private Transform teleportTarget;            // où placer Iris quand le levier la touche
    [SerializeField] private GameObject rampToEnable;             // rampe à activer
    [SerializeField] private DoorTeleport doorTeleport;           // optionnel : pour quitter le mode puzzle
    [SerializeField] private bool disableAfterHit = true;         // éviter de retrigger
    [SerializeField] private bool debugLogs = true;               // logs pour diagnostic

    private bool alreadyTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyTriggered && disableAfterHit) return;

        bool tagOk = other.CompareTag(irisTag);
        bool nameOk = string.IsNullOrEmpty(irisNameContains) || other.name.Contains(irisNameContains);

        if (debugLogs)
        {
            Debug.Log($"[LeverGoalTrigger] Trigger avec {other.name} | tagOk={tagOk} | nameOk={nameOk} | tag={other.tag}");
        }

        if (!tagOk || !nameOk) return;

        // Si un PuzzleLeverController est dans les parents, on utilise son flow complet (déparentage, reset rigidbody, rampe + sortie puzzle)
        var controller = GetComponentInParent<PuzzleLeverController>();
        if (controller != null)
        {
            controller.CompletePuzzle();
            if (teleportTarget != null)
            {
                other.transform.position = teleportTarget.position;
            }
            if (debugLogs)
            {
                Debug.Log("[LeverGoalTrigger] Puzzle complété via controller");
            }
        }
        else
        {
            Transform iris = other.transform;

            if (teleportTarget != null)
            {
                iris.position = teleportTarget.position;
            }

            if (rampToEnable != null)
            {
                rampToEnable.SetActive(true);
            }

            if (doorTeleport != null)
            {
                doorTeleport.ExitPuzzleMode();
            }
            else
            {
                // Fallback : s'assurer que Iris peut bouger à nouveau
                var rb = iris.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.constraints = RigidbodyConstraints2D.None;
                    rb.gravityScale = 1f;
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                var move = iris.GetComponent<PlayerMovement>();
                if (move != null) move.enabled = true;
            }

            if (debugLogs)
            {
                Debug.Log("[LeverGoalTrigger] Iris téléportée, rampe activée (fallback)");
            }
        }

        if (disableAfterHit)
        {
            alreadyTriggered = true;
            // désactive le collider pour éviter les retriggers
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}
