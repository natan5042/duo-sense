using UnityEngine;
using UnityEngine.UI;

// Скрипт для показа подсказки "Press E" при приближении к интерактивному объекту
// Использует стандартный подход Unity с триггерами
public class InteractionHint : MonoBehaviour
{
    [Header("Настройки подсказки")]
    public string hintText = "Press E";
    public float interactionDistance = 3f; // Расстояние взаимодействия
    public Vector3 hintOffset = new Vector3(0, 1.5f, 0); // Смещение подсказки над объектом
    public string playerTag = "Player"; // Тег игрока
    
    [Header("UI")]
    public GameObject hintCanvasPrefab; // Префаб Canvas с подсказкой (опционально)
    private GameObject hintUI; // Созданный UI элемент
    private Text hintTextComponent;
    private Canvas hintCanvas;
    
    private IInteractable interactable;
    private Transform playerTransform;
    private Camera playerCamera;
    private bool isPlayerNearby = false;
    private Collider triggerCollider;
    private bool hasInteracted = false; // Флаг для скрытия после первого взаимодействия
    
    void Start()
    {
        // Проверяем, есть ли компонент IInteractable
        interactable = GetComponent<IInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning($"InteractionHint на {gameObject.name}: нет компонента IInteractable! Проверяю компоненты...");
            // Пробуем найти компоненты, которые могут быть IInteractable
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp is IInteractable)
                {
                    interactable = comp as IInteractable;
                    Debug.Log($"✓ Найден IInteractable: {comp.GetType().Name}");
                    break;
                }
            }
            
            if (interactable == null)
            {
                Debug.LogError($"InteractionHint на {gameObject.name}: не найден компонент IInteractable! Скрипт отключен.");
                enabled = false;
                return;
            }
        }
        
        Debug.Log($"InteractionHint: Инициализация для {gameObject.name}, IInteractable: {interactable.GetType().Name}");
        
        // Создаем или настраиваем триггер коллайдер
        SetupTriggerCollider();
        
        // Удаляем старый UI, если он есть (для пересоздания с правильными настройками)
        if (hintUI != null)
        {
            DestroyImmediate(hintUI);
            hintUI = null;
            hintCanvas = null;
        }
        
        // Создаем UI для подсказки
        CreateHintUI();
        
        Debug.Log($"InteractionHint: UI создан для {gameObject.name}, hintUI активен: {hintUI != null && hintUI.activeSelf}");
        
        // Находим игрока и камеру
        FindPlayerAndCamera();
    }
    
    void SetupTriggerCollider()
    {
        // Проверяем, есть ли коллайдер (для триггеров)
        Collider col = GetComponent<Collider>();
        Collider2D col2D = GetComponent<Collider2D>();
        
        // Если есть коллайдер, используем его для триггеров
        if (col != null)
        {
            triggerCollider = col;
        }
        
        // Если нет коллайдера - создаем дочерний объект с триггером
        if (col == null && col2D == null)
        {
            GameObject triggerObj = new GameObject("InteractionTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            
            // Пробуем определить, 2D или 3D объект
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 2D объект
                CircleCollider2D trigger = triggerObj.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = interactionDistance;
                Debug.Log($"✓ Создан дочерний CircleCollider2D триггер для {gameObject.name}");
            }
            else
            {
                // 3D объект
                SphereCollider trigger = triggerObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = interactionDistance;
                triggerCollider = trigger;
                Debug.Log($"✓ Создан дочерний SphereCollider триггер для {gameObject.name}");
            }
        }
    }
    
    void CreateHintUI()
    {
        // Создаем Canvas для подсказки (если его нет)
        if (hintCanvasPrefab == null)
        {
            // Создаем Canvas вручную
            GameObject canvasObj = new GameObject("HintCanvas_" + gameObject.name);
            hintCanvas = canvasObj.AddComponent<Canvas>();
            hintCanvas.renderMode = RenderMode.WorldSpace;
            
            // Находим камеру
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
            hintCanvas.worldCamera = mainCam;
            hintCanvas.sortingOrder = 100; // Высокий порядок для видимости
            
            // CanvasScaler для масштабирования
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f; // НЕ используем маленький масштаб
            
            // GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Привязываем к объекту
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = hintOffset;
            canvasObj.transform.localRotation = Quaternion.identity;
            // УВЕЛИЧЕННЫЙ масштаб для видимости в world space
            canvasObj.transform.localScale = Vector3.one * 0.01f; // Правильный масштаб для world space Canvas
            
            // Создаем панель с текстом (меньший размер)
            GameObject panelObj = new GameObject("HintPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // Размер в пикселях для world space Canvas
            panelRect.sizeDelta = new Vector2(400, 100); // БОЛЬШЕ размер для видимости
            panelRect.anchoredPosition = Vector2.zero;
            // Убеждаемся, что панель правильно позиционирована
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Фон панели - ЯРЧЕ
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.9f); // Более темный и яркий фон
            
            // Текст
            GameObject textObj = new GameObject("HintText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            hintTextComponent = textObj.AddComponent<Text>();
            hintTextComponent.text = hintText;
            hintTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintTextComponent.fontSize = 40; // ЕЩЕ БОЛЬШЕ размер шрифта для видимости
            hintTextComponent.alignment = TextAnchor.MiddleCenter;
            hintTextComponent.color = Color.yellow; // ЯРКИЙ желтый цвет
            hintTextComponent.fontStyle = FontStyle.Bold; // Жирный шрифт
            
            hintUI = canvasObj;
            
            // Скрываем по умолчанию
            hintUI.SetActive(false);
            
            Debug.Log($"InteractionHint: UI создан для {gameObject.name}, hintUI != null: {hintUI != null}");
        }
        else
        {
            // Используем префаб
            hintUI = Instantiate(hintCanvasPrefab, transform);
            hintUI.transform.localPosition = hintOffset;
            hintTextComponent = hintUI.GetComponentInChildren<Text>();
            if (hintTextComponent != null)
            {
                hintTextComponent.text = hintText;
            }
        }
        
        // Скрываем подсказку по умолчанию
        if (hintUI != null)
        {
            hintUI.SetActive(false);
        }
    }
    
    void FindPlayerAndCamera()
    {
        // Ищем игрока
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Ищем камеру
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        // Обновляем камеру для Canvas
        if (hintCanvas != null && playerCamera != null)
        {
            hintCanvas.worldCamera = playerCamera;
        }
    }
    
    // Стандартный подход Unity: используем OnTriggerEnter/Exit (если есть триггер)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
            
            // Не показываем, если уже взаимодействовали
            if (!hasInteracted)
            {
                ShowHint();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
            HideHint();
        }
    }
    
    // Для 2D коллайдеров
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
            
            // Не показываем, если уже взаимодействовали
            if (!hasInteracted)
            {
                ShowHint();
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = false;
            HideHint();
        }
    }
    
    void Update()
    {
        // УПРОЩЕННАЯ логика: просто проверяем расстояние
        if (playerTransform == null)
        {
            FindPlayerAndCamera();
        }
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasNearby = isPlayerNearby;
            isPlayerNearby = distance <= interactionDistance;
            
            // Не показываем подсказку, если уже взаимодействовали
            if (hasInteracted)
            {
                HideHint();
                return;
            }
            
            // Показываем/скрываем подсказку
            if (isPlayerNearby && !wasNearby)
            {
                // Всегда пытаемся показать подсказку (UpdateHintText сам проверит текст)
                ShowHint();
            }
            else if (!isPlayerNearby && wasNearby)
            {
                HideHint();
            }
            
            // Обновляем текст подсказки, если игрок рядом
            if (isPlayerNearby)
            {
                UpdateHintText();
            }
            else
            {
                // Если игрок далеко - скрываем
                HideHint();
            }
        }
        
        // Поворачиваем подсказку к камере
        if (hintUI != null && hintUI.activeSelf)
        {
            UpdateHintRotation();
        }
    }
    
    void UpdateHintRotation()
    {
        if (hintUI == null || !hintUI.activeSelf) return;
        
        // Находим камеру, если не найдена
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        }
        
        if (playerCamera != null)
        {
            // Простой billboard эффект - всегда смотрим на камеру
            Vector3 directionToCamera = playerCamera.transform.position - hintUI.transform.position;
            
            // Поворачиваем только по Y оси (горизонтально), чтобы подсказка не наклонялась
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
            {
                hintUI.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
            
            // Обновляем камеру для Canvas
            if (hintCanvas != null && hintCanvas.worldCamera != playerCamera)
            {
                hintCanvas.worldCamera = playerCamera;
            }
        }
    }
    
    void CheckInteractionStatus()
    {
        // УПРОЩЕНО: проверяем только корзину
        if (interactable is LaundryBasket basket && basket.HasInteracted())
        {
            hasInteracted = true;
            HideHint();
        }
    }
    
    // Публичный метод для принудительного показа подсказки (для отладки)
    public void ForceShowHint()
    {
        if (hintUI != null)
        {
            hintUI.SetActive(true);
        }
    }
    
    void ShowHint()
    {
        if (hintUI == null)
        {
            Debug.LogError($"InteractionHint: hintUI == null для {gameObject.name}! Создаю заново...");
            CreateHintUI();
            if (hintUI == null)
            {
                Debug.LogError($"InteractionHint: Не удалось создать hintUI для {gameObject.name}!");
                return;
            }
        }
        
        // Получаем текст подсказки
        string currentText = GetDynamicHintText();
        
        // Если текст пустой - используем стандартный
        if (string.IsNullOrEmpty(currentText))
        {
            currentText = hintText;
            if (string.IsNullOrEmpty(currentText))
            {
                currentText = "Press M";
            }
        }
        
        // Активируем подсказку
        if (!hintUI.activeSelf)
        {
            hintUI.SetActive(true);
            Debug.Log($"InteractionHint: Показываю подсказку для {gameObject.name}, текст: '{currentText}'");
        }
        
        // Обновляем текст
        if (hintTextComponent != null)
        {
            hintTextComponent.text = currentText;
        }
        else
        {
            hintTextComponent = hintUI.GetComponentInChildren<Text>();
            if (hintTextComponent != null)
            {
                hintTextComponent.text = currentText;
            }
        }
    }
    
    void HideHint()
    {
        if (hintUI != null && hintUI.activeSelf)
        {
            hintUI.SetActive(false);
        }
    }
    
    void UpdateHintText()
    {
        if (hintUI == null || hintTextComponent == null) return;
        
        // Получаем актуальный текст подсказки
        string currentText = GetDynamicHintText();
        
        // Если текст пустой - используем стандартный
        if (string.IsNullOrEmpty(currentText))
        {
            currentText = hintText;
            if (string.IsNullOrEmpty(currentText))
            {
                currentText = "Press M";
            }
        }
        
        // Обновляем текст
        hintTextComponent.text = currentText;
        
        // Если игрок рядом и не взаимодействовал - показываем подсказку
        if (isPlayerNearby && !hasInteracted)
        {
            if (!hintUI.activeSelf)
            {
                hintUI.SetActive(true);
            }
        }
    }
    
    string GetDynamicHintText()
    {
        // Проверяем, есть ли специальная логика для получения текста
        if (interactable is WashingMachine machine)
        {
            string machineText = machine.GetHintText();
            if (string.IsNullOrEmpty(machineText))
            {
                Debug.Log($"WashingMachine.GetHintText() вернул пустую строку для {gameObject.name}");
            }
            return machineText;
        }
        
        // Возвращаем стандартный текст
        if (string.IsNullOrEmpty(hintText))
        {
            Debug.LogWarning($"InteractionHint: hintText пустой для {gameObject.name}");
        }
        return hintText;
    }
    
    void OnDestroy()
    {
        if (hintUI != null)
        {
            Destroy(hintUI);
        }
    }
}
