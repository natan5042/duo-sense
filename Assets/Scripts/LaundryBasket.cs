using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameQuests;

public class LaundryBasket : MonoBehaviour, IInteractable
{
    [Header("UI Сортировки")]
    public GameObject sortingCanvas; // Ссылка на Canvas сортировки
    public Camera uiCamera; // Камера для UI (если Screen Space - Camera)
    
    [Header("Зоны сортировки")]
    public GameObject zoneBlack; // Зона для черной одежды
    public GameObject zoneWhite; // Зона для белой одежды
    public GameObject zoneColor; // Зона для цветной одежды

    [Header("Данные")]
    public List<GameObject> clothesIcons; // Список иконок одежды
    private Dictionary<int, List<ClothesIconDrag>> sortedClothes = new Dictionary<int, List<ClothesIconDrag>>(); // Отсортированная одежда по зонам
    private int neededCount = 5; // Сколько нужно отсортировать 

    void Start()
    {
        // Инициализируем словарь для отсортированной одежды
        sortedClothes[0] = new List<ClothesIconDrag>(); // Черная
        sortedClothes[1] = new List<ClothesIconDrag>(); // Белая
        sortedClothes[2] = new List<ClothesIconDrag>(); // Цветная
        
        // Находим камеру, если не назначена
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            if (uiCamera == null) uiCamera = FindFirstObjectByType<Camera>();
        }
        
        // Инициализируем квест
        InitializeQuest();
    }
    
    void InitializeQuest()
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem == null)
        {
            Debug.LogError("LaundryBasket: QuestSystem.Instance == null! Квест не может быть создан. Убедитесь что в сцене есть объект с компонентом QuestSystem.");
            return;
        }
        
        // Проверяем, не создан ли уже квест стирки
        foreach (Quest q in questSystem.activeQuests)
        {
            if (q != null && q.questName == "Faire la lessive")
            {
                Debug.Log("LaundryBasket: Квест стирки уже существует, используем существующий");
                return;
            }
        }
        
        Quest laundryQuest = new Quest("Faire la lessive");
        laundryQuest.AddStep("Trier le linge", "Achille");
        laundryQuest.AddStep("Charger la machine à laver", "Achille");
        laundryQuest.AddStep("Accrocher le linge", "Iris");
        
        questSystem.AddQuest(laundryQuest);
        Debug.Log($"LaundryBasket: ✓ Квест стирки создан и добавлен в систему. Всего активных квестов: {questSystem.activeQuests.Count}");
    }
    
    [Header("Ограничения")]
    public bool onlyWheelchairPlayer = true; // Только игрок на коляске может взаимодействовать
    
    private bool hasInteracted = false; // Флаг для скрытия подсказки после первого взаимодействия

    public void Interact(PlayerInteraction player)
    {
        Debug.Log("LaundryBasket: Interact вызван! Руки: " + GlobalPlayerState.currentItem);
        Debug.Log($"LaundryBasket: Игрок: {player.gameObject.name}, Тип: {player.playerType}, onlyWheelchairPlayer: {onlyWheelchairPlayer}");
        
        // Проверяем тип игрока
        if (onlyWheelchairPlayer)
        {
            // Всегда пробуем определить тип заново (на случай, если определение не сработало)
            if (player.autoDetectPlayerType)
            {
                PlayerType oldType = player.playerType;
                player.DetectPlayerType();
                Debug.Log($"LaundryBasket: Тип игрока переопределен: {oldType} -> {player.playerType}");
            }
            
            if (player.playerType != PlayerType.Wheelchair)
            {
                Debug.LogWarning($"LaundryBasket: Только игрок на коляске может взаимодействовать с корзиной! Игрок: {player.gameObject.name}, Тип: {player.playerType}");
                return;
            }
            
            Debug.Log($"LaundryBasket: Проверка типа игрока пройдена - это игрок на коляске! ({player.gameObject.name})");
        }
        
        if (GlobalPlayerState.currentItem == HeldItemType.None)
        {
            if (sortingCanvas == null)
            {
                Debug.LogError("LaundryBasket: sortingCanvas НЕ ПРИВЯЗАН! Привяжи Panel_Menu в Inspector!");
                return;
            }
            
            Debug.Log("LaundryBasket: Открываю меню сортировки...");
            
            // Активируем панель
            sortingCanvas.SetActive(true);
            
            // Центрируем панель относительно камеры
            CenterPanelToCamera();
            
            // Сбрасываем состояние сортировки
            ResetSorting();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartSortingGame();
            hasInteracted = true; // Отмечаем, что взаимодействовали
            
            // Обновляем квест - шаг 0: Trier le linge
            UpdateQuestStep(0);
            
            Debug.Log("LaundryBasket: Меню открыто!");
        }
        else
        {
            Debug.Log("LaundryBasket: В руках что-то есть! Нужно сначала выбросить.");
        }
    }
    
    public bool HasInteracted()
    {
        return hasInteracted;
    }
    
    void UpdateQuestStep(int stepIndex)
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem != null)
        {
            // Ищем квест стирки
            foreach (Quest quest in questSystem.activeQuests)
            {
                if (quest != null && quest.questName == "Faire la lessive")
                {
                    quest.CompleteStep(stepIndex);
                    Debug.Log($"LaundryBasket: Шаг {stepIndex} квеста стирки выполнен!");
                    return;
                }
            }
        }
    }
    
    void CenterPanelToCamera()
    {
        if (sortingCanvas == null || uiCamera == null) return;
        
        Canvas canvas = sortingCanvas.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        
        // Если Canvas в режиме Screen Space - Camera, настраиваем его
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f; // Расстояние от камеры
        }
        
        // Настраиваем панель - по центру с небольшими полями
        RectTransform panelRect = sortingCanvas.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Якоря по центру
            panelRect.anchorMin = new Vector2(0.05f, 0.05f); // Небольшие поля (5%)
            panelRect.anchorMax = new Vector2(0.95f, 0.95f); // Небольшие поля (5%)
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
        }
    }

    void StartSortingGame()
    {
        // Сбрасываем все иконки
        ResetSorting();
        
        // Активируем все иконки одежды
        foreach (GameObject icon in clothesIcons)
        {
            if (icon != null)
            {
                icon.SetActive(true);
                ClothesIconDrag drag = icon.GetComponent<ClothesIconDrag>();
                if (drag != null)
                {
                    drag.ResetPosition();
                    drag.clothesCategory = -1;
                    drag.UpdateVisualFeedback(); // Сбрасываем визуальную обратную связь
                }
            }
        }
    }
    
    void ResetSorting()
    {
        // Очищаем отсортированную одежду
        sortedClothes[0].Clear();
        sortedClothes[1].Clear();
        sortedClothes[2].Clear();
    }
    
    // Вызывается когда иконка брошена в зону
    public void OnClothesDroppedInZone(ClothesIconDrag iconDrag, int zoneIndex)
    {
        if (iconDrag == null || zoneIndex < 0 || zoneIndex > 2) return;
        
        // Удаляем из предыдущей зоны, если была
        foreach (var list in sortedClothes.Values)
        {
            list.Remove(iconDrag);
        }
        
        // Добавляем в новую зону
        sortedClothes[zoneIndex].Add(iconDrag);
        iconDrag.clothesCategory = zoneIndex;
        
        // Обновляем визуальную обратную связь
        iconDrag.UpdateVisualFeedback();
        
        string zoneName = zoneIndex == 0 ? "черная" : (zoneIndex == 1 ? "белая" : "цветная");
        bool isCorrect = iconDrag.IsCorrectlySorted();
        Debug.Log($"LaundryBasket: Одежда брошена в зону '{zoneName}' (зона {zoneIndex}). Правильно: {isCorrect}. Всего в зоне: {sortedClothes[zoneIndex].Count}");
        
        // Проверяем, все ли отсортировано
        CheckIfAllSorted();
    }
    
    void CheckIfAllSorted()
    {
        int totalSorted = sortedClothes[0].Count + sortedClothes[1].Count + sortedClothes[2].Count;
        
        // Проверяем, что все иконки отсортированы
        if (totalSorted >= neededCount)
        {
            // Проверяем правильность сортировки
            bool allCorrect = true;
            int correctCount = 0;
            
            foreach (GameObject iconObj in clothesIcons)
            {
                if (iconObj == null || !iconObj.activeSelf) continue;
                
                ClothesIconDrag drag = iconObj.GetComponent<ClothesIconDrag>();
                if (drag != null)
                {
                    // Если иконка не отсортирована - не все готово
                    if (drag.clothesCategory == -1)
                    {
                        allCorrect = false;
                        break;
                    }
                    
                    // Проверяем правильность
                    if (drag.IsCorrectlySorted())
                    {
                        correctCount++;
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }
            }
            
            if (allCorrect && correctCount == neededCount)
        {
                Debug.Log($"LaundryBasket: Вся одежда правильно отсортирована! ({correctCount}/{neededCount}) Закрываю меню...");
            FinishSorting();
        }
            else
            {
                Debug.Log($"LaundryBasket: Одежда отсортирована, но не все правильно! Правильно: {correctCount}/{neededCount}");
            }
        }
    }
    
    // Получить массив зон (для ClothesIconDrag)
    public GameObject[] GetSortingZones()
    {
        return new GameObject[] { zoneBlack, zoneWhite, zoneColor };
    }

    void FinishSorting()
    {
        Debug.Log("LaundryBasket: FinishSorting вызван!");
        
        if (sortingCanvas != null)
    {
        sortingCanvas.SetActive(false);
            Debug.Log("LaundryBasket: Меню закрыто!");
        }
        else
        {
            Debug.LogError("LaundryBasket: sortingCanvas == null! Не могу закрыть меню!");
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Выдаем игроку кучу
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.PickUpItem(HeldItemType.SortedPile);
        Debug.Log("Сортировка окончена! Куча в руках.");
        }
        else
        {
            Debug.LogError("LaundryBasket: PlayerInteraction не найден!");
        }
        
        // Сбрасываем состояние
        ResetSorting();
    }
}