using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GameQuests;

/// <summary>
/// Игра с полкой - женщина управляет рукой через WASD,
/// мужик подсказывает какой продукт взять
/// </summary>
public class ShelfGame : MonoBehaviour, IInteractable
{
    [Header("UI Панель")]
    public GameObject shelfCanvas; // Панель с полкой
    public Camera uiCamera;
    public Image shelfBackground; // Фон полки (картинка с полочками)
    public Sprite shelfBackgroundSprite; // Спрайт фона для загрузки
    
    [Header("Рука/Курсор")]
    public Image handCursor; // Изображение руки (можно назначить в Inspector)
    public Sprite handCursorSprite; // Спрайт руки (можно загрузить PNG напрямую)
    public float handSpeed = 300f; // Скорость движения руки
    public RectTransform handRect; // RectTransform руки
    
    [Header("Продукты на полке")]
    public List<ShelfProduct> products = new List<ShelfProduct>(); // Продукты с позициями
    public Image highlightFrame; // Рамка подсветки текущего продукта
    
    [Header("Задание")]
    public ProductType requiredProduct = ProductType.Tomato; // Какой продукт нужен
    public Text taskText; // Текст задания
    public Text feedbackText; // Текст обратной связи
    
    [Header("Аудио")]
    public AudioSource audioSource;
    public AudioClip correctSound; // Звук правильного выбора
    public AudioClip wrongSound; // Звук неправильного выбора
    
    [Header("Настройки")]
    public bool onlyNormalPlayer = true; // Только женщина (Normal) может играть
    public float handBoundsMargin = 50f; // Отступ от краёв панели
    
    [Header("Префаб продукта (ОПЦИОНАЛЬНО)")]
    [Tooltip("Если не назначен - продукт не будет визуально появляться в мире, но квест все равно будет работать")]
    public GameObject productPrefab; // Префаб для спавна продукта в мире (опционально)
    [Tooltip("Точка спавна продукта. Если не назначена - спавнится на позиции объекта полки")]
    public Transform productSpawnPoint; // Точка спавна продукта (опционально)
    
    // Статический флаг для блокировки других обработчиков
    public static bool IsShelfGameActive { get; private set; } = false;
    
    private bool isPlaying = false;
    private Vector2 handPosition;
    private int currentHighlightIndex = -1;
    private RectTransform canvasRect;
    private Quest cookingQuest; // Квест готовки
    
    void Start()
    {
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            if (uiCamera == null) uiCamera = FindFirstObjectByType<Camera>();
        }
        
        // Устанавливаем фон
        if (shelfBackground != null && shelfBackgroundSprite != null)
        {
            shelfBackground.sprite = shelfBackgroundSprite;
        }
        
        // Находим handCursor автоматически если не назначен
        if (handCursor == null)
        {
            GameObject handObj = GameObject.Find("HandCursor");
            if (handObj == null)
            {
                // Ищем в Canvas
                if (shelfCanvas != null)
                {
                    handObj = shelfCanvas.transform.Find("Panel_ShelfGame/HandCursor")?.gameObject;
                }
            }
            
            if (handObj != null)
            {
                handCursor = handObj.GetComponent<Image>();
            }
        }
        
        // Устанавливаем спрайт руки если он назначен
        if (handCursorSprite != null && handCursor != null)
        {
            handCursor.sprite = handCursorSprite;
            Debug.Log("ShelfGame: Спрайт руки установлен из handCursorSprite");
        }
        
        // Получаем RectTransform руки
        if (handRect == null && handCursor != null)
        {
            handRect = handCursor.GetComponent<RectTransform>();
        }
        
        // Валидация продуктов
        ValidateProducts();
        
        // Создаём квест готовки
        InitializeCookingQuest();
    }
    
    void ValidateProducts()
    {
        Debug.Log($"ShelfGame: Валидация продуктов. Всего продуктов: {products.Count}, Требуется: {requiredProduct}");
        
        // Проверяем что все продукты имеют правильные настройки
        for (int i = 0; i < products.Count; i++)
        {
            ShelfProduct product = products[i];
            if (product.productSprite == null)
            {
                Debug.LogError($"ShelfGame: Продукт {i} ({product.productType}) не имеет productSprite! Это обязательно!");
            }
            else if (product.productImage == null)
            {
                Debug.LogWarning($"ShelfGame: Продукт {i} ({product.productType}) не имеет productImage - будет создан автоматически");
            }
            else
            {
                Debug.Log($"ShelfGame: Продукт {i}: {product.productType}, Sprite: {product.productSprite.name}, Image: {product.productImage.name}, Radius: {product.selectionRadius}");
            }
        }
        
        // Проверяем что requiredProduct есть в списке
        bool foundRequired = false;
        foreach (var product in products)
        {
            if (product.productType == requiredProduct)
            {
                foundRequired = true;
                Debug.Log($"ShelfGame: ✓ Требуемый продукт {requiredProduct} найден в списке!");
                break;
            }
        }
        
        if (!foundRequired)
        {
            Debug.LogError($"ShelfGame: ⚠ Требуемый продукт {requiredProduct} НЕ найден в списке продуктов! Проверь настройки в Inspector!");
        }
    }
    
    void InitializeCookingQuest()
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem != null && cookingQuest == null)
        {
            cookingQuest = new Quest("Cuisine");
            cookingQuest.AddStep("Trouve le bon produit sur l'étagère", "Iris");
            cookingQuest.AddStep("Cuisine avec Achille à la cuisinière", "Iris et Achille");
            
            questSystem.AddQuest(cookingQuest);
            Debug.Log("ShelfGame: Квест готовки создан");
        }
    }
    
    void Update()
    {
        if (!isPlaying) return;
        
        // Убеждаемся что handRect назначен
        if (handRect == null)
        {
            if (handCursor != null)
            {
                handRect = handCursor.GetComponent<RectTransform>();
            }
            
            if (handRect == null)
            {
                Debug.LogWarning("ShelfGame: handRect не найден! Рука не может двигаться.");
                return;
            }
        }
        
        // Убеждаемся что handCursor видим
        if (handCursor != null && !handCursor.gameObject.activeInHierarchy)
        {
            handCursor.gameObject.SetActive(true);
        }
        
        // WASD управление рукой
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) input.y += 1;
        if (Input.GetKey(KeyCode.S)) input.y -= 1;
        if (Input.GetKey(KeyCode.A)) input.x -= 1;
        if (Input.GetKey(KeyCode.D)) input.x += 1;
        
        // Двигаем руку только если есть ввод
        if (input.magnitude > 0.01f)
        {
            handPosition += input.normalized * handSpeed * Time.deltaTime;
        }
        
        // Ограничиваем позицию руки
        if (canvasRect != null)
        {
            float halfWidth = canvasRect.rect.width / 2 - handBoundsMargin;
            float halfHeight = canvasRect.rect.height / 2 - handBoundsMargin;
            handPosition.x = Mathf.Clamp(handPosition.x, -halfWidth, halfWidth);
            handPosition.y = Mathf.Clamp(handPosition.y, -halfHeight, halfHeight);
        }
        else
        {
            // Если canvasRect не назначен, используем разумные ограничения
            handPosition.x = Mathf.Clamp(handPosition.x, -400, 400);
            handPosition.y = Mathf.Clamp(handPosition.y, -300, 300);
        }
        
        // Применяем позицию
        if (handRect != null)
        {
            handRect.anchoredPosition = handPosition;
        }
        
        // Проверяем, над каким продуктом рука
        UpdateHighlight();
        
        // E для взятия продукта
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickProduct();
        }
        
        // Escape для выхода
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShelf();
        }
    }
    
    void UpdateHighlight()
    {
        if (handRect == null) return;
        
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;
        
        // Получаем локальную позицию руки
        Vector2 handLocalPos = handRect.anchoredPosition;
        
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i].productImage == null) continue;
            
            RectTransform productRect = products[i].productImage.GetComponent<RectTransform>();
            if (productRect == null) continue;
            
            // Простое расстояние между позициями
            Vector2 productLocalPos = productRect.anchoredPosition;
            float distance = Vector2.Distance(handLocalPos, productLocalPos);
            
            // ОГРОМНЫЙ радиус - минимум 300 пикселей, или 200% от размера продукта
            Vector2 productSize = productRect.sizeDelta;
            float minRadius = 300f; // Минимум 300 пикселей
            float sizeBasedRadius = Mathf.Max(productSize.x, productSize.y) * 2.0f; // 200% от размера
            float effectiveRadius = Mathf.Max(products[i].selectionRadius, minRadius, sizeBasedRadius);
            
            // Если расстояние меньше радиуса - это ближайший продукт
            if (distance < effectiveRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        
        // Обновляем подсветку
        if (nearestIndex != currentHighlightIndex)
        {
            currentHighlightIndex = nearestIndex;
            
            if (highlightFrame != null)
            {
                if (nearestIndex >= 0 && products[nearestIndex].productImage != null)
                {
                    highlightFrame.gameObject.SetActive(true);
                    RectTransform productRect = products[nearestIndex].productImage.GetComponent<RectTransform>();
                    RectTransform frameRect = highlightFrame.GetComponent<RectTransform>();
                    
                    frameRect.anchoredPosition = productRect.anchoredPosition;
                    frameRect.sizeDelta = productRect.sizeDelta;
                    
                    Debug.Log($"ShelfGame: Подсвечен продукт {nearestIndex}: {products[nearestIndex].productType}, Required: {requiredProduct}");
                }
                else
                {
                    highlightFrame.gameObject.SetActive(false);
                }
            }
        }
    }
    
    void TryPickProduct()
    {
        // ПРОСТАЯ ЛОГИКА: используем currentHighlightIndex
        int selectedIndex = currentHighlightIndex;
        
        Debug.Log($"\n========== TryPickProduct ==========");
        Debug.Log($"currentHighlightIndex: {selectedIndex}");
        Debug.Log($"products.Count: {products.Count}");
        Debug.Log($"requiredProduct: {requiredProduct} ({(int)requiredProduct})");
        
        // Выводим ВСЕ продукты для отладки
        Debug.Log($"--- ВСЕ ПРОДУКТЫ В СПИСКЕ ---");
        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i];
            Debug.Log($"  [{i}] ProductType: {p.productType} ({(int)p.productType}), Image: {(p.productImage != null ? p.productImage.name : "NULL")}, Sprite: {(p.productSprite != null ? p.productSprite.name : "NULL")}");
        }
        Debug.Log($"--- КОНЕЦ СПИСКА ---\n");
        
        if (selectedIndex < 0 || selectedIndex >= products.Count)
        {
            SetFeedback("Rien ici...", Color.gray);
            Debug.LogError($"❌ НЕТ ВЫБРАННОГО ПРОДУКТА! selectedIndex={selectedIndex}, products.Count={products.Count}");
            return;
        }
        
        ShelfProduct selected = products[selectedIndex];
        
        Debug.Log($"✅ ВЫБРАН ПРОДУКТ ПО ИНДЕКСУ {selectedIndex}:");
        Debug.Log($"   ProductType: {selected.productType} ({(int)selected.productType})");
        Debug.Log($"   Image Name: {(selected.productImage != null ? selected.productImage.name : "NULL")}");
        Debug.Log($"   Sprite Name: {(selected.productSprite != null ? selected.productSprite.name : "NULL")}");
        Debug.Log($"");
        Debug.Log($"🎯 ТРЕБУЕТСЯ:");
        Debug.Log($"   ProductType: {requiredProduct} ({(int)requiredProduct})");
        Debug.Log($"");
        
        // ПРОСТОЕ СРАВНЕНИЕ
        bool isCorrect = (selected.productType == requiredProduct);
        
        Debug.Log($"🔍 СРАВНЕНИЕ:");
        Debug.Log($"   selected.productType == requiredProduct: {isCorrect}");
        Debug.Log($"   selected.productType: {selected.productType} ({(int)selected.productType})");
        Debug.Log($"   requiredProduct: {requiredProduct} ({(int)requiredProduct})");
        Debug.Log($"   Сравнение по int: {(int)selected.productType == (int)requiredProduct}");
        Debug.Log($"   Сравнение по строке: {selected.productType.ToString()} == {requiredProduct.ToString()}: {selected.productType.ToString() == requiredProduct.ToString()}");
        Debug.Log($"=====================================\n");
        
        if (isCorrect)
        {
            // Правильный продукт!
            Debug.Log($"✓✓✓ ПРАВИЛЬНЫЙ ПРОДУКТ! ✓✓✓");
            SetFeedback("C'est bon! Tu as trouvé!", Color.green);
            PlaySound(correctSound);
            
            // Сохраняем продукт в глобальное состояние
            CookingState.PickProduct(selected.productType);
            
            // Спавним префаб продукта в мире
            SpawnProductInWorld(selected);
            
            // Завершаем первый шаг квеста
            if (cookingQuest != null)
            {
                cookingQuest.CompleteStep(0);
                Debug.Log("ShelfGame: Шаг 1 квеста готовки выполнен!");
            }
            
            // Закрываем панель с задержкой
            StartCoroutine(CloseAfterDelay(1.5f));
        }
        else
        {
            // Неправильный продукт
            string wrongMessage = $"Non, ce n'est pas ça! C'est {GetProductNameFrench(selected.productType)}. Cherche {GetProductNameFrench(requiredProduct)}!";
            SetFeedback(wrongMessage, Color.red);
            PlaySound(wrongSound);
            
            Debug.LogWarning($"✗✗✗ НЕПРАВИЛЬНЫЙ ПРОДУКТ! ✗✗✗");
            Debug.LogWarning($"Выбран: {selected.productType} ({(int)selected.productType})");
            Debug.LogWarning($"Нужен: {requiredProduct} ({(int)requiredProduct})");
            
            // Очищаем сообщение через 2 секунды
            StartCoroutine(ClearFeedbackAfterDelay(2f));
        }
    }
    
    void SpawnProductInWorld(ShelfProduct product)
    {
        // Опционально: если prefab не назначен, просто не спавним визуальный объект
        // Квест все равно будет работать через CookingState
        if (productPrefab == null)
        {
            Debug.Log("ShelfGame: Product Prefab не назначен - визуальный объект не будет создан, но продукт сохранен в состоянии");
            return;
        }
        
        Vector3 spawnPos = productSpawnPoint != null ? productSpawnPoint.position : transform.position + Vector3.up;
        
        GameObject spawned = Instantiate(productPrefab, spawnPos, Quaternion.identity);
        
        // Настраиваем компонент ProductItem
        ProductItem item = spawned.GetComponent<ProductItem>();
        if (item == null) item = spawned.AddComponent<ProductItem>();
        item.productType = product.productType;
        
        // Если есть SpriteRenderer, устанавливаем спрайт
        SpriteRenderer sr = spawned.GetComponent<SpriteRenderer>();
        if (sr != null && product.productSprite != null)
        {
            sr.sprite = product.productSprite;
        }
        
        // Добавляем женщине предмет
        PlayerInteraction player = FindPlayerByType(PlayerType.Normal);
        if (player != null)
        {
            // Привязываем к игроку (можно настроить как в стирке)
            spawned.transform.SetParent(player.transform);
            spawned.transform.localPosition = new Vector3(0.5f, 0.5f, 0);
        }
    }
    
    PlayerInteraction FindPlayerByType(PlayerType type)
    {
        PlayerInteraction[] players = FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.playerType == type) return p;
        }
        return null;
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseShelf();
    }
    
    IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }
    
    public void Interact(PlayerInteraction player)
    {
        // Проверяем тип игрока
        if (onlyNormalPlayer && player.playerType != PlayerType.Normal)
        {
            Debug.Log("ShelfGame: Только женщина может взаимодействовать с полкой!");
            return;
        }
        
        // Проверяем, нет ли уже продукта
        if (CookingState.hasProduct)
        {
            Debug.Log("ShelfGame: Уже есть продукт!");
            return;
        }
        
        OpenShelf();
    }
    
    public void OpenShelf()
    {
        if (isPlaying) return;
        
        // Если shelfCanvas не назначен, пытаемся найти автоматически
        if (shelfCanvas == null)
        {
            // Пробуем найти по стандартным именам
            GameObject found = GameObject.Find("ShelfCanvas");
            if (found == null)
            {
                found = GameObject.Find("ShelfGameCanvas");
            }
            
            if (found == null)
            {
                // Ищем Canvas с именем содержащим "Shelf"
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas c in allCanvases)
                {
                    string name = c.name.ToLower();
                    if (name.Contains("shelf"))
                    {
                        shelfCanvas = c.gameObject;
                        Debug.Log($"ShelfGame: Найден Canvas автоматически: {c.name}");
                        break;
                    }
                }
            }
            else
            {
                shelfCanvas = found;
                Debug.Log($"ShelfGame: Найден Canvas автоматически: {found.name}");
            }
            
            if (shelfCanvas == null)
            {
                Debug.LogError("ShelfGame: shelfCanvas не найден! Назначь его в Inspector или создайте Canvas с именем 'ShelfCanvas'");
                isPlaying = false;
                IsShelfGameActive = false;
                return;
            }
        }
        
        isPlaying = true;
        IsShelfGameActive = true;
        
        // Активируем Canvas
        shelfCanvas.SetActive(true);
        
        // Находим Canvas компонент (может быть на самом объекте или на дочернем)
        Canvas canvas = shelfCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = shelfCanvas.GetComponentInChildren<Canvas>();
        }
        
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            // Убеждаемся что Canvas видим
            canvas.enabled = true;
            canvas.gameObject.SetActive(true);
            
            // Настраиваем камеру если нужно
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (canvas.worldCamera == null)
                {
                    if (uiCamera != null)
                    {
                        canvas.worldCamera = uiCamera;
                    }
                    else
                    {
                        canvas.worldCamera = Camera.main;
                    }
                }
            }
            
            // Убеждаемся что sorting order достаточно высокий
            if (canvas.sortingOrder < 50)
            {
                canvas.sortingOrder = 50;
            }
            
            Debug.Log($"ShelfGame: Canvas активирован. RenderMode: {canvas.renderMode}, Camera: {canvas.worldCamera}, SortingOrder: {canvas.sortingOrder}");
        }
        else
        {
            Debug.LogWarning("ShelfGame: Canvas компонент не найден на shelfCanvas!");
        }
        
        // Находим handCursor если не назначен
        if (handCursor == null)
        {
            if (shelfCanvas != null)
            {
                // Ищем в разных местах
                Transform handTransform = shelfCanvas.transform.Find("Panel_ShelfGame/HandCursor");
                if (handTransform == null)
                {
                    handTransform = shelfCanvas.transform.Find("HandCursor");
                }
                if (handTransform == null)
                {
                    // Ищем по компоненту Image
                    Image[] allImages = shelfCanvas.GetComponentsInChildren<Image>(true);
                    foreach (Image img in allImages)
                    {
                        if (img.name.ToLower().Contains("hand") || img.name.ToLower().Contains("cursor"))
                        {
                            handTransform = img.transform;
                            break;
                        }
                    }
                }
                if (handTransform != null)
                {
                    handCursor = handTransform.GetComponent<Image>();
                    Debug.Log($"ShelfGame: Найден handCursor: {handTransform.name}");
                }
            }
        }
        
        // Если все еще не найден, создаем его
        if (handCursor == null && shelfCanvas != null)
        {
            Debug.LogWarning("ShelfGame: handCursor не найден, создаю автоматически...");
            Transform panel = shelfCanvas.transform.Find("Panel_ShelfGame");
            if (panel == null) panel = shelfCanvas.transform;
            
            GameObject handObj = new GameObject("HandCursor");
            handObj.transform.SetParent(panel, false);
            
            handCursor = handObj.AddComponent<Image>();
            if (handCursorSprite != null)
            {
                handCursor.sprite = handCursorSprite;
            }
            else
            {
                // Создаем простой цветной квадрат если нет спрайта
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                handCursor.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            
            handRect = handObj.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.5f, 0.5f);
            handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = new Vector2(0.5f, 0.5f);
            handRect.sizeDelta = new Vector2(50, 50);
            
            Debug.Log("ShelfGame: Создан handCursor автоматически");
        }
        
        // Убеждаемся что handCursor активен и видим
        if (handCursor != null)
        {
            handCursor.gameObject.SetActive(true);
            handCursor.enabled = true;
            handCursor.raycastTarget = false; // Чтобы не блокировал клики
            
            // Устанавливаем спрайт руки если он назначен
            if (handCursorSprite != null)
            {
                handCursor.sprite = handCursorSprite;
            }
            
            // Получаем RectTransform руки
            if (handRect == null)
            {
                handRect = handCursor.GetComponent<RectTransform>();
            }
            
            // Убеждаемся что RectTransform правильно настроен
            if (handRect != null)
            {
                // Если anchor не настроен, центрируем
                if (handRect.anchorMin == handRect.anchorMax && handRect.anchorMin == Vector2.zero)
                {
                    handRect.anchorMin = new Vector2(0.5f, 0.5f);
                    handRect.anchorMax = new Vector2(0.5f, 0.5f);
                    handRect.pivot = new Vector2(0.5f, 0.5f);
                }
                
                // Убеждаемся что курсор виден (высокий sorting order)
                Canvas handCanvas = handRect.GetComponentInParent<Canvas>();
                if (handCanvas != null && handCanvas.sortingOrder < 100)
                {
                    handCanvas.sortingOrder = 100;
                }
                
                Debug.Log($"ShelfGame: handRect настроен. Позиция: {handRect.anchoredPosition}, Size: {handRect.sizeDelta}, Anchor: {handRect.anchorMin}");
            }
        }
        else
        {
            Debug.LogError("ShelfGame: handCursor не может быть создан! Проверь настройки Canvas.");
        }
        
        // Сбрасываем позицию руки в центр
        handPosition = Vector2.zero;
        if (handRect != null)
        {
            handRect.anchoredPosition = handPosition;
            Debug.Log($"ShelfGame: Позиция руки сброшена в центр: {handPosition}");
        }
        
        // Находим taskText и feedbackText если не назначены
        if (taskText == null && shelfCanvas != null)
        {
            Transform taskTransform = shelfCanvas.transform.Find("Panel_ShelfGame/TaskText");
            if (taskTransform == null)
            {
                taskTransform = shelfCanvas.transform.Find("TaskText");
            }
            if (taskTransform != null)
            {
                taskText = taskTransform.GetComponent<Text>();
            }
        }
        
        if (feedbackText == null && shelfCanvas != null)
        {
            Transform feedbackTransform = shelfCanvas.transform.Find("Panel_ShelfGame/FeedbackText");
            if (feedbackTransform == null)
            {
                feedbackTransform = shelfCanvas.transform.Find("FeedbackText");
            }
            if (feedbackTransform != null)
            {
                feedbackText = feedbackTransform.GetComponent<Text>();
            }
        }
        
        // Убеждаемся что тексты активны
        if (taskText != null)
        {
            taskText.gameObject.SetActive(true);
            taskText.enabled = true;
        }
        
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.enabled = true;
            feedbackText.text = ""; // Очищаем предыдущий текст
        }
        else
        {
            Debug.LogWarning("ShelfGame: feedbackText не найден! Обратная связь не будет отображаться.");
        }
        
        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Устанавливаем задание
        SetTask();
        
        // Автоматически находим или создаем продукты
        if (shelfCanvas != null)
        {
            AutoSetupProducts();
        }
        
        // Повторная валидация при открытии
        ValidateProducts();
        
        Debug.Log($"=== ShelfGame: Панель открыта ===");
        Debug.Log($"Требуется продукт: {requiredProduct} ({(int)requiredProduct})");
        Debug.Log($"Всего продуктов в списке: {products.Count}");
        
        // Выводим все продукты с их типами
        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i];
            Debug.Log($"  Продукт {i}: {p.productType} ({(int)p.productType}), Sprite: {(p.productSprite != null ? p.productSprite.name : "NULL")}, Image: {(p.productImage != null ? p.productImage.name : "NULL")}");
        }
    }
    
    void AutoSetupProducts()
    {
        Debug.Log("ShelfGame: Автоматическая настройка продуктов...");
        
        // Находим родительский контейнер для продуктов (обычно это Panel_ShelfGame)
        Transform productsParent = null;
        if (shelfCanvas != null)
        {
            Transform panel = shelfCanvas.transform.Find("Panel_ShelfGame");
            if (panel == null) panel = shelfCanvas.transform;
            productsParent = panel;
        }
        
        if (productsParent == null)
        {
            Debug.LogWarning("ShelfGame: Не найден контейнер для продуктов!");
            return;
        }
        
        // Получаем все Image в UI
        Image[] allImages = productsParent.GetComponentsInChildren<Image>(true);
        
        // Для каждого продукта в списке проверяем/находим Image
        for (int i = 0; i < products.Count; i++)
        {
            ShelfProduct product = products[i];
            
            // Пропускаем если нет спрайта
            if (product.productSprite == null)
            {
                Debug.LogWarning($"ShelfGame: Продукт {i} ({product.productType}) не имеет спрайта!");
                continue;
            }
            
            // Если Image не назначен, ищем его
            if (product.productImage == null)
            {
                Image foundImage = null;
                
                // Сначала ищем по спрайту
                foreach (Image img in allImages)
                {
                    if (img.sprite == product.productSprite)
                    {
                        foundImage = img;
                        break;
                    }
                }
                
                // Если не нашли по спрайту, ищем по имени (Product_Tomato, Product_Pasta и т.д.)
                if (foundImage == null)
                {
                    string productName = $"Product_{product.productType}";
                    foreach (Image img in allImages)
                    {
                        if (img.name.Contains(productName) || img.name.Contains(product.productType.ToString()))
                        {
                            foundImage = img;
                            break;
                        }
                    }
                }
                
                if (foundImage != null)
                {
                    product.productImage = foundImage;
                    // Устанавливаем спрайт если его нет
                    if (foundImage.sprite == null)
                    {
                        foundImage.sprite = product.productSprite;
                    }
                    Debug.Log($"ShelfGame: Найден Image для продукта {product.productType}: {foundImage.name}");
                }
                else
                {
                    Debug.LogWarning($"ShelfGame: Не найден Image для продукта {product.productType}! Назначь его вручную в Inspector.");
                }
            }
            else
            {
                // Если Image назначен, устанавливаем спрайт если его нет
                if (product.productImage.sprite == null && product.productSprite != null)
                {
                    product.productImage.sprite = product.productSprite;
                    Debug.Log($"ShelfGame: Установлен спрайт для продукта {product.productType}");
                }
            }
        }
    }
    
    void SetTask()
    {
        string productName = GetProductNameFrench(requiredProduct);
        if (taskText != null)
        {
            taskText.text = $"Trouve: {productName}";
        }
    }
    
    string GetProductNameFrench(ProductType type)
    {
        switch (type)
        {
            case ProductType.Tomato: return "Tomate";
            case ProductType.Onion: return "Oignon";
            case ProductType.Carrot: return "Carotte";
            case ProductType.Cheese: return "Fromage";
            case ProductType.Egg: return "Œuf";
            case ProductType.Butter: return "Beurre";
            case ProductType.Milk: return "Lait";
            case ProductType.Pasta: return "Pâtes";
            default: return "???";
        }
    }
    
    void SetFeedback(string text, Color color)
    {
        // Если feedbackText не назначен, пытаемся найти
        if (feedbackText == null && shelfCanvas != null)
        {
            Transform feedbackTransform = shelfCanvas.transform.Find("Panel_ShelfGame/FeedbackText");
            if (feedbackTransform == null)
            {
                feedbackTransform = shelfCanvas.transform.Find("FeedbackText");
            }
            if (feedbackTransform != null)
            {
                feedbackText = feedbackTransform.GetComponent<Text>();
            }
        }
        
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            Debug.Log($"ShelfGame Feedback: {text}");
        }
        else
        {
            Debug.LogWarning($"ShelfGame: feedbackText не найден! Сообщение: {text}");
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void CloseShelf()
    {
        isPlaying = false;
        IsShelfGameActive = false;
        
        if (shelfCanvas != null)
        {
            shelfCanvas.SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// Устанавливает какой продукт нужно найти (вызывается мужиком или квестом)
    /// </summary>
    public void SetRequiredProduct(ProductType type)
    {
        requiredProduct = type;
        SetTask();
    }
}

/// <summary>
/// Данные о продукте на полке
/// </summary>
[System.Serializable]
public class ShelfProduct
{
    public ProductType productType;
    [Tooltip("Спрайт продукта (ОБЯЗАТЕЛЬНО)")]
    public Sprite productSprite; // Спрайт продукта (обязательно)
    [Tooltip("UI Image продукта (опционально - будет создан автоматически если не назначен)")]
    public Image productImage; // UI Image продукта на панели (опционально)
    [Tooltip("Позиция продукта на полке (0-1, где 0.5 = центр). Используется если productImage не назначен")]
    public Vector2 positionOnShelf = new Vector2(0.5f, 0.5f); // Позиция на полке (0-1)
    [Tooltip("Размер продукта на полке (в пикселях). Используется если productImage не назначен")]
    public Vector2 productSize = new Vector2(100, 100); // Размер продукта
    public float selectionRadius = 100f; // Радиус выбора (увеличен для удобства)
}
