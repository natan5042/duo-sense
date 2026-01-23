using UnityEngine;

public class DoorOpening : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Distance à partir de laquelle le joueur peut interagir")]
    public float interactionRadius = 3f;
    [Tooltip("Touche d'interaction")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Mode de téléportation")]
    [Tooltip("Si vrai, nécessite que les deux joueurs soient dans la zone et téléporte chacun vers sa destination.")]
    public bool requireBothPlayers = false;

    [Header("Teleportation simple")]
    [Tooltip("Destination pour le mode téléportation simple (un joueur)")]
    public Vector3 teleportDestination = Vector3.zero;

    [Header("Teleportation double")]
    [Tooltip("Premier joueur à téléporter (optionnel, trouvé par tag si vide)")]
    public Transform player1;
    [Tooltip("Tag pour trouver le premier joueur si le champ est vide")]
    public string player1Tag = "Player";
    [Tooltip("Destination du premier joueur en mode double")]
    public Vector3 teleportDestinationPlayer1 = Vector3.zero;

    [Tooltip("Second joueur à téléporter (optionnel, trouvé par tag si vide)")]
    public Transform player2;
    [Tooltip("Tag pour trouver le second joueur si le champ est vide (peut être identique au premier)")]
    public string player2Tag = "Player";
    [Tooltip("Destination du second joueur en mode double")]
    public Vector3 teleportDestinationPlayer2 = Vector3.zero;

    [Header("Divers")]
    [Tooltip("Nom de cette porte (pour debug)")]
    public string doorName = "Porte";
    [Tooltip("Délai après téléportation avant de pouvoir re-téléporter")]
    public float teleportCooldown = 1f;

    [Header("Debug")]
    public bool debugLogs = false;

    private static float lastTeleportTime = -999f;

    void Start()
    {
        ResolvePlayers();
    }

    void Update()
    {
        // Cooldown global
        if (Time.time - lastTeleportTime < teleportCooldown)
            return;

        // Met à jour les références si elles ont disparu (ex: respawn)
        ResolvePlayers();

        if (requireBothPlayers)
        {
            HandleDualTeleport();
        }
        else
        {
            HandleSingleTeleport();
        }
    }

    void HandleSingleTeleport()
    {
        // Téléporte le premier joueur (par tag player1Tag) qui interagit dans le rayon
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag(player1Tag);
        foreach (GameObject p in allPlayers)
        {
            Transform t = p.transform;
            float dist = Vector3.Distance(t.position, transform.position);
            if (dist <= interactionRadius && Input.GetKeyDown(interactKey))
            {
                if (debugLogs)
                {
                    Debug.Log($"[{doorName}] Téléport de {p.name} vers {teleportDestination}");
                }
                t.position = teleportDestination;
                lastTeleportTime = Time.time;
                return;
            }
        }
    }

    void HandleDualTeleport()
    {
        if (player1 == null || player2 == null) return;

        bool p1InRange = Vector3.Distance(player1.position, transform.position) <= interactionRadius;
        bool p2InRange = Vector3.Distance(player2.position, transform.position) <= interactionRadius;

        if (p1InRange && p2InRange && Input.GetKeyDown(interactKey))
        {
            if (debugLogs)
            {
                Debug.Log($"[{doorName}] Téléport duo: {player1.name} -> {teleportDestinationPlayer1}, {player2.name} -> {teleportDestinationPlayer2}");
            }

            player1.position = teleportDestinationPlayer1;
            player2.position = teleportDestinationPlayer2;
            lastTeleportTime = Time.time;
        }
    }

    void ResolvePlayers()
    {
        // Cas où les deux joueurs partagent le même tag : on prend les deux premiers différents
        if (player1Tag == player2Tag)
        {
            var all = GameObject.FindGameObjectsWithTag(player1Tag);
            if (player1 == null && all.Length > 0)
            {
                player1 = all[0].transform;
            }
            if (player2 == null && all.Length > 1)
            {
                // Choisir un joueur différent du premier
                for (int i = 0; i < all.Length; i++)
                {
                    if (player1 != null && all[i].transform == player1) continue;
                    player2 = all[i].transform;
                    break;
                }
            }
        }
        else
        {
            if (player1 == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(player1Tag);
                if (p != null) player1 = p.transform;
            }

            if (player2 == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(player2Tag);
                if (p != null) player2 = p.transform;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 0.2f, 0.12f);
        Gizmos.DrawSphere(transform.position, interactionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}

