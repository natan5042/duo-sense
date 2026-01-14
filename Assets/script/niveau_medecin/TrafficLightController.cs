using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple traffic light: toggles red/green visuals every switchInterval seconds.
/// When red, the linked dangerZone collider is enabled and will trigger an accident.
/// </summary>
public class TrafficLightController : MonoBehaviour
{
    [Header("Feux visuels")]
    [SerializeField] private GameObject greenLight;
    [SerializeField] private GameObject redLight;

    [Header("Zone mortelle")] 
    [Tooltip("Collider2D (isTrigger) sur la route qui tue quand le feu est rouge.")]
    [SerializeField] private Collider2D dangerZone;

    [Header("Réglages")]
    [SerializeField] private float switchInterval = 2f;
    [SerializeField] private bool startRed = false;
    [SerializeField] private string[] playerTags = { "Player", "Player2" }; // tags acceptés (Achille, Iris)
    [SerializeField] private string[] playerNameContains = { "Iris", "Achille" }; // noms (contient). Laisser vide pour ignorer.
    [SerializeField] private string accidentMessage = "Vous avez fait un accident";
    [SerializeField] private string sceneToLoad = ""; // si vide, on recharge la scène actuelle

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchClip;
    [SerializeField] private AudioClip accidentClip;
    [SerializeField] private AudioSource buzzingSource; // dédié au grésillement rouge
    [SerializeField] private AudioClip buzzClip;
    [SerializeField] [Range(0f, 1f)] private float buzzVolume = 0.35f;
    [SerializeField] private float buzzMaxDistance = 10f; // au-delà de cette distance, silence
    [SerializeField] private float buzzMinDistance = 3f;  // à partir de cette distance, volume max

    [Header("Evénements")] 
    [Tooltip("Appelé quand le joueur entre dans la zone avec le feu rouge (pour gérer mort/game over).")]
    public UnityEvent onAccident;

    private bool isRed;
    private float timer;
    private bool accidentTriggered;
    private Transform playerTransform;

    private void Awake()
    {
        isRed = startRed;
        CachePlayer();
        if (dangerZone != null)
        {
            dangerZone.isTrigger = true;
            dangerZone.enabled = false; // activée uniquement pendant le son
        }
        ApplyState();
    }

    private void CachePlayer()
    {
        GameObject playerObj = FindAnyPlayer();
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            timer = 0f;
            isRed = !isRed;
            ApplyState();
        }

        UpdateBuzzing();
    }

    private void ApplyState()
    {
        if (redLight != null) redLight.SetActive(isRed);
        if (greenLight != null) greenLight.SetActive(!isRed);

        // Son de bascule
        if (switchClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchClip);
        }

        UpdateBuzzing();
    }

    private void UpdateBuzzing()
    {
        float distanceFactor = ComputeDistanceFactor();
        bool canBuzz = isRed && distanceFactor > 0f;

        if (buzzingSource != null && buzzClip != null)
        {
            buzzingSource.loop = true;
            buzzingSource.clip = buzzClip;

            if (canBuzz)
            {
                buzzingSource.volume = buzzVolume * distanceFactor;
                if (!buzzingSource.isPlaying)
                {
                    buzzingSource.Play();
                }
            }
            else
            {
                if (buzzingSource.isPlaying)
                {
                    buzzingSource.Stop();
                }
            }
        }

        // On active la zone mortelle simplement quand c'est rouge
        UpdateDangerZone(isRed);
    }

    private float ComputeDistanceFactor()
    {
        if (playerTransform == null)
        {
            CachePlayer();
            if (playerTransform == null) return 0f;
        }

        float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
        float maxSqr = buzzMaxDistance * buzzMaxDistance;
        float minSqr = buzzMinDistance * buzzMinDistance;

        if (sqrDist >= maxSqr) return 0f;
        if (sqrDist <= minSqr) return 1f;

        float dist = Mathf.Sqrt(sqrDist);
        float t = 1f - Mathf.InverseLerp(buzzMinDistance, buzzMaxDistance, dist);
        return Mathf.Clamp01(t);
    }

    private void UpdateDangerZone(bool active)
    {
        if (dangerZone == null) return;
        dangerZone.isTrigger = true;
        dangerZone.enabled = active;

        // Si le collider est sur un autre GameObject, on ajoute un relay pour transmettre l'événement
        if (active && dangerZone.gameObject != gameObject)
        {
            var relay = dangerZone.GetComponent<TrafficLightDangerZoneRelay>();
            if (relay == null)
            {
                relay = dangerZone.gameObject.AddComponent<TrafficLightDangerZoneRelay>();
            }
            relay.Bind(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // We only care about the danger zone trigger when red.
        if (accidentTriggered) return;
        if (!isRed || dangerZone == null) return;
        if (!IsVictim(other)) return;

        accidentTriggered = true;

        Debug.Log(accidentMessage);
        onAccident?.Invoke();

        // Son d'accident avant de changer de scène
        float delay = 0f;
        if (accidentClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(accidentClip);
            delay = accidentClip.length;
        }

        string targetScene = string.IsNullOrEmpty(sceneToLoad)
            ? SceneManager.GetActiveScene().name
            : sceneToLoad;

        StartCoroutine(LoadSceneAfter(delay, targetScene));
    }

    // If the collider is on a child, ensure the trigger forwards to this script
    public void HandleZoneTrigger(Collider2D other)
    {
        OnTriggerEnter2D(other);
    }

    private bool IsVictim(Collider2D other)
    {
        if (other == null) return false;

        bool tagOk = false;
        if (playerTags != null)
        {
            foreach (var t in playerTags)
            {
                if (!string.IsNullOrEmpty(t) && other.CompareTag(t))
                {
                    tagOk = true;
                    break;
                }
            }
        }
        if (playerTags == null || playerTags.Length == 0) tagOk = true; // aucun tag renseigné => accepte tout

        bool hasNameFilter = false;
        bool nameOk = false;
        if (playerNameContains != null)
        {
            foreach (var n in playerNameContains)
            {
                if (string.IsNullOrEmpty(n)) continue;
                hasNameFilter = true;
                if (other.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nameOk = true;
                    break;
                }
            }
        }
        if (!hasNameFilter) nameOk = true;

        return tagOk && nameOk;
    }

    private GameObject FindAnyPlayer()
    {
        if (playerTags == null || playerTags.Length == 0) return null;

        foreach (var t in playerTags)
        {
            if (string.IsNullOrEmpty(t)) continue;
            var obj = GameObject.FindGameObjectWithTag(t);
            if (obj != null) return obj;
        }
        return null;
    }

    private IEnumerator LoadSceneAfter(float delay, string targetScene)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        SceneManager.LoadScene(targetScene);
    }
}

/// <summary>
/// Relai pour faire remonter l'événement OnTriggerEnter2D du collider de danger vers le TrafficLightController
/// quand le collider est sur un autre GameObject.
/// </summary>
public class TrafficLightDangerZoneRelay : MonoBehaviour
{
    private TrafficLightController controller;

    public void Bind(TrafficLightController ctrl)
    {
        controller = ctrl;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        controller?.HandleZoneTrigger(other);
    }
}
