using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gère la zone de la porte : son d'ouverture, animation, disparition du perso et changement de scène
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorZoneTrigger : MonoBehaviour
{
    [Header("Détection")]
    [SerializeField] private string[] playerTags = { "Player", "Player2" }; // tags acceptés
    [SerializeField] private string[] playerNameContains = { }; // noms (contient). Laisser vide pour ignorer le nom.
    [SerializeField] private bool requireNameMatch = false; // si true, on applique le filtre de nom
    [SerializeField] private bool acceptAnyTagIfListEmpty = true; // si aucun tag défini, accepte tout

    [Header("Porte")]
    [SerializeField] private GameObject doorObject;  // Objet porte à masquer/afficher
    [SerializeField] private Transform doorFrontPoint; // Point de référence devant la porte (optionnel)
    [SerializeField] private Vector2 doorFrontOffset = Vector2.zero; // Offset appliqué si pas de point dédié
    [SerializeField] private float doorFrontRadius = 0.9f; // Rayon d'acceptation devant la porte
    [SerializeField] private bool allowDoorFrontOutsideTrigger = true; // si true, on valide un joueur proche de la porte même hors trigger

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSound;  // Son d'ouverture
    [SerializeField] private float soundVolume = 1f;

    [Header("Comportement")]
    [SerializeField] private float delayBeforeDisappear = 0.5f;  // Délai avant que le perso disparaisse
    [SerializeField] private string nextSceneName = "";  // Nom de la scène à charger (optionnel)
    [SerializeField] private bool loadNextBuildIndexIfEmpty = true; // Si vide, charger la scène suivante du Build Settings
    [SerializeField] private bool debugLogs = true;

    private Collider2D triggerCollider;
    private bool isDoorOpen = false;
    private int playersOnTrigger = 0;
    private AudioSource audioSource;
    private bool isLoadingScene = false;
    private readonly HashSet<int> disappearingPlayers = new HashSet<int>();

    private void Update()
    {
        // Si on autorise la validation hors trigger, on check chaque frame quand la porte est ouverte
        if (allowDoorFrontOutsideTrigger && isDoorOpen)
        {
            CheckPlayersNearDoor();
        }
    }

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();

        // Créer une AudioSource si pas déjà présente
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifier si c'est le joueur
        if (!IsPlayer(other)) return;

        playersOnTrigger++;

        if (debugLogs) Debug.Log($"[DoorZone] Joueur détecté ! Nombre de joueurs: {playersOnTrigger}");

        // Ouvrir la porte au premier contact
        if (!isDoorOpen)
        {
            OpenDoor();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Dans le mode simple, on ne gère que l'ouverture ici. La disparition est vérifiée dans Update (si autorisée)
        if (!IsPlayer(other) || !isDoorOpen) return;

        if (!allowDoorFrontOutsideTrigger)
        {
            if (!IsPlayerInFrontOfDoor(other.transform)) return;
            if (debugLogs) Debug.Log($"[DoorZone] Joueur devant la porte ouverte - Disparition (dans trigger)...");
            StartDisappear(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Un joueur quitte la zone
        if (IsPlayer(other))
        {
            playersOnTrigger--;

            if (debugLogs) Debug.Log($"[DoorZone] Joueur a quitté la zone. Nombre restants: {playersOnTrigger}");

            // Fermer la porte si aucun joueur n'est dessus
            if (playersOnTrigger <= 0 && isDoorOpen)
            {
                CloseDoor();
            }
        }
    }

    private bool IsPlayer(Collider2D collider)
    {
        if (collider == null) return false;

        bool tagOk = false;
        if (playerTags != null && playerTags.Length > 0)
        {
            foreach (var t in playerTags)
            {
                if (!string.IsNullOrEmpty(t) && collider.CompareTag(t))
                {
                    tagOk = true;
                    break;
                }
            }
        }

        // Si aucun tag n'est renseigné, on peut accepter tout
        if (!tagOk && (playerTags == null || playerTags.Length == 0) && acceptAnyTagIfListEmpty)
        {
            tagOk = true;
        }

        bool hasNameFilter = requireNameMatch;
        bool nameOk = !requireNameMatch; // si on ne requiert pas, alors ok

        if (requireNameMatch && playerNameContains != null)
        {
            foreach (var n in playerNameContains)
            {
                if (string.IsNullOrEmpty(n)) continue;
                hasNameFilter = true;
                if (collider.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nameOk = true;
                    break;
                }
            }
        }

        return tagOk && nameOk;
    }

    private bool IsPlayerInFrontOfDoor(Transform playerTransform)
    {
        if (playerTransform == null) return false;

        Vector3 targetPos = GetDoorFrontPosition();
        if (!IsVectorValid(targetPos)) return true; // si pas de référence, on accepte

        float dist = Vector2.Distance(playerTransform.position, targetPos);
        return dist <= doorFrontRadius;
    }

    private void CheckPlayersNearDoor()
    {
        Vector3 targetPos = GetDoorFrontPosition();
        if (!IsVectorValid(targetPos)) return;

        var hits = Physics2D.OverlapCircleAll(targetPos, doorFrontRadius);
        foreach (var hit in hits)
        {
            if (IsPlayer(hit))
            {
                if (debugLogs) Debug.Log($"[DoorZone] Joueur proche de la porte ouverte - Disparition (scan)");
                StartDisappear(hit.gameObject);
                return; // une seule disparition à la fois
            }
        }
    }

    private bool IsPlayerNameOk(GameObject obj)
    {
        if (!requireNameMatch || playerNameContains == null || playerNameContains.Length == 0) return true;
        foreach (var n in playerNameContains)
        {
            if (string.IsNullOrEmpty(n)) continue;
            if (obj.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private Vector3 GetDoorFrontPosition()
    {
        if (doorFrontPoint != null) return doorFrontPoint.position;
        if (doorObject != null) return doorObject.transform.position + (Vector3)doorFrontOffset;
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    // Gizmo pour visualiser la zone devant la porte
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var pos = GetDoorFrontPosition();
        if (IsVectorValid(pos))
        {
            Gizmos.DrawWireSphere(pos, doorFrontRadius);
        }
    }

    private bool IsVectorValid(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
    }

    private void OpenDoor()
    {
        if (isDoorOpen) return;

        isDoorOpen = true;

        if (debugLogs) Debug.Log("[DoorZone] Porte en train de s'ouvrir...");

        // Jouer le son
        if (doorOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(doorOpenSound, soundVolume);
        }

        // Masquer la porte pendant qu'elle est "ouverte"
        if (doorObject != null)
        {
            doorObject.SetActive(false);
        }
    }

    private void CloseDoor()
    {
        if (!isDoorOpen) return;

        isDoorOpen = false;

        if (debugLogs) Debug.Log("[DoorZone] Porte en train de se fermer...");

        // Réafficher la porte
        if (doorObject != null)
        {
            doorObject.SetActive(true);
        }
    }

    private void StartDisappear(GameObject player)
    {
        if (player == null) return;

        if (isLoadingScene) return; // déjà en train de charger une scène

        int id = player.GetInstanceID();
        if (disappearingPlayers.Contains(id)) return; // déjà en cours

        disappearingPlayers.Add(id);
        StartCoroutine(DisappearPlayer(player));
    }

    private IEnumerator DisappearPlayer(GameObject player)
    {
        int id = player != null ? player.GetInstanceID() : -1;

        yield return new WaitForSeconds(delayBeforeDisappear);

        if (player == null)
        {
            if (id != -1) disappearingPlayers.Remove(id);
            yield break;
        }

        disappearingPlayers.Remove(id);

        if (debugLogs) Debug.Log($"[DoorZone] {player.name} disparaît - niveau complété!");

        // Désactiver le joueur
        player.SetActive(false);

        // Mettre à jour le compteur et refermer si plus personne
        playersOnTrigger = Mathf.Max(0, playersOnTrigger - 1);
        if (playersOnTrigger <= 0 && isDoorOpen)
        {
            CloseDoor();
        }

        // Charger la scène : d'abord nextSceneName, sinon scène suivante du Build Settings
        if (isLoadingScene) yield break;

        string sceneToLoad = nextSceneName;

        if (string.IsNullOrEmpty(sceneToLoad) && loadNextBuildIndexIfEmpty)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int targetIndex = currentIndex + 1;
            if (targetIndex < SceneManager.sceneCountInBuildSettings)
            {
                isLoadingScene = true;
                yield return new WaitForSeconds(0.5f);
                if (debugLogs) Debug.Log($"[DoorZone] Chargement de la scène index: {targetIndex}");
                SceneManager.LoadScene(targetIndex);
                yield break;
            }
            else
            {
                // Pas de scène suivante, on recharge l'actuelle
                sceneToLoad = SceneManager.GetActiveScene().name;
            }
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            isLoadingScene = true;
            yield return new WaitForSeconds(0.5f);
            if (debugLogs) Debug.Log($"[DoorZone] Chargement de la scène: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
