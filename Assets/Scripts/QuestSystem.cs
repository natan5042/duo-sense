using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameQuests
{
    // Minimal Quest representation used by several minigames
    public class Quest
    {
        public string title;
        // Legacy compatibility: some scripts reference questName instead of title
        public string questName
        {
            get => title;
            set => title = value;
        }
        public List<string> steps = new List<string>();
        public List<bool> completedSteps = new List<bool>();
        public List<string> stepAssignments = new List<string>(); // Имена персонажей для каждого шага
        /// <summary> Pour les étapes avec quantité (ex: LAIT x2). 0 = étape booléenne classique. </summary>
        public List<int> stepRequiredCounts = new List<int>();
        public List<int> stepCurrentCounts = new List<int>();

        public Quest(string title)
        {
            this.title = title;
        }

        public void AddStep(string description, string assignedTo = "")
        {
            steps.Add(description);
            completedSteps.Add(false);
            stepAssignments.Add(assignedTo);
            stepRequiredCounts.Add(0);
            stepCurrentCounts.Add(0);
        }

        /// <summary> Ajoute une étape avec quantité (ex: "LAIT", 2 pour LAIT x2). </summary>
        public void AddStepWithCount(string description, int requiredCount, string assignedTo = "")
        {
            steps.Add(description);
            completedSteps.Add(false);
            stepAssignments.Add(assignedTo);
            stepRequiredCounts.Add(requiredCount > 0 ? requiredCount : 1);
            stepCurrentCounts.Add(0);
        }

        /// <summary> Incrémente le compteur d'une étape (ex: produit passé en caisse). Complète l'étape si atteint. </summary>
        public void IncrementStepCount(int index, int amount = 1)
        {
            if (index < 0 || index >= steps.Count) return;
            if (stepCurrentCounts == null || stepRequiredCounts == null || index >= stepCurrentCounts.Count || index >= stepRequiredCounts.Count) return;
            stepCurrentCounts[index] = Mathf.Min(stepCurrentCounts[index] + amount, stepRequiredCounts[index] > 0 ? stepRequiredCounts[index] : int.MaxValue);
            if (stepRequiredCounts[index] > 0 && stepCurrentCounts[index] >= stepRequiredCounts[index])
            {
                completedSteps[index] = true;
                if (QuestSystem.Instance != null) QuestSystem.Instance.NotifyQuestsChanged();
            }
            if (QuestSystem.Instance != null) QuestSystem.Instance.NotifyQuestsChanged();
        }

        /// <summary> Texte d'affichage pour une étape (ex: "LAIT x2 (1/2)"). </summary>
        public string GetStepDisplay(int index)
        {
            if (index < 0 || index >= steps.Count) return "";
            int req = index < stepRequiredCounts.Count ? stepRequiredCounts[index] : 0;
            int cur = index < stepCurrentCounts.Count ? stepCurrentCounts[index] : 0;
            if (req > 0)
                return $"{steps[index]} ({cur}/{req})";
            return steps[index];
        }

        public void CompleteStep(int index)
        {
            if (index < 0 || index >= completedSteps.Count) return;
            completedSteps[index] = true;
            Debug.Log($"Quest '{title}': étape {index} marquée comme complétée.");
            
            // Уведомляем QuestSystem об изменении
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.NotifyQuestsChanged();
            }
        }

        public bool IsCompleted()
        {
            foreach (var c in completedSteps) if (!c) return false;
            return true;
        }
    }

    // Simple QuestSystem singleton used by minigames
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        public List<Quest> activeQuests = new List<Quest>();
        
        // Событие для уведомления UI об изменениях
        public System.Action OnQuestsChanged;

        [Tooltip("Noms de scènes (ex: maison1) pour lesquelles les quêtes supermarché sont auto-supprimées à l'entrée.")]
        public List<string> clearSupermarketQuestsOnScenes = new List<string> { "maison1", "maison" };

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (clearSupermarketQuestsOnScenes == null) return;
            string name = scene.name ?? "";
            foreach (string entry in clearSupermarketQuestsOnScenes)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                if (name.Equals(entry, System.StringComparison.OrdinalIgnoreCase) || name.IndexOf(entry, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    RemoveQuestsByTitles(new[] { "Traverser l'obstacle", "Acheter les produits" });
                    return;
                }
            }
        }

        public void AddQuest(Quest q)
        {
            if (q == null) return;
            
            // Проверяем, нет ли уже такого квеста
            foreach (Quest existing in activeQuests)
            {
                if (existing != null && existing.title == q.title)
                {
                    Debug.LogWarning($"QuestSystem: Квест '{q.title}' уже существует! Не добавляю дубликат.");
                    return;
                }
            }
            
            activeQuests.Add(q);
            Debug.Log($"QuestSystem: Quest '{q.title}' ajoutée. Total active: {activeQuests.Count}");
            
            // Уведомляем UI
            NotifyQuestsChanged();
        }

        public void CompleteQuest(Quest q)
        {
            if (q == null) return;
            if (activeQuests.Contains(q))
            {
                activeQuests.Remove(q);
                Debug.Log($"QuestSystem: Quest '{q.title}' complétée et retirée. Remaining: {activeQuests.Count}");
                
                // Уведомляем UI
                NotifyQuestsChanged();
            }
        }

        /// <summary> Удаляет квесты по названиям (например квесты супермаркета при переходе в дом). </summary>
        public void RemoveQuestsByTitles(System.Collections.Generic.IEnumerable<string> titles)
        {
            if (titles == null || activeQuests == null) return;
            var toRemove = new System.Collections.Generic.List<Quest>();
            foreach (var q in activeQuests)
            {
                if (q == null) continue;
                foreach (var t in titles)
                    if (!string.IsNullOrEmpty(t) && q.title == t) { toRemove.Add(q); break; }
            }
            foreach (var q in toRemove)
            {
                activeQuests.Remove(q);
                Debug.Log($"QuestSystem: Квест '{q.title}' удалён (смена уровня). Remaining: {activeQuests.Count}");
            }
            if (toRemove.Count > 0) NotifyQuestsChanged();
        }
        
        public void NotifyQuestsChanged()
        {
            if (OnQuestsChanged != null)
            {
                OnQuestsChanged.Invoke();
            }
        }
    }
}
