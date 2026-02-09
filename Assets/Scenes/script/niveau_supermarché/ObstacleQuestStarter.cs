using UnityEngine;
using GameQuests;

/// <summary>
/// Добавляет квест "Доска + Трамплин" по умолчанию и отмечает шаги при подборе.
/// Можно вешать на QuestCanvas или любой объект в сцене супермаркета — квест появится сразу, без SupermarketQuestManager.
/// </summary>
public class ObstacleQuestStarter : MonoBehaviour
{
    [Tooltip("ID PickableItem доски (должен совпадать с itemId на объекте)")]
    public string boardItemId = "planche";
    [Tooltip("ID PickableItem трамплина")]
    public string rampItemId = "trampoline";

    private Quest obstacleQuest;
    private int boardStepIndex = 0;
    private int rampStepIndex = 1;

    void Awake()
    {
        if (QuestSystem.Instance == null)
        {
            var go = new GameObject("QuestSystem");
            go.AddComponent<QuestSystem>();
        }

        obstacleQuest = new Quest("Traverser l'obstacle");
        obstacleQuest.AddStep("Prendre la planche", "");
        obstacleQuest.AddStep("Prendre le trampoline", "");
        QuestSystem.Instance.AddQuest(obstacleQuest);
    }

    void Start()
    {
        var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (p != null)
                p.onItemCollected += OnItemCollected;
        }
    }

    void OnDestroy()
    {
        var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (p != null)
                p.onItemCollected -= OnItemCollected;
        }
    }

    void OnItemCollected(PickableItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId)) return;
        if (QuestSystem.Instance == null || QuestSystem.Instance.activeQuests == null) return;

        var q = QuestSystem.Instance.activeQuests.Find(x => x != null && x.title == "Traverser l'obstacle");
        if (q == null) return;

        string id = item.itemId.Trim();
        if (id.Equals(boardItemId, System.StringComparison.OrdinalIgnoreCase))
            q.CompleteStep(boardStepIndex);
        else if (id.Equals(rampItemId, System.StringComparison.OrdinalIgnoreCase))
            q.CompleteStep(rampStepIndex);
    }
}
