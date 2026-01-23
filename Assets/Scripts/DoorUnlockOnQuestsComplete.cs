using UnityEngine;
using GameQuests;

/// <summary>
/// Скрывает спрайт двери когда все квесты выполнены
/// </summary>
public class DoorUnlockOnQuestsComplete : MonoBehaviour
{
    [Header("Настройки двери")]
    [Tooltip("Спрайт двери для скрытия (если не назначен, ищется SpriteRenderer на этом объекте)")]
    public SpriteRenderer doorSprite;
    
    [Tooltip("Или GameObject двери для деактивации (альтернатива SpriteRenderer)")]
    public GameObject doorObject;
    
    [Tooltip("Скрывать дверь (true) или деактивировать GameObject (false)")]
    public bool hideSprite = true;
    
    [Header("Настройки")]
    [Tooltip("Показывать логи отладки")]
    public bool debugLogs = true;
    
    private bool doorHidden = false;
    private bool questsWereCreated = false; // Флаг что квесты были созданы
    
    void Start()
    {
        // Автоматически находим SpriteRenderer если не назначен
        if (doorSprite == null && hideSprite)
        {
            doorSprite = GetComponent<SpriteRenderer>();
            if (doorSprite == null)
            {
                doorSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        // Автоматически находим GameObject если не назначен
        if (doorObject == null && !hideSprite)
        {
            doorObject = gameObject;
        }
        
        if (doorSprite == null && doorObject == null)
        {
            Debug.LogError("DoorUnlockOnQuestsComplete: Не найден ни SpriteRenderer, ни GameObject! Назначьте doorSprite или doorObject в Inspector.");
            return;
        }
        
        // Подписываемся на изменения квестов
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsChanged += CheckQuestsCompletion;
            
            // Проверяем, есть ли уже квесты при старте
            if (QuestSystem.Instance.activeQuests != null && QuestSystem.Instance.activeQuests.Count > 0)
            {
                questsWereCreated = true;
            }
            
            if (debugLogs)
            {
                Debug.Log($"DoorUnlockOnQuestsComplete: Подписан на изменения квестов. Текущее количество: {QuestSystem.Instance.activeQuests.Count}, questsWereCreated: {questsWereCreated}");
            }
        }
        else
        {
            Debug.LogWarning("DoorUnlockOnQuestsComplete: QuestSystem.Instance == null! Дверь не будет скрыта автоматически.");
        }
        
        // НЕ проверяем сразу при старте - дверь должна быть видна пока квесты не выполнены
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsChanged -= CheckQuestsCompletion;
        }
    }
    
    void CheckQuestsCompletion()
    {
        if (doorHidden) return; // Дверь уже скрыта
        
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("DoorUnlockOnQuestsComplete: QuestSystem.Instance == null!");
            }
            return;
        }
        
        // Отслеживаем создание квестов
        if (questSystem.activeQuests != null && questSystem.activeQuests.Count > 0)
        {
            questsWereCreated = true;
            if (debugLogs)
            {
                Debug.Log($"DoorUnlockOnQuestsComplete: Осталось активных квестов: {questSystem.activeQuests.Count}");
            }
            return; // Квесты есть - дверь должна быть видна
        }
        
        // Проверяем, все ли квесты выполнены
        // ВАЖНО: скрываем дверь только если квесты были созданы и затем выполнены
        if (questsWereCreated && (questSystem.activeQuests == null || questSystem.activeQuests.Count == 0))
        {
            HideDoor();
        }
    }
    
    void HideDoor()
    {
        if (doorHidden) return;
        
        doorHidden = true;
        
        if (debugLogs)
        {
            Debug.Log("DoorUnlockOnQuestsComplete: ✓ Все квесты выполнены! Скрываю дверь...");
        }
        
        if (hideSprite && doorSprite != null)
        {
            doorSprite.enabled = false;
            if (debugLogs)
            {
                Debug.Log($"DoorUnlockOnQuestsComplete: SpriteRenderer '{doorSprite.gameObject.name}' отключен.");
            }
        }
        else if (!hideSprite && doorObject != null)
        {
            doorObject.SetActive(false);
            if (debugLogs)
            {
                Debug.Log($"DoorUnlockOnQuestsComplete: GameObject '{doorObject.name}' деактивирован.");
            }
        }
    }
    
    // Метод для ручного скрытия двери (можно вызывать извне)
    public void ForceHideDoor()
    {
        HideDoor();
    }
    
    // Метод для показа двери обратно (для отладки)
    public void ShowDoor()
    {
        doorHidden = false;
        
        if (hideSprite && doorSprite != null)
        {
            doorSprite.enabled = true;
        }
        else if (!hideSprite && doorObject != null)
        {
            doorObject.SetActive(true);
        }
        
        if (debugLogs)
        {
            Debug.Log("DoorUnlockOnQuestsComplete: Дверь показана обратно.");
        }
    }
}
