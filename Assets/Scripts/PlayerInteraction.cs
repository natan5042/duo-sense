using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerType
{
    Normal,      // Обычный игрок
    Wheelchair   // Игрок на коляске
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("Точка в руках (куда крепить модель)")]
    public Transform holdPoint;
    
    [Header("Префабы куч (для визуализации)")]
    public GameObject sortedPilePrefab;
    public GameObject wetPilePrefab;
    
    [Header("Тип игрока")]
    public PlayerType playerType = PlayerType.Normal; // Тип игрока (на коляске или нет)
    [Tooltip("Автоматически определять тип игрока по компонентам")]
    public bool autoDetectPlayerType = true; // Автоматическое определение типа

    private GameObject currentVisualModel;

    void Start()
    {
        // Автоматически определяем тип игрока
        if (autoDetectPlayerType)
        {
            DetectPlayerType();
        }
        
        // При старте сцены проверяем, что мы принесли с прошлой сцены
        UpdateVisuals();
    }
    
    public void DetectPlayerType()
    {
        // Проверяем наличие компонента TopDownWheelchair (основной компонент коляски в сцене)
        // Используем рефлексию для поиска компонента, так как namespace может быть разным
        Component[] allComponents = GetComponents<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp != null && comp.GetType().Name == "TopDownWheelchair")
            {
                playerType = PlayerType.Wheelchair;
                Debug.Log($"PlayerInteraction: Тип игрока: Wheelchair (компонент {comp.GetType().Name} найден)");
                return;
            }
        }
        
        // Проверяем в дочерних объектах
        allComponents = GetComponentsInChildren<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp != null && comp.GetType().Name == "TopDownWheelchair")
            {
                playerType = PlayerType.Wheelchair;
                Debug.Log($"PlayerInteraction: Тип игрока: Wheelchair (компонент {comp.GetType().Name} найден в дочерних объектах)");
                return;
            }
        }
        
        // По умолчанию - обычный игрок
        playerType = PlayerType.Normal;
        Debug.Log($"PlayerInteraction: Тип игрока: Normal (объект: {gameObject.name})");
    }

    public void PickUpItem(HeldItemType type)
    {
        GlobalPlayerState.currentItem = type;
        UpdateVisuals();
    }

    public void DropItem()
    {
        GlobalPlayerState.ClearHands();
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (currentVisualModel != null) Destroy(currentVisualModel);

        if (GlobalPlayerState.currentItem == HeldItemType.SortedPile)
            currentVisualModel = Instantiate(sortedPilePrefab, holdPoint);
        else if (GlobalPlayerState.currentItem == HeldItemType.WetPile)
            currentVisualModel = Instantiate(wetPilePrefab, holdPoint);
            
        // Обнуляем позицию, чтобы префаб встал ровно в руку
        if(currentVisualModel != null) 
        {
            currentVisualModel.transform.localPosition = Vector3.zero;
            currentVisualModel.transform.localRotation = Quaternion.identity;
        }
    }

    [Header("Настройки взаимодействия")]
    public float interactionRange = 3f; // Радиус взаимодействия
    public LayerMask interactableLayer = -1; // Все слои (можно настроить)
    
    private IInteractable currentInteractable = null; // Текущий объект для взаимодействия
    
    // Простейшая система взаимодействия на E
    void Update()
    {
        // БЛОКИРУЕМ взаимодействие, если любая мини-игра активна
        if (ClothesLineGame.IsMiniGameActive || ShelfGame.IsShelfGameActive || CookingGame.IsCookingGameActive)
        {
            return; // Не обрабатываем взаимодействие, пока мини-игра активна
        }
        
        // Постоянно ищем ближайший интерактивный объект
        FindNearestInteractable();
        
        // Обрабатываем нажатие клавиш (M для корзины и стиралки, E для остального)
        bool shouldInteract = false;
        KeyCode requiredKey = KeyCode.E;
        
        if (currentInteractable != null)
        {
            // Корзина и стиралка - клавиша M (для мужика на коляске)
            if (currentInteractable is LaundryBasket || currentInteractable is WashingMachine)
            {
                requiredKey = KeyCode.M;
            }
            // Остальное - клавиша E
            else
            {
                requiredKey = KeyCode.E;
            }
            
            shouldInteract = Input.GetKeyDown(requiredKey);
        }
        else
        {
            // Fallback: пробуем E
            shouldInteract = Input.GetKeyDown(KeyCode.E);
        }
        
        if (shouldInteract)
        {
            if (currentInteractable != null)
            {
                Debug.Log("PlayerInteraction: Взаимодействие с " + currentInteractable.GetType().Name);
                currentInteractable.Interact(this);
            }
            else
            {
                // Fallback: пробуем Raycast
                TryRaycastInteraction();
            }
        }
    }
    
    void FindNearestInteractable()
    {
        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;
        
        // Метод 1: OverlapSphere для 3D (самый надежный)
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);
        foreach (Collider col in colliders)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }
        }
        
        // Метод 2: OverlapCircle для 2D (если 3D не нашел)
        if (nearest == null)
        {
            Collider2D[] colliders2D = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y), interactionRange);
            foreach (Collider2D col in colliders2D)
            {
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.y), 
                                                     new Vector2(col.transform.position.x, col.transform.position.y));
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = interactable;
                    }
                }
            }
        }
        
        currentInteractable = nearest;
    }
    
    void TryRaycastInteraction()
    {
        // Ищем камеру для правильного Raycast
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            Vector3 rayOrigin = cam.transform.position;
            Vector3 rayDirection = cam.transform.forward;
            
            // Пробуем 3D Raycast
            RaycastHit hit3D;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit3D, interactionRange))
            {
                var interactable = hit3D.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    Debug.Log("PlayerInteraction: Raycast нашел IInteractable: " + hit3D.collider.name);
                    interactable.Interact(this);
                    return;
                }
            }
            
            // Пробуем 2D Raycast
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Vector3 worldPoint = cam.ScreenToWorldPoint(screenCenter);
            Vector2 rayOrigin2D = new Vector2(worldPoint.x, worldPoint.y);
            Vector3 forward3D = cam.transform.forward;
            Vector2 rayDirection2D = new Vector2(forward3D.x, forward3D.y).normalized;
            
            if (rayDirection2D.magnitude < 0.1f)
            {
                rayDirection2D = Vector2.right;
            }
            
            RaycastHit2D hit2D = Physics2D.Raycast(rayOrigin2D, rayDirection2D, interactionRange);
            if (hit2D.collider != null)
            {
                var interactable = hit2D.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    Debug.Log("PlayerInteraction: 2D Raycast нашел IInteractable: " + hit2D.collider.name);
                    interactable.Interact(this);
                    return;
                }
            }
        }
    }
    
    // Для визуализации радиуса взаимодействия в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

// (IInteractable is declared in Assets/script/general/IInteractable.cs)