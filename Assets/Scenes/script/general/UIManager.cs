using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Références UI")]
    // Assurez-vous de glisser le Canvas Group du BlackScreen ici
    public CanvasGroup blackScreenGroup; 

    [Header("Réglages")]
    public float fadeDuration = 0.5f;   

    void Awake()
    {
        // Pattern Singleton (s'assurer qu'il n'y en a qu'un)
        if (Instance == null)
        {
            Instance = this;
            // CRUCIAL : L'objet UIManager survit au rechargement de scène
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        EnsureBlackScreenGroup();
        if (blackScreenGroup != null)
            blackScreenGroup.alpha = 0f;
    }

    void EnsureBlackScreenGroup()
    {
        if (blackScreenGroup != null) return;
        blackScreenGroup = GetComponentInChildren<CanvasGroup>();
        if (blackScreenGroup != null) return;
        blackScreenGroup = FindFirstObjectByType<CanvasGroup>();
    }

    public void StartFade(float targetAlpha)
    {
        EnsureBlackScreenGroup();
        StopAllCoroutines();
        StartCoroutine(FadeScreenRoutine(targetAlpha));
    }

    IEnumerator FadeScreenRoutine(float targetAlpha)
    {
        if (blackScreenGroup == null)
        {
            Debug.LogWarning("UIManager : CanvasGroup non assigné. Assignez Black Screen Group dans l'Inspector ou ajoutez un CanvasGroup à l'écran noir.");
            yield break;
        }

        float startAlpha = blackScreenGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            // S'assure que l'objet n'a pas été détruit en plein milieu
            if (blackScreenGroup == null) yield break; 
            
            blackScreenGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        blackScreenGroup.alpha = targetAlpha;
    }
    
    // Fonction appelée par FallingSpike.cs pour gérer une transition de Game Over propre
    public void HandleGameOver()
    {
        StopAllCoroutines();
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 1. Fondu au noir complet
        yield return StartCoroutine(FadeScreenRoutine(1f));

        // 2. Recharge la scène (l'objet UIManager ne bouge pas)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // 3. Laisse le temps au moteur de charger la scène
        yield return null; 

        // 4. Refait le fondu pour revenir à la scène
        yield return StartCoroutine(FadeScreenRoutine(0f));
    }
}