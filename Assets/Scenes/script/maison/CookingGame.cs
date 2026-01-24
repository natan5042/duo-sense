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
    public float beatInterval = 2f; // Интервал между битами ритма (увеличено для более медленной игры)
    public float noteSpeed = 200f; // Скорость падения нот
    public float noteSpawnInterval = 1.5f; // Интервал между нотами
    public float hitWindow = 1.2f; // Окно для попадания (увеличено - зеленая панелька будет светиться дольше)
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
        
        // Автоматически находим или создаем AudioSource для звуков
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                // Ищем на дочерних объектах
                AudioSource[] allSources = GetComponentsInChildren<AudioSource>(true);
                if (allSources.Length > 0)
                {
                    sfxSource = allSources[0];
                    Debug.Log($"CookingGame: Найден AudioSource на дочернем объекте: {sfxSource.gameObject.name}");
                }
                else
                {
                    // Создаем новый только если не нашли
                    sfxSource = gameObject.AddComponent<AudioSource>();
                    sfxSource.playOnAwake = false;
                    sfxSource.spatialBlend = 0f; // 2D звук
                    Debug.Log("CookingGame: Создан новый sfxSource");
                }
            }
        }
        
        // Убеждаемся что AudioSource включен и настроен правильно
        if (sfxSource != null)
        {
            sfxSource.enabled = true;
            if (sfxSource.volume == 0f)
            {
                sfxSource.volume = 1f; // Устанавливаем громкость только если она 0
            }
            sfxSource.mute = false;
            Debug.Log($"CookingGame: sfxSource настроен. enabled: {sfxSource.enabled}, volume: {sfxSource.volume}");
        }
        
        if (musicSource == null)
        {
            // Ищем AudioSource на дочерних объектах
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
            foreach (var source in sources)
            {
                if (source != sfxSource)
                {
                    musicSource = source;
                    break;
                }
            }
        }
        
        if (musicSource != null)
        {
            musicSource.enabled = true;
            if (musicSource.volume == 0f)
            {
                musicSource.volume = 1f;
            }
            musicSource.mute = false;
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
        if (rhythmBeatSound == null)
        {
            Debug.LogWarning("CookingGame: rhythmBeatSound не назначен в Inspector!");
            return;
        }
        
        if (sfxSource == null)
        {
            Debug.LogWarning("CookingGame: sfxSource не назначен! Пытаюсь найти...");
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
                if (sources.Length > 0) sfxSource = sources[0];
            }
        }
        
        if (sfxSource == null)
        {
            Debug.LogError("CookingGame: sfxSource == null! Не могу воспроизвести звук.");
            return;
        }
        
        // Убеждаемся что AudioSource активен и настроен
        if (!sfxSource.enabled)
        {
            Debug.LogWarning("CookingGame: sfxSource выключен! Включаю...");
            sfxSource.enabled = true;
        }
        
        if (sfxSource.volume <= 0f)
        {
            Debug.LogWarning("CookingGame: sfxSource.volume = 0! Устанавливаю 1...");
            sfxSource.volume = 1f;
        }
        
        if (sfxSource.mute)
        {
            Debug.LogWarning("CookingGame: sfxSource.mute = true! Выключаю...");
            sfxSource.mute = false;
        }
        
        // Проверяем что GameObject активен
        if (!sfxSource.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"CookingGame: GameObject с sfxSource неактивен! {sfxSource.gameObject.name}");
        }
        
        try
        {
            sfxSource.PlayOneShot(rhythmBeatSound);
            Debug.Log($"CookingGame: ✓ Звук ритма воспроизведен: {rhythmBeatSound.name}, Volume: {sfxSource.volume}, Enabled: {sfxSource.enabled}, Mute: {sfxSource.mute}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CookingGame: Ошибка воспроизведения звука: {e.Message}");
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
                        continue;
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
    
    IEnumerator UpdateHeartsAfterCanvasActivation()
    {
        // Ждем один кадр чтобы Canvas точно активировался
        yield return null;
        UpdateWomanHearts();
        UpdateManHearts();
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
                // Убеждаемся что Image активен
                if (!womanHearts[i].gameObject.activeInHierarchy)
                {
                    womanHearts[i].gameObject.SetActive(true);
                }
                
                if (!womanHearts[i].enabled)
                {
                    womanHearts[i].enabled = true;
                }
                
                // Определяем какой спрайт использовать
                bool shouldBeFull = i < womanLives;
                Sprite targetSprite = shouldBeFull ? heartFull : heartEmpty;
                
                // Обновляем спрайт (всегда, не только если изменился)
                womanHearts[i].sprite = targetSprite;
                
                // Принудительно обновляем Image
                womanHearts[i].SetNativeSize();
                womanHearts[i].SetAllDirty();
                
                // Уменьшаем размер в 2.5 раза
                RectTransform heartRect = womanHearts[i].GetComponent<RectTransform>();
                if (heartRect != null)
                {
                    heartRect.localScale = Vector3.one * (1f / 2.5f); // 0.4 = 1/2.5
                }
                
                // Устанавливаем цвет (полное = белый, пустое = полупрозрачное)
                womanHearts[i].color = shouldBeFull ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }
        
        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
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
                // Убеждаемся что Image активен
                if (!manHearts[i].gameObject.activeInHierarchy)
                {
                    manHearts[i].gameObject.SetActive(true);
                }
                
                if (!manHearts[i].enabled)
                {
                    manHearts[i].enabled = true;
                }
                
                // Определяем какой спрайт использовать
                bool shouldBeFull = i < manLives;
                Sprite targetSprite = shouldBeFull ? heartFull : heartEmpty;
                
                // Обновляем спрайт
                manHearts[i].sprite = targetSprite;
                
                // Принудительно обновляем Image
                manHearts[i].SetNativeSize();
                manHearts[i].SetAllDirty();
                
                // Уменьшаем размер в 2.5 раза
                RectTransform heartRect = manHearts[i].GetComponent<RectTransform>();
                if (heartRect != null)
                {
                    heartRect.localScale = Vector3.one * (1f / 2.5f); // 0.4 = 1/2.5
                }
                
                // Устанавливаем цвет (полное = белый, пустое = полупрозрачное)
                manHearts[i].color = shouldBeFull ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }
        
        // Принудительно обновляем Canvas
        Canvas.ForceUpdateCanvases();
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
        
        // Обновляем сердечки при старте игры
        UpdateWomanHearts();
        UpdateManHearts();
        
        Debug.Log($"CookingGame: Игра начата. Жизни: женщина={womanLives}, мужчина={manLives}");
        
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
        
        // Обновляем сердечки после активации Canvas (с небольшой задержкой для гарантии)
        StartCoroutine(UpdateHeartsAfterCanvasActivation());
        
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
            Debug.Log("CookingGame: Музыка запущена");
        }
        else
        {
            Debug.LogWarning("CookingGame: musicSource не назначен! Музыка не будет играть.");
        }
        
        // Проверяем что звуки настроены
        if (sfxSource == null)
        {
            Debug.LogError("CookingGame: sfxSource не назначен! Звуки не будут работать.");
        }
        else if (rhythmBeatSound == null)
        {
            Debug.LogWarning("CookingGame: rhythmBeatSound не назначен в Inspector! Звук ритма не будет играть.");
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
        if (questSystem == null)
        {
            Debug.LogWarning("CookingGame: QuestSystem.Instance == null! Квест не может быть завершен.");
            return;
        }
        
        // Ищем квест готовки
        Quest foundQuest = null;
        foreach (Quest quest in questSystem.activeQuests)
        {
            if (quest != null && quest.questName == "Préparer des œufs au plat")
            {
                foundQuest = quest;
                break;
            }
        }
        
        if (foundQuest == null)
        {
            Debug.LogWarning("CookingGame: Квест 'Préparer des œufs au plat' не найден в активных квестах!");
            return;
        }
        
        // Проверяем что есть второй шаг
        if (foundQuest.steps.Count <= 1)
        {
            Debug.LogWarning($"CookingGame: У квеста только {foundQuest.steps.Count} шаг(ов), не могу завершить шаг 1!");
            return;
        }
        
        // Завершаем второй шаг (индекс 1)
        foundQuest.CompleteStep(1);
        Debug.Log("CookingGame: ✓ Шаг 2 квеста готовки выполнен!");
        
        // Если весь квест выполнен, удаляем его
        if (foundQuest.IsCompleted())
        {
            questSystem.CompleteQuest(foundQuest);
            Debug.Log("CookingGame: ✓✓✓ Квест готовки полностью выполнен и удален из системы!");
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
