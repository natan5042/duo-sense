using UnityEngine;
using UnityEngine.UI;

public class InteractionHint : MonoBehaviour
{
    public string hintText = "Press E";
    public float interactionDistance = 3f;
    public Vector3 hintOffset = new Vector3(0, 1.5f, 0);
    public string playerTag = "Player";
    public GameObject hintCanvasPrefab;
    public int fontSize = 40;
    public Vector2 panelSize = new Vector2(400, 100);
    private GameObject hintUI;
    private Text hintTextComponent;
    private Canvas hintCanvas;
    
    private IInteractable interactable;
    private Transform playerTransform;
    private Camera playerCamera;
    private bool isPlayerNearby = false;
    private Collider triggerCollider;
    private bool hasInteracted = false;

    void Start()
    {
        interactable = GetComponent<IInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning($"InteractionHint на {gameObject.name}: нет компонента IInteractable!");
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp is IInteractable)
                {
                    interactable = comp as IInteractable;
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
        
        SetupTriggerCollider();

        if (hintUI != null)
        {
            DestroyImmediate(hintUI);
            hintUI = null;
            hintCanvas = null;
        }
        
        CreateHintUI();
        FindPlayerAndCamera();
    }
    
    void SetupTriggerCollider()
    {
        Collider col = GetComponent<Collider>();
        Collider2D col2D = GetComponent<Collider2D>();
        
        if (col != null)
            triggerCollider = col;

        if (col == null && col2D == null)
        {
            GameObject triggerObj = new GameObject("InteractionTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                CircleCollider2D trigger = triggerObj.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = interactionDistance;
            }
            else
            {
                SphereCollider trigger = triggerObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = interactionDistance;
                triggerCollider = trigger;
            }
        }
    }
    
    void CreateHintUI()
    {
        if (hintCanvasPrefab == null)
        {
            GameObject canvasObj = new GameObject("HintCanvas_" + gameObject.name);
            hintCanvas = canvasObj.AddComponent<Canvas>();
            hintCanvas.renderMode = RenderMode.WorldSpace;
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
            hintCanvas.worldCamera = mainCam;
            hintCanvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.position = transform.position + hintOffset;
            canvasObj.transform.localRotation = Quaternion.identity;
            float parentScale = Mathf.Max(0.001f, transform.lossyScale.x);
            canvasObj.transform.localScale = Vector3.one * (0.01f / parentScale);
            GameObject panelObj = new GameObject("HintPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            Vector2 ps = (panelSize.x > 0 && panelSize.y > 0) ? panelSize : new Vector2(400, 100);
            panelRect.sizeDelta = ps;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.9f);
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
            hintTextComponent.fontSize = fontSize > 0 ? fontSize : 40;
            hintTextComponent.alignment = TextAnchor.MiddleCenter;
            hintTextComponent.color = Color.yellow;
            hintTextComponent.fontStyle = FontStyle.Bold;
            hintUI = canvasObj;
            hintUI.SetActive(false);
        }
        else
        {
            hintUI = Instantiate(hintCanvasPrefab, transform);
            hintUI.transform.position = transform.position + hintOffset;
            hintTextComponent = hintUI.GetComponentInChildren<Text>();
            if (hintTextComponent != null)
            {
                hintTextComponent.text = hintText;
            }
        }
        if (hintUI != null)
        {
            hintUI.SetActive(false);
        }
    }
    
    void FindPlayerAndCamera()
    {
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        if (hintCanvas != null && playerCamera != null)
        {
            hintCanvas.worldCamera = playerCamera;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
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
        if (playerTransform == null)
        {
            FindPlayerAndCamera();
        }
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasNearby = isPlayerNearby;
            isPlayerNearby = distance <= interactionDistance;
            if (hasInteracted)
            {
                HideHint();
                return;
            }
            if (isPlayerNearby && !wasNearby)
                ShowHint();
            else if (!isPlayerNearby && wasNearby)
                HideHint();
            if (isPlayerNearby)
            {
                UpdateHintText();
            }
            else
                HideHint();
        }
        if (hintUI != null)
        {
            hintUI.transform.position = transform.position + hintOffset;
            float parentScale = Mathf.Max(0.001f, transform.lossyScale.x);
            hintUI.transform.localScale = Vector3.one * (0.01f / parentScale);
            if (hintUI.activeSelf)
                UpdateHintRotation();
        }
    }

    void UpdateHintRotation()
    {
        if (hintUI == null || !hintUI.activeSelf) return;
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        }
        
        if (playerCamera != null)
        {
            Vector3 directionToCamera = playerCamera.transform.position - hintUI.transform.position;
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
                hintUI.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            if (hintCanvas != null && hintCanvas.worldCamera != playerCamera)
            {
                hintCanvas.worldCamera = playerCamera;
            }
        }
    }
    
    void CheckInteractionStatus()
    {
        if (interactable is LaundryBasket basket && basket.HasInteracted())
        {
            hasInteracted = true;
            HideHint();
        }
    }

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
        string currentText = GetDynamicHintText();
        if (string.IsNullOrEmpty(currentText))
        {
            currentText = hintText;
            if (string.IsNullOrEmpty(currentText))
            {
                currentText = "Press M";
            }
        }
        if (!hintUI.activeSelf)
            hintUI.SetActive(true);
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
        string currentText = GetDynamicHintText();
        if (string.IsNullOrEmpty(currentText))
        {
            currentText = hintText;
            if (string.IsNullOrEmpty(currentText))
            {
                currentText = "Press M";
            }
        }
        hintTextComponent.text = currentText;
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
        if (interactable is WashingMachine machine)
        {
            string machineText = machine.GetHintText();
            if (!string.IsNullOrEmpty(machineText)) return machineText;
        }
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
