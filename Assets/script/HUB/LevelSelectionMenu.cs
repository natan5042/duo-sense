using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    
    private LevelDisplayInteraction displayInteraction;

    // Mémo de l'index pour savoir quel bouton est cliqué
    private int[] buttonIndices;

    void Start()
    {
        LevelProgressManager.Load();
        if (levels != null && levels.Length > 0)
        {
            // Assure que le premier niveau est toujours disponible au lancement
            string firstScene = ResolveFirstSceneName();
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

        displayInteraction = FindObjectOfType<LevelDisplayInteraction>();

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
    }

    private void LoadLevel(string sceneName, int index)
    {
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
}
