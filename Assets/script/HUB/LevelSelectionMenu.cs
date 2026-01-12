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
    }

    [SerializeField] private LevelButton[] levels;
    [SerializeField] private Button closeButton;
    
    private LevelDisplayInteraction displayInteraction;

    void Start()
    {
        displayInteraction = FindObjectOfType<LevelDisplayInteraction>();

        // Configurer les boutons de niveaux
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].button != null)
            {
                int levelIndex = i; // Capture pour la closure
                levels[i].button.onClick.AddListener(() => LoadLevel(levels[levelIndex].sceneName));
            }
        }

        // Configurer le bouton de fermeture
        if (closeButton != null)
        {
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

    private void LoadLevel(string sceneName)
    {
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
}
