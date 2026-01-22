using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GameQuests;
#if UNITY_TMPRO
using TMPro;
#endif

public class ClothesLineGame : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public GameObject gameCanvas; // Панель мини-игры
    public Camera uiCamera; // Камера для UI (если Screen Space - Camera)
    public Slider heightSlider; // Слайдер, по которому движется иконка одежды
    public Image clothesIcon; // Иконка одежды, которая движется по слайдеру
    public Text instructionText; // Текст инструкций (Legacy)
    public Text feedbackText; // Текст обратной связи (Legacy)
#if UNITY_TMPRO
    public TextMeshProUGUI instructionTextTMP; // Текст инструкций (TextMeshPro)
    public TextMeshProUGUI feedbackTextTMP; // Текст обратной связи (TextMeshPro)
#endif

    [Header("Настройки игры")]
    public float iconSpeed = 0.8f; // Скорость движения иконки (УМЕНЬШЕНА для пожилых учителей)
    public float targetZoneStart = 0.3f; // Начало зоны успеха (УВЕЛИЧЕНО)
    public float targetZoneEnd = 0.7f; // Конец зоны успеха (УВЕЛИЧЕНО)
    public int totalClothesItems = 3; // Количество элементов одежды
    public float timeBetweenItems = 2f; // Время между элементами (пауза)
    
    [Header("Иконки одежды")]
    public Sprite[] clothesIcons; // Массив спрайтов иконок одежды (3 элемента)
    public Color[] clothesColors; // Цвета для иконок (если нет спрайтов)

    [Header("Аудио")]
    public AudioSource voiceSource;
    public AudioClip[] goodPhrases; // Фразы успеха
    public AudioClip[] badPhrases; // Фразы неудачи
    public AudioClip hintSound; // Звук подсказки "нажимай сейчас" (для слепой женщины)
    public AudioClip itemStartSound; // Звук начала нового элемента

    private bool isPlaying = false;
    private float currentPosition = 0f;
    private bool movingRight = true;
    private bool canPressSpace = false; // Можно ли нажимать пробел сейчас
    private int currentItemIndex = 0; // Текущий элемент одежды (0, 1, 2)
    private int successfulItems = 0; // Успешно повешенные элементы
    private bool waitingForNextItem = false; // Ожидание следующего элемента
    
    // Статический флаг для блокировки других обработчиков E
    public static bool IsMiniGameActive { get; private set; } = false;

    void Start()
    {
        // Находим камеру, если не назначена
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            if (uiCamera == null) uiCamera = FindFirstObjectByType<Camera>();
        }
    }

    void Update()
    {
        if (isPlaying && !waitingForNextItem)
        {
            // Движение иконки одежды по слайдеру (МЕДЛЕННЕЕ)
            if (movingRight)
            {
                currentPosition += Time.deltaTime * iconSpeed;
                if (currentPosition >= 1f)
                {
                    currentPosition = 1f;
                    movingRight = false;
                }
            }
            else
            {
                currentPosition -= Time.deltaTime * iconSpeed;
                if (currentPosition <= 0f)
                {
                    currentPosition = 0f;
                    movingRight = true;
                }
            }
            
            heightSlider.value = currentPosition;
            
            // Обновляем позицию иконки одежды
            if (clothesIcon != null && heightSlider != null)
            {
                RectTransform sliderRect = heightSlider.GetComponent<RectTransform>();
                RectTransform iconRect = clothesIcon.GetComponent<RectTransform>();
                if (sliderRect != null && iconRect != null)
                {
                    // Убеждаемся, что иконка - дочерний элемент слайдера
                    if (iconRect.parent != sliderRect)
                    {
                        iconRect.SetParent(sliderRect, false);
                    }
                    
                    // Ограничиваем позицию в пределах 0-1
                    float clampedPosition = Mathf.Clamp01(currentPosition);
                    
                    // Используем anchors для позиционирования
                    iconRect.anchorMin = new Vector2(clampedPosition, 0.5f);
                    iconRect.anchorMax = new Vector2(clampedPosition, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(0, 50f);
                    
                    // Убеждаемся, что иконка не выходит за пределы слайдера
                    float sliderWidth = sliderRect.rect.width;
                    if (sliderWidth > 0)
                    {
                        float iconWidth = iconRect.rect.width;
                        float maxX = (sliderWidth - iconWidth) * 0.5f;
                        float minX = -maxX;
                        
                        Vector2 pos = iconRect.anchoredPosition;
                        pos.x = Mathf.Clamp(pos.x, minX, maxX);
                        iconRect.anchoredPosition = pos;
                    }
                }
            }
            
            // Проверяем, находится ли иконка в зоне успеха (для звуковой подсказки)
            bool inTargetZone = currentPosition >= targetZoneStart && currentPosition <= targetZoneEnd;
            
            if (inTargetZone && !canPressSpace)
            {
                canPressSpace = true;
                // Звуковая подсказка для слепой женщины
                PlayHintSound();
                SetFeedbackText("Appuie sur E maintenant!", Color.green);
            }
            else if (!inTargetZone && canPressSpace)
            {
                canPressSpace = false;
                SetFeedbackText("", Color.white);
            }
            
            // Игрок нажимает E (для женщины)
            // Блокируем другие обработчики E, когда мини-игра активна
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Поглощаем событие, чтобы другие скрипты не обработали E
                CheckPlacement(currentPosition);
            }
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (GlobalPlayerState.currentItem == HeldItemType.WetPile)
        {
            StartGame();
        }
        else
        {
            SetInstructionText("Il faut des vêtements mouillés!");
        }
    }

    void StartGame()
    {
        if (gameCanvas == null)
        {
            Debug.LogError("ClothesLineGame: gameCanvas не назначен!");
            return;
        }
        
        // Активируем панель
        gameCanvas.SetActive(true);
        
        // Центрируем панель относительно камеры
        CenterPanelToCamera();
        
        // Сбрасываем состояние
        isPlaying = true;
        IsMiniGameActive = true; // Блокируем другие обработчики E
        currentPosition = 0f;
        movingRight = true;
        canPressSpace = false;
        currentItemIndex = 0;
        successfulItems = 0;
        waitingForNextItem = false;
        
        // Обновляем иконку для первого элемента
        UpdateClothesIcon(0);
        
        // Разблокируем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        SetInstructionText($"Accroche les vêtements sur la corde à linge! ({currentItemIndex + 1}/{totalClothesItems})");
        SetFeedbackText("Écoute le son et appuie sur E quand tu l'entends!", Color.yellow);
        
        // Звук начала первого элемента
        if (itemStartSound != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(itemStartSound);
        }
    }
    
    void UpdateClothesIcon(int itemIndex)
    {
        if (clothesIcon == null) return;
        
        // Устанавливаем спрайт или цвет для текущего элемента
        if (clothesIcons != null && itemIndex < clothesIcons.Length && clothesIcons[itemIndex] != null)
        {
            clothesIcon.sprite = clothesIcons[itemIndex];
        }
        else if (clothesColors != null && itemIndex < clothesColors.Length)
        {
            clothesIcon.color = clothesColors[itemIndex];
        }
        else
        {
            // Цвета по умолчанию
            Color[] defaultColors = { new Color(1f, 0.5f, 0f), new Color(0.5f, 0.8f, 1f), new Color(1f, 0.8f, 0.5f) };
            if (itemIndex < defaultColors.Length)
            {
                clothesIcon.color = defaultColors[itemIndex];
            }
        }
    }
    
    void CenterPanelToCamera()
    {
        if (gameCanvas == null) return;
        
        Canvas canvas = gameCanvas.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        
        // Если Canvas в режиме Screen Space - Camera, настраиваем его (как у корзины)
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
                if (uiCamera == null) uiCamera = FindFirstObjectByType<Camera>();
            }
            
            if (uiCamera != null)
            {
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1f; // Расстояние от камеры
            }
        }
        
        // Настраиваем панель - по центру с полями 5% (как у корзины)
        RectTransform panelRect = gameCanvas.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.05f, 0.05f); // Поля 5% (как у корзины)
            panelRect.anchorMax = new Vector2(0.95f, 0.95f); // Поля 5% (как у корзины)
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
        }
    }

    void CheckPlacement(float value)
    {
        // Проверяем, попал ли в зону успеха (УВЕЛИЧЕННАЯ зона)
        bool inTargetZone = value >= targetZoneStart && value <= targetZoneEnd;

        if (inTargetZone)
        {
            // Успех для текущего элемента!
            successfulItems++;
            PlayVoice(goodPhrases);
            SetFeedbackText($"Parfait! Élément {currentItemIndex + 1} accroché! ({successfulItems}/{totalClothesItems})", Color.green);
            
            // Проверяем, все ли элементы повешены
            if (successfulItems >= totalClothesItems)
            {
                // Все элементы повешены - игра завершена
            EndGame(true);
        }
        else
        {
                // Переходим к следующему элементу
                StartCoroutine(NextItemRoutine());
            }
        }
        else
        {
            // Неудача
            PlayVoice(badPhrases);
            float center = (targetZoneStart + targetZoneEnd) * 0.5f;
            if (value < center)
            {
                SetFeedbackText("Trop bas! Écoute le son et appuie quand tu l'entends.", Color.red);
            }
            else
            {
                SetFeedbackText("Trop haut! Écoute le son et appuie quand tu l'entends.", Color.red);
            }
            
            // Сбрасываем подсказку
            canPressSpace = false;
            
            // Игра продолжается для того же элемента
        }
    }
    
    IEnumerator NextItemRoutine()
    {
        waitingForNextItem = true;
        canPressSpace = false;
        
        // Пауза перед следующим элементом
        yield return new WaitForSeconds(timeBetweenItems);
        
        // Переходим к следующему элементу
        currentItemIndex++;
        currentPosition = 0f;
        movingRight = true;
        
        // Обновляем иконку
        UpdateClothesIcon(currentItemIndex);
        
        // Обновляем текст
        SetInstructionText($"Accroche les vêtements sur la corde à linge! ({currentItemIndex + 1}/{totalClothesItems})");
        SetFeedbackText("Écoute le son et appuie sur E quand tu l'entends!", Color.yellow);
        
        // Звук начала нового элемента
        if (itemStartSound != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(itemStartSound);
        }
        
        waitingForNextItem = false;
    }
    
    void PlayHintSound()
    {
        // Звуковая подсказка для слепой женщины - когда можно нажимать
        if (hintSound != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(hintSound);
        }
    }

    void PlayVoice(AudioClip[] clips)
    {
        if (clips.Length > 0)
        {
            voiceSource.clip = clips[Random.Range(0, clips.Length)];
            voiceSource.Play();
        }
    }

    void EndGame(bool success)
    {
        isPlaying = false;
        IsMiniGameActive = false; // Разблокируем другие обработчики E
        
        if(success)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null)
            {
                player.DropItem(); // Белье повешено
            }
            
            // Обновляем квест
            UpdateQuestStep(3); // Шаг 3: Повесь одежду на сушилку - выполнен
            
            // Задержка перед закрытием UI, чтобы дослушать фразу
            Invoke("CloseUI", 2f);
        }
        else
        {
            // Если неудача, игра продолжается (не закрываем UI)
        }
    }
    
    void UpdateQuestStep(int stepIndex)
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem != null && questSystem.activeQuests.Count > 0)
        {
            Quest quest = questSystem.activeQuests[0]; // Первый квест - стирка
            if (quest != null)
            {
                quest.CompleteStep(stepIndex);
            }
        }
    }

    void CloseUI()
    {
        if (gameCanvas != null)
    {
        gameCanvas.SetActive(false);
        }
        
        // Разблокируем другие обработчики E
        IsMiniGameActive = false;
        isPlaying = false;
        
        // Блокируем курсор обратно
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Вспомогательные методы для установки текста
    void SetInstructionText(string text)
    {
#if UNITY_TMPRO
        if (instructionTextTMP != null)
            instructionTextTMP.text = text;
        else
#endif
        if (instructionText != null)
            instructionText.text = text;
    }
    
    void SetFeedbackText(string text, Color color)
    {
#if UNITY_TMPRO
        if (feedbackTextTMP != null)
        {
            feedbackTextTMP.text = text;
            feedbackTextTMP.color = color;
        }
        else
#endif
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
        }
    }
}