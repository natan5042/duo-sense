using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Gère le menu de sélection des niveaux
public class LevelSelectionMenu : MonoBehaviour
{
    [System.Serializable]
    public class LevelButton
    {
        public Button button;
        public string sceneName; // Nom de la scène à charger
        [Tooltip("Optionnel: visuel à afficher quand le niveau est verrouillé")]
        public Graphic lockedGraphic;
        [Tooltip("Optionnel: visuel mis tout devant quand le niveau est déverrouillé")]
        public Graphic unlockedFrontGraphic;
    }

    [SerializeField] private LevelButton[] levels;
    [SerializeField] private Button closeButton;
    [SerializeField] private string defaultFirstScene = "Level1"; // Scène qui doit être déverrouillée au départ
    [SerializeField] private bool logDebug = true;
    [SerializeField] private bool alwaysAllowFirstButton = true; // Secours: le premier bouton reste cliquable quoi qu'il arrive
    [SerializeField] private bool lockOnlyFirstAtStartup = false; // Pour les tests: verrouille tout sauf la première scène

    [Header("Intro niveau 1 (dialogue avant chargement)")]
    [SerializeField] private string introSceneName = ""; // Nom exact de la scène niveau1
    [SerializeField] private AudioClip introDialogueClip; // Assigne DIALOGUE1
    [TextArea]
    [SerializeField] private string introSubtitle; // Texte du sous-titre
    [SerializeField] private float introSubtitleHoldExtra = 0.25f; // Temps en plus après l'audio
    [SerializeField] private bool introPlayOncePerSession = true; // Evite de rejouer si déjà lancé
    [SerializeField] private GameObject introBlackScreen; // Image pleine écran noire (Canvas overlay)
    [SerializeField] private bool introBlackFade = true; // Si true, on fade
    [SerializeField] private float introBlackFadeDuration = 0.3f;
    [SerializeField] private bool introForceSubtitleOnTop = true; // Met les sous-titres au-dessus du noir
    [SerializeField] private int introBlackScreenSortingOrder = 1000; // Ordre du canvas du noir
    [SerializeField] private int introSubtitleSortingOrder = 5000;   // Ordre du canvas des sous-titres
    
    private LevelDisplayInteraction displayInteraction;

    // Mémo de l'index pour savoir quel bouton est cliqué
    private int[] buttonIndices;

    // Etat local
    private bool introAlreadyPlayed;
    private bool loadingRoutine;
    // Temp subtitle object on the black screen (created at runtime)
    private GameObject introTempSubtitleGO;
    private TextMeshProUGUI introTempSubtitleTMP;
    private CanvasGroup introTempSubtitleCanvasGroup;

    void Start()
    {
        LevelProgressManager.Load();
        if (levels != null && levels.Length > 0)
        {
            // Assure que le premier niveau est toujours disponible au lancement
            string firstScene = ResolveFirstSceneName();
            if (lockOnlyFirstAtStartup && !string.IsNullOrEmpty(firstScene))
            {
                LevelProgressManager.LockAllExcept(firstScene);
                if (logDebug) Debug.Log($"[LevelSelectionMenu] Locked all, kept only '{firstScene}' unlocked (test mode).");
            }
            if (!string.IsNullOrEmpty(firstScene))
            {
                LevelProgressManager.EnsureDefaultUnlocked(firstScene);
                if (logDebug) Debug.Log($"[LevelSelectionMenu] First scene ensured unlocked: {firstScene}");
            }
            else if (logDebug)
            {
                Debug.LogWarning("[LevelSelectionMenu] No first scene name found; buttons with empty sceneName will stay interactable.");
            }

            // Force aussi le sceneName du premier bouton si renseigné (sécurise le cas où defaultFirstScene diffère)
            if (!string.IsNullOrEmpty(levels[0].sceneName))
            {
                bool added = LevelProgressManager.Unlock(levels[0].sceneName);
                if (logDebug && added) Debug.Log($"[LevelSelectionMenu] Also unlocked first button scene '{levels[0].sceneName}'.");
            }
        }
        else if (logDebug)
        {
            Debug.LogWarning("[LevelSelectionMenu] levels array is null or empty.");
        }

        displayInteraction = UnityEngine.Object.FindAnyObjectByType<LevelDisplayInteraction>();

        // Configurer les boutons de niveaux
        buttonIndices = new int[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].button != null)
            {
                int levelIndex = i; // Capture pour la closure
                buttonIndices[i] = i;
                levels[i].button.onClick.RemoveAllListeners(); // evite les liens residuels (ex: back)
                levels[i].button.onClick.AddListener(() => LoadLevel(levels[levelIndex].sceneName, levelIndex));
                ApplyLockVisual(levels[i], i);
                if (logDebug)
                {
                    Debug.Log($"[LevelSelectionMenu] Button index {i} scene='{levels[i].sceneName}' interactable={levels[i].button.interactable}");
                }
            }
            else if (logDebug)
            {
                Debug.LogWarning($"[LevelSelectionMenu] Button at index {i} is null.");
            }
        }

        // Configurer le bouton de fermeture
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // garantit que seul CloseMenu est accroché
            closeButton.onClick.AddListener(CloseMenu);
        }

        // Fermer le menu par défaut
        GetComponent<Canvas>().enabled = false;
    }

    void Update()
    {
        // Permettre de fermer le menu avec Échap
        if (GetComponent<Canvas>().enabled && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
        }

        if (GetComponent<Canvas>().enabled)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LoadLevel(string sceneName, int index)
    {
        if (loadingRoutine)
        {
            if (logDebug) Debug.LogWarning("[LevelSelectionMenu] Chargement déjà en cours, clic ignoré.");
            return;
        }

        // Pas de nom => on autorise pour éviter de bloquer un bouton mal configuré
        if (string.IsNullOrEmpty(sceneName))
        {
            if (logDebug) Debug.LogWarning("[LevelSelectionMenu] sceneName is empty; level button will load nothing.");
            return;
        }

        // Ne charge pas un niveau verrouillé sauf si on est sur le premier bouton et le secours est activé
        if (!LevelProgressManager.IsUnlocked(sceneName))
        {
            if (alwaysAllowFirstButton && index == 0)
            {
                if (logDebug) Debug.LogWarning($"[LevelSelectionMenu] Scene '{sceneName}' locked but loading anyway via first-button fallback.");
            }
            else
            {
                if (logDebug) Debug.LogWarning($"[LevelSelectionMenu] Attempt to load locked scene '{sceneName}'.");
                return;
            }
        }

        // Si c'est le niveau1 et que l'intro n'a pas encore été jouée
        if (!string.IsNullOrEmpty(introSceneName)
            && string.Equals(sceneName, introSceneName)
            && introDialogueClip != null
            && (!introPlayOncePerSession || !introAlreadyPlayed))
        {
            if (logDebug) Debug.Log($"[LevelSelectionMenu] Playing intro dialogue before loading '{sceneName}'.");
            StartCoroutine(PlayIntroThenLoad(sceneName));
            return;
        }

        if (logDebug) Debug.Log($"[LevelSelectionMenu] Loading scene '{sceneName}'.");

        Time.timeScale = 1f; // Reprendre le jeu avant de charger
        SceneManager.LoadScene(sceneName);
    }

    private void CloseMenu()
    {
        if (displayInteraction != null)
        {
            displayInteraction.CloseMenu();
        }
        else
        {
            GetComponent<Canvas>().enabled = false;
            Time.timeScale = 1f;
        }
    }

    private void ApplyLockVisual(LevelButton level, int index)
    {
        if (level == null || level.button == null) return;

        // Si pas de nom de scène, on laisse cliquable (configuration partielle)
        if (string.IsNullOrEmpty(level.sceneName))
        {
            level.button.interactable = true;
            if (level.lockedGraphic != null) level.lockedGraphic.gameObject.SetActive(false);
            return;
        }

        bool unlocked = LevelProgressManager.IsUnlocked(level.sceneName);

        // Secours: le tout premier bouton reste cliquable même si la sauvegarde est absente ou le nom ne correspond pas
        if (!unlocked && alwaysAllowFirstButton && index == 0)
        {
            unlocked = true;
            if (logDebug) Debug.LogWarning("[LevelSelectionMenu] Force unlock first button as fallback.");
        }
        level.button.interactable = unlocked;

        if (level.lockedGraphic != null)
        {
            level.lockedGraphic.gameObject.SetActive(!unlocked);
        }

        // Met un élément tout devant quand le niveau est cliquable
        if (level.unlockedFrontGraphic != null)
        {
            level.unlockedFrontGraphic.gameObject.SetActive(unlocked);
            if (unlocked)
            {
                level.unlockedFrontGraphic.transform.SetAsLastSibling();
            }
        }

        // Grisage simple si pas de visuel dédié
        if (level.lockedGraphic == null)
        {
            var colors = level.button.colors;
            colors.normalColor = unlocked ? colors.normalColor : new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.highlightedColor = unlocked ? colors.highlightedColor : colors.normalColor;
            level.button.colors = colors;
        }
    }

    private string ResolveFirstSceneName()
    {
        if (!string.IsNullOrEmpty(defaultFirstScene)) return defaultFirstScene;
        if (levels == null || levels.Length == 0) return string.Empty;

        // Cherche le premier LevelButton avec un nom de scène non vide
        foreach (var lvl in levels)
        {
            if (lvl != null && !string.IsNullOrEmpty(lvl.sceneName))
            {
                return lvl.sceneName;
            }
        }

        return string.Empty;
    }

    private IEnumerator PlayIntroThenLoad(string sceneName)
    {
        loadingRoutine = true;

        // On veut que l'audio tourne même si le jeu était en pause
        Time.timeScale = 1f;

        // Ecran noir
        if (introBlackScreen != null)
        {
            ConfigureBlackScreenCanvas();
            if (introBlackFade)
                yield return StartCoroutine(FadeBlackScreen(true));
            else
                SetBlackScreen(true);
        }

        // Prépare une source 2D pour le dialogue
        var src = GetComponent<AudioSource>();
        if (src == null)
        {
            src = gameObject.AddComponent<AudioSource>();
        }
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        float duration = Mathf.Max(introDialogueClip != null ? introDialogueClip.length : 0f, 0f) + Mathf.Max(introSubtitleHoldExtra, 0f);

        // Affiche les sous-titres si possible — si un écran noir existe, on affiche un texte dessus
        if (!string.IsNullOrWhiteSpace(introSubtitle))
        {
            if (introBlackScreen != null)
            {
                CreateOrReuseTempSubtitleOnBlack();
                StartCoroutine(ShowTempSubtitleRoutine(introSubtitle, duration));
            }
            else
            {
                var ui = SubtitleUI.GetOrFindInstance();
                if (ui != null)
                {
                    if (introForceSubtitleOnTop)
                    {
                        BringSubtitleOnTop(ui);
                    }
                    if (logDebug)
                    {
                        var uiCanvas = ui.GetComponentInParent<Canvas>();
                        var cg = ui.canvasGroup;
                        Debug.Log($"[LevelSelectionMenu] Intro subtitle: ui found, canvas order={(uiCanvas!=null?uiCanvas.sortingOrder:-1)}, override={(uiCanvas!=null && uiCanvas.overrideSorting)}, alpha={(cg!=null?cg.alpha:-1f)}");
                    }
                    ui.ShowSubtitle(introSubtitle, duration);
                }
                else if (logDebug)
                {
                    Debug.LogWarning("[LevelSelectionMenu] Pas de SubtitleUI trouvé pour l'intro.");
                }
            }
        }

        if (introDialogueClip != null)
        {
            src.PlayOneShot(introDialogueClip);
        }

        // On attend en temps réel pour ne pas dépendre du timeScale
        yield return new WaitForSecondsRealtime(Mathf.Max(duration, 0.25f));

        if (introPlayOncePerSession)
        {
            introAlreadyPlayed = true;
        }

        if (introBlackScreen != null)
        {
            if (introBlackFade)
                yield return StartCoroutine(FadeBlackScreen(false));
            else
                SetBlackScreen(false);
        }

        SceneManager.LoadScene(sceneName);
    }

    private void BringSubtitleOnTop(SubtitleUI ui)
    {
        if (ui == null) return;
        var tr = ui.transform;
        if (tr != null && tr.parent != null)
        {
            tr.SetAsLastSibling();
        }

        var canvas = ui.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Force un ordre haut pour passer devant l'image noire si elle est dans le même Canvas
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, introSubtitleSortingOrder);
        }
        if (logDebug)
        {
            Debug.Log($"[LevelSelectionMenu] SubtitleUI bring on top: order={ui.GetComponentInParent<Canvas>()?.sortingOrder}");
        }
    }

    private void ConfigureBlackScreenCanvas()
    {
        if (introBlackScreen == null) return;
        var canvas = introBlackScreen.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = introBlackScreenSortingOrder;
            if (logDebug)
            {
                Debug.Log($"[LevelSelectionMenu] Black screen canvas order set to {canvas.sortingOrder}");
            }
        }
    }

    // Create a temporary TextMeshProUGUI child on the black screen to show subtitles above the black image
    private void CreateOrReuseTempSubtitleOnBlack()
    {
        if (introBlackScreen == null) return;
        if (introTempSubtitleGO != null) return;

        var existing = introBlackScreen.transform.Find("IntroTempSubtitle");
        if (existing != null)
        {
            introTempSubtitleGO = existing.gameObject;
            introTempSubtitleTMP = introTempSubtitleGO.GetComponentInChildren<TextMeshProUGUI>();
            introTempSubtitleCanvasGroup = introTempSubtitleGO.GetComponent<CanvasGroup>();
            return;
        }

        introTempSubtitleGO = new GameObject("IntroTempSubtitle");
        introTempSubtitleGO.transform.SetParent(introBlackScreen.transform, false);

        introTempSubtitleCanvasGroup = introTempSubtitleGO.AddComponent<CanvasGroup>();
        introTempSubtitleCanvasGroup.alpha = 0f;

        introTempSubtitleGO.transform.SetAsLastSibling();

        var rect = introTempSubtitleGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = new Vector2(800f, 200f);

        introTempSubtitleTMP = introTempSubtitleGO.AddComponent<TextMeshProUGUI>();
        introTempSubtitleTMP.alignment = TextAlignmentOptions.BottomGeoAligned;
        introTempSubtitleTMP.enableWordWrapping = true;
        introTempSubtitleTMP.fontSize = 36;
        introTempSubtitleTMP.color = Color.white;
        introTempSubtitleTMP.raycastTarget = false;
    }

    private IEnumerator ShowTempSubtitleRoutine(string text, float duration)
    {
        if (introTempSubtitleGO == null || introTempSubtitleTMP == null || introTempSubtitleCanvasGroup == null)
            yield break;

        introTempSubtitleTMP.text = text;

        float fade = Mathf.Min(0.15f, introBlackFadeDuration);
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            introTempSubtitleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fade);
            yield return null;
        }
        introTempSubtitleCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(duration);

        t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            introTempSubtitleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fade);
            yield return null;
        }
        introTempSubtitleCanvasGroup.alpha = 0f;
    }

    private void SetBlackScreen(bool visible)
    {
        introBlackScreen.SetActive(visible);
        var cg = introBlackScreen.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
        }
    }

    private IEnumerator FadeBlackScreen(bool visible)
    {
        if (introBlackScreen == null)
            yield break;

        var cg = introBlackScreen.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            introBlackScreen.SetActive(visible);
            yield break;
        }

        introBlackScreen.SetActive(true);

        float start = cg.alpha;
        float end = visible ? 1f : 0f;
        float duration = Mathf.Max(introBlackFadeDuration, 0.01f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }
        cg.alpha = end;

        if (!visible)
        {
            introBlackScreen.SetActive(false);
        }
    }
}
