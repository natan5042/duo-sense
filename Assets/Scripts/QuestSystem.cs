using System.Collections.Generic;
using UnityEngine;

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

        public Quest(string title)
        {
            this.title = title;
        }

        public void AddStep(string description, string assignedTo = "")
        {
            steps.Add(description);
            completedSteps.Add(false);
        }

        public void CompleteStep(int index)
        {
            if (index < 0 || index >= completedSteps.Count) return;
            completedSteps[index] = true;
            Debug.Log($"Quest '{title}': étape {index} marquée comme complétée.");
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
            }
        }

        public void AddQuest(Quest q)
        {
            if (q == null) return;
            activeQuests.Add(q);
            Debug.Log($"QuestSystem: Quest '{q.title}' ajoutée. Total active: {activeQuests.Count}");
        }

        public void CompleteQuest(Quest q)
        {
            if (q == null) return;
            if (activeQuests.Contains(q))
            {
                activeQuests.Remove(q);
                Debug.Log($"QuestSystem: Quest '{q.title}' complétée et retirée. Remaining: {activeQuests.Count}");
            }
        }
    }
}
