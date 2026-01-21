using UnityEngine;

public class DoorOpening : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Distance à partir de laquelle le joueur peut interagir")]
    public float interactionRadius = 3f;
    [Tooltip("Touche d'interaction")] 
    public KeyCode interactKey = KeyCode.E;

    [Header("Teleportation")]
    [Tooltip("Position de destination du téléport")]
    public Vector3 teleportDestination = Vector3.zero;
    [Tooltip("Nom de cette porte (pour debug)")]
    public string doorName = "Porte";
    [Tooltip("Délai après téléportation avant de pouvoir re-téléporter")]
    public float teleportCooldown = 1f;

    [Header("Player")]
    [Tooltip("Glissez ici l'objet joueur depuis la Hierarchy (optionnel). Si laissé vide, le script cherchera par tag 'Player'.")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLogs = false;

    private static float lastTeleportTime = -999f;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        // Vérifier le cooldown global
        if (Time.time - lastTeleportTime < teleportCooldown)
            return;

        // Trouver TOUS les joueurs avec le tag
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag(playerTag);
        
        foreach (GameObject p in allPlayers)
        {
            Transform playerTransform = p.transform;
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            bool inRange = dist <= interactionRadius;

            // Chaque joueur qui appuie sur E se téléporte individuellement
            if (inRange && Input.GetKeyDown(interactKey))
            {
                Debug.Log($"[{doorName}] Téléport de {p.name} vers {teleportDestination}");
                playerTransform.position = teleportDestination;
                lastTeleportTime = Time.time;
                return; // Cooldown global, empêche les doubles TP
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

