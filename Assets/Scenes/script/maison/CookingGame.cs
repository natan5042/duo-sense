using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GameQuests;

/// <summary>
/// Ритм-игра на плите - разделённый экран
/// Левая половина: женщина (WASD) - ритм по звуку
/// Правая половина: мужик (стрелки) - падающие ноты
/// </summary>
public class CookingGame : MonoBehaviour, IInteractable
{
    [Header("UI Панель")]
    public GameObject cookingCanvas;
    public Camera uiCamera;
    
    [Header("Левая панель (Женщина - ритм)")]
    public RectTransform womanPanel;
    public Image rhythmIndicator; // Индикатор ритма (пульсирует)
    public Text womanInstructionText;
    public Image[] womanHearts; // 3 сердечка
    public Sprite heartFull;
    public Sprite heartEmpty;
    
    [Header("Правая панель (Мужик - ноты)")]
    public RectTransform manPanel;
    public RectTransform notesContainer; // Контейнер для падающих нот
    public RectTransform hitZoneLeft; // Зона попадания левой стрелки
    public RectTransform hitZoneRight; // Зона попадания правой стрелки
    public Image leftArrowIndicator; // Индикатор левой стрелки
    public Image rightArrowIndicator; // Индикатор правой стрелки
    public Text manInstructionText;
    public Image[] manHearts; // 3 сердечка
    
    [Header("Общее")]
    public Text resultText;
    public GameObject gameOverPanel;
    public GameObject successPanel;
    
    [Header("Префабы нот")]
    public GameObject leftNotePrefab;
    public GameObject rightNotePrefab;
    
    [Header("Аудио")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip rhythmBeatSound; // Звук ритма для женщины
    public AudioClip hitSound; // Звук попадания
    public AudioClip missSound; // Звук промаха
    public AudioClip successSound;
    public AudioClip failSound;
    
    [Header("Настройки игры")]
    public float gameDuration = 30f; // Длительность игры в секундах
    public float beatInterval = 1f; // Интервал между битами ритма
    public float noteSpeed = 200f; // Скорость падения нот
    public float noteSpawnInterval = 1.5f; // Интервал между нотами
    public float hitWindow = 0.3f; // Окно для попадания (в секундах)
    public int requiredSuccessfulHits = 10; // Сколько попаданий нужно для победы
    
    [Header("Визуальные эффекты")]
    public Color hitZoneNormalColor = new Color(1f, 1f, 1f, 0.3f);
    public Color hitZoneActiveColor = new Color(0f, 1f, 0f, 0.6f);
    public float rhythmPulseScale = 1.3f;
    
    // Статический флаг
    public static bool IsCookingGameActive { get; private set; } = false;
    
    // Состояние игры
    private bool isPlaying = false;
    private float gameTimer = 0f;
    private float beatTimer = 0f;
    private float noteSpawnTimer = 0f;
    
    // Очки
    private int womanLives = 3;
    private int manLives = 3;
    private int womanHits = 0;
    private int manHits = 0;
    
    // Ритм для женщины
    private bool canWomanPress = false;
    private float womanPressWindow = 0f;
    
    // Ноты для мужика
    private List<FallingNote> activeNotes = new List<FallingNote>();
    
    void Start()
    {
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            if (uiCamera == null) uiCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    void Update()
    {
        if (!isPlaying) return;
        
        gameTimer += Time.deltaTime;
        
        // Проверяем окончание игры
        if (gameTimer >= gameDuration || womanLives <= 0 || manLives <= 0)
        {
            EndGame();
            return;
        }
        
        // === ЖЕНЩИНА: Ритм по звуку ===
        UpdateWomanRhythm();
        
        // === МУЖИК: Падающие ноты ===
        UpdateManNotes();
    }
    
    void UpdateWomanRhythm()
    {
        beatTimer += Time.deltaTime;
        
        // Время для нового бита
        if (beatTimer >= beatInterval)
        {
            beatTimer = 0f;
            StartRhythmBeat();
        }
        
        // Обновляем окно для нажатия
        if (canWomanPress)
        {
            womanPressWindow -= Time.deltaTime;
            
            if (womanPressWindow <= 0f)
            {
                // Пропустила бит
                canWomanPress = false;
                WomanMiss();
            }
        }
        
        // Обрабатываем ввод женщины (E или пробел)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (canWomanPress)
            {
                WomanHit();
            }
            else
            {
                // Нажала не вовремя
                WomanMiss();
            }
        }
        
        // Визуальный пульс индикатора ритма
        if (rhythmIndicator != null)
        {
            float pulse = canWomanPress ? rhythmPulseScale : 1f;
            rhythmIndicator.transform.localScale = Vector3.Lerp(
                rhythmIndicator.transform.localScale,
                Vector3.one * pulse,
                Time.deltaTime * 10f
            );
            
            // Цвет индикатора
            rhythmIndicator.color = canWomanPress ? Color.green : Color.white;
        }
    }
    
    void StartRhythmBeat()
    {
        canWomanPress = true;
        womanPressWindow = hitWindow;
        
        // Проигрываем звук ритма
        if (sfxSource != null && rhythmBeatSound != null)
        {
            sfxSource.PlayOneShot(rhythmBeatSound);
        }
        
        // Визуальный эффект
        if (rhythmIndicator != null)
        {
            rhythmIndicator.transform.localScale = Vector3.one * rhythmPulseScale;
        }
    }
    
    void WomanHit()
    {
        canWomanPress = false;
        womanHits++;
        
        if (sfxSource != null && hitSound != null)
        {
            sfxSource.PlayOneShot(hitSound);
        }
        
        SetWomanFeedback("Bien!", Color.green);
    }
    
    void WomanMiss()
    {
        canWomanPress = false;
        womanLives--;
        UpdateWomanHearts();
        
        if (sfxSource != null && missSound != null)
        {
            sfxSource.PlayOneShot(missSound);
        }
        
        SetWomanFeedback("Raté!", Color.red);
    }
    
    void UpdateManNotes()
    {
        noteSpawnTimer += Time.deltaTime;
        
        // Спавним новую ноту
        if (noteSpawnTimer >= noteSpawnInterval)
        {
            noteSpawnTimer = 0f;
            SpawnNote();
        }
        
        // Обновляем все активные ноты
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            FallingNote note = activeNotes[i];
            
            if (note == null || note.noteObject == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }
            
            // Двигаем ноту вниз
            RectTransform noteRect = note.noteObject.GetComponent<RectTransform>();
            if (noteRect != null)
            {
                noteRect.anchoredPosition += Vector2.down * noteSpeed * Time.deltaTime;
                
                // Проверяем, достигла ли нота зоны попадания
                RectTransform hitZone = note.isLeft ? hitZoneLeft : hitZoneRight;
                
                if (hitZone != null)
                {
                    float noteY = noteRect.anchoredPosition.y;
                    float zoneY = hitZone.anchoredPosition.y;
                    
                    // В зоне попадания
                    if (Mathf.Abs(noteY - zoneY) < 50f)
                    {
                        note.canBeHit = true;
                        
                        // Подсветка зоны
                        Image zoneImage = hitZone.GetComponent<Image>();
                        if (zoneImage != null)
                        {
                            zoneImage.color = hitZoneActiveColor;
                        }
                    }
                    else if (note.canBeHit && noteY < zoneY - 50f)
                    {
                        // Пропустил ноту
                        note.canBeHit = false;
                        ManMiss();
                        DestroyNote(note, i);
                        
                        // Убираем подсветку
                        Image zoneImage = hitZone.GetComponent<Image>();
                        if (zoneImage != null)
                        {
                            zoneImage.color = hitZoneNormalColor;
                        }
                    }
                    
                    // Удаляем ноту если ушла за экран
                    if (noteY < -400f)
                    {
                        DestroyNote(note, i);
                    }
                }
            }
        }
        
        // Обрабатываем ввод мужика (стрелки)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryHitNote(true);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryHitNote(false);
        }
    }
    
    void SpawnNote()
    {
        if (notesContainer == null) return;
        
        // Случайно выбираем левую или правую ноту
        bool isLeft = Random.value > 0.5f;
        GameObject prefab = isLeft ? leftNotePrefab : rightNotePrefab;
        
        if (prefab == null) return;
        
        GameObject noteObj = Instantiate(prefab, notesContainer);
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        
        if (noteRect != null)
        {
            // Позиционируем над соответствующей зоной
            RectTransform hitZone = isLeft ? hitZoneLeft : hitZoneRight;
            if (hitZone != null)
            {
                float startY = 300f; // Начинаем сверху
                noteRect.anchoredPosition = new Vector2(hitZone.anchoredPosition.x, startY);
            }
        }
        
        FallingNote note = new FallingNote
        {
            noteObject = noteObj,
            isLeft = isLeft,
            canBeHit = false
        };
        
        activeNotes.Add(note);
    }
    
    void TryHitNote(bool left)
    {
        // Ищем ноту, которую можно ударить
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            FallingNote note = activeNotes[i];
            
            if (note.isLeft == left && note.canBeHit)
            {
                ManHit();
                DestroyNote(note, i);
                
                // Убираем подсветку
                RectTransform hitZone = left ? hitZoneLeft : hitZoneRight;
                if (hitZone != null)
                {
                    Image zoneImage = hitZone.GetComponent<Image>();
                    if (zoneImage != null)
                    {
                        zoneImage.color = hitZoneNormalColor;
                    }
                }
                return;
            }
        }
        
        // Не попал ни в одну ноту
        ManMiss();
    }
    
    void ManHit()
    {
        manHits++;
        
        if (sfxSource != null && hitSound != null)
        {
            sfxSource.PlayOneShot(hitSound);
        }
        
        SetManFeedback("Super!", Color.green);
    }
    
    void ManMiss()
    {
        manLives--;
        UpdateManHearts();
        
        if (sfxSource != null && missSound != null)
        {
            sfxSource.PlayOneShot(missSound);
        }
        
        SetManFeedback("Raté!", Color.red);
    }
    
    void DestroyNote(FallingNote note, int index)
    {
        if (note.noteObject != null)
        {
            Destroy(note.noteObject);
        }
        activeNotes.RemoveAt(index);
    }
    
    void UpdateWomanHearts()
    {
        if (womanHearts == null || womanHearts.Length == 0)
        {
            Debug.LogWarning("CookingGame: womanHearts не назначен!");
            return;
        }
        
        if (heartFull == null || heartEmpty == null)
        {
            Debug.LogWarning("CookingGame: heartFull или heartEmpty не назначены в Inspector!");
            return;
        }
        
        for (int i = 0; i < womanHearts.Length; i++)
        {
            if (womanHearts[i] != null)
            {
                Sprite targetSprite = (i < womanLives) ? heartFull : heartEmpty;
                if (womanHearts[i].sprite != targetSprite)
                {
                    womanHearts[i].sprite = targetSprite;
                    // Принудительно обновляем через Canvas
                    Canvas.ForceUpdateCanvases();
                }
            }
        }
    }
    
    void UpdateManHearts()
    {
        if (manHearts == null || manHearts.Length == 0)
        {
            Debug.LogWarning("CookingGame: manHearts не назначен!");
            return;
        }
        
        if (heartFull == null || heartEmpty == null)
        {
            Debug.LogWarning("CookingGame: heartFull или heartEmpty не назначены в Inspector!");
            return;
        }
        
        for (int i = 0; i < manHearts.Length; i++)
        {
            if (manHearts[i] != null)
            {
                Sprite targetSprite = (i < manLives) ? heartFull : heartEmpty;
                if (manHearts[i].sprite != targetSprite)
                {
                    manHearts[i].sprite = targetSprite;
                    // Принудительно обновляем через Canvas
                    Canvas.ForceUpdateCanvases();
                }
            }
        }
    }
    
    void SetWomanFeedback(string text, Color color)
    {
        if (womanInstructionText != null)
        {
            womanInstructionText.text = text;
            womanInstructionText.color = color;
            StartCoroutine(ClearFeedback(womanInstructionText, 0.5f));
        }
    }
    
    void SetManFeedback(string text, Color color)
    {
        if (manInstructionText != null)
        {
            manInstructionText.text = text;
            manInstructionText.color = color;
            StartCoroutine(ClearFeedback(manInstructionText, 0.5f));
        }
    }
    
    IEnumerator ClearFeedback(Text textComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (textComponent != null)
        {
            textComponent.text = "";
        }
    }
    
    public void Interact(PlayerInteraction player)
    {
        // Проверяем, есть ли продукт
        if (!CookingState.hasProduct)
        {
            Debug.Log("CookingGame: Нужен продукт для готовки!");
            return;
        }
        
        StartCooking();
    }
    
    public void StartCooking()
    {
        if (isPlaying) return;
        
        isPlaying = true;
        IsCookingGameActive = true;
        
        // Сбрасываем состояние
        gameTimer = 0f;
        beatTimer = 0f;
        noteSpawnTimer = 0f;
        womanLives = 3;
        manLives = 3;
        womanHits = 0;
        manHits = 0;
        canWomanPress = false;
        
        // Очищаем ноты
        foreach (var note in activeNotes)
        {
            if (note.noteObject != null)
            {
                Destroy(note.noteObject);
            }
        }
        activeNotes.Clear();
        
        // Обновляем UI
        if (cookingCanvas != null)
        {
            cookingCanvas.SetActive(true);
        }
        
        // Обновляем сердечки после активации Canvas
        UpdateWomanHearts();
        UpdateManHearts();
        
        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
        
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
        
        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Запускаем музыку
        if (musicSource != null)
        {
            musicSource.Play();
        }
    }
    
    void EndGame()
    {
        isPlaying = false;
        IsCookingGameActive = false;
        
        // Останавливаем музыку
        if (musicSource != null)
        {
            musicSource.Stop();
        }
        
        // Определяем результат
        bool success = womanLives > 0 && manLives > 0;
        
        if (success)
        {
            // Победа!
            if (successPanel != null) successPanel.SetActive(true);
            if (sfxSource != null && successSound != null)
            {
                sfxSource.PlayOneShot(successSound);
            }
            
            if (resultText != null)
            {
                resultText.text = $"Bravo!\nFemme: {womanHits} points\nHomme: {manHits} points";
                resultText.color = Color.green;
            }
            
            // Используем продукт
            CookingState.DropProduct();
            
            // Завершаем второй шаг квеста готовки
            CompleteCookingQuestStep();
            
            // Закрываем с задержкой
            StartCoroutine(CloseAfterDelay(3f));
        }
        else
        {
            // Проигрыш
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (sfxSource != null && failSound != null)
            {
                sfxSource.PlayOneShot(failSound);
            }
            
            string loser = womanLives <= 0 ? "La femme" : "L'homme";
            if (resultText != null)
            {
                resultText.text = $"Perdu!\n{loser} a fait trop d'erreurs!";
                resultText.color = Color.red;
            }
        }
    }
    
    IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseCooking();
    }
    
    public void CloseCooking()
    {
        isPlaying = false;
        IsCookingGameActive = false;
        
        if (cookingCanvas != null)
        {
            cookingCanvas.SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// Кнопка "Повторить" в панели Game Over
    /// </summary>
    public void RetryGame()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        StartCooking();
    }
    
    /// <summary>
    /// Завершает второй шаг квеста готовки
    /// </summary>
    void CompleteCookingQuestStep()
    {
        QuestSystem questSystem = QuestSystem.Instance;
        if (questSystem != null)
        {
            // Ищем квест готовки
            foreach (Quest quest in questSystem.activeQuests)
            {
                if (quest.questName == "Cuisine" && quest.steps.Count > 1)
                {
                    quest.CompleteStep(1);
                    Debug.Log("CookingGame: Шаг 2 квеста готовки выполнен!");
                    
                    // Если весь квест выполнен, удаляем его
                    if (quest.IsCompleted())
                    {
                        questSystem.CompleteQuest(quest);
                        Debug.Log("CookingGame: Квест готовки полностью выполнен!");
                    }
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Данные о падающей ноте
/// </summary>
public class FallingNote
{
    public GameObject noteObject;
    public bool isLeft;
    public bool canBeHit;
}
