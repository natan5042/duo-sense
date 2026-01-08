using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DoorOpening : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Distance à partir de laquelle le joueur peut interagir pour entrer")]
    public float interactionRadius = 3f;
    [Tooltip("Touche d'interaction")] public KeyCode interactKey = KeyCode.E;
    [Tooltip("Nom de la scène à charger (exact, et ajoutée au Build Settings). Vous pouvez déposer une Scene depuis le Project (éditeur).")]
    public string sceneToLoad;

#if UNITY_EDITOR
    [Header("Editor: assign a SceneAsset (optional)")]
    [Tooltip("Glisser-déposer une scène depuis le Project ici (la scène doit être ajoutée au Build Settings). Ceci remplit automatiquement 'sceneToLoad'.")]
    public SceneAsset sceneAsset;

    void OnValidate()
    {
        if (sceneAsset != null)
        {
            // Sync the scene name for runtime use
            sceneToLoad = sceneAsset.name;
        }
    }
#endif

    [Header("Player")]
    [Tooltip("Glissez ici l'objet joueur depuis la Hierarchy (optionnel). Si laissé vide, le script cherchera par tag 'Player'.")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLogs = false;

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
        if (string.IsNullOrEmpty(sceneToLoad)) return; // rien à faire si pas de scène définie

        Transform target = player;
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) target = p.transform;
        }
        if (target == null) return;

        float dist = Vector3.Distance(target.position, transform.position);
        bool inRange = dist <= interactionRadius;

        if (inRange && Input.GetKeyDown(interactKey))
        {
            if (debugLogs) Debug.Log(name + " : Player in range (" + dist.ToString("F2") + ") -> LoadScene(" + sceneToLoad + ")");
            SceneManager.LoadScene(sceneToLoad);
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
