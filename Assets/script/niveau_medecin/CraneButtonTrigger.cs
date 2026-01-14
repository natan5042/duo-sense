using UnityEngine;

/// <summary>
/// Bouton physique : quand le joueur appuie sur la touche (par défaut S) en restant dans le trigger du bouton,
/// on appelle l'action correspondante sur la grue.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CraneButtonTrigger : MonoBehaviour
{
    public enum CraneAction
    {
        Lift,
        Drop,
        Reset
    }

    [SerializeField] private CraneHoistController hoist;
    [SerializeField] private CraneAction action = CraneAction.Lift;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.S;

    private bool playerInside;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
        }
    }

    private void Update()
    {
        if (!playerInside) return;
        if (!Input.GetKeyDown(interactKey)) return;
        if (hoist == null) return;

        switch (action)
        {
            case CraneAction.Lift:
                hoist.TryLift();
                break;
            case CraneAction.Drop:
                hoist.Drop();
                break;
            case CraneAction.Reset:
                hoist.ResetCrate();
                break;
        }
    }
}
