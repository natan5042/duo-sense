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
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string accidentMessage = "Vous avez fait un accident";
    [SerializeField] private string sceneToLoad = ""; // si vide, on recharge la scène actuelle

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchClip;
    [SerializeField] private AudioClip accidentClip;

    [Header("Evénements")] 
    [Tooltip("Appelé quand le joueur entre dans la zone avec le feu rouge (pour gérer mort/game over).")]
    public UnityEvent onAccident;

    private bool isRed;
    private float timer;
    private bool accidentTriggered;

    private void Awake()
    {
        isRed = startRed;
        ApplyState();
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
    }

    private void ApplyState()
    {
        if (redLight != null) redLight.SetActive(isRed);
        if (greenLight != null) greenLight.SetActive(!isRed);

        if (dangerZone != null)
        {
            dangerZone.enabled = isRed;
            dangerZone.isTrigger = true;
        }

        // Son de bascule
        if (switchClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchClip);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // We only care about the danger zone trigger when red.
        if (accidentTriggered) return;
        if (!isRed || dangerZone == null) return;
        if (other == null || !other.CompareTag(playerTag)) return;

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

    private IEnumerator LoadSceneAfter(float delay, string targetScene)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        SceneManager.LoadScene(targetScene);
    }
}
