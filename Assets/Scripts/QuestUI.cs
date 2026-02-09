using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using GameQuests;

/// <summary>
/// UI компонент для отображения активных квестов на экране
/// </summary>
public class QuestUI : MonoBehaviour
{
    [Header("UI Элементы")]
    [Tooltip("Текст для отображения квестов (Legacy Text)")]
    public Text questText;
    
    [Tooltip("Панель с квестами (опционально, для скрытия/показа)")]
    public GameObject questPanel;
    
    [Header("Настройки")]
    [Tooltip("Обновлять квесты каждый кадр (для отладки)")]
    public bool updateEveryFrame = false;
    
    [Tooltip("Позиция на экране (0-1, где 0.5 = центр)")]
    public Vector2 screenPosition = new Vector2(0.02f, 0.98f); // Верхний левый угол
    
    [Tooltip("Размер шрифта")]
    public int fontSize = 16;
    
    [Tooltip("Цвет текста")]
    public Color textColor = Color.white;
    
    private float updateTimer = 0f;
    private float updateInterval = 0.5f; // Обновляем каждые 0.5 секунды
    
    void Start()
    {
        // Автоматически находим Text компонент если не назначен
        if (questText == null)
        {
            questText = GetComponent<Text>();
            if (questText == null)
            {
                questText = GetComponentInChildren<Text>();
            }
        }
        
        // Автоматически находим панель если не назначена
        if (questPanel == null && questText != null)
        {
            questPanel = questText.transform.parent?.gameObject;
        }
        
        // Настраиваем Text если найден
        if (questText != null)
        {
            questText.fontSize = fontSize;
            questText.color = textColor;
            questText.alignment = TextAnchor.UpperLeft;
            questText.supportRichText = true; // ВАЖНО: включаем поддержку Rich Text для HTML тегов
            questText.horizontalOverflow = HorizontalWrapMode.Wrap;
            questText.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Настраиваем позицию - Text должен заполнять всю панель
            RectTransform rect = questText.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Text должен заполнять всю панель с отступами
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(10f, 10f);
                rect.offsetMax = new Vector2(-10f, -10f);
                
                Debug.Log($"QuestUI: RectTransform настроен. Size: {rect.sizeDelta}, Position: {rect.anchoredPosition}, Anchors: min={rect.anchorMin}, max={rect.anchorMax}");
            }
            
            // Проверяем Canvas
            Canvas canvas = questText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"QuestUI: Canvas найден. RenderMode: {canvas.renderMode}, Enabled: {canvas.enabled}, Active: {canvas.gameObject.activeInHierarchy}");
                
                // КРИТИЧЕСКИ ВАЖНО: Проверяем масштаб Canvas - если он 0, текст не будет виден!
                if (canvas.transform.localScale == Vector3.zero)
                {
                    Debug.LogError("QuestUI: Canvas имеет масштаб 0! Исправляю на (1,1,1)...");
                    canvas.transform.localScale = Vector3.one;
                }
                else if (canvas.transform.localScale.x == 0 || canvas.transform.localScale.y == 0)
                {
                    Debug.LogWarning($"QuestUI: Canvas имеет неправильный масштаб {canvas.transform.localScale}! Исправляю...");
                    canvas.transform.localScale = Vector3.one;
                }
                
                // Если Canvas в режиме Screen Space - Camera, проверяем камеру
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                {
                    Debug.LogWarning("QuestUI: Canvas в режиме Screen Space - Camera, но камера не назначена! Назначаю Main Camera...");
                    canvas.worldCamera = Camera.main;
                    if (canvas.worldCamera == null)
                    {
                        canvas.worldCamera = FindFirstObjectByType<Camera>();
                    }
                }
                
                // Исправляем позицию панели, чтобы она не выходила за пределы экрана
                if (questPanel != null)
                {
                    RectTransform panelRect = questPanel.GetComponent<RectTransform>();
                    if (panelRect != null)
                    {
                        // Убеждаемся что панель использует правильные якоря
                        // Это предотвратит выход за пределы экрана
                        if (panelRect.anchorMin == Vector2.zero && panelRect.anchorMax == Vector2.zero)
                        {
                            // Если якоря не настроены, устанавливаем их в верхний левый угол
                            panelRect.anchorMin = new Vector2(0f, 1f);
                            panelRect.anchorMax = new Vector2(0f, 1f);
                            panelRect.pivot = new Vector2(0f, 1f);
                        }
                        
                        // Ограничиваем позицию, чтобы не выходить за пределы видимой области
                        Vector2 safePosition = panelRect.anchoredPosition;
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            // Для Screen Space Overlay ограничиваем позицию размерами экрана
                            float maxX = Screen.width - panelRect.sizeDelta.x;
                            float maxY = -Screen.height + panelRect.sizeDelta.y;
                            safePosition.x = Mathf.Clamp(safePosition.x, 0f, Mathf.Max(0f, maxX));
                            safePosition.y = Mathf.Clamp(safePosition.y, Mathf.Min(0f, maxY), 0f);
                        }
                        else
                        {
                            // Для других режимов используем относительные координаты
                            // Ограничиваем, чтобы панель не выходила за пределы
                            safePosition.x = Mathf.Max(0f, safePosition.x);
                            safePosition.y = Mathf.Min(0f, safePosition.y);
                        }
                        panelRect.anchoredPosition = safePosition;
                        
                        Debug.Log($"QuestUI: Позиция панели исправлена: {panelRect.anchoredPosition}, Size: {panelRect.sizeDelta}");
                    }
                }
                
                // Убеждаемся что Canvas активен
                if (!canvas.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("QuestUI: Canvas неактивен! Активирую...");
                    canvas.gameObject.SetActive(true);
                }
                
                // Убеждаемся что QuestPanel активен
                if (questPanel != null && !questPanel.activeInHierarchy)
                {
                    Debug.LogWarning("QuestUI: QuestPanel неактивен! Активирую...");
                    questPanel.SetActive(true);
                }
                
                // Убеждаемся что Text GameObject активен
                if (!questText.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("QuestUI: Text GameObject неактивен! Активирую...");
                    questText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("QuestUI: Canvas не найден! Text не может отображаться.");
            }
            
            Debug.Log($"QuestUI: Text компонент настроен. Font: {questText.font}, Size: {questText.fontSize}, RichText: {questText.supportRichText}, Enabled: {questText.enabled}, Active: {questText.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("QuestUI: Text компонент не найден! Квесты не будут отображаться.");
        }
        
        // Проверяем QuestSystem — если нет, создаём автоматически (pour les scènes sans hub)
        if (QuestSystem.Instance == null)
        {
            GameObject go = new GameObject("QuestSystem");
            go.AddComponent<QuestSystem>();
            Debug.Log("QuestUI: QuestSystem créé automatiquement.");
        }
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsChanged += UpdateQuestDisplay;
        }
        
        // Панель квестов всегда видима par défaut
        var questCanvas = GetComponent<Canvas>();
        if (questCanvas == null) questCanvas = GetComponentInParent<Canvas>();
        if (questCanvas != null)
        {
            questCanvas.enabled = true;
            questCanvas.gameObject.SetActive(true);
            if (questCanvas.transform.localScale.x == 0 || questCanvas.transform.localScale.y == 0)
                questCanvas.transform.localScale = Vector3.one;
        }
        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }
        if (questText != null && questText.gameObject != null)
        {
            questText.gameObject.SetActive(true);
        }
        
        UpdateQuestDisplay();
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsChanged -= UpdateQuestDisplay;
        }
    }
    
    void Update()
    {
        if (updateEveryFrame)
        {
            UpdateQuestDisplay();
        }
        else
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateQuestDisplay();
            }
        }
    }
    
    void UpdateQuestDisplay()
    {
        if (questText == null)
        {
            Debug.LogWarning("QuestUI: questText == null! Не могу отобразить квесты.");
            return;
        }
        
        // Панель всегда видима
        if (questPanel != null && !questPanel.activeSelf) questPanel.SetActive(true);

        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem == null || questSystem.activeQuests == null || questSystem.activeQuests.Count == 0)
        {
            questText.text = "<b>Quêtes:</b>\n\nAucune quête pour le moment.";
            return;
        }
        
        // Строим текст со всеми активными квестами
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Quêtes:</b>");
        sb.AppendLine();
        
        foreach (Quest quest in questSystem.activeQuests)
        {
            if (quest == null) continue;
            
            sb.AppendLine($"<b>{quest.title}</b>");
            
            for (int i = 0; i < quest.steps.Count; i++)
            {
                bool completed = i < quest.completedSteps.Count && quest.completedSteps[i];
                string checkmark = completed ? "✓" : "○";
                string stepText = quest.steps[i];
                if (quest.stepRequiredCounts != null && quest.stepCurrentCounts != null && i < quest.stepRequiredCounts.Count && quest.stepRequiredCounts[i] > 0)
                {
                    int cur = i < quest.stepCurrentCounts.Count ? quest.stepCurrentCounts[i] : 0;
                    int req = quest.stepRequiredCounts[i];
                    stepText = $"{stepText} ({cur}/{req})";
                }
                string assignedTo = "";
                if (quest.stepAssignments != null && i < quest.stepAssignments.Count && !string.IsNullOrEmpty(quest.stepAssignments[i]))
                {
                    assignedTo = $" <color=#88CCFF>({quest.stepAssignments[i]})</color>";
                }
                if (completed)
                {
                    sb.AppendLine($"  <color=green>{checkmark} {stepText}{assignedTo}</color>");
                }
                else
                {
                    sb.AppendLine($"  {checkmark} {stepText}{assignedTo}");
                }
            }
            
            sb.AppendLine();
        }
        
        questText.text = sb.ToString();
        if (questText.gameObject != null && !questText.gameObject.activeSelf)
            questText.gameObject.SetActive(true);
    }
    
    // Метод для принудительного обновления (можно вызывать извне)
    public void Refresh()
    {
        UpdateQuestDisplay();
    }
}
